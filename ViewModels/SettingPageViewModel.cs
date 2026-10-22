using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;
using JSAT_UNDAI.Views.Base;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace JSAT_UNDAI.ViewModels;

public partial class SettingPageViewModel : ViewModelBase, INavigationAware
{
    // ---- Input limits (ASSUMED) ----
    private const double SpeedMin = 1, SpeedMax = 100;      // [1-100] from the screen labels
    private const double OffsetLimit = 180;                  // origin offset: -180..180 deg
    private const double AngleRangeMax = 180;                // search range (±): 0..180 deg
    private const double StepAngleMin = 0.1, StepAngleMax = 180;
    private const double RssiMin = -150, RssiMax = 0;        // dBm
    private const double SettleSecondsMax = 60;
    private const double TurnbackThresholdMax = 100;         // dBm
    private const int ContinuousRunMinutesMin = 1, ContinuousRunMinutesMax = 1440;
    private const int SatelliteNameMaxLength = 32;

    UndaiSettingStorageService _undaiSettingStorageService;
    PinStorageService _pinStorageService;

    MainPageViewModel _mainPageViewModel;

    readonly ViewModelStore _viewModelStore;
    public string _tempAppData = string.Empty;
    readonly NavigationService _mainPageNavigationService;
    readonly NavigationService _developerPageNavigationService;

    // ================= Bindable text fields =================

    // Network Setting
    [ObservableProperty] private string _undaiIpAddress = string.Empty;
    [ObservableProperty] private string _undaiSubnetMask = string.Empty;
    [ObservableProperty] private string _undaiGateway = string.Empty;
    [ObservableProperty] private string _modemIpAddress = string.Empty;

    // Satellite Setting
    [ObservableProperty] private string _satelliteName = string.Empty;
    [ObservableProperty] private string _satelliteLongitude = string.Empty;

    // Undai Setting (水平角 = Azimuth / 仰角 = Elevation)
    [ObservableProperty] private string _originOffsetAzimuth = string.Empty;
    [ObservableProperty] private string _originOffsetElevation = string.Empty;
    [ObservableProperty] private string _manualHighSpeedAzimuth = string.Empty;
    [ObservableProperty] private string _manualHighSpeedElevation = string.Empty;
    [ObservableProperty] private string _manualLowSpeedAzimuth = string.Empty;
    [ObservableProperty] private string _manualLowSpeedElevation = string.Empty;
    [ObservableProperty] private string _jogAngleAzimuth = string.Empty;
    [ObservableProperty] private string _jogAngleElevation = string.Empty;

    // Wide Search Setting
    [ObservableProperty] private string _wideSearchSpeed = string.Empty;
    [ObservableProperty] private string _wideSearchAzimuthRange = string.Empty;
    [ObservableProperty] private string _wideSearchElevationRange = string.Empty;
    [ObservableProperty] private string _wideSearchSuccessRssi = string.Empty;

    // Detail Search Setting
    [ObservableProperty] private string _detailSearchSpeed = string.Empty;
    [ObservableProperty] private string _detailSearchStepAngle = string.Empty;
    [ObservableProperty] private string _detailSearchAzimuthRange = string.Empty;
    [ObservableProperty] private string _detailSearchElevationRange = string.Empty;
    [ObservableProperty] private string _detailSearchSuccessRssi = string.Empty;
    [ObservableProperty] private string _detailSearchStepSettleSeconds = string.Empty;
    [ObservableProperty] private string _detailSearchTurnbackThresholdLevel = string.Empty;   // 打切り減衰(dBm)

    // Endurance Test
    [ObservableProperty] private string _continuousRunMinutes = string.Empty;

    // Undai Version
    [ObservableProperty] private string _undaiVersionText = "0.0";

    int currentPIN;

    // ================= State =================

    // 登録 is disabled until the settings were loaded, so a failed load can't overwrite good values with blanks
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLoaded;

    // 停止 is greyed out while no continuous run is active
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isContinuousRunning;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isStatusError;

