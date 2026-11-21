// SPDX-License-Identifier: MIT

namespace Mocassin.App.Composition;

/// <summary>
/// Resolves the application-data layout.
/// </summary>
/// <param name="RootDirectory">The application-data root.</param>
internal sealed record AppPaths(string RootDirectory)
{
    private const string applicationFolderName = "Mocassin";

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public string SettingsFile => Path.Combine(RootDirectory, "Mocassin.Settings.json");

    /// <summary>
    /// Gets the log directory.
    /// </summary>
    public string LogsDirectory => Path.Combine(RootDirectory, "logs");

    /// <summary>
    /// Gets the cache directory.
    /// </summary>
    public string CacheDirectory => Path.Combine(RootDirectory, "cache");

    /// <summary>
    /// Gets the recovery directory. It is reserved for Step 23B.
    /// </summary>
    public string RecoveryDirectory => Path.Combine(RootDirectory, "recovery");

    /// <summary>
    /// Resolves the paths from the startup override, the environment override, and the platform local-data root.
    /// </summary>
    /// <param name="dataDirectoryOverride">The command-line override.</param>
    /// <param name="environmentOverride">The environment override.</param>
    /// <param name="localApplicationData">The platform local-application-data root.</param>
    /// <returns>The resolved paths.</returns>
    public static AppPaths Resolve(
        string? dataDirectoryOverride,
        string? environmentOverride,
        string localApplicationData)
    {
        var root = dataDirectoryOverride;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = environmentOverride;
        }

        root = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(localApplicationData, applicationFolderName)
            : Path.GetFullPath(root);

        return new AppPaths(root);
    }

    /// <summary>
    /// Resolves the paths for the current process.
    /// </summary>
    /// <param name="options">The startup options.</param>
    /// <returns>The resolved paths.</returns>
    public static AppPaths ResolveForCurrentProcess(AppStartupOptions options) =>
        Resolve(
            options.DataDirectory,
            Environment.GetEnvironmentVariable("MOCASSIN_DATA_DIR"),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    /// <summary>
    /// Creates the application-data directories.
    /// </summary>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(RecoveryDirectory);
    }
}
