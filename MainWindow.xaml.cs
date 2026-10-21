using JSAT_UNDAI.Views.Base;
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

        var confirmDialog = new ConfirmDialog("終了確認", "アプリを終了しますか？")
        {
            Owner = this
        };

        if (confirmDialog.ShowDialog() != true)
            return;

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
}
