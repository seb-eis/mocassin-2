// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by parameterization templates.
/// </summary>
public static class ParameterizationDiagnosticCodes
{
    /// <summary>
    /// An energy is non-finite or outside the accepted energy range.
    /// </summary>
    public const string InvalidEnergy = "PRM004";

    /// <summary>
    /// A frequency is non-positive or non-finite.
    /// </summary>
    public const string InvalidFrequency = "PRM005";

    /// <summary>
    /// Two parameterization templates use the same name case-insensitively.
    /// </summary>
    public const string DuplicateName = "PRM010";

    /// <summary>
    /// A template or apply resource limit is exceeded.
    /// </summary>
    public const string LimitExceeded = "PRM013";

    /// <summary>
    /// A stable identity is malformed, a value key is duplicated, or a template identifier is empty.
    /// </summary>
    public const string InvalidKey = "PRM014";

    /// <summary>
    /// Gets the actionable help text for a parameterization code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The help text, or <see langword="null"/> for an unknown code.</returns>
    public static string? HelpFor(string code) => code switch
    {
        InvalidEnergy => "Set the energy to a finite value within the accepted -100..100 eV range.",
        InvalidFrequency => "Set the attempt frequency to a positive, finite value.",
        DuplicateName => "Give each parameterization template a distinct name.",
        LimitExceeded => "Reduce the template size or split the work into smaller templates.",
        InvalidKey => "Use lowercase 64-character hexadecimal stable keys and remove duplicate value keys.",
        _ => null,
    };
}
