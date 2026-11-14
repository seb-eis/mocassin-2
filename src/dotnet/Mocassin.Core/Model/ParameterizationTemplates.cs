// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model;

/// <summary>
/// A versioned set of numerical values that parameterizes a compiled model.
/// </summary>
/// <remarks>
/// Templates are sparse: a missing pair or group value means zero electronvolt, a missing rule frequency
/// means the compiled default, and a missing site-energy override means the authored model background.
/// Canonical ordering and signed-zero normalization are applied by the project serializer at write time, so
/// record copies made through <c>with</c> cannot bypass them.
/// </remarks>
public sealed record ParameterizationTemplate
{
    /// <summary>
    /// Initializes a parameterization template.
    /// </summary>
    /// <param name="id">The stable template identifier.</param>
    /// <param name="name">The user-visible template name.</param>
    /// <param name="modelSignature">The compiled-model signature this template targets.</param>
    /// <param name="createdUtc">The UTC creation timestamp.</param>
    /// <param name="stablePairSets">Stable-site pair-energy sets.</param>
    /// <param name="unstablePairSets">Unstable-site pair-energy sets.</param>
    /// <param name="groupSets">Cluster/group-energy sets.</param>
    /// <param name="siteEnergyOverrides">Site-energy overrides.</param>
    /// <param name="transitionRuleSets">Kinetic transition rule-frequency sets.</param>
    [JsonConstructor]
    public ParameterizationTemplate(
        ParameterizationId id,
        string name,
        string modelSignature,
        DateTimeOffset createdUtc,
        ImmutableArray<PairEnergySet> stablePairSets = default,
        ImmutableArray<PairEnergySet> unstablePairSets = default,
        ImmutableArray<GroupEnergySet> groupSets = default,
        ImmutableArray<SiteEnergyOverride> siteEnergyOverrides = default,
        ImmutableArray<TransitionRuleSet> transitionRuleSets = default)
    {
        Id = id;
        Name = name;
        ModelSignature = modelSignature;
        CreatedUtc = createdUtc;
        StablePairSets = stablePairSets.IsDefault ? [] : stablePairSets;
        UnstablePairSets = unstablePairSets.IsDefault ? [] : unstablePairSets;
        GroupSets = groupSets.IsDefault ? [] : groupSets;
        SiteEnergyOverrides = siteEnergyOverrides.IsDefault ? [] : siteEnergyOverrides;
        TransitionRuleSets = transitionRuleSets.IsDefault ? [] : transitionRuleSets;
    }

    /// <summary>
    /// Gets the stable template identifier.
    /// </summary>
    public ParameterizationId Id { get; init; }

    /// <summary>
    /// Gets the user-visible template name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the compiled-model signature this template targets.
    /// </summary>
    public string ModelSignature { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    [JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))]
    public DateTimeOffset CreatedUtc { get; init; }

    /// <summary>
    /// Gets the stable-site pair-energy sets.
    /// </summary>
    public ImmutableArray<PairEnergySet> StablePairSets { get; init; }

    /// <summary>
    /// Gets the unstable-site pair-energy sets.
    /// </summary>
    public ImmutableArray<PairEnergySet> UnstablePairSets { get; init; }

    /// <summary>
    /// Gets the cluster/group-energy sets.
    /// </summary>
    public ImmutableArray<GroupEnergySet> GroupSets { get; init; }

    /// <summary>
    /// Gets the site-energy overrides.
    /// </summary>
    public ImmutableArray<SiteEnergyOverride> SiteEnergyOverrides { get; init; }

    /// <summary>
    /// Gets the kinetic transition rule-frequency sets.
    /// </summary>
    public ImmutableArray<TransitionRuleSet> TransitionRuleSets { get; init; }
}

/// <summary>
/// One shared pair-energy table. The stable key is <see cref="EnergyId"/>; the site and distance fields are
/// display/rebase descriptors.
/// </summary>
public sealed record PairEnergySet
{
    /// <summary>
    /// Initializes a pair-energy set.
    /// </summary>
    /// <param name="energyId">The shared pair-energy identity.</param>
    /// <param name="centerSiteId">The representative centre site.</param>
    /// <param name="partnerSiteId">The representative partner site.</param>
    /// <param name="distanceBucketFm">The representative distance bucket in femtometres.</param>
    /// <param name="isChiral">Whether the reciprocal direction is a distinct chiral partner.</param>
    /// <param name="entries">The sparse pair-energy entries.</param>
    [JsonConstructor]
    public PairEnergySet(
        string energyId,
        CellSiteId centerSiteId,
        CellSiteId partnerSiteId,
        int distanceBucketFm,
        bool isChiral,
        ImmutableArray<PairEnergyEntry> entries = default)
    {
        EnergyId = energyId;
        CenterSiteId = centerSiteId;
        PartnerSiteId = partnerSiteId;
        DistanceBucketFm = distanceBucketFm;
        IsChiral = isChiral;
        Entries = entries.IsDefault ? [] : entries;
    }

    /// <summary>
    /// Gets the shared pair-energy identity.
    /// </summary>
    public string EnergyId { get; init; }

    /// <summary>
    /// Gets the representative centre site.
    /// </summary>
    public CellSiteId CenterSiteId { get; init; }

    /// <summary>
    /// Gets the representative partner site.
    /// </summary>
    public CellSiteId PartnerSiteId { get; init; }

    /// <summary>
    /// Gets the representative distance bucket in femtometres.
    /// </summary>
    public int DistanceBucketFm { get; init; }

