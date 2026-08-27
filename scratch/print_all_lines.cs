using System;
using System.Text;
using PdfiumViewer;

public class PrintAllLines
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Console.OutputEncoding = Encoding.UTF8;
        using (var doc = PdfDocument.Load(pdfPath))
        {
            string text = doc.GetPdfText(0);
            string[] rawLines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < rawLines.Length; i++)
            {
                Console.WriteLine(string.Format("L{0}: len={1}, [{2}]", i, rawLines[i].Length, rawLines[i]));
            }
        }
    }
}
