using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GenOffice.Core;
using GenOffice.Format;

public class TestOcrHandler
{
    public static void Main()
    {
        string filePath = @"C:\Users\Danh\Desktop\Chuyên Đề Kỹ Thuật PowerPoint - Lồng Ảnh & Merge Shapes.pdf";
        var doc = PdfHandler.LoadPdf(filePath);
        Console.WriteLine("Loaded doc. Testing OCR on page 0...");

        var elements = RecognizePage(doc, 0);
        Console.WriteLine("OCR Result count: " + elements.Count);
        foreach (var el in elements)
        {
            Console.WriteLine(string.Format("Elem: '{0}' | Bounds: {1} | TextColor: {2} | BgColor: {3}",
                el.Text, el.Bounds, el.TextColor, el.DetectedBackgroundColor));
        }
    }

    public static List<PdfTextElement> RecognizePage(PdfDocument doc, int pageIndex)
    {
        var list = new List<PdfTextElement>();
        if (doc == null || doc.UnderlyingPdfiumDoc == null || pageIndex < 0 || pageIndex >= doc.UnderlyingPdfiumDoc.PageCount)
            return list;

        string tempImg = Path.Combine(Path.GetTempPath(), "genoffice_ocr_" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var pSize = doc.UnderlyingPdfiumDoc.PageSizes[pageIndex];
            int renderDpi = 120;
            int imgW = (int)Math.Round(pSize.Width * renderDpi / 72.0f);
            int imgH = (int)Math.Round(pSize.Height * renderDpi / 72.0f);

            Bitmap pageBmp = null;
            try
            {
                pageBmp = (Bitmap)doc.UnderlyingPdfiumDoc.Render(pageIndex, imgW, imgH, renderDpi, renderDpi, false);
                pageBmp.Save(tempImg, ImageFormat.Png);
            }
            catch
            {
                return list;
            }

            string script = @"
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
";

            string psScriptPath = Path.Combine(Path.GetTempPath(), "genoffice_ocr_script.ps1");
            File.WriteAllText(psScriptPath, script, Encoding.UTF8);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -File \"{0}\" -ImagePath \"{1}\"", psScriptPath, tempImg),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            string output = "";
            using (Process proc = Process.Start(psi))
            {
                output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(10000);
            }

            if (!string.IsNullOrEmpty(output))
            {
                output = output.Trim();
                float scaleX = pSize.Width / (float)imgW;
                float scaleY = pSize.Height / (float)imgH;

                Regex reg = new Regex(@"""x"":\s*(\d+).*?""y"":\s*(\d+).*?""w"":\s*(\d+).*?""h"":\s*(\d+).*?""text"":\s*""(.*?)""", RegexOptions.Compiled);
                // Also match any order of keys
                Regex itemRegex = new Regex(@"\{[^{}]*\}", RegexOptions.Compiled);
                MatchCollection items = itemRegex.Matches(output);

                foreach (Match m in items)
                {
                    string itemStr = m.Value;
                    Match mx = Regex.Match(itemStr, @"""x"":\s*(\d+)");
                    Match my = Regex.Match(itemStr, @"""y"":\s*(\d+)");
                    Match mw = Regex.Match(itemStr, @"""w"":\s*(\d+)");
                    Match mh = Regex.Match(itemStr, @"""h"":\s*(\d+)");
                    Match mt = Regex.Match(itemStr, @"""text"":\s*""(.*?)""");

                    if (mx.Success && my.Success && mw.Success && mh.Success && mt.Success)
                    {
                        int rx = int.Parse(mx.Groups[1].Value);
                        int ry = int.Parse(my.Groups[1].Value);
                        int rw = int.Parse(mw.Groups[1].Value);
                        int rh = int.Parse(mh.Groups[1].Value);
                        string txt = Regex.Unescape(mt.Groups[1].Value);

                        float fx = rx * scaleX;
                        float fy = ry * scaleY;
                        float fw = rw * scaleX;
                        float fh = rh * scaleY;

                        Color bg = Color.White, fg = Color.Black;
                        if (pageBmp != null)
                        {
                            PdfHandler.SampleColors(pageBmp, rx, ry, rw, rh, out bg, out fg);
                        }

                        float fs = Math.Max(8f, (float)Math.Round(fh * 0.70f, 1));
                        var elem = new PdfTextElement(txt, new RectangleF(fx, fy, fw, fh), "Segoe UI", fs)
                        {
                            IsDetectedFromPdf = false,
                            TextColor = fg,
                            DetectedBackgroundColor = bg,
                            BackgroundColor = Color.Transparent,
                            BorderColor = Color.Transparent,
                            BorderWidth = 0f
                        };
                        list.Add(elem);
                    }
                }
            }

            if (pageBmp != null) pageBmp.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocr Exception: " + ex.Message);
        }
        finally
        {
            try { if (File.Exists(tempImg)) File.Delete(tempImg); } catch { }
        }

        return list;
    }
}
