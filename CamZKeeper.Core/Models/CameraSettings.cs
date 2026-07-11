namespace CamZKeeper.Core.Models;

/// <summary>
/// Configurações persistidas de uma câmera.
/// </summary>
public class CameraSettings
{
    public string CameraName { get; set; } = string.Empty;

    public string DevicePath { get; set; } = string.Empty;

    public List<UvcSetting> CameraControls { get; set; } = [];

    public List<UvcSetting> VideoProcAmpControls { get; set; } = [];

    public List<UvcSetting> ExtensionControls { get; set; } = [];
}