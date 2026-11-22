// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Model.Particles;
using ProjectModel = Mocassin.Core.Project.Project;

using Mocassin.App.Tests.LayoutSpike.Data;

namespace Mocassin.App.Tests.LayoutSpike.Editing;

/// <summary>
/// Builds the type-specific editor fields for one selected row from the live project value.
/// </summary>
internal static class SpikeEditorFields
{
    /// <summary>
    /// Builds the field list.
    /// </summary>
    /// <param name="project">The owning project.</param>
    /// <param name="domain">The row's domain.</param>
    /// <param name="row">The selected row.</param>
    /// <returns>The fields in display order.</returns>
    public static IReadOnlyList<SpikeEditorField> Build(ProjectModel project, SpikeDomain domain, SpikeRow row)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(row);
        return domain switch
        {
            SpikeDomain.Particles => ParticleFields(project, row.Id),
            SpikeDomain.ParticleSets => SetFields(project, row.Id),
            SpikeDomain.Sites => SiteFields(project, row.Id),
            SpikeDomain.StateChanges => StateChangeFields(project, row.Id),
            SpikeDomain.Chains => ChainFields(project, row.Id),
            SpikeDomain.Transitions => TransitionFields(project, row.Id),
            _ => [],
        };
    }

    /// <summary>
    /// Builds the searchable particle reference choices, including the implicit void particle.
    /// </summary>
    /// <param name="project">The owning project.</param>
    /// <returns>The choices, name plus details.</returns>
    public static IReadOnlyList<SpikeReferenceOption> ParticleChoices(ProjectModel project) =>
    [
        new(
            Particle.Void.Id.Value,
            Particle.Void.Name,
            $"implicit, ∅, q=0, {(Particle.Void.IsVacancy ? "vacancy" : "species")}"),
        .. project.Particles.Select(particle => new SpikeReferenceOption(
            particle.Id.Value,
            particle.Name,
            $"{particle.Symbol}, q={FormatSigned(particle.Charge.Value)}, " +
            $"{(particle.IsVacancy ? "vacancy" : "species")}")),
    ];

    /// <summary>
    /// Builds the searchable particle-set reference choices.
    /// </summary>
    /// <param name="project">The owning project.</param>
    /// <returns>The choices, name plus members.</returns>
    public static IReadOnlyList<SpikeReferenceOption> SetChoices(ProjectModel project) =>
    [
        .. project.Model.ParticleSets.Select(set => new SpikeReferenceOption(
            set.Id.Value,
            set.Name,
            $"{set.ParticleIds.Length} members: " +
            string.Join(
                ", ",
                set.ParticleIds.Select(id => SpikeModelLookup.FindParticle(project, id.Value)?.Name ?? "?")))),
    ];

    /// <summary>
    /// Builds the searchable chain reference choices.
    /// </summary>
    /// <param name="project">The owning project.</param>
    /// <returns>The choices, name plus pattern.</returns>
    public static IReadOnlyList<SpikeReferenceOption> ChainChoices(ProjectModel project) =>
    [
        .. project.Chains.Select(chain => new SpikeReferenceOption(
            chain.Id.Value,
            chain.Name,
            $"{chain.Pattern}, {chain.GroupIds.Length} positions")),
    ];

    private static IReadOnlyList<SpikeEditorField> ParticleFields(ProjectModel project, Guid id)
    {
        var particle = SpikeModelLookup.FindParticle(project, id);
        if (particle is null || particle.IsVoid)
        {
            return [];
        }

        var name = Text("name", "Name", particle.Name);
        var symbol = Text("symbol", "Symbol", particle.Symbol);
        var charge = Text("charge", "Charge", FormatSigned(particle.Charge.Value));
        var vacancy = Choice("vacancy", "Vacancy", particle.IsVacancy ? "yes" : "no");
        vacancy.Choices.Add("yes");
        vacancy.Choices.Add("no");
        return [name, symbol, charge, vacancy];
    }

    private static IReadOnlyList<SpikeEditorField> SetFields(ProjectModel project, Guid id)
    {
        var set = SpikeModelLookup.FindSet(project, id);
        if (set is null)
        {
            return [];
        }

        var members = new SpikeEditorField("members", "Members", SpikeFieldKind.Static, isReadOnly: true)
        {
            Text = string.Join(
                ", ",
                set.ParticleIds.Select(particleId => SpikeModelLookup.FindParticle(project, particleId.Value)?.Name ?? "?")),
        };
        return [Text("name", "Name", set.Name), members];
    }

    private static IReadOnlyList<SpikeEditorField> SiteFields(ProjectModel project, Guid id)
    {
        var site = SpikeModelLookup.FindSite(project, id);
        if (site is null)
        {
            return [];
        }

        var name = Text("name", "Name", site.Name);
        var a = Text("a", "A", SpikeRowBuilder.Format(site.Fractional.A));
        var b = Text("b", "B", SpikeRowBuilder.Format(site.Fractional.B));
        var c = Text("c", "C", SpikeRowBuilder.Format(site.Fractional.C));
        var stability = Choice("stability", "Stability", site.Stability.ToString().ToLowerInvariant());
        stability.Choices.Add("stable");
        stability.Choices.Add("unstable");
        var set = new SpikeEditorField("particleSet", "Particle set", SpikeFieldKind.Reference);
        set.SetOptions(SetChoices(project), site.ParticleSetId.Value);
        return [name, a, b, c, stability, set];
    }

    private static IReadOnlyList<SpikeEditorField> StateChangeFields(ProjectModel project, Guid id)
    {
        var change = SpikeModelLookup.FindStateChange(project, id);
        if (change is null)
        {
            return [];
        }

        var donor = new SpikeEditorField("donor", "Donor", SpikeFieldKind.Reference);
        donor.SetOptions(ParticleChoices(project), change.DonorParticleId.Value);
        var acceptor = new SpikeEditorField("acceptor", "Acceptor", SpikeFieldKind.Reference);
        acceptor.SetOptions(ParticleChoices(project), change.AcceptorParticleId.Value);
        return [Text("name", "Name", change.Name), donor, acceptor];
    }

    private static IReadOnlyList<SpikeEditorField> ChainFields(ProjectModel project, Guid id)
    {
        var chain = SpikeModelLookup.FindChain(project, id);
        if (chain is null)
        {
            return [];
        }

        var pattern = new SpikeEditorField("pattern", "Pattern", SpikeFieldKind.Static, isReadOnly: true)
        {
            Text = chain.Pattern.ToString(),
        };
        var groups = new SpikeEditorField("groups", "Groups", SpikeFieldKind.Static, isReadOnly: true)
        {
            Text = $"{chain.GroupIds.Length} positions, {chain.Linkers.Length} linkers",
        };
        var reverse = Choice("reverse", "Auto reverse", chain.GeneratesReverse ? "yes" : "no");
        reverse.Choices.Add("yes");
        reverse.Choices.Add("no");
        return [Text("name", "Name", chain.Name), pattern, groups, reverse];
    }

    private static IReadOnlyList<SpikeEditorField> TransitionFields(ProjectModel project, Guid id)
    {
        var kinetic = SpikeModelLookup.FindKinetic(project, id);
        var metropolis = SpikeModelLookup.FindMetropolis(project, id);
        if (kinetic is null && metropolis is null)
        {
            return [];
        }

        var chainId = kinetic?.ChainId.Value ?? metropolis!.ChainId.Value;
        var chain = new SpikeEditorField("chain", "Chain", SpikeFieldKind.Reference);
        chain.SetOptions(ChainChoices(project), chainId);
        var details = new SpikeEditorField("details", "Details", SpikeFieldKind.Static, isReadOnly: true)
        {
            Text = kinetic is not null
                ? $"KMC, {kinetic.ReferencePath.Length} reference path points"
                : $"MMC, {SpikeModelLookup.FindSite(project, metropolis!.SiteAId.Value)?.Name} → " +
                  $"{SpikeModelLookup.FindSite(project, metropolis.SiteBId.Value)?.Name}",
        };
        return [Text("name", "Name", kinetic?.Name ?? metropolis!.Name), chain, details];
    }

    private static SpikeEditorField Text(string key, string label, string value) =>
        new(key, label, SpikeFieldKind.Text) { Text = value };

    private static SpikeEditorField Choice(string key, string label, string value) =>
        new(key, label, SpikeFieldKind.Choice) { SelectedChoice = value };

    private static string FormatSigned(double value) =>
        value > 0
            ? "+" + value.ToString("0.###", CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
}
