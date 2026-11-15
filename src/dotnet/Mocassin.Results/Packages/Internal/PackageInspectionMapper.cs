// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Maps a read result to the public inspection projection.
/// </summary>
internal static class PackageInspectionMapper
{
    /// <summary>The empty inspection used when a package cannot be read at all.</summary>
    public static PackageInspection Empty { get; } = new(
        0,
        string.Empty,
        default,
        Guid.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        [],
        0,
        0,
        [],
        0.0,
        0.0,
        0.0,
        0.0,
        0,
        0.0);

    /// <summary>Maps a read result to its inspection.</summary>
    public static PackageInspection Map(PackageReadResult read)
    {
        if (read.Schema is not { } schema)
        {
            return Empty;
        }

        var models = read.Models
            .Select(static model => new PackageModelInfo(
                model.Signature,
                model.Decoded?.ModelSignature ?? string.Empty,
                model.FormatVersion,
                model.PayloadBytes))
            .OrderBy(static model => model.Signature, StringComparer.Ordinal)
            .ToImmutableArray();
        var collections = read.Jobs
            .GroupBy(static job => (job.Kind, job.CollectionName, job.ConfigName))
            .Select(static group => new PackageCollectionInfo(
                group.Key.Kind,
                group.Key.CollectionName,
                group.Key.ConfigName,
                group.Count()))
            .OrderBy(static collection => collection.Kind, StringComparer.Ordinal)
            .ThenBy(static collection => collection.CollectionName, StringComparer.Ordinal)
            .ThenBy(static collection => collection.ConfigName, StringComparer.Ordinal)
            .ToImmutableArray();
        var temperatures = read.Jobs.Select(static job => job.TemperatureK).ToArray();
        var fields = read.Jobs.Select(static job => job.ElectricFieldVm).ToArray();
        var latticeCount = read.Lattices.Length;
        var jobCount = read.Jobs.Length;
        return new PackageInspection(
            schema.FormatVersion,
            schema.AppVersion,
            schema.CreatedUtc,
            schema.ProjectId,
            schema.Generator,
            schema.JobSelection,
            schema.SymmetryContractVersion,
            schema.SpaceGroupDataVersion,
            schema.SpaceGroupDataSha256,
            models,
            latticeCount,
            jobCount,
            collections,
            temperatures.Length == 0 ? 0.0 : temperatures.Min(),
            temperatures.Length == 0 ? 0.0 : temperatures.Max(),
            fields.Length == 0 ? 0.0 : fields.Min(),
            fields.Length == 0 ? 0.0 : fields.Max(),
            read.SizeBytes,
            latticeCount == 0 ? 0.0 : (double)jobCount / latticeCount);
    }
}
