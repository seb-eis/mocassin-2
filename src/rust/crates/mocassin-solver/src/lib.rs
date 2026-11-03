// SPDX-License-Identifier: MIT

//! Placeholder for the simulation context, energy engine, KMC, MMC, and tracking code.
//!
//! This crate is a Step 00 placeholder. Its public surface is replaced by the real implementation in a
//! later roadmap step.

/// Placeholder for the simulation context, energy engine, KMC, MMC, and tracking code.
#[derive(Debug, Default)]
pub struct SolverModule;

#[cfg(test)]
mod tests {
    use super::SolverModule;

    #[test]
    fn smoke_test() {
        let _ = SolverModule;
    }
}
