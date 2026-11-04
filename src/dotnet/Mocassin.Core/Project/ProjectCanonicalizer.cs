// SPDX-License-Identifier: MIT

using System.Text;

namespace Mocassin.Core.Project;

/// <summary>
/// Produces the canonical JSON projection used for hashing, dirty tracking, and golden tests.
/// </summary>
public static class ProjectCanonicalizer
{
    /// <summary>
    /// Serializes the project to canonical UTF-8 JSON bytes.
    /// </summary>
    /// <param name="project">The project to serialize.</param>
    /// <returns>The canonical UTF-8 bytes without a byte-order mark.</returns>
    public static byte[] ToCanonicalUtf8(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return ProjectJson.SerializeToUtf8Bytes(project, indented: false);
    }

    /// <summary>
    /// Serializes the project to canonical JSON text.
    /// </summary>
    /// <param name="project">The project to serialize.</param>
    /// <returns>The canonical JSON text.</returns>
    public static string ToCanonicalJson(Project project) => Encoding.UTF8.GetString(ToCanonicalUtf8(project));
}
