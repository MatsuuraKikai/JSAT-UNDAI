public class UndaiSettingEntry
{
    /// <summary>
    /// Defines the DefaultUndaiIpAddress
    /// </summary>
    public const string DefaultUndaiIpAddress = "192.168.1.10";

    /// <summary>
    /// Defines the DefaultUndaiSubnetMask
    /// </summary>
    public const string DefaultUndaiSubnetMask = "255.255.255.0";

    /// <summary>
    /// Defines the DefaultUndaiGateway
    /// </summary>
    public const string DefaultUndaiGateway = "192.168.1.1";

    /// <summary>
    /// Defines the DefaultModemIpAddress
    /// </summary>
    public const string DefaultModemIpAddress = "192.168.1.1";

    /// <summary>
    /// Defines the DefaultUndaiPort
    /// </summary>
    public const int DefaultUndaiPort = 5625;

    /// <summary>
    /// Gets or sets the CurrentUndaiIpAddress
    /// </summary>
    public string CurrentUndaiIpAddress { get; set; } = DefaultUndaiIpAddress;

    /// <summary>
    /// Gets or sets the CurrentUndaiSubnetMask
    /// </summary>
    public string CurrentUndaiSubnetMask { get; set; } = DefaultUndaiSubnetMask;

    /// <summary>
    /// Gets or sets the CurrentUndaiGateway
    /// </summary>
    public string CurrentUndaiGateway { get; set; } = DefaultUndaiGateway;

    /// <summary>
    /// Gets or sets the ModemIpAddress
    /// </summary>
    public string ModemIpAddress { get; set; } = DefaultModemIpAddress;

    /// <summary>
    /// Gets or sets the UndaiVersion
    /// </summary>
    public string UndaiVersion { get; set; } = "0.0";

    /// <summary>
    /// Gets or sets the ContinuousRunMinutes
    /// </summary>
    public string ContinuousRunMinutes { get; set; } = "0.0";// 連続運転時間(分)

    /// <summary>
    /// Gets or sets the OriginOffsetAzimuth
    /// </summary>
    public string OriginOffsetAzimuth { get; set; } = "0.0";

    /// <summary>
    /// Gets or sets the OriginOffsetElevation
    /// </summary>
    public string OriginOffsetElevation { get; set; } = "0.0";

    /// <summary>
    /// Gets or sets the ManualHighSpeedAzimuth
    /// </summary>
    public string ManualHighSpeedAzimuth { get; set; } = "100.0";// 手動高速[1-100]

    /// <summary>
    /// Gets or sets the ManualHighSpeedElevation
    /// </summary>
    public string ManualHighSpeedElevation { get; set; } = "100.0";

    /// <summary>
    /// Gets or sets the ManualLowSpeedAzimuth
    /// </summary>
    public string ManualLowSpeedAzimuth { get; set; } = "40.0";// 手動低速[1-100]

    /// <summary>
    /// Gets or sets the ManualLowSpeedElevation
    /// </summary>
    public string ManualLowSpeedElevation { get; set; } = "40.0";

    /// <summary>
    /// Gets or sets the JogAngleAzimuth
    /// </summary>
    public string JogAngleAzimuth { get; set; } = "0.1";// 寸動角度(度)

    /// <summary>
    /// Gets or sets the JogAngleElevation
    /// </summary>
    public string JogAngleElevation { get; set; } = "0.1";

    // ---- 広域サーチ設定 ----

    /// <summary>
    /// Gets or sets the WideSearchSpeed
    /// </summary>
    public string WideSearchSpeed { get; set; } = "0.0";// 広域サーチ速度[1-100]

    /// <summary>
    /// Gets or sets the WideSearchAzimuthRange
    /// </summary>
    public string WideSearchAzimuthRange { get; set; } = "0.0";// 広域サーチ水平角度(度)(±)

    /// <summary>
    /// Gets or sets the WideSearchElevationRange
    /// </summary>
    public string WideSearchElevationRange { get; set; } = "0.0";// 広域サーチ垂直角度(度)(±)

    /// <summary>
    /// Gets or sets the WideSearchSuccessRssi
    /// </summary>
    public string WideSearchSuccessRssi { get; set; } = "0.0";// 広域成功RSSIレベル(dBm)

    // ---- 詳細サーチ設定 ----

    /// <summary>
    /// Gets or sets the DetailSearchSpeed
    /// </summary>
    public string DetailSearchSpeed { get; set; } = "0.0";// 詳細サーチ速度[1-100]

    /// <summary>
    /// Gets or sets the DetailSearchStepAngle
    /// </summary>
    public string DetailSearchStepAngle { get; set; } = "0.0";// 詳細サーチステップ角(度)

    /// <summary>
    /// Gets or sets the DetailSearchAzimuthRange
    /// </summary>
    public string DetailSearchAzimuthRange { get; set; } = "0.0";// 詳細サーチ水平角度(度)(±)

    /// <summary>
    /// Gets or sets the DetailSearchElevationRange
    /// </summary>
    public string DetailSearchElevationRange { get; set; } = "0.0";// 詳細サーチ垂直角度(度)(±)

    /// <summary>
    /// Gets or sets the DetailSearchSuccessRssi
    /// </summary>
    public string DetailSearchSuccessRssi { get; set; } = "0.0";// 詳細サーチ成功RSSIレベル(dBm)

    /// <summary>
    /// Gets or sets the DetailSearchStepSettleSeconds
    /// </summary>
    public string DetailSearchStepSettleSeconds { get; set; } = "0.0";// ステップ安定秒

    /// <summary>
    /// Gets or sets the DetailSearchTurnbackThresholdLevel
    /// </summary>
    public string DetailSearchTurnbackThresholdLevel { get; set; } = "0.0";// 打切り減衰(dBm)

    // ---- Satellite情報 ----

    /// <summary>
    /// Gets or sets the SatelliteName
    /// </summary>
    public string SatelliteName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SatelliteLongitude
    /// </summary>
    public string SatelliteLongitude { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="UndaiSettingEntry"/> class.
    /// </summary>
    public UndaiSettingEntry()
    {
    }
}
