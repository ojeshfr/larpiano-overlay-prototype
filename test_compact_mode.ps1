$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$settingsPath = Join-Path $appData "LarpianoOverlayPrototype\settings.json"

$compactSettings = @{
    Left = 300
    Top = 150
    Width = 760
    Height = 98
    IsCompact = $true
    IsPinned = $true
    Opacity = 0.94
    PlaybackSpeed = 1.25
    Volume = 0.85
} | ConvertTo-Json

Set-Content -Path $settingsPath -Value $compactSettings
Write-Host "Saved compact settings."

$proc = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2

Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WinBounds2
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

$rect = New-Object WinBounds2+RECT
[WinBounds2]::GetWindowRect($proc.MainWindowHandle, [ref]$rect)
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
Write-Host "Compact Window Dimensions: Width=$w, Height=$h"

$isCompactSize = ($w -ge 700 -and $w -le 800 -and $h -ge 80 -and $h -le 120)
Write-Host "Compact Mode Dimension Verified: $isCompactSize"

Stop-Process -Id $proc.Id -Force
