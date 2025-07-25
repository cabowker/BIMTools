﻿using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.HelperMethods;

namespace ValorVDC_BIMTools.Commands.WallSleeveRectangular.ViewModels;

public class RectangularWallSleeveViewModel : ObservableObject
{
    private const string DEFAULT_FAMILY_PATH = @"C:\ProgramData\ValorVDC\Families\Wall Rectangle Sleeve.rfa";
    private readonly Document _document;
    private readonly UIApplication _uiApplication;
    private readonly UIDocument _uiDocument;
    private double _addToHeight = 2.0;
    private double _addToWidth = 2.0;
    private double _roundUpValue = .25;
    private FamilySymbol _selectedWallSleeve;
    private bool _showLoadFamilyButtons;
    private string _statusMeassage = " Ready to Place Rectangular Wall Sleeve.";
    private ObservableCollection<FamilySymbol> _wallSleeveSymbols;

    public RectangularWallSleeveViewModel(ExternalCommandData commandData)
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

        LoadDefaultFamilyCommand = new RelayCommand(LoadDefaultFamily);
        BrowsePCCommand = new RelayCommand(BrowsePC);

        GetElementsByPartTypeAndSubType();
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
        set
        {
            if (SetProperty(ref _selectedWallSleeve, value))
                UpdatePlaceWallSleeve();
        }
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

    public bool ShowLoadFamilyButtons
    {
        get => _showLoadFamilyButtons;
        set => SetProperty(ref _showLoadFamilyButtons, value);
    }

    public RelayCommand PlaceWallSleeveCommand { get; private set; }

    public RelayCommand CancelCommand { get; }
    public RelayCommand LoadDefaultFamilyCommand { get; set; }
    public RelayCommand BrowsePCCommand { get; }
    public bool DialogResult { get; private set; }

    public void GetElementsByPartTypeAndSubType(string partType = "Sleeve",
        string partSubType = "Wall Sleeve-Rectangular")
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


    private void UpdatePlaceWallSleeve()
    {
        PlaceWallSleeveCommand = new RelayCommand(
            () =>
            {
                DialogResult = true;
                RequestClose?.Invoke();
            },
            CanPlaceWallSleeve);
        OnPropertyChanged(nameof(PlaceWallSleeveCommand));
    }

    private void LoadDefaultFamily()
    {
        try
        {
            StatusMessage = " Loading default Wall Sleeve Family Now...";

            if (!File.Exists(DEFAULT_FAMILY_PATH))
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
            {
                StatusMessage = "Failed to load default family. It may already be loaded or there was an error.";
            }
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