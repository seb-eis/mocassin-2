// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Stable compilation diagnostic code literals. The public catalog is added by the diagnostics feature.
/// </summary>
internal static class CompilationErrorCodes
{
    public const string Precondition = "CMP001";
    public const string Structure = "CMP002";
    public const string EnergyGeometry = "CMP003";
    public const string RuleAmbiguity = "CMP004";
    public const string NoValidRules = "CMP005";
    public const string CompileLimit = "CMP006";
    public const string PathExpansion = "CMP007";
    public const string Conservation = "CMP008";
    public const string UnusedTransition = "CMP009";
    public const string AdvisoryBudget = "CMP010";
    public const string Signature = "CMP011";
    public const string CacheIntegrity = "CMP012";
}
