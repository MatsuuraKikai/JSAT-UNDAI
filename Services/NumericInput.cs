using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JSAT_UNDAI.Services;

public static class NumericInput
{
    // digits with at most one dot: "12", "12.", ".5", "12.34" OK; "12.32.5", "1a" NG
    private static readonly Regex Pattern = new(@"^\d*\.?\d*$", RegexOptions.Compiled);

    public static readonly DependencyProperty IsDecimalOnlyProperty =
        DependencyProperty.RegisterAttached(
            "IsDecimalOnly", typeof(bool), typeof(NumericInput),
            new PropertyMetadata(false, OnIsDecimalOnlyChanged));

    public static bool GetIsDecimalOnly(DependencyObject d) => (bool)d.GetValue(IsDecimalOnlyProperty);
    public static void SetIsDecimalOnly(DependencyObject d, bool v) => d.SetValue(IsDecimalOnlyProperty, v);

    private static void OnIsDecimalOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;

        if ((bool)e.NewValue)
        {
            tb.PreviewTextInput += OnPreviewTextInput;
            tb.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(tb, OnPaste);
            InputMethod.SetIsInputMethodEnabled(tb, false);   // blocks full-width / IME input (Japanese IME)
        }
        else
        {
            tb.PreviewTextInput -= OnPreviewTextInput;
            tb.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(tb, OnPaste);
            InputMethod.SetIsInputMethodEnabled(tb, true);
        }
    }

    private static string Propose(TextBox tb, string incoming) =>
        tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, incoming);

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var tb = (TextBox)sender;
        e.Handled = !Pattern.IsMatch(Propose(tb, e.Text));
    }

    // PreviewTextInput does not fire for Space, so block it here
    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) e.Handled = true;
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        var tb = (TextBox)sender;
        if (e.DataObject.GetData(DataFormats.UnicodeText) is not string text ||
            !Pattern.IsMatch(Propose(tb, text)))
        {
            e.CancelCommand();
        }
    }
}
