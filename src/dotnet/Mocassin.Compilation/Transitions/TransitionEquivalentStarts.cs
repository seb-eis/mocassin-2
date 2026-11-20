// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Context;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Compilation.Transitions;

/// <summary>
/// Detects expanded transition paths that are the same jump direction modulo a pure lattice translation of the
/// centered space-group setting.
/// </summary>
internal static class TransitionEquivalentStarts
{
    private const double translationEquivalenceTolerance = 1e-9;

    /// <summary>
    /// Determines whether two expanded paths are the same jump direction modulo a pure lattice translation
    /// of the centered space-group setting.
    /// </summary>
    /// <remarks>
    /// The rule is exact and conservative:
    /// <list type="number">
    /// <item>the fractional displacement sequence <c>x(i) - x(0)</c> must match (this is what makes the
    /// relative jump geometry identical, including its periodic image);</item>
    /// <item>the start positions must be related by an operation of the setting whose linear part is the
    /// identity, i.e. a centering/lattice translation.</item>
    /// </list>
    /// A path that cannot be proven equivalent stays a distinct direction; equivalent starts are never inferred
    /// from a rotational or mirror image, because that would change the relative path and therefore the jump
    /// direction in the P1 frame.
    /// </remarks>
    internal static bool IsEquivalent(
        CompilationContext context,
        DirectionDraft existing,
        Vector4I[] absolute,
        ImmutableArray<SymmetryOperation> pureTranslations)
    {
        if (existing.Absolute.Length != absolute.Length)
        {
            return false;
        }

        var encoder = context.Encoder!;
        var existingStart = encoder.Decode(existing.Absolute[0]);
        var candidateStart = encoder.Decode(absolute[0]);
        for (var node = 0; node < absolute.Length; node++)
        {
            var existingDisplacement = Displacement(encoder.Decode(existing.Absolute[node]), existingStart);
            var candidateDisplacement = Displacement(encoder.Decode(absolute[node]), candidateStart);
            if (!NearlyEqual(existingDisplacement, candidateDisplacement))
            {
                return false;
            }
        }

        foreach (var operation in pureTranslations)
        {
            var image = operation.Transform(existingStart);
            if (encoder.TryIndexOf(image, out var mapped) && mapped == absolute[0].P)
            {
                return true;
            }
        }

        return false;
    }

    private static Fractional3 Displacement(Fractional3 point, Fractional3 start) =>
        new(point.A - start.A, point.B - start.B, point.C - start.C);

    private static bool NearlyEqual(Fractional3 left, Fractional3 right) =>
        Math.Abs(left.A - right.A) <= translationEquivalenceTolerance &&
        Math.Abs(left.B - right.B) <= translationEquivalenceTolerance &&
        Math.Abs(left.C - right.C) <= translationEquivalenceTolerance;

    /// <summary>
    /// One raw forward expansion kept for reverse-direction generation.
    /// </summary>
    /// <param name="Representative">The merged direction that owns the expansion.</param>
    /// <param name="Absolute">The expanded absolute encodings in path order.</param>
    /// <param name="Stability">The per-node stability sequence.</param>
    internal readonly record struct RawForwardPath(
        DirectionDraft Representative,
        Vector4I[] Absolute,
        SiteStability[] Stability);
}
