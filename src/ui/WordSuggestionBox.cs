using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Graphics = ZenQOffice.UI.D2DGraphics;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bảng gợi ý từ ngữ thông minh dạng popup nổi (IDE-style IntelliSense Autocomplete Box).
    /// Hiển thị 3 từ/cụm từ gợi ý ngay cạnh con trỏ soạn thảo.
    /// Hỗ trợ phím Tab/Enter để chọn, mũi tên Lên/Xuống để duyệt, hoặc nhấp chuột trực tiếp.
    /// </summary>
    public class WordSuggestionBox
    {
        public bool IsVisible { get; set; }
        public Rectangle Bounds { get; set; }
        public List<string> Suggestions { get; set; }
        public int SelectedIndex { get; set; }
        public int HoverIndex { get; set; }
        public string CurrentWord { get; set; }
        public Action<string> OnApplySuggestion { get; set; }

        private const int BoxWidth = 240;
        private const int ItemHeight = 28;
        private const int FooterHeight = 20;

        public WordSuggestionBox()
        {
            Suggestions = new List<string>();
            SelectedIndex = 0;
            HoverIndex = -1;
            IsVisible = false;
        }

        public void Show(int screenX, int screenY, int clientW, int clientH, string word, List<string> suggestions, Action<string> onApply)
        {
            if (suggestions == null || suggestions.Count == 0 || string.IsNullOrEmpty(word))
            {
                Hide();
                return;
            }

            Suggestions = new List<string>(suggestions);
            SelectedIndex = 0;
            HoverIndex = -1;
            CurrentWord = word;
            OnApplySuggestion = onApply;

            int totalH = Suggestions.Count * ItemHeight + FooterHeight + 6;
            int x = screenX;
            int y = screenY;

            // Đảm bảo không tràn viền dưới màn hình
            if (y + totalH > clientH - 30)
            {
                y = Math.Max(150, screenY - totalH - 24);
            }
            // Đảm bảo không tràn viền phải
            if (x + BoxWidth > clientW - 20)
            {
                x = Math.Max(10, clientW - BoxWidth - 20);
            }

            Bounds = new Rectangle(x, y, BoxWidth, totalH);
            IsVisible = true;
        }

        public void Reposition(int screenX, int screenY, int clientW, int clientH)
        {
            if (!IsVisible || Suggestions == null || Suggestions.Count == 0) return;
            int totalH = Suggestions.Count * ItemHeight + FooterHeight + 6;
            int x = screenX;
            int y = screenY;

            if (y + totalH > clientH - 30)
            {
                y = Math.Max(150, screenY - totalH - 24);
            }
            if (x + BoxWidth > clientW - 20)
            {
                x = Math.Max(10, clientW - BoxWidth - 20);
            }

            Bounds = new Rectangle(x, y, BoxWidth, totalH);
        }

        public void Hide()
        {
            IsVisible = false;
            Suggestions.Clear();
            CurrentWord = "";
            OnApplySuggestion = null;
            Bounds = Rectangle.Empty;
        }

        public void MoveDown()
        {
            if (!IsVisible || Suggestions.Count == 0) return;
            SelectedIndex = (SelectedIndex + 1) % Suggestions.Count;
        }

        public void MoveUp()
        {
            if (!IsVisible || Suggestions.Count == 0) return;
            SelectedIndex = (SelectedIndex - 1 + Suggestions.Count) % Suggestions.Count;
        }

        public bool ApplySelected()
        {
            if (!IsVisible || Suggestions.Count == 0 || SelectedIndex < 0 || SelectedIndex >= Suggestions.Count)
            {
                return false;
            }

            string chosen = Suggestions[SelectedIndex];
            Action<string> callback = OnApplySuggestion;
            Hide();

            if (callback != null)
            {
                callback(chosen);
                return true;
            }
            return false;
        }

        public bool HandleMouseMove(int x, int y)
        {
            if (!IsVisible || !Bounds.Contains(x, y))
            {
                if (HoverIndex != -1)
                {
                    HoverIndex = -1;
                    return true;
                }
                return false;
            }

            int relY = y - (Bounds.Y + 3);
            if (relY >= 0 && relY < Suggestions.Count * ItemHeight)
            {
                int newHov = relY / ItemHeight;
                if (newHov != HoverIndex)
                {
                    HoverIndex = newHov;
                    return true;
                }
            }
            else
            {
                if (HoverIndex != -1)
                {
                    HoverIndex = -1;
                    return true;
                }
            }
            return false;
        }

        public bool HandleClick(int x, int y)
        {
            if (!IsVisible || !Bounds.Contains(x, y)) return false;

            int relY = y - (Bounds.Y + 3);
            if (relY >= 0 && relY < Suggestions.Count * ItemHeight)
            {
                int clickedIdx = relY / ItemHeight;
                if (clickedIdx >= 0 && clickedIdx < Suggestions.Count)
                {
                    SelectedIndex = clickedIdx;
                    return ApplySelected();
                }
            }
            return false;
        }

        public void Draw(System.Drawing.Graphics g)
        {
            if (g == null || !IsVisible || Suggestions == null || Suggestions.Count == 0) return;
            using (D2DGraphics d2d = new D2DGraphics(g, Bounds.Right, Bounds.Bottom))
            {
                Draw(d2d);
            }
        }

        public void Draw(Graphics g)
        {
            if (!IsVisible || Suggestions == null || Suggestions.Count == 0) return;

            SmoothingMode prevSmooth = g.SmoothingMode;
            PixelOffsetMode prevPix = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 1. Bóng mờ nhẹ quanh bảng (Drop Shadow)
            Rectangle shadowRect = new Rectangle(Bounds.X + 2, Bounds.Y + 2, Bounds.Width, Bounds.Height);
            using (GraphicsPath shadowPath = CreateRoundedPath(shadowRect, 6))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            // 2. Khung nền chính (Nền trắng sang trọng phong cách IDE)
            using (GraphicsPath cardPath = CreateRoundedPath(Bounds, 6))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    g.FillPath(bgBrush, cardPath);
                }

                // Đường viền màu xanh IDE tinh tế
                using (Pen borderPen = new Pen(Color.FromArgb(0, 120, 212), 1.2f))
                {
                    g.DrawPath(borderPen, cardPath);
                }
            }

            // 3. Vẽ từng mục gợi ý
            using (Font itemFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (Font itemBoldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font iconFont = new Font("Segoe UI Symbol", 9.0f, FontStyle.Regular))
            using (Font badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Font footerFont = new Font("Segoe UI", 7.0f, FontStyle.Regular))
            {
                int curY = Bounds.Y + 3;

                for (int i = 0; i < Suggestions.Count; i++)
                {
                    Rectangle itemRect = new Rectangle(Bounds.X + 2, curY, Bounds.Width - 4, ItemHeight);
                    bool isSelected = (i == SelectedIndex);
                    bool isHovered = (i == HoverIndex);

                    if (isSelected)
                    {
                        // Nền mục đang chọn (Fluent Light Blue)
                        using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(235, 245, 255)))
                        {
                            g.FillRectangle(selBrush, itemRect);
                        }

                        // Dải nhấn màu xanh dương bên trái (Accent Strip)
                        using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(0, 102, 204)))
                        {
                            g.FillRectangle(accentBrush, itemRect.X, itemRect.Y + 2, 3, itemRect.Height - 4);
                        }
                    }
                    else if (isHovered)
                    {
                        using (SolidBrush hovBrush = new SolidBrush(Color.FromArgb(245, 247, 250)))
                        {
                            g.FillRectangle(hovBrush, itemRect);
                        }
                    }

                    // Icon IDE (Tia chớp hoặc ngôi sao)
                    string iconChar = isSelected ? "⚡" : "✦";
                    Color iconCol = isSelected ? Color.FromArgb(0, 102, 204) : Color.FromArgb(148, 163, 184);
                    using (SolidBrush icBrush = new SolidBrush(iconCol))
                    {
                        g.DrawString(iconChar, iconFont, icBrush, itemRect.X + 8, itemRect.Y + 5);
                    }

                    // Tên từ / cụm từ gợi ý
                    string text = Suggestions[i];
                    Color txtCol = isSelected ? Color.FromArgb(15, 23, 42) : Color.FromArgb(51, 65, 85);
                    Font activeFont = isSelected ? itemBoldFont : itemFont;
                    using (SolidBrush tBrush = new SolidBrush(txtCol))
                    {
                        g.DrawString(text, activeFont, tBrush, itemRect.X + 26, itemRect.Y + 4);
                    }

                    // Phím tắt gợi ý "Tab ⇥" cho mục đang chọn
                    if (isSelected)
                    {
                        string badge = "Tab ⇥";
                        SizeF bSize = g.MeasureString(badge, badgeFont);
                        RectangleF badgeRect = new RectangleF(itemRect.Right - bSize.Width - 10, itemRect.Y + 5, bSize.Width + 6, 16);

                        using (SolidBrush bBg = new SolidBrush(Color.FromArgb(219, 234, 254)))
                        using (SolidBrush bTxt = new SolidBrush(Color.FromArgb(30, 64, 175)))
                        {
                            g.FillRectangle(bBg, badgeRect);
                            g.DrawString(badge, badgeFont, bTxt, badgeRect.X + 3, badgeRect.Y + 1);
                        }
                    }

                    curY += ItemHeight;
                }

                // 4. Thanh phân cách và chân bảng gợi ý (Footer Guide)
                g.DrawLine(new Pen(Color.FromArgb(241, 245, 249), 1f), Bounds.X + 6, curY + 1, Bounds.Right - 6, curY + 1);

                string guide = "↑↓ Di chuyển  •  Tab / Click Chọn  •  Esc Đóng";
                using (SolidBrush gBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(guide, footerFont, gBrush, Bounds.X + 10, curY + 4);
                }
            }

            g.SmoothingMode = prevSmooth;
            g.PixelOffsetMode = prevPix;
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
