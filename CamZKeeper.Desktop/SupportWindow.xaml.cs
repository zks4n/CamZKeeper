using System.Diagnostics;
using System.Windows;

namespace CamZKeeper.Desktop
{
    public partial class SupportWindow : Window
    {
        public SupportWindow()
        {
            InitializeComponent();
        }

        private void TwitchButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirLink("https://www.twitch.tv/zks4n");
        }

        private void LivePixButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirLink("https://livepix.gg/zks4n");
        }

        private static void AbrirLink(string url)
        {
            try
            {
                // UseShellExecute=true é necessário no .NET moderno pra abrir
                // um link no navegador padrão - sem isso, o Process.Start
                // tenta executar a URL como se fosse um programa e falha.
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Falha segura - não derruba o app se não conseguir abrir o navegador.
            }
        }
    }
}