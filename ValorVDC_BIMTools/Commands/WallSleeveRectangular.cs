using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands.WallSleeveRectangular.ViewModels;
using ValorVDC_BIMTools.Commands.WallSleeveRectangular.Views;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class WallSleevesRectangular : IExternalCommand
{
    private readonly PipeInsulationMethods _pipeInsulationMethods = new PipeInsulationMethods();
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIDocument uiDocument = commandData.Application.ActiveUIDocument;
            Document document = uiDocument.Document;

            var viewmodel = new RectangularWallSleeveViewModel(commandData);
            var view = new RectangularWallSleeveView(viewmodel);

            if (view.ShowDialog() != true)
                return Result.Succeeded;

            var selectedSleeve = viewmodel.SelectedWallSleeve;
            if (selectedSleeve != null)
            {
                TaskDialog.Show("Error", "No Rectangular Wall Sleeve Type Selected");
                return Result.Failed;
            }

            double addToHeight = viewmodel.AddToHeight / 12.0;
            double addToWidth = viewmodel.AddToWidth / 12.0;
            double roundUpValue = viewmodel.RoundUpValue / 12.0;

            var continuesSelecting = true;

            while (continuesSelecting)
            {
                
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        
        
        return Result.Succeeded;
    }
}