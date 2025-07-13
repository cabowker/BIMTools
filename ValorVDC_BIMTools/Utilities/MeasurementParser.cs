using System.Text.RegularExpressions;

namespace ValorVDC_BIMTools.Utilities;

/// <summary>
///     Utility class for parsing various measurement formats including fractions and architectural notation
/// </summary>
public class MeasurementParser
{
    /// <summary>
    ///     Parses fractional inches input like "3.5", "1/2", "3 1/2"
    /// </summary>
    /// <param name="input">Input string to parse</param>
    /// <returns>Parsed value in inches</returns>
    /// <exception cref="ArgumentException">Thrown when input format is invalid</exception>
    public static double ParseFractionalInches(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        var originalInput = input;
        input = input.Trim();

        if (input.EndsWith("\""))
            input = input.Substring(0, input.Length - 1).Trim();

        if (string.IsNullOrWhiteSpace(input))
            return 0;

        try
        {
            // Try parsing as a simple decimal/whole number first
            if (double.TryParse(input, out var decimalValue)) return decimalValue;

            // Pattern to match fractions
            // This handles: "1/2", "3/4", "3 1/2", "5 3/4", etc.
            var fractionPattern = @"^(\d+)?\s*(\d+)/(\d+)$";
            var match = Regex.Match(input, fractionPattern);

            if (match.Success)
            {
                double wholeNumber = 0;
                double numerator = 0;
                double denominator = 1;

                // Parse whole number part (optional)
                if (!string.IsNullOrEmpty(match.Groups[1].Value)) wholeNumber = double.Parse(match.Groups[1].Value);

                // Parse numerator and denominator (required for fractions)
                numerator = double.Parse(match.Groups[2].Value);
                denominator = double.Parse(match.Groups[3].Value);

                if (denominator == 0)
                    throw new ArgumentException("Denominator cannot be zero");

                return wholeNumber + numerator / denominator;
            }


            // If we get here, the input doesn't match any expected pattern
            throw new ArgumentException(
                $"Invalid format: '{originalInput}'. Expected: whole numbers (3), decimals (3.5), fractions (1/2), mixed fractions (3 1/2), or any of these with inch symbol (3\", 1/2\")");
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            // Catch any unexpected exceptions and wrap them
            throw new ArgumentException($"Error parsing '{originalInput}': {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Parses architectural length formats like "6'-3 1/2\"", "12'-6\"", "5'"
    /// </summary>
    /// <param name="input">Input string to parse</param>
    /// <returns>Tuple containing feet and inches values</returns>
    /// <exception cref="ArgumentException">Thrown when input format is invalid</exception>
    public static (double feet, double ninches) ParseArchitecturalLength(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (0, 0);

        input = input.Trim();

        if (double.TryParse(input, out var decimalFeet))
            return (decimalFeet, decimalFeet);
        //return (decimalFeet, 0);

        if (input.Contains("'"))
        {
            // Pattern 1: Traditional format with hyphen: "6'-3 1/2\"", "12'-6\"", "5'-0\""
            var traditionalPattern = @"^(\d+(?:\.\d+)?)'(?:-(\d+(?:\s+\d+/\d+)?(?:\.\d+)?)""?)?$";
            var traditionalMatch = Regex.Match(input, traditionalPattern);


            if (traditionalMatch.Success)
            {
                var feet = double.Parse(traditionalMatch.Groups[1].Value);
                double inches = 0;
                if (!string.IsNullOrEmpty(traditionalMatch.Groups[2].Value))
                    try
                    {
                        inches = ParseFractionalInches(traditionalMatch.Groups[2].Value);
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid inches format in architectural measurement");
                    }

                return (feet, inches);
            }

            // Pattern 2: Compact format without hyphen: "1'2", "6'3 1/2", "8'11 7/8"
            var compactPattern = @"^(\d+(?:\.\d+)?)'(\d+(?:\s+\d+/\d+)?(?:\.\d+)?)""?$";
            var compactMatch = Regex.Match(input, compactPattern);

            if (compactMatch.Success)
            {
                var feet = double.Parse(compactMatch.Groups[1].Value);
                double inches = 0;

                try
                {
                    inches = ParseFractionalInches(compactMatch.Groups[2].Value);
                }
                catch
                {
                    throw new ArgumentException("Invalid inches format in compact architectural measurement");
                }

                return (feet, inches);
            }

            // Pattern 3: Just feet with apostrophe: "8'"
            var feetOnlyPattern = @"^(\d+(?:\.\d+)?)'$";
            var feetOnlyMatch = Regex.Match(input, feetOnlyPattern);

            if (feetOnlyMatch.Success)
            {
                var feet = double.Parse(feetOnlyMatch.Groups[1].Value);
                return (feet, 0);
            }
        }

        throw new ArgumentException("Invalid architectural format");
    }

    /// <summary>
    ///     Converts feet and inches to total feet
    /// </summary>
    /// <param name="feet">Feet value</param>
    /// <param name="inches">Inches value</param>
    /// <returns>Total length in feet</returns>
    public static double ConvertToFeet(double feet, double inches)
    {
        return feet + inches / 12.0;
    }

    /// <summary>
    ///     Parses any measurement input and returns the result in feet
    ///     Handles both fractional inches and architectural formats
    /// </summary>
    /// <param name="input">Input string to parse</param>
    /// <param name="isArchitectural">True if input should be treated as architectural format</param>
    /// <returns>Parsed value in feet</returns>
    public static double ParseMeasurement(string input, bool isArchitectural = false)
    {
        if (isArchitectural)
        {
            var (feet, inches) = ParseArchitecturalLength(input);
            return ConvertToFeet(feet, inches);
        }
        else
        {
            var inches = ParseFractionalInches(input);
            return inches / 12.0; // Convert inches to feet
        }
    }

    /// <summary>
    ///     Validates that a measurement value is within reasonable bounds
    /// </summary>
    /// <param name="value">Value to validate (in feet)</param>
    /// <param name="minValue">Minimum allowed value</param>
    /// <param name="maxValue">Maximum allowed value</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidMeasurement(double value, double minValue = 0, double maxValue = 1000)
    {
        return value >= minValue && value <= maxValue;
    }

    /// <summary>
    ///     Gets example formats for user help messages
    /// </summary>
    /// <param name="isArchitectural">True to get architectural examples</param>
    /// <returns>String with example formats</returns>
    public static string GetExampleFormats(bool isArchitectural = false)
    {
        if (isArchitectural)
            return "Examples: '6'-3 1/2\"', '12'-6\"', '5'', '8'-0\"'";
        return "Examples: '3.5', '1/2', '3 1/2', '7/8'";
    }
}