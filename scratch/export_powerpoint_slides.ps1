$pptxPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoThuyetTrinh_2026.pptx"
$outDir = "C:\Users\Danh\Desktop\genofficepy\scratch\"

try {
    Stop-Process -Name POWERPNT -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500

    $ppt = New-Object -ComObject PowerPoint.Application
    $ppt.WindowState = 2 # ppWindowMinimized

    # Open with Window: ReadOnly=True, Untitled=False, WithWindow=True (msoTrue = -1)
    $pres = $ppt.Presentations.Open($pptxPath, -1, 0, -1)
    Start-Sleep -Seconds 1

    for ($i = 1; $i -le $pres.Slides.Count; $i++) {
        $slide = $pres.Slides.Item($i)
        $imgPath = Join-Path $outDir ("powerpoint_slide" + $i + ".png")
        if (Test-Path $imgPath) { Remove-Item $imgPath -Force }
        $slide.Export($imgPath, "PNG", 1280, 720)
        Write-Output ("POWERPOINT_SLIDE_EXPORTED: " + $imgPath)
    }

    $pres.Close()
    $ppt.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
    Write-Output "POWERPOINT_EXPORT_FINISHED_SUCCESSFULLY"
} catch {
    Write-Output "ERROR: $_"
}
