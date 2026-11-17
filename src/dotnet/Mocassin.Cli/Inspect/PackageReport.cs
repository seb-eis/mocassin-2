// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Inspect;

/// <summary>
/// The <c>package</c> object shared by <c>inspect-v1</c> and <c>verify-v1</c>.
/// </summary>
/// <param name="FormatVersion">The package format version.</param>
/// <param name="AppVersion">The build app version.</param>
/// <param name="CreatedUtc">The build timestamp.</param>
/// <param name="ProjectId">The authored project identity.</param>
/// <param name="Generator">The build generator string.</param>
/// <param name="JobSelection">The recorded job selection.</param>
/// <param name="SymmetryContractVersion">The compiled symmetry contract version.</param>
/// <param name="SpaceGroupDataVersion">The compiled space-group catalog version.</param>
/// <param name="SpaceGroupDataSha256">The compiled space-group catalog SHA-256.</param>
/// <param name="Models">The model rows.</param>
/// <param name="LatticeCount">The distinct lattice count.</param>
/// <param name="JobCount">The job row count.</param>
/// <param name="Collections">The collection/config groups.</param>
/// <param name="MinTemperatureK">The minimum job temperature.</param>
/// <param name="MaxTemperatureK">The maximum job temperature.</param>
/// <param name="MinElectricFieldVm">The minimum electric-field modulus.</param>
/// <param name="MaxElectricFieldVm">The maximum electric-field modulus.</param>
/// <param name="SizeBytes">The package size in bytes.</param>
/// <param name="LatticeReuseFactor">The job/lattice reuse factor.</param>
internal sealed record PackageReport(
    int FormatVersion,
    string? AppVersion,
    string? CreatedUtc,
    string? ProjectId,
    string? Generator,
    string? JobSelection,
    string? SymmetryContractVersion,
    string? SpaceGroupDataVersion,
    string? SpaceGroupDataSha256,
    IReadOnlyList<PackageReportModel> Models,
    int LatticeCount,
    int JobCount,
    IReadOnlyList<PackageReportCollection> Collections,
    double MinTemperatureK,
    double MaxTemperatureK,
    double MinElectricFieldVm,
    double MaxElectricFieldVm,
    long SizeBytes,
    double LatticeReuseFactor);

/// <summary>
/// One model row in the package report.
/// </summary>
/// <param name="Signature">The parameterization signature.</param>
/// <param name="ModelSignature">The compiled-model signature.</param>
/// <param name="PayloadFormatVersion">The payload format version.</param>
/// <param name="PayloadBytes">The stored payload size.</param>
internal sealed record PackageReportModel(
    string? Signature,
    string? ModelSignature,
    int PayloadFormatVersion,
    long PayloadBytes);

/// <summary>
/// One collection/config group in the package report.
/// </summary>
/// <param name="Kind">The simulation kind.</param>
/// <param name="CollectionName">The authored collection name.</param>
/// <param name="ConfigName">The authored configuration name.</param>
/// <param name="JobCount">The group job count.</param>
internal sealed record PackageReportCollection(
    string? Kind,
    string? CollectionName,
    string? ConfigName,
    int JobCount);

/// <summary>
/// Maps the Step 11 inspection projection to the report object.
/// </summary>
internal static class PackageReportMapper
{
    /// <summary>
    /// Maps one inspection projection.
    /// </summary>
    /// <param name="inspection">The inspection.</param>
    /// <returns>The report object.</returns>
    public static PackageReport Map(PackageInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        return new PackageReport(
            inspection.FormatVersion,
            inspection.AppVersion,
            inspection.CreatedUtc.ToString("O", CultureInfo.InvariantCulture),
            inspection.ProjectId.ToString("D", CultureInfo.InvariantCulture),
            inspection.Generator,
            inspection.JobSelection,
            inspection.SymmetryContractVersion,
            inspection.SpaceGroupDataVersion,
            inspection.SpaceGroupDataSha256,
            inspection.Models
                .Select(static model => new PackageReportModel(
                    model.Signature,
                    model.ModelSignature,
                    model.PayloadFormatVersion,
                    model.PayloadBytes))
                .ToArray(),
            inspection.LatticeCount,
            inspection.JobCount,
            inspection.Collections
                .Select(static collection => new PackageReportCollection(
                    collection.Kind,
                    collection.CollectionName,
                    collection.ConfigName,
                    collection.JobCount))
                .ToArray(),
            inspection.MinTemperatureK,
            inspection.MaxTemperatureK,
            inspection.MinElectricFieldVm,
            inspection.MaxElectricFieldVm,
            inspection.SizeBytes,
            inspection.LatticeReuseFactor);
    }
}
