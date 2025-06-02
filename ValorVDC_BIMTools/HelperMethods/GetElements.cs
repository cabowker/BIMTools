using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace ValorVDC_BIMTools.HelperMethods
{
    /// <summary>
    ///  Gets the Nominal Diameter of round Curves with or without Insulation [Ducts, Pipes, Fab Parts]
    /// </summary>
    public static class GetElements
    {

        public static (double nominalDiameter, double insulationThickness, bool hasInsulation)
            GetElementDiameterAndInsulation(
                Document document, Element element)
        {
            double nominalDiameter = 0;
            double insulationThickness = 0;
            var hasInsulation = false;

            switch (element)
            {
                case Pipe pipe:
                    var pipeDiameterParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                    if (pipeDiameterParameter != null && pipeDiameterParameter.HasValue)
                    {
                        nominalDiameter = pipeDiameterParameter.AsDouble();

                        var pipeInsulation = new FilteredElementCollector(document)
                            .OfClass(typeof(PipeInsulation))
                            .Cast<PipeInsulation>()
                            .FirstOrDefault(pi => pi.HostElementId == pipe.Id);

                        if (pipeInsulation != null)
                        {
                            var pipeInsulationThicknessParameter =
                                pipeInsulation.LookupParameter("Insulation Thickness");
                            if (pipeInsulationThicknessParameter != null &&
                                pipeInsulationThicknessParameter.HasValue &&
                                pipeInsulationThicknessParameter.AsDouble() > 0)
                            {
                                insulationThickness = pipeInsulationThicknessParameter.AsDouble();
                                hasInsulation = true;
                            }
                        }
                    }

                    break;

                case Duct duct:
                    var ductDiameterParameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                    if (ductDiameterParameter != null && ductDiameterParameter.HasValue)
                        nominalDiameter = ductDiameterParameter.AsDouble();

                    var ductInsulation = new FilteredElementCollector(document)
                        .OfClass(typeof(DuctInsulation))
                        .Cast<DuctInsulation>()
                        .FirstOrDefault(di => di.HostElementId == duct.Id);

                    if (ductInsulation != null)
                    {
                        var ductInsulationThicknessParameter = ductInsulation.LookupParameter("Insulation Thickness");
                        if (ductInsulationThicknessParameter != null &&
                            ductInsulationThicknessParameter.HasValue &&
                            ductInsulationThicknessParameter.AsDouble() > 0)
                        {
                            insulationThickness = ductInsulationThicknessParameter.AsDouble();
                            hasInsulation = true;
                        }
                    }

                    break;

                case FabricationPart fabricationPart:
                    var fabDiameterParameter =
                        fabricationPart.get_Parameter(BuiltInParameter.FABRICATION_PART_DIAMETER_IN);
                    if (fabDiameterParameter != null && fabDiameterParameter.HasValue)
                        nominalDiameter = fabDiameterParameter.AsDouble();

                    var insulationParameter =
                        fabricationPart.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
                    if (insulationParameter != null && insulationParameter.HasValue &&
                        insulationParameter.AsDouble() > 0)
                    {
                        insulationThickness = insulationParameter.AsDouble();
                        hasInsulation = true;
                    }

                    break;

                default:
                    break;
            }

            return (nominalDiameter, insulationThickness, hasInsulation);
        }


      public static double[] GetAvailableSleeveSizes(FamilySymbol familySymbol)
    {
        Document doc = familySymbol.Document;
        Family family = familySymbol.Family;

        // Open the family for editing
        Document familyDoc = doc.EditFamily(family);
        if (familyDoc == null)
        {
            return null;
        }

        try
        {
            // Get the FamilySizeTableManager
            FamilySizeTableManager sizeTableManager = FamilySizeTableManager.GetFamilySizeTableManager(familyDoc, family.Id);
            if (sizeTableManager == null)
            {
                familyDoc.Close(false);
                return null;
            }

            // Try to get the "Lookup Table Name" parameter
            string lookupTableName = null;
            Parameter lookupTableParam = familySymbol.LookupParameter("Lookup Table Name");
            if (lookupTableParam != null && lookupTableParam.HasValue)
            {
                lookupTableName = lookupTableParam.AsString();
            }

            // If no "Lookup Table Name" parameter, get the first available lookup table
            if (string.IsNullOrEmpty(lookupTableName))
            {
                var tableNames = sizeTableManager.GetAllSizeTableNames();
                if (tableNames == null || tableNames.Count == 0)
                {
                    familyDoc.Close(false);
                    return null;
                }
                lookupTableName = tableNames.First(); // Get the first table name
            }

            // Export the lookup table to a temporary CSV file
            string tempPath = Path.Combine(Path.GetTempPath(), $"{lookupTableName}.csv");
            bool exportSuccess = sizeTableManager.ExportSizeTable(lookupTableName, tempPath);
            if (!exportSuccess)
            {
                familyDoc.Close(false);
                return null;
            }

            // Read the CSV content
            string[] csvLines = File.ReadAllLines(tempPath);
            if (csvLines.Length < 2)
            {
                File.Delete(tempPath);
                familyDoc.Close(false);
                return null;
            }

            // Get the second row (index 1, assuming first row is headers)
            string secondRow = csvLines[1];

            // Parse the second row into an array, handling commas and quotes
            string[] secondRowValues = secondRow.Split(',')
                .Select(value => value.Trim('"')) // Remove quotes if present
                .ToArray();

            // Ensure there are enough columns
            if (secondRowValues.Length < 2)
            {
                File.Delete(tempPath);
                familyDoc.Close(false);
                return null;
            }

            // Convert second column (index 1) and any additional columns to doubles, ignoring invalid values
            var availableSleeveSizes = secondRowValues.Skip(1) // Start from second column
                .Select(value =>
                {
                    double result;
                    return double.TryParse(value, out result) ? result : (double?)null;
                })
                .Where(value => value.HasValue) // Keep only valid doubles
                .Select(value => value.Value)
                .ToArray();

            // Clean up the temporary file
            File.Delete(tempPath);
            return availableSleeveSizes;
        }
        finally
        {
            // Close the family document without saving
            familyDoc.Close(false);
        }
    }



    public static ElementId GetClosestLevel(Document document, double elevation)
        {
            ElementId levelId = document.ActiveView?.GenLevel?.Id;
            if (levelId != null && levelId != ElementId.InvalidElementId)
                return levelId;

            var levelCollector = new FilteredElementCollector(document)
                .OfClass(typeof(Level));
            double closestdistance = double.MaxValue;
            Level closestLevel = null;

            foreach (Level level in levelCollector)
            {
                try
                {
                    double distance = Math.Abs(level.Elevation - elevation);
                    if (distance < closestdistance)
                    {
                        closestdistance = distance;
                        closestLevel = level;
                    }
                }
                catch (Exception e)
                {
                    //
                }
            }
            closestLevel ??= levelCollector.Cast<Level>().FirstOrDefault();
            return closestLevel?.Id;
        }
        
       public static List<FamilySymbol> GetElementByPartTypeAndPartSubType(Document document, 
            string partType, string partSubType)
        {
            var elementList = new List<FamilySymbol>();
            
            // Categories where wall sleeves are commonly found
            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_DuctAccessory,
                BuiltInCategory.OST_GenericModel
            };
            
            foreach (var category in categories)
            {
                var categoryElements = new FilteredElementCollector(document)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(category)
                    .Cast<FamilySymbol>()
                    .Where(fam =>
                    {
                        var partTypeParam = GetParameterCaseInsensitive(fam, "Part Type");
                        var partSubTypeParam = GetParameterCaseInsensitive(fam, "Part Sub-type");
                        return partTypeParam != null && partSubTypeParam != null &&
                               string.Equals(partTypeParam.AsString(), partType, StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(partSubTypeParam.AsString(), partSubType, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
                
                elementList.AddRange(categoryElements);
            }
            
            return elementList;
        }
       
        private static Parameter GetParameterCaseInsensitive(Element element, string parameterName)
        {
            var parameters = element.Parameters;

            foreach (Parameter parameter in parameters)
            {
                if (string.Equals(parameter.Definition.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                    return parameter;
            }

            return null;
        }
    }
}