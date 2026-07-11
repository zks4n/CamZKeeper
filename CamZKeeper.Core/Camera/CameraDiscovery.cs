using System.Runtime.InteropServices;
using DirectShowLib;

namespace CamZKeeper.Core.Camera;

/// <summary>
/// Responsável por localizar webcams disponíveis no sistema.
/// </summary>
public class CameraDiscovery
{
    public IReadOnlyList<CameraInfo> GetCameras()
    {
        return DsDevice
            .GetDevicesOfCat(FilterCategory.VideoInputDevice)
            .Select(device => new CameraInfo
            {
                Name = device.Name,
                DevicePath = device.DevicePath,
                Device = device
            })
            .Where(IsSupported)
            .ToList();
    }

    public CameraInfo? FindByName(string name)
    {
        return GetCameras()
            .FirstOrDefault(camera =>
                camera.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    public CameraInfo? FindByDevicePath(string devicePath)
    {
        return GetCameras()
            .FirstOrDefault(camera =>
                string.Equals(
                    camera.DevicePath,
                    devicePath,
                    StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifica se o dispositivo expõe as interfaces UVC padrão
    /// (IAMCameraControl/IAMVideoProcAmp). Câmeras virtuais (OBS Virtual Camera,
    /// ManyCam, etc.) geralmente não implementam essas interfaces e são
    /// filtradas para não aparecerem na lista.
    /// </summary>
    private static bool IsSupported(CameraInfo camera)
    {
        object? filterObject = null;

        try
        {
            Guid iid = typeof(IBaseFilter).GUID;

            // O parâmetro 'pbc' (IBindCtx) é não-anulável no DirectShowLib, mas a
            // API COM aceita null normalmente (= sem contexto de bind customizado).
            camera.Device.Mon.BindToObject(
                null!,
                null,
                ref iid,
                out filterObject);

            return filterObject is IAMCameraControl
                && filterObject is IAMVideoProcAmp;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (filterObject is not null)
                Marshal.ReleaseComObject(filterObject);
        }
    }
}