using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;
using System.Diagnostics;
using System.Windows;

namespace JSAT_UNDAI.ViewModels;

public partial class MainPageViewModel : ViewModelBase, INavigationAware
{
    SettingPageViewModel _settingPageViewModel;
    readonly ViewModelStore _viewModelStore;
    public string _tempAppData = string.Empty;
    readonly NavigationService _settingPageNavigationService;

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

    public MainPageViewModel(string tempAppData, ViewModelStore viewModelStore, NavigationService settingPageNavigationService)
    {
        _tempAppData = tempAppData;
        _viewModelStore = viewModelStore;
        _settingPageNavigationService = settingPageNavigationService;

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

            InitializeTcpConnection();
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

    [RelayCommand]
    private void NavigateToSettingPage()
    {
        try
        {
            _settingPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("Navigated to SettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to SettingPage.", ex);
        }
    }

    [RelayCommand]
    private void ClickMe()
    {
        try
        {
            // Example action for the ClickMe command
            _ = ErrorLoggingService.LogInfo("ClickMe command executed.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error executing ClickMe command.", ex);
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
            _ = ErrorLoggingService.LogInfo($"Received TCP message: {rawMessage}");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error handling received TCP message.", ex);
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
            MessageBoxResult result = MessageBox.Show(
                Application.Current.MainWindow,
                "アプリを終了しますか？",
                "ログアウト確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
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
}
