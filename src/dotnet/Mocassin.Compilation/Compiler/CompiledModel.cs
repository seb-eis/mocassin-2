// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// The pure, densely indexed result of a model compilation.
/// </summary>
/// <remarks>
/// The header records the format version, the Step 08 geometry/rules/encoding signature, and the symmetry
/// provenance. The particle, structure, energy, transition, and simulation tables are immutable and typed
/// with domain identifiers. The type is never the persisted package DTO and is never written to disk.
/// </remarks>
public sealed record CompiledModel
{
    /// <summary>
    /// The current compiled-model format version.
    /// </summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>
    /// Initializes the compiled-model header.
    /// </summary>
    /// <param name="formatVersion">The compiled-model format version.</param>
    /// <param name="modelSignature">The canonical geometry/rules/encoding signature.</param>
    /// <param name="symmetryContractVersion">The symmetry contract version the model was compiled against.</param>
    /// <param name="spaceGroupDataVersion">The embedded space-group catalog data version.</param>
    /// <param name="spaceGroupDataSha256">The SHA-256 of the embedded space-group catalog payload.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="formatVersion"/> is not positive.</exception>
    /// <exception cref="ArgumentException">A required string is empty.</exception>
    public CompiledModel(
        int formatVersion,
        string modelSignature,
        string symmetryContractVersion,
        string spaceGroupDataVersion,
        string spaceGroupDataSha256)
    {
        if (formatVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                formatVersion,
                "The compiled-model format version must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(modelSignature);
        ArgumentException.ThrowIfNullOrWhiteSpace(symmetryContractVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceGroupDataVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceGroupDataSha256);

        FormatVersion = formatVersion;
        ModelSignature = modelSignature;
        SymmetryContractVersion = symmetryContractVersion;
        SpaceGroupDataVersion = spaceGroupDataVersion;
        SpaceGroupDataSha256 = spaceGroupDataSha256;
    }

    /// <summary>
    /// Gets the compiled-model format version.
    /// </summary>
    public int FormatVersion { get; init; }

    /// <summary>
    /// Gets the canonical geometry/rules/encoding signature. Parameterization values and runtime simulation
    /// settings are excluded.
    /// </summary>
    public string ModelSignature { get; init; }

    /// <summary>
    /// Gets the symmetry contract version the model was compiled against.
    /// </summary>
    public string SymmetryContractVersion { get; init; }

    /// <summary>
    /// Gets the embedded space-group catalog data version.
    /// </summary>
    public string SpaceGroupDataVersion { get; init; }

    /// <summary>
    /// Gets the SHA-256 of the embedded space-group catalog payload.
    /// </summary>
    public string SpaceGroupDataSha256 { get; init; }
}
