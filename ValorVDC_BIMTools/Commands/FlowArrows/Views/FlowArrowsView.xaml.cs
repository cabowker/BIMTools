using System.Windows;
using ValorVDC_BIMTools.Commands.FlowArrows.ViewModels;

namespace FlowArrows.Views;

public sealed partial class FlowArrowsView : Window
{
    private readonly FlowArrowsViewModel _viewModel;

    public FlowArrowsView(FlowArrowsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.RequestClose += () =>
        {
            DialogResult = _viewModel.DialogResult;
            Close();
        };
    }

    private void OnSelectionComplete()
    {
        Hide();
        Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.RequestClose -= () => Close();

        base.OnClosed(e);
    }
}