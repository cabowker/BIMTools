using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

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

    public void GetElementsByPartTypeAndSubType(string partType = "Sleeve", string partSubType = "Wall Sleeve")
    {
        try
        {
            StatusMessage = "Please select a Wall Sleeve Part Type";
        
            // Use the helper method
            var wallSleeves = GetElements.GetElementByPartTypeAndPartSubType(_document, partType, partSubType);
        
            WallSleeveSymbols = new ObservableCollection<FamilySymbol>(wallSleeves);

            if (WallSleeveSymbols.Count > 0)
                SelectedWallSleeve = WallSleeveSymbols[0];
            else
                StatusMessage = "No wall Sleeve types found. Please load a wall sleeve family first.";
        }
        catch (Exception e)
        {
            StatusMessage = $"Error Loading Wall Sleeve Family: {e.Message}";
        }
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

