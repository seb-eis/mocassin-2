// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Model;

/// <summary>
/// Root of the authored scientific model.
/// </summary>
/// <remarks>
/// Step 01 establishes the project envelope and keeps the model body empty. Particle, structure,
/// transition, energy, lattice, and simulation sections are added by Steps 03 through 06. The JSON
/// representation is therefore an empty object in the initial schema.
/// </remarks>
public sealed record ModelDefinition();
