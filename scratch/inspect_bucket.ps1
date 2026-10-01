Add-Type -AssemblyName System.Drawing

$src = [System.Drawing.Bitmap]::FromFile("scratch/slides_wpf_home.png")

# Crop exactly the 16x16 icon at (1160, 70)
$bucketIcon = New-Object System.Drawing.Bitmap(16, 16)
$g = [System.Drawing.Graphics]::FromImage($bucketIcon)
$g.DrawImage($src, [System.Drawing.Rectangle]::new(0, 0, 16, 16), [System.Drawing.Rectangle]::new(1160, 70, 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()

# Upscale 16x
$big = New-Object System.Drawing.Bitmap(256, 256)
$gb = [System.Drawing.Graphics]::FromImage($big)
$gb.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gb.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$gb.DrawImage($bucketIcon, [System.Drawing.Rectangle]::new(0, 0, 256, 256), [System.Drawing.Rectangle]::new(0, 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
$gb.Dispose()

$big.Save("scratch/bucket_16x_inspect.png")
$big.Dispose()
$bucketIcon.Dispose()
$src.Dispose()
Write-Host "Exported bucket_16x_inspect.png"
