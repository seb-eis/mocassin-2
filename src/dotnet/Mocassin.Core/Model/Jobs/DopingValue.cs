// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// A requested fraction for one authored doping definition.
/// </summary>
/// <param name="DopingId">The referenced doping.</param>
/// <param name="Fraction">The requested fraction in <c>[0, 1]</c>.</param>
public sealed record DopingValue(DopingId DopingId, double Fraction);
