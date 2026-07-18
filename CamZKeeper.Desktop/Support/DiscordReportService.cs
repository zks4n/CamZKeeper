using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CamZKeeper.Desktop.Support;

/// <summary>
/// Envia reports de problema pro Discord via Webhook. Não guarda nenhuma
/// credencial de e-mail - só faz um POST HTTPS pro link do webhook.
/// </summary>
public static class DiscordReportService
{
    private static readonly HttpClient _client = new();

    public static async Task<bool> SendReportAsync(string cameraModel, string description)
    {
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "🐛 Novo report de problema - CamZKeeper",
                    color = 0xE67E22,
                    fields = new[]
                    {
                        new
                        {
                            name = "Modelo da câmera",
                            value = string.IsNullOrWhiteSpace(cameraModel) ? "(não informado)" : Truncate(cameraModel, 256),
                            inline = false
                        },
                        new
                        {
                            name = "Descrição",
                            value = Truncate(description, 1000),
                            inline = false
                        }
                    },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _client.PostAsync(DiscordWebhookConfig.WebhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}