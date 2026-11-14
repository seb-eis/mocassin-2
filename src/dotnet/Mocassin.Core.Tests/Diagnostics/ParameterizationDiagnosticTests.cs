// SPDX-License-Identifier: MIT

using System.Reflection;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Core.Tests.Diagnostics;

[Trait("Tier", "PR-fast")]
public sealed class ParameterizationDiagnosticTests
{
    [Fact]
    public void CatalogRegistersEveryDeclaredStructuralCodeExactlyOnce()
    {
        var declared = typeof(ParameterizationDiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var registered = ParameterizationDiagnosticCatalog.Descriptors
            .Select(static descriptor => descriptor.Code)
            .ToArray();

        Assert.Equal(declared, registered);
        Assert.Equal(registered.Length, registered.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("PRM007", registered);
    }

    [Fact]
    public void EveryDescriptorIsCompleteAndDocumented()
    {
        Assert.All(
            ParameterizationDiagnosticCatalog.Descriptors,
            static descriptor =>
            {
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Title));
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Help));
                Assert.Equal("Step 09 — parameterization templates", descriptor.Owner);
                Assert.StartsWith("#prm", descriptor.DocumentationAnchor, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void ReferencePageDocumentsEveryStructuralCode()
    {
        var text = File.ReadAllText(
            RepositoryPaths.Combine("docs", "reference", "diagnostics.md"));

        Assert.All(
            ParameterizationDiagnosticCatalog.Descriptors,
            descriptor =>
            {
                Assert.Contains($"### {descriptor.Code}", text, StringComparison.Ordinal);
                Assert.Contains($"[`{descriptor.Code}`](#{descriptor.DocumentationAnchor.TrimStart('#')})", text, StringComparison.Ordinal);
            });
    }
}
