using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class StyleEntry
    {
        public string Name { get; set; }
        public int Type { get; set; } // 0 = Paragraph (¶), 1 = Character (a), 2 = Linked (¶a), 3 = Table, 4 = List
        public int Priority { get; set; }
        public bool IsHideUntilUsed { get; set; }
        public bool IsHidden { get; set; }
        public bool IsPermitted { get; set; }
        public bool IsBuiltIn { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public Color TextColor { get; set; }
        public string Description { get; set; }

        public StyleEntry(string name, int type, int priority, bool hideUntilUsed, bool isBuiltIn, string fontName, float size, bool bold, bool italic, Color color, string desc)
        {
            Name = name;
            Type = type;
            Priority = priority;
            IsHideUntilUsed = hideUntilUsed;
            IsHidden = false;
            IsPermitted = true;
            IsBuiltIn = isBuiltIn;
            FontName = fontName;
            FontSize = size;
            IsBold = bold;
            IsItalic = italic;
            TextColor = color;
            Description = desc;
        }

        public string GetDisplayName(bool showPriority = false)
        {
            string suffix = IsHideUntilUsed ? " (Hide Until Used)" : (IsHidden ? " (Hidden)" : "");
            if (showPriority)
            {
                return Priority.ToString().PadRight(4) + Name + suffix;
            }
            return Name + suffix;
        }
    }

    public class ManageStylesForm : Form
    {
        public Action<string, float, Color, float, float, int, float, float, float, int, bool> OnApplyDefaults;

        private int _selectedTabIndex = 0; // 0 = Edit, 1 = Recommend, 2 = Restrict, 3 = Set Defaults
        private string[] _tabNames = new string[] { "Edit", "Recommend", "Restrict", "Set Defaults" };

        private List<StyleEntry> _stylesList;

        // Container Panels for 4 Tabs
        private Panel _pnlEdit;
        private Panel _pnlRecommend;
        private Panel _pnlRestrict;
        private Panel _pnlSetDefaults;

        // 1. Controls for "Edit" tab
        private ListBox _lstEditStyles;
        private Label _lblEditPreviewText;
        private Label _lblEditPreviewSize;
        private Label _lblEditDescription;
        private Button _btnEditModify;
        private Button _btnEditDelete;
        private Button _btnEditNewStyle;
        private ComboBox _cboEditSort;
        private CheckBox _chkEditShowRecOnly;

        // 2. Controls for "Recommend" tab
        private ListBox _lstRecommendStyles;
        private ComboBox _cboRecommendSort;
        private CheckBox _chkRecommendShowRecOnly;

        // 3. Controls for "Restrict" tab
        private ListBox _lstRestrictStyles;
        private ComboBox _cboRestrictSort;
        private CheckBox _chkRestrictShowRecOnly;

        // 4. Controls for "Set Defaults" tab
        private ListBox _lstFont;
        private ListBox _lstSize;
        private ComboBox _cboFontColor;
        private NumericUpDown _numLeft;
        private NumericUpDown _numRight;
        private ComboBox _cboSpecial;
        private NumericUpDown _numSpecialBy;
        private ComboBox _cboAlignment;
        private NumericUpDown _numBefore;
        private NumericUpDown _numAfter;
        private ComboBox _cboLineSpacing;
        private NumericUpDown _numLineSpacingAt;

        // Common bottom controls
        private RadioButton _radThisDoc;
        private RadioButton _radNewDocs;

        // Passed current settings
        private string _curFont;
        private float _curSize;
        private Color _curColor;
        private float _curLineSpacing;
        private float _curBefore;
        private float _curAfter;
        private int _curAlign;
        private List<string> _systemFonts;

        public ManageStylesForm(
            string curFont = "Calibri",
            float curSize = 11.0f,
            Color? curColor = null,
            float curLineSpacing = 1.15f,
            float curBefore = 0f,
            float curAfter = 8f,
            int curAlign = 0,
            List<string> systemFonts = null,
            int initialTab = 3)
        {
            _curFont = string.IsNullOrEmpty(curFont) ? "Calibri" : curFont;
            _curSize = curSize > 0 ? curSize : 11.0f;
            _curColor = curColor ?? Color.Black;
            _curLineSpacing = curLineSpacing > 0 ? curLineSpacing : 1.15f;
            _curBefore = curBefore;
            _curAfter = curAfter;
            _curAlign = curAlign;
            _systemFonts = systemFonts ?? new List<string>();
            _selectedTabIndex = Math.Max(0, Math.Min(3, initialTab));

            InitializeStylesData();
            InitializeComponent();
        }

        private void InitializeStylesData()
        {
            _stylesList = new List<StyleEntry>
            {
                new StyleEntry("Normal", 0, 1, false, true, _curFont, 11.0f, false, false, Color.Black,
                    "Font: (Default) +Body (" + _curFont + "), Left\nLine spacing: Multiple 1.15 li, Space\nAfter: 8 pt, Widow/Orphan control, Style: Show in the Styles gallery"),
                new StyleEntry("Default Paragraph Font", 1, 2, true, true, _curFont, 11.0f, false, false, Color.Black,
                    "The font of the Normal style with no additional character formatting applied."),
                new StyleEntry("No Spacing", 0, 2, false, true, _curFont, 11.0f, false, false, Color.Black,
                    "Font: (Default) +Body (" + _curFont + "), Left\nLine spacing: Single, Space After: 0 pt, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 1", 2, 10, false, true, "Calibri Light", 16.0f, true, false, Color.FromArgb(43, 87, 154),
                    "Font: (Default) +Headings (Calibri Light), 16 pt, Bold, Font color: Accent 1, Left\nSpace Before: 12 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 2", 2, 10, true, true, "Calibri Light", 13.0f, true, false, Color.FromArgb(43, 87, 154),
                    "Font: (Default) +Headings (Calibri Light), 13 pt, Bold, Font color: Accent 1, Left\nSpace Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 3", 2, 10, true, true, "Calibri Light", 12.0f, true, false, Color.FromArgb(68, 114, 196),
                    "Font: (Default) +Headings (Calibri Light), 12 pt, Bold, Font color: Accent 1, Left\nSpace Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 4", 2, 10, true, true, "Calibri Light", 11.0f, true, false, Color.FromArgb(68, 114, 196),
                    "Font: (Default) +Headings (Calibri Light), 11 pt, Bold, Font color: Accent 1, Left\nSpace Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 5", 2, 10, true, true, "Calibri Light", 11.0f, false, true, Color.FromArgb(68, 114, 196),
                    "Font: (Default) +Headings (Calibri Light), 11 pt, Italic, Font color: Accent 1, Left\nSpace Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 6", 2, 10, true, true, "Calibri Light", 10.5f, false, true, Color.FromArgb(89, 89, 89),
                    "Font: (Default) +Headings (Calibri Light), 10.5 pt, Italic, Font color: Text 1, Left\nSpace Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Heading 7", 2, 10, true, true, "Calibri Light", 10.0f, false, true, Color.FromArgb(89, 89, 89),
                    "Font: (Default) +Headings (Calibri Light), 10 pt, Italic, Font color: Text 1, Left\nSpace Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Title", 0, 11, false, true, "Calibri Light", 28.0f, false, false, Color.Black,
                    "Font: (Default) +Headings (Calibri Light), 28 pt, Left\nLine spacing: Single, Space After: 0 pt, Keep with next, Style: Show in the Styles gallery"),
                new StyleEntry("Subtitle", 0, 11, false, true, "Calibri", 11.0f, false, false, Color.FromArgb(89, 89, 89),
                    "Font: (Default) +Body (Calibri), 11 pt, Font color: Text 2, Left\nLine spacing: Single, Space Before: 0 pt, After: 8 pt, Style: Show in the Styles gallery"),
                new StyleEntry("Book Title", 0, 12, true, true, "Calibri Light", 18.0f, true, false, Color.FromArgb(43, 87, 154),
                    "Font: (Default) +Headings (Calibri Light), 18 pt, Bold, Font color: Accent 1, Left\nSpace Before: 18 pt, After: 6 pt"),
                new StyleEntry("Caption", 0, 35, true, true, "Calibri", 9.0f, false, true, Color.FromArgb(68, 114, 196),
                    "Font: (Default) +Body (Calibri), 9 pt, Italic, Font color: Accent 1, Left\nSpace After: 4 pt"),
                new StyleEntry("Emphasis", 1, 36, true, true, "Calibri", 11.0f, false, true, Color.Black,
                    "Font: (Default) +Body (Calibri), 11 pt, Italic, Character style")
            };
        }

        private void InitializeComponent()
        {
            Text = "Manage Styles";
            Size = new Size(540, 570);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(243, 244, 246);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // 1. Tab Header Bar
            Panel tabHeader = new Panel
            {
                Location = new Point(14, 10),
                Size = new Size(496, 32),
                BackColor = Color.Transparent
            };
            tabHeader.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int tabX = 0;
                for (int i = 0; i < _tabNames.Length; i++)
                {
                    string tName = _tabNames[i];
                    int tabW = (int)g.MeasureString(tName, Font).Width + 24;
                    Rectangle tabRect = new Rectangle(tabX, 2, tabW, 28);
                    bool isSel = (i == _selectedTabIndex);

                    if (isSel)
                    {
                        using (SolidBrush sb = new SolidBrush(Color.White)) g.FillRectangle(sb, tabRect);
                        using (Pen p = new Pen(Color.FromArgb(209, 213, 219)))
                        {
                            g.DrawLine(p, tabRect.Left, tabRect.Bottom, tabRect.Left, tabRect.Top);
                            g.DrawLine(p, tabRect.Left, tabRect.Top, tabRect.Right, tabRect.Top);
                            g.DrawLine(p, tabRect.Right, tabRect.Top, tabRect.Right, tabRect.Bottom);
                        }
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(17, 24, 39)))
                        {
                            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(tName, Font, tb, tabRect, sf);
                        }
                    }
                    else
                    {
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(75, 85, 99)))
                        {
                            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(tName, Font, tb, tabRect, sf);
                        }
                    }
                    tabX += tabW + 2;
                }
                using (Pen p = new Pen(Color.FromArgb(209, 213, 219)))
                {
                    g.DrawLine(p, 0, 30, tabHeader.Width, 30);
                }
            };

            tabHeader.MouseClick += (s, e) =>
            {
                Graphics g = tabHeader.CreateGraphics();
                int tabX = 0;
                for (int i = 0; i < _tabNames.Length; i++)
                {
                    string tName = _tabNames[i];
                    int tabW = (int)g.MeasureString(tName, Font).Width + 24;
                    Rectangle tabRect = new Rectangle(tabX, 2, tabW, 28);
                    if (tabRect.Contains(e.Location))
                    {
                        _selectedTabIndex = i;
                        tabHeader.Invalidate();
                        SwitchTabs();
                        break;
                    }
                    tabX += tabW + 2;
                }
            };
            Controls.Add(tabHeader);

            // Container Panel for Tab Contents
            Panel containerPanel = new Panel
            {
                Location = new Point(14, 41),
                Size = new Size(496, 430),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(containerPanel);

            // Build Tab 0: Edit
            _pnlEdit = BuildEditTabPanel();
            _pnlEdit.Dock = DockStyle.Fill;
            containerPanel.Controls.Add(_pnlEdit);

            // Build Tab 1: Recommend
            _pnlRecommend = BuildRecommendTabPanel();
            _pnlRecommend.Dock = DockStyle.Fill;
            containerPanel.Controls.Add(_pnlRecommend);

            // Build Tab 2: Restrict
            _pnlRestrict = BuildRestrictTabPanel();
            _pnlRestrict.Dock = DockStyle.Fill;
            containerPanel.Controls.Add(_pnlRestrict);

            // Build Tab 3: Set Defaults
            _pnlSetDefaults = BuildSetDefaultsTabPanel();
            _pnlSetDefaults.Dock = DockStyle.Fill;
            containerPanel.Controls.Add(_pnlSetDefaults);

            // Scope Radio Buttons at bottom of dialog
            _radThisDoc = new RadioButton
            {
                Text = "Only in this document",
                Location = new Point(24, 480),
                AutoSize = true,
                Checked = true
            };
            _radNewDocs = new RadioButton
            {
                Text = "New documents based on this template",
                Location = new Point(184, 480),
                AutoSize = true
            };
            Controls.Add(_radThisDoc);
            Controls.Add(_radNewDocs);

            // Bottom Buttons
            Button btnImport = new Button
            {
                Text = "Import/Export...",
                Location = new Point(14, 506),
                Size = new Size(110, 26),
                UseVisualStyleBackColor = true
            };
            btnImport.Click += (s, e) =>
            {
                using (OrganizerForm orgDlg = new OrganizerForm())
                {
                    orgDlg.ShowDialog(this);
                }
            };
            Controls.Add(btnImport);

            Button btnOK = new Button
            {
                Text = "OK",
                Location = new Point(340, 506),
                Size = new Size(80, 26),
                DialogResult = DialogResult.OK,
                UseVisualStyleBackColor = true
            };
            btnOK.Click += (s, e) =>
            {
                string selFont = _lstFont.SelectedItem != null ? _lstFont.SelectedItem.ToString() : "Calibri";
                if (selFont == "+Body") selFont = "Calibri";
                else if (selFont == "+Headings") selFont = "Calibri Light";

                float selSize = 11.0f;
                if (_lstSize.SelectedItem != null)
                {
                    float.TryParse(_lstSize.SelectedItem.ToString(), out selSize);
                }

                Color selColor = _curColor;
                if (_cboFontColor.SelectedIndex == 0) selColor = Color.Black; // Automatic
                else if (_cboFontColor.SelectedIndex == 1) selColor = Color.Black;
                else if (_cboFontColor.SelectedIndex == 2) selColor = Color.FromArgb(30, 58, 138);
                else if (_cboFontColor.SelectedIndex == 3) selColor = Color.FromArgb(29, 78, 216);
                else if (_cboFontColor.SelectedIndex == 4) selColor = Color.FromArgb(185, 28, 28);
                else if (_cboFontColor.SelectedIndex == 5) selColor = Color.FromArgb(4, 120, 87);

                float leftIndent = (float)_numLeft.Value;
                float rightIndent = (float)_numRight.Value;
                int align = _cboAlignment.SelectedIndex;
                float beforePt = (float)_numBefore.Value;
                float afterPt = (float)_numAfter.Value;
                float lineSpacingFactor = (float)_numLineSpacingAt.Value;
                int specialMode = _cboSpecial.SelectedIndex;
                bool isGlobalTemplate = _radNewDocs.Checked;

                if (OnApplyDefaults != null)
                {
                    OnApplyDefaults(selFont, selSize, selColor, leftIndent, rightIndent, align, beforePt, afterPt, lineSpacingFactor, specialMode, isGlobalTemplate);
                }
                Close();
            };
            Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(430, 506),
                Size = new Size(80, 26),
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = true
            };
            btnCancel.Click += (s, e) => Close();
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;

            SwitchTabs();
        }

        private void SwitchTabs()
        {
            _pnlEdit.Visible = (_selectedTabIndex == 0);
            _pnlRecommend.Visible = (_selectedTabIndex == 1);
            _pnlRestrict.Visible = (_selectedTabIndex == 2);
            _pnlSetDefaults.Visible = (_selectedTabIndex == 3);
        }

        // ==========================================
        // TAB 0: EDIT TAB (Screenshot 1)
        // ==========================================
        private Panel BuildEditTabPanel()
        {
            Panel pnl = new Panel { BackColor = Color.White };

            Label lblSort = new Label { Text = "Sort order:", Location = new Point(14, 12), AutoSize = true };
            _cboEditSort = new ComboBox
            {
                Location = new Point(80, 10),
                Size = new Size(160, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboEditSort.Items.AddRange(new object[] { "As Recommended", "Alphabetical", "By Type", "Based On" });
            _cboEditSort.SelectedIndex = 0;

            _chkEditShowRecOnly = new CheckBox
            {
                Text = "Show recommended styles only",
                Location = new Point(255, 12),
                AutoSize = true
            };

            Label lblSelect = new Label { Text = "Select a style to edit", Location = new Point(14, 40), AutoSize = true, ForeColor = Color.FromArgb(30, 30, 30) };

            _lstEditStyles = new ListBox
            {
                Location = new Point(14, 60),
                Size = new Size(464, 140),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22,
                Font = new Font("Segoe UI", 9.0f)
            };

            foreach (var st in _stylesList)
            {
                _lstEditStyles.Items.Add(st);
            }
            _lstEditStyles.SelectedIndex = 0;

            // Custom Owner-Draw for Icons (¶, a, ¶a) and Selection Bar
            _lstEditStyles.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= _lstEditStyles.Items.Count) return;
                StyleEntry item = _lstEditStyles.Items[e.Index] as StyleEntry;
                if (item == null) return;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Graphics g = e.Graphics;

                Color bgCol = isSelected ? Color.FromArgb(0, 120, 215) : Color.White;
                Color textCol = isSelected ? Color.White : Color.FromArgb(20, 20, 20);
                Color iconCol = isSelected ? Color.White : Color.FromArgb(0, 102, 204);

                using (SolidBrush bgB = new SolidBrush(bgCol)) g.FillRectangle(bgB, e.Bounds);

                // Draw Icon: 0=¶, 1=a, 2=¶a
                string iconStr = (item.Type == 0) ? "¶" : ((item.Type == 1) ? "a" : "¶a");
                using (Font iconFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (SolidBrush iconBrush = new SolidBrush(iconCol))
                {
                    g.DrawString(iconStr, iconFont, iconBrush, e.Bounds.X + 4, e.Bounds.Y + 2);
                }

                // Draw Name + Suffix
                string textStr = item.GetDisplayName(false);
                using (SolidBrush tb = new SolidBrush(textCol))
                {
                    g.DrawString(textStr, _lstEditStyles.Font, tb, e.Bounds.X + 26, e.Bounds.Y + 2);
                }
            };

            // Preview Group Box
            Label lblPreviewHeader = new Label { Text = "Preview of Normal:", Location = new Point(14, 208), AutoSize = true };

            Panel previewBox = new Panel
            {
                Location = new Point(14, 228),
                Size = new Size(240, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            _lblEditPreviewText = new Label
            {
                Text = "+Body",
                Location = new Point(6, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.Black
            };
            previewBox.Controls.Add(_lblEditPreviewText);

            _lblEditPreviewSize = new Label
            {
                Text = "10.5 pt",
                Location = new Point(185, 7),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };
            previewBox.Controls.Add(_lblEditPreviewSize);

            _btnEditModify = new Button
            {
                Text = "Modify...",
                Location = new Point(265, 228),
                Size = new Size(80, 28),
                UseVisualStyleBackColor = true
            };

            _btnEditDelete = new Button
            {
                Text = "Delete",
                Location = new Point(355, 228),
                Size = new Size(80, 28),
                UseVisualStyleBackColor = true,
                Enabled = false
            };

            // Description Box
            Panel descPanel = new Panel
            {
                Location = new Point(14, 266),
                Size = new Size(464, 66),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            _lblEditDescription = new Label
            {
                Location = new Point(6, 4),
                Size = new Size(450, 56),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(30, 30, 30),
                Text = "Font: (Default) +Body (Calibri), Left\nLine spacing: Multiple 1.15 li, Space\nAfter: 8 pt, Widow/Orphan control, Style: Show in the Styles gallery"
            };
            descPanel.Controls.Add(_lblEditDescription);

            _btnEditNewStyle = new Button
            {
                Text = "New Style...",
                Location = new Point(14, 340),
                Size = new Size(95, 26),
                UseVisualStyleBackColor = true
            };

            _lstEditStyles.SelectedIndexChanged += (s, e) =>
            {
                if (_lstEditStyles.SelectedItem != null)
                {
                    StyleEntry item = _lstEditStyles.SelectedItem as StyleEntry;
                    if (item != null)
                    {
                        lblPreviewHeader.Text = "Preview of " + item.Name + ":";
                        _lblEditPreviewText.Text = item.FontName ?? "+Body";
                        _lblEditPreviewText.Font = new Font(item.FontName ?? "Segoe UI", 9.5f, (item.IsBold ? FontStyle.Bold : FontStyle.Regular) | (item.IsItalic ? FontStyle.Italic : FontStyle.Regular));
                        _lblEditPreviewText.ForeColor = item.TextColor;
                        _lblEditPreviewSize.Text = item.FontSize.ToString("0.#") + " pt";
                        _lblEditDescription.Text = item.Description;
                        _btnEditDelete.Enabled = !item.IsBuiltIn;
                    }
                }
            };

            _btnEditModify.Click += (s, e) =>
            {
                if (_lstEditStyles.SelectedItem != null)
                {
                    StyleEntry item = _lstEditStyles.SelectedItem as StyleEntry;
                    _selectedTabIndex = 3; // Switch to Set Defaults
                    _lstFont.SelectedIndex = Math.Max(0, _lstFont.FindStringExact(item.FontName));
                    _cboAlignment.SelectedIndex = 0;
                    SwitchTabs();
                }
            };

            _btnEditNewStyle.Click += (s, e) =>
            {
                using (Form prompt = new Form())
                {
                    prompt.Text = "Create New Style";
                    prompt.Size = new Size(320, 140);
                    prompt.StartPosition = FormStartPosition.CenterParent;
                    prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                    prompt.MaximizeBox = false;
                    prompt.MinimizeBox = false;

                    Label lbl = new Label { Text = "Name:", Location = new Point(14, 14), AutoSize = true };
                    TextBox txt = new TextBox { Text = "Style 1", Location = new Point(14, 34), Size = new Size(276, 23) };
                    Button ok = new Button { Text = "OK", Location = new Point(134, 68), Size = new Size(75, 25), DialogResult = DialogResult.OK };
                    Button can = new Button { Text = "Cancel", Location = new Point(215, 68), Size = new Size(75, 25), DialogResult = DialogResult.Cancel };

                    prompt.Controls.Add(lbl);
                    prompt.Controls.Add(txt);
                    prompt.Controls.Add(ok);
                    prompt.Controls.Add(can);
                    prompt.AcceptButton = ok;
                    prompt.CancelButton = can;

                    if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        var newSt = new StyleEntry(txt.Text.Trim(), 0, 100, false, false, "Calibri", 11.0f, false, false, Color.Black, "Custom user style: " + txt.Text.Trim());
                        _stylesList.Add(newSt);
                        _lstEditStyles.Items.Add(newSt);
                        _lstEditStyles.SelectedItem = newSt;
                    }
                }
            };

            pnl.Controls.Add(lblSort);
            pnl.Controls.Add(_cboEditSort);
            pnl.Controls.Add(_chkEditShowRecOnly);
            pnl.Controls.Add(lblSelect);
            pnl.Controls.Add(_lstEditStyles);
            pnl.Controls.Add(lblPreviewHeader);
            pnl.Controls.Add(previewBox);
            pnl.Controls.Add(_btnEditModify);
            pnl.Controls.Add(_btnEditDelete);
            pnl.Controls.Add(descPanel);
            pnl.Controls.Add(_btnEditNewStyle);

            return pnl;
        }

        // ==========================================
        // TAB 1: RECOMMEND TAB (Screenshot 2)
        // ==========================================
        private Panel BuildRecommendTabPanel()
        {
            Panel pnl = new Panel { BackColor = Color.White };

            Label lblSort = new Label { Text = "Sort order:", Location = new Point(14, 12), AutoSize = true };
            _cboRecommendSort = new ComboBox
            {
                Location = new Point(80, 10),
                Size = new Size(160, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboRecommendSort.Items.AddRange(new object[] { "As Recommended", "Alphabetical", "By Type", "Based On" });
            _cboRecommendSort.SelectedIndex = 0;

            _chkRecommendShowRecOnly = new CheckBox
            {
                Text = "Show recommended styles only",
                Location = new Point(255, 12),
                AutoSize = true
            };

            Label lblSelect = new Label
            {
                Text = "Select one or more styles to set whether the style appears by default in the\nrecommended list and the order in which the styles appear",
                Location = new Point(14, 38),
                Size = new Size(464, 32),
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            _lstRecommendStyles = new ListBox
            {
                Location = new Point(14, 74),
                Size = new Size(464, 120),
                SelectionMode = SelectionMode.MultiExtended,
                IntegralHeight = false,
                Font = new Font("Consolas", 9.0f)
            };

            Action populateRecList = () =>
            {
                _lstRecommendStyles.Items.Clear();
                foreach (var st in _stylesList)
                {
                    _lstRecommendStyles.Items.Add(st.GetDisplayName(true));
                }
                if (_lstRecommendStyles.Items.Count > 0) _lstRecommendStyles.SelectedIndex = 0;
            };
            populateRecList();

            Button btnSelectAll = new Button { Text = "Select All", Location = new Point(14, 202), Size = new Size(85, 24), UseVisualStyleBackColor = true };
            btnSelectAll.Click += (s, e) =>
            {
                for (int i = 0; i < _lstRecommendStyles.Items.Count; i++) _lstRecommendStyles.SetSelected(i, true);
            };

            Button btnSelectBuiltin = new Button { Text = "Select Built-in", Location = new Point(106, 202), Size = new Size(95, 24), UseVisualStyleBackColor = true };
            btnSelectBuiltin.Click += (s, e) =>
            {
                for (int i = 0; i < _stylesList.Count; i++)
                {
                    _lstRecommendStyles.SetSelected(i, _stylesList[i].IsBuiltIn);
                }
            };

            // Section: Priority Sorting
            Label lblPriority = new Label { Text = "Set priority to use when sorting in recommended order", Location = new Point(14, 235), AutoSize = true };
            Button btnMoveUp = new Button { Text = "Move Up", Location = new Point(34, 255), Size = new Size(95, 24), UseVisualStyleBackColor = true };
            Button btnMoveDown = new Button { Text = "Move Down", Location = new Point(144, 255), Size = new Size(95, 24), UseVisualStyleBackColor = true };
            Button btnMakeLast = new Button { Text = "Make Last", Location = new Point(34, 285), Size = new Size(95, 24), UseVisualStyleBackColor = true };
            Button btnAssignVal = new Button { Text = "Assign Value...", Location = new Point(144, 285), Size = new Size(95, 24), UseVisualStyleBackColor = true };

            btnMoveUp.Click += (s, e) =>
            {
                foreach (int idx in _lstRecommendStyles.SelectedIndices)
                {
                    _stylesList[idx].Priority = Math.Max(1, _stylesList[idx].Priority - 1);
                }
                populateRecList();
            };
            btnMoveDown.Click += (s, e) =>
            {
                foreach (int idx in _lstRecommendStyles.SelectedIndices)
                {
                    _stylesList[idx].Priority = Math.Min(99, _stylesList[idx].Priority + 1);
                }
                populateRecList();
            };
            btnMakeLast.Click += (s, e) =>
            {
                foreach (int idx in _lstRecommendStyles.SelectedIndices)
                {
                    _stylesList[idx].Priority = 99;
                }
                populateRecList();
            };

            // Section: Visibility in Recommended
            Label lblVisibility = new Label { Text = "Set whether style shows when viewing recommended styles", Location = new Point(14, 320), AutoSize = true };
            Button btnShow = new Button { Text = "Show", Location = new Point(34, 340), Size = new Size(80, 24), UseVisualStyleBackColor = true };
            Button btnHideUntilUsed = new Button { Text = "Hide until used", Location = new Point(122, 340), Size = new Size(100, 24), UseVisualStyleBackColor = true };
            Button btnHide = new Button { Text = "Hide", Location = new Point(230, 340), Size = new Size(80, 24), UseVisualStyleBackColor = true };

            btnShow.Click += (s, e) =>
            {
                foreach (int idx in _lstRecommendStyles.SelectedIndices)
                {
                    _stylesList[idx].IsHideUntilUsed = false;
                    _stylesList[idx].IsHidden = false;
                }
                populateRecList();
            };
            btnHideUntilUsed.Click += (s, e) =>
            {
                foreach (int idx in _lstRecommendStyles.SelectedIndices)
                {
                    _stylesList[idx].IsHideUntilUsed = true;
                    _stylesList[idx].IsHidden = false;
                }
                populateRecList();
            };
            btnHide.Click += (s, e) =>
            {
                foreach (int idx in _lstRecommendStyles.SelectedIndices)
                {
                    _stylesList[idx].IsHideUntilUsed = false;
                    _stylesList[idx].IsHidden = true;
                }
                populateRecList();
            };

            pnl.Controls.Add(lblSort);
            pnl.Controls.Add(_cboRecommendSort);
            pnl.Controls.Add(_chkRecommendShowRecOnly);
            pnl.Controls.Add(lblSelect);
            pnl.Controls.Add(_lstRecommendStyles);
            pnl.Controls.Add(btnSelectAll);
            pnl.Controls.Add(btnSelectBuiltin);
            pnl.Controls.Add(lblPriority);
            pnl.Controls.Add(btnMoveUp);
            pnl.Controls.Add(btnMoveDown);
            pnl.Controls.Add(btnMakeLast);
            pnl.Controls.Add(btnAssignVal);
            pnl.Controls.Add(lblVisibility);
            pnl.Controls.Add(btnShow);
            pnl.Controls.Add(btnHideUntilUsed);
            pnl.Controls.Add(btnHide);

            return pnl;
        }

        // ==========================================
        // TAB 2: RESTRICT TAB (Screenshot 3)
        // ==========================================
        private Panel BuildRestrictTabPanel()
        {
            Panel pnl = new Panel { BackColor = Color.White };

            Label lblSort = new Label { Text = "Sort order:", Location = new Point(14, 12), AutoSize = true };
            _cboRestrictSort = new ComboBox
            {
                Location = new Point(80, 10),
                Size = new Size(160, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboRestrictSort.Items.AddRange(new object[] { "As Recommended", "Alphabetical", "By Type", "Based On" });
            _cboRestrictSort.SelectedIndex = 0;

            _chkRestrictShowRecOnly = new CheckBox
            {
                Text = "Show recommended styles only",
                Location = new Point(255, 12),
                AutoSize = true
            };

            Label lblSelect = new Label
            {
                Text = "Select one or more styles to set their availability when a document is protected\nfor formatting changes",
                Location = new Point(14, 38),
                Size = new Size(464, 32),
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            _lstRestrictStyles = new ListBox
            {
                Location = new Point(14, 74),
                Size = new Size(464, 120),
                SelectionMode = SelectionMode.MultiExtended,
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };

            Action populateRestrictList = () =>
            {
                _lstRestrictStyles.Items.Clear();
                foreach (var st in _stylesList)
                {
                    string resMark = st.IsPermitted ? "" : " [Restricted]";
                    _lstRestrictStyles.Items.Add(st.Name + resMark);
                }
                if (_lstRestrictStyles.Items.Count > 0) _lstRestrictStyles.SelectedIndex = 0;
            };
            populateRestrictList();

            Button btnSelectVis = new Button { Text = "Select Visible", Location = new Point(14, 202), Size = new Size(95, 24), UseVisualStyleBackColor = true };
            Button btnSelectBuiltin = new Button { Text = "Select Built-in", Location = new Point(116, 202), Size = new Size(95, 24), UseVisualStyleBackColor = true };
            Button btnSelectAll = new Button { Text = "Select All", Location = new Point(218, 202), Size = new Size(85, 24), UseVisualStyleBackColor = true };

            btnSelectVis.Click += (s, e) =>
            {
                for (int i = 0; i < _stylesList.Count; i++) _lstRestrictStyles.SetSelected(i, !_stylesList[i].IsHidden);
            };
            btnSelectBuiltin.Click += (s, e) =>
            {
                for (int i = 0; i < _stylesList.Count; i++) _lstRestrictStyles.SetSelected(i, _stylesList[i].IsBuiltIn);
            };
            btnSelectAll.Click += (s, e) =>
            {
                for (int i = 0; i < _lstRestrictStyles.Items.Count; i++) _lstRestrictStyles.SetSelected(i, true);
            };

            // Section: Availability
            Label lblAvail = new Label { Text = "Set availability of selected styles", Location = new Point(14, 235), AutoSize = true };
            Button btnPermit = new Button { Text = "Permit", Location = new Point(34, 255), Size = new Size(80, 24), UseVisualStyleBackColor = true };
            Button btnRestrict = new Button { Text = "Restrict", Location = new Point(122, 255), Size = new Size(80, 24), UseVisualStyleBackColor = true };

            btnPermit.Click += (s, e) =>
            {
                foreach (int idx in _lstRestrictStyles.SelectedIndices)
                {
                    _stylesList[idx].IsPermitted = true;
                }
                populateRestrictList();
            };
            btnRestrict.Click += (s, e) =>
            {
                foreach (int idx in _lstRestrictStyles.SelectedIndices)
                {
                    _stylesList[idx].IsPermitted = false;
                }
                populateRestrictList();
            };

            // Protection Checkboxes
            CheckBox chkLimit = new CheckBox { Text = "Limit formatting to permitted styles", Location = new Point(14, 290), AutoSize = true };
            CheckBox chkAutoFormat = new CheckBox { Text = "Allow AutoFormat to override formatting", Location = new Point(14, 314), AutoSize = true };
            CheckBox chkBlockTheme = new CheckBox { Text = "Block Theme or Scheme switching", Location = new Point(14, 338), AutoSize = true };
            CheckBox chkBlockStyleSet = new CheckBox { Text = "Block switching to a different style set", Location = new Point(14, 362), AutoSize = true };

            pnl.Controls.Add(lblSort);
            pnl.Controls.Add(_cboRestrictSort);
            pnl.Controls.Add(_chkRestrictShowRecOnly);
            pnl.Controls.Add(lblSelect);
            pnl.Controls.Add(_lstRestrictStyles);
            pnl.Controls.Add(btnSelectVis);
            pnl.Controls.Add(btnSelectBuiltin);
            pnl.Controls.Add(btnSelectAll);
            pnl.Controls.Add(lblAvail);
            pnl.Controls.Add(btnPermit);
            pnl.Controls.Add(btnRestrict);
            pnl.Controls.Add(chkLimit);
            pnl.Controls.Add(chkAutoFormat);
            pnl.Controls.Add(chkBlockTheme);
            pnl.Controls.Add(chkBlockStyleSet);

            return pnl;
        }

        // ==========================================
        // TAB 3: SET DEFAULTS TAB
        // ==========================================
        private Panel BuildSetDefaultsTabPanel()
        {
            Panel pnl = new Panel { BackColor = Color.White };

            // Section 1: Font
            Label lblFont = new Label { Text = "Font:", Location = new Point(24, 14), AutoSize = true, ForeColor = Color.FromArgb(31, 41, 55) };
            _lstFont = new ListBox
            {
                Location = new Point(24, 34),
                Size = new Size(240, 84),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };
            _lstFont.Items.Add("+Body");
            _lstFont.Items.Add("+Headings");
            _lstFont.Items.Add("Arial");
            _lstFont.Items.Add("Arial Black");
            _lstFont.Items.Add("Arial Narrow");
            _lstFont.Items.Add("Calibri");
            _lstFont.Items.Add("Cambria");
            _lstFont.Items.Add("Georgia");
            _lstFont.Items.Add("Segoe UI");
            _lstFont.Items.Add("Times New Roman");

            if (_systemFonts != null)
            {
                foreach (var f in _systemFonts)
                {
                    if (!_lstFont.Items.Contains(f)) _lstFont.Items.Add(f);
                }
            }

            int fIdx = _lstFont.FindStringExact(_curFont);
            if (fIdx >= 0) _lstFont.SelectedIndex = fIdx;
            else _lstFont.SelectedIndex = 0; // +Body

            Label lblSize = new Label { Text = "Size:", Location = new Point(350, 14), AutoSize = true, ForeColor = Color.FromArgb(31, 41, 55) };
            _lstSize = new ListBox
            {
                Location = new Point(350, 34),
                Size = new Size(116, 84),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };
            int[] sizes = new int[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            int selSizeIdx = 3; // 11
            for (int i = 0; i < sizes.Length; i++)
            {
                _lstSize.Items.Add(sizes[i].ToString());
                if (Math.Abs(sizes[i] - _curSize) < 0.5f) selSizeIdx = i;
            }
            _lstSize.SelectedIndex = selSizeIdx;

            Label lblFontColor = new Label { Text = "Font color:", Location = new Point(24, 128), AutoSize = true, ForeColor = Color.FromArgb(31, 41, 55) };
            _cboFontColor = new ComboBox
            {
                Location = new Point(135, 125),
                Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboFontColor.Items.Add("Automatic");
            _cboFontColor.Items.Add("Black");
            _cboFontColor.Items.Add("Dark Blue");
            _cboFontColor.Items.Add("Navy");
            _cboFontColor.Items.Add("Dark Red");
            _cboFontColor.Items.Add("Dark Green");
            _cboFontColor.Items.Add("More Colors...");
            _cboFontColor.SelectedIndex = 0;

            _cboFontColor.SelectedIndexChanged += (s, e) =>
            {
                if (_cboFontColor.SelectedIndex == _cboFontColor.Items.Count - 1)
                {
                    using (ColorDialog cd = new ColorDialog())
                    {
                        cd.Color = _curColor;
                        if (cd.ShowDialog(this) == DialogResult.OK)
                        {
                            _curColor = cd.Color;
                        }
                    }
                }
            };

            pnl.Controls.Add(lblFont);
            pnl.Controls.Add(_lstFont);
            pnl.Controls.Add(lblSize);
            pnl.Controls.Add(_lstSize);
            pnl.Controls.Add(lblFontColor);
            pnl.Controls.Add(_cboFontColor);

            // Group 2: Paragraph Position
            Label lblParaPos = new Label
            {
                Text = "Paragraph Position",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(18, 160),
                AutoSize = true
            };
            pnl.Controls.Add(lblParaPos);

            Label lblLeft = new Label { Text = "Left:", Location = new Point(24, 186), AutoSize = true };
            _numLeft = new NumericUpDown
            {
                Location = new Point(68, 184),
                Size = new Size(80, 23),
                DecimalPlaces = 1,
                Increment = 0.1M,
                Minimum = 0,
                Maximum = 10,
                Value = 0.0M
            };

            Label lblRight = new Label { Text = "Right:", Location = new Point(24, 216), AutoSize = true };
            _numRight = new NumericUpDown
            {
                Location = new Point(68, 214),
                Size = new Size(80, 23),
                DecimalPlaces = 1,
                Increment = 0.1M,
                Minimum = 0,
                Maximum = 10,
                Value = 0.0M
            };

            Label lblSpecial = new Label { Text = "Special:", Location = new Point(190, 186), AutoSize = true };
            _cboSpecial = new ComboBox
            {
                Location = new Point(245, 184),
                Size = new Size(95, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboSpecial.Items.AddRange(new object[] { "(none)", "First line", "Hanging" });
            _cboSpecial.SelectedIndex = 0;

            Label lblBy = new Label { Text = "By:", Location = new Point(355, 186), AutoSize = true };
            _numSpecialBy = new NumericUpDown
            {
                Location = new Point(385, 184),
                Size = new Size(80, 23),
                DecimalPlaces = 1,
                Increment = 0.1M,
                Minimum = 0,
                Maximum = 5,
                Value = 0.5M
            };

            Label lblAlign = new Label { Text = "Alignment:", Location = new Point(24, 246), AutoSize = true };
            _cboAlignment = new ComboBox
            {
                Location = new Point(100, 244),
                Size = new Size(240, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboAlignment.Items.AddRange(new object[] { "Left", "Centered", "Right", "Justified" });
            _cboAlignment.SelectedIndex = Math.Max(0, Math.Min(3, _curAlign));

            pnl.Controls.Add(lblLeft);
            pnl.Controls.Add(_numLeft);
            pnl.Controls.Add(lblRight);
            pnl.Controls.Add(_numRight);
            pnl.Controls.Add(lblSpecial);
            pnl.Controls.Add(_cboSpecial);
            pnl.Controls.Add(lblBy);
            pnl.Controls.Add(_numSpecialBy);
            pnl.Controls.Add(lblAlign);
            pnl.Controls.Add(_cboAlignment);

            // Group 3: Paragraph Spacing
            Label lblParaSpacing = new Label
            {
                Text = "Paragraph Spacing",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(18, 278),
                AutoSize = true
            };
            pnl.Controls.Add(lblParaSpacing);

            Label lblBefore = new Label { Text = "Before:", Location = new Point(24, 304), AutoSize = true };
            _numBefore = new NumericUpDown
            {
                Location = new Point(78, 302),
                Size = new Size(74, 23),
                Minimum = 0,
                Maximum = 72,
                Increment = 6,
                Value = (decimal)_curBefore
            };

            Label lblAfter = new Label { Text = "After:", Location = new Point(24, 334), AutoSize = true };
            _numAfter = new NumericUpDown
            {
                Location = new Point(78, 332),
                Size = new Size(74, 23),
                Minimum = 0,
                Maximum = 72,
                Increment = 6,
                Value = (decimal)_curAfter
            };

            Label lblLineSpacing = new Label { Text = "Line spacing:", Location = new Point(190, 304), AutoSize = true };
            _cboLineSpacing = new ComboBox
            {
                Location = new Point(275, 302),
                Size = new Size(95, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboLineSpacing.Items.AddRange(new object[] { "Single", "1.5 lines", "Double", "At least", "Exactly", "Multiple" });

            if (Math.Abs(_curLineSpacing - 1.0f) < 0.05f) _cboLineSpacing.SelectedIndex = 0;
            else if (Math.Abs(_curLineSpacing - 1.5f) < 0.05f) _cboLineSpacing.SelectedIndex = 1;
            else if (Math.Abs(_curLineSpacing - 2.0f) < 0.05f) _cboLineSpacing.SelectedIndex = 2;
            else _cboLineSpacing.SelectedIndex = 5; // Multiple

            Label lblAt = new Label { Text = "At:", Location = new Point(380, 304), AutoSize = true };
            _numLineSpacingAt = new NumericUpDown
            {
                Location = new Point(408, 302),
                Size = new Size(58, 23),
                DecimalPlaces = 2,
                Increment = 0.05M,
                Minimum = 0.5M,
                Maximum = 5.0M,
                Value = (decimal)_curLineSpacing
            };

            _cboLineSpacing.SelectedIndexChanged += (s, e) =>
            {
                if (_cboLineSpacing.SelectedIndex == 0) _numLineSpacingAt.Value = 1.00M;
                else if (_cboLineSpacing.SelectedIndex == 1) _numLineSpacingAt.Value = 1.50M;
                else if (_cboLineSpacing.SelectedIndex == 2) _numLineSpacingAt.Value = 2.00M;
                else if (_cboLineSpacing.SelectedIndex == 5) _numLineSpacingAt.Value = 1.15M;
            };

            pnl.Controls.Add(lblBefore);
            pnl.Controls.Add(_numBefore);
            pnl.Controls.Add(lblAfter);
            pnl.Controls.Add(_numAfter);
            pnl.Controls.Add(lblLineSpacing);
            pnl.Controls.Add(_cboLineSpacing);
            pnl.Controls.Add(lblAt);
            pnl.Controls.Add(_numLineSpacingAt);

            pnl.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                {
                    g.DrawLine(p, 18, 168, pnl.Width - 18, 168);
                    g.DrawLine(p, 18, 286, pnl.Width - 18, 286);
                }
            };

            return pnl;
        }
    }
}
