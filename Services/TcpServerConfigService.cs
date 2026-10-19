using JSAT_UNDAI.Models;
using System.IO;
using System.Text.Json;

namespace JSAT_UNDAI.Services;

public class TcpServerConfigService
{
    private readonly string _configFilePath;
    private readonly string _defaultServerIP;
    private readonly int _defaultServerPort;

    public TcpServerConfigService(string appDataDirectory, string defaultServerIP = "192.168.1.100", int defaultServerPort = 5625)
    {
        _configFilePath = Path.Combine(appDataDirectory, "ServerConfig.json");
        _defaultServerIP = defaultServerIP;
        _defaultServerPort = defaultServerPort;
    }

    public async Task<TcpServerConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                var defaultConfig = new TcpServerConfig { ServerIPAddress = _defaultServerIP, ServerPort = _defaultServerPort };
                await SaveAsync(defaultConfig);
                return defaultConfig;
            }

            await using var stream = File.OpenRead(_configFilePath);
            var config = await JsonSerializer.DeserializeAsync<TcpServerConfig>(stream);

            if (config == null || string.IsNullOrWhiteSpace(config.ServerIPAddress))
            {
                var fallback = new TcpServerConfig { ServerIPAddress = _defaultServerIP, ServerPort = _defaultServerPort };
                await SaveAsync(fallback);
                return fallback;
            }

            return config;
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError($"Error loading TCP server config. Path={_configFilePath}", ex);

            return new TcpServerConfig { ServerIPAddress = _defaultServerIP, ServerPort = _defaultServerPort };
        }
    }

    public async Task SaveAsync(TcpServerConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);

            var options = new JsonSerializerOptions { WriteIndented = true };
            await using var stream = File.Create(_configFilePath);
            await JsonSerializer.SerializeAsync(stream, config, options);
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError($"Error saving TCP server config. Path={_configFilePath}, IP={config?.ServerIPAddress}, Port={config?.ServerPort}", ex);
        }
    }
}
