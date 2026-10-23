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
                        string[] newMessage = messageList[3..(3 + dataCount)];
                        ProcessRealTimeMessages(newMessage, dataCount);
                    }
                    break;
                case "SY":
                    ProcessSystemMessages(_message);
                    break;
                case "SI":
                    ProcessSystemMessages(_message);
                    break;
                case "SA":
                    ProcessSystemMessages(_message);
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
        for (int i = 0; i < dataCount; i++)
        {
            switch (i)
            {
                case 0:
                    _mainPageViewModel.CurrentElevation = message[i];
                    break;
                case 1:
                    _mainPageViewModel.CurrentAzimuth = message[i];
                    break;
                case 2:
                    _mainPageViewModel.ChangePositionUnit(message[i], 0);
                    break;
                case 3:
                    _mainPageViewModel.ChangePositionUnit(message[i], 1);
                    break;
                case 4:
                    _mainPageViewModel.SpeedModeIndex = int.Parse(message[i]);
                    break;
                case 5:
                    if(int.TryParse(message[i], out int linkUpDown))
                    {
                        _mainPageViewModel.ModemStatus = (linkUpDown == 1);
                    }
                    else
                    {
                        _ = ErrorLoggingService.LogError($"Failed to parse ModemStatus: {message[i]}");
                    }
                    break;
                case 6:
                    _mainPageViewModel.CurrentRssi = message[i];
                    break;
                case 7:
                    _mainPageViewModel.PeakRssi = message[i];
                    break;
                case 8:
                    _mainPageViewModel.PeakElevation = message[i];
                    break;
                case 9:
                    _mainPageViewModel.PeakAzimuth = message[i];
                    break;
                case 10:
                    break;
                case 11:
                    break;
                case 12:
                    break;
                case 13:
                    if(int.TryParse(message[i], out int gpsStatus))
                    {
                        _mainPageViewModel.GpsStatus = (gpsStatus == 1);
                    }
                    else
                    {
                        _ = ErrorLoggingService.LogError($"Failed to parse GpsStatus: {message[i]}");
                    }
                    break;
                case 14:
                    _mainPageViewModel.ActiveHomePeakSearchIndex = int.Parse(message[i]);
                    break;
                case 15:
                    break;
                case 16:
                    _mainPageViewModel.AlarmStatusCode = message[i];
                    break;
                case 17:
                    if (int.TryParse(message[i], out int randomNumber))
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

    void ProcessSystemMessages(string message)
    {

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
