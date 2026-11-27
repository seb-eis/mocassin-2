// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// A typed reference to one authored model object, used as the usage-index key.
/// </summary>
/// <param name="Domain">The model-path domain, for example <c>particle</c> or <c>cellSite</c>.</param>
/// <param name="ObjectId">The stable object id.</param>
internal readonly record struct ModelObjectRef(string Domain, Guid ObjectId)
{
    /// <summary>
    /// Creates a particle reference.
    /// </summary>
    /// <param name="id">The particle id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef Particle(ParticleId id) => new(ModelEditorDomains.Particle, id.Value);

    /// <summary>
    /// Creates a particle-set reference.
    /// </summary>
    /// <param name="id">The set id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef ParticleSet(ParticleSetId id) => new(ModelEditorDomains.ParticleSet, id.Value);

    /// <summary>
    /// Creates a reference-site reference.
    /// </summary>
    /// <param name="id">The site id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef CellSite(CellSiteId id) => new(ModelEditorDomains.CellSite, id.Value);

    /// <summary>
    /// Creates a state-change reference.
    /// </summary>
    /// <param name="id">The state-change id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef StateChange(StateChangeId id) => new("stateChange", id.Value);

    /// <summary>
    /// Creates a transition reference. The <c>transition</c> domain is registered by 24B; 24A only counts it.
    /// </summary>
    /// <param name="id">The transition id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef Transition(Guid id) => new("transition", id);

    /// <summary>
    /// Creates an occupation-exchange reference.
    /// </summary>
    /// <param name="id">The exchange id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef OccupationExchange(OccupationExchangeId id) => new("occupationExchange", id.Value);

    /// <summary>
    /// Creates a doping reference.
    /// </summary>
    /// <param name="id">The doping id.</param>
    /// <returns>The reference.</returns>
    public static ModelObjectRef Doping(DopingId id) => new("doping", id.Value);

    /// <summary>
    /// Creates an anonymous energy-reference marker. Energy objects have no stable navigation id in 24A.
    /// </summary>
    /// <returns>The reference.</returns>
    public static ModelObjectRef Energy() => new("energy", Guid.Empty);

    /// <summary>
    /// Gets the navigation path to this object.
    /// </summary>
    /// <param name="property">The optional property name.</param>
    /// <returns>The model path.</returns>
    public ModelPath ToPath(string? property = null) => new(Domain, ObjectId, property);
}
