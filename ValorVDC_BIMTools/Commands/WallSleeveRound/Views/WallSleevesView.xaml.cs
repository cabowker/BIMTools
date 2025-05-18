using System.Windows;
using ValorVDC_BIMTools.Commands.WallSleeve.ViewModels;

namespace ValorVDC_BIMTools.Commands.WallSleeve.Views;

public partial class WallSleevesView : Window
{
    public WallSleevesView(WallSleeveViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += () => DialogResult = viewModel.DialogResult;
    }
}