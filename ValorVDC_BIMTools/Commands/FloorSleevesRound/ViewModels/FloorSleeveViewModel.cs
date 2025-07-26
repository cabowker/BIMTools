using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

namespace ValorVDC_BIMTools.Commands.FloorSleevesRound.ViewModels;

public class FloorSleeveViewModel : ObservableObject
{
    //                                          "C:\ProgramData\ValorVDC\Families\SLEEVE - Pipe Floor Sleeve.rfa"
    private const string DEFAULT_FAMILY_PATH = @"C:\ProgramData\ValorVDC\Families\SLEEVE - Pipe Floor Sleeve.rfa";

    //Remember the user last selection
    private static readonly Guid SchemaGuid = new("F3A2B3C4-D5E6-4F78-9A1B-2C3D4E5F6789");
    private static readonly string SchemaName = "FloorSleevePreferences";
    private readonly ExternalCommandData _commandData;
    private readonly Document _document;
    private readonly UIDocument _uiDocument;
    private ObservableCollection<PipeSize> _availablePipeSizes;
    private ObservableCollection<FamilySymbol> _floorSleeveSymbols;
    private FamilySymbol _selectedFloorSleeve;
    private double _selectedPipeSize;
    private FamilySymbol _selectedSleeveForLarger;
    private bool _showLoadFamilyButtons;
    private string _statusMessage = "Ready to place floor sleeves.";
    private bool _useMultipleSleeveTypes;

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
                TaskDialog.Show("Debug", "PlaceFloorSleeveCommand is being executed!");
                DialogResult = true;
                RequestClose?.Invoke();
            }, GetCanPlaceFloorSleeve);

            CancelCommand = new RelayCommand(() =>
            {
                DialogResult = false;
                RequestClose?.Invoke();
            });

            LoadDefaultFamilyCommand = new RelayCommand(LoadDefaultFamily);
            BrowsePCCommand = new RelayCommand(BrowsePC);

            LoadFloorSleevesSymbols();
            if (FloorSleeveSymbols?.Count > 0)
                LoadPreferences();
            else
                SetDefaultPreferences();
        }
        catch (Exception e)
        {
            // Log the exception
            StatusMessage = $"Error initializing ViewModel: {e.Message}";
        }
    }

    private bool ShowSingleSleeveSelection => !_useMultipleSleeveTypes;

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
            {
                UpdatePlaceFloorSleeveCommand();
                OnPropertyChanged(nameof(CanPlaceFloorSleeve));
            }
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
                OnPropertyChanged(nameof(ShowSingleSleeveSelection));
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
            {
                UpdatePlaceFloorSleeveCommand();
                OnPropertyChanged(nameof(CanPlaceFloorSleeve));
                OnPropertyChanged();
            }
        }
    }

    public FamilySymbol SelectedSleeveForLarger
    {
        get => _selectedSleeveForLarger;
        set
        {
            if (SetProperty(ref _selectedSleeveForLarger, value))
            {
                UpdatePlaceFloorSleeveCommand();
                OnPropertyChanged(nameof(CanPlaceFloorSleeve));
            }
        }
    }

    public PipeSize SelectedPipeSizeItem
    {
        get => AvailablePipeSizes?.FirstOrDefault(p => p.Size == _selectedPipeSize);
        set
        {
            if (value != null && SetProperty(ref _selectedPipeSize, value.Size))
            {
                UpdatePlaceFloorSleeveCommand();
                OnPropertyChanged(nameof(CanPlaceFloorSleeve));
                OnPropertyChanged();
            }
        }
    }


    public RelayCommand PlaceFloorSleeveCommand { get; }

    public RelayCommand CancelCommand { get; }
    public RelayCommand LoadDefaultFamilyCommand { get; }
    public RelayCommand BrowsePCCommand { get; }
    public bool DialogResult { get; private set; }
    public bool CanPlaceFloorSleeve => GetCanPlaceFloorSleeve();


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
                SelectedSleeveForLarger = FloorSleeveSymbols[0];
                SelectedPipeSize = 6; // This will automatically update SelectedPipeSizeItem

                StatusMessage = "Ready to place floor sleeve.";
                ShowLoadFamilyButtons = false;
            }
            else
            {
                StatusMessage = "No floor sleeve types found. Would you like to load a floor sleeve family?";
                ShowLoadFamilyButtons = true;
                SelectedFloorSleeve = null;
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
        PlaceFloorSleeveCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanPlaceFloorSleeve));
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
                //LoadPreferences();
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
                //LoadPreferences();
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

    public void SavePreferences()
    {
        var schema = Schema.Lookup(SchemaGuid) ?? CreateSchema();
        if (schema == null)
        {
            StatusMessage = "Failed to create or find schema.";
            return;
        }

        try
        {
            var entity = new Entity(schema);

            entity.Set("UseMultipleSleeveTypes", UseMultipleSleeveTypes);
            entity.Set("SelectedPipeSize", SelectedPipeSize, UnitTypeId.Inches);

            // Explicitly save the ID, or an invalid ID if the selection is null
            var floorSleeveId = SelectedFloorSleeve?.Id ?? ElementId.InvalidElementId;
            entity.Set("SelectedFloorSleeveId", floorSleeveId);
        
            var largerSleeveId = SelectedSleeveForLarger?.Id ?? ElementId.InvalidElementId;
            entity.Set("SelectedSleeveForLargerId", largerSleeveId);
        
            _document.ProjectInformation.SetEntity(entity);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save preferences: {ex.Message}";
        }


    }


    private Schema CreateSchema()
    {
        try
        {
            var schemaBuilder = new SchemaBuilder(SchemaGuid);
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);
            schemaBuilder.SetSchemaName(SchemaName);
            schemaBuilder.SetDocumentation("Floor Sleeve Preferences Schema");
        
            schemaBuilder.AddSimpleField("UseMultipleSleeveTypes", typeof(bool));
        
            var pipeSizeField = schemaBuilder.AddSimpleField("SelectedPipeSize", typeof(double));
        
            // Change from the generic 'Length' spec to the more specific 'PipeSize' spec.
            // This is more compatible with the units you are saving.
            pipeSizeField.SetSpec(SpecTypeId.PipeSize); 
        
            schemaBuilder.AddSimpleField("SelectedFloorSleeveId", typeof(ElementId));
            schemaBuilder.AddSimpleField("SelectedSleeveForLargerId", typeof(ElementId));
        
            return schemaBuilder.Finish();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create schema: {ex.Message}";
            throw;
        }



    }

    private void LoadPreferences()
    {
        try
        {
            var schema = Schema.Lookup(SchemaGuid);
            if (schema == null)
            {
                SetDefaultPreferences();
                return;
            }

            var entity = _document.ProjectInformation.GetEntity(schema);
            if (!entity.IsValid())
            {
                SetDefaultPreferences();
                return;
            }

            UseMultipleSleeveTypes = entity.Get<bool>("UseMultipleSleeveTypes");
            SelectedPipeSize = entity.Get<double>("SelectedPipeSize", UnitTypeId.Inches);
            SelectedPipeSizeItem = AvailablePipeSizes.FirstOrDefault(p => Math.Abs(p.Size - SelectedPipeSize) < 0.001);

            var floorSleeveId = entity.Get<ElementId>("SelectedFloorSleeveId");
            if (floorSleeveId != null && floorSleeveId != ElementId.InvalidElementId)
            {
                SelectedFloorSleeve = FloorSleeveSymbols.FirstOrDefault(s => s.Id == floorSleeveId);
            }

            var largerSleeveId = entity.Get<ElementId>("SelectedSleeveForLargerId");
            if (largerSleeveId != null && largerSleeveId != ElementId.InvalidElementId)
            {
                SelectedSleeveForLarger = FloorSleeveSymbols.FirstOrDefault(s => s.Id == largerSleeveId);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load preferences: {ex.Message}";
            SetDefaultPreferences();
        }
    }

    private void SetDefaultPreferences()
    {
        UseMultipleSleeveTypes = false;
        SelectedPipeSize = 6;

        if (FloorSleeveSymbols?.Count > 0)
        {
            SelectedFloorSleeve = FloorSleeveSymbols[0];
            SelectedSleeveForLarger = FloorSleeveSymbols[0];
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

    private bool GetCanPlaceFloorSleeve()
    {
        bool canExecute;
        if (UseMultipleSleeveTypes)
        {
            canExecute = SelectedFloorSleeve != null && SelectedSleeveForLarger != null && SelectedPipeSize > 0;
            Debug.WriteLine($"GetCanPlaceFloorSleeve (MultipleSleeveTypes): " +
                            $"SelectedFloorSleeve={SelectedFloorSleeve != null}, " +
                            $"SelectedSleeveForLarger={SelectedSleeveForLarger != null}, " +
                            $"SelectedPipeSize={SelectedPipeSize}, CanExecute={canExecute}");
        }
        else
        {
            canExecute = SelectedFloorSleeve != null;
            Debug.WriteLine($"GetCanPlaceFloorSleeve (SingleSleeve): " +
                            $"SelectedFloorSleeve={SelectedFloorSleeve != null}, CanExecute={canExecute}");
        }

        return canExecute;
    }

    public class PipeSize
    {
        public double Size { get; set; }
        public string DisplayText => $"{Size}\"";
    }
}