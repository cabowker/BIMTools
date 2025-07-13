using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

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
            var uiApplication = commandData.Application;
            var uidocument = uiApplication.ActiveUIDocument;
            var document = uidocument.Document;

            var pipeReference = uidocument.Selection.PickObject(ObjectType.Element,
                new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(),
                "Please Select a pipe or Duct to align the element to");

            var mepElement = document.GetElement(pipeReference);
            var locationCurve = mepElement.Location as LocationCurve;

            if (locationCurve == null)
            {
                TaskDialog.Show("Error", "Selected element does not have a valid curve.");
                return Result.Failed;
            }

            var curve = locationCurve.Curve;
            var selectedElementId = new List<ElementId> { mepElement.Id };
            var view = uidocument.ActiveView;

            var overrideGraphicSettings = new OverrideGraphicSettings();
            overrideGraphicSettings.SetProjectionLineColor(new Color(0, 150, 255)); // Light blue
            overrideGraphicSettings.SetProjectionLineWeight(5); // Make it thicker
            overrideGraphicSettings.SetSurfaceForegroundPatternColor(new Color(173, 216, 230)); // Light blue fill
            overrideGraphicSettings.SetSurfaceTransparency(50); // 50% transparency

            using (var tempTransaction = new Transaction(document, "Temporary Color Override"))
            {
                tempTransaction.Start();
                view.SetElementOverrides(mepElement.Id, overrideGraphicSettings);
                tempTransaction.Commit();
            }

            try
            {
                var continueSelecting = true;
                while (continueSelecting)
                    try
                    {
                        var elementReference = uidocument.Selection.PickObject(ObjectType.Element,
                            new SelectionFilters.ElementFilterByCategory(BuiltInCategory.OST_PipeAccessory),
                            "Please select an Element to realign");

                        var element = document.GetElement(elementReference) as FamilyInstance;
                        if (element == null)
                        {
                            TaskDialog.Show("Warning",
                                "Selected element is not a valid family instance. Please try again.");
                            uidocument.Selection.SetElementIds(selectedElementId);
                            continue;
                        }

                        var elementLocation = (element.Location as LocationPoint)?.Point;
                        if (elementLocation == null)
                        {
                            TaskDialog.Show("Warning",
                                $"Sleeve {element.Id} has no valid location point. Please try again.");
                            uidocument.Selection.SetElementIds(selectedElementId);
                            continue;
                        }

                        using (var transaction = new Transaction(document, "RealignElement"))
                        {
                            transaction.Start();

                            var projection = curve.Project(elementLocation);
                            var pointOnCurve = projection.XYZPoint;
                            var parameter = projection.Parameter;

                            var normalizedParameter = curve.ComputeNormalizedParameter(parameter);
                            var tangent = curve.ComputeDerivatives(normalizedParameter, true).BasisX.Normalize();

                            var newElementLocation = new XYZ(pointOnCurve.X, pointOnCurve.Y, pointOnCurve.Z);
                            element.Location.Move(newElementLocation - elementLocation);
                            var line = Line.CreateBound(pointOnCurve, pointOnCurve.Add(tangent));
                            _curveMethods.AlignElementWithCurve(document, element, line, newElementLocation);

                            SystemInformation.SetSystemInformation(mepElement, element);
                            SystemInformation.SetPipeSizeDuctDiameter(mepElement, element);

                            transaction.Commit();
                        }

                        uidocument.Selection.SetElementIds(selectedElementId);
                    }
                    catch (OperationCanceledException)
                    {
                        // User pressed ESC, exit the loop
                        continueSelecting = false;
                    }
                    catch (System.OperationCanceledException)
                    {
                        continueSelecting = false;
                    }

                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", ex.Message);
                    }
            }
            finally
            {
                using (var cleanupTransaction = new Transaction(document, "Remove Color Override"))
                {
                    cleanupTransaction.Start();
                    view.SetElementOverrides(mepElement.Id, new OverrideGraphicSettings());
                    cleanupTransaction.Commit();
                }
            }

            return Result.Succeeded;
        }
        catch (OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (System.OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Error", $"Exception in RealignElement: {ex.Message}\n{ex.StackTrace}");
            return Result.Failed;
        }
    }


    public static PushButtonData CreatePushButtonData()
    {
        var assembly = Assembly.GetExecutingAssembly();
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