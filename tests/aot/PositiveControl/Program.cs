// SPDX-License-Identifier: MIT

using System.Text.Json;

// This project is a positive control, never merged into the zero-warning host: reflection-based JSON
// serialization must emit IL2026 and IL3050 during the Native AOT publish.
Console.WriteLine(JsonSerializer.Serialize(new object()));
