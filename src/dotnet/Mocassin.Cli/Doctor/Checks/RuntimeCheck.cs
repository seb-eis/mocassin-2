// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Reports the .NET runtime, operating system, and process architecture.
/// </summary>
internal sealed class RuntimeCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "runtime";

    /// <inheritdoc />
    public string Name => CheckName;

    /// <inheritdoc />
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new DoctorCheckResult(
            Name,
            DoctorCheckStatus.Ok,
            $"{RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}",
            RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());
        return Task.FromResult(result);
    }
}
