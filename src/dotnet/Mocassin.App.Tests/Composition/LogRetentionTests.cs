// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.Composition;

/// <summary>
/// Verifies the explicit log-size retention sweep.
/// </summary>
public sealed class LogRetentionTests
{
    [Fact]
    public void TheOldestClosedFilesAreDeletedUntilTheBudgetIsMet()
    {
        using var workspace = new TempWorkspace();
        Directory.CreateDirectory(workspace.Path);
        var oldest = CreateLog(workspace, "mocassin-20261101.log", 40, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        var middle = CreateLog(workspace, "mocassin-20261102.log", 40, new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));
        var active = CreateLog(workspace, "mocassin-20261120.log", 40, new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc));

        var deleted = LogRetention.Sweep(workspace.Path, "mocassin-20261120.log", maxTotalBytes: 90);

        Assert.Equal([oldest], deleted);
        Assert.False(File.Exists(oldest));
        Assert.True(File.Exists(middle));
        Assert.True(File.Exists(active));
    }

    [Fact]
    public void TheActiveFileIsNeverDeleted()
    {
        using var workspace = new TempWorkspace();
        Directory.CreateDirectory(workspace.Path);
        var active = CreateLog(workspace, "mocassin-20261120.log", 200, new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc));

        var deleted = LogRetention.Sweep(workspace.Path, "mocassin-20261120.log", maxTotalBytes: 1);

        Assert.Empty(deleted);
        Assert.True(File.Exists(active));
    }

    [Fact]
    public void AMissingDirectoryIsNotAnError()
    {
        using var workspace = new TempWorkspace();

        var deleted = LogRetention.Sweep(workspace.Combine("missing"), "mocassin-20261120.log");

        Assert.Empty(deleted);
    }

    private static string CreateLog(TempWorkspace workspace, string name, int bytes, DateTime timestamp)
    {
        var path = workspace.Combine(name);
        File.WriteAllBytes(path, new byte[bytes]);
        File.SetLastWriteTimeUtc(path, timestamp);
        return path;
    }
}
