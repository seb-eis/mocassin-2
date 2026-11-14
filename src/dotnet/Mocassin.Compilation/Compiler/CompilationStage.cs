// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Identifies one ordered stage of the model compilation pipeline.
/// </summary>
/// <remarks>
/// The numeric order is the execution order. Stages before <see cref="Indexing"/> produce content, the
/// <see cref="Indexing"/> stage assigns dense indices, <see cref="Signing"/> produces the final
/// <see cref="CompiledModel"/>, and <see cref="Report"/> summarizes the run and always executes.
/// </remarks>
public enum CompilationStage
{
    /// <summary>
    /// Resolves the catalog setting, cell geometry, encoder, and sorted particles and sites.
    /// </summary>
    Resolve = 0,

    /// <summary>
    /// Expands the canonical P1 structure and partitions stable and unstable positions.
    /// </summary>
    Structure = 1,

    /// <summary>
    /// Builds the stable and unstable interaction environments.
    /// </summary>
    Environments = 2,

    /// <summary>
    /// Reduces pair geometries, shared energy identities, chirality, and pair tables.
    /// </summary>
    Pairs = 3,

    /// <summary>
    /// Reduces cluster geometries and builds canonical cluster tables.
    /// </summary>
    Clusters = 4,

    /// <summary>
    /// Expands transition directions and generates forward, inverse, and dependent rules.
    /// </summary>
    Rules = 5,

    /// <summary>
    /// Encodes collections, directions, rules, jump mappings, trackers, and derived solver intent.
    /// </summary>
    Encoding = 6,

    /// <summary>
    /// Assigns dense indices and stable identifier lookup maps.
    /// </summary>
    Indexing = 7,

    /// <summary>
    /// Computes the canonical model signature and returns the signed compiled model.
    /// </summary>
    Signing = 8,

    /// <summary>
    /// Summarizes counts, timings, diagnostics, and cache statistics.
    /// </summary>
    Report = 9,
}
