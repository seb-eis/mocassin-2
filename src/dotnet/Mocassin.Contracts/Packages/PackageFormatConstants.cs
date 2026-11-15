// SPDX-License-Identifier: MIT

namespace Mocassin.Contracts.Packages;

/// <summary>
/// Version constants for the <c>.msl</c> payload encodings.
/// </summary>
/// <remarks>
/// The package format version, the model/job/lattice payload versions, and <c>CompiledModel.FormatVersion</c>
/// are independent version domains. Version 1 writers write <c>1</c> for each.
/// </remarks>
public static class PackageFormatConstants
{
    /// <summary>
    /// The model payload (<c>ModelPayload</c>) encoding version.
    /// </summary>
    public const uint ModelPayloadFormatVersion = 1;

    /// <summary>
    /// The job payload (<c>JobPayload</c>) encoding version.
    /// </summary>
    public const uint JobPayloadFormatVersion = 1;

    /// <summary>
    /// The lattice payload (<c>LatticePayload</c>) encoding version.
    /// </summary>
    public const uint LatticePayloadFormatVersion = 1;
}
