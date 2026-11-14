// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Context;
using Mocassin.Core.Project;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Builds content-addressed cache keys for the compiler stages.
/// </summary>
internal static class StageCacheKeys
{
    /// <summary>
    /// Creates the structure-stage key.
    /// </summary>
    public static string Structure(CompilationContext context) =>
        Key(
            "structure",
            context,
            ProjectHasher.ComputeStructure(context.Project.Structure),
            ParticleHash(context));

    /// <summary>
    /// Creates the pair/energy-stage key.
    /// </summary>
    public static string Energy(CompilationContext context) =>
        Key(
            "energy",
            context,
            ProjectHasher.ComputeEnergy(context.Project.Model),
            ProjectHasher.ComputeStructure(context.Project.Structure));

    /// <summary>
    /// Creates the cluster-stage key.
    /// </summary>
    public static string Clusters(CompilationContext context) =>
        Key(
            "clusters",
            context,
            ProjectHasher.ComputeEnergy(context.Project.Model),
            ProjectHasher.ComputeStructure(context.Project.Structure));

    /// <summary>
    /// Creates the rule-stage key.
    /// </summary>
    public static string Rules(CompilationContext context)
    {
        var references = context.Project.Simulations.IsDefault
            ? []
            : context.Project.Simulations
                .SelectMany(static simulation => simulation.TransitionIds.IsDefault ? [] : simulation.TransitionIds)
                .Distinct()
                .OrderBy(static reference => reference.Kind)
                .ThenBy(static reference => reference.Id)
                .ToArray();
        var closures = references
            .Select(reference => ProjectHasher.ComputeTransition(context.Project, reference))
            .ToArray();
        return Key(
            "rules",
            context,
            ProjectHasher.ComputeSimulationSelection(context.Project),
            ProjectHasher.ComputeStructure(context.Project.Structure),
            ParticleHash(context),
            string.Join('|', closures));
    }

    private static string ParticleHash(CompilationContext context)
    {
        var builder = new StringBuilder();
        var particles = context.Project.Particles.IsDefault ? [] : context.Project.Particles;
        foreach (var particle in particles.OrderBy(static particle => particle.Id.Value))
        {
            builder.Append(particle.Id.Value.ToString("D", CultureInfo.InvariantCulture)).Append('|')
                .Append(particle.Symbol).Append('|')
                .Append(particle.Charge.Value.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(particle.IsVacancy ? '1' : '0').Append(';');
        }

        var sets = context.Project.Model.ParticleSets.IsDefault ? [] : context.Project.Model.ParticleSets;
        foreach (var set in sets.OrderBy(static set => set.Id.Value))
        {
            builder.Append('#')
                .Append(set.Id.Value.ToString("D", CultureInfo.InvariantCulture))
                .Append('|');
            foreach (var particle in set.ParticleIds)
            {
                builder.Append(particle.Value.ToString("D", CultureInfo.InvariantCulture)).Append(',');
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(builder.ToString())));
    }

    /// <summary>
    /// Creates the encoding-stage key.
    /// </summary>
    public static string Encoding(CompilationContext context) =>
        Key(
            "encoding",
            context,
            ProjectHasher.ComputeDocument(context.Project),
            ProjectHasher.ComputeStructure(context.Project.Structure));

    /// <summary>
    /// Creates a signing key from the canonical projection.
    /// </summary>
    public static string Signing(CompilationContext context, string projectionHash) =>
        Key("signing", context, projectionHash);

    private static string Key(string domain, CompilationContext context, params string[] parts) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{domain}|{context.Versions.SpaceGroupDataVersion}|{context.Versions.SpaceGroupDataSha256}|" +
            $"{context.Versions.SymmetryContractVersion}|{string.Join('|', parts)}");
}
