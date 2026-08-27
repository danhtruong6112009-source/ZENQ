using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using GenOffice.Core;
using GenOffice.Format;

public class BenchmarkNativeFontSize
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

    public static void Main()
    {
        string filePath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var pdfiumDoc = PdfiumViewer.PdfDocument.Load(filePath);

        var fileField = pdfiumDoc.GetType().GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance);
        object fileObj = fileField.GetValue(pdfiumDoc);
        var docField = fileObj.GetType().GetField("_document", BindingFlags.NonPublic | BindingFlags.Instance);
        IntPtr docHandle = (IntPtr)docField.GetValue(fileObj);

        Stopwatch sw = Stopwatch.StartNew();
        IntPtr pageHandle = FPDF_LoadPage(docHandle, 0);
        Console.WriteLine("FPDF_LoadPage: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        IntPtr textHandle = FPDFText_LoadPage(pageHandle);
        Console.WriteLine("FPDFText_LoadPage: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        for (int i = 0; i < 60; i++)
        {
            double fs = FPDFText_GetFontSize(textHandle, i * 10);
        }
        Console.WriteLine("FPDFText_GetFontSize 60 times: " + sw.ElapsedMilliseconds + " ms");

        FPDFText_ClosePage(textHandle);
        FPDF_ClosePage(pageHandle);
    }
}
