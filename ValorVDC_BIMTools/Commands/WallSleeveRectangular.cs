using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.Commands.WallSleeveRectangular.ViewModels;
using ValorVDC_BIMTools.Commands.WallSleeveRectangular.Views;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class WallSleevesRectangular : IExternalCommand
{
    private readonly CurveMethods _curveMethods = new();
    private readonly InsulationMethods _insulationMethods = new();

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            var viewmodel = new RectangularWallSleeveViewModel(commandData);
            var view = new RectangularWallSleeveView(viewmodel);

            if (view.ShowDialog() != true)
                return Result.Cancelled;

            var selectedSleeve = viewmodel.SelectedWallSleeve;
            if (selectedSleeve == null)
            {
                TaskDialog.Show("Error", "No Rectangular Wall Sleeve Type Selected");
                return Result.Failed;
            }

            var continuesSelecting = true;

            while (continuesSelecting)
                try
                {
                    var reference = uiDocument.Selection.PickObject(ObjectType.Element,
                        new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(),
                        "Please Select A Rectangular Duct, Cable Tray");

                    var element = document.GetElement(reference);
                    var locationCurve = element.Location as LocationCurve;

                    if (locationCurve == null)
                    {
                        TaskDialog.Show("Error", "Selected Item Does Not Have A Valid Curve.");
                        continue;
                    }

                    var height = 0.0;
                    var width = 0.0;
                    var insulationThickness = 0.0;
                    var hasInsulation = false;


                    if (element is Duct duct)
                    {
                        var heightParameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                        var widthParameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);

                        if (heightParameter != null && heightParameter.HasValue && widthParameter != null &&
                            widthParameter.HasValue)
                        {
                            height = heightParameter.AsDouble();
                            width = widthParameter.AsDouble();
                        }
                        else
                        {
                            heightParameter = duct.LookupParameter("Height");
                            widthParameter = duct.LookupParameter("Width");

                            if (heightParameter != null && heightParameter.HasValue && widthParameter != null &&
                                widthParameter.HasValue)
                            {
                                height = heightParameter.AsDouble();
                                width = widthParameter.AsDouble();
                            }
                        }

                        try
                        {
                            var ductInsulation = _insulationMethods.FindDuctInsulation(document, duct);
                            if (ductInsulation != null)
                            {
                                var thicknessParameter =
                                    ductInsulation.get_Parameter(BuiltInParameter.RBS_INSULATION_THICKNESS);

                                if (thicknessParameter == null || !thicknessParameter.HasValue)
                                {
                                    string[] possibleParameterNames =
                                        { "Thickness", "Insulation Thickness", "Duct Insulation Thickness" };
                                    foreach (var possibleParameterName in possibleParameterNames)
                                    {
                                        thicknessParameter = ductInsulation.LookupParameter(possibleParameterName);
                                        if (thicknessParameter != null && thicknessParameter.HasValue &&
                                            thicknessParameter.AsDouble() > 0)
                                            break;
                                    }
                                }

                                if (thicknessParameter != null && thicknessParameter.HasValue &&
                                    thicknessParameter.AsDouble() > 0)
                                {
                                    insulationThickness = thicknessParameter.AsDouble();
                                    hasInsulation = true;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                    else if (element is CableTray cableTray)
                    {
                        var heightParameter = cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM);
                        var widthParameter = cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM);

                        if (heightParameter != null && heightParameter.HasValue && widthParameter != null &&
                            widthParameter.HasValue)
                        {
                            height = heightParameter.AsDouble();
                            width = widthParameter.AsDouble();
                        }
                        else
                        {
                            heightParameter = cableTray.LookupParameter("Height");
                            widthParameter = cableTray.LookupParameter("Width");

                            if (heightParameter != null && heightParameter.HasValue && widthParameter != null &&
                                widthParameter.HasValue)
                            {
                                height = heightParameter.AsDouble();
                                width = widthParameter.AsDouble();
                            }
                        }
                    }
                    else if (element is FabricationPart fabPart)
                    {
                        var depthParameter = fabPart.LookupParameter("Depth");
                        var widthParameter = fabPart.LookupParameter("Width");

                        if (depthParameter != null && depthParameter.HasValue && widthParameter != null &&
                            widthParameter.HasValue)
                        {
                            height = depthParameter.AsDouble();
                            width = widthParameter.AsDouble();
                        }
                        else
                        {
                            string[] heightNames = { "Duct Depth", "Section Depth", "Main Primary Depth" };
                            string[] widthNames = { "Duct Width", "Section Width", "Main Primary Width" };

                            foreach (var paramName in heightNames)
                            {
                                depthParameter = fabPart.LookupParameter(paramName);
                                if (depthParameter != null && depthParameter.HasValue)
                                {
                                    height = depthParameter.AsDouble();
                                    break;
                                }
                            }

                            foreach (var paramName in widthNames)
                            {
                                widthParameter = fabPart.LookupParameter(paramName);
                                if (widthParameter != null && widthParameter.HasValue)
                                {
                                    width = widthParameter.AsDouble();
                                    break;
                                }
                            }
                        }

                        var insulationParameter = fabPart.LookupParameter("Insulation Thickness");
                        if (insulationParameter != null && insulationParameter.HasValue &&
                            insulationParameter.AsDouble() > 0)
                        {
                            insulationThickness = insulationParameter.AsDouble();
                            hasInsulation = true;
                        }
                    }

                    if (height == 0 || width == 0)
                    {
                        TaskDialog.Show("Error", "Could not retrieve height and width of the selected element.");
                        continue;
                    }

                    height *= 12.0;
                    width *= 12.0;
                    insulationThickness *= 12.0;

                    var totalHeight = height;
                    var totalWidth = width;

                    if (hasInsulation)
                    {
                        totalHeight += insulationThickness * 2;
                        totalWidth += insulationThickness * 2;
                    }

                    var finalHeight = totalHeight + viewmodel.AddToHeight;
                    var finalWidth = totalWidth + viewmodel.AddToWidth;

                    var roundUpValue = viewmodel.RoundUpValue;
                    if (roundUpValue > 0)
                    {
                        finalHeight = Math.Ceiling(finalHeight / roundUpValue) * roundUpValue;
                        finalWidth = Math.Ceiling(finalWidth / roundUpValue) * roundUpValue;
                    }

                    var finalHeightFeet = finalHeight / 12.0;
                    var finalWidthFeet = finalWidth / 12.0;

                    var curve = locationCurve.Curve;
                    var clickPoint = reference.GlobalPoint;
                    Line line = null;
                    if (curve is Line existingLine)
                    {
                        line = existingLine;
                    }
                    else
                    {
                        var parameter = curve.Project(clickPoint).Parameter;
                        var tangent = curve.ComputeDerivatives(parameter, true).BasisX.Normalize();
                        var pointOnCurve = curve.Project(clickPoint).XYZPoint;
                        line = Line.CreateBound(pointOnCurve, pointOnCurve.Add(tangent));
                    }

                    var (hostLevel, placemnentPoint) = _curveMethods.GetLevelAndPlacementPoint(
                        document, element, line, reference);

                    using (var transaction = new Transaction(document, "Place Rectangular Wall Sleeve"))
                    {
                        transaction.Start();
                        if (!selectedSleeve.IsActive)
                        {
                            selectedSleeve.Activate();
                            document.Regenerate();
                        }

                        var placeSleeve = document.Create.NewFamilyInstance(
                            placemnentPoint,
                            selectedSleeve,
                            hostLevel,
                            hostLevel,
                            StructuralType.NonStructural);

                        _curveMethods.AlignElementWithCurve(document, placeSleeve, line, placemnentPoint);

                        string[] heightParameterNames = { "Height", "Sleeve Height", "Nominal Height" };
                        var heightSet = false;
                        foreach (var heightParameterName in heightParameterNames)
                        {
                            var heightParameter = placeSleeve.LookupParameter(heightParameterName);
                            if (heightParameter != null && !heightParameter.IsReadOnly)
                            {
                                if (heightParameter.StorageType == StorageType.Double)
                                {
                                    heightParameter.Set(finalHeightFeet);
                                    heightSet = true;
                                    break;
                                }

                                if (heightParameter.StorageType == StorageType.String)
                                {
                                    heightParameter.Set(finalHeight + "\"");
                                    heightSet = true;
                                    break;
                                }
                            }
                        }

                        string[] widthParameterNames = { "Width", "Sleeve Width", "Nominal Width" };
                        var widthSet = false;
                        foreach (var paramName in widthParameterNames)
                        {
                            var widthParameter = placeSleeve.LookupParameter(paramName);
                            if (widthParameter != null && !widthParameter.IsReadOnly)
                            {
                                if (widthParameter.StorageType == StorageType.Double)
                                {
                                    widthParameter.Set(finalWidthFeet);
                                    widthSet = true;
                                    break;
                                }

                                if (widthParameter.StorageType == StorageType.String)
                                {
                                    widthParameter.Set(finalWidth + "\"");
                                    widthSet = true;
                                    break;
                                }
                            }
                        }

                        SystemInformation.SetSystemInformation(element, placeSleeve);

                        if (!heightSet || !widthSet)
                            TaskDialog.Show("Warning",
                                "Could not set all sleeve dimensions. Check the family parameters.");

                        transaction.Commit();
                    }
                }
                catch (OperationCanceledException)
                {
                    continuesSelecting = false;
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // Alternative exception that might be thrown for cancellation
                    continuesSelecting = false;
                    break;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                    // If we hit a non-cancellation error, ask if the user wants to continue
                    var result = TaskDialog.Show("Continue?",
                        "An error occurred. Do you want to continue placing sleeves?",
                        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                    if (result == TaskDialogResult.No)
                    {
                        continuesSelecting = false;
                        break;
                    }
                }

            return Result.Succeeded;
        }
        catch (Exception exception)
        {
            message = exception.Message;
            return Result.Failed;
        }
    }

    public static PushButtonData CreatePushButtonData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buttonName = "Rectangular Sleeve";
        var buttonText = "Rectangular" + Environment.NewLine + "Wall Sleeves";
        var className = typeof(WallSleevesRectangular).FullName;

        return new PushButtonData(buttonName, buttonText, assembly.Location, className)
        {
            ToolTip = "Create Rectangular Wall Sleeves",
            LargeImage = ImagineUtilities.LoadImage(assembly, "RectanglurSleeveButton_32x32.png")
        };
    }
}