using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using Windows.Storage;

public class TestWinMdOcr
{
    public static void Main()
    {
        Console.WriteLine("WinMD OCR compiling test!");
        var lang = new Windows.Globalization.Language("en-US");
        var engine = OcrEngine.TryCreateFromLanguage(lang);
        Console.WriteLine("OcrEngine created: " + (engine != null));
    }
}
