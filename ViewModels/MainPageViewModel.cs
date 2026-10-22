using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;
using JSAT_UNDAI.Views;
using JSAT_UNDAI.Views.Base;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace JSAT_UNDAI.ViewModels;

public partial class MainPageViewModel : ViewModelBase, INavigationAware
{
    SettingPageViewModel _settingPageViewModel;
    readonly ViewModelStore _viewModelStore;
    public string _tempAppData = string.Empty;
    readonly NavigationService _settingPageNavigationService;
    readonly NavigationService _systemResetSettingPageNavigationService;
    UndaiSettingStorageService _undaiSettingStorageService;
    PinStorageService _pinStorageService;
    UndaiSettingEntry _undaiSettingEntry;

    //TCP Server Connection
    private TcpServerConnectionService? _tcpServerConnectionService;
    private Stopwatch? _disconnectHoldStopwatch;
    private CancellationTokenSource? _disconnectHoldCts;
    private const int DisconnectHoldMs = 2000;
    private bool _disconnectHoldCompleted;

    private Stopwatch? _logoutHoldStopwatch;
    private CancellationTokenSource? _logoutHoldCts;
    private const int LogoutHoldMs = 2000;

    private const string ConnectButtonRedColor = "#dc5349";
    private const string ConnectButtonBlueColor = "#0d35b2";
    private const string ConnectButtonHoverRedColor = "#873a35";
    private const string ConnectButtonHoverBlueColor = "#1E3375";
    private const string LogoutButtonHoverRedColor = "#35070a";

    private double _manualLatitude;
    private double _manualLongitude;

    private const int defaultPIN = 1056; // Default PIN for settings access

    // =====================================================================
    // Clock (top right)
    // =====================================================================
    private DispatcherTimer? _clockTimer;

    public MainPageViewModel
    (
        string tempAppData, 
        ViewModelStore viewModelStore, 
        UndaiSettingStorageService undaiSettingStorageService,
        PinStorageService pinStorageService,
        NavigationService settingPageNavigationService, 
        NavigationService systemResetSettingPageNavigationService)
    {
        _tempAppData = tempAppData;
        _viewModelStore = viewModelStore;
        _undaiSettingStorageService = undaiSettingStorageService;
        _pinStorageService = pinStorageService;

        _settingPageNavigationService = settingPageNavigationService;
        _systemResetSettingPageNavigationService = systemResetSettingPageNavigationService;

        _ = ErrorLoggingService.LogInfo("MainPageViewModel created.");
    }

    public void Initialize()
    {
        // Initialize properties or commands here
        try
        {
            // Example: Load data or initialize properties
            _settingPageViewModel = _viewModelStore.SettingPageViewModel;
            _ = ErrorLoggingService.LogInfo("MainPageViewModel initialized.");

            if (_settingPageViewModel != null)
            {
                // Handle the case where the SettingPageViewModel is not available
            }

            StartClock();
            InitializeTcpConnection();

            _undaiSettingEntry = _undaiSettingStorageService.LoadSetting();
        }
        catch (Exception ex)
        {
            // Handle exceptions
            _ = ErrorLoggingService.LogError("Error initializing MainPageViewModel.", ex);
            throw;
        }
    }

    [ObservableProperty]
    private bool _isServerConnected;

    [ObservableProperty]
    private string _connectionButtonLabel = "雲台通信";

    [ObservableProperty]
    private string _connectionButtonColor = ConnectButtonRedColor;

    [ObservableProperty]
    private string _connectionButtonHoverColor = ConnectButtonHoverRedColor;

    [ObservableProperty]
    private double _disconnectHoldProgress; // 0.0 to 1.0

    [ObservableProperty]
    private double _logoutHoldProgress; // 0.0 to 1.0

    [ObservableProperty]
    private bool _isDisconnectHolding;

    [ObservableProperty]
    private bool _isLogoutHolding;

    [ObservableProperty]
    private string _logoutButtonHoverColor = LogoutButtonHoverRedColor;

    [ObservableProperty]
    private string _timeText = string.Empty;

    [ObservableProperty]
    private string _dateText = string.Empty;

    [ObservableProperty]
    private bool _gpsStatus = false;

    [ObservableProperty]
    private string _gpsStatusText = "GPS捕捉中";

    [ObservableProperty]
    private string _gpsStatusColor = "#D98280";

    [ObservableProperty]
    private bool _modemStatus = false;

    [ObservableProperty]
    private string _modemStatusText = "モデム捕捉中";

    [ObservableProperty]
    private string _modemStatusColor = "#D98280";

    [ObservableProperty]
    private bool _isControlEnabled;

    [ObservableProperty]
    private bool _isHighSpeed;

