using System;
using System.Windows;

namespace GameLauncher
{
    public partial class App : Application
    {
        public App()
        {
            // If the launcher ever crashes on startup, show the real error
            // instead of silently closing.
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "Launcher error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
                Shutdown(1);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show(e.ExceptionObject.ToString(), "Launcher error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
