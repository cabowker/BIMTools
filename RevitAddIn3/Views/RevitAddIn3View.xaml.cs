using RevitAddIn3.ViewModels;

namespace RevitAddIn3.Views;

public sealed partial class RevitAddIn3View
{
    public RevitAddIn3View(RevitAddIn3ViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}