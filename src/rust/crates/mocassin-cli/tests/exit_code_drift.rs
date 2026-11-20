// SPDX-License-Identifier: MIT

//! Documentation drift check for the canonical solver exit table.
//!
//! The table is mirrored in the architecture, the CLI reference, the solver architecture page, and the Step 13
//! page. This test extracts the codes and meanings from each mirror and compares them with
//! [`ExitCode`](mocassin_types::ExitCode), so the mirror cannot drift silently.

use std::collections::BTreeSet;
use std::fs;
use std::path::Path;

use mocassin_types::ExitCode;

fn workspace(relative: &str) -> String {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../")
        .join(relative);
    fs::read_to_string(&path).unwrap_or_else(|error| panic!("read {}: {error}", path.display()))
}

fn expected_codes() -> BTreeSet<u8> {
    ExitCode::ALL.iter().map(|code| code.code()).collect()
}

fn normalize(text: &str) -> String {
    text.split_whitespace()
        .collect::<Vec<_>>()
        .join(" ")
        .to_lowercase()
}

/// Collects the backticked numeric codes in a text fragment.
fn backticked_codes(text: &str) -> BTreeSet<u8> {
    let bytes = text.as_bytes();
    let mut codes = BTreeSet::new();
    let mut index = 0;
    while index < bytes.len() {
        if bytes[index] != b'`' {
            index += 1;
            continue;
        }

        let start = index + 1;
        let mut end = start;
        while end < bytes.len() && bytes[end].is_ascii_digit() {
            end += 1;
        }
        if end > start && end < bytes.len() && bytes[end] == b'`' {
            if let Ok(code) = text[start..end].parse::<u8>() {
                codes.insert(code);
            }
            index = end + 1;
        } else {
            index += 1;
        }
    }
    codes
}

/// Collects the numeric code column of a two-column table.
fn table_codes(section: &str, code_column: usize) -> BTreeSet<u8> {
    let mut codes = BTreeSet::new();
    for line in section.lines() {
        let line = line.trim();
        if !line.starts_with('|') || !line.ends_with('|') {
            continue;
        }

        let cells: Vec<&str> = line.trim_matches('|').split('|').map(str::trim).collect();
        if cells.len() == 2
            && let Ok(code) = cells[code_column].parse::<u8>()
        {
            codes.insert(code);
        }
    }
    codes
}

/// Collects the meaning column of a code-first two-column table.
fn table_meanings(section: &str) -> Vec<(u8, String)> {
    let mut rows = Vec::new();
    for line in section.lines() {
        let line = line.trim();
        if !line.starts_with('|') || !line.ends_with('|') {
            continue;
        }

        let cells: Vec<&str> = line.trim_matches('|').split('|').map(str::trim).collect();
        if cells.len() == 2
            && let Ok(code) = cells[0].parse::<u8>()
        {
            rows.push((code, cells[1].to_owned()));
        }
    }
    rows
}

#[test]
fn the_architecture_solver_table_matches_the_enum() {
    let document = workspace("docs/planning/01-target-architecture.md");
    let start = document
        .find("- Rust solver:")
        .expect("the architecture §7.3 solver bullet is present");
    let bullet = &document[start..];
    let end = bullet[2..]
        .find("\n- ")
        .map_or(bullet.len(), |offset| offset + 2);
    let bullet = &bullet[..end];

    assert_eq!(
        backticked_codes(bullet),
        expected_codes(),
        "architecture §7.3 codes"
    );

    let normalized = normalize(bullet);
    for phrase in [
        "0` completed",
        "5` partial success",
        "10` invalid arguments",
        "11` package error",
        "12` model/data error",
        "13` numerical failure",
        "internal invariant violation",
        "14` aborted by",
        "15` cancelled",
        "16` i/o error",
        "parse errors",
        "help and `--version` exit 0",
    ] {
        assert!(
            normalized.contains(phrase),
            "architecture §7.3 is missing {phrase:?}"
        );
    }
}

#[test]
fn the_cli_reference_table_matches_the_enum() {
    let document = workspace("docs/reference/cli.md");
    let start = document
        .find("### Solver exit codes")
        .expect("the CLI reference has a solver exit-code section");
    let section = &document[start..];
    let section = &section[..section.find("\n## ").unwrap_or(section.len())];

    let codes = table_codes(section, 0);
    assert_eq!(codes, expected_codes(), "cli.md exit codes");
    let rows = table_meanings(section);

    for (code, meaning) in rows {
        let canonical = ExitCode::try_from(code)
            .expect("every cli.md row is a canonical code")
            .meaning()
            .to_lowercase();
        assert!(
            meaning.to_lowercase().starts_with(&canonical),
            "cli.md row {code} ({meaning:?}) does not start with the canonical meaning {canonical:?}"
        );
    }
}

#[test]
fn the_solver_architecture_exit_paragraph_matches_the_enum() {
    let document = workspace("docs/development/solver-architecture.md");
    let start = document
        .find("Exit codes (canonical definition:")
        .expect("the solver architecture page mirrors the exit table");
    let paragraph = &document[start..];
    let paragraph = &paragraph[..paragraph.find("\n\n").unwrap_or(paragraph.len())];
    let normalized = normalize(paragraph);

    let expected = [
        (0, "0 success"),
        (5, "5 partial success"),
        (10, "10 invalid arguments"),
        (11, "11 package error"),
        (12, "12 model/data error"),
        (13, "13 numerical failure or internal invariant violation"),
        (14, "14 aborted by a configured limit"),
        (15, "15 cancelled"),
        (16, "16 i/o"),
    ];
    for (code, phrase) in expected {
        assert!(
            ExitCode::try_from(code).is_ok(),
            "{code} is a canonical exit code"
        );
        assert!(
            normalized.contains(phrase),
            "solver-architecture.md is missing {phrase:?}"
        );
    }
}

#[test]
fn the_step_page_exit_table_matches_the_enum() {
    let document =
        workspace("docs/planning/steps/step-13-rust-solver-foundation-and-package-reader.md");
    let start = document
        .find("Mapping in this reader/CLI:")
        .expect("the Step 13 page has the reader/CLI exit mapping");
    let section = &document[start..];
    let section = &section[..section.find("\n### ").unwrap_or(section.len())];

    let codes = table_codes(section, 1);
    assert_eq!(codes, expected_codes(), "Step 13 exit table codes");
}
