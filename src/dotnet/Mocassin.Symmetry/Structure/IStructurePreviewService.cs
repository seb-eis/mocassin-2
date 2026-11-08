// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Structure;

/// <summary>
/// Resolves space-group references and builds deterministic structure previews.
/// </summary>
public interface IStructurePreviewService
{
    /// <summary>
    /// Resolves a structured space-group reference against the embedded catalog.
    /// </summary>
    /// <param name="reference">The persisted reference.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns>The resolved setting, or <see langword="null"/> when the reference is invalid.</returns>
    public SpaceGroupSetting? TryResolve(
        SpaceGroupRef reference,
        out ImmutableArray<Diagnostic> diagnostics);

    /// <summary>
    /// Builds a structure preview for the requested supercell counts.
    /// </summary>
    /// <param name="structureModel">The authored structure.</param>
    /// <param name="cellCountA">The requested supercell count along a.</param>
    /// <param name="cellCountB">The requested supercell count along b.</param>
    /// <param name="cellCountC">The requested supercell count along c.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns>The preview, or <see langword="null"/> when the structure is invalid.</returns>
    public StructurePreview? TryCreatePreview(
        StructureModel structureModel,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);
}
