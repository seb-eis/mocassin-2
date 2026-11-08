// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Ids;

/// <summary>
/// Non-generic view over a strongly typed project identifier.
/// </summary>
public interface ITypedId
{
    /// <summary>
    /// Gets the underlying UUID value.
    /// </summary>
    public Guid Value { get; }
}

/// <summary>
/// Describes a strongly typed UUID identifier that can be constructed from a raw <see cref="Guid"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete identifier type.</typeparam>
public interface ITypedId<TSelf> : ITypedId
    where TSelf : struct, ITypedId<TSelf>
{
    /// <summary>
    /// Creates the identifier from a raw UUID value.
    /// </summary>
    /// <param name="value">The UUID value.</param>
    /// <returns>The typed identifier.</returns>
    public static abstract TSelf Create(Guid value);
}

/// <summary>
/// Stable identifier for a particle definition.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<ParticleId>))]
public readonly record struct ParticleId(Guid Value) : ITypedId<ParticleId>
{
    /// <summary>
    /// Gets the reserved identifier of the implicit void particle.
    /// </summary>
    public static ParticleId Void { get; } = Create(Guid.Parse("00000000-0000-0000-0000-000000000001", CultureInfo.InvariantCulture));

    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static ParticleId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static ParticleId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a particle set.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<ParticleSetId>))]
public readonly record struct ParticleSetId(Guid Value) : ITypedId<ParticleSetId>
{
    /// <summary>
    /// Gets the reserved identifier of the implicit empty particle set.
    /// </summary>
    public static ParticleSetId Empty { get; } = Create(Guid.Parse("00000000-0000-0000-0000-000000000002", CultureInfo.InvariantCulture));

    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static ParticleSetId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static ParticleSetId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a cell site.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<CellSiteId>))]
public readonly record struct CellSiteId(Guid Value) : ITypedId<CellSiteId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static CellSiteId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static CellSiteId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a cell dummy position.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<CellDummyPositionId>))]
public readonly record struct CellDummyPositionId(Guid Value) : ITypedId<CellDummyPositionId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static CellDummyPositionId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static CellDummyPositionId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a state change.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<StateChangeId>))]
public readonly record struct StateChangeId(Guid Value) : ITypedId<StateChangeId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static StateChangeId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static StateChangeId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a state-change group.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<StateChangeGroupId>))]
public readonly record struct StateChangeGroupId(Guid Value) : ITypedId<StateChangeGroupId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static StateChangeGroupId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static StateChangeGroupId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a transition chain.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<TransitionChainId>))]
public readonly record struct TransitionChainId(Guid Value) : ITypedId<TransitionChainId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static TransitionChainId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static TransitionChainId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a kinetic transition.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<KineticTransitionId>))]
public readonly record struct KineticTransitionId(Guid Value) : ITypedId<KineticTransitionId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static KineticTransitionId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static KineticTransitionId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a Metropolis transition.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<MetropolisTransitionId>))]
public readonly record struct MetropolisTransitionId(Guid Value) : ITypedId<MetropolisTransitionId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static MetropolisTransitionId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static MetropolisTransitionId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for an interaction cluster.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<ClusterId>))]
public readonly record struct ClusterId(Guid Value) : ITypedId<ClusterId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static ClusterId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static ClusterId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for an occupation exchange.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<OccupationExchangeId>))]
public readonly record struct OccupationExchangeId(Guid Value) : ITypedId<OccupationExchangeId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static OccupationExchangeId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static OccupationExchangeId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a doping definition.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<DopingId>))]
public readonly record struct DopingId(Guid Value) : ITypedId<DopingId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static DopingId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static DopingId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a simulation definition.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<SimulationId>))]
public readonly record struct SimulationId(Guid Value) : ITypedId<SimulationId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static SimulationId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static SimulationId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a parameterization template.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<ParameterizationId>))]
public readonly record struct ParameterizationId(Guid Value) : ITypedId<ParameterizationId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static ParameterizationId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static ParameterizationId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a job template.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<JobTemplateId>))]
public readonly record struct JobTemplateId(Guid Value) : ITypedId<JobTemplateId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static JobTemplateId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static JobTemplateId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

/// <summary>
/// Stable identifier for a build template.
/// </summary>
[JsonConverter(typeof(TypedIdJsonConverter<BuildTemplateId>))]
public readonly record struct BuildTemplateId(Guid Value) : ITypedId<BuildTemplateId>
{
    /// <summary>
    /// Creates a new sortable UUIDv7 identifier.
    /// </summary>
    /// <returns>The new identifier.</returns>
    public static BuildTemplateId New() => new(Uuid7.NewGuid());

    /// <inheritdoc />
    public static BuildTemplateId Create(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}
