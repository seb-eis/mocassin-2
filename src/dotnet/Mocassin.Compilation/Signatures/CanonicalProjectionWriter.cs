// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using MessagePack;

namespace Mocassin.Compilation.Signatures;

/// <summary>
/// Writes the fixed-width canonical MessagePack projection defined by ADR 0010.
/// </summary>
/// <remarks>
/// <see cref="MessagePackWriter"/> is used for the buffer and the fixed-width scalar writers. The compact
/// array/string convenience methods are deliberately not used because they choose fix/array16/str8/str16
/// encodings; forced headers are written with <c>MessagePackWriter.WriteRaw</c>.
/// </remarks>
internal ref struct CanonicalProjectionWriter
{
    private MessagePackWriter writer;

    /// <summary>Initializes a canonical projection writer over a buffer.</summary>
    public CanonicalProjectionWriter(ArrayBufferWriter<byte> buffer)
    {
        writer = new MessagePackWriter(buffer);
    }

    /// <summary>Writes an array32 header.</summary>
    public void WriteArrayHeader(int count)
    {
        var header = new byte[5];
        header[0] = 0xdd;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), (uint)count);
        writer.WriteRaw(header);
    }

    /// <summary>Writes a str32 header and UTF-8 bytes.</summary>
    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        var header = new byte[5];
        header[0] = 0xdb;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), (uint)bytes.Length);
        writer.WriteRaw(header);
        writer.WriteRaw(bytes);
    }

    /// <summary>Writes a bin32 header and bytes.</summary>
    public void WriteBinary(ReadOnlySpan<byte> value)
    {
        var header = new byte[5];
        header[0] = 0xc6;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), (uint)value.Length);
        writer.WriteRaw(header);
        writer.WriteRaw(value);
    }

    /// <summary>Writes a fixed-width uint64.</summary>
    public void WriteUInt64(ulong value) => writer.WriteUInt64(value);

    /// <summary>Writes a fixed-width int64.</summary>
    public void WriteInt64(long value) => writer.WriteInt64(value);

    /// <summary>Writes a canonical float64 value.</summary>
    public void WriteDouble(double value) => writer.Write(Quantize(value));

    /// <summary>Writes a boolean.</summary>
    public void WriteBool(bool value)
    {
        writer.WriteRaw(value ? new byte[] { 0xc3 } : new byte[] { 0xc2 });
    }

    /// <summary>Writes nil.</summary>
    public void WriteNil()
    {
        writer.WriteRaw(new byte[] { 0xc0 });
    }

    /// <summary>Flushes the final MessagePack span to the buffer.</summary>
    public void Flush() => writer.Flush();

    internal static double Quantize(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException("The canonical projection contains a non-finite value.");
        }

        var scaled = value * 1e12;
        var rounded = scaled >= 0.0 ? Math.Floor(scaled + 0.5) : Math.Ceiling(scaled - 0.5);
        var result = rounded / 1e12;
        return result == 0.0 ? 0.0 : result;
    }
}
