// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// Validates one nullable <see cref="JobSettings"/> override layer against the Step 06 hard ranges.
/// </summary>
/// <remarks>
/// A <see langword="null"/> field inherits and is not checked. Range violations are <c>JOB015</c>; invalid
/// extension content or bounds are <c>JOB018</c>. The validator never throws for user data.
/// </remarks>
public static class JobSettingsValidator
{
    private const int maxExtensions = 16;
    private const int maxExtensionIdLength = 64;
    private const int maxExtensionConfigBytes = 64 * 1024;
    private const int maxExtensionTotalBytes = 256 * 1024;

    /// <summary>
    /// Validates the non-null fields of one settings layer.
    /// </summary>
    /// <param name="settings">The override layer to validate, or <see langword="null"/> for an inheriting layer.</param>
    /// <param name="kind">
    /// The owning simulation kind, or <see langword="null"/> when the reference is unknown and kind applicability
    /// cannot yet be checked.
    /// </param>
    /// <param name="path">The project path of the owning settings object.</param>
    /// <param name="validateKindApplicability">
    /// Whether KMC-only and MMC-only fields are rejected on the other simulation kind. Compiled defaults can set
    /// this to <see langword="false"/> when they deliberately carry irrelevant zero values.
    /// </param>
    /// <returns>The diagnostics in stable order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ImmutableArray<Diagnostic> Validate(
        JobSettings? settings,
        SimulationKind? kind,
        ModelPath path,
        bool validateKindApplicability = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        var bag = new DiagnosticBag();
        if (settings is null)
        {
            return bag.Sorted;
        }

