// SPDX-License-Identifier: MIT

using System.Reflection;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;

namespace Mocassin.Core.Tests.Diagnostics;

[Trait("Tier", "PR-fast")]
public sealed class CoreDiagnosticCatalogTests
{
    private static readonly Assembly coreAssembly = typeof(Diagnostic).Assembly;

    [Fact]
    public void EveryCoreCodeIsRegistered()
    {
        var registered = CoreDiagnosticCatalog.Descriptors
            .Select(static descriptor => descriptor.Code)
            .ToHashSet(StringComparer.Ordinal);

        var declared = DeclaredCodes();
        Assert.NotEmpty(declared);
        Assert.DoesNotContain(declared, code => !registered.Contains(code));
    }

    [Fact]
    public void CatalogHasNoDuplicateOrMissingCodes()
    {
        var codes = CoreDiagnosticCatalog.Descriptors.Select(static descriptor => descriptor.Code).ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
        Assert.All(
            CoreDiagnosticCatalog.Descriptors,
            static descriptor =>
            {
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Title));
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Help));
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Owner));
                Assert.StartsWith("#", descriptor.DocumentationAnchor, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void ProjectEnvelopeCodesAreRegistered()
    {
        var registered = CoreDiagnosticCatalog.Descriptors
            .Select(static descriptor => descriptor.Code)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(ProjectDiagnosticCodes.InvalidDocument, registered);
        Assert.Contains(ProjectDiagnosticCodes.SymmetryMetadataMismatch, registered);
    }

    private static HashSet<string> DeclaredCodes()
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in coreAssembly.GetTypes().Where(static type =>
                     type.IsAbstract && type.IsSealed &&
                     (type.Namespace == "Mocassin.Core.Diagnostics" || type.Namespace == "Mocassin.Core.Project") &&
                     type.Name.EndsWith("DiagnosticCodes", StringComparison.Ordinal)))
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string) && field.GetRawConstantValue() is string code)
                {
                    codes.Add(code);
                }
            }
        }

        return codes;
    }
}
