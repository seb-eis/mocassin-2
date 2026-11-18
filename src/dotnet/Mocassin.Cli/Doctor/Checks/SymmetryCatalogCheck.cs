// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Checks that the embedded space-group catalog loads and reports the expected version, checksum, and count.
/// </summary>
internal sealed class SymmetryCatalogCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "symmetry-catalog";

    /// <summary>The documented embedded catalog setting count.</summary>
    public const int ExpectedSettingCount = 274;

    /// <inheritdoc />
    public string Name => CheckName;

    /// <inheritdoc />
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var catalog = SpaceGroupCatalog.Default;
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(catalog.DataVersion))
            {
                issues.Add("the data version is empty");
            }

            if (catalog.DataSha256 is not { Length: 64 } ||
                !catalog.DataSha256.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))
            {
                issues.Add("the SHA-256 is not 64 lowercase hexadecimal characters");
            }

            if (catalog.Settings.Length != ExpectedSettingCount)
            {
                issues.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"the catalog contains {catalog.Settings.Length} settings instead of {ExpectedSettingCount}"));
            }

            return Task.FromResult(issues.Count == 0
                ? new DoctorCheckResult(
                    Name,
                    DoctorCheckStatus.Ok,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Catalog {catalog.DataVersion} loaded with {catalog.Settings.Length} settings."),
                    catalog.DataSha256)
                : new DoctorCheckResult(
                    Name,
                    DoctorCheckStatus.Fail,
                    "The embedded symmetry catalog is malformed: " + string.Join("; ", issues) + ".",
                    catalog.DataSha256));
        }
        catch (Exception exception) when (exception is InvalidOperationException
                                              or InvalidDataException
                                              or IOException
                                              or NotSupportedException)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Fail,
                $"The embedded symmetry catalog could not be loaded: {exception.Message}",
                exception.GetType().Name));
        }
    }
}
