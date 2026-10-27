using JSAT_UNDAI.Services;
using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels;
using System.Runtime.Serialization;

namespace JSAT_UNDAI.Models;

public class MainPageModel
{
    ViewModelStore _viewModelStore;

    MainPageViewModel _mainPageViewModel;
    SettingPageViewModel _settingPageViewModel;
    DeveloperPageViewModel _developerPageViewModel;
    TcpServerConnectionService _tcpServerConnectionService;

    //Recevied Message Process
    string _message;
    string[] messageList;
    public int messageReceivedCount = 0;

    int randomNumber = 0;

    public MainPageModel(ViewModelStore viewModelStore, TcpServerConnectionService tcpServerConnectionService)
    {
        _viewModelStore = viewModelStore;
        _tcpServerConnectionService = tcpServerConnectionService;
        InitializeViewModels();
    }

    public async Task TcpReceivedMessageProcess(string rawMessage)
    {
        if(IsViewModelsInitialized())
        {
            _message = rawMessage.Trim().TrimEnd(',');
            messageReceivedCount++;
            messageList = _message.Split(new[] { "," }, StringSplitOptions.None);

            switch (messageList[0])
            {
                case "A":
                    if(int.TryParse(messageList[2], out int dataCount))
                    {
                        string[] newRealtimeMessage = messageList[3..(3 + dataCount)];
                        ProcessRealTimeMessages(newRealtimeMessage, dataCount);
                    }
                    break;
                case "SY":
                    string[] newSystemMessage = messageList[1..];
                    ProcessSystemMessages(1, newSystemMessage);
                    break;
                case "SI":
                    //ProcessSystemMessages(_message);
                    break;
                case "SA":
                    //ProcessSystemMessages(_message);
                    break;
            }
        }
        else
        {
            _ = ErrorLoggingService.LogError("ViewModels are not initialized in MainPageModel.");
            InitializeViewModels();
        }

    }

    void ProcessRealTimeMessages(string[] message, int dataCount)
    {
        for (int idx = 0; idx < dataCount; idx++)
        {
            switch (idx)
            {
                case 0:
                    _mainPageViewModel.CurrentElevation = message[idx];
                    break;
                case 1:
                    _mainPageViewModel.CurrentAzimuth = message[idx];
                    break;
                case 2:
                    _mainPageViewModel.ChangePositionUnit(message[idx], 0);
                    break;
                case 3:
                    _mainPageViewModel.ChangePositionUnit(message[idx], 1);
                    break;
                case 4:
                    _mainPageViewModel.SpeedModeIndex = int.Parse(message[idx]);
                    break;
                case 5:
                    if(int.TryParse(message[idx], out int linkUpDown))
                    {
                        _mainPageViewModel.ModemStatus = (linkUpDown == 1);
                    }
                    else
                    {
                        _ = ErrorLoggingService.LogError($"Failed to parse ModemStatus: {message[idx]}");
                    }
                    break;
                case 6:
                    _mainPageViewModel.CurrentRssi = message[idx];
                    break;
                case 7:
                    _mainPageViewModel.PeakRssi = message[idx];
                    break;
                case 8:
                    _mainPageViewModel.PeakElevation = message[idx];
                    break;
                case 9:
                    _mainPageViewModel.PeakAzimuth = message[idx];
                    break;
                case 10:
                    break;
                case 11:
                    break;
                case 12:
                    break;
                case 13:
                    if(int.TryParse(message[idx], out int gpsStatus))
                    {
                        _mainPageViewModel.GpsStatus = (gpsStatus == 1);
                    }
                    else
                    {
                        _ = ErrorLoggingService.LogError($"Failed to parse GpsStatus: {message[idx]}");
                    }
                    break;
                case 14:
                    _mainPageViewModel.ActiveHomePeakSearchIndex = int.Parse(message[idx]);
                    break;
                case 15:
                    break;
                case 16:
                    _mainPageViewModel.AlarmStatusCode = message[idx];
                    break;
                case 17:
                    if (int.TryParse(message[idx], out int randomNumber))
                    {
                        if(_tcpServerConnectionService.IsConnectionStale(randomNumber))
                        {
                            _tcpServerConnectionService.Disconnect();
                        }
                    }
                    break;
            }
        }
    }

