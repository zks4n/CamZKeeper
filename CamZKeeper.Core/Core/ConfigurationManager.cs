using DirectShowLib;
using CamZKeeper.Core.Services;
using CamZKeeper.Core.Models;
using CamZKeeper.Core.Configuration;
using CamZKeeper.Core.Camera;

namespace CamZKeeper.Core.Core;

public class ConfigurationManager
{
    private readonly CameraDiscovery _discovery = new();
    private readonly SettingsService _settingsService = new();
    private readonly ConfigurationService _configurationService = new();

    private CameraController? _controller;
    private CameraSettings? _currentSettings;
    private CameraInfo? _currentCamera;

    public CameraSettings? CurrentSettings => _currentSettings;

    /// <summary>
    /// Verdadeiro quando existe ao menos uma propriedade alterada
    /// desde o último salvamento em disco.
    /// </summary>
    public bool HasUnsavedChanges =>
        _currentSettings is not null &&
        (_currentSettings.CameraControls.Any(p => p.IsDirty) ||
         _currentSettings.VideoProcAmpControls.Any(p => p.IsDirty) ||
         _currentSettings.ExtensionControls.Any(p => p.IsDirty));

    public void SelectCamera(CameraInfo camera)
    {
        _currentCamera = camera;
        _controller = new CameraController(camera);

        if (!SettingsExists())
        {
            Export(camera);
        }

        _currentSettings = _settingsService.Load();

        ResolveEnums();

        ClearDirtyFlags();
    }

    public bool Export(CameraInfo camera)
    {
        var controller = new CameraController(camera);

        var settings = new CameraSettings
        {
            CameraName = camera.Name,
            DevicePath = camera.DevicePath,

            CameraControls = controller
                .DiscoverCameraControls()
                .ToList(),

            VideoProcAmpControls = controller
                .DiscoverVideoProcAmpControls()
                .ToList(),

            ExtensionControls = controller
                .DiscoverExtensionUnitControls()
                .ToList()
        };

        _settingsService.Save(settings);

        _currentSettings = settings;

        ResolveEnums();

        ClearDirtyFlags();

        return true;
    }

    public bool Save()
    {
        if (_currentSettings is null)
            return false;

        _settingsService.Save(_currentSettings);

        ClearDirtyFlags();

        return true;
    }

    /// <summary>
    /// Restaura os valores de fábrica (DefaultValue) de todas as propriedades
    /// da câmera atualmente selecionada e aplica imediatamente na webcam.
    /// O usuário ainda precisa clicar em "Salvar" para persistir em disco.
    /// </summary>
    public bool ResetToDefaults()
    {
        if (_controller is null || _currentSettings is null)
            return false;

        foreach (var property in _currentSettings.CameraControls)
        {
            if (property.Value != property.DefaultValue || property.IsAuto)
                property.IsDirty = true;

            property.Value = property.DefaultValue;
            property.IsAuto = false;

            if (property.CameraProperty is null)
                continue;

            _controller.SetCameraProperty(
                property.CameraProperty.Value,
                property.Value,
                property.IsAuto);
        }

        foreach (var property in _currentSettings.VideoProcAmpControls)
        {
            if (property.Value != property.DefaultValue || property.IsAuto)
                property.IsDirty = true;

            property.Value = property.DefaultValue;
            property.IsAuto = false;

            if (property.VideoProperty is null)
                continue;

            _controller.SetVideoProcAmpProperty(
                property.VideoProperty.Value,
                property.Value,
                property.IsAuto);
        }

        foreach (var property in _currentSettings.ExtensionControls)
        {
            if (property.Value != property.DefaultValue)
                property.IsDirty = true;

            property.Value = property.DefaultValue;

            if (property.ExtensionProperty is null)
                continue;

            _controller.SetExtensionProperty(
                property.ExtensionProperty.Value,
                property.Value);
        }

        return true;
    }

    public bool Apply()
    {
        var settings = _settingsService.Load();

        if (settings is null)
            return false;

        var camera = _discovery.FindByDevicePath(settings.DevicePath);

        if (camera is null)
            return false;

        var controller = new CameraController(camera);

        controller.ApplySettings(settings);

        return true;
    }

