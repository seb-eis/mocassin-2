// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The stable command names of the <c>mocassin</c> command graph.
/// </summary>
internal static class CliCommandNames
{
    /// <summary>The project creation command.</summary>
    public const string New = "new";

    /// <summary>The project validation command.</summary>
    public const string Validate = "validate";

    /// <summary>The model compilation command.</summary>
    public const string Compile = "compile";

    /// <summary>The package build command.</summary>
    public const string Build = "build";

    /// <summary>The package inspection command.</summary>
    public const string Inspect = "inspect";

    /// <summary>The package verification command.</summary>
    public const string Verify = "verify";

    /// <summary>The environment diagnosis command.</summary>
    public const string Doctor = "doctor";

    /// <summary>The job-folder cleanup command.</summary>
    public const string Clean = "clean";

    /// <summary>The reserved solver command.</summary>
    public const string Simulate = "simulate";

    /// <summary>The reserved results-collection command.</summary>
    public const string Collect = "collect";

    /// <summary>The reserved evaluation command.</summary>
    public const string Evaluate = "evaluate";

    /// <summary>The reserved export command.</summary>
    public const string Export = "export";
}
