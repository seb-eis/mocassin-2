// SPDX-License-Identifier: MIT

//! Simulation settings and the composed solver job-flag word.
//!
//! [`SimulationSettings`] keeps only the authored flag bits a model's simulation carries. [`JobSettings`] is the
//! resolved per-job word the .NET build composes; its [`SolverJobFlags`] additionally encodes the job kind. The
//! reader maps both payloads onto these types and validates the value ranges (Step 13 design §3.5).

use core::fmt;
use core::str::FromStr;

use serde::{Deserialize, Serialize};

/// The Monte Carlo engine a job or jump collection belongs to.
///
/// The payload stores the lowercase strings `"kmc"` and `"mmc"`; any other spelling is rejected rather than
/// silently defaulted.
#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord, Hash, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum SimulationKind {
    /// Kinetic Monte Carlo; advances simulation time.
    Kmc,
    /// Metropolis Monte Carlo; samples equilibrium states without advancing time.
    Mmc,
}

impl SimulationKind {
    /// Returns the payload string for this kind.
    #[must_use]
    pub const fn as_str(self) -> &'static str {
        match self {
            Self::Kmc => "kmc",
            Self::Mmc => "mmc",
        }
    }
}

impl fmt::Display for SimulationKind {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter.write_str(self.as_str())
    }
}

impl FromStr for SimulationKind {
    type Err = UnknownSimulationKind;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        match value {
            "kmc" => Ok(Self::Kmc),
            "mmc" => Ok(Self::Mmc),
            other => Err(UnknownSimulationKind(other.to_owned())),
        }
    }
}

/// The string in a package payload was not a supported simulation kind.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct UnknownSimulationKind(String);

impl UnknownSimulationKind {
    /// Returns the rejected payload string.
    #[must_use]
    pub fn value(&self) -> &str {
        &self.0
    }
}

impl fmt::Display for UnknownSimulationKind {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            formatter,
            "unknown simulation kind {:?}; expected \"kmc\" or \"mmc\"",
            self.0
        )
    }
}

impl std::error::Error for UnknownSimulationKind {}

/// Composed solver flag word (`SolverJobFlags` in the `.msl` job payload).
///
/// Bit 0 `no_saving`, bit 1 `no_jump_logging`, bit 2 `use_fast_exp`, bit 3 `deterministic`,
/// bit 4 `use_dual_dof_correction`, bit 5 `use_prerun`, bit 6 `kmc_simulation`, bit 7 `mmc_simulation`. Bits
/// 8–31 are reserved and must be zero in v1.
///
/// Exactly one kind bit must be set and must match the job's [`SimulationKind`];
/// `use_dual_dof_correction` and `use_prerun` are KMC-only. Use [`SolverJobFlags::validate`] for the full check.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, Hash)]
#[repr(transparent)]
pub struct SolverJobFlags(u32);

impl SolverJobFlags {
    /// Bit 0: the run suppresses state saving.
    pub const NO_SAVING: Self = Self(1 << 0);

    /// Bit 1: the run suppresses jump logging.
    pub const NO_JUMP_LOGGING: Self = Self(1 << 1);

    /// Bit 2: the run may use the fast exponential approximation.
    pub const USE_FAST_EXP: Self = Self(1 << 2);

    /// Bit 3: the run is deterministic.
    pub const DETERMINISTIC: Self = Self(1 << 3);

    /// Bit 4: the KMC normalization uses the dual-DOF correction.
    pub const USE_DUAL_DOF_CORRECTION: Self = Self(1 << 4);

    /// Bit 5: the KMC job runs a pre-run before the main run.
    pub const USE_PRERUN: Self = Self(1 << 5);

    /// Bit 6: the job is KMC.
    pub const KMC_SIMULATION: Self = Self(1 << 6);

    /// Bit 7: the job is MMC.
    pub const MMC_SIMULATION: Self = Self(1 << 7);

    /// Mask of the authored bits (0–3) that `simulation.default_settings.flags` may contain.
    pub const AUTHORED_BITS_MASK: u32 = 0b1111;

    /// Mask of the two job-kind bits.
    pub const KIND_BITS_MASK: u32 = Self::KMC_SIMULATION.0 | Self::MMC_SIMULATION.0;

