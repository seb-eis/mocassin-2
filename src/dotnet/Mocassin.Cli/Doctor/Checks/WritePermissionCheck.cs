// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Checks that the current directory is writable by creating and deleting a temporary file.
/// </summary>
internal sealed class WritePermissionCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "write-permission";

    /// <inheritdoc />
    public string Name => CheckName;

    /// <inheritdoc />
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Environment.CurrentDirectory;
        var path = Path.Combine(directory, ".mocassin-doctor-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(path, "probe");
            File.Delete(path);
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Ok,
                $"The current directory '{directory}' is writable.",
                directory));
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup.
            }

            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Fail,
                $"The current directory '{directory}' is not writable: {exception.Message}",
                exception.GetType().Name));
        }
    }
}
