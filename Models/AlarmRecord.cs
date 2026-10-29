using CommunityToolkit.Mvvm.ComponentModel;

namespace JSAT_UNDAI.Models;

public sealed partial class AlarmRecord : ObservableObject
{
    public long Id { get; set; } // permanent DB key
    public string AlarmName { get; set; } = "";
    public DateTime OccurredAt { get; set; }

    [ObservableProperty]
    private int _displayNo;

    public string DateText => OccurredAt.ToString("yyyy-MM-dd");
    public string TimeText => OccurredAt.ToString("HH:mm:ss");

}
