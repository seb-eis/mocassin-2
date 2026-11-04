// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using FsCheck.Xunit;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Packages.Tests;

public sealed class ProjectStoreTests
{
    private static readonly DateTimeOffset now = DateTimeOffset.Parse("2026-11-04T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task SaveAndLoadRoundTrip()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore(new FixedTimeProvider(now));
        var path = directory.File("ceria.mocprj");

        var saved = await store.SaveAsync(CreateProject(), path);
        var loaded = await store.LoadAsync(path);

        Assert.True(saved.IsSuccess);
        Assert.True(loaded.IsSuccess);
        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(saved.SavedProject!),
            ProjectCanonicalizer.ToCanonicalJson(loaded.Project!));
    }

    [Fact]
    public async Task SaveWritesUtf8WithoutBomAndLfLineEndings()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore(new FixedTimeProvider(now));
        var path = directory.File("ceria.mocprj");

        var result = await store.SaveAsync(CreateProject(), path);

        Assert.True(result.IsSuccess);
        var bytes = await File.ReadAllBytesAsync(path);
        Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.StartsWith("{\n  \"$schema\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveUpdatesModifiedTimestamp()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore(new FixedTimeProvider(now));
        var project = CreateProject();

        var result = await store.SaveAsync(project, directory.File("ceria.mocprj"));

        Assert.True(result.IsSuccess);
        Assert.Equal(now, result.SavedProject!.Metadata.ModifiedUtc);
    }

    [Fact]
    public async Task FailedSaveLeavesOriginalFileIntact()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var realStore = new ProjectStore(new FixedTimeProvider(now));
        Assert.True((await realStore.SaveAsync(CreateProject(), path)).IsSuccess);
        var original = await File.ReadAllBytesAsync(path);

        var failingStore = new ProjectStore(
            new FixedTimeProvider(now),
            new ProjectStoreOptions(),
            new ThrowingFileWriter());

        var result = await failingStore.SaveAsync(CreateProject("Changed"), path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SaveFailed);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task SuccessfulSaveLeavesNoTemporaryFiles()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore(new FixedTimeProvider(now));

        Assert.True((await store.SaveAsync(CreateProject(), directory.File("ceria.mocprj"))).IsSuccess);

        Assert.DoesNotContain(
            Directory.GetFiles(directory.Path, "*", SearchOption.AllDirectories),
            static path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LockPreventsSecondSessionAndReleasesOnDispose()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore(new FixedTimeProvider(now));
        var path = directory.File("ceria.mocprj");

        var first = await store.AcquireLockAsync(path);
        var second = await store.AcquireLockAsync(path);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Contains(second.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.LockHeld);

        await first.Lock!.DisposeAsync();

        var third = await store.AcquireLockAsync(path);
        Assert.True(third.IsSuccess);
        await third.Lock!.DisposeAsync();
    }

    [Fact]
    public async Task StaleLockIsReclaimed()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var lockPath = ProjectStore.GetLockPath(path);
        var timestamp = (now - TimeSpan.FromHours(1)).ToString("O", CultureInfo.InvariantCulture);
        await File.WriteAllTextAsync(
            lockPath,
            $$"""{"pid":2147483647,"host":"{{Environment.MachineName}}","token":"stale","acquiredUtc":"{{timestamp}}"}""");

        var store = new ProjectStore(new FixedTimeProvider(now));
        var result = await store.AcquireLockAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.StaleLockRecovered);
        await result.Lock!.DisposeAsync();
    }

    [Fact]
    public async Task MalformedLockIsReclaimed()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        await File.WriteAllTextAsync(ProjectStore.GetLockPath(path), "not-json");

        var store = new ProjectStore(new FixedTimeProvider(now));
        var result = await store.AcquireLockAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.StaleLockRecovered);
        await result.Lock!.DisposeAsync();
    }

    [Fact]
    public async Task LoadRejectsFutureSchemaVersion()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["schemaVersion"] = ProjectSchema.CurrentVersion + 1;
        await File.WriteAllTextAsync(path, root.ToJsonString());

        var result = await new ProjectStore(new FixedTimeProvider(now)).LoadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Project);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.FutureSchemaVersion);
    }

    [Fact]
    public async Task LoadRejectsByteOrderMark()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var bytes = ProjectJson.SerializeToUtf8Bytes(CreateProject());
        await File.WriteAllBytesAsync(path, [0xEF, 0xBB, 0xBF, .. bytes]);

        var result = await new ProjectStore(new FixedTimeProvider(now)).LoadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);
    }

    [Fact]
    public async Task LoadRejectsInvalidUtf8()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        await File.WriteAllBytesAsync(path, [0xFF, 0xFE, 0x00]);

        var result = await new ProjectStore(new FixedTimeProvider(now)).LoadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);
    }

    [Fact]
    public async Task LoadReturnsProjectAndReferenceDiagnostics()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var store = new ProjectStore(new FixedTimeProvider(now));
        var invalid = CreateProjectWithUnknownReference();

        Assert.True((await store.SaveAsync(invalid, path)).IsSuccess);
        var result = await store.LoadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Project);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public async Task AutosaveRoundTripAndDelete()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var store = new ProjectStore(new FixedTimeProvider(now));
        var project = CreateProject();

        var written = await store.WriteAutosaveAsync(project, path);
        var loaded = await store.LoadAutosaveAsync(path);

        Assert.True(written.IsSuccess);
        Assert.True(loaded.IsSuccess);
        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded.Project!));
        Assert.True(ProjectStore.DeleteAutosave(path));
        Assert.False(File.Exists(ProjectStore.GetAutosavePath(path)));
    }

    [Fact]
    public async Task ContractFixtureLoadsSavesAndReloadsWithoutDataLoss()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore(new FixedTimeProvider(now));
        var fixturePath = FindRepositoryFile("tests/contracts/project-minimal.mocprj");
        var loaded = await store.LoadAsync(fixturePath);
        var outputPath = directory.File("project-minimal.mocprj");

        var saved = await store.SaveAsync(loaded.Project!, outputPath);
        var reloaded = await store.LoadAsync(outputPath);

        Assert.True(loaded.IsSuccess);
        Assert.True(saved.IsSuccess);
        Assert.True(reloaded.IsSuccess);
        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(loaded.Project!),
            ProjectCanonicalizer.ToCanonicalJson(reloaded.Project!));
    }

    [Fact]
    public async Task MissingFileReturnsDiagnostic()
    {
        using var directory = new TempDirectory();

        var result = await new ProjectStore(new FixedTimeProvider(now)).LoadAsync(directory.File("missing.mocprj"));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Project);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);
    }

    [Theory]
    [InlineData("duplicate-name")]
    [InlineData("duplicate-schema-version")]
    [InlineData("missing-camera")]
    [InlineData("missing-render-box")]
    [InlineData("null-parameterization")]
    [InlineData("null-job-template")]
    [InlineData("null-build-template")]
    [InlineData("null-object-style")]
    [InlineData("non-finite-style-scale")]
    [InlineData("non-finite-additional-style")]
    public async Task LoadNeverThrowsForMalformedStructures(string mutation)
    {
        using var directory = new TempDirectory();
        var path = directory.File("malformed.mocprj");
        await File.WriteAllTextAsync(path, MutateFixture(mutation));

        var result = await new ProjectStore(new FixedTimeProvider(now)).LoadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);
    }

    [Property(MaxTest = 30)]
    public bool StructuredMutationsNeverEscapeAsExceptions(int seed)
    {
        using var directory = new TempDirectory();
        var path = directory.File("fuzz.mocprj");
        var mutations = new[]
        {
            "duplicate-name",
            "duplicate-schema-version",
            "missing-camera",
            "missing-render-box",
            "null-parameterization",
            "null-object-style",
            "non-finite-style-scale",
            "non-finite-additional-style",
        };
        var mutation = mutations[(int)((uint)seed % (uint)mutations.Length)];
        File.WriteAllText(path, MutateFixture(mutation));

        var result = new ProjectStore(new FixedTimeProvider(now)).LoadAsync(path).GetAwaiter().GetResult();

        return !result.IsSuccess &&
            result.Diagnostics.Any(static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);
    }

    [Fact]
    public async Task SaveReturnsDiagnosticForNonFiniteStyleScale()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var store = new ProjectStore(new FixedTimeProvider(now));
        Assert.True((await store.SaveAsync(CreateProject(), path)).IsSuccess);
        var original = await File.ReadAllBytesAsync(path);
        var invalid = CreateProjectWithStyleScale(double.PositiveInfinity);

        var result = await store.SaveAsync(invalid, path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SaveFailed);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task AutosaveReturnsDiagnosticForNonFiniteStyleScale()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var store = new ProjectStore(new FixedTimeProvider(now));

        var result = await store.WriteAutosaveAsync(
            CreateProjectWithStyleScale(double.PositiveInfinity),
            path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.AutosaveFailed);
        Assert.False(File.Exists(ProjectStore.GetAutosavePath(path)));
    }

    [Fact]
    public async Task LiveSameHostLockIsNotReclaimedByTimeoutAlone()
    {
        using var directory = new TempDirectory();
        var path = directory.File("ceria.mocprj");
        var lockPath = ProjectStore.GetLockPath(path);
        var timestamp = (now - TimeSpan.FromHours(1)).ToString("O", CultureInfo.InvariantCulture);
        await File.WriteAllTextAsync(
            lockPath,
            $$"""{"pid":{{Environment.ProcessId}},"host":"{{Environment.MachineName}}","token":"live","acquiredUtc":"{{timestamp}}"}""");

        var result = await new ProjectStore(new FixedTimeProvider(now)).AcquireLockAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.LockHeld);
        File.Delete(lockPath);
    }

    private static string MutateFixture(string mutation)
    {
        var text = File.ReadAllText(FindRepositoryFile("tests/contracts/project-minimal.mocprj"));
        return mutation switch
        {
            "duplicate-name" => text.Replace(
                "\"name\": \"Blank\"",
                "\"name\": \"Blank\",\n  \"name\": \"Duplicate\"",
                StringComparison.Ordinal),
            "duplicate-schema-version" => text.Replace(
                "\"schemaVersion\": 1",
                "\"schemaVersion\": 1,\n  \"schemaVersion\": 1",
                StringComparison.Ordinal),
            "missing-camera" => MutateJson(text, static root => root["viewSettings"]!.AsObject().Remove("camera")),
            "missing-render-box" => MutateJson(text, static root => root["viewSettings"]!.AsObject().Remove("renderBox")),
            "null-parameterization" => MutateJson(text, static root => root["parameterizations"] = JsonNode.Parse("[null]")),
            "null-job-template" => MutateJson(text, static root => root["jobTemplates"] = JsonNode.Parse("[null]")),
            "null-build-template" => MutateJson(text, static root => root["buildTemplates"] = JsonNode.Parse("[null]")),
            "null-object-style" => MutateJson(
                text,
                static root => root["viewSettings"]!["objectStyles"] = JsonNode.Parse("[null]")),
            "non-finite-style-scale" => text.Replace(
                "\"objectStyles\": []",
                """
                "objectStyles": [
                  {
                    "objectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e61",
                    "scale": 1e400
                  }
                ]
                """,
                StringComparison.Ordinal),
            "non-finite-additional-style" => text.Replace(
                "\"objectStyles\": []",
                """
                "objectStyles": [
                  {
                    "objectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e61",
                    "weird": 1e400
                  }
                ]
                """,
                StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unknown mutation."),
        };
    }

    private static string MutateJson(string text, Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(text)!.AsObject();
        mutation(root);
        return root.ToJsonString();
    }

    private static ProjectModel CreateProjectWithStyleScale(double scale)
    {
        var project = CreateProject();
        var style = new ObjectStyle(
            Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f"),
            true,
            "#ffffff",
            "standard",
            "high",
            scale,
            false,
            []);
        return project with
        {
            ViewSettings = new ViewSettings(
                project.ViewSettings.Camera,
                project.ViewSettings.RenderBox,
                null,
                [style]),
        };
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mocassin.slnx")))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static ProjectModel CreateProject(string name = "Ceria")
    {
        return new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f"),
                new string('a', 64),
                name,
                null,
                now,
                now),
            new ModelDefinition(),
            [],
            [],
            [],
            ViewSettings.Default);
    }

    private static ProjectModel CreateProjectWithUnknownReference()
    {
        var project = CreateProject();
        var build = new BuildTemplate(
            BuildTemplateId.New(),
            "Broken build",
            JobTemplateId.New(),
            ParameterizationId.New(),
            "broken");
        return project with { BuildTemplates = [build] };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcnow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcnow;
    }

    private sealed class ThrowingFileWriter : IProjectFileWriter
    {
        public Task WriteAtomicallyAsync(
            string targetPath,
            byte[] content,
            bool createBackup,
            CancellationToken cancellationToken) =>
            Task.FromException(new IOException("Injected write failure."));
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
