// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;

namespace Mocassin.Core.Model.Transitions;

internal static partial class TransitionModelValidator
{
    private static partial void ValidateModelTransitions(
        ModelDefinition model,
        Dictionary<ParticleId, Particle> particleById,
        DiagnosticBag bag)
    {
        // Chain and transition rules are added by the chain and transition feature slices.
    }
}
