// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using Mocassin.Core.Project;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// One recent-file entry.
/// </summary>
/// <param name="Path">The full project path.</param>
/// <param name="LastOpenedUtc">The UTC time the project was last opened.</param>
internal sealed record RecentFile(
    string Path,
    [property: JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))] DateTimeOffset LastOpenedUtc);
