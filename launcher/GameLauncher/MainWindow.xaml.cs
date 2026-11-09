using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GameLauncher.Config;
using GameLauncher.Models;
using GameLauncher.Services;

namespace GameLauncher
{
    public enum ButtonState { Install, Update, Play, Launching, Running, Busy }

    public partial class MainWindow : Window
    {
        private Manifest? _manifest;
        private ButtonState _state = ButtonState.Busy;
        private Process? _gameProcess;        // the game we started (or found already running)
        private bool _gameWindowShown;        // false = still launching, true = running
        private bool _pulsing;

        public MainWindow()
        {
            InitializeComponent();
            Title = AppConfig.GameName;
            Loaded += MainWindow_Loaded;
        }

        // ------------------------------------------------------------------
        //  Custom title bar
        // ------------------------------------------------------------------

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ------------------------------------------------------------------
        //  Startup
        // ------------------------------------------------------------------

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetBusy("Checking for launcher updates...");

            _manifest = await ManifestService.FetchAsync();
            AttachToRunningGame();

            if (_manifest == null)
            {
                // Offline / manifest unreachable - fall back to whatever's installed.
                StatusText.Text = "Couldn't reach update server. You can still play if installed.";
                RefreshButtons();
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
            RefreshButtons();
        }

        // ------------------------------------------------------------------
        //  The one main button: Install / Update / Play.
        //  Install and Update cover the game AND the song data - whichever of
        //  them (or both) is missing or out of date gets done in one go.
        // ------------------------------------------------------------------

        // gameData entry present in manifest.json (and we're online)
        private bool HasSongData => _manifest?.GameData != null && !string.IsNullOrWhiteSpace(_manifest.GameData.Url);

        private bool GameNeedsInstall => InstallService.GetLocalVersion() == null;

        private bool GameNeedsUpdate
        {
            get
            {
                var local = InstallService.GetLocalVersion();
                return _manifest != null && local != null && VersionComparer.IsNewer(local.Version, _manifest.Game.Version);
            }
        }

        private bool DataNeedsInstall => HasSongData && InstallService.GetLocalDataVersion() == null;

        private bool DataNeedsUpdate
        {
            get
            {
                if (!HasSongData) return false;
                var local = InstallService.GetLocalDataVersion();
                return local != null && VersionComparer.IsNewer(local.Version, _manifest!.GameData.Version);
            }
        }

        private void RefreshButtons()
        {
            RefreshDataInfo();

            var local = InstallService.GetLocalVersion();
            var remote = _manifest?.Game.Version;

            // While the game is open its files are in use, so nothing else can happen
            if (local != null && IsGameRunning)
            {
                if (_gameWindowShown)
                {
                    SetState(ButtonState.Running, $"v{local.Version}");
                    StatusText.Text = "Game is running. Close it to install updates.";
                }
                else
                {
                    SetState(ButtonState.Launching, $"v{local.Version}");
                    StatusText.Text = "Starting the game...";
                }
                return;
            }

            var gameLabel =
                local == null ? $"Not installed · v{remote ?? "-.-.-"}" :
                GameNeedsUpdate ? $"v{local.Version} -> v{remote}" :
                $"v{local.Version}";

            if (GameNeedsInstall || DataNeedsInstall)
                SetState(ButtonState.Install, gameLabel);
            else if (GameNeedsUpdate || DataNeedsUpdate)
                SetState(ButtonState.Update, gameLabel);
            else
                SetState(ButtonState.Play, gameLabel);
        }

        // Song data version line + the small Reinstall button
        private void RefreshDataInfo()
        {
            if (!HasSongData)
            {
                DataVersionRow.Visibility = Visibility.Collapsed;
                RepairPanel.Visibility = Visibility.Collapsed;
                return;
            }

            DataVersionRow.Visibility = Visibility.Visible;
            RepairPanel.Visibility = Visibility.Visible;

            var info = _manifest!.GameData;
            var local = InstallService.GetLocalDataVersion();

            DataVersionText.Text =
                local == null ? $"Not installed · v{info.Version}" :
                VersionComparer.IsNewer(local.Version, info.Version) ? $"v{local.Version} -> v{info.Version}" :
                $"v{local.Version}";

            // Reinstalling only makes sense once something is installed
            ReinstallButton.IsEnabled = local != null && !IsGameRunning;
        }

