// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;

namespace Mocassin.Packages.Tests.Build;

/// <summary>
/// A temporary copy of the ceria job sample that a test may mutate.
/// </summary>
internal sealed class TestProject : IDisposable
{
    private TestProject(string directory, string path, Project project)
    {
        Directory = directory;
        Path = path;
        Project = project;
    }

    public string Directory { get; }

    public string Path { get; }

    public Project Project { get; }

    public static string SamplePath => RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj");

    public static async Task<TestProject> CreateAsync(Action<JsonObject>? mutate = null)
    {
        var text = await File.ReadAllTextAsync(SamplePath);
        var root = JsonNode.Parse(text)!.AsObject();
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

        var load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
            .LoadAsync(path);
        Assert.NotNull(load.Project);
        return new TestProject(directory, path, load.Project);
    }

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
