// SPDX-License-Identifier: MIT

using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Project;

[Trait("Tier", "PR-fast")]
public sealed class ProjectHasherLatticeTests
{
    [Fact]
    public void ComputeParticlesIgnoresAuthoredArrayOrderAndUnrelatedSections()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        var model = project.Model;
        var shuffled = project with
        {
            Model = model with
            {
                Particles = [.. model.Particles.Reverse()],
                ParticleSets = [.. model.ParticleSets.Reverse()],
                OccupationExchanges = [],
                Dopings = [],
                Structure = model.Structure with { Name = "Changed" },
            },
        };

        Assert.Equal(ProjectHasher.ComputeParticles(project), ProjectHasher.ComputeParticles(shuffled));
    }

    [Fact]
    public void ComputeParticlesChangesWithParticleAndParticleSetContent()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        var model = project.Model;
        var changedParticle = project with
        {
            Model = model with
            {
                Particles = [.. model.Particles.Select(static (particle, index) =>
                    index == 0 ? particle with { Name = "Changed" } : particle)],
            },
        };
        var changedSet = project with
        {
            Model = model with
            {
                ParticleSets = [.. model.ParticleSets.Select(static (set, index) =>
                    index == 0 ? set with { ParticleIds = [.. set.ParticleIds.Reverse()] } : set)],
            },
        };

        Assert.NotEqual(ProjectHasher.ComputeParticles(project), ProjectHasher.ComputeParticles(changedParticle));
        Assert.NotEqual(ProjectHasher.ComputeParticles(project), ProjectHasher.ComputeParticles(changedSet));
    }

    [Fact]
    public void ComputeLatticeIgnoresAuthoredArrayOrderAndUnrelatedSections()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        var model = project.Model;
        var shuffled = project with
        {
            Model = model with
            {
                OccupationExchanges = [.. model.OccupationExchanges.Reverse()],
                Dopings = [.. model.Dopings.Reverse()],
                Particles = [.. model.Particles.Reverse()],
                ParticleSets = [],
            },
        };

        Assert.Equal(ProjectHasher.ComputeLattice(project), ProjectHasher.ComputeLattice(shuffled));
    }

    [Fact]
    public void ComputeLatticeChangesWithExchangeAndDopingContent()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        var model = project.Model;
        var changedExchange = project with
        {
            Model = model with
            {
                OccupationExchanges =
                [
                    model.OccupationExchanges[0] with { TargetParticleId = LatticeTestModels.OxygenVacancy.Id },
                    .. model.OccupationExchanges.Skip(1),
                ],
            },
        };
        var changedDoping = project with
        {
            Model = model with
            {
                Dopings = [model.Dopings[0] with { Priority = 99 }, model.Dopings[1]],
            },
        };

        Assert.NotEqual(ProjectHasher.ComputeLattice(project), ProjectHasher.ComputeLattice(changedExchange));
        Assert.NotEqual(ProjectHasher.ComputeLattice(project), ProjectHasher.ComputeLattice(changedDoping));
    }

    [Fact]
    public void HashesAreStableForRepeatedCalls()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        Assert.Equal(ProjectHasher.ComputeParticles(project), ProjectHasher.ComputeParticles(project));
        Assert.Equal(ProjectHasher.ComputeLattice(project), ProjectHasher.ComputeLattice(project));
        Assert.Equal(64, ProjectHasher.ComputeParticles(project).Length);
        Assert.Equal(64, ProjectHasher.ComputeLattice(project).Length);
    }
}
