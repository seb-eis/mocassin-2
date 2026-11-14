// SPDX-License-Identifier: MIT

using Mocassin.Core.Model;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Verifies the defaults of <see cref="ParameterizationOptions"/>.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationOptionsTests
{
    [Fact]
    public void DefaultAppliedValueLimitUsesTheCoreConstant()
    {
        Assert.Equal(
            ParameterizationLimits.MaxAppliedValueBytes,
            ParameterizationOptions.Default.MaxAppliedValueBytes);
    }
}
