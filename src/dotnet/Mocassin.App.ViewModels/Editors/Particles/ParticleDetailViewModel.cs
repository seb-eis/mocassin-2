// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Particles;

/// <summary>
/// The particle detail form. Name, symbol, charge, and the vacancy toggle each commit as one typed update through
/// the coordinator; Apply commits the whole form as one entry.
/// </summary>
internal sealed class ParticleDetailViewModel : ModelDetailViewModel
{
    private readonly string symbolSnapshot;
    private readonly double chargeSnapshot;
    private readonly bool vacancySnapshot;
    private string name;
    private string symbol;
    private double charge;
    private bool isVacancy;

    /// <summary>
    /// Initializes the detail from a particle.
    /// </summary>
    /// <param name="particle">The authored particle.</param>
    public ParticleDetailViewModel(Particle particle)
        : base(particle.Id.Value)
    {
        name = particle.Name;
        symbol = particle.Symbol;
        charge = particle.Charge.Value;
        isVacancy = particle.IsVacancy;
        symbolSnapshot = symbol;
        chargeSnapshot = charge;
        vacancySnapshot = isVacancy;
        CommitNameCommand = new RelayCommand(CommitName);
        CommitSymbolCommand = new RelayCommand(CommitSymbol);
        CommitChargeCommand = new RelayCommand(CommitCharge);
        CommitVacancyCommand = new RelayCommand(CommitVacancy);
        UpdateVacancyWarning();
    }

    /// <summary>
    /// Gets or sets the particle name.
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
    /// Gets or sets the mass-conservation symbol.
    /// </summary>
    public string Symbol
    {
        get => symbol;
        set
        {
            if (SetProperty(ref symbol, value))
            {
                HasUncommittedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the charge in elementary units.
    /// </summary>
    public double Charge
    {
        get => charge;
        set
        {
            if (SetProperty(ref charge, value))
            {
                HasUncommittedChanges = true;
                OnPropertyChanged(nameof(IsChargeValid));
                NotifyCanApplyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the particle is a vacancy pseudo-particle.
    /// </summary>
    public bool IsVacancy
    {
        get => isVacancy;
        set
        {
            if (SetProperty(ref isVacancy, value))
            {
                HasUncommittedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the form fields are editable.
    /// </summary>
    public bool CanEditForm => Coordinator?.CanEdit == true && !IsRemoved;

    /// <summary>
    /// Gets a value indicating whether the charge field is valid.
    /// </summary>
    public bool IsChargeValid =>
        double.IsFinite(charge) && charge >= ChargeElementary.Minimum && charge <= ChargeElementary.Maximum;

    /// <inheritdoc />
    public override bool CanApply => base.CanApply && IsChargeValid;

    /// <summary>
    /// Gets the command that commits the name field.
    /// </summary>
    public IRelayCommand CommitNameCommand { get; }

    /// <summary>
    /// Gets the command that commits the symbol field.
    /// </summary>
    public IRelayCommand CommitSymbolCommand { get; }

    /// <summary>
    /// Gets the command that commits the charge field.
    /// </summary>
    public IRelayCommand CommitChargeCommand { get; }

    /// <summary>
    /// Gets the command that commits the vacancy toggle.
    /// </summary>
    public IRelayCommand CommitVacancyCommand { get; }

    /// <inheritdoc />
    protected override string Description => "Update particle";

    /// <inheritdoc />
    protected override ProjectEditResult ApplyCore(ProjectEditor editor, ProjectModel project) =>
        TryBuildParticle(out var particle)
            ? editor.UpdateParticle(project, particle)
            : NoOp(project);

    private void CommitName()
    {
        if (CommittedParticle() is not { } particle)
        {
            return;
        }

        CommitOperation("Set particle name", (editor, project) =>
            editor.UpdateParticle(project, particle with { Name = Name }));
        UpdateDirtyState();
    }

    private void CommitSymbol()
    {
        if (CommittedParticle() is not { } particle)
        {
            return;
        }

        CommitOperation("Set particle symbol", (editor, project) =>
            editor.UpdateParticle(project, particle with { Symbol = Symbol }));
        UpdateDirtyState();
    }

    private void CommitCharge()
    {
        if (!IsChargeValid)
        {
            SetError($"The charge must be between {ChargeElementary.Minimum} and {ChargeElementary.Maximum}.");
            return;
        }

        if (CommittedParticle() is not { } particle)
        {
            return;
        }

        var chargeValue = new ChargeElementary(Charge);
        CommitOperation("Set particle charge", (editor, project) =>
            editor.UpdateParticle(project, particle with { Charge = chargeValue }));
        UpdateVacancyWarning();
        UpdateDirtyState();
    }

    private void CommitVacancy()
    {
        if (CommittedParticle() is not { } particle)
        {
            return;
        }

        CommitOperation("Set particle vacancy", (editor, project) =>
            editor.UpdateParticle(project, particle with { IsVacancy = IsVacancy }));
        UpdateVacancyWarning();
        UpdateDirtyState();
    }

    private void UpdateDirtyState()
    {
        var particle = CommittedParticle();
        HasUncommittedChanges = particle is not null
            && (!string.Equals(particle.Name, Name, StringComparison.Ordinal)
                || !string.Equals(particle.Symbol, Symbol, StringComparison.Ordinal)
                || particle.Charge.Value != Charge
                || particle.IsVacancy != IsVacancy);
    }

    private Particle? CommittedParticle() =>
        Coordinator?.Document.Project.Find(ParticleId.Create(ObjectId));

    /// <inheritdoc />
    protected override void OnAttached() => OnPropertyChanged(nameof(CanEditForm));

    /// <inheritdoc />
    protected override void RevertCore()
    {
        var particle = Coordinator?.Document.Project.Find(ParticleId.Create(ObjectId));
        if (particle is null)
        {
            return;
        }

        name = particle.Name;
        symbol = particle.Symbol;
        charge = particle.Charge.Value;
        isVacancy = particle.IsVacancy;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Symbol));
        OnPropertyChanged(nameof(Charge));
        OnPropertyChanged(nameof(IsVacancy));
        UpdateVacancyWarning();
    }

    private bool TryBuildParticle(out Particle particle)
    {
        particle = null!;
        if (!double.IsFinite(Charge) || Charge < ChargeElementary.Minimum || Charge > ChargeElementary.Maximum)
        {
            SetError($"The charge must be between {ChargeElementary.Minimum} and {ChargeElementary.Maximum}.");
            return false;
        }

        SetError(null);
        particle = new Particle(
            ParticleId.Create(ObjectId),
            Name,
            Symbol,
            new ChargeElementary(Charge),
            IsVacancy);
        return true;
    }

    private void UpdateVacancyWarning()
    {
        if (IsVacancy && Charge != 0d)
        {
            SetWarning(
                "PAR004: a charged vacancy warns when a particle set also has a charged default occupation; "
                + "set either the vacancy or the default occupation to charge zero.");
            return;
        }

        if (WarningMessage?.StartsWith("PAR004", StringComparison.Ordinal) == true)
        {
            SetWarning(null);
        }
    }

    private static ProjectEditResult NoOp(ProjectModel project) => new(project, null, []);
}
