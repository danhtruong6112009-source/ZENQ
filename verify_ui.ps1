Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$asm = [System.Reflection.Assembly]::LoadFrom((Resolve-Path "GenOffice.exe"))

# 1. Test Tab 4 (Settings Dashboard) Rendering - Category 0
$homeView = New-Object GenOffice.UI.HomeView
$homeView.ActiveTab = 4 # Cài đặt
$homeView.SettingsCategory = 0

$bmpHome = New-Object System.Drawing.Bitmap 1366, 800
$gHome = [System.Drawing.Graphics]::FromImage($bmpHome)
$homeView.Draw($gHome, 1366, 800)
$gHome.Dispose()
$bmpHome.Save("verify_settings_dashboard.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpHome.Dispose()
Write-Host "Saved verify_settings_dashboard.png (Category 0)"

# 1.1 Test Tab 4 - Category 1 (Hệ thống & Soạn thảo with Windows 11 Toggle switches)
$homeView.SettingsCategory = 1
$bmpHome1 = New-Object System.Drawing.Bitmap 1366, 800
$gHome1 = [System.Drawing.Graphics]::FromImage($bmpHome1)
$homeView.Draw($gHome1, 1366, 800)
$gHome1.Dispose()
$bmpHome1.Save("verify_settings_system.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpHome1.Dispose()
Write-Host "Saved verify_settings_system.png (Category 1)"

# 2. Test RibbonBar Island Mode Rendering
[GenOffice.Core.AppSettings]::Instance.RibbonStyle = [GenOffice.Core.RibbonStyle]::Island
$ribbon = New-Object GenOffice.UI.RibbonBar
$ribbon.CurrentMode = [GenOffice.UI.OfficeAppMode]::Writer
$writerView = New-Object GenOffice.UI.WriterView

$bmpIsland = New-Object System.Drawing.Bitmap 1366, 160
$gIsland = [System.Drawing.Graphics]::FromImage($bmpIsland)
$ribbon.Draw($gIsland, 1366, 160, $writerView)
$gIsland.Dispose()
$bmpIsland.Save("verify_ribbon_island.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpIsland.Dispose()
Write-Host "Saved verify_ribbon_island.png"

# 3. Test RibbonBar Standard Mode Rendering
[GenOffice.Core.AppSettings]::Instance.RibbonStyle = [GenOffice.Core.RibbonStyle]::Standard
$bmpStd = New-Object System.Drawing.Bitmap 1366, 160
$gStd = [System.Drawing.Graphics]::FromImage($bmpStd)
$ribbon.Draw($gStd, 1366, 160, $writerView)
$gStd.Dispose()
$bmpStd.Save("verify_ribbon_standard.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpStd.Dispose()
Write-Host "Saved verify_ribbon_standard.png"

# 4. Test Settings Clicks
[GenOffice.Core.AppSettings]::Instance.RibbonStyle = [GenOffice.Core.RibbonStyle]::Standard
Write-Host "Initial RibbonStyle:" [GenOffice.Core.AppSettings]::Instance.RibbonStyle

# Simulate clicking "Dạng Đảo" card in Settings
$docTitle = ""
$homeView.HandleClick([int]($homeView.SettingRibbonIslandRect.X + 10), [int]($homeView.SettingRibbonIslandRect.Y + 10), 1366, 800, [ref]$docTitle)
Write-Host "After clicking Island card in Settings:" [GenOffice.Core.AppSettings]::Instance.RibbonStyle

# Simulate clicking toggle button in Ribbon
$ribbon.HandleClick([int]($ribbon.RibbonToggleStyleBounds.X + 5), [int]($ribbon.RibbonToggleStyleBounds.Y + 5), $writerView)
Write-Host "After clicking Ribbon Toggle button:" [GenOffice.Core.AppSettings]::Instance.RibbonStyle

# Simulate clicking Category 1 (Hệ thống & Soạn thảo) in sidebar
$homeView.HandleClick([int]($homeView.SettingCategoryRects[1].X + 15), [int]($homeView.SettingCategoryRects[1].Y + 15), 1366, 800, [ref]$docTitle)
Write-Host "Active Category after clicking Sidebar Category 1:" $homeView.SettingsCategory

# Redraw to update bounds for Category 1
$bmpDummy = New-Object System.Drawing.Bitmap 1366, 800
$gDummy = [System.Drawing.Graphics]::FromImage($bmpDummy)
$homeView.Draw($gDummy, 1366, 800)
$gDummy.Dispose()
$bmpDummy.Dispose()

# Simulate clicking AutoSave toggle
$prevAuto = [GenOffice.Core.AppSettings]::Instance.AutoSaveEnabled
Write-Host "AutoSave before toggle click:" $prevAuto
$homeView.HandleClick([int]($homeView.SettingAutoSaveToggleRect.X + 5), [int]($homeView.SettingAutoSaveToggleRect.Y + 5), 1366, 800, [ref]$docTitle)
Write-Host "AutoSave after toggle click:" [GenOffice.Core.AppSettings]::Instance.AutoSaveEnabled
