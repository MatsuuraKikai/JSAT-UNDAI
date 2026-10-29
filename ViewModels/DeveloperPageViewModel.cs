using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Models;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;
using System.Globalization;

namespace JSAT_UNDAI.ViewModels;

public partial class DeveloperPageViewModel : ViewModelBase, INavigationAware
{
    readonly NavigationService _mainPageNavigationService;
    readonly NavigationService _settingPageNavigationService;
    readonly ViewModelStore _viewModelStore;

    private MainPageViewModel _mainPageViewModel;
    private SettingPageViewModel _settingPageViewModel;
    private MainPageModel _mainPageModel;

    public DeveloperPageViewModel(ViewModelStore viewModelStore, NavigationService mainPageNavigationService, NavigationService settingPageNavigationService)
    {
        _viewModelStore = viewModelStore;

        _mainPageNavigationService = mainPageNavigationService;
        _settingPageNavigationService = settingPageNavigationService;
    }

    public void Initialize()
    {
        try
        {
            _mainPageViewModel = _viewModelStore.MainPageViewModel;
            _settingPageViewModel = _viewModelStore.SettingPageViewModel;
            _mainPageModel = _mainPageViewModel._mainPageModel;

            _ = ErrorLoggingService.LogInfo("DeveloperPageViewModel initialized successfully.");

            if (_mainPageViewModel != null && _settingPageViewModel != null)
            {

            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error initializing DeveloperPageViewModel.", ex);
            throw;
        }
    }

    [ObservableProperty]
    private string _currentElevation = string.Empty;

    [ObservableProperty]
    private string _currentAzimuth = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetElevationAngleCommand))]
    private string _elevationAngleInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetAzimuthAngleCommand))]
    private string _azimuthAngleInput = string.Empty;

    [ObservableProperty]
    private string _logMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusError = false;

    [ObservableProperty]
    private string _wideSearchElevationStep = "1.0";

    [ObservableProperty]
    private string _wideSearchAzimuthStep = "1.0";

    private string? _activeJogDirection;

    private bool CanSetElevationAngle() => TryParseAngle(ElevationAngleInput, out _);

    private bool CanSetAzimuthAngle() => TryParseAngle(AzimuthAngleInput, out _);

    // ================= Commands =================

    [RelayCommand]
    private void NavigateToMainPage()
    {
        try
        {
            _mainPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("Navigated to MainPage from DeveloperPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to MainPage from DeveloperPage.", ex);
        }
    }

    [RelayCommand]
    private void NavigateToSettingPage()
    {
        try
        {
            _settingPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("Navigated to SettingPage from DeveloperPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to SettingPage from DeveloperPage.", ex);
        }
    }

    [RelayCommand]
    private void JogPressed(string? direction)
    {
        Safe(nameof(JogPressed), () =>
        {
            if (string.IsNullOrEmpty(direction))
            {
                return;
            }

            _activeJogDirection = direction;
            switch (_activeJogDirection)
            {
                case "Up":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 38);
                    break;
                case "Down":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 39);
                    break;
                case "CCW":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 36);
                    break;
                case "CW":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 37);
                    break;
            }
            _ = ErrorLoggingService.LogInfo($"Jog start: {direction}");
            // TODO: send "start jog" for this direction at the selected speed mode.
        });
    }

    [RelayCommand]
    private void JogReleased(string? direction)
    {
        Safe(nameof(JogReleased), () =>
        {
            if (_activeJogDirection is null)
            {
                return; // already stopped
            }

            string stopped = _activeJogDirection;
            switch (_activeJogDirection)
            {
                case "Up":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 0);
                    break;
                case "Down":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 0);
                    break;
                case "CCW":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 0);
                    break;
                case "CW":
                    _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 0);
                    break;
            }
            _activeJogDirection = null;

            _ = ErrorLoggingService.LogInfo($"Jog stop: {stopped}");
            // TODO: send "stop jog" to the device.
        });
    }

    [RelayCommand(CanExecute = nameof(CanSetElevationAngle))]
    private void SetElevationAngle()
    {
        Safe(nameof(SetElevationAngle), () =>
        {
            if (!TryParseAngle(ElevationAngleInput, out double angle))
            {
                return;
            }
            _mainPageModel.SendTcpMessage(message: $"R,1,{angle},0,0,0", isCommand: true, commandNo: 9);

            _ = ErrorLoggingService.LogInfo($"Go to elevation angle: {ElevationAngleInput}");
        });
    }

    [RelayCommand(CanExecute = nameof(CanSetAzimuthAngle))]
    private void SetAzimuthAngle()
    {
        Safe(nameof(SetAzimuthAngle), () =>
        {
            if (!TryParseAngle(AzimuthAngleInput, out double angle))
            {
                return;
            }
            _mainPageModel.SendTcpMessage(message: $"R,2,{angle},0,0,0", isCommand: true, commandNo: 10);

            _ = ErrorLoggingService.LogInfo($"Go to azimuth angle: {AzimuthAngleInput}");
        });
    }

    [RelayCommand]
    private void ElevationEncorderReset()
    {
        Safe(nameof(ElevationEncorderReset), () =>
        {
            _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 14);
            _ = ErrorLoggingService.LogInfo("Elevation encoder reset command sent.");
        });
    }

    [RelayCommand]
    private void AzimuthEncorderReset()
    {
        Safe(nameof(AzimuthEncorderReset), () =>
        {
            _mainPageModel.SendTcpMessage(isCommand: true, commandNo: 15);
            _ = ErrorLoggingService.LogInfo("Azimuth encoder reset command sent.");
        });
    }

    [RelayCommand]
    private void ResetStatusMessage()
    {
        Safe(nameof(ResetStatusMessage), () =>
        {
            LogMessage = string.Empty;
            _ = ErrorLoggingService.LogInfo("Status message reset.");
        });
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            _settingPageViewModel.Save();
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error saving UNDAI settings.", ex);
            SetStatus("登録に失敗しました", true);
        }
    }

    private static void Safe(string commandName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError($"Error in {commandName}.", ex);
        }
    }

    private bool TryParseAngle(string? text, out double angle)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out angle);
    }

    public void OnNavigatedTo()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated to DeveloperPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during DeveloperPage navigation entry.", ex);
        }
    }

    public void OnNavigatedFrom()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated away from DeveloperPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during DeveloperPage navigation exit.", ex);
        }
    }

    public void UpdateStatusMessage(string message)
    {
        int MaxLines = 20;

        var lines = (message + Environment.NewLine + LogMessage)
        .Split(Environment.NewLine)
        .Take(MaxLines);

        LogMessage = string.Join(Environment.NewLine, lines);
    }

    // ================= Helpers =================

    public void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
