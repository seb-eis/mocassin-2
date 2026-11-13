// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Tests.TestSupport;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Symmetry.Tests.Transitions;

/// <summary>
/// Covers the additive public point-level resolution boundary.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class TransitionPathPointTests
{
    [Fact]
    public void CanonicalPointResolvesToItsPositionAndOwningSite()
    {
        var model = TransitionTestModels.CreateModel();
        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));

        Assert.True(resolver!.TryResolvePoint(new Fractional3(0.25, 0.25, 0.25), out var position, out var diagnostics));

        Assert.Empty(diagnostics);
        Assert.NotNull(position);
        Assert.Equal(TransitionTestModels.SiteId(TransitionTestModels.Site2), position.SiteId);
        Assert.Equal(new Vector4I(0, 0, 0, position.P), position.Encoding);
    }

    [Fact]
    public void NonFinitePointIsRejectedWithoutThrowing()
    {
        var model = TransitionTestModels.CreateModel();
        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));

        Assert.False(resolver!.TryResolvePoint(new Fractional3(double.NaN, 0.0, 0.0), out var position, out var diagnostics));

        Assert.Null(position);
        Assert.NotEmpty(diagnostics);
    }
}
