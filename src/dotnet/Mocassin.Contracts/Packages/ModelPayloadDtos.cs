// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// The <c>.msl</c> compiled-model payload.
/// </summary>
/// <remarks>
/// This is the persisted, parameterized form of a compiled model. It is a MessagePack map with snake_case keys
/// inside a Zstd frame. <see cref="FormatVersion"/> is the payload-encoding version, independent of the
/// package schema version and <c>CompiledModel.FormatVersion</c>. The payload carries the applied values, so a
/// reader can re-encode the parameterization signature; the compiled-model signature is an identity string.
/// </remarks>
/// <param name="FormatVersion">The payload-encoding version.</param>
/// <param name="Signature">
/// The Step 09 parameterization signature; the <c>models.signature</c> uniqueness key.
/// </param>
/// <param name="ModelSignature">
/// The Step 08 compiled-model signature; an identity string that is not re-encoded from the payload.
/// </param>
/// <param name="SymmetryContractVersion">The symmetry contract version.</param>
/// <param name="SpaceGroupDataVersion">The space-group catalog data version.</param>
/// <param name="SpaceGroupDataSha256">The SHA-256 of the canonical space-group catalog payload.</param>
/// <param name="Particle">The compiled particle table; index zero is void.</param>
/// <param name="Structure">The compiled structure, including the per-image environment pair links.</param>
/// <param name="Energy">The applied pair, cluster, and site-energy values.</param>
/// <param name="Transitions">The compiled jump collections, directions, rules, and selection tables.</param>
/// <param name="Simulations">The compiled simulation encodings, in compiled simulation order.</param>
[MessagePackObject]
public sealed record ModelPayload(
    [property: Key("format_version")] uint FormatVersion,
    [property: Key("signature")] string Signature,
    [property: Key("model_signature")] string ModelSignature,
    [property: Key("symmetry_contract_version")] string SymmetryContractVersion,
    [property: Key("space_group_data_version")] string SpaceGroupDataVersion,
    [property: Key("space_group_data_sha256")] string SpaceGroupDataSha256,
    [property: Key("particle")] ParticlePayload Particle,
    [property: Key("structure")] StructurePayload Structure,
    [property: Key("energy")] EnergyPayload Energy,
    [property: Key("transitions")] TransitionPayload Transitions,
    [property: Key("simulations")] ImmutableArray<SimulationPayload> Simulations);

/// <summary>
/// The compiled particle table of a model payload.
/// </summary>
/// <remarks>
/// Entries are in compiled index order: index zero is void, then the authored particles in ascending
/// <c>ParticleId</c> order. This is not the preview/state-change-matrix order.
/// </remarks>
/// <param name="Charges">The particle charges in elementary-charge units, indexed by compiled particle index.</param>
/// <param name="Symbols">The particle symbols, indexed by compiled particle index.</param>
/// <param name="IsVacancy">Whether each compiled particle is a vacancy.</param>
/// <param name="Count">The compiled particle count, including void.</param>
[MessagePackObject]
public sealed record ParticlePayload(
    [property: Key("charges")] ImmutableArray<double> Charges,
    [property: Key("symbols")] ImmutableArray<string> Symbols,
    [property: Key("is_vacancy")] ImmutableArray<bool> IsVacancy,
    [property: Key("count")] uint Count);
