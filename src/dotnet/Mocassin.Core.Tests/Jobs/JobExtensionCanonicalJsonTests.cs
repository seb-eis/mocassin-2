// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Model.Jobs;

namespace Mocassin.Core.Tests.Jobs;

/// <summary>
/// Pins the canonical extension-config JSON used by the package job payload.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class JobExtensionCanonicalJsonTests
{
    [Fact]
    public void KeysAreSortedAndAuthoredNumberTokensArePreserved()
    {
        using var document = JsonDocument.Parse(
            """{"z":1,"n":123456789012345678901234567890,"a":1.0,"nested":{"b":2,"a":1}}""");
        var extension = new JobExtension("ext", document.RootElement.Clone());

        var json = extension.ToCanonicalJson();

        Assert.Equal(
            """{"a":1.0,"n":123456789012345678901234567890,"nested":{"a":1,"b":2},"z":1}""",
            json);
    }

    [Fact]
    public void ArrayOrderIsPreservedAndValuesRoundTrip()
    {
        using var document = JsonDocument.Parse("""{"values":[3,1,2],"text":"x","flag":true}""");
        var extension = new JobExtension("ext", document.RootElement.Clone());

        Assert.Equal("""{"flag":true,"text":"x","values":[3,1,2]}""", extension.ToCanonicalJson());
    }
}
