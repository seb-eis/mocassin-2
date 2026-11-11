// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model.Lattice;

/// <summary>
/// Validates the definition-level occupation-exchange and doping rules that need only the model graph.
/// </summary>
/// <remarks>
/// Fractions, supercell limits, same-site application, truncation, charge-compensation counts, and the lattice
/// preview are Step 10 rules. The validator never throws for user data.
/// </remarks>
internal static class LatticeModelValidator
{
    /// <summary>
    /// The smallest accepted doping priority.
    /// </summary>
    public const int MinPriority = 0;

    /// <summary>
    /// The largest accepted doping priority.
    /// </summary>
    public const int MaxPriority = 9999;

    /// <summary>
    /// Validates occupation exchanges, dopings, names, priorities, and compensation shape.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bag = new DiagnosticBag();
        var exchanges = model.OccupationExchanges.IsDefault
            ? ImmutableArray<OccupationExchange>.Empty
            : model.OccupationExchanges;
        var dopings = model.Dopings.IsDefault ? ImmutableArray<Doping>.Empty : model.Dopings;
        var sites = model.Structure.Sites.IsDefault
            ? ImmutableArray<CellSite>.Empty
            : model.Structure.Sites;
        var particleSets = model.ParticleSets.IsDefault
            ? ImmutableArray<ParticleSet>.Empty
            : model.ParticleSets;

        var siteById = new Dictionary<CellSiteId, CellSite>();
        foreach (var site in sites)
        {
            siteById.TryAdd(site.Id, site);
        }

        var particleSetById = new Dictionary<ParticleSetId, ParticleSet>();
        foreach (var particleSet in particleSets)
        {
            particleSetById.TryAdd(particleSet.Id, particleSet);
        }

        var particleById = new Dictionary<ParticleId, Particle>();
        if (!model.Particles.IsDefault)
        {
            foreach (var particle in model.Particles)
            {
                particleById.TryAdd(particle.Id, particle);
            }
        }

        particleById.TryAdd(Particle.Void.Id, Particle.Void);

        var exchangeById = ValidateExchanges(
            exchanges,
            siteById,
            particleSetById,
            particleById,
            bag);
        ValidateDopings(dopings, exchangeById, particleById, bag);

