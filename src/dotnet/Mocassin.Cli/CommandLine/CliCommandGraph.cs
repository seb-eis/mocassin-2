// SPDX-License-Identifier: MIT

using System.CommandLine;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The single command table of the <c>mocassin</c> CLI.
/// </summary>
/// <remarks>
/// The graph defines commands, options, arguments, arity, defaults, and validators once. The parser library owns
/// tokenization, arity, option applicability, and help; <see cref="CliApplication"/> owns the root
/// <c>--version</c> action, parse-error mapping, dispatch, and exit-code policy. No hand-written parser or
/// command catalog exists.
/// </remarks>
internal sealed class CliCommandGraph
{
    /// <summary>Initializes the complete command graph.</summary>
    public CliCommandGraph()
    {
        Root = new RootCommand("Build, inspect, and verify MOCASSIN simulation packages.")
        {
            HelpName = "mocassin",
        };

        ValidateJson = JsonOption();
        ValidateWarningsAsErrors = WarningsAsErrorsOption();
        ValidateStats = new Option<bool>("--stats")
        {
            Description = "Include model statistics in the validation output.",
        };
        ValidateProject = new Argument<string>("project")
        {
            Description = "The .mocprj project file to validate.",
        };
        ValidateCommand = new Command(CliCommandNames.Validate, "Validate a project document.");
        ValidateCommand.Add(ValidateProject);
        ValidateCommand.Add(ValidateJson);
        ValidateCommand.Add(ValidateWarningsAsErrors);
        ValidateCommand.Add(ValidateStats);

        BuildJson = JsonOption();
        BuildOutput = new Option<string>("-o", "--output")
        {
            Description = "The output .msl package path.",
        };
        BuildPreview = new Option<bool>("--preview")
        {
            Description = "Run the no-write preview instead of writing a package.",
        };
        BuildTemplate = new Option<Guid>("--template")
        {
            Description = "The build template ID.",
        };
        BuildJobTemplate = new Option<Guid>("--job-template")
        {
            Description = "The job template ID.",
        };
        BuildParameterization = new Option<Guid>("--parameterization")
        {
            Description = "The parameterization template ID.",
        };
        BuildJobs = new Option<string>("--jobs")
        {
            Description = "The 1-based job selection: all, 1-3, 2,5, or a mixed list.",
        };
        BuildParallel = new Option<int>("--parallel")
        {
            Description = "The maximum compile and lattice-generation concurrency (1..256).",
            DefaultValueFactory = _ => 0,
        };
        BuildParallel.Validators.Add(result =>
        {
            if (!result.Implicit && result.GetValueOrDefault<int>() is < 1 or > 256)
            {
                result.AddError("--parallel must be between 1 and 256.");
            }
        });
        BuildCompact = new Option<bool>("--compact")
        {
            Description = "Apply the Zstd quality 9 and VACUUM size preset.",
        };
        BuildForce = new Option<bool>("--force")
        {
            Description = "Replace an existing package atomically.",
        };
        BuildWarningsAsErrors = WarningsAsErrorsOption();
        BuildQuiet = new Option<bool>("--quiet")
        {
            Description = "Suppress stage, progress, and log events.",
        };
        BuildProject = new Argument<string>("project")
        {
            Description = "The .mocprj project file to build.",
        };
        BuildCommand = new Command(CliCommandNames.Build, "Build an immutable .msl package or preview it.");
        BuildCommand.Add(BuildProject);
        BuildCommand.Add(BuildOutput);
        BuildCommand.Add(BuildPreview);
        BuildCommand.Add(BuildTemplate);
        BuildCommand.Add(BuildJobTemplate);
        BuildCommand.Add(BuildParameterization);
        BuildCommand.Add(BuildJobs);
        BuildCommand.Add(BuildParallel);
        BuildCommand.Add(BuildCompact);
        BuildCommand.Add(BuildForce);
        BuildCommand.Add(BuildWarningsAsErrors);
        BuildCommand.Add(BuildQuiet);
        BuildCommand.Add(BuildJson);

        NewJson = JsonOption();
        NewName = new Argument<string>("name")
        {
            Description = "The project display name and file stem.",
        };
        NewOutput = new Option<string>("-o", "--output")
        {
            Description = "The directory that receives the project (default: the current directory).",
        };
        NewTemplate = new Option<string>("--template")
        {
            Description = "The starter template: blank or ceria (default: blank).",
            DefaultValueFactory = _ => "blank",
        };
        NewTemplate.AcceptOnlyFromAmong("blank", "ceria");
        NewCommand = new Command(CliCommandNames.New, "Create a new project document.");
        NewCommand.Add(NewName);
        NewCommand.Add(NewOutput);
        NewCommand.Add(NewTemplate);
        NewCommand.Add(NewJson);

        CompileJson = JsonOption();
        CompileProject = new Argument<string>("project")
        {
            Description = "The .mocprj project file to compile.",
        };
        CompileOutput = new Option<string>("-o", "--output")
        {
            Description = "Write the canonical compiled-inspection document to this path.",
        };
        CompileCommand = new Command(CliCommandNames.Compile, "Compile the model without writing a package.");
        CompileCommand.Add(CompileProject);
        CompileCommand.Add(CompileOutput);
        CompileCommand.Add(CompileJson);

        InspectJson = JsonOption();
        InspectPackage = new Argument<string>("package")
        {
            Description = "The .msl package to inspect.",
        };
        InspectCommand = new Command(CliCommandNames.Inspect, "Inspect a package read-only.");
        InspectCommand.Add(InspectPackage);
        InspectCommand.Add(InspectJson);

        VerifyJson = JsonOption();
        VerifyPackage = new Argument<string>("package")
        {
            Description = "The .msl package to verify.",
        };
        VerifyCommand = new Command(CliCommandNames.Verify, "Verify a package read-only.");
        VerifyCommand.Add(VerifyPackage);
        VerifyCommand.Add(VerifyJson);

        DoctorJson = JsonOption();
        DoctorCommand = new Command(CliCommandNames.Doctor, "Diagnose the runtime and the environment.");
        DoctorCommand.Add(DoctorJson);

        CleanJson = JsonOption();
        CleanPackage = new Argument<string>("package")
        {
            Description = "The .msl package whose job folders are listed.",
        };
        CleanDirectory = new Option<string>("--dir")
        {
            Description = "The job-folder directory (default: the package directory).",
        };
        CleanOlderThan = new Option<int>("--older-than")
        {
            Description = "List only folders older than this many days (0..36500).",
            DefaultValueFactory = _ => 0,
        };
        CleanOlderThan.Validators.Add(result =>
        {
            if (!result.Implicit && result.GetValueOrDefault<int>() is < 0 or > 36_500)
            {
                result.AddError("--older-than must be between 0 and 36500.");
            }
        });
        CleanDryRun = new Option<bool>("--dry-run")
        {
            Description = "List the job folders without deleting anything (the only Step 12 mode).",
        };
        CleanResults = new Option<string>("--results")
        {
            Description = "Reserved for the results store (Step 20/28).",
        };
        CleanCommand = new Command(CliCommandNames.Clean, "List job folders; deletion arrives in Step 28.");
        CleanCommand.Add(CleanPackage);
        CleanCommand.Add(CleanDirectory);
        CleanCommand.Add(CleanOlderThan);
        CleanCommand.Add(CleanDryRun);
        CleanCommand.Add(CleanResults);
        CleanCommand.Add(CleanJson);

        SimulateCommand = Reserved(CliCommandNames.Simulate);
        CollectCommand = Reserved(CliCommandNames.Collect);
        EvaluateCommand = Reserved(CliCommandNames.Evaluate);
        ExportCommand = Reserved(CliCommandNames.Export);

        Root.Add(NewCommand);
        Root.Add(ValidateCommand);
        Root.Add(CompileCommand);
        Root.Add(BuildCommand);
        Root.Add(InspectCommand);
        Root.Add(VerifyCommand);
        Root.Add(DoctorCommand);
        Root.Add(CleanCommand);
        Root.Add(SimulateCommand);
        Root.Add(CollectCommand);
        Root.Add(EvaluateCommand);
        Root.Add(ExportCommand);

        foreach (var command in Root.Subcommands)
        {
            command.Add(VersionGuardOption());
        }

        var versionOption = Root.Options.OfType<VersionOption>().Single();
        versionOption.Action = new CliVersionAction();
    }

