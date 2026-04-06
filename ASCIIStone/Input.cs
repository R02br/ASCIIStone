using System.Runtime.InteropServices;

static class Input
{
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    static IntPtr consoleHandle;

    public static void InitializeInput()
    {
        consoleHandle = GetConsoleWindow();
    }

    public static bool GetKey(ConsoleKey consoleKey)
    {
        if (GetForegroundWindow() != consoleHandle) { return false; }

        return (GetAsyncKeyState((int)consoleKey) & 0x8000) != 0;
    }

    public static bool GetKeyDown(ConsoleKey consoleKey)
    {
        if (GetForegroundWindow() != consoleHandle) { return false; }

        return (GetAsyncKeyState((int)consoleKey) & 0x0001) != 0;
    }
}