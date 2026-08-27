using System;
using System.IO;
using System.Drawing;
using GenOffice.Core;
using GenOffice.Format;

namespace GenOffice.Test
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("    KIỂM THỬ ĐỘNG CƠ MICROSOFT POWERPOINT 2016-365 (MORPH & OPENXML PPTX)");
            Console.WriteLine("================================================================================");

            // 1. Khởi tạo bản trình chiếu PowerPoint chuẩn
            PresentationRoot pres = new PresentationRoot();
            pres.SetSlideSize(true); // 16:9 Widescreen (1920x1080)
            pres.CurrentTheme = "Office";
            pres.Slides.Clear();

            // Slide 1: Slide Tiêu đề với hiệu ứng Morph
            SlideNode slide1 = new SlideNode();
            pres.Slides.Add(slide1);
            slide1.Title = "Bản Trình Chiếu Morph Đỉnh Cao";
            slide1.BgColor = 0xFFFFFF;
            slide1.Transition = TransitionType.Morph;
            slide1.MorphMode = MorphType.Objects;
            slide1.TransitionDuration = 1.25f;

            ShapeNode shape1 = new ShapeNode
            {
                Type = ShapeType.Rectangle,
                Transform = new Transform2D(200f, 300f, 400f, 300f),
                Text = "Khối Hình A (Morph Start)",
                MorphName = "!!morph_box",
                FillColor = 0x2563EB, // Royal Blue
                TextColor = 0xFFFFFF,
                FontSize = 24f,
                Bold = true
            };
            slide1.AddShape(shape1);

            // Thêm animation cho shape1
            ShapeAnimation anim1 = new ShapeAnimation
            {
                ShapeId = shape1.Id,
                Type = AnimationType.FlyIn,
                Trigger = AnimationTrigger.OnClick,
                Duration = 0.6f
            };
            slide1.Animations.Add(anim1);

            // Slide 2: Slide Đích chuyển hóa Morph
            pres.AddSlide(SlideLayoutType.TitleAndContent);
            SlideNode slide2 = pres.Slides[1];
            slide2.Title = "Biến Hình Sang Vị Trí Mới";
            slide2.BgColor = 0x0F172A; // Deep Navy Dark
            slide2.Transition = TransitionType.Morph;
            slide2.MorphMode = MorphType.Objects;
            slide2.TransitionDuration = 1.5f;

            ShapeNode shape2 = new ShapeNode
            {
                Type = ShapeType.RoundedRectangle,
                Transform = new Transform2D(1200f, 400f, 550f, 450f),
                Text = "Khối Hình A (Morph End Transformed)",
                MorphName = "!!morph_box",
                FillColor = 0xD24726, // Terracotta PowerPoint Orange
                TextColor = 0xFFFFFF,
                FontSize = 28f,
                Bold = true
            };
            slide2.AddShape(shape2);

            // 2. Kiểm thử Lưu tệp OpenXML .pptx
            string pptxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_output.pptx");
            Console.WriteLine("[1/3] Đang lưu bản trình chiếu chuẩn OpenXML .pptx: " + pptxPath);
            bool saveResult = PptxHandler.SavePptx(pptxPath, pres);
            if (!saveResult || !File.Exists(pptxPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=> [THẤT BẠI] Không thể tạo tệp .pptx!");
                Console.ResetColor();
                return 1;
            }
            FileInfo fi = new FileInfo(pptxPath);
            Console.WriteLine("=> [THÀNH CÔNG] Đã tạo tệp .pptx dung lượng: " + fi.Length + " bytes.");

            // 3. Kiểm thử Đọc lại tệp .pptx (Round-trip Verification)
            Console.WriteLine("[2/3] Đang đọc lại và phân tích tệp .pptx vừa tạo...");
            using (FileStream fs = new FileStream(pptxPath, FileMode.Open, FileAccess.Read))
            using (System.IO.Compression.ZipArchive arch = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read))
            {
                Console.WriteLine("Các tệp nén trong .pptx (" + arch.Entries.Count + " entries):");
                foreach (var ent in arch.Entries)
                {
                    Console.WriteLine("  - Entry: [" + ent.FullName + "] (" + ent.Length + " bytes)");
                }
            }

            PresentationRoot loadedPres;
            bool loadResult = PptxHandler.LoadPptx(pptxPath, out loadedPres);
            if (!loadResult || loadedPres == null || loadedPres.Slides.Count != 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=> [THẤT BẠI] Lỗi khi nạp lại tệp .pptx! (loadResult=" + loadResult + ", count=" + (loadedPres != null ? loadedPres.Slides.Count : 0) + ")");
                Console.ResetColor();
                return 2;
            }
            Console.WriteLine("=> [THÀNH CÔNG] Đã nạp thành công " + loadedPres.Slides.Count + " slides từ tệp .pptx.");

            // 4. Xác minh Hiệu ứng Morph và thuộc tính
            SlideNode loadedSlide1 = loadedPres.Slides[0];
            SlideNode loadedSlide2 = loadedPres.Slides[1];

            bool morphOk1 = (loadedSlide1.Transition == TransitionType.Morph);
            bool morphOk2 = (loadedSlide2.Transition == TransitionType.Morph);
            bool morphModeOk = (loadedSlide1.MorphMode == MorphType.Objects);
            bool shapeMatchOk = (loadedSlide1.Shapes.Count > 0 && loadedSlide2.Shapes.Count > 0);

            Console.WriteLine("[3/3] Xác minh thông số:");
            Console.WriteLine("   - Slide 1 Transition = " + loadedSlide1.Transition + " (Expected: Morph) -> " + (morphOk1 ? "PASS" : "FAIL"));
            Console.WriteLine("   - Slide 2 Transition = " + loadedSlide2.Transition + " (Expected: Morph) -> " + (morphOk2 ? "PASS" : "FAIL"));
            Console.WriteLine("   - Morph Sub-mode = " + loadedSlide1.MorphMode + " -> " + (morphModeOk ? "PASS" : "FAIL"));
            Console.WriteLine("   - Morph Shape matching = " + (shapeMatchOk ? "PASS" : "FAIL"));

            if (morphOk1 && morphOk2 && morphModeOk && shapeMatchOk)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🎉 TẤT CẢ KIỂM THỬ ĐỘNG CƠ POWERPOINT 2016-365 & MORPH ĐỀU ĐẠT CHUẨN XUẤT SẮC!");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n=> Có kiểm thử chưa đạt yêu cầu!");
                Console.ResetColor();
                return 3;
            }
        }
    }
}
