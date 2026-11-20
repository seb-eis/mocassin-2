// SPDX-License-Identifier: MIT

//! Shared test helpers: temporary directories, fixture paths, and a tiny MessagePack encoder.

#![allow(dead_code)]

use std::fs;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};

static COUNTER: AtomicU64 = AtomicU64::new(0);

/// A unique temporary directory that removes itself on drop.
pub struct TempDir {
    path: PathBuf,
}

impl TempDir {
    /// Creates a unique directory under the system temporary directory.
    pub fn new(label: &str) -> Self {
        let unique = COUNTER.fetch_add(1, Ordering::Relaxed);
        let path = std::env::temp_dir().join(format!(
            "mocassin-msl-test-{label}-{}-{unique}",
            std::process::id()
        ));
        fs::create_dir_all(&path).expect("create the temporary directory");
        Self { path }
    }

    /// Returns the directory path.
    pub fn path(&self) -> &Path {
        &self.path
    }

    /// Joins a file name onto the directory.
    pub fn join(&self, name: &str) -> PathBuf {
        self.path.join(name)
    }
}

impl Drop for TempDir {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.path);
    }
}

/// Returns the path of a checked-in package fixture directory.
pub fn fixture_dir(package: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages")
        .join(package)
}

/// Returns the path of a checked-in package file.
pub fn fixture_file(package: &str, name: &str) -> PathBuf {
    fixture_dir(package).join(name)
}

/// Returns the path of a raw payload extracted from the `msl-v1` fixture.
pub fn raw_payload(name: &str) -> PathBuf {
    fixture_dir("msl-v1").join("payloads").join(name)
}

/// Copies a fixture into a temporary directory and returns the copy's path.
pub fn copy_fixture(package: &str, name: &str, directory: &TempDir) -> PathBuf {
    let target = directory.join(name);
    fs::copy(fixture_file(package, name), &target).expect("copy the fixture");
    target
}

/// Minimal MessagePack encoding helpers for hostile and synthetic payloads.
pub mod msgpack {
    /// Encodes a map header.
    pub fn map(len: u32) -> Vec<u8> {
        if len < 16 {
            vec![0x80 | len as u8]
        } else if len <= u32::from(u16::MAX) {
            let mut bytes = vec![0xde];
            bytes.extend_from_slice(&(len as u16).to_be_bytes());
            bytes
        } else {
            let mut bytes = vec![0xdf];
            bytes.extend_from_slice(&len.to_be_bytes());
            bytes
        }
    }

    /// Encodes an array header.
    pub fn array(len: u32) -> Vec<u8> {
        if len < 16 {
            vec![0x90 | len as u8]
        } else if len <= u32::from(u16::MAX) {
            let mut bytes = vec![0xdc];
            bytes.extend_from_slice(&(len as u16).to_be_bytes());
            bytes
        } else {
            let mut bytes = vec![0xdd];
            bytes.extend_from_slice(&len.to_be_bytes());
            bytes
        }
    }

    /// Encodes a UTF-8 string.
    pub fn string(value: &str) -> Vec<u8> {
        let bytes = value.as_bytes();
        let mut encoded = if bytes.len() < 32 {
            vec![0xa0 | bytes.len() as u8]
        } else if bytes.len() <= usize::from(u8::MAX) {
            vec![0xd9, bytes.len() as u8]
        } else if bytes.len() <= usize::from(u16::MAX) {
            let mut header = vec![0xda];
            header.extend_from_slice(&(bytes.len() as u16).to_be_bytes());
            header
        } else {
            let mut header = vec![0xdb];
            header.extend_from_slice(&(bytes.len() as u32).to_be_bytes());
            header
        };
        encoded.extend_from_slice(bytes);
        encoded
    }

    /// Encodes an unsigned integer with the smallest width.
    pub fn uint(value: u64) -> Vec<u8> {
        if value < 128 {
            vec![value as u8]
        } else if value <= u64::from(u8::MAX) {
            vec![0xcc, value as u8]
        } else if value <= u64::from(u16::MAX) {
            let mut bytes = vec![0xcd];
            bytes.extend_from_slice(&(value as u16).to_be_bytes());
            bytes
        } else if value <= u64::from(u32::MAX) {
            let mut bytes = vec![0xce];
            bytes.extend_from_slice(&(value as u32).to_be_bytes());
            bytes
        } else {
            let mut bytes = vec![0xcf];
            bytes.extend_from_slice(&value.to_be_bytes());
            bytes
        }
    }

    /// Encodes a binary value.
    pub fn bin(value: &[u8]) -> Vec<u8> {
        let mut bytes = if value.len() <= usize::from(u8::MAX) {
            vec![0xc4, value.len() as u8]
        } else if value.len() <= usize::from(u16::MAX) {
            let mut header = vec![0xc5];
            header.extend_from_slice(&(value.len() as u16).to_be_bytes());
            header
        } else {
            let mut header = vec![0xc6];
            header.extend_from_slice(&(value.len() as u32).to_be_bytes());
            header
        };
        bytes.extend_from_slice(value);
        bytes
    }

    /// Concatenates encoded parts.
    pub fn concat(parts: &[&[u8]]) -> Vec<u8> {
        let mut bytes = Vec::new();
        for part in parts {
            bytes.extend_from_slice(part);
        }
        bytes
    }
}

/// Synthetic payload builders used by the decode and hostile-input tests.
pub mod payloads {
    use super::msgpack;

    /// Builds a v1 lattice payload with an explicit `position_count` encoding and particle form.
    pub fn lattice(
        a: u32,
        b: u32,
        c: u32,
        position_count: &[u8],
        particles: &[u8],
        as_bin: bool,
    ) -> Vec<u8> {
        let mut bytes = msgpack::map(4);
        bytes.extend(msgpack::string("format_version"));
        bytes.extend(msgpack::uint(1));
        bytes.extend(msgpack::string("lattice_size"));
        bytes.extend(msgpack::array(3));
        for value in [a, b, c] {
            bytes.extend(msgpack::uint(u64::from(value)));
        }
        bytes.extend(msgpack::string("position_count"));
        bytes.extend_from_slice(position_count);
        bytes.extend(msgpack::string("lattice"));
        if as_bin {
            bytes.extend(msgpack::bin(particles));
        } else {
            bytes.extend(msgpack::array(particles.len() as u32));
            for particle in particles {
                bytes.extend(msgpack::uint(u64::from(*particle)));
            }
        }
        bytes
    }
}
