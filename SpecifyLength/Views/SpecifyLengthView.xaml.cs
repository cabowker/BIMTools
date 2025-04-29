using System.Windows;
using System.Windows.Controls;
using SpecifyLength.ViewModels;

namespace SpecifyLength.Views;

public partial class SpecifyLengthView : Window
{
    public double? SpecifiedLength { get; set; }

    public SpecifyLengthView(SpecifyLengthViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
