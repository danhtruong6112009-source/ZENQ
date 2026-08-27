$ppt = New-Object -ComObject PowerPoint.Application
try {
    $pres = $ppt.Presentations.Open("C:\Users\Danh\Desktop\genofficepy\scratch\scratch\test_inject.pptx")
    Write-Output ("INJECT SLIDE COUNT: " + $pres.Slides.Count)
    $pres.Close()
} catch {
    Write-Output ("OPEN EXCEPTION: " + $_.Exception.Message)
} finally {
    $ppt.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
}
