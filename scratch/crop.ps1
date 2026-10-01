Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::FromFile("scratch/slides_wpf_home.png")
$crop = New-Object System.Drawing.Bitmap(650, 90)
$g = [System.Drawing.Graphics]::FromImage($crop)
$g.DrawImage($src, [System.Drawing.Rectangle]::new(0, 0, 650, 90), [System.Drawing.Rectangle]::new(640, 60, 650, 90), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$crop.Save("scratch/crop_home_p_d.png")
$crop.Dispose()
$src.Dispose()
Write-Host "Done"
