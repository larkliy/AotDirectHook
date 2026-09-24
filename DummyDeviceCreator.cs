using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

[SupportedOSPlatform("windows")]
public static unsafe class DummyDeviceCreator
{
    private static ID3D11Device* _device;
    private static ID3D11DeviceContext* _context;
    private static IDXGISwapChain* _swapChain;

    public static IDXGISwapChain* SwapChain => _swapChain;

    public static void** Create()
    {
        if (_swapChain != null)
        {
            return _swapChain->lpVtbl;
        }

        var hwnd = HiddenWindow.Create();

        if (hwnd == HWND.NULL)
        {
            return null;
        }

        var description = new DXGI_SWAP_CHAIN_DESC
        {
            BufferDesc = new DXGI_MODE_DESC
            {
                Width = 1,
                Height = 1,
                RefreshRate = new DXGI_RATIONAL { Numerator = 60, Denominator = 1 },
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            },
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            BufferUsage = DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = 2,
            OutputWindow = hwnd,
            Windowed = new BOOL(1),
            SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_DISCARD,
            Flags = 0,
        };

        var featureLevel = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0;

        ID3D11Device* device = null;
        ID3D11DeviceContext* context = null;
        IDXGISwapChain* swapChain = null;

        var hr = DirectX.D3D11CreateDeviceAndSwapChain(
            null,
            D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
            HMODULE.NULL,
            0,
            &featureLevel,
            1,
            D3D11.D3D11_SDK_VERSION,
            &description,
            &swapChain,
            &device,
            null,
            &context);

        if (hr.FAILED)
        {
            return null;
        }

        _device = device;
        _context = context;
        _swapChain = swapChain;

        return _swapChain->lpVtbl;
    }

    public static void Destroy()
    {
        if (_swapChain != null)
        {
            _swapChain->Release();
            _swapChain = null;
        }

        if (_context != null)
        {
            _context->Release();
            _context = null;
        }

        if (_device != null)
        {
            _device->Release();
            _device = null;
        }

        HiddenWindow.Destroy();
    }
}