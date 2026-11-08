// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests;

[Trait("Tier", "PR-fast")]
public sealed class ProjectFormatTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-02T12:00:00Z",
        CultureInfo.InvariantCulture);

    private static readonly Guid projectId = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f");
    private static readonly Guid parameterizationIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e70");
    private static readonly Guid jobTemplateIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e71");
    private static readonly Guid buildTemplateIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e72");

    [Fact]
    public void RoundTripPreservesCanonicalDocument()
    {
        var project = CreateProject();
        var json = ProjectJson.Serialize(project);
        var loaded = ProjectJson.Deserialize(json);

        Assert.Equal(ProjectCanonicalizer.ToCanonicalJson(project), ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Equal(project.Metadata, loaded.Metadata);
        Assert.Equal(project.Parameterizations.Single().Id, loaded.Parameterizations.Single().Id);
        Assert.Equal("kept", loaded.ViewSettings.ObjectStyles.Single().AdditionalProperties.Single().Value.StringValue);
    }

    [Fact]
    public void CanonicalJsonIsIndependentOfInputOrder()
    {
        var first = CreateProject();
        var second = first with
        {
            Parameterizations = [.. first.Parameterizations.Reverse()],
            JobTemplates = [.. first.JobTemplates.Reverse()],
            BuildTemplates = [.. first.BuildTemplates.Reverse()],
        };

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(first),
            ProjectCanonicalizer.ToCanonicalJson(second));
    }

    [Fact]
    public void CanonicalJsonHasExpectedEnvelopeAndFormatting()
    {
        var project = CreateProject();
        var json = ProjectCanonicalizer.ToCanonicalJson(project);

        Assert.DoesNotContain('\n', json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(
            [
                "$schema",
                "schemaVersion",
                "projectId",
                "masterSeed",
                "name",
                "description",
                "author",
                "notes",
                "createdUtc",
                "modifiedUtc",
                "model",
                "parameterizations",
                "jobTemplates",
                "buildTemplates",
                "viewSettings",
            ],
            root.EnumerateObject().Select(static property => property.Name));
        Assert.Equal(ProjectSchema.SchemaUri, root.GetProperty("$schema").GetString());
        Assert.Equal(ProjectSchema.CurrentVersion, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(projectId.ToString("D"), root.GetProperty("projectId").GetString());
        Assert.Equal("2026-11-02T12:00:00.0000000Z", root.GetProperty("createdUtc").GetString());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("model").ValueKind);
        Assert.Equal("perspective", root.GetProperty("viewSettings").GetProperty("camera").GetProperty("projection").GetString());
        Assert.Equal("kept", root.GetProperty("viewSettings").GetProperty("objectStyles")[0].GetProperty("customKey").GetString());
    }

    [Fact]
    public void FileSerializationUsesUtf8WithoutBomAndTwoSpaceIndentation()
    {
        var bytes = ProjectJson.SerializeToUtf8Bytes(CreateProject());

        Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.StartsWith("{\n  \"$schema\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownTopLevelPropertyIsRejected()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["unexpected"] = 1;

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
    }

    [Fact]
    public void FutureSchemaVersionIsRejected()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["schemaVersion"] = ProjectSchema.CurrentVersion + 1;

        var exception = Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
        Assert.Contains("newer than the supported version", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidUuidIsRejected()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["projectId"] = "not-a-uuid";

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
    }

    [Fact]
    public void ModelHashIgnoresMetadataButDocumentHashDoesNot()
    {
        var first = CreateProject();
        var second = first with
        {
            Metadata = first.Metadata with { Name = "Ceria copy" },
        };

        Assert.Equal(ProjectHasher.Compute(first), ProjectHasher.Compute(second));
        Assert.NotEqual(ProjectHasher.ComputeDocument(first), ProjectHasher.ComputeDocument(second));
    }

    [Fact]
    public void MigrationAcceptsCurrentVersionAsNoOp()
    {
        var document = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!;
        var result = ProjectMigrations.Apply(document, ProjectSchema.CurrentVersion);

        Assert.Equal(ProjectSchema.CurrentVersion, result.Version);
        Assert.Empty(result.Diagnostics);
        Assert.Same(document, result.Document);
    }

    [Fact]
    public void MigrationRejectsFutureVersion()
    {
        var document = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!;
        var result = ProjectMigrations.Apply(document, ProjectSchema.CurrentVersion + 1);

        Assert.Equal(ProjectSchema.CurrentVersion + 1, result.Version);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.FutureSchemaVersion);
    }

    [Fact]
    public void ValidatorAcceptsSampleProject()
    {
        Assert.Empty(ProjectValidator.Validate(CreateProject()));
    }

    [Fact]
    public void ValidatorReportsUnknownReference()
    {
        var project = CreateProject();
        var build = project.BuildTemplates.Single() with { JobTemplateId = JobTemplateId.Create(Guid.NewGuid()) };
        var invalid = project with { BuildTemplates = [build] };

        var diagnostics = ProjectValidator.Validate(invalid);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference &&
                diagnostic.Path?.Property == "jobTemplateId");
    }

    [Fact]
    public void ValidatorReportsWrongDomainReference()
    {
        var project = CreateProject();
        var build = project.BuildTemplates.Single() with
        {
            JobTemplateId = JobTemplateId.Create(parameterizationIdValue),
        };
        var invalid = project with { BuildTemplates = [build] };

        var diagnostics = ProjectValidator.Validate(invalid);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.WrongDomainReference &&
                diagnostic.Path?.Property == "jobTemplateId");
    }

    [Fact]
    public void ValidatorReportsDuplicateIds()
    {
        var project = CreateProject();
        var duplicate = project.JobTemplates.Single() with { Name = "Duplicate" };
        var invalid = project with { JobTemplates = [.. project.JobTemplates, duplicate] };

        var diagnostics = ProjectValidator.Validate(invalid);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.DuplicateId &&
                diagnostic.Path?.Domain == "jobTemplate");
    }

    [Fact]
    public void ValidatorReportsInvalidMasterSeed()
    {
        var project = CreateProject();
        var invalid = project with
        {
            Metadata = project.Metadata with { MasterSeed = "not-a-master-seed" },
        };

        var diagnostics = ProjectValidator.Validate(invalid);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidMetadata &&
                diagnostic.Path?.Property == "masterSeed");
    }

    [Fact]
    public void NegativeZeroInViewVectorsProducesIdenticalCanonicalBytesAndHash()
    {
        var project = CreateProject();
        var positive = project with
        {
            ViewSettings = new ViewSettings(
                new CameraSettings(
                    [0.0, 0.0, 0.0],
                    [0.0, 0.0, 0.0],
                    [0.0, 0.0, 0.0],
                    CameraProjection.Perspective,
                    45.0,
                    0.1,
                    1000.0),
                new RenderBoxSettings([0.0, 0.0, 0.0], [0.0, 0.0, 0.0], true),
                null,
                []),
        };
        var negative = project with
        {
            ViewSettings = new ViewSettings(
                new CameraSettings(
                    [-0.0, -0.0, -0.0],
                    [-0.0, -0.0, -0.0],
                    [-0.0, -0.0, -0.0],
                    CameraProjection.Perspective,
                    45.0,
                    0.1,
                    1000.0),
                new RenderBoxSettings([-0.0, -0.0, -0.0], [-0.0, -0.0, -0.0], true),
                null,
                []),
        };

        var positiveJson = ProjectCanonicalizer.ToCanonicalJson(positive);
        var negativeJson = ProjectCanonicalizer.ToCanonicalJson(negative);

        Assert.Equal(positiveJson, negativeJson);
        Assert.Equal(ProjectHasher.ComputeDocument(positive), ProjectHasher.ComputeDocument(negative));
        Assert.DoesNotContain("[-0", negativeJson, StringComparison.Ordinal);
        Assert.DoesNotContain(",-0", negativeJson, StringComparison.Ordinal);
        Assert.DoesNotContain(":-0", negativeJson, StringComparison.Ordinal);
    }

    [Fact]
    public void NumericEnumStringsAreRejected()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["viewSettings"]!["camera"]!["projection"] = "1";

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
    }

    [Fact]
    public void PublicDeserializeWrapsMissingCameraAsJsonException()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["viewSettings"]!.AsObject().Remove("camera");
        var json = root.ToJsonString();

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(json));
        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void PublicDeserializeWrapsNonFiniteStyleValueAsJsonException()
    {
        var json = ProjectJson.Serialize(CreateProject()).Replace(
            "\"customKey\": \"kept\"",
            "\"customKey\": 1e400",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(json));
        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void MissingOptionalViewFieldsUseDocumentedDefaults()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        var view = root["viewSettings"]!.AsObject();
        view["camera"]!.AsObject().Remove("projection");
        view["renderBox"]!.AsObject().Remove("visible");
        view["objectStyles"] = new JsonArray(
            new JsonObject
            {
                ["objectId"] = projectId.ToString("D"),
            });

        var loaded = ProjectJson.Deserialize(root.ToJsonString());
        var style = loaded.ViewSettings.ObjectStyles.Single();

        Assert.Equal(CameraProjection.Perspective, loaded.ViewSettings.Camera.Projection);
        Assert.False(loaded.ViewSettings.RenderBox.Visible);
        Assert.False(style.Visible);
        Assert.Equal(1.0, style.Scale);
        Assert.False(style.Wireframe);
    }

    [Fact]
    public void ValidatorRejectsNonFiniteStyleScale()
    {
        var project = CreateProject();
        var style = new ObjectStyle(
            project.ViewSettings.ObjectStyles.Single().ObjectId,
            true,
            "#ffffff",
            "standard",
            "high",
            double.PositiveInfinity,
            false,
            []);
        var invalid = project with
        {
            ViewSettings = new ViewSettings(
                project.ViewSettings.Camera,
                project.ViewSettings.RenderBox,
                project.ViewSettings.Background,
                [style]),
        };

        var diagnostics = ProjectValidator.Validate(invalid);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidViewSettings &&
                diagnostic.Path?.Property == "scale");
    }

    [Fact]
    public void DuplicateIdProducesOneDiagnostic()
    {
        var project = CreateProject();
        var duplicate = project.JobTemplates.Single() with { Name = "Duplicate" };
        var invalid = project with { JobTemplates = [.. project.JobTemplates, duplicate] };

        var diagnostics = ProjectValidator.Validate(invalid);

        Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.DuplicateId));
    }

    [Fact]
    public void AdditionalStyleKeysSurviveRoundTrip()
    {
        var project = CreateProject();
        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));
        var loadedStyle = loaded.ViewSettings.ObjectStyles.Single();

        Assert.Equal("kept", loadedStyle.AdditionalProperties.Single().Value.StringValue);
        Assert.Equal("customKey", loadedStyle.AdditionalProperties.Single().Key);
    }

    private static ProjectModel CreateProject()
    {
        var parameterization = new ParameterizationTemplate(
            ParameterizationId.Create(parameterizationIdValue),
            "Ceria parameters",
            "sha256:0123456789abcdef",
            createdUtc);
        var job = new JobTemplate(JobTemplateId.Create(jobTemplateIdValue), "KMC temperature series");
        var build = new BuildTemplate(
            BuildTemplateId.Create(buildTemplateIdValue),
            "KMC build",
            job.Id,
            parameterization.Id,
            "ceria-kmc");
        var style = new ObjectStyle(
            projectId,
            true,
            "#ffffff",
            "standard",
            "high",
            1.0,
            false,
            [new StyleProperty("customKey", StyleValue.FromString("kept"))]);
        var viewSettings = new ViewSettings(
            ViewSettings.Default.Camera,
            ViewSettings.Default.RenderBox,
            "dark",
            [style]);

        return new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                projectId,
                new string('a', 64),
                "Ceria",
                "Reduced ceria",
                createdUtc,
                createdUtc.AddMinutes(30),
                "Author",
                "Notes"),
            new ModelDefinition(),
            [parameterization],
            [job],
            [build],
            viewSettings);
    }
}
