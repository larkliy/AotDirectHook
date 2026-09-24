using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

[SupportedOSPlatform("windows")]
public static unsafe class PresentHook
{
    private const int PresentIndex = 8;
    private const int Present1Index = 22;

    private static void** _vtable;
    private static delegate* unmanaged[Stdcall]<IDXGISwapChain*, uint, uint, int> _originalPresent;
    private static delegate* unmanaged[Stdcall]<IDXGISwapChain1*, uint, uint, DXGI_PRESENT_PARAMETERS*, int> _originalPresent1;

    public static delegate* unmanaged[Stdcall]<IDXGISwapChain*, void> OnPresent;

    public static bool IsHooked => _vtable != null;

    public static bool Install()
    {
        if (_vtable != null)
        {
            return true;
        }

        var vtable = DummyDeviceCreator.Create();

        if (vtable == null)
        {
            return false;
        }

        if (!HasPresent1Slot(vtable))
        {
            DummyDeviceCreator.Destroy();
            return false;
        }

        if (!TrySetProtection(vtable, out var start, out var size, out var oldProtection))
        {
            DummyDeviceCreator.Destroy();
            return false;
        }

        _originalPresent = (delegate* unmanaged[Stdcall]<IDXGISwapChain*, uint, uint, int>)vtable[PresentIndex];
        _originalPresent1 = (delegate* unmanaged[Stdcall]<IDXGISwapChain1*, uint, uint, DXGI_PRESENT_PARAMETERS*, int>)vtable[Present1Index];

        vtable[PresentIndex] = (delegate* unmanaged[Stdcall]<IDXGISwapChain*, uint, uint, int>)&Present;
        vtable[Present1Index] = (delegate* unmanaged[Stdcall]<IDXGISwapChain1*, uint, uint, DXGI_PRESENT_PARAMETERS*, int>)&Present1;

        _ = Windows.VirtualProtect((void*)start, size, oldProtection, &oldProtection);
        _vtable = vtable;

        return true;
    }

    public static void Uninstall()
    {
        var vtable = _vtable;

        if (vtable == null)
        {
            return;
        }

        if (TrySetProtection(vtable, out var start, out var size, out var oldProtection))
        {
            vtable[PresentIndex] = _originalPresent;
            vtable[Present1Index] = _originalPresent1;

            _ = Windows.VirtualProtect((void*)start, size, oldProtection, &oldProtection);
        }

        _originalPresent = null;
        _originalPresent1 = null;
        _vtable = null;

        DummyDeviceCreator.Destroy();
    }

    private static bool HasPresent1Slot(void** vtable)
    {
        var swapChain1 = (IDXGISwapChain1*)null;
        var iid = IID.IID_IDXGISwapChain1;

        if (DummyDeviceCreator.SwapChain->QueryInterface(&iid, (void**)&swapChain1).FAILED)
        {
            return false;
        }

        var sameTable = swapChain1->lpVtbl == vtable;
        swapChain1->Release();

        return sameTable;
    }

    private static bool TrySetProtection(void** vtable, out nuint start, out nuint size, out uint oldProtection)
    {
        var pageSize = (nuint)Environment.SystemPageSize;
        var offset = PresentIndex * (nuint)sizeof(void*);
        var last = Present1Index * (nuint)sizeof(void*) + (nuint)sizeof(void*);

        start = ((nuint)vtable + offset) & ~(pageSize - 1);
        var end = ((nuint)vtable + last + pageSize - 1) & ~(pageSize - 1);
        size = end - start;

        uint previous = 0;
        var protectedSlots = Windows.VirtualProtect((void*)start, size, (uint)PAGE.PAGE_READWRITE, &previous).Value != 0;
        oldProtection = previous;

        return protectedSlots;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Present(IDXGISwapChain* swapChain, uint syncInterval, uint flags)
    {
        if (OnPresent != null)
        {
            OnPresent(swapChain);
        }

        return _originalPresent(swapChain, syncInterval, flags);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Present1(IDXGISwapChain1* swapChain, uint syncInterval, uint flags, DXGI_PRESENT_PARAMETERS* parameters)
    {
        if (OnPresent != null)
        {
            OnPresent((IDXGISwapChain*)swapChain);
        }

        return _originalPresent1(swapChain, syncInterval, flags, parameters);
    }
}
