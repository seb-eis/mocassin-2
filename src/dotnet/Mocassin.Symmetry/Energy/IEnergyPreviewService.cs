// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Creates cached energy cost and pair-geometry previews for a model.
/// </summary>
public interface IEnergyPreviewService
{
    /// <summary>
    /// Creates the advisory cost preview for a requested supercell.
    /// </summary>
    /// <param name="model">The model to preview.</param>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <param name="diagnostics">The structured diagnostics on failure.</param>
    /// <returns>The preview, or <see langword="null"/> when the model cannot be previewed.</returns>
    public EnergyCostPreview? TryCreatePreview(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);

    /// <summary>
    /// Creates the advisory directed pair-geometry preview for a requested supercell.
    /// </summary>
    /// <param name="model">The model to preview.</param>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <param name="diagnostics">The structured diagnostics on failure.</param>
    /// <returns>The pair previews, or <see langword="null"/> when the model cannot be previewed.</returns>
    public ImmutableArray<PairInteractionPreview>? TryCreatePairPreview(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);
}
