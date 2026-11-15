// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Computes the parameterization-independent Step 10 lattice-request hash.
/// </summary>
/// <remarks>
/// The projection is <c>SHA-256("mocassin/lattice-request/v1" || structure_hash || particle_hash ||
/// lattice_hash || u32_le(A) || u32_le(B) || u32_le(C) || u32_le(doping_count) || doping entries ||
/// space-group data version and digest || symmetry contract version)</c>. Every <c>[32]</c> input is a SHA-256
/// digest decoded from its canonical lowercase hexadecimal form. Doping entries are ordered by ascending
/// resolved priority and then <see cref="DopingId"/> and encode the identifier as 16 RFC 4122 big-endian bytes
/// followed by the fraction as a little-endian IEEE 754 double. The hash excludes transitions, parameterization
/// values, temperatures, and other runtime settings.
/// </remarks>
public static class LatticeRequestHasher
{
    /// <summary>
    /// The domain-separation prefix of the lattice-request projection.
    /// </summary>
    public const string Domain = "mocassin/lattice-request/v1";

    private const int digestByteLength = 32;

    private static readonly byte[] domainBytes = Encoding.UTF8.GetBytes(Domain);

    /// <summary>
    /// Computes the lattice-request hash of one project, compiled model, lattice size, and doping list.
    /// </summary>
    /// <param name="project">The project that supplies the canonical model hashes and doping priorities.</param>
    /// <param name="model">The compiled model that supplies the space-group and symmetry provenance.</param>
    /// <param name="request">The requested supercell size.</param>
    /// <param name="dopings">The requested doping fractions; an empty array means none.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// A requested doping is not authored, or a provenance string is not a 32-byte hexadecimal digest.
    /// </exception>
    public static string Compute(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);

        var requested = dopings.IsDefault ? ImmutableArray<DopingValue>.Empty : dopings;
        if (!TryResolveDopings(project, requested, out var resolved, out var unknown))
        {
            throw new ArgumentException(
                $"The lattice request references unknown doping '{unknown.GetValueOrDefault().Value:D}'.",
                nameof(dopings));
        }

