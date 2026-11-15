// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// The file-system seam used by the publication barrier. Tests inject failures at this true I/O boundary.
/// </summary>
internal interface IPackageFileSystem
{
    /// <summary>Returns whether a file exists.</summary>
    public bool FileExists(string path);

    /// <summary>Ensures the directory exists.</summary>
    public void EnsureDirectory(string directory);

    /// <summary>Opens a file for sequential reading.</summary>
    public Stream OpenRead(string path);

    /// <summary>Flushes the file's data to durable storage.</summary>
    public void FlushFile(string path);

    /// <summary>Flushes the directory entry to durable storage where the platform supports it.</summary>
    public void FlushDirectory(string directory);

    /// <summary>Atomically moves a file, optionally replacing the destination.</summary>
    public void Move(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>Deletes a file when it exists.</summary>
    public void Delete(string path);
}
