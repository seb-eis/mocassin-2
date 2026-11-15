// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The remaining count of one particle species within a compiled site.
/// </summary>
/// <param name="ParticleId">The stable particle identifier, never the implicit void particle.</param>
/// <param name="Count">The positive number of particles of this species on the site.</param>
public sealed record ParticlePopulation(ParticleId ParticleId, int Count);

/// <summary>
/// The remaining particle populations of one compiled site.
/// </summary>
/// <remarks>
/// The implicit void particle is excluded. A site is present when it has at least one remaining particle or
/// when a requested doping targets it, so a fully consumed site still appears with an empty particle list.
/// Sites are ordered by ascending <see cref="CellSiteId.Value"/> and particles by ascending compiled particle
/// index so repeated previews are deterministic.
/// </remarks>
/// <param name="SiteId">The stable compiled site identifier.</param>
/// <param name="Particles">The positive remaining particle counts of the site in compiled order.</param>
public sealed record SitePopulation(CellSiteId SiteId, ImmutableArray<ParticlePopulation> Particles);

/// <summary>
/// A preview of one requested supercell and doping list.
/// </summary>
/// <remarks>
/// A preview never compiles and never exposes a partial result: invalid user data returns <see langword="null"/>
/// plus diagnostics, and generation throws only for a null project/model or an inconsistent compiled model. The
/// requested lattice size and doping list are echoed exactly so a preview cannot be confused with a different
/// request. <see cref="Report"/> is the same <see cref="Jobs.DopingReport"/> produced by the build generator for
/// the same request, and <see cref="RemainingPopulations"/> supplies the per-site counts used by the Step 25
/// lattice editor.
/// </remarks>
/// <param name="Request">The exact requested supercell size.</param>
/// <param name="Dopings">The exact requested doping values.</param>
/// <param name="Report">The doping report of the generated lattice.</param>
/// <param name="RemainingPopulations">The per-site remaining populations in deterministic order.</param>
public sealed record LatticePreview(
    LatticeRequest Request,
    ImmutableArray<DopingValue> Dopings,
    DopingReport Report,
    ImmutableArray<SitePopulation> RemainingPopulations);
