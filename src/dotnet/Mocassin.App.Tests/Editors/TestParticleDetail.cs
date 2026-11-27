// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// A concrete detail over the real particle fields.
/// </summary>
internal sealed class TestParticleDetail : ModelDetailViewModel
{
    private readonly string symbol;
    private readonly double charge;
    private readonly bool isVacancy;
    private string name;

    /// <summary>
    /// Initializes the detail from a particle.
    /// </summary>
    /// <param name="particle">The particle.</param>
    public TestParticleDetail(Particle particle)
        : base(particle.Id.Value)
    {
        name = particle.Name;
        symbol = particle.Symbol;
        charge = particle.Charge.Value;
        isVacancy = particle.IsVacancy;
    }

    /// <summary>
    /// Gets or sets the draft name.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            name = value;
            HasUncommittedChanges = true;
        }
    }

    /// <inheritdoc />
    protected override string Description => "Update particle";

    /// <inheritdoc />
    protected override ProjectEditResult ApplyCore(ProjectEditor editor, ProjectModel project) =>
        editor.UpdateParticle(
            project,
            new Particle(
                ParticleId.Create(ObjectId),
                Name,
                symbol,
                new ChargeElementary(charge),
                isVacancy));

    /// <inheritdoc />
    protected override void RevertCore()
    {
        var particle = Coordinator?.Document.Project.Find(ParticleId.Create(ObjectId));
        if (particle is not null)
        {
            name = particle.Name;
            OnPropertyChanged(nameof(Name));
        }
    }
}
