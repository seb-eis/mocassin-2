// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry;

/// <summary>
/// Loads and validates the embedded space-group catalog.
/// </summary>
internal static class SpaceGroupCatalogLoader
{
    internal const string CatalogResourceName = "Mocassin.Symmetry.Data.space-groups.json";
    internal const string ChecksumResourceName = "Mocassin.Symmetry.Data.space-groups.sha256";
    internal const int CatalogSchemaVersion = 1;
    internal const string CatalogDataVersion = "spglib-2.8.0/catalog-1";
    internal const string CatalogSource = "spglib";
    internal const string CatalogSourceVersion = "2.8.0";
    internal const int CatalogSettingCount = 274;

    private static readonly JsonSerializerOptions canonicalOptions =
        new(CatalogJsonContext.Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
        };

    /// <summary>
    /// Loads the catalog from embedded resources.
    /// </summary>
    /// <returns>The validated catalog.</returns>
    /// <exception cref="InvalidOperationException">A resource is missing or the catalog is corrupt.</exception>
    public static SpaceGroupCatalog LoadEmbedded()
    {
        var json = ReadResource(CatalogResourceName);
        var checksum = Encoding.UTF8.GetString(ReadResource(ChecksumResourceName));
        return Load(json, checksum);
    }

    /// <summary>
    /// Loads a catalog from UTF-8 JSON and an expected canonical checksum.
    /// </summary>
    /// <param name="json">The catalog JSON.</param>
    /// <param name="expectedChecksum">The expected canonical settings checksum.</param>
    /// <returns>The validated catalog.</returns>
    /// <exception cref="InvalidOperationException">The catalog is corrupt.</exception>
    internal static SpaceGroupCatalog Load(ReadOnlySpan<byte> json, string expectedChecksum)
    {
        SpaceGroupCatalogDto? document;
        try
        {
            document = JsonSerializer.Deserialize(json, CatalogJsonContext.Default.SpaceGroupCatalogDto);
        }
        catch (JsonException exception)
        {
            throw Corrupt("the JSON document could not be deserialized.", exception);
        }

        if (document is null)
        {
            throw Corrupt("the JSON document is empty.");
        }

        ValidateMetadata(document);
        var computedChecksum = ComputeChecksum(document.Settings);
        if (!string.Equals(computedChecksum, expectedChecksum.Trim(), StringComparison.Ordinal))
        {
            throw Corrupt(
                $"the canonical settings checksum is {computedChecksum}, but the committed checksum is {expectedChecksum.Trim()}.");
        }

        var settings = ValidateSettings(document.Settings);
        return new SpaceGroupCatalog(document.DataVersion, computedChecksum, settings);
    }

    private static void ValidateMetadata(SpaceGroupCatalogDto document)
    {
        if (document.SchemaVersion != CatalogSchemaVersion)
        {
            throw Corrupt(
                $"schema version {document.SchemaVersion} is not the supported version {CatalogSchemaVersion}.");
        }

        if (!string.Equals(document.DataVersion, CatalogDataVersion, StringComparison.Ordinal))
        {
            throw Corrupt($"data version '{document.DataVersion}' is not '{CatalogDataVersion}'.");
        }

        if (!string.Equals(document.Source, CatalogSource, StringComparison.Ordinal)
            || !string.Equals(document.SourceVersion, CatalogSourceVersion, StringComparison.Ordinal))
        {
            throw Corrupt(
                $"source '{document.Source}' {document.SourceVersion} is not {CatalogSource} {CatalogSourceVersion}.");
        }

        if (document.Selection is null
            || document.Selection.Total != CatalogSettingCount
            || document.Settings.Length != CatalogSettingCount)
        {
            throw Corrupt($"the catalog must contain exactly {CatalogSettingCount} settings.");
        }
    }

    private static ImmutableArray<SpaceGroupSetting> ValidateSettings(SpaceGroupSettingDto[] dtoSettings)
    {
        if (dtoSettings.Length != CatalogSettingCount)
        {
            throw Corrupt($"the catalog contains {dtoSettings.Length} settings, expected {CatalogSettingCount}.");
        }

        var settings = ImmutableArray.CreateBuilder<SpaceGroupSetting>(dtoSettings.Length);
        var seenNumbers = new HashSet<int>();
        var seenKeys = new HashSet<(int, AxisSetting, OriginChoice)>();
        var seenHallSymbols = new HashSet<string>(StringComparer.Ordinal);
        (int Number, int Axis, int Origin)? previousKey = null;

        foreach (var dto in dtoSettings)
        {
            if (dto.Number is < 1 or > 230)
            {
                throw Corrupt($"space-group number {dto.Number} is outside 1..230.");
            }

            if (string.IsNullOrWhiteSpace(dto.HermannMauguin)
                || string.IsNullOrWhiteSpace(dto.HallSymbol))
            {
                throw Corrupt($"space group {dto.Number} has an empty symbol.");
            }

            var crystalSystem = ParseCrystalSystem(dto.CrystalSystem, dto.Number);
            var axis = ParseAxis(dto.Axis, dto.Number);
            var origin = ParseOrigin(dto.Origin, dto.Number);

            if (!seenKeys.Add((dto.Number, axis, origin)))
            {
                throw Corrupt($"setting ({dto.Number}, {axis}, {origin}) occurs more than once.");
            }

            seenNumbers.Add(dto.Number);

            if (!seenHallSymbols.Add(dto.HallSymbol))
            {
                throw Corrupt($"Hall symbol '{dto.HallSymbol}' occurs more than once.");
            }

            var currentKey = (Number: dto.Number, Axis: (int)axis, Origin: (int)origin);
            if (previousKey is { } previous
                && (currentKey.Number < previous.Number
                    || (currentKey.Number == previous.Number && currentKey.Axis < previous.Axis)
                    || (currentKey.Number == previous.Number
                        && currentKey.Axis == previous.Axis
                        && currentKey.Origin < previous.Origin)))
            {
                throw Corrupt("settings are not sorted by (number, axis, origin).");
            }

            previousKey = currentKey;

            var operations = ValidateOperations(dto, axis, origin);
            settings.Add(new SpaceGroupSetting(
                dto.Number,
                dto.HermannMauguin,
                dto.HallSymbol,
                crystalSystem,
                axis,
                origin,
                operations));
        }

        if (seenNumbers.Count != 230)
        {
            throw Corrupt($"the catalog contains {seenNumbers.Count} distinct group numbers, expected 230.");
        }

        return settings.MoveToImmutable();
    }

