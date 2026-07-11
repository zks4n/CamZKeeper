// CameraController.cs
using System.Runtime.InteropServices;
using DirectShowLib;
using CamZKeeper.Core.Models;

namespace CamZKeeper.Core.Camera;

/// <summary>
/// Responsável por controlar as propriedades UVC de uma webcam.
/// </summary>
public class CameraController
{
    private readonly IAMCameraControl _cameraControl;
    private readonly IAMVideoProcAmp _videoProcAmp;
    private readonly IKsPropertySet? _propertySet;

    /// <summary>
    /// Mapeamento de propriedades proprietárias (Extension Unit) para seu
    /// GUID de property set e selector, conforme documentado pelo fabricante.
    /// </summary>
    private static readonly Dictionary<ExtensionUnitProperty, (Guid PropertySet, int Selector)> ExtensionProperties = new()
    {
        // RightLight (Logitech) - Video Pipe Extension Unit (v3), selector XU_VIDEO_RIGHTLIGHT_MODE_CONTROL.
        [ExtensionUnitProperty.RightLight] = (new Guid("49E40215-F434-47fe-B158-0E885023E51B"), 0x04)
    };

    public CameraController(CameraInfo camera)
    {
        Guid iid = typeof(IBaseFilter).GUID;

        // O parâmetro 'pbc' (IBindCtx) é não-anulável no DirectShowLib, mas a
        // API COM aceita null normalmente (= sem contexto de bind customizado).
        camera.Device.Mon.BindToObject(
            null!,
            null,
            ref iid,
            out object filterObject);

        var filter = (IBaseFilter)filterObject;

        _cameraControl = (IAMCameraControl)filter;
        _videoProcAmp = (IAMVideoProcAmp)filter;
        _propertySet = filter as IKsPropertySet;
    }

    public int GetCameraProperty(CameraControlProperty property)
    {
        _cameraControl.Get(property, out int value, out _);
        return value;
    }

    public (int Min, int Max, int Step, int Default, CameraControlFlags Flags)
        GetCameraPropertyRange(CameraControlProperty property)
    {
        _cameraControl.GetRange(
            property,
            out int min,
            out int max,
            out int step,
            out int def,
            out CameraControlFlags flags);

        return (min, max, step, def, flags);
    }

    public void SetCameraProperty(
        CameraControlProperty property,
        int value,
        bool auto)
    {
        var flags = auto
            ? CameraControlFlags.Auto
            : CameraControlFlags.Manual;

        int hr = _cameraControl.Set(property, value, flags);

        DsError.ThrowExceptionForHR(hr);
    }

    public int GetVideoProcAmpProperty(VideoProcAmpProperty property)
    {
        _videoProcAmp.Get(property, out int value, out _);
        return value;
    }

    public (int Min, int Max, int Step, int Default, VideoProcAmpFlags Flags)
        GetVideoProcAmpPropertyRange(VideoProcAmpProperty property)
    {
        _videoProcAmp.GetRange(
            property,
            out int min,
            out int max,
            out int step,
            out int def,
            out VideoProcAmpFlags flags);

        return (min, max, step, def, flags);
    }

    public void SetVideoProcAmpProperty(
        VideoProcAmpProperty property,
        int value,
        bool auto)
    {
        var flags = auto
            ? VideoProcAmpFlags.Auto
            : VideoProcAmpFlags.Manual;

        int hr = _videoProcAmp.Set(property, value, flags);

        DsError.ThrowExceptionForHR(hr);
    }

    /// <summary>
    /// Verifica junto ao driver se uma propriedade de Extension Unit
    /// (proprietária do fabricante) é suportada por esta câmera.
    /// </summary>
    public bool IsExtensionPropertySupported(ExtensionUnitProperty property)
    {
        if (_propertySet is null)
            return false;

        if (!ExtensionProperties.TryGetValue(property, out var info))
            return false;

        var propertySet = info.PropertySet;

        int hr = _propertySet.QuerySupported(ref propertySet, info.Selector, out var support);

        return hr >= 0
            && support.HasFlag(KsPropertySupport.Get)
            && support.HasFlag(KsPropertySupport.Set);
    }

    public int GetExtensionProperty(ExtensionUnitProperty property)
    {
        if (_propertySet is null)
            throw new InvalidOperationException("IKsPropertySet não é suportado por este dispositivo.");

        var info = ExtensionProperties[property];
        var propertySet = info.PropertySet;

        IntPtr buffer = Marshal.AllocCoTaskMem(sizeof(int));

        try
        {
            Marshal.WriteInt32(buffer, 0);

            int hr = _propertySet.Get(
                ref propertySet,
                info.Selector,
                IntPtr.Zero,
                0,
                buffer,
                sizeof(int),
                out _);

            DsError.ThrowExceptionForHR(hr);

            return Marshal.ReadInt32(buffer);
        }
        finally
        {
            Marshal.FreeCoTaskMem(buffer);
        }
    }

