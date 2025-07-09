using System.Windows;
using ValorVDC_BIMTools.Commands.WallSleeveRectangular.ViewModels;

namespace ValorVDC_BIMTools.Commands.WallSleeveRectangular.Views;

public partial class RectangularWallSleeveView : Window
{
    private readonly RectangularWallSleeveViewModel _viewModel;
    private Action _closeHandler;
    public RectangularWallSleeveView(RectangularWallSleeveViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        AddToHeight.Text = viewModel.AddToHeight.ToString();
        AddToWidth.Text = viewModel.AddToWidth.ToString();

        RoundQuaterInch.IsChecked = true;

        RoundQuaterInch.Checked += (s, e) => viewModel.RoundUpValue = 0.25;
        RoundHalfInch.Checked += (s, e) => viewModel.RoundUpValue = 0.5;
        RoundOneInch.Checked += (s, e) => viewModel.RoundUpValue = 1.0;

        AddToHeight.TextChanged += (s, e) =>
        {
            if (double.TryParse(AddToHeight.Text, out var value))
                viewModel.AddToHeight = value;
        };

        AddToWidth.TextChanged += (s, e) =>
        {
            if (double.TryParse(AddToWidth.Text, out var value))
                viewModel.AddToWidth = value;
        };

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