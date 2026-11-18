// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Tests.Build;

/// <summary>
/// Wraps the real verifier and forces an invalid result for verification-failure tests.
/// </summary>
internal sealed class InvalidatingVerifier : IPackageVerifier
{
    private readonly PackageVerifier inner = new();

    public async Task<PackageVerificationReport> VerifyAsync(
        string path,
        PackageVerificationOptions options,
        CancellationToken cancellationToken = default)
    {
        var report = await inner.VerifyAsync(path, options, cancellationToken);
        return report with
        {
            IsValid = false,
            Diagnostics =
            [
                new Diagnostic(
                    "PKG005",
                    DiagnosticSeverity.Error,
                    "Simulated verification failure."),
            ],
        };
    }
}
