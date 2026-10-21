using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;

namespace JSAT_UNDAI.ViewModels;

public partial class SystemResetSettingPageViewModel : ViewModelBase, INavigationAware
{
    UndaiSettingStorageService _undaiSettingStorageService;
    UndaiSettingEntry savedUndaiSettingEntry;

    MainPageViewModel _mainPageViewModel;

    readonly ViewModelStore _viewModelStore;
    public string _tempAppData = string.Empty;
    readonly NavigationService _mapPageNavigationService;

    public SystemResetSettingPageViewModel(string tempAppData, ViewModelStore viewModelStore, UndaiSettingStorageService undaiSettingStorageService, NavigationService mapPageNavigationService)
    {
        _tempAppData = tempAppData;
        _viewModelStore = viewModelStore;
        _undaiSettingStorageService = undaiSettingStorageService;
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

            savedUndaiSettingEntry = _undaiSettingStorageService.LoadSetting();

            DefaultIPAddress = UndaiSettingEntry.DefaultUndaiIpAddress;
            CurrentIPAddress = savedUndaiSettingEntry.CurrentUndaiIpAddress;

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

    [ObservableProperty]
    private bool _resetUndaiImgVisibility = false;

    [ObservableProperty]
    private string _defaultIPAddress = string.Empty;

    [ObservableProperty]
    private string _currentIPAddress = string.Empty;

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
    private void ResetToDefault()
    {
        try
        {
            if(CurrentIPAddress != DefaultIPAddress)
            {
                CurrentIPAddress = DefaultIPAddress;
                ResetUndaiImgVisibility = true;

                _undaiSettingStorageService.WriteDefaultSettings();
                _ = ErrorLoggingService.LogInfo($"Reset IP address to default: {DefaultIPAddress}");
            }
            else
            {
                _ = ErrorLoggingService.LogInfo("Current IP address is already set to default.");
            }
            // Implement the logic to reset settings to default values here
            _ = ErrorLoggingService.LogInfo("Reset settings to default values.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error resetting settings to default values.", ex);
        }
    }

    public void OnNavigatedTo()
    {
        try
        {
            savedUndaiSettingEntry = _undaiSettingStorageService.LoadSetting();

            DefaultIPAddress = UndaiSettingEntry.DefaultUndaiIpAddress;
            CurrentIPAddress = savedUndaiSettingEntry.CurrentUndaiIpAddress;

            _ = ErrorLoggingService.LogInfo("Navigated to SystemResetSettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during SystemResetSettingPage navigation entry.", ex);
        }
    }

    public void OnNavigatedFrom()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated away from SystemResetSettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error during SystemResetSettingPage navigation exit.", ex);
        }
    }
}
