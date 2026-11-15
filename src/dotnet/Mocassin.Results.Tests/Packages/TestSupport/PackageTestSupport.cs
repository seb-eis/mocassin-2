// SPDX-License-Identifier: MIT

using System.Buffers;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;
using MessagePack;

namespace Mocassin.Results.Tests.Packages.TestSupport;

/// <summary>
/// Shared real-package fixtures for the inspector and verifier tests.
/// </summary>
internal static class PackageTestSupport
{
    public const string Sample = "samples/jobs/ceria-temperature-sweep.mocprj";
    private static readonly DateTimeOffset createdUtc = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a real package with the real writer.</summary>
    public static async Task<BuiltPackage> BuildAsync(string directory, int jobCount = 2)
    {
        var project = ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine(Sample)));
        var compiled = new ModelCompiler().Compile(project).Model!;
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model!;
        var expansion = JobTemplateExpander.Expand(project, compiled, project.JobTemplates.Single());
        Assert.True(expansion.IsSuccess);
        var path = Path.Combine(directory, "ceria.msl");
        var writer = SimulationPackageWriter.Create(
            new PackageBuildRequest(path, project.Metadata.ProjectId, "all", createdUtc),
            new PackageBuildOptions());
        await using (writer)
        {
            await writer.WriteModelAsync(applied);
            foreach (var descriptor in expansion.Jobs.Take(jobCount))
            {
                var generated = SupercellGenerator.Generate(project, compiled, descriptor);
                Assert.True(generated.IsSuccess);
                await writer.WriteJobAsync(new JobPackage(descriptor, generated.Lattice!));
            }

            await writer.CompleteAsync();
        }

        return new BuiltPackage(path, applied);
    }

    /// <summary>Opens a package read-write for a controlled mutation.</summary>
    public static async Task<SqliteConnection> OpenReadWriteAsync(string path)
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>Executes one mutation statement.</summary>
    public static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Reads the model payload from a package.</summary>
    public static async Task<byte[]> ReadModelPayloadAsync(string path)
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
        command.CommandText = "SELECT payload FROM models LIMIT 1;";
        return (byte[])(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Writes a mutated model payload.</summary>
    public static async Task WriteModelPayloadAsync(string path, byte[] payload)
    {
        await using var connection = await OpenReadWriteAsync(path);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE models SET payload = $payload;";
        command.Parameters.Add("$payload", SqliteType.Blob).Value = payload;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Encodes a payload with the same standard Zstd+MessagePack framing as the writer.</summary>
    public static byte[] Encode<TPayload>(TPayload payload)
    {
        var messagePack = MessagePackSerializer.Serialize(payload);
        var options = new ZstandardCompressionOptions { Quality = 3 };
        using var encoder = new ZstandardEncoder(options);
        encoder.SetSourceLength(messagePack.Length);
        var destination = new byte[ZstandardEncoder.GetMaxCompressedLength(messagePack.Length)];
        if (encoder.Compress(messagePack, destination, out _, out int written, isFinalBlock: true) !=
            OperationStatus.Done)
        {
            throw new InvalidOperationException("The test-support payload encoder failed.");
        }

        return destination.AsSpan(0, written).ToArray();
    }

    /// <summary>Decodes a model payload for a controlled mutation.</summary>
    public static ModelPayload DecodeModel(byte[] payload)
    {
        if (!ZstandardDecoder.TryGetMaxDecompressedLength(payload, out long bound))
        {
            throw new InvalidDataException("The test-support model payload is not a valid Zstd frame.");
        }

        var destination = new byte[(int)bound];
        if (!ZstandardDecoder.TryDecompress(payload, destination, out int written))
        {
            throw new InvalidDataException("The test-support model payload cannot be decompressed.");
        }

        return MessagePackSerializer.Deserialize<ModelPayload>(destination.AsMemory(0, written));
    }

    /// <summary>Reads a scalar from a read-only package.</summary>
    public static async Task<long> ReadInt64Async(string path, string sql)
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
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>A built package and the parameterized model that produced it.</summary>
internal sealed record BuiltPackage(string Path, ParameterizedModel Applied);

/// <summary>A real temporary directory removed after the test.</summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "mocassin-msl-results-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort test cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort test cleanup.
        }
    }
}
