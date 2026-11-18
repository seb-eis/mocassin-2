// SPDX-License-Identifier: MIT

using System.CommandLine;
using System.CommandLine.Invocation;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts.Cli;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The hidden per-subcommand <c>--version</c> guard that keeps <c>--version</c> root-only.
/// </summary>
/// <remarks>
/// Without a defined option the parser would bind the <c>--version</c> token to a positional argument.
/// The guard makes <c>mocassin &lt;command&gt; --version</c> the documented <c>CLI001</c>/<c>2</c> usage error.
/// </remarks>
internal sealed class CliSubcommandVersionAction : SynchronousCommandLineAction
{
    /// <summary>The stable CLI-owned message.</summary>
    public const string Message = "The --version option is only supported on the root command.";

    /// <inheritdoc />
    public override bool ClearsParseErrors => true;

    /// <inheritdoc />
    public override int Invoke(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        var diagnostic = CliDiagnosticFactory.Usage(Message);
        var writer = parseResult.InvocationConfiguration.Error ?? Console.Error;
        writer.WriteLine(string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{CliDiagnosticCodes.Usage}: {diagnostic.Message}"));
        return CliExitCode.Usage;
    }
}
