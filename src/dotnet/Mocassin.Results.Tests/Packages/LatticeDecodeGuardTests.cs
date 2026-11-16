// SPDX-License-Identifier: MIT

using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Results.Packages;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Exercises the lattice decode bound derived from the referencing job and model (amendment 1, A1-10 / R2-01).
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class LatticeDecodeGuardTests
{
    private const int derivedBoundForSmallLattice = 4 * 6 * 6 * 6 * 4;

    [Fact]
    public async Task ReferencedLatticeFrameAboveTheDerivedBoundIsPkg004()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 2);
        var bomb = CompressWithoutContentSize(new byte[1024 * 1024]);

        await PackageTestSupport.WriteLatticePayloadAsync(built.Path, latticeId: 1, bomb);

        var report = await new PackageVerifier().VerifyAsync(built.Path, new PackageVerificationOptions());

        Assert.False(report.IsValid);
        var diagnostic = Assert.Single(report.Diagnostics, item => item.Code == "PKG004");
        Assert.Contains($"above the {derivedBoundForSmallLattice} guard", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrphanLatticeFrameIsBoundedByTheFallback()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 2);
        var bomb = CompressWithoutContentSize(new byte[1024 * 1024]);

        await PackageTestSupport.InsertLatticeRowAsync(built.Path, latticeId: 99, new string('e', 64), bomb);

        var report = await new PackageVerifier().VerifyAsync(
            built.Path,
            new PackageVerificationOptions { MaxLatticePayloadBytes = 4096 });

        Assert.False(report.IsValid);
        var diagnostic = Assert.Single(report.Diagnostics, item => item.Code == "PKG004");
        Assert.Contains("above the 4096 guard", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrphanLatticeWithinTheFallbackStillDecodes()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 2);
        var legitimate = await ReadLatticePayloadAsync(built.Path, latticeId: 1);

        await PackageTestSupport.InsertLatticeRowAsync(built.Path, latticeId: 98, new string('e', 64), legitimate);

        var report = await new PackageVerifier().VerifyAsync(
            built.Path,
            new PackageVerificationOptions { MaxLatticePayloadBytes = 4096 });

        Assert.DoesNotContain(report.Diagnostics, item => item.Code == "PKG004");
        Assert.Equal(3, report.Inspection.LatticeCount);
    }

    [Fact]
    public async Task LegitimateLargeLatticePassesTheDerivedBound()
    {
        using var temp = new TemporaryDirectory();
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine(PackageTestSupport.Sample)));
        var compiled = new ModelCompiler().Compile(project).Model
            ?? throw new InvalidOperationException("compile failed");
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model
            ?? throw new InvalidOperationException("parameterization failed");
        var expansion = JobTemplateExpander.Expand(project, compiled, project.JobTemplates.Single());
        Assert.True(expansion.IsSuccess);
        var descriptor = expansion.Jobs[0] with { Lattice = new LatticeRequest(40, 40, 40) };
        var generated = SupercellGenerator.Generate(project, compiled, descriptor);
        Assert.True(generated.IsSuccess);
        Assert.Equal(256_000, generated.Lattice!.Particles.Length);

        var path = Path.Combine(temp.Path, "large-lattice.msl");
        var writer = SimulationPackageWriter.Create(
            new PackageBuildRequest(
                path,
                project.Metadata.ProjectId,
                "all",
                new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)),
            new PackageBuildOptions());
        await using (writer)
        {
            await writer.WriteModelAsync(applied);
            await writer.WriteJobAsync(new JobPackage(descriptor, generated.Lattice));
            await writer.CompleteAsync();
        }

        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(1, report.Inspection.LatticeCount);
        Assert.Equal(1, report.Inspection.JobCount);
        Assert.DoesNotContain(report.Diagnostics, item => item.Code == "PKG004");
    }

    private static async Task<byte[]> ReadLatticePayloadAsync(string path, long latticeId)
    {
        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM lattices WHERE lattice_id = $id;";
        command.Parameters.Add("$id", SqliteType.Integer).Value = latticeId;
        return (byte[])(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Builds a frame with an upper-bound (not exact) declared size and a large expansion.</summary>
    private static byte[] CompressWithoutContentSize(byte[] raw)
    {
        using var stream = new MemoryStream();
        using (var compressor = new ZstandardStream(stream, CompressionMode.Compress, leaveOpen: true))
        {
            compressor.Write(raw);
        }

        return stream.ToArray();
    }
}
