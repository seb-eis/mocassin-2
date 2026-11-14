// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Benchmarks.Parameterization;

/// <summary>
/// Parameterization template creation, apply, canonical JSON, and signature benchmarks over the checked-in
/// ceria and ceria-KMC samples.
/// </summary>
/// <remarks>
/// These benchmarks are dispatched manually through <c>benchmark.yml</c>; they are not part of the normal
/// verification loop. The source projects are compiled once in <see cref="Setup"/>.
/// </remarks>
[MemoryDiagnoser]
public class ParameterizationBenchmarks
{
    private const string ceriaPath = "samples/ceria/ceria.mocprj";
    private const string ceriaParametersPath = "samples/parameterizations/ceria-parameters.mocprj";
    private const string ceriaKmcPath = "samples/simulations/ceria-kmc.mocprj";

    private readonly ParameterizationService service = new();
    private readonly ParameterizationFactory factory =
        new(TimeProvider.System);
    private readonly ParameterizationTemplateOptions options = new() { Name = "Ceria parameters" };

    private Project ceria = null!;
    private CompiledModel ceriaModel = null!;
    private Project ceriaParameters = null!;
    private CompiledModel ceriaParametersModel = null!;
    private ParameterizationTemplate ceriaTemplate = null!;
    private Project ceriaKmc = null!;
    private CompiledModel ceriaKmcModel = null!;
    private ParameterizationValues ceriaValues = null!;
    private int checksum;

    /// <summary>
    /// Loads and compiles the benchmark projects.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        ceria = LoadProject(ceriaPath);
        ceriaModel = Compile(ceria);
        ceriaParameters = LoadProject(ceriaParametersPath);
        ceriaParametersModel = Compile(ceriaParameters);
        ceriaTemplate = ceriaParameters.Parameterizations.Single();
        ceriaKmc = LoadProject(ceriaKmcPath);
        ceriaKmcModel = Compile(ceriaKmc);
        ceriaValues = Apply(ceriaParameters, ceriaParametersModel, ceriaTemplate).Values;

        checksum = ceriaModel.ModelSignature.Length
                   + ceriaParametersModel.ModelSignature.Length
                   + ceriaKmcModel.ModelSignature.Length
                   + ceriaTemplate.Name.Length;
    }

    /// <summary>
    /// Creates a ceria template and its completeness summary.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int CreateCeriaTemplate()
    {
        var result = factory.Create(ceria, ceriaModel, options);
        checksum += result.Summary.RequiredSlotCount + result.Diagnostics.Length;
        return result.Summary.RequiredSlotCount;
    }

    /// <summary>
    /// Applies the checked-in ceria template and computes the parameterization signature.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int ApplyCeria()
    {
        var model = Apply(ceriaParameters, ceriaParametersModel, ceriaTemplate);
        checksum += model.Values.SiteEnergies.Length + model.Signature.Length;
        return model.Values.SiteEnergies.Length;
    }

    /// <summary>
    /// Applies the implicit default parameterization and computes the ceria KMC signature.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int ApplyCeriaKmc()
    {
        var model = Apply(ceriaKmc, ceriaKmcModel, template: null);
        checksum += model.Values.TransitionRuleFrequencies.Length + model.Signature.Length;
        return model.Values.TransitionRuleFrequencies.Length;
    }

    /// <summary>
    /// Signs an already-applied ceria value set.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int SignatureCeria()
    {
        var signature = ParameterizationSignatureV1.Compute(ceriaParametersModel.ModelSignature, ceriaValues);
        checksum += signature.Length;
        return signature.Length;
    }

    /// <summary>
    /// Round-trips the ceria-parameters project through canonical JSON.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int CanonicalTemplateJsonRoundTrip()
    {
        var bytes = ProjectCanonicalizer.ToCanonicalUtf8(ceriaParameters);
        var project = ProjectJson.Deserialize(bytes);
        var roundTrip = ProjectCanonicalizer.ToCanonicalUtf8(project);
        checksum += roundTrip.Length;
        return roundTrip.Length;
    }

    /// <summary>
    /// Gets the checksum accumulator so the benchmark cannot be optimized away.
    /// </summary>
    public int Checksum => checksum;

    private ParameterizedModel Apply(Project project, CompiledModel model, ParameterizationTemplate? template)
    {
        var result = service.Apply(project, model, template);
        if (!result.IsSuccess || result.Model is null)
        {
            throw new InvalidOperationException(
                $"The benchmark parameterization failed: {string.Join("; ", result.Diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        return result.Model;
    }

    private static Project LoadProject(string relativePath) =>
        ProjectJson.Deserialize(File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), relativePath)));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mocassin.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the benchmark output directory.");
    }

    private static CompiledModel Compile(Project project)
    {
        var result = new ModelCompiler().Compile(project);
        if (!result.IsSuccess || result.Model is null)
        {
            throw new InvalidOperationException(
                $"The benchmark model failed to compile: {string.Join("; ", result.Diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        return result.Model;
    }
}
