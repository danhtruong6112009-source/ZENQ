Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$xlsxPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoDoanhThu_2026.xlsx"
$imgPath = "C:\Users\Danh\Desktop\genofficepy\scratch\scratch\excel_screenshot.png"

try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $true
    $excel.DisplayAlerts = $false
    $excel.WindowState = -4137 # xlMaximized

    $wb = $excel.Workbooks.Open($xlsxPath)
    Start-Sleep -Seconds 2

    $ws = $wb.Sheets.Item(1)
    $range = $ws.Range("A1:G12")
    $range.CopyPicture(1, 2) # xlScreen, xlBitmap
    Start-Sleep -Milliseconds 500

    if ([System.Windows.Forms.Clipboard]::ContainsImage()) {
        $img = [System.Windows.Forms.Clipboard]::GetImage()
        $img.Save($imgPath, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Output "EXCEL_SCREENSHOT_SAVED: $imgPath"
    } else {
        Write-Output "CLIPBOARD_NO_IMAGE"
    }

    $wb.Close($false)
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
} catch {
    Write-Output "ERROR: $_"
}