    public void SetExtensionProperty(ExtensionUnitProperty property, int value)
    {
        if (_propertySet is null)
            throw new InvalidOperationException("IKsPropertySet não é suportado por este dispositivo.");

        var info = ExtensionProperties[property];
        var propertySet = info.PropertySet;

        IntPtr buffer = Marshal.AllocCoTaskMem(sizeof(int));

        try
        {
            Marshal.WriteInt32(buffer, value);

            int hr = _propertySet.Set(
                ref propertySet,
                info.Selector,
                IntPtr.Zero,
                0,
                buffer,
                sizeof(int));

            DsError.ThrowExceptionForHR(hr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(buffer);
        }
    }

    public void ApplySettings(CameraSettings settings)
    {
        foreach (var property in settings.CameraControls)
        {
            if (!Enum.TryParse(property.Name, out CameraControlProperty cameraProperty))
                continue;

            SetCameraProperty(
                cameraProperty,
                property.Value,
                property.IsAuto);
        }

        foreach (var property in settings.VideoProcAmpControls)
        {
            if (!Enum.TryParse(property.Name, out VideoProcAmpProperty videoProperty))
                continue;

            SetVideoProcAmpProperty(
                videoProperty,
                property.Value,
                property.IsAuto);
        }

        foreach (var property in settings.ExtensionControls)
        {
            if (!Enum.TryParse(property.Name, out ExtensionUnitProperty extensionProperty))
                continue;

            SetExtensionProperty(extensionProperty, property.Value);
        }
    }

    public IEnumerable<UvcSetting> DiscoverCameraControls()
    {
        foreach (CameraControlProperty property in Enum.GetValues<CameraControlProperty>())
        {
            int hr = _cameraControl.GetRange(
                property,
                out int min,
                out int max,
                out int step,
                out int def,
                out CameraControlFlags supportedFlags);

            if (hr < 0)
                continue;

            _cameraControl.Get(
                property,
                out int current,
                out CameraControlFlags currentFlags);

            yield return new UvcSetting
            {
                Name = property.ToString(),

                Type = UvcPropertyType.CameraControl,

                CameraProperty = property,

                Value = current,
                Minimum = min,
                Maximum = max,
                Step = step,
                DefaultValue = def,

                SupportsAuto =
        supportedFlags.HasFlag(CameraControlFlags.Auto),

                IsAuto =
        currentFlags.HasFlag(CameraControlFlags.Auto)
            };
        }
    }

    public IEnumerable<UvcSetting> DiscoverVideoProcAmpControls()
    {
        foreach (VideoProcAmpProperty property in Enum.GetValues<VideoProcAmpProperty>())
        {
            int hr = _videoProcAmp.GetRange(
                property,
                out int min,
                out int max,
                out int step,
                out int def,
                out VideoProcAmpFlags supportedFlags);

            if (hr < 0)
                continue;

            _videoProcAmp.Get(
                property,
                out int current,
                out VideoProcAmpFlags currentFlags);

            yield return new UvcSetting
            {
                Name = property.ToString(),

                Type = UvcPropertyType.VideoProcAmp,

                VideoProperty = property,

                Value = current,
                Minimum = min,
                Maximum = max,
                Step = step,
                DefaultValue = def,

                SupportsAuto =
        supportedFlags.HasFlag(VideoProcAmpFlags.Auto),

                IsAuto =
        currentFlags.HasFlag(VideoProcAmpFlags.Auto)
            };
        }
    }

    /// <summary>
    /// Descobre propriedades proprietárias (Extension Unit) suportadas por esta
    /// câmera especificamente. Câmeras que não suportam nenhuma (a maioria,
    /// exceto certos modelos Logitech) simplesmente não retornam nada aqui.
    /// </summary>
    public IEnumerable<UvcSetting> DiscoverExtensionUnitControls()
    {
        foreach (ExtensionUnitProperty property in Enum.GetValues<ExtensionUnitProperty>())
        {
            if (!IsExtensionPropertySupported(property))
                continue;

            int current;

            try
            {
                current = GetExtensionProperty(property);
            }
            catch
            {
                // Suportado segundo QuerySupported, mas o Get falhou na prática.
                // Melhor não expor do que quebrar a descoberta das outras propriedades.
                continue;
            }

            yield return new UvcSetting
            {
                Name = property.ToString(),

                Type = UvcPropertyType.ExtensionUnit,

                ExtensionProperty = property,

                Value = current,
                Minimum = 0,
                Maximum = 1,
                Step = 1,
                DefaultValue = 0,

                SupportsAuto = false,
                IsAuto = false
            };
        }
    }
}