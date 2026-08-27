using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class InspectPdfFonts
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        byte[] bytes = File.ReadAllBytes(pdfPath);
        string text = Encoding.ASCII.GetString(bytes);

        Console.WriteLine("=== FONTS IN PDF ===");
        Regex fontRegex = new Regex(@"/BaseFont\s*/([A-Za-z0-9\+\-_]+)", RegexOptions.IgnoreCase);
        MatchCollection matches = fontRegex.Matches(text);
        foreach (Match m in matches)
        {
            string rawName = m.Groups[1].Value;
            // PDF fonts often have subset prefix like ABCDEF+FontName
            string cleanName = rawName;
            int plusIdx = cleanName.IndexOf('+');
            if (plusIdx >= 0) cleanName = cleanName.Substring(plusIdx + 1);
            Console.WriteLine("Raw Font: " + rawName + " => Clean Font: " + cleanName);
        }

        Regex fontDescRegex = new Regex(@"/FontDescriptor.*?/FontName\s*/([A-Za-z0-9\+\-_]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        MatchCollection fdesc = fontDescRegex.Matches(text);
        foreach (Match m in fdesc)
        {
            Console.WriteLine("FontDescriptor: " + m.Groups[1].Value);
        }
    }
}
