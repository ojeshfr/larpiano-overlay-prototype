Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Helper
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
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, 256);
        return sb.ToString();
    }

    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(60);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }
}
"@

# Create a mock target background window (e.g. representing Roblox Game Window)
$targetForm = New-Object System.Windows.Forms.Form
$targetForm.Text = "Target Game Window (Roblox Mock)"
$targetForm.Width = 800
$targetForm.Height = 600
$targetForm.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
$targetForm.Location = New-Object System.Drawing.Point(100, 100)
$targetForm.Show()

[System.Windows.Forms.Application]::DoEvents()
Start-Sleep -Milliseconds 500

$targetHwnd = $targetForm.Handle
[Win32Helper]::SetForegroundWindow($targetHwnd)
[System.Windows.Forms.Application]::DoEvents()
Start-Sleep -Milliseconds 500

$initialFg = [Win32Helper]::GetForegroundWindow()
Write-Host "Target Game Form Created. Handle: $targetHwnd"
Write-Host "Current Foreground: $([Win32Helper]::GetTitle($initialFg)) ($initialFg)"

# Launch Larpiano Overlay
$overlayProc = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2
[System.Windows.Forms.Application]::DoEvents()

$fgAfterOverlay = [Win32Helper]::GetForegroundWindow()
Write-Host "Foreground after overlay startup: $([Win32Helper]::GetTitle($fgAfterOverlay)) ($fgAfterOverlay)"
$launchPreserved = ($fgAfterOverlay -eq $targetHwnd)
Write-Host "Launch preserved target focus: $launchPreserved"

# Locate overlay window
$overlayHwnd = $overlayProc.MainWindowHandle
$rect = New-Object Win32Helper+RECT
[Win32Helper]::GetWindowRect($overlayHwnd, [ref]$rect)
Write-Host "Overlay Rect: Left=$($rect.Left), Top=$($rect.Top), Right=$($rect.Right), Bottom=$($rect.Bottom)"

# Click Play/Pause on Overlay (roughly Left + 85, Bottom - 110)
$clickX = $rect.Left + 85
$clickY = $rect.Bottom - 110

Write-Host "Clicking Play/Pause on Overlay at ($clickX, $clickY)..."
[Win32Helper]::Click($clickX, $clickY)
[System.Windows.Forms.Application]::DoEvents()
Start-Sleep -Milliseconds 500

$fgAfterClick = [Win32Helper]::GetForegroundWindow()
Write-Host "Foreground after click: $([Win32Helper]::GetTitle($fgAfterClick)) ($fgAfterClick)"
$clickPreserved = ($fgAfterClick -eq $targetHwnd)
Write-Host "Click preserved target focus: $clickPreserved"

# Clean up
$targetForm.Close()
Stop-Process -Id $overlayProc.Id -Force -ErrorAction SilentlyContinue

Write-Host "`n=== FINAL TEST REPORT ==="
Write-Host "Target HWND: $targetHwnd"
Write-Host "Overlay Launched without stealing focus: $launchPreserved"
Write-Host "Overlay Clicked without stealing focus:  $clickPreserved"
