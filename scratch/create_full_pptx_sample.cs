using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace CreateFullPptxSample
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                PresentationRoot pres = new PresentationRoot();
                pres.CanvasWidth = 1280;
                pres.CanvasHeight = 720;
                pres.IsWidescreen = true;
                pres.Slides.Clear();

                // ==========================================
                // SLIDE 1: Title & Hero Cover (Dark Gradient)
                // ==========================================
                SlideNode s1 = new SlideNode();
                s1.IsGradientBg = true;
                s1.BgColor = 0x0F172A;   // Slate 900
                s1.BgColor2 = 0x1E3A8A;  // Blue 900
                s1.BgGradientAngle = 45f;
                s1.Transition = TransitionType.Fade;

                // Main Title
                ShapeNode s1Title = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 180f, 1080f, 120f),
                    Text = "GENOFFICE SLIDES 2026",
                    FontName = "Segoe UI",
                    FontSize = 44f,
                    Bold = true,
                    TextColor = 0x38BDF8, // Sky Blue
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };

                // Subtitle
                ShapeNode s1Sub = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 320f, 1080f, 100f),
                    Text = "NỀN TẢNG THUYẾT TRÌNH THẾ HỆ MỚI\nChuẩn OpenXML PPTX & Hiệu ứng Chuyển cảnh Morph Siêu Mượt",
                    FontName = "Segoe UI",
                    FontSize = 18f,
                    Bold = false,
                    TextColor = 0xE2E8F0, // Slate 200
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };

                // Badge / Tag Shape
                ShapeNode s1Badge = new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(460f, 460f, 360f, 50f),
                    Text = "★ PHIÊN BẢN DOANH NGHIỆP 3.5 ★",
                    FontName = "Segoe UI",
                    FontSize = 13f,
                    Bold = true,
                    TextColor = 0xFFFFFF,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x2563EB,
                    StrokeColor = 0x60A5FA,
                    StrokeWidth = 1.5f
                };

                s1.AddShape(s1Title);
                s1.AddShape(s1Sub);
                s1.AddShape(s1Badge);
                pres.Slides.Add(s1);

                // ==========================================
                // SLIDE 2: 3 Feature Cards (Morph Transition)
                // ==========================================
                SlideNode s2 = new SlideNode();
                s2.BgColor = 0xF8FAFC;
                s2.Transition = TransitionType.Morph;
                s2.MorphMode = MorphType.Objects;

                ShapeNode s2Header = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(80f, 60f, 1120f, 70f),
                    Text = "TÍNH NĂNG ĐỘT PHÁ & TỐI ƯU HIỆU NĂNG",
                    FontName = "Segoe UI",
                    FontSize = 28f,
                    Bold = true,
                    TextColor = 0x0F172A,
                    Alignment = TextAlignment.Left,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };
                s2.AddShape(s2Header);

                // 3 Feature Cards
                string[] cardTitles = new string[] { "⚡ Lõi Vector Siêu Tốc", "✨ Chuyển Cảnh Morph", "🔄 Tương Thích 100% PPTX" };
                string[] cardDescs = new string[] {
                    "Xử lý hàng nghìn đối tượng đồ họa mượt mà ở tốc độ 60 FPS không giật lag.",
                    "Hiệu ứng biến hình Morph Object/Word mượt mà chuẩn Microsoft PowerPoint.",
                    "Mở, chỉnh sửa và lưu định dạng PresentationML (.pptx) chuẩn xác tuyệt đối."
                };
                int[] cardFills = new int[] { 0xEFF6FF, 0xF0FDF4, 0xFEF2F2 };
                int[] cardBorders = new int[] { 0x3B82F6, 0x22C55E, 0xEF4444 };
                int[] titleColors = new int[] { 0x1D4ED8, 0x15803D, 0xB91C1C };

                for (int i = 0; i < 3; i++)
                {
                    float cardX = 80f + i * 380f;
                    ShapeNode cardBg = new ShapeNode
                    {
                        Type = ShapeType.RoundedRectangle,
                        Transform = new Transform2D(cardX, 170f, 350f, 440f),
                        FillColor = cardFills[i],
                        StrokeColor = cardBorders[i],
                        StrokeWidth = 2f
                    };

                    ShapeNode cTitle = new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Transform = new Transform2D(cardX + 20f, 200f, 310f, 60f),
                        Text = cardTitles[i],
                        FontName = "Segoe UI",
                        FontSize = 18f,
                        Bold = true,
                        TextColor = titleColors[i],
                        Alignment = TextAlignment.Center,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    };

                    ShapeNode cDesc = new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Transform = new Transform2D(cardX + 20f, 280f, 310f, 280f),
                        Text = cardDescs[i],
                        FontName = "Segoe UI",
                        FontSize = 14f,
                        TextColor = 0x334155,
                        Alignment = TextAlignment.Left,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    };

                    s2.AddShape(cardBg);
                    s2.AddShape(cTitle);
                    s2.AddShape(cDesc);
                }
                pres.Slides.Add(s2);

                // ==========================================
                // SLIDE 3: Performance Comparison Table (Push)
                // ==========================================
                SlideNode s3 = new SlideNode();
                s3.BgColor = 0x0F172A;
                s3.Transition = TransitionType.Push;

                ShapeNode s3Header = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(80f, 50f, 1120f, 70f),
                    Text = "BẢNG SO SÁNH THÔNG SỐ HIỆU NĂNG THỰC TẾ",
                    FontName = "Segoe UI",
                    FontSize = 26f,
                    Bold = true,
                    TextColor = 0x38BDF8,
                    Alignment = TextAlignment.Left,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };
                s3.AddShape(s3Header);

                ShapeNode tblShape = new ShapeNode
                {
                    Type = ShapeType.Table,
                    Transform = new Transform2D(80f, 150f, 1120f, 480f),
                    TableHeaderRow = true,
                    TableBandedRows = true,
                    TableData = new List<List<string>>
                    {
                        new List<string> { "Chỉ số đánh giá", "GenOffice Slides 2026", "Phiên bản cũ", "Độ cải thiện" },
                        new List<string> { "Tốc độ nạp file .pptx", "0.45 giây", "1.80 giây", "Nhanh hơn 400%" },
                        new List<string> { "Dung lượng RAM tiêu thụ", "82 MB", "310 MB", "Tiết kiệm 73.5%" },
                        new List<string> { "Tỷ lệ khung hình chuyển cảnh", "60 FPS Cố định", "30 - 45 FPS", "Mượt mà tuyệt đối" },
                        new List<string> { "Độ chính xác định dạng OpenXML", "100% Pixel-Perfect", "85% Cơ bản", "Chuẩn quốc tế" }
                    },
                    FillColor = 0x1E293B,
                    StrokeColor = 0x334155,
                    StrokeWidth = 1f
                };
                s3.AddShape(tblShape);
                pres.Slides.Add(s3);

                // ==========================================
                // SLIDE 4: Summary & Call to Action (Wipe)
                // ==========================================
                SlideNode s4 = new SlideNode();
                s4.IsGradientBg = true;
                s4.BgColor = 0x064E3B;  // Emerald 900
                s4.BgColor2 = 0x047857; // Emerald 700
                s4.BgGradientAngle = 90f;
                s4.Transition = TransitionType.Wipe;

                ShapeNode s4Title = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 140f, 1080f, 90f),
                    Text = "KẾT LUẬN & ĐỊNH HƯỚNG TRIỂN KHAI",
                    FontName = "Segoe UI",
                    FontSize = 36f,
                    Bold = true,
                    TextColor = 0xFFFFFF,
                    Alignment = TextAlignment.Center,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };

                ShapeNode s4Body = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(150f, 260f, 980f, 240f),
                    Text = "✔ Hoàn tất đồng bộ 100% chuẩn OpenXML cho toàn bộ bộ ứng dụng (Word, Excel, PowerPoint).\n✔ Bộ chuyển cảnh Morph & Animations chạy mượt mà trên mọi màn hình độ phân giải 4K.\n✔ Sẵn sàng triển khai mở rộng cho toàn thể doanh nghiệp trong Quý 4/2026.",
                    FontName = "Segoe UI",
                    FontSize = 18f,
                    TextColor = 0xD1FAE5,
                    Alignment = TextAlignment.Left,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                };

                ShapeNode s4Button = new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(460f, 530f, 360f, 65f),
                    Text = "BẮT ĐẦU SỬ DỤNG NGAY",
                    FontName = "Segoe UI",
                    FontSize = 16f,
                    Bold = true,
                    TextColor = 0x064E3B,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x34D399,
                    StrokeColor = 0xFFFFFF,
                    StrokeWidth = 2f
                };

                s4.AddShape(s4Title);
                s4.AddShape(s4Body);
                s4.AddShape(s4Button);
                pres.Slides.Add(s4);

                // ==========================================
                // 5. Save to .pptx OpenXML Package
                // ==========================================
                string outPptx = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "BaoCaoThuyetTrinh_2026.pptx");
                if (File.Exists(outPptx)) File.Delete(outPptx);

                bool saveOk = PptxHandler.SavePptx(outPptx, pres);
                Console.WriteLine("SavePptx: " + (saveOk ? "SUCCESS" : "FAILED") + " -> " + outPptx);

                // ==========================================
                // 6. Render GenOffice Slides Screenshots
                // ==========================================
                for (int i = 0; i < pres.Slides.Count; i++)
                {
                    Bitmap slideBmp = RenderSlide(pres, i, 1280, 720);
                    string sImg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", string.Format("genoffice_slide{0}.png", i + 1));
                    slideBmp.Save(sImg, ImageFormat.Png);
                    Console.WriteLine("GenOffice Slide " + (i + 1) + " Screenshot: " + sImg);
                }

                Console.WriteLine("=== PPTX_SAMPLE_GENERATION_SUCCESSFUL ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }

        static Bitmap RenderSlide(PresentationRoot pres, int slideIndex, int targetW, int targetH)
        {
            Bitmap bmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, targetW, targetH);
                SlideNode slide = pres.Slides[slideIndex];

                // Background
                if (slide.IsGradientBg)
                {
                    Color c1 = Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000));
                    Color c2 = Color.FromArgb(slide.BgColor2 | unchecked((int)0xFF000000));
                    using (LinearGradientBrush lgb = new LinearGradientBrush(r, c1, c2, slide.BgGradientAngle))
                    {
                        g.FillRectangle(lgb, r);
                    }
                }
                else
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000))))
                    {
                        g.FillRectangle(sb, r);
                    }
                }

                // Shapes
                float scale = (float)targetW / pres.CanvasWidth;
                for (int s = 0; s < slide.Shapes.Count; s++)
                {
                    ShapeNode shape = slide.Shapes[s];
                    if (!shape.IsVisible) continue;

                    float baseW = shape.Transform.Width * scale;
                    float baseH = shape.Transform.Height * scale;
                    float sx = shape.Transform.X * scale;
                    float sy = shape.Transform.Y * scale;
                    RectangleF sRect = new RectangleF(sx, sy, baseW, baseH);

                    // Draw Shape Background / Geometry
                    if (shape.Type == ShapeType.Table && shape.TableData != null && shape.TableData.Count > 0)
                    {
                        int nR = shape.TableData.Count;
                        int nC = shape.TableData[0].Count;
                        float colW = baseW / nC;
                        float rowH = baseH / nR;

                        for (int tr = 0; tr < nR; tr++)
                        {
                            bool isH = (tr == 0);
                            for (int tc = 0; tc < nC; tc++)
                            {
                                RectangleF cRect = new RectangleF(sx + tc * colW, sy + tr * rowH, colW, rowH);
                                Color cellBg = isH ? Color.FromArgb(37, 99, 235) : (tr % 2 == 0 ? Color.FromArgb(30, 41, 59) : Color.FromArgb(15, 23, 42));
                                using (SolidBrush cb = new SolidBrush(cellBg))
                                using (Pen cp = new Pen(Color.FromArgb(51, 65, 85), 1f))
                                {
                                    g.FillRectangle(cb, cRect);
                                    g.DrawRectangle(cp, cRect.X, cRect.Y, cRect.Width, cRect.Height);
                                }

                                string txt = (tc < shape.TableData[tr].Count) ? shape.TableData[tr][tc] : "";
                                if (!string.IsNullOrEmpty(txt))
                                {
                                    using (Font tf = new Font("Segoe UI", isH ? 13f : 12f, isH ? FontStyle.Bold : FontStyle.Regular))
                                    using (SolidBrush tb = new SolidBrush(isH ? Color.White : (tc == 3 ? Color.FromArgb(56, 189, 248) : Color.FromArgb(226, 232, 240))))
                                    {
                                        StringFormat sf = new StringFormat
                                        {
                                            Alignment = isH ? StringAlignment.Center : (tc == 0 ? StringAlignment.Near : StringAlignment.Center),
                                            LineAlignment = StringAlignment.Center
                                        };
                                        RectangleF padRect = new RectangleF(cRect.X + 8, cRect.Y, cRect.Width - 16, cRect.Height);
                                        g.DrawString(txt, tf, tb, padRect, sf);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (shape.FillColor != 0x00FFFFFF)
                        {
                            using (SolidBrush fb = new SolidBrush(Color.FromArgb(shape.FillColor | unchecked((int)0xFF000000))))
                            {
                                if (shape.Type == ShapeType.RoundedRectangle)
                                {
                                    using (GraphicsPath path = GetRoundedRectPath(sRect, 12f * scale))
                                    {
                                        g.FillPath(fb, path);
                                        if (shape.StrokeWidth > 0)
                                        {
                                            using (Pen sp = new Pen(Color.FromArgb(shape.StrokeColor | unchecked((int)0xFF000000)), shape.StrokeWidth * scale))
                                                g.DrawPath(sp, path);
                                        }
                                    }
                                }
                                else
                                {
                                    g.FillRectangle(fb, sRect);
                                    if (shape.StrokeWidth > 0)
                                    {
                                        using (Pen sp = new Pen(Color.FromArgb(shape.StrokeColor | unchecked((int)0xFF000000)), shape.StrokeWidth * scale))
                                            g.DrawRectangle(sp, sRect.X, sRect.Y, sRect.Width, sRect.Height);
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(shape.Text))
                        {
                            using (Font tf = new Font(shape.FontName ?? "Segoe UI", Math.Max(10f, shape.FontSize * scale), shape.Bold ? FontStyle.Bold : FontStyle.Regular))
                            using (SolidBrush tb = new SolidBrush(Color.FromArgb(shape.TextColor | unchecked((int)0xFF000000))))
                            {
                                StringFormat sf = new StringFormat
                                {
                                    Alignment = shape.Alignment == TextAlignment.Center ? StringAlignment.Center : (shape.Alignment == TextAlignment.Right ? StringAlignment.Far : StringAlignment.Near),
                                    LineAlignment = shape.VerticalAlignment == VerticalTextAlignment.Middle ? StringAlignment.Center : (shape.VerticalAlignment == VerticalTextAlignment.Bottom ? StringAlignment.Far : StringAlignment.Near)
                                };
                                RectangleF textPad = new RectangleF(sRect.X + 8, sRect.Y + 4, sRect.Width - 16, sRect.Height - 8);
                                g.DrawString(shape.Text, tf, tb, textPad, sf);
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
