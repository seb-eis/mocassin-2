// SPDX-License-Identifier: MIT

//! Read-only `.msl` package access.
//!
//! [`Package::open_readonly`] opens the SQLite file with `SQLITE_OPEN_READONLY | SQLITE_OPEN_NO_MUTEX`, sets
//! `query_only`, `foreign_keys`, and a bounded busy timeout, and checks the package identity, schema, and
//! provenance keys before any payload is read. The package is never written and never gains `-wal`/`-shm`
//! sidecars. One connection is single-threaded; a future batch worker opens one package per worker.

use std::cell::RefCell;
use std::collections::HashMap;
use std::fmt;
use std::fs;
use std::io;
use std::path::Path;
use std::sync::Arc;
use std::time::Duration;

use mocassin_types::ReaderLimits;
use mocassin_types::payload::{JobPayload, LatticePayload, ModelPayload};
use rusqlite::{Connection, OpenFlags, OptionalExtension};

use crate::decode;
use crate::error::{PackageContext, PackageError};

/// The SQLite `application_id` of a `.msl` package (`"MOCS"`).
pub const APPLICATION_ID: u32 = 0x4D4F_4353;

/// The only supported package and payload encoding version in v1.
const SUPPORTED_VERSION: u32 = 1;

/// Safety cap for `job_ids()`, well above the writer's 99,999-job limit.
const MAX_JOB_IDS: usize = 1_000_000;

/// Safety cap for the `schema_info` row count.
const MAX_SCHEMA_INFO_ROWS: usize = 64;

/// Safety cap for one table's column count.
const MAX_TABLE_COLUMNS: usize = 512;

/// The required `schema_info` keys (Step 13 design §3.1).
const REQUIRED_SCHEMA_INFO_KEYS: [&str; 9] = [
    "format_version",
    "app_version",
    "created_utc",
    "project_id",
    "generator",
    "job_selection",
    "symmetry_contract_version",
    "space_group_data_version",
    "space_group_data_sha256",
];

const MODEL_COLUMNS: &[&str] = &["model_id", "signature", "format_version", "payload"];
const LATTICE_COLUMNS: &[&str] = &["lattice_id", "signature", "format_version", "payload"];

const JOB_COLUMNS: &[&str] = &[
    "job_id",
    "model_id",
    "lattice_id",
    "kind",
    "collection_name",
    "config_name",
    "collection_index",
    "config_index",
    "job_index",
    "temperature_k",
    "electric_field_vm",
    "target_mcsp",
    "write_out_count",
    "pre_run_mcsp",
    "time_limit_s",
    "min_success_rate",
    "lattice_a",
    "lattice_b",
    "lattice_c",
    "doping_info",
    "flags",
    "seed",
    "payload",
    "created_utc",
];

const JOB_ROW_SQL: &str = "
    SELECT j.job_id, j.model_id, j.lattice_id, m.signature, l.signature,
           j.kind, j.collection_name, j.config_name, j.collection_index, j.config_index, j.job_index,
           j.temperature_k, j.electric_field_vm, j.target_mcsp, j.write_out_count, j.pre_run_mcsp,
           j.time_limit_s, j.min_success_rate, j.lattice_a, j.lattice_b, j.lattice_c,
           j.doping_info, j.flags, j.seed, j.payload, j.created_utc
    FROM jobs j
    JOIN models m ON m.model_id = j.model_id
    JOIN lattices l ON l.lattice_id = j.lattice_id
    WHERE j.job_id = ?1";

/// The nine `schema_info` rows, parsed.
///
/// `app_version`, `created_utc`, and `generator` are build metadata: they are read but must never appear in a
/// deterministic summary or golden.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct SchemaInfo {
    /// The package format version; always 1 for a supported package.
    pub format_version: u32,
    /// The builder version that produced the package (build metadata).
    pub app_version: String,
    /// The package creation timestamp (build metadata).
    pub created_utc: String,
    /// The project identity.
    pub project_id: String,
    /// The generator description (build metadata).
    pub generator: String,
    /// The authored job selection description.
    pub job_selection: String,
    /// The symmetry contract version.
    pub symmetry_contract_version: String,
    /// The space-group catalog data version.
    pub space_group_data_version: String,
    /// The SHA-256 of the canonical space-group catalog payload.
    pub space_group_data_sha256: String,
}

