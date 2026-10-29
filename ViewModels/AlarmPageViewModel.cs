using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Models;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;
using JSAT_UNDAI.Views.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace JSAT_UNDAI.ViewModels;

public partial class AlarmPageViewModel : ViewModelBase, INavigationAware
{
    readonly NavigationService _mainPageNavigationService;
    readonly NavigationService _settingPageNavigationService;
    readonly ViewModelStore _viewModelStore;

    private MainPageViewModel _mainPageViewModel;
    private SettingPageViewModel _settingPageViewModel;

    private AlarmDbService _alarmDbService;

    public ObservableCollection<AlarmRecord> AlarmRecords { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearAllCommand))]
    private int _alarmRecordsCount = 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    private AlarmRecord? _selectedAlarm;

    public AlarmPageViewModel(ViewModelStore viewModelStore, NavigationService mainPageNavigationService, NavigationService settingPageNavigationService)
    {
        _viewModelStore = viewModelStore;

        _mainPageNavigationService = mainPageNavigationService;
        _settingPageNavigationService = settingPageNavigationService;
    }

    public async Task Initialize()
    {
        try
        {
            _mainPageViewModel = _viewModelStore.MainPageViewModel;
            _settingPageViewModel = _viewModelStore.SettingPageViewModel;

            _ = ErrorLoggingService.LogInfo("AlarmPageViewModelを正常に初期化しました。");

            if (_mainPageViewModel != null && _settingPageViewModel != null)
            {
                _alarmDbService = new AlarmDbService(_mainPageViewModel._tempAppData);
                var items = await _alarmDbService.LoadLatestAsync(500);
                AlarmRecords.Clear();
                foreach (var item in items)
                {
                    AlarmRecords.Add(item);
                }
                Renumber();
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("AlarmPageViewModelの初期化に失敗しました。", ex);
            throw;
        }
    }

    [RelayCommand]
    private void NavigateToMainPage()
    {
        try
        {
            _mainPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("AlarmPageからMainPageに遷移しました。");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("AlarmPageからMainPageへの遷移に失敗しました。", ex);
        }
    }

    [RelayCommand]
    private void NavigateToSettingPage()
    {
        try
        {
            _settingPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("AlarmPageからSettingPageに遷移しました。");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("AlarmPageからSettingPageへの遷移に失敗しました。", ex);
        }
    }

    private bool CanDeleteSelected() => SelectedAlarm is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var target = SelectedAlarm;
        if (target is null || _alarmDbService is null)
        {
            return;
        }

        ConfirmDialog confirmDialog = new ConfirmDialog
        (
            "アラームの削除",
            $"「{target.AlarmName}」を削除しますか？"
        )
        {
            Owner = Application.Current.MainWindow
        };

        if (confirmDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _alarmDbService.DeleteAsync(target.Id);   // DB first, UI second
            AlarmRecords.Remove(target);
            Renumber();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"削除に失敗しました: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanClearAllAsync() => AlarmRecordsCount > 0;

    [RelayCommand(CanExecute = nameof(CanClearAllAsync))]
    private async Task ClearAllAsync()
    {
        if (_alarmDbService is null || AlarmRecords.Count == 0)
        {
            return;
        }
        ConfirmDialog confirmDialog = new ConfirmDialog
        (
            "アラーム履歴の消去",
            $"{AlarmRecords.Count}件のアラームをすべて削除しますか？この操作は取り消せません。"
        )
        {
            Owner = Application.Current.MainWindow
        };

        if (confirmDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _alarmDbService.ClearAllAsync(resetIds: true);
            AlarmRecords.Clear();
            SelectedAlarm = null;
            Renumber();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"消去に失敗しました: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task RaiseAlarmAsync(string name)
    {
        var alarm = new AlarmRecord
        {
            AlarmName = name,
            OccurredAt = DateTime.Now
        };

        await _alarmDbService.InsertAsync(alarm);

        // ObservableCollection must be modified on the UI thread
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            AlarmRecords.Insert(0, alarm);
            Renumber();
        });
    }

    private void Renumber()
    {
        for (int i = 0; i < AlarmRecords.Count; i++)
        {
            AlarmRecords[i].DisplayNo = i + 1;
        }

        AlarmRecordsCount = AlarmRecords.Count;
    }

    public void OnNavigatedTo()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("AlarmPageに遷移しました。");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("AlarmPageへの遷移処理中にエラーが発生しました。", ex);
        }
    }

    public void OnNavigatedFrom()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("AlarmPageから離脱しました。");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("AlarmPageからの遷移終了処理中にエラーが発生しました。", ex);
        }
    }
}
