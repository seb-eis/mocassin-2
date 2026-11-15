// SPDX-License-Identifier: MIT

using Mocassin.Contracts.Packages;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Contracts.Tests.Packages;

/// <summary>
/// Verifies the Contracts-owned <c>PKG</c> diagnostic descriptors.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class PackageDiagnosticDescriptorsTests
{
    [Fact]
    public void DescriptorSetContainsPkg001ToPkg010()
    {
        var descriptors = PackageDiagnosticDescriptors.All;

        Assert.Equal(10, descriptors.Length);
        Assert.Equal(
            Enumerable.Range(1, 10).Select(static index => $"PKG{index:000}"),
            descriptors.Select(static descriptor => descriptor.Code));
    }

    [Fact]
    public void OnlyTheAdvisorySizeCodeIsAWarning()
    {
        foreach (var descriptor in PackageDiagnosticDescriptors.All)
        {
            var expected = descriptor.Code == PackageDiagnosticCodes.AdvisorySize
                ? DiagnosticSeverity.Warning
                : DiagnosticSeverity.Error;
            Assert.Equal(expected, descriptor.DefaultSeverity);
        }
    }

    [Fact]
    public void DescriptorsCarryOwnerTitleHelpAndAnchor()
    {
        foreach (var descriptor in PackageDiagnosticDescriptors.All)
        {
            Assert.Equal("Step 11 — simulation package", descriptor.Owner);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Title));
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Help));
            Assert.Equal(
                "#" + descriptor.Code.ToLowerInvariant(),
                descriptor.DocumentationAnchor);
        }
    }

    [Fact]
    public void CodesMatchTheDescriptorSet()
    {
        Assert.Equal(
            [
                PackageDiagnosticCodes.UnreadableDatabase,
                PackageDiagnosticCodes.UnsupportedSchema,
                PackageDiagnosticCodes.MissingSchemaElement,
                PackageDiagnosticCodes.InvalidPayload,
                PackageDiagnosticCodes.SignatureMismatch,
                PackageDiagnosticCodes.InvalidReference,
                PackageDiagnosticCodes.ColumnMismatch,
                PackageDiagnosticCodes.NotImmutable,
                PackageDiagnosticCodes.AdvisorySize,
                PackageDiagnosticCodes.OutOfBounds,
            ],
            PackageDiagnosticDescriptors.All.Select(static descriptor => descriptor.Code));
        Assert.Equal(10, PackageDiagnosticDescriptors.All.Select(static descriptor => descriptor.Code).Distinct().Count());
    }

    [Fact]
    public void DescriptorsAreImmutableAndOrdinalSorted()
    {
        var descriptors = PackageDiagnosticDescriptors.All;

        Assert.True(descriptors is { IsDefault: false });
        Assert.Equal(
            descriptors.OrderBy(static descriptor => descriptor.Code, StringComparer.Ordinal),
            descriptors);
    }
}
