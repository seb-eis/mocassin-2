// SPDX-License-Identifier: MIT

using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps the real Step 08/09 <see cref="ParameterizedModel"/> to the persisted model payload.
/// </summary>
internal static class ModelPayloadMapper
{
    /// <summary>
    /// Maps the parameterized model to the contract payload.
    /// </summary>
    /// <param name="model">The compiled and parameterized model.</param>
    /// <returns>The model payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The compiled model and applied values disagree.</exception>
    public static ModelPayload Map(ParameterizedModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var compiled = model.CompiledModel;
        return new ModelPayload(
            PackageFormatConstants.ModelPayloadFormatVersion,
            model.Signature,
            compiled.ModelSignature,
            compiled.SymmetryContractVersion,
            compiled.SpaceGroupDataVersion,
            compiled.SpaceGroupDataSha256,
            MapParticles(compiled.Particles),
            StructurePayloadMapper.Map(compiled),
            EnergyPayloadMapper.Map(
                compiled.Energy,
                model.Values,
                compiled.Structure.PositionCount,
                compiled.Particles.Count),
            TransitionPayloadMapper.Map(compiled.Transitions, model.Values, compiled.Particles.Count),
            SimulationPayloadMapper.Map(compiled));
    }

    /// <summary>
    /// Rebuilds the dense applied values from the decoded payload.
    /// </summary>
    /// <param name="payload">The decoded model payload.</param>
    /// <returns>The values that the parameterization projection signs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public static ParameterizationValues ToParameterizationValues(ModelPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new ParameterizationValues(
            [.. payload.Energy.PairTables.Select(static table => table.Energies)],
            [.. payload.Energy.ClusterTables.Select(static table => table.Energies)],
            payload.Energy.DefectBackground,
            [.. payload.Transitions.JumpRules.Select(static rule => rule.FrequencyHz)]);
    }

    private static ParticlePayload MapParticles(Compilation.Structure.CompiledParticleTable particles) =>
        new(
            [.. particles.Particles.Select(static particle => particle.ChargeElementary)],
            [.. particles.Particles.Select(static particle => particle.Symbol)],
            [.. particles.Particles.Select(static particle => particle.IsVacancy)],
            (uint)particles.Count);
}
