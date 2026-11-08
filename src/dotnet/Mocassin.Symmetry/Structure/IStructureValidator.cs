// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Symmetry.Structure;

/// <summary>
/// Validates the catalog-dependent rules of a structure.
/// </summary>
public interface IStructureValidator
{
    /// <summary>
    /// Validates space-group resolution, cell geometry, and P1 orbit overlap for a structure.
    /// </summary>
    /// <param name="structureModel">The structure to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public ImmutableArray<Diagnostic> Validate(StructureModel structureModel);
}
