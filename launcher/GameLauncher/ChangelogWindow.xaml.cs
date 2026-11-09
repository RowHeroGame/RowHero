using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameLauncher
{
    public partial class ChangelogWindow : Window
    {
        // subject = what is being updated, e.g. "game" or "song data"
        public ChangelogWindow(string newVersion, string changelog, string subject = "")
        {
            InitializeComponent();

            var hasSubject = !string.IsNullOrWhiteSpace(subject);
            BadgeText.Text = hasSubject ? $"{subject.ToUpperInvariant()} UPDATE" : "UPDATE AVAILABLE";
            TitleText.Text = hasSubject
                ? $"What's new in {subject} v{newVersion}"
                : $"What's new in v{newVersion}";

            BuildNotes(changelog);

            KeyDown += (s, e) => { if (e.Key == Key.Escape) DialogResult = false; };
        }

        // Custom badge + title, e.g. when the game and song data update together
        public ChangelogWindow(string badge, string title, string changelog, bool customTitle)
        {
            InitializeComponent();

            BadgeText.Text = badge;
            TitleText.Text = title;

            BuildNotes(changelog);

            KeyDown += (s, e) => { if (e.Key == Key.Escape) DialogResult = false; };
        }

        /// <summary>
        /// Lines starting with "- " become bullets with a yellow square;
        /// any other line is shown as a bold sub-heading.
        /// </summary>
        private void BuildNotes(string changelog)
        {
            var foam = (Brush)Application.Current.FindResource("FoamBrush");
            var mist = (Brush)Application.Current.FindResource("MistBrush");
            var oar = (Brush)Application.Current.FindResource("OarBrush");

            if (string.IsNullOrWhiteSpace(changelog))
            {
                NotesPanel.Children.Add(new TextBlock
                {
                    Text = "No changelog details provided for this update.",
                    Foreground = mist,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });
                return;
            }

            foreach (var raw in changelog.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                var isBullet = line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• ");
                if (isBullet) line = line.Substring(2).Trim();

                var text = new TextBlock
                {
                    Text = line,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = foam,
                    FontSize = 14,
                    LineHeight = 21
                };

                if (!isBullet)
                {
                    text.FontWeight = FontWeights.Bold;
                    text.Margin = new Thickness(0, 4, 0, 8);
                    NotesPanel.Children.Add(text);
                    continue;
                }

                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var dot = new Rectangle
                {
                    Width = 8,
                    Height = 8,
                    Fill = oar,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 7, 12, 0)
                };

                Grid.SetColumn(text, 1);
                row.Children.Add(dot);
                row.Children.Add(text);
                NotesPanel.Children.Add(row);
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
