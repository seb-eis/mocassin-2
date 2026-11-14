// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Parameterization;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// The build-time composition of a compiled model, its dense applied values, and its parameterization signature.
/// </summary>
/// <param name="CompiledModel">The Step 08 compiled geometry and rules.</param>
/// <param name="Values">The dense, quantized applied values.</param>
/// <param name="Signature">The Step 09 parameterization signature.</param>
/// <remarks>
/// This record is consumed by Step 10 and mapped to the <c>.msl</c> payload by Step 11. It is not the persisted
/// package DTO. <see cref="Mocassin.Compilation.Compiler.CompiledModel"/> remains the only compiled
/// geometry/rules/encoding truth; the values are meaningless outside that model.
/// </remarks>
public sealed record ParameterizedModel(
    CompiledModel CompiledModel,
    ParameterizationValues Values,
    string Signature)
{
    /// <summary>
    /// Gets the Step 08 compiled-model signature the parameterization targets.
    /// </summary>
    public string ModelSignature => CompiledModel.ModelSignature;
}
