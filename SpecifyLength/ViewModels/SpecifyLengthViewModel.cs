using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Autodesk.Revit.UI;
using SpecifyLength.Infrastructure;

namespace SpecifyLength.ViewModels;

public class SpecifyLengthViewModel : INotifyPropertyChanged
{
    private string _lengthFeet;
    private string _lengthInches;
    private readonly ExternalEvent _externalEvent;
    private readonly SpecifyLengthHandler _handler;

    public ICommand SubmitCommand { get; private set; }
    public ICommand CloseCommand { get; private set; }

    public string LengthFeet
    {
        get => _lengthFeet;
        set
        {
            if (_lengthFeet != value)
            {
                _lengthFeet = value;
                OnPropertyChanged();
            }
        }
    }

    public string LengthInches
    {
        get => _lengthInches;
        set
        {
            if (_lengthInches != value)
            {
                _lengthInches = value;
                OnPropertyChanged();
            }
        }
    }

    public SpecifyLengthViewModel(UIApplication uiApplication)
    {
        _handler = new SpecifyLengthHandler(uiApplication);
        _externalEvent = ExternalEvent.Create(_handler);

        SubmitCommand = new RelayCommand(ExecuteSubmit, CanExecuteSubmit);
        CloseCommand = new RelayCommand(ExecuteClose);
    }

    private void ExecuteSubmit()
    {
        double length = 0;

        if (!string.IsNullOrEmpty(_lengthFeet) && double.TryParse(_lengthFeet, out double feet))
        {
            length = feet;
        }

        if (!string.IsNullOrEmpty(_lengthInches) && double.TryParse(_lengthInches, out double inches))
        {
            length += inches / 12.0; // Convert inches to feet
        }

        _handler.SelectedLength = length;
        _handler.KeepRunning = true;
        _externalEvent.Raise();
    }

    private bool CanExecuteSubmit()
    {
        if (string.IsNullOrEmpty(_lengthFeet) && string.IsNullOrEmpty(_lengthInches))
            return false;

        if (!string.IsNullOrEmpty(_lengthFeet))
            return double.TryParse(_lengthFeet, out _);

        if (!string.IsNullOrEmpty(_lengthInches))
            return double.TryParse(_lengthInches, out _);

        return false;
    }

    private void ExecuteClose()
    {
        _handler.KeepRunning = false; // Ensure the handler stops running
        _externalEvent.Dispose();    // Clean up the ExternalEvent
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}