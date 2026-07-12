// SettingsService.cs
using System.Text.Json;
using CamZKeeper.Core.Models;

namespace CamZKeeper.Core.Services;

/// <summary>
/// Responsável por salvar e carregar as configurações da câmera.
/// </summary>
public class SettingsService
{
    // %AppData%\CamZKeeper\ - pasta de dados do usuário, sempre gravável,
    // ao contrário da pasta de instalação (Program Files), que exige admin.
    private static readonly string AppDataFolder =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CamZKeeper");

    private static readonly string FilePath =
    Path.Combine(AppDataFolder, "CameraSettings.json");

    public void Save(CameraSettings settings)
    {
        Directory.CreateDirectory(AppDataFolder);

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