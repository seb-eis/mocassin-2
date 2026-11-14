// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
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
/// A diff is produced by <c>Apply</c> when <c>PRM001</c> reports a signature mismatch and by the explicit
/// <c>Diff</c> service. It never copies values; rebase is a separate, explicit operation.
/// </remarks>
public sealed record ParameterizationDiff(
    string TemplateModelSignature,
    string ModelSignature,
    ImmutableArray<ParameterizationDiffEntry> Added,
    ImmutableArray<ParameterizationDiffEntry> Removed,
    ImmutableArray<ParameterizationDiffEntry> Matched,
    ImmutableArray<ParameterizationGeometryChange> GeometryChanges);

/// <summary>
/// Selects how a stale template is rebased onto the current compiled model.
/// </summary>
public enum ParameterizationRebaseStrategy
{
    /// <summary>Copy only exact stable-identity matches; every other slot defaults.</summary>
    KeepMatching = 0,

    /// <summary>
    /// Copy exact stable-identity matches and unique pair descriptor matches within the configured distance
    /// tolerance. Ambiguous matches copy nothing.
    /// </summary>
    NearestGeometry = 1,

    /// <summary>Discard every value and keep only the template identity and the current set headers.</summary>
    Reset = 2,
}

/// <summary>
/// Controls a parameterization rebase.
/// </summary>
public sealed record ParameterizationRebaseOptions
{
    /// <summary>
    /// Gets the rebase strategy. The default is <see cref="ParameterizationRebaseStrategy.KeepMatching"/>.
    /// </summary>
    public ParameterizationRebaseStrategy Strategy { get; init; } = ParameterizationRebaseStrategy.KeepMatching;

    /// <summary>
    /// Gets the maximum accepted difference between the source and target distance buckets, in femtometres.
    /// The template stores the 1 fm bucket, so the default of 100 fm is 0.001 Å.
    /// </summary>
    public int NearestDistanceToleranceFm { get; init; } = 100;

    /// <summary>
    /// Gets the default rebase options.
    /// </summary>
    public static ParameterizationRebaseOptions Default { get; } = new();
}

/// <summary>
/// Reports one nearest-geometry pair value that a rebase copied between different stable identities.
/// </summary>
/// <param name="RemovedKey">The stale template key that supplied the value.</param>
/// <param name="AddedKey">The current compiled key that received the value.</param>
/// <param name="Value">The copied energy in electronvolt.</param>
/// <param name="SourceDistanceBucketFm">The source pair distance bucket in femtometres.</param>
/// <param name="TargetDistanceBucketFm">The target pair distance bucket in femtometres.</param>
public sealed record ParameterizationDescriptorMatch(
    ParameterizationValueKey RemovedKey,
    ParameterizationValueKey AddedKey,
    double Value,
    int SourceDistanceBucketFm,
    int TargetDistanceBucketFm);

/// <summary>
/// Summarizes one rebase operation for the graphical editor and CLI.
/// </summary>
/// <param name="Strategy">The strategy that produced the rebased template.</param>
/// <param name="Diff">The comparison of the input template with the current compiled model.</param>
/// <param name="CopiedValues">Explicit values copied by exact identity or by a unique nearest descriptor.</param>
/// <param name="DefaultedValues">Compiled required slots that the rebased template leaves unset.</param>
/// <param name="DroppedValues">Template entries discarded without a value copy.</param>
/// <param name="AmbiguousMatches">Nearest pairs with more than one candidate; nothing was copied for them.</param>
/// <param name="DescriptorMatches">The number of unique nearest-descriptor copies.</param>
public sealed record ParameterizationRebaseReport(
    ParameterizationRebaseStrategy Strategy,
    ParameterizationDiff Diff,
    int CopiedValues,
    int DefaultedValues,
    int DroppedValues,
    int AmbiguousMatches,
    int DescriptorMatches)
{
    /// <summary>
    /// Gets every unique nearest-descriptor copy, in stable key order.
    /// </summary>
    /// <remarks>
    /// The list is the detail behind <see cref="DescriptorMatches"/>; it lets a caller show exactly which stale
    /// value was reused and where. The list is empty for <see cref="ParameterizationRebaseStrategy.Reset"/> and
    /// for strategies that copied nothing by descriptor.
    /// </remarks>
    public ImmutableArray<ParameterizationDescriptorMatch> DescriptorMatchDetails { get; init; } = [];
}

/// <summary>
/// The rebased template, its report, and the <c>PRM009</c> informational diagnostic.
/// </summary>
/// <param name="Template">The new immutable template; the input template is not mutated.</param>
/// <param name="Report">The strategy, diff, and value counts.</param>
/// <param name="Diagnostics">The rebase report diagnostic.</param>
public sealed record ParameterizationRebaseResult(
    ParameterizationTemplate Template,
    ParameterizationRebaseReport Report,
    ImmutableArray<Diagnostic> Diagnostics);
