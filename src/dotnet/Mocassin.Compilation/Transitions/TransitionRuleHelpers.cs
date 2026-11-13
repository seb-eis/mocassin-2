// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Context;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Geometry;

namespace Mocassin.Compilation.Transitions;

/// <summary>
/// Pure helpers shared by transition direction and rule compilation.
/// </summary>
internal static class TransitionRuleHelpers
{
    internal const string DirectionDomain = "mocassin/jump-direction/v1";
    internal const string RuleDomain = "mocassin/rule-geometry-id/v1";
    internal const double ChargeTolerance = 1e-9;

    internal static bool MassConserved(CompilationContext context, int[] state0, int[] state2)
    {
        var left = new Dictionary<string, int>(StringComparer.Ordinal);
        var right = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var node = 0; node < state0.Length; node++)
        {
            if (state0[node] != 0)
            {
                var symbol = context.Particles!.Particles[state0[node]].Symbol;
                left[symbol] = left.GetValueOrDefault(symbol) + 1;
            }

            if (state2[node] != 0)
            {
                var symbol = context.Particles!.Particles[state2[node]].Symbol;
                right[symbol] = right.GetValueOrDefault(symbol) + 1;
            }
        }

        return left.Count == right.Count
            && left.All(pair => right.TryGetValue(pair.Key, out var count) && count == pair.Value);
    }

    internal static bool ChargeConserved(CompilationContext context, int[] state0, int[] state2)
    {
        var left = state0.Where(static particle => particle != 0)
            .Sum(particle => context.Particles!.Particles[particle].ChargeElementary);
        var right = state2.Where(static particle => particle != 0)
            .Sum(particle => context.Particles!.Particles[particle].ChargeElementary);
        return Math.Abs(left - right) <= ChargeTolerance;
    }

    internal static (Fractional3[] Movement, Cartesian3 ChargeTransportVector)? ComputeTransport(
        CompilationContext context,
        DirectionDraft direction,
        int[] state0,
        int[] state2)
    {
        var movement = new Fractional3[direction.Absolute.Length];
        var vector = new Cartesian3(0.0, 0.0, 0.0);
        for (var particle = 1; particle < context.Particles!.Count; particle++)
        {
            var from = state0
                .Select((value, node) => (value, node))
                .Where(entry => entry.value == particle)
                .Select(static entry => entry.node)
                .ToArray();
            var to = state2
                .Select((value, node) => (value, node))
                .Where(entry => entry.value == particle)
                .Select(static entry => entry.node)
                .ToArray();
            if (from.Length != to.Length)
            {
                return null;
            }

            var charge = context.Particles.Particles[particle].ChargeElementary;
            for (var index = 0; index < from.Length; index++)
            {
                var sourceNode = from[index];
                var targetNode = to[index];
                if (sourceNode == targetNode)
                {
                    continue;
                }

                var source = context.Encoder!.Decode(direction.Absolute[sourceNode]);
                var target = context.Encoder.Decode(direction.Absolute[targetNode]);
                var delta = new Fractional3(
                    target.A - source.A,
                    target.B - source.B,
                    target.C - source.C);
                movement[sourceNode] = delta;
                var cartesian = context.Cell!.ToCartesian(delta);
                vector = new Cartesian3(
                    vector.X + (charge * cartesian.X),
                    vector.Y + (charge * cartesian.Y),
                    vector.Z + (charge * cartesian.Z));
            }
        }

        return (movement, vector);
    }

    internal static Cartesian3 SumMovement(CellGeometry cell, ImmutableArray<Fractional3> movement)
    {
        var x = 0.0;
        var y = 0.0;
        var z = 0.0;
        foreach (var value in movement)
        {
            var cartesian = cell.ToCartesian(value);
            x += cartesian.X;
            y += cartesian.Y;
            z += cartesian.Z;
        }

        return new Cartesian3(x, y, z);
    }

    internal static string ComputeDirectionId(DirectionDraft draft)
    {
        var builder = new StringBuilder(DirectionDomain)
            .Append('|')
            .Append(draft.Transition.Kind.ToString())
            .Append('|')
            .Append(draft.Transition.Id.ToString("D", CultureInfo.InvariantCulture));
        AppendPath(builder, draft.Relative);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    internal static string ComputeRuleGeometryId(RuleOutcome outcome, DirectionDraft direction)
    {
        var builder = new StringBuilder(RuleDomain)
            .Append('|')
            .Append(direction.Transition.Kind.ToString())
            .Append('|')
            .Append(direction.Transition.Id.ToString("D", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(direction.Chain.Pattern.ToString())
            .Append('|')
            .Append(direction.Key)
            .Append('|')
            .Append(outcome.State0.ToString(CultureInfo.InvariantCulture))
            .Append('|')
            .Append(outcome.State1.ToString(CultureInfo.InvariantCulture))
            .Append('|')
            .Append(outcome.State2.ToString(CultureInfo.InvariantCulture));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    internal static string ComputeRuleGeometryId(RuleDraft rule, DirectionDraft direction) =>
        ComputeRuleGeometryId(
            new RuleOutcome(rule.State0, rule.State1, rule.State2, rule.Movement, rule.ChargeTransportVector, rule.IsInverse),
            direction);

    internal static void AppendPath(StringBuilder builder, Vector4I[] relative)
    {
        foreach (var encoding in relative)
        {
            builder.Append('|')
                .Append(encoding.A.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(encoding.B.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(encoding.C.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(encoding.P.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static string DirectionKey(Vector4I[] relative, bool isInverse)
    {
        var builder = new StringBuilder(isInverse ? "i" : "f");
        AppendPath(builder, relative);
        return builder.ToString();
    }

    internal static ulong EncodeState(int[] state)
    {
        ulong code = 0;
        for (var node = 0; node < state.Length; node++)
        {
            code |= (ulong)state[node] << (6 * node);
        }

        return code;
    }

    internal static int[] DecodeState(ulong code, int nodeCount)
    {
        var state = new int[nodeCount];
        for (var node = 0; node < nodeCount; node++)
        {
            state[node] = (int)((code >> (6 * node)) & 63);
        }

        return state;
    }

    internal static int CompareDrafts(DirectionDraft left, DirectionDraft right)
    {
        var comparison = left.CollectionIndex.CompareTo(right.CollectionIndex);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.Key, right.Key);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.StartP.CompareTo(right.StartP);
    }

}
