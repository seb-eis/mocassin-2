// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;

namespace Mocassin.Core.Model.Transitions;

internal static partial class TransitionModelValidator
{
    private static void ValidateModelTransitions(
        ModelDefinition model,
        Dictionary<ParticleId, Particle> particleById,
        DiagnosticBag bag)
    {
        var groups = model.StateChangeGroups.IsDefault
            ? ImmutableArray<StateChangeGroup>.Empty
            : model.StateChangeGroups;
        var chains = model.Chains.IsDefault ? ImmutableArray<TransitionChain>.Empty : model.Chains;

        ValidateChains(chains, groups, bag);
    }
}
