using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
[Journaling(JournalingMode.UsingCommandData)]
public class RealignMultiElements : IExternalCommand
{
    private readonly CurveMethods _curveMethods = new();
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uidocument = commandData.Application.ActiveUIDocument;
            var document = uidocument.Document;

            var pipeReference = uidocument.Selection.PickObject(ObjectType.Element,
                new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(),
                "Please Select a pipe or Duct to align the sleeve to");
            
            var pipeElement = document.GetElement(pipeReference);
            var locationCurve = pipeElement.Location as LocationCurve;

            if (locationCurve == null)
            {
                TaskDialog.Show("Error", "Selected element does not have a valid curve.");
                return Result.Failed;
            }
            
            var curve = locationCurve.Curve;

            var sleeveReferences = uidocument.Selection.PickObjects(ObjectType.Element,
                new SelectionFilters.ElementFilterByCategory(BuiltInCategory.OST_PipeAccessory),
                "Please select Wall Sleeve to realign");

            if (!sleeveReferences.Any())
            {
                TaskDialog.Show("Error", "No sleeves selected.");
                return Result.Cancelled; 
            }

            using (Transaction transaction = new Transaction(document, "RealignElements"))
            {
                transaction.Start();
                
                foreach (var sleeveReference in sleeveReferences)
                {
                    var sleeve = document.GetElement(sleeveReference) as FamilyInstance;
                    if (sleeve ==null)
                    {
                        TaskDialog.Show("Warning", $"Sleeve {sleeve.Id} has no valid location point. Skipping.");
                        continue;
                    }
                    var sleeveLocation = (sleeve.Location as LocationPoint)?.Point;
                    if (sleeveLocation == null)
                    {
                        TaskDialog.Show("Warning", $"Sleeve {sleeve.Id} has no valid location point. Skipping.");
                        continue;
                    }
                    
                    var projection = curve.Project(sleeveLocation);
                    var pointOnCurve = projection.XYZPoint;
                    var parameter = projection.Parameter;

                    var normalizedParameter = curve.ComputeNormalizedParameter(parameter);
                    var tangent = curve.ComputeDerivatives(normalizedParameter, true).BasisX.Normalize();
                    
                    var newSleeveLocation = new XYZ(pointOnCurve.X, pointOnCurve.Y, pointOnCurve.Z);
                    
                    sleeve.Location.Move(newSleeveLocation - sleeveLocation);
                    var line = Line.CreateBound(pointOnCurve, pointOnCurve.Add(tangent));
                    _curveMethods.AlignElementWithCurve(document, sleeve, line, newSleeveLocation);
                    
                    SystemInformation.SetSystemInformation(pipeElement, sleeve);
                    SystemInformation.SetPipeSizeDuctDiameter(pipeElement, sleeve);
                }
                transaction.Commit();
            }
            return Result.Succeeded;
        }
        catch (OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Error", $"Exception in RealignWallSleeves: {ex.Message}\n{ex.StackTrace}");
            return Result.Failed;
        }
    }
    
    public static PushButtonData CreatePushButtonData()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var buttonName = "RealignWallSleeves";
        var buttonText = "Realign\nWall Sleeves";
        var className = typeof(RealignMultiElements).FullName;

        return new PushButtonData(buttonName, buttonText, assembly.Location, className)
        {
            ToolTip = "Realign selected wall sleeves to a selected pipe or duct",
            LargeImage = ImagineUtilities.LoadImage(assembly, "deathStar-32.png")
        };
    }
}