    private static ImmutableArray<SymmetryOperation> ValidateOperations(
        SpaceGroupSettingDto dto,
        AxisSetting axis,
        OriginChoice origin)
    {
        if (dto.Operations.Length == 0)
        {
            throw Corrupt($"space-group setting ({dto.Number}, {axis}, {origin}) has no operations.");
        }

        var operations = ImmutableArray.CreateBuilder<SymmetryOperation>(dto.Operations.Length);
        var identityPresent = false;

        foreach (var operation in dto.Operations)
        {
            if (operation.Linear.Length != 9)
            {
                throw Corrupt($"setting ({dto.Number}, {axis}, {origin}) has a non-3×3 linear part.");
            }

            if (operation.Translation.Length != 3)
            {
                throw Corrupt($"setting ({dto.Number}, {axis}, {origin}) has a translation that is not 3D.");
            }

            if (operation.Translation.Any(static numerator =>
                    numerator is < 0 or >= Rational3.Denominator))
            {
                throw Corrupt(
                    $"setting ({dto.Number}, {axis}, {origin}) has an unnormalized translation.");
            }

            var linear = new Int3x3(
                operation.Linear[0],
                operation.Linear[1],
                operation.Linear[2],
                operation.Linear[3],
                operation.Linear[4],
                operation.Linear[5],
                operation.Linear[6],
                operation.Linear[7],
                operation.Linear[8]);
            if (Math.Abs(linear.Determinant) != 1)
            {
                throw Corrupt(
                    $"setting ({dto.Number}, {axis}, {origin}) has a non-unimodular operation.");
            }

            var translation = new Rational3(
                operation.Translation[0],
                operation.Translation[1],
                operation.Translation[2]);
            if (linear == Int3x3.Identity && translation.IsZero)
            {
                identityPresent = true;
            }

            operations.Add(new SymmetryOperation(linear, translation));
        }

        if (!identityPresent)
        {
            throw Corrupt($"setting ({dto.Number}, {axis}, {origin}) has no identity operation.");
        }

        return operations.MoveToImmutable();
    }

    internal static string ComputeChecksum(SpaceGroupSettingDto[] settings)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(settings, canonicalOptions);
        return Convert.ToHexStringLower(SHA256.HashData(payload));
    }

    private static CrystalSystem ParseCrystalSystem(string value, int number) =>
        value switch
        {
            "triclinic" => CrystalSystem.Triclinic,
            "monoclinic" => CrystalSystem.Monoclinic,
            "orthorhombic" => CrystalSystem.Orthorhombic,
            "tetragonal" => CrystalSystem.Tetragonal,
            "trigonal" => CrystalSystem.Trigonal,
            "hexagonal" => CrystalSystem.Hexagonal,
            "cubic" => CrystalSystem.Cubic,
            _ => throw Corrupt($"space group {number} has unknown crystal system '{value}'."),
        };

    private static AxisSetting ParseAxis(string value, int number) =>
        value switch
        {
            "none" => AxisSetting.None,
            "uniqueAxisB" => AxisSetting.UniqueAxisB,
            "uniqueAxisC" => AxisSetting.UniqueAxisC,
            "hexagonal" => AxisSetting.Hexagonal,
            "rhombohedral" => AxisSetting.Rhombohedral,
            _ => throw Corrupt($"space group {number} has unknown axis setting '{value}'."),
        };

    private static OriginChoice ParseOrigin(string value, int number) =>
        value switch
        {
            "none" => OriginChoice.None,
            "originChoice1" => OriginChoice.OriginChoice1,
            "originChoice2" => OriginChoice.OriginChoice2,
            _ => throw Corrupt($"space group {number} has unknown origin choice '{value}'."),
        };

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(SpaceGroupCatalogLoader).Assembly.GetManifestResourceStream(name)
            ?? throw Corrupt($"the embedded resource '{name}' is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static InvalidOperationException Corrupt(string message, Exception? innerException = null) =>
        new($"The embedded space-group catalog is invalid: {message}", innerException);
}
