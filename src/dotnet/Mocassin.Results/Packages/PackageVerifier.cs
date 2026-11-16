// SPDX-License-Identifier: MIT

using Mocassin.Results.Packages.Internal;

namespace Mocassin.Results.Packages;

/// <summary>
/// Runs the complete ordered <c>PKG</c> verification checks against a published <c>.msl</c> package.
/// </summary>
public sealed class PackageVerifier : IPackageVerifier
{
    /// <inheritdoc />
    public async Task<PackageVerificationReport> VerifyAsync(
        string path,
        PackageVerificationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        var read = await PackageReader.ReadAsync(path, options, cancellationToken).ConfigureAwait(false);
        return PackageVerifierEngine.Verify(read, options);
    }
}
