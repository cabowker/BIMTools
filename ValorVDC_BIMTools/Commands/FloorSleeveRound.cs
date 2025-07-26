using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.Commands.FloorSleevesRound.ViewModels;
using ValorVDC_BIMTools.Commands.FloorSleevesRound.Views;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class FloorSleeveRound : IExternalCommand
{
    private readonly CurveMethods _curveMethods = new();
    private readonly InsulationMethods _insulationMethods = new();
    private Element _selectedElement;


    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            var viewModel = new FloorSleeveViewModel(commandData);
            var view = new FloorSleeveView(viewModel);


            if (view.ShowDialog() != true)
                return Result.Cancelled;

            using (var trans = new Transaction(document, "Save Floor Sleeve Preferences"))
            {
                trans.Start();
                viewModel.SavePreferences();
                trans.Commit();
            }

            var useMultipleSleeveTypes = viewModel.UseMultipleSleeveTypes;
            var selectedSleeve = viewModel.SelectedFloorSleeve;
            var selectedSleeveForLarger = viewModel.SelectedSleeveForLarger;
            var selectedPipeSize = viewModel.SelectedPipeSize;

            if (selectedSleeve == null)
            {
                TaskDialog.Show("Error", "No Floor Sleeve Type Selected");
                return Result.Failed;
            }

            if (useMultipleSleeveTypes && selectedSleeveForLarger == null)
            {
                TaskDialog.Show("Error", "Both sleeve types must be selected for multiple sleeve types");
                return Result.Failed;
            }


            double[] sleeveSize;
            try
            {
                sleeveSize = GetElements.GetAvailableSizes(selectedSleeve);
            }
            catch (ArgumentException
                   ex)
            {
                TaskDialog.Show("Error", $"Could not get available sleeve size: {ex.Message}\n" +
                                         $"Using these sizes: 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48");
                sleeveSize = new[] { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48 };
            }
            catch (FormatException ex)
            {
                TaskDialog.Show("Error", $"Could not get available sleeve size: {ex.Message}");
                sleeveSize = new[] { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48 };
            }

            var continueSelecting = true;
            while (continueSelecting)
                try
                {
                    var reference = uiDocument.Selection.PickObject(ObjectType.Element,
                        new SelectionFilters.VerticalMepCurveFilter(),
                        "Please Select a vertical pipe or duct passing through a floor");
                    var element = document.GetElement(reference);
                    var locationCurve = element.Location as LocationCurve;
                    if (locationCurve == null)
                    {
                        TaskDialog.Show("Error", "Selected Item Does Not Have A Valid Curve.");
                        continue;
                    }

                    var curve = locationCurve.Curve;

                    if (!IsVerticalCurve(curve))
                    {
                        TaskDialog.Show("Error",
                            "Selected curve is not vertical. Please select a vertical pipe or duct.");
                        continue;
                    }

                    var intersectingFloors = FindIntersectingFloors(document, curve);
                    if (!intersectingFloors.Any())
                    {
                        TaskDialog.Show("Error", "No floors found intersecting the selected vertical curve.");
                        continue;
                    }

                    var (nominalDiameter, insulationThickness, hasInsulation) =
                        GetElements.GetElementDiameterAndInsulation(document, element);

                    if (nominalDiameter == 0)
                    {
                        TaskDialog.Show("Error", "Could not retrieve nominal diameter of the selected element.");
                        continue;
                    }

                    nominalDiameter *= 12;
                    insulationThickness *= 12;

                    double? totalDiameter = nominalDiameter;
                    if (hasInsulation)
                        totalDiameter += insulationThickness * 2;
                    var currentSleeve = useMultipleSleeveTypes && totalDiameter > selectedPipeSize
                        ? selectedSleeveForLarger
                        : selectedSleeve;

                    int finalSizeIndex;
                    var requiresLargerSize = false;

                    if (hasInsulation)
                    {
                        finalSizeIndex = sleeveSize.Length - 1;
                        for (var i = 0; i < sleeveSize.Length; i++)
                            if (sleeveSize[i] > totalDiameter)
                            {
                                finalSizeIndex = i;
                                break;
                            }

                        if (finalSizeIndex == sleeveSize.Length - 1 && sleeveSize[finalSizeIndex] <= totalDiameter)
                            requiresLargerSize = true;
                    }
                    else
                    {
                        var baseSizeIndex = sleeveSize.Length - 1;
                        for (var i = 0; i < sleeveSize.Length; i++)
                            if (sleeveSize[i] > nominalDiameter)
                            {
                                baseSizeIndex = i;
                                break;
                            }

                        finalSizeIndex = Math.Min(baseSizeIndex + 1, sleeveSize.Length - 1);

                        if (finalSizeIndex == sleeveSize.Length - 1 && baseSizeIndex == sleeveSize.Length - 1)
                            requiresLargerSize = true;
                    }

                    if (requiresLargerSize)
                    {
                        var largestAvailableSize = sleeveSize[sleeveSize.Length - 1];
                        var userChoice = TaskDialog.Show(
                            "Sleeve Size Warning",
                            $"The sleeve required is larger than what the family has available!\n\n" +
                            $"Required diameter: {(hasInsulation ? totalDiameter : nominalDiameter):F2}\"\n" +
                            $"Largest size available: {largestAvailableSize}\"\n\n" +
                            $"Would you like to use the largest available size ({largestAvailableSize}\") instead?",
                            TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                        if (userChoice == TaskDialogResult.No)
                        {
                            continueSelecting = false;
                            continue;
                        }

                        finalSizeIndex = sleeveSize.Length - 1;
                    }

                    var sleeveDiameterInches = sleeveSize[finalSizeIndex];
                    var sleeveDiameterFeet = sleeveDiameterInches / 12.0;

                    using (var transaction = new Transaction(document, "Place Floor Sleeve"))
                    {
                        transaction.Start();

                        if (!currentSleeve.IsActive)
                        {
                            currentSleeve.Activate();
                            document.Regenerate();
                        }

                        foreach (var floor in intersectingFloors)
                        {
                            var intersectionPoint = GetFloorIntersectionPoint(floor, curve);
                            if (intersectionPoint != null)
                            {
                                var placeSleeve = document.Create.NewFamilyInstance(
                                    intersectionPoint,
                                    currentSleeve,
                                    floor,
                                    StructuralType.NonStructural);

                                // Set diameter parameter
                                SetSleeveParameters(currentSleeve, placeSleeve, sleeveDiameterFeet,
                                    sleeveDiameterInches);

                                // Set system information and pipe/duct size
                                SystemInformation.SetSystemInformation(element, placeSleeve);
                                SystemInformation.SetPipeSizeDuctDiameter(element, placeSleeve);
                            }
                        }

                        transaction.Commit();
                    }
                }
                catch (OperationCanceledException)
                {
                    continueSelecting = false;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                }

            return Result.Succeeded;
        }
        catch (Exception e)
        {
            message = e.Message;
            return Result.Failed;
        }
    }

    private bool IsVerticalCurve(Curve curve)
    {
        var direction = curve.GetEndPoint(1) - curve.GetEndPoint(0);
        var normalizedDirection = direction.Normalize();

        var verticalDotProduct = Math.Abs(normalizedDirection.DotProduct(XYZ.BasisZ));
        return verticalDotProduct > 0.966;
    }

    private List<Floor> FindIntersectingFloors(Document document, Curve curve)
    {
        var floors = new List<Floor>();
        var floorCollector = new FilteredElementCollector(document)
            .OfClass(typeof(Floor))
            .Cast<Floor>();

        foreach (var floor in floorCollector)
            if (DoesCurveIntersectFloor(floor, curve))
                floors.Add(floor);

        return floors;
    }

    private bool DoesCurveIntersectFloor(Floor floor, Curve curve)
    {
        try
        {
            var floorGeometry = floor.get_Geometry(new Options());
            foreach (var geometryObject in floorGeometry)
                if (geometryObject is Solid solid)
                {
                    var intersection = solid.IntersectWithCurve(curve, new SolidCurveIntersectionOptions());
                    if (intersection.SegmentCount > 0) return true;
                }
        }
        catch (Exception)
        {
            // Fallback: check if curve endpoints are above and below floor level
            var floorLevel = floor.LevelId;
            var level = floor.Document.GetElement(floorLevel) as Level;
            if (level != null)
            {
                var floorElevation = level.Elevation;
                var curveStart = curve.GetEndPoint(0);
                var curveEnd = curve.GetEndPoint(1);

                return (curveStart.Z <= floorElevation && curveEnd.Z >= floorElevation) ||
                       (curveStart.Z >= floorElevation && curveEnd.Z <= floorElevation);
            }
        }

        return false;
    }

    private XYZ GetFloorIntersectionPoint(Floor floor, Curve curve)
    {
        try
        {
            var floorGeometry = floor.get_Geometry(new Options());
            foreach (var geometryObject in floorGeometry)
                if (geometryObject is Solid solid)
                {
                    var intersection = solid.IntersectWithCurve(curve, new SolidCurveIntersectionOptions());
                    if (intersection.SegmentCount > 0) return intersection.GetCurveSegment(0).GetEndPoint(0);
                }
        }
        catch (Exception e)
        {
            var floorLevel = floor.LevelId;
            var level = floor.Document.GetElement(floorLevel) as Level;
            if (level != null)
            {
                var curveMidPoint = curve.Evaluate(0.5, true);
                return new XYZ(curveMidPoint.X, curveMidPoint.Y, level.Elevation);
            }
        }

        return null;
    }

    private void SetSleeveParameters(FamilySymbol sleeve, FamilyInstance placeSleeve, double sleeveDiameterFeet,
        double sleeveDiameterInches)
    {
        var parameterNameSet = false;
        var possibleParameterNames = new[]
        {
            "Nominal Diameter",
            "Diameter",
            "NominalDiameter"
        };

        foreach (var parameterName in possibleParameterNames)
        {
            var diameterParameter = sleeve.LookupParameter(parameterName);
            if (diameterParameter != null && !diameterParameter.IsReadOnly)
            {
                if (diameterParameter.StorageType == StorageType.Double)
                {
                    diameterParameter.Set(sleeveDiameterFeet);
                    parameterNameSet = true;
                    break;
                }

                if (diameterParameter.StorageType == StorageType.String)
                {
                    diameterParameter.Set(sleeveDiameterInches + "\"");
                    parameterNameSet = true;
                    break;
                }
            }
        }

        if (!parameterNameSet)
            foreach (var paramName in possibleParameterNames)
            {
                var instanceDiamParam = placeSleeve.LookupParameter(paramName);
                if (instanceDiamParam != null && !instanceDiamParam.IsReadOnly)
                {
                    if (instanceDiamParam.StorageType == StorageType.Double)
                    {
                        instanceDiamParam.Set(sleeveDiameterFeet);
                        parameterNameSet = true;
                        break;
                    }

                    if (instanceDiamParam.StorageType == StorageType.String)
                    {
                        instanceDiamParam.Set(sleeveDiameterInches + "\"");
                        parameterNameSet = true;
                        break;
                    }
                }
            }

        if (!parameterNameSet)
            TaskDialog.Show("Warning", "Could not set sleeve diameter parameter. Using default sleeve size.");
    }

    public static PushButtonData CreatePushButtonData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buttonName = "Floor Sleeves";
        var buttonText = "Floor" + Environment.NewLine + "Sleeves";
        var className = typeof(FloorSleeveRound).FullName;

        return new PushButtonData(buttonName, buttonText, assembly.Location, className)
        {
            ToolTip = "Create Floor Sleeves for Vertical Pipes/Ducts",
            LargeImage = ImagineUtilities.LoadThemeImage(assembly, "falcon.png", "falcon.png")
        };
    }
}