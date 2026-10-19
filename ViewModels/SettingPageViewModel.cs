using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;

namespace JSAT_UNDAI.ViewModels;

public partial class SettingPageViewModel : ViewModelBase, INavigationAware
{
    MainPageViewModel _mainPageViewModel;
    readonly ViewModelStore _viewModelStore;
    public string _tempAppData = string.Empty;
    readonly NavigationService _mapPageNavigationService;

    public SettingPageViewModel(string tempAppData, ViewModelStore viewModelStore, NavigationService mapPageNavigationService)
    {
        _tempAppData = tempAppData;
        _viewModelStore = viewModelStore;
        _mapPageNavigationService = mapPageNavigationService;
    }

    public void Initialize()
    {
        // Initialize properties or commands here
        try
        {
            // Example: Load initial data or set default values
            _mainPageViewModel = _viewModelStore.MainPageViewModel;
            _ = ErrorLoggingService.LogInfo("SettingPageViewModel initialized.");

            if (_mainPageViewModel != null)
            {
                // Handle the case where the MainPageViewModel is not available
            }
        }
        catch (Exception ex) 
        {
        
            // Handle exceptions
            _ = ErrorLoggingService.LogError("Error initializing SettingPageViewModel.", ex);
            throw;
        }
    }

    public void OnNavigatedTo()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated to SettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during SettingPage navigation entry.", ex);
        }
    }

    public void OnNavigatedFrom()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated away from SettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during SettingPage navigation exit.", ex);
        }
    }

    [RelayCommand]
    private void NavigateToMainPage()
    {
        try
        {
            _mapPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("Navigated to MainPage from SettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to MainPage from SettingPage.", ex);
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
}
