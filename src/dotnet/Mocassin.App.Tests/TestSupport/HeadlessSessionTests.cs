// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Guards the async headless dispatch: the inner task must be awaited so assertions and exceptions surface.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class HeadlessSessionTests
{
    [Fact]
    public async Task AFailureAfterAnAwaitIsObserved()
    {
        var task = HeadlessSession.DispatchAsync(
            async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("observed after await");
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal("observed after await", exception.Message);
    }

    [Fact]
    public async Task AnAssertionAfterAnAwaitIsObserved()
    {
        var task = HeadlessSession.DispatchAsync(
            async () =>
            {
                await Task.Yield();
                Assert.Fail("assertion after await");
            });

        await Assert.ThrowsAnyAsync<Exception>(() => task);
    }
}
