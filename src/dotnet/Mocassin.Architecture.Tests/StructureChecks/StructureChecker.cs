// SPDX-License-Identifier: MIT

using System.IO.Enumeration;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocassin.Architecture.Tests.StructureChecks;

internal static class StructureChecker
{
    private static readonly string[] generatedSuffixes = [".g.cs", ".generated.cs", ".designer.cs"];

    public static IReadOnlyList<string> Check(string repositoryRoot, StructurePolicy policy)
    {
        var violations = new List<string>();
        var projectRoot = Path.Combine(repositoryRoot, "src", "dotnet");
        if (!Directory.Exists(projectRoot))
        {
            violations.Add($"Project root does not exist: {projectRoot}");
            return violations;
        }

        foreach (var projectFile in Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.AllDirectories))
        {
            if (IsUnderBuildOutput(projectFile))
            {
                continue;
            }

            CheckProject(projectFile, policy, repositoryRoot, violations);
        }

        CheckNamespaceBaseline(repositoryRoot, policy, violations);
        return violations;
    }

    private static void CheckProject(
        string projectFile,
        StructurePolicy policy,
        string repositoryRoot,
        List<string> violations)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile)!;
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        var rootNamespace = ReadElement(projectFile, "RootNamespace") ?? projectName;
        var isTestProject = projectName.EndsWith(".Tests", StringComparison.Ordinal)
            || string.Equals(ReadElement(projectFile, "IsTestProject"), "true", StringComparison.OrdinalIgnoreCase);

        var files = EnumerateSourceFiles(projectDirectory);
        var rootFiles = files.Where(file => Path.GetDirectoryName(file) == projectDirectory).ToList();

        if (isTestProject)
        {
            if (rootFiles.Count > policy.TestRootFileLimit)
            {
                violations.Add(
                    $"{Relative(repositoryRoot, projectDirectory)}: test project root has {rootFiles.Count} .cs files; limit is {policy.TestRootFileLimit}.");
            }

            foreach (var file in rootFiles)
            {
                if (!MatchesAny(Path.GetFileName(file), policy.TestRootFilePatterns))
                {
                    violations.Add(
                        $"{Relative(repositoryRoot, file)}: test files at the project root must be named *Tests.cs or be a permitted support file.");
                }
            }
        }
        else
        {
            foreach (var file in rootFiles)
            {
                if (!MatchesAny(Path.GetFileName(file), policy.SourceRootFilePatterns))
                {
                    var suggestion = SuggestFolder(file, rootNamespace);
                    var suffix = suggestion is null ? string.Empty : $" Move to: {suggestion}.";
                    violations.Add(
                        $"{Relative(repositoryRoot, file)}: source files may not live in the project root.{suffix}");
                }
            }
        }

        CheckFolderSizes(projectDirectory, files, policy, repositoryRoot, violations);

        foreach (var file in files)
        {
            CheckFile(projectDirectory, projectName, rootNamespace, file, policy, repositoryRoot, violations);
        }
    }

    private static void CheckFolderSizes(
        string projectDirectory,
        IEnumerable<string> files,
        StructurePolicy policy,
        string repositoryRoot,
        List<string> violations)
    {
        foreach (var group in files.GroupBy(file => Path.GetDirectoryName(file)!))
        {
            if (group.Count() > policy.MaxFilesPerFolder)
            {
                violations.Add(
                    $"{Relative(repositoryRoot, group.Key)}: folder has {group.Count()} direct .cs files; limit is {policy.MaxFilesPerFolder}. Split it into sub-concepts.");
            }
        }
    }

    private static void CheckFile(
        string projectDirectory,
        string projectName,
        string rootNamespace,
        string file,
        StructurePolicy policy,
        string repositoryRoot,
        List<string> violations)
    {
        var relativePath = Relative(repositoryRoot, file);
        var relative = Path.GetRelativePath(projectDirectory, file);
        var folder = Path.GetDirectoryName(relative);
        var segments = string.IsNullOrEmpty(folder)
            ? []
            : folder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (!IsPascalCase(segment))
            {
                violations.Add(
                    $"{relativePath}: folder '{segment}' must use PascalCase.");
            }

            if (IsAllowedFolder(projectName, segment, policy))
            {
                continue;
            }

            if (policy.ForbiddenFolders.Contains(segment, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add(
                    $"{relativePath}: folder '{segment}' is a catch-all; name it after the domain concept it owns.");
            }

            if (policy.NestedOnlyFolders.Contains(segment, StringComparer.OrdinalIgnoreCase) && index == 0)
            {
                violations.Add(
                    $"{relativePath}: '{segment}' is not a concept and may only appear inside a named concept folder.");
            }
        }

        if (segments.Length > policy.MaxDepth)
        {
            violations.Add(
                $"{relativePath}: folder depth is {segments.Length}; limit is {policy.MaxDepth}.");
        }

        var text = File.ReadAllText(file);
        var lineCount = File.ReadLines(file).Count();
        if (lineCount > policy.MaxFileLines)
        {
            violations.Add(
                $"{relativePath}: file has {lineCount} lines; limit is {policy.MaxFileLines}. Extract collaborators into the concept folder.");
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(text, path: file);
        var root = syntaxTree.GetRoot();
        if (root.DescendantTrivia(descendIntoTrivia: true)
            .Any(trivia => trivia.IsKind(SyntaxKind.RegionDirectiveTrivia)))
        {
            violations.Add(
                $"{relativePath}: do not use #region as a filing system; split the file instead.");
        }

        var namespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToList();
        if (namespaces.Count > 1)
        {
            violations.Add(
                $"{relativePath}: file declares multiple namespaces; use one file-scoped namespace.");
        }

        var fileName = Path.GetFileName(file);
        var declaredNamespace = GetDeclaredNamespace(root);
        var expectedNamespace = string.Join('.', new[] { rootNamespace }.Concat(segments));
        if (declaredNamespace is null)
        {
            if (!policy.NoNamespaceFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase)
                && !IsTopLevelProgram(root))
            {
                violations.Add(
                    $"{relativePath}: missing namespace; expected '{expectedNamespace}'.");
            }
        }
        else if (!string.Equals(declaredNamespace, expectedNamespace, StringComparison.Ordinal))
        {
            var baselineAllows = policy.NamespaceBaseline.Contains(relativePath, StringComparer.Ordinal)
                && string.Equals(declaredNamespace, rootNamespace, StringComparison.Ordinal);
            if (!baselineAllows)
            {
                violations.Add(
                    $"{relativePath}: namespace '{declaredNamespace}' does not match the folder; expected '{expectedNamespace}'. Move the file or fix the namespace.");
            }
        }

        var typeNames = root.DescendantNodes()
            .Where(node => node.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<MemberDeclarationSyntax>()
            .Select(GetTypeName)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();

        if (typeNames.Count == 0)
        {
            if (!policy.NoTypeFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase)
                && !IsTopLevelProgram(root))
            {
                violations.Add(
                    $"{relativePath}: no top-level type declaration found; split the file or name it after its primary type.");
            }

            return;
        }

        var stem = GetFileStem(fileName);
        if (typeNames.Count == 1)
        {
            if (!string.Equals(stem, typeNames[0], StringComparison.Ordinal)
                && !stem.StartsWith(typeNames[0] + ".", StringComparison.Ordinal))
            {
                violations.Add(
                    $"{relativePath}: file name '{stem}' must match the primary type '{typeNames[0]}' or use '{typeNames[0]}.<Aspect>.cs'.");
            }

            return;
        }

        var matchesOneType = typeNames.Any(
            typeName => string.Equals(stem, typeName, StringComparison.Ordinal)
                || stem.StartsWith(typeName + ".", StringComparison.Ordinal));
        var matchesConceptSuffix = policy.AllowedMultiTypeSuffixes.Any(
            suffix => stem.EndsWith(suffix, StringComparison.Ordinal));
        if (!matchesOneType && !matchesConceptSuffix)
        {
            violations.Add(
                $"{relativePath}: multiple top-level types require a file named after one of them or a concept file name ending in one of: {string.Join(", ", policy.AllowedMultiTypeSuffixes)}.");
        }
    }

    private static void CheckNamespaceBaseline(
        string repositoryRoot,
        StructurePolicy policy,
        List<string> violations)
    {
        foreach (var entry in policy.NamespaceBaseline)
        {
            var file = Path.Combine(repositoryRoot, entry.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
            {
                violations.Add($"{entry}: namespace baseline entry refers to a missing file; remove it.");
                continue;
            }

            var projectDirectory = FindProjectDirectory(Path.GetDirectoryName(file)!);
            var projectFile = projectDirectory is null
                ? null
                : Directory.EnumerateFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
            if (projectDirectory is null || projectFile is null)
            {
                violations.Add($"{entry}: namespace baseline entry is not under a .NET project; remove it.");
                continue;
            }

            var rootNamespace = ReadElement(projectFile, "RootNamespace")
                ?? Path.GetFileNameWithoutExtension(projectFile);
            var relative = Path.GetRelativePath(projectDirectory, file);
            var folder = Path.GetDirectoryName(relative);
            var segments = string.IsNullOrEmpty(folder)
                ? []
                : folder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var expectedNamespace = string.Join('.', new[] { rootNamespace }.Concat(segments));
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetRoot();
            var declaredNamespace = GetDeclaredNamespace(root);

            if (string.Equals(declaredNamespace, expectedNamespace, StringComparison.Ordinal))
            {
                violations.Add($"{entry}: namespace baseline entry is no longer needed; remove it.");
            }
            else if (!string.Equals(declaredNamespace, rootNamespace, StringComparison.Ordinal))
            {
                violations.Add(
                    $"{entry}: namespace baseline may only cover files that keep the project root namespace; fix the namespace or remove the entry.");
            }
        }
    }

    private static string? FindProjectDirectory(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current is not null)
        {
            if (current.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? GetDeclaredNamespace(SyntaxNode root) =>
        root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();

    private static string? SuggestFolder(string file, string rootNamespace)
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetRoot();
        var declaredNamespace = GetDeclaredNamespace(root);

        if (declaredNamespace is null
            || !declaredNamespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal))
        {
            return null;
        }

        var relativeNamespace = declaredNamespace[(rootNamespace.Length + 1)..];
        return string.Join('/', relativeNamespace.Split('.')) + "/" + Path.GetFileName(file);
    }

    private static bool IsAllowedFolder(string projectName, string folder, StructurePolicy policy)
    {
        var entry = policy.AllowedFolders.FirstOrDefault(
            pair => string.Equals(pair.Key, projectName, StringComparison.OrdinalIgnoreCase));

        return entry.Value?.Contains(folder, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static string? GetTypeName(MemberDeclarationSyntax member) =>
        member switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
            _ => null,
        };

    private static string GetFileStem(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (stem.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)
            || stem.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            stem = Path.GetFileNameWithoutExtension(stem);
        }

        return stem;
    }

    private static bool IsTopLevelProgram(SyntaxNode root) =>
        root is CompilationUnitSyntax compilationUnit
        && compilationUnit.Members.OfType<GlobalStatementSyntax>().Any();

    private static bool IsPascalCase(string value) =>
        value.Length > 0
        && char.IsUpper(value[0])
        && value.All(character => char.IsLetterOrDigit(character));

    private static bool MatchesAny(string fileName, IEnumerable<string> patterns) =>
        patterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true));

    private static string? ReadElement(string projectFile, string elementName)
    {
        var document = XDocument.Load(projectFile);
        return document.Descendants()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, elementName, StringComparison.Ordinal))
            ?.Value.Trim();
    }

    private static List<string> EnumerateSourceFiles(string projectDirectory) =>
        Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsUnderBuildOutput(file))
            .Where(file => !generatedSuffixes.Any(
                suffix => file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    private static bool IsUnderBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "obj" or "bin");

    private static string Relative(string repositoryRoot, string path) =>
        Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/');
}