/// An open, validated, read-only `.msl` package.
///
/// The package holds a [`rusqlite::Connection`] and a single-threaded model cache; it is not `Sync`. Production
/// runs one job per process, so the cache is a test/short-batch convenience.
pub struct Package {
    connection: Connection,
    limits: ReaderLimits,
    schema: SchemaInfo,
    model_cache: RefCell<HashMap<(u32, String), Arc<ModelPayload>>>,
}

/// One `jobs` row with the joined model/lattice signatures.
///
/// The decoded payloads live in [`LoadedJob`]; this row is the stored column view that the later mapping stage
/// cross-checks.
#[derive(Debug, Clone, PartialEq)]
pub struct JobRow {
    /// The job id.
    pub job_id: u32,
    /// The SQLite model row id.
    pub model_id: u32,
    /// The SQLite lattice row id.
    pub lattice_id: u32,
    /// The `models.signature` of the referenced model.
    pub model_signature: String,
    /// The `lattices.signature` of the referenced lattice.
    pub lattice_signature: String,
    /// The row kind (`"kmc"` or `"mmc"`).
    pub kind: String,
    /// The authored collection name.
    pub collection_name: String,
    /// The authored configuration name.
    pub config_name: String,
    /// The zero-based collection index.
    pub collection_index: u32,
    /// The zero-based configuration index.
    pub config_index: u32,
    /// The zero-based duplicate index.
    pub job_index: u32,
    /// The row temperature in kelvin.
    pub temperature_k: f64,
    /// The row electric-field modulus in V/m.
    pub electric_field_vm: f64,
    /// The row target MCSP.
    pub target_mcsp: u64,
    /// The row write-out count.
    pub write_out_count: u32,
    /// The row KMC pre-run MCSP.
    pub pre_run_mcsp: u64,
    /// The row wall-clock limit, or `None`.
    pub time_limit_s: Option<u64>,
    /// The row minimum success rate, or `None`.
    pub min_success_rate: Option<f64>,
    /// The row supercell size `[a, b, c]`.
    pub lattice_size: [u32; 3],
    /// The authored doping summary.
    pub doping_info: String,
    /// The composed solver flag word.
    pub flags: u32,
    /// The 32-byte effective seed.
    pub seed: [u8; 32],
    /// The row creation timestamp (build metadata).
    pub created_utc: String,
}

/// One decoded job plus the model and lattice it references.
///
/// The later mapping stage turns this into a validated `JobPackage`; this type guarantees only that the
/// payloads were bounded, decoded, and version-checked.
#[derive(Debug)]
pub struct LoadedJob {
    /// The stored job row.
    pub row: JobRow,
    /// The decoded job payload.
    pub job: JobPayload,
    /// The decoded model payload, shared through the package cache.
    pub model: Arc<ModelPayload>,
    /// The decoded initial lattice.
    pub lattice: LatticePayload,
}

impl fmt::Debug for Package {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter
            .debug_struct("Package")
            .field("schema", &self.schema)
            .field("limits", &self.limits)
            .finish_non_exhaustive()
    }
}

impl Package {
    /// Opens a `.msl` package read-only and validates its identity, schema, and `schema_info`.
    ///
    /// # Errors
    ///
    /// Returns [`PackageError::Io`] for a missing, unreadable, or non-file path, and a schema/package error for
    /// an unusable or unsupported database.
    pub fn open_readonly(
        path: impl AsRef<Path>,
        limits: ReaderLimits,
    ) -> Result<Self, PackageError> {
        let path = path.as_ref();

        let metadata = fs::metadata(path)?;
        if !metadata.is_file() {
            return Err(PackageError::Io(io::Error::new(
                io::ErrorKind::InvalidInput,
                format!("{} is not a regular file", path.display()),
            )));
        }

        // Probe readability before SQLite turns it into a generic open error.
        let _ = fs::File::open(path)?;

        let connection = Connection::open_with_flags(
            path,
            OpenFlags::SQLITE_OPEN_READ_ONLY | OpenFlags::SQLITE_OPEN_NO_MUTEX,
        )?;

        connection.busy_timeout(Duration::from_secs(2))?;
        connection.execute_batch("PRAGMA query_only = ON; PRAGMA foreign_keys = ON;")?;

        let application_id =
            connection.pragma_query_value(None, "application_id", |row| row.get::<_, i64>(0))?;
        if application_id != i64::from(APPLICATION_ID) {
            return Err(PackageError::InvalidSchemaInfo(format!(
                "application_id is 0x{application_id:08X}; expected 0x{APPLICATION_ID:08X}"
            )));
        }

        let user_version =
            connection.pragma_query_value(None, "user_version", |row| row.get::<_, i64>(0))?;
        require_version(user_version)?;

        let schema = read_schema_info(&connection)?;
        require_schema(&connection)?;

        Ok(Self {
            connection,
            limits,
            schema,
            model_cache: RefCell::new(HashMap::new()),
        })
    }

