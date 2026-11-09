using System.Windows;
using System.Windows.Input;

namespace GameLauncher
{
    /// <summary>
    /// Rounded, themed replacement for MessageBox.
    /// Pass cancelText = null for a single-button (OK only) dialog.
    /// </summary>
    public partial class MessageDialog : Window
    {
        public MessageDialog(string title, string message, string confirmText, string? cancelText)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;

            if (cancelText == null)
                CancelButton.Visibility = Visibility.Collapsed;
            else
                CancelButton.Content = cancelText;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && cancelText != null) DialogResult = false;
            };
        }

        /// <summary>Shows the dialog; returns true if the confirm button was pressed.</summary>
        public static bool Show(Window owner, string title, string message,
            string confirmText = "OK", string? cancelText = null)
        {
            var dialog = new MessageDialog(title, message, confirmText, cancelText) { Owner = owner };
            return dialog.ShowDialog() == true;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
