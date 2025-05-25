using System.Collections.ObjectModel;

namespace ValorVDC_BIMTools.HelperMethods
{
    public class GetElements
    {
        public static ElementId GetClosestLevel(Document document, double elevation)
        {
            ElementId levelId = document.ActiveView?.GenLevel?.Id;
            if (levelId != null && levelId != ElementId.InvalidElementId)
                return levelId;

            var levelCollector = new FilteredElementCollector(document)
                .OfClass(typeof(Level));
            double closestdistance = double.MaxValue;
            Level closestLevel = null;

            foreach (Level  level in levelCollector)
            {
                try
                {
                    if (level.Elevation > elevation)
                    {
                        double distance = elevation - level.Elevation;
                        if (level.Elevation < closestdistance)
                        {
                            closestdistance = level.Elevation;
                            closestLevel = level;
                        }
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

/*
namespace ValorVDC_BIMTools.Commands.WallSleeve.ViewModels
{
    public sealed partial class WallSleeveViewModel
    {
        public void GetElementsByPartTypeAndSubType()
        {
            try
            {
                StatusMessage = "Loading Wall Sleeve Families...maybe";

                var wallSleeves = new List<FamilySymbol>();

                var pipeAccessories = new FilteredElementCollector(_document)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_PipeAccessory)
                    .Cast<FamilySymbol>()
                    .Where(fam =>
                    {
                        var partTypeParam = GetParameterCaseInsenitive(fam, "Part Type");
                        var partSubTypeParam = GetParameterCaseInsenitive(fam, "Part Sub-type");
                        return partTypeParam != null && partSubTypeParam != null &&
                               string.Equals(partTypeParam.AsString(), "Sleeve", StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(partSubTypeParam.AsString(), "Wall Sleeve",
                                   StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
                wallSleeves.AddRange(pipeAccessories);

                var ductAccessories = new FilteredElementCollector(_document)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_DuctAccessory)
                    .Cast<FamilySymbol>()
                    .Where(fam =>
                    {
                        var partTypeParam = GetParameterCaseInsenitive(fam, "Part Type");
                        var partSubTypeParam = GetParameterCaseInsenitive(fam, "Part Sub-type");
                        return partTypeParam != null && partSubTypeParam != null &&
                               string.Equals(partTypeParam.AsString(), "Sleeve", StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(partSubTypeParam.AsString(), "Wall Sleeve",
                                   StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
                wallSleeves.AddRange(ductAccessories);

                var genericModels = new FilteredElementCollector(_document)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_GenericModel)
                    .Cast<FamilySymbol>()
                    .Where(fam =>
                    {
                        var partTypeParam = GetParameterCaseInsenitive(fam, "Part Type");
                        var partSubTypeParam = GetParameterCaseInsenitive(fam, "Part Sub-type");
                        return partTypeParam != null && partSubTypeParam != null &&
                               string.Equals(partTypeParam.AsString(), "Sleeve", StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(partSubTypeParam.AsString(), "Wall Sleeve",
                                   StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
                wallSleeves.AddRange(genericModels);

                WallSleeveSymbols = new ObservableCollection<FamilySymbol>(wallSleeves);

                if (WallSleeveSymbols.Count > 0)
                    SelectedWallSleeve = WallSleeveSymbols[0];
                else
                    StatusMessage = "No wall Sleeve families found. Please load a wall sleeve family first.";
            }
            catch (Exception e)
            {
                StatusMessage = $"Error Loading Wall Sleeve Family: {e.Message}";
            }
        }
    }

}*/