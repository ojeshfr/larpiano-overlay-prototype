Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class FGChecker
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public static string Current()
    {
        IntPtr h = GetForegroundWindow();
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(h, sb, 256);
        return h.ToString() + ": " + sb.ToString();
    }
}
"@

Write-Host "Current Foreground: $([FGChecker]::Current())"
