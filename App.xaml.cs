using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels;
using JSAT_UNDAI.ViewModels.Base;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace JSAT_UNDAI;

public partial class App : Application
{
    private const string AppDataFolderName = "JSAT_UNDAI";
    private const string MutexName = "MKS.UNDAI";
    public const string MainWindowTitle = "JSAT UNDAI";

    private const string LogFolderName = "Logs";
    private const int SwRestore = 9;

    private static Mutex? _mutex;
    private bool _mutexOwned;

    private bool _isShowingErrorDialog;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private NavigationStore _navigationStore = null!;
    private ViewModelStore _viewModelStore = null!;

    public MainPageViewModel _mainPageViewModel = null!;
    public SettingPageViewModel _settingPageViewModel = null!;
    public SystemResetSettingPageViewModel _systemResetSettingPageViewModel = null!;
    public DeveloperPageViewModel _developerPageViewModel = null!;

    UndaiSettingStorageService _undaiSettingStorageService = null!;
    PinStorageService _pinStorageService = null!;

    private string _appDataDirectory = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            DispatcherUnhandledException += DispatcherUnhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += TaskExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += DomainUnhandledExceptionHandler;

            _mutex = new Mutex(true, MutexName, out bool createdNew);
            _mutexOwned = createdNew;

            if (!createdNew)
            {
                ActivateExistingInstance();
                Shutdown();
                return;
            }

            _appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppDataFolderName);
            Directory.CreateDirectory(_appDataDirectory);

            ErrorLoggingService.Initialize(Path.Combine(_appDataDirectory, LogFolderName));
            _ = ErrorLoggingService.LogInfo("Application starting.");

            _navigationStore = new NavigationStore();
            _viewModelStore = new ViewModelStore();

            _undaiSettingStorageService = new UndaiSettingStorageService(_appDataDirectory);
            _pinStorageService = new PinStorageService(_appDataDirectory);

            _settingPageViewModel = new SettingPageViewModel(
                _appDataDirectory,
                _viewModelStore, 
                _undaiSettingStorageService,
                _pinStorageService,
                new NavigationService(_navigationStore, () => _mainPageViewModel),
                new NavigationService(_navigationStore, () => _developerPageViewModel));

            _systemResetSettingPageViewModel = new SystemResetSettingPageViewModel(
                _appDataDirectory,
                _viewModelStore,
                _undaiSettingStorageService,
                new NavigationService(_navigationStore, () => _mainPageViewModel));

            _mainPageViewModel = new MainPageViewModel(
                _appDataDirectory,
                _viewModelStore,
                _undaiSettingStorageService,
                _pinStorageService,
                new NavigationService(_navigationStore, () => _settingPageViewModel),
                new NavigationService(_navigationStore, () => _systemResetSettingPageViewModel));

            _developerPageViewModel = new DeveloperPageViewModel(
                _viewModelStore,
                new NavigationService(_navigationStore, () => _mainPageViewModel),
                new NavigationService(_navigationStore, () => _settingPageViewModel));

            _viewModelStore.MainPageViewModel = _mainPageViewModel;
            _viewModelStore.SettingPageViewModel = _settingPageViewModel;
            _viewModelStore.SystemResetSettingPageViewModel = _systemResetSettingPageViewModel;
            _viewModelStore.DeveloperPageViewModel = _developerPageViewModel;

            _mainPageViewModel.Initialize();
            _settingPageViewModel.Initialize();
            _systemResetSettingPageViewModel.Initialize();
            _developerPageViewModel.Initialize();

            _navigationStore.CurrentViewModel = _mainPageViewModel;

            MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_navigationStore)
            };

            MainWindow.Show();

            _ = ErrorLoggingService.LogInfo("Main window started.");

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during application startup.", ex);

            MessageBox.Show(
                $"An error occurred during application startup:\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Application shutdown completed.");
            ErrorLoggingService.Shutdown();

            if (_mutexOwned)
            {
                _mutex?.ReleaseMutex();
            }

            _mutex?.Dispose();
            _mutex = null;
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during application shutdown.", ex);
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private static void ActivateExistingInstance()
    {
        IntPtr existingWindow = FindWindow(null, MainWindowTitle);

        if (existingWindow == IntPtr.Zero)
        {
            return;
        }

        if (IsIconic(existingWindow))
        {
            ShowWindow(existingWindow, SwRestore);
        }

        SetForegroundWindow(existingWindow);
    }

    private void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _ = ErrorLoggingService.LogError("Unhandled UI exception.", e.Exception);

        e.Handled = true;

        if (_isShowingErrorDialog)
        {
            return;
        }

        try
        {
            _isShowingErrorDialog = true;

            MessageBox.Show(
                $"An unexpected error occurred. Check the log.\n\n{e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isShowingErrorDialog = false;
        }
    }

    private void TaskExceptionHandler(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _ = ErrorLoggingService.LogError("Unhandled background task exception.", e.Exception);

        e.SetObserved();
    }

    private void DomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _ = ErrorLoggingService.LogError(
                e.IsTerminating
                    ? "Fatal application exception (process terminating)."
                    : "Unhandled application domain exception.",
                ex);
        }
    }
}
