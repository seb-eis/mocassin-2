// SPDX-License-Identifier: MIT

using Avalonia;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Fast contract tests for the shared screenshot comparer.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class VisualComparerTests
{
    [Fact]
    public void IdenticalImagesMatch()
    {
        var image = new VisualImage(new PixelSize(4, 3), CreateSolidPixels(4, 3));

        var result = VisualComparer.Compare(image, image, VisualComparer.DefaultTolerance);

        Assert.True(result.IsMatch, result.FailureReason);
        Assert.Equal(0, result.ChangedPixels);
        Assert.Equal(12, result.TotalPixels);
    }

    [Fact]
    public void ADifferenceBelowTheChannelThresholdMatches()
    {
        var expectedPixels = CreateSolidPixels(4, 3);
        var actualPixels = (byte[])expectedPixels.Clone();
        actualPixels[0] += 4;
        var expected = new VisualImage(new PixelSize(4, 3), expectedPixels);
        var actual = new VisualImage(new PixelSize(4, 3), actualPixels);

        var result = VisualComparer.Compare(expected, actual, VisualComparer.DefaultTolerance);

        Assert.True(result.IsMatch, result.FailureReason);
        Assert.Equal(0, result.ChangedPixels);
        Assert.Equal(4, result.MaximumObservedChannelDelta);
    }

    [Fact]
    public void TheDefaultThresholdAllowsASmallDifference()
    {
        var expectedPixels = CreateSolidPixels(100, 100);
        var actualPixels = (byte[])expectedPixels.Clone();
        actualPixels[0] += 9;
        var expected = new VisualImage(new PixelSize(100, 100), expectedPixels);
        var actual = new VisualImage(new PixelSize(100, 100), actualPixels);

        var result = VisualComparer.Compare(expected, actual, VisualComparer.DefaultTolerance);

        Assert.True(result.IsMatch, result.FailureReason);
        Assert.Equal(1, result.ChangedPixels);
        Assert.Equal(1, result.MaximumObservedRegionChangedPixels);
    }

    [Fact]
    public void ASingleChannelDifferenceFailsAZeroBudget()
    {
        var expectedPixels = CreateSolidPixels(4, 3);
        var actualPixels = (byte[])expectedPixels.Clone();
        actualPixels[0] += 9;
        var expected = new VisualImage(new PixelSize(4, 3), expectedPixels);
        var actual = new VisualImage(new PixelSize(4, 3), actualPixels);

        var result = VisualComparer.Compare(expected, actual, new VisualComparisonTolerance(8, 0));

        Assert.False(result.IsMatch);
        Assert.Equal(1, result.ChangedPixels);
        Assert.Equal(9, result.MaximumObservedChannelDelta);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void ASizeMismatchFails()
    {
        var expected = new VisualImage(new PixelSize(4, 3), CreateSolidPixels(4, 3));
        var actual = new VisualImage(new PixelSize(5, 3), CreateSolidPixels(5, 3));

        var result = VisualComparer.Compare(expected, actual, VisualComparer.DefaultTolerance);

        Assert.False(result.IsMatch);
        Assert.Equal(expected.Size, result.ExpectedSize);
        Assert.Equal(actual.Size, result.ActualSize);
        Assert.Equal(0, result.TotalPixels);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void TheChangedPixelBudgetIsAbsoluteAndDoesNotScaleWithImageSize()
    {
        var expectedPixels = CreateSolidPixels(100, 100);
        var actualPixels = (byte[])expectedPixels.Clone();
        actualPixels[0] += 9;
        actualPixels[4] += 9;
        var expected = new VisualImage(new PixelSize(100, 100), expectedPixels);
        var actual = new VisualImage(new PixelSize(100, 100), actualPixels);

        var withinBudget = VisualComparer.Compare(expected, actual, new VisualComparisonTolerance(8, 2));
        var aboveBudget = VisualComparer.Compare(expected, actual, new VisualComparisonTolerance(8, 1));

        Assert.True(withinBudget.IsMatch, withinBudget.FailureReason);
        Assert.False(aboveBudget.IsMatch);
        Assert.Equal(2, aboveBudget.ChangedPixels);
    }

    [Fact]
    public void ARegionBudgetRejectsAConcentratedDifferenceBelowTheGlobalBudget()
    {
        var expectedPixels = CreateSolidPixels(100, 100);
        var actualPixels = (byte[])expectedPixels.Clone();
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                actualPixels[((y * 100) + x) * 4] += 9;
            }
        }

        var expected = new VisualImage(new PixelSize(100, 100), expectedPixels);
        var actual = new VisualImage(new PixelSize(100, 100), actualPixels);

        var result = VisualComparer.Compare(expected, actual, new VisualComparisonTolerance(8, 1024, 100, 64));

        Assert.False(result.IsMatch);
        Assert.Equal(256, result.ChangedPixels);
        Assert.Equal(256, result.MaximumObservedRegionChangedPixels);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void TheDefaultThresholdRejectsADifferenceAboveTheWholeImageBudget()
    {
        var expectedPixels = CreateSolidPixels(100, 100);
        var actualPixels = (byte[])expectedPixels.Clone();
        for (var y = 0; y < 33; y++)
        {
            for (var x = 0; x < 33; x++)
            {
                actualPixels[((y * 100) + x) * 4] += 9;
            }
        }

        var expected = new VisualImage(new PixelSize(100, 100), expectedPixels);
        var actual = new VisualImage(new PixelSize(100, 100), actualPixels);

        var result = VisualComparer.Compare(expected, actual, VisualComparer.DefaultTolerance);

        Assert.False(result.IsMatch);
        Assert.Equal(1089, result.ChangedPixels);
        Assert.NotNull(result.FailureReason);
    }

    private static byte[] CreateSolidPixels(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = 64;
            pixels[offset + 1] = 128;
            pixels[offset + 2] = 255;
            pixels[offset + 3] = 255;
        }

        return pixels;
    }
}
