using System.Windows;
using JSAT_UNDAI.ViewModels;

namespace JSAT_UNDAI.Views;

/// <summary>
/// Modal dialog for the installation settings. Open it with ShowDialog() after setting DataContext
/// to an <see cref="InstallationEditViewModel"/>. DialogResult is true only after a successful save.
/// </summary>
public partial class InstallationEditDialog : Window
{
    public InstallationEditDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is InstallationEditViewModel oldViewModel)
        {
            oldViewModel.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is InstallationEditViewModel newViewModel)
        {
            newViewModel.CloseRequested += OnCloseRequested;
        }
    }

    // Setting DialogResult closes a window that was opened with ShowDialog().
    private void OnCloseRequested(bool saved) => DialogResult = saved;
}
