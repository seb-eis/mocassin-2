// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by lattice and doping model validation.
/// </summary>
/// <remarks>
/// Core owns the definition-level rules. Fractions, supercells, truncation, charge-compensation counts, and the
/// lattice preview belong to Step 10 (<c>JOB004</c>–<c>JOB015</c>).
/// </remarks>
public static class LatticeDiagnosticCodes
{
    /// <summary>
    /// An occupation exchange references an unknown site or particle.
    /// </summary>
    public const string UnknownReference = "LAT001";

    /// <summary>
    /// An occupation exchange references an unstable site.
    /// </summary>
    public const string UnstableSite = "LAT002";

    /// <summary>
    /// An occupation-exchange source particle is not in the site's particle set.
    /// </summary>
    public const string SourceParticleNotInSet = "LAT003";

    /// <summary>
    /// An occupation-exchange target particle is not in the site's particle set.
    /// </summary>
    public const string TargetParticleNotInSet = "LAT004";

    /// <summary>
    /// An occupation exchange uses the same source and target particle.
    /// </summary>
    public const string SourceEqualsTarget = "LAT005";

    /// <summary>
    /// A doping priority is used more than once.
    /// </summary>
    public const string DuplicatePriority = "LAT006";

    /// <summary>
    /// A doping references an unknown occupation exchange.
    /// </summary>
    public const string UnknownExchange = "LAT007";

    /// <summary>
    /// The dependent exchange and <c>chargeCompensation</c> disagree.
    /// </summary>
    public const string CompensationShape = "LAT008";

    /// <summary>
    /// A compensating dependent exchange has a zero or same-sign charge delta.
    /// </summary>
    public const string CompensationCharge = "LAT009";

    /// <summary>
    /// A doping priority is outside <c>0..9999</c>.
    /// </summary>
    public const string PriorityOutOfRange = "LAT010";

    /// <summary>
    /// An occupation-exchange or doping name is empty or duplicated case-insensitively.
    /// </summary>
    public const string InvalidName = "LAT011";

    /// <summary>
    /// Gets the actionable help text for a lattice diagnostic code.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <returns>The help text, or <see langword="null"/> when the code is not a lattice code.</returns>
    public static string? HelpFor(string code) => code switch
    {
        UnknownReference => "Reference an existing site and existing particles (or the implicit void particle).",
        UnstableSite => "Apply occupation exchanges only at stable sites.",
        SourceParticleNotInSet => "Use a source particle that belongs to the site's particle set.",
        TargetParticleNotInSet => "Use a target particle that belongs to the site's particle set.",
        SourceEqualsTarget => "Choose a different source or target particle.",
        DuplicatePriority => "Give every doping a unique priority so the application order is deterministic.",
        UnknownExchange => "Reference an existing primary or dependent occupation exchange.",
        CompensationShape => "Set chargeCompensation true only with a dependent exchange, and false without one.",
        CompensationCharge => "Use non-zero primary and dependent charge deltas with opposite signs.",
        PriorityOutOfRange => "Use a doping priority in the inclusive range 0..9999.",
        InvalidName => "Use a unique non-empty name within the collection.",
        _ => null,
    };
}
