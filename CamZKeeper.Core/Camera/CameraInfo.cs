using DirectShowLib;

namespace CamZKeeper.Core.Camera;

/// <summary>
/// Representa uma webcam detectada no sistema.
/// </summary>
public class CameraInfo
{
    public string Name { get; init; } = string.Empty;

    public string DevicePath { get; init; } = string.Empty;

    public DsDevice Device { get; init; } = null!;
}