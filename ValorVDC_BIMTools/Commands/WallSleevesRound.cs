using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.Commands.WallSleeve.ViewModels;
using ValorVDC_BIMTools.Commands.WallSleeve.Views;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;
using ValorVDC_BIMTools.Utilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;


namespace ValorVDC_BIMTools.Commands;

public partial class InsulationMethods
{
}



[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class WallSleevesRound : IExternalCommand
{
    private readonly InsulationMethods _insulationMethods = new InsulationMethods();
    private readonly CurveMethods _curveMethods = new CurveMethods();

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        {
            try
            {
                var uiDocument = commandData.Application.ActiveUIDocument;
                var document = uiDocument.Document;

                var viewModel = new WallSleeveViewModel(commandData);
                var view = new WallSleevesView(viewModel);

                if (view.ShowDialog() != true)
                    return Result.Succeeded;

                var selectedSleeve = viewModel.SelectedWallSleeve;
                if (selectedSleeve == null)
                {
                    TaskDialog.Show("Error", "No Wall Sleeve Type Selected");
                    return Result.Failed;
                }
                
                // DEBUG: Show selected sleeve info with lookup table details
                string lookupTableInfo = "No lookup table found";

                // Try to get lookup table info
                Parameter lookupTableParam = selectedSleeve.LookupParameter("Lookup Table Name");
                if (lookupTableParam != null && lookupTableParam.HasValue)
                {
                    lookupTableInfo = $"Lookup Table Name: {lookupTableParam.AsString()}";
                }
                else
                {
                    // Open the family temporarily to check available tables
                    Document doc = selectedSleeve.Document;
                    Family family = selectedSleeve.Family;
                    
                    try
                    {
                        Document familyDoc = doc.EditFamily(family);
                        if (familyDoc != null)
                        {
                            FamilySizeTableManager sizeTableManager = FamilySizeTableManager.GetFamilySizeTableManager(familyDoc, family.Id);
                            if (sizeTableManager != null)
                            {
                                var tableNames = sizeTableManager.GetAllSizeTableNames();
                                if (tableNames != null && tableNames.Count > 0)
                                {
                                    lookupTableInfo = $"Available Lookup Tables: {string.Join(", ", tableNames)}";
                                }
                            }
                            familyDoc.Close(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        lookupTableInfo = $"Error checking lookup tables: {ex.Message}";
                    }
                }

                TaskDialog.Show("Debug - Selected Sleeve", 
                    $"Selected Sleeve: {selectedSleeve.Name}\n" +
                    $"Family: {selectedSleeve.Family.Name}\n" +
                    $"Category: {selectedSleeve.Category.Name}\n" +
                    $"Lookup Table: {lookupTableInfo}");

                
                // Get available sleeve sizes from CSV file using the helper method
                double[] availableSleeveSize = GetElements.GetAvailableSleeveSizes(selectedSleeve);
                if (availableSleeveSize == null || availableSleeveSize.Length == 0)
                {
                    TaskDialog.Show("Fallback", "No sizes found in CSV, using default sizes");
                    availableSleeveSize = new double[] { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48 };
                }
                else
                {
                    TaskDialog.Show("Debug - Sleeve Sizes", 
                        $"Found {availableSleeveSize.Length} sizes from lookup table:\n" +
                        $"{string.Join(", ", availableSleeveSize)}");

                }


                
                var continueSelecting = true;

                while (continueSelecting)
                    try
                    {
                        var wallSleeves =
                            GetElements.GetElementByPartTypeAndPartSubType(document, "Sleeve", "Wall Sleeve");
                        var sleeve = selectedSleeve;

                        var reference = uiDocument.Selection.PickObject(ObjectType.Element,
                            new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(), "Please Select a pipe or duct");
                        var element = document.GetElement(reference);
                        
                        // DEBUG: Show selected element info
                        TaskDialog.Show("Debug - Selected Element", 
                            $"Selected Element: {element.Name}\n" +
                            $"Category: {element.Category.Name}\n" +
                            $"ID: {element.Id.IntegerValue}");

                        
                        var locationCurve = element.Location as LocationCurve;
                        if (locationCurve == null)
                        {
                            TaskDialog.Show("Error", "Selected Item Does Not Have A Valid Curve.");
                            continue;
                        }

                        var (nominalDiameter, insulationThickness, hasInsulation) = GetElements.GetElementDiameterAndInsulation(document, element);

                        if (nominalDiameter == 0)
                        {
                            TaskDialog.Show("Error", "Could not retrieve nominal diameter of the selected element.");
                            continue;
                        }

                        nominalDiameter *= 12;
                        insulationThickness *= 12;

                        double? totalDiameter = nominalDiameter;
                        if (hasInsulation)
                            totalDiameter += (insulationThickness * 2);
                        
                        double[] sleeveSize = availableSleeveSize;
                        
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

                        }
                        else
                        {
                            int baseSizeIndex = sleeveSize.Length - 1;
                            for (int i = 0; i < sleeveSize.Length; i++)
                            {
                                if (sleeveSize[i] > nominalDiameter)
                                {
                                    baseSizeIndex = i;
                                    break;
                                }
                            }
                            
                            finalSizeIndex = Math.Min(baseSizeIndex + 1, sleeveSize.Length - 1);
                        }
                        
                        var sleeveDiameterInches = sleeveSize[finalSizeIndex];
                        var sleeveDiameterFeet = sleeveDiameterInches / 12.0;

                        var curve = locationCurve.Curve;
                        var clickPoint = reference.GlobalPoint;
                        Line line = null;
                        if (curve is Line existingLine)
                            line = existingLine;
                        else
                        {
                            var parameter = curve.Project(clickPoint).Parameter;
                            var tangent = curve.ComputeDerivatives(parameter, true).BasisX.Normalize();
                            var pointOnCurve = curve.Project(clickPoint).XYZPoint;
                            line = Line.CreateBound(pointOnCurve, pointOnCurve.Add(tangent));
                        }

                        var (hostLevel, placemnentPoint) = _curveMethods.GetLevelAndPlacementPoint(
                            document, element, line, reference);
                        
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
                            foreach (var parameterame in possibleParameterNames)
                            {
                                var diameterParameter = sleeve.LookupParameter(parameterame);
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

                            var placeSleeve = document.Create.NewFamilyInstance(
                                    placemnentPoint,
                                    sleeve,
                                    hostLevel,
                                    hostLevel,
                                    StructuralType.NonStructural);
                            
                            _curveMethods.AlignElementWithCurve(document, placeSleeve, line, placemnentPoint);
                            if (!parameterNameSet)
                            {

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

    public static void CreateButton(RibbonPanel panel)
    {
        PushButtonUtility.CreatePushButton(
            panel,
            "Round Wall Sleeves",
            "Round" + Environment.NewLine + "Wall Sleeves",
            "Place Wall Sleeves to any Pipe, Duct, or other MEP Curves",
            "deathStar-32.png",
            MethodBase.GetCurrentMethod().DeclaringType
        );
    }
}