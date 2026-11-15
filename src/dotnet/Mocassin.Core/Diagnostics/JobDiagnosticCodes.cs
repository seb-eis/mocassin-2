// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by job-template and job-settings validation.
/// </summary>
/// <remarks>
/// Step 10 owns the structural rules that can run without a compiled model. Compiled-bound doping, supercell,
/// and optimizer checks belong to the Packages build stage and reuse these codes.
/// </remarks>
public static class JobDiagnosticCodes
{
    /// <summary>
    /// A job template has no collections.
    /// </summary>
    public const string NoCollections = "JOB001";

    /// <summary>
    /// A collection references an unknown simulation.
    /// </summary>
    public const string UnknownSimulation = "JOB002";

    /// <summary>
    /// A collection has no configurations.
    /// </summary>
    public const string NoConfigs = "JOB003";

    /// <summary>
    /// A cell size is outside <c>1..256</c>, or the total position count exceeds the hard cap.
    /// </summary>
    public const string InvalidCellSize = "JOB004";

    /// <summary>
    /// A doping fraction is non-finite or outside <c>[0, 1]</c>.
    /// </summary>
    public const string InvalidDopingFraction = "JOB005";

    /// <summary>
    /// A doping reference does not resolve to an authored doping.
    /// </summary>
    public const string UnknownDoping = "JOB006";

    /// <summary>
    /// A collection or configuration name is empty or duplicated.
    /// </summary>
    public const string DuplicateName = "JOB007";

    /// <summary>
    /// A job count is outside <c>1..1000</c>.
    /// </summary>
    public const string InvalidJobCount = "JOB008";

    /// <summary>
    /// A selection optimizer references an invalid site or particle and cannot remove a usable start option.
    /// </summary>
    public const string InvalidOptimizer = "JOB009";

    /// <summary>
    /// A requested doping cannot be realized and applies zero counts.
    /// </summary>
    public const string UnrealizedDoping = "JOB010";

    /// <summary>
    /// The effective doping fraction differs from the requested fraction.
    /// </summary>
    public const string FractionMismatch = "JOB011";

    /// <summary>
    /// A compensation pair cannot be realized or is not charge-balanced.
    /// </summary>
    public const string InvalidCompensation = "JOB012";

    /// <summary>
    /// The expanded job total exceeds the warning threshold.
    /// </summary>
    public const string JobCountWarning = "JOB013";

    /// <summary>
    /// The generated supercell position count exceeds the warning threshold.
    /// </summary>
    public const string LargeSupercell = "JOB014";

    /// <summary>
    /// Inherited settings are missing, invalid, or not applicable to the simulation kind.
    /// </summary>
    public const string InvalidSettings = "JOB015";

    /// <summary>
    /// The final lattice is not charge-neutral because an uncompensated charged doping was applied.
    /// </summary>
    public const string NonNeutralLattice = "JOB016";

    /// <summary>
    /// A configuration repeats a doping reference, or a collection repeats an optimizer entry.
    /// </summary>
    public const string DuplicateReference = "JOB017";

    /// <summary>
    /// An extension id, config shape, or extension bound is invalid.
    /// </summary>
    public const string InvalidExtension = "JOB018";

    /// <summary>
    /// The expanded job total exceeds the hard cap.
    /// </summary>
    public const string JobLimitExceeded = "JOB019";

    /// <summary>
    /// A template, collection, configuration, optimizer, or doping count exceeds a structural cap.
    /// </summary>
    public const string LimitExceeded = "JOB020";

    /// <summary>
    /// Gets the actionable help text for a job-template diagnostic code.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <returns>The help text, or <see langword="null"/> when the code is not a job-template code.</returns>
    public static string? HelpFor(string code) => code switch
    {
        NoCollections => "Add at least one collection with one configuration.",
        UnknownSimulation => "Reference an existing simulation of the matching kind.",
        NoConfigs => "Add at least one configuration to the collection.",
        InvalidCellSize => "Use cell counts in 1..256 and keep the generated supercell within 500,000 positions.",
        InvalidDopingFraction => "Use a finite doping fraction in the inclusive range 0..1.",
        UnknownDoping => "Reference an existing doping definition.",
        DuplicateName => "Give every collection and every configuration a unique, non-empty name.",
        InvalidJobCount => "Use a job count in 1..1000.",
        InvalidOptimizer => "Use a stable site and a particle allowed at that site, and keep a usable start option.",
        UnrealizedDoping => "Increase the available source population or lower the requested fraction.",
        FractionMismatch => "Review the requested fraction; applied counts are truncated downward.",
        InvalidCompensation => "Choose dopings, charges, and fractions that admit an integer balanced pair.",
        JobCountWarning => "Reduce the configurations or jobs per configuration unless the large run is intentional.",
        LargeSupercell => "Reduce the cell size unless the large supercell is intentional.",
        InvalidSettings => "Set finite values within the documented ranges and use only fields for the simulation kind.",
        NonNeutralLattice => "Enable charge compensation or accept the non-neutral lattice knowingly.",
        DuplicateReference => "Remove duplicate doping references and duplicate (site, particle) optimizers.",
        InvalidExtension => "Use a unique non-empty id (at most 64 characters), an object config of at most 64 KiB, at most 16 entries, and at most 256 KiB in total.",
        JobLimitExceeded => "Reduce the template to at most 99,999 expanded jobs.",
        LimitExceeded => "Reduce the template to the documented Step 10 structural caps.",
        _ => null,
    };
}
