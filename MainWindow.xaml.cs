using JSAT_UNDAI.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace JSAT_UNDAI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public bool CloseConfirmed { get; set; } = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (CloseConfirmed)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;

        var mainPageViewModel = ((App)Application.Current)._mainPageViewModel;

        var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            CloseConfirmed = true;
            mainPageViewModel.DisconnectAndShutdown();

        }
        base.OnClosing(e);
    }
}
