using JSAT_UNDAI.ViewModels.Base;
using System.Windows;
using System.Windows.Input;

namespace JSAT_UNDAI.Views.Base;

public partial class PinEntryDialog : Window
{
    public PinEntryDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => PinBox.Focus();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PinEntryViewModel oldViewModel)
        {
            oldViewModel.CloseRequested -= OnCloseRequested;
            oldViewModel.ClearRequested -= OnClearRequested;
        }

        if (e.NewValue is PinEntryViewModel newViewModel)
        {
            newViewModel.CloseRequested += OnCloseRequested;
            newViewModel.ClearRequested += OnClearRequested;
        }
    }

    private void OnPinPasswordChanged(object sender, RoutedEventArgs e)
    {
        (DataContext as PinEntryViewModel)?.SetPin(PinBox.Password);
    }

    private void OnPinPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsAsciiDigit);
    }

    private void OnClearRequested()
    {
        PinBox.Clear();
        PinBox.Focus();
    }

    private void OnCloseRequested(bool accepted)
    {
        DialogResult = accepted;
    }
}
