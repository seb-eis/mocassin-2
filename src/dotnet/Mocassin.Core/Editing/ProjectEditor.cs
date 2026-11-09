// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

/// <summary>
/// Applies one mutation to an immutable project and validates the candidate before committing it.
/// </summary>
public sealed partial class ProjectEditor
{
    private readonly Func<ProjectModel, ImmutableArray<Diagnostic>> validator;

    /// <summary>
    /// Initializes a project editor.
    /// </summary>
    /// <param name="validator">Optional validator override. The default is <see cref="ProjectValidator.Validate"/>.</param>
    public ProjectEditor(Func<ProjectModel, ImmutableArray<Diagnostic>>? validator = null)
    {
        this.validator = validator ?? ProjectValidator.Validate;
    }

    /// <summary>
    /// Applies a mutation and returns the candidate only when validation has no errors.
    /// </summary>
    /// <param name="project">The current project value.</param>
    /// <param name="description">The user-visible change description.</param>
    /// <param name="mutation">The pure mutation that returns the candidate project.</param>
    /// <returns>The edit result. The input is returned unchanged on validation errors.</returns>
    public ProjectEditResult Apply(
        ProjectModel project,
        string description,
        Func<ProjectModel, ProjectModel> mutation)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(mutation);

        var candidate = mutation(project)
            ?? throw new InvalidOperationException("A project mutation returned null.");

