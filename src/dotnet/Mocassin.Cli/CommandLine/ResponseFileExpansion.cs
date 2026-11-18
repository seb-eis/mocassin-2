// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The flattened command line and, when expansion failed, the CLI-owned error message.
/// </summary>
/// <param name="Tokens">The raw tokens and the tokens contributed by response files that expanded successfully.
/// A top-level file that failed contributes no tokens.</param>
/// <param name="Error">The <c>CLI001</c> message, or <see langword="null"/> when expansion succeeded.</param>
internal sealed record ResponseFileExpansion(IReadOnlyList<string> Tokens, string? Error)
{
    /// <summary>
    /// Gets a value indicating whether expansion succeeded.
    /// </summary>
    public bool IsSuccess => Error is null;
}