    /// Mask of the bits that are valid only for a KMC job.
    pub const KMC_ONLY_BITS_MASK: u32 = Self::USE_DUAL_DOF_CORRECTION.0 | Self::USE_PRERUN.0;

    /// Mask of the bits that v1 reserves and requires to be zero.
    pub const RESERVED_BITS_MASK: u32 =
        !(Self::AUTHORED_BITS_MASK | Self::KMC_ONLY_BITS_MASK | Self::KIND_BITS_MASK);

    /// Wraps a raw flag word. The bits are not validated; call [`SolverJobFlags::validate`].
    #[must_use]
    pub const fn from_bits(bits: u32) -> Self {
        Self(bits)
    }

    /// Returns the raw flag word.
    #[must_use]
    pub const fn bits(self) -> u32 {
        self.0
    }

    /// Returns `true` when every bit of `other` is also set in `self`.
    #[must_use]
    pub const fn contains(self, other: Self) -> bool {
        self.0 & other.0 == other.0
    }

    /// Returns the union of both flag words.
    #[must_use]
    pub const fn union(self, other: Self) -> Self {
        Self(self.0 | other.0)
    }

    /// Returns the declared job kind, or `None` when neither or both kind bits are set.
    #[must_use]
    pub const fn kind(self) -> Option<SimulationKind> {
        match self.0 & Self::KIND_BITS_MASK {
            bits if bits == Self::KMC_SIMULATION.0 => Some(SimulationKind::Kmc),
            bits if bits == Self::MMC_SIMULATION.0 => Some(SimulationKind::Mmc),
            _ => None,
        }
    }

    /// Returns `true` when any reserved bit (8–31) is set.
    #[must_use]
    pub const fn has_reserved_bits(self) -> bool {
        self.0 & Self::RESERVED_BITS_MASK != 0
    }

    /// Validates the composed word against the job's kind.
    ///
    /// The checks run in a fixed order so a malformed word has one documented error:
    /// reserved bits, single kind bit, kind match, then KMC-only bits on an MMC job.
    ///
    /// # Errors
    ///
    /// Returns the first violated rule as a [`SolverJobFlagsError`].
    pub const fn validate(self, kind: SimulationKind) -> Result<(), SolverJobFlagsError> {
        if self.has_reserved_bits() {
            return Err(SolverJobFlagsError::ReservedBitsSet);
        }

        let declared = match self.kind() {
            Some(declared) => declared,
            None => return Err(SolverJobFlagsError::KindBitsNotSingle),
        };

        if !matches!(
            (declared, kind),
            (SimulationKind::Kmc, SimulationKind::Kmc) | (SimulationKind::Mmc, SimulationKind::Mmc)
        ) {
            return Err(SolverJobFlagsError::KindMismatch {
                expected: kind,
                found: declared,
            });
        }

        if matches!(kind, SimulationKind::Mmc) && self.0 & Self::KMC_ONLY_BITS_MASK != 0 {
            return Err(SolverJobFlagsError::KmcOnlyBitForMmc);
        }

        Ok(())
    }
}

impl fmt::Display for SolverJobFlags {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(formatter, "0x{:02x}", self.0)
    }
}

/// A composed solver job-flag word violates the v1 contract.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum SolverJobFlagsError {
    /// A reserved bit (8–31) is set.
    ReservedBitsSet,
    /// Neither or both job-kind bits are set.
    KindBitsNotSingle,
    /// The declared kind bit does not match the job kind.
    KindMismatch {
        /// The kind the job actually is.
        expected: SimulationKind,
        /// The kind encoded by the flag word.
        found: SimulationKind,
    },
    /// An MMC job sets a KMC-only bit (`use_dual_dof_correction` or `use_prerun`).
    KmcOnlyBitForMmc,
}

impl fmt::Display for SolverJobFlagsError {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::ReservedBitsSet => formatter.write_str("solver job flags set reserved bits 8-31"),
            Self::KindBitsNotSingle => {
                formatter.write_str("solver job flags must set exactly one kind bit")
            }
            Self::KindMismatch { expected, found } => write!(
                formatter,
                "solver job flags declare kind {found} but the job is {expected}"
            ),
            Self::KmcOnlyBitForMmc => {
                formatter.write_str("solver job flags set a KMC-only bit on an MMC job")
            }
        }
    }
}

impl std::error::Error for SolverJobFlagsError {}

