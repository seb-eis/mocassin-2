// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts.Cli;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Tests.CommandLine;

[Trait("Tier", "PR-fast")]
public sealed class ReportContractTests
{
    [Fact]
    public void EveryManifestReportHasAGeneratedSchema()
    {
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("contracts", "manifest.json")));
        var reports = manifest.RootElement.GetProperty("reports").EnumerateObject().ToArray();
        Assert.Equal(8, reports.Length);
        foreach (var report in reports)
        {
            var version = report.Value.GetProperty("schemaVersion").GetInt32();
            var path = RepositoryPaths.Combine(
                "contracts",
                "generated",
                "schema",
                $"{report.Name}-v{version}.schema.json");
            Assert.True(File.Exists(path), $"Missing generated schema for report '{report.Name}'.");

            using var schema = JsonDocument.Parse(File.ReadAllText(path));
            var required = schema.RootElement.GetProperty("required")
                .EnumerateArray()
                .Select(static item => item.GetString())
                .ToArray();
            Assert.Contains("schemaVersion", required);
            Assert.Contains("kind", required);
            Assert.Contains("exitCode", required);
            Assert.Contains("summary", required);
            Assert.Contains("diagnostics", required);
            if (report.Name == "validation")
            {
                Assert.Contains("valid", required);
            }
            else
            {
                Assert.Contains("ok", required);
            }
        }
    }

    [Fact]
    public void EnvelopeSerializesAsOneCamelCaseObjectWithDerivedOkAndSummary()
    {
        var envelope = ReportEnvelope.Create(
            1,
            "build",
            2,
            [CliDiagnosticFactory.ToReportDiagnostic(CliDiagnosticFactory.Usage("bad line"))]);
        var json = JsonReportWriter.Serialize(envelope);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("build", root.GetProperty("kind").GetString());
        Assert.False(root.GetProperty("ok").GetBoolean());
        Assert.Equal(2, root.GetProperty("exitCode").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("errors").GetInt32());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("warnings").GetInt32());
        var diagnostic = root.GetProperty("diagnostics")[0];
        Assert.Equal("CLI001", diagnostic.GetProperty("code").GetString());
        Assert.Equal("error", diagnostic.GetProperty("severity").GetString());
        Assert.Equal(JsonValueKind.Null, diagnostic.GetProperty("path").ValueKind);
        Assert.Equal(JsonValueKind.Array, diagnostic.GetProperty("related").ValueKind);
    }

    [Fact]
    public void EnvelopeIsOkExactlyWhenTheExitCodeIsZero()
    {
        Assert.True(ReportEnvelope.Create(1, "build", 0, []).Ok);
        Assert.False(ReportEnvelope.Create(1, "build", 1, []).Ok);
        Assert.False(ReportEnvelope.Create(1, "build", 10, []).Ok);
    }

    [Fact]
    public void HumanReportWriterPrintsTheSharedDiagnosticTable()
    {
        var envelope = ReportEnvelope.Create(
            1,
            "validate",
            1,
            [CliDiagnosticFactory.ToReportDiagnostic(CliDiagnosticFactory.Io("cannot read it"))]);
        using var writer = new StringWriter();

        HumanReportWriter.Write(writer, envelope);

        var text = writer.ToString();
        Assert.Contains("SEVERITY  CODE    MESSAGE", text, StringComparison.Ordinal);
        Assert.Contains("CLI002", text, StringComparison.Ordinal);
        Assert.Contains("Summary: 1 error(s), 0 warning(s), 0 info(s)", text, StringComparison.Ordinal);
        Assert.Contains("Result: failed (exit 1)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CliDescriptorsAreRegisteredInTheDefaultCatalog()
    {
        var catalog = Mocassin.Packages.Projects.Validation.DiagnosticCatalog.Default;
        foreach (var code in new[]
                 {
                     CliDiagnosticCodes.Usage,
                     CliDiagnosticCodes.Io,
                     CliDiagnosticCodes.OutputExists,
                     CliDiagnosticCodes.Selection,
                     CliDiagnosticCodes.JobSelection,
                     CliDiagnosticCodes.Cancelled,
                     CliDiagnosticCodes.Internal,
                     CliDiagnosticCodes.Reserved,
                     CliDiagnosticCodes.NotAPackage,
                 })
        {
            Assert.True(catalog.TryGet(code, out _), $"CLI code '{code}' is not in the default catalog.");
        }
    }

    [Fact]
    public void DiagnosticConversionPreservesTheValidationElementShape()
    {
        var path = new ModelPath("transitions", Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80"), "referencePath[1]");
        var diagnostic = new Diagnostic(
            "TRN012",
            DiagnosticSeverity.Warning,
            "Kinetic path position 1 must be unstable.",
            path,
            "Mark the site as unstable.",
            [new ModelPath("transitions", path.ObjectId, "referencePath[0]")]);

        var report = CliDiagnosticFactory.ToReportDiagnostic(diagnostic);

        Assert.Equal("TRN012", report.Code);
        Assert.Equal("warning", report.Severity);
        Assert.Equal("transitions", report.Path!.Domain);
        Assert.Equal("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80", report.Path.ObjectId);
        Assert.Equal("referencePath[1]", report.Path.Property);
        Assert.Single(report.Related);
    }
}
