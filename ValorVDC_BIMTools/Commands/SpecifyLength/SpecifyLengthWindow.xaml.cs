using System.Windows;
using System.Windows.Controls;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

public partial class SpecifyLengthWindow : Window
{
    public double? SpecifiedLength { get; private set; }
    public SpecifyLengthWindow(double currentLength)
    {
        InitializeComponent();
        InputLengthFeet.Text = currentLength.ToString();
    }
    

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        double feet = 0;
        if (!string.IsNullOrWhiteSpace(InputLengthFeet.Text))
        {
            if (!double.TryParse(InputLengthFeet.Text, out feet) || feet < 0)
            {
                MessageBox.Show("Please enter a valid non-negative number for feet.");
                return;
            }
        }

        double inches = 0;
        if (!string.IsNullOrWhiteSpace((InputLengthInches.Text)))
        {
            if (!double.TryParse(InputLengthInches.Text, out inches) || inches < 0)
            {
                MessageBox.Show("Please enter a valid non-negative number for inches.");
                return;
            }
        }

        double totalLengthInFeet = feet + (inches / 12);
        SpecifiedLength = totalLengthInFeet;

        this.DialogResult = true;
        this.Close();
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