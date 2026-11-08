// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry.Tests;

[Trait("Tier", "PR-fast")]
public sealed class SpaceGroupCatalogTests
{
    private static readonly SpaceGroupCatalog catalog = SpaceGroupCatalog.Default;

    [Fact]
    public void EmbeddedCatalogHasThePinnedVersionAndChecksum()
    {
        Assert.Equal("spglib-2.8.0/catalog-1", catalog.DataVersion);
        Assert.Equal("1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395", catalog.DataSha256);
        Assert.Equal(274, catalog.Settings.Length);
        Assert.Equal(230, catalog.Settings.Select(static setting => setting.Number).Distinct().Count());
    }

    [Fact]
    public void SelectionCategoriesMatchThe274SettingRule()
    {
        var settings = catalog.Settings;

        Assert.Equal(
            186,
            settings.Count(static setting =>
                setting.Axis == AxisSetting.None
                && setting.Origin == OriginChoice.None
                && setting.Number is < 3 or > 15));
        Assert.Equal(48, settings.Count(static setting => setting.Origin != OriginChoice.None));
        Assert.Equal(
            26,
            settings.Count(static setting => setting.Number is >= 3 and <= 15));
        Assert.Equal(
            14,
            settings.Count(static setting => setting.Number is 146 or 148 or 155 or 160 or 161 or 166 or 167));
        Assert.Equal(
            26,
            settings.Count(static setting =>
                setting.Axis is AxisSetting.UniqueAxisB or AxisSetting.UniqueAxisC));
        Assert.Equal(
            14,
            settings.Count(static setting =>
                setting.Axis is AxisSetting.Hexagonal or AxisSetting.Rhombohedral));
    }

    [Fact]
    public void SettingsAreInCanonicalOrder()
    {
        for (var index = 1; index < catalog.Settings.Length; index++)
        {
            var previous = catalog.Settings[index - 1];
            var current = catalog.Settings[index];

            Assert.True(
                (current.Number, (int)current.Axis, (int)current.Origin)
                .CompareTo((previous.Number, (int)previous.Axis, (int)previous.Origin)) > 0);
        }
    }

