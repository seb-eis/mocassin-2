// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

using Mocassin.App.Tests.LayoutSpike.Data;

namespace Mocassin.App.Tests.LayoutSpike.Editing;

/// <summary>
/// Turns the editor's field values into one pure project mutation, or into a local field error.
/// </summary>
/// <remarks>
/// The mutation is pure and unvalidated; the production validator runs inside
/// <c>ProjectEditCoordinator.Apply</c>. Local errors cover only values that cannot be represented at all, such as a
/// non-numeric charge; structural and scientific rules stay with the real validator.
/// </remarks>
internal static class SpikeEditorMutation
{
    /// <summary>
    /// Builds the candidate project for one editor form.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="domain">The edited domain.</param>
    /// <param name="id">The edited object identifier.</param>
    /// <param name="values">The field values.</param>
    /// <param name="description">The change description.</param>
    /// <param name="error">The local field error, or <see langword="null"/>.</param>
    /// <returns>The candidate project, or <see langword="null"/> on a local error.</returns>
    public static ProjectModel? Build(
        ProjectModel project,
        SpikeDomain domain,
        Guid id,
        SpikeEditorValues values,
        out string description,
        out string? error)
    {
        error = null;
        description = $"Edit {Label(domain)}";
        return domain switch
        {
            SpikeDomain.Particles => UpdateParticle(project, id, values, out error),
            SpikeDomain.ParticleSets => UpdateSet(project, id, values, out error),
            SpikeDomain.Sites => UpdateSite(project, id, values, out error),
            SpikeDomain.StateChanges => UpdateStateChange(project, id, values, out error),
            SpikeDomain.Chains => UpdateChain(project, id, values, out error),
            SpikeDomain.Transitions => UpdateTransition(project, id, values, out error),
            _ => null,
        };
    }

    /// <summary>
    /// Removes the selected rows in one mutation.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="domain">The edited domain.</param>
    /// <param name="ids">The selected identifiers.</param>
    /// <param name="description">The change description.</param>
    /// <returns>The candidate project.</returns>
    public static ProjectModel Remove(
        ProjectModel project,
        SpikeDomain domain,
        IReadOnlySet<Guid> ids,
        out string description)
    {
        description = $"Remove {ids.Count} {Label(domain)} object(s)";
        return domain switch
        {
            SpikeDomain.Particles => project with
            {
                Model = project.Model with
                {
                    Particles = project.Model.Particles.Where(item => !ids.Contains(item.Id.Value)).ToImmutableArray(),
                },
            },
            SpikeDomain.ParticleSets => project with
            {
                Model = project.Model with
                {
                    ParticleSets = project.Model.ParticleSets
                        .Where(item => !ids.Contains(item.Id.Value))
                        .ToImmutableArray(),
                },
            },
            SpikeDomain.Sites => project with
            {
                Model = project.Model with
                {
                    Structure = project.Structure with
                    {
                        Sites = project.Structure.Sites
                            .Where(item => !ids.Contains(item.Id.Value))
                            .ToImmutableArray(),
                    },
                },
            },
            SpikeDomain.StateChanges => project with
            {
                Model = project.Model with
                {
                    StateChanges = project.StateChanges
                        .Where(item => !ids.Contains(item.Id.Value))
                        .ToImmutableArray(),
                },
            },
            SpikeDomain.Chains => project with
            {
                Model = project.Model with
                {
                    Chains = project.Chains.Where(item => !ids.Contains(item.Id.Value)).ToImmutableArray(),
                },
            },
            SpikeDomain.Transitions => project with
            {
                Model = project.Model with
                {
                    KineticTransitions = project.KineticTransitions
                        .Where(item => !ids.Contains(item.Id.Value))
                        .ToImmutableArray(),
                    MetropolisTransitions = project.MetropolisTransitions
                        .Where(item => !ids.Contains(item.Id.Value))
                        .ToImmutableArray(),
                },
            },
            _ => project,
        };
    }

