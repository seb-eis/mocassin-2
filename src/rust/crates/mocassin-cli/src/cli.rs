// SPDX-License-Identifier: MIT

//! The `mocassin-sim` argument surface.
//!
//! The parser is the Step 13 inspection CLI: `--dry-run` loads and validates one job and prints a stable
//! summary without writing anything. The simulation engines and the full option surface (batch, extension
//! loading, `--stdout`, `--validate-only`, `--no-state`, `--log-file`) arrive in Steps 16–19.
//!
//! `--log-level` is the only log filter; `RUST_LOG` is deliberately not consulted so scripts get deterministic
//! output. Parse errors are remapped to the invalid-arguments exit code by the caller.

use std::path::PathBuf;

use clap::{Parser, ValueEnum};

/// Command-line options for the MOCASSIN solver.
#[derive(Debug, Parser)]
#[command(name = "mocassin-sim", version, about = "MOCASSIN Monte Carlo solver")]
pub struct Cli {
    /// The immutable `.msl` package to inspect.
    #[arg(long, value_name = "path")]
    pub db: PathBuf,

    /// The 1-based job id.
    #[arg(long, value_name = "id")]
    pub job: u32,

    /// The job output directory; accepted in Step 13 but never created.
    #[arg(long, value_name = "dir")]
    pub io: Option<PathBuf>,

    /// Override the effective seed for this process (exactly 64 hexadecimal characters).
    #[arg(long, value_name = "hex32", value_parser = parse_seed)]
    pub seed: Option<[u8; 32]>,

    /// Worker threads in 1..=256; accepted and validated, unused until Step 19.
    #[arg(long, value_name = "n", value_parser = parse_threads)]
    pub threads: Option<u16>,

    /// Request a solver extension; the ABI is implemented in Step 19.
    #[arg(
        long,
        value_name = "path",
        action = clap::ArgAction::Append,
        conflicts_with = "no_extensions"
    )]
    pub extension: Vec<PathBuf>,

    /// Disable all extensions; a no-op in Step 13.
    #[arg(long)]
    pub no_extensions: bool,

    /// Jump-log energy threshold in eV; accepted and validated, unused until Step 18.
    #[arg(long, value_name = "ev", value_parser = parse_jump_log)]
    pub jump_log_max_ev: Option<f64>,

    /// Log level (the only filter; `RUST_LOG` is ignored).
    #[arg(long, value_enum, default_value = "info")]
    pub log_level: LogLevel,

    /// Log format.
    #[arg(long, value_enum, default_value = "human")]
    pub log_format: LogFormat,

    /// Load, validate, and print the summary; write nothing.
    #[arg(long)]
    pub dry_run: bool,
}

/// The `--log-level` values.
#[derive(Debug, Clone, Copy, PartialEq, Eq, ValueEnum)]
pub enum LogLevel {
    /// Errors only.
    Error,
    /// Warnings and errors.
    Warn,
    /// Informational messages; the default.
    Info,
    /// Debug diagnostics.
    Debug,
    /// Trace diagnostics.
    Trace,
}

impl LogLevel {
    /// Maps the CLI level to a [`tracing::Level`].
    #[must_use]
    pub const fn tracing_level(self) -> tracing::Level {
        match self {
            Self::Error => tracing::Level::ERROR,
            Self::Warn => tracing::Level::WARN,
            Self::Info => tracing::Level::INFO,
            Self::Debug => tracing::Level::DEBUG,
            Self::Trace => tracing::Level::TRACE,
        }
    }
}

/// The `--log-format` values.
#[derive(Debug, Clone, Copy, PartialEq, Eq, ValueEnum)]
pub enum LogFormat {
    /// Human-readable log lines.
    Human,
    /// JSON log records.
    Json,
}

/// Parses a 64-character hexadecimal seed, case-insensitively.
fn parse_seed(value: &str) -> Result<[u8; 32], String> {
    if value.len() != 64 {
        return Err(format!(
            "the seed must contain exactly 64 hexadecimal characters, found {}",
            value.len()
        ));
    }
    if !value.is_ascii() {
        return Err("the seed must be ASCII hexadecimal".to_owned());
    }

    let mut seed = [0_u8; 32];
    for (index, chunk) in value.as_bytes().as_chunks::<2>().0.iter().enumerate() {
        let text = core::str::from_utf8(chunk).expect("ASCII bytes");
        seed[index] = u8::from_str_radix(text, 16)
            .map_err(|_| format!("the seed contains a non-hexadecimal pair \"{text}\""))?;
    }
    Ok(seed)
}

