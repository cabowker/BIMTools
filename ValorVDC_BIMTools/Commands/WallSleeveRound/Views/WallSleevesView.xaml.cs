using System.Windows;
using ValorVDC_BIMTools.Commands.WallSleeveRound.ViewModels;

namespace ValorVDC_BIMTools.Commands.WallSleeveRound.Views;

public partial class WallSleevesView : Window
{
    private readonly WallSleeveViewModel _viewModel;
    private readonly Action _closeHandler;


    public WallSleevesView(WallSleeveViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        _closeHandler = () =>
        {
            DialogResult = _viewModel.DialogResult;
            Close();
        };

        _viewModel.RequestClose += _closeHandler;
    }

    protected override void OnClosed(EventArgs e)
    {

        _viewModel.RequestClose -= _closeHandler;
        base.OnClosed(e);
    }
}