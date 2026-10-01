Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap(16, 16)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::White)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Colors
$priColor = [System.Drawing.Color]::FromArgb(60, 70, 85)
$priPen = New-Object System.Drawing.Pen($priColor, 1.0)
$priPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$accBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0, 120, 212))

# 1. Swatch bar at bottom (x: 2.5 to 13.5, y: 12.5 to 14.5)
$g.FillRectangle($accBrush, 2.5, 12.5, 11.0, 2.2)

# 2. Wire handle (Arching above the top edge)
$handlePen = New-Object System.Drawing.Pen($priColor, 0.9)
$handlePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$handlePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawBezier($handlePen, 
    [System.Drawing.PointF]::new(5.0, 5.2),
    [System.Drawing.PointF]::new(6.8, 1.6),
    [System.Drawing.PointF]::new(9.2, 2.2),
    [System.Drawing.PointF]::new(9.0, 4.6)
)

# 3. Bucket Body (Diamond/tilted cylinder polygon)
# Left: (3.8, 6.8), Top: (6.8, 3.8), Right: (9.8, 6.2), Bottom: (6.8, 9.8)
# With a small flat base at the bottom: (6.0, 9.8) to (7.6, 9.6)
$pts = @(
    [System.Drawing.PointF]::new(3.8, 6.4),
    [System.Drawing.PointF]::new(6.6, 3.6),
    [System.Drawing.PointF]::new(9.6, 5.8),
    [System.Drawing.PointF]::new(7.6, 9.8),
    [System.Drawing.PointF]::new(5.8, 9.8)
)
$g.FillPolygon([System.Drawing.Brushes]::White, $pts)
$g.DrawPolygon($priPen, $pts)

# Optional: subtle rim arc on mouth to show open can
$rimPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 190, 200), 0.8)
$g.DrawLine($rimPen, [System.Drawing.PointF]::new(4.6, 6.0), [System.Drawing.PointF]::new(8.8, 5.6))

# 4. Paint pouring out from right corner (9.6, 5.8)
$streamPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(0, 120, 212), 1.3)
$streamPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$streamPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawBezier($streamPen, 
    [System.Drawing.PointF]::new(9.4, 5.8),
    [System.Drawing.PointF]::new(11.8, 5.6),
    [System.Drawing.PointF]::new(12.0, 7.6),
    [System.Drawing.PointF]::new(11.6, 9.8)
)
$g.FillEllipse($accBrush, 10.8, 10.8, 1.4, 1.4)

$g.Dispose()

# Upscale 16x
$big = New-Object System.Drawing.Bitmap(256, 256)
$gb = [System.Drawing.Graphics]::FromImage($big)
$gb.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gb.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$gb.DrawImage($bmp, [System.Drawing.Rectangle]::new(0, 0, 256, 256), [System.Drawing.Rectangle]::new(0, 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
$gb.Dispose()
$big.Save("scratch/bucket_exact_test3.png")
$big.Dispose()
$bmp.Dispose()
Write-Host "Generated scratch/bucket_exact_test3.png"
