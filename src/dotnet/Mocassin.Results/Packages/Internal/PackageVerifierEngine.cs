// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Runs the ordered <c>PKG</c> verification checks over a read package.
/// </summary>
internal static class PackageVerifierEngine
{
    /// <summary>Verifies a read package.</summary>
    public static PackageVerificationReport Verify(PackageReadResult read, PackageVerificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(options);
        var checks = ImmutableArray.CreateBuilder<PackageVerificationCheck>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (read.FatalCode is { } fatalCode)
        {
            checks.Add(new PackageVerificationCheck(fatalCode, false, read.FatalMessage ?? fatalCode));
            diagnostics.Add(Diagnostic(fatalCode, read.FatalMessage ?? fatalCode));
            return new PackageVerificationReport(
                false,
                checks.ToImmutable(),
                diagnostics.ToImmutable(),
                PackageInspectionMapper.Empty);
        }

        CheckImmutability(read, checks, diagnostics);
        checks.Add(new PackageVerificationCheck(
            PackageDiagnosticCodes.UnsupportedSchema,
            true,
            "application_id, user_version, and schema_info agree."));
        CheckReferences(read, checks, diagnostics);
        CheckPayloads(read, checks, diagnostics);
        CheckModelInvariants(read, checks, diagnostics);
        CheckModelSignatures(read, checks, diagnostics);
        CheckLattices(read, checks, diagnostics);
        CheckColumns(read, checks, diagnostics);
        CheckFlags(read, checks, diagnostics);
        CheckSize(read, options, checks, diagnostics);

        var isValid = !diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return new PackageVerificationReport(
            isValid,
            checks.ToImmutable(),
            diagnostics.ToImmutable(),
            PackageInspectionMapper.Map(read));
    }

    private static void CheckImmutability(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var issues = new List<string>();
        if (read.HasResultsTable)
        {
            issues.Add("a results table is present");
        }

        if (read.HasWalSidecar)
        {
            issues.Add("a -wal or -shm sidecar is present");
        }

        if (read.IsWalJournal)
        {
            issues.Add("journal_mode is WAL");
        }

        if (issues.Count == 0)
        {
            checks.Add(new PackageVerificationCheck(
                PackageDiagnosticCodes.UnreadableDatabase,
                true,
                "The package is readable, has the required schema, and is immutable."));
            return;
        }

        var message = "The package is not immutable: " + string.Join("; ", issues) + ".";
        checks.Add(new PackageVerificationCheck(PackageDiagnosticCodes.NotImmutable, false, message));
        diagnostics.Add(Diagnostic(PackageDiagnosticCodes.NotImmutable, message));
    }

