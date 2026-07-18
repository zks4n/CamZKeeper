using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CamZKeeper.Core.Camera;
using CamZKeeper.Core.Models;
using CamZKeeper.Desktop.Localization;

namespace CamZKeeper.Desktop
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly CamZKeeper.Core.Core.ConfigurationManager _manager = new();
        private readonly CameraUsageWatcher _usageWatcher = new();

        private readonly DispatcherTimer _reapplyDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
        private readonly DispatcherTimer _reapplyBackupTimer = new() { Interval = TimeSpan.FromMilliseconds(1500) };

        private bool _isClosingFromTray;
        private System.Windows.Forms.NotifyIcon _notifyIcon = null!;
        private System.Windows.Forms.ToolStripMenuItem _trayOpenItem = null!;
        private System.Windows.Forms.ToolStripMenuItem _trayExitItem = null!;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            VerificarEstadoConfiguracoes();
            UpdateLanguageButtonContent();
            UpdateSupportButtonContent();

            _reapplyDebounceTimer.Tick += ReapplyDebounceTimer_Tick;
            _reapplyBackupTimer.Tick += ReapplyBackupTimer_Tick;

            _usageWatcher.CameraUsageChanged += UsageWatcher_CameraUsageChanged;
            _usageWatcher.Start();

            _ = InitializeAsync();
        }

        /// <summary>
        /// Inicialização independente do ciclo de vida visual da janela.
        /// Roda mesmo quando o app inicia oculto na bandeja (--startup --background),
        /// já que nesse caso o evento Loaded nunca dispara (a janela nunca é exibida).
        /// </summary>
        private async Task InitializeAsync()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (Array.IndexOf(args, "--startup") >= 0)
            {
                // Dá tempo do driver da webcam terminar de inicializar no boot do Windows.
                await Task.Delay(3000);
            }

            // Localiza a câmera com configs salvas e aplica direto na webcam,
            // mesmo que a janela nunca seja exibida.
            _manager.Apply();

            InicializarDispositivos();
        }

        /// <summary>
        /// Disparado (fora da UI thread) sempre que qualquer app começa ou para
        /// de usar qualquer webcam do sistema. Só reinicia o debounce - a
        /// reaplicação de verdade acontece no Tick, já na UI thread.
        /// </summary>
        private void UsageWatcher_CameraUsageChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _reapplyDebounceTimer.Stop();
                _reapplyDebounceTimer.Start();
            }));
        }

        private void ReapplyDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _reapplyDebounceTimer.Stop();

            _manager.ReapplyToCurrentCamera();

            // Reforço único (não contínuo) pra cobrir o driver resetando
            // sozinho um instante depois do stream realmente começar.
            _reapplyBackupTimer.Stop();
            _reapplyBackupTimer.Start();
        }

        private void ReapplyBackupTimer_Tick(object? sender, EventArgs e)
        {
            _reapplyBackupTimer.Stop();
            _manager.ReapplyToCurrentCamera();
        }

        private void InicializarDispositivos()
        {
            var cameras = _manager.GetAvailableCameras();
            CameraComboBox.ItemsSource = cameras;

            string savedCamera = "";
            try
            {
                using (var appKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\CamZKeeper", false))
                {
                    if (appKey != null)
                    {
                        savedCamera = appKey.GetValue("SelectedCamera", "").ToString() ?? "";
                    }
                }
            }
            catch
            {
                // Falha segura
            }

            int targetIndex = 0;
            int currentIndex = 0;
            bool hasCameras = false;

            foreach (var camera in cameras)
            {
                hasCameras = true;
                if (camera.Name == savedCamera)
                {
                    targetIndex = currentIndex;
                }
                currentIndex++;
            }

            if (!hasCameras)
            {
                StatusText.Text = LocalizationManager.GetString("Status_NoCameraDetected");
                return;
            }

            CameraComboBox.SelectedIndex = targetIndex;
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();

            try
            {
                if (Environment.ProcessPath != null)
                {
                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
                }
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.Text = "CamZKeeper";
            _notifyIcon.DoubleClick += (s, e) => RestaurarJanela();

            _trayOpenItem = new System.Windows.Forms.ToolStripMenuItem(
                LocalizationManager.GetString("Tray_Open"), null, (s, e) => RestaurarJanela());

            _trayExitItem = new System.Windows.Forms.ToolStripMenuItem(
                LocalizationManager.GetString("Tray_Exit"), null, (s, e) => FecharAplicacao());

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add(_trayOpenItem);
            contextMenu.Items.Add(_trayExitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        public void RestaurarJanela()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void FecharAplicacao()
        {
            _isClosingFromTray = true;
            _usageWatcher.Stop();
            _reapplyDebounceTimer.Stop();
            _reapplyBackupTimer.Stop();
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            bool isTrayChecked = TrayCheckBox.IsChecked ?? false;

            if (!_isClosingFromTray && isTrayChecked)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }
            else
            {
                _usageWatcher.Stop();
                _reapplyDebounceTimer.Stop();
                _reapplyBackupTimer.Stop();
                _notifyIcon.Dispose();
                base.OnClosing(e);
            }
        }

        private void VerificarEstadoConfiguracoes()
        {
            StartupCheckBox.Checked -= ConfigCheckBox_Changed;
            StartupCheckBox.Unchecked -= ConfigCheckBox_Changed;
            TrayCheckBox.Checked -= ConfigCheckBox_Changed;
            TrayCheckBox.Unchecked -= ConfigCheckBox_Changed;

            try
            {
                using (var appKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\CamZKeeper", false))
                {
                    if (appKey != null)
                    {
                        var trayVal = appKey.GetValue("TrayEnabled");
                        TrayCheckBox.IsChecked = trayVal != null && (int)trayVal == 1;
                    }
                }

                using (var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (runKey != null)
                    {
                        StartupCheckBox.IsChecked = runKey.GetValue("CamZKeeper") != null;
                    }
                }
            }
            catch
            {
                // Falha segura
            }
            finally
            {
                StartupCheckBox.Checked += ConfigCheckBox_Changed;
                StartupCheckBox.Unchecked += ConfigCheckBox_Changed;
                TrayCheckBox.Checked += ConfigCheckBox_Changed;
                TrayCheckBox.Unchecked += ConfigCheckBox_Changed;

                _notifyIcon.Visible = TrayCheckBox.IsChecked ?? false;
            }
        }

        private void ConfigCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isStartupChecked = StartupCheckBox.IsChecked ?? false;
            bool isTrayChecked = TrayCheckBox.IsChecked ?? false;

            string runKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            string appKeyPath = @"SOFTWARE\CamZKeeper";
            string appName = "CamZKeeper";
            string? appPath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(appPath)) return;

            try
            {
                using (var appKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(appKeyPath, true))
                {
                    appKey.SetValue("TrayEnabled", isTrayChecked ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                }

                _notifyIcon.Visible = isTrayChecked;

                using (var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKeyPath, true))
                {
                    if (runKey != null)
                    {
                        if (isStartupChecked)
                        {
                            string arguments = isTrayChecked ? "--startup --background" : "--startup";
                            runKey.SetValue(appName, $"\"{appPath}\" {arguments}");
                        }
                        else
                        {
                            runKey.DeleteValue(appName, false);
                        }
                    }
                }

                StatusText.Text = LocalizationManager.GetString("Status_ConfigUpdated");
            }
            catch (Exception ex)
            {
                StatusText.Text = string.Format(LocalizationManager.GetString("Status_SaveError"), ex.Message);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var success = _manager.Save();
            if (success) LoadProperties();
            StatusText.Text = success
                ? LocalizationManager.GetString("Status_Saved")
                : LocalizationManager.GetString("Status_SaveFailed");
            UpdateTitle();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var success = _manager.ResetToDefaults();
            if (!success)
            {
                StatusText.Text = LocalizationManager.GetString("Status_NoCameraSelected");
                return;
            }
            LoadProperties();
            StatusText.Text = LocalizationManager.GetString("Status_DefaultsRestored");
            UpdateTitle();
        }

        private void SupportButton_Click(object sender, RoutedEventArgs e)
        {
            var supportWindow = new SupportWindow
            {
                Owner = this
            };

            supportWindow.ShowDialog();
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ToggleLanguage();

            _trayOpenItem.Text = LocalizationManager.GetString("Tray_Open");
            _trayExitItem.Text = LocalizationManager.GetString("Tray_Exit");

            UpdateLanguageButtonContent();
            UpdateSupportButtonContent();

            // Os nomes das propriedades (Brilho, Foco, etc.) não usam DynamicResource
            // (são dinâmicos por instância), então precisam ser recarregados manualmente.
            if (CameraComboBox.SelectedItem is not null)
                LoadProperties();

            UpdateTitle();
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraComboBox.SelectedItem is not CameraInfo camera) return;

            try
            {
                _manager.SelectCamera(camera);
            }
            catch (Exception ex)
            {
                StatusText.Text = string.Format(LocalizationManager.GetString("Status_CameraOpenError"), camera.Name, ex.Message);
                return;
            }

            LoadProperties();
            UpdateTitle();

            try
            {
                using (var appKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\CamZKeeper", true))
                {
                    appKey.SetValue("SelectedCamera", camera.Name);
                }
            }
            catch
            {
                // Falha segura
            }
        }

        private void LoadProperties()
        {
            PropertiesPanel.Children.Clear();
            var properties = _manager.GetProperties();
            foreach (var property in properties)
            {
                AddPropertyControl(property);
            }
            StatusText.Text = string.Format(LocalizationManager.GetString("Status_PropertiesLoaded"), properties.Count);
        }

        private void AddPropertyControl(UvcSetting property)
        {
            var control = new CamZKeeper.Desktop.Controls.PropertyControl();
            control.Bind(property);
            control.ValueChanged += Property_ValueChanged;
            control.AutoChanged += Property_AutoChanged;
            PropertiesPanel.Children.Add(control);
        }

        private void Property_ValueChanged(object? sender, int value)
        {
            if (sender is not CamZKeeper.Desktop.Controls.PropertyControl control || control.Setting is null) return;
            var success = _manager.UpdateProperty(control.Setting, value);
            if (!success)
            {
                control.Value = control.Setting.Value;
                StatusText.Text = string.Format(LocalizationManager.GetString("Status_ValueRejected"), control.Setting.Name);
                return;
            }
            StatusText.Text = string.Format(LocalizationManager.GetString("Status_ValueChanged"), control.Setting.Name, value);
            UpdateTitle();
        }

        private void Property_AutoChanged(object? sender, bool isAuto)
        {
            if (sender is not CamZKeeper.Desktop.Controls.PropertyControl control || control.Setting is null) return;

            var success = _manager.UpdateAuto(control.Setting, isAuto);

            control.ApplyAutoResult(control.Setting.IsAuto, control.Setting.Value);

            StatusText.Text = success
                ? string.Format(
                    LocalizationManager.GetString("Status_AutoMode"),
                    control.Setting.Name,
                    control.Setting.IsAuto
                        ? LocalizationManager.GetString("Mode_Auto")
                        : LocalizationManager.GetString("Mode_Manual"))
                : string.Format(LocalizationManager.GetString("Status_AutoRejected"), control.Setting.Name);

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Title = _manager.HasUnsavedChanges ? "CamZKeeper *" : "CamZKeeper";
        }

        private void UpdateLanguageButtonContent()
        {
            bool isCurrentlyPortuguese = LocalizationManager.CurrentLanguage == LocalizationManager.PortugueseBr;

            var flagImage = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(isCurrentlyPortuguese
                        ? "/Assets/flag_us.png"
                        : "/Assets/flag_br.png", UriKind.Relative)),
                Width = 18,
                Height = 13,
                VerticalAlignment = VerticalAlignment.Center
            };

            var text = new TextBlock
            {
                Text = LocalizationManager.GetString("LanguageButton"),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var panel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            panel.Children.Add(flagImage);
            panel.Children.Add(text);

            LanguageButton.Content = panel;
        }

        private void UpdateSupportButtonContent()
        {
            var coffeeImage = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("/Assets/coffee.png", UriKind.Relative)),
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            };

            var text = new TextBlock
            {
                Text = LocalizationManager.GetString("SupportButton"),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var panel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            panel.Children.Add(coffeeImage);
            panel.Children.Add(text);

            SupportButton.Content = panel;
        }

        private void ProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            var cameraName = (CameraComboBox.SelectedItem as CameraInfo)?.Name;

            var reportWindow = new ReportProblemWindow(cameraName)
            {
                Owner = this
            };

            reportWindow.ShowDialog();
        }
    }
}