    [Fact]
    public void EverySettingPassesCheapStructuralChecks()
    {
        foreach (var setting in catalog.Settings)
        {
            Assert.NotEmpty(setting.Operations);
            Assert.NotEmpty(setting.HermannMauguin);
            Assert.NotEmpty(setting.HallSymbol);

            var identityPresent = false;
            var operationKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var operation in setting.Operations)
            {
                Assert.True(Math.Abs(operation.Determinant) == 1);
                Assert.InRange(operation.Translation.A, 0, 23);
                Assert.InRange(operation.Translation.B, 0, 23);
                Assert.InRange(operation.Translation.C, 0, 23);

                if (operation.Linear == Int3x3.Identity
                    && operation.Translation == Rational3.Zero)
                {
                    identityPresent = true;
                }

                Assert.True(operationKeys.Add(operationKey(operation)));
            }

            Assert.True(identityPresent, $"Space group {setting.Number} has no identity.");
        }
    }

    [Theory]
    [InlineData(1, "none", "none", "P 1", 1)]
    [InlineData(2, "none", "none", "-P 1", 2)]
    [InlineData(3, "uniqueAxisB", "none", "P 2y", 2)]
    [InlineData(3, "uniqueAxisC", "none", "P 2", 2)]
    [InlineData(47, "none", "none", "-P 2 2", 8)]
    [InlineData(146, "hexagonal", "none", "R 3", 9)]
    [InlineData(146, "rhombohedral", "none", "P 3*", 3)]
    [InlineData(166, "hexagonal", "none", "-R 3 2\"", 36)]
    [InlineData(225, "none", "none", "-F 4 2 3", 192)]
    public void KnownSettingsHaveTheExpectedHallSymbolAndMultiplicity(
        int number,
        string axis,
        string origin,
        string hallSymbol,
        int multiplicity)
    {
        var setting = catalog.GetSetting(
            number,
            Enum.Parse<AxisSetting>(axis, ignoreCase: true),
            Enum.Parse<OriginChoice>(origin, ignoreCase: true));

        Assert.NotNull(setting);
        Assert.Equal(hallSymbol, setting.HallSymbol);
        Assert.Equal(multiplicity, setting.Operations.Length);
    }

    [Fact]
    public void Group47IsTheCorrectedPmmmSetting()
    {
        var setting = catalog.GetSetting(47, AxisSetting.None, OriginChoice.None);

        Assert.NotNull(setting);
        Assert.Equal("Pmmm", setting.HermannMauguin);
        Assert.Equal(CrystalSystem.Orthorhombic, setting.CrystalSystem);
        Assert.Equal(8, setting.Operations.Length);

        var inversion = new Int3x3(-1, 0, 0, 0, -1, 0, 0, 0, -1);
        Assert.Contains(setting.Operations, static operation => operation.Linear == Int3x3.Identity);
        Assert.Contains(setting.Operations, operation => operation.Linear == inversion);

        foreach (var operation in setting.Operations)
        {
            var linear = operation.Linear;
            Assert.False(
                linear.M11 == 0 && linear.M12 == 0 && linear.M13 == 0,
                "Group 47 has a degenerate x-row.");
            Assert.False(
                linear.M21 == 0 && linear.M22 == 0 && linear.M23 == 0,
                "Group 47 has a degenerate y-row.");
            Assert.False(
                linear.M31 == 0 && linear.M32 == 0 && linear.M33 == 0,
                "Group 47 has a degenerate z-row.");
        }
    }

    [Fact]
    public void EverySettingIsClosedUnderCompositionAndInverse()
    {
        foreach (var setting in catalog.Settings)
        {
            var operations = setting.Operations.ToHashSet();

            foreach (var left in setting.Operations)
            {
                Assert.True(
                    operations.Contains(left.Compose(left.Inverse())),
                    $"Space group {setting.Number} is missing an inverse closure.");

                foreach (var right in setting.Operations)
                {
                    Assert.True(
                        operations.Contains(left.Compose(right)),
                        $"Space group {setting.Number} is not closed under composition.");
                }
            }
        }
    }

    [Fact]
    public void EverySettingMatchesTheFrozenGeneralMultiplicityTable()
    {
        var json = File.ReadAllText(RepositoryPaths.Combine(
            "tests",
            "reference",
            "symmetry",
            "ase-general-multiplicities.json"));
        using var document = JsonDocument.Parse(json);
        var counts = document.RootElement.GetProperty("multiplicities")
            .EnumerateArray()
            .Select(static element => element.GetInt32())
            .ToArray();

        Assert.Equal(230, counts.Length);
        foreach (var setting in catalog.Settings)
        {
            var expected = counts[setting.Number - 1];
            if (setting.Axis == AxisSetting.Rhombohedral)
            {
                expected /= 3;
            }

            Assert.Equal(expected, setting.Operations.Length);
        }
    }

    [Fact]
    public void LookupsDisambiguateBySettingIdentity()
    {
        Assert.Null(catalog.GetSetting(3, AxisSetting.UniqueAxisA, OriginChoice.None));
        Assert.Null(catalog.GetSetting(999, AxisSetting.None, OriginChoice.None));

        var p2Settings = catalog.GetByHermannMauguin("P2");
        Assert.Equal(2, p2Settings.Length);
        Assert.Equal(AxisSetting.UniqueAxisB, p2Settings[0].Axis);
        Assert.Equal(AxisSetting.UniqueAxisC, p2Settings[1].Axis);

        var fm3m = catalog.GetByHermannMauguin("Fm-3m");
        Assert.Single(fm3m);
        Assert.Equal(225, fm3m[0].Number);

        Assert.Equal(225, catalog.GetByHallSymbol("-F 4 2 3")!.Number);
        Assert.Null(catalog.GetByHallSymbol("not a Hall symbol"));
    }

    [Fact]
    public void CatalogSummaryMatchesTheFrozenGoldenHash()
    {
        Assert.Equal("2b216b13367362eff53cf0a4624437d783851c053ef0f93948e1be0aeab8c9a4", ComputeCatalogSummary());
    }

    private static string ComputeCatalogSummary()
    {
        var builder = new StringBuilder();
        foreach (var setting in catalog.Settings)
        {
            builder.Append(setting.Number)
                .Append(';')
                .Append((int)setting.Axis)
                .Append(';')
                .Append((int)setting.Origin)
                .Append(';')
                .Append(setting.HermannMauguin)
                .Append(';')
                .Append(setting.HallSymbol)
                .Append(';')
                .Append((int)setting.CrystalSystem)
                .Append(';')
                .Append(setting.Operations.Length)
                .Append(';');

            foreach (var operation in setting.Operations)
            {
                builder.Append(operation.Linear.M11).Append(',')
                    .Append(operation.Linear.M12).Append(',')
                    .Append(operation.Linear.M13).Append(',')
                    .Append(operation.Linear.M21).Append(',')
                    .Append(operation.Linear.M22).Append(',')
                    .Append(operation.Linear.M23).Append(',')
                    .Append(operation.Linear.M31).Append(',')
                    .Append(operation.Linear.M32).Append(',')
                    .Append(operation.Linear.M33).Append(',')
                    .Append(operation.Translation.A).Append(',')
                    .Append(operation.Translation.B).Append(',')
                    .Append(operation.Translation.C).Append(';');
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string operationKey(SymmetryOperation operation) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{operation.Linear.M11},{operation.Linear.M12},{operation.Linear.M13},{operation.Linear.M21},{operation.Linear.M22},{operation.Linear.M23},{operation.Linear.M31},{operation.Linear.M32},{operation.Linear.M33}|{operation.Translation.A},{operation.Translation.B},{operation.Translation.C}");
}
