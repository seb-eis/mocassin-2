// SPDX-License-Identifier: MIT

using System.Globalization;

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Reports the free space on the current directory's volume.
/// </summary>
internal sealed class DiskSpaceCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "disk-space";

    private const long warningThresholdBytes = 100L * 1024 * 1024;

    /// <inheritdoc />
    public string Name => CheckName;

    /// <inheritdoc />
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var root = Path.GetPathRoot(Environment.CurrentDirectory);
            if (string.IsNullOrEmpty(root))
            {
                return Task.FromResult(new DoctorCheckResult(
                    Name,
                    DoctorCheckStatus.Fail,
                    "The current directory has no volume root.",
                    Environment.CurrentDirectory));
            }

            var available = new DriveInfo(root).AvailableFreeSpace;
            var status = available < warningThresholdBytes ? DoctorCheckStatus.Warn : DoctorCheckStatus.Ok;
            return Task.FromResult(new DoctorCheckResult(
                Name,
                status,
                string.Create(CultureInfo.InvariantCulture, $"{available} bytes free on '{root}'."),
                root));
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or ArgumentException
                                              or NotSupportedException)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Fail,
                $"The free disk space could not be determined: {exception.Message}",
                exception.GetType().Name));
        }
    }
}
