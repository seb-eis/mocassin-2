// SPDX-License-Identifier: MIT

//! Canonical projection round-trip tests over the .NET-generated `.bin` vectors.
//!
//! Each vector is decoded into the runtime types, re-encoded with the production `mocassin_msl::projection`
//! encoder, and compared byte-for-byte. The sidecar digest and the `signature-vectors.json` entry are checked
//! against the production domain signature, so the Rust consumer reads the same metadata as the .NET tests.

use std::fs;
use std::path::{Path, PathBuf};

use mocassin_msl::{
    lattice_projection, lattice_signature, parameterization_projection, parameterization_signature,
};
use mocassin_types::{
    ClusterTable, EnergyModel, InitialLattice, JumpCollectionId, JumpRule, ModelCounts, PairTable,
    ParameterizedModel, ParticleId, ParticleTable, RuleId, StateCode, StructureModel,
    TransitionModel,
};
use sha2::{Digest, Sha256};

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

fn array(value: &Value) -> &[Value] {
    match value {
        Value::Array(values) => values,
        other => panic!("expected an array, found {other:?}"),
    }
}

fn string(value: &Value) -> &str {
    match value {
        Value::String(text) => text,
        other => panic!("expected a string, found {other:?}"),
    }
}

fn binary(value: &Value) -> &[u8] {
    match value {
        Value::Binary(bytes) => bytes,
        other => panic!("expected binary, found {other:?}"),
    }
}

fn u64_value(value: &Value) -> u64 {
    match value {
        Value::U64(value) => *value,
        other => panic!("expected a uint64, found {other:?}"),
    }
}

fn doubles(value: &Value) -> Vec<f64> {
    array(value)
        .iter()
        .map(|item| match item {
            Value::F64(value) => *value,
            other => panic!("expected a float, found {other:?}"),
        })
        .collect()
}

fn nested_doubles(value: &Value) -> Vec<Vec<f64>> {
    array(value).iter().map(doubles).collect()
}

fn parameterized_model(value: &Value) -> (ParameterizedModel, String) {
    let fields = array(value);
    assert_eq!(fields.len(), 6, "parameterization projection shape");
    assert_eq!(u64_value(&fields[0]), 1, "projection version");
    let model_signature = string(&fields[1]).to_owned();
    let pair_tables = nested_doubles(&fields[2])
        .into_iter()
        .map(|energies| PairTable {
            object_id: String::new(),
            energies,
        })
        .collect();
    let cluster_tables = nested_doubles(&fields[3])
        .into_iter()
        .map(|energies| ClusterTable {
            object_id: String::new(),
            occupation_codes: Vec::new(),
            energies,
            particle_table_mapping: Vec::new(),
            expansions: None,
        })
        .collect();
    let defect_background = doubles(&fields[4]);
    let rules = doubles(&fields[5])
        .into_iter()
        .enumerate()
        .map(|(index, frequency_hz)| JumpRule {
            rule_id: RuleId::new(index as u32),
            collection_id: JumpCollectionId::new(0),
            state0: StateCode::new(0),
            state1: StateCode::new(0),
            state2: StateCode::new(0),
            frequency_hz,
            charge_transport_vector: [0.0; 3],
            static_correction: None,
            tracker_order: Vec::new(),
        })
        .collect();

    let model = ParameterizedModel {
        format_version: 1,
        signature: String::new(),
        model_signature: model_signature.clone(),
        symmetry_contract_version: String::new(),
        space_group_data_version: String::new(),
        space_group_data_sha256: String::new(),
        particle: ParticleTable {
            charges: Vec::new(),
            symbols: Vec::new(),
            is_vacancy: Vec::new(),
        },
        structure: StructureModel {
            cell_vectors_ang: [[0.0; 3]; 3],
            positions: Vec::new(),
            site_count: 0,
            environments: Vec::new(),
            interaction_range: [0; 4],
            static_trackers_per_cell: 0,
            global_tracker_count: 0,
        },
        energy: EnergyModel {
            pair_tables,
            cluster_tables,
            defect_background,
        },
        transitions: TransitionModel {
            collections: Vec::new(),
            directions: Vec::new(),
            rules,
            jump_count: Vec::new(),
            jump_offsets: Vec::new(),
            jump_mapping: Vec::new(),
            static_tracker_mapping: Vec::new(),
            global_tracker_mapping: Vec::new(),
        },
        simulations: Vec::new(),
        counts: ModelCounts {
            particle_count: 0,
            position_count: 0,
            site_count: 0,
            environment_count: 0,
            pair_table_count: 0,
            cluster_table_count: 0,
            collection_count: 0,
            direction_count: 0,
            rule_count: 0,
            simulation_count: 0,
            global_tracker_count: 0,
            static_trackers_per_cell: 0,
        },
    };
    (model, model_signature)
}

