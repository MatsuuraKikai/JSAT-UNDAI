namespace JSAT_UNDAI.Views.Base
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// PIN change dialog: current PIN + new PIN (exactly 4 digits) + confirmation.
    /// The dialog only validates input. The caller verifies the current PIN
    /// (via the callback) and stores the new PIN (dialog.NewPin) after ShowDialog() == true
    /// </summary>
    public partial class PinChangeDialog : Window
    {
        /// <summary>
        /// Defines the PinLength
        /// </summary>
        private const int PinLength = 4;

        // Returns true when the entered current PIN is correct. Null = skip the check.

        /// <summary>
        /// Defines the _verifyCurrent
        /// </summary>
        private readonly Func<string, bool>? _verifyCurrent;

        /// <summary>The new PIN. Valid only when ShowDialog() returned true.</summary>

        /// <summary>
        /// Gets the NewPin
        /// </summary>
        public string NewPin { get; private set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinChangeDialog"/> class.
        /// </summary>
        /// <param name="verifyCurrent">The verifyCurrent<see cref="Func{string, bool}?"/></param>
        public PinChangeDialog(Func<string, bool>? verifyCurrent = null)
        {
            InitializeComponent();
            _verifyCurrent = verifyCurrent;
            Loaded += (_, _) => CurrentPinBox.Focus();
        }

        // Blocks any non-digit keystroke (letters, symbols, space).

        /// <summary>
        /// The OnDigitOnlyPreview
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="TextCompositionEventArgs"/></param>
        private void OnDigitOnlyPreview(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsAsciiDigit);
        }

        /// <summary>
        /// The IsValidPin
        /// </summary>
        /// <param name="pin">The pin<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private static bool IsValidPin(string pin)
            => pin.Length == PinLength && pin.All(char.IsAsciiDigit);

        /// <summary>
        /// The OnOkClick
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var current = CurrentPinBox.Password;
            var newPin = NewPinBox.Password;
            var confirm = ConfirmPinBox.Password;

            // 1) Current PIN must be correct.
            if (_verifyCurrent != null && !_verifyCurrent(current))
            {
                ShowError("現在の暗証番号が正しくありません");
                CurrentPinBox.Clear();
                CurrentPinBox.Focus();
                return;
            }

            // 2) New PIN must be exactly 4 digits.
            if (!IsValidPin(newPin))
            {
                ShowError("新しい暗証番号は数字4桁で入力してください");
                NewPinBox.Clear();
                ConfirmPinBox.Clear();
                NewPinBox.Focus();
                return;
            }

            // 3) Must differ from the current PIN.
            if (newPin == current)
            {
                ShowError("現在と同じ暗証番号は設定できません");
                NewPinBox.Clear();
                ConfirmPinBox.Clear();
                NewPinBox.Focus();
                return;
            }

            // 4) Confirmation must match.
            if (newPin != confirm)
            {
                ShowError("確認用の暗証番号が一致しません");
                ConfirmPinBox.Clear();
                ConfirmPinBox.Focus();
                return;
            }

            NewPin = newPin;
            DialogResult = true;
        }

        /// <summary>
        /// The ShowError
        /// </summary>
        /// <param name="message">The message<see cref="string"/></param>
        private void ShowError(string message)
        {
            MessageText.Text = message;
            MessageText.Foreground = Brushes.Red;
        }
    }
}
