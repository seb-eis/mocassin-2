// SPDX-License-Identifier: MIT

using System.Reflection;

namespace Mocassin.Symmetry.Tests.Diagnostics;

[Trait("Tier", "PR-fast")]
public sealed class SymmetryDiagnosticCatalogTests
{
    [Fact]
    public void EverySymmetryCodeIsRegistered()
    {
        var registered = SymmetryDiagnosticCatalog.Descriptors
            .Select(static descriptor => descriptor.Code)
            .ToHashSet(StringComparer.Ordinal);

        var declared = new[] { typeof(StructureDiagnosticCodes), typeof(SymmetryDiagnosticCodes) }
            .SelectMany(static type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(declared);
        Assert.DoesNotContain(declared, code => !registered.Contains(code));
    }

    [Fact]
    public void CatalogIsSortedAndUnique()
    {
        var codes = SymmetryDiagnosticCatalog.Descriptors.Select(static descriptor => descriptor.Code).ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
    }
}
