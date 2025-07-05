using System.Windows;
using System.Windows.Input;
using ValorVDC_BIMTools.Commands.WallSleeveRound.ViewModels;

namespace ValorVDC_BIMTools.Commands.WallSleeveRound.Views;

public partial class WallSleevesView : Window
{
    private readonly WallSleeveViewModel _viewModel;
    private Action _closeHandler;


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