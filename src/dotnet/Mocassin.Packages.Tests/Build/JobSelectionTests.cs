// SPDX-License-Identifier: MIT

using Mocassin.Packages.Build;

namespace Mocassin.Packages.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class JobSelectionTests
{
    [Theory]
    [InlineData("all", 6, "all", "1,2,3,4,5,6")]
    [InlineData("1,2,3", 6, "1-3", "1,2,3")]
    [InlineData("1-3", 6, "1-3", "1,2,3")]
    [InlineData("5,2,2", 6, "2,5", "2,5")]
    [InlineData("1-2,3-4", 6, "1-4", "1,2,3,4")]
    [InlineData("1,2,3,4,5,6", 6, "all", "1,2,3,4,5,6")]
    [InlineData("1-6", 6, "all", "1,2,3,4,5,6")]
    [InlineData(" 1 , 3 ", 6, "1,3", "1,3")]
    [InlineData("2,4,6", 6, "2,4,6", "2,4,6")]
    public void ParsesAndNormalizes(string expression, int expanded, string normal, string ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var result = JobSelection.Parse(expression, expanded);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal(normal, result.Expression);
        Assert.Equal(ids.Split(',').Select(int.Parse), result.JobIds);
        Assert.Equal(expanded, result.ExpandedCount);
        Assert.Equal(result.JobIds.Length, result.SelectedCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1-")]
    [InlineData("-2")]
    [InlineData("3-1")]
    [InlineData("1-2-3")]
    [InlineData("1,,2")]
    [InlineData("1,  ,2")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("+1")]
    [InlineData("7")]
    [InlineData("1-7")]
    [InlineData("ALL")]
    public void RejectsMalformedEmptyOrOutOfRangeSelections(string expression)
    {
        var result = JobSelection.Parse(expression, expandedCount: 6);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Empty(result.JobIds);
    }

    [Fact]
    public void RejectsASelectionAgainstAnEmptyExpansion()
    {
        var result = JobSelection.Parse("all", expandedCount: 0);

        Assert.False(result.IsValid);
        Assert.Contains("no jobs", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizeMergesAdjacentAndOverlappingRanges()
    {
        Assert.Equal("1-3,5,7-9", JobSelection.Normalize([1, 2, 3, 5, 7, 8, 9]));
        Assert.Equal("1,3", JobSelection.Normalize([1, 3]));
        Assert.Equal(string.Empty, JobSelection.Normalize([]));
    }
}
