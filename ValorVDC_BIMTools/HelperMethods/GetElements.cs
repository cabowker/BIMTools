using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace ValorVDC_BIMTools.HelperMethods;

/// <summary>
///     Gets the Nominal Diameter of round Curves with or without Insulation [Ducts, Pipes, Fab Parts]
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
        }

        return (nominalDiameter, insulationThickness, hasInsulation);
    }

    public static double[] GetAvailableSizes(FamilySymbol sleeveInstance)
    {
        if (sleeveInstance == null)
            throw new ArgumentNullException(nameof(sleeveInstance), "Sleeve Instance cannot be null.");

        var availableSizeParameter = sleeveInstance.LookupParameter("Available Sizes");

        if (availableSizeParameter == null) throw new ArgumentException("Parameter 'Available Sizes' not found.");

        if (!availableSizeParameter.HasValue) throw new ArgumentException("Parameter 'Available Sizes' has no value.");

        var sizeString = availableSizeParameter.AsString();

        if (string.IsNullOrEmpty(sizeString)) throw new ArgumentException("Parameter 'Available Sizes' is empty");


        var stringString = availableSizeParameter.AsString();
        if (string.IsNullOrEmpty(stringString))
            throw new ArgumentException("Parameter 'Available Sizes' is empty");

        var availableSizes = stringString
            .Split(',')
            .Select(s => s.Trim())
            .Select(s =>
            {
                if (double.TryParse(s, out var size)) return size;

                throw new FormatException($"Invalid number in size: {s}");
            })
            .ToArray();
        return availableSizes;
    }

    public static ElementId GetClosestLevel(Document document, double elevation)
    {
        var levelId = document.ActiveView?.GenLevel?.Id;
        if (levelId != null && levelId != ElementId.InvalidElementId)
            return levelId;

        var levelCollector = new FilteredElementCollector(document)
            .OfClass(typeof(Level));
        var closestdistance = double.MaxValue;
        Level closestLevel = null;

        foreach (Level level in levelCollector)
            try
            {
                var distance = Math.Abs(level.Elevation - elevation);
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
            if (string.Equals(parameter.Definition.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                return parameter;

        return null;
    }
}