/// Resolved settings of one simulation job (the `.msl` job payload).
///
/// The job payload is authoritative; its values are not required to equal the referenced
/// `simulations[].default_settings` (the ceria fixture's job target is 150 while simulation 0's default is 200).
#[derive(Clone, Debug, PartialEq)]
pub struct JobSettings {
    /// Index of the referenced simulation in the model payload.
    pub simulation_index: u32,
    /// Temperature in kelvin; must be positive.
    pub temperature_k: f64,
    /// Target number of Monte Carlo steps per particle.
    pub target_mcsp: u64,
    /// Number of state write-outs requested over the run.
    pub write_out_count: u32,
    /// KMC pre-run length in MCSP.
    pub pre_run_mcsp: u64,
    /// Wall-clock limit in seconds, when configured.
    pub time_limit_s: Option<u64>,
    /// Minimum accepted-success rate, when configured.
    pub min_success_rate: Option<f64>,
    /// Electric-field modulus in volts per metre.
    pub electric_field_vm: f64,
    /// Normalized Cartesian electric-field direction.
    pub electric_field_direction: [f64; 3],
    /// Normalization energy in electronvolts, when configured.
    pub normalization_energy_ev: Option<f64>,
    /// Maximum attempt frequency in hertz, when configured.
    pub max_attempt_frequency_hz: Option<f64>,
    /// Equilibrium break tolerance, when configured (MMC only).
    pub break_tolerance: Option<f64>,
    /// Equilibrium break sample length in MCSP, when configured (MMC only).
    pub break_sample_length_mcsp: Option<u64>,
    /// Equilibrium break sample interval in MCSP, when configured (MMC only).
    pub break_sample_interval_mcsp: Option<u64>,
    /// Result sampling interval in MCSP, when configured.
    pub result_sample_mcsp: Option<u64>,
    /// Composed job-flag word; see [`SolverJobFlags`].
    pub flags: SolverJobFlags,
}

/// Default settings of one simulation in the model payload.
///
/// [`Self::flags`] holds only the authored bits (0–3); the composed job word is [`JobSettings::flags`].
#[derive(Clone, Debug, PartialEq)]
pub struct SimulationSettings {
    /// Temperature in kelvin; must be positive.
    pub temperature_k: f64,
    /// Target number of Monte Carlo steps per particle.
    pub target_mcsp: u64,
    /// Number of state write-outs requested over the run.
    pub write_out_count: u32,
    /// KMC pre-run length in MCSP.
    pub pre_run_mcsp: u64,
    /// Wall-clock limit in seconds, when configured.
    pub time_limit_s: Option<u64>,
    /// Minimum accepted-success rate, when configured.
    pub min_success_rate: Option<f64>,
    /// Electric-field modulus in volts per metre.
    pub electric_field_vm: f64,
    /// Normalized Cartesian electric-field direction.
    pub electric_field_direction: [f64; 3],
    /// Normalization energy in electronvolts, when configured.
    pub normalization_energy_ev: Option<f64>,
    /// Maximum attempt frequency in hertz, when configured.
    pub max_attempt_frequency_hz: Option<f64>,
    /// Equilibrium break tolerance, when configured (MMC only).
    pub break_tolerance: Option<f64>,
    /// Equilibrium break sample length in MCSP, when configured (MMC only).
    pub break_sample_length_mcsp: Option<u64>,
    /// Equilibrium break sample interval in MCSP, when configured (MMC only).
    pub break_sample_interval_mcsp: Option<u64>,
    /// Result sampling interval in MCSP, when configured.
    pub result_sample_mcsp: Option<u64>,
    /// Authored flag bits 0–3 only.
    pub flags: u32,
}

#[cfg(test)]
mod tests {
    use core::str::FromStr;

    use super::{
        JobSettings, SimulationKind, SimulationSettings, SolverJobFlags, SolverJobFlagsError,
    };

    #[test]
    fn kinds_round_trip_the_payload_strings() {
        assert_eq!(SimulationKind::Kmc.as_str(), "kmc");
        assert_eq!(SimulationKind::Mmc.as_str(), "mmc");
        assert_eq!(SimulationKind::from_str("kmc"), Ok(SimulationKind::Kmc));
        assert_eq!("kmc".parse::<SimulationKind>(), Ok(SimulationKind::Kmc));
        assert_eq!("mmc".parse(), Ok(SimulationKind::Mmc));
        assert_eq!(SimulationKind::Kmc.to_string(), "kmc");
    }

