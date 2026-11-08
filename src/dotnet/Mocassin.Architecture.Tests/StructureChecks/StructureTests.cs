// SPDX-License-Identifier: MIT

using Mocassin.Architecture.Tests.TestSupport;

namespace Mocassin.Architecture.Tests.StructureChecks;

public sealed class StructureTests
{
    [Fact]
    public void Structure_policy_is_satisfied()
    {
        var policy = StructurePolicy.Load(RepositoryPaths.Combine("tools", "structure-policy.json"));
        var violations = StructureChecker.Check(RepositoryPaths.Root, policy);

        Assert.True(
            violations.Count == 0,
            "The code structure policy is not satisfied:" + Environment.NewLine
            + string.Join(Environment.NewLine, violations.Select(violation => "  - " + violation)));
    }
}
