// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests;

[Trait("Tier", "PR-fast")]
public sealed class UnitValuesTests
{
    public static TheoryData<Type> UnitTypes =>
    [
        typeof(LengthAngstrom),
        typeof(EnergyEv),
        typeof(TemperatureKelvin),
        typeof(ChargeElementary),
        typeof(TimeSeconds),
        typeof(FrequencyHz),
        typeof(ElectricFieldVm),
        typeof(AngleDegrees),
    ];

    [Theory]
    [MemberData(nameof(UnitTypes))]
    public void JsonRoundTripPreservesValue(Type unitType)
    {
        var value = Activator.CreateInstance(unitType, 1.25)!;
        var json = JsonSerializer.Serialize(value, unitType);
        var roundTripped = JsonSerializer.Deserialize(json, unitType);

        Assert.Equal("1.25", json);
        Assert.Equal(value, roundTripped);
    }

    [Theory]
    [MemberData(nameof(UnitTypes))]
    public void JsonRejectsNonNumberToken(Type unitType)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("\"1.25\"", unitType));
    }

    [Theory]
    [MemberData(nameof(UnitTypes))]
    public void JsonRejectsNonFiniteValues(Type unitType)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("null", unitType));
    }

    [Theory]
    [MemberData(nameof(UnitTypes))]
    public void TryCreateRejectsNaNAndInfinity(Type unitType)
    {
        ArgumentNullException.ThrowIfNull(unitType);
        var tryCreate = unitType.GetMethod("TryCreate", [typeof(double), unitType.MakeByRefType()])!;

        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var arguments = new object?[] { value, null };
            var accepted = (bool)tryCreate.Invoke(null, arguments)!;
            Assert.False(accepted);
            Assert.Equal(Activator.CreateInstance(unitType), arguments[1]);
        }
    }

    [Fact]
    public void LengthRejectsNegativeValues()
    {
        Assert.False(LengthAngstrom.TryCreate(-0.1, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LengthAngstrom(-0.1));
    }

    [Fact]
    public void ChargeRejectsValuesOutsideSupportedRange()
    {
        Assert.False(ChargeElementary.TryCreate(1000.1, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChargeElementary(-1000.1));
    }

    [Fact]
    public void AngleRejectsValuesOutsideSupportedRange()
    {
        Assert.False(AngleDegrees.TryCreate(360.1, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AngleDegrees(-0.1));
    }

    [Fact]
    public void UnitTypesAreDistinct()
    {
        Assert.NotEqual(typeof(LengthAngstrom), typeof(EnergyEv));
        Assert.NotEqual(typeof(ChargeElementary), typeof(TemperatureKelvin));
    }
}
