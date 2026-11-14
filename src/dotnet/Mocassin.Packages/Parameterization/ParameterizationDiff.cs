// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Classifies one parameterization value slot.
/// </summary>
public enum ParameterizationValueKind
{
    /// <summary>A value in a stable-site pair table.</summary>
    StablePair = 0,

    /// <summary>A value in an unstable-site pair table.</summary>
    UnstablePair = 1,

    /// <summary>A value in a cluster/group table.</summary>
    Group = 2,

    /// <summary>An absolute attempt frequency of one compiled rule.</summary>
    RuleFrequency = 3,

    /// <summary>A site-energy override.</summary>
    SiteEnergyOverride = 4,
}

/// <summary>
/// Identifies one parameterization value slot without free-form delimiter parsing.
/// </summary>
/// <param name="Kind">The value kind.</param>
/// <param name="SetId">The pair <c>EnergyId</c>, cluster <c>ClusterEnergyId</c>, or rule-set transition identifier.</param>
/// <param name="TransitionKind">The transition kind for a rule-frequency key; otherwise <see langword="null"/>.</param>
/// <param name="TransitionId">The transition identifier for a rule-frequency key; otherwise <see langword="null"/>.</param>
/// <param name="EntryId">
/// The canonical entry key: particle IDs for a pair, comma-joined occupation particle IDs for a group, or the
/// rule geometry identifier for a rule frequency.
/// </param>
/// <param name="SiteId">The reference site for a site-energy override; otherwise <see langword="null"/>.</param>
/// <param name="ParticleId">The occupying particle for a site-energy override; otherwise <see langword="null"/>.</param>
public sealed record ParameterizationValueKey(
    ParameterizationValueKind Kind,
    string? SetId,
    TransitionKind? TransitionKind,
    Guid? TransitionId,
    string? EntryId,
    CellSiteId? SiteId,
    ParticleId? ParticleId);

/// <summary>
/// One entry in a parameterization diff.
/// </summary>
/// <param name="Key">The stable slot identity.</param>
/// <param name="TemplateValue">
/// The template value for a removed or matched entry; <see langword="null"/> for an added compiled slot. The
/// unit is electronvolt for pair/group/site kinds and hertz for <see cref="ParameterizationValueKind.RuleFrequency"/>.
/// </param>
/// <param name="Descriptor">A short model-supplied geometry or occupation description, when available.</param>
public sealed record ParameterizationDiffEntry(
    ParameterizationValueKey Key,
    double? TemplateValue,
    string? Descriptor);

/// <summary>
/// Reports one removed template key and one added compiled key that share a logical descriptor.
/// </summary>
/// <param name="Kind">The value kind of the related keys.</param>
/// <param name="RemovedKey">The template key that no longer exists.</param>
/// <param name="AddedKey">The compiled key that has no template entry.</param>
/// <param name="Reason">A short explanation of the descriptor match.</param>
/// <remarks>
/// A geometry change is display information. It does not authorize copying a value; rebase is an explicit
/// operation.
/// </remarks>
public sealed record ParameterizationGeometryChange(
    ParameterizationValueKind Kind,
    ParameterizationValueKey RemovedKey,
    ParameterizationValueKey AddedKey,
    string Reason);

/// <summary>
/// Compares a template against a compiled model by stable identity.
/// </summary>
/// <param name="TemplateModelSignature">The model signature stored in the template.</param>
/// <param name="ModelSignature">The current compiled-model signature.</param>
/// <param name="Added">Compiled required slots with no matched template entry; applying would default them.</param>
/// <param name="Removed">Template entries with no compiled slot; applying would ignore them.</param>
/// <param name="Matched">Template entries whose stable key and descriptor agree with the compiled model.</param>
/// <param name="GeometryChanges">Removed/added pair or group keys related by their logical descriptor.</param>
/// <remarks>
/// The diff is produced by <c>Apply</c> when <c>PRM001</c> reports a signature mismatch. The full diff/rebase
/// service surface belongs to a later Step 09 feature and builds on the same comparison.
/// </remarks>
public sealed record ParameterizationDiff(
    string TemplateModelSignature,
    string ModelSignature,
    ImmutableArray<ParameterizationDiffEntry> Added,
    ImmutableArray<ParameterizationDiffEntry> Removed,
    ImmutableArray<ParameterizationDiffEntry> Matched,
    ImmutableArray<ParameterizationGeometryChange> GeometryChanges);
