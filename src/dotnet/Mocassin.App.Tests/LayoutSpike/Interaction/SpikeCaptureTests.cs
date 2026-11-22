// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Tests.LayoutSpike.Interaction;

/// <summary>
/// Renders the layout spike's capture matrix: per option, the list with a selection, the type-specific editor open,
/// and a filtered state, each in light, dark, and high contrast, plus the generated-breadth and interaction shots.
/// </summary>
/// <remarks>
/// The captures land in the ignored <c>artifacts/step-24-layout-poc</c> directory. The report commits a bounded
/// subset under <c>docs/planning/maintenance/assets/step-24-layout-poc/</c>; no visual baseline is added or changed.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class SpikeCaptureTests
{
    private static readonly (string Name, AppSettings Settings)[] themes =
    [
        ("light", new AppSettings { Theme = ThemeSelection.Light }),
        ("dark", new AppSettings { Theme = ThemeSelection.Dark }),
        (
            "highcontrast",
            new AppSettings
            {
                Theme = ThemeSelection.Dark,
                HighContrast = true,
                Density = Density.Compact,
                ReduceMotion = true,
            }),
    ];

    private static readonly string[] scenarios = ["selection", "editor", "filtered"];

    [Theory]
    [InlineData(SpikeOption.ListDetail)]
    [InlineData(SpikeOption.TreeDetail)]
    [InlineData(SpikeOption.Tabs)]
    [InlineData(SpikeOption.SplitPane)]
    [InlineData(SpikeOption.CustomTable)]
    public Task TheOptionCaptureMatrixIsWritten(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                var slug = SpikeOptionText.Slug(option);
                foreach (var (theme, settings) in themes)
                {
                    foreach (var scenario in scenarios)
                    {
                        using var host = await SpikeTestHost.OpenAsync(option, settings);
                        Prepare(host, scenario);
                        var path = Path.Combine(
                            SpikeTestHost.OutputDirectory,
                            $"{slug}-{scenario}-{theme}.png");
                        var size = host.SaveScreenshot(path);
                        Assert.True(size.Width > 1000, $"{path} has an unexpected width ({size.Width}).");
                        Assert.True(new FileInfo(path).Length > 5000, $"{path} is unexpectedly small.");
                    }
                }

                // The 1,024-row breadth and the interaction states that justify the recommendation.
                foreach (var (name, prepare, width, height) in new (string, Action<SpikeTestHost>, double, double)[]
                {
                    (
                        "breadth",
                        target =>
                        {
                            target.Workspace.ActiveDomain = target.Workspace.Domains[2];
                            target.Workspace.GeneratedBreadth = true;
                            target.Pump();
                            SpikeInteractions.SelectRow(target, "O0025");
                        },
                        1280,
                        800),
                    (
                        "narrow",
                        target =>
                        {
                            target.Workspace.ActiveDomain = target.Workspace.Domains[2];
                            SpikeInteractions.SelectRow(target, "O");
                        },
                        720,
                        640),
                })
                {
                    using var host = await SpikeTestHost.OpenAsync(
                        option,
                        themes[0].Settings,
                        width,
                        height);
                    prepare(host);
                    host.Pump();
                    var path = Path.Combine(SpikeTestHost.OutputDirectory, $"{slug}-{name}-light.png");
                    host.SaveScreenshot(path);
                    Assert.True(new FileInfo(path).Length > 5000, $"{path} is unexpectedly small.");
                }
            });

    private static void Prepare(SpikeTestHost host, string scenario)
    {
        switch (scenario)
        {
            case "selection":
                SpikeInteractions.SelectRow(host, "Ce4+");
                break;
            case "editor":
                host.Workspace.ActiveDomain = host.Workspace.Domains[5];
                host.Pump();
                SpikeInteractions.SelectRow(host, "O migration");
                break;
            case "filtered":
                host.Workspace.ActiveDomain = host.Workspace.Domains[0];
                host.Pump();
                host.Workspace.ActiveDomain.SearchText = "O2";
                host.Pump();
                SpikeInteractions.SelectRow(host, "O2-");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown capture scenario.");
        }
    }
}