    private static ProjectModel? UpdateParticle(
        ProjectModel project,
        Guid id,
        SpikeEditorValues values,
        out string? error)
    {
        error = null;
        var particle = SpikeModelLookup.FindParticle(project, id);
        if (particle is null)
        {
            error = "The particle no longer exists; reload the selection.";
            return null;
        }

        if (!double.TryParse(
                values.Text("charge"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var charge) ||
            !double.IsFinite(charge))
        {
            error = "Charge must be a finite number, for example -2 or +4.";
            return null;
        }

        try
        {
            var updated = particle with
            {
                Name = values.Text("name"),
                Symbol = values.Text("symbol"),
                Charge = new ChargeElementary(charge),
                IsVacancy = values.Choice("vacancy") == "yes",
            };
            return project with
            {
                Model = project.Model with
                {
                    Particles = Replace(project.Model.Particles, item => item.Id == particle.Id, updated),
                },
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "Charge must be between -1000 and 1000 elementary charges.";
            return null;
        }
    }

    private static ProjectModel? UpdateSet(
        ProjectModel project,
        Guid id,
        SpikeEditorValues values,
        out string? error)
    {
        error = null;
        var set = SpikeModelLookup.FindSet(project, id);
        if (set is null)
        {
            error = "The particle set no longer exists; reload the selection.";
            return null;
        }

        var updated = new ParticleSet(set.Id, values.Text("name"), set.ParticleIds);
        return project with
        {
            Model = project.Model with
            {
                ParticleSets = Replace(
                    project.Model.ParticleSets,
                    item => item.Id == set.Id,
                    updated),
            },
        };
    }

    private static ProjectModel? UpdateSite(
        ProjectModel project,
        Guid id,
        SpikeEditorValues values,
        out string? error)
    {
        error = null;
        var site = SpikeModelLookup.FindSite(project, id);
        if (site is null)
        {
            error = "The site no longer exists; reload the selection.";
            return null;
        }

        if (!TryPosition(values, out var position))
        {
            error = "A, B, and C must be finite fractional numbers, for example 0.25.";
            return null;
        }

        var setReference = values.Reference("particleSet");
        if (setReference is null)
        {
            error = "Choose a particle set; a site cannot use the empty reference.";
            return null;
        }

        var stability = string.Equals(values.Choice("stability"), "unstable", StringComparison.Ordinal)
            ? SiteStability.Unstable
            : SiteStability.Stable;
        var updated = site with
        {
            Name = values.Text("name"),
            Fractional = position,
            ParticleSetId = ParticleSetId.Create(setReference.Value),
            Stability = stability,
        };
        return project with
        {
            Model = project.Model with
            {
                Structure = project.Structure with
                {
                    Sites = Replace(project.Structure.Sites, item => item.Id == site.Id, updated),
                },
            },
        };
    }

    private static ProjectModel? UpdateStateChange(
        ProjectModel project,
        Guid id,
        SpikeEditorValues values,
        out string? error)
    {
        error = null;
        var change = SpikeModelLookup.FindStateChange(project, id);
        if (change is null)
        {
            error = "The state change no longer exists; reload the selection.";
            return null;
        }

        var donor = values.Reference("donor");
        var acceptor = values.Reference("acceptor");
        if (donor is null || acceptor is null)
        {
            error = "Choose both a donor and an acceptor particle.";
            return null;
        }

        var updated = change with
        {
            Name = values.Text("name"),
            DonorParticleId = ParticleId.Create(donor.Value),
            AcceptorParticleId = ParticleId.Create(acceptor.Value),
        };
        return project with
        {
            Model = project.Model with
            {
                StateChanges = Replace(project.StateChanges, item => item.Id == change.Id, updated),
            },
        };
    }

    private static ProjectModel? UpdateChain(
        ProjectModel project,
        Guid id,
        SpikeEditorValues values,
        out string? error)
    {
        error = null;
        var chain = SpikeModelLookup.FindChain(project, id);
        if (chain is null)
        {
            error = "The chain no longer exists; reload the selection.";
            return null;
        }

        var updated = chain with
        {
            Name = values.Text("name"),
            GeneratesReverse = values.Choice("reverse") == "yes",
        };
        return project with
        {
            Model = project.Model with
            {
                Chains = Replace(project.Chains, item => item.Id == chain.Id, updated),
            },
        };
    }

    private static ProjectModel? UpdateTransition(
        ProjectModel project,
        Guid id,
        SpikeEditorValues values,
        out string? error)
    {
        error = null;
        var chainId = values.Reference("chain");
        if (chainId is null)
        {
            error = "Choose the abstract chain the transition is bound to.";
            return null;
        }

        var kinetic = SpikeModelLookup.FindKinetic(project, id);
        if (kinetic is not null)
        {
            var updated = kinetic with
            {
                Name = values.Text("name"),
                ChainId = TransitionChainId.Create(chainId.Value),
            };
            return project with
            {
                Model = project.Model with
                {
                    KineticTransitions = Replace(project.KineticTransitions, item => item.Id == kinetic.Id, updated),
                },
            };
        }

        var metropolis = SpikeModelLookup.FindMetropolis(project, id);
        if (metropolis is null)
        {
            error = "The transition no longer exists; reload the selection.";
            return null;
        }

        var changed = metropolis with
        {
            Name = values.Text("name"),
            ChainId = TransitionChainId.Create(chainId.Value),
        };
        return project with
        {
            Model = project.Model with
            {
                MetropolisTransitions = Replace(
                    project.MetropolisTransitions,
                    item => item.Id == metropolis.Id,
                    changed),
            },
        };
    }

    private static bool TryPosition(SpikeEditorValues values, out Fractional3 position)
    {
        position = Fractional3.Zero;
        if (!TryNumber(values.Text("a"), out var a)
            || !TryNumber(values.Text("b"), out var b)
            || !TryNumber(values.Text("c"), out var c))
        {
            return false;
        }

        position = new Fractional3(a, b, c);
        return true;
    }

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value);

    private static ImmutableArray<T> Replace<T>(ImmutableArray<T> items, Func<T, bool> match, T replacement)
    {
        var builder = items.ToBuilder();
        for (var index = 0; index < builder.Count; index++)
        {
            if (match(builder[index]))
            {
                builder[index] = replacement;
                break;
            }
        }

        return builder.ToImmutable();
    }

    private static string Label(SpikeDomain domain) => domain switch
    {
        SpikeDomain.Particles => "particle",
        SpikeDomain.ParticleSets => "particle set",
        SpikeDomain.Sites => "site",
        SpikeDomain.StateChanges => "state change",
        SpikeDomain.Chains => "transition chain",
        SpikeDomain.Transitions => "transition",
        _ => "model object",
    };
}
