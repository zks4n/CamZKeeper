using System;
using System.Linq;
using System.Windows;

namespace CamZKeeper.Desktop
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow mainWindow = new MainWindow();

            if (e.Args.Contains("--startup") && e.Args.Contains("--background"))
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.ShowInTaskbar = false;
                mainWindow.Hide();
            }
            else
            {
                mainWindow.RestaurarJanela();
            }
        }
    }
}