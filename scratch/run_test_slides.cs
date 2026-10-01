using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace FullSlideTestSuite
{
    class Program
    {
        static string DesktopDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GenOffice_Test_All_Modules_Report");
        static string SlideDir = Path.Combine(DesktopDir, "01_SLIDES_TEST");
        static string SlideFilesDir = Path.Combine(SlideDir, "Files");
        static string SlideImagesDir = Path.Combine(SlideDir, "Images");
        static string SlideVideosDir = Path.Combine(SlideDir, "Videos");
        static string SlideFramesDir = Path.Combine(SlideDir, "AnimFrames");

        [STAThread]
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==================================================");
            Console.WriteLine(">>> BẮT ĐẦU KIỂM THỬ PHÂN HỆ SLIDE (PRESENTATION) <<<");
            Console.WriteLine("==================================================");

            Directory.CreateDirectory(SlideFilesDir);
            Directory.CreateDirectory(SlideImagesDir);
            Directory.CreateDirectory(SlideVideosDir);
            Directory.CreateDirectory(SlideFramesDir);

            // 1. TẠO FILE PPTX TỪ GENOFFICE
            Console.WriteLine("[1/5] Tạo file PPTX từ GenOffice...");
            string genPptxPath = Path.Combine(SlideFilesDir, "GenOffice_Created_Presentation.pptx");
            PresentationRoot pres = CreateGenOfficePresentation();
            bool saveOk = PptxHandler.SavePptx(genPptxPath, pres);
            Console.WriteLine("  -> Lưu PPTX: " + (saveOk ? "THÀNH CÔNG" : "THẤT BẠI") + " (" + (File.Exists(genPptxPath) ? new FileInfo(genPptxPath).Length : 0) + " bytes)");

            // 2. TẠO FILE PPTX TỪ MICROSOFT POWERPOINT (COM AUTOMATION)
            Console.WriteLine("[2/5] Tạo file PPTX từ Microsoft PowerPoint COM...");
            string pptPptxPath = Path.Combine(SlideFilesDir, "MSPowerPoint_Created_Presentation.pptx");
            CreatePowerPointViaCOM(pptPptxPath);

            // 3. KIỂM THỬ MỞ 2 CHIỀU (BIDIRECTIONAL COMPATIBILITY)
            Console.WriteLine("[3/5] Kiểm thử mở 2 chiều...");
            TestBidirectionalPptx(genPptxPath, pptPptxPath);

            // 4. CHỤP ẢNH TẤT CẢ CÁC TAB RIBBON VÀ GIAO DIỆN SLIDES
            Console.WriteLine("[4/5] Chụp ảnh toàn bộ giao diện & Ribbon Tabs của Slides...");
            CaptureSlidesRibbonAndViews(pres);

            // 5. RENDER CHUỖI KHUNG HÌNH HOẠT HÌNH & CHUYỂN CẢNH MORPH (60 FPS)
            Console.WriteLine("[5/5] Render chuỗi khung hình Hoạt hình & Morph Transition...");
            RenderSlideAnimationFrames(pres);

            Console.WriteLine("\n[HOÀN THÀNH] Đã hoàn tất kiểm thử Slide!");
        }

        static PresentationRoot CreateGenOfficePresentation()
        {
            PresentationRoot pres = new PresentationRoot();
            pres.CanvasWidth = 1280;
            pres.CanvasHeight = 720;
            pres.IsWidescreen = true;
            pres.Title = "Báo Cáo Tổng Hợp GenOffice Presentation 2026";
            pres.Slides.Clear();

            // Slide 1: Bìa Dark Slate Gradient
            SlideNode s1 = new SlideNode();
            s1.IsGradientBg = true;
            s1.BgColor = unchecked((int)0xFF0F172A);
            s1.BgColor2 = unchecked((int)0xFF1E3A8A);
            s1.BgGradientAngle = 45f;
            s1.Transition = TransitionType.Fade;
            s1.TransitionDuration = 1.0f;

            ShapeNode s1Title = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(100f, 180f, 1080f, 120f),
                Text = "GENOFFICE SLIDES 2026",
                FontName = "Segoe UI",
                FontSize = 44f,
                Bold = true,
                TextColor = unchecked((int)0xFF38BDF8),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = 0x00FFFFFF,
                StrokeWidth = 0f
            };

            ShapeNode s1Sub = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(100f, 320f, 1080f, 100f),
                Text = "NỀN TẢNG THUYẾT TRÌNH THẾ HỆ MỚI\nChuẩn OpenXML PPTX & Hiệu ứng Chuyển cảnh Morph Siêu Mượt",
                FontName = "Segoe UI",
                FontSize = 18f,
                Bold = false,
                TextColor = unchecked((int)0xFFE2E8F0),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = 0x00FFFFFF,
                StrokeWidth = 0f
            };

            ShapeNode s1Badge = new ShapeNode
            {
                Type = ShapeType.RoundedRectangle,
                Transform = new Transform2D(460f, 460f, 360f, 50f),
                Text = "★ PHIÊN BẢN DOANH NGHIỆP 3.5 ★",
                FontName = "Segoe UI",
                FontSize = 13f,
                Bold = true,
                TextColor = unchecked((int)0xFFFFFFFF),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = unchecked((int)0xFF2563EB),
                StrokeColor = unchecked((int)0xFF60A5FA),
                StrokeWidth = 1.5f
            };

            s1.AddShape(s1Title);
            s1.AddShape(s1Sub);
            s1.AddShape(s1Badge);
            pres.Slides.Add(s1);

            // Slide 2: Feature Cards with Morph
            SlideNode s2 = new SlideNode();
            s2.BgColor = unchecked((int)0xFFF8FAFC);
            s2.Transition = TransitionType.Morph;
            s2.MorphMode = MorphType.Objects;
            s2.TransitionDuration = 1.2f;

            ShapeNode s2Header = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(80f, 40f, 1120f, 70f),
                Text = "TÍNH NĂNG ĐỘT PHÁ CỦA GENOFFICE SLIDES",
                FontName = "Segoe UI",
                FontSize = 26f,
                Bold = true,
                TextColor = unchecked((int)0xFF1E293B),
                Alignment = TextAlignment.Left,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = 0x00FFFFFF,
                StrokeWidth = 0f
            };
            s2.AddShape(s2Header);

            string[] cardTitles = new string[] { "Tương thích OpenXML 100%", "Hiệu ứng Morph Siêu Mượt", "Biểu đồ 3D Chuyên Nghiệp" };
            string[] cardTexts = new string[] { 
                "Mở và lưu định dạng PPTX 2 chiều chuẩn xác, bảo toàn trọn vẹn văn bản, bố cục và màu sắc.",
                "Tự động tính toán quỹ đạo nội suy biến hình các đối tượng và văn bản giữa các slide liên tiếp.",
                "Hỗ trợ biểu đồ 3D Cột, Tròn, Miền cùng hệ thống phân tích dữ liệu thông minh thời gian thực."
            };
            int[] cardColors = new int[] { unchecked((int)0xFF3B82F6), unchecked((int)0xFF10B981), unchecked((int)0xFF8B5CF6) };

            for (int i = 0; i < 3; i++)
            {
                float x = 80f + i * 380f;
                ShapeNode card = new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(x, 150f, 360f, 480f),
                    FillColor = unchecked((int)0xFFFFFFFF),
                    StrokeColor = unchecked((int)0xFFE2E8F0),
                    StrokeWidth = 1.5f
                };

                ShapeNode banner = new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(x + 15f, 170f, 330f, 70f),
                    Text = cardTitles[i],
                    FontName = "Segoe UI",
                    FontSize = 14f,
                    Bold = true,
                    TextColor = unchecked((int)0xFFFFFFFF),
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = cardColors[i],
                    StrokeWidth = 0f
                };

                ShapeNode content = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(x + 20f, 260f, 320f, 340f),
                    Text = cardTexts[i],
                    FontName = "Segoe UI",
                    FontSize = 13f,
                    Bold = false,
                    TextColor = unchecked((int)0xFF475569),
                    Alignment = TextAlignment.Left,
                    VerticalAlignment = VerticalTextAlignment.Top,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };

                s2.AddShape(card);
                s2.AddShape(banner);
                s2.AddShape(content);
            }
            pres.Slides.Add(s2);

            // Slide 3: Charts & Tables
            SlideNode s3 = new SlideNode();
            s3.BgColor = unchecked((int)0xFFF1F5F9);
            s3.Transition = TransitionType.Push;

            ShapeNode s3Header = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(80f, 40f, 1120f, 60f),
                Text = "BÁO CÁO DOANH THU & PHÂN TÍCH TĂNG TRƯỞNG",
                FontName = "Segoe UI",
                FontSize = 24f,
                Bold = true,
                TextColor = unchecked((int)0xFF0F172A),
                Alignment = TextAlignment.Left,
                FillColor = 0x00FFFFFF
            };
            s3.AddShape(s3Header);

            ShapeNode s3ChartBox = new ShapeNode
            {
                Type = ShapeType.RoundedRectangle,
                Transform = new Transform2D(80f, 130f, 540f, 480f),
                Text = "📊 BIỂU ĐỒ DOANH THU 3D\n\nQ1: 15.2 Tỷ VNĐ  (+15%)\nQ2: 18.5 Tỷ VNĐ  (+22%)\nQ3: 22.1 Tỷ VNĐ  (+30%)\nQ4: 28.6 Tỷ VNĐ  (+35%)\n\nTổng Doanh thu: 84.4 Tỷ VNĐ",
                FontName = "Segoe UI",
                FontSize = 16f,
                Bold = true,
                TextColor = unchecked((int)0xFF1E293B),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = unchecked((int)0xFFFFFFFF),
                StrokeColor = unchecked((int)0xFFCBD5E1),
                StrokeWidth = 1.5f
            };
            s3.AddShape(s3ChartBox);

            ShapeNode s3TableBox = new ShapeNode
            {
                Type = ShapeType.RoundedRectangle,
                Transform = new Transform2D(660f, 130f, 540f, 480f),
                Text = "📋 BẢNG THỊ PHẦN SẢN PHẨM 2026\n\n• GenOffice Enterprise: 45.2%\n• Cloud Collaboration: 28.5%\n• AI Assistant Pro: 16.3%\n• Security Suite: 10.0%\n\nĐánh giá: Tăng trưởng dẫn đầu thị trường",
                FontName = "Segoe UI",
                FontSize = 16f,
                Bold = true,
                TextColor = unchecked((int)0xFF1E293B),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = unchecked((int)0xFFFFFFFF),
                StrokeColor = unchecked((int)0xFFCBD5E1),
                StrokeWidth = 1.5f
            };
            s3.AddShape(s3TableBox);
            pres.Slides.Add(s3);

            // Slide 4: Neon Cyber Effect
            SlideNode s4 = new SlideNode();
            s4.BgColor = unchecked((int)0xFF090D16);
            s4.Transition = TransitionType.Wipe;

            ShapeNode s4Header = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(80f, 40f, 1120f, 60f),
                Text = "HIỆU ỨNG ÁNH SÁNG NEON CYBER PHÁT QUANG",
                FontName = "Segoe UI",
                FontSize = 24f,
                Bold = true,
                TextColor = unchecked((int)0xFF00F0FF),
                Alignment = TextAlignment.Left,
                FillColor = 0x00FFFFFF
            };
            s4.AddShape(s4Header);

            ShapeNode neonBox = new ShapeNode
            {
                Type = ShapeType.RoundedRectangle,
                Transform = new Transform2D(240f, 160f, 800f, 450f),
                Text = "⚡ NEON GLOW PULSE EFFECT ⚡\n\nPhát sáng đa tầng 60 FPS\nTùy chỉnh màu sắc, bán kính tỏa sáng & độ khuếch tán\nHiển thị siêu mượt trên mọi màn hình",
                FontName = "Segoe UI",
                FontSize = 20f,
                Bold = true,
                TextColor = unchecked((int)0xFFFFFFFF),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = unchecked((int)0xFF1E1B4B),
                StrokeColor = unchecked((int)0xFF00F0FF),
                StrokeWidth = 3f
            };
            s4.AddShape(neonBox);
            pres.Slides.Add(s4);

            // Slide 5: Motion Paths & Vector Badges
            SlideNode s5 = new SlideNode();
            s5.BgColor = unchecked((int)0xFFF8FAFC);
            s5.Transition = TransitionType.Split;

            ShapeNode s5Header = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(80f, 40f, 1120f, 60f),
                Text = "HOẠT HÌNH ĐƯỜNG DẪN CHUYỂN ĐỘNG (MOTION PATHS)",
                FontName = "Segoe UI",
                FontSize = 24f,
                Bold = true,
                TextColor = unchecked((int)0xFF1E293B),
                Alignment = TextAlignment.Left,
                FillColor = 0x00FFFFFF
            };
            s5.AddShape(s5Header);

            ShapeNode starBadge = new ShapeNode
            {
                Type = ShapeType.Star,
                Transform = new Transform2D(540f, 240f, 200f, 200f),
                Text = "GENOFFICE\nEXCELLENCE",
                FontName = "Segoe UI",
                FontSize = 13f,
                Bold = true,
                TextColor = unchecked((int)0xFFFFFFFF),
                Alignment = TextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                FillColor = unchecked((int)0xFFEF4444),
                StrokeColor = unchecked((int)0xFFF87171),
                StrokeWidth = 2f
            };
            s5.AddShape(starBadge);
            pres.Slides.Add(s5);

            return pres;
        }

        static void CreatePowerPointViaCOM(string outPath)
        {
            try
            {
                Type pptType = Type.GetTypeFromProgID("PowerPoint.Application");
                if (pptType == null)
                {
                    Console.WriteLine("  [CẢNH BÁO] Không tìm thấy PowerPoint COM.");
                    return;
                }

                object pptApp = Activator.CreateInstance(pptType);
                object presentations = pptType.InvokeMember("Presentations", BindingFlags.GetProperty, null, pptApp, null);
                object pres = presentations.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, presentations, new object[] { 0 });

                object slides = pres.GetType().InvokeMember("Slides", BindingFlags.GetProperty, null, pres, null);
                
                // Add Slide 1: Title Slide (ppLayoutTitle = 1)
                object s1 = slides.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, slides, new object[] { 1, 1 });
                object shapes1 = s1.GetType().InvokeMember("Shapes", BindingFlags.GetProperty, null, s1, null);
                object title1 = shapes1.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, shapes1, new object[] { 1 });
                object textFrame1 = title1.GetType().InvokeMember("TextFrame", BindingFlags.GetProperty, null, title1, null);
                object textRange1 = textFrame1.GetType().InvokeMember("TextRange", BindingFlags.GetProperty, null, textFrame1, null);
                textRange1.GetType().InvokeMember("Text", BindingFlags.SetProperty, null, textRange1, new object[] { "POWERPOINT OFFICIAL TEST FILE" });

                // Add Slide 2: Blank Slide (ppLayoutBlank = 12) with Shape
                object s2 = slides.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, slides, new object[] { 2, 12 });
                object shapes2 = s2.GetType().InvokeMember("Shapes", BindingFlags.GetProperty, null, s2, null);
                
                object shape2 = shapes2.GetType().InvokeMember("AddShape", BindingFlags.InvokeMethod, null, shapes2, new object[] { 5, 100f, 100f, 500f, 200f });
                object tf2 = shape2.GetType().InvokeMember("TextFrame", BindingFlags.GetProperty, null, shape2, null);
                object tr2 = tf2.GetType().InvokeMember("TextRange", BindingFlags.GetProperty, null, tf2, null);
                tr2.GetType().InvokeMember("Text", BindingFlags.SetProperty, null, tr2, new object[] { "Tạo từ Microsoft PowerPoint 16.0 COM Automation\nKiểm thử khả năng tương thích 2 chiều hoàn hảo với GenOffice" });

                if (File.Exists(outPath)) File.Delete(outPath);
                pres.GetType().InvokeMember("SaveAs", BindingFlags.InvokeMethod, null, pres, new object[] { outPath });
                pres.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, pres, null);
                pptApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, pptApp, null);

                Marshal.ReleaseComObject(pres);
                Marshal.ReleaseComObject(pptApp);

                Console.WriteLine("  -> Đã tạo thành công file PPTX từ MS PowerPoint: " + outPath + " (" + new FileInfo(outPath).Length + " bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI COM PPT] " + ex.Message);
            }
        }

        static void TestBidirectionalPptx(string genPptxPath, string pptPptxPath)
        {
            // 1. Mở file của GenOffice trong PowerPoint COM & Export slide screenshots
            try
            {
                Type pptType = Type.GetTypeFromProgID("PowerPoint.Application");
                if (pptType != null && File.Exists(genPptxPath))
                {
                    object pptApp = Activator.CreateInstance(pptType);
                    object presentations = pptType.InvokeMember("Presentations", BindingFlags.GetProperty, null, pptApp, null);
                    object pres = presentations.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, presentations, new object[] { genPptxPath, 0, 0, 0 });
                    
                    object slides = pres.GetType().InvokeMember("Slides", BindingFlags.GetProperty, null, pres, null);
                    int count = (int)slides.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, slides, null);
                    Console.WriteLine("  [THÀNH CÔNG] PowerPoint đã mở file PPTX của GenOffice! Số lượng slide: " + count);

                    for (int i = 1; i <= Math.Min(count, 3); i++)
                    {
                        object slide = slides.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, slides, new object[] { i });
                        string exportImg = Path.Combine(SlideImagesDir, "Roundtrip_GenOffice_in_PowerPoint_Slide_" + i + ".png");
                        slide.GetType().InvokeMember("Export", BindingFlags.InvokeMethod, null, slide, new object[] { exportImg, "PNG", 1280, 720 });
                    }

                    pres.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, pres, null);
                    pptApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, pptApp, null);
                    Marshal.ReleaseComObject(pptApp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI TEST PPT COM] " + ex.Message);
            }

            // 2. Mở file của PowerPoint trong GenOffice
            try
            {
                if (File.Exists(pptPptxPath))
                {
                    PresentationRoot loadedPres = PptxHandler.LoadPptx(pptPptxPath);
                    if (loadedPres != null)
                    {
                        Console.WriteLine("  [THÀNH CÔNG] GenOffice đã nạp thành công file PPTX của PowerPoint! Số slide: " + loadedPres.Slides.Count);
                        string roundtripBack = Path.Combine(SlideFilesDir, "Roundtrip_MSPowerPoint_re-saved_by_GenOffice.pptx");
                        PptxHandler.SavePptx(roundtripBack, loadedPres);
                        Console.WriteLine("  -> Đã lưu lại bản roundtrip: " + roundtripBack);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI GENOFFICE LOAD PPTX] " + ex.Message);
            }
        }

        static void CaptureSlidesRibbonAndViews(PresentationRoot pres)
        {
            try
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(1366, 800);
                    form.Ribbon.CurrentMode = OfficeAppMode.Slides;
                    form.SlidesView.Presentation = pres;
                    form.SlidesView.ActiveSlideIndex = 0;

                    string[] tabNames = new string[] {
                        "01_Home_TrangDau",
                        "02_Insert_Chen",
                        "03_Design_ThietKe",
                        "04_Transitions_ChuyenTiep",
                        "05_Animations_HoatHinh",
                        "06_SlideShow_TrinhChieu",
                        "07_Review_XemLai",
                        "08_View_Xem"
                    };

                    for (int i = 0; i < tabNames.Length; i++)
                    {
                        form.Ribbon.ActiveRibbonTab = i;
                        using (Bitmap bmp = new Bitmap(1366, 800))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.SlidesView.Draw(g, 1366, 800);
                                form.Ribbon.Draw(g, 1366, 800, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            string imgPath = Path.Combine(SlideImagesDir, "Ribbon_Tab_" + tabNames[i] + ".png");
                            bmp.Save(imgPath, ImageFormat.Png);
                        }
                    }

                    // Render all 5 slides in HD
                    for (int i = 0; i < pres.Slides.Count; i++)
                    {
                        form.SlidesView.ActiveSlideIndex = i;
                        using (Bitmap bmp = new Bitmap(1366, 800))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.SlidesView.Draw(g, 1366, 800);
                                form.Ribbon.Draw(g, 1366, 800, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            string imgPath = Path.Combine(SlideImagesDir, "GenOffice_Slide_View_Slide_" + (i + 1) + ".png");
                            bmp.Save(imgPath, ImageFormat.Png);
                        }
                    }

                    // Capture Presenter View Mode
                    form.SlidesView.IsPresenterViewMode = true;
                    form.SlidesView.IsPresenting = true;
                    using (Bitmap bmp = new Bitmap(1366, 800))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.SlidesView.DrawFullscreenPresenter(g, 1366, 800, true);
                        }
                        string imgPath = Path.Combine(SlideImagesDir, "Slides_Presenter_View_Mode.png");
                        bmp.Save(imgPath, ImageFormat.Png);
                    }
                }

                Console.WriteLine("  -> Đã chụp toàn bộ ảnh Ribbon tabs, Slide views và Presenter view!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI CAPTURE SLIDES] " + ex.Message);
            }
        }

        static void RenderSlideAnimationFrames(PresentationRoot pres)
        {
            try
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(1280, 720);
                    form.Ribbon.CurrentMode = OfficeAppMode.Slides;
                    form.SlidesView.Presentation = pres;
                    form.SlidesView.IsPresenting = true;

                    // 1. Render Morph Transition frames (Slide 1 -> Slide 2)
                    form.SlidesView.ActiveSlideIndex = 1;
                    form.SlidesView.IsTransitioning = true;
                    form.SlidesView.TransitionFromSlideIndex = 0;
                    form.SlidesView.TransitionToSlideIndex = 1;

                    int frameCount = 60; // 60 frames = 1 second at 60fps
                    for (int f = 0; f < frameCount; f++)
                    {
                        form.SlidesView.TransitionProgress = (float)f / (float)(frameCount - 1);
                        using (Bitmap bmp = new Bitmap(1280, 720))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.SlidesView.DrawFullscreenPresenter(g, 1280, 720, false);
                            }
                            string framePath = Path.Combine(SlideFramesDir, string.Format("morph_frame_{0:D3}.png", f));
                            bmp.Save(framePath, ImageFormat.Png);
                        }
                    }
                    form.SlidesView.IsTransitioning = false;

                    // 2. Render Animation Preview frames (Slide 4 Neon / Slide 5 Star)
                    form.SlidesView.ActiveSlideIndex = 3;
                    form.SlidesView.IsPlayingAnimationPreview = true;
                    for (int f = 0; f < 40; f++)
                    {
                        form.SlidesView.AnimationProgress = (float)f / 40f;
                        using (Bitmap bmp = new Bitmap(1280, 720))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.SlidesView.DrawFullscreenPresenter(g, 1280, 720, false);
                            }
                            string framePath = Path.Combine(SlideFramesDir, string.Format("anim_frame_{0:D3}.png", f));
                            bmp.Save(framePath, ImageFormat.Png);
                        }
                    }

                    Console.WriteLine("  -> Đã xuất " + (frameCount + 40) + " khung hình hiệu ứng Morph và Animation chất lượng cao!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI RENDER FRAMES] " + ex.Message);
            }
        }
    }
}
