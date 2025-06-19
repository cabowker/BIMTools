using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace ValorVDC_BIMTools.HelperMethods
{
    public static class InsulationMethods
    {
    }
}

namespace ValorVDC_BIMTools.Commands
{
    public partial class InsulationMethods
    {
        public PipeInsulation FindPipeInsulation(Document document, MEPCurve mepCurve)
        {
            var collector = new FilteredElementCollector(document)
                .OfClass(typeof(PipeInsulation));
            return collector
                .Cast<PipeInsulation>()
                .FirstOrDefault(pi => pi.HostElementId == mepCurve.Id);
        }

        public DuctInsulation FindDuctInsulation(Document document, MEPCurve mepCurve)
        {
            var collector = new FilteredElementCollector(document)
                .OfClass(typeof(DuctInsulation));
            return collector
                .Cast<DuctInsulation>()
                .FirstOrDefault(di => di.HostElementId == mepCurve.Id);
        }
    }
}