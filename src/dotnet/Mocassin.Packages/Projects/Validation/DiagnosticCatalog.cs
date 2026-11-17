// SPDX-License-Identifier: MIT

using System.Collections.Frozen;
using System.Collections.Immutable;
using Mocassin.Compilation.Diagnostics;
using Mocassin.Contracts.Cli;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Validation.Diagnostics;
using Mocassin.Symmetry.Diagnostics;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// A registry over the stable diagnostic codes and their descriptors.
/// </summary>
/// <remarks>
/// The catalog is a static lookup. It does not produce diagnostics; rules keep using their stable code
/// constants. The default composition is added together with the domain catalogs in the diagnostic-catalog
/// feature.
/// </remarks>
public sealed class DiagnosticCatalog
{
    private readonly FrozenDictionary<string, DiagnosticDescriptor> descriptors;

    /// <summary>
    /// Initializes a catalog from a set of descriptors.
    /// </summary>
    /// <param name="descriptors">The descriptors to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="descriptors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A descriptor code occurs more than once.</exception>
    public DiagnosticCatalog(IEnumerable<DiagnosticDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var all = descriptors
            .OrderBy(static descriptor => descriptor.Code, StringComparer.Ordinal)
            .ToImmutableArray();
        var map = new Dictionary<string, DiagnosticDescriptor>(StringComparer.Ordinal);
        foreach (var descriptor in all)
        {
            if (!map.TryAdd(descriptor.Code, descriptor))
            {
                throw new ArgumentException($"Duplicate diagnostic code '{descriptor.Code}'.", nameof(descriptors));
            }
        }

        All = all;
        this.descriptors = map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the default catalog, composed from the Core, Symmetry, Compilation, package, Step 07, and CLI
    /// descriptors.
    /// </summary>
    public static DiagnosticCatalog Default { get; } = new(
        CoreDiagnosticCatalog.Descriptors
            .Concat(SymmetryDiagnosticCatalog.Descriptors)
            .Concat(CompilationDiagnosticCatalog.Descriptors)
            .Concat(PackageDiagnosticDescriptors.All)
            .Concat(Step07Descriptors.All)
            .Concat(CliDiagnosticDescriptors.All));

    /// <summary>
    /// Gets every registered descriptor, sorted by ordinal code.
    /// </summary>
    public ImmutableArray<DiagnosticDescriptor> All { get; }

    /// <summary>
    /// Tries to resolve a code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="descriptor">The resolved descriptor, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the code is registered.</returns>
    public bool TryGet(string code, out DiagnosticDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(code);
        return descriptors.TryGetValue(code, out descriptor!);
    }
}
