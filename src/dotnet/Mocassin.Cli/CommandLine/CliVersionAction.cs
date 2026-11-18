// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The CLI-owned root <c>--version</c> action.
/// </summary>
/// <remarks>
/// The action replaces the library's root version action so that <c>mocassin --version</c> prints the
/// assembly informational version and the runtime description. <c>--version</c> stays root-only: each
/// subcommand carries a hidden guard option that reports <c>CLI001</c> and exits <c>2</c>.
/// </remarks>
internal sealed class CliVersionAction : SynchronousCommandLineAction
{
    /// <inheritdoc />
    public override bool ClearsParseErrors => true;

    /// <inheritdoc />
    public override int Invoke(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        var writer = parseResult.InvocationConfiguration.Output ?? Console.Out;
        writer.WriteLine(DescribeVersion());
        writer.WriteLine(DescribeRuntime());
        return CliExitCode.Success;
    }

    /// <summary>
    /// Builds the informational-version line.
    /// </summary>
    /// <returns><c>mocassin &lt;informational version&gt;</c>.</returns>
    public static string DescribeVersion()
    {
        var assembly = typeof(CliVersionAction).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
        {
            informational = assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        return string.Create(CultureInfo.InvariantCulture, $"mocassin {informational}");
    }

    /// <summary>
    /// Builds the runtime-description line.
    /// </summary>
    /// <returns><c>Runtime: &lt;framework&gt; (&lt;rid&gt;)</c>.</returns>
    public static string DescribeRuntime() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.RuntimeIdentifier})");
}
