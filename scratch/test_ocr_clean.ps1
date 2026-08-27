param([string]$ImagePath)

$ErrorActionPreference = 'Stop'
[Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null
[Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
Add-Type -AssemblyName System.Runtime.WindowsRuntime

$asTaskGeneric = [System.WindowsRuntimeSystemExtensions].GetMethods() | 
    Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.IsGenericMethod } | 
    Select-Object -First 1

function AwaitTask($op, $type) {
    $m = $asTaskGeneric.MakeGenericMethod($type)
    $t = $m.Invoke($null, @($op))
    $t.Wait()
    return $t.Result
}

$fullPath = [System.IO.Path]::GetFullPath($ImagePath)
$fileOp = [Windows.Storage.StorageFile]::GetFileFromPathAsync($fullPath)
$file = AwaitTask $fileOp ([Windows.Storage.StorageFile])

$streamOp = $file.OpenAsync([Windows.Storage.FileAccessMode]::Read)
$stream = AwaitTask $streamOp ([Windows.Storage.Streams.IRandomAccessStream])

$decoderOp = [Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)
$decoder = AwaitTask $decoderOp ([Windows.Graphics.Imaging.BitmapDecoder])

$softBmpOp = $decoder.GetSoftwareBitmapAsync()
$softBmp = AwaitTask $softBmpOp ([Windows.Graphics.Imaging.SoftwareBitmap])

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if (-not $engine) {
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage([Windows.Globalization.Language]::new('en-US'))
}

$ocrOp = $engine.RecognizeAsync($softBmp)
$ocrResult = AwaitTask $ocrOp ([Windows.Media.Ocr.OcrResult])

$linesOut = @()
foreach ($line in $ocrResult.Lines) {
    $minX = 999999; $minY = 999999; $maxX = 0; $maxY = 0;
    foreach ($w in $line.Words) {
        $minX = [Math]::Min($minX, [int]$w.BoundingRect.X)
        $minY = [Math]::Min($minY, [int]$w.BoundingRect.Y)
        $maxX = [Math]::Max($maxX, [int]($w.BoundingRect.X + $w.BoundingRect.Width))
        $maxY = [Math]::Max($maxY, [int]($w.BoundingRect.Y + $w.BoundingRect.Height))
    }
    $lineObj = @{
        x = $minX
        y = $minY
        w = [Math]::Max(10, $maxX - $minX)
        h = [Math]::Max(10, $maxY - $minY)
        text = $line.Text
    }
    $linesOut += $lineObj
}

$json = ConvertTo-Json -InputObject $linesOut -Compress
Write-Output $json
