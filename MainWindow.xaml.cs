using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using GameLauncher.Config;
using GameLauncher.Models;
using GameLauncher.Services;

namespace GameLauncher
{
    public enum ButtonState { Install, Update, Play, Busy }

    public partial class MainWindow : Window
    {
        private Manifest? _manifest;
        private ButtonState _state = ButtonState.Busy;

        public MainWindow()
        {
            InitializeComponent();
            Title = AppConfig.GameName;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetBusy("Checking for launcher updates...");

            _manifest = await ManifestService.FetchAsync();

            if (_manifest == null)
            {
                // Offline / manifest unreachable - fall back to whatever's installed.
                StatusText.Text = "Couldn't reach update server. You can still play if installed.";
                RefreshGameButtonState();
                return;
            }

            // 1. Launcher self-update check (happens silently before anything else)
            if (VersionComparer.IsNewer(AppConfig.CurrentLauncherVersion, _manifest.Launcher.Version))
            {
                StatusText.Text = $"Updating launcher to v{_manifest.Launcher.Version}...";
                var applied = await UpdaterService.DownloadAndApplyAsync(_manifest.Launcher.Url);
                if (applied)
                {
                    // The helper script will relaunch us - close this instance now.
                    Application.Current.Shutdown();
                    return;
                }
                // If it failed, just continue into the game-check flow below.
            }

            StatusText.Text = "";
            RefreshGameButtonState();
        }

        private void RefreshGameButtonState()
        {
            var local = InstallService.GetLocalVersion();
            var manifestGameVersion = _manifest?.Game.Version;

            if (local == null)
            {
                SetState(ButtonState.Install, manifestGameVersion ?? "-.-.-");
            }
            else if (_manifest != null && VersionComparer.IsNewer(local.Version, manifestGameVersion))
            {
                SetState(ButtonState.Update, local.Version);
            }
            else
            {
                SetState(ButtonState.Play, local.Version);
            }
        }

        private void SetState(ButtonState state, string versionLabel)
        {
            _state = state;
            VersionText.Text = $"v{versionLabel}";
            ActionButton.IsEnabled = true;

            ActionButton.Content = state switch
            {
                ButtonState.Install => "Install Game",
                ButtonState.Update => "Update",
                ButtonState.Play => "Play",
                _ => "..."
            };
        }

        private void SetBusy(string status)
        {
            _state = ButtonState.Busy;
            StatusText.Text = status;
            ActionButton.IsEnabled = false;
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case ButtonState.Install:
                    await RunInstallOrUpdate();
                    break;

                case ButtonState.Update:
                    await ShowChangelogThenUpdate();
                    break;

                case ButtonState.Play:
                    LaunchGame();
                    break;
            }
        }

        private async Task ShowChangelogThenUpdate()
        {
            if (_manifest == null) return;

            var dialog = new ChangelogWindow(_manifest.Game.Version, _manifest.Game.Changelog)
            {
                Owner = this
            };
            var confirmed = dialog.ShowDialog();

            if (confirmed == true)
            {
                await RunInstallOrUpdate();
            }
        }

        private async Task RunInstallOrUpdate()
        {
            if (_manifest == null) return;

            ActionButton.IsEnabled = false;
            ProgressBarControl.Visibility = Visibility.Visible;

            var progress = new Progress<InstallProgress>(p =>
            {
                StatusText.Text = p.Stage;
                if (p.Percent >= 0)
                {
                    ProgressBarControl.IsIndeterminate = false;
                    ProgressBarControl.Value = p.Percent;
                }
                else
                {
                    ProgressBarControl.IsIndeterminate = true;
                }
            });

            try
            {
                await InstallService.InstallOrUpdateAsync(_manifest.Game, progress);
                StatusText.Text = "Ready to play.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Install failed - check your connection and try again.";
                MessageBox.Show(ex.Message, "Install error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressBarControl.Visibility = Visibility.Collapsed;
                RefreshGameButtonState();
            }
        }

        private void LaunchGame()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppConfig.GameExePath,
                    WorkingDirectory = AppConfig.GameInstallDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't start the game:\n{ex.Message}", "Launch error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
