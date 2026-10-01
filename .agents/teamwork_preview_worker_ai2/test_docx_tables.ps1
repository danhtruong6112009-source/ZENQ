[System.Reflection.Assembly]::LoadFrom("c:\Users\Danh\Desktop\genofficepy\GenOffice.exe") | Out-Null

$item = Get-ChildItem -Path "C:\Users\Danh\Downloads" -Filter "*ND GHI*.docx" | Select-Object -First 1
if ($item -ne $null) {
    Write-Host "Found file: $($item.FullName)"
    $meta = $null
    $res = [GenOffice.Format.DocxHandler]::LoadDocxFile($item.FullName, [ref]$meta)
    Write-Host "Docx loaded: $res"
    if ($meta -ne $null -and $meta.Tables -ne $null) {
        Write-Host "Total tables in document: $($meta.Tables.Count)"
        for ($i = 0; $i -lt $meta.Tables.Count; $i++) {
            $tbl = $meta.Tables[$i]
            Write-Host "  Table $($i + 1): Rows=$($tbl.Rows.Count)"
        }
        if ($meta.Tables.Count -eq 8) {
            Write-Host ">>> SUCCESS: Exactly 8 tables loaded intact. R4 Guardrail fully respected! <<<"
        } else {
            Write-Host ">>> WARNING: Expected 8 tables, got $($meta.Tables.Count) <<<"
        }
    }
} else {
    Write-Host "File not found."
}
