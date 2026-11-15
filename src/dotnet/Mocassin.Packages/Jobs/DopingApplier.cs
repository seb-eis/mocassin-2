// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Applies the requested dopings of one job to an already-replicated base lattice.
/// </summary>
/// <remarks>
/// Dopings are applied sequentially in ascending resolved priority. Every application operates on the current
/// lattice, so a later doping sees the particles that remain after all higher-priority dopings. The primary
/// exchange is sampled without replacement from the current source positions with one deterministic
/// <see cref="LatticeSampler"/> stream per lattice. A compensating doping recomputes its dependent source
/// positions after the primary application and samples from the current lattice. After the last application the
/// final lattice is checked against the compiled site masks and unstable-site voids.
/// </remarks>
internal static class DopingApplier
{
    /// <summary>
    /// Applies the requested dopings to the particle buffer.
    /// </summary>
    /// <param name="project">The project that supplies the authored exchanges and dopings.</param>
    /// <param name="model">The compiled model that supplies the compiled particles and sites.</param>
    /// <param name="dopings">The requested dopings in ascending resolved priority order.</param>
    /// <param name="particles">The mutable supercell particle buffer, replaced in place.</param>
    /// <param name="positionCount">The number of canonical P1 positions per unit cell.</param>
    /// <param name="sampler">The single deterministic sampler stream of the generated lattice.</param>
    /// <param name="path">The diagnostic path of the job doping section.</param>
    /// <param name="bag">The diagnostic bag that receives the doping diagnostics.</param>
    /// <param name="report">The doping report of the final lattice, or an empty report on failure.</param>
    /// <returns><see langword="true"/> when every doping was applied; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The project and compiled model are inconsistent: an authored doping, exchange, particle, or site does not
    /// resolve, a compiled particle index does not fit the byte lattice, or the final lattice violates a compiled
    /// site mask or the unstable-site void rule.
    /// </exception>
    public static bool TryApply(
        Project project,
        CompiledModel model,
        ImmutableArray<LatticeDoping> dopings,
        Span<byte> particles,
        int positionCount,
        LatticeSampler sampler,
        ModelPath path,
        DiagnosticBag bag,
        out DopingReport report)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(bag);

        var requested = dopings.IsDefault ? ImmutableArray<LatticeDoping>.Empty : dopings;
        var siteById = IndexSites(model.Structure);
        var totalBefore = SumCharge(model, particles);
        var applications = ImmutableArray.CreateBuilder<DopingApplication>(requested.Length);
        var hasUncompensatedCharge = false;

