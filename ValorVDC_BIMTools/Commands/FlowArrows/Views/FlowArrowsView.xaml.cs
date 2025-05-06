using System.Windows;
using FlowArrows.ViewModels;

namespace FlowArrows.Views;

public sealed partial class FlowArrowsView : Window
{
    private readonly FlowArrowsViewModel _viewModel;
    public FlowArrowsView(FlowArrowsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        //_viewModel.SelectionComplete += OnSelectionComplete;

        _viewModel.RequestClose += () =>
        {
            this.DialogResult = _viewModel.DialogResult;
            this.Close();

        };
    }

    private void OnSelectionComplete( )
    {
        this.Hide();
        this.Show();
    }
    protected override void OnClosed(EventArgs e)
    {
        // Clean up event handlers
        _viewModel.SelectionComplete -= OnSelectionComplete;
        _viewModel.RequestClose -= () => Close();
        
        base.OnClosed(e);
    }

}