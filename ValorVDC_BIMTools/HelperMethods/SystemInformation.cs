using System.Text;
using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace ValorVDC_BIMTools.HelperMethods;

public class SystemInformation
{
    public static void SetSystemInformation(Element mepElement, FamilyInstance sleeveInstance)
    {
        string systemName = "";
        string systemAbbreviation = "";

        try
        {
            if (mepElement is MEPCurve mepCurve)
            {
                MEPSystem mepSystem = mepCurve.MEPSystem;
                if (mepSystem != null)
                {
                    systemName = mepSystem.Name;

                    Parameter systemTypeParameter =
                        mepSystem.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                    if (systemTypeParameter != null && systemTypeParameter.HasValue)
                        systemAbbreviation = systemTypeParameter.AsString();
                    else
                    {
                        ElementId systemTypeId = mepSystem.GetTypeId();
                        if (systemTypeId != ElementId.InvalidElementId)
                        {
                            Element systemType = mepElement.Document.GetElement(systemTypeId);
                            if (systemType != null)
                            {
                                systemName = systemType.Name;
                                
                                systemTypeParameter = systemType.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                                if (systemTypeParameter != null && systemTypeParameter.HasValue)
                                    systemAbbreviation = systemTypeParameter.AsString();
                            }
                        }
                    }
                }
                else
                {
                    // If no system, try to get from system classification
                    Parameter systemClassParam = mepCurve.get_Parameter(BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
                    if (systemClassParam != null && systemClassParam.HasValue)
                    {
                        systemName = systemClassParam.AsString();
                    }
                }

            }
            
            else if (mepElement is FabricationPart fabPart)
            {
                Parameter serviceNameParameter = fabPart.LookupParameter("Fabrication Service Name");
                if (serviceNameParameter != null && serviceNameParameter.HasValue)
                    systemName = serviceNameParameter.AsString();
                else
                {
                    string[] serviceNameParams = { 
                        "Service", 
                        "Fabrication Service Name", 
                        "System Name", 
                        "SystemName", 
                        "System Type",
                        "ServiceName"
                    };
                    foreach (string paramName in serviceNameParams)
                    {
                        serviceNameParameter = fabPart.LookupParameter(paramName);
                        if (serviceNameParameter != null && serviceNameParameter.HasValue)
                        {
                            systemName = serviceNameParameter.AsString();
                            break;
                        }
                    }
                }
                Parameter serviceAbbrevParameter = fabPart.LookupParameter("ServiceAbbreviation");
                if (serviceAbbrevParameter != null && serviceAbbrevParameter.HasValue)
                {
                    systemAbbreviation = serviceAbbrevParameter.AsString();
                }
                else
                {
                    string[] serviceAbbrevParameters = { 
                        "ServiceAbbreviation", 
                        "System Abbreviation", 
                        "Abbreviation" 
                    };
                    foreach (string paramName in serviceAbbrevParameters)
                    {
                        serviceAbbrevParameter = fabPart.LookupParameter(paramName);
                        if (serviceAbbrevParameter != null && serviceAbbrevParameter.HasValue)
                        {
                            systemAbbreviation = serviceAbbrevParameter.AsString();
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(systemName) && string.IsNullOrEmpty(systemAbbreviation))
                {
                    string[] mysteryWords = systemName.Split(' ');
                    if (mysteryWords.Length > 0)
                    {
                        StringBuilder abbreviation = new StringBuilder();
                        foreach (string word in mysteryWords)
                        {
                            if (!string.IsNullOrEmpty(word) && char.IsLetter(word[0]))
                            {
                                abbreviation.Append(char.ToUpper(word[0]));
                            }
                        }
                        systemAbbreviation = abbreviation.ToString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(systemName))
            {
                string[] systemParameterNames = { "System", "System Name", "System Type" };
                foreach (string paramName in systemParameterNames)
                {
                    Parameter systemParam = sleeveInstance.LookupParameter(paramName);
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

                foreach (string parameterName in abbreviationParameters)
                {
                    Parameter abbreviationParameter = sleeveInstance.LookupParameter(parameterName);
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
}