using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;


namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class WallSleeves : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        {
            try
            {
                var uiDocument = commandData.Application.ActiveUIDocument;
                var document = uiDocument.Document;

                var continueSelecting = true;

                while (continueSelecting)
                    try
                    {
                        var sleeve = new FilteredElementCollector(document)
                            .OfClass(typeof(FamilySymbol))
                            .OfCategory(BuiltInCategory.OST_PipeAccessory)
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(fam => fam.FamilyName.Contains("Wall Sleeve"));

                        var reference = uiDocument.Selection.PickObject(ObjectType.Element,
                            new MepCurveAndFabFilter(), "Please Select a pipe or duct");
                        var element = document.GetElement(reference);
                        var locationCurve = element.Location as LocationCurve;
                        if (locationCurve == null)
                        {
                            TaskDialog.Show("Error", "Selected Item Does Not Have A Valid Curve.");
                            continue;
                        }

                        double nominalDiameter = 0;
                        double insulationThickness = 0;
                        bool hasInsulation = false;
                        string debugInfo = "";
                        if (element is Pipe pipe)
                        {
                            var diameterParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                            if (diameterParameter != null && diameterParameter.HasValue)
                            {
                                nominalDiameter = diameterParameter.AsDouble();
                                debugInfo += $"Pipe outer diameter: {nominalDiameter * 12:F2} inches\n";

                            }
                            else
                            {
                                diameterParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                                if (diameterParameter != null && diameterParameter.HasValue)
                                {
                                    nominalDiameter = diameterParameter.AsDouble();
                                    debugInfo += $"Pipe parameter 'Diameter': {nominalDiameter * 12:F2} inches\n";

                                }
                                else
                                {
                                    diameterParameter = pipe.LookupParameter("Diameter");
                                    if (diameterParameter != null && diameterParameter.HasValue)
                                        nominalDiameter = diameterParameter.AsDouble();
                                    debugInfo += $"Pipe diameter from parameter: {nominalDiameter * 12:F2} inches\n";

                                }
                            }

                            PipeInsulation pipeInsulation = FindPipeInsulation(document, pipe);
                            if (pipeInsulation != null)
                            {
                                Parameter thicknessParameter =
                                    pipeInsulation.get_Parameter(BuiltInParameter.RBS_PIPE_INSULATION_THICKNESS);

                                if (thicknessParameter == null || !thicknessParameter.HasValue)
                                {
                                    string[] possibleParamNames = { "Thickness", "Insulation Thickness", "Pipe Insulation Thickness" };
                                    foreach (string paramName in possibleParamNames)
                                    {
                                        thicknessParameter = pipeInsulation.LookupParameter(paramName);
                                        if (thicknessParameter != null && thicknessParameter.HasValue && thicknessParameter.AsDouble() > 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (thicknessParameter != null && thicknessParameter.HasValue && thicknessParameter.AsDouble() > 0)
                                {
                                    insulationThickness = thicknessParameter.AsDouble();
                                    hasInsulation = true;
                                    debugInfo += $"Pipe insulation found. Thickness: {insulationThickness * 12:F2} inches\n";
                                    debugInfo += $"Parameter name: {thicknessParameter.Definition.Name}\n";
                                }
                                else
                                {
                                    debugInfo += "Pipe insulation found but couldn't get thickness parameter.\n";
                                    debugInfo += "Available parameters on insulation:\n";
                                    foreach (Parameter param in pipeInsulation.Parameters)
                                    {
                                        if (param.HasValue && param.StorageType == StorageType.Double)
                                        {
                                            debugInfo += $"- {param.Definition.Name}: {param.AsDouble() * 12:F2} inches\n";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                debugInfo += "No pipe insulation found for this element.\n";
                            }
                        }

                        else if (element is Duct duct)
                        {
                            var diameterParameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                            if (diameterParameter != null && diameterParameter.HasValue)
                            {
                                nominalDiameter = diameterParameter.AsDouble();
                            }

                            else
                            {
                                // Try to get by parameter name
                                diameterParameter = duct.LookupParameter("Diameter");
                                if (diameterParameter != null && diameterParameter.HasValue)
                                {
                                    nominalDiameter = diameterParameter.AsDouble();
                                    debugInfo += $"Duct diameter: {nominalDiameter * 12:F2} inches\n";

                                }
                                debugInfo += "This appears to be a rectangular duct, which is not supported.\n";

                            }

                            DuctInsulation ductInsulation = FindDuctInsulation(document, duct);
                            if (ductInsulation != null )
                            {
                                Parameter thicknessParameter = ductInsulation.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
            
                                if (thicknessParameter == null || !thicknessParameter.HasValue)
                                {
                                    // Try alternative parameter names if the built-in parameter doesn't work
                                    string[] possibleParamNames = { "Thickness", "Insulation Thickness", "Duct Insulation Thickness" };
                                    foreach (string paramName in possibleParamNames)
                                    {
                                        thicknessParameter = ductInsulation.LookupParameter(paramName);
                                        if (thicknessParameter != null && thicknessParameter.HasValue && thicknessParameter.AsDouble() > 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (thicknessParameter != null && thicknessParameter.HasValue && thicknessParameter.AsDouble() > 0)
                                {
                                    insulationThickness = thicknessParameter.AsDouble();
                                    hasInsulation = true;
                                    debugInfo += $"Duct insulation found. Thickness: {insulationThickness * 12:F2} inches\n";
                                    debugInfo += $"Parameter name: {thicknessParameter.Definition.Name}\n";
                                }
                                else
                                {
                                    debugInfo += "Duct insulation found but couldn't get thickness parameter.\n";
                                    debugInfo += "Available parameters on insulation:\n";
                                    foreach (Parameter param in ductInsulation.Parameters)
                                    {
                                        if (param.HasValue && param.StorageType == StorageType.Double)
                                        {
                                            debugInfo += $"- {param.Definition.Name}: {param.AsDouble() * 12:F2} inches\n";
                                        }
                                    }
                                }

                            }
                        }

                        if (nominalDiameter == 0)
                        {
                            TaskDialog.Show("Error", "Could not retrieve nominal diameter of the selected element.");
                            continue;
                        }

                        nominalDiameter *= 12;
                        insulationThickness *= 12;

                        double? totalDiameter = nominalDiameter;
                        if (hasInsulation)
                        {
                            totalDiameter += (insulationThickness * 2);
                            debugInfo += $"Total diameter with insulation: {totalDiameter:F2} inches\n";
                        }
                        else
                        {
                            debugInfo += $"Total diameter (no insulation): {totalDiameter:F2} inches\n";

                        }
                        
                        double[] sleeveSize = { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48 };
                        
                        int finalSizeIndex;
                        if (hasInsulation)
                        {
                            finalSizeIndex = sleeveSize.Length - 1;
                            for (int i = 0; i < sleeveSize.Length; i++)
                            {
                                if (sleeveSize[i] > totalDiameter)
                                {
                                    finalSizeIndex = i;
                                    break;
                                }
                            }
                            debugInfo += $"For insulated element, initial sleeve size: {sleeveSize[finalSizeIndex]} inches\n";
                        }
                        else
                        {
                            // For non-insulated elements:
                            // 1. Find the first sleeve size larger than nominal diameter
                            int baseSizeIndex = sleeveSize.Length - 1;
                            for (int i = 0; i < sleeveSize.Length; i++)
                            {
                                if (sleeveSize[i] > nominalDiameter)
                                {
                                    baseSizeIndex = i;
                                    break;
                                }
                            }
    
                            // 2. Go up one size
                            finalSizeIndex = Math.Min(baseSizeIndex + 1, sleeveSize.Length - 1);
                            debugInfo += $"Base sleeve size: {sleeveSize[baseSizeIndex]} inches\n";
                            debugInfo += $"After going up one size: {sleeveSize[finalSizeIndex]} inches\n";


                        }
                        TaskDialog.Show("Sleeve Size Calculation", debugInfo);

                        
                        var sleeveDiameterInches = sleeveSize[finalSizeIndex];
                        var sleeveDiameterFeet = sleeveDiameterInches / 12.0;

                        var curve = locationCurve.Curve;
                        var clickPoint = reference.GlobalPoint;
                        var centerLinePoint = curve.Project(clickPoint).XYZPoint;
                        XYZ curveDirection;

                        if (curve is Line line)
                        {
                            curveDirection = line.Direction;
                        }
                        else
                        {
                            var parameter = curve.Project(clickPoint).Parameter;
                            curveDirection = curve.ComputeDerivatives(parameter, true).BasisX.Normalize();
                        }

                        var startPoint = curve.GetEndPoint(0);
                        var endPoint = curve.GetEndPoint(1);
                        var geometricDirection = (endPoint - startPoint).Normalize();

                        var levelId = element.LevelId;
                        if (levelId == ElementId.InvalidElementId)
                            levelId = document.ActiveView.GenLevel?.Id ?? ElementId.InvalidElementId;

                        if (levelId == ElementId.InvalidElementId)
                        {
                            TaskDialog.Show("Error", "Could not determine level for placement.");
                            continue; // Try again
                        }

                        using (var transaction = new Transaction(document, "Place Sleeves"))
                        {
                            transaction.Start();
                            if (!sleeve.IsActive)
                            {
                                sleeve.Activate();
                                document.Regenerate();
                            }

                            var parameterNameSet = false;
                            var possibleParameterNames = new[]
                            {
                                "Nominal Diameter",
                                "Diameter",
                                "Size",
                                "Sleeve Diameter",
                                "Nominal Size"
                            };
                            foreach (var paramName in possibleParameterNames)
                            {
                                var diameterParam = sleeve.LookupParameter(paramName);
                                if (diameterParam != null && !diameterParam.IsReadOnly)
                                {
                                    if (diameterParam.StorageType == StorageType.Double)
                                    {
                                        diameterParam.Set(sleeveDiameterFeet);
                                        parameterNameSet = true;
                                        break;
                                    }

                                    if (diameterParam.StorageType == StorageType.String)
                                    {
                                        diameterParam.Set(sleeveDiameterInches + "\"");
                                        parameterNameSet = true;
                                        break;
                                    }
                                }
                            }

                            if (!parameterNameSet)
                            {
                                var placeSleeve = document.Create.NewFamilyInstance(
                                    centerLinePoint,
                                    sleeve,
                                    curveDirection,
                                    document.GetElement(levelId) as Level,
                                    StructuralType.NonStructural);
                                var rotationLine = Line.CreateBound(centerLinePoint, centerLinePoint.Add(XYZ.BasisZ));
                                ElementTransformUtils.RotateElement(
                                    document, placeSleeve.Id, rotationLine, Math.PI / 2);


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

                                if (parameterNameSet)
                                {
                                    // If we set a parameter on the instance, return that instance
                                    // Don't return here, let the transaction complete
                                    // We'll return Result.Succeeded at the end of the method
                                }
                                else
                                {
                                    // Still using default size
                                    TaskDialog.Show("Warning",
                                        "Could not set sleeve diameter parameter. Using default sleeve size.");
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
            catch (Exception exception)
            {
                message = exception.Message;
                return Result.Succeeded;
            }
        }
    }
        private PipeInsulation FindPipeInsulation(Document document, MEPCurve mepCurve)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document)
                .OfClass(typeof(PipeInsulation));
            return collector
                .Cast<PipeInsulation>()
                .FirstOrDefault(pi => pi.HostElementId == mepCurve.Id);
        }
        private DuctInsulation FindDuctInsulation(Document document, MEPCurve mepCurve)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document)
                .OfClass(typeof(DuctInsulation));
            return collector
                .Cast<DuctInsulation>()
                .FirstOrDefault(di => di.HostElementId == mepCurve.Id);
        }
    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Wall Sleeves";
        var buttonText = "Walls Sleeves";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Place Wall Sleeves to any Pipe, Duct, or other MEP Curves",
                LargeImage = ImagineUtilities.LoadImage(assembly, "deathStar-32.png")
            });
    }
}