﻿using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace ValorVDC_BIMTools.HelperMethods;

public class SelectionFilters
{
    public class MepCurveAndFabFilter : ISelectionFilter
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
            if (element.Category == null)
                return false;
            int categoryId;
            if (int.TryParse(element.Document.Application.VersionNumber, out int versionNumber) && versionNumber >= 2024)
                categoryId = (int)element.Category.Id.Value;
            else
                categoryId = element.Category.Id.IntegerValue;
        
            return categoryId == (int)BuiltInCategory.OST_FabricationPipework ||
                   categoryId == (int)BuiltInCategory.OST_FabricationDuctwork;
        }
    }
    
    public class MepCurveAndFabFilterWithOutInsulation : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            if (element is PipeInsulation || element is DuctInsulation)
                return false;
            return element is MEPCurve || IsFabricationPart(element);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }

        private bool IsFabricationPart(Element element)
        {
            if (element.Category == null)
                return false;
            int categoryId;
            if (int.TryParse(element.Document.Application.VersionNumber, out int versionNumber) && versionNumber >= 2024)
                categoryId = (int)element.Category.Id.Value;
            else
                categoryId = element.Category.Id.IntegerValue;
        
            return categoryId == (int)BuiltInCategory.OST_FabricationPipework ||
                   categoryId == (int)BuiltInCategory.OST_FabricationDuctwork;
        }
    }
}