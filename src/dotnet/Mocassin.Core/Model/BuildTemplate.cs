// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model;

/// <summary>
/// A reusable recipe that binds a job template to a parameterization.
/// </summary>
/// <param name="Id">The stable build-template identifier.</param>
/// <param name="Name">The user-visible template name.</param>
/// <param name="JobTemplateId">The referenced job template.</param>
/// <param name="ParameterizationId">The referenced parameterization template.</param>
/// <param name="OutputName">The suggested output name.</param>
public sealed record BuildTemplate(
    BuildTemplateId Id,
    string Name,
    JobTemplateId JobTemplateId,
    ParameterizationId ParameterizationId,
    string OutputName);
