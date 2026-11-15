// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Packages.SimulationPackage.Internal;

namespace Mocassin.Packages.Tests.SimulationPackage.TestSupport;

/// <summary>
/// Shared real-file fixtures for the simulation-package schema and publication tests.
/// </summary>
internal static class SimulationPackageTestSupport
{
    public static readonly Guid ProjectId =
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8b01", CultureInfo.InvariantCulture);

    public static readonly DateTimeOffset CreatedUtc =
        new(2026, 11, 15, 12, 30, 0, TimeSpan.Zero);

    public const string ModelSignature =
        "3cee47582fabf954a4b2d176d87ed25a79905f96a21a430fef6d9b6ccd6e6003";

    public const string LatticeSignature =
        "0d8327cda4f8d6b4637208894c350b2c5e1686548d951b4326fb59eeeab4a42a";

    public static PackageBuildRequest Request(string targetPath) =>
        new(targetPath, ProjectId, "all", CreatedUtc);

    public static PackageModelRow ModelRow() =>
        new(
            ModelSignature,
            1,
            new byte[] { 0x81, 0xA1, 0x78, 0x01 },
            "symmetry-1",
            "spglib-2.8.0/catalog-1",
            new string('a', 64));

    public static PackageLatticeRow LatticeRow(string signature = LatticeSignature) =>
        new(signature, 1, new byte[] { 0x82, 0xA1, 0x79, 0x01 });

    public static PackageJobRow JobRow(
        int jobId = 1,
        string latticeSignature = LatticeSignature,
        int temperatureK = 900,
        uint flags = 0x40,
        string? modelSignature = null)
    {
        var seed = new byte[32];
        Array.Fill(seed, (byte)0x5a);
        return new PackageJobRow(
            jobId,
            modelSignature ?? ModelSignature,
            LatticeRow(latticeSignature),
            "kmc",
            "collection",
            "config",
            0,
            0,
            jobId - 1,
            temperatureK,
            0.0,
            500,
            7,
            0,
            null,
            null,
            2,
            2,
            2,
            string.Empty,
            flags,
            seed,
            1,
            new byte[] { 0x83, 0xA1, 0x7A, 0x01 });
    }

    public static async Task<PackageWriteResult> BuildAsync(
        string targetPath,
        PackageBuildOptions? options = null,
        IPackageFileSystem? fileSystem = null)
    {
        var writer = SimulationPackageWriter.Create(
            Request(targetPath),
            options ?? new PackageBuildOptions(),
            fileSystem ?? new PackageFileSystem());
        await using (writer)
        {
            await writer.WriteModelRowAsync(ModelRow()).ConfigureAwait(false);
            await writer.WriteJobRowAsync(JobRow()).ConfigureAwait(false);
            return await writer.CompleteAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// A real temporary directory removed after the test.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "mocassin-msl-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(string name) => System.IO.Path.Combine(Path, name);

    public string[] TemporaryFiles() =>
        Directory.Exists(Path)
            ? Directory.GetFiles(Path, "*.msl.tmp-*", SearchOption.TopDirectoryOnly)
            : [];

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

/// <summary>
/// A real file system with injectable publication-barrier failures.
/// </summary>
internal sealed class FaultInjectingPackageFileSystem : IPackageFileSystem
{
    private readonly PackageFileSystem inner = new();

    public bool FailFlushFile { get; init; }

    public bool FailMove { get; init; }

    public bool FileExists(string path) => inner.FileExists(path);

    public void EnsureDirectory(string directory) => inner.EnsureDirectory(directory);

    public Stream OpenRead(string path) => inner.OpenRead(path);

    public void FlushFile(string path)
    {
        if (FailFlushFile)
        {
            throw new IOException("No space left on device.");
        }

        inner.FlushFile(path);
    }

    public void FlushDirectory(string directory) => inner.FlushDirectory(directory);

    public void Move(string sourcePath, string destinationPath, bool overwrite)
    {
        if (FailMove)
        {
            throw new IOException("Injected atomic-move failure.");
        }

        inner.Move(sourcePath, destinationPath, overwrite);
    }

    public void Delete(string path) => inner.Delete(path);
}
