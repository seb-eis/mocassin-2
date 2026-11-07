// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace Mocassin.Symmetry.Tests;

public sealed class SpaceGroupCatalogCorruptionTests
{
    [Fact]
    public void RejectsAChecksumMismatch()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SpaceGroupCatalogLoader.Load(ReadCatalogJson(), "deadbeef"));

        Assert.Contains("checksum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsAnUnsupportedSchemaVersion()
    {
        var document = ReadCatalog();
        document.SchemaVersion = 2;

        AssertCorrupt(document, "schema version");
    }

    [Fact]
    public void RejectsAnUnexpectedDataVersion()
    {
        var document = ReadCatalog();
        document.DataVersion = "spglib-2.8.0/catalog-2";

        AssertCorrupt(document, "data version");
    }

    [Fact]
    public void RejectsAnUnnormalizedTranslation()
    {
        var document = ReadCatalog();
        document.Settings[0].Operations[0].Translation[0] = 24;

        AssertCorrupt(document, "unnormalized translation");
    }

    [Fact]
    public void RejectsANonUnimodularOperation()
    {
        var document = ReadCatalog();
        document.Settings[0].Operations[0].Linear[0] = 2;

        AssertCorrupt(document, "non-unimodular");
    }

    [Fact]
    public void RejectsASettingWithoutIdentity()
    {
        var document = ReadCatalog();
        var setting = document.Settings.First(static setting => setting.Number == 2);
        setting.Operations = setting.Operations
            .Where(static operation => !IsIdentity(operation))
            .ToArray();

        AssertCorrupt(document, "identity");
    }

    [Fact]
    public void RejectsTheReservedUniqueAxisAValue()
    {
        var document = ReadCatalog();
        document.Settings[0].Axis = "uniqueAxisA";

        AssertCorrupt(document, "unknown axis");
    }

    [Fact]
    public void RejectsUnsortedSettings()
    {
        var document = ReadCatalog();
        (document.Settings[0], document.Settings[1]) = (document.Settings[1], document.Settings[0]);

        AssertCorrupt(document, "sorted");
    }

    [Fact]
    public void RejectsAMissingSetting()
    {
        var document = ReadCatalog();
        document.Settings = document.Settings.Skip(1).ToArray();

        AssertCorrupt(document, "274");
    }

    private static void AssertCorrupt(SpaceGroupCatalogDto document, string messageFragment)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(
            document,
            CatalogJsonContext.Default.SpaceGroupCatalogDto);
        var checksum = SpaceGroupCatalogLoader.ComputeChecksum(document.Settings);

        var exception = Assert.Throws<InvalidOperationException>(
            () => SpaceGroupCatalogLoader.Load(json, checksum));
        Assert.Contains(messageFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SpaceGroupCatalogDto ReadCatalog() =>
        JsonSerializer.Deserialize(
            ReadCatalogJson(),
            CatalogJsonContext.Default.SpaceGroupCatalogDto)
        ?? throw new InvalidOperationException("The test catalog is empty.");

    private static byte[] ReadCatalogJson() =>
        File.ReadAllBytes(RepositoryPaths.Combine(
            "src",
            "dotnet",
            "Mocassin.Symmetry",
            "Data",
            "space-groups.json"));

    private static bool IsIdentity(SpaceGroupOperationDto operation) =>
        operation.Linear is [1, 0, 0, 0, 1, 0, 0, 0, 1]
        && operation.Translation.All(static component => component == 0);
}
