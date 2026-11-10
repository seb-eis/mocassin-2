// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by simulation-model validation.
/// </summary>
/// <remarks>
/// Step 06 owns the definition-level rules. Job expansion, effective job counts, and package writing belong to
/// Steps 10 and 11.
/// </remarks>
public static class SimulationDiagnosticCodes
{
    /// <summary>
    /// A simulation references an unknown transition.
    /// </summary>
    public const string UnknownTransition = "SIM001";

    /// <summary>
    /// A simulation references a transition of the other kind.
    /// </summary>
    public const string WrongTransitionKind = "SIM002";

    /// <summary>
    /// A simulation has no transitions and cannot be built until Step 07/10 feasibility passes.
    /// </summary>
    public const string NoTransitions = "SIM003";

    /// <summary>
    /// Temperature is non-finite or outside the hard range.
    /// </summary>
    public const string InvalidTemperature = "SIM004";

    /// <summary>
    /// Target MCSP is outside the hard range.
    /// </summary>
    public const string InvalidTargetMcsp = "SIM005";

    /// <summary>
    /// Job count is outside the hard range.
    /// </summary>
    public const string InvalidJobCount = "SIM006";

    /// <summary>
    /// The KMC field direction is non-finite or zero with a non-zero modulus.
    /// </summary>
    public const string InvalidFieldDirection = "SIM007";

    /// <summary>
    /// KMC pre-run MCSP is outside the hard range.
    /// </summary>
    public const string InvalidPreRunMcsp = "SIM008";

    /// <summary>
    /// A simulation name is empty, whitespace-only, or duplicated.
    /// </summary>
    public const string InvalidName = "SIM009";

    /// <summary>
    /// Write-out count is outside the hard range.
    /// </summary>
    public const string InvalidWriteOutCount = "SIM010";

    /// <summary>
    /// The wall-clock time limit is present but not positive.
    /// </summary>
    public const string InvalidTimeLimit = "SIM011";

    /// <summary>
    /// The minimum success rate is negative, non-finite, or above <see cref="int.MaxValue"/>.
    /// </summary>
    public const string InvalidMinSuccessRate = "SIM012";

    /// <summary>
    /// The KMC field modulus is negative or non-finite.
    /// </summary>
    public const string InvalidFieldModulus = "SIM013";

    /// <summary>
    /// The KMC normalization energy is negative or non-finite.
    /// </summary>
    public const string InvalidNormalizationEnergy = "SIM014";

    /// <summary>
    /// The KMC maximum attempt frequency is zero, negative, or non-finite.
    /// </summary>
    public const string InvalidMaxAttemptFrequency = "SIM015";

    /// <summary>
    /// MMC break tolerance is non-finite or outside <c>0..1</c>.
    /// </summary>
    public const string InvalidBreakTolerance = "SIM016";

    /// <summary>
    /// MMC break sample length is outside the hard range.
    /// </summary>
    public const string InvalidBreakSampleLength = "SIM017";

    /// <summary>
    /// MMC break sample interval is outside the hard range.
    /// </summary>
    public const string InvalidBreakSampleInterval = "SIM018";

    /// <summary>
    /// MMC result sample is outside the hard range.
    /// </summary>
    public const string InvalidResultSample = "SIM019";

    /// <summary>
    /// The RNG seed is longer than 100 characters.
    /// </summary>
    public const string RngSeedTooLong = "SIM020";

    /// <summary>
    /// A simulation references more than 100 transitions.
    /// </summary>
    public const string TransitionLimitExceeded = "SIM021";

    /// <summary>
    /// A simulation references the same transition more than once.
    /// </summary>
    public const string DuplicateTransitionReference = "SIM022";

    /// <summary>
    /// The persisted kind-specific shape disagrees with the concrete simulation record.
    /// </summary>
    public const string KindShapeMismatch = "SIM023";

    /// <summary>
    /// Temperature is outside the recommended band.
    /// </summary>
    public const string TemperatureOutsideRecommendedBand = "SIM024";

    /// <summary>
    /// Target MCSP is above the recommended value.
    /// </summary>
    public const string TargetMcspAboveRecommended = "SIM025";

    /// <summary>
    /// Job count is outside the recommended band.
    /// </summary>
    public const string JobCountOutsideRecommendedBand = "SIM026";

    /// <summary>
    /// KMC pre-run MCSP is above the recommended value.
    /// </summary>
    public const string PreRunAboveRecommended = "SIM027";

    /// <summary>
    /// Write-out count is outside the recommended band; 0 is allowed.
    /// </summary>
    public const string WriteOutOutsideRecommendedBand = "SIM028";

    /// <summary>
    /// The KMC field modulus is outside the recommended band.
    /// </summary>
    public const string FieldModulusOutsideRecommendedBand = "SIM029";

    /// <summary>
    /// MMC break tolerance is above the recommended value.
    /// </summary>
    public const string BreakToleranceAboveRecommended = "SIM030";

