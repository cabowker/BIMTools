using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

namespace ValorVDC_BIMTools.Commands.FlowArrows.ViewModels;

public sealed class FlowArrowsViewModel : ObservableObject
{
    private const string DEFAULT_FAMILY_PATH = @"C:\ProgramData\ValorVDC\Families\Flow Arrow.rfa";
    private readonly Document _document;
    private readonly UIApplication _uiApplication;
    private readonly UIDocument _uiDocument;
    private ObservableCollection<FamilySymbol> _flowArrowsSymbols;
    private FamilySymbol _selectedFlowArrow;
    private bool _showLoadFamilyButtons;
    private string _statusMessage = "Ready to place flow arrow";

    public FlowArrowsViewModel(ExternalCommandData commandData)
    {
        _uiApplication = commandData.Application;
        _uiDocument = _uiApplication.ActiveUIDocument;
        _document = _uiDocument.Document;

        PlaceFlowArrowCommand = new RelayCommand(
            () =>
            {
                DialogResult = true;
                RequestClose?.Invoke();
            },
            CanPlaceFlowArrow);

        CancelCommand = new RelayCommand(() =>
        {
            DialogResult = false;
            RequestClose?.Invoke();
        });

        LoadDefaultFamilyCommand = new RelayCommand(LoadDefaultFamily);
        BrowsePCCommand = new RelayCommand(BrowsePC);


        GetElementsByPartTypeAndSubType();
    }

    public event Action RequestClose;

    #region Public Methods

    public void GetElementsByPartTypeAndSubType(string partType = "Annotation", string partSubType = "Flow Arrow")
    {
        try
        {
            StatusMessage = "Loading Flow Arrow Families...";

            var flowArrows = GetElements.GetElementByPartTypeAndPartSubType(_document, partType, partSubType);

            if (flowArrows.Count == 0)
            {
                flowArrows = GetElements.GetElementByPartTypeAndPartSubType(_document, "Flow Arrow", "Flow Arrow");

                if (flowArrows.Count == 0)
                    flowArrows =
                        GetElements.GetElementByPartTypeAndPartSubType(_document, "Generic Annotation", "Flow Arrow");

                // if (flowArrows.Count == 0)
                // {
                //     var collector = new FilteredElementCollector(_document)
                //         .OfClass(typeof(FamilySymbol))
                //         .Cast<FamilySymbol>()
                //         .Where(fs =>
                //             (fs.Family.Name.ToLower().Contains("flow") && fs.Family.Name.ToLower().Contains("arrow")) ||
                //             fs.Name.ToLower().Contains("flow arrow"))
                //         .ToList();
                //
                //     flowArrows = collector;
                // }
            }

            FlowArrowSymbols = new ObservableCollection<FamilySymbol>(flowArrows);

            if (FlowArrowSymbols.Count > 0)
            {
                SelectedFLowArrow = FlowArrowSymbols[0];
                StatusMessage = "Ready to place flow arrow";
                ShowLoadFamilyButtons = false;
            }
            else
            {
                StatusMessage = "There are no flow arrow families loaded in the project, would you like to load one?";
                ShowLoadFamilyButtons = true;
                SelectedFLowArrow = null;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading flow arrow families: {ex.Message}";
            ShowLoadFamilyButtons = false;
        }
    }

    #endregion

    #region Public Properties

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<FamilySymbol> FlowArrowSymbols
    {
        get => _flowArrowsSymbols;
        set => SetProperty(ref _flowArrowsSymbols, value);
    }

    public FamilySymbol SelectedFLowArrow
    {
        get => _selectedFlowArrow;
        set
        {
            if (SetProperty(ref _selectedFlowArrow, value)) UpdatePlaceFlowArrowCommand();
        }
    }

    public bool ShowLoadFamilyButtons
    {
        get => _showLoadFamilyButtons;
        set => SetProperty(ref _showLoadFamilyButtons, value);
    }

    public RelayCommand PlaceFlowArrowCommand { get; private set; }

    public RelayCommand CancelCommand { get; }
    public RelayCommand LoadDefaultFamilyCommand { get; }
    public RelayCommand BrowsePCCommand { get; }

    public bool DialogResult { get; private set; }

    #endregion

    #region Private Methods

    private void UpdatePlaceFlowArrowCommand()
    {
        PlaceFlowArrowCommand = new RelayCommand(
            () =>
            {
                DialogResult = true;
                RequestClose?.Invoke();
            },
            CanPlaceFlowArrow);

        OnPropertyChanged(nameof(PlaceFlowArrowCommand));
    }

    private void LoadDefaultFamily()
    {
        try
        {
            StatusMessage = "Loading default flow arrow family...";

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
                "Load Default Flow Arrow Family");

            if (family != null)
            {
                StatusMessage = "Family loaded successfully!";
                GetElementsByPartTypeAndSubType();
            }
            else
            {
                StatusMessage = "Failed to load default family. It may already be loaded or there was an error.";
            }
        }

        catch (Exception ex)
        {
            StatusMessage = $"Error loading default family: {ex.Message}";
        }
    }

    private void BrowsePC()
    {
        try
        {
            StatusMessage = "Browsing for flow arrow family...";

            var family = LoadFamilies.BrowseAndLoadFamily(
                _document,
                _uiDocument,
                "Select Flow Arrow Family",
                "Load Flow Arrow Family");

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

    private bool CanPlaceFlowArrow()
    {
        return SelectedFLowArrow != null;
    }

    #endregion
}