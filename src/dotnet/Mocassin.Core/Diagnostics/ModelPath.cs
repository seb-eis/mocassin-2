// SPDX-License-Identifier: MIT

using System.Globalization;

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Identifies a project object or one of its properties in a diagnostic.
/// </summary>
/// <param name="Domain">The project domain, for example <c>project</c>, <c>particle</c>, or <c>buildTemplate</c>.</param>
/// <param name="ObjectId">The UUID of the addressed object.</param>
/// <param name="Property">The optional property name within the object.</param>
public sealed record ModelPath(string Domain, Guid ObjectId, string? Property = null)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var id = ObjectId.ToString("D", CultureInfo.InvariantCulture);
        return string.IsNullOrEmpty(Property)
            ? string.Create(CultureInfo.InvariantCulture, $"{Domain}/{id}")
            : string.Create(CultureInfo.InvariantCulture, $"{Domain}/{id}/{Property}");
    }
}
