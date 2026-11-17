// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The stable process exit codes of the <c>mocassin</c> command line.
/// </summary>
internal static class CliExitCode
{
    /// <summary>The command succeeded; warnings may still have been reported.</summary>
    public const int Success = 0;

    /// <summary>Validation, model, package, or doctor failure.</summary>
    public const int Invalid = 1;

    /// <summary>A usage, argument, or response-file error.</summary>
    public const int Usage = 2;

    /// <summary>An I/O failure, a missing input, or an existing target without <c>--force</c>.</summary>
    public const int Io = 3;

    /// <summary>A compilation or parameterization error.</summary>
    public const int Compilation = 4;

    /// <summary>Partial success; reserved for <c>simulate</c> and <c>collect</c>.</summary>
    public const int Partial = 5;

    /// <summary>The operation was cancelled by the user.</summary>
    public const int Cancelled = 6;

    /// <summary>An unexpected internal error.</summary>
    public const int Internal = 10;
}
