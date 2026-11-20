// SPDX-License-Identifier: MIT

using Mocassin.Packages.Projects.Templates;

namespace Mocassin.Packages.Tests.Projects;

/// <summary>
/// Verifies the shared CLI/GUI project name and target rules.
/// </summary>
public sealed class ProjectPathRulesTests
{
    [Fact]
    public void ANameIsTrimmedAndOneSuffixIsRemoved()
    {
        var result = ProjectPathRules.NormalizeName("  Named.mocprj  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Named", result.Name);
        Assert.Equal(ProjectPathFailure.None, result.Failure);
    }

    [Theory]
    [InlineData("", ProjectPathFailure.EmptyName)]
    [InlineData("   ", ProjectPathFailure.EmptyName)]
    [InlineData("a/b", ProjectPathFailure.PathSeparator)]
    [InlineData("a\\b", ProjectPathFailure.PathSeparator)]
    [InlineData("..", ProjectPathFailure.ParentDirectory)]
    [InlineData("a\tb", ProjectPathFailure.ControlCharacter)]
    [InlineData("name:bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData("name*bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData("name?bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData("name\"bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData("name<bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData("name>bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData("name|bad", ProjectPathFailure.ReservedCharacter)]
    [InlineData(".mocprj", ProjectPathFailure.EmptyName)]
    public void InvalidNamesReportTheSameFailure(string name, ProjectPathFailure failure)
    {
        var result = ProjectPathRules.NormalizeName(name);

        Assert.False(result.IsSuccess);
        Assert.Equal(failure, result.Failure);
    }

    [Fact]
    public void AnOverlongNameIsRejected()
    {
        var result = ProjectPathRules.NormalizeName(new string('a', ProjectPathRules.MaxNameLength + 1));

        Assert.Equal(ProjectPathFailure.NameTooLong, result.Failure);
        Assert.Equal("The project name must not exceed 128 characters.", result.Message);
    }

    [Fact]
    public void AnAbsentTargetIsAcceptedWithTheFullPath()
    {
        var name = ProjectPathRules.NormalizeName("Named");
        var target = ProjectPathRules.ValidateTarget("/tmp/projects", name, ProjectTargetState.Absent);

        Assert.True(target.IsSuccess);
        Assert.Equal(Path.Combine("/tmp/projects", "Named.mocprj"), target.FullPath);
    }

    [Fact]
    public void AnExistingFileAndDirectoryAreRejectedWithTheCliMessages()
    {
        var name = ProjectPathRules.NormalizeName("Taken");
        var file = ProjectPathRules.ValidateTarget("/tmp/projects", name, ProjectTargetState.File);
        var directory = ProjectPathRules.ValidateTarget("/tmp/projects", name, ProjectTargetState.Directory);

        Assert.Equal(ProjectPathFailure.ExistingTarget, file.Failure);
        Assert.Contains("already exists", file.Message, StringComparison.Ordinal);
        Assert.Equal(ProjectPathFailure.IoFailure, directory.Failure);
        Assert.Contains("existing directory", directory.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyDirectoryIsTheGuiOnlyInvalidTargetFailure()
    {
        var name = ProjectPathRules.NormalizeName("Named");
        var target = ProjectPathRules.ValidateTarget(" ", name, ProjectTargetState.Absent);

        Assert.Equal(ProjectPathFailure.InvalidTargetPath, target.Failure);
        Assert.Equal("The selected target directory is not valid.", target.Message);
    }

    [Fact]
    public void ANameFailureIsPropagatedByTargetValidation()
    {
        var name = ProjectPathRules.NormalizeName("a/b");
        var target = ProjectPathRules.ValidateTarget("/tmp", name, ProjectTargetState.Absent);

        Assert.Equal(ProjectPathFailure.PathSeparator, target.Failure);
        Assert.Equal(name.Message, target.Message);
    }

    [Theory]
    [InlineData(ProjectPathFailure.EmptyName, "A project name is required.")]
    [InlineData(ProjectPathFailure.NameTooLong, "The project name must not exceed 128 characters.")]
    [InlineData(ProjectPathFailure.PathSeparator, "The project name must not contain path separators or '..'.")]
    [InlineData(ProjectPathFailure.ParentDirectory, "The project name must not contain path separators or '..'.")]
    [InlineData(ProjectPathFailure.ControlCharacter, "The project name must not contain control characters.")]
    public void MessagesAreStable(ProjectPathFailure failure, string expected)
    {
        Assert.Equal(expected, ProjectPathRules.GetMessage(failure));
    }
}
