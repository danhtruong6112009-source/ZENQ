Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Reflection.Assembly]::LoadFrom("$PSScriptRoot\..\ZenQOffice.dll") | Out-Null

$form = New-Object GenOffice.MainForm
$form.Size = New-Object System.Drawing.Size(1366, 768)
$form.Show()
[System.Windows.Forms.Application]::DoEvents()

$form.SwitchToModule([GenOffice.AppMode]::Slides)
[System.Windows.Forms.Application]::DoEvents()
[System.Threading.Thread]::Sleep(400)

function Find-Ribbon($ctrl) {
    if ($ctrl -is [GenOffice.UI.RibbonBar]) { return $ctrl }
    foreach ($c in $ctrl.Controls) {
        $res = Find-Ribbon $c
        if ($res) { return $res }
    }
    return $null
}

$rb = Find-Ribbon $form
if ($rb) {
    Write-Output "Found RibbonBar!"
    $tabList = @(
        @{ Idx = 0; Name = "home" },
        @{ Idx = 1; Name = "insert" },
        @{ Idx = 2; Name = "design" },
        @{ Idx = 3; Name = "transitions" },
        @{ Idx = 5; Name = "slideshow" },
        @{ Idx = 6; Name = "view" }
    )
    foreach ($t in $tabList) {
        $rb.ActiveTabIndex = $t.Idx
        $form.Refresh()
        [System.Windows.Forms.Application]::DoEvents()
        [System.Threading.Thread]::Sleep(200)
        
        $bmp = New-Object System.Drawing.Bitmap($rb.Width, $rb.Height)
        $rb.DrawToBitmap($bmp, (New-Object System.Drawing.Rectangle(0, 0, $rb.Width, $rb.Height)))
        $out = "C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\slides_wpf_$($t.Name).png"
        $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Output "Saved $out"
    }
} else {
    Write-Output "Ribbon not found"
}

$form.Close()
$form.Dispose()
