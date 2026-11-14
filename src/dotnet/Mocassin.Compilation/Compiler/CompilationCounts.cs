// SPDX-License-Identifier: MIT


namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Authoritative compiled-content counts for the compilation report.
/// </summary>
public sealed record CompilationCounts
{
    /// <summary>Gets the compiled particle count, including void.</summary>
    public int Particles { get; init; }
    /// <summary>Gets the canonical P1 position count.</summary>
    public int Positions { get; init; }
    /// <summary>Gets the stable position count.</summary>
    public int StablePositions { get; init; }
    /// <summary>Gets the unstable position count.</summary>
    public int UnstablePositions { get; init; }
    /// <summary>Gets the reference-site count.</summary>
    public int Sites { get; init; }
    /// <summary>Gets the interaction-environment count.</summary>
    public int Environments { get; init; }
    /// <summary>Gets the directed pair-geometry count.</summary>
    public int PairGeometries { get; init; }
    /// <summary>Gets the shared pair-table count.</summary>
    public int PairTables { get; init; }
    /// <summary>Gets the reciprocal chiral-pair count.</summary>
    public int ChiralPairs { get; init; }
    /// <summary>Gets the shared cluster-table count.</summary>
    public int ClusterTables { get; init; }
    /// <summary>Gets the canonical cluster-occupation count.</summary>
    public int ClusterOccupations { get; init; }
    /// <summary>Gets the compiled transition count.</summary>
    public int Transitions { get; init; }
    /// <summary>Gets the compiled collection count.</summary>
    public int Collections { get; init; }
    /// <summary>Gets the compiled direction count.</summary>
    public int Directions { get; init; }
    /// <summary>Gets the compiled rule count.</summary>
    public int Rules { get; init; }
    /// <summary>Gets the number of non-inverse rules.</summary>
    public int ForwardRules { get; init; }
    /// <summary>Gets the number of automatic inverse rules.</summary>
    public int InverseRules { get; init; }
    /// <summary>Gets the number of extra merged direction instances.</summary>
    public int DependentRules { get; init; }
    /// <summary>Gets the rules rejected by mass conservation.</summary>
    public int RejectedByMass { get; init; }
    /// <summary>Gets the rules rejected by charge conservation.</summary>
    public int RejectedByCharge { get; init; }
    /// <summary>Gets the rules rejected by invalid state roles.</summary>
    public int RejectedByInvalidState { get; init; }
    /// <summary>Gets the global tracker count.</summary>
    public int GlobalTrackers { get; init; }
    /// <summary>Gets the static tracker count per cell.</summary>
    public int StaticTrackersPerCell { get; init; }
    /// <summary>Gets the compiled simulation count.</summary>
    public int Simulations { get; init; }
    /// <summary>Gets the estimated energy-table size in bytes.</summary>
    public long EstimatedEnergyTableBytes { get; init; }

    /// <summary>Gets the empty count record.</summary>
    public static CompilationCounts Empty { get; } = new();

    /// <summary>
    /// Builds the counts from a compiled model.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The counts.</returns>
    public static CompilationCounts FromModel(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var particleCount = model.Particles.Count;
        var pairBytes = model.Energy.PairTables.Sum(
            static table => (long)table.ParticleCount * table.ParticleCount * sizeof(double));
        var clusterBytes = model.Energy.ClusterTables.Sum(
            static table => (long)table.OccupationCount * sizeof(double));
        return new CompilationCounts
        {
            Particles = particleCount,
            Positions = model.Structure.PositionCount,
            StablePositions = model.Structure.Positions.Count(static position => position.Stability == Core.Model.Structure.SiteStability.Stable),
            UnstablePositions = model.Structure.Positions.Count(static position => position.Stability == Core.Model.Structure.SiteStability.Unstable),
            Sites = model.Structure.SiteCount,
            Environments = model.Structure.EnvironmentCount,
            PairGeometries = model.Energy.PairGeometryCount,
            PairTables = model.Energy.PairTableCount,
            ChiralPairs = model.Energy.PairGeometries.Count(static geometry => geometry.IsChiral) / 2,
            ClusterTables = model.Energy.ClusterTableCount,
            ClusterOccupations = model.Energy.ClusterOccupationCount,
            Transitions = model.Transitions.CollectionCount,
            Collections = model.Transitions.CollectionCount,
            Directions = model.Transitions.DirectionCount,
            Rules = model.Transitions.RuleCount,
            ForwardRules = model.Transitions.Rules.Count(static rule => !rule.IsInverse),
            InverseRules = model.Transitions.InverseRuleCount,
            DependentRules = model.Transitions.Rules.Sum(static rule => Math.Max(0, rule.DirectionIndices.Length - 1)),
            RejectedByMass = model.Transitions.RejectedByMass,
            RejectedByCharge = model.Transitions.RejectedByCharge,
            RejectedByInvalidState = model.Transitions.RejectedByInvalidState,
            GlobalTrackers = model.Transitions.GlobalTrackers.Length,
            StaticTrackersPerCell = model.Transitions.StaticTrackers.Length,
            Simulations = model.Simulations.Length,
            EstimatedEnergyTableBytes = pairBytes + clusterBytes,
        };
    }
}
