// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Structure;

namespace Mocassin.App.Tests.Editors.StructureEditors;

/// <summary>
/// Pins the debounced structure-preview scheduler with a fake clock: rapid edits coalesce into one computation
/// and a superseded/disposed schedule is discarded.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class StructurePreviewSchedulerTests
{
    [Fact]
    public void RapidSchedulesCoalesceIntoOneComputation()
    {
        var clock = new ManualTimeProvider();
        using var scheduler = new StructurePreviewScheduler(clock, new InlineUiDispatcher());
        var computes = 0;
        var applied = 0;

        for (var index = 0; index < 5; index++)
        {
            scheduler.Schedule(
                () => ++computes,
                value => applied = value);
        }

        clock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, computes);

        clock.Advance(TimeSpan.FromMilliseconds(150));
        Assert.Equal(1, computes);
        Assert.Equal(1, applied);
    }

    [Fact]
    public void ASupersededScheduleIsDiscarded()
    {
        var clock = new ManualTimeProvider();
        using var scheduler = new StructurePreviewScheduler(clock, new InlineUiDispatcher());
        var applied = 0;

        scheduler.Schedule(() => 1, value => applied = value);
        scheduler.Schedule(() => 2, value => applied = value);
        clock.Advance(TimeSpan.FromMilliseconds(250));

        Assert.Equal(2, applied);
    }

    [Fact]
    public void DisposingTheSchedulerCancelsThePendingComputation()
    {
        var clock = new ManualTimeProvider();
        var scheduler = new StructurePreviewScheduler(clock, new InlineUiDispatcher());
        var computes = 0;

        scheduler.Schedule(() => ++computes, _ => { });
        scheduler.Dispose();
        clock.Advance(TimeSpan.FromMilliseconds(250));

        Assert.Equal(0, computes);
    }
}
