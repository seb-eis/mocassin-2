// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace Mocassin.Cli.Doctor;

/// <summary>
/// Probes <c>mocassin-sim</c> from <c>MOCASSIN_SIM</c> or <c>PATH</c> with a five-second timeout.
/// </summary>
/// <remarks>
/// This is the only process boundary in <c>doctor</c>. A missing solver is reported as not configured (the
/// warning case); a configured or discovered binary that cannot report its version is reported as configured
/// with no version (the failure case).
/// </remarks>
internal sealed class ProcessSolverProbe : ISolverProbe
{
    private static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);
    private readonly string executableName = OperatingSystem.IsWindows() ? "mocassin-sim.exe" : "mocassin-sim";

    /// <inheritdoc />
    public async Task<SolverProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        var configuredPath = Environment.GetEnvironmentVariable("MOCASSIN_SIM");
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? FindOnPath()
            : configuredPath.Trim();
        if (path is null)
        {
            return new SolverProbeResult(
                Configured: false,
                Path: null,
                Version: null,
                "mocassin-sim was not found on PATH.");
        }

        if (!File.Exists(path))
        {
            return new SolverProbeResult(
                Configured: true,
                Path: path,
                Version: null,
                $"The configured solver '{path}' does not exist.");
        }

        try
        {
            var startInfo = new ProcessStartInfo(path)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("--version");
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new SolverProbeResult(true, path, null, $"The solver '{path}' could not be started.");
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                return new SolverProbeResult(
                    true,
                    path,
                    null,
                    $"The solver '{path}' did not answer --version within five seconds.");
            }

            var output = (await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).Trim();
            if (process.ExitCode != 0)
            {
                var error = (await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).Trim();
                return new SolverProbeResult(
                    true,
                    path,
                    null,
                    $"The solver '{path}' exited {process.ExitCode} for --version: {FirstLine(error)}");
            }

            var version = FirstLine(output);
            return string.IsNullOrWhiteSpace(version)
                ? new SolverProbeResult(true, path, null, $"The solver '{path}' printed no version.")
                : new SolverProbeResult(true, path, version, $"mocassin-sim {version}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception
                                              or InvalidOperationException
                                              or IOException
                                              or NotSupportedException)
        {
            return new SolverProbeResult(
                true,
                path,
                null,
                $"The solver '{path}' could not be probed: {exception.Message}");
        }
    }

    private string? FindOnPath()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string FirstLine(string text)
    {
        var line = text.Split('\n').FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item));
        return line?.Trim() ?? string.Empty;
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The process cannot be killed; the timeout result is still reported.
        }
    }
}
