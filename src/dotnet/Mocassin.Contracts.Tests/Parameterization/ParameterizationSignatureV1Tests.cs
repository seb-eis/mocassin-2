// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MessagePack;
using Mocassin.Contracts.Parameterization;

namespace Mocassin.Contracts.Tests.Parameterization;

[Trait("Tier", "PR-fast")]
public sealed class ParameterizationSignatureV1Tests
{
    private const string modelSignature =
        "812a7a77a8b78993cf8c475061fbf071fea27082569f154737ed12eb3d64a314";

    [Fact]
    public void ProjectionHasSixFieldsInManifestOrder()
    {
        var values = Values();
        var projection = ParameterizationSignatureV1.ComputeProjection(modelSignature, values);
        var reader = new MessagePackReader(projection);

        Assert.Equal(6, reader.ReadArrayHeader());
        Assert.Equal(ParameterizationSignatureV1.FormatVersion, reader.ReadUInt64());
        Assert.Equal(modelSignature, reader.ReadString());

        Assert.Equal(2, reader.ReadArrayHeader());
        Assert.Equal(2, reader.ReadArrayHeader());
        Assert.Equal(0.25, reader.ReadDouble());
        Assert.Equal(0.0, reader.ReadDouble());
        Assert.Equal(2, reader.ReadArrayHeader());
        Assert.Equal(0.5, reader.ReadDouble());
        Assert.Equal(2.0, reader.ReadDouble());

        Assert.Equal(1, reader.ReadArrayHeader());
        Assert.Equal(3, reader.ReadArrayHeader());
        Assert.Equal(0.125, reader.ReadDouble());
        Assert.Equal(0.25, reader.ReadDouble());
        Assert.Equal(0.5, reader.ReadDouble());

        Assert.Equal(3, reader.ReadArrayHeader());
        Assert.Equal(0.0, reader.ReadDouble());
        Assert.Equal(0.5, reader.ReadDouble());
        Assert.Equal(0.75, reader.ReadDouble());

        Assert.Equal(1, reader.ReadArrayHeader());
        Assert.Equal(1.0e13, reader.ReadDouble());
        Assert.Equal(projection.Length, reader.Consumed);
    }

    [Fact]
    public void ComputeMatchesTheDomainSeparatedProjectionHash()
    {
        var values = Values();
        var projection = ParameterizationSignatureV1.ComputeProjection(modelSignature, values);
        var domain = Encoding.UTF8.GetBytes(ParameterizationSignatureV1.Domain);
        var input = new byte[domain.Length + 1 + projection.Length];
        domain.CopyTo(input, 0);
        input[domain.Length] = 0;
        projection.CopyTo(input, domain.Length + 1);

        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(input)),
            ParameterizationSignatureV1.Compute(modelSignature, values));
    }

    [Fact]
    public void ComputeIsDeterministicAndSeparatesModelIdentityFromValues()
    {
        var values = Values();
        var baseline = ParameterizationSignatureV1.Compute(modelSignature, values);

        Assert.Equal(baseline, ParameterizationSignatureV1.Compute(modelSignature, Values()));
        Assert.NotEqual(
            baseline,
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: 0.250000000001)));
        Assert.NotEqual(
            baseline,
            ParameterizationSignatureV1.Compute(new string('a', 64), values));
    }

    [Theory]
    [InlineData(5e-13, 1e-12)]
    [InlineData(-5e-13, -1e-12)]
    [InlineData(-4e-13, 0.0)]
    [InlineData(0.30000000000000004, 0.3)]
    [InlineData(2.0, 2.0)]
    public void QuantizationUsesTwelveDecimalsAndRoundHalfAwayFromZero(double raw, double quantized)
    {
        Assert.Equal(
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: raw)),
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: quantized)));
    }

    [Fact]
    public void NegativeZeroIsNormalizedBeforeHashing()
    {
        var negative = ParameterizationSignatureV1.ComputeProjection(
            modelSignature,
            Values(pairValue: -0.0, siteValue: -0.0));
        var positive = ParameterizationSignatureV1.ComputeProjection(
            modelSignature,
            Values(pairValue: 0.0, siteValue: 0.0));

        Assert.Equal(positive, negative);
        Assert.Equal(
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: -0.0, siteValue: -0.0)),
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: 0.0, siteValue: 0.0)));
    }

    [Fact]
    public void NonFiniteValuesAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: double.NaN)));
        Assert.Throws<InvalidOperationException>(() =>
            ParameterizationSignatureV1.Compute(modelSignature, Values(pairValue: double.PositiveInfinity)));
    }

    [Fact]
    public void EmptymodelSignatureIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ParameterizationSignatureV1.Compute(null!, Values()));
        Assert.Throws<ArgumentException>(() =>
            ParameterizationSignatureV1.Compute(" ", Values()));
        Assert.Throws<ArgumentNullException>(() =>
            ParameterizationSignatureV1.Compute(modelSignature, null!));
    }

    [Fact]
    public void SignatureIsCultureInvariant()
    {
        var values = Values();
        var invariant = ParameterizationSignatureV1.Compute(modelSignature, values);
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal(invariant, ParameterizationSignatureV1.Compute(modelSignature, values));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void PublicSurfaceContainsOnlyValuesAndTheOpaquemodelSignature()
    {
        var compute = typeof(ParameterizationSignatureV1).GetMethod(
            nameof(ParameterizationSignatureV1.Compute),
            [typeof(string), typeof(ParameterizationValues)]);
        Assert.NotNull(compute);

        var properties = typeof(ParameterizationValues)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(
            ["GroupSets", "PairTables", "SiteEnergies", "TransitionRuleFrequencies"],
            properties);
    }

    private static ParameterizationValues Values(
        double pairValue = 0.25,
        double groupValue = 0.125,
        double siteValue = 0.5)
    {
        ImmutableArray<ImmutableArray<double>> pairTables =
        [
            [pairValue, -0.0],
            [0.5, 2.0],
        ];
        ImmutableArray<ImmutableArray<double>> groupSets =
        [
            [groupValue, 0.25, 0.5],
        ];
        ImmutableArray<double> siteEnergies = [-0.0, siteValue, 0.75];
        ImmutableArray<double> frequencies = [1.0e13];

        return new ParameterizationValues(pairTables, groupSets, siteEnergies, frequencies);
    }
}
