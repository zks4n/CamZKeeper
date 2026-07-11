using System;
using System.Runtime.InteropServices;

namespace CamZKeeper.Core.Camera;

[Flags]
public enum KsPropertySupport
{
    None = 0,
    Get = 0x1,
    Set = 0x2
}

/// <summary>
/// Interface COM de baixo nível (WDM/Kernel Streaming) usada para acessar
/// propriedades proprietárias de fabricantes (Extension Units) que não são
/// expostas pelas interfaces padrão do DirectShow (IAMCameraControl/IAMVideoProcAmp).
/// </summary>
[ComImport]
[Guid("31EFAC30-515C-11d0-A9AA-00AA0061BE93")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IKsPropertySet
{
    [PreserveSig]
    int Set(
        ref Guid guidPropSet,
        int dwPropID,
        IntPtr pInstanceData,
        int cbInstanceData,
        IntPtr pPropData,
        int cbPropData);

    [PreserveSig]
    int Get(
        ref Guid guidPropSet,
        int dwPropID,
        IntPtr pInstanceData,
        int cbInstanceData,
        IntPtr pPropData,
        int cbPropData,
        out int pcbReturned);

    [PreserveSig]
    int QuerySupported(
        ref Guid guidPropSet,
        int dwPropID,
        out KsPropertySupport pTypeSupport);
}