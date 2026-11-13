// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Diagnostics;

/// <summary>
/// Stable diagnostic codes owned by the Step 08 model compilation pipeline.
/// </summary>
public static class CompilationDiagnosticCodes
{
    /// <summary>A required compilation precondition failed.</summary>
    public const string Precondition = "CMP001";

    /// <summary>The structure cannot be encoded or P1-expanded for compilation.</summary>
    public const string Structure = "CMP002";

    /// <summary>Pair or cluster geometry cannot be resolved during compilation.</summary>
    public const string EnergyGeometry = "CMP003";

    /// <summary>Two rules in one collection share a start state but differ in outcome.</summary>
    public const string RuleAmbiguity = "CMP004";

    /// <summary>A transition produces no valid rules after conservation filtering.</summary>
    public const string NoValidRules = "CMP005";

    /// <summary>A configured compilation limit is exceeded.</summary>
    public const string CompileLimit = "CMP006";

    /// <summary>A transition path cannot be expanded into compiled directions.</summary>
    public const string PathExpansion = "CMP007";

    /// <summary>Mass or charge conservation cannot be evaluated.</summary>
    public const string Conservation = "CMP008";

    /// <summary>A transition is not referenced by any simulation and was not compiled.</summary>
    public const string UnusedTransition = "CMP009";

    /// <summary>The advisory rule or table budget is exceeded.</summary>
    public const string AdvisoryBudget = "CMP010";

    /// <summary>The compiled model cannot be signed.</summary>
    public const string Signature = "CMP011";

    /// <summary>A cached stage result failed its integrity check and was recomputed.</summary>
    public const string CacheIntegrity = "CMP012";
}
