// SPDX-License-Identifier: MIT

using System.Security.Cryptography;

namespace Mocassin.Core.Project;

/// <summary>
/// Computes SHA-256 content hashes for project values.
/// </summary>
public static class ProjectHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of the canonical model section.
    /// </summary>
    /// <param name="project">The project whose model section is hashed.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <remarks>
    /// This is the Step 01 model-content identity used for dirty tracking and for the future compiled
    /// model signature. It is deliberately distinct from <see cref="ComputeDocument"/>.
    /// </remarks>
    public static string Compute(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var bytes = ProjectJson.SerializeCanonicalModel(project.Model);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Computes the SHA-256 hash of the entire canonical project document.
    /// </summary>
    /// <param name="project">The project to hash.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    public static string ComputeDocument(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var bytes = ProjectCanonicalizer.ToCanonicalUtf8(project);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
