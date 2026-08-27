using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PdfiumViewer;

public class TestFontSize
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

    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
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

            string[] testPhrases = new string[] {
                "SỔ TAY TRỌNG TÂM",
                "Sách Giáo Khoa",
                "CHƯƠNG I: MỆNH ĐỀ VÀ TẬP HỢP",
                "Bài 1: Mệnh đề",
                "1. Mệnh đề, mệnh đề chứa biến",
                "Khái niệm: Mệnh đề logic"
            };

            foreach (var phrase in testPhrases)
            {
                int idx = fullPageText.IndexOf(phrase);
                if (idx >= 0)
                {
                    double fs = FPDFText_GetFontSize(textHandle, idx);
                    Console.WriteLine(string.Format("Phrase: [{0}] => Native FontSize = {1:0.0} pt", phrase, fs));
                }
            }

            FPDFText_ClosePage(textHandle);
            FPDF_ClosePage(pageHandle);
        }
    }
}
