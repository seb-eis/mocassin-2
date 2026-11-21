// SPDX-License-Identifier: MIT

using System.Xml.Linq;
using Mocassin.Architecture.Tests.TestSupport;

namespace Mocassin.Architecture.Tests.AppBoundaryChecks;

/// <summary>
/// Verifies the App-project dependency direction and the view-model I/O token tripwire.
/// </summary>
public sealed class AppBoundaryTests
{
    [Fact]
    public void TheViewModelProjectHasNoAvaloniaOrViewReference()
    {
        var references = ReadProjectReferences("Mocassin.App.ViewModels");

        Assert.DoesNotContain(references, reference => reference.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain("Mocassin.App.Views", references);
        Assert.DoesNotContain("Mocassin.App", references);
    }

    [Fact]
    public void TheApplicationProjectHasNoCliOrCompilationReference()
    {
        var references = ReadProjectReferences("Mocassin.App");

        Assert.DoesNotContain("Mocassin.Cli", references);
        Assert.DoesNotContain("Mocassin.Compilation", references);
    }

    [Fact]
    public void TheViewModelSourcesContainNoBannedIoTokens()
    {
        var root = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App.ViewModels");
        var violations = new List<string>();

        foreach (var file in EnumerateSources(root))
        {
            violations.AddRange(AppBoundaryScanner.ScanSource(File.ReadAllText(file), file));
        }

        Assert.True(
            violations.Count == 0,
            "The view-model I/O boundary is violated:" + Environment.NewLine
            + string.Join(Environment.NewLine, violations.Select(violation => "  - " + violation)));
    }

    [Fact]
    public void TheViewModelSourcesUseTheInMemoryValidationPathOnly()
    {
        var root = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App.ViewModels");
        var violations = EnumerateSources(root)
            .Where(file => File.ReadAllText(file).Contains("ValidateFileAsync", StringComparison.Ordinal))
            .Select(file => Path.GetFileName(file))
            .ToList();

        Assert.True(
            violations.Count == 0,
            "The ViewModels layer must use the in-memory pipeline only: " + string.Join(", ", violations));
    }

    [Fact]
    public void TheViewModelSourcesDoNotHashProjects()
    {
        var root = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App.ViewModels");
        var violations = EnumerateSources(root)
            .Where(file => File.ReadAllText(file).Contains("ProjectHasher", StringComparison.Ordinal))
            .Select(file => Path.GetFileName(file))
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Project hashing stays in Core; the ViewModels layer must not call ProjectHasher: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void TheScannerReportsASeededViolation()
    {
        const string source = "namespace Sample; internal sealed class C { internal void M() { File.ReadAllText(\"x\"); } }";

        var violations = AppBoundaryScanner.ScanSource(source, "Seed.cs");

        Assert.Contains(violations, violation => violation.Contains("File.", StringComparison.Ordinal));
    }

    [Fact]
    public void TheScannerIgnoresMemberNamesAndLiterals()
    {
        const string source = """
            namespace Sample;

            internal sealed class C
            {
                // System.IO and File. appear only in this comment.
                private const string Text = "System.IO File.ReadAllText";

                internal string LastOpenDirectory { get; } = "/tmp";

                internal string M() => LastOpenDirectory.Trim();
            }
            """;

        Assert.Empty(AppBoundaryScanner.ScanSource(source, "Clean.cs"));
    }

    [Fact]
    public void TheScannerDetectsUsingStaticSystemIo()
    {
        const string source = "using static System.IO; namespace Sample; internal sealed class C { }";

        Assert.Contains(
            AppBoundaryScanner.ScanSource(source, "UsingStatic.cs"),
            violation => violation.Contains("System.IO", StringComparison.Ordinal));
    }

    private static List<string> ReadProjectReferences(string projectName)
    {
        var projectPath = RepositoryPaths.Combine("src", "dotnet", projectName, projectName + ".csproj");
        var document = XDocument.Load(projectPath);
        return document.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Select(value => Path.GetFileNameWithoutExtension(value!.Replace('\\', '/')))
            .ToList();
    }

    private static IEnumerable<string> EnumerateSources(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
}
