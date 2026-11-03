// SPDX-License-Identifier: MIT

//! Placeholder for the opt-in extension ABI.
//!
//! This crate is a Step 00 placeholder. Its public surface is replaced by the real implementation in a
//! later roadmap step.

/// Placeholder for the opt-in extension ABI.
#[derive(Debug, Default)]
pub struct ExtensionModule;

#[cfg(test)]
mod tests {
    use super::ExtensionModule;

    #[test]
    fn smoke_test() {
        let _ = ExtensionModule;
    }
}
