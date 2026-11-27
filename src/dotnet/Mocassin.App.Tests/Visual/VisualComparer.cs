// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Absolute pixel-change budgets for a screenshot comparison.
/// </summary>
internal sealed record VisualComparisonTolerance(int MaximumChannelDelta, int MaximumChangedPixels);

/// <summary>
/// A decoded BGRA image consumed by the platform-independent comparison core.
/// </summary>
internal readonly record struct VisualImage(PixelSize Size, ReadOnlyMemory<byte> Pixels);

/// <summary>
/// Measured outcome of comparing a decoded baseline with a captured frame.
/// </summary>
internal sealed record VisualComparison(
    bool IsMatch,
    PixelSize ExpectedSize,
    PixelSize ActualSize,
    int ChangedPixels,
    int TotalPixels,
    int MaximumObservedChannelDelta,
    string? FailureReason);

/// <summary>
/// Shared deterministic pixel comparison for shell and editor screenshots.
/// </summary>
internal static class VisualComparer
{
    /// <summary>
    /// Gets the calibrated screenshot policy: channel deltas up to 8 are ignored, and no changed pixel is allowed.
    /// Two independent normalized captures of every current shell/editor baseline had zero changed pixels.
    /// </summary>
    public static VisualComparisonTolerance DefaultTolerance { get; } = new(8, 0);

    public static VisualComparison Compare(
        VisualImage expected,
        VisualImage actual,
        VisualComparisonTolerance tolerance)
    {
        ArgumentNullException.ThrowIfNull(tolerance);
        ValidateImage(expected);
        ValidateImage(actual);

        if (expected.Size != actual.Size)
        {
            return new VisualComparison(
                false,
                expected.Size,
                actual.Size,
                0,
                0,
                0,
                $"Expected {expected.Size}, but the captured frame is {actual.Size}.");
        }

        var expectedPixels = expected.Pixels.Span;
        var actualPixels = actual.Pixels.Span;
        var changedPixels = 0;
        var maximumObservedChannelDelta = 0;
        for (var offset = 0; offset < expectedPixels.Length; offset += 4)
        {
            var pixelDelta = 0;
            for (var channel = 0; channel < 4; channel++)
            {
                pixelDelta = Math.Max(
                    pixelDelta,
                    Math.Abs(expectedPixels[offset + channel] - actualPixels[offset + channel]));
            }

            maximumObservedChannelDelta = Math.Max(maximumObservedChannelDelta, pixelDelta);
            if (pixelDelta > tolerance.MaximumChannelDelta)
            {
                changedPixels++;
            }
        }

        var totalPixels = expected.Size.Width * expected.Size.Height;
        var isMatch = changedPixels <= tolerance.MaximumChangedPixels;
        return new VisualComparison(
            isMatch,
            expected.Size,
            actual.Size,
            changedPixels,
            totalPixels,
            maximumObservedChannelDelta,
            isMatch
                ? null
                : $"Changed {changedPixels} pixels; the absolute budget is {tolerance.MaximumChangedPixels}.");
    }

    public static VisualComparison CompareBaseline(
        string baselinePath,
        WriteableBitmap actual,
        VisualComparisonTolerance tolerance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baselinePath);
        ArgumentNullException.ThrowIfNull(actual);

        using var expected = new Bitmap(baselinePath);

        // Decode the actual frame through PNG exactly as the checked-in baseline was decoded. This keeps the
        // channel representation identical instead of comparing platform-specific raw WriteableBitmap bytes.
        using var stream = new MemoryStream();
        actual.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        using var actualBitmap = new Bitmap(stream);
        return Compare(ToImage(expected), ToImage(actualBitmap), tolerance);
    }

    public static void AssertBaselineMatches(
        string baselinePath,
        WriteableBitmap actual,
        VisualComparisonTolerance tolerance)
    {
        var result = CompareBaseline(baselinePath, actual, tolerance);
        if (result.IsMatch)
        {
            return;
        }

        var directory = RepositoryPaths.Combine(".tmp", "visual-baseline-failures");
        Directory.CreateDirectory(directory);
        var name = Path.GetFileNameWithoutExtension(baselinePath);
        var actualPath = Path.Combine(directory, name + ".actual.png");
        var diffPath = Path.Combine(directory, name + ".diff.png");
        SaveFailureArtifacts(baselinePath, actual, tolerance, actualPath, diffPath);
        Assert.Fail(
            $"{Path.GetFileName(baselinePath)}: {result.FailureReason} "
            + $"Maximum channel delta: {result.MaximumObservedChannelDelta}. "
            + $"Actual: {actualPath}; diff: {diffPath}");
    }

    private static void SaveFailureArtifacts(
        string baselinePath,
        WriteableBitmap actual,
        VisualComparisonTolerance tolerance,
        string actualPath,
        string diffPath)
    {
        actual.Save(actualPath, PngBitmapEncoderOptions.Default);
        using var expected = new Bitmap(baselinePath);
        using var stream = new MemoryStream();
        actual.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        using var actualBitmap = new Bitmap(stream);
        var expectedImage = ToImage(expected);
        var actualImage = ToImage(actualBitmap);
        var diffPixels = BuildDiffPixels(expectedImage, actualImage, tolerance.MaximumChannelDelta);
        using var diff = new WriteableBitmap(
            actualImage.Size,
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
        using var locked = diff.Lock();
        Marshal.Copy(diffPixels, 0, locked.Address, diffPixels.Length);
        diff.Save(diffPath, PngBitmapEncoderOptions.Default);
    }

    private static byte[] BuildDiffPixels(
        VisualImage expected,
        VisualImage actual,
        int maximumChannelDelta)
    {
        var diff = new byte[actual.Pixels.Length];
        var expectedPixels = expected.Pixels.Span;
        var actualPixels = actual.Pixels.Span;
        var sameSize = expected.Size == actual.Size;
        for (var offset = 0; offset < diff.Length; offset += 4)
        {
            var pixelDelta = sameSize ? 0 : 256;
            if (sameSize)
            {
                for (var channel = 0; channel < 4; channel++)
                {
                    pixelDelta = Math.Max(
                        pixelDelta,
                        Math.Abs(expectedPixels[offset + channel] - actualPixels[offset + channel]));
                }
            }

            if (pixelDelta > maximumChannelDelta)
            {
                diff[offset] = 255;
                diff[offset + 1] = 0;
                diff[offset + 2] = 255;
                diff[offset + 3] = 255;
            }
            else
            {
                actualPixels.Slice(offset, 4).CopyTo(diff.AsSpan(offset, 4));
            }
        }

        return diff;
    }

    private static VisualImage ToImage(Bitmap bitmap)
    {
        var stride = bitmap.PixelSize.Width * 4;
        return new VisualImage(bitmap.PixelSize, CopyPixels(bitmap, stride));
    }

    private static void ValidateImage(VisualImage image)
    {
        var expectedLength = image.Size.Width * image.Size.Height * 4;
        if (image.Pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                $"A {image.Size} BGRA image requires {expectedLength} bytes, but received {image.Pixels.Length}.");
        }
    }

    private static byte[] CopyPixels(Bitmap bitmap, int stride)
    {
        var size = stride * bitmap.PixelSize.Height;
        var pointer = Marshal.AllocHGlobal(size);
        try
        {
            bitmap.CopyPixels(
                new PixelRect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height),
                pointer,
                size,
                stride);
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
