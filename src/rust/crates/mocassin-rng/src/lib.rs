//! Placeholder for deterministic RNG stream derivation.
//!
//! This crate is a Step 00 placeholder. Its public surface is replaced by the real implementation in a
//! later roadmap step.

/// Placeholder for deterministic RNG stream derivation.
#[derive(Debug, Default)]
pub struct RngModule;

#[cfg(test)]
mod tests {
    use super::RngModule;

    #[test]
    fn smoke_test() {
        let _ = RngModule;
    }
}
