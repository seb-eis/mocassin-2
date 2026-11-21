// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Recovery;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.Tests.Recovery;

/// <summary>
/// Verifies the timestamp-normalized recovery comparison and the retention boundary.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class RecoveryComparisonTests
{
    [Fact]
    public void AContentChangeIsDifferent()
    {
        var saved = CreateProject();
        var snapshot = saved with { Metadata = saved.Metadata with { Name = "Changed" } };

        Assert.True(RecoveryComparison.HasContentDifference(snapshot, saved));
    }

    [Fact]
    public void ATimestampOnlyChangeIsNotDifferent()
    {
        var saved = CreateProject();
        var snapshot = saved with
        {
            Metadata = saved.Metadata with { ModifiedUtc = saved.Metadata.ModifiedUtc.AddMinutes(5) },
        };

        Assert.False(RecoveryComparison.HasContentDifference(snapshot, saved));
    }

    [Fact]
    public void RetentionIsSevenDaysAndTheBoundaryIsInclusive()
    {
        var now = new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromDays(7), RecoveryRetentionPolicy.Retention);
        Assert.False(RecoveryRetentionPolicy.IsExpired(now.AddDays(-7), now));
        Assert.True(RecoveryRetentionPolicy.IsExpired(now.AddDays(-7).AddTicks(-1), now));
    }

    private static Mocassin.Core.Project.Project CreateProject() =>
        ProjectTemplateFactory.CreateProject(
            ProjectTemplateKind.Blank,
            "Comparison",
            new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero));
}