    #[test]
    fn kind_parsing_is_exact_and_case_sensitive() {
        assert!("KMC".parse::<SimulationKind>().is_err());
        assert!("".parse::<SimulationKind>().is_err());
        let error = "FFT".parse::<SimulationKind>().expect_err("must reject");
        assert_eq!(error.value(), "FFT");
        assert!(error.to_string().contains("\"FFT\""));
    }

    #[test]
    fn flag_bits_match_the_data_contract() {
        assert_eq!(SolverJobFlags::NO_SAVING.bits(), 0x01);
        assert_eq!(SolverJobFlags::NO_JUMP_LOGGING.bits(), 0x02);
        assert_eq!(SolverJobFlags::USE_FAST_EXP.bits(), 0x04);
        assert_eq!(SolverJobFlags::DETERMINISTIC.bits(), 0x08);
        assert_eq!(SolverJobFlags::USE_DUAL_DOF_CORRECTION.bits(), 0x10);
        assert_eq!(SolverJobFlags::USE_PRERUN.bits(), 0x20);
        assert_eq!(SolverJobFlags::KMC_SIMULATION.bits(), 0x40);
        assert_eq!(SolverJobFlags::MMC_SIMULATION.bits(), 0x80);
        assert_eq!(SolverJobFlags::AUTHORED_BITS_MASK, 0x0F);
        assert_eq!(SolverJobFlags::KMC_ONLY_BITS_MASK, 0x30);
        assert_eq!(SolverJobFlags::KIND_BITS_MASK, 0xC0);
        assert_eq!(SolverJobFlags::RESERVED_BITS_MASK, 0xFFFF_FF00);
    }

    #[test]
    fn the_fixture_word_validates_as_kmc() {
        // The ceria fixture job word from the dry-run contract: no-jump-logging, dual-DOF, pre-run, KMC.
        let flags = SolverJobFlags::from_bits(0x72);
        assert!(flags.contains(SolverJobFlags::NO_JUMP_LOGGING));
        assert!(flags.contains(SolverJobFlags::USE_DUAL_DOF_CORRECTION));
        assert!(flags.contains(SolverJobFlags::USE_PRERUN));
        assert!(flags.contains(SolverJobFlags::KMC_SIMULATION));
        assert!(!flags.contains(SolverJobFlags::MMC_SIMULATION));
        assert_eq!(flags.kind(), Some(SimulationKind::Kmc));
        assert_eq!(flags.validate(SimulationKind::Kmc), Ok(()));
        assert_eq!(flags.bits() & SolverJobFlags::AUTHORED_BITS_MASK, 0x02);
    }

    #[test]
    fn union_and_contains_compose_the_words() {
        let flags = SolverJobFlags::NO_SAVING.union(SolverJobFlags::DETERMINISTIC);
        assert_eq!(flags.bits(), 0b1001);
        assert!(flags.contains(SolverJobFlags::NO_SAVING));
        assert!(!flags.contains(SolverJobFlags::NO_JUMP_LOGGING));
        assert_eq!(SolverJobFlags::default().bits(), 0);
    }

    #[test]
    fn kind_requires_exactly_one_kind_bit() {
        assert_eq!(SolverJobFlags::default().kind(), None);
        assert_eq!(
            SolverJobFlags::KMC_SIMULATION.kind(),
            Some(SimulationKind::Kmc)
        );
        assert_eq!(
            SolverJobFlags::MMC_SIMULATION.kind(),
            Some(SimulationKind::Mmc)
        );
        assert_eq!(
            SolverJobFlags::KMC_SIMULATION
                .union(SolverJobFlags::MMC_SIMULATION)
                .kind(),
            None
        );
    }

    #[test]
    fn validate_rejects_reserved_bits_first() {
        let flags = SolverJobFlags::from_bits(1 << 8).union(SolverJobFlags::KMC_SIMULATION);
        assert_eq!(
            flags.validate(SimulationKind::Kmc),
            Err(SolverJobFlagsError::ReservedBitsSet)
        );
        assert!(flags.has_reserved_bits());
        assert!(
            !SolverJobFlags::NO_SAVING
                .union(SolverJobFlags::MMC_SIMULATION)
                .has_reserved_bits()
        );
    }

