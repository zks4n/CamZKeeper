// ConfigurationService.cs
using System.Text.Json;

namespace CamZKeeper.Core.Configuration;

public class ConfigurationService
{
    // %AppData%\CamZKeeper\ - mesma pasta usada pelo SettingsService,
    // sempre gravável sem precisar de admin.
    private static readonly string AppDataFolder =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CamZKeeper");

    private static readonly string FilePath =
    Path.Combine(AppDataFolder, "config.json");

    public ApplicationConfiguration Load()
    {
        if (!File.Exists(FilePath))
            throw new FileNotFoundException(
                $"Arquivo '{FilePath}' não encontrado.");

        var json = File.ReadAllText(FilePath);

        var configuration = JsonSerializer.Deserialize<ApplicationConfiguration>(
            json,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        if (configuration is null)
            throw new InvalidOperationException(
                "Não foi possível carregar a configuração.");

        return configuration;
    }

    public void Save(ApplicationConfiguration configuration)
    {
        Directory.CreateDirectory(AppDataFolder);

        var json = JsonSerializer.Serialize(
            configuration,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        File.WriteAllText(FilePath, json);
    }
}