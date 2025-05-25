namespace ValorVDC_BIMTools.HelperMethods;

public class SystemInformation
{
    private void SetSystemInformation(Element mepElement, FamilyInstance sleeveInstance)
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
                Parameter serviceNameParameter = fabPart.LookupParameter("ServiceName");
                if (serviceNameParameter != null && serviceNameParameter.HasValue)
                    systemName = serviceNameParameter.AsString();
                else
                {
                    string[] serviceNameParams = { 
                        "Service", 
                        "Fabrication Service", 
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
                    
                }
            }

        }
    }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}