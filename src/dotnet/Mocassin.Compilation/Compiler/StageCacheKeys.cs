// SPDX-License-Identifier: MIT

using System.Globalization;
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
        Key("structure", context, ProjectHasher.ComputeStructure(context.Project.Structure));

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
    public static string Rules(CompilationContext context) =>
        Key(
            "rules",
            context,
            ProjectHasher.ComputeSimulationSelection(context.Project),
            ProjectHasher.ComputeStructure(context.Project.Structure));

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
