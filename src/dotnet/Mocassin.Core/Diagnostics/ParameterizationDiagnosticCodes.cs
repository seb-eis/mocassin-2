// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by parameterization templates.
/// </summary>
/// <remarks>
/// <c>PRM007</c> is retired and intentionally has no constant. Chiral partners share one <c>EnergyId</c>, so an
/// independent A-to-B and B-to-A value has no representation. <c>PRM009</c> belongs to the rebase feature and is
/// not registered here.
/// </remarks>
public static class ParameterizationDiagnosticCodes
{
    /// <summary>
    /// A template model signature does not match the compiled model.
    /// </summary>
    public const string ModelSignatureMismatch = "PRM001";

    /// <summary>
    /// A template entry references an unknown stable key or disagrees with the current model descriptor.
    /// </summary>
    public const string UnknownReference = "PRM002";

    /// <summary>
    /// Required pair, group, or kinetic-rule slots are unset and defaults are applied.
    /// </summary>
    public const string RequiredValuesUnset = "PRM003";

    /// <summary>
    /// A site-energy value targets an unknown site or particle, or a particle the site cannot occupy.
    /// </summary>
    public const string InvalidSiteEnergyTarget = "PRM008";

    /// <summary>
    /// A pair or group entry names a particle that cannot occupy the referenced site.
    /// </summary>
    public const string ForbiddenParticle = "PRM012";

    /// <summary>
    /// An energy is non-finite or outside the accepted energy range.
    /// </summary>
    public const string InvalidEnergy = "PRM004";

    /// <summary>
    /// A frequency is non-positive or non-finite.
    /// </summary>
    public const string InvalidFrequency = "PRM005";

    /// <summary>
    /// An explicit template has no explicit values at all.
    /// </summary>
    public const string NoExplicitValues = "PRM006";

    /// <summary>
    /// A template contains a Metropolis transition rule set, which is not parameterizable.
    /// </summary>
    public const string MetropolisRuleSetIgnored = "PRM011";

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
    /// Suspiciously uniform non-default pair or group values suggest a bulk-fill or paste error.
    /// </summary>
    public const string UniformValues = "PRM015";

    /// <summary>
    /// An attempt frequency is outside the plausible range but still positive and finite.
    /// </summary>
    public const string ImplausibleFrequency = "PRM016";

    /// <summary>
    /// Gets the actionable help text for a parameterization code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The help text, or <see langword="null"/> for an unknown code.</returns>
    public static string? HelpFor(string code) => code switch
    {
        ModelSignatureMismatch => "Rebase or recreate the template against the current compiled model.",
        UnknownReference => "Remove the stale entry or rebase the template against the current model.",
        RequiredValuesUnset => "Set the missing values explicitly, or review the applied defaults before building.",
        InvalidSiteEnergyTarget => "Correct the site/particle key or remove the site-energy override.",
        ForbiddenParticle => "Use a particle the reference site can occupy, or remove the entry.",
        InvalidEnergy => "Set the energy to a finite value within the accepted -100..100 eV range.",
        InvalidFrequency => "Set the attempt frequency to a positive, finite value.",
        NoExplicitValues => "Set at least one value, or build with the implicit default parameterization.",
        MetropolisRuleSetIgnored => "Remove the Metropolis rule set; MMC rules are not parameterizable.",
        DuplicateName => "Give each parameterization template a distinct name.",
        LimitExceeded => "Reduce the template size or split the work into smaller templates.",
        InvalidKey => "Use lowercase 64-character hexadecimal stable keys and remove duplicate value keys.",
        UniformValues => "Check for a bulk-fill or paste error and vary the reported values deliberately.",
        ImplausibleFrequency => "Check the attempt frequency; the accepted physical range is 1e-3..1e18 Hz.",
        _ => null,
    };
}
