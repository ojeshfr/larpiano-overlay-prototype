Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32TestUtils
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

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
        System.Threading.Thread.Sleep(80);
    }
}
"@

# 1. Start a foreground window (e.g. Explorer window or Notepad with active UI)
$dummy = Start-Process "notepad.exe" -PassThru
Start-Sleep -Seconds 2

# Find the real notepad main window handle
$npProc = Get-Process notepad | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$npHwnd = $npProc.MainWindowHandle
Write-Host "Target Window (Notepad) HWND: $npHwnd"
[Win32TestUtils]::SetForegroundWindow($npHwnd)
Start-Sleep -Milliseconds 500

$initialFg = [Win32TestUtils]::GetForegroundWindow()
Write-Host "Initial Foreground: $([Win32TestUtils]::GetTitle($initialFg)) ($initialFg)"

# 2. Start Overlay
$overlay = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2

$overlayHwnd = $overlay.MainWindowHandle
Write-Host "Overlay HWND: $overlayHwnd"

$fgAfterOverlay = [Win32TestUtils]::GetForegroundWindow()
Write-Host "Foreground after overlay startup: $([Win32TestUtils]::GetTitle($fgAfterOverlay)) ($fgAfterOverlay)"

# Ensure target is foreground
[Win32TestUtils]::SetForegroundWindow($npHwnd)
Start-Sleep -Milliseconds 500

$rect = New-Object Win32TestUtils+RECT
[Win32TestUtils]::GetWindowRect($overlayHwnd, [ref]$rect)
Write-Host "Overlay Bounds: Left=$($rect.Left), Top=$($rect.Top), Right=$($rect.Right), Bottom=$($rect.Bottom)"

# 3. Click the Play/Pause button on the overlay
# The Play/Pause button is at roughly Left + 100, Bottom - 80
$clickX = $rect.Left + 100
$clickY = $rect.Bottom - 80

Write-Host "Clicking Overlay Play/Pause button at ($clickX, $clickY)..."
[Win32TestUtils]::ClickAt($clickX, $clickY)
Start-Sleep -Milliseconds 300

$fgAfterClick = [Win32TestUtils]::GetForegroundWindow()
Write-Host "Foreground after clicking Overlay button: $([Win32TestUtils]::GetTitle($fgAfterClick)) ($fgAfterClick)"

$didOverlayStealFocus = ($fgAfterClick -eq $overlayHwnd)
Write-Host "Did overlay become the foreground window? $didOverlayStealFocus"

# Clean up
Stop-Process -Id $overlay.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Name notepad -Force -ErrorAction SilentlyContinue