    [ObservableProperty]
    private bool _isLowSpeed;

    [ObservableProperty]
    private bool _isInching = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetElevationAngleCommand))]
    private string _elevationAngleInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetAzimuthAngleCommand))]
    private string _azimuthAngleInput = string.Empty;

    [ObservableProperty]
    private string _currentRssi = "0.0";

    [ObservableProperty]
    private string _currentElevation = "0.0";

    [ObservableProperty]
    private string _currentAzimuth = "0.0";

    [ObservableProperty]
    private string _peakRssi = "0.0";

    [ObservableProperty]
    private string _peakElevation = "0.0";

    [ObservableProperty]
    private string _peakAzimuth = "0.0";

    [ObservableProperty]
    private string _targetPolarize = "0.0";

    [ObservableProperty]
    private string _targetElevation = "0.0";

    [ObservableProperty]
    private string _targetAzimuth = "0.0";

    [ObservableProperty]
    private string _unitLabel = "DD";

    [ObservableProperty]
    private string _latitude = "0.00000";

    [ObservableProperty]
    private string _longitude = "0.00000";

    [ObservableProperty]
    private bool _isHoming;

    [ObservableProperty]
    private bool _isPeakSearching;

    [ObservableProperty]
    private string _statusMessage = "READY";

    [ObservableProperty]
    private string _satelliteName = "-----";

    [ObservableProperty]
    private string _satelliteLongitude = "0.00000";

    [ObservableProperty]
    private bool _isManualGps;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AlarmResetCommand))]
    private bool _hasAlarm;

    private string CurrentSpeedModeName => IsHighSpeed ? "High" : IsLowSpeed ? "Low" : "Inching";

    private string? _activeJogDirection;

    private bool CanAlarmReset() => HasAlarm;

    private bool CanSetElevationAngle() => TryParseAngle(ElevationAngleInput, out _);

    private bool CanSetAzimuthAngle() => TryParseAngle(AzimuthAngleInput, out _);

    [RelayCommand]
    private void NavigateToSettingPage()
    {
        try
        {
            if (IsServerConnected)
            {
                var pinViewModel = new PinEntryViewModel(_pinStorageService.Verify);
                var dialog = new PinEntryDialog
                {
                    DataContext = pinViewModel,
                    Owner = Application.Current?.MainWindow,
                };

                if (dialog.ShowDialog() == true)
                {
                    _settingPageNavigationService.Navigate();
                    _ = ErrorLoggingService.LogInfo("Navigated to SettingPage.");
                }
                else
                {
                    _ = ErrorLoggingService.LogInfo("Navigation to SettingPage canceled by user.");
                }
            }
            else
            {
                _systemResetSettingPageNavigationService.Navigate();
                _ = ErrorLoggingService.LogInfo("Navigated to SystemResetSettingPage (not connected).");
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to SettingPage.", ex);
        }
    }

    [RelayCommand]
    private void ConnectButtonPressed()
    {
        try
        {
            _disconnectHoldCompleted = false;
            if (!IsServerConnected)
            {
                return; // long-press only matters while connected
            }

            _disconnectHoldCts = new CancellationTokenSource();
            _disconnectHoldStopwatch = Stopwatch.StartNew();
            IsDisconnectHolding = true;
            DisconnectHoldProgress = 0;

            _ = MonitorDisconnectHoldAsync(_disconnectHoldCts.Token);
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error starting disconnect hold.", ex);
        }
    }

    [RelayCommand]
    private void ConnectButtonReleased()
    {
        try
        {
            _disconnectHoldCts?.Cancel();
            _disconnectHoldStopwatch?.Stop();
            _disconnectHoldStopwatch = null;
            IsDisconnectHolding = false;
            DisconnectHoldProgress = 0;
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error stopping disconnect hold.", ex);
        }
    }

    [RelayCommand]
    private async Task ConnectButtonClicked()
    {
        try
        {
            if (_disconnectHoldCompleted)
            {
                _disconnectHoldCompleted = false;
                return;
            }

            if (IsServerConnected || _tcpServerConnectionService == null)
            {
                return; // already connected — disconnect only via long-press
            }

            await _tcpServerConnectionService.ConnectAsync();
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error on connect button click.", ex);
        }
    }

    [RelayCommand]
    private void LogoutButtonPressed()
    {
        try
        {
            _logoutHoldCts = new CancellationTokenSource();
            _logoutHoldStopwatch = Stopwatch.StartNew();
            IsLogoutHolding = true;
            LogoutHoldProgress = 0;

            _ = MonitorLogoutHoldAsync(_logoutHoldCts.Token);
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error starting logout hold.", ex);
        }
    }

    [RelayCommand]
    private void LogoutButtonReleased()
    {
        try
        {
            _logoutHoldCts?.Cancel();
            _logoutHoldStopwatch?.Stop();
            _logoutHoldStopwatch = null;
            IsLogoutHolding = false;
            LogoutHoldProgress = 0;
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error stopping logout hold.", ex);
        }
    }

    [RelayCommand]
    private void SelectSpeedMode(string? mode)
    {
        Safe(nameof(SelectSpeedMode), () =>
        {
            IsHighSpeed = mode == "High";
            IsLowSpeed = mode == "Low";
            IsInching = mode == "Inching";

            _ = ErrorLoggingService.LogInfo($"Speed mode: {CurrentSpeedModeName}");
            // TODO: send speed mode to the device.
        });
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
            _ = ErrorLoggingService.LogInfo($"Jog start: {direction} ({CurrentSpeedModeName})");
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
    private void SaveAngle()
    {
        Safe(nameof(SaveAngle), () =>
        {
            _ = ErrorLoggingService.LogInfo("Save angle requested.");
            // TODO: store the current angles.
        });
    }

    [RelayCommand]
    private void LoadAngle()
    {
        Safe(nameof(LoadAngle), () =>
        {
            _ = ErrorLoggingService.LogInfo("Load angle requested.");
            // TODO: recall the stored angles.
        });
    }

    [RelayCommand]
    private void ToggleUnit()
    {
        Safe(nameof(ToggleUnit), () =>
        {
            _ = ErrorLoggingService.LogInfo("Coordinate unit button pressed.");
            // TODO: switch the coordinate format and update UnitLabel / Latitude / Longitude.
        });
    }

    [RelayCommand]
    private void Home()
    {
        Safe(nameof(Home), () =>
        {
            _ = ErrorLoggingService.LogInfo("Home requested.");
            // TODO: send home command, set IsHoming while it runs.
        });
    }

    [RelayCommand]
    private void PeakSearch()
    {
        Safe(nameof(PeakSearch), () =>
        {
            _ = ErrorLoggingService.LogInfo("Peak search requested.");
            // TODO: send peak search command, set IsPeakSearching while it runs.
        });
    }

    /// <summary>Fires on button PRESS (ClickMode=Press). Highest priority, never gated.</summary>
    [RelayCommand]
    private void Stop()
    {
        Safe(nameof(Stop), () =>
        {
            _activeJogDirection = null;
            IsHoming = false;
            IsPeakSearching = false;

            _ = ErrorLoggingService.LogWarning("STOP pressed.");
            // TODO: send stop to the device FIRST, bypassing any queue.
        });
    }

    [RelayCommand(CanExecute = nameof(CanAlarmReset))]
    private void AlarmReset()
    {
        Safe(nameof(AlarmReset), () =>
        {
            _ = ErrorLoggingService.LogInfo("Alarm reset requested.");
            // TODO: send alarm reset; clear HasAlarm when the device confirms.
        });
    }

    [RelayCommand]
    private void EditInstallation()
    {
        var dialogViewModel = new InstallationEditViewModel(IsManualGps, _manualLatitude, _manualLongitude);

        var dialog = new InstallationEditDialog
        {
            DataContext = dialogViewModel,
            Owner = Application.Current?.MainWindow,
        };

        // false = cancelled or closed: nothing changes on the main screen.
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        bool wasManual = IsManualGps;
        IsManualGps = dialogViewModel.IsManualGps;

        if (IsManualGps)
        {
            _manualLatitude = dialogViewModel.ResultLatitude;
            _manualLongitude = dialogViewModel.ResultLongitude;

            // Assumes Latitude / Longitude are string properties (the screen shows "0.00000").
            Latitude = _manualLatitude.ToString("0.00000", CultureInfo.InvariantCulture);
            Longitude = _manualLongitude.ToString("0.00000", CultureInfo.InvariantCulture);
        }
        else if (wasManual)
        {
            // Back to auto: clear the typed values so they are not mistaken for a live GPS fix.
            // The next GPS receiver update fills them in again.
            Latitude = "0.00000";
            Longitude = "0.00000";
        }
    }

    private void InitializeTcpConnection()
    {
        _tcpServerConnectionService = new TcpServerConnectionService(_tempAppData);
        _tcpServerConnectionService.ConnectionStatusChanged += OnTcpConnectionStatusChanged;
        _tcpServerConnectionService.MessageReceived += OnTcpMessageReceived;
    }

    private void OnTcpConnectionStatusChanged(bool connected)
    {
        try
        {
            IsServerConnected = connected;
            ConnectionButtonLabel = connected ? "雲台通信" : "雲台接続";
            ConnectionButtonColor = connected ? ConnectButtonBlueColor : ConnectButtonRedColor;
            ConnectionButtonHoverColor = connected ? ConnectButtonHoverBlueColor : ConnectButtonHoverRedColor;

            _ = ErrorLoggingService.LogInfo($"TCP connection status changed: {(connected ? "Connected" : "Disconnected")}");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error updating TCP connection status UI.", ex);
        }
    }

    private void OnTcpMessageReceived(string rawMessage)
    {
        try
        {
            // Handle the received message here
            //_ = ErrorLoggingService.LogInfo($"Received TCP message: {rawMessage}");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error handling received TCP message.", ex);
        }
    }

    /// <summary>Starts the 1 s clock. Call once from Initialize().</summary>
    private void StartClock()
    {
        UpdateClock();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
    }

    private void UpdateClock()
    {
        DateTime now = DateTime.Now;
        TimeText = now.ToString("HH:mm:ss");
        DateText = now.ToString("yyyy/M/d");
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

    /// <summary>Controls follow the TCP connection state.</summary>
    partial void OnIsServerConnectedChanged(bool value)
    {
        IsControlEnabled = value;

        if (!value)
        {
            // Link lost while a jog button may be held: drop the held state.
            _activeJogDirection = null;
            // TODO: the device side must stop by itself on link loss (watchdog / keep-alive).

            _ = ErrorLoggingService.LogWarning("TCP connection lost. Controls disabled.");
            GpsStatus = false;
            ModemStatus = false;
        }
    }

    partial void OnGpsStatusChanged(bool value)
    {
        if (value)
        {
            GpsStatusText = "GPS捕捉";
            GpsStatusColor = "#719B82"; //#3785c3
        }
        else
        {
            GpsStatusText = "GPS捕捉中";
            GpsStatusColor = "#D98280";  //#ED524F
        }
    }

    partial void OnModemStatusChanged(bool value)
    {
        if (value)
        {
            ModemStatusText = "モデム捕捉";
            ModemStatusColor = "#719B82";
        }
        else
        {
            ModemStatusText = "モデム捕捉中";
            ModemStatusColor = "#D98280";
        }
    }

    public void DisconnectAndShutdown()
    {
        try
        {
            if (IsServerConnected)
            {
                _tcpServerConnectionService?.Disconnect();
            }

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.CloseConfirmed = true;
            }
        }
        finally
        {
            Application.Current.Shutdown();
        }
    }

    private async Task MonitorDisconnectHoldAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_disconnectHoldStopwatch != null)
                {
                    double elapsed = _disconnectHoldStopwatch.ElapsedMilliseconds;
                    DisconnectHoldProgress = Math.Min(elapsed / DisconnectHoldMs, 1.0);

                    if (elapsed >= DisconnectHoldMs)
                    {
                        _disconnectHoldCompleted = true;
                        _tcpServerConnectionService?.Disconnect();
                        return;
                    }
                }

                await Task.Delay(50, token);
            }
        }
        catch (OperationCanceledException)
        {
            // released before 3s — normal, not an error
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error monitoring disconnect hold duration.", ex);
        }
        finally
        {
            IsDisconnectHolding = false;
        }
    }

    private async Task MonitorLogoutHoldAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_logoutHoldStopwatch != null)
                {
                    double elapsed = _logoutHoldStopwatch.ElapsedMilliseconds;
                    LogoutHoldProgress = Math.Min(elapsed / LogoutHoldMs, 1.0);

                    if (elapsed >= LogoutHoldMs)
                    {
                        IsLogoutHolding = false;
                        LogoutHoldProgress = 0;
                        await ConfirmAndLogoutAsync();
                        return;
                    }
                }

                await Task.Delay(50, token);
            }
        }
        catch (OperationCanceledException)
        {
            // released before 2s — normal, not an error
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error monitoring logout hold duration.", ex);
        }
        finally
        {
            IsLogoutHolding = false;
        }
    }

    private async Task ConfirmAndLogoutAsync()
    {
        try
        {
            ConfirmDialog confirmDialog = new ConfirmDialog("終了確認", "アプリを終了しますか？")
            {
                Owner = Application.Current.MainWindow
            };

            if (confirmDialog.ShowDialog() == true)
            {
                _ = ErrorLoggingService.LogInfo("User confirmed logout. Disconnecting and shutting down.");
                DisconnectAndShutdown();
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error confirming logout.", ex);
        }
    }

    public void OnNavigatedTo()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated to MainPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during MainPage navigation entry.", ex);
        }
    }

    public void OnNavigatedFrom()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated away from MainPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during MainPage navigation exit.", ex);
        }
    }

    private bool TryParseAngle(string? text, out double angle)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out angle);
    }
}
