// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// One opaque, versioned extension configuration entry.
/// </summary>
/// <param name="Id">The stable extension identifier.</param>
/// <param name="Config">The extension configuration object.</param>
/// <remarks>
/// Step 10 owns persistence and canonical JSON ordering; the concrete extension schema is owned by the
/// extension consumer (Steps 27/28).
/// </remarks>
public sealed record JobExtension(string Id, JsonElement Config);
