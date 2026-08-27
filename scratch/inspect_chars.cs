using System;
using System.Reflection;
using System.Runtime.InteropServices;
using PdfiumViewer;

public class InspectCharsInBai1
{
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_ClosePage(IntPtr page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFText_LoadPage(IntPtr page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFText_ClosePage(IntPtr text_page);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern double FPDFText_GetFontSize(IntPtr text_page, int index);
    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_GetUnicode(IntPtr text_page, int index);

    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var pdfiumDoc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var fileField = pdfiumDoc.GetType().GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance);
        object fileObj = fileField.GetValue(pdfiumDoc);
        var docField = fileObj.GetType().GetField("_document", BindingFlags.NonPublic | BindingFlags.Instance);
        IntPtr docHandle = (IntPtr)docField.GetValue(fileObj);

        IntPtr pageHandle = FPDF_LoadPage(docHandle, 0);
        IntPtr textHandle = FPDFText_LoadPage(pageHandle);

        string fullText = pdfiumDoc.GetPdfText(0);
        int idx = fullText.IndexOf("Bài 1: Mệnh đề");
        Console.WriteLine("Index of 'Bài 1: Mệnh đề': " + idx);
        for (int i = idx; i < idx + 14; i++)
        {
            char ch = (char)FPDFText_GetUnicode(textHandle, i);
            double fs = FPDFText_GetFontSize(textHandle, i);
            Console.WriteLine(string.Format("Char[{0}] = '{1}', fs = {2}", i, ch, fs));
        }

        FPDFText_ClosePage(textHandle);
        FPDF_ClosePage(pageHandle);
    }
}
