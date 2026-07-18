using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CamZKeeper.Desktop.Localization;
using CamZKeeper.Desktop.Support;

namespace CamZKeeper.Desktop
{
    public partial class ReportProblemWindow : Window
    {
        public ReportProblemWindow(string? suggestedCameraModel = null)
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(suggestedCameraModel))
                CameraModelTextBox.Text = suggestedCameraModel;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var model = CameraModelTextBox.Text?.Trim() ?? "";
            var description = DescriptionTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(description))
            {
                FeedbackText.Foreground = System.Windows.Media.Brushes.DarkOrange;
                FeedbackText.Text = LocalizationManager.GetString("ReportWindow_EmptyDescription");
                return;
            }

            SendButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            FeedbackText.Foreground = System.Windows.Media.Brushes.Gray;
            FeedbackText.Text = LocalizationManager.GetString("ReportWindow_Sending");

            var success = await DiscordReportService.SendReportAsync(model, description);

            if (success)
            {
                FeedbackText.Foreground = System.Windows.Media.Brushes.Green;
                FeedbackText.Text = LocalizationManager.GetString("ReportWindow_Sent");

                await Task.Delay(1200);
                Close();
            }
            else
            {
                FeedbackText.Foreground = System.Windows.Media.Brushes.DarkRed;
                FeedbackText.Text = LocalizationManager.GetString("ReportWindow_Error");
                SendButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}