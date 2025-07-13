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
public class RealignElement : IExternalCommand

{
    private readonly CurveMethods _curveMethods = new();

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiApplication = commandData.Application;
            var uidocument = uiApplication.ActiveUIDocument;
            var document = uidocument.Document;
            var view = uidocument.ActiveView;

            var continueCommand = true;
            while (continueCommand)
            {
                Element mepElement = null;
                Curve curve = null;

                try
                {
                    var pipeReference = uidocument.Selection.PickObject(ObjectType.Element,
                        new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(),
                        "Please Select a pipe or Duct to align the element to");

                    mepElement = document.GetElement(pipeReference);
                    var locationCurve = mepElement.Location as LocationCurve;

                    if (locationCurve == null)
                    {
                        TaskDialog.Show("Error", "Selected element does not have a valid curve.");
                        return Result.Failed;
                    }

                    curve = locationCurve.Curve;

                    var overrideGraphicSettings = new OverrideGraphicSettings();
                    overrideGraphicSettings.SetProjectionLineColor(new Color(0, 150, 255)); // Light blue
                    overrideGraphicSettings.SetProjectionLineWeight(5); // Make it thicker
                    overrideGraphicSettings.SetSurfaceForegroundPatternColor(new Color(173, 216,
                        230)); // Light blue fill
                    overrideGraphicSettings.SetSurfaceTransparency(50); // 50% transparency

                    var solidFillPattern = FillPatternElement.GetFillPatternElementByName(document,
                                               FillPatternTarget.Drafting, "<Solid fill>") ??
                                           FillPatternElement.GetFillPatternElementByName(document,
                                               FillPatternTarget.Model, "<Solid fill>");

                    if (solidFillPattern != null)
                    {
                        overrideGraphicSettings.SetSurfaceForegroundPatternId(solidFillPattern.Id);
                    }

                    using (var tempTransaction = new Transaction(document, "Temporary Color Override"))
                    {
                        tempTransaction.Start();
                        view.SetElementOverrides(mepElement.Id, overrideGraphicSettings);
                        tempTransaction.Commit();
                    }

                    try
                    {
                        var elementReference = uidocument.Selection.PickObject(ObjectType.Element,
                            new SelectionFilters.ElementFilterByCategory(BuiltInCategory.OST_PipeAccessory),
                            "Please select an Element to realign");

                        var element = document.GetElement(elementReference) as FamilyInstance;
                        if (element == null)
                        {
                            TaskDialog.Show("Warning", "Selected element is not a valid family instance.");
                            continue;

                        }

                        var elementLocation = (element.Location as LocationPoint)?.Point;
                        if (elementLocation == null)
                        {
                            TaskDialog.Show("Warning", $"Element {element.Id} has no valid location point.");
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
                    }
                    catch (OperationCanceledException)
                    {
                        // User cancelled element selection, exit command
                        continueCommand = false;
                    }
                    // catch (OperationCanceledException)
                    // {
                    //     // User cancelled element selection, exit command
                    //     continueCommand = false;
                    // }
                    finally
                    {
                        // Always remove the temporary color override
                        if (mepElement != null)
                        {
                            using (var cleanupTransaction = new Transaction(document, "Remove Color Override"))
                            {
                                cleanupTransaction.Start();
                                view.SetElementOverrides(mepElement.Id, new OverrideGraphicSettings());
                                cleanupTransaction.Commit();
                            }
                        }
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // User cancelled curve selection, exit command
                    continueCommand = false;
                }
                // catch (OperationCanceledException)
                // {
                //     // User cancelled curve selection, exit command
                //     continueCommand = false;
                // }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);

                    // Ask if user wants to continue after error
                    var result = TaskDialog.Show("Continue?",
                        "An error occurred. Do you want to continue with the command?",
                        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                    if (result == TaskDialogResult.No)
                    {
                        continueCommand = false;
                    }
                }
            }

            return Result.Succeeded;
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
        var buttonName = "RealignElement";
        var buttonText = "Realign Element";
        var className = typeof(RealignElement).FullName;

        return new PushButtonData(buttonName, buttonText, assembly.Location, className)
        {
            ToolTip = "Realign a element to a selected pipe or duct",
            LargeImage = ImagineUtilities.LoadImage(assembly, "r2d2.png")
        };
    }
}