    void ProcessSystemMessages(int systemMessageType, string[] message)
    {
        if(systemMessageType == 1)
        {
            for (int idx = 0; idx < message.Length; idx++)
            {
                switch (idx)
                {
                    case 0:
                        _settingPageViewModel.UndaiIpAddress = message[idx];
                        break;
                    case 1:
                        _settingPageViewModel.UndaiSubnetMask = message[idx];
                        break;
                    case 2:
                        _settingPageViewModel.UndaiGateway = message[idx];
                        break;
                    case 3:
                        _settingPageViewModel.ModemIpAddress = message[idx];
                        break;
                    case 4:
                        _settingPageViewModel.OriginOffsetAzimuth = message[idx];
                        break;
                    case 5:
                        _settingPageViewModel.OriginOffsetElevation = message[idx];
                        break;
                    case 6:
                        _settingPageViewModel.ManualHighSpeedAzimuth = message[idx];
                        break;
                    case 7:
                        _settingPageViewModel.ManualHighSpeedElevation = message[idx];
                        break;
                    case 8:
                        _settingPageViewModel.ManualLowSpeedAzimuth = message[idx];
                        break;
                    case 9:
                        _settingPageViewModel.ManualLowSpeedElevation = message[idx];
                        break;
                    case 10:
                        _settingPageViewModel.JogAngleAzimuth = message[idx];
                        break;
                    case 11:
                        _settingPageViewModel.JogAngleElevation = message[idx];
                        break;
                    case 12:
                        _settingPageViewModel.SatelliteName = message[idx];
                        break;
                    case 13:
                        _settingPageViewModel.SatelliteLongitude= message[idx];
                        break;
                    case 14:
                        _settingPageViewModel.WideSearchSpeed = message[idx];
                        break;
                    case 15:
                        _settingPageViewModel.WideSearchAzimuthRange = message[idx];
                        break;
                    case 16:
                        _settingPageViewModel.WideSearchElevationRange = message[idx];
                        break;
                    case 17:
                        _settingPageViewModel.WideSearchSuccessRssi = message[idx];
                        break;
                    case 18:
                        _settingPageViewModel.DetailSearchSpeed = message[idx];
                        break;
                    case 19:
                        _settingPageViewModel.DetailSearchStepAngle = message[idx];
                        break;
                    case 20:
                        _settingPageViewModel.DetailSearchAzimuthRange = message[idx];
                        break;
                    case 21:
                        _settingPageViewModel.DetailSearchElevationRange = message[idx];
                        break;
                    case 22:
                        _settingPageViewModel.DetailSearchSuccessRssi = message[idx];
                        break;
                    case 23:
                        _settingPageViewModel.DetailSearchStepSettleSeconds = message[idx];
                        break;
                    case 24:
                        _settingPageViewModel.DetailSearchTurnbackThresholdLevel = message[idx];
                        break;
                    case 25:
                        _developerPageViewModel.WideSearchElevationStep = message[idx];
                        break;
                    case 26:
                        _developerPageViewModel.WideSearchAzimuthStep = message[idx];
                        break;
                    case 27:
                        _settingPageViewModel.UndaiVersionText = message[idx];
                        break;
                }
            }
        }
    }

    public void SendTcpMessage(string message = "", bool isCommand = false, int commandNo = 0)
    {
        if (isCommand)
        {
            string sendMessage = string.Empty;
            switch (commandNo)
            {
                case 1:
                    sendMessage = "I,1";
                    break;
                case 2:
                    sendMessage = "I,2";
                    break;
                case 3:
                    sendMessage = "I,3";
                    break;
                case 4:
                    sendMessage = "I,4";
                    break;
                case 5:
                    sendMessage = "I,5";
                    break;
                case 6:
                    sendMessage = "I,6";
                    break;
                case 7:
                    sendMessage = "I,7";
                    break;
                case 8:
                    sendMessage = "I,8";
                    break;
                case 9:
                    sendMessage = message;
                    break;
                case 10:
                    sendMessage = message;
                    break;
                case 11:
                    sendMessage = message;
                    break;
                case 18:
                    sendMessage = "I,18";
                    break;
                case 19:
                    sendMessage = "I,19";
                    break;
                case 20:
                    sendMessage = "I,20";
                    break;
                case 21:
                    sendMessage = "I,21";
                    break;
                case 22:
                    sendMessage = "I,22";
                    break;
                case 30:
                    sendMessage = "I,30";
                    break;
                case 31:
                    sendMessage = "I,31";
                    break;
            }
            ;
            _tcpServerConnectionService.SendMessage(sendMessage);
        }
    }

    void InitializeViewModels()
    {
        _mainPageViewModel = _viewModelStore.MainPageViewModel;
        _settingPageViewModel = _viewModelStore.SettingPageViewModel;
        _developerPageViewModel = _viewModelStore.DeveloperPageViewModel;

        _ = ErrorLoggingService.LogInfo("ViewModels initialized in MainPageModel.");
    }

    bool IsViewModelsInitialized()
    {
        return _mainPageViewModel != null && _settingPageViewModel != null && _developerPageViewModel != null;
    }
}