        ValidateCommon(settings, path, bag);
        ValidateKindSpecific(settings, kind, path, validateKindApplicability, bag);
        ValidateExtensions(settings.Extensions, path, bag);
        return bag.Sorted;
    }

    private static void ValidateCommon(JobSettings settings, ModelPath path, DiagnosticBag bag)
    {
        if (settings.TemperatureK is { } temperature &&
            (!double.IsFinite(temperature) || temperature is < 0.1 or > 10_000.0))
        {
            AddSettingsError(
                bag,
                path,
                "temperatureK",
                "Temperature must be finite and within 0.1..10000 K.");
        }

        if (settings.TargetMcsp is { } targetMcsp && targetMcsp is < 1 or > 1_000_000)
        {
            AddSettingsError(bag, path, "targetMcsp", "Target MCSP must be within 1..1,000,000.");
        }

        if (settings.WriteOutCount is { } writeOutCount && writeOutCount is < 0 or > 1000)
        {
            AddSettingsError(bag, path, "writeOutCount", "Write-out count must be within 0..1000.");
        }

        if (settings.TimeLimitSeconds is { } timeLimit && timeLimit <= 0)
        {
            AddSettingsError(bag, path, "timeLimitSeconds", "Time limit must be positive when present.");
        }

        if (settings.MinSuccessRate is { } minSuccessRate &&
            (!double.IsFinite(minSuccessRate) ||
             minSuccessRate < 0.0 ||
             minSuccessRate > int.MaxValue))
        {
            AddSettingsError(
                bag,
                path,
                "minSuccessRate",
                "Minimum success rate must be finite and within 0..int.MaxValue.");
        }

        if (settings.Flags is { } flags && (flags & ~ExecutionFlagsJsonConverter.AuthoredMask) != 0)
        {
            AddSettingsError(
                bag,
                path,
                "flags",
                "Execution flags contain a duplicate, derived, or compiler-owned flag.");
        }
    }

    private static void ValidateKindSpecific(
        JobSettings settings,
        SimulationKind? kind,
        ModelPath path,
        bool validateKindApplicability,
        DiagnosticBag bag)
    {
        if (validateKindApplicability && kind == SimulationKind.Mmc)
        {
            AddKindError(bag, path, settings.PreRunMcsp, "preRunMcsp");
            AddKindError(bag, path, settings.ElectricFieldModulusVm, "electricFieldModulusVm");
            AddKindError(bag, path, settings.ElectricFieldDirection, "electricFieldDirection");
            AddKindError(bag, path, settings.NormalizationEnergyEv, "normalizationEnergyEv");
            AddKindError(bag, path, settings.MaxAttemptFrequencyHz, "maxAttemptFrequencyHz");
        }

        if (validateKindApplicability && kind == SimulationKind.Kmc)
        {
            AddKindError(bag, path, settings.BreakTolerance, "breakTolerance");
            AddKindError(bag, path, settings.BreakSampleLengthMcsp, "breakSampleLengthMcsp");
            AddKindError(bag, path, settings.BreakSampleIntervalMcsp, "breakSampleIntervalMcsp");
            AddKindError(bag, path, settings.ResultSampleMcsp, "resultSampleMcsp");
        }

        if (settings.PreRunMcsp is { } preRunMcsp && preRunMcsp is < 0 or > 1_000_000)
        {
            AddSettingsError(bag, path, "preRunMcsp", "Pre-run MCSP must be within 0..1,000,000.");
        }

        if (settings.ElectricFieldModulusVm is { } fieldModulus &&
            (!double.IsFinite(fieldModulus) || fieldModulus < 0.0))
        {
            AddSettingsError(
                bag,
                path,
                "electricFieldModulusVm",
                "Electric-field modulus must be finite and non-negative.");
        }

        if (settings.ElectricFieldDirection is { } direction && !direction.IsFinite)
        {
            AddSettingsError(
                bag,
                path,
                "electricFieldDirection",
                "Electric-field direction must have three finite components.");
        }

        if (settings.NormalizationEnergyEv is { } normalizationEnergy &&
            (!double.IsFinite(normalizationEnergy) || normalizationEnergy < 0.0))
        {
            AddSettingsError(
                bag,
                path,
                "normalizationEnergyEv",
                "Normalization energy must be finite and non-negative when present.");
        }

        if (settings.MaxAttemptFrequencyHz is { } maxAttemptFrequency &&
            (!double.IsFinite(maxAttemptFrequency) || maxAttemptFrequency <= 0.0))
        {
            AddSettingsError(
                bag,
                path,
                "maxAttemptFrequencyHz",
                "Maximum attempt frequency must be finite and positive when present.");
        }

        if (settings.BreakTolerance is { } breakTolerance &&
            (!double.IsFinite(breakTolerance) || breakTolerance is < 0.0 or > 1.0))
        {
            AddSettingsError(
                bag,
                path,
                "breakTolerance",
                "Break tolerance must be finite and within 0..1.");
        }

        if (settings.BreakSampleLengthMcsp is { } breakSampleLength &&
            breakSampleLength is < 0 or > 100_000)
        {
            AddSettingsError(
                bag,
                path,
                "breakSampleLengthMcsp",
                "Break sample length must be within 0..100,000 MCSP.");
        }

        if (settings.BreakSampleIntervalMcsp is { } breakSampleInterval &&
            breakSampleInterval is < 1 or > 1000)
        {
            AddSettingsError(
                bag,
                path,
                "breakSampleIntervalMcsp",
                "Break sample interval must be within 1..1000 MCSP.");
        }

        if (settings.ResultSampleMcsp is { } resultSample && resultSample is < 0 or > 1_000_000)
        {
            AddSettingsError(
                bag,
                path,
                "resultSampleMcsp",
                "Result sample must be within 0..1,000,000 MCSP.");
        }
    }

    private static void ValidateExtensions(
        ImmutableArray<JobExtension>? extensions,
        ModelPath path,
        DiagnosticBag bag)
    {
        if (extensions is not { } entries || entries.IsDefault)
        {
            return;
        }

        if (entries.Length > maxExtensions)
        {
            bag.Add(ExtensionError(
                path,
                "extensions",
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"A settings layer may contain at most {maxExtensions} extensions; it contains {entries.Length}.")));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var totalBytes = 0;
        for (var index = 0; index < entries.Length; index++)
        {
            var extension = entries[index];
            var entryPath = $"extensions[{index}]";
            if (extension is null)
            {
                bag.Add(ExtensionError(path, entryPath, "Extension entries must not be null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(extension.Id))
            {
                bag.Add(ExtensionError(path, $"{entryPath}.id", "Extension id must not be empty."));
            }
            else
            {
                if (extension.Id.Length > maxExtensionIdLength)
                {
                    bag.Add(ExtensionError(
                        path,
                        $"{entryPath}.id",
                        string.Create(
                            System.Globalization.CultureInfo.InvariantCulture,
                            $"Extension id must not be longer than {maxExtensionIdLength} characters.")));
                }

                if (!ids.Add(extension.Id))
                {
                    bag.Add(ExtensionError(
                        path,
                        $"{entryPath}.id",
                        $"Extension id '{extension.Id}' is used more than once."));
                }
            }

            if (extension.Config.ValueKind != JsonValueKind.Object)
            {
                bag.Add(ExtensionError(
                    path,
                    $"{entryPath}.config",
                    "Extension config must be a JSON object."));
                continue;
            }

            var configBytes = Encoding.UTF8.GetByteCount(extension.Config.GetRawText());
            totalBytes += configBytes;
            if (configBytes > maxExtensionConfigBytes)
            {
                bag.Add(ExtensionError(
                    path,
                    $"{entryPath}.config",
                    string.Create(
                        System.Globalization.CultureInfo.InvariantCulture,
                        $"Extension config must not exceed {maxExtensionConfigBytes} bytes; it uses {configBytes}.")));
            }
        }

        if (totalBytes > maxExtensionTotalBytes)
        {
            bag.Add(ExtensionError(
                path,
                "extensions",
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"The extensions must not exceed {maxExtensionTotalBytes} bytes in total; they use {totalBytes}.")));
        }
    }

    private static void AddKindError<T>(DiagnosticBag bag, ModelPath path, T? value, string property)
        where T : struct
    {
        if (value is null)
        {
            return;
        }

        AddSettingsError(
            bag,
            path,
            property,
            $"'{property}' is not applicable to the collection's simulation kind.");
    }

    private static void AddKindError(DiagnosticBag bag, ModelPath path, string? value, string property)
    {
        if (value is null)
        {
            return;
        }

        AddSettingsError(
            bag,
            path,
            property,
            $"'{property}' is not applicable to the collection's simulation kind.");
    }

    private static void AddSettingsError(DiagnosticBag bag, ModelPath path, string property, string message) =>
        bag.Add(new Diagnostic(
            JobDiagnosticCodes.InvalidSettings,
            DiagnosticSeverity.Error,
            message,
            PropertyPath(path, property),
            JobDiagnosticCodes.HelpFor(JobDiagnosticCodes.InvalidSettings)));

    private static Diagnostic ExtensionError(ModelPath path, string property, string message) =>
        new(
            JobDiagnosticCodes.InvalidExtension,
            DiagnosticSeverity.Error,
            message,
            PropertyPath(path, property),
            JobDiagnosticCodes.HelpFor(JobDiagnosticCodes.InvalidExtension));

    private static ModelPath PropertyPath(ModelPath path, string property) =>
        new(
            path.Domain,
            path.ObjectId,
            string.IsNullOrEmpty(path.Property) ? property : $"{path.Property}.{property}");
}
