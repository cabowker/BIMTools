using System.Collections.ObjectModel;
using Autodesk.Revit.UI;

namespace ValorVDC_BIMTools.Commands.WallSleeve.ViewModels;

public sealed partial class WallSleeveViewModel : ObservableObject
{
    private readonly Document _document;
    private readonly UIApplication _uiApplication;
    private readonly UIDocument _uiDocument;
    private ObservableCollection<FamilySymbol> _wallSleeveSymbols;
    private FamilySymbol _selectedWallSleeve;
    private string _statusMessage = "Read to place wall Sleeve.";

    public WallSleeveViewModel(ExternalCommandData commandData)
    {
        _uiApplication = commandData.Application;
        _uiDocument = _uiApplication.ActiveUIDocument;
        _document = _uiDocument.Document;

        PlaceWallSleeveCommand = new RelayCommand(() =>
        {
            DialogResult = true;
            RequestClose?.Invoke();
        }, CanPlaceWallSleeve);

        CancelCommand = new RelayCommand(() =>
        {
            DialogResult = false;
            RequestClose?.Invoke();
        });
        
        GetElementsByPartTypeAndSubType();
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<FamilySymbol> WallSleeveSymbols
    {
        get => _wallSleeveSymbols;
        set => SetProperty(ref _wallSleeveSymbols, value);
    }
    
    public FamilySymbol SelectedWallSleeve
    {
        get => _selectedWallSleeve;
        set => SetProperty(ref _selectedWallSleeve, value);
    }
    
    
    public RelayCommand PlaceWallSleeveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public bool DialogResult { get; private set; }
    
    public event Action RequestClose;

    private Parameter GetParameterCaseInsenitive(Element element, string parameterName)
    {
        var parameters = element.Parameters;

        foreach (Parameter parameter in parameters)
        {
            if (string.Equals(parameter.Definition.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                return parameter;
        }

        return null;
    }

    private bool CanPlaceWallSleeve()

    {
        return SelectedWallSleeve != null;
    }
}