    /// <summary>
    /// MMC break sample length is above the recommended value.
    /// </summary>
    public const string BreakSampleLengthAboveRecommended = "SIM031";

    /// <summary>
    /// MMC break sample interval is above the recommended value.
    /// </summary>
    public const string BreakSampleIntervalAboveRecommended = "SIM032";

    /// <summary>
    /// MMC result sample is above the recommended value.
    /// </summary>
    public const string ResultSampleAboveRecommended = "SIM033";

    /// <summary>
    /// KMC normalization energy is above the recommended value.
    /// </summary>
    public const string NormalizationEnergyAboveRecommended = "SIM034";

    /// <summary>
    /// The project execution-flag value contains a duplicate, derived, or compiler-owned flag.
    /// </summary>
    public const string InvalidExecutionFlags = "SIM035";

    /// <summary>
    /// A simulation references 11 to 100 transitions.
    /// </summary>
    public const string TransitionCountOutsideRecommendedBand = "SIM036";

    /// <summary>
    /// The minimum success rate is positive but below the recommended value.
    /// </summary>
    public const string MinSuccessRateBelowRecommended = "SIM037";

    /// <summary>
    /// Gets the actionable help text for a simulation diagnostic code.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <returns>The help text, or <see langword="null"/> when the code is not a simulation code.</returns>
    public static string? HelpFor(string code) => code switch
    {
        UnknownTransition => "Reference an existing transition of the simulation's kind.",
        WrongTransitionKind => "Use kinetic transitions for KMC and Metropolis transitions for MMC.",
        NoTransitions => "Assign at least one transition before building the simulation.",
        InvalidTemperature => "Use a finite temperature in 0.1..10000 K.",
        InvalidTargetMcsp => "Use a target MCSP in 1..1,000,000.",
        InvalidJobCount => "Use a job count in 1..1000.",
        InvalidFieldDirection => "Use three finite direction components and a non-zero vector for a non-zero field.",
        InvalidPreRunMcsp => "Use a pre-run MCSP in 0..1,000,000; 0 disables it.",
        InvalidName => "Use a unique non-empty simulation name.",
        InvalidWriteOutCount => "Use a write-out count in 0..1000; 0 writes only the final state.",
        InvalidTimeLimit => "Use a positive wall-clock limit or null for no limit.",
        InvalidMinSuccessRate => "Use null or 0 to disable the abort, or a finite value up to int.MaxValue.",
        InvalidFieldModulus => "Use a finite non-negative field modulus.",
        InvalidNormalizationEnergy => "Use null, 0, or a finite positive normalization energy.",
        InvalidMaxAttemptFrequency => "Use null or a finite positive maximum attempt frequency.",
        InvalidBreakTolerance => "Use a finite break tolerance in 0..1.",
        InvalidBreakSampleLength => "Use a break sample length in 0..100,000.",
        InvalidBreakSampleInterval => "Use a break sample interval in 1..1000.",
        InvalidResultSample => "Use a result sample in 0..1,000,000.",
        RngSeedTooLong => "Shorten the RNG seed to at most 100 characters or leave it null.",
        TransitionLimitExceeded => "Keep at most 100 transition references in one simulation.",
        DuplicateTransitionReference => "Remove the duplicate transition reference.",
        KindShapeMismatch => "Use only the matching transition array and kind-specific settings for the declared kind.",
        TemperatureOutsideRecommendedBand => "Use 100..5000 K unless the unusual temperature is intentional.",
        TargetMcspAboveRecommended => "Use at most 1000 target MCSP unless the longer run is intentional.",
        JobCountOutsideRecommendedBand => "Use 5..100 jobs unless the unusual count is intentional.",
        PreRunAboveRecommended => "Use at most 1000 pre-run MCSP unless the longer relaxation is intentional.",
        WriteOutOutsideRecommendedBand => "Use 0 or 5..100 write-outs unless the unusual count is intentional.",
        FieldModulusOutsideRecommendedBand => "Use 1e4..1e9 V/m unless the unusual field is intentional.",
        BreakToleranceAboveRecommended => "Use at most 0.1 break tolerance unless the looser check is intentional.",
        BreakSampleLengthAboveRecommended => "Use at most 10,000 break sample MCSP unless intentional.",
        BreakSampleIntervalAboveRecommended => "Use at most 100 break sample interval MCSP unless intentional.",
        ResultSampleAboveRecommended => "Use at most 10,000 result sample MCSP unless intentional.",
        NormalizationEnergyAboveRecommended => "Use at most 10 eV normalization energy unless intentional.",
        InvalidExecutionFlags => "Use only noSaving, noJumpLogging, useFastExp, and deterministic; omit duplicates.",
        TransitionCountOutsideRecommendedBand => "Use 1..10 transitions unless the larger set is intentional.",
        MinSuccessRateBelowRecommended => "Use null or at least 10 MCS/s unless the low rate is intentional.",
        _ => null,
    };
}
