using System.Windows;
using ValorVDC_BIMTools.Commands.FloorSleevesRound.ViewModels;

namespace ValorVDC_BIMTools.Commands.FloorSleevesRound.Views;

public partial class FloorSleeveView : Window
{
    private readonly FloorSleeveViewModel _viewModel;
    private Action _closeHandler;

    public FloorSleeveView(FloorSleeveViewModel viewModel)
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

    private void OnSelectionComplete()
    {
        Hide();
        Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Properly unsubscribe using the stored reference
        if (_closeHandler != null)
        {
            _viewModel.RequestClose -= _closeHandler;
            _closeHandler = null;
        }

        base.OnClosed(e);
    }
}