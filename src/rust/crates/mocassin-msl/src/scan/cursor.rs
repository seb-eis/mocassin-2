// SPDX-License-Identifier: MIT

//! A non-allocating MessagePack cursor used by the payload scan.
//!
//! The cursor validates every declared length against the remaining input, enforces the nesting-depth and
//! element budgets, and skips values without allocating. Only the schema walk records facts; the cursor itself
//! never builds a document tree.

use crate::error::PackageError;

/// A forward-only cursor over one decompressed MessagePack payload.
pub(crate) struct Cursor<'a> {
    data: &'a [u8],
    position: usize,
    elements: u64,
    max_elements: u64,
    depth: u32,
    max_depth: u32,
    section: &'static str,
}

impl<'a> Cursor<'a> {
    /// Creates a cursor for one payload section.
    pub(crate) const fn new(
        data: &'a [u8],
        max_elements: u64,
        max_depth: u32,
        section: &'static str,
    ) -> Self {
        Self {
            data,
            position: 0,
            elements: 0,
            max_elements,
            depth: 0,
            max_depth,
            section,
        }
    }

    /// Returns the number of unread bytes.
    pub(crate) fn remaining(&self) -> usize {
        self.data.len() - self.position
    }

    /// Fails when the whole payload was not consumed.
    pub(crate) fn finish(&self) -> Result<(), PackageError> {
        if self.position == self.data.len() {
            Ok(())
        } else {
            Err(self.scan_error(format!(
                "{} trailing bytes after the top-level map",
                self.remaining()
            )))
        }
    }

    /// Reads the next marker byte and charges one element against the budget.
    fn marker(&mut self) -> Result<u8, PackageError> {
        let marker = *self
            .data
            .get(self.position)
            .ok_or_else(|| self.scan_error("unexpected end of input"))?;
        self.position += 1;
        self.elements += 1;
        if self.elements > self.max_elements {
            return Err(PackageError::ElementBudgetExceeded {
                section: self.section,
                limit: self.max_elements,
            });
        }
        Ok(marker)
    }

