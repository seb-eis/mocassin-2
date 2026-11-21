// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.App.Composition;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Json;

namespace Mocassin.App.Settings;

/// <summary>
/// Atomic settings store in the application-data directory.
/// </summary>
internal sealed class AppSettingsStore : IAppSettingsStore
{
    private readonly AppPaths paths;
    private readonly bool ignoreCasePaths;

    /// <summary>
    /// Initializes a settings store.
    /// </summary>
    /// <param name="paths">The application-data paths.</param>
    public AppSettingsStore(AppPaths paths)
    {
        this.paths = paths;
        ignoreCasePaths = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
    }

    /// <inheritdoc />
    public AppSettingsLoadResult Load()
    {
        var path = paths.SettingsFile;
        if (!File.Exists(path))
        {
            return AppSettingsLoadResult.Missing();
        }

        try
        {
            var length = new FileInfo(path).Length;
            if (SettingsWritePolicy.IsOversized(length))
            {
                return AppSettingsLoadResult.Fatal(
                    $"The settings document exceeds {SettingsDefaults.MaxSettingsBytes} bytes and was not loaded.");
            }

            var bytes = File.ReadAllBytes(path);
            return AppSettingsJsonReader.Read(bytes, ignoreCasePaths);
        }
        catch (IOException exception)
        {
            return AppSettingsLoadResult.Fatal($"The settings document could not be read: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return AppSettingsLoadResult.Fatal($"The settings document could not be read: {exception.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<AppSettingsSaveResult> SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        byte[] payload;
        try
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(settings, AppSettingsJsonContext.Default.AppSettings);
        }
        catch (JsonException exception)
        {
            return new AppSettingsSaveResult(false, $"The settings could not be serialized: {exception.Message}");
        }

        if (SettingsWritePolicy.IsOversized(payload.Length))
        {
            return new AppSettingsSaveResult(
                false,
                $"The settings document exceeds {SettingsDefaults.MaxSettingsBytes} bytes and was not written.");
        }

        var temporaryPath = SettingsWritePolicy.CreateTemporaryPath(paths.SettingsFile);
        try
        {
            Directory.CreateDirectory(paths.RootDirectory);
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             useAsync: true))
            {
                await stream.WriteAsync(payload, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, paths.SettingsFile, overwrite: true);
            return AppSettingsSaveResult.Success;
        }
        catch (IOException exception)
        {
            DeleteTemporary(temporaryPath);
            return new AppSettingsSaveResult(false, $"The settings could not be written: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            DeleteTemporary(temporaryPath);
            return new AppSettingsSaveResult(false, $"The settings could not be written: {exception.Message}");
        }
    }

    private static void DeleteTemporary(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A leftover temporary file is harmless; the previous settings file is intact.
        }
    }
}