        private void SetState(ButtonState state, string versionLabel)
        {
            _state = state;
            VersionText.Text = versionLabel;
            ActionButton.IsEnabled = true;

            // Grey, pressed-in, not clickable while the game is starting or open
            var gameActive = state == ButtonState.Launching || state == ButtonState.Running;
            ActionButton.Style = (Style)FindResource(gameActive ? "PressedButton" : "PrimaryButton");

            ActionButton.Content = state switch
            {
                // Game already there, only the songs are missing
                ButtonState.Install when !GameNeedsInstall => "Install Song Data",
                ButtonState.Install => "Install Game",
                ButtonState.Update => "Update",
                ButtonState.Play => "Play",
                ButtonState.Launching => "Launching...",
                ButtonState.Running => "Running",
                _ => "..."
            };
        }

        private void SetBusy(string status)
        {
            _state = ButtonState.Busy;
            StatusText.Text = status;
            ActionButton.IsEnabled = false;
            ReinstallButton.IsEnabled = false;
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case ButtonState.Install:
                case ButtonState.Update:
                    await InstallOrUpdateAllAsync();
                    break;

                case ButtonState.Play:
                    await LaunchGameAsync();
                    break;
            }
        }

        /// <summary>
        /// Installs/updates everything that needs it - the game, the song data, or both -
        /// one after the other with a single progress bar.
        /// </summary>
        private async Task InstallOrUpdateAllAsync()
        {
            if (_manifest == null) return;
            var game = _manifest.Game;
            var data = _manifest.GameData;

            var doGame = GameNeedsInstall || GameNeedsUpdate;
            var doData = DataNeedsInstall || DataNeedsUpdate;
            if (!doGame && !doData) return;

            // Show what's new for anything being UPDATED (fresh installs skip this)
            var notes = new List<string>();
            if (GameNeedsUpdate) notes.Add($"Game v{game.Version}\n{NotesOrDefault(game.Changelog)}");
            if (DataNeedsUpdate) notes.Add($"Song data v{data.Version}\n{NotesOrDefault(data.Changelog)}");

            if (notes.Count > 0)
            {
                bool confirmed;
                if (GameNeedsUpdate && DataNeedsUpdate)
                {
                    confirmed = ShowCombinedChangelog(string.Join("\n", notes));
                }
                else if (GameNeedsUpdate)
                {
                    confirmed = ShowChangelog("game", game.Version, game.Changelog);
                }
                else
                {
                    confirmed = ShowChangelog("song data", data.Version, data.Changelog);
                }
                if (!confirmed) return;
            }

            var steps = new List<Func<IProgress<InstallProgress>, Task>>();
            if (doGame) steps.Add(p => InstallService.InstallOrUpdateGameAsync(game, p));
            if (doData) steps.Add(p => InstallService.InstallOrUpdateGameDataAsync(data, p));

            var success =
                doGame && doData ? "Game and song data ready." :
                doGame ? "Game ready." :
                "Song data ready.";

            await RunWithProgressAsync(async progress =>
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = i;
                    // Each step fills its share of one bar, e.g. 0-50% then 50-100%
                    var scaled = new Progress<InstallProgress>(p => progress.Report(new InstallProgress
                    {
                        Stage = steps.Count > 1 ? $"{p.Stage} ({step + 1}/{steps.Count})" : p.Stage,
                        Percent = p.Percent < 0 ? -1 : (step * 100 + p.Percent) / steps.Count
                    }));
                    await steps[i](scaled);
                }
            }, success);
        }

        private static string NotesOrDefault(string changelog) =>
            string.IsNullOrWhiteSpace(changelog) ? "- No changelog details provided." : changelog;

        private async void ReinstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_manifest == null) return;
            var data = _manifest.GameData;

            var confirmed = MessageDialog.Show(this,
                "Reinstall song data?",
                "This downloads the song data again and replaces the original song files.\n\n" +
                "Only do this if you get an error, or songs are missing or were deleted by mistake. " +
                "If everything is working, you don't need to.\n\n" +
                "Songs you added yourself won't be deleted.",
                "Reinstall", "Cancel");

            if (!confirmed) return;

            await RunWithProgressAsync(p => InstallService.InstallOrUpdateGameDataAsync(data, p), "Song data reinstalled.");
        }

        // ------------------------------------------------------------------
        //  Shared
        // ------------------------------------------------------------------

        private bool ShowChangelog(string subject, string newVersion, string changelog)
        {
            var dialog = new ChangelogWindow(newVersion, changelog, subject) { Owner = this };
            return dialog.ShowDialog() == true;
        }

        // Game and song data both updating: one window with a section for each
        private bool ShowCombinedChangelog(string changelog)
        {
            var dialog = new ChangelogWindow("GAME + SONG DATA UPDATE", "What's new", changelog, customTitle: true) { Owner = this };
            return dialog.ShowDialog() == true;
        }

        private async Task RunWithProgressAsync(Func<IProgress<InstallProgress>, Task> work, string successMessage)
        {
            ActionButton.IsEnabled = false;
            ReinstallButton.IsEnabled = false;
            SetProgress(0);
            ProgressBarControl.Visibility = Visibility.Visible;

            var progress = new Progress<InstallProgress>(p =>
            {
                StatusText.Text = p.Stage;
                SetProgress(p.Percent);
            });

            try
            {
                await work(progress);
                StatusText.Text = successMessage;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Install failed - check your connection and try again.";
                MessageDialog.Show(this, "Install error", ex.Message, "OK");
            }
            finally
            {
                StopPulse();
                ProgressBarControl.Visibility = Visibility.Hidden;
                RefreshButtons();
            }
        }

        // Known percentage -> fill the bar. Unknown (extracting) -> fill it and pulse.
        private void SetProgress(double percent)
        {
            if (percent >= 0)
            {
                StopPulse();
                ProgressBarControl.Value = percent;
            }
            else if (!_pulsing)
            {
                _pulsing = true;
                ProgressBarControl.Value = 100;
                var pulse = new DoubleAnimation(1.0, 0.35, TimeSpan.FromMilliseconds(700))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                ProgressBarControl.BeginAnimation(OpacityProperty, pulse);
            }
        }

        private void StopPulse()
        {
            _pulsing = false;
            ProgressBarControl.BeginAnimation(OpacityProperty, null);
            ProgressBarControl.Opacity = 1;
        }

        // ------------------------------------------------------------------
        //  Launching / running the game
        // ------------------------------------------------------------------

        private bool IsGameRunning
        {
            get
            {
                try { return _gameProcess != null && !_gameProcess.HasExited; }
                catch { return false; }
            }
        }

        private async Task LaunchGameAsync()
        {
            // Song data is a separate download, so warn if it has never been installed (e.g. offline)
            if (DataNeedsInstall)
            {
                var launchAnyway = MessageDialog.Show(this,
                    "Song data missing",
                    "The song data hasn't been installed yet, so the game may not work properly.",
                    "Launch anyway", "Cancel");
                if (!launchAnyway) return;
            }

            Process? process;
            try
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = AppConfig.GameExePath,
                    WorkingDirectory = AppConfig.GameInstallDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageDialog.Show(this, "Launch error", $"Couldn't start the game:\n{ex.Message}", "OK");
                return;
            }

            if (process == null) return; // started, but Windows gave us nothing to track

            _gameProcess = process;
            _gameWindowShown = false;
            RefreshButtons();   // -> grey "Launching..."
            _ = WatchGameAsync(process);

            await WaitForGameWindowAsync(process);

            // Only switch to "Running" if this is still our game and it hasn't already quit
            if (_gameProcess == process && IsGameRunning)
            {
                _gameWindowShown = true;
                RefreshButtons(); // -> grey "Running"
            }
        }

        // "Launching" lasts until the game opens its window (at least 1.5s so it's visible, at most 30s)
        private static async Task WaitForGameWindowAsync(Process process)
        {
            var minimum = Task.Delay(1500);
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    if (process.HasExited) break;
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero) break;
                }
                catch { break; }

                await Task.Delay(300);
            }

            await minimum;
        }

        // Back to Play / Update the moment the game closes
        private async Task WatchGameAsync(Process process)
        {
            try { await process.WaitForExitAsync(); }
            catch { /* can't watch it any more - treat as closed */ }

            if (_gameProcess == process)
            {
                _gameProcess = null;
                _gameWindowShown = false;
            }

            if (!IsLoaded) return;

            StatusText.Text = "";
            RefreshButtons();
        }

        // If the game was already open when the launcher started, show "Running" straight away
        private void AttachToRunningGame()
        {
            if (IsGameRunning) return;

            try
            {
                var name = Path.GetFileNameWithoutExtension(AppConfig.GameExeName);
                foreach (var candidate in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (!string.Equals(candidate.MainModule?.FileName, AppConfig.GameExePath,
                                StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    catch { continue; } // can't read its path -> not something we can track

                    _gameProcess = candidate;
                    _gameWindowShown = true;
                    _ = WatchGameAsync(candidate);
                    return;
                }
            }
            catch { /* best effort only */ }
        }
    }
}
