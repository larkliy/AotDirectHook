using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.CS;
using static TerraFX.Interop.Windows.IDC;
using static TerraFX.Interop.Windows.WS;

[SupportedOSPlatform("windows")]
public static unsafe class HiddenWindow
{
    private const string ClassName = "AotDirectHookDummyWindow";

    private static HWND _hwnd;
    private static HINSTANCE _hInstance;

    public static HWND Handle => _hwnd;

    public static HWND Create()
    {
        if (_hwnd != HWND.NULL)
        {
            return _hwnd;
        }

        _hInstance = Windows.GetModuleHandleW(null);

        fixed (char* className = ClassName)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                style = CS_HREDRAW | CS_VREDRAW,
                lpfnWndProc = &WindowProc,
                hInstance = _hInstance,
                hCursor = Windows.LoadCursorW(HINSTANCE.NULL, IDC_ARROW),
                lpszClassName = className,
            };

            _ = Windows.RegisterClassExW(&windowClass);

            _hwnd = Windows.CreateWindowExW(
                0,
                className,
                className,
                WS_POPUP,
                0,
                0,
                1,
                1,
                HWND.NULL,
                HMENU.NULL,
                _hInstance,
                null);
        }

        return _hwnd;
    }

    public static void Destroy()
    {
        if (_hwnd == HWND.NULL)
        {
            return;
        }

        _ = Windows.DestroyWindow(_hwnd);
        _hwnd = HWND.NULL;

        fixed (char* className = ClassName)
        {
            _ = Windows.UnregisterClassW(className, _hInstance);
        }
    }

    [UnmanagedCallersOnly]
    private static LRESULT WindowProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
        => Windows.DefWindowProcW(hwnd, message, wParam, lParam);
}
