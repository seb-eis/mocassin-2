// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Mocassin.Core.Ids;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The realized counts of one requested doping application.
/// </summary>
/// <remarks>
/// <see cref="RequestedPrimary"/> is the truncated request <c>floor(fraction × available)</c> before a
/// compensating pair is reduced. <see cref="AppliedPrimary"/> and <see cref="AppliedDependent"/> are the
/// counts that were actually written to the lattice, so a compensated pair can apply fewer primary exchanges
/// than requested. <see cref="EffectiveFraction"/> is
/// <c>available == 0 ? 0 : appliedPrimary / available</c>. A later same-site doping can consume particles that
/// an earlier application produced; the counts describe what each application wrote, not the final population.
/// </remarks>
/// <param name="DopingId">The authored doping identifier.</param>
/// <param name="Priority">The authored application priority.</param>
/// <param name="AvailablePrimary">The source-particle count on the primary exchange site before application.</param>
/// <param name="RequestedPrimary">The truncated primary request before compensation reduction.</param>
/// <param name="AppliedPrimary">The number of primary exchanges written to the lattice.</param>
/// <param name="AppliedDependent">The number of dependent exchanges written to the lattice.</param>
/// <param name="EffectiveFraction">The applied primary count divided by the available primary count.</param>
public sealed record DopingApplication(
    DopingId DopingId,
    int Priority,
    int AvailablePrimary,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);

/// <summary>
/// The build-time doping report of one generated lattice.
/// </summary>
/// <remarks>
/// The report is build-time and preview data, not a package field; the package stores only the compact
/// <see cref="ToDopingInfo"/> summary. <see cref="TotalChargeBefore"/> is the absolute charge of the base
/// lattice before any doping and <see cref="TotalChargeAfter"/> is the absolute charge of the final lattice, in
/// elementary-charge units. <see cref="IsChargeNeutral"/> describes the absolute final lattice:
/// <c>abs(TotalChargeAfter) &lt;= ChargeNeutralityToleranceElementary × max(1, TotalPositions)</c>. A
/// non-neutral base lattice can therefore set it false without any diagnostic.
/// </remarks>
/// <param name="Applications">The applied dopings in ascending priority order.</param>
/// <param name="TotalPositions">The number of positions in the generated supercell.</param>
/// <param name="TotalChargeBefore">The total elementary charge before doping application.</param>
/// <param name="TotalChargeAfter">The total elementary charge after doping application.</param>
/// <param name="IsChargeNeutral">Whether the absolute final lattice charge is within tolerance.</param>
public sealed record DopingReport(
    ImmutableArray<DopingApplication> Applications,
    int TotalPositions,
    double TotalChargeBefore,
    double TotalChargeAfter,
    bool IsChargeNeutral)
{
    /// <summary>
    /// The charge-neutrality tolerance in elementary-charge units.
    /// </summary>
    /// <remarks>
    /// The same <c>1e-6</c> value is the original dependent-count reduction tolerance and the relative
    /// final-neutrality tolerance. It is intentionally not a scientific correction.
    /// </remarks>
    public const double ChargeNeutralityToleranceElementary = 1e-6;

    /// <summary>
    /// Formats the canonical, priority-ordered <c>DopingInfo</c> summary.
    /// </summary>
    /// <returns>
    /// The <c>v1:{dopingId}:{priority}:{requested}:{appliedPrimary}:{appliedDependent}:{effectiveFraction:R}</c>
    /// entries joined by <c>;</c>, or an empty string when no doping was requested.
    /// </returns>
    /// <remarks>
    /// The formatting is invariant-culture and uses the lowercase hyphenated GUID form for the doping
    /// identifier. Zero-applied applications keep their entry, so "no doping configured" and "doping applied
    /// zero" stay distinguishable.
    /// </remarks>
    public string ToDopingInfo()
    {
        var applications = Applications.IsDefault
            ? ImmutableArray<DopingApplication>.Empty
            : Applications;
        if (applications.Length == 0)
        {
            return string.Empty;
        }

        var ordered = applications
            .OrderBy(static application => application.Priority)
            .ThenBy(static application => application.DopingId.Value)
            .ToArray();
        var builder = new StringBuilder();
        for (var index = 0; index < ordered.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(';');
            }

            var application = ordered[index];
            builder.Append(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"v1:{application.DopingId.Value:D}:{application.Priority}:{application.RequestedPrimary}:" +
                    $"{application.AppliedPrimary}:{application.AppliedDependent}:{application.EffectiveFraction:R}"));
        }

        return builder.ToString();
    }
}
