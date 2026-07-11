namespace CamZKeeper.Core.Models;

/// <summary>
/// Propriedades proprietárias de fabricantes, acessadas via Extension Unit
/// (IKsPropertySet), fora do padrão UVC exposto por IAMCameraControl/IAMVideoProcAmp.
/// </summary>
public enum ExtensionUnitProperty
{
    /// <summary>
    /// RightLight (Logitech) - compensação automática de pouca luz.
    /// </summary>
    RightLight
}