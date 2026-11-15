// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Generates the deterministic base lattice of one expanded job.
/// </summary>
/// <remarks>
/// The base lattice is the unit cell replicated in row-major order with
/// <c>index = ((a * B + b) * C + c) * PositionCount + P</c>. A stable site defaults to the first particle of its
/// authored particle set and an unstable site defaults to the compiled void index. Doping application, charge
/// compensation, and selection optimizers belong to later Step 10 features; this generator deliberately only
/// resolves the request hash and lattice seed they consume. Invalid user data returns diagnostics and no partial
/// lattice.
/// </remarks>
public static class SupercellGenerator
{
    /// <summary>
    /// The minimum number of unit cells per axis.
    /// </summary>
    public const int MinimumCellCount = 1;

    /// <summary>
    /// The maximum number of unit cells per axis.
    /// </summary>
    public const int MaximumCellCount = 256;

    /// <summary>
    /// The maximum number of positions in one generated supercell.
    /// </summary>
    public const int MaximumPositions = 500_000;

    /// <summary>
    /// The position count above which a <c>JOB014</c> warning is reported.
    /// </summary>
    public const int PositionWarningThreshold = 100_000;

    /// <summary>
    /// Generates the base lattice of one job.
    /// </summary>
    /// <param name="project">The project that supplies the authored particles, sets, and dopings.</param>
    /// <param name="model">The compiled model that supplies the P1 positions and compiled particle indices.</param>
    /// <param name="job">The expanded job descriptor whose lattice, dopings, and seed are used.</param>
    /// <param name="cancellationToken">Cancels generation between cell layers.</param>
    /// <returns>The generated lattice and diagnostics, or diagnostics and no lattice on invalid user data.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The job seed is not 32 bytes.</exception>
    /// <exception cref="InvalidOperationException">
    /// The compiled model is inconsistent with the project: it has no positions, a stable site has no usable
    /// particle set, or a particle index does not fit the byte lattice.
    /// </exception>
    public static SupercellGenerationResult Generate(
        Project project,
        CompiledModel model,
        JobDescriptor job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();

        var bag = new DiagnosticBag();
        var request = job.Lattice;
        var positionCount = model.Structure.PositionCount;
        if (positionCount <= 0)
        {
            throw new InvalidOperationException("The compiled model has no canonical P1 positions.");
        }

        if (!TryGetTotalPositions(request, positionCount, out var totalPositions))
        {
            bag.Add(Error(
                JobDiagnosticCodes.InvalidCellSize,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The supercell request ({request.A}, {request.B}, {request.C}) with {positionCount} positions per cell " +
                    $"must use cell counts in {MinimumCellCount}..{MaximumCellCount} and stay within {MaximumPositions} positions."),
                Path(project, job, "lattice")));
            return Failed(bag);
        }

        if (totalPositions > PositionWarningThreshold)
        {
            bag.Add(Warning(
                JobDiagnosticCodes.LargeSupercell,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The supercell has {totalPositions} positions; the warning threshold is {PositionWarningThreshold}."),
                Path(project, job, "lattice")));
        }

        var dopings = job.Dopings.IsDefault ? ImmutableArray<DopingValue>.Empty : job.Dopings;
        if (!LatticeRequestHasher.TryResolveDopings(project, dopings, out _, out var unknown))
        {
            bag.Add(Error(
                JobDiagnosticCodes.UnknownDoping,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The lattice request references unknown doping '{unknown.GetValueOrDefault().Value:D}'."),
                Path(project, job, "dopings")));
            return Failed(bag);
        }

        if (job.Seed.IsDefault || job.Seed.Length != LatticeSeedDeriver.SeedByteLength)
        {
            throw new ArgumentException(
                $"The job seed must contain exactly {LatticeSeedDeriver.SeedByteLength} bytes.",
                nameof(job));
        }

        var requestHash = LatticeRequestHasher.Compute(project, model, request, dopings);
        var latticeSeed = LatticeSeedDeriver.DeriveLatticeSeed(
            job.Seed.AsSpan(),
            Convert.FromHexString(requestHash));

        var pattern = CreateBasePattern(project, model);
        var particles = new byte[(int)totalPositions];
        var offset = 0;
        for (var a = 0; a < request.A; a++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var b = 0; b < request.B; b++)
            {
                for (var c = 0; c < request.C; c++)
                {
                    pattern.CopyTo(particles, offset);
                    offset += pattern.Length;
                }
            }
        }

        var lattice = new GeneratedLattice(
            request.A,
            request.B,
            request.C,
            positionCount,
            [.. particles]);
        return new SupercellGenerationResult(lattice, requestHash, latticeSeed, bag.Sorted);
    }

    private static bool TryGetTotalPositions(
        LatticeRequest request,
        int positionCount,
        out long totalPositions)
    {
        totalPositions = 0;
        if (request.A is < MinimumCellCount or > MaximumCellCount ||
            request.B is < MinimumCellCount or > MaximumCellCount ||
            request.C is < MinimumCellCount or > MaximumCellCount)
        {
            return false;
        }

        totalPositions = (long)request.A * request.B * request.C * positionCount;
        return totalPositions <= MaximumPositions;
    }

    private static byte[] CreateBasePattern(Project project, CompiledModel model)
    {
        var structure = model.Structure;
        var defaults = new byte[structure.Sites.Length];
        for (var index = 0; index < structure.Sites.Length; index++)
        {
            var site = structure.Sites[index];
            if (site.Stability == SiteStability.Unstable)
            {
                defaults[index] = (byte)CompiledParticleTable.VoidIndex;
                continue;
            }

            var set = project.Find(site.ParticleSetId);
            if (set is null || set.ParticleIds.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException(
                    $"Stable compiled site '{site.Id:D}' has no authored particle set '{site.ParticleSetId.Value:D}'.");
            }

            var first = set.ParticleIds[0];
            if (!model.Particles.TryGetIndex(first, out var particleIndex))
            {
                throw new InvalidOperationException(
                    $"The first particle '{first:D}' of stable site '{site.Id:D}' is not compiled.");
            }

            if (particleIndex is < 0 or > byte.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Compiled particle index {particleIndex} does not fit the byte lattice.");
            }

            defaults[index] = (byte)particleIndex;
        }

        var pattern = new byte[structure.PositionCount];
        for (var positionIndex = 0; positionIndex < structure.Positions.Length; positionIndex++)
        {
            var position = structure.Positions[positionIndex];
            if (position.SiteIndex < 0 || position.SiteIndex >= defaults.Length)
            {
                throw new InvalidOperationException(
                    $"Canonical P1 position {position.P} references invalid site index {position.SiteIndex}.");
            }

            pattern[positionIndex] = position.Stability == SiteStability.Unstable
                ? (byte)CompiledParticleTable.VoidIndex
                : defaults[position.SiteIndex];
        }

        return pattern;
    }

    private static SupercellGenerationResult Failed(DiagnosticBag bag) =>
        new(null, null, [], bag.Sorted);

    private static Diagnostic Error(string code, string message, ModelPath? path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(string code, string message, ModelPath? path) =>
        new(code, DiagnosticSeverity.Warning, message, path, JobDiagnosticCodes.HelpFor(code));

    private static ModelPath Path(Project project, JobDescriptor job, string property) =>
        new(
            "job",
            project.Metadata.ProjectId,
            string.Create(CultureInfo.InvariantCulture, $"jobs/{job.JobId}/{property}"));
}
