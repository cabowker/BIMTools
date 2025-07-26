using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ValorVDC_BIMTools.Utilities;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

public partial class SpecifyLengthWindow : Window
{
    public SpecifyLengthWindow()
    {
        InitializeComponent();
    }

    public double? SpecifiedLength { get; private set; }


    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedRadioButton = FindSelectedRadioButton();

            if (selectedRadioButton != null)
            {
                var tagValue = selectedRadioButton.Tag?.ToString();

                if (!string.IsNullOrWhiteSpace(tagValue) && double.TryParse(tagValue, out var presetValue) &&
                    presetValue > 0)
                {
                    SpecifiedLength = presetValue;
                    DialogResult = true;
                    Close();
                    return;
                }

                MessageBox.Show(
                    $"Invalid preset value for {selectedRadioButton.Name} (Tag: {tagValue}). Please contact support or use manual input.",
                    "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double feet = 0;
            double inchesFromFeet = 0;

            if (!string.IsNullOrWhiteSpace(InputLengthFeet.Text))
                try
                {
                    var inputText = InputLengthFeet.Text.Trim();

                    if (double.TryParse(inputText, out var simpleNumber) && !inputText.Contains("'"))
                        inputText = inputText + "'";

                    var parseResult = MeasurementParser.ParseArchitecturalLength(inputText);
                    feet = parseResult.feet;
                    inchesFromFeet = parseResult.ninches;

                    if (feet <= 0)
                    {
                        MessageBox.Show("Please enter a positive number for feet.", "Invalid Input",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        InputLengthFeet.Focus();
                        return;
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(
                        "Please enter a valid length. Examples:\n" +
                        "• Simple: 5, 10.5, 12\n" +
                        "• Architectural: 6'-3 1/2\", 12'-6\", 5'\n\n" +
                        $"Error: {ex.Message}",
                        "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    InputLengthFeet.Focus();
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Unexpected error parsing feet input: {ex.Message}\n\n" +
                        "Please enter a valid length. Examples:\n" +
                        "• Simple: 5, 10.5, 12\n" +
                        "• Architectural: 6'-3 1/2\", 12'-6\", 5'\n" +
                        "• Using no inch-sign at the end of the measurement",
                        "Input Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    InputLengthFeet.Focus();
                    return;
                }

            double inches = 0;
            if (!string.IsNullOrWhiteSpace(InputLengthInches.Text))
                try
                {
                    inches = MeasurementParser.ParseFractionalInches(InputLengthInches.Text);
                    if (inches < 0)
                    {
                        MessageBox.Show(
                            $"Please enter a positive number for inches. {MeasurementParser.GetExampleFormats()}",
                            "Invalid Input",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        InputLengthInches.Focus();
                        return;
                    }

                    if (inches >= 252) // 21' feet worth of inches
                    {
                        var result = MessageBox.Show(
                            $"The specified inches value ({inches} inches = {inches / 12:F2} feet) seems very large. Continue?",
                            "Large Value Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                        {
                            InputLengthInches.Focus();
                            return;
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(
                        $"Invalid inches input: {ex.Message}\n\n{MeasurementParser.GetExampleFormats()}",
                        "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    InputLengthInches.Focus();
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error parsing inches: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    InputLengthInches.Focus();
                    return;
                }

            if (inchesFromFeet > 0 && inches > 0)
            {
                MessageBox.Show(
                    "Please use either the architectural format (e.g., '6'-3 1/2\"') in the feet field OR separate feet and inches fields, not both.",
                    "Conflicting Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var totalInches = inchesFromFeet > 0 ? inchesFromFeet : inches;

            Debug.WriteLine(
                $"Debug: feet={feet}, inchesFromFeet={inchesFromFeet}, inches={inches}, totalInches={totalInches}");

            if (feet == 0 && totalInches == 0 && selectedRadioButton == null)
            {
                MessageBox.Show("Please enter a length or select a preset value.", "No Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var totalLengthInFeet = feet + totalInches / 12.0;

            if (totalLengthInFeet <= 0)
            {
                MessageBox.Show("The total length must be greater than 0.", "Invalid Length",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (totalLengthInFeet > 1000) // Reasonable upper limit
            {
                var result = MessageBox.Show(
                    $"The specified length ({totalLengthInFeet:F2} feet) seems very large. Continue?",
                    "Large Length Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            SpecifiedLength = totalLengthInFeet;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while processing the input: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
        }
    }

    private RadioButton FindSelectedRadioButton()
    {
        try
        {
            if (Length5Feet?.IsChecked == true) return Length5Feet;
            if (Length10Feet?.IsChecked == true) return Length10Feet;
            if (Length20Feet?.IsChecked == true) return Length20Feet;
            if (Length21Feet?.IsChecked == true) return Length21Feet;
            return null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error checking radio buttons: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private void InputLengthFeet_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputLengthFeet.Text))
        {
            InputLengthInches.Text = string.Empty;
            ClearRadioButtons();
            ValidateFeetInput();
        }
    }

    private void ValidateFeetInput()
    {
        if (string.IsNullOrWhiteSpace(InputLengthFeet.Text))
        {
            InputLengthFeet.Background = Brushes.White;
            return;
        }

        try
        {
            var (feet, inches) = MeasurementParser.ParseArchitecturalLength(InputLengthFeet.Text);
            if (feet >= 0 && inches >= 0)
                InputLengthFeet.Background = Brushes.LightBlue;
            else
                InputLengthFeet.Background = Brushes.LightPink;
        }
        catch
        {
            InputLengthFeet.Background = Brushes.LightSalmon;
        }
    }

    private void InputLengthInches_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputLengthInches.Text))
        {
            InputLengthFeet.Text = string.Empty;
            ClearRadioButtons();
        }
    }

    private void ClearRadioButtons()
    {
        Length5Feet.IsChecked = false;
        Length10Feet.IsChecked = false;
        Length20Feet.IsChecked = false;
        Length21Feet.IsChecked = false;
    }

    private void PresetLength_Checked(object sender, RoutedEventArgs e)
    {
        InputLengthFeet.Text = string.Empty;
        InputLengthInches.Text = string.Empty;
    }

    private void CustomLength_Checked(object sender, RoutedEventArgs e)
    {
        InputLengthFeet.Focus();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}