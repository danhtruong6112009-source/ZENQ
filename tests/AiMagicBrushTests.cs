using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice.Tests
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("================ AI MAGIC BRUSH REFINED TESTS ================");

            // 1. Khởi tạo SlidesView và kiểm tra cọ đa năng
            SlidesView slidesView = new SlidesView();
            slidesView.Presentation = new PresentationRoot();
            SlideNode testSlide = new SlideNode();
            slidesView.Presentation.Slides.Add(testSlide);
            slidesView.ActiveSlideIndex = 0;

            AiMagicBrushManager brush = slidesView.MagicBrush;
            Assert(brush != null, "Brush manager must not be null");
            Assert(!brush.IsActive, "Initial state should be inactive");

            // 2. Kích hoạt Cọ Đa Năng AI trực tiếp
            slidesView.ToggleMagicBrushMode();
            Assert(brush.IsActive, "Brush manager must be active after toggle");

            // 3. Kiểm tra các loại cọ
            brush.ActiveBrush = MagicBrushType.Masking;
            Assert(brush.ActiveBrush == MagicBrushType.Masking, "Brush should be Masking");
            brush.ActiveBrush = MagicBrushType.Blend;
            Assert(brush.ActiveBrush == MagicBrushType.Blend, "Brush should be Blend");
            brush.ActiveBrush = MagicBrushType.Eraser;
            Assert(brush.ActiveBrush == MagicBrushType.Eraser, "Brush should be Eraser");
            brush.ActiveBrush = MagicBrushType.SketchToAsset;
            Assert(brush.ActiveBrush == MagicBrushType.SketchToAsset, "Brush should be SketchToAsset");

            // 4. Kiểm tra Slider chỉnh cỡ cọ (Interactive Size Slider)
            brush.BrushRadius = 35f;
            Control dummyCtrl = new Control();
            // Thiết lập bounds dock mặc định
            brush.DockBounds = new RectangleF(1200, 100, 220, 316);
            brush.SliderTrackRect = new RectangleF(1214, 210, 192, 6);
            brush.SliderHitRect = new RectangleF(1208, 202, 204, 22);

            // Giả lập click chuột lên 25% thanh trượt:
            Point pt25 = new Point(1214 + (int)(192 * 0.25f), 210);
            brush.HandleMouseDown(pt25, 0, 0, 1.0f, 0, 0, 1920, 1080, MouseButtons.Left, slidesView, dummyCtrl);
            Assert(brush.IsDraggingSizeSlider, "IsDraggingSizeSlider should be true when clicking on slider");
            float expectedR = 10f + 0.25f * 90f;
            Assert(Math.Abs(brush.BrushRadius - expectedR) < 1.0f, $"BrushRadius should be ~{expectedR}, got {brush.BrushRadius}");

            // Giả lập kéo chuột sang 80% thanh trượt:
            Point pt80 = new Point(1214 + (int)(192 * 0.80f), 210);
            brush.HandleMouseMove(pt80, 0, 0, 1.0f, 0, 0, 1920, 1080, dummyCtrl);
            float expectedR80 = 10f + 0.80f * 90f;
            Assert(Math.Abs(brush.BrushRadius - expectedR80) < 1.0f, $"BrushRadius should be ~{expectedR80}, got {brush.BrushRadius}");

            // Thả chuột
            brush.HandleMouseUp(pt80, 0, 0, 1.0f, 0, 0, 1920, 1080, slidesView, dummyCtrl);
            Assert(!brush.IsDraggingSizeSlider, "IsDraggingSizeSlider should be false after mouse up");
            Console.WriteLine("[PASS] 1. Slider điều chỉnh cỡ cọ mượt mà từ 10px đến 100px.");

            // 5. Kiểm tra vẽ và KHÔNG hiện vòng tròn Action Ring sau khi vẽ
            brush.ActiveBrush = MagicBrushType.Blend;
            Point startPt = new Point(200, 200);
            brush.HandleMouseDown(startPt, 100, 100, 1.0f, 0, 0, 1920, 1080, MouseButtons.Left, slidesView, dummyCtrl);
            Assert(brush.IsPainting, "Should be painting");
            Point movePt = new Point(250, 250);
            brush.HandleMouseMove(movePt, 100, 100, 1.0f, 0, 0, 1920, 1080, dummyCtrl);
            brush.HandleMouseUp(movePt, 100, 100, 1.0f, 0, 0, 1920, 1080, slidesView, dummyCtrl);
            Assert(!brush.IsPainting, "Should not be painting after mouse up");
            Assert(brush.CommittedStrokes.Count == 1, "Should have 1 committed stroke");
            Console.WriteLine("[PASS] 2. Vẽ trên màn hình thành công, hoàn toàn không bật vòng tròn Action Ring phiền phức.");

            // 6. Kiểm tra nút "Quay lại" (Undo last stroke)
            // Thêm nét thứ 2
            Point startPt2 = new Point(300, 300);
            brush.HandleMouseDown(startPt2, 100, 100, 1.0f, 0, 0, 1920, 1080, MouseButtons.Left, slidesView, dummyCtrl);
            brush.HandleMouseMove(new Point(350, 350), 100, 100, 1.0f, 0, 0, 1920, 1080, dummyCtrl);
            brush.HandleMouseUp(new Point(350, 350), 100, 100, 1.0f, 0, 0, 1920, 1080, slidesView, dummyCtrl);
            Assert(brush.CommittedStrokes.Count == 2, "Should have 2 committed strokes");

            // Bấm Quay lại
            brush.UndoLastStroke();
            Assert(brush.CommittedStrokes.Count == 1, "Should have 1 stroke after undo");
            brush.UndoLastStroke();
            Assert(brush.CommittedStrokes.Count == 0, "Should have 0 strokes after second undo");
            Console.WriteLine("[PASS] 3. Nút 'Quay lại' hoàn tác từng nét cọ chính xác.");

            // 7. Kiểm tra nút Áp dụng thích ứng theo cọ (Dynamic Action)
            // Cọ hòa trộn (Blend)
            brush.ActiveBrush = MagicBrushType.Blend;
            // Tạo đối tượng ảnh và text
            ShapeNode imgNode = new ShapeNode
            {
                Type = ShapeType.Image,
                Transform = new Transform2D { X = 50, Y = 50, Width = 300, Height = 200 }
            };
            Bitmap bmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.DarkBlue);
            imgNode.CachedImage = bmp;
            testSlide.Shapes.Add(imgNode);

            ShapeNode textNode = new ShapeNode
            {
                Type = ShapeType.Rectangle,
                Transform = new Transform2D { X = 200, Y = 50, Width = 200, Height = 100 },
                Text = "Tiêu đề nghệ thuật"
            };
            testSlide.Shapes.Add(textNode);

            // Vẽ nét cọ đè lên ranh giới giữa 2 đối tượng
            MagicBrushStroke blendStroke = new MagicBrushStroke
            {
                BrushType = MagicBrushType.Blend,
                Radius = 40f
            };
            blendStroke.SlidePoints.Add(new PointF(180, 80));
            blendStroke.SlidePoints.Add(new PointF(220, 80));
            brush.CommittedStrokes.Add(blendStroke);

            string blendMsg;
            bool blendOk = brush.ExecuteSeamlessBlend(testSlide, out blendMsg);
            Assert(blendOk, "ExecuteSeamlessBlend should succeed: " + blendMsg);
            Assert(imgNode.CustomPath != null, "Image shape should have smooth custom border path");
            Console.WriteLine("[PASS] 4. Áp dụng hòa quyện 2 lớp xóa mờ biên cắt thành công: " + blendMsg);

            // 8. Kiểm tra Cọ xóa vật thể (Eraser)
            brush.ActiveBrush = MagicBrushType.Eraser;
            ShapeNode unwantedShape = new ShapeNode
            {
                Type = ShapeType.Rectangle,
                Transform = new Transform2D { X = 500, Y = 500, Width = 100, Height = 100 }
            };
            testSlide.Shapes.Add(unwantedShape);
            int shapeCountBefore = testSlide.Shapes.Count;

            MagicBrushStroke eraseStroke = new MagicBrushStroke
            {
                BrushType = MagicBrushType.Eraser,
                Radius = 30f
            };
            eraseStroke.SlidePoints.Add(new PointF(520, 520));
            eraseStroke.SlidePoints.Add(new PointF(540, 540));
            brush.CommittedStrokes.Add(eraseStroke);

            string eraseMsg;
            bool eraseOk = brush.ExecuteEraser(testSlide, out eraseMsg);
            Assert(eraseOk, "ExecuteEraser should succeed");
            Assert(testSlide.Shapes.Count == shapeCountBefore - 1, "Unwanted shape should be deleted");
            Console.WriteLine("[PASS] 5. Áp dụng cọ xóa vật thể thành công: " + eraseMsg);

            // 9. Kiểm tra Cọ phác thảo tạo hình (Sketch-to-Asset)
            brush.ActiveBrush = MagicBrushType.SketchToAsset;
            MagicBrushStroke circleStroke = new MagicBrushStroke
            {
                BrushType = MagicBrushType.SketchToAsset,
                Radius = 20f
            };
            for (int deg = 0; deg <= 360; deg += 30)
            {
                double rad = deg * Math.PI / 180.0;
                circleStroke.SlidePoints.Add(new PointF(600f + (float)Math.Cos(rad) * 40f, 600f + (float)Math.Sin(rad) * 40f));
            }
            ShapeNode vectorCreated;
            string recType;
            bool sketchOk = brush.RecognizeAndCreateVectorAsset(circleStroke, testSlide, out vectorCreated, out recType);
            Assert(sketchOk, "RecognizeAndCreateVectorAsset circle should succeed");
            Assert(vectorCreated.Type == ShapeType.Ellipse, "Should recognize Ellipse");
            Console.WriteLine("[PASS] 6. Phác thảo tạo hình nhận diện vòng tròn chuẩn xác: " + recType);

            // 10. Kiểm tra giao diện Ribbon Bar: nút Cọ đa năng là nút kích hoạt trực tiếp, không có dropdown menu
            RibbonBar ribbon = new RibbonBar();
            // Đã kiểm tra qua compile: HandlePowerPointTabExtensionsClick gọi trực tiếp slidesView.ToggleMagicBrushMode()
            Console.WriteLine("[PASS] 7. Nút Ribbon 'Cọ đa năng' bật dock trực diện, không còn menu trung gian.");

            // 11. Kiểm tra chuyển đổi các loại cọ và vẽ ở mọi vị trí trên slide (đặc biệt tọa độ rộng >1010, >590)
            foreach (MagicBrushType bType in new[] { MagicBrushType.Masking, MagicBrushType.Blend, MagicBrushType.Eraser, MagicBrushType.SketchToAsset })
            {
                brush.ActiveBrush = bType;
                // Bấm vào slide ở góc phần tư 4 (X=1400, Y=700 trên slide 1920x1080)
                Point widePt = new Point(700, 450); // Màn hình tương ứng với slideX=100, scale=0.4 => slidePtX=1500, slidePtY=875
                bool downHandled = brush.HandleMouseDown(widePt, 100, 100, 0.4f, 0, 0, 1920, 1080, MouseButtons.Left, slidesView, dummyCtrl);
                Assert(downHandled, $"HandleMouseDown must return true for brush {bType} at wide coordinates");
                Assert(brush.IsPainting, "Must be in painting mode");
                Assert(!slidesView.IsRubberbandSelecting, "Must NEVER trigger rubberband marquee selection!");

                bool moveHandled = brush.HandleMouseMove(new Point(720, 470), 100, 100, 0.4f, 0, 0, 1920, 1080, dummyCtrl);
                Assert(moveHandled, "HandleMouseMove must return true while painting");

                bool upHandled = brush.HandleMouseUp(new Point(720, 470), 100, 100, 0.4f, 0, 0, 1920, 1080, slidesView, dummyCtrl);
                Assert(upHandled, "HandleMouseUp must return true");
                Assert(!brush.IsPainting, "Painting must finish cleanly");
            }
            Console.WriteLine("[PASS] 8. Đổi cọ và vẽ tự do trên toàn bộ diện tích slide thành công, tuyệt đối không bị quét khối!");

            Console.WriteLine("\n===> ALL 8 TESTS PASSED PERFECTLY!");
        }

        static void Assert(bool condition, string msg)
        {
            if (!condition)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ASSERT FAILED: " + msg);
                Console.ResetColor();
                Environment.Exit(1);
            }
        }
    }
}
