// SPDX-License-Identifier: MIT

using System.Reflection;
using Mocassin.Compilation.Diagnostics;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Validation;
using Mocassin.Symmetry.Diagnostics;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-fast")]
public sealed class DiagnosticCatalogTests
{
    [Fact]
    public void DefaultIsTheUnionOfTheDomainCatalogsAndStep07Codes()
    {
        var expected = CoreDiagnosticCatalog.Descriptors
            .Concat(SymmetryDiagnosticCatalog.Descriptors)
            .Concat(CompilationDiagnosticCatalog.Descriptors)
            .Select(static descriptor => descriptor.Code)
            .Concat(Step07Codes())
            .Order(StringComparer.Ordinal)
            .ToArray();
        var actual = DiagnosticCatalog.Default.All.Select(static descriptor => descriptor.Code).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EveryRegisteredCodeResolves()
    {
        foreach (var descriptor in DiagnosticCatalog.Default.All)
        {
            Assert.True(DiagnosticCatalog.Default.TryGet(descriptor.Code, out var resolved));
            Assert.Equal(descriptor.Code, resolved.Code);
        }

        Assert.False(DiagnosticCatalog.Default.TryGet("ZZZ999", out _));
    }

    [Fact]
    public void DefaultIsSortedAndUnique()
    {
        var codes = DiagnosticCatalog.Default.All.Select(static descriptor => descriptor.Code).ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
    }

    [Fact]
    public void Step07CodesAreRegisteredWithTheirDocumentedSeverity()
    {
        Assert.Equal(
            DiagnosticSeverity.Warning,
            Descriptor(Step07DiagnosticCodes.KmcSimulationWithoutMobileStart).DefaultSeverity);
        Assert.Equal(
            DiagnosticSeverity.Warning,
            Descriptor(Step07DiagnosticCodes.NoMobileStartParticle).DefaultSeverity);
        Assert.Equal(
            DiagnosticSeverity.Error,
            Descriptor(Step07DiagnosticCodes.EstimatedRuleCountExceeded).DefaultSeverity);
        Assert.Equal(
            DiagnosticSeverity.Error,
            Descriptor(Step07DiagnosticCodes.EstimatedMemoryExceeded).DefaultSeverity);
    }

    private static DiagnosticDescriptor Descriptor(string code)
    {
        Assert.True(DiagnosticCatalog.Default.TryGet(code, out var descriptor));
        return descriptor;
    }

    private static IEnumerable<string> Step07Codes() =>
        typeof(Step07DiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!);
}
