Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::FromFile("scratch/crop_home_p_d.png")

# Zoom Numbering (rect: x=38, y=7, w=30, h=22)
$nb = New-Object System.Drawing.Bitmap(150, 110)
$g = [System.Drawing.Graphics]::FromImage($nb)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.DrawImage($src, [System.Drawing.Rectangle]::new(0, 0, 150, 110), [System.Drawing.Rectangle]::new(38, 7, 30, 22), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$nb.Save("scratch/zoom_numbering_4x.png")
$nb.Dispose()

# Zoom Paint Bucket (rect: x=515, y=7, w=30, h=22)
$pb = New-Object System.Drawing.Bitmap(150, 110)
$g2 = [System.Drawing.Graphics]::FromImage($pb)
$g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g2.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g2.DrawImage($src, [System.Drawing.Rectangle]::new(0, 0, 150, 110), [System.Drawing.Rectangle]::new(515, 7, 30, 22), [System.Drawing.GraphicsUnit]::Pixel)
$g2.Dispose()
$pb.Save("scratch/zoom_bucket_4x.png")
$pb.Dispose()

$src.Dispose()
Write-Host "Zoomed"
