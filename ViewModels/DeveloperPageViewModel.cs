using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

            _ = ErrorLoggingService.LogInfo("DeveloperPageViewModel initialized successfully.");

            if (_mainPageViewModel != null && _settingPageViewModel != null)
            {
                // Additional initialization logic can be added here if needed
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
    private string _elevationAngleInput = string.Empty;

    [ObservableProperty]
    private string _azimuthAngleInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

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
            _ = ErrorLoggingService.LogInfo("Navigated to MainPage from SettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to MainPage from SettingPage.", ex);
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
            _ = ErrorLoggingService.LogInfo($"Jog start: {direction}");
            UpdateStatusMessage($"Jog start: {direction}");
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
            _activeJogDirection = null;

            _ = ErrorLoggingService.LogInfo($"Jog stop: {stopped}");
            UpdateStatusMessage($"Jog stop: {stopped}");
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

            _ = ErrorLoggingService.LogInfo($"Go to elevation angle: {ElevationAngleInput}");
            // TODO: validate range, then send the position command.
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

            _ = ErrorLoggingService.LogInfo($"Go to azimuth angle: {AzimuthAngleInput}");
            // TODO: validate range, then send the position command.
        });
    }

    [RelayCommand]
    private void ElevationEncorderReset()
    {
        Safe(nameof(ElevationEncorderReset), () =>
        {
            _ = ErrorLoggingService.LogInfo("Elevation encoder reset command sent.");
        });
    }

    [RelayCommand]
    private void AzimuthEncorderReset()
    {
        Safe(nameof(AzimuthEncorderReset), () =>
        {
            _ = ErrorLoggingService.LogInfo("Azimuth encoder reset command sent.");
        });
    }

    [RelayCommand]
    private void ResetStatusMessage()
    {
        Safe(nameof(ResetStatusMessage), () =>
        {
            StatusMessage = string.Empty;
            _ = ErrorLoggingService.LogInfo("Status message reset.");
        });
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

    void UpdateStatusMessage(string message)
    {
        int MaxLines = 20;

        var lines = (message + Environment.NewLine + StatusMessage)
        .Split(Environment.NewLine)
        .Take(MaxLines);

        StatusMessage = string.Join(Environment.NewLine, lines);
    }
}
