using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands.SpecifyLength;

namespace ValorVDC_BIMTools.SpecifyLength.ViewModels;

public class SpecifyLengthViewModel : INotifyPropertyChanged
{
    private readonly UIApplication _application;
    private double _lengthFeet;
    private double _lengthInches;
    private readonly SpecifyLengthHandler _handler;
    public SpecifyLengthViewModel(SpecifyLengthHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }


    public SpecifyLengthViewModel(UIApplication application)
    {
        _application = application;
        SubmitCommand = new RelayCommand(Submit, CanSubmit);
        CancelCommand = new RelayCommand(CloseWindow);
        _handler = new SpecifyLengthHandler(application.Application);
    }

    public double LengthFeet
    {
        get => _lengthFeet;
        set
        {
            if (_lengthFeet != value)
            {
                _lengthFeet = value;
                LengthInches = 0; // Clear inches value when feet changes
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public double LengthInches
    {
        get => _lengthInches;
        set
        {
            if (_lengthInches != value)
            {
                _lengthInches = value;
                LengthFeet = 0; // Clear feet value when inches changes
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ICommand SubmitCommand { get; }
    public ICommand CancelCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool CanSubmit() => LengthFeet > 0 || LengthInches > 0;

    private void Submit()
    {
        var specifiedLength = LengthFeet + LengthInches / 12.0; // Convert inches to feet
        _handler.SelectedLength = specifiedLength;
        _handler.Execute(_application); // Pass execution to the handler
    }

    private void CloseWindow()
    {
        // Call logic to close the view, likely via Messenger or Binding
    }
}
