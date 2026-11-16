// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// Validation, version, timestamp, warning, and cleanup helpers for the package writer.
/// </summary>
internal static class SimulationPackageWriterSupport
{
    /// <summary>Validates the build request.</summary>
    public static void ValidateRequest(PackageBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JobSelection);
        if (request.ProjectId == Guid.Empty)
        {
            throw new ArgumentException("The project id must not be empty.", nameof(request));
        }
    }

    /// <summary>Validates the build options.</summary>
    public static void ValidateOptions(PackageBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.CompressionLevel is < 1 or > 19)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.CompressionLevel,
                "The compression level must be in 1..19.");
        }

        if (options.BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.BatchSize, "The batch size must be positive.");
        }

        if (options.AdvisorySizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.AdvisorySizeBytes,
                "The advisory size must be positive.");
        }
    }

    /// <summary>Validates a contract-typed model row.</summary>
    public static void ValidateModelRow(PackageModelRow model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Signature);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.SymmetryContractVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.SpaceGroupDataVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.SpaceGroupDataSha256);
        if (model.FormatVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(model), model.FormatVersion, "The payload format version must be positive.");
        }

        if (model.Payload.IsEmpty)
        {
            throw new ArgumentException("The model payload must not be empty.", nameof(model));
        }
    }

    /// <summary>Validates a contract-typed job row.</summary>
    public static void ValidateJobRow(PackageJobRow job)
    {
        if (job.JobId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(job), job.JobId, "The job id must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(job.Lattice.Signature);
        ArgumentException.ThrowIfNullOrWhiteSpace(job.CollectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(job.ConfigName);
        if (job.Lattice.FormatVersion < 1 || job.PayloadFormatVersion < 1)
        {
            throw new ArgumentException("The payload format versions must be positive.", nameof(job));
        }

        if (job.Lattice.Payload.IsEmpty || job.Payload.IsEmpty)
        {
            throw new ArgumentException("The lattice and job payloads must not be empty.", nameof(job));
        }

        if (job.Kind is not ("kmc" or "mmc"))
        {
            throw new ArgumentException("The job kind must be 'kmc' or 'mmc'.", nameof(job));
        }

        if (job.CollectionIndex < 0 || job.ConfigIndex < 0 || job.JobIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(job), "The job indices must be non-negative.");
        }

        if (job.LatticeA <= 0 || job.LatticeB <= 0 || job.LatticeC <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(job), "The lattice sizes must be positive.");
        }

        if (job.Seed.Length != 32)
        {
            throw new ArgumentException("The job seed must be 32 bytes.", nameof(job));
        }
    }

    /// <summary>Resolves the entry-assembly informational version with a stable fallback.</summary>
    public static string ResolveAppVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var informational = entryAssembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        return entryAssembly?.GetName().Version?.ToString()
            ?? typeof(SimulationPackageWriterSupport).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }

    /// <summary>Formats a build timestamp as UTC ISO 8601 with a trailing <c>Z</c>.</summary>
    public static string FormatCreatedUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);

    /// <summary>Returns the advisory <c>PKG009</c> warning when the package exceeds the limit.</summary>
    public static ImmutableArray<Diagnostic> AdvisoryWarnings(long sizeBytes, long advisorySizeBytes)
    {
        if (sizeBytes <= advisorySizeBytes)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        return
        [
            new Diagnostic(
                PackageDiagnosticCodes.AdvisorySize,
                DiagnosticSeverity.Warning,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The package is {sizeBytes} bytes, above the advisory limit of {advisorySizeBytes} bytes."),
                hint: "Reduce duplicate jobs or supercell sizes, or rebuild with --compact compression."),
        ];
    }

    /// <summary>Best-effort temp-file deletion; the target is never truncated.</summary>
    public static void TryDelete(IPackageFileSystem fileSystem, string path)
    {
        try
        {
            if (fileSystem.FileExists(path))
            {
                fileSystem.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; the target was never truncated.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup; the target was never truncated.
        }
    }
}
