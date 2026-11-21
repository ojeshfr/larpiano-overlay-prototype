$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$settingsDir = Join-Path $appData "LarpianoOverlayPrototype"
$settingsPath = Join-Path $settingsDir "settings.json"

Write-Host "Settings directory: $settingsDir"
Write-Host "Settings file exists: $(Test-Path $settingsPath)"

if (Test-Path $settingsPath) {
    Write-Host "`nCurrent settings.json content:"
    Get-Content $settingsPath
}

# Test 1: Write an invalid off-screen coordinate to settings.json
Write-Host "`n--- Testing Off-Screen Recovery ---"
$badSettings = @{
    Left = -99999
    Top = -99999
    Width = 920
    Height = 440
    IsCompact = $false
    IsPinned = $true
    Opacity = 0.94
    PlaybackSpeed = 1.0
    Volume = 0.8
} | ConvertTo-Json

Set-Content -Path $settingsPath -Value $badSettings
Write-Host "Saved corrupted off-screen settings."

# Launch the app
$proc = Start-Process "d:\projects\larpiano-overlay-prototype\publish\Larpiano.OverlayPrototype.exe" -PassThru
Start-Sleep -Seconds 2

# Check window position via Win32 GetWindowRect
Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WinBounds
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

$rect = New-Object WinBounds+RECT
[WinBounds]::GetWindowRect($proc.MainWindowHandle, [ref]$rect)
Write-Host "Window bounds after launching with off-screen config:"
Write-Host "Left: $($rect.Left), Top: $($rect.Top), Right: $($rect.Right), Bottom: $($rect.Bottom)"

$isRecoveredOnScreen = ($rect.Left -ge 0 -and $rect.Top -ge 0)
Write-Host "Off-screen recovery successful: $isRecoveredOnScreen"

Stop-Process -Id $proc.Id -Force
