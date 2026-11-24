// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Pure helpers for the proportional shell layout. The shell persists fractions, never pixels, so this type
/// owns the persisted-range clamp and the conversion from measured track sizes after a completed gesture.
/// </summary>
internal static class ShellLayoutFractions
{
    /// <summary>
    /// Normalizes a side-panel fraction read from settings. A non-finite or out-of-range value falls back to the
    /// supplied default, matching the tolerant settings reader.
    /// </summary>
    /// <param name="value">The candidate fraction.</param>
    /// <param name="fallback">The default fraction for the panel.</param>
    /// <returns>The valid fraction or the fallback.</returns>
    public static double NormalizeSide(double value, double fallback) =>
        double.IsFinite(value) && value >= SettingsDefaults.MinSideFraction && value <= SettingsDefaults.MaxSideFraction
            ? value
            : fallback;

    /// <summary>
    /// Normalizes the console fraction read from settings. A non-finite or out-of-range value falls back to the
    /// default.
    /// </summary>
    /// <param name="value">The candidate fraction.</param>
    /// <returns>The valid fraction or the default.</returns>
    public static double NormalizeConsole(double value) =>
        double.IsFinite(value)
        && value >= SettingsDefaults.MinConsoleFraction
        && value <= SettingsDefaults.MaxConsoleFraction
            ? value
            : SettingsDefaults.DefaultConsoleFraction;

    /// <summary>
    /// Clamps a runtime side-panel fraction to the persisted range. Non-finite input falls back to the panel's
    /// default.
    /// </summary>
    /// <param name="value">The candidate fraction.</param>
    /// <param name="fallback">The default fraction for the panel.</param>
    /// <returns>The clamped fraction.</returns>
    public static double ClampSide(double value, double fallback) =>
        double.IsFinite(value)
            ? Math.Clamp(value, SettingsDefaults.MinSideFraction, SettingsDefaults.MaxSideFraction)
            : fallback;

    /// <summary>
    /// Clamps a runtime console fraction to the persisted range. Non-finite input falls back to the default.
    /// </summary>
    /// <param name="value">The candidate fraction.</param>
    /// <returns>The clamped fraction.</returns>
    public static double ClampConsole(double value) =>
        double.IsFinite(value)
            ? Math.Clamp(value, SettingsDefaults.MinConsoleFraction, SettingsDefaults.MaxConsoleFraction)
            : SettingsDefaults.DefaultConsoleFraction;

    /// <summary>
    /// Clamps the editor selection-pane share to its persisted range. Non-finite input falls back to the default.
    /// </summary>
    /// <param name="value">The candidate fraction.</param>
    /// <returns>The clamped fraction.</returns>
    public static double ClampEditor(double value) =>
        double.IsFinite(value)
            ? Math.Clamp(value, SettingsDefaults.MinEditorFraction, SettingsDefaults.MaxEditorFraction)
            : SettingsDefaults.DefaultEditorFraction;

    /// <summary>
    /// Normalizes the editor selection-pane share read from settings. An out-of-range value falls back to the
    /// default, matching the tolerant settings reader.
    /// </summary>
    /// <param name="value">The candidate fraction.</param>
    /// <returns>The valid fraction or the default.</returns>
    public static double NormalizeEditor(double value) =>
        double.IsFinite(value) && value >= SettingsDefaults.MinEditorFraction && value <= SettingsDefaults.MaxEditorFraction
            ? value
            : SettingsDefaults.DefaultEditorFraction;

    /// <summary>
    /// Gets the centre column share for the supplied side fractions.
    /// </summary>
    /// <param name="left">The left fraction.</param>
    /// <param name="right">The right fraction.</param>
    /// <returns>The non-negative centre share.</returns>
    public static double Centre(double left, double right) => Math.Max(0d, 1d - left - right);

    /// <summary>
    /// Gets the documents row share for the supplied console fraction.
    /// </summary>
    /// <param name="console">The console fraction.</param>
    /// <returns>The non-negative documents share.</returns>
    public static double Documents(double console) => Math.Max(0d, 1d - console);

    /// <summary>
    /// Converts a measured pixel size into a share of the measured total.
    /// </summary>
    /// <param name="pixels">The measured panel extent.</param>
    /// <param name="total">The measured extents of the visible panels.</param>
    /// <param name="fallback">The current fraction used when the measurement is degenerate.</param>
    /// <returns>The share, or the fallback for a degenerate measurement.</returns>
    public static double Share(double pixels, double total, double fallback) =>
        double.IsFinite(pixels) && double.IsFinite(total) && total > 0d && pixels > 0d
            ? Math.Clamp(pixels / total, 0d, 1d)
            : fallback;

    /// <summary>
    /// Divides a legacy pixel value by the design extent and clamps it to a persisted range.
    /// </summary>
    /// <param name="pixels">The legacy pixel value.</param>
    /// <param name="designExtent">The 1200 or 800 design extent.</param>
    /// <param name="minimum">The persisted minimum.</param>
    /// <param name="maximum">The persisted maximum.</param>
    /// <param name="fallback">The fallback for a non-finite pixel value.</param>
    /// <returns>The migrated fraction.</returns>
    public static double FromLegacyPixels(
        double pixels,
        double designExtent,
        double minimum,
        double maximum,
        double fallback) =>
        double.IsFinite(pixels) && designExtent > 0d
            ? Math.Clamp(pixels / designExtent, minimum, maximum)
            : fallback;
}
