[System.Reflection.Assembly]::LoadFrom("c:\Users\Danh\Desktop\genofficepy\GenOffice.exe") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

function Test-Scale-And-Tab($scaleName, $scaleFactor, $activeTabIdx, $tabName) {
    Write-Host "========================================="
    Write-Host "Testing scale: $scaleName ($scaleFactor) - Active: $tabName (Index $activeTabIdx)"
    Write-Host "========================================="

    $ribbon = New-Object GenOffice.UI.RibbonBar
    $ribbon.AddDocTab("Doc 1.docx", [GenOffice.UI.OfficeAppMode]::Writer)
    $ribbon.ActiveDocIndex = $activeTabIdx

    $w = 1000
    $h = 300
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    
    $dpi = 96.0 * $scaleFactor
    $bmp.SetResolution($dpi, $dpi)
    $g = [System.Drawing.Graphics]::FromImage($bmp)

    $ribbon.Draw($g, $w, $h)

    $seamY = [int][Math]::Round(36.0 * $scaleFactor)
    Write-Host "Calculated Seam Y: $seamY"
    
    $activeTab = $ribbon.DocTabBounds[$activeTabIdx]
    Write-Host "Active Tab Bounds: X=$($activeTab.X), Y=$($activeTab.Y), W=$($activeTab.Width), H=$($activeTab.Height), Bottom=$($activeTab.Bottom)"

    $sampleX = $activeTab.X + [int]($activeTab.Width / 2)
    Write-Host "Sampling at X = $sampleX (middle of active tab):"

    $allPassed = $true
    for ($y = ($seamY - 3); $y -le ($seamY + 3); $y++) {
        $c = $bmp.GetPixel($sampleX, $y)
        $flag = ""
        if ($y -eq $seamY) {
            if ($c.R -ge 248 -and $c.G -ge 250 -and $c.B -ge 252) {
                $flag = " <--- SEAM ROW (SEAMLESS PASS)"
            } else {
                $flag = " <--- SEAM ROW (FAIL - VISIBLE SEAM GAP)"
                $allPassed = $false
            }
        }
        Write-Host ("Y={0:D2}: R={1:D3} G={2:D3} B={3:D3}{4}" -f $y, $c.R, $c.G, $c.B, $flag)
    }

    $g.Dispose()
    $bmp.Dispose()
    return $allPassed
}

$results = @()

# Test Document Tab (Index 1) at 100%, 125%, 150%
$results += Test-Scale-And-Tab "100% DPI" 1.0 1 "DocTab"
$results += Test-Scale-And-Tab "125% DPI" 1.25 1 "DocTab"
$results += Test-Scale-And-Tab "150% DPI" 1.50 1 "DocTab"

# Test Home Tab (Index 0) at 100%, 125%, 150%
$results += Test-Scale-And-Tab "100% DPI" 1.0 0 "HomeTab"
$results += Test-Scale-And-Tab "125% DPI" 1.25 0 "HomeTab"
$results += Test-Scale-And-Tab "150% DPI" 1.50 0 "HomeTab"

Write-Host "========================================="
Write-Host "SUMMARY RESULTS: $([string]::Join(', ', $results))"
$allOk = (-not ($results -contains $false))
Write-Host "ALL TESTS PASSED: $allOk"
Write-Host "========================================="
