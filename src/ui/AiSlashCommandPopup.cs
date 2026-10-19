using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Popup thả xuống gợi ý các lệnh Slash Commands (/) khi người dùng gõ "/" trong ô chat AI.
    /// Thiết kế hiện đại chuẩn Google Workspace / Fluent Design: đổ bóng mềm, viền bo tròn, hỗ trợ phím mũi tên và Enter.
    /// </summary>
    public class AiSlashCommandPopup : ToolStripDropDown
    {
        private List<AiSlashCommand> _commands;
        private int _selectedIndex = 0;
        private ToolStripControlHost _host;
        private SlashCommandListControl _listControl;

        public event Action<AiSlashCommand> CommandSelected;

        public AiSlashCommandPopup()
        {
            this.AutoClose = true;
            this.DropShadowEnabled = true;
            this.Padding = Padding.Empty;
            this.Margin = Padding.Empty;
            this.BackColor = Color.White;

            _listControl = new SlashCommandListControl();
            _listControl.ItemClicked += (cmd) =>
            {
                this.Close();
                if (CommandSelected != null) CommandSelected(cmd);
            };

            _host = new ToolStripControlHost(_listControl);
            _host.Margin = Padding.Empty;
            _host.Padding = Padding.Empty;
            _host.AutoSize = false;
            this.Items.Add(_host);
        }

        public void UpdateList(List<AiSlashCommand> commands)
        {
            _commands = commands;
            _selectedIndex = 0;
            _listControl.SetCommands(commands);

            int itemH = 40;
            int totalH = Math.Min(320, Math.Max(40, commands.Count * itemH + 8));
            _listControl.Size = new Size(330, totalH);
            _host.Size = new Size(330, totalH);
            this.Size = new Size(330, totalH);
        }

        public bool SelectNext()
        {
            if (_commands == null || _commands.Count == 0) return false;
            _selectedIndex = (_selectedIndex + 1) % _commands.Count;
            _listControl.SelectedIndex = _selectedIndex;
            _listControl.Invalidate();
            return true;
        }

        public bool SelectPrevious()
        {
            if (_commands == null || _commands.Count == 0) return false;
            _selectedIndex = (_selectedIndex - 1 + _commands.Count) % _commands.Count;
            _listControl.SelectedIndex = _selectedIndex;
            _listControl.Invalidate();
            return true;
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
                this.Close();
                if (CommandSelected != null) CommandSelected(cmd);
            }
        }

        private class SlashCommandListControl : Control
        {
            private List<AiSlashCommand> _items = new List<AiSlashCommand>();
            private int _hoverIndex = -1;
            public int SelectedIndex { get; set; } = 0;
            public event Action<AiSlashCommand> ItemClicked;

            public SlashCommandListControl()
            {
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.BackColor = Color.White;
                this.Cursor = Cursors.Hand;
            }

            public void SetCommands(List<AiSlashCommand> items)
            {
                _items = items ?? new List<AiSlashCommand>();
                _hoverIndex = -1;
                SelectedIndex = 0;
                this.Invalidate();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int idx = (e.Y - 4) / 40;
                if (idx >= 0 && idx < _items.Count && idx != _hoverIndex)
                {
                    _hoverIndex = idx;
                    SelectedIndex = idx;
                    this.Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hoverIndex = -1;
                this.Invalidate();
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int idx = (e.Y - 4) / 40;
                if (idx >= 0 && idx < _items.Count)
                {
                    if (ItemClicked != null) ItemClicked(_items[idx]);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Nền trắng tinh tế
                g.Clear(Color.White);

                // Viền khung popup
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }

                if (_items == null || _items.Count == 0)
                {
                    using (Font emptyFont = new Font("Segoe UI", 9f, FontStyle.Italic))
                    using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        g.DrawString("Không tìm thấy lệnh phù hợp...", emptyFont, textBrush, new PointF(16, 12));
                    }
                    return;
                }

                int itemH = 40;
                using (Font nameFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
                using (Font descFont = new Font("Segoe UI", 8.2f, FontStyle.Regular))
                using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (SolidBrush descBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                using (SolidBrush selNameBrush = new SolidBrush(Color.FromArgb(67, 56, 202)))
                {
                    for (int i = 0; i < _items.Count; i++)
                    {
                        var cmd = _items[i];
                        int itemY = 4 + i * itemH;
                        Rectangle itemRect = new Rectangle(4, itemY, this.Width - 8, itemH);

                        bool isSelected = (i == SelectedIndex);
                        if (isSelected)
                        {
                            using (GraphicsPath gp = RoundRect(itemRect, 6))
                            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(238, 242, 255)))
                            {
                                g.FillPath(bgBrush, gp);
                            }
                        }

                        // Vẽ icon glyph
                        Rectangle iconRect = new Rectangle(itemRect.X + 8, itemY + 10, 20, 20);
                        Color iconCol = isSelected ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139);
                        FluentIconRenderer.DrawGlyph(g, cmd.IconGlyph, iconRect, iconCol, 14f);

                        // Vẽ Tên lệnh & Mô tả
                        PointF namePt = new PointF(itemRect.X + 34, itemY + 4);
                        g.DrawString(cmd.DisplayName ?? cmd.Name, nameFont, isSelected ? selNameBrush : nameBrush, namePt);

                        PointF descPt = new PointF(itemRect.X + 34, itemY + 21);
                        string shortDesc = cmd.Description;
                        if (shortDesc != null && shortDesc.Length > 42) shortDesc = shortDesc.Substring(0, 42) + "...";
                        g.DrawString(shortDesc, descFont, descBrush, descPt);
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
}