    /// Returns the parsed `schema_info` rows.
    #[must_use]
    pub const fn schema(&self) -> &SchemaInfo {
        &self.schema
    }

    /// Returns the limits this package was opened with.
    #[must_use]
    pub const fn limits(&self) -> &ReaderLimits {
        &self.limits
    }

    /// Returns the ascending job ids present in the package.
    ///
    /// # Errors
    ///
    /// Rejects a row whose id does not fit an unsigned 32-bit value and a package with more than the internal
    /// one-million-row safety cap.
    pub fn job_ids(&self) -> Result<Vec<u32>, PackageError> {
        let mut statement = self
            .connection
            .prepare("SELECT job_id FROM jobs ORDER BY job_id")?;
        let rows = statement.query_map([], |row| row.get::<_, i64>(0))?;

        let mut ids = Vec::new();
        for row in rows {
            if ids.len() >= MAX_JOB_IDS {
                return Err(PackageError::InvalidValue {
                    what: "jobs",
                    detail: format!("more than {MAX_JOB_IDS} rows"),
                });
            }
            ids.push(require_u32(row?, "jobs.job_id")?);
        }
        Ok(ids)
    }

    /// Loads and decodes one model, using the package cache.
    ///
    /// # Errors
    ///
    /// Returns a structured error when the model row is absent, the payload is corrupt, or the payload version
    /// is unsupported.
    pub fn load_model(&self, model_id: u32) -> Result<Arc<ModelPayload>, PackageContext> {
        self.load_model_at(model_id, None)
    }

    /// Loads one job with its model and lattice.
    ///
    /// Full payload-to-runtime mapping, index validation, and signature verification belong to the next step;
    /// this method guarantees the bounded decode and the stored format-version cross-checks.
    ///
    /// # Errors
    ///
    /// Returns [`PackageError::JobNotFound`] for an unknown id and a structured package/model error for a
    /// corrupt payload.
    pub fn load_job(&self, job_id: u32) -> Result<LoadedJob, PackageContext> {
        let job_context = |error: PackageError| PackageContext::new(Some(job_id), -1, "job", error);

        let raw = self.read_job_row(job_id).map_err(job_context)?;
        let (row, compressed_job) = JobRow::from_raw(raw).map_err(job_context)?;

        let model = self.load_model_at(row.model_id, Some(job_id))?;
        let lattice = self.load_lattice(&row, Some(job_id))?;

        let context = |error: PackageError| {
            PackageContext::new(Some(job_id), i64::from(row.model_id), "job", error)
        };
        let payload =
            decode::decompress_payload("job", &compressed_job, self.limits.max_job_payload_bytes)
                .map_err(context)?;
        let job = decode::decode_job_payload(&payload, &self.limits).map_err(context)?;

        Ok(LoadedJob {
            row,
            job,
            model,
            lattice,
        })
    }

