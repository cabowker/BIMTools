using Autodesk.Revit.UI;

namespace ValorVDC_BIMTools.Commands.FloorSleevesRound;

/// <summary>
/// A generic external event handler that executes an action within the Revit API context.
/// This follows a robust singleton pattern to prevent memory leaks.
/// </summary>
public class RevitTaskExecutor : IExternalEventHandler
{
    // Using Lazy<T> for thread-safe singleton initialization
    private static readonly Lazy<RevitTaskExecutor> LazyInstance = new(() => new RevitTaskExecutor());

    private Action<UIApplication> _task;
    private ExternalEvent _externalEvent;

    // Private constructor to enforce singleton pattern
    private RevitTaskExecutor()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    /// <summary>
    /// Gets the singleton instance of the RevitTaskExecutor.
    /// </summary>
    public static RevitTaskExecutor Instance => LazyInstance.Value;

    /// <summary>
    /// Executes the stored task.
    /// </summary>
    public void Execute(UIApplication app)
    {
        try
        {
            _task?.Invoke(app);
        }
        finally
        {
            // Clear the task after execution to prevent holding references
            _task = null;
        }
    }

    public string GetName() => "Revit Task Executor";

    /// <summary>
    /// Raises the external event to execute a given task.
    /// </summary>
    /// <param name="task">The action to execute.</param>
    public void Raise(Action<UIApplication> task)
    {
        _task = task;
        _externalEvent.Raise();
    }
}
