using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace JSAT_UNDAI.ViewModels.Base;

public partial class PinEntryViewModel : ObservableObject
{
    public const int PinLength = 4;

    private readonly Func<string, bool>? _verify;

    public PinEntryViewModel(Func<string, bool>? verify = null)
    {
        _verify = verify;
    }

    public event Action<bool>? CloseRequested;

    public event Action? ClearRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinDisplay))]
    private int _enteredLength;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public string EnteredPin { get; private set; } = string.Empty;

    public string PinDisplay
    {
        get
        {
            string result = string.Empty;
            for (int i = 0; i < PinLength; i++)
            {
                if (i < EnteredLength)
                {
                    result += "●";
                }
                else
                {
                    result += "_";
                }

                if(i < (PinLength - 1))
                {
                    result += " ";
                }
            }
            return result;
        }
    }

    public void SetPin(string pin)
    {
        EnteredPin = pin;
        EnteredLength = pin.Length;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void Ok()
    {
        if (EnteredPin.Length != PinLength || !EnteredPin.All(char.IsAsciiDigit))
        {
            ErrorMessage = $"{PinLength}桁の数字を入力してください";
            return;
        }

        if (_verify is not null && !_verify(EnteredPin))
        {
            ClearRequested?.Invoke();
            ErrorMessage = "暗証番号が違います";
            return;
        }

        CloseRequested?.Invoke(true);
    }
}