    /// <summary>Gets the root command.</summary>
    public RootCommand Root { get; }

    /// <summary>Gets the <c>validate</c> command.</summary>
    public Command ValidateCommand { get; }

    /// <summary>Gets the <c>validate</c> project argument.</summary>
    public Argument<string> ValidateProject { get; }

    /// <summary>Gets the <c>validate --json</c> option.</summary>
    public Option<bool> ValidateJson { get; }

    /// <summary>Gets the <c>validate --warnings-as-errors</c> option.</summary>
    public Option<bool> ValidateWarningsAsErrors { get; }

    /// <summary>Gets the <c>validate --stats</c> option.</summary>
    public Option<bool> ValidateStats { get; }

    /// <summary>Gets the <c>build</c> command.</summary>
    public Command BuildCommand { get; }

    /// <summary>Gets the <c>build</c> project argument.</summary>
    public Argument<string> BuildProject { get; }

    /// <summary>Gets the <c>build -o</c> option.</summary>
    public Option<string> BuildOutput { get; }

    /// <summary>Gets the <c>build --preview</c> option.</summary>
    public Option<bool> BuildPreview { get; }

    /// <summary>Gets the <c>build --template</c> option.</summary>
    public Option<Guid> BuildTemplate { get; }

    /// <summary>Gets the <c>build --job-template</c> option.</summary>
    public Option<Guid> BuildJobTemplate { get; }

    /// <summary>Gets the <c>build --parameterization</c> option.</summary>
    public Option<Guid> BuildParameterization { get; }

    /// <summary>Gets the <c>build --jobs</c> option.</summary>
    public Option<string> BuildJobs { get; }

    /// <summary>Gets the <c>build --parallel</c> option.</summary>
    public Option<int> BuildParallel { get; }

