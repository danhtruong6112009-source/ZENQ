[System.Reflection.Assembly]::LoadFrom('c:\Users\Danh\Desktop\genofficepy\GenOffice.exe') | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName('System.Drawing') | Out-Null

$ribbon = New-Object GenOffice.UI.RibbonBar
$ribbon.AddDocTab('Doc 1.docx', [GenOffice.UI.OfficeAppMode]::Writer)

$w = 1000
$h = 200
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$ribbon.Draw($g, $w, $h)

# Now simulate the proposed fix:
# 1. Fill the seam at Y=36 across the active tab width with the seamless surface color (248, 250, 252)
$activeTabRect = $ribbon.DocTabBounds[$ribbon.ActiveDocIndex]
$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(248, 250, 252))
$g.FillRectangle($brush, $activeTabRect.X + 1, 35, $activeTabRect.Width - 2, 2)

Write-Host "AFTER SIMULATED SEAM BRIDGE AT X = 320:"
for ($y = 33; $y -le 39; $y++) {
    $c = $bmp.GetPixel(320, $y)
    Write-Host ("Y={0:D2}: R={1:D3} G={2:D3} B={3:D3}" -f $y, $c.R, $c.G, $c.B)
}
