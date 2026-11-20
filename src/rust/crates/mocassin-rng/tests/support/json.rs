// SPDX-License-Identifier: MIT

//! A tiny JSON reader for the checked-in RNG vector document.
//!
//! The workspace does not add a JSON dependency for tests; the vector document is a small, fixed schema.

/// One parsed JSON value.
#[derive(Debug, Clone, PartialEq)]
pub enum Json {
    /// `null`.
    Null,
    /// A boolean.
    Bool(bool),
    /// A number, parsed as `f64`.
    Number(f64),
    /// A string.
    String(String),
    /// An array.
    Array(Vec<Json>),
    /// An object with ordered keys.
    Object(Vec<(String, Json)>),
}

impl Json {
    /// Parses a complete document.
    ///
    /// # Panics
    ///
    /// Panics on malformed input; the document is a checked-in fixture.
    pub fn parse(text: &str) -> Self {
        let mut parser = Parser {
            bytes: text.as_bytes(),
            position: 0,
        };
        let value = parser.value();
        parser.whitespace();
        assert_eq!(parser.position, parser.bytes.len(), "trailing JSON content");
        value
    }

    /// Returns an object member.
    ///
    /// # Panics
    ///
    /// Panics when the value is not an object or the key is absent.
    pub fn get(&self, key: &str) -> &Self {
        match self {
            Self::Object(entries) => entries
                .iter()
                .find(|(name, _)| name == key)
                .map(|(_, value)| value)
                .unwrap_or_else(|| panic!("missing JSON key {key}")),
            _ => panic!("expected a JSON object for key {key}"),
        }
    }

    /// Returns an array element.
    ///
    /// # Panics
    ///
    /// Panics when the value is not an array or the index is out of range.
    pub fn index(&self, index: usize) -> &Self {
        match self {
            Self::Array(values) => &values[index],
            _ => panic!("expected a JSON array"),
        }
    }

    /// Returns the string value.
    ///
    /// # Panics
    ///
    /// Panics when the value is not a string.
    pub fn as_str(&self) -> &str {
        match self {
            Self::String(value) => value,
            _ => panic!("expected a JSON string"),
        }
    }

    /// Returns the numeric value.
    ///
    /// # Panics
    ///
    /// Panics when the value is not a number.
    pub fn as_f64(&self) -> f64 {
        match self {
            Self::Number(value) => *value,
            _ => panic!("expected a JSON number"),
        }
    }

    /// Returns the numeric value as an unsigned integer.
    ///
    /// # Panics
    ///
    /// Panics when the value is not a number.
    pub fn as_u64(&self) -> u64 {
        self.as_f64() as u64
    }

    /// Returns the boolean value.
    ///
    /// # Panics
    ///
    /// Panics when the value is not a boolean.
    pub fn as_bool(&self) -> bool {
        match self {
            Self::Bool(value) => *value,
            _ => panic!("expected a JSON boolean"),
        }
    }

    /// Returns the array values.
    ///
    /// # Panics
    ///
    /// Panics when the value is not an array.
    pub fn as_array(&self) -> &[Self] {
        match self {
            Self::Array(values) => values,
            _ => panic!("expected a JSON array"),
        }
    }
}

struct Parser<'a> {
    bytes: &'a [u8],
    position: usize,
}

impl Parser<'_> {
    fn whitespace(&mut self) {
        while matches!(
            self.bytes.get(self.position),
            Some(b' ' | b'\n' | b'\r' | b'\t')
        ) {
            self.position += 1;
        }
    }

    fn peek(&self) -> u8 {
        self.bytes[self.position]
    }

    fn value(&mut self) -> Json {
        self.whitespace();
        match self.peek() {
            b'{' => self.object(),
            b'[' => self.array(),
            b'"' => Json::String(self.string()),
            b't' => {
                self.literal("true");
                Json::Bool(true)
            }
            b'f' => {
                self.literal("false");
                Json::Bool(false)
            }
            b'n' => {
                self.literal("null");
                Json::Null
            }
            _ => self.number(),
        }
    }

    fn literal(&mut self, expected: &str) {
        assert!(
            self.bytes[self.position..].starts_with(expected.as_bytes()),
            "expected JSON literal {expected}"
        );
        self.position += expected.len();
    }

    fn object(&mut self) -> Json {
        self.position += 1;
        let mut entries = Vec::new();
        self.whitespace();
        if self.peek() == b'}' {
            self.position += 1;
            return Json::Object(entries);
        }
        loop {
            self.whitespace();
            let key = self.string();
            self.whitespace();
            assert_eq!(self.peek(), b':');
            self.position += 1;
            let value = self.value();
            entries.push((key, value));
            self.whitespace();
            match self.peek() {
                b',' => self.position += 1,
                b'}' => {
                    self.position += 1;
                    return Json::Object(entries);
                }
                other => panic!("unexpected JSON byte {other} in object"),
            }
        }
    }

    fn array(&mut self) -> Json {
        self.position += 1;
        let mut values = Vec::new();
        self.whitespace();
        if self.peek() == b']' {
            self.position += 1;
            return Json::Array(values);
        }
        loop {
            values.push(self.value());
            self.whitespace();
            match self.peek() {
                b',' => self.position += 1,
                b']' => {
                    self.position += 1;
                    return Json::Array(values);
                }
                other => panic!("unexpected JSON byte {other} in array"),
            }
        }
    }

    fn string(&mut self) -> String {
        assert_eq!(self.peek(), b'"');
        self.position += 1;
        let mut text = String::new();
        loop {
            let byte = self.peek();
            self.position += 1;
            match byte {
                b'"' => return text,
                b'\\' => {
                    let escape = self.peek();
                    self.position += 1;
                    match escape {
                        b'"' => text.push('"'),
                        b'\\' => text.push('\\'),
                        b'/' => text.push('/'),
                        b'b' => text.push('\u{0008}'),
                        b'f' => text.push('\u{000c}'),
                        b'n' => text.push('\n'),
                        b'r' => text.push('\r'),
                        b't' => text.push('\t'),
                        b'u' => {
                            let hex = &self.bytes[self.position..self.position + 4];
                            let code = u32::from_str_radix(
                                core::str::from_utf8(hex).expect("ASCII hex"),
                                16,
                            )
                            .expect("valid Unicode escape");
                            self.position += 4;
                            text.push(char::from_u32(code).expect("valid Unicode scalar"));
                        }
                        other => panic!("unsupported JSON escape {other}"),
                    }
                }
                other => text.push(char::from(other)),
            }
        }
    }

    fn number(&mut self) -> Json {
        let start = self.position;
        if self.peek() == b'-' {
            self.position += 1;
        }
        while self.peek().is_ascii_digit() {
            self.position += 1;
        }
        if self.peek() == b'.' {
            self.position += 1;
            while self.peek().is_ascii_digit() {
                self.position += 1;
            }
        }
        if matches!(self.peek(), b'e' | b'E') {
            self.position += 1;
            if matches!(self.peek(), b'+' | b'-') {
                self.position += 1;
            }
            while self.peek().is_ascii_digit() {
                self.position += 1;
            }
        }
        let text = core::str::from_utf8(&self.bytes[start..self.position]).expect("ASCII number");
        Json::Number(text.parse().expect("valid JSON number"))
    }
}
