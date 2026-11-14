// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Mocassin.Compilation.Tests")]

[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Structure is the accepted Step 08 concept folder and namespace; the project is C#-only.",
    Scope = "namespace",
    Target = "~N:Mocassin.Compilation.Structure")]

[assembly: SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "CompiledJumpCollection is the accepted Step 08 contract name.",
    Scope = "type",
    Target = "~T:Mocassin.Compilation.Transitions.CompiledJumpCollection")]
