// SPDX-License-Identifier: MIT

using System.Globalization;

namespace Mocassin.App.ViewModels.Editors.Expressions;

/// <summary>
/// The in-house numeric expression grammar used by cell parameters and fractional-coordinate fields.
/// </summary>
/// <remarks>
/// <para>
/// The grammar is <c>+ - * / ^</c>, parentheses, unary minus, the constants <c>pi</c> and <c>e</c>, and the
/// functions <c>sqrt</c>, <c>ln</c>, <c>exp</c>, and <c>abs</c>. There are no trigonometric functions in v1: the
/// fields are angles in degrees while an expression is unitless, so the unit convention would be a guess.
/// </para>
/// <para>
/// Lexing rule (m-6): exponent notation is part of a number and requires at least one exponent digit, so
/// <c>1e5</c> and <c>2e-3</c> are numbers while <c>1e</c> and <c>2e</c> are malformed numbers. The bare identifier
/// <c>e</c> is the constant. There is no implicit multiplication (<c>2 pi</c> is an error). Parsing is
/// culture-invariant and bounded in input length and nesting depth; every non-finite result is rejected.
/// </para>
/// </remarks>
internal static class NumericExpression
{
    /// <summary>
    /// The maximum accepted input length, including whitespace.
    /// </summary>
    public const int MaxInputLength = 128;

    /// <summary>
    /// The maximum accepted parenthesis/function nesting depth.
    /// </summary>
    public const int MaxNestingDepth = 16;

    /// <summary>
    /// Tries to parse an expression.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="value">The finite value when parsing succeeds.</param>
    /// <param name="error">A user-readable message when parsing fails.</param>
    /// <returns><see langword="true"/> when the text is a valid finite expression.</returns>
    public static bool TryParse(string? text, out double value, out string? error)
    {
        value = 0d;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter a number or expression.";
            return false;
        }

        if (text.Length > MaxInputLength)
        {
            error = string.Create(
                CultureInfo.InvariantCulture,
                $"The expression is longer than {MaxInputLength} characters.");
            return false;
        }

        try
        {
            var parser = new Parser(text);
            var parsed = parser.Parse();
            if (!double.IsFinite(parsed))
            {
                error = "The expression does not evaluate to a finite number.";
                return false;
            }

            value = parsed;
            return true;
        }
        catch (ExpressionException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private sealed class Parser(string text)
    {
        private int index;
        private int depth;

        public double Parse()
        {
            var value = ParseExpression();
            SkipWhitespace();
            if (index < text.Length)
            {
                throw Error($"Unexpected '{text[index]}' after the expression.");
            }

            return value;
        }

        private double ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+'))
                {
                    value += ParseTerm();
                }
                else if (Match('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseTerm()
        {
            var value = ParseUnary();
            while (true)
            {
                SkipWhitespace();
                if (Match('*'))
                {
                    value *= ParseUnary();
                }
                else if (Match('/'))
                {
                    value /= ParseUnary();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseUnary()
        {
            SkipWhitespace();
            if (Match('+'))
            {
                return ParseUnary();
            }

            if (Match('-'))
            {
                return -ParseUnary();
            }

            return ParsePower();
        }

        private double ParsePower()
        {
            var value = ParsePrimary();
            SkipWhitespace();
            return Match('^') ? Math.Pow(value, ParseUnary()) : value;
        }

        private double ParsePrimary()
        {
            SkipWhitespace();
            if (index >= text.Length)
            {
                throw Error("The expression ends where a value was expected.");
            }

            if (Match('('))
            {
                depth++;
                if (depth > MaxNestingDepth)
                {
                    throw Error($"The expression nests deeper than {MaxNestingDepth} levels.");
                }

                var value = ParseExpression();
                SkipWhitespace();
                if (!Match(')'))
                {
                    throw Error("A closing parenthesis is missing.");
                }

                depth--;
                RejectImplicitMultiplication();
                return value;
            }

            if (char.IsAsciiDigit(text[index]) || text[index] == '.')
            {
                var value = ParseNumber();
                RejectImplicitMultiplication();
                return value;
            }

            if (char.IsAsciiLetter(text[index]))
            {
                var value = ParseIdentifier();
                RejectImplicitMultiplication();
                return value;
            }

            throw Error($"Unexpected '{text[index]}'.");
        }

        private double ParseNumber()
        {
            var start = index;
            var hasDigit = false;
            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
                hasDigit = true;
            }

            if (index < text.Length && text[index] == '.')
            {
                index++;
                while (index < text.Length && char.IsAsciiDigit(text[index]))
                {
                    index++;
                    hasDigit = true;
                }
            }

            if (!hasDigit)
            {
                throw Error("A number must contain at least one digit.");
            }

            if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
            {
                index++;
                if (index < text.Length && (text[index] == '+' || text[index] == '-'))
                {
                    index++;
                }

                var exponentStart = index;
                while (index < text.Length && char.IsAsciiDigit(text[index]))
                {
                    index++;
                }

                if (index == exponentStart)
                {
                    throw Error("An exponent must contain at least one digit.");
                }
            }

            var token = text[start..index];
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw Error($"'{token}' is not a valid number.");
            }

            return value;
        }

        private double ParseIdentifier()
        {
            var start = index;
            while (index < text.Length && char.IsAsciiLetter(text[index]))
            {
                index++;
            }

            var name = text[start..index];
            SkipWhitespace();
            if (Match('('))
            {
                depth++;
                if (depth > MaxNestingDepth)
                {
                    throw Error($"The expression nests deeper than {MaxNestingDepth} levels.");
                }

                var argument = ParseExpression();
                SkipWhitespace();
                if (!Match(')'))
                {
                    throw Error($"The function '{name}' is missing its closing parenthesis.");
                }

                depth--;
                return ApplyFunction(name, argument);
            }

            return name.ToLowerInvariant() switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                _ => throw Error($"Unknown name '{name}'. The constants are pi and e."),
            };
        }

        private static double ApplyFunction(string name, double argument) => name.ToLowerInvariant() switch
        {
            "sqrt" => Math.Sqrt(argument),
            "ln" => Math.Log(argument),
            "exp" => Math.Exp(argument),
            "abs" => Math.Abs(argument),
            _ => throw new ExpressionException($"Unknown function '{name}'."),
        };

        private void RejectImplicitMultiplication()
        {
            SkipWhitespace();
            if (index < text.Length)
            {
                var next = text[index];
                if (char.IsAsciiLetterOrDigit(next) || next is '.' or '(')
                {
                    throw Error("Implicit multiplication is not supported; use an explicit operator.");
                }
            }
        }

        private bool Match(char expected)
        {
            if (index < text.Length && text[index] == expected)
            {
                index++;
                return true;
            }

            return false;
        }

        private void SkipWhitespace()
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }

        private static ExpressionException Error(string message) => new(message);
    }

    private sealed class ExpressionException(string message) : Exception(message);
}
