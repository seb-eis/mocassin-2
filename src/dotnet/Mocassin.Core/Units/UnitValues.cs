// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Units;

/// <summary>
/// Describes a scalar value with an explicit physical unit.
/// </summary>
/// <typeparam name="TSelf">The concrete unit type.</typeparam>
public interface IUnitValue<TSelf>
    where TSelf : struct, IUnitValue<TSelf>
{
    /// <summary>
    /// Gets the numeric value in the unit represented by the type.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Creates the unit value and validates its allowed range.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>The validated unit value.</returns>
    public static abstract TSelf Create(double value);

    /// <summary>
    /// Tries to create the unit value without throwing for invalid input.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <param name="result">The validated unit value when successful.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public static abstract bool TryCreate(double value, out TSelf result);
}

/// <summary>
/// A length in ångström.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<LengthAngstrom>))]
public readonly record struct LengthAngstrom : IUnitValue<LengthAngstrom>
{
    /// <summary>
    /// Initializes a new length in ångström.
    /// </summary>
    /// <param name="value">The non-negative finite length.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public LengthAngstrom(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Length must be finite and non-negative.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid length.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite non-negative value.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value >= 0.0;

    /// <inheritdoc />
    public static LengthAngstrom Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out LengthAngstrom result)
    {
        if (IsValid(value))
        {
            result = new LengthAngstrom(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} Å");
}

/// <summary>
/// An energy in electronvolt.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<EnergyEv>))]
public readonly record struct EnergyEv : IUnitValue<EnergyEv>
{
    /// <summary>
    /// Initializes a new energy in electronvolt.
    /// </summary>
    /// <param name="value">The finite energy.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
    public EnergyEv(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Energy must be finite.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid energy.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite value.</returns>
    public static bool IsValid(double value) => double.IsFinite(value);

    /// <inheritdoc />
    public static EnergyEv Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out EnergyEv result)
    {
        if (IsValid(value))
        {
            result = new EnergyEv(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} eV");
}

/// <summary>
/// A temperature in kelvin.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<TemperatureKelvin>))]
public readonly record struct TemperatureKelvin : IUnitValue<TemperatureKelvin>
{
    /// <summary>
    /// Initializes a new temperature in kelvin.
    /// </summary>
    /// <param name="value">The non-negative finite temperature.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public TemperatureKelvin(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Temperature must be finite and non-negative.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid temperature.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite non-negative value.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value >= 0.0;

    /// <inheritdoc />
    public static TemperatureKelvin Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out TemperatureKelvin result)
    {
        if (IsValid(value))
        {
            result = new TemperatureKelvin(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} K");
}

/// <summary>
/// A charge in units of the elementary charge.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<ChargeElementary>))]
public readonly record struct ChargeElementary : IUnitValue<ChargeElementary>
{
    /// <summary>
    /// The smallest accepted charge.
    /// </summary>
    public const double Minimum = -1000.0;

    /// <summary>
    /// The largest accepted charge.
    /// </summary>
    public const double Maximum = 1000.0;

    /// <summary>
    /// Initializes a new charge in elementary-charge units.
    /// </summary>
    /// <param name="value">The finite charge in the inclusive range -1000..1000.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the allowed range.</exception>
    public ChargeElementary(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Charge must be finite and within -1000..1000.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid charge.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite value in the allowed range.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value is >= Minimum and <= Maximum;

    /// <inheritdoc />
    public static ChargeElementary Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out ChargeElementary result)
    {
        if (IsValid(value))
        {
            result = new ChargeElementary(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} e");
}

/// <summary>
/// A time in seconds.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<TimeSeconds>))]
public readonly record struct TimeSeconds : IUnitValue<TimeSeconds>
{
    /// <summary>
    /// Initializes a new time in seconds.
    /// </summary>
    /// <param name="value">The non-negative finite time.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public TimeSeconds(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Time must be finite and non-negative.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid time.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite non-negative value.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value >= 0.0;

    /// <inheritdoc />
    public static TimeSeconds Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out TimeSeconds result)
    {
        if (IsValid(value))
        {
            result = new TimeSeconds(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} s");
}

/// <summary>
/// A frequency in hertz.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<FrequencyHz>))]
public readonly record struct FrequencyHz : IUnitValue<FrequencyHz>
{
    /// <summary>
    /// Initializes a new frequency in hertz.
    /// </summary>
    /// <param name="value">The non-negative finite frequency.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public FrequencyHz(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Frequency must be finite and non-negative.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid frequency.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite non-negative value.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value >= 0.0;

    /// <inheritdoc />
    public static FrequencyHz Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out FrequencyHz result)
    {
        if (IsValid(value))
        {
            result = new FrequencyHz(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} Hz");
}

/// <summary>
/// An electric-field magnitude in volt per metre.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<ElectricFieldVm>))]
public readonly record struct ElectricFieldVm : IUnitValue<ElectricFieldVm>
{
    /// <summary>
    /// Initializes a new electric-field magnitude in volt per metre.
    /// </summary>
    /// <param name="value">The non-negative finite field magnitude.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public ElectricFieldVm(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Electric field must be finite and non-negative.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid electric-field magnitude.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite non-negative value.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value >= 0.0;

    /// <inheritdoc />
    public static ElectricFieldVm Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out ElectricFieldVm result)
    {
        if (IsValid(value))
        {
            result = new ElectricFieldVm(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value} V/m");
}

/// <summary>
/// An angle in degrees.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter<AngleDegrees>))]
public readonly record struct AngleDegrees : IUnitValue<AngleDegrees>
{
    /// <summary>
    /// The smallest accepted angle.
    /// </summary>
    public const double Minimum = 0.0;

    /// <summary>
    /// The largest accepted angle.
    /// </summary>
    public const double Maximum = 360.0;

    /// <summary>
    /// Initializes a new angle in degrees.
    /// </summary>
    /// <param name="value">The finite angle in the inclusive range 0..360.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the allowed range.</exception>
    public AngleDegrees(double value)
    {
        Value = IsValid(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Angle must be finite and within 0..360 degrees.");
    }

    /// <inheritdoc />
    public double Value { get; }

    /// <summary>
    /// Determines whether a numeric value is a valid angle.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite value in the allowed range.</returns>
    public static bool IsValid(double value) => double.IsFinite(value) && value is >= Minimum and <= Maximum;

    /// <inheritdoc />
    public static AngleDegrees Create(double value) => new(value);

    /// <inheritdoc />
    public static bool TryCreate(double value, out AngleDegrees result)
    {
        if (IsValid(value))
        {
            result = new AngleDegrees(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Value}°");
}