    /// <summary>
    /// Reaplica as configurações atuais na câmera já selecionada, sem precisar
    /// reabrir/rebindar o dispositivo. Usado quando o CameraUsageWatcher detecta
    /// que outro app começou a usar a webcam, já que algumas câmeras (ex: Logitech
    /// C920) resetam os valores sozinhas nesse momento.
    /// </summary>
    public bool ReapplyToCurrentCamera()
    {
        if (_controller is null || _currentSettings is null)
            return Apply(); // fallback: tenta localizar pelo DevicePath salvo em disco

        try
        {
            _controller.ApplySettings(_currentSettings);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<CameraInfo> GetAvailableCameras()
    {
        return _discovery.GetCameras();
    }

    public IReadOnlyList<UvcSetting> GetProperties()
    {
        if (_currentSettings is null)
            return [];

        return _currentSettings.CameraControls
            .Concat(_currentSettings.VideoProcAmpControls)
            .Concat(_currentSettings.ExtensionControls)
            .OrderBy(p => p.Type)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public CameraSettings? LoadSettings()
    {
        _currentSettings ??= _settingsService.Load();

        ResolveEnums();

        return _currentSettings;
    }

    public bool SettingsExists()
    {
        return _settingsService.Load() is not null;
    }

    /// <summary>
    /// Retorna false se a câmera rejeitar o valor (ex: fora do incremento
    /// suportado). Nesse caso o valor anterior é restaurado.
    /// </summary>
    public bool UpdateProperty(
    UvcSetting setting,
    int newValue)
    {
        if (_controller is null)
            return false;

        var previousValue = setting.Value;

        setting.Value = newValue;

        try
        {
            switch (setting.Type)
            {
                case UvcPropertyType.CameraControl:

                    if (setting.CameraProperty is null)
                        return false;

                    _controller.SetCameraProperty(
                        setting.CameraProperty.Value,
                        setting.Value,
                        setting.IsAuto);

                    break;

                case UvcPropertyType.VideoProcAmp:

                    if (setting.VideoProperty is null)
                        return false;

                    _controller.SetVideoProcAmpProperty(
                        setting.VideoProperty.Value,
                        setting.Value,
                        setting.IsAuto);

                    break;

                case UvcPropertyType.ExtensionUnit:

                    if (setting.ExtensionProperty is null)
                        return false;

                    _controller.SetExtensionProperty(
                        setting.ExtensionProperty.Value,
                        setting.Value);

                    break;
            }
        }
        catch (Exception)
        {
            // A câmera recusou o valor (ex: fora do incremento aceito).
            // Desfaz para não deixar o slider dessincronizado da webcam.
            setting.Value = previousValue;
            return false;
        }

        setting.IsDirty = true;

        return true;
    }

    /// <summary>
    /// Alterna o modo Automático/Manual de uma propriedade em tempo real.
    /// Ao trocar de modo, resincroniza o valor com o que a câmera reporta,
    /// já que o valor pode ter mudado sozinho enquanto estava em Automático.
    /// </summary>
    public bool UpdateAuto(UvcSetting setting, bool isAuto)
    {
        if (_controller is null)
            return false;

        var previousValue = setting.Value;
        var previousIsAuto = setting.IsAuto;

        setting.IsAuto = isAuto;

        try
        {
            switch (setting.Type)
            {
                case UvcPropertyType.CameraControl:

                    if (setting.CameraProperty is null)
                        return false;

                    if (!isAuto)
                        setting.Value = _controller.GetCameraProperty(setting.CameraProperty.Value);

                    _controller.SetCameraProperty(
                        setting.CameraProperty.Value,
                        setting.Value,
                        setting.IsAuto);

                    if (isAuto)
                        setting.Value = _controller.GetCameraProperty(setting.CameraProperty.Value);

                    break;

                case UvcPropertyType.VideoProcAmp:

                    if (setting.VideoProperty is null)
                        return false;

                    if (!isAuto)
                        setting.Value = _controller.GetVideoProcAmpProperty(setting.VideoProperty.Value);

                    _controller.SetVideoProcAmpProperty(
                        setting.VideoProperty.Value,
                        setting.Value,
                        setting.IsAuto);

                    if (isAuto)
                        setting.Value = _controller.GetVideoProcAmpProperty(setting.VideoProperty.Value);

                    break;
            }
        }
        catch (Exception)
        {
            setting.Value = previousValue;
            setting.IsAuto = previousIsAuto;
            return false;
        }

        setting.IsDirty = true;

        return true;
    }

    /// <summary>
    /// Marca todas as propriedades como salvas (sem alterações pendentes).
    /// </summary>
    private void ClearDirtyFlags()
    {
        if (_currentSettings is null)
            return;

        foreach (var property in _currentSettings.CameraControls)
            property.IsDirty = false;

        foreach (var property in _currentSettings.VideoProcAmpControls)
            property.IsDirty = false;

        foreach (var property in _currentSettings.ExtensionControls)
            property.IsDirty = false;
    }

    private void ResolveEnums()
    {
        if (_currentSettings is null)
            return;

        foreach (var property in _currentSettings.CameraControls)
        {
            property.Type = UvcPropertyType.CameraControl;

            if (Enum.TryParse(property.Name, out CameraControlProperty cameraProperty))
            {
                property.CameraProperty = cameraProperty;
            }
        }

        foreach (var property in _currentSettings.VideoProcAmpControls)
        {
            property.Type = UvcPropertyType.VideoProcAmp;

            if (Enum.TryParse(property.Name, out VideoProcAmpProperty videoProperty))
            {
                property.VideoProperty = videoProperty;
            }
        }

        foreach (var property in _currentSettings.ExtensionControls)
        {
            property.Type = UvcPropertyType.ExtensionUnit;

            if (Enum.TryParse(property.Name, out ExtensionUnitProperty extensionProperty))
            {
                property.ExtensionProperty = extensionProperty;
            }
        }
    }
}