// SPDX-License-Identifier: MIT

//! `tracing` initialization for `mocassin-sim`.
//!
//! Logs always go to stderr; stdout is reserved for the dry-run summary. `--log-level` is the only filter and
//! `RUST_LOG` is not consulted.

use tracing_subscriber::fmt;

use crate::cli::{LogFormat, LogLevel};

/// Installs the stderr subscriber for the requested level and format.
pub fn init(level: LogLevel, format: LogFormat) {
    let level = level.tracing_level();
    let _ = match format {
        LogFormat::Human => fmt()
            .with_writer(std::io::stderr)
            .with_max_level(level)
            .with_target(false)
            .with_ansi(false)
            .try_init(),
        LogFormat::Json => fmt()
            .with_writer(std::io::stderr)
            .with_max_level(level)
            .with_target(false)
            .with_ansi(false)
            .json()
            .with_current_span(false)
            .with_span_list(false)
            .try_init(),
    };
}
