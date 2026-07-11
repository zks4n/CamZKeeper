using System.Text.Json;
using CamZKeeper.Core.Models;

namespace CamZKeeper.Core.Services;

/// <summary>
/// Responsável por salvar e carregar as configurações da câmera.
/// </summary>
public class SettingsService
{
    private static readonly string FilePath =
    Path.Combine(AppContext.BaseDirectory, "CameraSettings.json");

    public void Save(CameraSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(settings, options);

        File.WriteAllText(FilePath, json);
    }

    public CameraSettings? Load()
    {
        if (!File.Exists(FilePath))
            return null;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = File.ReadAllText(FilePath);

        return JsonSerializer.Deserialize<CameraSettings>(json, options);
    }
}