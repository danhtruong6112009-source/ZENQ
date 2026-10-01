[System.Reflection.Assembly]::LoadFrom('c:\Users\Danh\Desktop\genofficepy\GenOffice.exe') | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName('System.Drawing') | Out-Null

function Test-SeamForScale([float]$scale, [string]$label) {
    Write-Host "`n==============================================="
    Write-Host "TESTING $label (Scale = $scale)"
    Write-Host "==============================================="

    $ribbon = New-Object GenOffice.UI.RibbonBar
    $ribbon.AddDocTab('Doc 1.docx', [GenOffice.UI.OfficeAppMode]::Writer)

    $w = [int](1000 * $scale)
    $h = [int](200 * $scale)
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $bmp.SetResolution(96.0 * $scale, 96.0 * $scale)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    
    # Draw NATIVELY with NO simulation or extra calls
    $ribbon.Draw($g, $w, $h)

    $activeTabRect = $ribbon.DocTabBounds[$ribbon.ActiveDocIndex]
    Write-Host "Active Tab Rect: X=$($activeTabRect.X), Y=$($activeTabRect.Y), W=$($activeTabRect.Width), H=$($activeTabRect.Height), Bottom=$($activeTabRect.Bottom)"
    
    $seamY = [int][Math]::Round(36.0 * $scale)
    $sampleX = $activeTabRect.X + [int]($activeTabRect.Width / 2)
    Write-Host "Sampling at X = $sampleX across row $($seamY - 3) to $($seamY + 3):"
    
    $hasSeamDefect = $false
    for ($y = $seamY - 3; $y -le $seamY + 3; $y++) {
        $c = $bmp.GetPixel($sampleX, $y)
        Write-Host ("  Y={0:D2}: R={1:D3} G={2:D3} B={3:D3}" -f $y, $c.R, $c.G, $c.B)
        # Any dark grey line (R < 240 or G < 245) indicates a seam defect
        if ($c.R -lt 240 -or $c.G -lt 245) {
            $hasSeamDefect = $true
        }
    }
    
    if (-not $hasSeamDefect) {
        Write-Host "PASS: Seamless porcelain transition verified (No grey seam line)!" -ForegroundColor Green
    } else {
        Write-Host "FAIL: Detected grey seam artifact!" -ForegroundColor Red
    }
    
    $g.Dispose()
    $bmp.Dispose()
}

Test-SeamForScale 1.0 "100% DPI"
Test-SeamForScale 1.25 "125% DPI"
Test-SeamForScale 1.50 "150% DPI"

# Also test Active Home Tab
Write-Host "`n==============================================="
Write-Host "TESTING ACTIVE HOME TAB (100% DPI)"
Write-Host "==============================================="
$ribbonHome = New-Object GenOffice.UI.RibbonBar
$w = 1000
$h = 200
$bmpHome = New-Object System.Drawing.Bitmap $w, $h
$gHome = [System.Drawing.Graphics]::FromImage($bmpHome)
$ribbonHome.Draw($gHome, $w, $h)

$homeRect = $ribbonHome.DocTabBounds[0]
Write-Host "Home Tab Rect: X=$($homeRect.X), Y=$($homeRect.Y), W=$($homeRect.Width), H=$($homeRect.Height), Bottom=$($homeRect.Bottom)"
$sampleXHome = $homeRect.X + [int]($homeRect.Width / 2)
$hasSeamDefectHome = $false
for ($y = 33; $y -le 39; $y++) {
    $c = $bmpHome.GetPixel($sampleXHome, $y)
    Write-Host ("  Y={0:D2}: R={1:D3} G={2:D3} B={3:D3}" -f $y, $c.R, $c.G, $c.B)
    if ($c.R -lt 240 -or $c.G -lt 245) {
        $hasSeamDefectHome = $true
    }
}
if (-not $hasSeamDefectHome) {
    Write-Host "PASS: Home Tab seamless porcelain transition verified!" -ForegroundColor Green
} else {
    Write-Host "FAIL: Home Tab detected grey seam artifact!" -ForegroundColor Red
}
$gHome.Dispose()
$bmpHome.Dispose()
