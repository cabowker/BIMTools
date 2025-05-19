using System.Collections.ObjectModel;

namespace ValorVDC_BIMTools.HelperMethods
{
    public class GetElements
    {

    }
}

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

}