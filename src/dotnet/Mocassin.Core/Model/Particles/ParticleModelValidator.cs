// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Particles;

/// <summary>
/// Validates the particle and particle-set domain without throwing for user data.
/// </summary>
internal static partial class ParticleModelValidator
{
    /// <summary>
    /// Validates particle names, symbols, limits, references, reserved identifiers, and set contents.
    /// </summary>
    /// <param name="model">The in-memory particle grouping to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(ParticleModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bag = new DiagnosticBag();
        var particles = model.Particles;
        var particleSets = model.ParticleSets;

        if (particles.Length > ParticleModel.MaxCustomParticles)
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.ParticleLimitExceeded,
                $"The model defines {particles.Length} custom particles; the limit is {ParticleModel.MaxCustomParticles}.",
                "particle",
                Guid.Empty));
        }

        if (particleSets.Length > ParticleModel.MaxParticleSets)
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.SetLimitExceeded,
                $"The model defines {particleSets.Length} particle sets; the limit is {ParticleModel.MaxParticleSets}.",
                "particleSet",
                Guid.Empty));
        }

        var particleIds = new HashSet<ParticleId>();
        var particleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var particle in particles)
        {
            particleIds.Add(particle.Id);

            if (particle.Id == ParticleId.Void)
            {
                bag.Add(Error(
                    ParticleDiagnosticCodes.ReservedIdentifier,
                    "The reserved void particle identifier must not be used by an authored particle.",
                    "particle",
                    particle.Id.Value));
            }

            ValidateParticleName(particle, particleNames, bag);
            ValidateParticleSymbol(particle, bag);
        }

        var setNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var acceptedSets = new List<ParticleSet>(particleSets.Length);
        foreach (var particleSet in particleSets)
        {
            if (particleSet.Id == ParticleSetId.Empty)
            {
                bag.Add(Error(
                    ParticleDiagnosticCodes.ReservedIdentifier,
                    "The reserved empty particle-set identifier must not be used by an authored particle set.",
                    "particleSet",
                    particleSet.Id.Value));
            }

            ValidateSetName(particleSet, setNames, bag);
            ValidateSetMembers(particleSet, particleIds, bag);

            if (acceptedSets.Exists(previous => previous.ParticleIds.SequenceEqual(particleSet.ParticleIds)))
            {
                bag.Add(Warning(
                    ParticleDiagnosticCodes.DuplicateSetContent,
                    "The particle set has the same ordered particle list as another set.",
                    "particleSet",
                    particleSet.Id.Value));
            }

            acceptedSets.Add(particleSet);
        }

        return bag.Sorted;
    }

    private static void ValidateParticleName(
        Particle particle,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(particle.Name))
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.InvalidName,
                "Particle name must not be empty.",
                "particle",
                particle.Id.Value,
                "name"));
            return;
        }

        if (!names.Add(particle.Name))
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.InvalidName,
                $"Particle name '{particle.Name}' is used more than once.",
                "particle",
                particle.Id.Value,
                "name"));
        }
    }

    private static void ValidateParticleSymbol(Particle particle, DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(particle.Symbol) || !SymbolPattern().IsMatch(particle.Symbol))
        {
            bag.Add(Warning(
                ParticleDiagnosticCodes.NonRecommendedSymbol,
                $"Particle symbol '{particle.Symbol}' does not match the recommended pattern " +
                $"'{Particle.RecommendedSymbolPattern}'.",
                "particle",
                particle.Id.Value,
                "symbol"));
        }
    }

    private static void ValidateSetName(
        ParticleSet particleSet,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(particleSet.Name))
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.InvalidSetName,
                "Particle-set name must not be empty.",
                "particleSet",
                particleSet.Id.Value,
                "name"));
            return;
        }

        if (!names.Add(particleSet.Name))
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.InvalidSetName,
                $"Particle-set name '{particleSet.Name}' is used more than once.",
                "particleSet",
                particleSet.Id.Value,
                "name"));
        }
    }

    private static void ValidateSetMembers(
        ParticleSet particleSet,
        HashSet<ParticleId> authoredParticles,
        DiagnosticBag bag)
    {
        var memberIds = particleSet.ParticleIds;
        if (memberIds.IsDefault || memberIds.Length == 0)
        {
            bag.Add(Error(
                ParticleDiagnosticCodes.InvalidSetMembers,
                "Particle set must contain at least one particle.",
                "particleSet",
                particleSet.Id.Value,
                "particleIds"));
            return;
        }

        var seen = new HashSet<ParticleId>();
        var reportedVoid = false;
        foreach (var memberId in memberIds)
        {
            if (memberId == ParticleId.Void)
            {
                if (!reportedVoid)
                {
                    bag.Add(Error(
                        ParticleDiagnosticCodes.InvalidSetMembers,
                        "Particle set must not contain the implicit void particle.",
                        "particleSet",
                        particleSet.Id.Value,
                        "particleIds"));
                    reportedVoid = true;
                }
            }
            else if (!authoredParticles.Contains(memberId))
            {
                bag.Add(Error(
                    ParticleDiagnosticCodes.UnknownParticleReference,
                    $"Particle set references unknown particle '{memberId:D}'.",
                    "particleSet",
                    particleSet.Id.Value,
                    "particleIds"));
            }

            if (!seen.Add(memberId))
            {
                bag.Add(Error(
                    ParticleDiagnosticCodes.DuplicateSetMember,
                    $"Particle set contains particle '{memberId:D}' more than once.",
                    "particleSet",
                    particleSet.Id.Value,
                    "particleIds"));
            }
        }
    }

    [GeneratedRegex(Particle.RecommendedSymbolPattern, RegexOptions.CultureInvariant)]
    private static partial Regex SymbolPattern();

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property));

    private static Diagnostic Warning(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Warning, message, new ModelPath(domain, objectId, property));
}