        return Compute(
            Convert.FromHexString(ProjectHasher.ComputeStructure(project.Model.Structure)),
            Convert.FromHexString(ProjectHasher.ComputeParticles(project)),
            Convert.FromHexString(ProjectHasher.ComputeLattice(project)),
            request,
            resolved,
            model.SpaceGroupDataVersion,
            Convert.FromHexString(model.SpaceGroupDataSha256),
            model.SymmetryContractVersion);
    }

    /// <summary>
    /// Resolves the requested doping values to authored priorities and the deterministic hash order.
    /// </summary>
    /// <param name="project">The project that owns the doping definitions.</param>
    /// <param name="dopings">The requested doping fractions in authored order.</param>
    /// <param name="resolved">The resolved entries in ascending priority and identifier order.</param>
    /// <param name="unknown">The first unknown doping identifier, when resolution fails.</param>
    /// <returns><see langword="true"/> when every requested doping is authored.</returns>
    internal static bool TryResolveDopings(
        Project project,
        ImmutableArray<DopingValue> dopings,
        out ImmutableArray<LatticeDoping> resolved,
        out DopingId? unknown)
    {
        ArgumentNullException.ThrowIfNull(project);
        var requested = dopings.IsDefault ? ImmutableArray<DopingValue>.Empty : dopings;
        var priorities = new Dictionary<DopingId, int>();
        foreach (var doping in project.Model.Dopings.IsDefault
                     ? ImmutableArray<Doping>.Empty
                     : project.Model.Dopings)
        {
            if (doping is not null)
            {
                priorities.TryAdd(doping.Id, doping.Priority);
            }
        }

        var entries = ImmutableArray.CreateBuilder<LatticeDoping>(requested.Length);
        foreach (var value in requested)
        {
            if (value is null)
            {
                continue;
            }

            if (!priorities.TryGetValue(value.DopingId, out var priority))
            {
                resolved = [];
                unknown = value.DopingId;
                return false;
            }

            entries.Add(new LatticeDoping(value.DopingId, priority, value.Fraction));
        }

        resolved =
        [
            .. entries
                .OrderBy(static entry => entry.Priority)
                .ThenBy(static entry => entry.DopingId.Value),
        ];
        unknown = null;
        return true;
    }

    /// <summary>
    /// Computes the projection from already-canonical hash bytes and resolved doping entries.
    /// </summary>
    /// <param name="structureHash">The 32-byte structure hash.</param>
    /// <param name="particleHash">The 32-byte particle hash.</param>
    /// <param name="latticeHash">The 32-byte lattice hash.</param>
    /// <param name="request">The requested supercell size.</param>
    /// <param name="dopings">The resolved doping entries in hash order.</param>
    /// <param name="spaceGroupDataVersion">The embedded space-group catalog data version.</param>
    /// <param name="spaceGroupDataSha256">The 32-byte space-group catalog digest.</param>
    /// <param name="symmetryContractVersion">The symmetry contract version.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <exception cref="ArgumentException">A digest is not exactly 32 bytes.</exception>
    internal static string Compute(
        ReadOnlySpan<byte> structureHash,
        ReadOnlySpan<byte> particleHash,
        ReadOnlySpan<byte> latticeHash,
        LatticeRequest request,
        ImmutableArray<LatticeDoping> dopings,
        string spaceGroupDataVersion,
        ReadOnlySpan<byte> spaceGroupDataSha256,
        string symmetryContractVersion)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceGroupDataVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(symmetryContractVersion);
        ValidateDigest(structureHash, nameof(structureHash));
        ValidateDigest(particleHash, nameof(particleHash));
        ValidateDigest(latticeHash, nameof(latticeHash));
        ValidateDigest(spaceGroupDataSha256, nameof(spaceGroupDataSha256));

        var entries = dopings.IsDefault ? ImmutableArray<LatticeDoping>.Empty : dopings;
        var versionBytes = Encoding.UTF8.GetBytes(spaceGroupDataVersion);
        var contractBytes = Encoding.UTF8.GetBytes(symmetryContractVersion);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(domainBytes);
        hash.AppendData(structureHash);
        hash.AppendData(particleHash);
        hash.AppendData(latticeHash);

        Span<byte> integer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(integer, (uint)request.A);
        hash.AppendData(integer);
        BinaryPrimitives.WriteUInt32LittleEndian(integer, (uint)request.B);
        hash.AppendData(integer);
        BinaryPrimitives.WriteUInt32LittleEndian(integer, (uint)request.C);
        hash.AppendData(integer);
        BinaryPrimitives.WriteUInt32LittleEndian(integer, (uint)entries.Length);
        hash.AppendData(integer);

        Span<byte> identifier = stackalloc byte[16];
        Span<byte> fraction = stackalloc byte[sizeof(double)];
        foreach (var entry in entries)
        {
            entry.DopingId.Value.TryWriteBytes(identifier, bigEndian: true, out _);
            hash.AppendData(identifier);
            BinaryPrimitives.WriteDoubleLittleEndian(fraction, entry.Fraction);
            hash.AppendData(fraction);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(integer, (uint)versionBytes.Length);
        hash.AppendData(integer);
        hash.AppendData(versionBytes);
        hash.AppendData(spaceGroupDataSha256);
        BinaryPrimitives.WriteUInt32LittleEndian(integer, (uint)contractBytes.Length);
        hash.AppendData(integer);
        hash.AppendData(contractBytes);

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void ValidateDigest(ReadOnlySpan<byte> value, string parameterName)
    {
        if (value.Length != digestByteLength)
        {
            throw new ArgumentException(
                $"The hash must contain exactly {digestByteLength} bytes.",
                parameterName);
        }
    }
}

/// <summary>
/// One requested doping value with its authored priority and fraction.
/// </summary>
/// <param name="DopingId">The authored doping identifier.</param>
/// <param name="Priority">The authored application priority.</param>
/// <param name="Fraction">The requested fraction.</param>
internal sealed record LatticeDoping(DopingId DopingId, int Priority, double Fraction);
