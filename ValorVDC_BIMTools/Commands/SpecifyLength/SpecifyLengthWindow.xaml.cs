using System.Windows;
using System.Windows.Controls;

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
            // If a radio button is selected and no manual input, use radio button value
            if (selectedRadioButton != null &&
                string.IsNullOrWhiteSpace(InputLengthFeet.Text) &&
                string.IsNullOrWhiteSpace(InputLengthInches.Text))
                if (double.TryParse(selectedRadioButton.Tag?.ToString(), out var presetValue) && presetValue > 0)
                {
                    SpecifiedLength = presetValue;
                    DialogResult = true;
                    Close();
                    return;
                }

            double feet = 0;
            if (!string.IsNullOrWhiteSpace(InputLengthFeet.Text))
                if (!double.TryParse(InputLengthFeet.Text.Trim(), out feet) || feet < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative number for feet.", "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    InputLengthFeet.Focus();
                    return;
                }


            double inches = 0;
            if (!string.IsNullOrWhiteSpace(InputLengthInches.Text))
                if (!double.TryParse(InputLengthInches.Text.Trim(), out inches) || inches < 0 || inches >= 12)
                {
                    MessageBox.Show("Please enter a valid number for inches (0-11.99).", "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    InputLengthInches.Focus();
                    return;
                }

            if (feet == 0 && inches == 0 && selectedRadioButton == null)
            {
                MessageBox.Show("Please enter a length or select a preset value.", "No Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Calculate total length in feet
            var totalLengthInFeet = feet + inches / 12.0;

            // Validate the total length is reasonable
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
        }
    }

    private RadioButton FindSelectedRadioButton()
    {
        if (Length5Feet.IsChecked == true) return Length5Feet;
        if (Length10Feet.IsChecked == true) return Length10Feet;
        if (Length20Feet.IsChecked == true) return Length20Feet;
        if (Length21Feet.IsChecked == true) return Length21Feet;
        return null;
    }

    private void InputLengthFeet_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputLengthFeet.Text))
            InputLengthInches.Text = string.Empty;
    }

    private void InputLengthInches_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputLengthInches.Text))
            InputLengthFeet.Text = string.Empty;
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