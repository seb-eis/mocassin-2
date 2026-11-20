// SPDX-License-Identifier: MIT

//! `mocassin-sim` command-line entry point.
//!
//! Step 13 implements the inspection path: parse the options, open the package read-only, load and validate one
//! job, and either print the dry-run summary or report that the simulation engines arrive in Steps 14–19. Logs
//! go to stderr; stdout is reserved for the summary. A panic in the inspection path is caught and mapped to the
//! internal-invariant exit code (13), never to the invalid-arguments code.

mod cli;
mod dry_run;
mod logging;

use std::process::ExitCode as ProcessExitCode;

use clap::Parser;
use clap::error::ErrorKind;
use mocassin_msl::Package;
use mocassin_types::{ExitCode, ReaderLimits};

use cli::Cli;

fn main() -> ProcessExitCode {
    match std::panic::catch_unwind(run) {
        Ok(code) => ProcessExitCode::from(code.code()),
        Err(_) => {
            eprintln!("error: internal error: the inspection path panicked");
            tracing::error!("internal error: the inspection path panicked");
            ProcessExitCode::from(ExitCode::InternalError.code())
        }
    }
}

fn run() -> ExitCode {
    let cli = match Cli::try_parse() {
        Ok(cli) => cli,
        Err(error) => {
            let _ = error.print();
            return if matches!(
                error.kind(),
                ErrorKind::DisplayHelp | ErrorKind::DisplayVersion
            ) {
                ExitCode::Completed
            } else {
                ExitCode::InvalidArguments
            };
        }
    };

    logging::init(cli.log_level, cli.log_format);

    // Debug-only hook so the CLI test can exercise the panic containment path.
    #[cfg(debug_assertions)]
    if std::env::var_os("MOCASSIN_SIM_TEST_PANIC").is_some() {
        panic!("MOCASSIN_SIM_TEST_PANIC");
    }

    inspect(&cli)
}

fn inspect(cli: &Cli) -> ExitCode {
    if !cli.extension.is_empty() {
        eprintln!("error: unsupported: the extension ABI is implemented in Step 19");
        return ExitCode::InvalidArguments;
    }

    if let Some(io) = &cli.io {
        tracing::debug!(path = %io.display(), "--io accepted but not created");
    }
    if let Some(threads) = cli.threads {
        tracing::debug!(threads, "--threads accepted and unused in Step 13");
    }
    if let Some(limit) = cli.jump_log_max_ev {
        tracing::debug!(limit, "--jump-log-max-ev accepted and unused in Step 13");
    }
    if cli.no_extensions {
        tracing::debug!("--no-extensions accepted as a no-op in Step 13");
    }

    let package = match Package::open_readonly(&cli.db, ReaderLimits::default()) {
        Ok(package) => package,
        Err(error) => {
            eprintln!("error: {error}");
            return error.exit_code();
        }
    };

    let job = match package.load_job_with_seed(cli.job, cli.seed) {
        Ok(job) => job,
        Err(context) => {
            eprintln!("error: {context}");
            return context.exit_code();
        }
    };

    tracing::info!(job_id = cli.job, kind = job.kind.as_str(), "job loaded");

    if !cli.dry_run {
        eprintln!("error: unsupported: simulation engines are implemented in Steps 14-19");
        return ExitCode::InvalidArguments;
    }

    print!("{}", dry_run::summary(&cli.db, &package, &job, &job.seed));
    ExitCode::Completed
}
