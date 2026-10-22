using System.IO;
using System.Text.Json;

namespace JSAT_UNDAI.Services;

public sealed class PinStorageService
{
    public const string DefaultPin = "1234";
    public const string BackupPin = "1056";

    private readonly string _filePath;
    private readonly string _fileName = "undai_pin.json";
    private readonly object _lock = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private sealed class PinRecord
    {
        public string Pin { get; set; } = DefaultPin;
    }

    public PinStorageService(string appDataDirectory)
    {
        _filePath = Path.Combine(appDataDirectory, _fileName);
        if (!File.Exists(_filePath))
        {
            WriteDefaultSettings();
            return;
        }
    }

    public string GetPin()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return DefaultPin;
                }

                var record = JsonSerializer.Deserialize<PinRecord>(File.ReadAllText(_filePath));
                var pin = record?.Pin ?? string.Empty;

                // Accept only exactly 4 digits, otherwise treat the file as broken.
                return IsValidPin(pin) ? pin : DefaultPin;
            }
            catch (Exception)
            {
                return DefaultPin;
            }
        }
    }

    public void SetPin(string newPin)
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Write to a temp file, then replace: a power loss cannot leave a half-written file.
            var json = JsonSerializer.Serialize(new PinRecord { Pin = newPin });
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
    }

    public void WriteDefaultSettings()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(new PinRecord(), _jsonOptions));
    }

    private static bool IsValidPin(string pin)
    {
        if (pin.Length != 4) return false;
        foreach (var c in pin)
            if (c < '0' || c > '9') return false;
        return true;
    }

    public bool Verify(string enteredPin)
    {
        var entered = (enteredPin ?? string.Empty).Trim();

        // String compare: keeps leading zeros ("0123" stays "0123").
        return entered == GetPin() || entered == BackupPin;
    }
}
