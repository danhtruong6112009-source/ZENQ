[System.Reflection.Assembly]::LoadFrom("c:\Users\Danh\Desktop\genofficepy\GenOffice.exe") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null

$testResults = @{}

# =========================================================================
# TEST 1: Seamless Ribbon Seam Across Multi-DPI (100%, 125%, 150%)
# Both Document Tab and Home Tab, in Normal (1000px) and Maximized (1920px)
# =========================================================================
function Test-RibbonSeam($scaleName, $scaleFactor, $activeTabIdx, $windowWidth) {
    $ribbon = New-Object GenOffice.UI.RibbonBar
    $ribbon.AddDocTab("Doc 1.docx", [GenOffice.UI.OfficeAppMode]::Writer)
    $ribbon.ActiveDocIndex = $activeTabIdx

    $w = $windowWidth
    $h = 300
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $dpi = 96.0 * $scaleFactor
    $bmp.SetResolution($dpi, $dpi)
    $g = [System.Drawing.Graphics]::FromImage($bmp)

    $ribbon.Draw($g, $w, $h)

    $seamY = [int][Math]::Round(36.0 * $scaleFactor)
    $activeTab = $ribbon.DocTabBounds[$activeTabIdx]
    $sampleX = $activeTab.X + [int]($activeTab.Width / 2)

    $c = $bmp.GetPixel($sampleX, $seamY)
    $isSeamless = ($c.R -ge 248 -and $c.G -ge 250 -and $c.B -ge 252)

    $g.Dispose()
    $bmp.Dispose()

    return $isSeamless
}

Write-Host "--- Running Seam Tests ---"
$testResults["Seam_Normal_100_Doc"] = Test-RibbonSeam "100%" 1.0 1 1000
$testResults["Seam_Normal_125_Doc"] = Test-RibbonSeam "125%" 1.25 1 1000
$testResults["Seam_Normal_150_Doc"] = Test-RibbonSeam "150%" 1.50 1 1000
$testResults["Seam_Maximized_100_Doc"] = Test-RibbonSeam "100%" 1.0 1 1920
$testResults["Seam_Maximized_125_Doc"] = Test-RibbonSeam "125%" 1.25 1 1920
$testResults["Seam_Maximized_150_Doc"] = Test-RibbonSeam "150%" 1.50 1 1920

$testResults["Seam_Normal_100_Home"] = Test-RibbonSeam "100%" 1.0 0 1000
$testResults["Seam_Normal_125_Home"] = Test-RibbonSeam "125%" 1.25 0 1000
$testResults["Seam_Normal_150_Home"] = Test-RibbonSeam "150%" 1.50 0 1000
$testResults["Seam_Maximized_100_Home"] = Test-RibbonSeam "100%" 1.0 0 1920
$testResults["Seam_Maximized_125_Home"] = Test-RibbonSeam "125%" 1.25 0 1920
$testResults["Seam_Maximized_150_Home"] = Test-RibbonSeam "150%" 1.50 0 1920

# =========================================================================
# TEST 2: '+' Button wiring to NewTabRadialLauncher (OnMouseDown & HandleClick)
# =========================================================================
Write-Host "--- Running '+' Button Wiring Tests ---"

# Close launcher first to start clean
if ([GenOffice.NewTabRadialLauncher]::IsOpen) {
    [GenOffice.NewTabRadialLauncher]::Close()
}

$ribbon = New-Object GenOffice.UI.RibbonBar
$ribbon.AddDocTab("Doc 1.docx", [GenOffice.UI.OfficeAppMode]::Writer)

$bmp = New-Object System.Drawing.Bitmap 1000, 300
$g = [System.Drawing.Graphics]::FromImage($bmp)
$ribbon.Draw($g, 1000, 300)
$g.Dispose()
$bmp.Dispose()

$plusBounds = $ribbon.DocTabPlusBounds
Write-Host "DocTabPlusBounds: X=$($plusBounds.X), Y=$($plusBounds.Y), W=$($plusBounds.Width), H=$($plusBounds.Height)"
$expectedCenter = New-Object System.Drawing.Point ($plusBounds.X + [int]($plusBounds.Width / 2)), ($plusBounds.Y + [int]($plusBounds.Height / 2))
Write-Host "Expected Plus Center: X=$($expectedCenter.X), Y=$($expectedCenter.Y)"

# Test form with appropriate width
$form = New-Object System.Windows.Forms.Form
$form.ClientSize = New-Object System.Drawing.Size 1000, 700
$ribbon.Parent = $form

# Initially closed
$testResults["Launcher_Initially_Closed"] = (-not [GenOffice.NewTabRadialLauncher]::IsOpen)

# 1. Click plus via Ribbon.OnMouseDown(Point, parentForm)
$clicked = $ribbon.OnMouseDown($expectedCenter, $form)
$testResults["OnMouseDown_Handled"] = $clicked
$testResults["Launcher_Open_After_MouseDown"] = [GenOffice.NewTabRadialLauncher]::IsOpen
$actualCenter = [GenOffice.NewTabRadialLauncher]::Center
$testResults["Launcher_Center_Matches_Plus"] = ($actualCenter.X -eq $expectedCenter.X -and $actualCenter.Y -eq $expectedCenter.Y)

# 2. Click plus again via OnMouseDown to toggle closed
$clicked2 = $ribbon.OnMouseDown($expectedCenter, $form)
$testResults["OnMouseDown_Toggle_Handled"] = $clicked2
$state = [GenOffice.NewTabRadialLauncher]::State
$testResults["Launcher_Closing_Or_Closed"] = ($state -eq [GenOffice.LauncherState]::Closing -or $state -eq [GenOffice.LauncherState]::Closed)

# Reset state to Closed for HandleClick test
$field = [GenOffice.NewTabRadialLauncher].GetField("_state", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static)
$field.SetValue($null, [GenOffice.LauncherState]::Closed)

# 3. Click plus via Ribbon.HandleClick(x, y, null, parentForm)
$clickHandled = $ribbon.HandleClick($expectedCenter.X, $expectedCenter.Y, $null, $form)
$testResults["HandleClick_Handled"] = $clickHandled
$testResults["Launcher_Open_After_HandleClick"] = [GenOffice.NewTabRadialLauncher]::IsOpen

# 4. Click plus again via Ribbon.HandleClick to toggle closed
$clickHandled2 = $ribbon.HandleClick($expectedCenter.X, $expectedCenter.Y, $null, $form)
$testResults["HandleClick_Toggle_Handled"] = $clickHandled2
$state2 = [GenOffice.NewTabRadialLauncher]::State
$testResults["Launcher_Closing_Or_Closed_After_HandleClick"] = ($state2 -eq [GenOffice.LauncherState]::Closing -or $state2 -eq [GenOffice.LauncherState]::Closed)

# Clean up
$form.Dispose()

# =========================================================================
# Output Summary
# =========================================================================
Write-Host "`n================================================="
Write-Host "TEST EXECUTION SUMMARY:"
Write-Host "================================================="
$allPassed = $true
foreach ($key in $testResults.Keys) {
    $res = $testResults[$key]
    if (-not $res) { $allPassed = $false }
    $resStr = if ($res) { "PASS" } else { "FAIL" }
    Write-Host ("{0,-45} : {1}" -f $key, $resStr)
}
Write-Host "================================================="
Write-Host "FINAL OUTCOME: $(if ($allPassed) { 'ALL TESTS PASSED' } else { 'SOME TESTS FAILED' })"
Write-Host "================================================="