        foreach (var entry in requested)
        {
            var fraction = entry.Fraction;
            if (!double.IsFinite(fraction) || fraction is < 0.0 or > 1.0)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidDopingFraction,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Doping '{entry.DopingId.Value:D}' has the non-finite or out-of-range fraction " +
                        $"{fraction:R}; use a finite value in 0..1."),
                    path));
                report = EmptyReport(particles.Length);
                return false;
            }

            var doping = project.Find(entry.DopingId)
                ?? throw new InvalidOperationException(
                    $"Requested doping '{entry.DopingId.Value:D}' is not authored.");
            var primary = project.Find(doping.PrimaryExchangeId)
                ?? throw new InvalidOperationException(
                    $"Doping '{doping.Id.Value:D}' references unknown primary exchange " +
                    $"'{doping.PrimaryExchangeId.Value:D}'.");
            var primarySite = ResolveSite(model, siteById, primary.SiteId, doping);
            var primarySource = CompiledIndex(model, primary.SourceParticleId);
            var primaryTarget = CompiledIndex(model, primary.TargetParticleId);
            var primaryCandidates = new List<int>();
            CollectSourcePositions(
                particles,
                positionCount,
                primarySite,
                primarySource,
                primaryCandidates);
            var available = primaryCandidates.Count;
            var requestedPrimary = available == 0
                ? 0
                : (int)Math.Floor(fraction * available);
            var primaryDelta = Charge(model, primaryTarget) - Charge(model, primarySource);

            CompiledSite? dependentSite = null;
            var dependentSource = 0;
            var dependentTarget = 0;
            var dependentDelta = 0.0;
            var appliedPrimary = 0;
            var appliedDependent = 0;

            if (requestedPrimary == 0)
            {
                if (fraction > 0.0 && available == 0)
                {
                    bag.Add(Warning(
                        JobDiagnosticCodes.UnrealizedDoping,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Doping '{doping.Id.Value:D}' cannot be realized: the current lattice has no " +
                            $"'{primary.SourceParticleId:D}' particles on site '{primary.SiteId:D}'."),
                        path));
                }
            }
            else if (doping.ChargeCompensation)
            {
                var dependentExchangeId = doping.DependentExchangeId
                    ?? throw new InvalidOperationException(
                        $"Compensating doping '{doping.Id.Value:D}' has no dependent exchange.");
                var dependent = project.Find(dependentExchangeId)
                    ?? throw new InvalidOperationException(
                        $"Doping '{doping.Id.Value:D}' references unknown dependent exchange " +
                        $"'{dependentExchangeId.Value:D}'.");
                dependentSite = ResolveSite(model, siteById, dependent.SiteId, doping);
                dependentSource = CompiledIndex(model, dependent.SourceParticleId);
                dependentTarget = CompiledIndex(model, dependent.TargetParticleId);
                dependentDelta = Charge(model, dependentTarget) - Charge(model, dependentSource);

                if (primaryDelta == 0.0 ||
                    dependentDelta == 0.0 ||
                    Math.Sign(primaryDelta) == Math.Sign(dependentDelta))
                {
                    bag.Add(Error(
                        JobDiagnosticCodes.InvalidCompensation,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Doping '{doping.Id.Value:D}' cannot be charge-compensated: the exchange charge " +
                            $"deltas must be non-zero and have opposite signs."),
                        path));
                    report = EmptyReport(particles.Length);
                    return false;
                }

                var dependentCandidates = new List<int>();
                CollectSourcePositions(
                    particles,
                    positionCount,
                    dependentSite,
                    dependentSource,
                    dependentCandidates);
                if (!TryFindCompensatedPair(
                        requestedPrimary,
                        primaryDelta,
                        dependentDelta,
                        dependentCandidates.Count,
                        out appliedPrimary,
                        out appliedDependent))
                {
                    bag.Add(Error(
                        JobDiagnosticCodes.InvalidCompensation,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Doping '{doping.Id.Value:D}' has no integer charge-balanced compensation pair: " +
                            $"{requestedPrimary} requested primary exchanges with charge delta " +
                            $"{primaryDelta:R} and {dependentCandidates.Count} available dependent positions " +
                            $"with charge delta {dependentDelta:R}."),
                        path));
                    report = EmptyReport(particles.Length);
                    return false;
                }
            }
            else
            {
                appliedPrimary = requestedPrimary;
            }

            if (appliedPrimary > 0)
            {
                var selected = sampler.SampleWithoutReplacement([.. primaryCandidates], appliedPrimary);
                foreach (var position in selected)
                {
                    particles[position] = (byte)primaryTarget;
                }
            }

            if (appliedDependent > 0)
            {
                var dependentCandidates = new List<int>();
                CollectSourcePositions(
                    particles,
                    positionCount,
                    dependentSite!,
                    dependentSource,
                    dependentCandidates);
                if (dependentCandidates.Count < appliedDependent)
                {
                    bag.Add(Error(
                        JobDiagnosticCodes.InvalidCompensation,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Doping '{doping.Id.Value:D}' requires {appliedDependent} dependent exchanges but " +
                            $"only {dependentCandidates.Count} dependent source particles remain after the primary " +
                            $"application."),
                        path));
                    report = EmptyReport(particles.Length);
                    return false;
                }

                var selected = sampler.SampleWithoutReplacement([.. dependentCandidates], appliedDependent);
                foreach (var position in selected)
                {
                    particles[position] = (byte)dependentTarget;
                }

                var residual = (appliedPrimary * primaryDelta) + (appliedDependent * dependentDelta);
                if (Math.Abs(residual) >
                    DopingReport.ChargeNeutralityToleranceElementary * Math.Max(1, appliedPrimary + appliedDependent))
                {
                    bag.Add(Error(
                        JobDiagnosticCodes.InvalidCompensation,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Doping '{doping.Id.Value:D}' applied a compensated pair that is not charge-balanced " +
                            $"within tolerance (residual {residual:R})."),
                        path));
                    report = EmptyReport(particles.Length);
                    return false;
                }
            }

            if (appliedPrimary > 0 && !doping.ChargeCompensation && primaryDelta != 0.0)
            {
                hasUncompensatedCharge = true;
            }

            var effectiveFraction = available == 0 ? 0.0 : appliedPrimary / (double)available;
            if (effectiveFraction != fraction)
            {
                bag.Add(Warning(
                    JobDiagnosticCodes.FractionMismatch,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Doping '{doping.Id.Value:D}' applied the effective fraction " +
                        $"{effectiveFraction:R} instead of the requested {fraction:R} " +
                        $"({appliedPrimary} of {available} available primary positions)."),
                    path));
            }

            applications.Add(new DopingApplication(
                entry.DopingId,
                entry.Priority,
                available,
                requestedPrimary,
                appliedPrimary,
                appliedDependent,
                effectiveFraction));
        }

        var totalAfter = SumCharge(model, particles);
        ValidateFinalLattice(model, particles, positionCount);
        var isChargeNeutral =
            Math.Abs(totalAfter) <=
            DopingReport.ChargeNeutralityToleranceElementary * Math.Max(1, particles.Length);
        if (hasUncompensatedCharge && !isChargeNeutral)
        {
            bag.Add(Warning(
                JobDiagnosticCodes.NonNeutralLattice,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The final lattice is not charge-neutral (total charge {totalAfter:R}) because an " +
                    $"uncompensated charged doping was applied."),
                path));
        }

        report = new DopingReport(
            applications.ToImmutable(),
            particles.Length,
            totalBefore,
            totalAfter,
            isChargeNeutral);
        return true;
    }

    private static bool TryFindCompensatedPair(
        int requestedPrimary,
        double primaryDelta,
        double dependentDelta,
        int dependentAvailable,
        out int appliedPrimary,
        out int appliedDependent)
    {
        var ratio = -primaryDelta / dependentDelta;
        for (var primaryCount = requestedPrimary; primaryCount >= 1; primaryCount--)
        {
            var dependentCount = primaryCount * ratio;
            if (!double.IsFinite(dependentCount))
            {
                continue;
            }

            var rounded = Math.Round(dependentCount);
            if (Math.Abs(dependentCount - rounded) > DopingReport.ChargeNeutralityToleranceElementary)
            {
                continue;
            }

            if (rounded < 0.0 || rounded > dependentAvailable)
            {
                continue;
            }

            appliedPrimary = primaryCount;
            appliedDependent = (int)rounded;
            return true;
        }

        appliedPrimary = 0;
        appliedDependent = 0;
        return false;
    }

    private static void CollectSourcePositions(
        Span<byte> particles,
        int positionCount,
        CompiledSite site,
        int sourceIndex,
        List<int> candidates)
    {
        candidates.Clear();
        foreach (var position in site.PositionIndices)
        {
            if (position < 0 || position >= positionCount)
            {
                throw new InvalidOperationException(
                    $"Compiled site '{site.Id.Value:D}' owns canonical position {position}, which is outside " +
                    $"the {positionCount} positions per unit cell.");
            }
        }

        for (var offset = 0; offset < particles.Length; offset += positionCount)
        {
            foreach (var position in site.PositionIndices)
            {
                var index = offset + position;
                if (particles[index] == sourceIndex)
                {
                    candidates.Add(index);
                }
            }
        }
    }

    private static CompiledSite ResolveSite(
        CompiledModel model,
        Dictionary<CellSiteId, CompiledSite> siteById,
        CellSiteId siteId,
        Doping doping)
    {
        if (!siteById.TryGetValue(siteId, out var site))
        {
            throw new InvalidOperationException(
                $"Doping '{doping.Id.Value:D}' references site '{siteId.Value:D}', which is not compiled.");
        }

        foreach (var position in site.PositionIndices)
        {
            if (position < 0 || position >= model.Structure.PositionCount)
            {
                throw new InvalidOperationException(
                    $"Compiled site '{siteId.Value:D}' owns canonical position {position}, which is outside " +
                    $"the {model.Structure.PositionCount} canonical P1 positions.");
            }
        }

        return site;
    }

    private static void ValidateFinalLattice(
        CompiledModel model,
        ReadOnlySpan<byte> particles,
        int positionCount)
    {
        var positions = model.Structure.Positions;
        if (positionCount != positions.Length)
        {
            throw new InvalidOperationException(
                $"The lattice position count {positionCount} does not match the {positions.Length} canonical " +
                "P1 positions.");
        }

        if (particles.Length % positionCount != 0)
        {
            throw new InvalidOperationException(
                $"The lattice length {particles.Length} is not a multiple of the {positionCount} positions per cell.");
        }

        for (var offset = 0; offset < particles.Length; offset += positionCount)
        {
            for (var index = 0; index < positionCount; index++)
            {
                var value = particles[offset + index];
                var position = positions[index];
                if (position.Stability == SiteStability.Unstable)
                {
                    if (value != CompiledParticleTable.VoidIndex)
                    {
                        throw new InvalidOperationException(
                            $"Final lattice position {offset + index} of unstable site " +
                            $"'{position.SiteId.Value:D}' is occupied by compiled particle index {value}.");
                    }

                    continue;
                }

                if (value >= 64 || (position.AllowedParticleMask & (1UL << value)) == 0)
                {
                    throw new InvalidOperationException(
                        $"Final lattice position {offset + index} of stable site " +
                        $"'{position.SiteId.Value:D}' is occupied by compiled particle index {value}, which " +
                        "is not in the compiled allowed-particle mask.");
                }
            }
        }
    }

    private static Dictionary<CellSiteId, CompiledSite> IndexSites(CompiledStructureModel structure)
    {
        var sites = new Dictionary<CellSiteId, CompiledSite>(structure.Sites.Length);
        foreach (var site in structure.Sites)
        {
            sites.TryAdd(site.Id, site);
        }

        return sites;
    }

    private static int CompiledIndex(CompiledModel model, ParticleId particleId)
    {
        if (!model.Particles.TryGetIndex(particleId, out var index))
        {
            throw new InvalidOperationException(
                $"Particle '{particleId.Value:D}' is not compiled.");
        }

        if (index is < 0 or > byte.MaxValue)
        {
            throw new InvalidOperationException(
                $"Compiled particle index {index} does not fit the byte lattice.");
        }

        return index;
    }

    private static double Charge(CompiledModel model, int particleIndex) =>
        model.Particles.Particles[particleIndex].ChargeElementary;

    private static double SumCharge(CompiledModel model, ReadOnlySpan<byte> particles)
    {
        var table = model.Particles.Particles;
        var sum = 0.0;
        foreach (var value in particles)
        {
            if (value >= table.Length)
            {
                throw new InvalidOperationException(
                    $"Lattice particle index {value} is outside the {table.Length} compiled particles.");
            }

            sum += table[value].ChargeElementary;
        }

        return sum;
    }

    private static DopingReport EmptyReport(int totalPositions) =>
        new([], totalPositions, 0.0, 0.0, true);

    private static Diagnostic Error(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Warning, message, path, JobDiagnosticCodes.HelpFor(code));
}
