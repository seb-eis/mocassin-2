// SPDX-License-Identifier: MIT

using System.CommandLine;
using Mocassin.Cli.Build;
using Mocassin.Cli.Diagnostics;
using Mocassin.Cli.Validation;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The test entry point of the CLI: expands response files, parses once, maps usage errors, dispatches a typed
/// request, and maps the handler result to the documented exit code.
/// </summary>
internal sealed class CliApplication
{
    private static readonly ParserConfiguration parserConfiguration = new()
    {
        EnablePosixBundling = false,
        ResponseFileTokenReplacer = null,
    };

    private readonly CliCommandGraph graph;

    /// <summary>
    /// Initializes the application with the complete command graph.
    /// </summary>
    public CliApplication()
        : this(new CliCommandGraph())
    {
    }

    /// <summary>
    /// Initializes the application with a graph supplied by a test.
    /// </summary>
    /// <param name="graph">The command graph.</param>
    public CliApplication(CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        this.graph = graph;
    }

    /// <summary>
    /// Runs one command line.
    /// </summary>
    /// <param name="args">The raw process arguments.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        var expansion = CliResponseFiles.Expand(args);
        if (!expansion.IsSuccess)
        {
            return WriteExpansionError(args, expansion, stdout, stderr);
        }

        var parseResult = graph.Root.Parse(expansion.Tokens, parserConfiguration);
        if (parseResult.Errors.Count > 0)
        {
            return WriteParseErrors(parseResult, expansion.Tokens, stdout, stderr);
        }

        if (parseResult.Action is not null)
        {
            var configuration = new InvocationConfiguration { Output = stdout, Error = stderr };
            return await parseResult.InvokeAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        var command = parseResult.CommandResult.Command;
        if (ReferenceEquals(command, graph.Root))
        {
            stderr.WriteLine("CLI001: A command is required.");
            CliParseErrors.WriteRootHelp(graph, stderr);
            return CliExitCode.Usage;
        }

        try
        {
            return await DispatchAsync(command, parseResult, stdout, stderr, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var diagnostic = CliDiagnosticFactory.Cancelled("The command was cancelled.");
            stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            return CliExitCode.Cancelled;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            var diagnostic = CliDiagnosticFactory.Internal(
                $"Unexpected internal error: {exception.GetType().Name}: {exception.Message}");
            stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            if (string.Equals(
                    Environment.GetEnvironmentVariable("MOCASSIN_DEBUG"),
                    "1",
                    StringComparison.Ordinal))
            {
                stderr.WriteLine(exception);
            }

            return CliExitCode.Internal;
        }
    }

    private Task<int> DispatchAsync(
        Command command,
        ParseResult parseResult,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        if (ReferenceEquals(command, graph.ValidateCommand))
        {
            return ValidateCommand.RunAsync(
                CliBinding.BindValidate(parseResult, graph),
                stdout,
                stderr,
                cancellationToken);
        }

        if (ReferenceEquals(command, graph.BuildCommand))
        {
            return RunBuildAsync(CliBinding.BindBuild(parseResult, graph), stdout, stderr, cancellationToken);
        }

        if (ReferenceEquals(command, graph.NewCommand))
        {
            WriteUnavailable(command, stderr, "Step 12 feature 4");
            return Task.FromResult(CliExitCode.Usage);
        }

        if (ReferenceEquals(command, graph.CompileCommand))
        {
            WriteUnavailable(command, stderr, "Step 12 feature 4");
            return Task.FromResult(CliExitCode.Usage);
        }

        if (ReferenceEquals(command, graph.InspectCommand) || ReferenceEquals(command, graph.VerifyCommand))
        {
            WriteUnavailable(command, stderr, "Step 12 feature 5");
            return Task.FromResult(CliExitCode.Usage);
        }

        if (ReferenceEquals(command, graph.DoctorCommand) || ReferenceEquals(command, graph.CleanCommand))
        {
            WriteUnavailable(command, stderr, "Step 12 feature 6");
            return Task.FromResult(CliExitCode.Usage);
        }

        var owner = command.Name switch
        {
            CliCommandNames.Simulate => "Step 19/27",
            CliCommandNames.Collect => "Step 20",
            CliCommandNames.Evaluate => "Step 21",
            _ => "Step 21",
        };
        WriteReserved(command, stderr, owner);
        return Task.FromResult(CliExitCode.Usage);
    }

    private static async Task<int> RunBuildAsync(
        BuildRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        if (!request.Preview)
        {
            WriteUnavailable(stderr, "CLI008", "The 'build' command without --preview arrives with Step 12 feature 3.");
            return CliExitCode.Usage;
        }

        if (request.OutputPath is not null)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept -o/--output.");
        }

        if (request.Force)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept --force.");
        }

