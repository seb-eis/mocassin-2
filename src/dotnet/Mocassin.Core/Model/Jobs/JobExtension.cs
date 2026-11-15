// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Project;

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
public sealed record JobExtension(string Id, JsonElement Config)
{
    /// <summary>
    /// Produces the canonical UTF-8 JSON text of <see cref="Config"/>.
    /// </summary>
    /// <remarks>
    /// Object properties are sorted ordinally, array order is preserved, and authored number tokens are kept
    /// verbatim, so integers above 2^53 and values outside binary64 stay exact. The text is stored as the
    /// package job payload's <c>config_json</c> string.
    /// </remarks>
    /// <returns>The canonical JSON text.</returns>
    public string ToCanonicalJson() => CanonicalJson.Normalize(Config).GetRawText();
}
