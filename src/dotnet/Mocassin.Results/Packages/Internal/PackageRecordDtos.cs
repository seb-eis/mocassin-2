// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Contracts.Packages;

namespace Mocassin.Results.Packages.Internal;

/// <summary>The parsed <c>schema_info</c> of a package.</summary>
internal sealed record PackageSchemaInfo(
    int FormatVersion,
    string AppVersion,
    DateTimeOffset CreatedUtc,
    Guid ProjectId,
    string Generator,
    string JobSelection,
    string SymmetryContractVersion,
    string SpaceGroupDataVersion,
    string SpaceGroupDataSha256);

/// <summary>One read model row and its decoded payload.</summary>
internal sealed record PackageModelRecord(
    long ModelId,
    string Signature,
    int FormatVersion,
    byte[] Payload,
    ModelPayload? Decoded,
    string? DecodeError)
{
    public long PayloadBytes => Payload.LongLength;
}

/// <summary>One read lattice row and its decoded payload.</summary>
internal sealed record PackageLatticeRecord(
    long LatticeId,
    string Signature,
    int FormatVersion,
    byte[] Payload,
    LatticePayload? Decoded,
    string? DecodeError)
{
    public long PayloadBytes => Payload.LongLength;
}

/// <summary>One read job row and its decoded payload.</summary>
internal sealed record PackageJobRecord(
    long JobId,
    long ModelId,
    long LatticeId,
    string Kind,
    string CollectionName,
    string ConfigName,
    long CollectionIndex,
    long ConfigIndex,
    long JobIndex,
    double TemperatureK,
    double ElectricFieldVm,
    long TargetMcsp,
    int WriteOutCount,
    long PreRunMcsp,
    long? TimeLimitS,
    double? MinSuccessRate,
    int LatticeA,
    int LatticeB,
    int LatticeC,
    string DopingInfo,
    long Flags,
    byte[] Seed,
    byte[] Payload,
    string CreatedUtc,
    JobPayload? Decoded,
    string? DecodeError);

/// <summary>The complete read result, including fatal and immutability flags.</summary>
internal sealed record PackageReadResult(
    PackageSchemaInfo? Schema,
    ImmutableArray<PackageModelRecord> Models,
    ImmutableArray<PackageLatticeRecord> Lattices,
    ImmutableArray<PackageJobRecord> Jobs,
    long SizeBytes,
    bool HasResultsTable,
    bool HasWalSidecar,
    bool IsWalJournal,
    string? FatalCode,
    string? FatalMessage)
{
    public static PackageReadResult Fatal(string code, string message) =>
        new(
            null,
            [],
            [],
            [],
            0,
            false,
            false,
            false,
            code,
            message);
}