    private static void CheckReferences(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var modelIds = read.Models.Select(static model => model.ModelId).ToHashSet();
        var latticeIds = read.Lattices.Select(static lattice => lattice.LatticeId).ToHashSet();
        var jobIds = new HashSet<long>();
        var triples = new HashSet<(long, long, long)>();
        var issues = new List<string>();
        long previousJobId = 0;
        foreach (var job in read.Jobs)
        {
            if (job.JobId <= 0 || job.JobId <= previousJobId || !jobIds.Add(job.JobId))
            {
                issues.Add($"job_id {job.JobId} is not unique and strictly ascending.");
            }

            previousJobId = job.JobId;
            if (!modelIds.Contains(job.ModelId))
            {
                issues.Add($"job {job.JobId} references missing model {job.ModelId}.");
            }

            if (!latticeIds.Contains(job.LatticeId))
            {
                issues.Add($"job {job.JobId} references missing lattice {job.LatticeId}.");
            }

            if (string.IsNullOrWhiteSpace(job.CollectionName) || string.IsNullOrWhiteSpace(job.ConfigName))
            {
                issues.Add($"job {job.JobId} has an empty collection or configuration name.");
            }

            if (job.CollectionIndex < 0 || job.ConfigIndex < 0 || job.JobIndex < 0)
            {
                issues.Add($"job {job.JobId} has a negative authored index.");
            }

            if (!triples.Add((job.CollectionIndex, job.ConfigIndex, job.JobIndex)))
            {
                issues.Add($"job {job.JobId} duplicates the (collection, config, job) index triple.");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.InvalidReference,
            issues,
            "Every job references an existing model and lattice with unique ascending ids.");
    }

    private static void CheckPayloads(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var issues = new List<string>();
        foreach (var model in read.Models)
        {
            if (model.DecodeError is { } error)
            {
                issues.Add($"model {model.Signature}: {error}");
            }
            else if (model.Decoded is { } payload && payload.FormatVersion != model.FormatVersion)
            {
                issues.Add($"model {model.Signature}: payload format_version disagrees with the column.");
            }
        }

        foreach (var lattice in read.Lattices)
        {
            if (lattice.DecodeError is { } error)
            {
                issues.Add($"lattice {lattice.Signature}: {error}");
            }
            else if (lattice.Decoded is { } payload && payload.FormatVersion != lattice.FormatVersion)
            {
                issues.Add($"lattice {lattice.Signature}: payload format_version disagrees with the column.");
            }
        }

        foreach (var job in read.Jobs)
        {
            if (job.DecodeError is { } error)
            {
                issues.Add($"job {job.JobId}: {error}");
            }
            else if (job.Decoded is { } payload && payload.FormatVersion != 1)
            {
                issues.Add($"job {job.JobId}: unsupported payload format_version.");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.InvalidPayload,
            issues,
            "Every payload decodes as a bounds-valid Zstd/MessagePack map.");
    }

    private static void CheckModelInvariants(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var issues = new List<string>();
        foreach (var model in read.Models)
        {
            if (model.Decoded is not { } payload)
            {
                continue;
            }

            try
            {
                var errors = new List<string>();
                PackageVerificationChecks.ValidateModel(payload, errors);
                issues.AddRange(errors.Select(error => $"model {model.Signature}: {error}"));
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or OverflowException)
            {
                issues.Add($"model {model.Signature}: payload validation failed: {exception.Message}");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.OutOfBounds,
            issues,
            "Model payload table, index, tracker, and numeric bounds are valid.");
    }

    private static void CheckModelSignatures(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var issues = new List<string>();
        foreach (var model in read.Models)
        {
            if (model.Decoded is not { } payload)
            {
                continue;
            }

            if (!PackageVerificationChecks.IsHex64(payload.ModelSignature))
            {
                issues.Add($"model {model.Signature}: model_signature is not a 64-hex identity string.");
            }

            try
            {
                var values = PackageVerificationChecks.ValuesFrom(payload);
                var reencoded = ParameterizationSignatureV1.Compute(payload.ModelSignature, values);
                if (!string.Equals(reencoded, model.Signature, StringComparison.Ordinal) ||
                    !string.Equals(reencoded, payload.Signature, StringComparison.Ordinal))
                {
                    issues.Add($"model {model.Signature}: the parameterization projection does not match the stored signature.");
                }
            }
            catch (InvalidOperationException exception)
            {
                issues.Add($"model {model.Signature}: the parameterization projection cannot be re-encoded: {exception.Message}");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.SignatureMismatch,
            issues,
            "The parameterization projection re-encodes to models.signature.");
    }

    private static void CheckLattices(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var issues = new List<string>();
        var model = read.Models.FirstOrDefault(static item => item.Decoded is not null);
        if (model?.Decoded is not { } modelPayload)
        {
            issues.Add("No decoded model is available to verify the lattice projection.");
            AddCheck(
                checks,
                diagnostics,
                PackageDiagnosticCodes.SignatureMismatch,
                issues,
                "The lattice projection re-encodes to lattices.signature.");
            return;
        }

        foreach (var lattice in read.Lattices)
        {
            if (lattice.Decoded is not { } payload)
            {
                continue;
            }

            try
            {
                var errors = new List<string>();
                PackageVerificationChecks.ValidateLattice(payload, (int)modelPayload.Particle.Count, errors);
                issues.AddRange(errors.Select(error => $"lattice {lattice.Signature}: {error}"));
                var reencoded = LatticeSignatureV1.Compute(modelPayload.ModelSignature, payload);
                if (!string.Equals(reencoded, lattice.Signature, StringComparison.Ordinal))
                {
                    issues.Add($"lattice {lattice.Signature}: the lattice projection does not match the stored signature.");
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or OverflowException)
            {
                issues.Add($"lattice {lattice.Signature}: payload validation failed: {exception.Message}");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.SignatureMismatch,
            issues,
            "The lattice projection re-encodes to lattices.signature.");
    }

    private static void CheckColumns(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var lattices = read.Lattices.ToDictionary(static lattice => lattice.LatticeId);
        var issues = new List<string>();
        foreach (var job in read.Jobs)
        {
            if (job.Decoded is not { } payload)
            {
                continue;
            }

            if (!lattices.TryGetValue(job.LatticeId, out var lattice) || lattice.Decoded is not { } latticePayload)
            {
                continue;
            }

            if (!string.Equals(job.Kind, payload.Kind, StringComparison.Ordinal) ||
                job.TemperatureK != payload.TemperatureK ||
                job.ElectricFieldVm != payload.ElectricFieldVm ||
                job.TargetMcsp != (long)payload.TargetMcsp ||
                job.WriteOutCount != (int)payload.WriteOutCount ||
                job.PreRunMcsp != (long)payload.PreRunMcsp ||
                job.TimeLimitS != (long?)payload.TimeLimitS ||
                job.MinSuccessRate != payload.MinSuccessRate ||
                job.DopingInfo != payload.DopingInfo ||
                job.Flags != (long)payload.Flags)
            {
                issues.Add($"job {job.JobId}: a payload-derived column disagrees with the payload.");
            }

            if (latticePayload.LatticeSize.IsDefaultOrEmpty || latticePayload.LatticeSize.Length != 3)
            {
                issues.Add($"job {job.JobId}: the referenced lattice payload has no valid lattice_size.");
                continue;
            }

            if (job.LatticeA != (int)latticePayload.LatticeSize[0] ||
                job.LatticeB != (int)latticePayload.LatticeSize[1] ||
                job.LatticeC != (int)latticePayload.LatticeSize[2])
            {
                issues.Add($"job {job.JobId}: the lattice size columns disagree with the lattice payload.");
            }

            if (job.Seed.Length != 32)
            {
                issues.Add($"job {job.JobId}: the seed is not 32 bytes.");
            }

            if (!DateTimeOffset.TryParse(
                    job.CreatedUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out _))
            {
                issues.Add($"job {job.JobId}: created_utc is not a timestamp.");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.ColumnMismatch,
            issues,
            "Payload-derived columns equal the decoded payload values.");
    }

    private static void CheckFlags(
        PackageReadResult read,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var issues = new List<string>();
        foreach (var job in read.Jobs)
        {
            if (job.Decoded is not { } payload)
            {
                continue;
            }

            try
            {
                var errors = new List<string>();
                PackageVerificationChecks.ValidateFlags(payload, errors);
                issues.AddRange(errors.Select(error => $"job {job.JobId}: {error}"));
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or OverflowException)
            {
                issues.Add($"job {job.JobId}: flag validation failed: {exception.Message}");
            }
        }

        AddCheck(
            checks,
            diagnostics,
            PackageDiagnosticCodes.OutOfBounds,
            issues,
            "The flags word has one kind bit and consistent pre-run/dual-DOF intent.");
    }

    private static void CheckSize(
        PackageReadResult read,
        PackageVerificationOptions options,
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (read.SizeBytes <= options.AdvisorySizeBytes)
        {
            checks.Add(new PackageVerificationCheck(
                PackageDiagnosticCodes.AdvisorySize,
                true,
                $"The package size {read.SizeBytes} is within the advisory limit."));
            return;
        }

        var message = string.Create(
            CultureInfo.InvariantCulture,
            $"The package size {read.SizeBytes} exceeds the advisory limit {options.AdvisorySizeBytes}.");
        checks.Add(new PackageVerificationCheck(PackageDiagnosticCodes.AdvisorySize, false, message));
        diagnostics.Add(Diagnostic(PackageDiagnosticCodes.AdvisorySize, message));
    }

    private static void AddCheck(
        ImmutableArray<PackageVerificationCheck>.Builder checks,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        string code,
        List<string> issues,
        string passedMessage)
    {
        if (issues.Count == 0)
        {
            checks.Add(new PackageVerificationCheck(code, true, passedMessage));
            return;
        }

        checks.Add(new PackageVerificationCheck(code, false, issues[0]));
        foreach (var issue in issues)
        {
            diagnostics.Add(Diagnostic(code, issue));
        }
    }

    private static Diagnostic Diagnostic(string code, string message)
    {
        var descriptor = PackageDiagnosticDescriptors.All
            .FirstOrDefault(descriptor => string.Equals(descriptor.Code, code, StringComparison.Ordinal));
        return new Diagnostic(
            code,
            descriptor?.DefaultSeverity ?? DiagnosticSeverity.Error,
            message,
            hint: descriptor?.Help);
    }
}
