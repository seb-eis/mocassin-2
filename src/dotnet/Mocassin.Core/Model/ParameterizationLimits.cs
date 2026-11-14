// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Model;

/// <summary>
/// Defines the hard bounds applied to parameterization templates before large allocations.
/// </summary>
/// <remarks>
/// The project document already has a byte cap. These limits bound the nested template structure and the
/// expanded applied-value tables. Exceeding a structural limit is <c>PRM013</c>; a malformed stable key is
/// <c>PRM014</c>.
/// </remarks>
public static class ParameterizationLimits
{
    /// <summary>
    /// The maximum number of parameterization templates in one project.
    /// </summary>
    public const int TemplatesPerProject = 256;

    /// <summary>
    /// The maximum number of stable and unstable pair sets in one template.
    /// </summary>
    public const int PairSetsPerTemplate = 100_000;

    /// <summary>
    /// The maximum number of entries in one pair set.
    /// </summary>
    /// <remarks>
    /// A compiled table has at most 64 particles including void, i.e. at most 4,096 cells; the sparse
    /// template cannot contain more than 63 x 63 = 3,969 non-void entries.
    /// </remarks>
    public const int PairEntriesPerSet = 4_096;

    /// <summary>
    /// The maximum number of group sets in one template.
    /// </summary>
    public const int GroupSetsPerTemplate = 100_000;

    /// <summary>
    /// The maximum number of entries in one group set.
    /// </summary>
    public const int GroupEntriesPerSet = 1_000_000;

    /// <summary>
    /// The maximum number of site-energy overrides in one template.
    /// </summary>
    public const int SiteOverridesPerTemplate = 1_000_000;

    /// <summary>
    /// The maximum number of transition rule sets in one template.
    /// </summary>
    public const int TransitionRuleSetsPerTemplate = 100_000;

    /// <summary>
    /// The maximum number of rule-frequency entries in one transition rule set.
    /// </summary>
    public const int RuleEntriesPerSet = 1_000_000;

    /// <summary>
    /// The maximum number of explicit pair, group, site, and rule entries in one template.
    /// </summary>
    public const int TotalExplicitValueEntriesPerTemplate = 2_000_000;

    /// <summary>
    /// The required length of a lowercase hexadecimal stable identity.
    /// </summary>
    public const int StableIdLength = 64;

    /// <summary>
    /// The maximum length of a template name in characters.
    /// </summary>
    public const int TemplateNameLength = 200;

    /// <summary>
    /// The default maximum size of the dense applied values in bytes.
    /// </summary>
    /// <remarks>
    /// This value is the default for the configurable apply limit; callers may lower it for a smaller batch.
    /// </remarks>
    public const long MaxAppliedValueBytes = 2L * 1024 * 1024 * 1024;
}