    /// <summary>Gets the <c>build --compact</c> option.</summary>
    public Option<bool> BuildCompact { get; }

    /// <summary>Gets the <c>build --force</c> option.</summary>
    public Option<bool> BuildForce { get; }

    /// <summary>Gets the <c>build --warnings-as-errors</c> option.</summary>
    public Option<bool> BuildWarningsAsErrors { get; }

    /// <summary>Gets the <c>build --quiet</c> option.</summary>
    public Option<bool> BuildQuiet { get; }

    /// <summary>Gets the <c>build --json</c> option.</summary>
    public Option<bool> BuildJson { get; }

    /// <summary>Gets the <c>new</c> command.</summary>
    public Command NewCommand { get; }

    /// <summary>Gets the <c>new</c> name argument.</summary>
    public Argument<string> NewName { get; }

    /// <summary>Gets the <c>new -o</c> option.</summary>
    public Option<string> NewOutput { get; }

    /// <summary>Gets the <c>new --template</c> option.</summary>
    public Option<string> NewTemplate { get; }

    /// <summary>Gets the <c>new --json</c> option.</summary>
    public Option<bool> NewJson { get; }

    /// <summary>Gets the <c>compile</c> command.</summary>
    public Command CompileCommand { get; }

    /// <summary>Gets the <c>compile</c> project argument.</summary>
    public Argument<string> CompileProject { get; }

    /// <summary>Gets the <c>compile -o</c> option.</summary>
    public Option<string> CompileOutput { get; }

    /// <summary>Gets the <c>compile --json</c> option.</summary>
    public Option<bool> CompileJson { get; }

    /// <summary>Gets the <c>inspect</c> command.</summary>
    public Command InspectCommand { get; }

    /// <summary>Gets the <c>inspect</c> package argument.</summary>
    public Argument<string> InspectPackage { get; }

    /// <summary>Gets the <c>inspect --json</c> option.</summary>
    public Option<bool> InspectJson { get; }

    /// <summary>Gets the <c>verify</c> command.</summary>
    public Command VerifyCommand { get; }

    /// <summary>Gets the <c>verify</c> package argument.</summary>
    public Argument<string> VerifyPackage { get; }

    /// <summary>Gets the <c>verify --json</c> option.</summary>
    public Option<bool> VerifyJson { get; }

    /// <summary>Gets the <c>doctor</c> command.</summary>
    public Command DoctorCommand { get; }

    /// <summary>Gets the <c>doctor --json</c> option.</summary>
    public Option<bool> DoctorJson { get; }

    /// <summary>Gets the <c>clean</c> command.</summary>
    public Command CleanCommand { get; }

    /// <summary>Gets the <c>clean</c> package argument.</summary>
    public Argument<string> CleanPackage { get; }

    /// <summary>Gets the <c>clean --dir</c> option.</summary>
    public Option<string> CleanDirectory { get; }

    /// <summary>Gets the <c>clean --older-than</c> option.</summary>
    public Option<int> CleanOlderThan { get; }

    /// <summary>Gets the <c>clean --dry-run</c> option.</summary>
    public Option<bool> CleanDryRun { get; }

    /// <summary>Gets the <c>clean --results</c> option.</summary>
    public Option<string> CleanResults { get; }

    /// <summary>Gets the <c>clean --json</c> option.</summary>
    public Option<bool> CleanJson { get; }

    /// <summary>Gets the reserved <c>simulate</c> command.</summary>
    public Command SimulateCommand { get; }

    /// <summary>Gets the reserved <c>collect</c> command.</summary>
    public Command CollectCommand { get; }

    /// <summary>Gets the reserved <c>evaluate</c> command.</summary>
    public Command EvaluateCommand { get; }

    /// <summary>Gets the reserved <c>export</c> command.</summary>
    public Command ExportCommand { get; }

    /// <summary>
    /// Returns whether a command is one of the report-backed Step 12 commands.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns><see langword="true"/> for <c>validate</c> and the seven report commands.</returns>
    public bool IsReportBacked(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ReferenceEquals(command, ValidateCommand)
            || ReferenceEquals(command, NewCommand)
            || ReferenceEquals(command, CompileCommand)
            || ReferenceEquals(command, BuildCommand)
            || ReferenceEquals(command, InspectCommand)
            || ReferenceEquals(command, VerifyCommand)
            || ReferenceEquals(command, DoctorCommand)
            || ReferenceEquals(command, CleanCommand);
    }

    private static Option<bool> JsonOption() =>
        new("--json")
        {
            Description = "Print exactly one JSON report on standard output.",
        };

    private static Option<bool> WarningsAsErrorsOption() =>
        new("--warnings-as-errors")
        {
            Description = "Promote validation warnings to errors.",
        };

    private static Option<bool> VersionGuardOption() =>
        new("--version")
        {
            Hidden = true,
            Action = new CliSubcommandVersionAction(),
        };

    private static Command Reserved(string name)
    {
        var command = new Command(name, $"{name} is reserved for a later step.")
        {
            TreatUnmatchedTokensAsErrors = false,
        };
        return command;
    }
}
