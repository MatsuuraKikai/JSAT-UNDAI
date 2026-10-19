using JSAT_UNDAI.Stores;
using JSAT_UNDAI.ViewModels.Base;

namespace JSAT_UNDAI.Services;

public class NavigationService
{
    readonly NavigationStore _navigationStore;
    readonly Func<ViewModelBase> _createViewModel;

    public NavigationService(NavigationStore navigationStore, Func<ViewModelBase> createViewModel)
    {
        _navigationStore = navigationStore;
        _createViewModel = createViewModel;
    }

    public void Navigate()
    {
        if(_navigationStore.CurrentViewModel is INavigationAware outgoing)
        {
            outgoing.OnNavigatedFrom();
        }

        var viewModel = _createViewModel();
        _navigationStore.CurrentViewModel = viewModel;

        if (viewModel is INavigationAware incoming)
        {
            incoming.OnNavigatedTo();
        }
    }
}
