// SPDX-License-Identifier: MIT

using Avalonia.Input;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.Views.Platform;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Verifies the Avalonia translation of framework-neutral gestures.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class GestureTranslatorTests
{
    [Fact]
    public void PrimaryMapsToControlOnWindowsAndLinux()
    {
        var gesture = new KeyGestureDescriptor("P", GestureModifier.Primary | GestureModifier.Shift);

        var windows = GestureTranslator.ToKeyGesture(gesture, GesturePlatform.Windows);
        var linux = GestureTranslator.ToKeyGesture(gesture, GesturePlatform.Linux);

        Assert.Equal(Key.P, windows.Key);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, windows.KeyModifiers);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, linux.KeyModifiers);
    }

    [Fact]
    public void PrimaryMapsToMetaOnMacOS()
    {
        var gesture = new KeyGestureDescriptor("P", GestureModifier.Primary);

        var mac = GestureTranslator.ToKeyGesture(gesture, GesturePlatform.MacOS);

        Assert.Equal(KeyModifiers.Meta, mac.KeyModifiers);
    }

    [Fact]
    public void ArrowAndFunctionKeysAreMapped()
    {
        var left = GestureTranslator.ToKeyGesture(
            new KeyGestureDescriptor("Left", GestureModifier.Alt),
            GesturePlatform.Linux);
        var f1 = GestureTranslator.ToKeyGesture(new KeyGestureDescriptor("F1"), GesturePlatform.Linux);

        Assert.Equal(Key.Left, left.Key);
        Assert.Equal(KeyModifiers.Alt, left.KeyModifiers);
        Assert.Equal(Key.F1, f1.Key);
    }
}
