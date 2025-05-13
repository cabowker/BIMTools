using System.Windows;

namespace ValorVDC_BIMTools.Commands.Views;

public partial class WallSleevesView : Window
{
    public WallSleevesView(WallSleevesView viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += DialogResult = viewModel.DialogResult;
    }
}