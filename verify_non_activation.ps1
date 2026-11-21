Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Test
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public static string GetWindowTitle(IntPtr hWnd)
    {
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, 256);
        return sb.ToString();
    }

    public static void ClickAt(int x, int y)
    {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }
}
"@

# 1. Start a foreground target window (Notepad)
Write-Host "--- Step 1: Starting Target Foreground Window (Notepad) ---"
$notepad = Start-Process notepad.exe -PassThru
Start-Sleep -Seconds 1
$notepadHwnd = $notepad.MainWindowHandle
[Win32Test]::SetForegroundWindow($notepadHwnd)
Start-Sleep -Milliseconds 500

$initialFg = [Win32Test]::GetForegroundWindow()
$initialFgTitle = [Win32Test]::GetWindowTitle($initialFg)
Write-Host "Target foreground window: $initialFgTitle (Handle: $initialFg)"

# 2. Launch the Larpiano Overlay Prototype
Write-Host "`n--- Step 2: Launching Larpiano Overlay Prototype ---"
$overlayProc = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2

$overlayHwnd = $overlayProc.MainWindowHandle
Write-Host "Overlay window handle: $overlayHwnd"

$fgAfterLaunch = [Win32Test]::GetForegroundWindow()
$fgAfterLaunchTitle = [Win32Test]::GetWindowTitle($fgAfterLaunch)
Write-Host "Foreground window after overlay launch: $fgAfterLaunchTitle (Handle: $fgAfterLaunch)"

$launchDidNotActivate = ($fgAfterLaunch -eq $notepadHwnd -or $fgAfterLaunchTitle -like "*Notepad*")
Write-Host "Launch non-activation check: $launchDidNotActivate"

# 3. Test clicking on the Play/Pause button on the overlay
# Let's find the overlay position
# By default, overlay left = 200, top = 100.
# The Play/Pause button is approximately at Left + 100, Top + 350.
Write-Host "`n--- Step 3: Clicking Play/Pause on Overlay without activating ---"
[Win32Test]::SetForegroundWindow($notepadHwnd)
Start-Sleep -Milliseconds 300

# Perform click at overlay play/pause button area
# In default layout: WindowLeft ~ 200-400, WindowTop ~ 100-200.
# Let's get window rect using Win32 if possible or click within the window
Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WinRect
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

$rect = New-Object WinRect+RECT
[WinRect]::GetWindowRect($overlayHwnd, [ref]$rect)
Write-Host "Overlay Rect: Left=$($rect.Left), Top=$($rect.Top), Right=$($rect.Right), Bottom=$($rect.Bottom)"

# Play/Pause button is roughly Left + 85, Bottom - 110
$clickX = $rect.Left + 85
$clickY = $rect.Bottom - 110

Write-Host "Simulating real mouse click at Play/Pause ($clickX, $clickY)..."
[Win32Test]::ClickAt($clickX, $clickY)
Start-Sleep -Milliseconds 300

$fgAfterClick = [Win32Test]::GetForegroundWindow()
$fgAfterClickTitle = [Win32Test]::GetWindowTitle($fgAfterClick)
Write-Host "Foreground window AFTER clicking Play/Pause: $fgAfterClickTitle (Handle: $fgAfterClick)"

$clickDidNotActivate = ($fgAfterClick -eq $notepadHwnd -or $fgAfterClickTitle -like "*Notepad*")
Write-Host "Click non-activation preserved foreground target: $clickDidNotActivate"

# Clean up processes
Start-Sleep -Seconds 1
Stop-Process -Id $overlayProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $notepad.Id -Force -ErrorAction SilentlyContinue

Write-Host "`n--- Verification Result ---"
Write-Host "Launch Non-Activating: $launchDidNotActivate"
Write-Host "Click Non-Activating:  $clickDidNotActivate"
