// SPDX-License-Identifier: MIT

using Mocassin.Packages.Build;

namespace Mocassin.Cli.Build;

/// <summary>
/// Projects the library's typed <see cref="Mocassin.Packages.Build.BuildPreview"/> into the <c>build-v1</c>
/// preview payload.
/// </summary>
/// <remarks>
/// The mapping is field-for-field. It never reloads the project, recompiles the model, or calls the preview
/// service again; the pipeline already evaluated the selected configurations.
/// </remarks>
internal static class BuildPreviewMapper
{
    /// <summary>
    /// Maps one typed preview payload.
    /// </summary>
    /// <param name="preview">The library payload.</param>
    /// <returns>The report payload.</returns>
    public static BuildReportPreview Map(BuildPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new BuildReportPreview(
            preview.JobTemplateId,
            preview.JobTemplateName,
            preview.TotalJobs,
            preview.Collections.Select(static collection => new BuildReportCollection(
                    collection.Id,
                    collection.Name,
                    collection.SimulationId,
                    collection.Kind,
                    collection.UseDualDofCorrection,
                    collection.JobCount,
                    collection.ConfigCount,
                    collection.Optimizers.Select(static optimizer => new BuildReportOptimizer(
                            optimizer.SiteId,
                            optimizer.ParticleId,
                            optimizer.TargetCount,
                            optimizer.RemovedStartOptions))
                        .ToArray(),
                    collection.Configs.Select(static config => new BuildReportConfig(
                            config.Id,
                            config.Name,
                            config.JobCount,
                            config.CellA,
                            config.CellB,
                            config.CellC,
                            config.PositionCount,
                            config.TotalChargeBefore,
                            config.TotalChargeAfter,
                            config.IsChargeNeutral,
                            config.Dopings.Select(static doping => new BuildReportDoping(
                                    doping.DopingId,
                                    doping.Priority,
                                    doping.RequestedFraction,
                                    doping.AvailablePrimary,
                                    doping.RequestedPrimary,
                                    doping.AppliedPrimary,
                                    doping.AppliedDependent,
                                    doping.EffectiveFraction))
                                .ToArray()))
                        .ToArray()))
                .ToArray());
    }
}
