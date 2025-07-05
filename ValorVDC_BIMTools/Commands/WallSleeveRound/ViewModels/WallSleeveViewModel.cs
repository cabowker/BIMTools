using System.Collections.ObjectModel;
using System.Windows.Input;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

namespace ValorVDC_BIMTools.Commands.WallSleeveRound.ViewModels;

public sealed class WallSleeveViewModel : ObservableObject
{
    private readonly Document _document;
    private readonly UIApplication _uiApplication;
    private readonly UIDocument _uiDocument;
    private FamilySymbol _selectedWallSleeve;
    private string _statusMessage = "Read to place wall Sleeve.";
    private ObservableCollection<FamilySymbol> _wallSleeveSymbols;
    private bool _showLoadFamilyButtons = false;
    private RelayCommand _placeWallSleeveCommand;

    private const string DEFAULT_FAMILY_PATH = @"C:\ProgramData\ValorVDC\Families\Wall Sleeves.rfa";

    public WallSleeveViewModel(ExternalCommandData commandData)
    {
        try
        {
            _uiApplication = commandData.Application;
            _uiDocument = _uiApplication.ActiveUIDocument;
            _document = _uiDocument.Document;

            _placeWallSleeveCommand = new RelayCommand(() =>
            {
                DialogResult = true;
                RequestClose?.Invoke();
            }, CanPlaceWallSleeve);

            CancelCommand = new RelayCommand(() =>
            {
                DialogResult = false;
                RequestClose?.Invoke();
            });
        
            LoadDefaultFamilyCommand = new RelayCommand(LoadDefaultFamily);
            BrowsePCCommand = new RelayCommand(BrowsePC);
        
            GetElementsByPartTypeAndSubType();
        }
        catch (Exception e)
        {
            // Log the exception
            StatusMessage = $"Error initializing ViewModel: {e.Message}";
            // Don't rethrow - let the window show with the error message

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
        set
        {
            if (SetProperty(ref _selectedWallSleeve, value))
                UpdatePlaceWallSleeveCommand();
        }
    }

    public bool ShowLoadFamilyButtons
    {
        get => _showLoadFamilyButtons;
        set => SetProperty(ref _showLoadFamilyButtons, value);
    }

    public RelayCommand PlaceWallSleeveCommand
    {
        get => _placeWallSleeveCommand;
        private set => _placeWallSleeveCommand = value;
    }
    public RelayCommand CancelCommand { get; }
    public RelayCommand LoadDefaultFamilyCommand { get; }
    public RelayCommand BrowsePCCommand { get; }
    public bool DialogResult { get; private set; }

    public void GetElementsByPartTypeAndSubType(string partType = "Sleeve", string partSubType = "Wall Sleeve")
    {
        try
        {
            StatusMessage = "Please select a Wall Sleeve Part Type";

            var wallSleeves = GetElements.GetElementByPartTypeAndPartSubType(_document, partType, partSubType);

            WallSleeveSymbols = new ObservableCollection<FamilySymbol>(wallSleeves);

            if (WallSleeveSymbols.Count > 0)
            {
                SelectedWallSleeve = WallSleeveSymbols[0];
                StatusMessage = "Ready to place wall Sleeve.";
                ShowLoadFamilyButtons = false;
            }
            else
            {
                StatusMessage = "No wall Sleeve types found. Would you like to load a wall sleeve family?";
                ShowLoadFamilyButtons = true;
                SelectedWallSleeve = null;
            }
        }
        catch (Exception e)
        {
            StatusMessage = $"Error Loading Wall Sleeve Family: {e.Message}";
        }
    }
    
    private void UpdatePlaceWallSleeveCommand()
    {
        _placeWallSleeveCommand = new RelayCommand(
            () => { DialogResult = true; RequestClose?.Invoke();}, 
            CanPlaceWallSleeve);
        OnPropertyChanged(nameof(PlaceWallSleeveCommand));
    }


    private void LoadDefaultFamily()
    {
        try
        {
            StatusMessage = " Loading default Wall Sleeve Family Now...";
            
            if (!System.IO.File.Exists(DEFAULT_FAMILY_PATH))
            {
                StatusMessage = "Default family file not found at the specified path.";
                return;
            }

            var family = LoadFamilies.LoadDefaultFamily(
                _document,
                _uiDocument,
                DEFAULT_FAMILY_PATH,
                "Load Default Wall Sleeve Family");

            if (family != null)
            {
                StatusMessage = "Family loaded successfully!";
                GetElementsByPartTypeAndSubType();
            }
            else 
                StatusMessage = "Failed to load default family. It may already be loaded or there was an error.";
        }
        catch (Exception e)
        {
            StatusMessage = $"Error loading default family: {e.Message}";
        }
    }
    private void BrowsePC()
    {
        try
        {
            StatusMessage = "Browsing for Wall Sleeve family...";

            var family = LoadFamilies.BrowseAndLoadFamily(
                _document, 
                _uiDocument, 
                "Select Wall Sleeve Family", 
                "Load Wall Sleeve Family");

            if (family != null)
            {
                StatusMessage = "Family loaded successfully!";
                GetElementsByPartTypeAndSubType();
            }
            else
            {
                StatusMessage = "Family loading was cancelled or failed.";
            }
        }

        catch (Exception ex)
        {
            StatusMessage = $"Error loading family: {ex.Message}";
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

    private bool CanPlaceWallSleeve()

    {
        return SelectedWallSleeve != null;
    }
}