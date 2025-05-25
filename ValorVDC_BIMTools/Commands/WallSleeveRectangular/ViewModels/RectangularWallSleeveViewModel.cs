using System.Collections.ObjectModel;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

namespace ValorVDC_BIMTools.Commands.WallSleeveRectangular.ViewModels;

public class RectangularWallSleeveViewModel : ObservableObject
{
    private readonly UIApplication _uiApplication;
    private readonly UIDocument _uiDocument;
    private readonly Document _document;
    private ObservableCollection<FamilySymbol> _wallSleeveSymbols;
    private FamilySymbol _selectedWallSleeve;
    private string _statusMeassage = " Ready to Place Rectangular Wall Sleeve.";
    private double _addToHeight = 2.0;
    private double _addToWidth = 2.0;
    private double _roundUpValue = .25;

    public RectangularWallSleeveViewModel(ExternalCommandData commandData)
    {
        _uiApplication = commandData.Application;
        _uiDocument = _uiApplication.ActiveUIDocument;
        _document = _uiDocument.Document;

        PlaceWallSleeveCommand = new RelayCommand(() =>
        {
            DialogResult = true;
            RequestClose?.Invoke();
        }, CanPlaceSleeve);

        CancelCommand = new RelayCommand(() =>
        {
            DialogResult = false;
            RequestClose?.Invoke();
        });

        GetElementsByPartTypeAndSubType();
    }

    public void GetElementsByPartTypeAndSubType(string partType = "Sleeve", string partSubType = "Wall Sleeve-Rectangular")
    {
        try
        {
            StatusMessage = "Loading Rectangular Wall Sleeve Families...";

            var wallSleeves = GetElements.GetElementByPartTypeAndPartSubType(_document, partType, partSubType);
            WallSleeveSymbols = new ObservableCollection<FamilySymbol>(wallSleeves);
            if (WallSleeveSymbols.Count > 0)
                SelectedWallSleeve = WallSleeveSymbols[0];
            else
                StatusMessage = "No wall sleeve families found. Please load a rectangular wall sleeve family first.";

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }



    public string StatusMessage
    {
        get => _statusMeassage;
        set => SetProperty(ref _statusMeassage, value);
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
    
    public double AddToHeight
    {
        get => _addToHeight;
        set => SetProperty(ref _addToHeight, value);
    }
    
    public double AddToWidth
    {
        get => _addToWidth;
        set => SetProperty(ref _addToWidth, value);
    }
    
    public double RoundUpValue
    {
        get => _roundUpValue;
        set => SetProperty(ref _roundUpValue, value);
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

    private bool CanPlaceSleeve()
    {
        return SelectedWallSleeve != null;
    }
}