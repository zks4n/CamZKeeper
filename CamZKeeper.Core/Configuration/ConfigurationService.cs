using System.Text.Json;

namespace CamZKeeper.Core.Configuration;

public class ConfigurationService
{
    private static readonly string FilePath =
    Path.Combine(AppContext.BaseDirectory, "config.json");

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
}