// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Tests.TestSupport;

/// <summary>
/// A clock that never advances.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public FixedTimeProvider(DateTimeOffset now) => this.now = now;

    public override DateTimeOffset GetUtcNow() => now;

    public override long GetTimestamp() => 0;
}
