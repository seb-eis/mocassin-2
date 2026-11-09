// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Tests.TestSupport;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Symmetry.Tests.Transitions;

[Trait("Tier", "PR-medium")]
public sealed class TransitionPathPropertyTests
{
    [Property(MaxTest = 100)]
    public bool ResolverMatchesBruteForceNearestShift(int seed)
    {
        var model = TransitionTestModels.CreateModel();
        if (!TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _) ||
            resolver is null)
        {
            return false;
        }

        var random = new Random(seed);
        var sourceSites = model.Structure.Sites
            .Where(static site => site.Stability == Mocassin.Core.Model.Structure.SiteStability.Stable)
            .Take(3)
            .ToArray();
        if (sourceSites.Length < 3)
        {
            return false;
        }

        var points = new Fractional3[3];
        for (var index = 0; index < points.Length; index++)
        {
            var source = sourceSites[index].Fractional;
            var offsetA = random.Next(-2, 3);
            var offsetB = random.Next(-2, 3);
            var offsetC = random.Next(-2, 3);
            var perturbation = ((random.NextDouble() - 0.5) * 8e-7);
            points[index] = new Fractional3(
                source.A + offsetA + perturbation,
                source.B + offsetB + perturbation,
                source.C + offsetC + perturbation);
        }

        var transition = TransitionTestModels.KineticTransitionWith(model, points);
        var resolved = resolver.TryResolve(transition, out var diagnostics);
        if (resolved is null)
        {
            return false;
        }

        for (var index = 0; index < points.Length; index++)
        {
            if (!TryBruteForceMatch(resolver.Encoder, points[index], out var expectedEncoding))
            {
                return false;
            }

            if (resolved.Positions[index].Encoding != expectedEncoding)
            {
                return false;
            }
        }

        return diagnostics.IsEmpty;
    }

    private static bool TryBruteForceMatch(
        UnitCellVectorEncoder encoder,
        Fractional3 point,
        out Vector4I encoding)
    {
        var matches = new List<Vector4I>();
        for (var p = 0; p < encoder.PositionCount; p++)
        {
            var canonical = encoder.Positions[p];
            for (var a = -5; a <= 5; a++)
            {
                for (var b = -5; b <= 5; b++)
                {
                    for (var c = -5; c <= 5; c++)
                    {
                        if (Math.Abs(point.A - (canonical.A + a)) <= GeometryTolerances.PathMatchTolerance &&
                            Math.Abs(point.B - (canonical.B + b)) <= GeometryTolerances.PathMatchTolerance &&
                            Math.Abs(point.C - (canonical.C + c)) <= GeometryTolerances.PathMatchTolerance)
                        {
                            matches.Add(new Vector4I(a, b, c, p));
                        }
                    }
                }
            }
        }

        if (matches.Count != 1)
        {
            encoding = default;
            return false;
        }

        encoding = matches[0];
        return true;
    }
}
