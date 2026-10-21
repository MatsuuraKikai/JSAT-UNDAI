using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JSAT_UNDAI.Services;

namespace JSAT_UNDAI.ViewModels;

/// <summary>
/// ViewModel of the installation edit dialog (UNDAI GPS source only).
/// Auto  = the GPS receiver supplies the position.
/// Manual = the operator types latitude and longitude.
/// Nothing is written to disk: manual values live only for the current session.
/// </summary>
public partial class InstallationEditViewModel : ObservableObject
{
    public InstallationEditViewModel(bool isManualGps, double manualLatitude, double manualLongitude)
    {
        _isManualGps = isManualGps;
        _latitudeText = manualLatitude.ToString("0.00000", CultureInfo.InvariantCulture);
        _longitudeText = manualLongitude.ToString("0.00000", CultureInfo.InvariantCulture);
        ResultLatitude = manualLatitude;
        ResultLongitude = manualLongitude;
    }

    /// <summary>Raised with true after a successful save. The dialog window closes itself.</summary>
    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GpsModeLabel))]
    private bool _isManualGps;

    /// <summary>Text shown next to the toggle switch.</summary>
    public string GpsModeLabel => IsManualGps ? "手動" : "自動";

    [ObservableProperty]
    private string _latitudeText = string.Empty;

    [ObservableProperty]
    private string _longitudeText = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Parsed manual latitude. Valid after a successful save while IsManualGps is true.</summary>
    public double ResultLatitude { get; private set; }

    /// <summary>Parsed manual longitude. Valid after a successful save while IsManualGps is true.</summary>
    public double ResultLongitude { get; private set; }

    // Clear a stale error message when the operator flips the toggle.
    partial void OnIsManualGpsChanged(bool value) => ErrorMessage = string.Empty;

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = string.Empty;

        // Manual values only matter, and are only validated, in manual mode.
        if (IsManualGps)
        {
            if (!InstallationValidation.TryParseLatitude(LatitudeText, out double latitude))
            {
                ErrorMessage = "緯度は -90 ～ 90 の数値で入力してください";
                return;
            }

            if (!InstallationValidation.TryParseLongitude(LongitudeText, out double longitude))
            {
                ErrorMessage = "経度は -180 ～ 180 の数値で入力してください";
                return;
            }

            ResultLatitude = latitude;
            ResultLongitude = longitude;
        }

        CloseRequested?.Invoke(true);
    }
}
