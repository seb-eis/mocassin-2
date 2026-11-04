// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Tests;

public sealed class IdsTests
{
    public static TheoryData<Type> IdTypes =>
    [
        typeof(ParticleId),
        typeof(ParticleSetId),
        typeof(CellSiteId),
        typeof(StateChangeId),
        typeof(StateChangeGroupId),
        typeof(TransitionChainId),
        typeof(KineticTransitionId),
        typeof(MetropolisTransitionId),
        typeof(ClusterId),
        typeof(OccupationExchangeId),
        typeof(DopingId),
        typeof(SimulationId),
        typeof(ParameterizationId),
        typeof(JobTemplateId),
        typeof(BuildTemplateId),
    ];

    [Theory]
    [MemberData(nameof(IdTypes))]
    public void JsonRoundTripPreservesValue(Type idType)
    {
        var guid = Guid.NewGuid();
        var value = Activator.CreateInstance(idType, guid)!;
        var json = JsonSerializer.Serialize(value, idType);
        var roundTripped = JsonSerializer.Deserialize(json, idType);

        Assert.Equal($"\"{guid:D}\"", json);
        Assert.Equal(value, roundTripped);
        Assert.Equal(guid, ((ITypedId)roundTripped!).Value);
    }

    [Theory]
    [MemberData(nameof(IdTypes))]
    public void JsonRejectsNonUuidString(Type idType)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("\"not-a-uuid\"", idType));
    }

    [Theory]
    [MemberData(nameof(IdTypes))]
    public void JsonRejectsNonStringToken(Type idType)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("42", idType));
    }

    [Fact]
    public void Uuid7CreatesVersion7ValuesInTimestampOrder()
    {
        var early = Uuid7.NewGuid(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var late = Uuid7.NewGuid(DateTimeOffset.Parse("2027-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal('7', early.ToString("D")[14]);
        Assert.Equal('7', late.ToString("D")[14]);
        Assert.Contains(early.ToString("D")[19], "89ab");
        Assert.True(string.CompareOrdinal(early.ToString("D"), late.ToString("D")) < 0);
    }

    [Fact]
    public void ReservedIdsAreNonEmptyAndDistinct()
    {
        Assert.NotEqual(Guid.Empty, ParticleId.Void.Value);
        Assert.NotEqual(Guid.Empty, ParticleSetId.Empty.Value);
        Assert.NotEqual(ParticleId.Void.Value, ParticleSetId.Empty.Value);
    }

    [Fact]
    public void NewIdsUseUuid7()
    {
        Assert.Equal('7', ParticleId.New().ToString()[14]);
    }
}