    /// <summary>
    /// Gets a value indicating whether the reciprocal direction is a distinct chiral partner.
    /// </summary>
    public bool IsChiral { get; init; }

    /// <summary>
    /// Gets the sparse pair-energy entries.
    /// </summary>
    public ImmutableArray<PairEnergyEntry> Entries { get; init; }
}

/// <summary>
/// One pair-energy value for a centre/partner particle combination.
/// </summary>
/// <param name="CenterParticleId">The particle in the centre site.</param>
/// <param name="PartnerParticleId">The particle in the partner site.</param>
/// <param name="Energy">The interaction energy in electronvolt.</param>
public sealed record PairEnergyEntry(
    ParticleId CenterParticleId,
    ParticleId PartnerParticleId,
    [property: JsonPropertyName("energyEv")] EnergyEv Energy);

/// <summary>
/// One shared cluster/group-energy table. The stable key is <see cref="ClusterEnergyId"/> plus the canonical
/// occupation tuple of each entry.
/// </summary>
public sealed record GroupEnergySet
{
    /// <summary>
    /// Initializes a group-energy set.
    /// </summary>
    /// <param name="clusterEnergyId">The shared cluster-energy identity.</param>
    /// <param name="clusterId">The representative authored cluster.</param>
    /// <param name="centerSiteId">The representative centre site.</param>
    /// <param name="entries">The sparse group-energy entries.</param>
    [JsonConstructor]
    public GroupEnergySet(
        string clusterEnergyId,
        ClusterId clusterId,
        CellSiteId centerSiteId,
        ImmutableArray<GroupEnergyEntry> entries = default)
    {
        ClusterEnergyId = clusterEnergyId;
        ClusterId = clusterId;
        CenterSiteId = centerSiteId;
        Entries = entries.IsDefault ? [] : entries;
    }

    /// <summary>
    /// Gets the shared cluster-energy identity.
    /// </summary>
    public string ClusterEnergyId { get; init; }

    /// <summary>
    /// Gets the representative authored cluster.
    /// </summary>
    public ClusterId ClusterId { get; init; }

    /// <summary>
    /// Gets the representative centre site.
    /// </summary>
    public CellSiteId CenterSiteId { get; init; }

    /// <summary>
    /// Gets the sparse group-energy entries.
    /// </summary>
    public ImmutableArray<GroupEnergyEntry> Entries { get; init; }
}

/// <summary>
/// One group-energy value for a canonical occupation tuple in site order.
/// </summary>
public sealed record GroupEnergyEntry
{
    /// <summary>
    /// Initializes a group-energy entry.
    /// </summary>
    /// <param name="occupationParticleIds">The occupation particles in site order, centre first.</param>
    /// <param name="energy">The interaction energy in electronvolt.</param>
    [JsonConstructor]
    public GroupEnergyEntry(
        ImmutableArray<ParticleId> occupationParticleIds = default,
        EnergyEv energy = default)
    {
        OccupationParticleIds = occupationParticleIds.IsDefault ? [] : occupationParticleIds;
        Energy = energy;
    }

    /// <summary>
    /// Gets the occupation particles in site order, centre first.
    /// </summary>
    public ImmutableArray<ParticleId> OccupationParticleIds { get; init; }

    /// <summary>
    /// Gets the interaction energy in electronvolt.
    /// </summary>
    [JsonPropertyName("energyEv")]
    public EnergyEv Energy { get; init; }
}

/// <summary>
/// One site-energy override. The stable key is the site/particle pair.
/// </summary>
/// <param name="SiteId">The reference site.</param>
/// <param name="ParticleId">The particle occupying the site.</param>
/// <param name="Energy">The site energy in electronvolt.</param>
public sealed record SiteEnergyOverride(
    CellSiteId SiteId,
    ParticleId ParticleId,
    [property: JsonPropertyName("energyEv")] EnergyEv Energy);

/// <summary>
/// One kinetic transition rule-frequency set. The stable key is the transition kind and identifier.
/// </summary>
public sealed record TransitionRuleSet
{
    /// <summary>
    /// Initializes a transition rule-frequency set.
    /// </summary>
    /// <param name="transitionKind">The transition kind.</param>
    /// <param name="transitionId">The transition identifier.</param>
    /// <param name="rules">The sparse rule-frequency entries.</param>
    [JsonConstructor]
    public TransitionRuleSet(
        TransitionKind transitionKind,
        Guid transitionId,
        ImmutableArray<RuleFrequencyEntry> rules = default)
    {
        TransitionKind = transitionKind;
        TransitionId = transitionId;
        Rules = rules.IsDefault ? [] : rules;
    }

    /// <summary>
    /// Gets the transition kind.
    /// </summary>
    public TransitionKind TransitionKind { get; init; }

    /// <summary>
    /// Gets the transition identifier.
    /// </summary>
    public Guid TransitionId { get; init; }

    /// <summary>
    /// Gets the sparse rule-frequency entries.
    /// </summary>
    public ImmutableArray<RuleFrequencyEntry> Rules { get; init; }
}

/// <summary>
/// One rule attempt frequency. The stable key is <see cref="RuleGeometryId"/>.
/// </summary>
/// <param name="RuleGeometryId">The stable rule geometry identity.</param>
/// <param name="Frequency">The absolute attempt frequency in hertz.</param>
public sealed record RuleFrequencyEntry(
    string RuleGeometryId,
    [property: JsonPropertyName("frequencyHz")] FrequencyHz Frequency);
