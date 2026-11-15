// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// A reusable job-template definition.
/// </summary>
/// <param name="Id">The stable template identifier.</param>
/// <param name="Name">The user-visible template name.</param>
/// <remarks>
/// Collections are optional so a template can be created incrementally; an empty or missing collection list is
/// reported by the Step 10 template validator.
/// </remarks>
public sealed record JobTemplate(JobTemplateId Id, string Name)
{
    /// <summary>
    /// Gets the collections in authored order.
    /// </summary>
    public ImmutableArray<JobCollection>? Collections { get; init; }
}