/// Parses a thread count in `1..=256`.
fn parse_threads(value: &str) -> Result<u16, String> {
    let threads = value
        .parse::<u16>()
        .map_err(|_| format!("the thread count \"{value}\" is not an integer"))?;
    if !(1..=256).contains(&threads) {
        return Err(format!("the thread count {threads} is outside 1..=256"));
    }
    Ok(threads)
}

/// Parses a finite jump-log threshold greater than zero.
fn parse_jump_log(value: &str) -> Result<f64, String> {
    let limit = value
        .parse::<f64>()
        .map_err(|_| format!("the jump-log threshold \"{value}\" is not a number"))?;
    if !limit.is_finite() || limit <= 0.0 {
        return Err("the jump-log threshold must be finite and greater than zero".to_owned());
    }
    Ok(limit)
}

#[cfg(test)]
mod tests {
    use super::{Cli, LogFormat, LogLevel, parse_jump_log, parse_seed, parse_threads};
    use clap::CommandFactory;
    use clap::Parser;

    fn parse(arguments: &[&str]) -> Result<Cli, clap::Error> {
        Cli::try_parse_from(arguments)
    }

    #[test]
    fn the_parser_definition_is_valid() {
        Cli::command().debug_assert();
    }

    #[test]
    fn a_seed_is_case_insensitive_hexadecimal() {
        let lower = parse_seed(&"ab".repeat(32)).expect("lowercase hex");
        let upper = parse_seed(&"AB".repeat(32)).expect("uppercase hex");
        assert_eq!(lower, upper);
        assert_eq!(lower[0], 0xab);
        assert_eq!(lower[31], 0xab);
    }

    #[test]
    fn a_bad_seed_is_rejected() {
        assert!(parse_seed("00").is_err());
        assert!(parse_seed(&"gg".repeat(32)).is_err());
        assert!(parse_seed(&"é".repeat(32)).is_err());
    }

    #[test]
    fn thread_counts_are_limited_to_one_through_256() {
        assert_eq!(parse_threads("1"), Ok(1));
        assert_eq!(parse_threads("256"), Ok(256));
        assert!(parse_threads("0").is_err());
        assert!(parse_threads("257").is_err());
        assert!(parse_threads("x").is_err());
    }

    #[test]
    fn jump_log_thresholds_are_finite_and_positive() {
        assert_eq!(parse_jump_log("1.5"), Ok(1.5));
        assert!(parse_jump_log("0").is_err());
        assert!(parse_jump_log("-1").is_err());
        assert!(parse_jump_log("NaN").is_err());
        assert!(parse_jump_log("inf").is_err());
    }

    #[test]
    fn the_defaults_are_info_and_human() {
        let cli = parse(&["mocassin-sim", "--db", "p.msl", "--job", "1"]).expect("parse");
        assert_eq!(cli.log_level, LogLevel::Info);
        assert_eq!(cli.log_format, LogFormat::Human);
        assert!(!cli.dry_run);
        assert!(cli.extension.is_empty());
    }

    #[test]
    fn extensions_conflict_with_no_extensions() {
        let error = parse(&[
            "mocassin-sim",
            "--db",
            "p.msl",
            "--job",
            "1",
            "--extension",
            "x.so",
            "--no-extensions",
        ])
        .expect_err("the options conflict");
        assert_eq!(error.kind(), clap::error::ErrorKind::ArgumentConflict);
    }

    #[test]
    fn a_bad_thread_value_is_a_parse_error() {
        let error = parse(&[
            "mocassin-sim",
            "--db",
            "p.msl",
            "--job",
            "1",
            "--threads",
            "0",
        ])
        .expect_err("the thread count is invalid");
        assert_eq!(error.kind(), clap::error::ErrorKind::ValueValidation);
    }
}
