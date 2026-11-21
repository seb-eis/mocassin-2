// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Headless;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Renders the shell and its overlay surfaces and compares the checked-in PNG baselines on Linux.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ScreenshotTests
{
    private const string regenerateVariable = "MOCASSIN_REGENERATE_VISUAL_BASELINES";

    [Fact]
    public Task TheShellRendersLightDarkAndHighContrastBaselines() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                Capture("light", new AppSettings { Theme = ThemeSelection.Light });
                Capture("dark", new AppSettings { Theme = ThemeSelection.Dark });
                Capture(
                    "highcontrast",
                    new AppSettings
                    {
                        Theme = ThemeSelection.Dark,
                        HighContrast = true,
                        Density = Density.Compact,
                        ReduceMotion = true,
                    });
            });

    [Fact]
    public Task TheValidationAndSettingsOverlaysHaveLightAndDarkBaselines() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                Capture(
                    "light-validation",
                    new AppSettings { Theme = ThemeSelection.Light },
                    (_, shell) => shell.Console.ActiveChannel =
                        shell.Console.Channels.Single(channel => channel.Id == "problems"));
                Capture(
                    "dark-validation",
                    new AppSettings { Theme = ThemeSelection.Dark },
                    (_, shell) => shell.Console.ActiveChannel =
                        shell.Console.Channels.Single(channel => channel.Id == "problems"));
                Capture(
                    "light-settings",
                    new AppSettings { Theme = ThemeSelection.Light },
                    (_, shell) => shell.Settings.Open());
                Capture(
                    "dark-settings",
                    new AppSettings { Theme = ThemeSelection.Dark },
                    (_, shell) => shell.Settings.Open());
            });

    private static void Capture(string name, AppSettings settings, Action<ShellWindow, ShellViewModel>? configure = null)
    {
        using var workspace = new TempWorkspace();
        using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
        var window = host.Services.GetRequiredService<ShellWindow>();
        new AvaloniaThemeApplier().Apply(settings);
        window.Width = 1000;
        window.Height = 700;
        window.Show();
        HeadlessSession.Pump(window);
        try
        {
            if (configure is not null)
            {
                configure(window, Assert.IsType<ShellViewModel>(window.DataContext));
                HeadlessSession.Pump(window);
            }

            // A second pass settles theme-variant and font resources after earlier tests in the assembly.
            HeadlessSession.Pump(window);
            using var frame = window.CaptureRenderedFrame()
                ?? throw new InvalidOperationException("The headless window did not produce a frame.");
            var path = BaselinePath(name);
            if (Environment.GetEnvironmentVariable(regenerateVariable) == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                frame.Save(path, PngBitmapEncoderOptions.Default);
                return;
            }

            Assert.True(File.Exists(path), $"Missing screenshot baseline {path}. Set {regenerateVariable}=1 to create it.");
            AssertSimilar(path, frame);
        }
        finally
        {
            window.Close();
        }
    }

    private static string BaselinePath(string name) =>
        RepositoryPaths.Combine(
            "src",
            "dotnet",
            "Mocassin.App.Tests",
            "Visual",
            "Baselines",
            name + ".png");

    private static void AssertSimilar(string baselinePath, WriteableBitmap actual)
    {
        using var expected = new Bitmap(baselinePath);
        using var stream = new MemoryStream();
        actual.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        using var actualBitmap = new Bitmap(stream);
        Assert.Equal(expected.PixelSize, actualBitmap.PixelSize);
        var width = expected.PixelSize.Width;
        var height = expected.PixelSize.Height;
        var stride = width * 4;
        var expectedPixels = CopyPixels(expected, stride);
        var actualPixels = CopyPixels(actualBitmap, stride);
        var changedPixels = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * stride) + (x * 4);
                var different = Math.Abs(expectedPixels[offset] - actualPixels[offset]) > 8
                    || Math.Abs(expectedPixels[offset + 1] - actualPixels[offset + 1]) > 8
                    || Math.Abs(expectedPixels[offset + 2] - actualPixels[offset + 2]) > 8;
                if (different)
                {
                    changedPixels++;
                }
            }
        }

        var total = width * height;
        Assert.True(
            changedPixels <= total / 20,
            $"The {Path.GetFileName(baselinePath)} screenshot changed in {changedPixels} pixels (>{total / 20}).");
    }

    private static byte[] CopyPixels(Bitmap bitmap, int stride)
    {
        var size = stride * bitmap.PixelSize.Height;
        var pointer = Marshal.AllocHGlobal(size);
        try
        {
            bitmap.CopyPixels(new PixelRect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height), pointer, size, stride);
            var bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, size);
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }
}
