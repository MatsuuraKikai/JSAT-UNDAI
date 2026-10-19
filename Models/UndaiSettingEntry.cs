public class UndaiSettingEntry
{
    public string UndaiIpAddress { get; set; } = "192.168.1.10";
    public string UndaiSubnetMask { get; set; } = "255.255.255.0";
    public string UndaiGateway { get; set; } = "192.168.1.1";

    public UndaiSettingEntry(string undaiIpAddress, string undaiSubnetMask, string undaiGateway)
    {
        UndaiIpAddress = undaiIpAddress;
        UndaiSubnetMask = undaiSubnetMask;
        UndaiGateway = undaiGateway;
    }
}