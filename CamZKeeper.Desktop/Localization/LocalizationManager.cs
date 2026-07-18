using System;
using System.Linq;
using System.Windows;

namespace CamZKeeper.Desktop.Localization;

/// <summary>
/// Gerencia o idioma da interface: carrega o dicionário de strings certo,
/// persiste a escolha no registro e permite alternar em tempo de execução
/// (os controles ligados via DynamicResource se atualizam sozinhos).
/// </summary>
public static class LocalizationManager
{
    public const string PortugueseBr = "pt-BR";
    public const string EnglishUs = "en-US";

    private const string RegistryKeyPath = @"SOFTWARE\CamZKeeper";
    private const string RegistryValueName = "Language";

    public static string CurrentLanguage { get; private set; } = PortugueseBr;

    /// <summary>
    /// Deve ser chamado uma vez, no início do App.OnStartup, antes de criar
    /// a MainWindow, pra já carregar o dicionário certo desde o primeiro frame.
    /// </summary>
    public static void Initialize()
    {
        ApplyLanguage(LoadSavedLanguage());
    }

    public static void ToggleLanguage()
    {
        var next = CurrentLanguage == PortugueseBr ? EnglishUs : PortugueseBr;
        ApplyLanguage(next);
        SaveLanguage(next);
    }

    public static void ApplyLanguage(string languageCode)
    {
        var dictionaryPath = languageCode == EnglishUs
            ? "Resources/Strings.en-US.xaml"
            : "Resources/Strings.pt-BR.xaml";

        var newDictionary = new ResourceDictionary
        {
            Source = new Uri(dictionaryPath, UriKind.Relative)
        };

        var existing = System.Windows.Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));

        if (existing != null)
            System.Windows.Application.Current.Resources.MergedDictionaries.Remove(existing);

        System.Windows.Application.Current.Resources.MergedDictionaries.Add(newDictionary);

        CurrentLanguage = languageCode;
    }

    /// <summary>
    /// Busca uma string pelo idioma atual. Usado no code-behind pra mensagens
    /// dinâmicas (StatusText, tray) que não podem usar DynamicResource direto.
    /// </summary>
    public static string GetString(string key)
    {
        return System.Windows.Application.Current.Resources[key] as string ?? key;
    }

    private static string LoadSavedLanguage()
    {
        try
        {
            using var appKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = appKey?.GetValue(RegistryValueName) as string;

            return value == PortugueseBr ? PortugueseBr : EnglishUs;
        }
        catch
        {
            return EnglishUs;
        }
    }

    private static void SaveLanguage(string languageCode)
    {
        try
        {
            using var appKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryKeyPath, true);
            appKey?.SetValue(RegistryValueName, languageCode);
        }
        catch
        {
            // Falha segura
        }
    }

    /// <summary>
    /// Traduz o nome cru de uma propriedade UVC (ex: "Brightness", "Zoom") pro
    /// idioma atual. Se não existir tradução cadastrada (chave "Property_{nome}"),
    /// cai de volta pro nome original em inglês - cobre propriedades novas ou
    /// pouco comuns sem quebrar nada.
    /// </summary>
    public static string GetPropertyDisplayName(string rawName)
    {
        var key = $"Property_{rawName}";
        return System.Windows.Application.Current.Resources[key] as string ?? rawName;
    }
}