    /// Reads `count` raw bytes.
    fn take(&mut self, count: usize) -> Result<&'a [u8], PackageError> {
        let end = self
            .position
            .checked_add(count)
            .ok_or_else(|| self.scan_error("declared length overflows the payload"))?;
        let bytes = self
            .data
            .get(self.position..end)
            .ok_or_else(|| self.scan_error("declared length exceeds the remaining input"))?;
        self.position = end;
        Ok(bytes)
    }

    /// Advances past `count` bytes.
    fn advance(&mut self, count: u64) -> Result<(), PackageError> {
        let count = usize::try_from(count)
            .map_err(|_| self.scan_error("declared length does not fit the platform"))?;
        let _ = self.take(count)?;
        Ok(())
    }

    /// Rejects a declared element count that cannot fit in the remaining bytes.
    fn check_declared(&self, declared: u64, minimum_bytes_each: u64) -> Result<(), PackageError> {
        let required = declared
            .checked_mul(minimum_bytes_each)
            .ok_or_else(|| self.scan_error("declared length overflows the payload"))?;
        if required > self.remaining() as u64 {
            return Err(self.scan_error("declared length exceeds the remaining input"));
        }
        Ok(())
    }

    /// Enters a container and enforces the nesting-depth limit.
    fn enter(&mut self) -> Result<(), PackageError> {
        if self.depth >= self.max_depth {
            return Err(self.scan_error(format!(
                "nesting depth exceeds the {} container level limit",
                self.max_depth
            )));
        }
        self.depth += 1;
        Ok(())
    }

    /// Leaves the current container.
    pub(crate) fn leave(&mut self) {
        debug_assert!(self.depth > 0, "container depth underflow");
        self.depth -= 1;
    }

    /// Reads an unsigned integer of any stored width.
    pub(crate) fn read_u64(&mut self) -> Result<u64, PackageError> {
        let marker = self.marker()?;
        match marker {
            0x00..=0x7f => Ok(u64::from(marker)),
            0xcc => Ok(u64::from(self.take(1)?[0])),
            0xcd => Ok(u64::from(u16::from_be_bytes(self.fixed::<2>()?))),
            0xce => Ok(u64::from(u32::from_be_bytes(self.fixed::<4>()?))),
            0xcf => Ok(u64::from_be_bytes(self.fixed::<8>()?)),
            0xd0..=0xd3 | 0xe0..=0xff => {
                Err(self
                    .scan_error("negative or signed integer where an unsigned value is required"))
            }
            _ => Err(self.scan_error("expected an unsigned integer")),
        }
    }

    /// Reads an unsigned 32-bit integer.
    pub(crate) fn read_u32(&mut self) -> Result<u32, PackageError> {
        u32::try_from(self.read_u64()?)
            .map_err(|_| self.scan_error("integer does not fit a 32-bit unsigned value"))
    }

    /// Reads a MessagePack string and validates it as UTF-8.
    pub(crate) fn read_str(&mut self) -> Result<&'a str, PackageError> {
        let marker = self.marker()?;
        let length = match marker {
            0xa0..=0xbf => u64::from(marker & 0x1f),
            0xd9 => u64::from(self.take(1)?[0]),
            0xda => u64::from(u16::from_be_bytes(self.fixed::<2>()?)),
            0xdb => u64::from(u32::from_be_bytes(self.fixed::<4>()?)),
            _ => return Err(self.scan_error("expected a string")),
        };
        self.check_declared(length, 1)?;
        let bytes = self.take(
            usize::try_from(length)
                .map_err(|_| self.scan_error("string length does not fit the platform"))?,
        )?;
        core::str::from_utf8(bytes).map_err(|_| self.scan_error("string is not valid UTF-8"))
    }

    /// Reads an array header and enters the array.
    pub(crate) fn read_array_len(&mut self) -> Result<u64, PackageError> {
        let marker = self.marker()?;
        let length = match marker {
            0x90..=0x9f => u64::from(marker & 0x0f),
            0xdc => u64::from(u16::from_be_bytes(self.fixed::<2>()?)),
            0xdd => u64::from(u32::from_be_bytes(self.fixed::<4>()?)),
            _ => return Err(self.scan_error("expected an array")),
        };
        self.enter()?;
        self.check_declared(length, 1)?;
        Ok(length)
    }

    /// Reads a map header and enters the map.
    pub(crate) fn read_map_len(&mut self) -> Result<u64, PackageError> {
        let marker = self.marker()?;
        let length = match marker {
            0x80..=0x8f => u64::from(marker & 0x0f),
            0xde => u64::from(u16::from_be_bytes(self.fixed::<2>()?)),
            0xdf => u64::from(u32::from_be_bytes(self.fixed::<4>()?)),
            _ => return Err(self.scan_error("expected a map")),
        };
        self.enter()?;
        self.check_declared(length, 2)?;
        Ok(length)
    }

    /// Consumes a nil marker and returns `true`, or leaves the cursor before a non-nil value and returns
    /// `false`.
    pub(crate) fn read_nil_marker(&mut self) -> Result<bool, PackageError> {
        let marker = *self
            .data
            .get(self.position)
            .ok_or_else(|| self.scan_error("unexpected end of input"))?;
        if marker != 0xc0 {
            return Ok(false);
        }

        let _ = self.marker()?;
        Ok(true)
    }

    /// Reads a particle array stored either as a MessagePack array of `u8` or as a binary value.
    ///
    /// Returns the declared particle count. Array elements are checked to fit a byte; binary contents are
    /// advanced without copying. The caller compares the count with the expected `a * b * c * position_count`.
    pub(crate) fn read_u8_sequence_len(&mut self) -> Result<u64, PackageError> {
        let marker = self.marker()?;
        match marker {
            0x90..=0x9f => self.read_u8_array(u64::from(marker & 0x0f)),
            0xdc => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                self.read_u8_array(length)
            }
            0xdd => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                self.read_u8_array(length)
            }
            0xc4 => {
                let length = u64::from(self.take(1)?[0]);
                self.advance(length)?;
                Ok(length)
            }
            0xc5 => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                self.advance(length)?;
                Ok(length)
            }
            0xc6 => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                self.advance(length)?;
                Ok(length)
            }
            _ => Err(self.scan_error("expected a particle array or binary value")),
        }
    }

    fn read_u8_array(&mut self, length: u64) -> Result<u64, PackageError> {
        self.enter()?;
        self.check_declared(length, 1)?;
        for _ in 0..length {
            if self.read_u64()? > u64::from(u8::MAX) {
                return Err(self.scan_error("particle index does not fit an unsigned byte"));
            }
        }
        self.leave();
        Ok(length)
    }

    /// Skips `count` complete values.
    pub(crate) fn skip_n(&mut self, count: u64) -> Result<(), PackageError> {
        for _ in 0..count {
            self.skip_value()?;
        }
        Ok(())
    }

    /// Skips one complete value without allocating.
    pub(crate) fn skip_value(&mut self) -> Result<(), PackageError> {
        let marker = self.marker()?;
        match marker {
            0x00..=0x7f | 0xe0..=0xff => Ok(()),
            0x80..=0x8f => {
                let length = u64::from(marker & 0x0f);
                self.enter()?;
                self.check_declared(length, 2)?;
                for _ in 0..length {
                    self.skip_value()?;
                    self.skip_value()?;
                }
                self.leave();
                Ok(())
            }
            0x90..=0x9f => {
                let length = u64::from(marker & 0x0f);
                self.enter()?;
                self.check_declared(length, 1)?;
                for _ in 0..length {
                    self.skip_value()?;
                }
                self.leave();
                Ok(())
            }
            0xa0..=0xbf => self.advance(u64::from(marker & 0x1f)),
            0xc0 | 0xc2 | 0xc3 => Ok(()),
            0xc1 => Err(self.scan_error("reserved MessagePack marker 0xc1")),
            0xc4 => {
                let length = u64::from(self.take(1)?[0]);
                self.advance(length)
            }
            0xc5 => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                self.advance(length)
            }
            0xc6 => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                self.advance(length)
            }
            0xc7 => {
                let length = u64::from(self.take(1)?[0]);
                let _ = self.take(1)?;
                self.advance(length)
            }
            0xc8 => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                let _ = self.take(1)?;
                self.advance(length)
            }
            0xc9 => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                let _ = self.take(1)?;
                self.advance(length)
            }
            0xca => self.advance(4),
            0xcb => self.advance(8),
            0xcc | 0xd0 => self.advance(1),
            0xcd | 0xd1 => self.advance(2),
            0xce | 0xd2 => self.advance(4),
            0xcf | 0xd3 => self.advance(8),
            0xd4 => self.advance(2),
            0xd5 => self.advance(3),
            0xd6 => self.advance(5),
            0xd7 => self.advance(9),
            0xd8 => self.advance(17),
            0xd9 => {
                let length = u64::from(self.take(1)?[0]);
                self.advance(length)
            }
            0xda => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                self.advance(length)
            }
            0xdb => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                self.advance(length)
            }
            0xdc => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                self.enter()?;
                self.check_declared(length, 1)?;
                for _ in 0..length {
                    self.skip_value()?;
                }
                self.leave();
                Ok(())
            }
            0xdd => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                self.enter()?;
                self.check_declared(length, 1)?;
                for _ in 0..length {
                    self.skip_value()?;
                }
                self.leave();
                Ok(())
            }
            0xde => {
                let length = u64::from(u16::from_be_bytes(self.fixed::<2>()?));
                self.enter()?;
                self.check_declared(length, 2)?;
                for _ in 0..length {
                    self.skip_value()?;
                    self.skip_value()?;
                }
                self.leave();
                Ok(())
            }
            0xdf => {
                let length = u64::from(u32::from_be_bytes(self.fixed::<4>()?));
                self.enter()?;
                self.check_declared(length, 2)?;
                for _ in 0..length {
                    self.skip_value()?;
                    self.skip_value()?;
                }
                self.leave();
                Ok(())
            }
        }
    }

    fn fixed<const N: usize>(&mut self) -> Result<[u8; N], PackageError> {
        let bytes = self.take(N)?;
        Ok(bytes.try_into().expect("N-byte slice"))
    }

    /// Builds a scan error for this section.
    pub(crate) fn scan_error(&self, detail: impl Into<String>) -> PackageError {
        PackageError::Scan {
            section: self.section,
            detail: detail.into(),
        }
    }
}
