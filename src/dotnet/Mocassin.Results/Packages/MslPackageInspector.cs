// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Results.Packages.Internal;

namespace Mocassin.Results.Packages;

/// <summary>
/// Reads the structural content of a published <c>.msl</c> package without modifying it.
/// </summary>
public sealed class MslPackageInspector : IMslPackageInspector
{
    /// <inheritdoc />
    public async Task<PackageInspection> InspectAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var read = await PackageReader.ReadAsync(path, new PackageVerificationOptions(), cancellationToken)
            .ConfigureAwait(false);
        ThrowIfFatal(read);
        return PackageInspectionMapper.Map(read);
    }

    /// <inheritdoc />
    public async Task<JsonDocument> BuildCanonicalSummaryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var read = await PackageReader.ReadAsync(path, new PackageVerificationOptions(), cancellationToken)
            .ConfigureAwait(false);
        ThrowIfFatal(read);
        return PackageSummaryBuilder.Build(read);
    }

    private static void ThrowIfFatal(PackageReadResult read)
    {
        if (read.FatalCode is { } code)
        {
            throw new InvalidDataException($"{code}: {read.FatalMessage}");
        }
    }
}
