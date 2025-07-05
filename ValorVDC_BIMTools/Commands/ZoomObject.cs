using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
public class ZoomObject : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        var document = uiDocument.Document;
        View activeView = uiDocument.ActiveView;

        try
        {
            IList<ElementId> selectedIds = uiDocument.Selection.GetElementIds().ToList();
            if (!selectedIds.Any())
            {
                TaskDialog.Show("Error", "No object were selected. Please select one or more objects first.");
                return Result.Failed;
            }

            BoundingBoxXYZ combinedBox = null;
            foreach (ElementId id in selectedIds)
            {
                Element element = document.GetElement(id);
                BoundingBoxXYZ bbox = element.get_BoundingBox(activeView);
                if (bbox == null)
                {
                    TaskDialog.Show("Warning", $"Element with ID {id} is not visible in the current view.");
                        continue;
                }

                if (combinedBox == null)
                {
                    combinedBox = new BoundingBoxXYZ
                    {
                        Min = bbox.Min,
                        Max = bbox.Max
                    };
                }
                else
                {
                    combinedBox.Min = new XYZ(
                        Math.Min(combinedBox.Min.X, bbox.Min.X),
                        Math.Min(combinedBox.Min.Y, bbox.Min.Y),
                        Math.Min(combinedBox.Min.Z, bbox.Min.Z));
                    combinedBox.Max = new XYZ(
                        Math.Max(combinedBox.Max.X, bbox.Max.X),
                        Math.Max(combinedBox.Max.Y, bbox.Max.Y),
                        Math.Max(combinedBox.Max.Z, bbox.Max.Z));
                }
            }

            if (combinedBox == null)
            {
                TaskDialog.Show("Error", "None of the selected objects are visible in the current view.");
            }

            double paddingFactor = 3.0;
            XYZ boxSize = combinedBox.Max - combinedBox.Min;
            XYZ padding = new XYZ(
                boxSize.X * (paddingFactor - 1) /2,
                boxSize.Y * (paddingFactor - 1) /2,
                boxSize.Z * (paddingFactor - 1) /2);
            
            BoundingBoxXYZ paddedBox = new BoundingBoxXYZ()
            {
                Min = new XYZ(
                    combinedBox.Min.X - padding.X,
                    combinedBox.Min.Y - padding.Y,
                    combinedBox.Min.Z - padding.Z),
                Max = new XYZ(
                    combinedBox.Max.X + padding.X,
                    combinedBox.Max.Y + padding.Y,
                    combinedBox.Max.Z + padding.Z)
            };
            
            UIView uiView = uiDocument.GetOpenUIViews()
                .FirstOrDefault(v => v.ViewId == activeView.Id);
            if (uiView != null)
                uiView.ZoomAndCenterRectangle(paddedBox.Min, paddedBox.Max);
            else
            {
                TaskDialog.Show("Error", "Unable to access the current view for zooming.");
                return Result.Failed;
            }
            
            uiDocument.Selection.SetElementIds(selectedIds);
            return Result.Succeeded;
        }
        catch (Exception e)
        {
            TaskDialog.Show("Error", "An error occurred: " + e.Message);
            return Result.Failed;
        }
    }
    
    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Zoom Object";
        var buttonText = "Zoom" + Environment.NewLine + "Object";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Zooms to a pre-selected Object or Element",
                LargeImage = ImagineUtilities.LoadImage(assembly, "falcon.png")
            });
    }
}