    public SettingPageViewModel
    (
        string tempAppData, 
        ViewModelStore viewModelStore, 
        UndaiSettingStorageService undaiSettingStorageService,
        PinStorageService pinStorageService, 
        NavigationService mainPageNavigationService,
        NavigationService developerPageNavigationService)
    {
        _tempAppData = tempAppData;
        _viewModelStore = viewModelStore;
        _undaiSettingStorageService = undaiSettingStorageService;
        _pinStorageService = pinStorageService;
        _mainPageNavigationService = mainPageNavigationService;
        _developerPageNavigationService = developerPageNavigationService;
    }

    public void Initialize()
    {
        try
        {
            _mainPageViewModel = _viewModelStore.MainPageViewModel;
            _ = ErrorLoggingService.LogInfo("SettingPageViewModel initialized.");

            if (_mainPageViewModel != null)
            {
                // Handle the case where the MainPageViewModel is not available
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error initializing SettingPageViewModel.", ex);
            throw;
        }
    }

    public void OnNavigatedTo()
    {
        try
        {
            _ = ErrorLoggingService.LogInfo("Navigated to SettingPage.");

            // Reload on every visit: unsaved edits from a previous visit are discarded
            LoadSettings();
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
    private void NavigateToDeveloperPage()
    {
        try
        {
            _developerPageNavigationService.Navigate();
            _ = ErrorLoggingService.LogInfo("Navigated to DeveloperPage from SettingPage.");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error navigating to DeveloperPage from SettingPage.", ex);
        }
    }

    // 登録: validate everything first, write nothing if any field is bad
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            // Start from the stored entry so fields not on this page are preserved
            var entry = _undaiSettingStorageService.LoadSetting();

            if (!TryApplyToEntry(entry, out var error))
            {
                SetStatus(error, true);
                return;
            }

            _undaiSettingStorageService.SaveSetting(entry);
            _ = ErrorLoggingService.LogInfo("UNDAI settings saved.");

            // Reload from disk: shows exactly what was stored (normalized numbers) and proves the round trip
            LoadSettings();
            if (IsLoaded)
            {
                SetStatus("登録しました", false);
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error saving UNDAI settings.", ex);
            SetStatus("登録に失敗しました", true);
        }
    }

    private bool CanSave() => IsLoaded;

    // 停止: TODO wire to the device. Stop must never be gated by anything except "a run is active" (UI only).
    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _ = ErrorLoggingService.LogInfo("TODO: Stop command (device protocol not implemented).");
    }

    private bool CanStop() => IsContinuousRunning;

    // 連続運転開始: TODO. IsContinuousRunning is intentionally NOT set here, since nothing is really running yet.
    [RelayCommand]
    private void StartContinuousRun()
    {
        _ = ErrorLoggingService.LogInfo("TODO: StartContinuousRun command (device protocol not implemented).");
    }

    [RelayCommand]
    private void AlarmReset()
    {
        _ = ErrorLoggingService.LogInfo("TODO: AlarmReset command (device protocol not implemented).");
    }

    [RelayCommand]
    private void AlarmHistory()
    {
        _ = ErrorLoggingService.LogInfo("TODO: AlarmHistory command (not implemented).");
    }

    [RelayCommand]
    private void PinReset()
    {
        var pinChangeDialog = new PinChangeDialog(_pinStorageService.Verify)
        {
            Owner = Application.Current.MainWindow
        };

        if (pinChangeDialog.ShowDialog() == true)
        {
            _pinStorageService.SetPin(pinChangeDialog.NewPin);
            _ = ErrorLoggingService.LogInfo("PinReset command completed successfully.");
        }
        else
        {
            _ = ErrorLoggingService.LogInfo("TODO: PinReset command (PIN storage not decided).");
        }
    }

    // ================= Load: UndaiSettingEntry -> text boxes =================

    private void LoadSettings()
    {
        try
        {
            var s = _undaiSettingStorageService.LoadSetting();

            UndaiIpAddress = s.CurrentUndaiIpAddress;
            UndaiSubnetMask = s.CurrentUndaiSubnetMask;
            UndaiGateway = s.CurrentUndaiGateway;
            ModemIpAddress = s.ModemIpAddress;

            OriginOffsetAzimuth = s.OriginOffsetAzimuth;
            OriginOffsetElevation = s.OriginOffsetElevation;
            ManualHighSpeedAzimuth = s.ManualHighSpeedAzimuth;
            ManualHighSpeedElevation = s.ManualHighSpeedElevation;
            ManualLowSpeedAzimuth = s.ManualLowSpeedAzimuth;
            ManualLowSpeedElevation = s.ManualLowSpeedElevation;
            JogAngleAzimuth = s.JogAngleAzimuth;
            JogAngleElevation = s.JogAngleElevation;

            SatelliteName = s.SatelliteName;
            SatelliteLongitude = s.SatelliteLongitude;

            WideSearchSpeed = s.WideSearchSpeed;
            WideSearchAzimuthRange = s.WideSearchAzimuthRange;
            WideSearchElevationRange = s.WideSearchElevationRange;
            WideSearchSuccessRssi = s.WideSearchSuccessRssi;

            DetailSearchSpeed = s.DetailSearchSpeed;
            DetailSearchStepAngle = s.DetailSearchStepAngle;
            DetailSearchAzimuthRange = s.DetailSearchAzimuthRange;
            DetailSearchElevationRange = s.DetailSearchElevationRange;
            DetailSearchSuccessRssi = s.DetailSearchSuccessRssi;
            DetailSearchStepSettleSeconds = s.DetailSearchStepSettleSeconds;
            DetailSearchTurnbackThresholdLevel = s.DetailSearchTurnbackThresholdLevel;

            ContinuousRunMinutes = s.ContinuousRunMinutes;
            UndaiVersionText = s.UndaiVersion;

            IsLoaded = true;
            SetStatus(string.Empty, false);
        }
        catch (Exception ex)
        {
            // Leave the fields as they are and keep 登録 disabled
            IsLoaded = false;
            _ = ErrorLoggingService.LogError("Error loading UNDAI settings.", ex);
            SetStatus("設定の読み込みに失敗しました", true);
        }
    }

    // ================= Save: text boxes -> UndaiSettingEntry (validated, normalized) =================

    // Writes into 'entry' only if EVERY field is valid (all values are parsed into locals first)
    private bool TryApplyToEntry(UndaiSettingEntry entry, out string error)
    {
        error = string.Empty;

        // ---- Network ----
        if (!TryParseIPv4(UndaiIpAddress, out var ip))
        {
            error = "雲台IPアドレスが正しくありません";
            return false;
        }
        if (!TryParseIPv4(UndaiSubnetMask, out var mask) || !IsContiguousMask(mask))
        {
            error = "サブネットマスクが正しくありません";
            return false;
        }
        if (!TryParseIPv4(UndaiGateway, out var gateway))
        {
            error = "デフォルトゲートウェイが正しくありません";
            return false;
        }
        if (!TryParseIPv4(ModemIpAddress, out var modem))
        {
            error = "無線機IPアドレスが正しくありません";
            return false;
        }

        // ---- UNDAI ----
        if (!TryParseNumber("原点オフセット(水平角)", OriginOffsetAzimuth, -OffsetLimit, OffsetLimit, out var originAz, out error)) return false;
        if (!TryParseNumber("原点オフセット(仰角)", OriginOffsetElevation, -OffsetLimit, OffsetLimit, out var originEl, out error)) return false;
        if (!TryParseNumber("手動高速(水平角)", ManualHighSpeedAzimuth, SpeedMin, SpeedMax, out var highAz, out error)) return false;
        if (!TryParseNumber("手動高速(仰角)", ManualHighSpeedElevation, SpeedMin, SpeedMax, out var highEl, out error)) return false;
        if (!TryParseNumber("手動低速(水平角)", ManualLowSpeedAzimuth, SpeedMin, SpeedMax, out var lowAz, out error)) return false;
        if (!TryParseNumber("手動低速(仰角)", ManualLowSpeedElevation, SpeedMin, SpeedMax, out var lowEl, out error)) return false;
        if (!TryParseNumber("寸動角度(水平角)", JogAngleAzimuth, StepAngleMin, StepAngleMax, out var jogAz, out error)) return false;
        if (!TryParseNumber("寸動角度(仰角)", JogAngleElevation, StepAngleMin, StepAngleMax, out var jogEl, out error)) return false;

        if (lowAz > highAz)
        {
            error = "手動低速(水平角)は手動高速(水平角)以下にしてください";
            return false;
        }
        if (lowEl > highEl)
        {
            error = "手動低速(仰角)は手動高速(仰角)以下にしてください";
            return false;
        }

        // ---- Wide search ----
        if (!TryParseNumber("広域サーチ速度", WideSearchSpeed, SpeedMin, SpeedMax, out var wideSpeed, out error)) return false;
        if (!TryParseNumber("広域サーチ水平角度", WideSearchAzimuthRange, 0, AngleRangeMax, out var wideAz, out error)) return false;
        if (!TryParseNumber("広域サーチ垂直角度", WideSearchElevationRange, 0, AngleRangeMax, out var wideEl, out error)) return false;
        if (!TryParseNumber("広域成功RSSIレベル", WideSearchSuccessRssi, RssiMin, RssiMax, out var wideRssi, out error)) return false;

        // ---- Detail search ----
        if (!TryParseNumber("詳細サーチ速度", DetailSearchSpeed, SpeedMin, SpeedMax, out var detSpeed, out error)) return false;
        if (!TryParseNumber("詳細サーチステップ角", DetailSearchStepAngle, StepAngleMin, StepAngleMax, out var detStep, out error)) return false;
        if (!TryParseNumber("詳細サーチ水平角度", DetailSearchAzimuthRange, 0, AngleRangeMax, out var detAz, out error)) return false;
        if (!TryParseNumber("詳細サーチ垂直角度", DetailSearchElevationRange, 0, AngleRangeMax, out var detEl, out error)) return false;
        if (!TryParseNumber("詳細サーチ成功RSSIレベル", DetailSearchSuccessRssi, RssiMin, RssiMax, out var detRssi, out error)) return false;
        if (!TryParseNumber("ステップ安定秒", DetailSearchStepSettleSeconds, 0, SettleSecondsMax, out var settle, out error)) return false;
        if (!TryParseNumber("打切り減衰", DetailSearchTurnbackThresholdLevel, 0, TurnbackThresholdMax, out var turnback, out error)) return false;

        // ---- Continuous run ----
        if (!TryParseInteger("連続運転終了時間", ContinuousRunMinutes, ContinuousRunMinutesMin, ContinuousRunMinutesMax, out var runMinutes, out error)) return false;

        // ---- Satellite ----
        // Name: free text, trimmed, max 32 chars. Empty = not set.
        if (!TryParseName("衛星名", SatelliteName, 1, SatelliteNameMaxLength, out var satName, out error)) return false;

        // Longitude: empty = not set, otherwise - 180..180 degrees.
        if (!TryParseNumber("衛星経度", SatelliteLongitude, -180, 180, out var satLon, out error)) return false;

        // ---- All valid: write normalized strings ----
        entry.CurrentUndaiIpAddress = ip.ToString();
        entry.CurrentUndaiSubnetMask = mask.ToString();
        entry.CurrentUndaiGateway = gateway.ToString();
        entry.ModemIpAddress = modem.ToString();

        entry.OriginOffsetAzimuth = FormatNumber(originAz);
        entry.OriginOffsetElevation = FormatNumber(originEl);
        entry.ManualHighSpeedAzimuth = FormatNumber(highAz);
        entry.ManualHighSpeedElevation = FormatNumber(highEl);
        entry.ManualLowSpeedAzimuth = FormatNumber(lowAz);
        entry.ManualLowSpeedElevation = FormatNumber(lowEl);
        entry.JogAngleAzimuth = FormatNumber(jogAz);
        entry.JogAngleElevation = FormatNumber(jogEl);

        entry.SatelliteName = satName;
        entry.SatelliteLongitude = FormatNumber(satLon);

        entry.WideSearchSpeed = FormatNumber(wideSpeed);
        entry.WideSearchAzimuthRange = FormatNumber(wideAz);
        entry.WideSearchElevationRange = FormatNumber(wideEl);
        entry.WideSearchSuccessRssi = FormatNumber(wideRssi);

        entry.DetailSearchSpeed = FormatNumber(detSpeed);
        entry.DetailSearchStepAngle = FormatNumber(detStep);
        entry.DetailSearchAzimuthRange = FormatNumber(detAz);
        entry.DetailSearchElevationRange = FormatNumber(detEl);
        entry.DetailSearchSuccessRssi = FormatNumber(detRssi);
        entry.DetailSearchStepSettleSeconds = FormatNumber(settle);
        entry.DetailSearchTurnbackThresholdLevel = FormatNumber(turnback);

        entry.ContinuousRunMinutes = runMinutes.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    // ================= Helpers =================

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    // "0.0", "0.1", "0.25": at least one decimal, up to three
    private static string FormatNumber(double value) =>
        value.ToString("0.0###", CultureInfo.InvariantCulture);

    // Touch-panel IME input: full-width digits / "．" / "－" / ideographic space become ASCII (Unicode FormKC)
    private static string Normalize(string? text) =>
        (text ?? string.Empty).Normalize(NormalizationForm.FormKC).Trim();

    private static bool TryParseNumber(string label, string? text, double min, double max, out double value, out string error)
    {
        error = string.Empty;

        if (!double.TryParse(Normalize(text), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value))
        {
            error = $"{label}は数値で入力してください";
            return false;
        }

        if (value < min || value > max)
        {
            error = $"{label}は{min.ToString(CultureInfo.InvariantCulture)}～{max.ToString(CultureInfo.InvariantCulture)}の範囲で入力してください";
            return false;
        }

        return true;
    }

    private static bool TryParseInteger(string label, string? text, int min, int max, out int value, out string error)
    {
        error = string.Empty;

        if (!int.TryParse(Normalize(text), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
        {
            error = $"{label}は整数で入力してください";
            return false;
        }

        if (value < min || value > max)
        {
            error = $"{label}は{min}～{max}の範囲で入力してください";
            return false;
        }

        return true;
    }

    // IPAddress.TryParse accepts "1" or "1.2", so require exactly 4 dot-separated parts
    private static bool TryParseIPv4(string? text, out IPAddress address)
    {
        string normalized = Normalize(text);
        if (normalized.Count(c => c == '.') == 3 &&
            IPAddress.TryParse(normalized, out var parsed) &&
            parsed.AddressFamily == AddressFamily.InterNetwork)
        {
            address = parsed;
            return true;
        }

        address = IPAddress.None;
        return false;
    }

    // Valid mask = ones followed by zeros only (255.255.0.0 yes, 255.0.255.0 no)
    private static bool IsContiguousMask(IPAddress mask)
    {
        byte[] b = mask.GetAddressBytes();
        uint m = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        uint inverted = ~m;
        return m != 0 && (inverted & (inverted + 1)) == 0;
    }

    private static bool TryParseName(string label, string? text, int minLen, int maxLen, out string name, out string error)
    {
        error = string.Empty;
        name = (text ?? string.Empty).Trim();

        if (name.Length < minLen)
        {
            error = minLen <= 1
            ? $"{label}を入力してください"
            : $"{label}は{minLen}文字以上で入力してください";
            return false;
        }
        if (name.Length > maxLen)
        {
            error = $"{label}は{maxLen}文字以内で入力してください";
            return false;
        }

        return true;
    }
}
