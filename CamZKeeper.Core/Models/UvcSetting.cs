using System.Text.Json.Serialization;
using DirectShowLib;

namespace CamZKeeper.Core.Models;

/// <summary>
/// Representa uma propriedade UVC persistida.
/// </summary>
public class UvcSetting
{
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }

    public int Minimum { get; set; }

    public int Maximum { get; set; }

    public int Step { get; set; }

    public int DefaultValue { get; set; }

    public bool SupportsAuto { get; set; }

    public bool IsAuto { get; set; }

    public UvcPropertyType Type { get; set; }

    public CameraControlProperty? CameraProperty { get; set; }

    public VideoProcAmpProperty? VideoProperty { get; set; }

    public ExtensionUnitProperty? ExtensionProperty { get; set; }

    /// <summary>
    /// Indica que o valor foi alterado desde o último salvamento em disco.
    /// Não é persistido no CameraSettings.json.
    /// </summary>
    [JsonIgnore]
    public bool IsDirty { get; set; }
}