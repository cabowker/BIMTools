using Autodesk.Revit.UI;


namespace ValorVDC_BIMTools.Utilities;

public static class RevitUIUtils
{
        /// <summary>
    /// Zooms the active view to fit a collection of elements.
    /// </summary>
    /// <param name="uiDoc">The active UIDocument.</param>
    /// <param name="elementIds">A list of ElementIds to zoom to.</param>
    /// <param name="paddingFactor">A factor to control the padding around the elements (e.g., 1.2 for 20% padding).</param>
    public static void ZoomToElements(UIDocument uiDoc, IList<ElementId> elementIds, double paddingFactor = 1.2)
    {
        if (uiDoc == null || elementIds == null || !elementIds.Any()) return;

        var view = uiDoc.ActiveView;
        var doc = uiDoc.Document;

        BoundingBoxXYZ collectiveBbox = null;

        foreach (var id in elementIds)
        {
            var element = doc.GetElement(id);
            var bbox = element?.get_BoundingBox(view);
            if (bbox == null) continue; // Skip elements not visible in the view

            if (collectiveBbox == null)
            {
                collectiveBbox = bbox;
            }
            else
            {
                collectiveBbox.Min = new XYZ(
                    Math.Min(collectiveBbox.Min.X, bbox.Min.X),
                    Math.Min(collectiveBbox.Min.Y, bbox.Min.Y),
                    Math.Min(collectiveBbox.Min.Z, bbox.Min.Z));
                collectiveBbox.Max = new XYZ(
                    Math.Max(collectiveBbox.Max.X, bbox.Max.X),
                    Math.Max(collectiveBbox.Max.Y, bbox.Max.Y),
                    Math.Max(collectiveBbox.Max.Z, bbox.Max.Z));
            }
        }

        if (collectiveBbox == null) return;

        // Apply padding
        var center = (collectiveBbox.Min + collectiveBbox.Max) / 2.0;
        var extents = (collectiveBbox.Max - collectiveBbox.Min) / 2.0;
        extents *= paddingFactor;

        var paddedMin = center - extents;
        var paddedMax = center + extents;

        var uiView = uiDoc.GetOpenUIViews().FirstOrDefault(v => v.ViewId == view.Id);
        uiView?.ZoomAndCenterRectangle(paddedMin, paddedMax);
    }

}