        if (request.Compact)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept --compact.");
        }

        if (request.BuildTemplateId is not null)
        {
            var diagnostic = CliDiagnosticFactory.Selection(
                "The --template option selects a build template, which arrives with Step 12 feature 3.",
                "Use --job-template to select a job template for build --preview.");
            stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message} {diagnostic.Hint}");
            return CliExitCode.Usage;
        }

        if (request.ParameterizationId is not null)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept --parameterization in this feature.");
        }

        if (request.Jobs is not null)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept --jobs in this feature.");
        }

        if (request.Parallel != 0)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept --parallel in this feature.");
        }

        if (request.WarningsAsErrors)
        {
            return WritePreviewUsageError(
                stderr,
                "build --preview does not accept --warnings-as-errors in this feature.");
        }

        if (request.Quiet)
        {
            return WritePreviewUsageError(stderr, "build --preview does not accept --quiet in this feature.");
        }

        return await PreviewCommand.RunAsync(
                new PreviewRequest(request.ProjectPath, request.JobTemplateId, request.Json),
                stdout,
                stderr,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static int WritePreviewUsageError(TextWriter stderr, string message)
    {
        var diagnostic = CliDiagnosticFactory.Usage(message);
        stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
        return CliExitCode.Usage;
    }

    private int WriteParseErrors(
        ParseResult parseResult,
        IReadOnlyList<string> tokens,
        TextWriter stdout,
        TextWriter stderr)
    {
        var command = parseResult.CommandResult.Command;
        if (WantsJson(tokens) && graph.IsReportBacked(command))
        {
            var diagnostics = CliParseErrors.ToDiagnostics(parseResult);
            if (ReferenceEquals(command, graph.ValidateCommand))
            {
                ValidationReportWriter.WriteJson(stdout, ValidationReportWriter.UsageError(diagnostics));
                return CliExitCode.Usage;
            }

            if (ReferenceEquals(command, graph.BuildCommand) && WantsPreview(tokens))
            {
                PreviewCommand.WriteUsageReport(stdout, diagnostics);
                return CliExitCode.Usage;
            }
        }

        CliParseErrors.WriteHuman(parseResult, graph, stderr);
        return CliExitCode.Usage;
    }

    private int WriteExpansionError(
        IReadOnlyList<string> rawArgs,
        ResponseFileExpansion expansion,
        TextWriter stdout,
        TextWriter stderr)
    {
        var diagnostics = new[] { CliDiagnosticFactory.Usage(expansion.Error!) };
        var candidates = rawArgs
            .Where(static token => !token.StartsWith('@'))
            .TakeWhile(static token => token != "--")
            .Concat(expansion.Tokens.TakeWhile(static token => token != "--"))
            .ToArray();
        var command = RecogniseCommand(candidates);

        if (candidates.Contains("--json", StringComparer.Ordinal))
        {
            if (string.Equals(command, CliCommandNames.Validate, StringComparison.Ordinal))
            {
                ValidationReportWriter.WriteJson(stdout, ValidationReportWriter.UsageError(diagnostics));
                return CliExitCode.Usage;
            }

            if (string.Equals(command, CliCommandNames.Build, StringComparison.Ordinal)
                && candidates.Contains("--preview", StringComparer.Ordinal))
            {
                PreviewCommand.WriteUsageReport(stdout, diagnostics);
                return CliExitCode.Usage;
            }
        }

        stderr.WriteLine($"{diagnostics[0].Code}: {diagnostics[0].Message}");
        CliParseErrors.WriteRootHelp(graph, stderr);
        return CliExitCode.Usage;
    }

    private static string? RecogniseCommand(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (token is CliCommandNames.New or CliCommandNames.Validate or CliCommandNames.Compile
                or CliCommandNames.Build or CliCommandNames.Inspect or CliCommandNames.Verify
                or CliCommandNames.Doctor or CliCommandNames.Clean or CliCommandNames.Simulate
                or CliCommandNames.Collect or CliCommandNames.Evaluate or CliCommandNames.Export)
            {
                return token;
            }
        }

        return null;
    }

    private static bool WantsJson(IReadOnlyList<string> tokens) =>
        tokens.TakeWhile(static token => token != "--").Contains("--json", StringComparer.Ordinal);

    private static bool WantsPreview(IReadOnlyList<string> tokens) =>
        tokens.TakeWhile(static token => token != "--").Contains("--preview", StringComparer.Ordinal);

    private static void WriteUnavailable(Command command, TextWriter stderr, string feature) =>
        WriteUnavailable(
            stderr,
            "CLI008",
            $"The '{command.Name}' command arrives with {feature}.");

    private static void WriteReserved(Command command, TextWriter stderr, string owner) =>
        WriteUnavailable(stderr, "CLI008", $"The '{command.Name}' command is reserved for {owner}.");

    private static void WriteUnavailable(TextWriter stderr, string code, string message) =>
        stderr.WriteLine($"{code}: {message}");
}
