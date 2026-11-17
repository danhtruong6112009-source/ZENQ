using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Popup gợi ý lệnh Slash Commands (/) dạng UserControl nội bộ (In-Panel Control).
    /// Khắc phục 100% lỗi cướp focus của ToolStripDropDown: Không bao giờ làm mất con nháy (caret) của ô nhập,
    /// cho phép người dùng xoá liên tục nhiều ký tự hoặc xoá dấu '/' ngay lập tức mà không cần click chuột.
    /// </summary>
    public class AiSlashCommandPopup : UserControl
    {
        private List<AiSlashCommand> _commands = new List<AiSlashCommand>();
        private int _selectedIndex = 0;
        private int _hoverIndex = -1;
        private int _scrollOffset = 0;
        private const int ItemHeight = 38;
        private const int MaxVisibleItems = 5;

        public event Action<AiSlashCommand> CommandSelected;

        public AiSlashCommandPopup()
        {
            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.Selectable, false);
            this.TabStop = false; // Ngăn chặn việc chiếm TabStop hay Focus từ txtInput
            this.BackColor = Color.White;
            this.Cursor = Cursors.Hand;
            this.Visible = false;
        }

        public void UpdateList(List<AiSlashCommand> commands)
        {
            _commands = commands ?? new List<AiSlashCommand>();
            _selectedIndex = 0;
            _hoverIndex = -1;
            _scrollOffset = 0;

            int count = Math.Min(MaxVisibleItems, _commands.Count);
            int totalH = Math.Max(42, count * ItemHeight + 8);
            this.Height = totalH;
            this.Invalidate();
        }

        public void Close()
        {
            this.Visible = false;
        }

        public bool SelectNext()
        {
            if (_commands == null || _commands.Count == 0) return false;
            _selectedIndex = (_selectedIndex + 1) % _commands.Count;
            EnsureVisible(_selectedIndex);
            this.Invalidate();
            return true;
        }

        public bool SelectPrevious()
        {
            if (_commands == null || _commands.Count == 0) return false;
            _selectedIndex = (_selectedIndex - 1 + _commands.Count) % _commands.Count;
            EnsureVisible(_selectedIndex);
            this.Invalidate();
            return true;
        }

        private void EnsureVisible(int index)
        {
            if (index < _scrollOffset)
            {
                _scrollOffset = index;
            }
            else if (index >= _scrollOffset + MaxVisibleItems)
            {
                _scrollOffset = index - MaxVisibleItems + 1;
            }
        }

        public AiSlashCommand GetSelectedCommand()
        {
            if (_commands != null && _selectedIndex >= 0 && _selectedIndex < _commands.Count)
            {
                return _commands[_selectedIndex];
            }
            return null;
        }

        public void ConfirmSelection()
        {
            var cmd = GetSelectedCommand();
            if (cmd != null)
            {
                this.Visible = false;
                CommandSelected?.Invoke(cmd);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_commands.Count > MaxVisibleItems)
            {
                int maxOffset = _commands.Count - MaxVisibleItems;
                if (e.Delta < 0) _scrollOffset = Math.Min(maxOffset, _scrollOffset + 1);
                else _scrollOffset = Math.Max(0, _scrollOffset - 1);
                this.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = _scrollOffset + (e.Y - 4) / ItemHeight;
            if (idx >= 0 && idx < _commands.Count && idx != _hoverIndex)
            {
                _hoverIndex = idx;
                _selectedIndex = idx;
                this.Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int idx = _scrollOffset + (e.Y - 4) / ItemHeight;
            if (idx >= 0 && idx < _commands.Count)
            {
                _selectedIndex = idx;
                ConfirmSelection();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var frame = AiGpuPaint.Begin(this, e);
            var g = frame.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Đổ bóng & Viền bo tròn thanh lịch
            Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath gp = RoundRect(bounds, 8))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(bgBrush, gp);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                {
                    g.DrawPath(borderPen, gp);
                }
            }

            if (_commands == null || _commands.Count == 0)
            {
                using (Font emptyFont = new Font("Segoe UI", 9f, FontStyle.Italic))
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(AppL10n.T("Không tìm thấy lệnh phù hợp...", "No matching commands found..."), emptyFont, textBrush, new PointF(16, 12));
                }
                return;
            }

            int visibleCount = Math.Min(MaxVisibleItems, _commands.Count - _scrollOffset);
            using (Font nameFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
            using (Font descFont = new Font("Segoe UI", 8.2f, FontStyle.Regular))
            using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush descBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush selNameBrush = new SolidBrush(Color.FromArgb(67, 56, 202)))
            {
                for (int i = 0; i < visibleCount; i++)
                {
                    int itemIdx = _scrollOffset + i;
                    var cmd = _commands[itemIdx];
                    int itemY = 4 + i * ItemHeight;
                    Rectangle itemRect = new Rectangle(4, itemY, this.Width - 8, ItemHeight);

                    bool isSelected = (itemIdx == _selectedIndex);
                    if (isSelected)
                    {
                        using (GraphicsPath gp = RoundRect(itemRect, 6))
                        using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(238, 242, 255)))
                        {
                            g.FillPath(bgBrush, gp);
                        }
                    }

                    // Biểu tượng glyph
                    Rectangle iconRect = new Rectangle(itemRect.X + 8, itemY + 9, 20, 20);
                    Color iconCol = isSelected ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139);
                    FluentIconRenderer.DrawGlyph(g, cmd.IconGlyph, iconRect, iconCol, 13.5f);

                    // Tên lệnh & mô tả
                    PointF namePt = new PointF(itemRect.X + 34, itemY + 3);
                    g.DrawString(cmd.DisplayName ?? cmd.Name, nameFont, isSelected ? selNameBrush : nameBrush, namePt);

                    PointF descPt = new PointF(itemRect.X + 34, itemY + 20);
                    string shortDesc = cmd.Description;
                    if (shortDesc != null && shortDesc.Length > 44) shortDesc = shortDesc.Substring(0, 44) + "...";
                    g.DrawString(shortDesc, descFont, descBrush, descPt);
                }
            }

            // Thanh cuộn mini nếu có nhiều hơn 5 mục
            if (_commands.Count > MaxVisibleItems)
            {
                int trackH = this.Height - 12;
                int thumbH = Math.Max(16, trackH * MaxVisibleItems / _commands.Count);
                int maxOffset = _commands.Count - MaxVisibleItems;
                int thumbY = 6 + (trackH - thumbH) * _scrollOffset / maxOffset;
                Rectangle thumbR = new Rectangle(this.Width - 6, thumbY, 3, thumbH);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, 203, 213, 225)))
                {
                    g.FillRectangle(sb, thumbR);
                }
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
