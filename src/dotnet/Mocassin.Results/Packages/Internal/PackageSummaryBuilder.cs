// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Builds the <c>package-summary-v1</c> canonical JSON projection from a read package.
/// </summary>
internal static class PackageSummaryBuilder
{
    private static readonly JsonSerializerOptions options = new() { WriteIndented = true, NewLine = "\n" };

    /// <summary>Builds the canonical summary document.</summary>
    /// <exception cref="InvalidDataException">A required payload cannot be decoded.</exception>
    public static JsonDocument Build(PackageReadResult read)
    {
        if (read.Schema is not { } schema)
        {
            throw new InvalidDataException("The package schema_info cannot be read.");
        }

        var models = read.Models.OrderBy(static model => model.Signature, StringComparer.Ordinal).ToArray();
        var lattices = read.Lattices.OrderBy(static lattice => lattice.Signature, StringComparer.Ordinal).ToArray();
        var jobs = read.Jobs.OrderBy(static job => job.JobId).ToArray();
        var modelById = models.ToDictionary(static model => model.ModelId);
        var latticeById = lattices.ToDictionary(static lattice => lattice.LatticeId);

        var modelArray = new JsonArray();
        foreach (var model in models)
        {
            var payload = RequireDecoded(model.Decoded, model.DecodeError, $"model {model.Signature}");
            modelArray.Add(new JsonObject
            {
                ["signature"] = model.Signature,
                ["modelSignature"] = payload.ModelSignature,
                ["payloadFormatVersion"] = payload.FormatVersion,
                ["particleCount"] = payload.Particle.Count,
                ["positionCount"] = payload.Structure.PositionCount,
                ["siteCount"] = payload.Structure.SiteCount,
                ["environmentCount"] = payload.Structure.Environments.Length,
                ["pairTableCount"] = payload.Energy.PairTables.Length,
                ["clusterTableCount"] = payload.Energy.ClusterTables.Length,
                ["collectionCount"] = payload.Transitions.JumpCollections.Length,
                ["ruleCount"] = payload.Transitions.JumpRules.Length,
            });
        }

        var latticeArray = new JsonArray();
        foreach (var lattice in lattices)
        {
            var payload = RequireDecoded(lattice.Decoded, lattice.DecodeError, $"lattice {lattice.Signature}");
            latticeArray.Add(new JsonObject
            {
                ["signature"] = lattice.Signature,
                ["latticeSize"] = SizeArray(payload.LatticeSize),
                ["positionCount"] = payload.PositionCount,
                ["latticeLength"] = payload.Lattice.Length,
            });
        }

        var jobArray = new JsonArray();
        foreach (var job in jobs)
        {
            var payload = RequireDecoded(job.Decoded, job.DecodeError, $"job {job.JobId}");
            if (!modelById.TryGetValue(job.ModelId, out var model))
            {
                throw new InvalidDataException($"Job {job.JobId} references missing model {job.ModelId}.");
            }

            if (!latticeById.TryGetValue(job.LatticeId, out var lattice))
            {
                throw new InvalidDataException($"Job {job.JobId} references missing lattice {job.LatticeId}.");
            }

            var modelPayload = RequireDecoded(model.Decoded, model.DecodeError, $"model {model.Signature}");
            var latticePayload = RequireDecoded(lattice.Decoded, lattice.DecodeError, $"lattice {lattice.Signature}");
            jobArray.Add(new JsonObject
            {
                ["jobId"] = job.JobId,
                ["modelSignature"] = modelPayload.ModelSignature,
                ["latticeSignature"] = lattice.Signature,
                ["kind"] = job.Kind,
                ["collectionName"] = job.CollectionName,
                ["configName"] = job.ConfigName,
                ["collectionIndex"] = job.CollectionIndex,
                ["configIndex"] = job.ConfigIndex,
                ["jobIndex"] = job.JobIndex,
                ["temperatureK"] = Normalize(job.TemperatureK),
                ["electricFieldVm"] = Normalize(job.ElectricFieldVm),
                ["targetMcsp"] = job.TargetMcsp,
                ["writeOutCount"] = job.WriteOutCount,
                ["preRunMcsp"] = job.PreRunMcsp,
                ["timeLimitS"] = NullableNumber(payload.TimeLimitS),
                ["minSuccessRate"] = NullableNumber(payload.MinSuccessRate),
                ["latticeSize"] = SizeArray(latticePayload.LatticeSize),
                ["dopingInfo"] = payload.DopingInfo,
                ["flags"] = payload.Flags,
                ["seedHex"] = Convert.ToHexStringLower(job.Seed),
            });
        }

        var root = new JsonObject
        {
            ["format"] = "mocassin.package-summary",
            ["formatVersion"] = 1,
            ["schema"] = new JsonObject
            {
                ["formatVersion"] = schema.FormatVersion,
                ["projectId"] = schema.ProjectId.ToString("D"),
                ["jobSelection"] = schema.JobSelection,
                ["symmetryContractVersion"] = schema.SymmetryContractVersion,
                ["spaceGroupDataVersion"] = schema.SpaceGroupDataVersion,
                ["spaceGroupDataSha256"] = schema.SpaceGroupDataSha256,
            },
            ["models"] = modelArray,
            ["lattices"] = latticeArray,
            ["jobs"] = jobArray,
            ["counts"] = new JsonObject
            {
                ["modelCount"] = models.Length,
                ["latticeCount"] = lattices.Length,
                ["jobCount"] = jobs.Length,
                ["latticeReuseFactor"] = lattices.Length == 0 ? 0.0 : (double)jobs.Length / lattices.Length,
            },
        };

        return JsonDocument.Parse(root.ToJsonString(options));
    }

    private static T RequireDecoded<T>(T? payload, string? error, string description)
        where T : class =>
        payload ?? throw new InvalidDataException($"The {description} payload cannot be decoded: {error}");

    private static JsonArray SizeArray(System.Collections.Immutable.ImmutableArray<uint> size)
    {
        if (size.IsDefault || size.Length != 3)
        {
            throw new InvalidDataException("The lattice size must contain exactly three values.");
        }

        return new JsonArray(
            JsonValue.Create(size[0]),
            JsonValue.Create(size[1]),
            JsonValue.Create(size[2]));
    }

    private static JsonValue? NullableNumber(ulong? value) =>
        value is { } present ? JsonValue.Create(present) : null;

    private static JsonValue? NullableNumber(double? value) =>
        value is { } present ? JsonValue.Create(Normalize(present)) : null;

    private static double Normalize(double value) => value == 0.0 ? 0.0 : value;
}
