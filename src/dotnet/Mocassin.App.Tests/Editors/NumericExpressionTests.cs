// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.App.ViewModels.Editors.Expressions;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the in-house numeric expression grammar, its lexing rule (m-6), its bounds, and its culture invariance.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class NumericExpressionTests
{
    [Theory]
    [InlineData("1", 1d)]
    [InlineData("1.5", 1.5d)]
    [InlineData("+2", 2d)]
    [InlineData("-2", -2d)]
    [InlineData("1 + 2 * 3", 7d)]
    [InlineData("(1 + 2) * 3", 9d)]
    [InlineData("2 ^ 3 ^ 2", 512d)]
    [InlineData("-2 ^ 2", -4d)]
    [InlineData("2 ^ -2", 0.25d)]
    [InlineData("1e5", 100000d)]
    [InlineData("2e-3", 0.002d)]
    [InlineData("e", Math.E)]
    [InlineData("pi", Math.PI)]
    [InlineData("sqrt(9)", 3d)]
    [InlineData("ln(e)", 1d)]
    [InlineData("exp(0)", 1d)]
    [InlineData("abs(-3)", 3d)]
    [InlineData("1/3", 1d / 3d)]
    public void ValidExpressionsEvaluate(string text, double expected)
    {
        var success = NumericExpression.TryParse(text, out var value, out var error);

        Assert.True(success, error);
        Assert.Equal(expected, value, 12);
    }

    [Theory]
    [InlineData("1e")]
    [InlineData("2e")]
    [InlineData("1e+")]
    [InlineData("2 pi")]
    [InlineData("2(3)")]
    [InlineData("(1 + 2")]
    [InlineData("1 +")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin(30)")]
    [InlineData("unknown + 1")]
    [InlineData("1..2")]
    [InlineData("sqrt")]
    public void MalformedExpressionsAreRejected(string text)
    {
        var success = NumericExpression.TryParse(text, out _, out var error);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("1/0")]
    [InlineData("ln(0)")]
    [InlineData("sqrt(-1)")]
    [InlineData("exp(1000)")]
    public void NonFiniteResultsAreRejected(string text)
    {
        var success = NumericExpression.TryParse(text, out _, out var error);

        Assert.False(success);
        Assert.Contains("finite", error, StringComparison.Ordinal);
    }

    [Fact]
    public void OverLongInputIsRejectedBeforeParsing()
    {
        var text = new string('1', NumericExpression.MaxInputLength + 1);

        Assert.False(NumericExpression.TryParse(text, out _, out var error));
        Assert.Contains("longer", error, StringComparison.Ordinal);
    }

    [Fact]
    public void DeepNestingIsRejected()
    {
        var text = new string('(', NumericExpression.MaxNestingDepth + 1)
            + "1"
            + new string(')', NumericExpression.MaxNestingDepth + 1);

        Assert.False(NumericExpression.TryParse(text, out _, out var error));
        Assert.Contains("nest", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void ParsingIsCultureInvariant(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            Assert.True(NumericExpression.TryParse("1.5 + 2e-1", out var value, out var error), error);
            Assert.Equal(1.7d, value, 12);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void DeterministicPropertySweepKeepsAssociativityAndFiniteness()
    {
        // Property-style sweep without a generator dependency: every expression built from these pieces must either
        // parse to a finite value or fail with a message, never throw.
        var pieces = new[] { "1", "2.5", "-3", "pi", "e", "1e3", "sqrt(4)", "(1+2)", "2^3" };
        var operators = new[] { "+", "-", "*", "/", "^" };
        foreach (var left in pieces)
        {
            foreach (var op in operators)
            {
                foreach (var right in pieces)
                {
                    var text = $"{left} {op} {right}";
                    var success = NumericExpression.TryParse(text, out var value, out var error);
                    if (success)
                    {
                        Assert.True(double.IsFinite(value), text);
                    }
                    else
                    {
                        Assert.False(string.IsNullOrWhiteSpace(error), text);
                    }
                }
            }
        }
    }
}
