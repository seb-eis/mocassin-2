// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Reduction;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Default energy-geometry sampler shared by preview and compilation.
/// </summary>
public sealed class EnergyGeometrySampler : IEnergyGeometrySampler
{
    private const string geometryDomain = "mocassin/energy-geometry/v1";
    private readonly SpaceGroupCatalog catalog;

    /// <summary>
    /// Initializes a sampler.
    /// </summary>
    /// <param name="catalog">The optional catalog override. The default is <see cref="SpaceGroupCatalog.Default"/>.</param>
    public EnergyGeometrySampler(SpaceGroupCatalog? catalog = null)
    {
        this.catalog = catalog ?? SpaceGroupCatalog.Default;
    }

    /// <inheritdoc />
    public bool TrySamplePairs(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<PairInteractionPreview> pairs,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        pairs = [];
        if (model is null ||
            cellCountA <= 0 ||
            cellCountB <= 0 ||
            cellCountC <= 0)
        {
            diagnostics = [InputError("Pair sampling requires a model and positive cell counts.")];
            return false;
        }

        if (!EnergyGeometryContext.TryCreate(model, catalog, out var context, out diagnostics) ||
            context is null)
        {
            return false;
        }

        var bag = new DiagnosticBag();
        var stableEnvironment = model.StableEnvironment ?? StableEnvironment.Default;
        if (!IsUsableCutoff(stableEnvironment.Cutoff))
        {
            bag.Add(EnvironmentError(
                EnergyDiagnosticCodes.InvalidCutoff,
                "The stable environment cutoff must be positive and finite.",
                "stableEnvironment",
                Guid.Empty));
            diagnostics = bag.Sorted;
            return false;
        }

        var candidates = new List<PairCandidate>();
        AddStableCandidates(context, stableEnvironment, candidates);

        var unstableEnvironments = model.UnstableEnvironments.IsDefault ? [] : model.UnstableEnvironments;
        foreach (var environment in unstableEnvironments.OrderBy(static environment => environment.SiteId.Value))
        {
            if (!IsUsableCutoff(environment.Cutoff))
            {
                bag.Add(EnvironmentError(
                    EnergyDiagnosticCodes.InvalidCutoff,
                    "An unstable environment cutoff must be positive and finite.",
                    "unstableEnvironment",
                    environment.SiteId.Value));
                diagnostics = bag.Sorted;
                return false;
            }

            AddUnstableCandidates(context, environment, candidates);
        }

        var geometryIds = new Dictionary<(int CenterP, int ShellIndex), string>();
        foreach (var candidate in candidates)
        {
            geometryIds[(candidate.CenterP, candidate.ShellIndex)] = ComputeGeometryId(candidate);
        }

        var previews = ImmutableArray.CreateBuilder<PairInteractionPreview>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var geometryId = geometryIds[(candidate.CenterP, candidate.ShellIndex)];
            var energyId = geometryId;
            string? partnerId = null;
            var isChiral = false;
            if (candidate.ChiralPartnerIndex is { } partnerIndex &&
                geometryIds.TryGetValue((candidate.CenterP, partnerIndex), out var partnerGeometryId))
            {
                isChiral = true;
                partnerId = partnerGeometryId;
                energyId = string.CompareOrdinal(geometryId, partnerGeometryId) <= 0
                    ? geometryId
                    : partnerGeometryId;
            }

            previews.Add(new PairInteractionPreview(
                geometryId,
                energyId,
                candidate.Center.Id,
                candidate.CenterP,
                candidate.Partner.Id,
                candidate.Shell.Representative.P,
                candidate.Shell.DistanceAngstrom,
                isChiral,
                partnerId));
        }

