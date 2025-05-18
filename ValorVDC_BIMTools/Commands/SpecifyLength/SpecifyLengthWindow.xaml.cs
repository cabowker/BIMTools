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
        var selectedRadioButton = FindSelectedRadioButton();
        if (string.IsNullOrWhiteSpace(InputLengthFeet.Text) && selectedRadioButton != null)
            InputLengthFeet.Text = selectedRadioButton.Tag.ToString();

        
        double feet = 0;
        if (!string.IsNullOrWhiteSpace(InputLengthFeet.Text))
            if (!double.TryParse(InputLengthFeet.Text, out feet) || feet < 0)
            {
                MessageBox.Show("Please enter a valid non-negative number for feet.");
                return;
            }

        double inches = 0;
        if (!string.IsNullOrWhiteSpace(InputLengthInches.Text))
            if (!double.TryParse(InputLengthInches.Text, out inches) || inches < 0)
            {
                MessageBox.Show("Please enter a valid non-negative number for inches.");
                return;
            }

        var totalLengthInFeet = feet + inches / 12;
        SpecifiedLength = totalLengthInFeet;

        DialogResult = true;
        Close();
    }

    private RadioButton FindSelectedRadioButton()
    {
        if (Length5Feet.IsChecked  == true) return Length5Feet;
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
}