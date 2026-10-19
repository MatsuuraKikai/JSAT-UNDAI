using System.ComponentModel;
using System.Windows;

namespace JSAT_UNDAI;

public partial class MainWindow : Window
{
    private static App? CurrentApp => Application.Current as App;
    public bool CloseConfirmed { get; set; } = false;

    public MainWindow()
    {
        InitializeComponent();
        Title = App.MainWindowTitle;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (CloseConfirmed)
        {
            base.OnClosing(e);
            return;
        }

        // Cancel by default; only a confirmed exit goes through.
        e.Cancel = true;

        var result = MessageBox.Show(
            this,
            "アプリを終了しますか？",
            "終了確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CloseConfirmed = true;

            if (CurrentApp is { } app)
            {
                // Disconnects TCP, then calls Application.Shutdown().
                app._mainPageViewModel.DisconnectAndShutdown();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        base.OnClosing(e);
    }
}
