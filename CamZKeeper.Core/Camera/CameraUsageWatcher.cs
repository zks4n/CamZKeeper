using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace CamZKeeper.Core.Camera;

/// <summary>
/// Observa em segundo plano a chave de auditoria de privacidade de câmera do
/// Windows (CapabilityAccessManager\ConsentStore\webcam), que é atualizada em
/// tempo real sempre que qualquer aplicativo começa ou para de usar uma webcam.
/// Não faz polling: a thread fica bloqueada aguardando o SO notificar a mudança
/// (RegNotifyChangeKeyValue), sem custo de CPU enquanto nada acontece.
/// </summary>
public class CameraUsageWatcher : IDisposable
{
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegNotifyChangeKeyValue(
        SafeRegistryHandle hKey,
        bool bWatchSubtree,
        RegNotifyFilter dwNotifyFilter,
        SafeWaitHandle hEvent,
        bool fAsynchronous);

    [Flags]
    private enum RegNotifyFilter
    {
        ChangeName = 0x1,
        ChangeLastSet = 0x4
    }

    private const string ConsentStorePath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

    private Thread? _thread;
    private ManualResetEvent? _stopEvent;
    private volatile bool _running;

    /// <summary>
    /// Disparado sempre que algum aplicativo começa ou para de usar QUALQUER
    /// webcam do sistema (a chave não distingue qual dispositivo).
    /// </summary>
    public event EventHandler? CameraUsageChanged;

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _stopEvent = new ManualResetEvent(false);

        _thread = new Thread(WatchLoop)
        {
            IsBackground = true,
            Name = "CameraUsageWatcher"
        };

        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _stopEvent?.Set();
        _thread?.Join(1000);
    }

    private void WatchLoop()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ConsentStorePath, false);

        if (key is null)
        {
            System.Diagnostics.Debug.WriteLine(
                "[CameraUsageWatcher] Chave ConsentStore\\webcam não encontrada nesta versão do Windows.");
            return;
        }

        using var notifyEvent = new AutoResetEvent(false);

        while (_running)
        {
            int hr = RegNotifyChangeKeyValue(
                key.Handle,
                true, // watch subtree - pega qualquer app, não só um específico
                RegNotifyFilter.ChangeName | RegNotifyFilter.ChangeLastSet,
                notifyEvent.SafeWaitHandle,
                true);

            if (hr != 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CameraUsageWatcher] RegNotifyChangeKeyValue falhou, hr={hr}");
                return;
            }

            int signaled = WaitHandle.WaitAny(new WaitHandle[] { notifyEvent, _stopEvent! });

            if (signaled == 1 || !_running)
                break;

            CameraUsageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        Stop();
        _stopEvent?.Dispose();
    }
}