// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Numerics;

namespace Mocassin.Core.Geometry;

/// <summary>
/// A row-based 4×4 matrix for floating-point geometry.
/// </summary>
/// <typeparam name="T">The floating-point component type.</typeparam>
/// <remarks>
/// Storage is row-major: <see cref="Row0"/> is the first row. <see cref="Transform(Vector4{T})"/> treats the
/// vector as a column vector, so it computes <c>M·v</c>. Homogeneous point/direction semantics are not part of
/// this type; callers own that convention.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Generic math primitives require static operators, identity values, and factories.")]
public readonly record struct Matrix4<T>(
    Vector4<T> Row0,
    Vector4<T> Row1,
    Vector4<T> Row2,
    Vector4<T> Row3)
    where T : unmanaged, IFloatingPointIeee754<T>
{
    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static Matrix4<T> Identity =>
        new(
            new Vector4<T>(T.One, T.Zero, T.Zero, T.Zero),
            new Vector4<T>(T.Zero, T.One, T.Zero, T.Zero),
            new Vector4<T>(T.Zero, T.Zero, T.One, T.Zero),
            new Vector4<T>(T.Zero, T.Zero, T.Zero, T.One));

    /// <summary>Gets the row-1, column-1 component.</summary>
    public T M11 => Row0.X;

    /// <summary>Gets the row-1, column-2 component.</summary>
    public T M12 => Row0.Y;

    /// <summary>Gets the row-1, column-3 component.</summary>
    public T M13 => Row0.Z;

    /// <summary>Gets the row-1, column-4 component.</summary>
    public T M14 => Row0.W;

    /// <summary>Gets the row-2, column-1 component.</summary>
    public T M21 => Row1.X;

    /// <summary>Gets the row-2, column-2 component.</summary>
    public T M22 => Row1.Y;

    /// <summary>Gets the row-2, column-3 component.</summary>
    public T M23 => Row1.Z;

    /// <summary>Gets the row-2, column-4 component.</summary>
    public T M24 => Row1.W;

    /// <summary>Gets the row-3, column-1 component.</summary>
    public T M31 => Row2.X;

    /// <summary>Gets the row-3, column-2 component.</summary>
    public T M32 => Row2.Y;

    /// <summary>Gets the row-3, column-3 component.</summary>
    public T M33 => Row2.Z;

    /// <summary>Gets the row-3, column-4 component.</summary>
    public T M34 => Row2.W;

    /// <summary>Gets the row-4, column-1 component.</summary>
    public T M41 => Row3.X;

    /// <summary>Gets the row-4, column-2 component.</summary>
    public T M42 => Row3.Y;

    /// <summary>Gets the row-4, column-3 component.</summary>
    public T M43 => Row3.Z;

    /// <summary>Gets the row-4, column-4 component.</summary>
    public T M44 => Row3.W;

    /// <summary>Gets the first column.</summary>
    public Vector4<T> Column0 => new(Row0.X, Row1.X, Row2.X, Row3.X);

    /// <summary>Gets the second column.</summary>
    public Vector4<T> Column1 => new(Row0.Y, Row1.Y, Row2.Y, Row3.Y);

    /// <summary>Gets the third column.</summary>
    public Vector4<T> Column2 => new(Row0.Z, Row1.Z, Row2.Z, Row3.Z);

    /// <summary>Gets the fourth column.</summary>
    public Vector4<T> Column3 => new(Row0.W, Row1.W, Row2.W, Row3.W);

    /// <summary>
    /// Creates a matrix from four rows.
    /// </summary>
    /// <param name="row0">The first row.</param>
    /// <param name="row1">The second row.</param>
    /// <param name="row2">The third row.</param>
    /// <param name="row3">The fourth row.</param>
    /// <returns>The matrix.</returns>
    public static Matrix4<T> FromRows(Vector4<T> row0, Vector4<T> row1, Vector4<T> row2, Vector4<T> row3) =>
        new(row0, row1, row2, row3);

    /// <summary>
    /// Creates a matrix from four columns.
    /// </summary>
    /// <param name="column0">The first column.</param>
    /// <param name="column1">The second column.</param>
    /// <param name="column2">The third column.</param>
    /// <param name="column3">The fourth column.</param>
    /// <returns>The matrix.</returns>
    public static Matrix4<T> FromColumns(
        Vector4<T> column0,
        Vector4<T> column1,
        Vector4<T> column2,
        Vector4<T> column3) =>
        new(
            new Vector4<T>(column0.X, column1.X, column2.X, column3.X),
            new Vector4<T>(column0.Y, column1.Y, column2.Y, column3.Y),
            new Vector4<T>(column0.Z, column1.Z, column2.Z, column3.Z),
            new Vector4<T>(column0.W, column1.W, column2.W, column3.W));

    /// <summary>
    /// Gets the transpose.
    /// </summary>
    public Matrix4<T> Transpose() =>
        new(Column0, Column1, Column2, Column3);

    /// <summary>
    /// Gets the determinant.
    /// </summary>
    public T Determinant
    {
        get
        {
            var determinant = T.Zero;
            for (var column = 0; column < 4; column++)
            {
                var sign = (column & 1) == 0 ? T.One : -T.One;
                determinant += sign * At(0, column) * Minor(0, column).Determinant;
            }

            return determinant;
        }
    }

    /// <summary>
    /// Gets the infinity norm (maximum absolute row sum).
    /// </summary>
    public T InfinityNorm
    {
        get
        {
            var first = T.Abs(Row0.X) + T.Abs(Row0.Y) + T.Abs(Row0.Z) + T.Abs(Row0.W);
            var second = T.Abs(Row1.X) + T.Abs(Row1.Y) + T.Abs(Row1.Z) + T.Abs(Row1.W);
            var third = T.Abs(Row2.X) + T.Abs(Row2.Y) + T.Abs(Row2.Z) + T.Abs(Row2.W);
            var fourth = T.Abs(Row3.X) + T.Abs(Row3.Y) + T.Abs(Row3.Z) + T.Abs(Row3.W);
            return T.Max(T.Max(first, second), T.Max(third, fourth));
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
    public Vector4<T> Transform(Vector4<T> vector) =>
        new(
            Vector4<T>.Dot(Row0, vector),
            Vector4<T>.Dot(Row1, vector),
            Vector4<T>.Dot(Row2, vector),
            Vector4<T>.Dot(Row3, vector));

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="right">The right matrix.</param>
    /// <returns>The matrix product.</returns>
    public static Matrix4<T> operator *(Matrix4<T> left, Matrix4<T> right)
    {
        var right0 = right.Column0;
        var right1 = right.Column1;
        var right2 = right.Column2;
        var right3 = right.Column3;
        return new Matrix4<T>(
            MultiplyRow(left.Row0, right0, right1, right2, right3),
            MultiplyRow(left.Row1, right0, right1, right2, right3),
            MultiplyRow(left.Row2, right0, right1, right2, right3),
            MultiplyRow(left.Row3, right0, right1, right2, right3));
    }

    /// <summary>
    /// Computes the inverse.
    /// </summary>
    /// <returns>The inverse matrix.</returns>
    /// <exception cref="InvalidOperationException">The determinant is zero.</exception>
    public Matrix4<T> Inverse()
    {
        var determinant = Determinant;
        if (determinant == T.Zero)
        {
            throw new InvalidOperationException("A singular matrix has no inverse.");
        }

        var inverseDeterminant = T.One / determinant;
        return new Matrix4<T>(
            new Vector4<T>(
                Cofactor(0, 0) * inverseDeterminant,
                Cofactor(1, 0) * inverseDeterminant,
                Cofactor(2, 0) * inverseDeterminant,
                Cofactor(3, 0) * inverseDeterminant),
            new Vector4<T>(
                Cofactor(0, 1) * inverseDeterminant,
                Cofactor(1, 1) * inverseDeterminant,
                Cofactor(2, 1) * inverseDeterminant,
                Cofactor(3, 1) * inverseDeterminant),
            new Vector4<T>(
                Cofactor(0, 2) * inverseDeterminant,
                Cofactor(1, 2) * inverseDeterminant,
                Cofactor(2, 2) * inverseDeterminant,
                Cofactor(3, 2) * inverseDeterminant),
            new Vector4<T>(
                Cofactor(0, 3) * inverseDeterminant,
                Cofactor(1, 3) * inverseDeterminant,
                Cofactor(2, 3) * inverseDeterminant,
                Cofactor(3, 3) * inverseDeterminant));
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"[{Row0}; {Row1}; {Row2}; {Row3}]");

    private T At(int row, int column)
    {
        var value = row switch
        {
            0 => Row0,
            1 => Row1,
            2 => Row2,
            _ => Row3,
        };

        return column switch
        {
            0 => value.X,
            1 => value.Y,
            2 => value.Z,
            _ => value.W,
        };
    }

    private Matrix3<T> Minor(int skipRow, int skipColumn)
    {
        var rows = new int[3];
        var columns = new int[3];
        var rowIndex = 0;
        var columnIndex = 0;
        for (var index = 0; index < 4; index++)
        {
            if (index != skipRow)
            {
                rows[rowIndex++] = index;
            }

            if (index != skipColumn)
            {
                columns[columnIndex++] = index;
            }
        }

        return new Matrix3<T>(
            new Vector3<T>(At(rows[0], columns[0]), At(rows[0], columns[1]), At(rows[0], columns[2])),
            new Vector3<T>(At(rows[1], columns[0]), At(rows[1], columns[1]), At(rows[1], columns[2])),
            new Vector3<T>(At(rows[2], columns[0]), At(rows[2], columns[1]), At(rows[2], columns[2])));
    }

    private T Cofactor(int row, int column)
    {
        var sign = ((row + column) & 1) == 0 ? T.One : -T.One;
        return sign * Minor(row, column).Determinant;
    }

    private static Vector4<T> MultiplyRow(
        Vector4<T> row,
        Vector4<T> right0,
        Vector4<T> right1,
        Vector4<T> right2,
        Vector4<T> right3) =>
        new(
            Vector4<T>.Dot(row, right0),
            Vector4<T>.Dot(row, right1),
            Vector4<T>.Dot(row, right2),
            Vector4<T>.Dot(row, right3));
}