    #[test]
    fn validate_rejects_missing_or_double_kind_bits() {
        assert_eq!(
            SolverJobFlags::NO_SAVING.validate(SimulationKind::Kmc),
            Err(SolverJobFlagsError::KindBitsNotSingle)
        );
        let both = SolverJobFlags::KMC_SIMULATION.union(SolverJobFlags::MMC_SIMULATION);
        assert_eq!(
            both.validate(SimulationKind::Kmc),
            Err(SolverJobFlagsError::KindBitsNotSingle)
        );
    }

    #[test]
    fn validate_rejects_a_kind_mismatch() {
        assert_eq!(
            SolverJobFlags::KMC_SIMULATION.validate(SimulationKind::Mmc),
            Err(SolverJobFlagsError::KindMismatch {
                expected: SimulationKind::Mmc,
                found: SimulationKind::Kmc,
            })
        );
    }

    #[test]
    fn validate_rejects_kmc_only_bits_on_mmc() {
        for kmc_only in [
            SolverJobFlags::USE_DUAL_DOF_CORRECTION,
            SolverJobFlags::USE_PRERUN,
        ] {
            let flags = SolverJobFlags::MMC_SIMULATION.union(kmc_only);
            assert_eq!(flags.kind(), Some(SimulationKind::Mmc));
            assert_eq!(
                flags.validate(SimulationKind::Mmc),
                Err(SolverJobFlagsError::KmcOnlyBitForMmc)
            );
        }
    }

    #[test]
    fn validate_accepts_a_minimal_word_for_each_kind() {
        assert_eq!(
            SolverJobFlags::KMC_SIMULATION.validate(SimulationKind::Kmc),
            Ok(())
        );
        assert_eq!(
            SolverJobFlags::MMC_SIMULATION.validate(SimulationKind::Mmc),
            Ok(())
        );
    }

    #[test]
    fn flag_errors_describe_the_violation() {
        assert!(
            SolverJobFlagsError::ReservedBitsSet
                .to_string()
                .contains("reserved")
        );
        assert!(
            SolverJobFlagsError::KindBitsNotSingle
                .to_string()
                .contains("exactly one kind bit")
        );
        assert!(
            SolverJobFlagsError::KmcOnlyBitForMmc
                .to_string()
                .contains("KMC-only")
        );
        assert_eq!(SolverJobFlags::from_bits(0x72).to_string(), "0x72");
    }

    #[test]
    fn job_settings_keep_the_composed_word_and_simulation_settings_the_authored_bits() {
        let job = JobSettings {
            simulation_index: 0,
            temperature_k: 773.0,
            target_mcsp: 150,
            write_out_count: 100,
            pre_run_mcsp: 250,
            time_limit_s: Some(31_536_000),
            min_success_rate: None,
            electric_field_vm: 10_000_000.0,
            electric_field_direction: [1.0, 0.0, 0.0],
            normalization_energy_ev: None,
            max_attempt_frequency_hz: None,
            break_tolerance: None,
            break_sample_length_mcsp: None,
            break_sample_interval_mcsp: None,
            result_sample_mcsp: None,
            flags: SolverJobFlags::from_bits(0x72),
        };
        assert_eq!(
            job.flags.bits() & SolverJobFlags::AUTHORED_BITS_MASK,
            SolverJobFlags::NO_JUMP_LOGGING.bits()
        );
        assert_eq!(job.flags.validate(SimulationKind::Kmc), Ok(()));

        let simulation = SimulationSettings {
            temperature_k: 773.0,
            target_mcsp: 200,
            write_out_count: 100,
            pre_run_mcsp: 0,
            time_limit_s: None,
            min_success_rate: None,
            electric_field_vm: 0.0,
            electric_field_direction: [1.0, 0.0, 0.0],
            normalization_energy_ev: None,
            max_attempt_frequency_hz: None,
            break_tolerance: None,
            break_sample_length_mcsp: None,
            break_sample_interval_mcsp: None,
            result_sample_mcsp: None,
            flags: SolverJobFlags::USE_FAST_EXP.bits(),
        };
        assert_eq!(simulation.flags, 0x04);
        assert_eq!(
            simulation.flags & !SolverJobFlags::AUTHORED_BITS_MASK,
            0,
            "simulation defaults may carry authored bits only"
        );
    }
}
