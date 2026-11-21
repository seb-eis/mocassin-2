// SPDX-License-Identifier: MIT

using MessagePack;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Non-allocating bounds scan for the optional cluster <c>code_expansions</c> member.
/// </summary>
/// <remarks>
/// The scan runs on the decompressed MessagePack bytes before
/// <c>MessagePackSerializer.Deserialize&lt;ModelPayload&gt;</c>. It walks only
/// <c>energy.cluster_tables[*].code_expansions</c>, rejects an unequal code/index pair and a duplicate known key
/// at scan time, and enforces the cumulative <c>Σ(8E + 4I)</c> budget before the typed arrays are allocated.
/// Every other value is skipped with a depth-bounded, non-allocating walk. A malformed or over-budget document
/// is reported as a decode failure that maps to <c>PKG004</c>.
/// </remarks>
internal static class ClusterExpansionScan
{
    private const int maxDepth = 32;
    private const long maxElements = 512_000_000;
    private const string fieldPath = "energy.cluster_tables.code_expansions";

    /// <summary>
    /// Validates the optional expansion maps of one decompressed model payload.
    /// </summary>
    /// <param name="payload">The decompressed MessagePack document.</param>
    /// <param name="maxClusterExpansionBytes">The cumulative typed-array budget, <c>Σ(8E + 4I)</c>.</param>
    /// <exception cref="MessagePackSerializationException">
    /// The document is malformed, a known key is duplicated, a length pair differs, or the budget is exceeded.
    /// </exception>
    public static void Validate(ReadOnlyMemory<byte> payload, long maxClusterExpansionBytes)
    {
        if (maxClusterExpansionBytes < 0)
        {
            throw new MessagePackSerializationException("The cluster expansion budget is negative.");
        }

        var reader = new MessagePackReader(payload);
        var elements = 0L;
        var total = 0L;
        var seenEnergy = false;
        var length = reader.ReadMapHeader();
        for (var index = 0; index < length; index++)
        {
            var key = reader.ReadString();
            if (string.Equals(key, "energy", StringComparison.Ordinal))
            {
                if (seenEnergy)
                {
                    throw new MessagePackSerializationException("The model payload repeats the energy member.");
                }

                seenEnergy = true;
                ScanEnergy(ref reader, ref total, maxClusterExpansionBytes, ref elements);
            }
            else
            {
                SkipValue(ref reader, 1, ref elements);
            }
        }
    }

    private static void ScanEnergy(
        ref MessagePackReader reader,
        ref long total,
        long maxClusterExpansionBytes,
        ref long elements)
    {
        var length = reader.ReadMapHeader();
        var seenTables = false;
        for (var index = 0; index < length; index++)
        {
            var key = reader.ReadString();
            if (string.Equals(key, "cluster_tables", StringComparison.Ordinal))
            {
                if (seenTables)
                {
                    throw new MessagePackSerializationException(
                        "The model payload repeats the energy.cluster_tables member.");
                }

                seenTables = true;
                var tableCount = reader.ReadArrayHeader();
                for (var table = 0; table < tableCount; table++)
                {
                    ScanClusterTable(
                        ref reader,
                        table,
                        ref total,
                        maxClusterExpansionBytes,
                        ref elements);
                }
            }
            else
            {
                SkipValue(ref reader, 2, ref elements);
            }
        }
    }

    private static void ScanClusterTable(
        ref MessagePackReader reader,
        int table,
        ref long total,
        long maxClusterExpansionBytes,
        ref long elements)
    {
        var length = reader.ReadMapHeader();
        var seenExpansions = false;
        for (var index = 0; index < length; index++)
        {
            var key = reader.ReadString();
            if (string.Equals(key, "code_expansions", StringComparison.Ordinal))
            {
                if (seenExpansions)
                {
                    throw new MessagePackSerializationException(
                        $"The model payload repeats {fieldPath} in cluster table {table}.");
                }

                seenExpansions = true;
                ScanCodeExpansion(
                    ref reader,
                    table,
                    ref total,
                    maxClusterExpansionBytes,
                    ref elements);
            }
            else
            {
                SkipValue(ref reader, 3, ref elements);
            }
        }
    }

