// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;

namespace Mocassin.Symmetry.Reduction;

/// <summary>
/// An immutable permutation of site indices.
/// </summary>
/// <remarks>
/// <see cref="Mapping"/> maps an input site index to its image. The identity is <c>[0..N-1]</c>.
/// </remarks>
public sealed record SitePermutation
{
    /// <summary>
    /// Initializes a permutation from an index mapping.
    /// </summary>
    /// <param name="mapping">The permutation mapping.</param>
    /// <exception cref="ArgumentException">The mapping is not a permutation of <c>0..N-1</c>.</exception>
    public SitePermutation(ImmutableArray<int> mapping)
    {
        if (mapping.IsDefault)
        {
            throw new ArgumentException("A permutation mapping is required.", nameof(mapping));
        }

        var seen = new bool[mapping.Length];
        foreach (var index in mapping)
        {
            if (index < 0 || index >= mapping.Length || seen[index])
            {
                throw new ArgumentException("The mapping must be a permutation of 0..N-1.", nameof(mapping));
            }

            seen[index] = true;
        }

        Mapping = mapping;
    }

    /// <summary>
    /// Gets the index mapping.
    /// </summary>
    public ImmutableArray<int> Mapping { get; }

    /// <summary>
    /// Gets the number of permuted sites.
    /// </summary>
    public int Count => Mapping.Length;

    /// <summary>
    /// Gets the image of a site index.
    /// </summary>
    /// <param name="index">The input site index.</param>
    /// <returns>The image index.</returns>
    public int this[int index] => Mapping[index];

    /// <summary>
    /// Determines whether this is the identity permutation.
    /// </summary>
    public bool IsIdentity
    {
        get
        {
            for (var index = 0; index < Mapping.Length; index++)
            {
                if (Mapping[index] != index)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Creates the identity permutation for a site count.
    /// </summary>
    /// <param name="count">The number of sites.</param>
    /// <returns>The identity permutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static SitePermutation Identity(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var mapping = ImmutableArray.CreateBuilder<int>(count);
        for (var index = 0; index < count; index++)
        {
            mapping.Add(index);
        }

        return new SitePermutation(mapping.MoveToImmutable());
    }

    /// <summary>
    /// Compares permutations by their mapping values.
    /// </summary>
    /// <param name="other">The other permutation.</param>
    /// <returns><see langword="true"/> when the mappings are equal.</returns>
    public bool Equals(SitePermutation? other) => other is not null && Mapping.SequenceEqual(other.Mapping);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var index in Mapping)
        {
            hash.Add(index);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{string.Join(", ", Mapping)}]");
}
