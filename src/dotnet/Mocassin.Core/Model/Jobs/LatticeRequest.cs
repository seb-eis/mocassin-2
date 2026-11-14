// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// A requested supercell size in unit cells.
/// </summary>
/// <param name="A">The number of cells along <c>a</c>.</param>
/// <param name="B">The number of cells along <c>b</c>.</param>
/// <param name="C">The number of cells along <c>c</c>.</param>
public sealed record LatticeRequest(int A, int B, int C);
