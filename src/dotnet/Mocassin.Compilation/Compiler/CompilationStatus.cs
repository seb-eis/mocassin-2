// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Describes whether a compilation produced a complete, usable compiled model.
/// </summary>
public enum CompilationStatus
{
    /// <summary>
    /// The pipeline completed and the result carries a signed <see cref="CompiledModel"/>.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// The pipeline stopped because user data could not be compiled; the result carries diagnostics only.
    /// </summary>
    Failed = 1,
}
