using JSAT_UNDAI.ViewModels;

namespace JSAT_UNDAI.Stores;

public class ViewModelStore
{
    public MainPageViewModel MainPageViewModel { get; set; } = null!;

    public SettingPageViewModel SettingPageViewModel { get; set; } = null!;

    public SystemResetSettingPageViewModel SystemResetSettingPageViewModel { get; set; } = null!;

    public DeveloperPageViewModel DeveloperPageViewModel { get; set; } = null!;
}
