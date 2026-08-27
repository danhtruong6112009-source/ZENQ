Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$docxPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoTongKet_2026.docx"
$imgPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\word_screenshot.png"

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $true
    $word.DisplayAlerts = 0
    $word.WindowState = 1

    $doc = $word.Documents.Open($docxPath)
    Start-Sleep -Seconds 2

    $word.ActiveWindow.View.Type = 3 # wdPrintView
    $word.ActiveWindow.ActivePane.View.Zoom.Percentage = 100
    Start-Sleep -Seconds 1

    # Bring window to front
    $wshell = New-Object -ComObject WScript.Shell
    $wshell.AppActivate("Word")
    Start-Sleep -Milliseconds 500

    # Capture the screen
    $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(0, 0, 0, 0, $bounds.Size)
    $g.Dispose()

    # Crop out the centered page area (X: center-500..center+500, Y: 120..1000)
    $cropW = [Math]::Min(1100, $bounds.Width)
    $cropH = [Math]::Min(950, $bounds.Height - 140)
    $cropX = [Math]::Max(0, [int](($bounds.Width - $cropW) / 2))
    $cropY = 140

    $cropBmp = New-Object System.Drawing.Bitmap($cropW, $cropH)
    $cg = [System.Drawing.Graphics]::FromImage($cropBmp)
    $srcRect = New-Object System.Drawing.Rectangle($cropX, $cropY, $cropW, $cropH)
    $destRect = New-Object System.Drawing.Rectangle(0, 0, $cropW, $cropH)
    $cg.DrawImage($bmp, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $cg.Dispose()
    $bmp.Dispose()

    $cropBmp.Save($imgPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $cropBmp.Dispose()
    Write-Output "WORD_SCREENSHOT_SAVED: $imgPath"

    $doc.Close($false)
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
} catch {
    Write-Output "ERROR: $_"
}
