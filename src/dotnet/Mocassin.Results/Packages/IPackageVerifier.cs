// SPDX-License-Identifier: MIT

namespace Mocassin.Results.Packages;

/// <summary>
/// Runs the complete ordered <c>PKG</c> verification checks against a published <c>.msl</c> package.
/// </summary>
public interface IPackageVerifier
{
    /// <summary>Verifies a package.</summary>
    /// <param name="path">The <c>.msl</c> path.</param>
    /// <param name="options">The decode and advisory bounds.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The ordered checks, diagnostics, and inspection projection.</returns>
    public Task<PackageVerificationReport> VerifyAsync(
        string path,
        PackageVerificationOptions options,
        CancellationToken cancellationToken = default);
}
