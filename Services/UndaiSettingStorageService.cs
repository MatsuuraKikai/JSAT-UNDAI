using JSAT_UNDAI.Models;
using System.IO;
using System.Text.Json;

namespace JSAT_UNDAI.Services;

public class UndaiSettingStorageService
{
    private readonly string _filePath;
    private readonly string _fileName = "undai_settings.json";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public UndaiSettingStorageService(string appDataDirectory)
    {
        _filePath = Path.Combine(appDataDirectory, _fileName);

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

    public void WriteDefaultSettings()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(CreateDefaultSettings(), _jsonOptions));
    }

    private UndaiSettingEntry CreateDefaultSettings()
    {
        return new UndaiSettingEntry(
            UndaiSettingEntry.DefaultUndaiIpAddress,
            UndaiSettingEntry.DefaultUndaiSubnetMask,
            UndaiSettingEntry.DefaultUndaiGateway);
    }

    private bool IsValid(UndaiSettingEntry setting)
    {
        return !string.IsNullOrEmpty(setting.CurrentUndaiIpAddress) &&
               !string.IsNullOrEmpty(setting.CurrentUndaiSubnetMask) &&
               !string.IsNullOrEmpty(setting.CurrentUndaiGateway);
    }
}
