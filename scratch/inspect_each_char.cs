using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PdfiumViewer;

public class InspectEachCharFontSize
{
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_ClosePage(IntPtr page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDFText_LoadPage(IntPtr page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFText_ClosePage(IntPtr text_page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double FPDFText_GetFontSize(IntPtr text_page, int char_index);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_CountChars(IntPtr text_page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_GetText(IntPtr text_page, int start_index, int count, byte[] result);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFText_GetCharBox(IntPtr page, int index, out double left, out double right, out double bottom, out double top);

    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Console.OutputEncoding = Encoding.UTF8;
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var fileField = doc.GetType().GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance);
            object fileObj = fileField.GetValue(doc);
            var docField = fileObj.GetType().GetField("_document", BindingFlags.NonPublic | BindingFlags.Instance);
            IntPtr docHandle = (IntPtr)docField.GetValue(fileObj);

            IntPtr pageHandle = FPDF_LoadPage(docHandle, 0);
            IntPtr textHandle = FPDFText_LoadPage(pageHandle);

            int charCount = FPDFText_CountChars(textHandle);
            byte[] textBuf = new byte[(charCount + 1) * 2];
            int written = FPDFText_GetText(textHandle, 0, charCount, textBuf);
            string fullPageText = Encoding.Unicode.GetString(textBuf, 0, written * 2);

            int chuongIdx = fullPageText.IndexOf("CHƯƠNG");
            Console.WriteLine("Index: " + chuongIdx);
            for (int i = chuongIdx; i < chuongIdx + 28 && i < charCount; i++)
            {
                double fs = FPDFText_GetFontSize(textHandle, i);
                double l, r, b, t;
                FPDFText_GetCharBox(textHandle, i, out l, out r, out b, out t);
                Console.WriteLine(string.Format("Char {0} '{1}': FontSize={2:0.0}pt, Box: L={3:0.0}, R={4:0.0}, B={5:0.0}, T={6:0.0}, W={7:0.0}, H={8:0.0}",
                    i, fullPageText[i], fs, l, r, b, t, r - l, t - b));
            }

            FPDFText_ClosePage(textHandle);
            FPDF_ClosePage(pageHandle);
        }
    }
}
