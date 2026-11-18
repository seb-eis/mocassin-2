// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// Owns response-file (<c>@file</c>) tokenization, recursive expansion, and the documented limits.
/// </summary>
/// <remarks>
/// The parser library's response-file hook is disabled (<c>ResponseFileTokenReplacer = null</c>); this expander
/// runs before parsing and produces the flattened token list. The syntax is the library's observable
/// <c>SplitCommandLine</c> syntax, but the contract is CLI-owned and pinned by golden tests.
/// </remarks>
internal static class CliResponseFiles
{
    /// <summary>The maximum raw byte size of one response file.</summary>
    public const int MaxFileBytes = 1024 * 1024;

    /// <summary>The maximum raw byte size of all response files on one command line.</summary>
    public const int MaxTotalBytes = 4 * 1024 * 1024;

    /// <summary>The maximum response-file nesting depth.</summary>
    public const int MaxDepth = 8;

    /// <summary>The maximum number of tokens contributed by response files.</summary>
    public const int MaxContributedTokens = 10_000;

    /// <summary>The maximum number of response files on one command line.</summary>
    public const int MaxFiles = 32;

    private static readonly UTF8Encoding strictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    /// <summary>
    /// Expands every response-file reference in the raw process arguments.
    /// </summary>
    /// <param name="arguments">The raw process arguments.</param>
    /// <returns>The flattened tokens and, on failure, the <c>CLI001</c> message.</returns>
    public static ResponseFileExpansion Expand(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var state = new ExpansionState();
        var tokens = new List<string>(arguments.Count);
        try
        {
            var optionsEnded = false;
            foreach (var token in arguments)
            {
                if (optionsEnded)
                {
                    tokens.Add(token);
                    continue;
                }

                if (string.Equals(token, "--", StringComparison.Ordinal))
                {
                    tokens.Add(token);
                    optionsEnded = true;
                    continue;
                }

                if (TryGetReference(token, out var path))
                {
                    var start = tokens.Count;
                    try
                    {
                        state.ExpandFile(path, depth: 1, tokens);
                    }
                    catch
                    {
                        // The contract recognises a command only outside the file that failed to expand.
                        // Drop every token contributed by that top-level file (including nested ones) so a
                        // partial expansion cannot masquerade as a command-line command.
                        tokens.RemoveRange(start, tokens.Count - start);
                        throw;
                    }

                    continue;
                }

                tokens.Add(token);
            }

            return new ResponseFileExpansion(tokens, null);
        }
        catch (ResponseFileException exception)
        {
            return new ResponseFileExpansion(tokens, exception.Message);
        }
    }

    /// <summary>
    /// Tokenizes one response-file line with the documented quoting rules.
    /// </summary>
    /// <param name="line">The trimmed, non-empty line.</param>
    /// <returns>The tokens of the line.</returns>
    public static IReadOnlyList<string> TokenizeLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var tokens = new List<string>();
        var token = new StringBuilder();
        var inQuotes = false;
        var startedWithQuote = false;
        var tokenStarted = false;

        foreach (var character in line)
        {
            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                Flush(tokens, token, ref tokenStarted, ref startedWithQuote);
                continue;
            }

            if (character == '"')
            {
                if (inQuotes)
                {
                    inQuotes = false;
                    if (startedWithQuote)
                    {
                        // A token that started with a quote ends at its closing quote; any following character
                        // starts a new token.
                        Flush(tokens, token, ref tokenStarted, ref startedWithQuote);
                    }

                    continue;
                }

                if (!tokenStarted)
                {
                    startedWithQuote = true;
                }

                inQuotes = true;
                tokenStarted = true;
                continue;
            }

