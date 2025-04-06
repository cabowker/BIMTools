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
}