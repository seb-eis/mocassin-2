// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Content hashes of the compilation inputs.
/// </summary>
/// <param name="Model">The canonical model-section hash.</param>
/// <param name="Structure">The canonical structure-section hash.</param>
/// <param name="Energy">The canonical energy-section hash.</param>
/// <param name="Transitions">The aggregated referenced-transition closure hash.</param>
/// <param name="Simulations">The canonical document hash used as the simulation-section fingerprint.</param>
/// <param name="ActiveTransitions">The canonical simulation active-transition selection hash.</param>
public sealed record CompilationInputHashes(
    string Model,
    string Structure,
    string Energy,
    string Transitions,
    string Simulations,
    string ActiveTransitions)
{
    /// <summary>Gets the empty input hashes.</summary>
    public static CompilationInputHashes Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}
