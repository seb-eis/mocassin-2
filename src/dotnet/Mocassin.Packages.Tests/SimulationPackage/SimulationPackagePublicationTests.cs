// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Mocassin.Contracts.Parameterization;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Packages.SimulationPackage.Internal;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;
using Mocassin.Packages.Tests.SimulationPackage.TestSupport;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Verifies the temporary-file, overwrite, cancellation, and publication-barrier guarantees with real files.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SimulationPackagePublicationTests
{
    [Fact]
    public void ExistingPackageWithoutOverwriteIsRejectedBeforeCreatingAnything()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB, 0xCC };
        File.WriteAllBytes(path, original);

        var exception = Assert.Throws<IOException>(() =>
            SimulationPackageWriter.Create(
                SimulationPackageTestSupport.Request(path),
                new PackageBuildOptions()));

        Assert.Contains("already exists", exception.Message, StringComparison.Ordinal);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task OverwriteReplacesTheExistingPackageAtomically()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB, 0xCC };
        File.WriteAllBytes(path, original);

        var result = await SimulationPackageTestSupport.BuildAsync(
            path,
            new PackageBuildOptions { Overwrite = true });

        Assert.False(original.AsSpan().SequenceEqual(File.ReadAllBytes(path)));
        Assert.Equal(result.Sha256, Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task DisposingWithoutCompletingDeletesTheTempFileAndPreservesTheTarget()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB, 0xCC };
        File.WriteAllBytes(path, original);

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { Overwrite = true },
            new PackageFileSystem());
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await writer.DisposeAsync();

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task CompleteRequiresOneModelAndAtLeastOneJob()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.CompleteAsync());
        await writer.DisposeAsync();
        Assert.Empty(temp.TemporaryFiles());

        writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.CompleteAsync());
        await writer.DisposeAsync();
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task ASecondModelRowIsARejectedContractError()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow()));
        await writer.DisposeAsync();

        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task AJobBeforeTheModelIsARejectedContractError()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.WriteJobRowAsync(SimulationPackageTestSupport.JobRow()));
        await writer.DisposeAsync();

        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task CancellationBeforeJobInsertLeavesNoTempFileAndPreservesTheTarget()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB };
        File.WriteAllBytes(path, original);

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { Overwrite = true },
            new PackageFileSystem());
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            writer.WriteJobRowAsync(SimulationPackageTestSupport.JobRow(), cancellation.Token));
        await writer.DisposeAsync();

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task CancellationBeforePublicationLeavesNoTempFileAndPreservesTheTarget()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB };
        File.WriteAllBytes(path, original);

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { Overwrite = true },
            new PackageFileSystem());
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await writer.WriteJobRowAsync(SimulationPackageTestSupport.JobRow());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.CompleteAsync(cancellation.Token));
        await writer.DisposeAsync();

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task BatchingCommitsMultipleJobsAndDeduplicatesLattices()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { BatchSize = 1 },
            new PackageFileSystem());
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await writer.WriteJobRowAsync(
            SimulationPackageTestSupport.JobRow(1, latticeSignature: "lattice-a"));
        await writer.WriteJobRowAsync(
            SimulationPackageTestSupport.JobRow(2, latticeSignature: "lattice-a"));
        await writer.WriteJobRowAsync(
            SimulationPackageTestSupport.JobRow(3, latticeSignature: "lattice-b"));
        var result = await writer.CompleteAsync();

        Assert.Equal(3, result.JobCount);
        Assert.Equal(2, result.LatticeCount);
        Assert.True(File.Exists(result.Path));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task VacuumOptionStillPublishesAValidPackage()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var result = await SimulationPackageTestSupport.BuildAsync(
            path,
            new PackageBuildOptions { Vacuum = true });

        Assert.True(File.Exists(result.Path));
        Assert.Equal(1, result.JobCount);
        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task WriteModelAsyncStoresAReencodableModelPayload()
    {
        var (project, compiled) = ParameterizationTestSupport.LoadAndCompile(ParameterizationTestSupport.Ceria);
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model!;
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await using (writer)
        {
            await writer.WriteModelAsync(applied);
            await writer.WriteJobRowAsync(
                SimulationPackageTestSupport.JobRow(modelSignature: applied.Signature));
            var result = await writer.CompleteAsync();
            Assert.Equal(path, result.Path);
            Assert.Equal(1, result.ModelCount);
        }

        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM models;";
        var bytes = (byte[])(await command.ExecuteScalarAsync())!;
        var decoded = PayloadEncoder.DecodeModel(bytes);
        Assert.Equal(applied.Signature, decoded.Signature);
        Assert.Equal(applied.ModelSignature, decoded.ModelSignature);
        var values = ModelPayloadMapper.ToParameterizationValues(decoded);
        Assert.Equal(applied.Signature, ParameterizationSignatureV1.Compute(decoded.ModelSignature, values));
    }

    [Fact]
    public async Task DiskFullAtTheFileFlushDeletesTheTempFileAndPreservesTheTarget()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB };
        File.WriteAllBytes(path, original);

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { Overwrite = true },
            new FaultInjectingPackageFileSystem { FailFlushFile = true });
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await writer.WriteJobRowAsync(SimulationPackageTestSupport.JobRow());
        var exception = await Assert.ThrowsAsync<IOException>(() => writer.CompleteAsync());

        Assert.Contains("No space left", exception.Message, StringComparison.Ordinal);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));
    }

    [Fact]
    public async Task AFailedAtomicMoveDeletesTheTempFileAndPreservesTheTarget()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB };
        File.WriteAllBytes(path, original);

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { Overwrite = true },
            new FaultInjectingPackageFileSystem { FailMove = true });
        await writer.WriteModelRowAsync(SimulationPackageTestSupport.ModelRow());
        await writer.WriteJobRowAsync(SimulationPackageTestSupport.JobRow());
        var exception = await Assert.ThrowsAsync<IOException>(() => writer.CompleteAsync());

        Assert.Contains("atomic-move", exception.Message, StringComparison.Ordinal);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));
    }
}
