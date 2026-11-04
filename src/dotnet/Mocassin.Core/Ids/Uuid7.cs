// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Ids;

/// <summary>
/// Creates UUID version 7 identifiers for new project objects.
/// </summary>
public static class Uuid7
{
    /// <summary>
    /// Creates a new UUIDv7 value using the current UTC time.
    /// </summary>
    /// <returns>A new sortable UUID.</returns>
    public static Guid NewGuid() => Guid.CreateVersion7();

    /// <summary>
    /// Creates a new UUIDv7 value for an explicit timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp embedded in the UUID.</param>
    /// <returns>A new sortable UUID.</returns>
    public static Guid NewGuid(DateTimeOffset timestamp) => Guid.CreateVersion7(timestamp);
}
