// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.Views.Theming;

using Mocassin.App.Tests.LayoutSpike.Data;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// A headless spike window: the production application host, the production theme applier, the shared workspace, and
/// one layout option.
/// </summary>
internal sealed class SpikeTestHost : IDisposable
{
    private readonly TempWorkspace workspace;
    private readonly AppHost host;

    private SpikeTestHost(
        TempWorkspace workspace,
        AppHost host,
        SpikeWorkspaceViewModel workspaceModel,
        SpikeStage stage,
        Window window)
    {
        this.workspace = workspace;
        this.host = host;
        Workspace = workspaceModel;
        Stage = stage;
        Window = window;
    }

    /// <summary>Gets the spike workspace.</summary>
    public SpikeWorkspaceViewModel Workspace { get; }

    /// <summary>Gets the stage.</summary>
    public SpikeStage Stage { get; }

    /// <summary>Gets the headless window.</summary>
    public Window Window { get; }

    /// <summary>
    /// Opens a headless spike window on the UI thread.
    /// </summary>
    /// <param name="option">The option to build.</param>
    /// <param name="settings">The theme settings; light by default.</param>
    /// <param name="width">The window width.</param>
    /// <param name="height">The window height.</param>
    /// <returns>The open host.</returns>
    public static async Task<SpikeTestHost> OpenAsync(
        SpikeOption option,
        AppSettings? settings = null,
        double width = 1280,
        double height = 800)
    {
        var projects = await SpikeFixtures.GetAsync();
        var workspace = new TempWorkspace();
        var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
        new AvaloniaThemeApplier().Apply(settings ?? AppSettings.Default with { Theme = ThemeSelection.Light });
        var model = SpikeWorkspaceViewModel.Create(projects);
        var stage = new SpikeStage(option, model);
        var window = new Window
        {
            Title = stage.Title,
            Width = width,
            Height = height,
            Content = stage.Root,
        };
        window.Show();
        HeadlessSession.Pump(window);
        return new SpikeTestHost(workspace, host, model, stage, window);
    }

    /// <summary>
    /// Runs pending dispatcher work and forces a render pass.
    /// </summary>
    public void Pump() => HeadlessSession.Pump(Window);

    /// <summary>
    /// Captures the window frame and writes it as a PNG.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <returns>The captured frame size.</returns>
    public PixelSize SaveScreenshot(string path)
    {
        using var frame = Window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("The headless spike window did not produce a frame.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        frame.Save(path, PngBitmapEncoderOptions.Default);
        return frame.PixelSize;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Window.Close();
        host.Dispose();
        workspace.Dispose();
    }

    /// <summary>
    /// Gets the spike capture output directory. It is ignored by git; only the bounded report subset is committed.
    /// </summary>
    /// <returns>The absolute output directory.</returns>
    public static string OutputDirectory =>
        Environment.GetEnvironmentVariable("MOCASSIN_LAYOUT_POC_OUTPUT")
        ?? RepositoryPaths.Combine("artifacts", "step-24-layout-poc");
}