fn initial_lattice(value: &Value) -> (InitialLattice, String) {
    let fields = array(value);
    assert_eq!(fields.len(), 5, "lattice projection shape");
    assert_eq!(u64_value(&fields[0]), 1, "projection version");
    let model_signature = string(&fields[1]).to_owned();
    let size = array(&fields[2]);
    assert_eq!(size.len(), 3, "lattice size shape");
    let lattice = InitialLattice {
        signature: String::new(),
        lattice_size: [
            u32::try_from(u64_value(&size[0])).expect("lattice size"),
            u32::try_from(u64_value(&size[1])).expect("lattice size"),
            u32::try_from(u64_value(&size[2])).expect("lattice size"),
        ],
        position_count: u32::try_from(u64_value(&fields[3])).expect("position count"),
        particles: binary(&fields[4])
            .iter()
            .map(|value| ParticleId::new(*value))
            .collect(),
    };
    (lattice, model_signature)
}

fn hex(bytes: &[u8]) -> String {
    use core::fmt::Write as _;

    let mut text = String::with_capacity(bytes.len() * 2);
    for byte in bytes {
        let _ = write!(text, "{byte:02x}");
    }
    text
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

/// Returns the `signature-vectors.json` entry with `id`, as a JSON text slice.
fn signature_vector_entry<'a>(document: &'a str, id: &str) -> &'a str {
    let marker = format!("\"id\": \"{id}\"");
    let start = document
        .find(&marker)
        .unwrap_or_else(|| panic!("signature-vectors.json has no {id}"));
    let rest = &document[start + marker.len()..];
    let end = rest
        .find("\"id\": \"")
        .map(|offset| start + marker.len() + offset)
        .unwrap_or(document.len());
    &document[start..end]
}

#[test]
fn every_parameterization_projection_round_trips() {
    check_vectors("parameterization", 4);
}

#[test]
fn every_lattice_projection_round_trips() {
    check_vectors("lattice", 2);
}

fn check_vectors(kind: &str, minimum: usize) {
    let directory = projections_dir(kind);
    let signature_vectors = fs::read_to_string(
        Path::new(env!("CARGO_MANIFEST_DIR"))
            .join("../../../../tests/contracts/signature-vectors.json"),
    )
    .expect("read the signature-vectors document");
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
        let sidecar = vector.with_extension("json");
        let metadata = fs::read_to_string(&sidecar).expect("read the projection sidecar");
        let id = json_field(&metadata, "id");
        let signature_entry = signature_vector_entry(signature_vectors.as_str(), id);

        let (re_encoded, signature, model_signature) = if kind == "parameterization" {
            let (model, model_signature) = parameterized_model(&decoded);
            let projection =
                parameterization_projection(&model).expect("the production projection encodes");
            let signature =
                parameterization_signature(&model).expect("the production signature encodes");
            (projection, signature, model_signature)
        } else {
            let (lattice, model_signature) = initial_lattice(&decoded);
            let projection = lattice_projection(&model_signature, &lattice);
            let signature = lattice_signature(&model_signature, &lattice);
            (projection, signature, model_signature)
        };

        assert_eq!(
            re_encoded,
            bytes,
            "{} does not round-trip through the production projection",
            vector.display()
        );
        assert_eq!(
            json_field(&metadata, "projectionSha256"),
            hex(&Sha256::digest(&bytes)),
            "{} projection hash",
            vector.display()
        );
        assert_eq!(
            json_field(&metadata, "signatureHex"),
            signature,
            "{} production domain signature",
            vector.display()
        );
        assert_eq!(
            json_field(&metadata, "modelSignature"),
            model_signature,
            "{} model signature",
            vector.display()
        );
        assert_eq!(
            json_field(signature_entry, "signatureHex"),
            json_field(&metadata, "signatureHex"),
            "{id} signature-vectors.json signature"
        );
        assert_eq!(
            json_field(signature_entry, "projectionSha256"),
            json_field(&metadata, "projectionSha256"),
            "{id} signature-vectors.json projection hash"
        );
        assert_eq!(
            json_field(signature_entry, "modelSignature"),
            json_field(&metadata, "modelSignature"),
            "{id} signature-vectors.json model signature"
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
