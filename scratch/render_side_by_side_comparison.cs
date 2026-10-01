using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;
using PdfiumViewer;

namespace SideBySideComparison
{
    class Program
    {
        static string DesktopDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GenOffice_Test_All_Modules_Report");
        static string StrictTestDir = Path.Combine(DesktopDir, "03_WRITER_TEST", "Strict_Fidelity_Tests");

        [STAThread]
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==========================================================================");
            Console.WriteLine(">>> RENDER SO SÁNH TRỰC QUAN SIDE-BY-SIDE GIỮA MS WORD VÀ GENOFFICE <<<");
            Console.WriteLine("==========================================================================");

            string docxPath = Path.Combine(StrictTestDir, "Strict_Typography_Stress_Test.docx");
            string wordPdfPath = Path.Combine(StrictTestDir, "MSWord_Rendered.pdf");
            string wordPngPage1 = Path.Combine(StrictTestDir, "MSWord_Page_1.png");
            string wordPngPage2 = Path.Combine(StrictTestDir, "MSWord_Page_2.png");
            string genPngPage1 = Path.Combine(StrictTestDir, "GenOffice_Page_1.png");
            string comparisonPng = Path.Combine(StrictTestDir, "Side_By_Side_Word_vs_GenOffice.png");

            // 1. EXPORT MS WORD AS PDF VIA COM
            try
            {
                Type wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType != null && File.Exists(docxPath))
                {
                    object wordApp = Activator.CreateInstance(wordType);
                    wordApp.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, wordApp, new object[] { false });

                    object docs = wordType.InvokeMember("Documents", BindingFlags.GetProperty, null, wordApp, null);
                    object doc = docs.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, docs, new object[] { docxPath });

                    if (File.Exists(wordPdfPath)) File.Delete(wordPdfPath);
                    // wdExportFormatPDF = 17
                    doc.GetType().InvokeMember("ExportAsFixedFormat", BindingFlags.InvokeMethod, null, doc, new object[] { wordPdfPath, 17 });
                    doc.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, new object[] { false });
                    wordApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, wordApp, new object[] { false });
                    Marshal.ReleaseComObject(wordApp);

                    Console.WriteLine("  -> MS Word đã xuất PDF chuẩn xác: " + wordPdfPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI EXPORT WORD PDF] " + ex.Message);
            }

            // 2. RENDER WORD PDF TO PNG VIA PDFIUM
            Bitmap bmpWordP1 = null;
            if (File.Exists(wordPdfPath))
            {
                try
                {
                    using (var pdfDoc = PdfiumViewer.PdfDocument.Load(wordPdfPath))
                    {
                        var img1 = pdfDoc.Render(0, 300, 300, PdfRenderFlags.Annotations);
                        bmpWordP1 = new Bitmap(img1);
                        bmpWordP1.Save(wordPngPage1, ImageFormat.Png);

                        if (pdfDoc.PageCount > 1)
                        {
                            var img2 = pdfDoc.Render(1, 300, 300, PdfRenderFlags.Annotations);
                            img2.Save(wordPngPage2, ImageFormat.Png);
                        }
                        Console.WriteLine("  -> Đã trích xuất ảnh Render của MS Word: " + wordPngPage1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  [LỖI RENDER PDFIUM] " + ex.Message);
                }
            }

            // 3. RENDER GENOFFICE WRITERVIEW TO PNG
            Bitmap bmpGenP1 = new Bitmap(794, 1123);
            try
            {
                WriterView wv = new WriterView();
                wv.LoadDocumentFile(docxPath, null);
                RibbonBar ribbon = new RibbonBar();

                using (Graphics g = Graphics.FromImage(bmpGenP1))
                {
                    g.Clear(Color.White);
                    wv.Draw(g, 794, 1123, ribbon);
                }
                bmpGenP1.Save(genPngPage1, ImageFormat.Png);
                Console.WriteLine("  -> Đã render ảnh Trang của GenOffice: " + genPngPage1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI RENDER GENOFFICE] " + ex.Message);
            }

            // 4. COMBINE INTO HIGH-RES SIDE-BY-SIDE COMPARISON IMAGE
            if (bmpWordP1 != null && bmpGenP1 != null)
            {
                try
                {
                    int targetW = 900;
                    int targetH = 1250;
                    using (Bitmap sideBySide = new Bitmap(targetW * 2 + 40, targetH + 80))
                    using (Graphics g = Graphics.FromImage(sideBySide))
                    {
                        g.Clear(Color.FromArgb(15, 23, 42)); // Slate 900 background

                        using (Font headerFont = new Font("Segoe UI", 16f, FontStyle.Bold))
                        using (Font subFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
                        {
                            g.DrawString("BẢN GỐC TỪ MICROSOFT WORD 16.0 (PDF EXPORT)", headerFont, Brushes.SkyBlue, 20, 15);
                            g.DrawString("BẢN KẾT XUẤT TỪ GENOFFICE WRITER 2026", headerFont, Brushes.LightGreen, targetW + 40, 15);
                            g.DrawString("Kiểm tra ranh giới ngắt dòng, khoảng cách đoạn, phông chữ và bố cục lề A4", subFont, Brushes.LightGray, 20, 48);
                        }

                        // Draw Word Page (left)
                        g.DrawImage(bmpWordP1, new Rectangle(20, 75, targetW, targetH));
                        // Draw GenOffice Page (right)
                        g.DrawImage(bmpGenP1, new Rectangle(targetW + 40, 75, targetW, targetH));

                        sideBySide.Save(comparisonPng, ImageFormat.Png);
                        Console.WriteLine("  -> Đã tạo ảnh so sánh trực quan Side-by-Side: " + comparisonPng);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  [LỖI TẠO ẢNH SO SÁNH] " + ex.Message);
                }
            }

            Console.WriteLine("\n[HOÀN TẤT RENDER SO SÁNH TRỰC QUAN]");
        }
    }
}