        return bag.Sorted;
    }

    private static Dictionary<OccupationExchangeId, OccupationExchange> ValidateExchanges(
        ImmutableArray<OccupationExchange> exchanges,
        Dictionary<CellSiteId, CellSite> siteById,
        Dictionary<ParticleSetId, ParticleSet> particleSetById,
        Dictionary<ParticleId, Particle> particleById,
        DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var exchangeById = new Dictionary<OccupationExchangeId, OccupationExchange>();

        foreach (var exchange in exchanges)
        {
            exchangeById.TryAdd(exchange.Id, exchange);
            ValidateName(exchange.Name, "occupationExchange", exchange.Id.Value, names, bag);

            var siteKnown = siteById.TryGetValue(exchange.SiteId, out var site);
            if (!siteKnown)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.UnknownReference,
                    $"Occupation exchange references unknown site '{exchange.SiteId:D}'.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "siteId"));
            }

            var sourceKnown = particleById.TryGetValue(exchange.SourceParticleId, out _);
            if (!sourceKnown)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.UnknownReference,
                    $"Occupation exchange references unknown source particle '{exchange.SourceParticleId:D}'.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "sourceParticleId"));
            }

            var targetKnown = particleById.TryGetValue(exchange.TargetParticleId, out _);
            if (!targetKnown)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.UnknownReference,
                    $"Occupation exchange references unknown target particle '{exchange.TargetParticleId:D}'.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "targetParticleId"));
            }

            if (exchange.SourceParticleId == exchange.TargetParticleId)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.SourceEqualsTarget,
                    "Occupation-exchange source and target particles must differ.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "targetParticleId"));
            }

            if (!siteKnown)
            {
                continue;
            }

            if (site!.Stability == SiteStability.Unstable)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.UnstableSite,
                    $"Occupation exchange references unstable site '{site.Id:D}'.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "siteId"));
            }

            if (!particleSetById.TryGetValue(site.ParticleSetId, out var particleSet) ||
                particleSet.ParticleIds.IsDefaultOrEmpty)
            {
                continue;
            }

            if (sourceKnown && !particleSet.ParticleIds.Contains(exchange.SourceParticleId))
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.SourceParticleNotInSet,
                    "Occupation-exchange source particle is not in the site's particle set.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "sourceParticleId"));
            }

            if (targetKnown && !particleSet.ParticleIds.Contains(exchange.TargetParticleId))
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.TargetParticleNotInSet,
                    "Occupation-exchange target particle is not in the site's particle set.",
                    "occupationExchange",
                    exchange.Id.Value,
                    "targetParticleId"));
            }
        }

        return exchangeById;
    }

    private static void ValidateDopings(
        ImmutableArray<Doping> dopings,
        Dictionary<OccupationExchangeId, OccupationExchange> exchangeById,
        Dictionary<ParticleId, Particle> particleById,
        DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var priorities = new HashSet<int>();

        foreach (var doping in dopings)
        {
            ValidateName(doping.Name, "doping", doping.Id.Value, names, bag);

            if (doping.Priority is < MinPriority or > MaxPriority)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.PriorityOutOfRange,
                    $"Doping priority {doping.Priority} is outside the range {MinPriority}..{MaxPriority}.",
                    "doping",
                    doping.Id.Value,
                    "priority"));
            }

            if (!priorities.Add(doping.Priority))
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.DuplicatePriority,
                    $"Doping priority {doping.Priority} is used more than once.",
                    "doping",
                    doping.Id.Value,
                    "priority"));
            }

            var primaryKnown = exchangeById.TryGetValue(doping.PrimaryExchangeId, out var primary);
            if (!primaryKnown)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.UnknownExchange,
                    $"Doping references unknown primary exchange '{doping.PrimaryExchangeId:D}'.",
                    "doping",
                    doping.Id.Value,
                    "primaryExchangeId"));
            }

            OccupationExchange? dependent = null;
            var dependentKnown = false;
            if (doping.DependentExchangeId is { } dependentId)
            {
                dependentKnown = exchangeById.TryGetValue(dependentId, out dependent);
                if (!dependentKnown)
                {
                    bag.Add(Error(
                        LatticeDiagnosticCodes.UnknownExchange,
                        $"Doping references unknown dependent exchange '{dependentId:D}'.",
                        "doping",
                        doping.Id.Value,
                        "dependentExchangeId"));
                }
            }

            if (doping.ChargeCompensation && doping.DependentExchangeId is null)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.CompensationShape,
                    "Charge compensation requires a dependent exchange.",
                    "doping",
                    doping.Id.Value,
                    "dependentExchangeId"));
            }

            if (!doping.ChargeCompensation && doping.DependentExchangeId is not null)
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.CompensationShape,
                    "A dependent exchange requires charge compensation to be enabled.",
                    "doping",
                    doping.Id.Value,
                    "dependentExchangeId"));
            }

            if (!doping.ChargeCompensation || !primaryKnown || !dependentKnown || dependent is null)
            {
                continue;
            }

            var primaryDelta = ChargeDelta(primary!, particleById);
            var dependentDelta = ChargeDelta(dependent, particleById);
            if (primaryDelta is null || dependentDelta is null)
            {
                continue;
            }

            if (primaryDelta.Value == 0.0 ||
                dependentDelta.Value == 0.0 ||
                Math.Sign(primaryDelta.Value) == Math.Sign(dependentDelta.Value))
            {
                bag.Add(Error(
                    LatticeDiagnosticCodes.CompensationCharge,
                    "Compensating exchanges require non-zero charge deltas with opposite signs.",
                    "doping",
                    doping.Id.Value,
                    "dependentExchangeId"));
            }
        }
    }

    private static double? ChargeDelta(
        OccupationExchange exchange,
        Dictionary<ParticleId, Particle> particleById)
    {
        if (!particleById.TryGetValue(exchange.SourceParticleId, out var source) ||
            !particleById.TryGetValue(exchange.TargetParticleId, out var target))
        {
            return null;
        }

        return target.Charge.Value - source.Charge.Value;
    }

    private static void ValidateName(
        string name,
        string domain,
        Guid objectId,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bag.Add(Error(
                LatticeDiagnosticCodes.InvalidName,
                $"{DomainName(domain)} name must not be empty.",
                domain,
                objectId,
                "name"));
            return;
        }

        if (!names.Add(name))
        {
            bag.Add(Error(
                LatticeDiagnosticCodes.InvalidName,
                $"{DomainName(domain)} name '{name}' is used more than once.",
                domain,
                objectId,
                "name"));
        }
    }

    private static string DomainName(string domain) => domain switch
    {
        "occupationExchange" => "Occupation-exchange",
        "doping" => "Doping",
        _ => domain,
    };

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property), LatticeDiagnosticCodes.HelpFor(code));
}
