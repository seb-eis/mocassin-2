// SPDX-License-Identifier: MIT

using System.CommandLine;
using System.CommandLine.Parsing;

// The AOT smoke host defines a representative command/option/argument shape, disables the library's
// response-file hook exactly like the real CLI, and maps every parse error to CLI001/exit 2.
var root = BuildRoot();
return Run(root, args);

static RootCommand BuildRoot()
{
    var root = new RootCommand("MOCASSIN System.CommandLine AOT smoke host")
    {
        HelpName = "mocassin-aot",
    };

    var path = new Argument<string>("project") { Description = "The project path." };
    var json = new Option<bool>("--json") { Description = "Print one JSON report." };
    var parallel = new Option<int>("--parallel") { Description = "Concurrency.", DefaultValueFactory = _ => 1 };
    parallel.Validators.Add(result =>
    {
        if (!result.Implicit && result.GetValueOrDefault<int>() is < 1 or > 256)
        {
            result.AddError("--parallel must be between 1 and 256.");
        }
    });

    var validate = new Command("validate", "Validate a project document.");
    validate.Add(path);
    validate.Add(json);
    validate.Add(parallel);
    validate.SetAction(parseResult =>
    {
        Console.WriteLine($"validate {parseResult.GetValue(path)} json={parseResult.GetValue(json)}");
        return 0;
    });

    var build = new Command("build", "Build a package.");
    var preview = new Option<bool>("--preview") { Description = "Preview without writing." };
    build.Add(preview);
    build.SetAction(parseResult =>
    {
        Console.WriteLine($"build preview={parseResult.GetValue(preview)}");
        return 0;
    });

    root.Add(validate);
    root.Add(build);
    return root;
}

static int Run(RootCommand root, string[] arguments)
{
    var configuration = new ParserConfiguration
    {
        EnablePosixBundling = false,
        ResponseFileTokenReplacer = null,
    };
    var parseResult = root.Parse(arguments, configuration);
    if (parseResult.Errors.Count > 0)
    {
        foreach (var error in parseResult.Errors)
        {
            Console.Error.WriteLine($"CLI001: Invalid command line: {error.Message}");
        }

        return 2;
    }

    return parseResult.Invoke();
}
