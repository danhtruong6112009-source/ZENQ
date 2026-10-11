using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class MoreAnimationsModalForm : Form
    {
        public event Action<AnimationType> OnAnimationSelected;

        private SlidesView _slidesView;
        private AnimationType _selectedAnimation;
        private string _activeCategory = "All";
        private string _searchQuery = "";
        private FlowLayoutPanel _cardsPanel;
        private CheckBox _chkPreview;
        private AnimationType _initialAnimation;

        private class AnimationItem
        {
            public AnimationType Type;
            public string NameVi;
            public string NameEn;
            public string Category; // "Exclusive", "Entrance", "Emphasis", "Exit", "MotionPaths"
            public string Description;
        }

        private List<AnimationItem> _allAnimations = new List<AnimationItem>();

        public MoreAnimationsModalForm(SlidesView slidesView, string initialCategory = "All", AnimationType currentAnim = AnimationType.None)
        {
            _slidesView = slidesView;
            _initialAnimation = currentAnim;
            _selectedAnimation = (currentAnim != AnimationType.None) ? currentAnim : AnimationType.Appear;
            _activeCategory = !string.IsNullOrEmpty(initialCategory) ? initialCategory : "All";

            InitializeData();

            Text = "Thêm hiệu ứng hoạt họa - ZenQ Slides";
            Size = new Size(800, 580);
            MinimumSize = new Size(740, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f);
            ShowInTaskbar = false;

            BuildUI();
        }

        private void InitializeData()
        {
            // 🌟 Exclusive GenOffice Effects
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_NeonPulse, NameVi = "Phát sáng Neon", NameEn = "Neon Glow Pulse", Category = "Exclusive", Description = "Xung nhịp phát sáng viền Neon đa tầng kết hợp hiệu ứng nở rộng" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_LiquidMorph, NameVi = "Chảy lỏng ma thuật", NameEn = "Liquid Morph", Category = "Exclusive", Description = "Biến dạng nén dẹp đàn hồi chất lỏng siêu thực" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_Cinematic3DFlip, NameVi = "Lật 3D điện ảnh", NameEn = "3D Flip & Flare", Category = "Exclusive", Description = "Lật phối cảnh không gian 3D kèm tia lóe sáng lướt chéo" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_GlitchHologram, NameVi = "Nhiễu sóng Hologram", NameEn = "Cyber Hologram", Category = "Exclusive", Description = "Phân tách sắc sai RGB và sọc quét Hologram Sci-Fi" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_KineticSnap, NameVi = "Động năng nam châm", NameEn = "Kinetic Snap", Category = "Exclusive", Description = "Lao tới tốc độ cao và giật lùi đàn hồi theo lực hút nam châm" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_VortexSpiral, NameVi = "Xoáy lốc vũ trụ", NameEn = "Cosmic Vortex", Category = "Exclusive", Description = "Xoáy tròn từ vô cực về tâm theo đường xoắn ốc" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_OrigamiUnfold, NameVi = "Gấp giấy Origami 3D", NameEn = "3D Origami Unfold", Category = "Exclusive", Description = "Mở cánh gấp Origami đa chiều trong không gian" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_StarlightAssembly, NameVi = "Ngưng tụ ánh sao", NameEn = "Starlight Burst", Category = "Exclusive", Description = "Hạt sao từ xa tụ về tâm và bùng nổ lóe sáng" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_ElasticJellyPop, NameVi = "Thạch Jelly đàn hồi", NameEn = "Jelly Pop", Category = "Exclusive", Description = "Bật nảy rung chấn biến dạng thạch Jelly tự nhiên" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GenOffice_GoldenRibbonSwirl, NameVi = "Dải lụa hoàng kim", NameEn = "Golden Ribbon", Category = "Exclusive", Description = "Lướt sóng lụa xoay vòng 3D kèm màu vàng kim lấp lánh" });

            // 🟢 Entrance Effects
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Appear, NameVi = "Xuất hiện", NameEn = "Appear", Category = "Entrance", Description = "Hiển thị tức thì" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.FadeIn, NameVi = "Mờ dần vào", NameEn = "Fade", Category = "Entrance", Description = "Mờ dần hiện rõ" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.FlyIn, NameVi = "Bay vào", NameEn = "Fly In", Category = "Entrance", Description = "Bay vào từ các cạnh trang chiếu" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.FloatIn, NameVi = "Trôi nổi vào", NameEn = "Float In", Category = "Entrance", Description = "Trôi nhẹ nhàng vào vị trí" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.SplitIn, NameVi = "Tách đôi", NameEn = "Split In", Category = "Entrance", Description = "Mở rộng từ giữa ra hai phía" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.WipeIn, NameVi = "Lau vào", NameEn = "Wipe In", Category = "Entrance", Description = "Quét dần qua bề mặt" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.ShapeIn, NameVi = "Hình dạng", NameEn = "Shape In", Category = "Entrance", Description = "Mở rộng theo khối hình" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.WheelIn, NameVi = "Bánh xe", NameEn = "Wheel In", Category = "Entrance", Description = "Quay tròn nan hoa như bánh xe" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.RandomBarsIn, NameVi = "Thanh ngẫu nhiên", NameEn = "Random Bars", Category = "Entrance", Description = "Các dải thanh ngẫu nhiên hiện dần" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GrowAndTurn, NameVi = "Lớn lên và xoay", NameEn = "Grow & Turn", Category = "Entrance", Description = "Phóng to từ nhỏ kết hợp xoay vòng" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.ZoomIn, NameVi = "Thu phóng vào", NameEn = "Zoom In", Category = "Entrance", Description = "Phóng to từ tâm ra ngoài" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Swivel, NameVi = "Xoay lật", NameEn = "Swivel", Category = "Entrance", Description = "Xoay lật quanh trục thẳng đứng" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Bounce, NameVi = "Nảy tưng", NameEn = "Bounce", Category = "Entrance", Description = "Rơi từ trên xuống và bật nảy nhiều lần" });

            // 🟡 Emphasis Effects
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Pulse, NameVi = "Xung nhịp", NameEn = "Pulse", Category = "Emphasis", Description = "Phập phồng nhẹ nhàng tạo điểm nhấn" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.ColorPulse, NameVi = "Xung nhịp màu", NameEn = "Color Pulse", Category = "Emphasis", Description = "Đổi sắc thái màu sắc chớp nháy" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Teeter, NameVi = "Bập bênh", NameEn = "Teeter", Category = "Emphasis", Description = "Lắc lư nghiêng qua lại hai bên" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Spin, NameVi = "Xoay tròn", NameEn = "Spin", Category = "Emphasis", Description = "Quay tròn quanh tâm" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.GrowShrink, NameVi = "Phóng to thu nhỏ", NameEn = "Grow/Shrink", Category = "Emphasis", Description = "Tăng kích thước tạm thời rồi co lại" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.BoldFlash, NameVi = "Chớp đậm", NameEn = "Bold Flash", Category = "Emphasis", Description = "Nhấp nháy độ đậm văn bản" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.BoldReveal, NameVi = "Hiện đậm", NameEn = "Bold Reveal", Category = "Emphasis", Description = "Làm nổi bật dần viền chữ" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Wave, NameVi = "Lượn sóng", NameEn = "Wave", Category = "Emphasis", Description = "Dao động lượn sóng liên tục" });

            // 🔴 Exit Effects
            _allAnimations.Add(new AnimationItem { Type = AnimationType.Disappear, NameVi = "Biến mất tức thì", NameEn = "Disappear", Category = "Exit", Description = "Biến mất ngay lập tức" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.FadeOut, NameVi = "Mờ dần ra", NameEn = "Fade Out", Category = "Exit", Description = "Mờ dần cho đến khi ẩn hoàn toàn" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.FlyOut, NameVi = "Bay ra ngoài", NameEn = "Fly Out", Category = "Exit", Description = "Bay ra ngoài cạnh trang chiếu" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.FloatOut, NameVi = "Trôi nổi ra", NameEn = "Float Out", Category = "Exit", Description = "Trôi nhẹ nhàng ra khỏi màn hình" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.SplitOut, NameVi = "Khép đôi", NameEn = "Split Out", Category = "Exit", Description = "Thu hẹp vào giữa rồi biến mất" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.WipeOut, NameVi = "Lau ra", NameEn = "Wipe Out", Category = "Exit", Description = "Quét dần biến mất" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.ShapeOut, NameVi = "Thu hình khối", NameEn = "Shape Out", Category = "Exit", Description = "Co lại theo hình khối" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.WheelOut, NameVi = "Bánh xe biến mất", NameEn = "Wheel Out", Category = "Exit", Description = "Quay tròn nan hoa biến mất" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.RandomBarsOut, NameVi = "Thanh ngẫu nhiên ra", NameEn = "Random Bars Out", Category = "Exit", Description = "Thanh dải ngẫu nhiên biến mất" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.ShrinkAndTurn, NameVi = "Thu nhỏ và xoay", NameEn = "Shrink & Turn", Category = "Exit", Description = "Thu nhỏ lại kết hợp xoay biến mất" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.ZoomOut, NameVi = "Thu phóng ra", NameEn = "Zoom Out", Category = "Exit", Description = "Thu nhỏ dần vào tâm" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.SwivelOut, NameVi = "Xoay lật biến mất", NameEn = "Swivel Out", Category = "Exit", Description = "Xoay lật trục dọc biến mất" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.BounceOut, NameVi = "Nảy tưng biến mất", NameEn = "Bounce Out", Category = "Exit", Description = "Nảy tưng rồi rơi ra khỏi slide" });

            // 🔵 Motion Paths
            _allAnimations.Add(new AnimationItem { Type = AnimationType.MotionLine, NameVi = "Đường thẳng (Lines)", NameEn = "Motion Line", Category = "MotionPaths", Description = "Di chuyển theo đường thẳng" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.MotionArc, NameVi = "Đường cong (Arcs)", NameEn = "Motion Arc", Category = "MotionPaths", Description = "Di chuyển theo quỹ đạo cung tròn" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.MotionTurn, NameVi = "Chuyển hướng (Turns)", NameEn = "Motion Turn", Category = "MotionPaths", Description = "Di chuyển vuông góc uốn lượn" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.MotionShape, NameVi = "Hình khối (Shapes)", NameEn = "Motion Shape", Category = "MotionPaths", Description = "Di chuyển vòng quanh hình elip/tròn" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.MotionLoop, NameVi = "Vòng lặp (Loops)", NameEn = "Motion Loop", Category = "MotionPaths", Description = "Di chuyển theo hình số 8 vô cực" });
            _allAnimations.Add(new AnimationItem { Type = AnimationType.MotionCustom, NameVi = "Đường tự vẽ (Custom Path)", NameEn = "Custom Freehand Path", Category = "MotionPaths", Description = "Dùng chuột vẽ quỹ đạo tự do bất kỳ" });
        }

        private void BuildUI()
        {
            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(79, 70, 229),
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(pnlHeader.ClientRectangle, Color.FromArgb(79, 70, 229), Color.FromArgb(147, 51, 234), 0f))
                {
                    e.Graphics.FillRectangle(lgb, pnlHeader.ClientRectangle);
                }
            };

            Label lblTitle = new Label
            {
                Text = "🌟 THƯ VIỆN HIỆU ỨNG HOẠT HỌA (ANIMATION EFFECTS)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(14, 8),
                BackColor = Color.Transparent
            };
            Label lblSub = new Label
            {
                Text = "Khám phá & áp dụng các hiệu ứng hoạt họa tiêu chuẩn và hiệu ứng độc quyền ZenQ",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(233, 213, 255),
                AutoSize = true,
                Location = new Point(16, 30),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            Controls.Add(pnlHeader);

            // 2. Filter Bar (Categories & Search)
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlFilter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(p, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
                }
            };

            // Category Tab Buttons
            string[] catKeys = new string[] { "All", "Exclusive", "Entrance", "Emphasis", "Exit", "MotionPaths" };
            string[] catNames = new string[] { "Tất cả", "🌟 Độc quyền", "Xuất hiện", "Nhấn mạnh", "Biến mất", "Chuyển động" };
            int[] btnWidths = new int[] { 58, 92, 74, 78, 72, 86 };
            Color[] catColors = new Color[] { Color.FromArgb(71, 85, 105), Color.FromArgb(147, 51, 234), Color.FromArgb(22, 163, 74), Color.FromArgb(202, 138, 4), Color.FromArgb(220, 38, 38), Color.FromArgb(37, 99, 235) };

            int btnX = 10;
            List<Button> tabButtons = new List<Button>();
            for (int i = 0; i < catKeys.Length; i++)
            {
                string key = catKeys[i];
                string label = catNames[i];
                Color themeCol = catColors[i];

                Button btnTab = new Button
                {
                    Text = label,
                    Tag = key,
                    Location = new Point(btnX, 7),
                    Size = new Size(btnWidths[i], 28),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.25f, key == _activeCategory ? FontStyle.Bold : FontStyle.Regular),
                    BackColor = (key == _activeCategory) ? Color.FromArgb(243, 232, 255) : Color.FromArgb(248, 250, 252),
                    ForeColor = (key == _activeCategory) ? themeCol : Color.FromArgb(71, 85, 105),
                    Cursor = Cursors.Hand
                };
                btnTab.FlatAppearance.BorderSize = (key == _activeCategory) ? 2 : 1;
                btnTab.FlatAppearance.BorderColor = (key == _activeCategory) ? themeCol : Color.FromArgb(226, 232, 240);

                btnTab.Click += (s, e) =>
                {
                    _activeCategory = (string)((Button)s).Tag;
                    foreach (var tb in tabButtons)
                    {
                        bool isSel = ((string)tb.Tag == _activeCategory);
                        tb.Font = new Font("Segoe UI", 8.25f, isSel ? FontStyle.Bold : FontStyle.Regular);
                        tb.BackColor = isSel ? Color.FromArgb(243, 232, 255) : Color.FromArgb(248, 250, 252);
                        tb.ForeColor = isSel ? Color.FromArgb(124, 58, 237) : Color.FromArgb(71, 85, 105);
                        tb.FlatAppearance.BorderSize = isSel ? 2 : 1;
                        tb.FlatAppearance.BorderColor = isSel ? Color.FromArgb(124, 58, 237) : Color.FromArgb(226, 232, 240);
                    }
                    RefreshCards();
                };

                tabButtons.Add(btnTab);
                pnlFilter.Controls.Add(btnTab);
                btnX += btnTab.Width + 4;
            }

            // Search Box with Cue Banner on the right (Far from the buttons, never cut off)
            int searchBoxW = 180;
            TextBox txtSearch = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlFilter.Width - searchBoxW - 14, 8),
                Width = searchBoxW,
                Font = new Font("Segoe UI", 8.5f),
                Text = ""
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchQuery = txtSearch.Text.Trim().ToLower();
                RefreshCards();
            };
            pnlFilter.Resize += (s, e) =>
            {
                txtSearch.Location = new Point(pnlFilter.Width - searchBoxW - 14, 8);
            };
            pnlFilter.Controls.Add(txtSearch);

            Controls.Add(pnlFilter);

            // 3. Bottom Action Bar
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
                }
            };

            _chkPreview = new CheckBox
            {
                Text = "Xem trước hoạt họa trên hình vẽ (Preview effect)",
                Checked = true,
                AutoSize = true,
                Location = new Point(16, 16),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlBottom.Controls.Add(_chkPreview);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlBottom.Width - 95, 10),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            pnlBottom.Controls.Add(btnCancel);

            Button btnOK = new Button
            {
                Text = "Áp dụng",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlBottom.Width - 185, 10),
                Size = new Size(84, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) =>
            {
                ApplySelectionAndClose();
            };
            pnlBottom.Controls.Add(btnOK);

            pnlBottom.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(pnlBottom.Width - 95, 10);
                btnOK.Location = new Point(pnlBottom.Width - 185, 10);
            };

            Controls.Add(pnlBottom);

            // 4. Cards Scroll Panel
            _cardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12),
                WrapContents = true
            };
            Controls.Add(_cardsPanel);

            _cardsPanel.BringToFront();
            RefreshCards();
        }

        private void RefreshCards()
        {
            _cardsPanel.SuspendLayout();
            _cardsPanel.Controls.Clear();

            foreach (var item in _allAnimations)
            {
                if (_activeCategory != "All" && item.Category != _activeCategory)
                    continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    bool match = item.NameVi.ToLower().Contains(_searchQuery) ||
                                 item.NameEn.ToLower().Contains(_searchQuery) ||
                                 item.Description.ToLower().Contains(_searchQuery);
                    if (!match) continue;
                }

                Panel card = CreateAnimationCard(item);
                _cardsPanel.Controls.Add(card);
            }

            _cardsPanel.ResumeLayout();
        }

        private Panel CreateAnimationCard(AnimationItem item)
        {
            int cardW = 142;
            int cardH = 96;

            Panel card = new Panel
            {
                Size = new Size(cardW, cardH),
                Margin = new Padding(6),
                BackColor = (_selectedAnimation == item.Type) ? Color.FromArgb(237, 233, 254) : Color.White,
                Cursor = Cursors.Hand,
                Tag = item
            };

            bool isHovered = false;
            card.MouseEnter += (s, e) => { isHovered = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHovered = false; card.Invalidate(); };

            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                bool isSel = (_selectedAnimation == item.Type);
                Rectangle rect = new Rectangle(0, 0, cardW - 1, cardH - 1);

                // Background & Border
                if (isSel)
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(237, 233, 254)))
                    using (Pen sp = new Pen(Color.FromArgb(124, 58, 237), 1.8f))
                    {
                        g.FillRectangle(sb, rect);
                        g.DrawRectangle(sp, rect);
                    }
                }
                else if (isHovered)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(245, 243, 255)))
                    using (Pen hp = new Pen(Color.FromArgb(192, 132, 252), 1.2f))
                    {
                        g.FillRectangle(hb, rect);
                        g.DrawRectangle(hp, rect);
                    }
                }
                else
                {
                    using (SolidBrush nb = new SolidBrush(Color.White))
                    using (Pen np = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        g.FillRectangle(nb, rect);
                        g.DrawRectangle(np, rect);
                    }
                }

                // PRO badge for exclusive
                if (item.Category == "Exclusive")
                {
                    using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(243, 232, 255)))
                    using (Pen badgeP = new Pen(Color.FromArgb(192, 132, 252), 1f))
                    using (Font badgeF = new Font("Segoe UI", 6.8f, FontStyle.Bold))
                    {
                        Rectangle bRect = new Rectangle(4, 4, 30, 13);
                        g.FillRectangle(badgeBg, bRect);
                        g.DrawRectangle(badgeP, bRect);
                        g.DrawString("PRO", badgeF, Brushes.Purple, 7, 3);
                    }
                }

                // Vector Icon
                AnimationsGalleryDropDownForm.DrawAnimationVector(g, cardW / 2, 28, item.Type, item.Category);

                // Title label
                using (Font nameF = new Font("Segoe UI", 8.25f, isSel ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(isSel ? Color.FromArgb(91, 33, 182) : Color.FromArgb(30, 41, 59)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(item.NameVi, nameF, tb, new RectangleF(2, 58, cardW - 4, 20), sf);
                }

                // English sub-label
                using (Font subF = new Font("Segoe UI", 7.0f, FontStyle.Regular))
                using (SolidBrush stb = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(item.NameEn, subF, stb, new RectangleF(2, 76, cardW - 4, 16), sf);
                }
            };

            card.Click += (s, e) =>
            {
                _selectedAnimation = item.Type;
                foreach (Control c in _cardsPanel.Controls) c.Invalidate();

                if (_chkPreview.Checked && _slidesView != null)
                {
                    if (item.Type == AnimationType.MotionCustom)
                    {
                        // Freehand mode
                    }
                    else
                    {
                        _slidesView.ApplyOrAddShapeAnimation(item.Type, false);
                        _slidesView.PlayAnimationPreview();
                    }
                }
            };

            card.DoubleClick += (s, e) =>
            {
                _selectedAnimation = item.Type;
                ApplySelectionAndClose();
            };

            return card;
        }

        private void ApplySelectionAndClose()
        {
            if (OnAnimationSelected != null)
            {
                OnAnimationSelected(_selectedAnimation);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
