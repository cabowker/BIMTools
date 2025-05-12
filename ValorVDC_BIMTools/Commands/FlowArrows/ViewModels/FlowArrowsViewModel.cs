using System.Collections.ObjectModel;
using System.Windows.Input;
using Autodesk.Revit.UI;

namespace FlowArrows.ViewModels;

public sealed class FlowArrowsViewModel : ObservableObject
{
    private readonly Document _document;
    private readonly UIApplication _uiApplication;
    private readonly UIDocument _uiDocument;
    private ObservableCollection<FamilySymbol> _flowArrowsSymbols;
    private FamilySymbol _selectedFlowArrow;

    private string _statusMessage = "Ready to place flow arrow";

    public FlowArrowsViewModel(ExternalCommandData commandData)
    {
        _uiApplication = commandData.Application;
        _uiDocument = _uiApplication.ActiveUIDocument;
        _document = _uiDocument.Document;

        PlaceFlowArrowCommand = new RelayCommand(() =>
        {
            DialogResult = true;
            RequestClose?.Invoke();
        }, CanPlaceFlowArrow);
        CancelCommand = new RelayCommand(() =>
        {
            DialogResult = false;
            RequestClose?.Invoke();
        });


        LoadFlowArrowSymbols();
    }

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
        set => SetProperty(ref _selectedFlowArrow, value);
    }

    public ICommand PlaceFlowArrowCommand { get; }
    public ICommand CancelCommand { get; }
    public bool DialogResult { get; private set; }

    public event Action RequestClose;
    public event Action SelectionComplete;

    private void LoadFlowArrowSymbols()
    {
        try
        {
            StatusMessage = "Loading flow arrow families...";

            // Get all flow arrow family symbols (from various categories)
            var flowArrows = new List<FamilySymbol>();

            // Look in pipe accessories
            var pipeAccessories = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                .Cast<FamilySymbol>()
                .Where(fam => fam.Family.Name.Contains("Flow") && fam.Family.Name.Contains("Arrow"))
                .ToList();

            flowArrows.AddRange(pipeAccessories);

            // Look in duct accessories
            var ductAccessories = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DuctAccessory)
                .Cast<FamilySymbol>()
                .Where(fam => fam.Family.Name.Contains("Flow") && fam.Family.Name.Contains("Arrow"))
                .ToList();

            flowArrows.AddRange(ductAccessories);

            // Look in generic models
            var genericModels = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .Cast<FamilySymbol>()
                .Where(fam => fam.Family.Name.Contains("Flow") && fam.Family.Name.Contains("Arrow"))
                .ToList();

            flowArrows.AddRange(genericModels);

            // Add to the observable collection
            FlowArrowSymbols = new ObservableCollection<FamilySymbol>(flowArrows);

            // Set default selected flow arrow
            if (FlowArrowSymbols.Count > 0)
            {
                SelectedFLowArrow = FlowArrowSymbols[0];
                StatusMessage =
                    $"Found {FlowArrowSymbols.Count} flow arrow families. Select one and click 'Place Flow Arrow'.";
            }
            else
            {
                StatusMessage = "No flow arrow families found. Please load a flow arrow family first.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading flow arrow families: {ex.Message}";
        }
    }

    private bool CanPlaceFlowArrow()
    {
        return SelectedFLowArrow != null;
    }

// private void PlaceFlowArrow()
//     {
//         RequestClose?.Invoke();
//         try
//         {
//             StatusMessage = "Please Select a Pipe or Line Based Element...";
//
//             Reference reference = null;
//             try
//             {
//                 reference = _uiDocument.Selection.PickObject(ObjectType.PointOnElement,
//                     new MEPCurveFilter(), "Select a point a point on Pipe or Duct");
//             }
//             catch (OperationCanceledException)
//             {
//                 StatusMessage = "Selection cancelled. Ready to try again?";
//                 return;
//             }
//
//             Element element = _document.GetElement(reference);
//             Line line = null;
//
//             if (element is MEPCurve mepCurve) 
//             {
//                 LocationCurve locationCurve = mepCurve.Location as LocationCurve;
//                 if (locationCurve?.Curve is Line locationLine)
//                     line = locationLine;
//             }
//             else
//             {
//                 var locationCurve = element.Location as LocationCurve;
//                 if (locationCurve?.Curve is Line locationLine)
//                     line = locationLine;
//             }
//
//             if (line == null)
//             {
//                 StatusMessage = "Selected element does not have a valid curve.";
//                 return;
//             }
//
//             ElementId levelId = element.LevelId;
//             if (levelId == ElementId.InvalidElementId)
//                 levelId = _document.ActiveView.GenLevel?.Id ?? ElementId.InvalidElementId;
//
//             if (levelId == ElementId.InvalidElementId)
//             {
//                 StatusMessage = "Could not determine level for placement. Please Try again.";
//                 return;
//             }
//             XYZ pipeDirection = line.Direction;
//             XYZ point = reference.GlobalPoint;
//
//             using (Transaction transaction = new Transaction(_document, "Flow Arrows"))
//             {
//                 transaction.Start();
//
//                 StatusMessage = "Creating Flow Arrows";
//                 if (!SelectedFLowArrow.IsActive)
//                 {
//                     SelectedFLowArrow.Activate();
//                     _document.Regenerate();
//                 }
//
//                 var placeArrow = _document.Create.NewFamilyInstance(point,
//                     _selectedFlowArrow, 
//                     pipeDirection, 
//                     _document.GetElement(levelId) as Level, 
//                     StructuralType.NonStructural);
//                 
//                 _document.Regenerate();
//                 LocationPoint arrowLocationPoint = placeArrow.Location as LocationPoint;
//                 if (arrowLocationPoint != null)
//                 {
//                 XYZ location = arrowLocationPoint.Point;
//                 XYZ differencePoint = new XYZ(
//                     point.X - location.X, 
//                     point.Y - location.Y, 
//                     point.Z - location.Z);
//                 placeArrow.Location.Move(differencePoint);
//                 }
//
//                 transaction.Commit();
//
//                 StatusMessage = "Flow Arrow Placed Successfully.";
//             }
//         }
//         catch (Autodesk.Revit.Exceptions.OperationCanceledException)
//         {
//             StatusMessage = "Operation cancelled.";
//         }
//         catch (Exception e)
//         {
//             StatusMessage = $"Error: {e.Message}";
//         }
//     }
}