// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// The contract-typed job row handed to the writer before the payload encoder of Step 11 feature 5 exists.
/// </summary>
/// <remarks>
/// The columns mirror the payload-derived and authored/build values of the <c>jobs</c> table. The payload
/// encoders of features 4/5 build this record from the real descriptor and lattice; feature 3 exercises the
/// schema and publication barrier through it.
/// </remarks>
/// <param name="JobId">The 1-based deterministic job identifier.</param>
/// <param name="ModelSignature">The referenced model signature.</param>
/// <param name="Lattice">The referenced initial lattice row.</param>
/// <param name="Kind">The job kind (<c>kmc</c> or <c>mmc</c>).</param>
/// <param name="CollectionName">The authored collection name.</param>
/// <param name="ConfigName">The authored configuration name.</param>
/// <param name="CollectionIndex">The zero-based collection index.</param>
/// <param name="ConfigIndex">The zero-based configuration index.</param>
/// <param name="JobIndex">The zero-based duplicate index.</param>
/// <param name="TemperatureK">The temperature in kelvin.</param>
/// <param name="ElectricFieldVm">The electric-field modulus in V/m.</param>
/// <param name="TargetMcsp">The target MCSP.</param>
/// <param name="WriteOutCount">The intermediate data-out count.</param>
/// <param name="PreRunMcsp">The KMC pre-run MCSP.</param>
/// <param name="TimeLimitSeconds">The wall-clock limit, or <see langword="null"/>.</param>
/// <param name="MinSuccessRate">The lower abort rate, or <see langword="null"/>.</param>
/// <param name="LatticeA">The supercell size along a.</param>
/// <param name="LatticeB">The supercell size along b.</param>
/// <param name="LatticeC">The supercell size along c.</param>
/// <param name="DopingInfo">The canonical doping summary.</param>
/// <param name="Flags">The composed <c>SolverJobFlags</c> word.</param>
/// <param name="Seed">The 32-byte deterministic job seed.</param>
/// <param name="PayloadFormatVersion">The encoded job-payload format version.</param>
/// <param name="Payload">The encoded Zstd+MessagePack job payload.</param>
internal sealed record PackageJobRow(
    int JobId,
    string ModelSignature,
    PackageLatticeRow Lattice,
    string Kind,
    string CollectionName,
    string ConfigName,
    int CollectionIndex,
    int ConfigIndex,
    int JobIndex,
    double TemperatureK,
    double ElectricFieldVm,
    long TargetMcsp,
    int WriteOutCount,
    long PreRunMcsp,
    long? TimeLimitSeconds,
    double? MinSuccessRate,
    int LatticeA,
    int LatticeB,
    int LatticeC,
    string DopingInfo,
    uint Flags,
    ReadOnlyMemory<byte> Seed,
    int PayloadFormatVersion,
    ReadOnlyMemory<byte> Payload);
