Add-Type -AssemblyName System.Drawing

$src = [System.Drawing.Bitmap]::FromFile("scratch/slides_wpf_home.png")

# Crop exactly the 16x16 icon at (684, 72)
$numIcon = New-Object System.Drawing.Bitmap(16, 16)
$g = [System.Drawing.Graphics]::FromImage($numIcon)
$g.DrawImage($src, [System.Drawing.Rectangle]::new(0, 0, 16, 16), [System.Drawing.Rectangle]::new(684, 72, 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()

# Upscale 16x
$big = New-Object System.Drawing.Bitmap(256, 256)
$gb = [System.Drawing.Graphics]::FromImage($big)
$gb.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gb.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$gb.DrawImage($numIcon, [System.Drawing.Rectangle]::new(0, 0, 256, 256), [System.Drawing.Rectangle]::new(0, 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
$gb.Dispose()

$big.Save("scratch/numbering_16x_inspect.png")
$big.Dispose()
$numIcon.Dispose()
$src.Dispose()
Write-Host "Exported numbering_16x_inspect.png"
