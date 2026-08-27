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

$imgPath = [System.IO.Path]::GetFullPath("scratch\problem_file_page0.png")
$fileOp = [Windows.Storage.StorageFile]::GetFileFromPathAsync($imgPath)
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

foreach ($line in $ocrResult.Lines) {
    $firstWord = $line.Words[0]
    $lastWord = $line.Words[$line.Words.Count - 1]
    $x = [int]$firstWord.BoundingRect.X
    $y = [int]$firstWord.BoundingRect.Y
    $w = [int]($lastWord.BoundingRect.X + $lastWord.BoundingRect.Width - $x)
    $h = [int]$firstWord.BoundingRect.Height
    Write-Host "OCR_LINE: $x,$y,$w,$h | $($line.Text)"
}
