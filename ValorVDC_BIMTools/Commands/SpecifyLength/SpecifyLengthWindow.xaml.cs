using System.Windows;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

public partial class SpecifyLengthWindow : Window
{
    public double? SpecifiedLength { get; private set; }
    public SpecifyLengthWindow(double currentLength)
    {
        InitializeComponent();
        InputLength.Text = currentLength.ToString("F2");
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(InputLength.Text, out double specificedLength) && specificedLength > 0)
        {
            SpecifiedLength = specificedLength;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please enter a valid positive number.", "Invalid Input", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}