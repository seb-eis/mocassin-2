// SPDX-License-Identifier: MIT

using Mocassin.Core.Project;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Symmetry.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionSampleTests
{
    private static readonly string[] sampleFiles =
    [
        "ceria-kmc.mocprj",
        "ceria-mmc.mocprj",
        "interstitialcy5.mocprj",
        "polaron-hop.mocprj",
        "vehicle5.mocprj",
    ];

    [Theory]
    [MemberData(nameof(Samples))]
    public void TransitionSampleValidatesAndPreviews(string fileName)
    {
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine("samples", "transitions", fileName)));
        var pathValidator = new TransitionPathValidator();
        var previewService = new TransitionPreviewService();

        Assert.Empty(ProjectValidator.Validate(project));
        Assert.Empty(pathValidator.Validate(project.Model));

        foreach (var transition in project.Model.KineticTransitions)
        {
            var preview = previewService.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);
            Assert.Empty(diagnostics);
            Assert.NotNull(preview);
        }

        foreach (var transition in project.Model.MetropolisTransitions)
        {
            var preview = previewService.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);
            Assert.Empty(diagnostics);
            Assert.NotNull(preview);
        }
    }

    public static TheoryData<string> Samples => [.. sampleFiles];
}
