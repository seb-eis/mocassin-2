// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.LayoutSpike.Data;

/// <summary>
/// Builds the spike's generated breadth value: 1,024 reference sites and a padded particle list.
/// </summary>
/// <remarks>
/// This is breadth data only, per the testing strategy's "generated toy data for breadth". The value deliberately
/// exceeds <see cref="StructureModel.MaxReferenceSites"/> so the spike can prove virtualization above 1,000 rows;
/// it is display-only and is never applied through the production validator.
/// </remarks>
internal static class SpikeToyProject
{
    /// <summary>The generated site count used for the virtualization evidence.</summary>
    public const int SiteCount = 1024;

    /// <summary>The number of padded particles appended to the real ceria particle list.</summary>
    public const int PaddedParticleCount = 24;

    /// <summary>
    /// Generates the toy project from the real ceria value.
    /// </summary>
    /// <param name="ceria">The real ceria project; its metadata, sets, and cell are reused.</param>
    /// <returns>The generated breadth value.</returns>
    public static ProjectModel Create(ProjectModel ceria)
    {
        ArgumentNullException.ThrowIfNull(ceria);

        var particles = ceria.Particles.ToBuilder();
        for (var index = 0; index < PaddedParticleCount; index++)
        {
            particles.Add(
                new Particle(
                    ParticleId.Create(GeneratedId(2, index)),
                    $"Toy species {index + 1:D2}",
                    $"X{index + 1:D2}",
                    new ChargeElementary((index % 3) - 1),
                    IsVacancy: false));
        }

        var oxygenSet = ceria.Model.ParticleSets
            .FirstOrDefault(set => set.Name.Contains("Oxygen", StringComparison.OrdinalIgnoreCase));
        var particleSetId = oxygenSet?.Id ?? ceria.Model.ParticleSets[0].Id;
        var sites = ImmutableArray.CreateBuilder<CellSite>(SiteCount);
        for (var index = 0; index < SiteCount; index++)
        {
            sites.Add(
                new CellSite(
                    CellSiteId.Create(GeneratedId(1, index)),
                    $"O{index + 1:D4}",
                    new Fractional3(
                        (index % 10) / 10.0,
                        (index / 10 % 10) / 10.0,
                        (index / 100 % 10) / 10.0),
                    particleSetId,
                    index % 5 == 0 ? SiteStability.Unstable : SiteStability.Stable));
        }

        return ceria with
        {
            Model = ceria.Model with
            {
                Particles = particles.ToImmutable(),
                Structure = ceria.Structure with { Sites = sites.ToImmutable() },
            },
        };
    }

    /// <summary>
    /// Builds the display tables for the generated breadth value.
    /// </summary>
    /// <param name="toy">The generated project.</param>
    /// <returns>The particle and site tables.</returns>
    public static ImmutableArray<SpikeTable> BuildTables(ProjectModel toy) =>
    [
        Annotate(SpikeRowBuilder.ParticleTable(toy, readOnly: true)),
        Annotate(SpikeRowBuilder.SiteTable(toy, readOnly: true)),
    ];

    private static SpikeTable Annotate(SpikeTable table) => table with
    {
        Rows =
        [
            .. table.Rows.Select(row => row with
            {
                SourceLabel = row.IsPinned ? row.SourceLabel : "generated breadth",
                Explanation = row.Explanation
                    ?? "Generated breadth row: display-only. The value deliberately exceeds the 1,000 "
                    + "reference-site limit so the spike can prove virtualization above 1,000 rows.",
            }),
        ],
    };

    private static Guid GeneratedId(int family, int index) =>
        Guid.Parse(
            $"00000000-0000-0000-{family:D4}-{index + 1:D12}",
            CultureInfo.InvariantCulture);
}
