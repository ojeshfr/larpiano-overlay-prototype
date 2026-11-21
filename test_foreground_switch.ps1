Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class ForegroundTest
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public const byte VK_MENU = 0x12; // ALT key
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static void ForceForeground(IntPtr hWnd)
    {
        keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
        SetForegroundWindow(hWnd);
        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static string GetTitle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return "(None)";
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, 256);
        return sb.ToString();
    }

    public static void ClickAt(int x, int y)
    {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(80);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(80);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }
}
"@

# Start Notepad as mock background game window
$np = Start-Process notepad.exe -PassThru
Start-Sleep -Seconds 2

$npProc = Get-Process notepad | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$npHwnd = $npProc.MainWindowHandle
Write-Host "Target Mock Game Window HWND: $npHwnd"

[ForegroundTest]::ForceForeground($npHwnd)
Start-Sleep -Milliseconds 500

$activeFg = [ForegroundTest]::GetForegroundWindow()
Write-Host "Active Foreground Window: $([ForegroundTest]::GetTitle($activeFg)) ($activeFg)"

# Launch Larpiano Overlay
$overlay = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2

$overlayHwnd = $overlay.MainWindowHandle
Write-Host "Overlay HWND: $overlayHwnd"

# Check if overlay stole focus on startup
$fgAfterLaunch = [ForegroundTest]::GetForegroundWindow()
Write-Host "Foreground after Overlay Launched: $([ForegroundTest]::GetTitle($fgAfterLaunch)) ($fgAfterLaunch)"

# Now click on the Overlay Play/Pause button
$rect = New-Object ForegroundTest+RECT
[ForegroundTest]::GetWindowRect($overlayHwnd, [ref]$rect)
$clickX = $rect.Left + 100
$clickY = $rect.Bottom - 80

Write-Host "Simulating user mouse click on Overlay button at ($clickX, $clickY)..."
[ForegroundTest]::ClickAt($clickX, $clickY)
Start-Sleep -Milliseconds 400

$fgAfterClick = [ForegroundTest]::GetForegroundWindow()
Write-Host "Foreground after clicking Overlay button: $([ForegroundTest]::GetTitle($fgAfterClick)) ($fgAfterClick)"

$didOverlayStealFocus = ($fgAfterClick -eq $overlayHwnd)
Write-Host "Did overlay steal foreground focus away from the game? $didOverlayStealFocus"

# Cleanup
Stop-Process -Id $overlay.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Name notepad -Force -ErrorAction SilentlyContinue
