// SPDX-License-Identifier: MIT

using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Pins the model-payload <c>jump_mapping</c> produced for translation-equivalent starts.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CenteredEquivalentStartPayloadTests
{
    [Fact]
    public void PayloadMapsADirectionIntoAnotherStartBucket()
    {
        var (project, compiled) = ParameterizationTestSupport.LoadAndCompile(
            "samples/simulations/centered-exchange.mocprj");
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model!;

        var payload = ModelPayloadMapper.Map(applied);
        var transitions = payload.Transitions;
        var particleCount = (int)payload.Particle.Count;

        Assert.Equal(8, payload.Structure.Positions.Length);
        Assert.Equal(4, transitions.JumpDirections.Length);

        var crossBucket = Enumerable.Range(0, transitions.JumpCount.Length)
            .SelectMany(bucket =>
            {
                var position = bucket / particleCount;
                var offset = transitions.JumpCount.Take(bucket).Sum(static count => (int)count);
                return Enumerable.Range(0, (int)transitions.JumpCount[bucket])
                    .Select(slot => transitions.JumpMapping[offset + slot])
                    .Where(direction => transitions.JumpDirections[(int)direction].PositionId != position)
                    .Select(direction => (Position: position, Direction: (int)direction));
            })
            .ToArray();

        Assert.NotEmpty(crossBucket);

        // Cross-check every equivalent start from the compiled model against the encoded payload.
        foreach (var direction in compiled.Transitions.Directions)
        {
            foreach (var start in direction.EquivalentStartPositions)
            {
                for (var particle = 1; particle < particleCount; particle++)
                {
                    var bucket = (start * particleCount) + particle;
                    Assert.True(transitions.JumpCount[bucket] > 0);
                    var offset = transitions.JumpCount.Take(bucket).Sum(static count => (int)count);
                    var mapped = Enumerable.Range(0, (int)transitions.JumpCount[bucket])
                        .Select(slot => (int)transitions.JumpMapping[offset + slot])
                        .ToArray();
                    Assert.Contains(direction.Index, mapped);
                }
            }
        }
    }

    [Fact]
    public void PayloadKeepsTheSingleStartRepresentationForAnUncenteredModel()
    {
        var (project, compiled) = ParameterizationTestSupport.LoadAndCompile(
            "samples/simulations/ceria-kmc.mocprj");
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model!;

        var payload = ModelPayloadMapper.Map(applied);
        var transitions = payload.Transitions;
        var particleCount = (int)payload.Particle.Count;

        // No bucket may reference a direction whose representative position differs from the bucket position.
        for (var bucket = 0; bucket < transitions.JumpCount.Length; bucket++)
        {
            var position = bucket / particleCount;
            var offset = transitions.JumpCount.Take(bucket).Sum(static count => (int)count);
            foreach (var direction in Enumerable.Range(0, (int)transitions.JumpCount[bucket])
                         .Select(slot => transitions.JumpMapping[offset + slot]))
            {
                Assert.Equal((uint)position, transitions.JumpDirections[(int)direction].PositionId);
            }
        }
    }
}
