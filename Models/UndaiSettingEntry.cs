namespace JSAT_UNDAI.Models;

public class UndaiSettingEntry
{
    //Undai IP Address, Subnet Mask, and Gateway
    public string UndaiIpAddress { get; set; }
    public string UndaiSubnetMask { get; set; }
    public string UndaiGateway { get; set; }

    //Camera IP Address and Port
    public string CameraIpAddress { get; set; }
    public string CameraPort { get; set; }

    public UndaiSettingEntry(string UndaiIpAddress, string UndaiSubnetMask, string UndaiGateway, string CameraIpAddress, string CameraPort)
    {
        this.UndaiIpAddress = UndaiIpAddress;
        this.UndaiSubnetMask = UndaiSubnetMask;
        this.UndaiGateway = UndaiGateway;
        this.CameraIpAddress = CameraIpAddress;
        this.CameraPort = CameraPort;
    }
}
