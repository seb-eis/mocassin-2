// SPDX-License-Identifier: MIT

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Results.Packages;

namespace Mocassin.Results.Tests.Packages.TestSupport;

/// <summary>
/// Builds and regenerates the checked-in Step 13 C-centered package fixture.
/// </summary>
/// <remarks>
/// The fixture is produced by the real writer from
/// <c>samples/step-13-centered/step-13-centered.mocprj</c> with a fixed <c>created_utc</c>. Its canonical
/// summary and payload index are the comparable artifacts; the <c>.msl</c> bytes are recorded by SHA-256 only.
/// Regenerate with:
/// <c>MOCASSIN_REGENERATE_CENTERED_FIXTURE=1 dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter "FullyQualifiedName~RegenerateCenteredPackageFixture"</c>.
/// </remarks>
internal static class CenteredPackageFixtureBuilder
{
    public const string Sample = "samples/step-13-centered/step-13-centered.mocprj";
    public const string DirectoryName = "tests/contracts/packages/step-13-centered";
    public const string PackageFileName = "step-13-centered.msl";
    public const string SummaryFileName = "package-summary-v1.json";
    public const string HashFileName = "step-13-centered.sha256";
    public const string PayloadDirectoryName = "payloads";
    public static readonly DateTimeOffset CreatedUtc = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Builds the centered fixture package into <paramref name="directory"/>.</summary>
    public static async Task<string> BuildAsync(string directory, CancellationToken cancellationToken = default)
    {
        var project = ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine(Sample)));
        var compiled = new ModelCompiler().Compile(project, cancellationToken: cancellationToken).Model
            ?? throw new InvalidOperationException("The centered fixture project did not compile.");
        var applied = new ParameterizationService().Apply(project, compiled, template: null, cancellationToken: cancellationToken).Model
            ?? throw new InvalidOperationException("The centered fixture project did not parameterize.");
        var expansion = JobTemplateExpander.Expand(project, compiled, project.JobTemplates.Single());
        if (!expansion.IsSuccess)
        {
            throw new InvalidOperationException(string.Join(
                Environment.NewLine,
                expansion.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, PackageFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var writer = SimulationPackageWriter.Create(
            new PackageBuildRequest(path, project.Metadata.ProjectId, "all", CreatedUtc),
            new PackageBuildOptions());
        await using (writer)
        {
            await writer.WriteModelAsync(applied, cancellationToken);
            foreach (var descriptor in expansion.Jobs)
            {
                var generated = SupercellGenerator.Generate(project, compiled, descriptor, cancellationToken);
                if (!generated.IsSuccess)
                {
                    throw new InvalidOperationException(string.Join(
                        Environment.NewLine,
                        generated.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
                }

                await writer.WriteJobAsync(new JobPackage(descriptor, generated.Lattice!), cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        return path;
    }

    /// <summary>Builds the package and writes the summary, hash, and extracted payloads.</summary>
    public static async Task WriteArtifactsAsync(string directory, CancellationToken cancellationToken = default)
    {
        var path = await BuildAsync(directory, cancellationToken);
        var hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, cancellationToken)));
        await File.WriteAllTextAsync(
            Path.Combine(directory, HashFileName),
            $"{hash}  {PackageFileName}\n",
            cancellationToken);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path, cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(directory, SummaryFileName),
            JsonSerializer.Serialize(summary.RootElement, jsonOptions) + "\n",
            cancellationToken);

        await WritePayloadsAsync(path, Path.Combine(directory, PayloadDirectoryName), cancellationToken);
    }

    /// <summary>Extracts the model, lattice, and job payloads as raw MessagePack and an index.</summary>
    public static async Task WritePayloadsAsync(
        string packagePath,
        string payloadDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(payloadDirectory);
        var entries = new List<PayloadIndexEntry>();
        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = packagePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync(cancellationToken);

        await ExtractAsync("model", "SELECT model_id, payload FROM models ORDER BY model_id;");
        await ExtractAsync("lattice", "SELECT lattice_id, payload FROM lattices ORDER BY lattice_id;");
        await ExtractAsync("job", "SELECT job_id, payload FROM jobs ORDER BY job_id;");

        var index = new
        {
            format = "mocassin.package-payloads",
            formatVersion = 1,
            note = "Raw MessagePack payloads extracted from step-13-centered.msl (real .NET writer). " +
                   "The compressed frames live in the .msl; the .msl SHA-256 is in step-13-centered.sha256.",
            payloads = entries.OrderBy(static entry => entry.Kind, StringComparer.Ordinal)
                .ThenBy(static entry => entry.Index)
                .ToArray(),
        };
        await File.WriteAllTextAsync(
            Path.Combine(payloadDirectory, "index.json"),
            JsonSerializer.Serialize(index, jsonOptions) + "\n",
            cancellationToken);

        async Task ExtractAsync(string kind, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var indexValue = reader.GetInt64(0);
                var compressed = (byte[])reader[1];
                var raw = Decompress(compressed);
                var name = $"{kind}-{indexValue}.bin";
                await File.WriteAllBytesAsync(Path.Combine(payloadDirectory, name), raw, cancellationToken);
                entries.Add(new PayloadIndexEntry(
                    kind,
                    indexValue,
                    name,
                    compressed.Length,
                    Convert.ToHexStringLower(SHA256.HashData(compressed)),
                    raw.Length,
                    Convert.ToHexStringLower(SHA256.HashData(raw))));
            }
        }
    }

    private static byte[] Decompress(byte[] frame)
    {
        if (!ZstandardDecoder.TryGetMaxDecompressedLength(frame, out long bound))
        {
            throw new InvalidDataException("The centered fixture payload is not a valid Zstd frame.");
        }

        var destination = new byte[bound];
        if (!ZstandardDecoder.TryDecompress(frame, destination, out int written))
        {
            throw new InvalidDataException("The centered fixture payload could not be decompressed.");
        }

        return destination.AsSpan(0, written).ToArray();
    }

    private sealed record PayloadIndexEntry(
        string Kind,
        long Index,
        string Path,
        int CompressedBytes,
        string CompressedSha256,
        int RawBytes,
        string RawSha256);
}
