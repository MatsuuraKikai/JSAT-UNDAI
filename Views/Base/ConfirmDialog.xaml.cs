using System.Windows;

namespace JSAT_UNDAI.Views.Base;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message,
                         string yesText = "はい", string noText = "いいえ")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        YesButton.Content = yesText;
        NoButton.Content = noText;
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}