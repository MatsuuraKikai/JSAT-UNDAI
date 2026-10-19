using JSAT_UNDAI.Models;
using System.IO;
using System.Text.Json;

namespace JSAT_UNDAI.Services;

public class UndaiSettingStorageService
{
    private readonly string _filePath;
    private readonly string _fileName = "undai_settings.json";

    public const string DefaultUndaiIpAddress = "192.168.1.10";
    public const string DefaultUndaiSubnetMask = "255.255.255.0";
    public const string DefaultUndaiGateway = "192.168.1.1";

    public const string DefaultCameraIpAddress = "192.168.1.20";
    public const string DefaultCameraPort = "1234";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public UndaiSettingStorageService()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UNDAI");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, _fileName);

        if (!File.Exists(_filePath))
        {
            WriteDefaultSettings();
            return;
        }

        try
        {
            var settings = LoadSetting();
            if (!IsValid(settings))
            {
                WriteDefaultSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UNDAI settings load failed, resetting: {ex.Message}");
            WriteDefaultSettings();
        }
    }

    public void SaveSetting(UndaiSettingEntry setting)
    {
        if (!IsValid(setting))
        {
            throw new ArgumentException("Invalid UNDAI settings provided.");
        }

        try
        {
            string json = JsonSerializer.Serialize(setting, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to save UNDAI settings — file may be locked.", ex);
        }
    }

    public UndaiSettingEntry LoadSetting()
    {
        if (!File.Exists(_filePath))
        {
            return CreateDefaultSettings();
        }
        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<UndaiSettingEntry>(json, _jsonOptions) ?? CreateDefaultSettings();
    }

    private void WriteDefaultSettings()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(CreateDefaultSettings(), _jsonOptions));
    }

    private static UndaiSettingEntry CreateDefaultSettings()
    {
        return new UndaiSettingEntry(
            DefaultUndaiIpAddress,
            DefaultUndaiSubnetMask,
            DefaultUndaiGateway,
            DefaultCameraIpAddress,
            DefaultCameraPort);
    }

    private static bool IsValid(UndaiSettingEntry setting)
    {
        return !string.IsNullOrEmpty(setting.UndaiIpAddress) &&
               !string.IsNullOrEmpty(setting.UndaiSubnetMask) &&
               !string.IsNullOrEmpty(setting.UndaiGateway) &&
               !string.IsNullOrEmpty(setting.CameraIpAddress) &&
               !string.IsNullOrEmpty(setting.CameraPort);
    }
}
