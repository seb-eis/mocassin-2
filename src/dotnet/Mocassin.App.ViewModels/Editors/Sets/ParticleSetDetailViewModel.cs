// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Sets;

/// <summary>
/// The particle-set detail: an ordered member list with a local "Project particles" drag source, keyboard
/// add/remove/move buttons as first-class equivalents, the default-occupation badge, and occupation chips.
/// </summary>
internal sealed class ParticleSetDetailViewModel : ModelDetailViewModel
{
    private readonly List<ParticleId> memberIds = [];
    private ProjectModel project;
    private string name;
    private ProjectParticleChoiceViewModel? selectedProjectParticle;
    private ParticleSetMemberViewModel? selectedMember;
    private string? validationMessage;
    private string? statusMessage;

    /// <summary>
    /// Initializes the detail from a set and the current project snapshot.
    /// </summary>
    /// <param name="particleSet">The set.</param>
    /// <param name="project">The current project snapshot.</param>
    public ParticleSetDetailViewModel(ParticleSet particleSet, ProjectModel project)
        : base(particleSet.Id.Value)
    {
        this.project = project;
        name = particleSet.Name;
        memberIds.AddRange(particleSet.ParticleIds);
        AddMemberCommand = new RelayCommand(
            AddSelectedMember,
            () => CanEditForm && SelectedProjectParticle is not null);
        RemoveMemberCommand = new RelayCommand(
            RemoveSelectedMember,
            () => CanEditForm && SelectedMember is not null);
        MoveUpCommand = new RelayCommand(
            () => MoveSelectedMember(-1),
            () => CanEditForm && CanMoveSelected(-1));
        MoveDownCommand = new RelayCommand(
            () => MoveSelectedMember(1),
            () => CanEditForm && CanMoveSelected(1));
        RefreshProjectParticles();
        RefreshMembers();
    }

    /// <summary>
    /// Gets or sets the set name.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                HasUncommittedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets the ordered members.
    /// </summary>
    public ObservableCollection<ParticleSetMemberViewModel> Members { get; } = [];

    /// <summary>
    /// Gets the project particles used as the drag source and keyboard add list.
    /// </summary>
    public ObservableCollection<ProjectParticleChoiceViewModel> ProjectParticles { get; } = [];

    /// <summary>
    /// Gets the occupation-chip preview in member order.
    /// </summary>
    public ObservableCollection<string> OccupationChips { get; } = [];

