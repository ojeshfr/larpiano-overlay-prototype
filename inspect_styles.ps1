Add-Type @"
using System;
using System.Runtime.InteropServices;

public class Win32Inspector
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;

    public static string InspectExStyle(IntPtr hWnd)
    {
        int style = GetWindowLong(hWnd, GWL_EXSTYLE);
        bool hasNoActivate = (style & WS_EX_NOACTIVATE) != 0;
        bool hasToolWindow = (style & WS_EX_TOOLWINDOW) != 0;
        bool hasTopmost = (style & WS_EX_TOPMOST) != 0;

        return string.Format(
            "Raw: 0x{0:X8}\nWS_EX_NOACTIVATE: {1}\nWS_EX_TOOLWINDOW: {2}\nWS_EX_TOPMOST: {3}",
            style, hasNoActivate, hasToolWindow, hasTopmost);
    }
}
"@

$proc = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2

$hwnd = $proc.MainWindowHandle
Write-Host "HWND: $hwnd"
Write-Host ([Win32Inspector]::InspectExStyle($hwnd))

Stop-Process -Id $proc.Id -Force
