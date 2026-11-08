// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace Mocassin.Architecture.Tests.StructureChecks;

internal sealed record StructurePolicy
{
    public required string[] SourceRootFilePatterns { get; init; }

    public required string[] TestRootFilePatterns { get; init; }

    public string[] NoNamespaceFiles { get; init; } = [];

    public string[] NoTypeFiles { get; init; } = [];

    public string[] ForbiddenFolders { get; init; } = [];

    public string[] NestedOnlyFolders { get; init; } = [];

    public string[] AllowedMultiTypeSuffixes { get; init; } = [];

    public Dictionary<string, string[]> AllowedFolders { get; init; } = [];

    public int TestRootFileLimit { get; init; } = 20;

    public int MaxFilesPerFolder { get; init; } = 20;

    public int MaxDepth { get; init; } = 4;

    public int MaxFileLines { get; init; } = 800;

    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static StructurePolicy Load(string path)
    {
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<StructurePolicy>(json, options)
            ?? throw new InvalidOperationException($"Could not read the structure policy at {path}.");
    }
}