            token.Append(character);
            tokenStarted = true;
        }

        if (inQuotes)
        {
            // An unterminated quote discards a token that began with the quote; a token with unquoted text
            // before the quote keeps that text and the rest of the line, with the quote removed.
            if (!startedWithQuote)
            {
                Flush(tokens, token, ref tokenStarted, ref startedWithQuote);
            }

            return tokens;
        }

        Flush(tokens, token, ref tokenStarted, ref startedWithQuote);
        return tokens;
    }

    private static void Flush(
        List<string> tokens,
        StringBuilder token,
        ref bool tokenStarted,
        ref bool startedWithQuote)
    {
        if (!tokenStarted)
        {
            return;
        }

        tokens.Add(token.ToString());
        token.Clear();
        tokenStarted = false;
        startedWithQuote = false;
    }

    private static bool TryGetReference(string token, out string path)
    {
        if (token.Length >= 2 && token[0] == '@')
        {
            path = token[1..];
            return true;
        }

        path = string.Empty;
        return false;
    }

    private sealed class ExpansionState
    {
        private int fileCount;
        private long totalBytes;
        private int contributedTokens;

        public void ExpandFile(string path, int depth, List<string> tokens)
        {
            if (depth > MaxDepth)
            {
                throw new ResponseFileException(
                    $"Response file nesting exceeds the depth limit of {MaxDepth} at '{path}'.");
            }

            fileCount++;
            if (fileCount > MaxFiles)
            {
                throw new ResponseFileException(
                    $"Response files exceed the limit of {MaxFiles} files at '{path}'.");
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception exception) when (exception is ArgumentException
                                                  or NotSupportedException
                                                  or PathTooLongException)
            {
                throw new ResponseFileException(
                    $"Response file '{path}' could not be read: {exception.Message}");
            }

            if (Directory.Exists(fullPath))
            {
                throw new ResponseFileException(
                    $"Response file '{path}' could not be read: the path is a directory.");
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(fullPath);
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                throw new ResponseFileException($"Response file '{path}' was not found.");
            }
            catch (Exception exception) when (exception is IOException
                                                  or UnauthorizedAccessException
                                                  or NotSupportedException
                                                  or System.Security.SecurityException)
            {
                throw new ResponseFileException(
                    $"Response file '{path}' could not be read: {exception.Message}");
            }

            if (bytes.Length > MaxFileBytes)
            {
                throw new ResponseFileException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Response file '{path}' exceeds the per-file limit of {MaxFileBytes} bytes."));
            }

            totalBytes += bytes.Length;
            if (totalBytes > MaxTotalBytes)
            {
                throw new ResponseFileException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Response files exceed the total limit of {MaxTotalBytes} bytes at '{path}'."));
            }

            string text;
            try
            {
                text = strictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                throw new ResponseFileException($"Response file '{path}' is not valid UTF-8 text.");
            }

            if (text.Contains('\0'))
            {
                throw new ResponseFileException($"Response file '{path}' is not valid UTF-8 text.");
            }

            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                text = text[1..];
            }

            var optionsEnded = false;
            foreach (var rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal)
                         .Replace('\r', '\n')
                         .Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                foreach (var token in TokenizeLine(line))
                {
                    if (optionsEnded)
                    {
                        AddContributedToken(token, path, tokens);
                        continue;
                    }

                    if (string.Equals(token, "--", StringComparison.Ordinal))
                    {
                        optionsEnded = true;
                        AddContributedToken(token, path, tokens);
                        continue;
                    }

                    if (TryGetReference(token, out var nestedPath))
                    {
                        ExpandFile(nestedPath, depth + 1, tokens);
                        continue;
                    }

                    AddContributedToken(token, path, tokens);
                }
            }
        }

        private void AddContributedToken(string token, string path, List<string> tokens)
        {
            contributedTokens++;
            if (contributedTokens > MaxContributedTokens)
            {
                throw new ResponseFileException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Response files exceed the token limit of {MaxContributedTokens} at '{path}'."));
            }

            tokens.Add(token);
        }
    }

    private sealed class ResponseFileException : Exception
    {
        public ResponseFileException(string message)
            : base(message)
        {
        }
    }
}
