// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// One immutable line in the application console.
/// </summary>
/// <param name="TimestampUtc">The UTC timestamp.</param>
/// <param name="Severity">The severity.</param>
/// <param name="Source">The source category.</param>
/// <param name="Message">The message text.</param>
internal sealed record ConsoleEntryViewModel(
    DateTimeOffset TimestampUtc,
    ConsoleSeverity Severity,
    string Source,
    string Message);
