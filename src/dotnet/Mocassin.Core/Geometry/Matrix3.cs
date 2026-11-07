// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Mocassin.Core.Geometry;

/// <summary>
/// A row-based 3×3 matrix for floating-point geometry.
/// </summary>
/// <typeparam name="T">The floating-point component type.</typeparam>
/// <remarks>
/// Storage is row-major: <see cref="Row0"/> is the first row. <see cref="Transform(Vector3{T})"/> treats the
/// vector as a column vector, so it computes <c>M·v</c>. <see cref="FromColumns"/> builds a matrix whose
/// columns are the supplied vectors.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Generic math primitives require static operators, identity values, and factories.")]
public readonly record struct Matrix3<T>(
    Vector3<T> Row0,
    Vector3<T> Row1,
    Vector3<T> Row2)
    where T : unmanaged, IFloatingPointIeee754<T>
{
    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static Matrix3<T> Identity =>
        new(
            new Vector3<T>(T.One, T.Zero, T.Zero),
            new Vector3<T>(T.Zero, T.One, T.Zero),
            new Vector3<T>(T.Zero, T.Zero, T.One));

    /// <summary>Gets the row-1, column-1 component.</summary>
    public T M11 => Row0.X;

    /// <summary>Gets the row-1, column-2 component.</summary>
    public T M12 => Row0.Y;

    /// <summary>Gets the row-1, column-3 component.</summary>
    public T M13 => Row0.Z;

    /// <summary>Gets the row-2, column-1 component.</summary>
    public T M21 => Row1.X;

    /// <summary>Gets the row-2, column-2 component.</summary>
    public T M22 => Row1.Y;

    /// <summary>Gets the row-2, column-3 component.</summary>
    public T M23 => Row1.Z;

    /// <summary>Gets the row-3, column-1 component.</summary>
    public T M31 => Row2.X;

    /// <summary>Gets the row-3, column-2 component.</summary>
    public T M32 => Row2.Y;

    /// <summary>Gets the row-3, column-3 component.</summary>
    public T M33 => Row2.Z;

    /// <summary>Gets the first column.</summary>
    public Vector3<T> Column0 => new(Row0.X, Row1.X, Row2.X);

    /// <summary>Gets the second column.</summary>
    public Vector3<T> Column1 => new(Row0.Y, Row1.Y, Row2.Y);

    /// <summary>Gets the third column.</summary>
    public Vector3<T> Column2 => new(Row0.Z, Row1.Z, Row2.Z);

    /// <summary>
    /// Creates a matrix from three rows.
    /// </summary>
    /// <param name="row0">The first row.</param>
    /// <param name="row1">The second row.</param>
    /// <param name="row2">The third row.</param>
    /// <returns>The matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3<T> FromRows(Vector3<T> row0, Vector3<T> row1, Vector3<T> row2) =>
        new(row0, row1, row2);

    /// <summary>
    /// Creates a matrix from three columns.
    /// </summary>
    /// <param name="column0">The first column.</param>
    /// <param name="column1">The second column.</param>
    /// <param name="column2">The third column.</param>
    /// <returns>The matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3<T> FromColumns(Vector3<T> column0, Vector3<T> column1, Vector3<T> column2) =>
        new(
            new Vector3<T>(column0.X, column1.X, column2.X),
            new Vector3<T>(column0.Y, column1.Y, column2.Y),
            new Vector3<T>(column0.Z, column1.Z, column2.Z));

    /// <summary>
    /// Gets the transpose.
    /// </summary>
    public Matrix3<T> Transpose() =>
        new(Column0, Column1, Column2);

    /// <summary>
    /// Gets the determinant.
    /// </summary>
    public T Determinant =>
        (M11 * ((M22 * M33) - (M23 * M32)))
        - (M12 * ((M21 * M33) - (M23 * M31)))
        + (M13 * ((M21 * M32) - (M22 * M31)));

    /// <summary>
    /// Gets the infinity norm (maximum absolute row sum).
    /// </summary>
    public T InfinityNorm
    {
        get
        {
            var first = T.Abs(Row0.X) + T.Abs(Row0.Y) + T.Abs(Row0.Z);
            var second = T.Abs(Row1.X) + T.Abs(Row1.Y) + T.Abs(Row1.Z);
            var third = T.Abs(Row2.X) + T.Abs(Row2.Y) + T.Abs(Row2.Z);
            return T.Max(first, T.Max(second, third));
        }
    }

    /// <summary>
    /// Gets the infinity-norm condition number.
    /// </summary>
    /// <exception cref="InvalidOperationException">The matrix is singular.</exception>
    public T ConditionNumber => InfinityNorm * Inverse().InfinityNorm;

    /// <summary>
    /// Transforms a column vector as <c>M·v</c>.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<T> Transform(Vector3<T> vector) =>
        new(
            Vector3<T>.Dot(Row0, vector),
            Vector3<T>.Dot(Row1, vector),
            Vector3<T>.Dot(Row2, vector));

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="right">The right matrix.</param>
    /// <returns>The matrix product.</returns>
    public static Matrix3<T> operator *(Matrix3<T> left, Matrix3<T> right)
    {
        var right0 = right.Column0;
        var right1 = right.Column1;
        var right2 = right.Column2;
        return new Matrix3<T>(
            new Vector3<T>(
                Vector3<T>.Dot(left.Row0, right0),
                Vector3<T>.Dot(left.Row0, right1),
                Vector3<T>.Dot(left.Row0, right2)),
            new Vector3<T>(
                Vector3<T>.Dot(left.Row1, right0),
                Vector3<T>.Dot(left.Row1, right1),
                Vector3<T>.Dot(left.Row1, right2)),
            new Vector3<T>(
                Vector3<T>.Dot(left.Row2, right0),
                Vector3<T>.Dot(left.Row2, right1),
                Vector3<T>.Dot(left.Row2, right2)));
    }

    /// <summary>
    /// Computes the inverse.
    /// </summary>
    /// <returns>The inverse matrix.</returns>
    /// <exception cref="InvalidOperationException">The determinant is zero.</exception>
    public Matrix3<T> Inverse()
    {
        var determinant = Determinant;
        if (determinant == T.Zero)
        {
            throw new InvalidOperationException("A singular matrix has no inverse.");
        }

        var inverseDeterminant = T.One / determinant;
        var c00 = (M22 * M33) - (M23 * M32);
        var c01 = -((M21 * M33) - (M23 * M31));
        var c02 = (M21 * M32) - (M22 * M31);
        var c10 = -((M12 * M33) - (M13 * M32));
        var c11 = (M11 * M33) - (M13 * M31);
        var c12 = -((M11 * M32) - (M12 * M31));
        var c20 = (M12 * M23) - (M13 * M22);
        var c21 = -((M11 * M23) - (M13 * M21));
        var c22 = (M11 * M22) - (M12 * M21);

        return new Matrix3<T>(
            new Vector3<T>(c00 * inverseDeterminant, c10 * inverseDeterminant, c20 * inverseDeterminant),
            new Vector3<T>(c01 * inverseDeterminant, c11 * inverseDeterminant, c21 * inverseDeterminant),
            new Vector3<T>(c02 * inverseDeterminant, c12 * inverseDeterminant, c22 * inverseDeterminant));
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"[{Row0}; {Row1}; {Row2}]");
}
