using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

namespace ValorVDC_BIMTools.Commands.FloorSleevesRound.ViewModels;

public partial class FloorSleeveViewModel : ObservableObject
{
    private const string DEFAULT_FAMILY_PATH = @"C:\ProgramData\ValorVDC\Families\SLEEVE - Pipe Floor Sleeve.rfa";
    private readonly ExternalCommandData _commandData;
    private readonly Document _document;
    private readonly UIDocument _uiDocument;
    private FamilySymbol _selectedFloorSleeve;
    private bool _showLoadFamilyButtons;
    private string _statusMessage = "Ready to place floor sleeves.";
    private ObservableCollection<FamilySymbol> _floorSleeveSymbols;
    private bool _useMultipleSleeveTypes;
    private double _selectedPipeSize;
    private FamilySymbol _selectedSleeveForSmaller;
    private FamilySymbol _selectedSleeveForLarger;
    private ObservableCollection<PipeSize> _availablePipeSizes;
    public class PipeSize
    {
        public double Size { get; set; }
        public string DisplayText => $"{Size}\"";
    }

    public FloorSleeveViewModel(ExternalCommandData commandData)
    {
        try
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;
            _uiDocument = commandData.Application.ActiveUIDocument;

            AvailablePipeSizes = new ObservableCollection<PipeSize>(Enumerable.Range(1, 42)
                .Select(i => new PipeSize { Size = i }));
        
            PlaceFloorSleeveCommand = new RelayCommand(() =>
            {
                DialogResult = true;
                RequestClose?.Invoke();
            }, CanPlaceFloorSleeve);

            CancelCommand = new RelayCommand(() =>
            {
                DialogResult = false;
                RequestClose?.Invoke();
            });

            LoadDefaultFamilyCommand = new RelayCommand(LoadDefaultFamily);
            BrowsePCCommand = new RelayCommand(BrowsePC);

            LoadFloorSleevesSymbols();
        }
        catch (Exception e)
        {
            // Log the exception
            StatusMessage = $"Error initializing ViewModel: {e.Message}";
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<FamilySymbol> FloorSleeveSymbols
    {
        get => _floorSleeveSymbols;
        set => SetProperty(ref _floorSleeveSymbols, value);
    }

    public FamilySymbol SelectedFloorSleeve
    {
        get => _selectedFloorSleeve;
        set
        {
            if (SetProperty(ref _selectedFloorSleeve, value))
                UpdatePlaceFloorSleeveCommand();
        }
    }

    public bool ShowLoadFamilyButtons
    {
        get => _showLoadFamilyButtons;
        set => SetProperty(ref _showLoadFamilyButtons, value);
    }
    
    public bool UseMultipleSleeveTypes
    {
        get => _useMultipleSleeveTypes;
        set
        {
            if (SetProperty(ref _useMultipleSleeveTypes, value))
            {
                UpdatePlaceFloorSleeveCommand();
                OnPropertyChanged(nameof(CanPlaceFloorSleeve));
            }
        }
    }
    
    public ObservableCollection<PipeSize> AvailablePipeSizes
    {
        get => _availablePipeSizes;
        set => SetProperty(ref _availablePipeSizes, value);
    }
    
    public double SelectedPipeSize
    {
        get => _selectedPipeSize;
        set
        {
            if (SetProperty(ref _selectedPipeSize, value))
                UpdatePlaceFloorSleeveCommand();
        }
    }
    
    public FamilySymbol SelectedSleeveForSmaller
    {
        get => _selectedSleeveForSmaller;
        set
        {
            if (SetProperty(ref _selectedSleeveForSmaller, value))
                UpdatePlaceFloorSleeveCommand();
        }
    }
    
    public FamilySymbol SelectedSleeveForLarger
    {
        get => _selectedSleeveForLarger;
        set
        {
            if (SetProperty(ref _selectedSleeveForLarger, value))
                UpdatePlaceFloorSleeveCommand();
        }
    }
    
    public RelayCommand PlaceFloorSleeveCommand { get; private set; }

    public RelayCommand CancelCommand { get; }
    public RelayCommand LoadDefaultFamilyCommand { get; }
    public RelayCommand BrowsePCCommand { get; }
    public bool DialogResult { get; private set; }


    public void LoadFloorSleevesSymbols(string partType = "Sleeve", string partSubType = "Floor Sleeve")
    {
        try
        {
            StatusMessage = "Please select a Floor Sleeve Type";

            var floorSleeves = GetElements.GetElementByPartTypeAndPartSubType(_document, partType, partSubType);

            FloorSleeveSymbols = new ObservableCollection<FamilySymbol>(floorSleeves);

            if (FloorSleeveSymbols.Count > 0)
            {
                SelectedFloorSleeve = FloorSleeveSymbols[0];
                SelectedSleeveForSmaller = FloorSleeveSymbols[0];
                SelectedSleeveForLarger = FloorSleeveSymbols[0];
                SelectedPipeSize = 12; // Default to 12"
                StatusMessage = "Ready to place floor sleeve.";
                ShowLoadFamilyButtons = false;
            }
            else
            {
                StatusMessage = "No floor sleeve types found. Would you like to load a floor sleeve family?";
                ShowLoadFamilyButtons = true;
                SelectedFloorSleeve = null;
                SelectedSleeveForSmaller = null;
                SelectedSleeveForLarger = null;
            }
        }
        catch (Exception e)
        {
            StatusMessage = $"Error Loading Floor Sleeve Family: {e.Message}";
        }
    }

    private void UpdatePlaceFloorSleeveCommand()
    {
        PlaceFloorSleeveCommand = new RelayCommand(
            () =>
            {
                DialogResult = true;
                RequestClose?.Invoke();
            },
            CanPlaceFloorSleeve);
        OnPropertyChanged(nameof(PlaceFloorSleeveCommand));
    }

    private void LoadDefaultFamily()
    {
        try
        {
            StatusMessage = "Loading default floor sleeve family...";

            // Check if the default family file exists
            if (!File.Exists(DEFAULT_FAMILY_PATH))
            {
                StatusMessage = "Default family file not found at the specified path.";
                return;
            }

            var family = LoadFamilies.LoadDefaultFamily(
                _document,
                _uiDocument,
                DEFAULT_FAMILY_PATH,
                "Load Default Floor Sleeve Family");

            if (family != null)
            {
                StatusMessage = "Family loaded successfully!";
                LoadFloorSleevesSymbols();
            }
            else
            {
                StatusMessage = "Failed to load default family. It may already be loaded or there was an error.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load default family: {ex.Message}";
        }


    }

    private void BrowsePC()
    {
        try
        {
            StatusMessage = "Browsing for floor sleeve family...";

            var family = LoadFamilies.BrowseAndLoadFamily(
                _document,
                _uiDocument,
                "Select Floor Sleeve Family",
                "Load Floor Sleeve Family");

            if (family != null)
            {
                StatusMessage = "Family loaded successfully!";
                LoadFloorSleevesSymbols();
            }
            else
            {
                StatusMessage = "Family loading was cancelled or failed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load family: {ex.Message}";
        }
    }

    public event Action RequestClose;

    private Parameter GetParameterCaseInsenitive(Element element, string parameterName)
    {
        var parameters = element.Parameters;

        foreach (Parameter parameter in parameters)
            if (string.Equals(parameter.Definition.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                return parameter;

        return null;
    }

    private bool CanPlaceFloorSleeve()
    {
        if (UseMultipleSleeveTypes)
            return SelectedSleeveForSmaller != null && SelectedSleeveForLarger != null && SelectedPipeSize > 0;
        return SelectedFloorSleeve != null;
    }

}