// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mocassin.Cli.Tests.Build;

/// <summary>
/// A temporary copy of the ceria job sample that a CLI test may mutate.
/// </summary>
internal sealed class CliTestProject : IDisposable
{
    private CliTestProject(string directory, string path)
    {
        Directory = directory;
        Path = path;
    }

    public string Directory { get; }

    public string Path { get; }

    public static string SamplePath =>
        RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj");

    public static async Task<CliTestProject> CreateAsync(Action<JsonObject>? mutate = null)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(SamplePath))!.AsObject();
        mutate?.Invoke(root);
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "mocassin-tests",
            Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "project.mocprj");
        await File.WriteAllTextAsync(
            path,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return new CliTestProject(directory, path);
    }

    public static void AddEmptyParameterization(JsonObject root, string id, string modelSignature)
    {
        var array = root["parameterizations"] as JsonArray ?? [];
        root["parameterizations"] = array;
        array.Add(new JsonObject
        {
            ["id"] = id,
            ["name"] = "Empty explicit template",
            ["modelSignature"] = modelSignature,
            ["createdUtc"] = "2026-11-17T12:00:00.0000000Z",
            ["stablePairSets"] = new JsonArray(),
            ["unstablePairSets"] = new JsonArray(),
            ["groupSets"] = new JsonArray(),
            ["siteEnergyOverrides"] = new JsonArray(),
            ["transitionRuleSets"] = new JsonArray(),
        });
    }

    public static void AddBuildTemplate(
        JsonObject root,
        string id,
        string name,
        string jobTemplateId,
        string parameterizationId,
        string outputName)
    {
        var array = root["buildTemplates"] as JsonArray ?? [];
        root["buildTemplates"] = array;
        array.Add(new JsonObject
        {
            ["id"] = id,
            ["name"] = name,
            ["jobTemplateId"] = jobTemplateId,
            ["parameterizationId"] = parameterizationId,
            ["outputName"] = outputName,
        });
    }

    public static string JobTemplateId(JsonObject root) =>
        root["jobTemplates"]!.AsArray()[0]!["id"]!.GetValue<string>();

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort test cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort test cleanup.
        }
    }
}
