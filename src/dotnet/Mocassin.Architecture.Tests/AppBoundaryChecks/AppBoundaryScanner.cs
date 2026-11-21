// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Mocassin.Architecture.Tests.AppBoundaryChecks;

/// <summary>
/// Source-token tripwire for the view-model I/O boundary. It blanks comments and literal contents, then matches
/// whole identifiers so that a member such as <c>LastOpenDirectory.Trim</c> is not mistaken for
/// <c>Directory.</c>. The scan is a tripwire, not a compiler guarantee.
/// </summary>
internal static class AppBoundaryScanner
{
    private static readonly (string Name, Regex Pattern)[] banned =
    [
        ("System.IO", new Regex(@"(?<![\w])System\.IO(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("File.", new Regex(@"(?<![\w])File\.", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("FileInfo", new Regex(@"(?<![\w])FileInfo(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Directory.", new Regex(@"(?<![\w])Directory\.", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("DirectoryInfo", new Regex(@"(?<![\w])DirectoryInfo(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Path.Combine", new Regex(@"(?<![\w])Path\.Combine(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Path.Get", new Regex(@"(?<![\w])Path\.Get(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("FileStream", new Regex(@"(?<![\w])FileStream(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("StreamReader", new Regex(@"(?<![\w])StreamReader(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("StreamWriter", new Regex(@"(?<![\w])StreamWriter(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("System.Environment", new Regex(@"(?<![\w])System\.Environment(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Environment.", new Regex(@"(?<![\w])Environment\.", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Avalonia", new Regex(@"(?<![\w])Avalonia(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Dispatcher.UIThread", new Regex(@"(?<![\w])Dispatcher\.UIThread(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("Application.Current", new Regex(@"(?<![\w])Application\.Current(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("ResourceDictionary", new Regex(@"(?<![\w])ResourceDictionary(?![\w])", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
    ];

    /// <summary>
    /// Scans one source text for banned tokens.
    /// </summary>
    /// <param name="source">The C# source text.</param>
    /// <param name="fileName">The optional file name used in the reported violations.</param>
    /// <returns>The violations, empty when the source is clean.</returns>
    public static IReadOnlyList<string> ScanSource(string source, string fileName = "<memory>")
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: fileName);
        var root = tree.GetRoot();
        var masked = Mask(source, root);
        var violations = new List<string>();

        foreach (var (name, pattern) in banned)
        {
            foreach (Match match in pattern.Matches(masked))
            {
                var line = tree.GetLineSpan(new TextSpan(match.Index, match.Length)).StartLinePosition.Line + 1;
                violations.Add($"{fileName}({line}): banned token '{name}'.");
            }
        }

        return violations;
    }

    private static string Mask(string source, SyntaxNode root)
    {
        var characters = source.ToCharArray();
        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            Blank(characters, trivia.Span);
        }

        foreach (var token in root.DescendantTokens())
        {
            if (IsLiteral(token))
            {
                Blank(characters, token.Span);
            }
        }

        return new string(characters);
    }

    private static bool IsLiteral(SyntaxToken token) =>
        token.IsKind(SyntaxKind.StringLiteralToken)
        || token.IsKind(SyntaxKind.Utf8StringLiteralToken)
        || token.IsKind(SyntaxKind.CharacterLiteralToken)
        || token.IsKind(SyntaxKind.InterpolatedStringTextToken)
        || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken)
        || token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken)
        || token.IsKind(SyntaxKind.Utf8SingleLineRawStringLiteralToken)
        || token.IsKind(SyntaxKind.Utf8MultiLineRawStringLiteralToken);

    private static void Blank(char[] characters, TextSpan span)
    {
        for (var index = span.Start; index < span.End && index < characters.Length; index++)
        {
            if (characters[index] is not ('\r' or '\n'))
            {
                characters[index] = ' ';
            }
        }
    }
}
