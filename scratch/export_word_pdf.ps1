$docxPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoTongKet_2026.docx"
$pdfPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\word_export.pdf"

try {
    if (Test-Path $pdfPath) { Remove-Item $pdfPath -Force }

    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0

    $doc = $word.Documents.Open($docxPath, $false, $true) # Open ReadOnly
    Start-Sleep -Seconds 1

    # 17 = wdExportFormatPDF
    $doc.ExportAsFixedFormat($pdfPath, 17, $false, 0, 0, 1, 1, 0, $true, $true, 0, $true, $true, $false)
    Start-Sleep -Seconds 1

    $doc.Close($false)
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null

    if (Test-Path $pdfPath) {
        Write-Output "WORD_PDF_EXPORT_SUCCESS: $pdfPath"
    } else {
        Write-Output "WORD_PDF_EXPORT_FAILED"
    }
} catch {
    Write-Output "ERROR: $_"
}