        var diagnostics = validator(candidate);
        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return new ProjectEditResult(project, null, diagnostics);
        }

        if (ReferenceEquals(candidate, project) ||
            string.Equals(
                ProjectHasher.ComputeDocument(candidate),
                ProjectHasher.ComputeDocument(project),
                StringComparison.Ordinal))
        {
            return new ProjectEditResult(project, null, diagnostics);
        }

        return new ProjectEditResult(
            candidate,
            new ProjectChange(description, project, candidate),
            diagnostics);
    }

    /// <summary>
    /// Adds an authored particle.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="particle">The particle to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddParticle(ProjectModel project, Particle particle)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(particle);
        return Apply(project, "Add particle", current => current with
        {
            Model = current.Model with { Particles = Append(current.Model.Particles, particle) },
        });
    }

    /// <summary>
    /// Updates an authored particle by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="particle">The replacement particle.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateParticle(ProjectModel project, Particle particle)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(particle);
        if (!Contains(project.Model.Particles, candidate => candidate.Id == particle.Id))
        {
            return Unknown(project, "particle", particle.Id.Value, $"Particle '{particle.Id:D}' does not exist.");
        }

        return Apply(project, "Update particle", current =>
        {
            var model = current.Model;
            return TryReplace(model.Particles, candidate => candidate.Id == particle.Id, particle, out var particles)
                ? current with { Model = model with { Particles = particles } }
                : current;
        });
    }

    /// <summary>
    /// Removes an authored particle by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The particle identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveParticle(ProjectModel project, ParticleId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Particles, candidate => candidate.Id == id))
        {
            return Unknown(project, "particle", id.Value, $"Particle '{id:D}' does not exist.");
        }

        return Apply(project, "Remove particle", current => current with
        {
            Model = current.Model with
            {
                Particles = RemoveWhere(current.Model.Particles, candidate => candidate.Id == id),
            },
        });
    }

    /// <summary>
    /// Adds an authored particle set.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="particleSet">The particle set to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddParticleSet(ProjectModel project, ParticleSet particleSet)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(particleSet);
        return Apply(project, "Add particle set", current => current with
        {
            Model = current.Model with { ParticleSets = Append(current.Model.ParticleSets, particleSet) },
        });
    }

    /// <summary>
    /// Updates an authored particle set by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="particleSet">The replacement particle set.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateParticleSet(ProjectModel project, ParticleSet particleSet)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(particleSet);
        if (!Contains(project.Model.ParticleSets, candidate => candidate.Id == particleSet.Id))
        {
            return Unknown(
                project,
                "particleSet",
                particleSet.Id.Value,
                $"Particle set '{particleSet.Id:D}' does not exist.");
        }

        return Apply(project, "Update particle set", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.ParticleSets,
                candidate => candidate.Id == particleSet.Id,
                particleSet,
                out var particleSets)
                ? current with { Model = model with { ParticleSets = particleSets } }
                : current;
        });
    }

    /// <summary>
    /// Removes an authored particle set by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The particle-set identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveParticleSet(ProjectModel project, ParticleSetId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.ParticleSets, candidate => candidate.Id == id))
        {
            return Unknown(
                project,
                "particleSet",
                id.Value,
                $"Particle set '{id:D}' does not exist.");
        }

        return Apply(project, "Remove particle set", current => current with
        {
            Model = current.Model with
            {
                ParticleSets = RemoveWhere(current.Model.ParticleSets, candidate => candidate.Id == id),
            },
        });
    }

    /// <summary>
    /// Sets the structure name.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="name">The new structure name.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetStructureName(ProjectModel project, string name)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(name);
        return Apply(project, "Set structure name", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with { Name = name },
            },
        });
    }

    /// <summary>
    /// Sets the space-group reference.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="reference">The new space-group reference.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetSpaceGroup(ProjectModel project, SpaceGroupRef reference)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(reference);
        return Apply(project, "Set space group", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with { SpaceGroup = reference },
            },
        });
    }

    /// <summary>
    /// Sets the unit-cell parameters.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="parameters">The new cell parameters.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetCellParameters(ProjectModel project, CellParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(parameters);
        return Apply(project, "Set cell parameters", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with { Cell = parameters },
            },
        });
    }

    /// <summary>
    /// Adds a cell site.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="site">The site to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddCellSite(ProjectModel project, CellSite site)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(site);
        return Apply(project, "Add cell site", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with
                {
                    Sites = Append(current.Model.Structure.Sites, site),
                },
            },
        });
    }

    /// <summary>
    /// Updates a cell site by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="site">The replacement site.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateCellSite(ProjectModel project, CellSite site)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(site);
        if (!Contains(project.Model.Structure.Sites, candidate => candidate.Id == site.Id))
        {
            return Unknown(project, "cellSite", site.Id.Value, $"Cell site '{site.Id:D}' does not exist.");
        }

        return Apply(project, "Update cell site", current =>
        {
            var structure = current.Model.Structure;
            return TryReplace(structure.Sites, candidate => candidate.Id == site.Id, site, out var sites)
                ? current with
                {
                    Model = current.Model with { Structure = structure with { Sites = sites } },
                }
                : current;
        });
    }

    /// <summary>
    /// Removes a cell site by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The cell-site identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveCellSite(ProjectModel project, CellSiteId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Structure.Sites, candidate => candidate.Id == id))
        {
            return Unknown(project, "cellSite", id.Value, $"Cell site '{id:D}' does not exist.");
        }

        return Apply(project, "Remove cell site", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with
                {
                    Sites = RemoveWhere(current.Model.Structure.Sites, candidate => candidate.Id == id),
                },
            },
        });
    }

    /// <summary>
    /// Adds a visual-only dummy position.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="dummyPosition">The dummy position to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddDummyPosition(ProjectModel project, CellDummyPosition dummyPosition)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(dummyPosition);
        return Apply(project, "Add dummy position", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with
                {
                    DummyPositions = Append(current.Model.Structure.DummyPositions, dummyPosition),
                },
            },
        });
    }

    /// <summary>
    /// Updates a dummy position by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="dummyPosition">The replacement dummy position.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateDummyPosition(ProjectModel project, CellDummyPosition dummyPosition)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(dummyPosition);
        if (!Contains(
                project.Model.Structure.DummyPositions,
                candidate => candidate.Id == dummyPosition.Id))
        {
            return Unknown(
                project,
                "cellDummyPosition",
                dummyPosition.Id.Value,
                $"Dummy position '{dummyPosition.Id:D}' does not exist.");
        }

        return Apply(project, "Update dummy position", current =>
        {
            var structure = current.Model.Structure;
            return TryReplace(
                structure.DummyPositions,
                candidate => candidate.Id == dummyPosition.Id,
                dummyPosition,
                out var dummyPositions)
                ? current with
                {
                    Model = current.Model with
                    {
                        Structure = structure with { DummyPositions = dummyPositions },
                    },
                }
                : current;
        });
    }

    /// <summary>
    /// Removes a dummy position by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The dummy-position identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveDummyPosition(ProjectModel project, CellDummyPositionId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Structure.DummyPositions, candidate => candidate.Id == id))
        {
            return Unknown(
                project,
                "cellDummyPosition",
                id.Value,
                $"Dummy position '{id:D}' does not exist.");
        }

        return Apply(project, "Remove dummy position", current => current with
        {
            Model = current.Model with
            {
                Structure = current.Model.Structure with
                {
                    DummyPositions = RemoveWhere(
                        current.Model.Structure.DummyPositions,
                        candidate => candidate.Id == id),
                },
            },
        });
    }

    private static ImmutableArray<T> Append<T>(ImmutableArray<T> values, T value) =>
        values.IsDefault ? [value] : [.. values, value];

    private static ImmutableArray<T> RemoveWhere<T>(ImmutableArray<T> values, Func<T, bool> predicate) =>
        values.IsDefault ? [] : [.. values.Where(value => !predicate(value))];

    private static bool Contains<T>(ImmutableArray<T> values, Func<T, bool> predicate)
    {
        if (values.IsDefault)
        {
            return false;
        }

        foreach (var value in values)
        {
            if (predicate(value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReplace<T>(
        ImmutableArray<T> values,
        Func<T, bool> predicate,
        T replacement,
        out ImmutableArray<T> result)
    {
        result = values;
        if (values.IsDefault)
        {
            return false;
        }

        var index = -1;
        for (var i = 0; i < values.Length; i++)
        {
            if (predicate(values[i]))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return false;
        }

        var builder = values.ToBuilder();
        builder[index] = replacement;
        result = builder.MoveToImmutable();
        return true;
    }

    private static ProjectEditResult Unknown(
        ProjectModel project,
        string domain,
        Guid objectId,
        string message) =>
        new(
            project,
            null,
            [
                new Diagnostic(
                    ProjectDiagnosticCodes.UnknownReference,
                    DiagnosticSeverity.Error,
                    message,
                    new ModelPath(domain, objectId)),
            ]);
}
