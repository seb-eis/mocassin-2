// SPDX-License-Identifier: MIT

//! Canonical projection round-trip tests over the .NET-generated `.bin` vectors.
//!
//! Each vector is decoded with a strict fixed-width reader, re-encoded with the forced-width writer, and compared
//! byte-for-byte. The sidecar JSON's projection SHA-256 and domain signature are recomputed from the bytes.

use std::fs;
use std::path::{Path, PathBuf};

use sha2::{Digest, Sha256};

const PARAMETERIZATION_DOMAIN: &str = "mocassin/parameterization-signature/v1";
const LATTICE_DOMAIN: &str = "mocassin/lattice-signature/v1";

#[derive(Debug, Clone, PartialEq)]
enum Value {
    Array(Vec<Value>),
    String(String),
    Binary(Vec<u8>),
    U64(u64),
    F64(f64),
}

fn projections_dir(kind: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/projections")
        .join(kind)
}

fn parse(bytes: &[u8]) -> Value {
    let mut position = 0;
    let value = parse_value(bytes, &mut position);
    assert_eq!(position, bytes.len(), "trailing projection bytes");
    value
}

fn parse_value(bytes: &[u8], position: &mut usize) -> Value {
    let marker = bytes[*position];
    *position += 1;
    match marker {
        0xdd => {
            let length = u32::from_be_bytes(take::<4>(bytes, position)) as usize;
            let mut values = Vec::with_capacity(length);
            for _ in 0..length {
                values.push(parse_value(bytes, position));
            }
            Value::Array(values)
        }
        0xdb => {
            let length = u32::from_be_bytes(take::<4>(bytes, position)) as usize;
            let text = core::str::from_utf8(&bytes[*position..*position + length])
                .expect("UTF-8 projection string")
                .to_owned();
            *position += length;
            Value::String(text)
        }
        0xc6 => {
            let length = u32::from_be_bytes(take::<4>(bytes, position)) as usize;
            let data = bytes[*position..*position + length].to_vec();
            *position += length;
            Value::Binary(data)
        }
        0xcf => Value::U64(u64::from_be_bytes(take::<8>(bytes, position))),
        0xcb => Value::F64(f64::from_bits(u64::from_be_bytes(take::<8>(
            bytes, position,
        )))),
        other => panic!("unexpected projection marker 0x{other:02x}"),
    }
}

fn take<const N: usize>(bytes: &[u8], position: &mut usize) -> [u8; N] {
    let value: [u8; N] = bytes[*position..*position + N]
        .try_into()
        .expect("N-byte slice");
    *position += N;
    value
}

fn encode(value: &Value, output: &mut Vec<u8>) {
    match value {
        Value::Array(values) => {
            output.push(0xdd);
            output.extend_from_slice(&(values.len() as u32).to_be_bytes());
            for value in values {
                encode(value, output);
            }
        }
        Value::String(text) => {
            output.push(0xdb);
            output.extend_from_slice(&(text.len() as u32).to_be_bytes());
            output.extend_from_slice(text.as_bytes());
        }
        Value::Binary(data) => {
            output.push(0xc6);
            output.extend_from_slice(&(data.len() as u32).to_be_bytes());
            output.extend_from_slice(data);
        }
        Value::U64(value) => {
            output.push(0xcf);
            output.extend_from_slice(&value.to_be_bytes());
        }
        Value::F64(value) => {
            output.push(0xcb);
            output.extend_from_slice(&value.to_bits().to_be_bytes());
        }
    }
}

fn hex(bytes: &[u8]) -> String {
    use core::fmt::Write as _;

    let mut text = String::with_capacity(bytes.len() * 2);
    for byte in bytes {
        let _ = write!(text, "{byte:02x}");
    }
    text
}

fn domain_signature(domain: &str, bytes: &[u8]) -> String {
    let mut hasher = Sha256::new();
    hasher.update(domain.as_bytes());
    hasher.update([0_u8]);
    hasher.update(bytes);
    hex(&hasher.finalize())
}

/// Extracts a string field from the simple sidecar JSON (no escapes in these fixtures).
fn json_field<'a>(text: &'a str, key: &str) -> &'a str {
    let needle = format!("\"{key}\"");
    let key_start = text
        .find(&needle)
        .unwrap_or_else(|| panic!("missing {key}"));
    let colon = text[key_start + needle.len()..]
        .find(':')
        .map(|offset| key_start + needle.len() + offset + 1)
        .expect("JSON colon");
    let quote = text[colon..]
        .find('"')
        .map(|offset| colon + offset + 1)
        .expect("JSON quote");
    let end = text[quote..]
        .find('"')
        .map(|offset| quote + offset)
        .expect("JSON quote");
    &text[quote..end]
}

#[test]
fn every_parameterization_projection_round_trips() {
    check_vectors("parameterization", PARAMETERIZATION_DOMAIN, 4);
}

#[test]
fn every_lattice_projection_round_trips() {
    check_vectors("lattice", LATTICE_DOMAIN, 2);
}

fn check_vectors(kind: &str, domain: &str, minimum: usize) {
    let directory = projections_dir(kind);
    let mut vectors: Vec<PathBuf> = fs::read_dir(&directory)
        .unwrap_or_else(|error| panic!("read {}: {error}", directory.display()))
        .map(|entry| entry.expect("directory entry").path())
        .filter(|path| path.extension().is_some_and(|extension| extension == "bin"))
        .collect();
    vectors.sort();
    assert!(
        vectors.len() >= minimum,
        "{kind} has {} vectors, expected at least {minimum}",
        vectors.len()
    );

    for vector in vectors {
        let bytes = fs::read(&vector).expect("read the projection vector");
        let decoded = parse(&bytes);
        let mut re_encoded = Vec::with_capacity(bytes.len());
        encode(&decoded, &mut re_encoded);
        assert_eq!(
            re_encoded,
            bytes,
            "{} does not round-trip byte-identically",
            vector.display()
        );

        let sidecar = vector.with_extension("json");
        let metadata = fs::read_to_string(&sidecar).expect("read the projection sidecar");
        assert_eq!(
            json_field(&metadata, "projectionSha256"),
            hex(&Sha256::digest(&bytes)),
            "{} projection hash",
            vector.display()
        );
        assert_eq!(
            json_field(&metadata, "signatureHex"),
            domain_signature(domain, &bytes),
            "{} domain signature",
            vector.display()
        );
        let expected_descriptor = if kind == "parameterization" {
            "parameterization-v1"
        } else {
            "lattice-v1"
        };
        assert_eq!(
            json_field(&metadata, "descriptor"),
            expected_descriptor,
            "{} descriptor",
            vector.display()
        );
    }
}
