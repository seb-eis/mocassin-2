using System.Reactive.Linq;
using Mocassin.Model.DataManagement;
using Mocassin.UI.Data.Helper;
using Mocassin.UI.Data.Main;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: Mocassin.ReferenceHarness <project.json> <output.msl> [build-name-filter]");
    return 2;
}

var jsonPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
var buildFilter = args.Length >= 3 ? args[2] : "KMC";

if (!File.Exists(jsonPath))
{
    Console.Error.WriteLine($"Project JSON not found: {jsonPath}");
    return 1;
}

var project = new MocassinProject();
project.FromJson(File.ReadAllText(jsonPath));

Console.WriteLine($"Loaded project: {project.Name}, builds: {project.SimulationDbBuildTemplates.Count}, jobsets: {project.JobSetTemplates.Count}");
foreach (var build in project.SimulationDbBuildTemplates)
{
    Console.WriteLine($"  Build: {build.Name}, model={build.ModelKey}, customization={build.CustomizationKey}, jobs={build.JobTranslationKey}");
}

var template = project.SimulationDbBuildTemplates.FirstOrDefault(
                   x => x.Name.Contains(buildFilter, StringComparison.OrdinalIgnoreCase))
               ?? project.SimulationDbBuildTemplates.FirstOrDefault();

if (template == null)
{
    Console.Error.WriteLine("No simulation build template found.");
    return 3;
}

// The original stores string.GetHashCode(), which is randomized per process on modern .NET.
// Recompute it in this process before the builder's compatibility check.
if (template.ProjectCustomizationTemplate != null)
{
    template.ProjectCustomizationTemplate.ModelHash = template.ProjectModelData.Json.GetHashCode();
}

Console.WriteLine($"Building template: {template.Name}");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

using var builder = new SimulationLibraryBuilder { IsSaveAfterBuildActive = true };
using var statusSubscription = builder.LibraryBuildStatusNotifications.Subscribe(
    status => Console.WriteLine($"  status: {status}"),
    ex => Console.Error.WriteLine($"  builder error: {ex}"),
    () => Console.WriteLine("  status stream completed"));

var modelProject = ModelProjectFactory.CreateDefault();
var library = builder.BuildLibrary(template, outputPath, modelProject);

if (library == null)
{
    Console.Error.WriteLine("BuildLibrary returned null.");
    return 4;
}

using (library)
{
    var size = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
    Console.WriteLine($"Built simulation library: {outputPath}");
    Console.WriteLine($"File exists: {File.Exists(outputPath)}, size: {size} bytes");
}

return 0;
