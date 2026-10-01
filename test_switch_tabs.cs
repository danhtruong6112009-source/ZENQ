using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestSwitchTabs
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                int w = 1280;
                int h = 768;
                string artifactDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";

                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(w, h);
                    form.Ribbon.StandaloneMode = false;

                    // Setup tabs for all 4 apps:
                    // 0: Trang chính (Home)
                    // 1: Văn bản 1.docx (Writer)
                    // 2: Bảng tính 1.xlsx (Calc)
                    // 3: Trình chiếu 1.pptx (Slides)
                    // 4: Tài liệu 1.pdf (Pdf)
                    form.Ribbon.DocTabs.Clear();
                    form.Ribbon.DocTabs.Add(new DocTab("Trang chính", OfficeAppMode.Home, false));
                    form.Ribbon.DocTabs.Add(new DocTab("Văn bản 1.docx", OfficeAppMode.Writer, false));
                    form.Ribbon.DocTabs.Add(new DocTab("Bảng tính 1.xlsx", OfficeAppMode.Calc, false));
                    form.Ribbon.DocTabs.Add(new DocTab("Trình chiếu 1.pptx", OfficeAppMode.Slides, false));
                    form.Ribbon.DocTabs.Add(new DocTab("Tài liệu 1.pdf", OfficeAppMode.Pdf, false));

                    // Standard Mode Tests
                    AppSettings.Instance.RibbonStyle = RibbonStyle.Standard;

                    Bitmap[] standardBitmaps = new Bitmap[4];
                    string[] appNames = new string[] { "Writer", "Calc", "Slides", "PDF" };

                    for (int tabIdx = 1; tabIdx <= 4; tabIdx++)
                    {
                        form.Ribbon.SwitchDocTab(tabIdx, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                        Bitmap bmp = new Bitmap(w, 160);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.Ribbon.Draw(g, w, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        standardBitmaps[tabIdx - 1] = bmp;
                        string outPath = Path.Combine(artifactDir, "switch_std_" + appNames[tabIdx - 1].ToLower() + ".png");
                        bmp.Save(outPath, ImageFormat.Png);

                        // Print DocTabBounds info
                        Console.Write("Active App: " + appNames[tabIdx - 1] + " | DocTabBounds: ");
                        for (int k = 1; k < form.Ribbon.DocTabBounds.Count; k++)
                        {
                            Console.Write("[" + k + "]=" + form.Ribbon.DocTabBounds[k].X + ".." + form.Ribbon.DocTabBounds[k].Right + " ");
                        }
                        Console.WriteLine();
                    }

                    // Stack all 4 standard ribbons vertically into one comparison image with vertical alignment guidelines
                    using (Bitmap stacked = new Bitmap(w, 160 * 4))
                    {
                        using (Graphics g = Graphics.FromImage(stacked))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                g.DrawImage(standardBitmaps[i], 0, i * 160);
                                if (i > 0)
                                {
                                    g.DrawLine(Pens.Black, 0, i * 160, w, i * 160);
                                }
                            }
                            // Draw red alignment reference lines across all 4 ribbons
                            using (Pen redPen = new Pen(Color.FromArgb(200, 255, 0, 0), 1.0f))
                            {
                                // Vertical guides
                                g.DrawLine(redPen, 14, 0, 14, 160 * 4); // First group button start
                                g.DrawLine(redPen, 120, 0, 120, 160 * 4); // Top doc tab start
                                g.DrawLine(redPen, 120 + 175, 0, 120 + 175, 160 * 4); // Tab 1 divider
                                g.DrawLine(redPen, 120 + 350, 0, 120 + 350, 160 * 4); // Tab 2 divider
                                g.DrawLine(redPen, 120 + 525, 0, 120 + 525, 160 * 4); // Tab 3 divider
                                g.DrawLine(redPen, 120 + 700, 0, 120 + 700, 160 * 4); // Tab 4 divider
                                g.DrawLine(redPen, 70, 0, 70, 160 * 4); // Ribbon tabs start
                                // Horizontal guides for each ribbon section (Y:68 = button top, Y:130 = button bottom, Y:137 = label)
                                for (int i = 0; i < 4; i++)
                                {
                                    int yOff = i * 160;
                                    g.DrawLine(redPen, 0, yOff + 68, w, yOff + 68); // Button top edge
                                    g.DrawLine(redPen, 0, yOff + 130, w, yOff + 130); // Button bottom edge
                                    g.DrawLine(redPen, 0, yOff + 137, w, yOff + 137); // Group label Y
                                }
                            }
                        }
                        stacked.Save(Path.Combine(artifactDir, "verify_four_apps_standard_stacked.png"), ImageFormat.Png);
                    }

                    // Island Mode Tests
                    AppSettings.Instance.RibbonStyle = RibbonStyle.Island;

                    Bitmap[] islandBitmaps = new Bitmap[4];
                    for (int tabIdx = 1; tabIdx <= 4; tabIdx++)
                    {
                        form.Ribbon.SwitchDocTab(tabIdx, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                        Bitmap bmp = new Bitmap(w, 160);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.Ribbon.Draw(g, w, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        islandBitmaps[tabIdx - 1] = bmp;
                    }

                    // Stack all 4 island ribbons vertically into one comparison image with vertical alignment guidelines
                    using (Bitmap stacked = new Bitmap(w, 160 * 4))
                    {
                        using (Graphics g = Graphics.FromImage(stacked))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                g.DrawImage(islandBitmaps[i], 0, i * 160);
                                if (i > 0)
                                {
                                    g.DrawLine(Pens.Black, 0, i * 160, w, i * 160);
                                }
                            }
                            // Draw red alignment reference lines across all 4 ribbons
                            using (Pen redPen = new Pen(Color.FromArgb(200, 255, 0, 0), 1.5f))
                            {
                                g.DrawLine(redPen, 14, 0, 14, 160 * 4); // First group button start
                                g.DrawLine(redPen, 120, 0, 120, 160 * 4); // Top doc tab start
                                g.DrawLine(redPen, 120 + 175, 0, 120 + 175, 160 * 4); // Tab 1 divider
                                g.DrawLine(redPen, 120 + 350, 0, 120 + 350, 160 * 4); // Tab 2 divider
                                g.DrawLine(redPen, 120 + 525, 0, 120 + 525, 160 * 4); // Tab 3 divider
                                g.DrawLine(redPen, 120 + 700, 0, 120 + 700, 160 * 4); // Tab 4 divider
                            }
                        }
                        stacked.Save(Path.Combine(artifactDir, "verify_four_apps_island_stacked.png"), ImageFormat.Png);
                    }

                    // Test Back-and-Forth Switching:
                    // 1 -> 2 -> 1 -> 2 -> 3 -> 4 -> 2 -> 1
                    int[] switchSequence = new int[] { 1, 2, 1, 2, 3, 4, 2, 1 };
                    Console.WriteLine("\nTesting rapid back-and-forth switching sequence:");
                    foreach (int targetTab in switchSequence)
                    {
                        form.Ribbon.SwitchDocTab(targetTab, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                        Console.WriteLine("Switched to Tab " + targetTab + " (" + form.Ribbon.DocTabs[targetTab].Title + "), ActiveRibbonTab=" + form.Ribbon.ActiveRibbonTab + ", CurrentMode=" + form.Ribbon.CurrentMode);
                    }

                    // Clean up bitmaps
                    for (int i = 0; i < 4; i++)
                    {
                        standardBitmaps[i].Dispose();
                        islandBitmaps[i].Dispose();
                    }

                    Console.WriteLine("\nALL TESTS COMPLETED SUCCESSFULLY!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex);
            }
        }
    }
}
