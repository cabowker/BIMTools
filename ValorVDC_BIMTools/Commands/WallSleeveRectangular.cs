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
    private readonly PipeInsulationMethods _pipeInsulationMethods = new PipeInsulationMethods();
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIDocument uiDocument = commandData.Application.ActiveUIDocument;
            Document document = uiDocument.Document;

            var viewmodel = new RectangularWallSleeveViewModel(commandData);
            var view = new RectangularWallSleeveView(viewmodel);

            if (view.ShowDialog() != true)
                return Result.Succeeded;

            var selectedSleeve = viewmodel.SelectedWallSleeve;
            if (selectedSleeve == null)
            {
                TaskDialog.Show("Error", "No Rectangular Wall Sleeve Type Selected");
                return Result.Failed;
            }

            var continuesSelecting = true;

            while (continuesSelecting)
            {
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

                    double height = 0.0;
                    double width = 0.0;
                    double insulationThickness = 0.0;
                    bool hasInsulation = false;


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
                            DuctInsulation ductInsulation = _pipeInsulationMethods.FindDuctInsulation(document, duct);
                            if (ductInsulation != null)
                            {
                                Parameter thicknessParameter =
                                    ductInsulation.get_Parameter(BuiltInParameter.RBS_INSULATION_THICKNESS);

                                if (thicknessParameter == null || !thicknessParameter.HasValue)
                                {
                                    string[] possibleParameterNames =
                                        { "Thickness", "Insulation Thickness", "Duct Insulation Thickness" };
                                    foreach (string possibleParameterName in possibleParameterNames)
                                    {
                                        thicknessParameter = ductInsulation.LookupParameter(possibleParameterName);
                                        if (thicknessParameter != null && thicknessParameter.HasValue &&
                                            thicknessParameter.AsDouble() > 0)
                                        {
                                            break;
                                        }
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
                            
                            foreach (string paramName in heightNames)
                            {
                                depthParameter = fabPart.LookupParameter(paramName);
                                if (depthParameter != null && depthParameter.HasValue)
                                {
                                    height = depthParameter.AsDouble();
                                    break;
                                }
                            }
                            foreach (string paramName in widthNames)
                            {
                                widthParameter = fabPart.LookupParameter(paramName);
                                if (widthParameter != null && widthParameter.HasValue)
                                {
                                    width = widthParameter.AsDouble();
                                    break;
                                }
                            }
                        }
                        Parameter insulationParameter = fabPart.LookupParameter("InsulationThickness");
                        if (insulationParameter != null && insulationParameter.HasValue && insulationParameter.AsDouble() > 0)
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

                    double totalHeight = height;
                    double totalWidth = width;

                    if (hasInsulation)
                    {
                        totalHeight += (insulationThickness * 2);
                        totalWidth += (insulationThickness * 2);
                    }

                    double finalHeight = totalHeight + (viewmodel.AddToHeight);
                    double finalWidth = totalWidth + (viewmodel.AddToWidth);
                    
                    //Debugging output for sizes
                    TaskDialog.Show("Size Debug", 
                        $"Original size: {width}\" × {height}\"\n" +
                        $"With insulation: {totalWidth}\" × {totalHeight}\"\n" +
                        $"After adding to size: {finalWidth}\" × {finalHeight}\"\n" +
                        $"AddToWidth: {viewmodel.AddToWidth}\"\n" +
                        $"AddToHeight: {viewmodel.AddToHeight}\"");

                    
                    double roundUpValue = viewmodel.RoundUpValue;
                    if (roundUpValue > 0)
                    {
                        finalHeight = Math.Ceiling(finalHeight / roundUpValue) * roundUpValue;
                        finalWidth = Math.Ceiling(finalWidth / roundUpValue) * roundUpValue;
                    }

                    double finalHeightFeet = finalHeight / 12.0;
                    double finalWidthFeet = finalWidth / 12.0;

                    var curve = locationCurve.Curve;
                    var clickPoint = reference.GlobalPoint;
                    var centerLinePoint = curve.Project(clickPoint).XYZPoint;
                    XYZ curveDirection;

                    if (curve is Line line)
                        curveDirection = line.Direction;
                    else
                    {
                        var parameter = curve.Project(clickPoint).Parameter;
                        curveDirection = curve.ComputeDerivatives(parameter, true).BasisX.Normalize();
                    }

                    var levelId = element.LevelId;
                    if (levelId == ElementId.InvalidElementId)
                        levelId = document.ActiveView.GenLevel?.Id ?? ElementId.InvalidElementId;

                    if (levelId == ElementId.InvalidElementId)
                    {
                        TaskDialog.Show("Error", "Could not determine level for placement.");
                        continue; // Try again
                    }

                    using (Transaction transaction = new Transaction(document, "Place Rectangular Wall Sleeve"))
                    {
                        transaction.Start();
                        if (!selectedSleeve.IsActive)
                        {
                            selectedSleeve.Activate();
                            document.Regenerate();
                        }

                        var placeSleeve = document.Create.NewFamilyInstance(
                            centerLinePoint,
                            selectedSleeve,
                            curveDirection,
                            document.GetElement(levelId) as Level,
                            StructuralType.NonStructural);

                        string[] heightParameterNames = { "Height", "Sleeve Height", "Nominal Height" };
                        bool heightSet = false;
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
                                else if (heightParameter.StorageType == StorageType.String)
                                {
                                    heightParameter.Set(finalHeight + "\"");
                                    heightSet = true;
                                    break;

                                }
                            }
                        }

                        string[] widthParameterNames = { "Width", "Sleeve Width", "Nominal Width" };
                        bool widthSet = false;
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
                                else if (widthParameter.StorageType == StorageType.String)
                                {
                                    widthParameter.Set(finalWidth + "\"");
                                    widthSet = true;
                                    break;
                                }
                            }
                        }

                        var rotationDirection = Line.CreateBound(centerLinePoint, centerLinePoint.Add(XYZ.BasisZ));
                        ElementTransformUtils.RotateElement(document, placeSleeve.Id, rotationDirection, Math.PI / 2);
                        if (!heightSet || !widthSet)
                        {
                            TaskDialog.Show("Warning",
                                "Could not set all sleeve dimensions. Check the family parameters.");
                        }

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
                    TaskDialogResult result = TaskDialog.Show("Continue?",
                        "An error occurred. Do you want to continue placing sleeves?",
                        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                    if (result == TaskDialogResult.No)
                    {
                        continuesSelecting = false;
                        break;
                    }
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
    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Rectangular Wall Sleeves";
        var buttonText = "Rectangular" + Environment.NewLine + "Wall Sleeves";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Place Rectangular Wall Sleeves to Ducts, Cable Trays, and Fabrication Ductwork",
                LargeImage = ImagineUtilities.LoadImage(assembly, "r2d2.png")
            });
    }

}