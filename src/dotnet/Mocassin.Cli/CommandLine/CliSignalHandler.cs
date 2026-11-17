// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// Converts the first SIGINT or SIGTERM into cooperative cancellation.
/// </summary>
/// <remarks>
/// The first signal requests cancellation; a second signal is left to the platform default handler so a stuck
/// build can always be terminated. The token is passed to the pipeline, the writer, and the verifier.
/// </remarks>
internal sealed class CliSignalHandler : IDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly ConsoleCancelEventHandler cancelKeyPressHandler;
    private readonly List<PosixSignalRegistration> registrations = [];
    private bool disposed;

    /// <summary>
    /// Installs the signal handlers.
    /// </summary>
    public CliSignalHandler()
    {
        cancelKeyPressHandler = (_, args) =>
        {
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            args.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelKeyPressHandler;
        if (!OperatingSystem.IsWindows())
        {
            registrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }

                context.Cancel = true;
                cancellation.Cancel();
            }));
        }
    }

    /// <summary>Gets the cooperative cancellation token.</summary>
    public CancellationToken Token => cancellation.Token;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Console.CancelKeyPress -= cancelKeyPressHandler;
        foreach (var registration in registrations)
        {
            registration.Dispose();
        }

        cancellation.Dispose();
    }
}
