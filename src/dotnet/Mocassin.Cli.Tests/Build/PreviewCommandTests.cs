// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Cli.Build;

namespace Mocassin.Cli.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class PreviewCommandTests
{
    private static readonly string sample = RepositoryPaths.Combine(
        "samples",
        "jobs",
        "ceria-temperature-sweep.mocprj");

    [Fact]
    public async Task SamplePreviewSucceedsAndWritesNoPackage()
    {
        var (exitCode, stdout, stderr) = await Run(sample, "--preview");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("Template: Ceria temperature and doping sweep", stdout, StringComparison.Ordinal);
        Assert.Contains("Jobs: 6 across 2 collection(s) and 4 configuration(s)", stdout, StringComparison.Ordinal);
        Assert.Contains("Doping", stdout, StringComparison.Ordinal);
        Assert.Contains("removedStartOptions=0", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: valid (exit 0)", stdout, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(sample)!, "*.msl", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task JsonPreviewReportsJobsDopingsAndOptimizers()
    {
        var (exitCode, stdout, stderr) = await Run(sample, "--preview", "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("buildPreview", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("valid").GetBoolean());
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32());
        Assert.Equal(6, root.GetProperty("summary").GetProperty("totalJobs").GetInt32());
        var preview = root.GetProperty("preview");
        Assert.Equal(6, preview.GetProperty("totalJobs").GetInt32());
        Assert.Equal(2, preview.GetProperty("collections").GetArrayLength());
        var collection = preview.GetProperty("collections")[0];
        Assert.Equal("kmc", collection.GetProperty("kind").GetString());
        Assert.True(collection.GetProperty("useDualDofCorrection").GetBoolean());
        var optimizer = collection.GetProperty("optimizers")[0];
        Assert.Equal(1, optimizer.GetProperty("targetCount").GetInt32());
        Assert.Equal(0, optimizer.GetProperty("removedStartOptions").GetInt32());
        var config = collection.GetProperty("configs")[0];
        Assert.Equal(6, config.GetProperty("cellA").GetInt32());
        Assert.Equal(864, config.GetProperty("positionCount").GetInt32());
        Assert.True(config.GetProperty("isChargeNeutral").GetBoolean());
        var doping = config.GetProperty("dopings")[0];
        Assert.Equal(0.01, doping.GetProperty("requestedFraction").GetDouble());
        Assert.Equal(2, doping.GetProperty("appliedPrimary").GetInt32());
        Assert.Equal(1, doping.GetProperty("appliedDependent").GetInt32());
    }

    [Fact]
    public async Task PreviewOfATempCopyCreatesNoMslBesideTheProject()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        await File.WriteAllBytesAsync(path, await File.ReadAllBytesAsync(sample));

        var (exitCode, _, _) = await Run(path, "--preview");

        Assert.Equal(0, exitCode);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.msl", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SeveralTemplatesWithoutSelectionAreAUsageError()
    {
        using var directory = new TempDirectory();
        var path = directory.File("two-templates.mocprj");
        await File.WriteAllTextAsync(path, AddSecondTemplate(await File.ReadAllTextAsync(sample)));

        var (exitCode, stdout, stderr) = await Run(path, "--preview", "--json");

        Assert.Equal(2, exitCode);
        Assert.Contains("--template", stderr, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(2, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("preview").ValueKind);
    }

    [Fact]
    public async Task ExplicitTemplateSelectionPreviewsOneTemplate()
    {
        using var directory = new TempDirectory();
        var path = directory.File("two-templates.mocprj");
        var (content, selected) = AddSecondTemplateWithId(await File.ReadAllTextAsync(sample));
        await File.WriteAllTextAsync(path, content);

        var (exitCode, stdout, _) = await Run(path, "--preview", "--json", "--template", selected);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(selected, document.RootElement.GetProperty("preview").GetProperty("templateId").GetString());
    }

    [Fact]
    public async Task UnknownTemplateIsAProj010Error()
    {
        var missing = Guid.NewGuid().ToString();

        var (exitCode, stdout, _) = await Run(sample, "--preview", "--json", "--template", missing);

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.False(root.GetProperty("valid").GetBoolean());
        Assert.Equal(1, root.GetProperty("exitCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("preview").ValueKind);
        Assert.Contains(
            root.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PROJ010");
    }

    [Fact]
    public async Task ProjectWithoutJobTemplatesReturnsOne()
    {
        using var directory = new TempDirectory();
        var path = directory.File("no-templates.mocprj");
        var root = JsonNode.Parse(await File.ReadAllTextAsync(sample))!.AsObject();
        root["jobTemplates"] = new JsonArray();
        await File.WriteAllTextAsync(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        var (exitCode, stdout, _) = await Run(path, "--preview", "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        var rootElement = document.RootElement;
        Assert.False(rootElement.GetProperty("valid").GetBoolean());
        Assert.Contains(
            rootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PROJ010");
    }

    [Fact]
    public async Task MissingProjectReturnsThree()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await Run(directory.File("missing.mocprj"), "--preview", "--json");

        Assert.Equal(3, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(3, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("projectId").ValueKind);
    }

    [Fact]
    public async Task UnknownOptionReturnsTwo()
    {
        var (exitCode, _, stderr) = await Run(sample, "--preview", "--parallel", "4");

        Assert.Equal(2, exitCode);
        Assert.Contains("--parallel", stderr, StringComparison.Ordinal);
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> Run(params string[] arguments)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = await PreviewCommand.RunAsync(arguments, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string AddSecondTemplate(string content)
    {
        var (modified, _) = AddSecondTemplateWithId(content);
        return modified;
    }

    private static (string Content, string SecondTemplateId) AddSecondTemplateWithId(string content)
    {
        var root = JsonNode.Parse(content)!.AsObject();
        var templates = root["jobTemplates"]!.AsArray();
        var second = JsonNode.Parse(templates[0]!.ToJsonString())!.AsObject();
        second["name"] = "Second temperature series";
        Reidentify(second);
        var id = second["id"]!.GetValue<string>();
        templates.Add(second);
        return (root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), id);
    }

    private static void Reidentify(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToArray())
                {
                    if (property.Key == "id" && property.Value is JsonValue)
                    {
                        obj["id"] = Guid.NewGuid().ToString("D");
                    }
                    else if (property.Value is not null)
                    {
                        Reidentify(property.Value);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        Reidentify(item);
                    }
                }

                break;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "mocassin-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string File(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
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
}
