using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PdfiumViewer;

public class TestNativePdfiumFonts
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
    public static extern int FPDFText_GetFontInfo(IntPtr text_page, int char_index, byte[] buffer, int buflen, out int flags);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_CountChars(IntPtr text_page);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_GetText(IntPtr text_page, int start_index, int count, byte[] result);

    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var fileField = doc.GetType().GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance);
            object fileObj = fileField.GetValue(doc);
            var docField = fileObj.GetType().GetField("_document", BindingFlags.NonPublic | BindingFlags.Instance);
            IntPtr docHandle = (IntPtr)docField.GetValue(fileObj);

            Console.WriteLine("Doc handle: " + docHandle);

            IntPtr pageHandle = FPDF_LoadPage(docHandle, 0);
            IntPtr textHandle = FPDFText_LoadPage(pageHandle);

            int charCount = FPDFText_CountChars(textHandle);
            Console.WriteLine("Char count: " + charCount);

            byte[] textBuf = new byte[(charCount + 1) * 2];
            int written = FPDFText_GetText(textHandle, 0, charCount, textBuf);
            string fullPageText = Encoding.Unicode.GetString(textBuf, 0, written * 2);

            // Tìm vị trí chữ "CHƯƠNG"
            int chuongIdx = fullPageText.IndexOf("CHƯƠNG");
            Console.WriteLine("CHƯƠNG index: " + chuongIdx);

            if (chuongIdx >= 0)
            {
                double fontSize = FPDFText_GetFontSize(textHandle, chuongIdx);
                int flags;
                byte[] fontBuf = new byte[256];
                int fontLen = FPDFText_GetFontInfo(textHandle, chuongIdx, fontBuf, fontBuf.Length, out flags);
                string fontName = Encoding.ASCII.GetString(fontBuf, 0, Math.Max(0, fontLen - 1));

                Console.WriteLine("=== FONT INFO FOR 'CHƯƠNG' ===");
                Console.WriteLine("FontSize: " + fontSize);
                Console.WriteLine("FontName: [" + fontName + "]");
                Console.WriteLine("Flags: " + flags);
            }

            // Tìm vị trí chữ "Sách Giáo Khoa"
            int sgkIdx = fullPageText.IndexOf("Sách");
            if (sgkIdx >= 0)
            {
                double fontSize = FPDFText_GetFontSize(textHandle, sgkIdx);
                int flags;
                byte[] fontBuf = new byte[256];
                int fontLen = FPDFText_GetFontInfo(textHandle, sgkIdx, fontBuf, fontBuf.Length, out flags);
                string fontName = Encoding.ASCII.GetString(fontBuf, 0, Math.Max(0, fontLen - 1));

                Console.WriteLine("=== FONT INFO FOR 'Sách' ===");
                Console.WriteLine("FontSize: " + fontSize);
                Console.WriteLine("FontName: [" + fontName + "]");
            }

            FPDFText_ClosePage(textHandle);
            FPDF_ClosePage(pageHandle);
        }
    }
}
