// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Mocassin.Core.Tests")]
[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Structure is the accepted Step 03 domain folder and namespace; the project is C#-only.",
    Scope = "namespace",
    Target = "~N:Mocassin.Core.Model.Structure")]
