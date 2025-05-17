using System.Collections.ObjectModel;
using Autodesk.Revit.UI;

namespace ValorVDC_BIMTools.Commands.WallSleeve.ViewModels;

public sealed class WallSleeveViewModel : ObservableObject
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
        
        LoadWallSleeveSymbols();
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

    public void LoadWallSleeveSymbols()
    {
        try
        {
            StatusMessage = "Loading Wall Sleeve Families...maybe";

            var wallSleeves = new List<FamilySymbol>();

            var pipeAccessories = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                .Cast<FamilySymbol>()
                .Where(fam =>
                {
                    var partTypeParam = GetParameterCaseInsenitive(fam, "Part Type");
                    var partSubTypeParam = GetParameterCaseInsenitive(fam, "Part Sub-type");
                    return partTypeParam != null && partSubTypeParam != null &&
                           string.Equals(partTypeParam.AsString(), "Sleeve", StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(partSubTypeParam.AsString(), "Wall Sleeve", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            wallSleeves.AddRange(pipeAccessories);
            
            var ductAccessories = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DuctAccessory)
                .Cast<FamilySymbol>()
                .Where(fam =>
                {
                    var partTypeParam = GetParameterCaseInsenitive(fam, "Part Type");
                    var partSubTypeParam = GetParameterCaseInsenitive(fam, "Part Sub-type");
                    return partTypeParam != null && partSubTypeParam != null &&
                           string.Equals(partTypeParam.AsString(), "Sleeve", StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(partSubTypeParam.AsString(), "Wall Sleeve", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            wallSleeves.AddRange(ductAccessories);
            
            var genericModels = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .Cast<FamilySymbol>()
                .Where(fam =>
                {
                    var partTypeParam = GetParameterCaseInsenitive(fam, "Part Type");
                    var partSubTypeParam = GetParameterCaseInsenitive(fam, "Part Sub-type");
                    return partTypeParam != null && partSubTypeParam != null &&
                           string.Equals(partTypeParam.AsString(), "Sleeve", StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(partSubTypeParam.AsString(), "Wall Sleeve", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            wallSleeves.AddRange(genericModels);

            WallSleeveSymbols = new ObservableCollection<FamilySymbol>(wallSleeves);

            if (WallSleeveSymbols.Count > 0)
                SelectedWallSleeve = WallSleeveSymbols[0];
            else
                StatusMessage = "No wall Sleeve families found. Please load a wall sleeve family first.";
        }
        catch (Exception e)
        {
            StatusMessage = $"Error Loading Wall Sleeve Family: {e.Message}";
        }
    }

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

