// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Bounded back/forward history of navigation entries.
/// </summary>
internal sealed class NavigationHistory
{
    /// <summary>
    /// The maximum number of retained entries.
    /// </summary>
    public const int MaxEntries = 100;

    private readonly List<NavigationEntry> entries = [];
    private int index = -1;

    /// <summary>
    /// Gets a value indicating whether back navigation is possible.
    /// </summary>
    public bool CanGoBack => index > 0;

    /// <summary>
    /// Gets a value indicating whether forward navigation is possible.
    /// </summary>
    public bool CanGoForward => index >= 0 && index < entries.Count - 1;

    /// <summary>
    /// Gets the current entry.
    /// </summary>
    public NavigationEntry? Current => index >= 0 && index < entries.Count ? entries[index] : null;

    /// <summary>
    /// Gets the retained entries.
    /// </summary>
    public IReadOnlyList<NavigationEntry> Entries => entries;

    /// <summary>
    /// Pushes a new entry, discarding the forward stack.
    /// </summary>
    /// <param name="entry">The entry to push.</param>
    public void Push(NavigationEntry entry)
    {
        if (Current == entry)
        {
            return;
        }

        if (index < entries.Count - 1)
        {
            entries.RemoveRange(index + 1, entries.Count - index - 1);
        }

        entries.Add(entry);
        if (entries.Count > MaxEntries)
        {
            entries.RemoveAt(0);
        }

        index = entries.Count - 1;
    }

    /// <summary>
    /// Moves one entry back.
    /// </summary>
    /// <returns>The entry to activate, or <see langword="null"/>.</returns>
    public NavigationEntry? Back()
    {
        if (!CanGoBack)
        {
            return null;
        }

        index--;
        return entries[index];
    }

    /// <summary>
    /// Moves one entry forward.
    /// </summary>
    /// <returns>The entry to activate, or <see langword="null"/>.</returns>
    public NavigationEntry? Forward()
    {
        if (!CanGoForward)
        {
            return null;
        }

        index++;
        return entries[index];
    }
}
