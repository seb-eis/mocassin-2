// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;

namespace Mocassin.App.Tests.Navigation;

/// <summary>
/// Verifies the bounded navigation history.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class NavigationHistoryTests
{
    [Fact]
    public void TheHistoryRetainsAtMostOneHundredEntries()
    {
        var history = new NavigationHistory();
        for (var index = 0; index < NavigationHistory.MaxEntries + 20; index++)
        {
            history.Push(Entry(index));
        }

        Assert.Equal(NavigationHistory.MaxEntries, history.Entries.Count);
    }

    [Fact]
    public void BackAndForwardMoveOverTheRetainedEntries()
    {
        var history = new NavigationHistory();
        var first = Entry(0);
        var second = Entry(1);
        history.Push(first);
        history.Push(second);

        Assert.Equal(first, history.Back());
        Assert.Equal(second, history.Forward());
        Assert.False(history.CanGoForward);
    }

    [Fact]
    public void PushingAfterBackDiscardsTheForwardStack()
    {
        var history = new NavigationHistory();
        history.Push(Entry(0));
        history.Push(Entry(1));
        history.Back();
        var third = Entry(2);

        history.Push(third);

        Assert.False(history.CanGoForward);
        Assert.Equal(third, history.Current);
    }

    [Fact]
    public void DuplicateConsecutiveEntriesAreIgnored()
    {
        var history = new NavigationHistory();
        var entry = Entry(0);
        history.Push(entry);
        history.Push(entry);

        Assert.Single(history.Entries);
    }

    private static NavigationEntry Entry(int index) =>
        new(
            Guid.NewGuid(),
            new DocumentKey(DocumentKinds.ProjectOverview, Guid.NewGuid()),
            null,
            NavigationIntent.Activate);
}
