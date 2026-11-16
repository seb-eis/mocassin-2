// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// The default publication-barrier file system.
/// </summary>
internal sealed class PackageFileSystem : IPackageFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public void EnsureDirectory(string directory) => Directory.CreateDirectory(directory);

    /// <inheritdoc />
    public Stream OpenRead(string path) =>
        new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);

    /// <inheritdoc />
    public void FlushFile(string path)
    {
        // Open read-write so Windows FlushFileBuffers is allowed; the file is ours and still writable.
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.Read,
            bufferSize: 1,
            FileOptions.None);
        stream.Flush(flushToDisk: true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// .NET has no managed directory-fsync API. Unix uses a small libc <c>open</c>/<c>fsync</c>/<c>close</c>
    /// interop; Windows has no equivalent directory-durability primitive, so the file fsync plus the atomic
    /// replace is the documented Windows guarantee. See the Step 11 R2-7 decision.
    /// </remarks>
    public void FlushDirectory(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        UnixDirectorySync.Flush(directory);
    }

    /// <inheritdoc />
    public void Move(string sourcePath, string destinationPath, bool overwrite) =>
        File.Move(sourcePath, destinationPath, overwrite);

    /// <inheritdoc />
    public void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

/// <summary>
/// A minimal Unix directory fsync. The only native call is <c>fsync</c> on a read-only directory descriptor.
/// </summary>
internal static class UnixDirectorySync
{
    /// <summary>Opens the directory read-only.</summary>
    /// <param name="path">The UTF-8 directory path.</param>
    /// <param name="flags">The <c>open(2)</c> flags; zero means <c>O_RDONLY</c>.</param>
    /// <returns>The file descriptor, or a negative value on failure.</returns>
    [DllImport("libc", EntryPoint = "open", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int Open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    /// <summary>Flushes the directory descriptor.</summary>
    /// <param name="fileDescriptor">The open directory descriptor.</param>
    /// <returns>Zero on success, or a negative value on failure.</returns>
    [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static extern int Fsync(int fileDescriptor);

    /// <summary>Closes the directory descriptor.</summary>
    /// <param name="fileDescriptor">The open directory descriptor.</param>
    /// <returns>Zero on success, or a negative value on failure.</returns>
    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int Close(int fileDescriptor);

    /// <summary>
    /// Opens and fsyncs a directory so a subsequent rename is durable.
    /// </summary>
    /// <param name="directory">The directory to flush.</param>
    /// <exception cref="IOException">The directory could not be opened or synced.</exception>
    public static void Flush(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var descriptor = Open(directory, 0);
        if (descriptor < 0)
        {
            throw Error("open", directory);
        }

        try
        {
            if (Fsync(descriptor) != 0)
            {
                throw Error("fsync", directory);
            }
        }
        finally
        {
            _ = Close(descriptor);
        }
    }

    private static IOException Error(string operation, string directory) =>
        new(
            $"Could not {operation} the package directory '{directory}'.",
            new Win32Exception(Marshal.GetLastPInvokeError()));
}
