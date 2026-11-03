// SPDX-License-Identifier: MIT

//! Placeholder for the MessagePack .mcs state document.
//!
//! This crate is a Step 00 placeholder. Its public surface is replaced by the real implementation in a
//! later roadmap step.

/// Placeholder for the MessagePack .mcs state document.
#[derive(Debug, Default)]
pub struct StateModule;

#[cfg(test)]
mod tests {
    use super::StateModule;

    #[test]
    fn smoke_test() {
        let _ = StateModule;
    }
}
