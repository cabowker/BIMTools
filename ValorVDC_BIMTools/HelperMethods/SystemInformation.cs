using System.Text;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace ValorVDC_BIMTools.HelperMethods;

public class SystemInformation
{
    public static void SetSystemInformation(Element mepElement, FamilyInstance sleeveInstance)
    {
        var systemName = "";
        var systemAbbreviation = "";

        try
        {
            if (mepElement is MEPCurve mepCurve)
            {
                var mepSystem = mepCurve.MEPSystem;
                if (mepSystem != null)
                {
                    systemName = mepSystem.Name;

                    var systemTypeParameter =
                        mepSystem.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                    if (systemTypeParameter != null && systemTypeParameter.HasValue)
                    {
                        systemAbbreviation = systemTypeParameter.AsString();
                    }
                    else
                    {
                        var systemTypeId = mepSystem.GetTypeId();
                        if (systemTypeId != ElementId.InvalidElementId)
                        {
                            var systemType = mepElement.Document.GetElement(systemTypeId);
                            if (systemType != null)
                            {
                                systemName = systemType.Name;

                                systemTypeParameter =
                                    systemType.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                                if (systemTypeParameter != null && systemTypeParameter.HasValue)
                                    systemAbbreviation = systemTypeParameter.AsString();
                            }
                        }
                    }
                }
                else
                {
                    var systemClassParam = mepCurve.get_Parameter(BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
                    if (systemClassParam != null && systemClassParam.HasValue) systemName = systemClassParam.AsString();
                }
            }

            else if (mepElement is FabricationPart fabPart)
            {
                var serviceNameParameter = fabPart.LookupParameter("Fabrication Service Name");
                if (serviceNameParameter != null && serviceNameParameter.HasValue)
                    systemName = serviceNameParameter.AsString();

                var serviceAbbrevParameter = fabPart.LookupParameter("ServiceAbbreviation");
                if (serviceAbbrevParameter != null && serviceAbbrevParameter.HasValue)
                    systemAbbreviation = serviceAbbrevParameter.AsString();

                if (!string.IsNullOrEmpty(systemName) && string.IsNullOrEmpty(systemAbbreviation))
                {
                    var mysteryWords = systemName.Split(' ');
                    if (mysteryWords.Length > 0)
                    {
                        var abbreviation = new StringBuilder();
                        foreach (var word in mysteryWords)
                            if (!string.IsNullOrEmpty(word) && char.IsLetter(word[0]))
                                abbreviation.Append(char.ToUpper(word[0]));

                        systemAbbreviation = abbreviation.ToString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(systemName))
            {
                string[] systemParameterNames = { "System", "System Name", "System Type" };
                foreach (var paramName in systemParameterNames)
                {
                    var systemParam = sleeveInstance.LookupParameter(paramName);
                    if (systemParam != null && !systemParam.IsReadOnly)
                    {
                        systemParam.Set(systemName);
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(systemAbbreviation))
            {
                string[] abbreviationParameters =
                {
                    "ServiceAbbreviation",
                    "System Abbreviation",
                    "SystemAbbreviation",
                    "Service Code",
                    "Abbreviation"
                };

                foreach (var parameterName in abbreviationParameters)
                {
                    var abbreviationParameter = sleeveInstance.LookupParameter(parameterName);
                    if (abbreviationParameter != null && !abbreviationParameter.IsReadOnly)
                    {
                        abbreviationParameter.Set(systemAbbreviation);
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            TaskDialog.Show("Warning", $"Could not set system information: {e.Message}");
        }
    }


    public static void SetPipeSizeDuctDiameter(Element mepElement, FamilyInstance familyInstance)
    {
        try
        {
            var document = mepElement.Document;

            var parameterName = "";
            double sizeValue = 0;

            if (mepElement is Pipe pipe)
            {
                parameterName = "Host Size";

                var pipeDiameterParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (pipeDiameterParameter != null && pipeDiameterParameter.HasValue)
                    sizeValue = pipeDiameterParameter.AsDouble();
            }
            else if (mepElement is Duct duct)
            {
                parameterName = "Host Size";

                var ductDiameterParam = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                if (ductDiameterParam != null && ductDiameterParam.HasValue) sizeValue = ductDiameterParam.AsDouble();
            }
            else if (mepElement is FabricationPart fabPart)
            {
                var fabDiameterParameter = fabPart.get_Parameter(BuiltInParameter.FABRICATION_PART_DIAMETER_IN);
                if (fabDiameterParameter != null && fabDiameterParameter.HasValue)
                {
                    sizeValue = fabDiameterParameter.AsDouble();

                    if (fabPart.Category.Id.IntegerValue == (double)BuiltInCategory.OST_PipeFitting ||
                        fabPart.Category.Id.IntegerValue == (double)BuiltInCategory.OST_PipeAccessory)
                        parameterName = "Host Size";
                    else if (fabPart.Category.Id.IntegerValue == (double)BuiltInCategory.OST_DuctFitting ||
                             fabPart.Category.Id.IntegerValue == (double)BuiltInCategory.OST_DuctAccessory)
                        parameterName = "Host Size";
                    else
                        parameterName = "Host Size";
                }
            }

            if (sizeValue > 0 && !string.IsNullOrEmpty(parameterName))
            {
                var sizeParameter = familyInstance.LookupParameter(parameterName);
                if (sizeParameter != null && !sizeParameter.IsReadOnly)
                    sizeParameter.Set(sizeValue);
                else
                    TaskDialog.Show("Info",
                        $"Parameter '{parameterName}' not found or is read-only on the sleeve family. " +
                        $"Please ensure the family has this parameter defined as an instance parameter.");
            }
        }
        catch (Exception e)
        {
            TaskDialog.Show("Warning", $"Could not set pipe size or duct diameter: {e.Message}");
        }
    }
}