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
                
                double[] sleeveSize;
                try
                {
                    sleeveSize = GetElements.GetAvailableSizes(selectedSleeve);
                }
                catch (ArgumentException ex)
                {
                    TaskDialog.Show("Error", $"Could not get available sleeve size: ex.Message /newline" +
                                             $"Using these sizes: 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48)");
                    sleeveSize = new double[] { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48 };

                }
                catch (FormatException ex)
                {
                    TaskDialog.Show("Error", $"Could not get available sleeve size: {ex.Message}");
                    sleeveSize = new double[] { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48 };

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
                        
                        int finalSizeIndex;
                        bool requiresLargerSize = false;
                        
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
                            if (finalSizeIndex == sleeveSize.Length - 1 && sleeveSize[finalSizeIndex] <= totalDiameter)
                                requiresLargerSize = true;
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
                            
                            double requiredSize = nominalDiameter;
                            if (finalSizeIndex == sleeveSize.Length - 1 && baseSizeIndex == sleeveSize.Length - 1)
                            {
                                requiresLargerSize = true;
                            }
                        }
                        
                        if (requiresLargerSize)
                        {
                            double largestAvailableSize = sleeveSize[sleeveSize.Length - 1];
                            TaskDialogResult userChoice = TaskDialog.Show(
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
                                "NominalDiameter",
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