    fn load_model_at(
        &self,
        model_id: u32,
        job_id: Option<u32>,
    ) -> Result<Arc<ModelPayload>, PackageContext> {
        let context =
            |error: PackageError| PackageContext::new(job_id, i64::from(model_id), "model", error);

        let (signature, format_version) = self.read_model_meta(model_id).map_err(context)?;
        let key = (model_id, signature);

        if let Some(cached) = self.model_cache.borrow().get(&key) {
            return Ok(Arc::clone(cached));
        }

        let compressed = self.read_model_payload(model_id).map_err(context)?;
        let payload =
            decode::decompress_payload("model", &compressed, self.limits.max_model_payload_bytes)
                .map_err(context)?;
        let model = decode::decode_model_payload(&payload, &self.limits).map_err(context)?;

        check_format_version(
            "models.format_version",
            format_version,
            model.format_version,
        )
        .map_err(context)?;

        let model = Arc::new(model);
        self.model_cache
            .borrow_mut()
            .insert(key, Arc::clone(&model));
        Ok(model)
    }

    fn load_lattice(
        &self,
        row: &JobRow,
        job_id: Option<u32>,
    ) -> Result<LatticePayload, PackageContext> {
        let context = |error: PackageError| {
            PackageContext::new(job_id, i64::from(row.model_id), "lattice", error)
        };

        let (format_version, compressed) =
            self.read_lattice_row(row.lattice_id).map_err(context)?;
        let payload = decode::decompress_payload(
            "lattice",
            &compressed,
            self.limits.max_lattice_payload_bytes,
        )
        .map_err(context)?;
        let lattice = decode::decode_lattice_payload(&payload, &self.limits).map_err(context)?;

        check_format_version(
            "lattices.format_version",
            format_version,
            lattice.format_version,
        )
        .map_err(context)?;

        Ok(lattice)
    }

    fn read_model_meta(&self, model_id: u32) -> Result<(String, i64), PackageError> {
        self.connection
            .query_row(
                "SELECT signature, format_version FROM models WHERE model_id = ?1",
                [i64::from(model_id)],
                |row| Ok((row.get::<_, String>(0)?, row.get::<_, i64>(1)?)),
            )
            .optional()?
            .ok_or_else(|| PackageError::InvalidValue {
                what: "model_id",
                detail: format!("the models row {model_id} is absent"),
            })
    }

    fn read_model_payload(&self, model_id: u32) -> Result<Vec<u8>, PackageError> {
        self.connection
            .query_row(
                "SELECT payload FROM models WHERE model_id = ?1",
                [i64::from(model_id)],
                |row| row.get::<_, Vec<u8>>(0),
            )
            .optional()?
            .ok_or_else(|| PackageError::InvalidValue {
                what: "model_id",
                detail: format!("the models row {model_id} is absent"),
            })
    }

    fn read_lattice_row(&self, lattice_id: u32) -> Result<(i64, Vec<u8>), PackageError> {
        self.connection
            .query_row(
                "SELECT format_version, payload FROM lattices WHERE lattice_id = ?1",
                [i64::from(lattice_id)],
                |row| Ok((row.get::<_, i64>(0)?, row.get::<_, Vec<u8>>(1)?)),
            )
            .optional()?
            .ok_or_else(|| PackageError::InvalidValue {
                what: "lattice_id",
                detail: format!("the lattices row {lattice_id} is absent"),
            })
    }

    fn read_job_row(&self, job_id: u32) -> Result<RawJobRow, PackageError> {
        self.connection
            .query_row(JOB_ROW_SQL, [i64::from(job_id)], |row| {
                Ok(RawJobRow {
                    job_id: row.get(0)?,
                    model_id: row.get(1)?,
                    lattice_id: row.get(2)?,
                    model_signature: row.get(3)?,
                    lattice_signature: row.get(4)?,
                    kind: row.get(5)?,
                    collection_name: row.get(6)?,
                    config_name: row.get(7)?,
                    collection_index: row.get(8)?,
                    config_index: row.get(9)?,
                    job_index: row.get(10)?,
                    temperature_k: row.get(11)?,
                    electric_field_vm: row.get(12)?,
                    target_mcsp: row.get(13)?,
                    write_out_count: row.get(14)?,
                    pre_run_mcsp: row.get(15)?,
                    time_limit_s: row.get(16)?,
                    min_success_rate: row.get(17)?,
                    lattice_a: row.get(18)?,
                    lattice_b: row.get(19)?,
                    lattice_c: row.get(20)?,
                    doping_info: row.get(21)?,
                    flags: row.get(22)?,
                    seed: row.get(23)?,
                    payload: row.get(24)?,
                    created_utc: row.get(25)?,
                })
            })
            .optional()?
            .ok_or(PackageError::JobNotFound { job_id })
    }
}

