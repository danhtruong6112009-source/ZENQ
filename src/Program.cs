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
                FluentUi.Initialize();

                try { FluentIconRenderer.EnsureFontLoaded(); } catch { }
                try { DatabaseAuthService.Initialize(); } catch { }
                try { CloudShareService.Initialize(); } catch { }



                if (args != null && args.Length > 0 && args[0].Equals("--render-slide", StringComparison.OrdinalIgnoreCase))

                {

                    string pptxPath = args.Length > 1 ? args[1] : "";

                    int slideIdx = args.Length > 2 ? int.Parse(args[2]) : 0;

                    string outPath = args.Length > 3 ? args[3] : "slide_out.png";

                    try

                    {

                        var pres = ZenQOffice.Format.PptxHandler.LoadPptx(pptxPath);

                        if (pres != null && pres.Slides.Count > slideIdx)

                        {

                            var s = pres.Slides[slideIdx];

                            var sv = new SlidesView();

                            {

                                sv.Presentation = pres;

                                sv.ActiveSlideIndex = slideIdx;

                                using (var bmp = sv.RenderSlideToBitmap(s, 1920, 1080))

                                {

                                    bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);

                                    Console.WriteLine("SAVED: " + outPath);

                                }

                            }

                        }

                    }

                    catch (Exception ex)

                    {

                        Console.WriteLine("ERROR: " + ex.Message);

                    }

                    return;

                }



                if (args != null && args.Length > 0 && args[0].Equals("--verify-pptx", StringComparison.OrdinalIgnoreCase))

                {

                    VerifyPptx(args.Length > 1 ? args[1] : "sample.pptx");

                    return;

                }



                if (args != null && args.Length > 0 && args[0].Equals("--test-morph", StringComparison.OrdinalIgnoreCase))

                {

                    TestMorphTransition(args.Length > 1 ? args[1] : @"C:\Users\Danh\Downloads\Trắng Đỏ Và Vàng Cổ Điển Bắc Kinh Bài Thuyết Trình.pptx");

                    return;

                }



                if (args != null && args.Length > 0 && args[0].Equals("--test-slide-ai", StringComparison.OrdinalIgnoreCase))

                {

                    TestSlideAiGeneration();

                    return;

                }



                if (args != null && args.Length > 0 && args[0].Equals("--test-offslide", StringComparison.OrdinalIgnoreCase))

                {

                    TestOffslideScroll();

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

                    HandleSafeException(e.Exception, AppL10n.T("Sự cố giao diện", "UI Exception"), false);

                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>

                {

                    Exception ex = e.ExceptionObject as Exception;

                    HandleSafeException(ex, AppL10n.T("Sự cố hệ thống", "System Exception"), true);

                };



                Application.Run(new GenOfficeApplicationContext(args));

            }

            catch (Exception ex)

            {

                HandleSafeException(ex, AppL10n.T("Lỗi khởi động ZENQ OFFICE", "ZENQ OFFICE Startup Error"), true);

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

                                Text = "ZenQ Office\nChuẩn ZenQ Slides\nCăn giữa & Cách lề an toàn",

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



        private static void TestOffslideScroll()

        {

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== TEST OFFLIDE SCROLL ===");

            try

            {

                using (AppForm form = new AppForm())

                {

                    form.Size = new System.Drawing.Size(1366, 768);

                    form.OpenDirectMode(OfficeAppMode.Slides, null);

                    form.Show();

                    Application.DoEvents();



                    var sv = form.SlidesView;

                    int canvasW = 1000;

                    int canvasH = 550;

                    int minX, maxX, minY, maxY;



                    // 1. Initial blank slide (all inside)

                    sv.GetScrollLimits(canvasW, canvasH, out minX, out maxX, out minY, out maxY);

                    sb.AppendLine(string.Format("Step 1 (Blank slide): minX={0}, maxX={1}, minY={2}, maxY={3}", minX, maxX, minY, maxY));

                    bool initialClean = (minX == 0 && maxX == 0 && minY == 0 && maxY == 0);

                    sb.AppendLine("Initial clean check: " + (initialClean ? "PASSED" : "FAILED"));



                    // 2. Add shape far right (X = 2600)

                    var rightShape = new ShapeNode

                    {

                        Type = ShapeType.Rectangle,

                        FillColor = 0xFF5533,

                        Text = "Far Right Item",

                        Transform = new Transform2D { X = 2600, Y = 300, Width = 300, Height = 150 }

                    };

                    sv.ActiveSlide.Shapes.Add(rightShape);

                    sv.GetScrollLimits(canvasW, canvasH, out minX, out maxX, out minY, out maxY);

                    sb.AppendLine(string.Format("Step 2 (Item far right): minX={0}, maxX={1}, minY={2}, maxY={3}", minX, maxX, minY, maxY));

                    bool rightPassed = (maxX > 100);

                    sb.AppendLine("Far right check: " + (rightPassed ? "PASSED" : "FAILED"));



                    // 3. Add shape far left (X = -800)

                    var leftShape = new ShapeNode

                    {

                        Type = ShapeType.Rectangle,

                        FillColor = 0x3355FF,

                        Text = "Far Left Item",

                        Transform = new Transform2D { X = -800, Y = 300, Width = 300, Height = 150 }

                    };

                    sv.ActiveSlide.Shapes.Add(leftShape);

                    sv.GetScrollLimits(canvasW, canvasH, out minX, out maxX, out minY, out maxY);

                    sb.AppendLine(string.Format("Step 3 (Item far left): minX={0}, maxX={1}, minY={2}, maxY={3}", minX, maxX, minY, maxY));

                    bool leftPassed = (minX < -100);

                    sb.AppendLine("Far left check: " + (leftPassed ? "PASSED" : "FAILED"));



                    // 4. Add shape far bottom (Y = 1800)

                    var btmShape = new ShapeNode

                    {

                        Type = ShapeType.Rectangle,

                        FillColor = 0x33AA55,

                        Text = "Far Bottom Item",

                        Transform = new Transform2D { X = 400, Y = 1800, Width = 300, Height = 150 }

                    };

                    sv.ActiveSlide.Shapes.Add(btmShape);

                    sv.GetScrollLimits(canvasW, canvasH, out minX, out maxX, out minY, out maxY);

                    sb.AppendLine(string.Format("Step 4 (Item far bottom): minX={0}, maxX={1}, minY={2}, maxY={3}", minX, maxX, minY, maxY));

                    bool btmPassed = (maxY > 100);

                    sb.AppendLine("Far bottom check: " + (btmPassed ? "PASSED" : "FAILED"));



                    // 5. Add shape far top (Y = -600)

                    var topShape = new ShapeNode

                    {

                        Type = ShapeType.Rectangle,

                        FillColor = 0xAA33AA,

                        Text = "Far Top Item",

                        Transform = new Transform2D { X = 400, Y = -600, Width = 300, Height = 150 }

                    };

                    sv.ActiveSlide.Shapes.Add(topShape);

                    sv.GetScrollLimits(canvasW, canvasH, out minX, out maxX, out minY, out maxY);

                    sb.AppendLine(string.Format("Step 5 (Item far top): minX={0}, maxX={1}, minY={2}, maxY={3}", minX, maxX, minY, maxY));

                    bool topPassed = (minY < -100);

                    sb.AppendLine("Far top check: " + (topPassed ? "PASSED" : "FAILED"));



                    // 6. Test scrolling to right shape and rendering screenshot

                    sv.CanvasScrollX = maxX;

                    sv.CanvasScrollY = 0;

                    string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "offslide_test");

                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);



                    using (var bmp = new System.Drawing.Bitmap(1366, 768))

                    {

                        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));

                        string fn = Path.Combine(outDir, "scrolled_far_right.png");

                        bmp.Save(fn, System.Drawing.Imaging.ImageFormat.Png);

                        sb.AppendLine("Saved scrolled_far_right screenshot: " + fn);

                    }



                    // 7. Test scrolling to bottom shape and rendering screenshot

                    sv.CanvasScrollX = 0;

                    sv.CanvasScrollY = maxY;

                    using (var bmp = new System.Drawing.Bitmap(1366, 768))

                    {

                        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));

                        string fn = Path.Combine(outDir, "scrolled_far_bottom.png");

                        bmp.Save(fn, System.Drawing.Imaging.ImageFormat.Png);

                        sb.AppendLine("Saved scrolled_far_bottom screenshot: " + fn);

                    }



                    form.Close();

                }

            }

            catch (Exception ex)

            {

                sb.AppendLine("ERROR: " + ex.ToString());

            }



            File.WriteAllText("test_offslide.log", sb.ToString());

            Console.WriteLine(sb.ToString());

        }



        private static void TestMorphTransition(string pptxPath)

        {

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== TEST MORPH TRANSITION ===");

            sb.AppendLine("File: " + pptxPath);

            try

            {

                using (AppForm form = new AppForm())

                {

                    form.Size = new System.Drawing.Size(1920, 1080);

                    form.OpenDirectMode(OfficeAppMode.Slides, pptxPath);

                    form.Show();

                    Application.DoEvents();



                    var sv = form.SlidesView;

                    if (sv == null || sv.Presentation == null || sv.Presentation.Slides.Count < 2)

                    {

                        sb.AppendLine("SlidesView or Presentation not loaded or < 2 slides");

                        File.WriteAllText("test_morph.log", sb.ToString());

                        return;

                    }



                    string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "morph_frames");

                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);



                    // Force slide 1 transition to Morph

                    sv.Presentation.Slides[1].Transition = TransitionType.Morph;

                    sv.TransitionFromSlideIndex = 0;

                    sv.TransitionToSlideIndex = 1;

                    sv.IsTransitioning = true;

                    sv.IsPresenting = true;



                    float[] steps = new float[] { 0.0f, 0.1f, 0.25f, 0.5f, 0.75f, 0.9f, 0.98f, 1.0f };

                    foreach (float p in steps)

                    {

                        sv.TransitionProgress = p;

                        using (var bmp = new System.Drawing.Bitmap(1920, 1080))

                        {

                            using (var g = System.Drawing.Graphics.FromImage(bmp))

                            {

                                sv.DrawFullscreenPresenter(g, 1920, 1080, false);

                            }

                            string filename = Path.Combine(outDir, string.Format("morph_frame_{0:0.00}.png", p));

                            bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);

                            sb.AppendLine(string.Format("Rendered progress {0:0.00} -> {1}", p, filename));

                        }

                    }

                    form.Close();

                }

            }

            catch (Exception ex)

            {

                sb.AppendLine("ERROR: " + ex.ToString());

            }

            File.WriteAllText("test_morph.log", sb.ToString());

            Console.WriteLine(sb.ToString());

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



                    // Render Slide 2

                    if (form.SlidesView != null && form.SlidesView.Presentation != null && form.SlidesView.Presentation.Slides.Count > 1)

                    {

                        form.SlidesView.ActiveSlideIndex = 1;

                        Application.DoEvents();

                        System.Threading.Thread.Sleep(300);

                        using (var bmp = new System.Drawing.Bitmap(1366, 768))

                        {

                            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, 1366, 768));

                            bmp.Save(Path.Combine(artifactDir, "verify_slide2.png"), System.Drawing.Imaging.ImageFormat.Png);

                        }

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

        static void TestSlideAiGeneration()

        {

            Console.WriteLine("=== TEST SLIDE AI GENERATION ===");

            string query1 = "Tạo 1 slide ngay tại chỗ tôi đang đứng về 'Kỷ Niệm 51 Năm Ngày Giải Phóng Miền Nam _Quốc Khánh 2.9' phong cách Canva sắc nét";

            string prompt1 = SlideDesignMetadata.BuildSlidePrompt(query1, 1, 0, "Slide 1");

            Console.WriteLine("Prompt length: " + prompt1.Length);

            Console.WriteLine("Contains VietnamCrimsonGold: " + prompt1.Contains("VietnamCrimsonGold"));

            Console.WriteLine("Contains mode=\"update\": " + prompt1.Contains("mode=\"update\""));



            using (var form = new AppForm())

            {

                form.Width = 1366;

                form.Height = 768;

                form.OpenDirectMode(OfficeAppMode.Slides, null);

                form.Show();

                Application.DoEvents();



                // Test Cover XML execution

                string sampleCoverXml = @"<slide title=""Kỷ Niệm 51 Năm Ngày Giải Phóng Miền Nam &amp; Quốc Khánh 2.9"" bg=""gradient:#2B0000,#7F1D1D,135"" theme=""VietnamCrimsonGold"" transition=""Morph"" mode=""update"">

  <shape type=""circle"" x=""-120"" y=""-120"" w=""500"" h=""500"" fill=""#F6C445"" opacity=""0.12"" />

  <shape type=""circle"" x=""1600"" y=""700"" w=""550"" h=""550"" fill=""#F6C445"" opacity=""0.10"" />

  <shape type=""rounded_rectangle"" x=""130"" y=""130"" w=""440"" h=""46"" fill=""#1A0303"" stroke=""#F6C445"" stroke_width=""1.5"" radius=""23"" />

  <text x=""130"" y=""135"" w=""440"" h=""38"" font_size=""20"" bold=""true"" color=""#F6C445"" align=""center"">★ HÀO KHÍ NON SÔNG VIỆT NAM ★</text>

  <text x=""130"" y=""220"" w=""1660"" h=""260"" font_size=""60"" bold=""true"" color=""#FFFFFF"" align=""left"">KỶ NIỆM 51 NĂM NGÀY GIẢI PHÓNG MIỀN NAM &amp; QUỐC KHÁNH 2.9</text>

  <shape type=""rectangle"" x=""130"" y=""500"" w=""160"" h=""6"" fill=""#F6C445"" />

  <text x=""130"" y=""530"" w=""1400"" h=""100"" font_size=""28"" color=""#FEF08A"" align=""left"">Hào khí thống nhất non sông, vững bước tiến vào kỷ nguyên hội nhập và thịnh vượng trường tồn.</text>

  <shape type=""rounded_rectangle"" x=""130"" y=""780"" w=""720"" h=""120"" fill=""#1A0303"" stroke=""#991B1B"" stroke_width=""1.0"" radius=""16"" />

  <shape type=""circle"" x=""160"" y=""805"" w=""70"" h=""70"" fill=""#F6C445"" />

  <text x=""160"" y=""820"" w=""70"" h=""40"" font_size=""24"" bold=""true"" color=""#7F1D1D"" align=""center"">VN</text>

  <text x=""250"" y=""800"" w=""560"" h=""40"" font_size=""24"" bold=""true"" color=""#FFFFFF"">Ban Tuyên Giáo &amp; Truyền Thông</text>

  <text x=""250"" y=""845"" w=""560"" h=""35"" font_size=""20"" color=""#FCA5A5"">Chương trình Kỷ niệm Đại lễ Dân tộc • Năm 2026</text>

</slide>";



                var result = AiActionEngine.ProcessAndExecute(form, sampleCoverXml);

                Console.WriteLine("Action executed: " + result.HasAction + ", summary: " + result.ExecutionSummary);

                Application.DoEvents();



                string artifactDir = @"C:\Users\Danh\.gemini\antigravity\brain\c59ab960-1014-4893-9c40-c3b88069a236";

                var curS = form.SlidesView.ActiveSlide;

                using (var bmp = form.SlidesView.RenderSlideToBitmap(curS, 1920, 1080))

                {

                    string outPath = Path.Combine(artifactDir, "canva_cover_slide_ai_generated.png");

                    bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);

                    Console.WriteLine("Saved screenshot to: " + outPath);

                }



                // Test 3 Cards Content XML execution

                string sampleCardsXml = @"<slide title=""Ba Trụ Cột Đột Phá"" bg=""gradient:#0B0F19,#1E293B,135"" theme=""ModernBlue"" transition=""Morph"">

  <shape type=""rounded_rectangle"" x=""130"" y=""80"" w=""260"" h=""40"" fill=""#131C2E"" stroke=""#38BDF8"" stroke_width=""1.5"" radius=""20"" />

  <text x=""130"" y=""85"" w=""260"" h=""30"" font_size=""18"" bold=""true"" color=""#38BDF8"" align=""center"">TRỌNG TÂM CHIẾN LƯỢC</text>

  <text x=""130"" y=""135"" w=""1660"" h=""70"" font_size=""48"" bold=""true"" color=""#FFFFFF"">BA TRỤ CỘT ĐỘT PHÁ NĂM 2026</text>

  <shape type=""rectangle"" x=""130"" y=""215"" w=""120"" h=""4"" fill=""#38BDF8"" />

  <shape type=""rounded_rectangle"" x=""130"" y=""260"" w=""520"" h=""600"" fill=""#131C2E"" stroke=""#334155"" stroke_width=""1.5"" radius=""16"" />

  <shape type=""rounded_rectangle"" x=""170"" y=""300"" w=""70"" h=""70"" fill=""#38BDF8"" radius=""14"" />

  <text x=""170"" y=""315"" w=""70"" h=""40"" font_size=""28"" bold=""true"" color=""#000000"" align=""center"">01</text>

  <text x=""170"" y=""400"" w=""440"" h=""60"" font_size=""32"" bold=""true"" color=""#38BDF8"">Đổi Mới Công Nghệ</text>

  <text x=""170"" y=""470"" w=""440"" h=""240"" font_size=""22"" color=""#D1D5DB"">Ứng dụng trí tuệ nhân tạo toàn diện vào quy trình sản xuất, tự động hóa 80% tác vụ văn phòng.</text>

  <shape type=""rounded_rectangle"" x=""170"" y=""740"" w=""220"" h=""44"" fill=""#0F172A"" stroke=""#38BDF8"" stroke_width=""1.0"" radius=""10"" />

  <text x=""170"" y=""750"" w=""220"" h=""30"" font_size=""18"" bold=""true"" color=""#38BDF8"" align=""center"">+150% HIỆU QUẢ</text>

  <shape type=""rounded_rectangle"" x=""700"" y=""260"" w=""520"" h=""600"" fill=""#131C2E"" stroke=""#334155"" stroke_width=""1.5"" radius=""16"" />

  <shape type=""rounded_rectangle"" x=""740"" y=""300"" w=""70"" h=""70"" fill=""#38BDF8"" radius=""14"" />

  <text x=""740"" y=""315"" w=""70"" h=""40"" font_size=""28"" bold=""true"" color=""#000000"" align=""center"">02</text>

  <text x=""740"" y=""400"" w=""440"" h=""60"" font_size=""32"" bold=""true"" color=""#38BDF8"">Phát Triển Con Người</text>

  <text x=""740"" y=""470"" w=""440"" h=""240"" font_size=""22"" color=""#D1D5DB"">Đào tạo nhân sự chất lượng cao, xây dựng văn hóa học tập suốt đời và khuyến khích sáng tạo cá nhân.</text>

  <shape type=""rounded_rectangle"" x=""740"" y=""740"" w=""220"" h=""44"" fill=""#0F172A"" stroke=""#38BDF8"" stroke_width=""1.0"" radius=""10"" />

  <text x=""740"" y=""750"" w=""220"" h=""30"" font_size=""18"" bold=""true"" color=""#38BDF8"" align=""center"">100% ĐÀO TẠO</text>

  <shape type=""rounded_rectangle"" x=""1270"" y=""260"" w=""520"" h=""600"" fill=""#131C2E"" stroke=""#334155"" stroke_width=""1.5"" radius=""16"" />

  <shape type=""rounded_rectangle"" x=""1310"" y=""300"" w=""70"" h=""70"" fill=""#38BDF8"" radius=""14"" />

  <text x=""1310"" y=""315"" w=""70"" h=""40"" font_size=""28"" bold=""true"" color=""#000000"" align=""center"">03</text>

  <text x=""1310"" y=""400"" w=""440"" h=""60"" font_size=""32"" bold=""true"" color=""#38BDF8"">Mở Rộng Thị Trường</text>

  <text x=""1310"" y=""470"" w=""440"" h=""240"" font_size=""22"" color=""#D1D5DB"">Thâm nhập thị trường khu vực Đông Nam Á, xây dựng mạng lưới đối tác chiến lược bền vững.</text>

  <shape type=""rounded_rectangle"" x=""1310"" y=""740"" w=""220"" h=""44"" fill=""#0F172A"" stroke=""#38BDF8"" stroke_width=""1.0"" radius=""10"" />

  <text x=""1310"" y=""750"" w=""220"" h=""30"" font_size=""18"" bold=""true"" color=""#38BDF8"" align=""center"">TOP 5 KHU VỰC</text>

</slide>";



                var result2 = AiActionEngine.ProcessAndExecute(form, sampleCardsXml);

                Console.WriteLine("Cards executed: " + result2.HasAction + ", summary: " + result2.ExecutionSummary);

                Application.DoEvents();



                var curS2 = form.SlidesView.ActiveSlide;

                using (var bmp2 = form.SlidesView.RenderSlideToBitmap(curS2, 1920, 1080))

                {

                    string outPath2 = Path.Combine(artifactDir, "canva_cards_slide_ai_generated.png");

                    bmp2.Save(outPath2, System.Drawing.Imaging.ImageFormat.Png);

                    Console.WriteLine("Saved screenshot to: " + outPath2);

                }

            }

            Console.WriteLine("=== TEST COMPLETED SUCCESSFULLY ===");

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



            _splash = new GenOfficeSplashScreen(directMode, directFile);

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

                        try { ZenQOffice.Core.TempCleanupManager.CleanupOnAppExit(); } catch { }

                        ExitThread();

                        try { Environment.Exit(0); } catch { }

                    };



                    if (directMode.HasValue)

                    {

                        mainForm.OpenDirectMode(directMode.Value, directFile);

                        mainForm.Show();

                        mainForm.Activate();

                        mainForm.BringToFront();

                        mainForm.EnsureKeyboardFocus();

                    }

                    else

                    {

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

                }

                catch (Exception ex)

                {

                    ZenQCrashDialogForm.ShowDialogSafe(ex, AppL10n.T("Lỗi khởi tạo giao diện", "UI Initialization Error"), true);

                }

            };



            _splash.Show();

        }

        }

}

