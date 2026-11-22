// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

using Mocassin.App.Tests.LayoutSpike.Data;
using Mocassin.App.Tests.LayoutSpike.Editing;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// The spike workspace: the real model and transition samples, their editable documents, the generated breadth
/// value, and one selection surface per domain.
/// </summary>
/// <remarks>
/// The workspace is layout-agnostic. Every option renders the same instance, so the recorded differences are the
/// selection surface's cost, not differences in the data or in the editor.
/// </remarks>
internal sealed partial class SpikeWorkspaceViewModel : ObservableObject
{
    private readonly List<SpikeDataset> datasets;
    private SpikeDomainViewModel activeDomain;
    private bool generatedBreadth;

    private SpikeWorkspaceViewModel(IEnumerable<SpikeDataset> datasets, IEnumerable<SpikeDomainViewModel> domains)
    {
        this.datasets = [.. datasets];
        Domains = [.. domains];
        activeDomain = Domains[0];
        foreach (var dataset in this.datasets.Where(dataset => dataset.IsEditable))
        {
            dataset.Rebuilt += OnDatasetRebuilt;
        }
    }

    /// <summary>Gets the domain surfaces in display order.</summary>
    public ObservableCollection<SpikeDomainViewModel> Domains { get; }

    /// <summary>Gets or sets the active domain.</summary>
    public SpikeDomainViewModel ActiveDomain
    {
        get => activeDomain;
        set
        {
            if (value is not null && SetProperty(ref activeDomain, value))
            {
                OnPropertyChanged(nameof(Editor));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>Gets the active domain's editor.</summary>
    public SpikeEditorViewModel Editor => ActiveDomain.Editor;

    /// <summary>Gets a value indicating whether generated breadth is available.</summary>
    public bool HasGeneratedBreadth => Domains.Any(domain => domain.HasGeneratedBreadth);

    /// <summary>Gets or sets a value indicating whether generated breadth rows are shown.</summary>
    public bool GeneratedBreadth
    {
        get => generatedBreadth;
        set
        {
            if (!SetProperty(ref generatedBreadth, value))
            {
                return;
            }

            foreach (var domain in Domains)
            {
                domain.SetGeneratedBreadth(value);
            }

            OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>Gets the combined status line.</summary>
    public string StatusText =>
        ActiveDomain.ActionText.Length > 0
            ? $"{ActiveDomain.StatusText} · {ActiveDomain.ActionText}"
            : $"{ActiveDomain.StatusText} · {ActiveDomain.Editor.StatusText}";

    /// <summary>
    /// Builds the spike workspace from the loaded projects.
    /// </summary>
    /// <param name="projects">The loaded projects.</param>
    /// <returns>The workspace.</returns>
    public static SpikeWorkspaceViewModel Create(SpikeProjects projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var ceria = new SpikeDataset("ceria", projects.Ceria, editable: true, SpikeRowBuilder.BuildModelTables);
        var kmc = new SpikeDataset(
            "ceria-kmc",
            projects.CeriaKmc,
            editable: true,
            project => SpikeRowBuilder.BuildTransitionTables(project, "ceria-kmc", readOnly: false));
        var mmc = new SpikeDataset(
            "ceria-mmc",
            projects.CeriaMmc,
            editable: false,
            project => SpikeRowBuilder.BuildTransitionTables(project, "ceria-mmc", readOnly: true));
        var interstitialcy = new SpikeDataset(
            "interstitialcy5",
            projects.Interstitialcy5,
            editable: false,
            project => SpikeRowBuilder.BuildTransitionTables(project, "interstitialcy5", readOnly: true));
        var vehicle = new SpikeDataset(
            "vehicle5",
            projects.Vehicle5,
            editable: false,
            project => SpikeRowBuilder.BuildTransitionTables(project, "vehicle5", readOnly: true));
        var polaron = new SpikeDataset(
            "polaron-hop",
            projects.PolaronHop,
            editable: false,
            project => SpikeRowBuilder.BuildTransitionTables(project, "polaron-hop", readOnly: true));
        var toy = new SpikeDataset("generated-1024", projects.Toy, editable: false, SpikeToyProject.BuildTables);

        var datasets = new[] { ceria, kmc, mmc, interstitialcy, vehicle, polaron, toy };
        var domains = new List<SpikeDomainViewModel>
        {
            new(SpikeDomain.Particles, ceria, [], toy.Table(SpikeDomain.Particles)),
            new(SpikeDomain.ParticleSets, ceria, [], null),
            new(SpikeDomain.Sites, ceria, [], toy.Table(SpikeDomain.Sites)),
            new(
                SpikeDomain.StateChanges,
                kmc,
                BreadthRows(mmc, interstitialcy, vehicle, polaron, SpikeDomain.StateChanges),
                null),
            new(
                SpikeDomain.Chains,
                kmc,
                BreadthRows(mmc, interstitialcy, vehicle, polaron, SpikeDomain.Chains),
                null),
            new(
                SpikeDomain.Transitions,
                kmc,
                BreadthRows(mmc, interstitialcy, vehicle, polaron, SpikeDomain.Transitions),
                null),
        };
        return new SpikeWorkspaceViewModel(datasets, domains);
    }

    private static ImmutableArray<SpikeRow> BreadthRows(
        SpikeDataset first,
        SpikeDataset second,
        SpikeDataset third,
        SpikeDataset fourth,
        SpikeDomain domain)
    {
        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        foreach (var dataset in new[] { first, second, third, fourth })
        {
            if (dataset.Table(domain) is { } table)
            {
                rows.AddRange(table.Rows);
            }
        }

        return rows.ToImmutable();
    }

    private void OnDatasetRebuilt(object? sender, EventArgs e)
    {
        foreach (var domain in Domains.Where(domain => ReferenceEquals(domain.Editable, sender)))
        {
            domain.Refresh();
        }

        OnPropertyChanged(nameof(StatusText));
    }
}
