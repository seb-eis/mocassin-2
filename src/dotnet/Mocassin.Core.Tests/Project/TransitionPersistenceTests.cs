// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Packages.Projects;

namespace Mocassin.Core.Tests.Project;

[Trait("Tier", "PR-fast")]
public sealed class TransitionPersistenceTests
{
    [Fact]
    public async Task ProjectStoreSaveAndLoadPreservesTransitions()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-transitions-");
        try
        {
            var path = Path.Combine(directory.FullName, "transitions.mocprj");
            var store = new ProjectStore();
            var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());

            var saved = await store.SaveAsync(project, path);
            var loaded = await store.LoadAsync(path);

            Assert.True(saved.IsSuccess);
            Assert.True(loaded.IsSuccess);
            Assert.Equal(
                ProjectCanonicalizer.ToCanonicalJson(saved.SavedProject!),
                ProjectCanonicalizer.ToCanonicalJson(loaded.Project!));

            var model = loaded.Project!.Model;
            Assert.Equal(2, model.StateChanges.Length);
            Assert.Equal(2, model.StateChangeGroups.Length);
            Assert.Equal(2, model.Chains.Length);
            Assert.Single(model.KineticTransitions);
            Assert.Single(model.MetropolisTransitions);
            Assert.Equal(
                TransitionTestModels.KineticTransition.ReferencePath.ToArray(),
                model.KineticTransitions[0].ReferencePath.ToArray());
            Assert.Equal(
                TransitionTestModels.MigrationChain.Linkers.ToArray(),
                model.Chains.Single(static chain => chain.Pattern == TransitionPattern.Migration3).Linkers.ToArray());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void TransitionEditChangesModelAndDocumentHashes()
    {
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var editedStateChange = project.Model.StateChanges[0] with { Name = "Edited state change" };
        var edited = project with
        {
            Model = project.Model with
            {
                StateChanges = [editedStateChange, .. project.Model.StateChanges.Skip(1)],
            },
        };

        Assert.NotEqual(ProjectHasher.Compute(project), ProjectHasher.Compute(edited));
        Assert.NotEqual(ProjectHasher.ComputeDocument(project), ProjectHasher.ComputeDocument(edited));
    }

    [Fact]
    public async Task TransitionOnlyProjectRoundTripsThroughProjectStoreFile()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-transitions-only-");
        try
        {
            var path = Path.Combine(directory.FullName, "transitions-only.mocprj");
            var store = new ProjectStore();
            var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateTransitionOnlyModel());

            var saved = await store.SaveAsync(project, path);
            var loaded = await store.LoadAsync(path);

            Assert.True(saved.IsSuccess);
            Assert.True(loaded.IsSuccess);
            Assert.Equal(
                ProjectHasher.Compute(saved.SavedProject!),
                ProjectHasher.Compute(loaded.Project!));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task SavedTransitionDocumentContainsNoPreviewPayload()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-transitions-preview-");
        try
        {
            var path = Path.Combine(directory.FullName, "transitions.mocprj");
            var store = new ProjectStore();
            var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());

            Assert.True((await store.SaveAsync(project, path)).IsSuccess);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            var model = document.RootElement.GetProperty("model");
            var propertyNames = model.EnumerateObject().Select(static property => property.Name).ToArray();

            Assert.DoesNotContain(propertyNames, static name => name.Contains("preview", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, static name => name.Contains("resolved", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("transitions", propertyNames);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