impl JobRow {
    fn from_raw(raw: RawJobRow) -> Result<(Self, Vec<u8>), PackageError> {
        let RawJobRow {
            job_id,
            model_id,
            lattice_id,
            model_signature,
            lattice_signature,
            kind,
            collection_name,
            config_name,
            collection_index,
            config_index,
            job_index,
            temperature_k,
            electric_field_vm,
            target_mcsp,
            write_out_count,
            pre_run_mcsp,
            time_limit_s,
            min_success_rate,
            lattice_a,
            lattice_b,
            lattice_c,
            doping_info,
            flags,
            seed,
            payload,
            created_utc,
        } = raw;

        let seed: [u8; 32] =
            seed.try_into()
                .map_err(|seed: Vec<u8>| PackageError::LengthMismatch {
                    what: "jobs.seed",
                    expected: 32,
                    found: seed.len() as u64,
                })?;

        let row = Self {
            job_id: require_u32(job_id, "jobs.job_id")?,
            model_id: require_u32(model_id, "jobs.model_id")?,
            lattice_id: require_u32(lattice_id, "jobs.lattice_id")?,
            model_signature,
            lattice_signature,
            kind,
            collection_name,
            config_name,
            collection_index: require_u32(collection_index, "jobs.collection_index")?,
            config_index: require_u32(config_index, "jobs.config_index")?,
            job_index: require_u32(job_index, "jobs.job_index")?,
            temperature_k,
            electric_field_vm,
            target_mcsp: require_u64(target_mcsp, "jobs.target_mcsp")?,
            write_out_count: require_u32(write_out_count, "jobs.write_out_count")?,
            pre_run_mcsp: require_u64(pre_run_mcsp, "jobs.pre_run_mcsp")?,
            time_limit_s: time_limit_s
                .map(|value| require_u64(value, "jobs.time_limit_s"))
                .transpose()?,
            min_success_rate,
            lattice_size: [
                require_u32(lattice_a, "jobs.lattice_a")?,
                require_u32(lattice_b, "jobs.lattice_b")?,
                require_u32(lattice_c, "jobs.lattice_c")?,
            ],
            doping_info,
            flags: require_u32(flags, "jobs.flags")?,
            seed,
            created_utc,
        };

        Ok((row, payload))
    }
}

/// The un-converted form of one SQLite job row.
struct RawJobRow {
    job_id: i64,
    model_id: i64,
    lattice_id: i64,
    model_signature: String,
    lattice_signature: String,
    kind: String,
    collection_name: String,
    config_name: String,
    collection_index: i64,
    config_index: i64,
    job_index: i64,
    temperature_k: f64,
    electric_field_vm: f64,
    target_mcsp: i64,
    write_out_count: i64,
    pre_run_mcsp: i64,
    time_limit_s: Option<i64>,
    min_success_rate: Option<f64>,
    lattice_a: i64,
    lattice_b: i64,
    lattice_c: i64,
    doping_info: String,
    flags: i64,
    seed: Vec<u8>,
    payload: Vec<u8>,
    created_utc: String,
}

fn require_u32(value: i64, what: &'static str) -> Result<u32, PackageError> {
    u32::try_from(value).map_err(|_| PackageError::InvalidValue {
        what,
        detail: format!("{value} does not fit an unsigned 32-bit integer"),
    })
}

fn require_u64(value: i64, what: &'static str) -> Result<u64, PackageError> {
    u64::try_from(value).map_err(|_| PackageError::InvalidValue {
        what,
        detail: format!("{value} does not fit an unsigned 64-bit integer"),
    })
}

fn require_version(value: i64) -> Result<(), PackageError> {
    let found = u32::try_from(value.max(0)).unwrap_or(u32::MAX);
    if found == SUPPORTED_VERSION {
        return Ok(());
    }

    Err(PackageError::SchemaVersion {
        found,
        supported: SUPPORTED_VERSION..=SUPPORTED_VERSION,
    })
}

