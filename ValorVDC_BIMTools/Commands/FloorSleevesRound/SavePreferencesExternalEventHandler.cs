using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands.FloorSleevesRound.ViewModels;

namespace ValorVDC_BIMTools.Commands.FloorSleevesRound;

public class SavePreferencesExternalEventHandler : IExternalEventHandler
{
    private static SavePreferencesExternalEventHandler _instance;
    private static ExternalEvent _event;
    private static FloorSleeveViewModel _viewModel;

    private SavePreferencesExternalEventHandler()
    {
    }

    public void Execute(UIApplication app)
    {
        if (_viewModel != null) _viewModel.SavePreferences();
    }

    public string GetName()
    {
        return "Save Floor Sleeve Preferences";
    }

    public static ExternalEvent GetEvent()
    {
        if (_instance == null)
        {
            _instance = new SavePreferencesExternalEventHandler();
            _event = ExternalEvent.Create(_instance);
        }

        return _event;
    }

    public static void SetViewModel(FloorSleeveViewModel viewModel)
    {
        _viewModel = viewModel;
    }
}