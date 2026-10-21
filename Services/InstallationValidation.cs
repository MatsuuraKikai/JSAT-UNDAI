using System.Globalization;
using System.Text;

namespace JSAT_UNDAI.Services;

/// <summary>Validation for the manual UNDAI position typed in the installation dialog.</summary>
public static class InstallationValidation
{
    public static bool TryParseLatitude(string? text, out double value) =>
        TryParseRange(text, -90.0, 90.0, out value);

    public static bool TryParseLongitude(string? text, out double value) =>
        TryParseRange(text, -180.0, 180.0, out value);

    private static bool TryParseRange(string? text, double min, double max, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // FormKC turns full-width digits, "．" and "－" (typed with a Japanese IME) into ASCII.
        string normalized = text.Trim().Normalize(NormalizationForm.FormKC);

        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        // "NaN" and "Infinity" parse successfully, so reject them explicitly.
        if (!double.IsFinite(parsed) || parsed < min || parsed > max)
        {
            return false;
        }

        value = parsed;
        return true;
    }
}
