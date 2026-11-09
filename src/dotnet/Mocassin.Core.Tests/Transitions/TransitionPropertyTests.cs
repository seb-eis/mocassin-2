// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Transitions;

[Trait("Tier", "PR-medium")]
public sealed class TransitionPropertyTests
{
    [Property(MaxTest = 100)]
    public bool ShuffledTransitionCollectionsRoundTripCanonically(int seed)
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var random = new Random(seed);
        var shuffled = model with
        {
            StateChanges = [.. Shuffle(model.StateChanges, random)],
            StateChangeGroups = [.. Shuffle(model.StateChangeGroups, random)],
            Chains = [.. Shuffle(model.Chains, random)],
            KineticTransitions = [.. Shuffle(model.KineticTransitions, random)],
            MetropolisTransitions = [.. Shuffle(model.MetropolisTransitions, random)],
        };
        var project = TransitionTestModels.CreateProject(shuffled);
        var reloaded = ProjectJson.Deserialize(ProjectCanonicalizer.ToCanonicalUtf8(project));

        return string.Equals(
                ProjectCanonicalizer.ToCanonicalJson(project),
                ProjectCanonicalizer.ToCanonicalJson(reloaded),
                StringComparison.Ordinal)
            && string.Equals(
                ProjectHasher.Compute(TransitionTestModels.CreateProject(model)),
                ProjectHasher.Compute(project),
                StringComparison.Ordinal);
    }

    private static IEnumerable<T> Shuffle<T>(IEnumerable<T> values, Random random) =>
        values.OrderBy(_ => random.Next());
}
