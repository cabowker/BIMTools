using System.Windows;
using ValorVDC_BIMTools.Commands.WallSleeveRectangular.ViewModels;

namespace ValorVDC_BIMTools.Commands.WallSleeveRectangular.Views;

public partial class RectangularWallSleeveView : Window
{
    public RectangularWallSleeveView(RectangularWallSleeveViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        AddToHeight.Text = viewModel.AddToHeight.ToString();
        AddToWidth.Text = viewModel.AddToWidth.ToString();

        RoundQuaterInch.IsChecked = true;
        
        RoundQuaterInch.Checked += (s, e) => viewModel.RoundUpValue = 0.25;
        RoundHalfInch.Checked += (s, e) => viewModel.RoundUpValue = 0.5;
        RoundOneInch.Checked += (s, e) => viewModel.RoundUpValue = 1.0;

        AddToHeight.TextChanged += (s, e ) =>
        {
            if (double.TryParse(AddToHeight.Text, out double value))
                viewModel.AddToHeight = value;
        };
        
        AddToWidth.TextChanged += (s, e ) =>
        {
            if (double.TryParse(AddToWidth.Text, out double value))
                viewModel.AddToWidth = value;
        };
        

        viewModel.RequestClose += () => DialogResult = viewModel.DialogResult;
    }


}