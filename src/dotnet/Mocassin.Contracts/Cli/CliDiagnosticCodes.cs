// SPDX-License-Identifier: MIT

namespace Mocassin.Contracts.Cli;

/// <summary>
/// Stable diagnostic codes owned by the <c>mocassin</c> command-line interface.
/// </summary>
/// <remarks>
/// The descriptors are owned by <c>Mocassin.Contracts</c> so that <c>Mocassin.Packages</c> can compose them into
/// its default diagnostic catalog without a <c>Packages -&gt; Cli</c> project edge. Domain diagnostics keep their
/// own <c>PROJ</c>, <c>STR</c>, <c>TRN</c>, <c>ENM</c>, <c>SIM</c>, <c>CMP</c>, <c>PRM</c>, <c>JOB</c>, and
/// <c>PKG</c> codes; the CLI never rewrites one into a <c>CLI</c> code.
/// </remarks>
public static class CliDiagnosticCodes
{
    /// <summary>A usage or argument error, including a library parse error or a response-file failure.</summary>
    public const string Usage = "CLI001";

    /// <summary>An I/O failure that has no domain diagnostic.</summary>
    public const string Io = "CLI002";

    /// <summary>An output already exists and overwriting was not requested.</summary>
    public const string OutputExists = "CLI003";

    /// <summary>A build template, job template, or parameterization selection is missing or ambiguous.</summary>
    public const string Selection = "CLI004";

    /// <summary>A <c>--jobs</c> selection is malformed, empty, or out of range.</summary>
    public const string JobSelection = "CLI005";

    /// <summary>The operation was cancelled by the user.</summary>
    public const string Cancelled = "CLI006";

    /// <summary>An unexpected internal error occurred.</summary>
    public const string Internal = "CLI007";

    /// <summary>The requested command or option is reserved for a later step.</summary>
    public const string Reserved = "CLI008";

    /// <summary>The input cannot be read as a simulation package.</summary>
    public const string NotAPackage = "CLI009";
}
