using System.Windows;

namespace GameLauncher
{
    public partial class ChangelogWindow : Window
    {
        public ChangelogWindow(string newVersion, string changelog)
        {
            InitializeComponent();
            TitleText.Text = $"What's new in v{newVersion}";
            ChangelogText.Text = string.IsNullOrWhiteSpace(changelog)
                ? "No changelog details provided for this update."
                : changelog;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
