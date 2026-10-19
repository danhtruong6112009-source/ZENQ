using System;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice
{
    static class Program
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string AppID);

        [STAThread]
        static void Main(string[] args)
        {
            try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
            try
            {
                // Tách biệt hoàn toàn AppUserModelID cho từng phân hệ để Windows Taskbar
                // hiển thị chuẩn 100% icon của từng ứng dụng con và icon mẹ ZenQ Office chính thức:
                string appId = "ZenQOffice.Suite.Hub";
                if (args != null && args.Length > 0)
                {
                    string first = args[0].Trim().ToLowerInvariant();
                    if (first == "-calc" || first == "-excel" || first == "-sheet") appId = "ZenQOffice.Suite.Calc";
                    else if (first == "-word" || first == "-writer" || first == "-doc") appId = "ZenQOffice.Suite.Writer";
                    else if (first == "-slides" || first == "-present" || first == "-ppt") appId = "ZenQOffice.Suite.Slides";
                    else if (first == "-pdf") appId = "ZenQOffice.Suite.Pdf";
                    else if (System.IO.File.Exists(first))
                    {
                        string ext = System.IO.Path.GetExtension(first).ToLowerInvariant();
                        if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") appId = "ZenQOffice.Suite.Calc";
                        else if (ext == ".pptx" || ext == ".ppt") appId = "ZenQOffice.Suite.Slides";
                        else if (ext == ".pdf") appId = "ZenQOffice.Suite.Pdf";
                        else appId = "ZenQOffice.Suite.Writer";
                    }
                }
                try { SetCurrentProcessExplicitAppUserModelID(appId); } catch { }

                try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try { FluentIconRenderer.EnsureFontLoaded(); } catch { }

                if (args != null && args.Length > 0 && args[0].Equals("--verify-pptx", StringComparison.OrdinalIgnoreCase))
                {
                    VerifyPptx(args.Length > 1 ? args[1] : "sample.pptx");
                    return;
                }


                if (args != null && args.Length > 0 && args[0].Equals("--test-svg", StringComparison.OrdinalIgnoreCase))
                {
                    string svgPath = args.Length > 1 ? args[1] : "sample.svg";
                    string outPath = args.Length > 2 ? args[2] : "rendered_out.png";
                    int tw = args.Length > 3 ? int.Parse(args[3]) : 0;
                    int th = args.Length > 4 ? int.Parse(args[4]) : 0;
                    Console.WriteLine($"Reading SVG {svgPath} -> {outPath} ({tw}x{th})");
                    string xml = File.ReadAllText(svgPath);
                    var bmp = ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(xml, tw, th);
                    if (bmp != null)
                    {
                        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                        Console.WriteLine($"SUCCESS: saved {bmp.Width}x{bmp.Height} to " + outPath);
                    }
                    else
                    {
                        Console.WriteLine("FAILED: RenderSvgToBitmap returned null");
                    }
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                {
                    HandleSafeException(e.Exception, "Sự cố giao diện", false);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    Exception ex = e.ExceptionObject as Exception;
                    HandleSafeException(ex, "Sự cố hệ thống", true);
                };

                Application.Run(new GenOfficeApplicationContext(args));
            }
            catch (Exception ex)
            {
                HandleSafeException(ex, "Lỗi khởi động ZENQ OFFICE", true);
            }
            finally
            {
                try { Environment.Exit(0); } catch { }
            }
        }

        private static void HandleSafeException(Exception ex, string contextTitle, bool isFatal)
        {
            if (ex == null) return;

            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDir = Path.Combine(localApp, "ZenQOffice");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "crash.log");

                string cleanDetails = ZenQCrashDialogForm.SanitizeExceptionDetails(ex);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logPath, $"[{timestamp}] [{contextTitle}]\r\n{cleanDetails}\r\n----------------------------------------\r\n");
            }
            catch { }

            ZenQCrashDialogForm.ShowDialogSafe(ex, contextTitle, isFatal);
        }




        private static void RenderRibbonPreview()
        {
            try
            {
                File.WriteAllText("render_log.txt", "Started RenderRibbonPreview\n");
                using (AppForm form = new AppForm())
                {
                    form.Size = new System.Drawing.Size(1366, 768);
                    form.OpenDirectMode(OfficeAppMode.Slides, null);
                    form.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(500);

                    File.AppendAllText("render_log.txt", $"Form opened. Ribbon is null? {(form.Ribbon == null)}\n");
                    if (form.Ribbon != null)
                    {
                        var tabs = new[] {
                            new { Idx = 0, Name = "home" },
                            new { Idx = 1, Name = "insert" },
                            new { Idx = 2, Name = "design" },
                            new { Idx = 3, Name = "transitions" },
                            new { Idx = 4, Name = "animations" },
                            new { Idx = 5, Name = "slideshow" },
                            new { Idx = 6, Name = "view" }
                        };

                        string artifactDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch");
                        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);
                        foreach (var t in tabs)
                        {
                            form.Ribbon.ActiveRibbonTab = t.Idx;
                            using (var bmp = new System.Drawing.Bitmap(1366, 165))
                            using (var g = System.Drawing.Graphics.FromImage(bmp))
                            {
                                g.Clear(System.Drawing.Color.White);
                                form.Ribbon.Draw(g, 1366, 768, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                                string path1 = Path.Combine(artifactDir, $"slides_wpf_{t.Name}.png");
                                string path2 = $"scratch/slides_wpf_{t.Name}.png";
                                bmp.Save(path1, System.Drawing.Imaging.ImageFormat.Png);
                                bmp.Save(path2, System.Drawing.Imaging.ImageFormat.Png);
                                File.AppendAllText("render_log.txt", $"Saved {path1} and {path2}\n");
                            }
                        }

                        // Contextual Tabs
                        if (form.SlidesView != null && form.SlidesView.ActiveSlide != null)
                        {
                            // 1. Shape Format
                            var shp = new ShapeNode { Type = ShapeType.Rectangle, Transform = new Transform2D { X = 50, Y = 50, Width = 100, Height = 100 } };
                            form.SlidesView.ActiveSlide.Shapes.Add(shp);
                            form.SlidesView.SelectedShapeIndex = form.SlidesView.ActiveSlide.Shapes.Count - 1;
                            form.Ribbon.ActiveRibbonTab = 7;
                            using (var bmp = new System.Drawing.Bitmap(1366, 165))
                            using (var g = System.Drawing.Graphics.FromImage(bmp))
                            {
                                g.Clear(System.Drawing.Color.White);
                                form.Ribbon.Draw(g, 1366, 768, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                                bmp.Save(Path.Combine(artifactDir, "slides_wpf_shapeformat.png"), System.Drawing.Imaging.ImageFormat.Png);
                                bmp.Save("scratch/slides_wpf_shapeformat.png", System.Drawing.Imaging.ImageFormat.Png);
                            }

                            // 1b. Home tab with selected shape (active font buttons)
                            form.Ribbon.ActiveRibbonTab = 0;
                            shp.Bold = true;
                            shp.HasTextShadow = true;
                            using (var bmp = new System.Drawing.Bitmap(1366, 165))
                            using (var g = System.Drawing.Graphics.FromImage(bmp))
                            {
                                g.Clear(System.Drawing.Color.White);
                                form.Ribbon.Draw(g, 1366, 768, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                                bmp.Save(Path.Combine(artifactDir, "slides_wpf_home_selected.png"), System.Drawing.Imaging.ImageFormat.Png);
                            }

                            // 2. Picture Format
                            var pic = new ShapeNode { Type = ShapeType.Image, CachedImage = new System.Drawing.Bitmap(16, 16), Transform = new Transform2D { X = 50, Y = 50, Width = 100, Height = 100 } };
                            form.SlidesView.ActiveSlide.Shapes.Add(pic);
                            form.SlidesView.SelectedShapeIndex = form.SlidesView.ActiveSlide.Shapes.Count - 1;
                            form.Ribbon.ActiveRibbonTab = 7;
                            using (var bmp = new System.Drawing.Bitmap(1366, 165))
                            using (var g = System.Drawing.Graphics.FromImage(bmp))
                            {
                                g.Clear(System.Drawing.Color.White);
                                form.Ribbon.Draw(g, 1366, 768, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                                bmp.Save(Path.Combine(artifactDir, "slides_wpf_pictureformat.png"), System.Drawing.Imaging.ImageFormat.Png);
                                bmp.Save("scratch/slides_wpf_pictureformat.png", System.Drawing.Imaging.ImageFormat.Png);
                            }

                            // 3. Table Design
                            var tbl = new ShapeNode { Type = ShapeType.Table, TableRows = 2, TableColumns = 2, Transform = new Transform2D { X = 50, Y = 50, Width = 100, Height = 100 } };
                            form.SlidesView.ActiveSlide.Shapes.Add(tbl);
                            form.SlidesView.SelectedShapeIndex = form.SlidesView.ActiveSlide.Shapes.Count - 1;
                            form.Ribbon.ActiveRibbonTab = 7;
                            using (var bmp = new System.Drawing.Bitmap(1366, 165))
                            using (var g = System.Drawing.Graphics.FromImage(bmp))
                            {
                                g.Clear(System.Drawing.Color.White);
                                form.Ribbon.Draw(g, 1366, 768, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                                bmp.Save(Path.Combine(artifactDir, "slides_wpf_tabledesign.png"), System.Drawing.Imaging.ImageFormat.Png);
                                bmp.Save("scratch/slides_wpf_tabledesign.png", System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        // Clean up temporary test shapes so slide canvas and ribbon tabs are pristine on initial app view
                        if (form.SlidesView != null && form.SlidesView.ActiveSlide != null)
                        {
                            form.SlidesView.ActiveSlide.Shapes.Clear();
                            // Re-add standard title and subtitle placeholders
                            form.SlidesView.AddSlide();
                            if (form.SlidesView.Presentation.Slides.Count > 1)
                            {
                                form.SlidesView.Presentation.Slides.RemoveAt(0);
                                form.SlidesView.ActiveSlideIndex = 0;
                            }
                            form.SlidesView.SelectedShapeIndex = -1;
                        }

                        // Reset to Home tab and capture full application window (Ribbon + Canvas)
                        form.Ribbon.ActiveRibbonTab = 0;
                        form.Invalidate();
                        form.Refresh();
                        using (var fullBmp = new System.Drawing.Bitmap(1366, 768))
                        {
                            form.DrawToBitmap(fullBmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                            fullBmp.Save(Path.Combine(artifactDir, "slides_appview_verified.png"), System.Drawing.Imaging.ImageFormat.Png);
                            fullBmp.Save("scratch/slides_appview_verified.png", System.Drawing.Imaging.ImageFormat.Png);
                        }

                        // Render Parallelogram with text to verify center alignment, white text, and slant margins
                        if (form.SlidesView != null && form.SlidesView.ActiveSlide != null)
                        {
                            var para = new ShapeNode
                            {
                                Type = ShapeType.Parallelogram,
                                Transform = new Transform2D(400, 260, 560, 320),
                                FillColor = 0x2563EB,
                                TextColor = 0xFFFFFF,
                                Text = "ZenQ Office\nChuẩn PowerPoint 365\nCăn giữa & Cách lề an toàn",
                                FontSize = 24f,
                                Alignment = TextAlignment.Center,
                                VerticalAlignment = VerticalTextAlignment.Middle
                            };
                            form.SlidesView.ActiveSlide.Shapes.Add(para);
                            form.SlidesView.SelectedShapeIndex = form.SlidesView.ActiveSlide.Shapes.Count - 1;
                            form.Invalidate();
                            form.Refresh();
                            using (var shapeBmp = new System.Drawing.Bitmap(1366, 768))
                            {
                                form.DrawToBitmap(shapeBmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                                shapeBmp.Save(Path.Combine(artifactDir, "shape_text_verified.png"), System.Drawing.Imaging.ImageFormat.Png);
                                shapeBmp.Save("scratch/shape_text_verified.png", System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    form.Close();
                }
                File.AppendAllText("render_log.txt", "Finished successfully!\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("render_log.txt", "Render error:\n" + ex.ToString() + "\n");
            }
        }

        private static void VerifyPptx(string pptxPath)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== VERIFY PPTX ===");
            sb.AppendLine("File: " + pptxPath);
            try
            {
                var pres = ZenQOffice.Format.PptxHandler.LoadPptx(pptxPath);
                sb.AppendLine($"Total slides: {pres.Slides.Count}");
                sb.AppendLine($"Canvas: {pres.CanvasWidth} x {pres.CanvasHeight}");

                // Slide 1
                if (pres.Slides.Count > 0)
                {
                    var s1 = pres.Slides[0];
                    sb.AppendLine($"Slide 1 shapes count: {s1.Shapes.Count}");
                    if (s1.Shapes.Count > 0)
                    {
                        var sh = s1.Shapes[0];
                        sb.AppendLine($"Slide 1 Shape 0: Type={sh.Type}, Name='{sh.Name}', X={sh.Transform.X}, Y={sh.Transform.Y}, W={sh.Transform.Width}, H={sh.Transform.Height}");
                    }
                }

                // Slide 3
                if (pres.Slides.Count > 2)
                {
                    var s3 = pres.Slides[2];
                    sb.AppendLine($"Slide 3 shapes count: {s3.Shapes.Count}");
                    int cxn = 0;
                    foreach (var sh in s3.Shapes)
                    {
                        if (sh.Type == ShapeType.Line || sh.Type == ShapeType.Arrow)
                        {
                            cxn++;
                            sb.AppendLine($"  Connector {cxn}: Name='{sh.Name}', Type={sh.Type}, X={sh.Transform.X}, Y={sh.Transform.Y}, W={sh.Transform.Width}, H={sh.Transform.Height}");
                        }
                    }
                }

                // Slide 6
                if (pres.Slides.Count > 5)
                {
                    var s6 = pres.Slides[5];
                    sb.AppendLine($"Slide 6 animations count: {s6.Animations.Count}");
                    foreach (var a in s6.Animations)
                    {
                        var tgt = s6.Shapes.Find(s => s.Id == a.ShapeId);
                        sb.AppendLine($"  Anim: Type={a.Type}, Trigger={a.Trigger}, TargetId='{a.ShapeId}', TargetName='{tgt?.Name}', Text='{tgt?.Text}'");
                    }
                }

                // Slide 18 (Table)
                if (pres.Slides.Count > 17)
                {
                    var s18 = pres.Slides[17];
                    sb.AppendLine($"Slide 18 shapes count: {s18.Shapes.Count}");
                    foreach (var sh in s18.Shapes)
                    {
                        if (sh.Type == ShapeType.Table)
                        {
                            sb.AppendLine($"  Table: Name='{sh.Name}', Rows={sh.TableRows}, Cols={sh.TableColumns}, W={sh.Transform.Width}, H={sh.Transform.Height}");
                            if (sh.TableData != null && sh.TableData.Count > 0)
                            {
                                sb.AppendLine("  Row 0: " + string.Join(" | ", sh.TableData[0]));
                                if (sh.TableData.Count > 1) sb.AppendLine("  Row 1: " + string.Join(" | ", sh.TableData[1]));
                            }
                        }
                    }
                }

                // Slide 21 (Table)
                if (pres.Slides.Count > 20)
                {
                    var s21 = pres.Slides[20];
                    sb.AppendLine($"Slide 21 shapes count: {s21.Shapes.Count}");
                    foreach (var sh in s21.Shapes)
                    {
                        if (sh.Type == ShapeType.Table)
                        {
                            sb.AppendLine($"  Table: Name='{sh.Name}', Rows={sh.TableRows}, Cols={sh.TableColumns}");
                            if (sh.TableData != null && sh.TableData.Count > 0)
                            {
                                sb.AppendLine("  Row 0: " + string.Join(" | ", sh.TableData[0]));
                            }
                        }
                    }
                }

                // Also render screenshots using AppForm to verify visuals
                using (AppForm form = new AppForm())
                {
                    form.Size = new System.Drawing.Size(1366, 768);
                    form.OpenDirectMode(OfficeAppMode.Slides, pptxPath);
                    form.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(500);

                    string artifactDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch");
                    if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

                    // Render Slide 1
                    using (var bmp = new System.Drawing.Bitmap(1366, 768))
                    {
                        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                        bmp.Save(Path.Combine(artifactDir, "verify_slide1.png"), System.Drawing.Imaging.ImageFormat.Png);
                    }

                    // Render Slide 3
                    if (form.SlidesView != null && form.SlidesView.Presentation != null && form.SlidesView.Presentation.Slides.Count > 2)
                    {
                        form.SlidesView.ActiveSlideIndex = 2;
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(300);
                        using (var bmp = new System.Drawing.Bitmap(1366, 768))
                        {
                            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                            bmp.Save(Path.Combine(artifactDir, "verify_slide3_connectors.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }

                    // Render Slide 6
                    if (form.SlidesView != null && form.SlidesView.Presentation != null && form.SlidesView.Presentation.Slides.Count > 5)
                    {
                        form.SlidesView.ActiveSlideIndex = 5;
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(300);
                        using (var bmp = new System.Drawing.Bitmap(1366, 768))
                        {
                            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                            bmp.Save(Path.Combine(artifactDir, "verify_slide6_anim.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }

                    // Render Slide 18
                    if (form.SlidesView != null && form.SlidesView.Presentation != null && form.SlidesView.Presentation.Slides.Count > 17)
                    {
                        form.SlidesView.ActiveSlideIndex = 17;
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(300);
                        using (var bmp = new System.Drawing.Bitmap(1366, 768))
                        {
                            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                            bmp.Save(Path.Combine(artifactDir, "verify_slide18_table.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }

                        // Render Slide 18 in Fullscreen Presenter Mode
                        using (var bmpPres = new System.Drawing.Bitmap(1366, 768))
                        {
                            using (var g = System.Drawing.Graphics.FromImage(bmpPres))
                            {
                                form.SlidesView.DrawFullscreenPresenter(g, 1366, 768, false);
                            }
                            bmpPres.Save(Path.Combine(artifactDir, "verify_slide18_presentation.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }

                    // Render Slide 21
                    if (form.SlidesView != null && form.SlidesView.Presentation != null && form.SlidesView.Presentation.Slides.Count > 20)
                    {
                        form.SlidesView.ActiveSlideIndex = 20;
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(300);
                        using (var bmp = new System.Drawing.Bitmap(1366, 768))
                        {
                            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));
                            bmp.Save(Path.Combine(artifactDir, "verify_slide21_table.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    form.Close();
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("ERROR: " + ex.ToString());
            }

            File.WriteAllText("verify_pptx.log", sb.ToString());
            Console.WriteLine(sb.ToString());
        }
    }

    /// <summary>
    /// Điều phối vòng đời ứng dụng GenOffice: Khởi chạy Màn hình Động (Animated Splash Screen) trước,
    /// sau đó chuyển tiếp mượt mà sang Cửa sổ chính AppForm; hoặc mở thẳng chế độ mong muốn nếu có tham số dòng lệnh.
    /// </summary>
    public class GenOfficeApplicationContext : ApplicationContext
    {
        private GenOfficeSplashScreen _splash;

        public GenOfficeApplicationContext(string[] args = null)
        {
            OfficeAppMode? directMode = null;
            string directFile = null;

            if (args != null && args.Length > 0)
            {
                string first = args[0].Trim();
                if (first.Equals("-calc", StringComparison.OrdinalIgnoreCase) || 
                    first.Equals("-excel", StringComparison.OrdinalIgnoreCase) ||
                    first.Equals("-sheet", StringComparison.OrdinalIgnoreCase))
                {
                    directMode = OfficeAppMode.Calc;
                }
                else if (first.Equals("-word", StringComparison.OrdinalIgnoreCase) || 
                         first.Equals("-writer", StringComparison.OrdinalIgnoreCase) ||
                         first.Equals("-doc", StringComparison.OrdinalIgnoreCase))
                {
                    directMode = OfficeAppMode.Writer;
                }
                else if (first.Equals("-slides", StringComparison.OrdinalIgnoreCase) || 
                         first.Equals("-present", StringComparison.OrdinalIgnoreCase) ||
                         first.Equals("-ppt", StringComparison.OrdinalIgnoreCase))
                {
                    directMode = OfficeAppMode.Slides;
                }
                else if (first.Equals("-pdf", StringComparison.OrdinalIgnoreCase))
                {
                    directMode = OfficeAppMode.Pdf;
                }
                else if (System.IO.File.Exists(first))
                {
                    directFile = first;
                    string ext = System.IO.Path.GetExtension(first).ToLowerInvariant();
                    if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") directMode = OfficeAppMode.Calc;
                    else if (ext == ".pptx" || ext == ".ppt") directMode = OfficeAppMode.Slides;
                    else if (ext == ".pdf") directMode = OfficeAppMode.Pdf;
                    else directMode = OfficeAppMode.Writer;
                }

                if (args.Length > 1 && System.IO.File.Exists(args[1]))
                {
                    directFile = args[1];
                }
            }

            if (directMode.HasValue)
            {
                // MỞ THẲNG NGAY LẬP TỨC: Bỏ qua Splash Screen & toàn bộ animation
                AppForm mainForm = new AppForm();
                MainForm = mainForm;
                mainForm.FormClosed += (s, e) =>
                {
                    ExitThread();
                    try { Environment.Exit(0); } catch { }
                };

                mainForm.OpenDirectMode(directMode.Value, directFile);

                mainForm.Show();
                mainForm.Activate();
                mainForm.BringToFront();
                mainForm.EnsureKeyboardFocus();
                return;
            }

            _splash = new GenOfficeSplashScreen();
            _splash.OnSplashFinished = () =>
            {
                try
                {
                    try
                    {
                        if (_splash != null)
                        {
                            _splash.TopMost = false;
                            _splash.Hide();
                            _splash.Close();
                            _splash.Dispose();
                            _splash = null;
                        }
                    }
                    catch { }

                    AppForm mainForm = new AppForm();
                    MainForm = mainForm;
                    mainForm.FormClosed += (s, e) =>
                    {
                        ExitThread();
                        try { Environment.Exit(0); } catch { }
                    };

                    // Show the form FIRST so Home tab renders immediately
                    mainForm.Show();
                    mainForm.Activate();
                    mainForm.BringToFront();
                    mainForm.EnsureKeyboardFocus();

                    // Defer session restore by 150ms so the form finishes its first paint
                    // before we do any heavy file I/O on the UI thread
                    Timer sessionTimer = new Timer();
                    sessionTimer.Interval = 150;
                    sessionTimer.Tick += (ts, te) =>
                    {
                        try
                        {
                            sessionTimer.Stop();
                            sessionTimer.Dispose();
                            SessionManager.RestoreSessionIntoApp(mainForm);
                            mainForm.Invalidate();
                            mainForm.EnsureKeyboardFocus();
                        }
                        catch { }
                    };
                    sessionTimer.Start();
                }
                catch (Exception ex)
                {
                    ZenQCrashDialogForm.ShowDialogSafe(ex, "Lỗi khởi tạo giao diện", true);
                }
            };
            _splash.Show();
        }
    }
}
