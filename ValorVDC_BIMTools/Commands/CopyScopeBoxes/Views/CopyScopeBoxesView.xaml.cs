using System.Windows;
using ValorVDC_BIMTools.Commands.CopyScopeBoxes.ViewModels;

namespace ValorVDC_BIMTools.Commands.CopyScopeBoxes.Views;

public partial class CopyScopeBoxesView : Window
{
    public CopyScopeBoxesView(Document document)
    {
        InitializeComponent();
        DataContext = new ScopeBoxManagerViewModel(document, this);
    }
}