fn check_format_version(
    column: &'static str,
    column_value: i64,
    payload_value: u32,
) -> Result<(), PackageError> {
    if column_value == i64::from(payload_value) {
        return Ok(());
    }

    Err(PackageError::ColumnMismatch {
        column,
        column_value: column_value.to_string(),
        payload_value: payload_value.to_string(),
    })
}

fn read_schema_info(connection: &Connection) -> Result<SchemaInfo, PackageError> {
    require_table(connection, "schema_info")?;

    let mut statement = connection.prepare("SELECT key, value FROM schema_info")?;
    let rows = statement.query_map([], |row| {
        Ok((row.get::<_, String>(0)?, row.get::<_, String>(1)?))
    })?;

    let mut values: HashMap<String, String> = HashMap::new();
    let mut count = 0_usize;
    for row in rows {
        count += 1;
        if count > MAX_SCHEMA_INFO_ROWS {
            return Err(PackageError::InvalidSchemaInfo(format!(
                "schema_info has more than {MAX_SCHEMA_INFO_ROWS} rows"
            )));
        }
        let (key, value) = row?;
        if values.insert(key.clone(), value).is_some() {
            return Err(PackageError::InvalidSchemaInfo(format!(
                "schema_info contains the key \"{key}\" twice"
            )));
        }
    }

    for key in REQUIRED_SCHEMA_INFO_KEYS {
        if !values.contains_key(key) {
            return Err(PackageError::MissingSchemaElement {
                what: "schema_info key",
                name: key,
            });
        }
    }

    let format_text = values
        .get("format_version")
        .expect("the required key was checked");
    let format_version = format_text.parse::<u32>().map_err(|_| {
        PackageError::InvalidSchemaInfo(format!("schema_info.format_version is \"{format_text}\""))
    })?;
    if format_version != SUPPORTED_VERSION {
        return Err(PackageError::SchemaVersion {
            found: format_version,
            supported: SUPPORTED_VERSION..=SUPPORTED_VERSION,
        });
    }

    Ok(SchemaInfo {
        format_version,
        app_version: take(&values, "app_version"),
        created_utc: take(&values, "created_utc"),
        project_id: take(&values, "project_id"),
        generator: take(&values, "generator"),
        job_selection: take(&values, "job_selection"),
        symmetry_contract_version: take(&values, "symmetry_contract_version"),
        space_group_data_version: take(&values, "space_group_data_version"),
        space_group_data_sha256: take(&values, "space_group_data_sha256"),
    })
}

fn take(values: &HashMap<String, String>, key: &str) -> String {
    values.get(key).cloned().unwrap_or_default()
}

fn require_schema(connection: &Connection) -> Result<(), PackageError> {
    for (table, columns) in [
        ("models", MODEL_COLUMNS),
        ("lattices", LATTICE_COLUMNS),
        ("jobs", JOB_COLUMNS),
    ] {
        require_table(connection, table)?;
        require_columns(connection, table, columns)?;
    }
    Ok(())
}

fn require_table(connection: &Connection, table: &'static str) -> Result<(), PackageError> {
    let found = connection
        .query_row(
            "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = ?1",
            [table],
            |row| row.get::<_, i64>(0),
        )
        .optional()?;

    if found.is_none() {
        return Err(PackageError::MissingSchemaElement {
            what: "table",
            name: table,
        });
    }
    Ok(())
}

fn require_columns(
    connection: &Connection,
    table: &'static str,
    required: &'static [&'static str],
) -> Result<(), PackageError> {
    let mut statement = connection.prepare(&format!("PRAGMA table_info({table})"))?;
    let rows = statement.query_map([], |row| row.get::<_, String>(1))?;

    let mut present: Vec<String> = Vec::new();
    for row in rows {
        if present.len() >= MAX_TABLE_COLUMNS {
            return Err(PackageError::InvalidSchemaInfo(format!(
                "table {table} has more than {MAX_TABLE_COLUMNS} columns"
            )));
        }
        present.push(row?);
    }

    for column in required {
        if !present.iter().any(|name| name == column) {
            return Err(PackageError::MissingSchemaElement {
                what: "column",
                name: column,
            });
        }
    }
    Ok(())
}
