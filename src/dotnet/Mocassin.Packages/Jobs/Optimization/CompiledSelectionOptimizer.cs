// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Jobs.Optimization;

/// <summary>
/// One compiled <c>(position, particle)</c> start option removed from a KMC selection pool.
/// </summary>
/// <remarks>
/// The pair uses the canonical P1 position index and the dense compiled particle index. Step 11 encodes the
/// pair in the package job payload, and Step 14 applies it to the runtime selection pool. One authored
/// <c>(site, particle)</c> optimizer expands to one entry per canonical P1 position owned by the site.
/// </remarks>
/// <param name="PositionId">The canonical P1 position index.</param>
/// <param name="ParticleId">The dense compiled particle index removed at the position.</param>
public sealed record CompiledSelectionOptimizer(int PositionId, int ParticleId);
