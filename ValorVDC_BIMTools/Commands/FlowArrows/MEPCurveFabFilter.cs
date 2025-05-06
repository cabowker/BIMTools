using Autodesk.Revit.UI.Selection;

namespace FlowArrows.Commands;

public class MEPCurveFabFilter : ISelectionFilter
{
    public bool AllowElement(Element element)
    {
        return element is MEPCurve || IsFabricationPart(element);
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return true;
    }

    private bool IsFabricationPart(Element element)
    {
        return element.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_FabricationPipework ||
               element.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_FabricationDuctwork;
    }
}