    /// <summary>
    /// Gets or sets the selected project particle for the keyboard add path.
    /// </summary>
    public ProjectParticleChoiceViewModel? SelectedProjectParticle
    {
        get => selectedProjectParticle;
        set
        {
            if (SetProperty(ref selectedProjectParticle, value))
            {
                AddMemberCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected member for remove/move.
    /// </summary>
    public ParticleSetMemberViewModel? SelectedMember
    {
        get => selectedMember;
        set
        {
            if (SetProperty(ref selectedMember, value))
            {
                RemoveMemberCommand.NotifyCanExecuteChanged();
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the empty-set or invalid-membership message.
    /// </summary>
    public string? ValidationMessage
    {
        get => validationMessage;
        private set
        {
            if (SetProperty(ref validationMessage, value))
            {
                OnPropertyChanged(nameof(HasValidationMessage));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the set is currently invalid.
    /// </summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>
    /// Gets the refusal message for a void/duplicate drop.
    /// </summary>
    public string? StatusMessage
    {
        get => statusMessage;
        private set
        {
            if (SetProperty(ref statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a drop was refused.
    /// </summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>
    /// Gets the number of members.
    /// </summary>
    public int MemberCount => memberIds.Count;

    /// <summary>
    /// Gets the command that adds the selected project particle by keyboard.
    /// </summary>
    public IRelayCommand AddMemberCommand { get; }

    /// <summary>
    /// Gets the command that removes the selected member.
    /// </summary>
    public IRelayCommand RemoveMemberCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected member up.
    /// </summary>
    public IRelayCommand MoveUpCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected member down.
    /// </summary>
    public IRelayCommand MoveDownCommand { get; }

    /// <inheritdoc />
    public override bool CanApply => base.CanApply && memberIds.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the form's membership and name controls are editable.
    /// </summary>
    public bool CanEditForm => Coordinator?.CanEdit == true && !IsRemoved;

    /// <inheritdoc />
    protected override string Description => "Update particle set";

    /// <summary>
    /// Adds a particle at the end, the keyboard equivalent of a drop.
    /// </summary>
    /// <param name="id">The particle id.</param>
    /// <returns><see langword="true"/> when the member was added.</returns>
    public bool AddMember(ParticleId id) => InsertMember(id, memberIds.Count);

    /// <summary>
    /// Inserts a particle at a target index; drag-and-drop and the keyboard path share this method.
    /// </summary>
    /// <param name="id">The particle id.</param>
    /// <param name="index">The target index.</param>
    /// <returns><see langword="true"/> when the member was inserted.</returns>
    public bool InsertMember(ParticleId id, int index)
    {
        if (id == ParticleId.Void)
        {
            StatusMessage = "PAR005: the implicit void particle cannot be a set member.";
            return false;
        }

        if (memberIds.Contains(id))
        {
            StatusMessage = "PAR012: a particle set must not contain the same particle more than once.";
            return false;
        }

        if (project.Find(id) is null)
        {
            StatusMessage = "PAR006: the referenced particle does not exist.";
            return false;
        }

        var target = Math.Clamp(index, 0, memberIds.Count);
        memberIds.Insert(target, id);
        StatusMessage = null;
        HasUncommittedChanges = true;
        RefreshProjectParticles();
        RefreshMembers();
        return true;
    }

    /// <summary>
    /// Drops a dragged particle at a target index.
    /// </summary>
    /// <param name="id">The particle id.</param>
    /// <param name="targetIndex">The target index.</param>
    /// <returns><see langword="true"/> when the member was inserted.</returns>
    public bool TryDropParticle(ParticleId id, int targetIndex) => InsertMember(id, targetIndex);

    /// <summary>
    /// Moves a member within the ordered list.
    /// </summary>
    /// <param name="fromIndex">The source index.</param>
    /// <param name="toIndex">The target index.</param>
    /// <returns><see langword="true"/> when the member moved.</returns>
    public bool MoveMember(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= memberIds.Count || toIndex < 0 || toIndex >= memberIds.Count)
        {
            return false;
        }

        if (fromIndex == toIndex)
        {
            return true;
        }

        var id = memberIds[fromIndex];
        memberIds.RemoveAt(fromIndex);
        memberIds.Insert(toIndex, id);
        HasUncommittedChanges = true;
        RefreshMembers();
        return true;
    }

    /// <summary>
    /// Removes a member by particle id.
    /// </summary>
    /// <param name="id">The particle id.</param>
    /// <returns><see langword="true"/> when the member was removed.</returns>
    public bool RemoveMember(ParticleId id)
    {
        if (!memberIds.Remove(id))
        {
            return false;
        }

        StatusMessage = null;
        HasUncommittedChanges = true;
        RefreshProjectParticles();
        RefreshMembers();
        return true;
    }

    /// <inheritdoc />
    protected override ProjectEditResult ApplyCore(ProjectEditor editor, ProjectModel projectSnapshot) =>
        editor.UpdateParticleSet(
            projectSnapshot,
            new ParticleSet(ParticleSetId.Create(ObjectId), Name, [.. memberIds]));

    /// <inheritdoc />
    protected override void RevertCore()
    {
        var set = Coordinator?.Document.Project.Find(ParticleSetId.Create(ObjectId));
        if (set is null)
        {
            return;
        }

        name = set.Name;
        memberIds.Clear();
        memberIds.AddRange(set.ParticleIds);
        StatusMessage = null;
        OnPropertyChanged(nameof(Name));
        RefreshProjectParticles();
        RefreshMembers();
    }

    /// <inheritdoc />
    protected override void RefreshOptionsCore(ProjectModel projectSnapshot)
    {
        project = projectSnapshot;
        RefreshProjectParticles();
        RefreshMembers();
    }

    /// <inheritdoc />
    protected override void OnCanApplyChanged() => NotifyCommandStates();

    /// <inheritdoc />
    protected override void OnAttached()
    {
        OnPropertyChanged(nameof(CanEditForm));
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        AddMemberCommand.NotifyCanExecuteChanged();
        RemoveMemberCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private void AddSelectedMember()
    {
        if (SelectedProjectParticle is { } choice)
        {
            AddMember(choice.Id);
        }
    }

    private void RemoveSelectedMember()
    {
        if (SelectedMember is { } member && RemoveMember(member.Id))
        {
            SelectedMember = Members.Count > 0 ? Members[0] : null;
        }
    }

    private bool CanMoveSelected(int delta)
    {
        if (SelectedMember is null)
        {
            return false;
        }

        var index = memberIds.IndexOf(SelectedMember.Id);
        var target = index + delta;
        return index >= 0 && target >= 0 && target < memberIds.Count;
    }

    private void MoveSelectedMember(int delta)
    {
        if (SelectedMember is not { } member)
        {
            return;
        }

        var index = memberIds.IndexOf(member.Id);
        if (MoveMember(index, index + delta))
        {
            SelectedMember = Members.FirstOrDefault(candidate => candidate.Id == member.Id);
        }
    }

    private void RefreshProjectParticles()
    {
        var selected = SelectedProjectParticle?.Id;
        ProjectParticles.Clear();
        foreach (var particle in project.Particles)
        {
            ProjectParticles.Add(
                new ProjectParticleChoiceViewModel(
                    particle.Id,
                    particle.Name,
                    particle.Symbol,
                    memberIds.Contains(particle.Id)));
        }

        SelectedProjectParticle = selected is { } id
            ? ProjectParticles.FirstOrDefault(choice => choice.Id == id)
            : ProjectParticles.FirstOrDefault(choice => !choice.IsMember);
    }

    private void RefreshMembers()
    {
        var selected = SelectedMember?.Id;
        Members.Clear();
        for (var index = 0; index < memberIds.Count; index++)
        {
            var id = memberIds[index];
            var particle = project.Find(id);
            Members.Add(
                new ParticleSetMemberViewModel(
                    id,
                    particle?.Name ?? string.Empty,
                    particle?.Symbol ?? string.Empty,
                    index == 0,
                    particle is null));
        }

        OccupationChips.Clear();
        foreach (var member in Members)
        {
            OccupationChips.Add(member.DisplayName);
        }

        SelectedMember = selected is { } selectedId
            ? Members.FirstOrDefault(member => member.Id == selectedId) ?? Members.FirstOrDefault()
            : Members.FirstOrDefault();

        ValidationMessage = memberIds.Count == 0
            ? "PAR005: a particle set must contain at least one particle."
            : null;
        OnPropertyChanged(nameof(MemberCount));
        NotifyCanApplyChanged();
        AddMemberCommand.NotifyCanExecuteChanged();
        RemoveMemberCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
