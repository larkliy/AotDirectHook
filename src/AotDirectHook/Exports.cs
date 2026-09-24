using System.Runtime.InteropServices;

namespace AotDirectHook;

public static partial class Exports
{
    public static volatile bool _isRunning = false;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)] 
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll")] 
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();


    [UnmanagedCallersOnly(EntryPoint = "Initialize")]
    public static void Initialize()
    {
        if (_isRunning) return;

        try
        {
            _isRunning = true;
            AllocConsole();

        }
        finally
        {
            FreeConsole();
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "Shutdown")]
    public static void Shutdown()
    {
        _isRunning = false;
    }
}
