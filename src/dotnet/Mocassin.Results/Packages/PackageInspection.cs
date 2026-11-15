// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Results.Packages;

/// <summary>
/// The read-only projection of one <c>.msl</c> model row.
/// </summary>
/// <param name="Signature">The Step 09 parameterization signature and <c>models.signature</c> key.</param>
/// <param name="ModelSignature">The Step 08 compiled-model signature carried by the payload.</param>
/// <param name="PayloadFormatVersion">The payload encoding version.</param>
/// <param name="PayloadBytes">The stored compressed payload size.</param>
public sealed record PackageModelInfo(
    string Signature,
    string ModelSignature,
    int PayloadFormatVersion,
    long PayloadBytes);

/// <summary>
/// The read-only projection of one collection/config group of jobs.
/// </summary>
/// <param name="Kind">The simulation kind.</param>
/// <param name="CollectionName">The authored collection name.</param>
/// <param name="ConfigName">The authored configuration name.</param>
/// <param name="JobCount">The number of jobs in the group.</param>
public sealed record PackageCollectionInfo(
    string Kind,
    string CollectionName,
    string ConfigName,
    int JobCount);

/// <summary>
/// The read-only inspection result of a published <c>.msl</c> package.
/// </summary>
/// <param name="FormatVersion">The package format version.</param>
/// <param name="AppVersion">The build app version (metadata, excluded from the canonical summary).</param>
/// <param name="CreatedUtc">The build timestamp (metadata, excluded from the canonical summary).</param>
/// <param name="ProjectId">The authored project identifier.</param>
/// <param name="Generator">The build generator string (metadata).</param>
/// <param name="JobSelection">The job-selection expression.</param>
/// <param name="SymmetryContractVersion">The compiled symmetry contract version.</param>
/// <param name="SpaceGroupDataVersion">The compiled space-group catalog data version.</param>
/// <param name="SpaceGroupDataSha256">The compiled space-group catalog SHA-256.</param>
/// <param name="Models">The model rows sorted by signature.</param>
/// <param name="LatticeCount">The number of distinct lattice rows.</param>
/// <param name="JobCount">The number of job rows.</param>
/// <param name="Collections">The collection/config groups sorted by names.</param>
/// <param name="MinTemperatureK">The minimum job temperature.</param>
/// <param name="MaxTemperatureK">The maximum job temperature.</param>
/// <param name="MinElectricFieldVm">The minimum job electric-field modulus.</param>
/// <param name="MaxElectricFieldVm">The maximum job electric-field modulus.</param>
/// <param name="SizeBytes">The package size in bytes.</param>
/// <param name="LatticeReuseFactor">The job count divided by the distinct lattice count.</param>
public sealed record PackageInspection(
    int FormatVersion,
    string AppVersion,
    DateTimeOffset CreatedUtc,
    Guid ProjectId,
    string Generator,
    string JobSelection,
    string SymmetryContractVersion,
    string SpaceGroupDataVersion,
    string SpaceGroupDataSha256,
    ImmutableArray<PackageModelInfo> Models,
    int LatticeCount,
    int JobCount,
    ImmutableArray<PackageCollectionInfo> Collections,
    double MinTemperatureK,
    double MaxTemperatureK,
    double MinElectricFieldVm,
    double MaxElectricFieldVm,
    long SizeBytes,
    double LatticeReuseFactor);

/// <summary>
/// One named verification check.
/// </summary>
/// <param name="Code">The primary <c>PKG</c> code or check code.</param>
/// <param name="Passed">Whether the check passed.</param>
/// <param name="Message">A short human-readable result.</param>
public sealed record PackageVerificationCheck(string Code, bool Passed, string Message);

/// <summary>
/// The report of one complete package verification.
/// </summary>
/// <param name="IsValid">Whether no error-severity diagnostic was produced.</param>
/// <param name="Checks">The ordered checks.</param>
/// <param name="Diagnostics">The <c>PKG</c> diagnostics.</param>
/// <param name="Inspection">The inspection projection.</param>
public sealed record PackageVerificationReport(
    bool IsValid,
    ImmutableArray<PackageVerificationCheck> Checks,
    ImmutableArray<Diagnostic> Diagnostics,
    PackageInspection Inspection);

/// <summary>
/// Decode and advisory bounds for package verification.
/// </summary>
/// <param name="MaxModelPayloadBytes">The maximum decompressed model-payload size.</param>
/// <param name="MaxJobPayloadBytes">The maximum decompressed job-payload size.</param>
/// <param name="AdvisorySizeBytes">The advisory package-size limit; larger packages produce a <c>PKG009</c> warning.</param>
public sealed record PackageVerificationOptions(
    long MaxModelPayloadBytes = 512L * 1024 * 1024,
    long MaxJobPayloadBytes = 8L * 1024 * 1024,
    long AdvisorySizeBytes = 2L * 1024 * 1024 * 1024);
