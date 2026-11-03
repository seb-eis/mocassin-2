// SPDX-License-Identifier: MIT

//! `mocassin-sim` command-line entry point.
//!
//! Step 00 provides the argument-parser and `--version` stub. The production CLI is implemented in Step 19.

use clap::Parser;

/// Command-line options for the MOCASSIN solver.
#[derive(Debug, Parser)]
#[command(name = "mocassin-sim", version, about = "MOCASSIN Monte Carlo solver")]
struct Cli {}

fn main() {
    let _ = Cli::parse();
}

#[cfg(test)]
mod tests {
    use super::Cli;
    use clap::CommandFactory;

    #[test]
    fn cli_definition_is_valid() {
        Cli::command().debug_assert();
    }
}
