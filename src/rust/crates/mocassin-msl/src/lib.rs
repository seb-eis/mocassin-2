//! Placeholder for the .msl package reader.
//!
//! This crate is a Step 00 placeholder. Its public surface is replaced by the real implementation in a
//! later roadmap step.

/// Placeholder for the .msl package reader.
#[derive(Debug, Default)]
pub struct MslModule;

#[cfg(test)]
mod tests {
    use super::MslModule;

    #[test]
    fn smoke_test() {
        let _ = MslModule;
    }
}