        pairs =
        [
            .. previews
                .OrderBy(static pair => pair.SiteAId.Value)
                .ThenBy(static pair => pair.CenterP)
                .ThenBy(static pair => pair.SiteBId.Value)
                .ThenBy(static pair => pair.PartnerP)
                .ThenBy(static pair => pair.DistanceAngstrom)
                .ThenBy(static pair => pair.GeometryId, StringComparer.Ordinal),
        ];
        diagnostics = bag.Sorted;
        return true;
    }

    private static void AddStableCandidates(
        EnergyGeometryContext context,
        StableEnvironment environment,
        List<PairCandidate> candidates)
    {
        for (var referenceIndex = 0; referenceIndex < context.Sites.Length; referenceIndex++)
        {
            var center = context.Sites[referenceIndex];
            if (center.Stability != SiteStability.Stable)
            {
                continue;
            }

            foreach (var centerP in context.GetSitePIndices(center.Id))
            {
                var result = PairReduction.Reduce(context.Geometry, centerP, environment.Cutoff.Value);
                for (var shellIndex = 0; shellIndex < result.Shells.Length; shellIndex++)
                {
                    var shell = result.Shells[shellIndex];
                    var partner = context.GetSiteAt(shell.Representative.P);
                    if (partner.Stability != SiteStability.Stable ||
                        EnergyGeometryContext.IsFiltered(
                            environment.Filters,
                            center,
                            partner,
                            shell.DistanceAngstrom,
                            symmetric: true))
                    {
                        continue;
                    }

                    candidates.Add(new PairCandidate(
                        center,
                        partner,
                        centerP,
                        shellIndex,
                        shell,
                        shell.ChiralPartnerIndex));
                }
            }
        }
    }

    private static void AddUnstableCandidates(
        EnergyGeometryContext context,
        UnstableEnvironment environment,
        List<PairCandidate> candidates)
    {
        if (!context.TryGetSite(environment.SiteId, out var center) ||
            center.Stability != SiteStability.Unstable)
        {
            return;
        }

        foreach (var centerP in context.GetSitePIndices(center.Id))
        {
            var result = PairReduction.Reduce(context.Geometry, centerP, environment.Cutoff.Value);
            for (var shellIndex = 0; shellIndex < result.Shells.Length; shellIndex++)
            {
                var shell = result.Shells[shellIndex];
                var partner = context.GetSiteAt(shell.Representative.P);
                if (partner.Stability != SiteStability.Stable ||
                    EnergyGeometryContext.IsFiltered(
                        environment.Filters,
                        center,
                        partner,
                        shell.DistanceAngstrom,
                        symmetric: false))
                {
                    continue;
                }

                candidates.Add(new PairCandidate(
                    center,
                    partner,
                    centerP,
                    shellIndex,
                    shell,
                    ChiralPartnerIndex: null));
            }
        }
    }

    private static string ComputeGeometryId(PairCandidate candidate)
    {
        var offset = candidate.Shell.Representative;
        var projection = string.Create(
            CultureInfo.InvariantCulture,
            $"{geometryDomain}|{candidate.Center.Id:D}|{candidate.CenterP}|" +
            $"{candidate.Partner.Id:D}|{offset.P}|{offset.A}|{offset.B}|{offset.C}|" +
            $"{candidate.Shell.DistanceBucketFm}");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(projection)));
    }

    private static bool IsUsableCutoff(Core.Units.LengthAngstrom cutoff) =>
        double.IsFinite(cutoff.Value) && cutoff.Value > 0.0;

    private static Diagnostic InputError(string message) =>
        new(
            ProjectDiagnosticCodes.InvalidDocument,
            DiagnosticSeverity.Error,
            message,
            new ModelPath("model", Guid.Empty));

    private static Diagnostic EnvironmentError(string code, string message, string domain, Guid objectId) =>
        new(
            code,
            DiagnosticSeverity.Error,
            message,
            new ModelPath(domain, objectId, "cutoff"),
            EnergyDiagnosticCodes.HelpFor(code));

    private sealed record PairCandidate(
        CellSite Center,
        CellSite Partner,
        int CenterP,
        int ShellIndex,
        PairShell Shell,
        int? ChiralPartnerIndex);
}
