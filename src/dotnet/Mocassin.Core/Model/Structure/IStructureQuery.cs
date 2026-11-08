// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// Read-only access to the authored crystal structure of a project snapshot.
/// </summary>
public interface IStructureQuery
{
    /// <summary>
    /// Gets the authored crystal structure.
    /// </summary>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Structure is the accepted Step 03 domain term and interface member name.",
        Scope = "member",
        Target = "~P:Mocassin.Core.Model.Structure.IStructureQuery.Structure")]
    public StructureModel Structure { get; }

    /// <summary>
    /// Finds an authored cell site.
    /// </summary>
    /// <param name="id">The cell-site identifier.</param>
    /// <returns>The matching site, or <see langword="null"/> when the identifier is unknown.</returns>
    public CellSite? Find(CellSiteId id);
}
