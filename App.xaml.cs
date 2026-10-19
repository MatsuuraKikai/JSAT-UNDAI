using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels;
using JSAT_UNDAI.ViewModels.Base;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace JSAT_UNDAI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    private readonly NavigationStore _navigationStore;
    private readonly ViewModelStore _viewModelStore;
    public readonly MainPageViewModel _mainPageViewModel;
    public readonly SettingPageViewModel _settingPageViewModel;
    private string tempAppData = string.Empty;

    public App()
    {
        try
        {
            tempAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JSAT_UNDAI");
            Directory.CreateDirectory(tempAppData);

            _navigationStore = new NavigationStore();
            _viewModelStore = new ViewModelStore();

            _settingPageViewModel = new SettingPageViewModel(tempAppData, _viewModelStore, new NavigationService(_navigationStore, () => _mainPageViewModel));
            _mainPageViewModel = new MainPageViewModel(tempAppData, _viewModelStore, new NavigationService(_navigationStore, () => _settingPageViewModel));

            _viewModelStore.MainPageViewModel = _mainPageViewModel;
            _viewModelStore.SettingPageViewModel = _settingPageViewModel;

            _mainPageViewModel.Initialize();
            _settingPageViewModel.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred during application initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            DispatcherUnhandledException += DispatcherUnhandledExceptionHandler;

            TaskScheduler.UnobservedTaskException += TaskExceptionHandler;

            AppDomain.CurrentDomain.UnhandledException += DomainUnhandledExceptionHandler;

            const string appName = "MKS.UNDAI";

            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                IntPtr existingWindow = FindWindow(null, "UNDAI");

                if (existingWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(existingWindow);
                }

                Shutdown();

                return;
            }

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

            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Application shutdown completed.");
            ErrorLoggingService.Shutdown();

            _mutex?.ReleaseMutex();
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

    private void DispatcherUnhandledExceptionHandler(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _ = ErrorLoggingService.LogError("Unhandled UI exception.", e.Exception);

        e.Handled = true;
    }

    private void TaskExceptionHandler(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        _ = ErrorLoggingService.LogError("Unhandled background task exception.", e.Exception);

        e.SetObserved();
    }

    private void DomainUnhandledExceptionHandler(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _ = ErrorLoggingService.LogError("Fatal application exception.", ex);
        }
    }
}
