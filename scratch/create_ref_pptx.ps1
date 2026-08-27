$ppt = New-Object -ComObject PowerPoint.Application
try {
    $pres = $ppt.Presentations.Add([Microsoft.Office.Core.MsoTriState]::msoTrue)
    $slide = $pres.Slides.Add(1, 12) # 12 = ppLayoutBlank
    $pres.SaveAs("C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx")
    $pres.Close()
    Write-Output "SAVED_VALID_POWERPOINT"
} catch {
    Write-Output ("EXCEPTION: " + $_.Exception.Message)
} finally {
    $ppt.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
}
