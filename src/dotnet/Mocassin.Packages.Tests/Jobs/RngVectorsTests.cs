// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

/// <summary>
/// Asserts the Step 10 seed derivations against the cross-language Step 13 RNG vector document.
/// </summary>
/// <remarks>
/// The draw, bounded-draw, fork, and state vectors are consumed by the Rust tests because they exercise the
/// ChaCha12 stream; this test covers the seed and seed-hash projections that both languages implement.
/// </remarks>
[Trait("Tier", "PR-fast")]
public sealed class RngVectorsTests
{
    [Fact]
    public void TheVectorDocumentAgreesWithTheStep10SeedDerivations()
    {
        var path = RepositoryPaths.Combine("tests/contracts/rng-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal("mocassin.rng-vectors", root.GetProperty("format").GetString());
        Assert.Equal(1, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal("chacha12-v1", root.GetProperty("algorithm").GetString());

        var seeds = root.GetProperty("seeds");
        var master = Convert.FromHexString(seeds.GetProperty("masterHex").GetString()!);

        foreach (var entry in seeds.GetProperty("job").EnumerateArray())
        {
            var derived = JobSeedDeriver.DeriveJobSeed(
                master,
                entry.GetProperty("collection").GetInt32(),
                entry.GetProperty("config").GetInt32(),
                entry.GetProperty("job").GetInt32());
            Assert.Equal(entry.GetProperty("expectedHex").GetString(), Convert.ToHexStringLower(derived.AsSpan()));
        }

        foreach (var entry in seeds.GetProperty("lattice").EnumerateArray())
        {
            var derived = LatticeSeedDeriver.DeriveLatticeSeed(
                Convert.FromHexString(entry.GetProperty("jobSeedHex").GetString()!),
                Convert.FromHexString(entry.GetProperty("requestHashHex").GetString()!));
            Assert.Equal(entry.GetProperty("expectedHex").GetString(), Convert.ToHexStringLower(derived.AsSpan()));
        }

        foreach (var entry in seeds.GetProperty("seedHash").EnumerateArray())
        {
            var seed = Convert.FromHexString(entry.GetProperty("seedHex").GetString()!);
            Assert.Equal(
                entry.GetProperty("expectedHex").GetString(),
                Convert.ToHexStringLower(SHA256.HashData(seed)));
        }

        Assert.True(root.TryGetProperty("draws", out _));
        Assert.True(root.TryGetProperty("fork", out _));
        Assert.True(root.TryGetProperty("state", out _));
    }
}
