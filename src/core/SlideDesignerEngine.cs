using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Đại diện cho một gợi ý ý tưởng thiết kế thông minh trong PowerPoint 365 Designer.
    /// </summary>
    public class SlideDesignIdea
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int BackgroundColor { get; set; } = 0xFFFFFF;
        public int AccentColor { get; set; } = 0x2563EB;
        public Action<SlideNode, float, float> Apply { get; set; }
        public Action<Graphics, RectangleF> DrawMiniPreview { get; set; }
    }

    /// <summary>
    /// Động cơ tạo và áp dụng ý tưởng thiết kế PowerPoint 365 Designer (Design Ideas).
    /// Tự động phân tích nội dung slide (tiêu đề, thẻ chữ, bullet points, hình ảnh) và tạo ra
    /// các biến thể bố cục hiện đại, chuyên nghiệp với tỷ lệ vàng, typography và màu sắc tối ưu.
    /// </summary>
    public static class SlideDesignerEngine
    {
        public static List<SlideDesignIdea> GenerateIdeas(SlideNode slide, float canvasW = 1920f, float canvasH = 1080f)
        {
            List<SlideDesignIdea> ideas = new List<SlideDesignIdea>();
            if (slide == null) return ideas;

            // 1. Phân tích nội dung hiện có trên Slide
            string slideTitle = "Tiêu đề Bài thuyết trình";
            string slideSubtitle = "Nội dung tóm tắt & điểm nhấn chiến lược";
            List<string> contentPoints = new List<string>();
            ShapeNode existingImage = null;

            if (slide.Shapes != null && slide.Shapes.Count > 0)
            {
                foreach (var shp in slide.Shapes)
                {
                    if (shp == null || !shp.IsVisible) continue;

                    if (shp.Type == ShapeType.Image || shp.CachedImage != null || !string.IsNullOrEmpty(shp.ImagePath))
                    {
                        if (existingImage == null) existingImage = shp;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(shp.Text))
                    {
                        string trimmed = shp.Text.Trim();
                        if (shp.PlaceholderType == "Title" || (shp.FontSize >= 28f && slideTitle == "Tiêu đề Bài thuyết trình"))
                        {
                            slideTitle = trimmed;
                        }
                        else if (shp.PlaceholderType == "Subtitle" || (shp.FontSize >= 18f && slideSubtitle == "Nội dung tóm tắt & điểm nhấn chiến lược"))
                        {
                            slideSubtitle = trimmed;
                        }
                        else
                        {
                            string[] lines = trimmed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                string l = line.Trim().TrimStart('-', '•', '*', '1', '2', '3', '4', '5', '.', ' ');
                                if (!string.IsNullOrWhiteSpace(l)) contentPoints.Add(l);
                            }
                        }
                    }
                }
            }

            if (contentPoints.Count == 0)
            {
                contentPoints.Add("Phân tích cơ hội & xu hướng công nghệ mới");
                contentPoints.Add("Tối ưu hóa năng suất và trải nghiệm người dùng");
                contentPoints.Add("Định vị giá trị cốt lõi và lộ trình phát triển bền vững");
            }

            // 2. Sinh 8 ý tưởng thiết kế phong cách PowerPoint 365 chuẩn mực

            // =========================================================================
            // IDEA 1: Hero Split (Chia đôi Hiện đại)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "hero_split",
                Title = "Hero Split Đẳng cấp",
                Category = "Hiện đại",
                Description = "Bố cục chia đôi cân đối với khối màu ấn tượng bên trái và thẻ nội dung trang nhã bên phải.",
                BackgroundColor = 0xF8FAFC,
                AccentColor = 0x0F172A,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xF8FAFC;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    float halfW = w * 0.44f;

                    // Left Hero Accent Block
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Hero_LeftBlock",
                        Type = ShapeType.Rectangle,
                        FillColor = 0x0F172A,
                        StrokeColor = 0x0F172A,
                        Transform = new Transform2D { X = 0, Y = 0, Width = halfW, Height = h },
                        FillOpacity = 1f
                    });

                    // Category Pill on Left
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Hero_Pill",
                        Type = ShapeType.RoundedRectangle,
                        CornerRadius = 14f,
                        FillColor = 0x2563EB,
                        StrokeColor = 0x2563EB,
                        Text = "CHIẾN LƯỢC TOÀN CẦU",
                        FontName = "Segoe UI",
                        FontSize = 14f,
                        Bold = true,
                        TextColor = 0xFFFFFF,
                        Transform = new Transform2D { X = 60, Y = 100, Width = 220, Height = 42 },
                        Alignment = TextAlignment.Center
                    });

                    // Title on Left
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Hero_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 42f,
                        Bold = true,
                        TextColor = 0xFFFFFF,
                        Transform = new Transform2D { X = 60, Y = 180, Width = halfW - 120, Height = 260 },
                        Alignment = TextAlignment.Left
                    });

                    // Subtitle on Left
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Hero_Subtitle",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Subtitle",
                        Text = slideSubtitle,
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        TextColor = 0x94A3B8,
                        Transform = new Transform2D { X = 60, Y = 460, Width = halfW - 120, Height = 180 },
                        Alignment = TextAlignment.Left
                    });

                    // Right Content Area (Image or Cards)
                    float rightX = halfW + 60;
                    float rightW = w - halfW - 120;

                    if (existingImage != null)
                    {
                        var imgCopy = existingImage.Clone();
                        imgCopy.Transform = new Transform2D { X = rightX, Y = 120, Width = rightW, Height = h - 240 };
                        imgCopy.CornerRadius = 16f;
                        imgCopy.HasShadow = true;
                        imgCopy.ShadowColor = 0xCBD5E1;
                        s.Shapes.Add(imgCopy);
                    }
                    else
                    {
                        int count = Math.Min(3, contentPoints.Count);
                        float cardH = (h - 240 - (count - 1) * 24) / count;
                        for (int i = 0; i < count; i++)
                        {
                            float cy = 120 + i * (cardH + 24);
                            // Card container
                            s.Shapes.Add(new ShapeNode
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = $"Hero_Card_{i + 1}",
                                Type = ShapeType.RoundedRectangle,
                                CornerRadius = 16f,
                                FillColor = 0xFFFFFF,
                                StrokeColor = 0xE2E8F0,
                                StrokeWidth = 1.5f,
                                HasShadow = true,
                                ShadowColor = 0xE2E8F0,
                                ShadowOffsetY = 4f,
                                ShadowBlur = 8f,
                                Transform = new Transform2D { X = rightX, Y = cy, Width = rightW, Height = cardH }
                            });

                            // Card text
                            s.Shapes.Add(new ShapeNode
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = $"Hero_CardText_{i + 1}",
                                Type = ShapeType.TextBox,
                                Text = $"0{i + 1}.  {contentPoints[i]}",
                                FontName = "Segoe UI",
                                FontSize = 20f,
                                Bold = (i == 0),
                                TextColor = 0x1E293B,
                                Transform = new Transform2D { X = rightX + 30, Y = cy + cardH * 0.25f, Width = rightW - 60, Height = cardH * 0.5f },
                                Alignment = TextAlignment.Left
                            });
                        }
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), rect);
                    float splitW = rect.Width * 0.42f;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 23, 42)), rect.X, rect.Y, splitW, rect.Height);

                    // Mini title lines on left
                    g.FillRectangle(new SolidBrush(Color.FromArgb(37, 99, 235)), rect.X + 8, rect.Y + 12, splitW - 16, 6);
                    g.FillRectangle(Brushes.White, rect.X + 8, rect.Y + 24, splitW - 20, 8);
                    g.FillRectangle(Brushes.White, rect.X + 8, rect.Y + 36, splitW - 32, 6);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), rect.X + 8, rect.Y + 48, splitW - 24, 4);

                    // Mini cards on right
                    float rx = rect.X + splitW + 8;
                    float rw = rect.Width - splitW - 16;
                    float ch = (rect.Height - 30) / 3f;
                    for (int i = 0; i < 3; i++)
                    {
                        float cy = rect.Y + 8 + i * (ch + 4);
                        g.FillRectangle(Brushes.White, rx, cy, rw, ch);
                        g.DrawRectangle(new Pen(Color.FromArgb(226, 232, 240)), rx, cy, rw, ch);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 116, 139)), rx + 6, cy + ch * 0.35f, rw - 12, 3);
                    }
                }
            });

            // =========================================================================
            // IDEA 2: Bento Box Grid (Lưới Bento Đa năng)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "bento_grid",
                Title = "Bento Grid Đa Năng",
                Category = "Thanh lịch",
                Description = "Bố cục khối Bento dạng thẻ bo tròn hiện đại với các dải màu điểm xuyết tinh tế.",
                BackgroundColor = 0xF1F5F9,
                AccentColor = 0x2563EB,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xF1F5F9;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Header Title
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Bento_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 40f,
                        Bold = true,
                        TextColor = 0x0F172A,
                        Transform = new Transform2D { X = 80, Y = 60, Width = w - 160, Height = 70 },
                        Alignment = TextAlignment.Left
                    });

                    // Subtitle
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Bento_Subtitle",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Subtitle",
                        Text = slideSubtitle,
                        FontName = "Segoe UI",
                        FontSize = 18f,
                        TextColor = 0x64748B,
                        Transform = new Transform2D { X = 80, Y = 130, Width = w - 160, Height = 40 },
                        Alignment = TextAlignment.Left
                    });

                    // 3 Bento Cards
                    int cols = 3;
                    float cardGap = 24f;
                    float totalW = w - 160;
                    float cardW = (totalW - (cols - 1) * cardGap) / cols;
                    float cardY = 200f;
                    float cardH = h - cardY - 80f;

                    int[] accentColors = new int[] { 0x2563EB, 0x10B981, 0xF59E0B };
                    string[] cardTitles = new string[] { "ĐỊNH HƯỚNG", "THỰC THI", "KẾT QUẢ" };

                    for (int i = 0; i < cols; i++)
                    {
                        float cx = 80 + i * (cardW + cardGap);
                        int acc = accentColors[i % accentColors.Length];

                        // Card background
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Bento_CardBg_{i + 1}",
                            Type = ShapeType.RoundedRectangle,
                            CornerRadius = 18f,
                            FillColor = 0xFFFFFF,
                            StrokeColor = 0xE2E8F0,
                            StrokeWidth = 1.5f,
                            HasShadow = true,
                            ShadowColor = 0xE2E8F0,
                            ShadowOffsetY = 6f,
                            ShadowBlur = 10f,
                            Transform = new Transform2D { X = cx, Y = cardY, Width = cardW, Height = cardH }
                        });

                        // Top Accent Bar
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Bento_CardAccent_{i + 1}",
                            Type = ShapeType.RoundedRectangle,
                            CornerRadius = 6f,
                            FillColor = acc,
                            StrokeColor = acc,
                            Transform = new Transform2D { X = cx + 24, Y = cardY + 24, Width = 80, Height = 8 }
                        });

                        // Card Category Header
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Bento_CardHead_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = cardTitles[i % cardTitles.Length],
                            FontName = "Segoe UI",
                            FontSize = 14f,
                            Bold = true,
                            TextColor = acc,
                            Transform = new Transform2D { X = cx + 24, Y = cardY + 44, Width = cardW - 48, Height = 30 }
                        });

                        // Card Content
                        string pointText = (i < contentPoints.Count) ? contentPoints[i] : $"Giải pháp trọng tâm cho trụ cột {i + 1}";
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Bento_CardContent_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = pointText,
                            FontName = "Segoe UI",
                            FontSize = 20f,
                            TextColor = 0x1E293B,
                            Transform = new Transform2D { X = cx + 24, Y = cardY + 84, Width = cardW - 48, Height = cardH - 110 }
                        });
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), rect);
                    // Mini title
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 23, 42)), rect.X + 8, rect.Y + 8, rect.Width * 0.45f, 6);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), rect.X + 8, rect.Y + 18, rect.Width * 0.3f, 4);

                    // 3 mini cards
                    float cw = (rect.Width - 32) / 3f;
                    float cy = rect.Y + 28;
                    float ch = rect.Height - 36;
                    Color[] colors = new Color[] { Color.FromArgb(37, 99, 235), Color.FromArgb(16, 185, 129), Color.FromArgb(245, 158, 11) };

                    for (int i = 0; i < 3; i++)
                    {
                        float cx = rect.X + 8 + i * (cw + 4);
                        g.FillRectangle(Brushes.White, cx, cy, cw, ch);
                        g.DrawRectangle(new Pen(Color.FromArgb(226, 232, 240)), cx, cy, cw, ch);
                        g.FillRectangle(new SolidBrush(colors[i]), cx + 4, cy + 5, cw * 0.4f, 3);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 116, 139)), cx + 4, cy + 12, cw - 8, 3);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), cx + 4, cy + 18, cw - 12, 3);
                    }
                }
            });

            // =========================================================================
            // IDEA 3: Executive Dark Obsidian (Đêm Huyền bí Sang trọng)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "dark_obsidian",
                Title = "Executive Dark Obsidian",
                Category = "Sang trọng",
                Description = "Giao diện nền tối Obsidian quý phái với thanh dải màu Gradient rực rỡ và hiệu ứng thẻ thủy tinh.",
                BackgroundColor = 0x0B0F19,
                AccentColor = 0x6366F1,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0x0B0F19;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Glowing Accent Line Top
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Dark_GlowLine",
                        Type = ShapeType.Rectangle,
                        FillColor = 0x6366F1,
                        HasGradientFill = true,
                        GradientColor2 = 0xEC4899,
                        GradientAngle = 90f,
                        Transform = new Transform2D { X = 80, Y = 60, Width = 120, Height = 6 }
                    });

                    // Title
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Dark_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 44f,
                        Bold = true,
                        TextColor = 0xFFFFFF,
                        Transform = new Transform2D { X = 80, Y = 84, Width = w - 160, Height = 75 }
                    });

                    // Subtitle
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Dark_Subtitle",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Subtitle",
                        Text = slideSubtitle,
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        TextColor = 0x94A3B8,
                        Transform = new Transform2D { X = 80, Y = 165, Width = w - 160, Height = 45 }
                    });

                    // Frosted Glass Cards
                    int count = Math.Min(3, contentPoints.Count);
                    float cardY = 240f;
                    float cardH = h - cardY - 90f;
                    float cardW = (w - 160 - (count - 1) * 28) / count;

                    for (int i = 0; i < count; i++)
                    {
                        float cx = 80 + i * (cardW + 28);
                        // Glass card
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Dark_GlassCard_{i + 1}",
                            Type = ShapeType.RoundedRectangle,
                            CornerRadius = 18f,
                            FillColor = 0x1E293B,
                            StrokeColor = 0x334155,
                            StrokeWidth = 1.5f,
                            Transform = new Transform2D { X = cx, Y = cardY, Width = cardW, Height = cardH }
                        });

                        // Pill number
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Dark_Pill_{i + 1}",
                            Type = ShapeType.Ellipse,
                            FillColor = 0x6366F1,
                            Text = $"{i + 1}",
                            FontName = "Segoe UI",
                            FontSize = 16f,
                            Bold = true,
                            TextColor = 0xFFFFFF,
                            Transform = new Transform2D { X = cx + 28, Y = cardY + 28, Width = 36, Height = 36 },
                            Alignment = TextAlignment.Center
                        });

                        // Content text
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Dark_CardText_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = contentPoints[i],
                            FontName = "Segoe UI",
                            FontSize = 20f,
                            TextColor = 0xF1F5F9,
                            Transform = new Transform2D { X = cx + 28, Y = cardY + 84, Width = cardW - 56, Height = cardH - 110 }
                        });
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(11, 15, 25)), rect);
                    // Glowing top line
                    g.FillRectangle(new SolidBrush(Color.FromArgb(99, 102, 241)), rect.X + 8, rect.Y + 8, 20, 2);
                    // White title
                    g.FillRectangle(Brushes.White, rect.X + 8, rect.Y + 13, rect.Width * 0.5f, 6);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), rect.X + 8, rect.Y + 22, rect.Width * 0.35f, 3);

                    // 3 dark glass cards
                    float cw = (rect.Width - 32) / 3f;
                    float cy = rect.Y + 30;
                    float ch = rect.Height - 38;
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = rect.X + 8 + i * (cw + 4);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(30, 41, 59)), cx, cy, cw, ch);
                        g.DrawRectangle(new Pen(Color.FromArgb(51, 65, 85)), cx, cy, cw, ch);
                        g.FillEllipse(new SolidBrush(Color.FromArgb(99, 102, 241)), cx + 4, cy + 4, 8, 8);
                        g.FillRectangle(Brushes.White, cx + 4, cy + 16, cw - 8, 3);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), cx + 4, cy + 22, cw - 10, 2);
                    }
                }
            });

            // =========================================================================
            // IDEA 4: Minimalist Editorial (Tạp chí Tối giản)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "editorial",
                Title = "Tạp Chí Nghệ Thuật Tối Giản",
                Category = "Tối giản",
                Description = "Không gian trắng thanh thoát chuẩn tạp chí quốc tế với typography sắc nét và thanh kẻ màu cobalt.",
                BackgroundColor = 0xFFFFFF,
                AccentColor = 0x2563EB,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xFFFFFF;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Vertical Accent Bar
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Editorial_Bar",
                        Type = ShapeType.Rectangle,
                        FillColor = 0x2563EB,
                        StrokeColor = 0x2563EB,
                        Transform = new Transform2D { X = 80, Y = 90, Width = 6, Height = 100 }
                    });

                    // Title
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Editorial_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 46f,
                        Bold = true,
                        TextColor = 0x0F172A,
                        Transform = new Transform2D { X = 104, Y = 80, Width = w - 180, Height = 65 }
                    });

                    // Subtitle
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Editorial_Subtitle",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Subtitle",
                        Text = slideSubtitle,
                        FontName = "Segoe UI",
                        FontSize = 22f,
                        TextColor = 0x64748B,
                        Transform = new Transform2D { X = 104, Y = 150, Width = w - 180, Height = 40 }
                    });

                    // Horizontal divider rule
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Editorial_Line",
                        Type = ShapeType.Line,
                        StrokeColor = 0xE2E8F0,
                        StrokeWidth = 2f,
                        Transform = new Transform2D { X = 80, Y = 220, Width = w - 160, Height = 2 }
                    });

                    // 3 Content Columns with giant numbers
                    int count = Math.Min(3, contentPoints.Count);
                    float colW = (w - 160 - (count - 1) * 40) / count;
                    float colY = 260f;
                    float colH = h - colY - 80f;

                    for (int i = 0; i < count; i++)
                    {
                        float cx = 80 + i * (colW + 40);

                        // Giant Number
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Editorial_Num_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = $"0{i + 1}",
                            FontName = "Segoe UI",
                            FontSize = 48f,
                            Bold = true,
                            TextColor = 0x2563EB,
                            Transform = new Transform2D { X = cx, Y = colY, Width = 120, Height = 60 }
                        });

                        // Narrative Text
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Editorial_Text_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = contentPoints[i],
                            FontName = "Segoe UI",
                            FontSize = 20f,
                            TextColor = 0x334155,
                            Transform = new Transform2D { X = cx, Y = colY + 70, Width = colW, Height = colH - 70 }
                        });
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(Brushes.White, rect);
                    // Blue vertical bar
                    g.FillRectangle(new SolidBrush(Color.FromArgb(37, 99, 235)), rect.X + 8, rect.Y + 8, 2, 16);
                    // Title text
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 23, 42)), rect.X + 13, rect.Y + 8, rect.Width * 0.5f, 6);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(100, 116, 139)), rect.X + 13, rect.Y + 18, rect.Width * 0.35f, 4);

                    // Divider line
                    g.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), rect.X + 8, rect.Y + 28, rect.Right - 8, rect.Y + 28);

                    // 3 columns
                    float cw = (rect.Width - 32) / 3f;
                    float cy = rect.Y + 34;
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = rect.X + 8 + i * (cw + 4);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(37, 99, 235)), cx, cy, 10, 6);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(51, 65, 85)), cx, cy + 9, cw - 4, 3);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), cx, cy + 15, cw - 6, 2);
                    }
                }
            });

            // =========================================================================
            // IDEA 5: Horizontal Timeline / Step Process (Quy trình 3 Bước)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "process_steps",
                Title = "Quy Trình 3 Bước Liền Mạch",
                Category = "Quy trình",
                Description = "Sắp xếp thông tin dạng lộ trình tiến độ với huy hiệu số tròn và đường dẫn kết nối trực quan.",
                BackgroundColor = 0xF8FAFC,
                AccentColor = 0x059669,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xF8FAFC;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Title
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Proc_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 42f,
                        Bold = true,
                        TextColor = 0x0F172A,
                        Transform = new Transform2D { X = 80, Y = 60, Width = w - 160, Height = 65 },
                        Alignment = TextAlignment.Center
                    });

                    // Subtitle
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Proc_Subtitle",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Subtitle",
                        Text = slideSubtitle,
                        FontName = "Segoe UI",
                        FontSize = 18f,
                        TextColor = 0x64748B,
                        Transform = new Transform2D { X = 80, Y = 130, Width = w - 160, Height = 40 },
                        Alignment = TextAlignment.Center
                    });

                    // Connecting Line Across Process Steps
                    float lineY = 270f;
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Proc_ConnectLine",
                        Type = ShapeType.Line,
                        StrokeColor = 0xCBD5E1,
                        StrokeWidth = 4f,
                        Transform = new Transform2D { X = 200, Y = lineY, Width = w - 400, Height = 4 }
                    });

                    int count = 3;
                    float cardW = 440f;
                    float startX = (w - (count * cardW + (count - 1) * 60)) / 2f;
                    float cardY = 320f;
                    float cardH = h - cardY - 90f;

                    for (int i = 0; i < count; i++)
                    {
                        float cx = startX + i * (cardW + 60);

                        // Step Circle Badge
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Proc_Badge_{i + 1}",
                            Type = ShapeType.Ellipse,
                            FillColor = 0x059669,
                            Text = $"{i + 1}",
                            FontName = "Segoe UI",
                            FontSize = 24f,
                            Bold = true,
                            TextColor = 0xFFFFFF,
                            Transform = new Transform2D { X = cx + cardW / 2f - 35, Y = lineY - 35, Width = 70, Height = 70 },
                            Alignment = TextAlignment.Center
                        });

                        // Step Card Box
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Proc_Card_{i + 1}",
                            Type = ShapeType.RoundedRectangle,
                            CornerRadius = 16f,
                            FillColor = 0xFFFFFF,
                            StrokeColor = 0xE2E8F0,
                            StrokeWidth = 1.5f,
                            HasShadow = true,
                            ShadowColor = 0xE2E8F0,
                            ShadowOffsetY = 4f,
                            ShadowBlur = 8f,
                            Transform = new Transform2D { X = cx, Y = cardY, Width = cardW, Height = cardH }
                        });

                        // Step Text
                        string text = (i < contentPoints.Count) ? contentPoints[i] : $"Bước hành động then chốt {i + 1}";
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Proc_Text_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = text,
                            FontName = "Segoe UI",
                            FontSize = 19f,
                            TextColor = 0x1E293B,
                            Transform = new Transform2D { X = cx + 24, Y = cardY + 30, Width = cardW - 48, Height = cardH - 60 },
                            Alignment = TextAlignment.Center
                        });
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), rect);
                    // Centered title
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 23, 42)), rect.X + rect.Width * 0.25f, rect.Y + 8, rect.Width * 0.5f, 5);

                    // Connecting line
                    float ly = rect.Y + 22;
                    g.DrawLine(new Pen(Color.FromArgb(203, 213, 225), 1.5f), rect.X + 16, ly, rect.Right - 16, ly);

                    // 3 circles & cards
                    float cw = (rect.Width - 32) / 3f;
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = rect.X + 8 + i * (cw + 4);
                        g.FillEllipse(new SolidBrush(Color.FromArgb(5, 150, 105)), cx + cw / 2f - 4, ly - 4, 8, 8);
                        g.FillRectangle(Brushes.White, cx, ly + 8, cw, rect.Height - ly - 14);
                        g.DrawRectangle(new Pen(Color.FromArgb(226, 232, 240)), cx, ly + 8, cw, rect.Height - ly - 14);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 116, 139)), cx + 3, ly + 14, cw - 6, 2);
                    }
                }
            });

            // =========================================================================
            // IDEA 6: Sunset Gradient Header (Dải màu Hoàng hôn Sáng tạo)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "sunset_header",
                Title = "Dải Màu Hoàng Hôn Sáng Tạo",
                Category = "Sáng tạo",
                Description = "Thanh tiêu đề Gradient rực rỡ từ cánh hoa hồng đến cam san hô tràn ngập năng lượng tích cực.",
                BackgroundColor = 0xFFFFFF,
                AccentColor = 0xF43F5E,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xFFFFFF;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Top Gradient Banner
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Sunset_Banner",
                        Type = ShapeType.Rectangle,
                        FillColor = 0xF43F5E,
                        HasGradientFill = true,
                        GradientColor2 = 0xFB923C,
                        GradientAngle = 90f,
                        Transform = new Transform2D { X = 0, Y = 0, Width = w, Height = 220 }
                    });

                    // Title on Banner
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Sunset_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 44f,
                        Bold = true,
                        TextColor = 0xFFFFFF,
                        Transform = new Transform2D { X = 80, Y = 50, Width = w - 160, Height = 70 }
                    });

                    // Subtitle on Banner
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Sunset_Subtitle",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Subtitle",
                        Text = slideSubtitle,
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        TextColor = 0xFFF1F2,
                        Transform = new Transform2D { X = 80, Y = 130, Width = w - 160, Height = 40 }
                    });

                    // Cards Below
                    int count = Math.Min(3, contentPoints.Count);
                    float cardY = 280f;
                    float cardH = h - cardY - 80f;
                    float cardW = (w - 160 - (count - 1) * 32) / count;

                    for (int i = 0; i < count; i++)
                    {
                        float cx = 80 + i * (cardW + 32);

                        // Card
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Sunset_Card_{i + 1}",
                            Type = ShapeType.RoundedRectangle,
                            CornerRadius = 18f,
                            FillColor = 0xFFF7ED,
                            StrokeColor = 0xFED7AA,
                            StrokeWidth = 1.5f,
                            Transform = new Transform2D { X = cx, Y = cardY, Width = cardW, Height = cardH }
                        });

                        // Card Text
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Sunset_Text_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = contentPoints[i],
                            FontName = "Segoe UI",
                            FontSize = 20f,
                            TextColor = 0x9A3412,
                            Transform = new Transform2D { X = cx + 28, Y = cardY + 40, Width = cardW - 56, Height = cardH - 80 }
                        });
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(Brushes.White, rect);
                    // Gradient header
                    using (LinearGradientBrush lgb = new LinearGradientBrush(new RectangleF(rect.X, rect.Y, rect.Width, 20), Color.FromArgb(244, 63, 94), Color.FromArgb(251, 146, 60), 0f))
                    {
                        g.FillRectangle(lgb, rect.X, rect.Y, rect.Width, 20);
                    }
                    g.FillRectangle(Brushes.White, rect.X + 8, rect.Y + 6, rect.Width * 0.45f, 5);

                    // Cards below
                    float cw = (rect.Width - 32) / 3f;
                    float cy = rect.Y + 26;
                    float ch = rect.Height - 32;
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = rect.X + 8 + i * (cw + 4);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 247, 237)), cx, cy, cw, ch);
                        g.DrawRectangle(new Pen(Color.FromArgb(254, 215, 170)), cx, cy, cw, ch);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(154, 52, 18)), cx + 4, cy + 8, cw - 8, 3);
                    }
                }
            });

            // =========================================================================
            // IDEA 7: Stat & KPI Metric Spotlight (Tiêu điểm Số liệu)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "kpi_spotlight",
                Title = "Tiêu Điểm Số Liệu & KPI",
                Category = "Số liệu",
                Description = "Tập trung sự chú ý vào số liệu ấn tượng hoặc phần trăm tăng trưởng với thẻ thông tin phân cấp.",
                BackgroundColor = 0xF8FAFC,
                AccentColor = 0x4F46E5,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xF8FAFC;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Title
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "KPI_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 42f,
                        Bold = true,
                        TextColor = 0x0F172A,
                        Transform = new Transform2D { X = 80, Y = 60, Width = w - 160, Height = 65 }
                    });

                    // Big KPI Hero Box (Left side)
                    float heroW = w * 0.38f;
                    float heroH = h - 220f;
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "KPI_HeroBox",
                        Type = ShapeType.RoundedRectangle,
                        CornerRadius = 24f,
                        FillColor = 0x4F46E5,
                        StrokeColor = 0x4F46E5,
                        Transform = new Transform2D { X = 80, Y = 160, Width = heroW, Height = heroH }
                    });

                    // Giant Metric Number
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "KPI_Number",
                        Type = ShapeType.TextBox,
                        Text = "98.5%",
                        FontName = "Segoe UI",
                        FontSize = 64f,
                        Bold = true,
                        TextColor = 0xFFFFFF,
                        Transform = new Transform2D { X = 110, Y = 240, Width = heroW - 60, Height = 100 },
                        Alignment = TextAlignment.Center
                    });

                    // Metric Label
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "KPI_Label",
                        Type = ShapeType.TextBox,
                        Text = "Chỉ số hoàn thành mục tiêu chiến lược",
                        FontName = "Segoe UI",
                        FontSize = 22f,
                        TextColor = 0xE0E7FF,
                        Transform = new Transform2D { X = 110, Y = 360, Width = heroW - 60, Height = 70 },
                        Alignment = TextAlignment.Center
                    });

                    // Right Supporting Cards
                    float rightX = 80 + heroW + 40;
                    float rightW = w - rightX - 80;
                    int count = Math.Min(3, contentPoints.Count);
                    float rCardH = (heroH - (count - 1) * 20) / count;

                    for (int i = 0; i < count; i++)
                    {
                        float ry = 160 + i * (rCardH + 20);
                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"KPI_Card_{i + 1}",
                            Type = ShapeType.RoundedRectangle,
                            CornerRadius = 16f,
                            FillColor = 0xFFFFFF,
                            StrokeColor = 0xE2E8F0,
                            StrokeWidth = 1.5f,
                            Transform = new Transform2D { X = rightX, Y = ry, Width = rightW, Height = rCardH }
                        });

                        s.Shapes.Add(new ShapeNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"KPI_CardText_{i + 1}",
                            Type = ShapeType.TextBox,
                            Text = contentPoints[i],
                            FontName = "Segoe UI",
                            FontSize = 19f,
                            TextColor = 0x1E293B,
                            Transform = new Transform2D { X = rightX + 24, Y = ry + rCardH * 0.2f, Width = rightW - 48, Height = rCardH * 0.6f }
                        });
                    }
                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), rect);
                    // Title
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 23, 42)), rect.X + 8, rect.Y + 8, rect.Width * 0.4f, 5);

                    // Hero purple box
                    float hw = rect.Width * 0.36f;
                    float hh = rect.Height - 22;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(79, 70, 229)), rect.X + 8, rect.Y + 16, hw, hh);
                    g.FillRectangle(Brushes.White, rect.X + 16, rect.Y + 28, hw - 16, 8); // metric
                    g.FillRectangle(new SolidBrush(Color.FromArgb(224, 231, 255)), rect.X + 16, rect.Y + 40, hw - 16, 4);

                    // Right cards
                    float rx = rect.X + 8 + hw + 6;
                    float rw = rect.Width - rx - 8;
                    float rch = (hh - 8) / 3f;
                    for (int i = 0; i < 3; i++)
                    {
                        float ry = rect.Y + 16 + i * (rch + 4);
                        g.FillRectangle(Brushes.White, rx, ry, rw, rch);
                        g.DrawRectangle(new Pen(Color.FromArgb(226, 232, 240)), rx, ry, rw, rch);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 116, 139)), rx + 4, ry + 4, rw - 8, 3);
                    }
                }
            });

            // =========================================================================
            // IDEA 8: Side-by-Side Dual Comparison (So sánh Song song)
            // =========================================================================
            ideas.Add(new SlideDesignIdea
            {
                Id = "dual_comparison",
                Title = "So Sánh Hai Cột Song Song",
                Category = "So sánh",
                Description = "Định dạng đối chiếu rõ ràng giữa Hiện trạng và Giải pháp hoặc Thách thức và Cơ hội.",
                BackgroundColor = 0xFFFFFF,
                AccentColor = 0x0284C7,
                Apply = (s, w, h) =>
                {
                    s.BgColor = 0xFFFFFF;
                    s.IsGradientBg = false;
                    s.Shapes.Clear();

                    // Title
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Title",
                        Type = ShapeType.TextBox,
                        PlaceholderType = "Title",
                        Text = slideTitle,
                        FontName = "Segoe UI",
                        FontSize = 42f,
                        Bold = true,
                        TextColor = 0x0F172A,
                        Transform = new Transform2D { X = 80, Y = 60, Width = w - 160, Height = 65 },
                        Alignment = TextAlignment.Center
                    });

                    // 2 Comparison Columns
                    float colGap = 40f;
                    float colW = (w - 160 - colGap) / 2f;
                    float colY = 160f;
                    float colH = h - colY - 90f;

                    // Left Column (Neutral / Current State)
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Col1_Bg",
                        Type = ShapeType.RoundedRectangle,
                        CornerRadius = 18f,
                        FillColor = 0xF8FAFC,
                        StrokeColor = 0xE2E8F0,
                        StrokeWidth = 1.5f,
                        Transform = new Transform2D { X = 80, Y = colY, Width = colW, Height = colH }
                    });

                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Col1_Header",
                        Type = ShapeType.TextBox,
                        Text = "HIỆN TRẠNG & THÁCH THỨC",
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        Bold = true,
                        TextColor = 0x64748B,
                        Transform = new Transform2D { X = 110, Y = colY + 30, Width = colW - 60, Height = 35 }
                    });

                    string leftText = (contentPoints.Count > 0) ? contentPoints[0] : "Các vấn đề còn tồn đọng và hạn chế hiện hành";
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Col1_Text",
                        Type = ShapeType.TextBox,
                        Text = leftText,
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        TextColor = 0x334155,
                        Transform = new Transform2D { X = 110, Y = colY + 80, Width = colW - 60, Height = colH - 110 }
                    });

                    // Right Column (Primary Accent / Future Solution)
                    float rightColX = 80 + colW + colGap;
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Col2_Bg",
                        Type = ShapeType.RoundedRectangle,
                        CornerRadius = 18f,
                        FillColor = 0xF0F9FF,
                        StrokeColor = 0xBAE6FD,
                        StrokeWidth = 2f,
                        Transform = new Transform2D { X = rightColX, Y = colY, Width = colW, Height = colH }
                    });

                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Col2_Header",
                        Type = ShapeType.TextBox,
                        Text = "MỤC TIÊU & GIẢI PHÁP ĐỘT PHÁ",
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        Bold = true,
                        TextColor = 0x0284C7,
                        Transform = new Transform2D { X = rightColX + 30, Y = colY + 30, Width = colW - 60, Height = 35 }
                    });

                    string rightText = (contentPoints.Count > 1) ? contentPoints[1] : (contentPoints.Count > 0 ? contentPoints[0] : "Lộ trình cải tiến đột phá và kết quả kỳ vọng");
                    s.Shapes.Add(new ShapeNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Comp_Col2_Text",
                        Type = ShapeType.TextBox,
                        Text = rightText,
                        FontName = "Segoe UI",
                        FontSize = 20f,
                        TextColor = 0x0369A1,
                        Transform = new Transform2D { X = rightColX + 30, Y = colY + 80, Width = colW - 60, Height = colH - 110 }
                    });

                    s.IsThumbnailDirty = true;
                },
                DrawMiniPreview = (g, rect) =>
                {
                    g.FillRectangle(Brushes.White, rect);
                    // Title
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 23, 42)), rect.X + rect.Width * 0.25f, rect.Y + 8, rect.Width * 0.5f, 5);

                    // 2 columns
                    float cw = (rect.Width - 22) / 2f;
                    float cy = rect.Y + 18;
                    float ch = rect.Height - 26;

                    // Col 1
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), rect.X + 8, cy, cw, ch);
                    g.DrawRectangle(new Pen(Color.FromArgb(226, 232, 240)), rect.X + 8, cy, cw, ch);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(100, 116, 139)), rect.X + 12, cy + 6, cw * 0.6f, 3);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(148, 163, 184)), rect.X + 12, cy + 14, cw - 8, 2);

                    // Col 2
                    float c2x = rect.X + 8 + cw + 6;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 249, 255)), c2x, cy, cw, ch);
                    g.DrawRectangle(new Pen(Color.FromArgb(186, 230, 253)), c2x, cy, cw, ch);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(2, 132, 199)), c2x + 12, cy + 6, cw * 0.6f, 3);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(3, 105, 161)), c2x + 12, cy + 14, cw - 8, 2);
                }
            });

            return ideas;
        }
    }
}
