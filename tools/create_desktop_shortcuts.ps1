$desktop = [System.Environment]::GetFolderPath('Desktop')
$wsh = New-Object -ComObject WScript.Shell
$targetExe = "C:\Users\Danh\Desktop\genofficepy\GenOffice.exe"
$iconDir = "C:\Users\Danh\Desktop\genofficepy\icon"
$workDir = "C:\Users\Danh\Desktop\genofficepy"

$shortcuts = @(
    @{ Name = "GenOffice Writer.lnk"; Args = "-writer"; Icon = "writer.ico"; Desc = "Soạn thảo văn bản GenOffice Writer" },
    @{ Name = "GenOffice Calc.lnk";   Args = "-calc";   Icon = "calc.ico";   Desc = "Bảng tính GenOffice Calc" },
    @{ Name = "GenOffice Slides.lnk"; Args = "-slides"; Icon = "present.ico"; Desc = "Trình chiếu GenOffice Slides" },
    @{ Name = "GenOffice PDF.lnk";    Args = "-pdf";    Icon = "pdf.ico";    Desc = "Đọc và chỉnh sửa GenOffice PDF" }
)

foreach ($s in $shortcuts) {
    $lnkPath = Join-Path $desktop $s.Name
    $sc = $wsh.CreateShortcut($lnkPath)
    $sc.TargetPath = $targetExe
    $sc.Arguments = $s.Args
    $sc.WorkingDirectory = $workDir
    $sc.IconLocation = "$($iconDir)\$($s.Icon),0"
    $sc.Description = $s.Desc
    $sc.Save()
    Write-Host "Created shortcut: $lnkPath (Icon: $($s.Icon))"
}