    private static void ScanCodeExpansion(
        ref MessagePackReader reader,
        int table,
        ref long total,
        long maxClusterExpansionBytes,
        ref long elements)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var length = reader.ReadMapHeader();
        var seenCodes = false;
        var seenIndices = false;
        var codeCount = -1;
        var indexCount = -1;
        for (var index = 0; index < length; index++)
        {
            var key = reader.ReadString();
            if (string.Equals(key, "expanded_codes", StringComparison.Ordinal))
            {
                if (seenCodes)
                {
                    throw new MessagePackSerializationException(
                        $"The model payload repeats {fieldPath}.expanded_codes in cluster table {table}.");
                }

                seenCodes = true;
                codeCount = reader.ReadArrayHeader();
                SkipElements(ref reader, codeCount, ref elements);
            }
            else if (string.Equals(key, "canonical_indices", StringComparison.Ordinal))
            {
                if (seenIndices)
                {
                    throw new MessagePackSerializationException(
                        $"The model payload repeats {fieldPath}.canonical_indices in cluster table {table}.");
                }

                seenIndices = true;
                indexCount = reader.ReadArrayHeader();
                SkipElements(ref reader, indexCount, ref elements);
            }
            else
            {
                SkipValue(ref reader, 4, ref elements);
            }
        }

        if (codeCount < 0 || indexCount < 0)
        {
            throw new MessagePackSerializationException(
                $"The model payload is missing a required member of {fieldPath} in cluster table {table}.");
        }

        if (codeCount != indexCount)
        {
            throw new MessagePackSerializationException(
                $"{fieldPath}.expanded_codes has {codeCount} entries but canonical_indices has {indexCount} in cluster table {table}.");
        }

        long cost;
        try
        {
            cost = checked((codeCount * 8L) + (indexCount * 4L));
        }
        catch (OverflowException exception)
        {
            throw new MessagePackSerializationException(
                $"The typed {fieldPath} budget overflows in cluster table {table}.", exception);
        }

        try
        {
            total = checked(total + cost);
        }
        catch (OverflowException exception)
        {
            throw new MessagePackSerializationException(
                $"The cumulative {fieldPath} budget overflows.", exception);
        }

        if (total > maxClusterExpansionBytes)
        {
            throw new MessagePackSerializationException(
                $"The cumulative {fieldPath} cost {total} bytes exceeds the {maxClusterExpansionBytes}-byte decode budget.");
        }
    }

    private static void SkipElements(ref MessagePackReader reader, int count, ref long elements)
    {
        for (var index = 0; index < count; index++)
        {
            SkipValue(ref reader, 5, ref elements);
        }
    }

    private static void SkipValue(ref MessagePackReader reader, int depth, ref long elements)
    {
        if (depth > maxDepth)
        {
            throw new MessagePackSerializationException(
                $"The model payload exceeds the {maxDepth}-level decode scan depth.");
        }

        if (++elements > maxElements)
        {
            throw new MessagePackSerializationException(
                $"The model payload exceeds the {maxElements}-element decode scan budget.");
        }

        switch (reader.NextMessagePackType)
        {
            case MessagePackType.Array:
                var arrayLength = reader.ReadArrayHeader();
                for (var index = 0; index < arrayLength; index++)
                {
                    SkipValue(ref reader, depth + 1, ref elements);
                }

                break;
            case MessagePackType.Map:
                var mapLength = reader.ReadMapHeader();
                for (var index = 0; index < mapLength; index++)
                {
                    SkipValue(ref reader, depth + 1, ref elements);
                    SkipValue(ref reader, depth + 1, ref elements);
                }

                break;
            default:
                reader.Skip();
                break;
        }
    }
}
