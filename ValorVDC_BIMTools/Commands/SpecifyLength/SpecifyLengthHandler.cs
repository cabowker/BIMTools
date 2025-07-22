using System.Windows.Input;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.Utilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

public class SpecifyLengthHandler : IExternalEventHandler
{
    private readonly ExternalCommandData _commandData;
    private readonly ExternalEvent _externalEvent;


    public SpecifyLengthHandler(ExternalCommandData commandData)
    {
        _commandData = commandData;
        keepRunning = true;
        _externalEvent = ExternalEvent.Create(this);
    }

    public double SelectedLength { get; set; }
    public bool keepRunning { get; set; }

    public void Execute(UIApplication application)
    {
        try
        {
            StopOnEscape();
            if (!keepRunning)
            {
                Stop();
                return;
            }

            var uiDocument = _commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            var pickedReference = uiDocument.Selection.PickObject(ObjectType.Element,
                new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(), "Please Select a Pipe");

            if (pickedReference == null)
            {
                TaskDialog.Show("Cancelled", "No element was selected");
                return;
            }

            var element = document.GetElement(pickedReference.ElementId);
            var success = false;
            // Handle MEP Curves
            if (element is MEPCurve mepCurve)
            {
                success = MepCurveAdjustmentUtility.AdjustMepCurveLength(mepCurve, SelectedLength,
                    pickedReference.GlobalPoint, document);

                if (!success) TaskDialog.Show("Error", "Failed to adjust MEP Curve length.");
            }

            // Handle Fabrication Parts
            else if (element is FabricationPart fabricationPart)
            {
                success = FabricationPartAdjustmentUtility.AdjustFabricationPartLength(fabricationPart, SelectedLength,
                    pickedReference.GlobalPoint, document);

                if (!success) TaskDialog.Show("Error", "Failed to adjust Fabrication Part length.");
            }
            else
            {
                TaskDialog.Show("Error", "Selected element is not an MEP Curve or Fabrication Part.");
            }
        }
        catch (OperationCanceledException)
        {
            keepRunning = false;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"An error occurred:\n{ex.Message}");
        }

        if (keepRunning)
            _externalEvent.Raise();
    }

    public string GetName()
    {
        return "Specify Length Interaction Handler";
    }


    public void Stop()
    {
        keepRunning = false;
        SpecifyLength.SpecifyLengthHandlerManager.SignalCompletion();
        TaskDialog.Show("Stopped", "Command has been stopped.");
    }

    public void RaiseEvent()
    {
        _externalEvent.Raise();
    }

    private void StopOnEscape()
    {
        if (Keyboard.IsKeyDown(Key.Escape))
        {
            keepRunning = false;
            TaskDialog.Show("Stopped", "Command has been stopped. Via Escape key.");
        }
    }
}