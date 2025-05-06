using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FlowArrows.Commands;
using FlowArrows.ViewModels;
using FlowArrows.Views;

namespace ValorVDC_BIMTools.Commands.FlowArrows;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[Transaction(TransactionMode.Manual)]
public class FlowArrow : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIApplication uiApplication = commandData.Application;
            UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Document document = uiDocument.Document;
            
            
            var viewModel = new FlowArrowsViewModel(commandData);
            var view = new FlowArrowsView(viewModel);

            if (view.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            FamilySymbol selectedArrow = viewModel.SelectedFLowArrow;
            if (selectedArrow == null)
            {
                TaskDialog.Show("Error", "No Flow arrow tyoe selected.");
                return Result.Failed;
            }

            bool continueSelecting = true;
            while (continueSelecting)
            {
                try
                {
                    Reference reference = uiDocument.Selection.PickObject(ObjectType.Element,
                        new MEPCurveFabFilter(), "Please Select a pipe or duct");

                    Element element = document.GetElement(reference);
                    LocationCurve locationCurve = element.Location as LocationCurve;
                    if (locationCurve == null)
                    {
                        TaskDialog.Show("Error", "Selected element does not have a valid curve");
                        continue;
                    }

                    Curve curve = locationCurve.Curve;
                    XYZ clickPoint = reference.GlobalPoint;
                    XYZ centerLinePoint = curve.Project(clickPoint).XYZPoint;
                    XYZ curveDirection;

                    if (curve is Line line)
                        curveDirection = line.Direction;
                    else
                    {
                        double parameter = curve.Project(clickPoint).Parameter;
                        curveDirection = curve.ComputeDerivatives(parameter, true).BasisX.Normalize();
                    }

                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);
                    XYZ geometricDirection = (endPoint - startPoint).Normalize();

                    if (curveDirection.DotProduct(geometricDirection) < 0)
                        curveDirection = curveDirection.Negate();

                    bool isVertical = Math.Abs(curveDirection.Z) > 7071;
                    XYZ finalPlacementDirection = curveDirection;
                    XYZ rotationAxis = XYZ.Zero;
                    double rotationAngle = 0;
                    
                    if (isVertical)
                    {
                        bool flowDirectionIsUp = curveDirection.Z > 0;
                        XYZ verticalDirection = flowDirectionIsUp ? XYZ.BasisZ : XYZ.BasisZ.Negate();
                        XYZ originalDirection = curveDirection;
                        finalPlacementDirection = verticalDirection;
                        rotationAxis = new XYZ(1, 0, 0);

                        if (Math.Abs(originalDirection.X) > 0.001 || Math.Abs(originalDirection.Y) > 0.001)
                        {
                            XYZ horizontalCompont = new XYZ(originalDirection.X, originalDirection.Y, 0).Normalize();
                            rotationAxis = horizontalCompont.CrossProduct(verticalDirection).Normalize();
                            rotationAngle = Math.PI / 2;

                            // if (element is MEPCurve mepCurve && mepCurve.ConnectorManager != null)
                            // {
                            //     ConnectorSet connectors = mepCurve.ConnectorManager.Connectors;
                            //     if (connectors.Size == 2)
                            //     {
                            //         Connector connector1 = null;
                            //         Connector connector2 = null;
                            //
                            //         foreach (Connector  connector in connectors)
                            //         {
                            //             if (connector1 == null)
                            //                 connector1 = connector;
                            //             else
                            //                 connector2 = connector;
                            //         }
                            //
                            //         if (connector1 != null && connector2 != null) 
                            //         {
                            //             FlowDirectionType flowDirection1 = connector1.Direction;
                            //             FlowDirectionType flowDirection2 = connector1.Direction;
                            //             if (flowDirection1 == FlowDirectionType.In &&
                            //                 flowDirection2 == FlowDirectionType.Out)
                            //             {
                            //                 XYZ connectorDirection = (connector2.Origin - connector1.Origin).Normalize();
                            //                 flowDirectionIsUp = connectorDirection.Z > 0;
                            //             }
                            //             else if (flowDirection1 ==
                            //                      FlowDirectionType.Out && flowDirection2 == FlowDirectionType.In)
                            //             {
                            //                 XYZ connectorDirection = (connector1.Origin - connector2.Origin).Normalize();
                            //                 flowDirectionIsUp = connectorDirection.Z > 0;
                            //             }
                            //         }
                            //     }
                            // }
                            //
                            // if (!flowDirectionIsUp)
                            //     horizontalVector = horizontalVector.Negate();
                            // curveDirection = horizontalVector;
                        }
                    }
                    ElementId levelId = element.LevelId;
                    if (levelId == ElementId.InvalidElementId)
                    {
                        levelId = document.ActiveView.GenLevel?.Id ?? ElementId.InvalidElementId;
                    }
                    if (levelId == ElementId.InvalidElementId)
                    {
                        TaskDialog.Show("Error", "Could not determine level for placement.");
                        continue; // Try again
                    }
                    using (Transaction trans = new Transaction(document, "Place Flow Arrow"))
                    {
                        trans.Start();
                        
                        // Ensure the family symbol is active
                        if (!selectedArrow.IsActive)
                        {
                            selectedArrow.Activate();
                            document.Regenerate();
                        }
                        
                        // Create the family instance at the centerline point with proper orientation
                        FamilyInstance arrow = document.Create.NewFamilyInstance(
                            centerLinePoint,
                            selectedArrow,
                            curveDirection,
                            document.GetElement(levelId) as Level,
                            StructuralType.NonStructural);
                        if (isVertical && !rotationAxis.IsZeroLength() && rotationAngle != 0)
                        {
                            ElementTransformUtils.RotateElement(
                                document,
                                arrow.Id,
                                Line.CreateBound(centerLinePoint, centerLinePoint + rotationAxis),
                                rotationAngle);
                        }
                        
                        trans.Commit();
                    }

                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // User pressed ESC, exit the loop
                    continueSelecting = false;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                    // Continue the loop to allow another attempt
                }
            }
            
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}