using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class OrganizerForm : Form
    {
        private int _activeTab = 0; // 0 = Styles, 1 = Macro Project Items
        private string[] _tabs = new string[] { "Styles", "Macro Project Items" };

        // Styles Tab Lists
        private ListBox _lstDocStyles;
        private ListBox _lstNormalStyles;
        private ComboBox _cboDocStylesScope;
        private ComboBox _cboNormalStylesScope;
        private Button _btnCopy;
        private Button _btnDelete;
        private Button _btnRename;
        private Label _lblDescriptionText;

        // Macro Tab Lists
        private ListBox _lstDocMacros;
        private ListBox _lstNormalMacros;
        private ComboBox _cboDocMacrosScope;
        private ComboBox _cboNormalMacrosScope;

        private Panel _pnlStyles;
        private Panel _pnlMacros;

        private Dictionary<string, string> _styleDescriptions = new Dictionary<string, string>
        {
            { "Normal", "Font: (Default) +Body (Calibri), 11 pt, Left, Line spacing: Multiple 1.15 li, Space After: 8 pt, Widow/Orphan control, Style: Show in the Styles gallery" },
            { "Default Paragraph Font", "The font of the Normal style with no additional character formatting applied." },
            { "No Spacing", "Font: (Default) +Body (Calibri), 11 pt, Left, Line spacing: Single, Space After: 0 pt, Style: Show in the Styles gallery" },
            { "Heading 1", "Font: (Default) +Headings (Calibri Light), 16 pt, Bold, Font color: Accent 1, Left, Space Before: 12 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery" },
            { "Heading 2", "Font: (Default) +Headings (Calibri Light), 13 pt, Bold, Font color: Accent 1, Left, Space Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery" },
            { "Heading 3", "Font: (Default) +Headings (Calibri Light), 12 pt, Bold, Font color: Accent 1, Left, Space Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery" },
            { "Heading 4", "Font: (Default) +Headings (Calibri Light), 11 pt, Bold, Font color: Accent 1, Left, Space Before: 2 pt, After: 0 pt, Keep with next, Style: Show in the Styles gallery" },
            { "Book Title", "Font: (Default) +Headings (Calibri Light), 18 pt, Bold, Font color: Accent 1, Left, Space Before: 18 pt, After: 6 pt" },
            { "Caption", "Font: (Default) +Body (Calibri), 9 pt, Italic, Font color: Accent 1, Left, Space After: 4 pt" },
            { "Emphasis", "Font: (Default) +Body (Calibri), 11 pt, Italic, Character style" },
            { "Table Normal", "Table style: Default grid formatting, Top/Bottom padding 0.05\", Left/Right padding 0.15\"" },
            { "No List", "List formatting: None" }
        };

        public OrganizerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Organizer";
            Size = new Size(620, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(240, 240, 240);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // Tab Header Panel
            Panel tabHeader = new Panel
            {
                Location = new Point(14, 10),
                Size = new Size(576, 32),
                BackColor = Color.Transparent
            };
            tabHeader.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int tabX = 0;
                for (int i = 0; i < _tabs.Length; i++)
                {
                    string tName = _tabs[i];
                    int tabW = (int)g.MeasureString(tName, Font).Width + 24;
                    Rectangle tabRect = new Rectangle(tabX, 2, tabW, 28);
                    bool isSel = (i == _activeTab);

                    if (isSel)
                    {
                        using (SolidBrush sb = new SolidBrush(Color.White)) g.FillRectangle(sb, tabRect);
                        using (Pen p = new Pen(Color.FromArgb(200, 200, 200)))
                        {
                            g.DrawLine(p, tabRect.Left, tabRect.Bottom, tabRect.Left, tabRect.Top);
                            g.DrawLine(p, tabRect.Left, tabRect.Top, tabRect.Right, tabRect.Top);
                            g.DrawLine(p, tabRect.Right, tabRect.Top, tabRect.Right, tabRect.Bottom);
                        }
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(20, 20, 20)))
                        {
                            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(tName, Font, tb, tabRect, sf);
                        }
                    }
                    else
                    {
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(80, 80, 80)))
                        {
                            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(tName, Font, tb, tabRect, sf);
                        }
                    }
                    tabX += tabW + 2;
                }
                using (Pen p = new Pen(Color.FromArgb(200, 200, 200)))
                {
                    g.DrawLine(p, 0, 30, tabHeader.Width, 30);
                }
            };

            tabHeader.MouseClick += (s, e) =>
            {
                Graphics g = tabHeader.CreateGraphics();
                int tabX = 0;
                for (int i = 0; i < _tabs.Length; i++)
                {
                    string tName = _tabs[i];
                    int tabW = (int)g.MeasureString(tName, Font).Width + 24;
                    Rectangle tabRect = new Rectangle(tabX, 2, tabW, 28);
                    if (tabRect.Contains(e.Location))
                    {
                        _activeTab = i;
                        tabHeader.Invalidate();
                        SwitchTabs();
                        break;
                    }
                    tabX += tabW + 2;
                }
            };
            Controls.Add(tabHeader);

            // Container Panel
            Panel mainContainer = new Panel
            {
                Location = new Point(14, 41),
                Size = new Size(576, 385),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(mainContainer);

            // 1. Build Styles Panel
            _pnlStyles = BuildStylesPanel();
            _pnlStyles.Dock = DockStyle.Fill;
            mainContainer.Controls.Add(_pnlStyles);

            // 2. Build Macro Panel
            _pnlMacros = BuildMacrosPanel();
            _pnlMacros.Dock = DockStyle.Fill;
            _pnlMacros.Visible = false;
            mainContainer.Controls.Add(_pnlMacros);

            // Bottom Close Button
            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(510, 434),
                Size = new Size(80, 26),
                UseVisualStyleBackColor = true,
                DialogResult = DialogResult.OK
            };
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private void SwitchTabs()
        {
            _pnlStyles.Visible = (_activeTab == 0);
            _pnlMacros.Visible = (_activeTab == 1);
        }

        private Panel BuildStylesPanel()
        {
            Panel pnl = new Panel { BackColor = Color.White };

            // Left Side: In Document1:
            Label lblDoc = new Label { Text = "In Document1:", Location = new Point(12, 10), AutoSize = true };
            pnl.Controls.Add(lblDoc);

            _lstDocStyles = new ListBox
            {
                Location = new Point(12, 30),
                Size = new Size(224, 160),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };
            string[] docStyles = new string[] {
                "Book Title", "Caption", "Default Paragraph Font", "Emphasis",
                "Heading 1", "Heading 2", "Heading 3", "Heading 4",
                "Heading 5", "Heading 6", "Heading 7", "Normal", "No Spacing"
            };
            foreach (var st in docStyles) _lstDocStyles.Items.Add(st);
            _lstDocStyles.SelectedIndex = 0;
            pnl.Controls.Add(_lstDocStyles);

            Label lblDocScope = new Label { Text = "Styles available in:", Location = new Point(12, 198), AutoSize = true };
            pnl.Controls.Add(lblDocScope);

            _cboDocStylesScope = new ComboBox
            {
                Location = new Point(12, 218),
                Size = new Size(224, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboDocStylesScope.Items.Add("Document1 (Document)");
            _cboDocStylesScope.SelectedIndex = 0;
            pnl.Controls.Add(_cboDocStylesScope);

            Button btnCloseDocFile = new Button
            {
                Text = "Close File",
                Location = new Point(161, 247),
                Size = new Size(75, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnCloseDocFile);

            // Middle Action Buttons
            _btnCopy = new Button
            {
                Text = "Copy",
                Location = new Point(246, 30),
                Size = new Size(80, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(_btnCopy);

            _btnDelete = new Button
            {
                Text = "Delete",
                Location = new Point(246, 60),
                Size = new Size(80, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(_btnDelete);

            _btnRename = new Button
            {
                Text = "Rename...",
                Location = new Point(246, 90),
                Size = new Size(80, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(_btnRename);

            // Right Side: In Normal:
            Label lblNormal = new Label { Text = "In Normal:", Location = new Point(336, 10), AutoSize = true };
            pnl.Controls.Add(lblNormal);

            _lstNormalStyles = new ListBox
            {
                Location = new Point(336, 30),
                Size = new Size(224, 160),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };
            string[] normStyles = new string[] {
                "Default Paragraph Font", "No List", "Normal", "Table Normal"
            };
            foreach (var st in normStyles) _lstNormalStyles.Items.Add(st);
            _lstNormalStyles.SelectedIndex = 2; // Normal
            pnl.Controls.Add(_lstNormalStyles);

            Label lblNormalScope = new Label { Text = "Styles available in:", Location = new Point(336, 198), AutoSize = true };
            pnl.Controls.Add(lblNormalScope);

            _cboNormalStylesScope = new ComboBox
            {
                Location = new Point(336, 218),
                Size = new Size(224, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboNormalStylesScope.Items.Add("Normal.dotm (global template)");
            _cboNormalStylesScope.SelectedIndex = 0;
            pnl.Controls.Add(_cboNormalStylesScope);

            Button btnCloseNormalFile = new Button
            {
                Text = "Close File",
                Location = new Point(485, 247),
                Size = new Size(75, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnCloseNormalFile);

            // Bottom Description Area
            Label lblDescHeader = new Label { Text = "Description", Location = new Point(12, 280), AutoSize = true, ForeColor = Color.FromArgb(70, 70, 70) };
            pnl.Controls.Add(lblDescHeader);

            _lblDescriptionText = new Label
            {
                Location = new Point(12, 302),
                Size = new Size(548, 70),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(40, 40, 40),
                Text = "Font: (Default) +Headings (Calibri Light), 18 pt, Bold, Font color: Accent 1, Left, Space Before: 18 pt, After: 6 pt"
            };
            pnl.Controls.Add(_lblDescriptionText);

            // Wire selection events to update description & copy direction
            _lstDocStyles.SelectedIndexChanged += (s, e) =>
            {
                if (_lstDocStyles.SelectedItem != null)
                {
                    string sName = _lstDocStyles.SelectedItem.ToString();
                    _btnCopy.Text = "Copy ->";
                    if (_styleDescriptions.ContainsKey(sName)) _lblDescriptionText.Text = _styleDescriptions[sName];
                    else _lblDescriptionText.Text = "Custom style: " + sName;
                }
            };

            _lstNormalStyles.SelectedIndexChanged += (s, e) =>
            {
                if (_lstNormalStyles.SelectedItem != null)
                {
                    string sName = _lstNormalStyles.SelectedItem.ToString();
                    _btnCopy.Text = "<- Copy";
                    if (_styleDescriptions.ContainsKey(sName)) _lblDescriptionText.Text = _styleDescriptions[sName];
                    else _lblDescriptionText.Text = "Template style: " + sName;
                }
            };

            _btnCopy.Click += (s, e) =>
            {
                if (_btnCopy.Text.Contains("->") && _lstDocStyles.SelectedItem != null)
                {
                    string item = _lstDocStyles.SelectedItem.ToString();
                    if (!_lstNormalStyles.Items.Contains(item)) _lstNormalStyles.Items.Add(item);
                }
                else if (_lstNormalStyles.SelectedItem != null)
                {
                    string item = _lstNormalStyles.SelectedItem.ToString();
                    if (!_lstDocStyles.Items.Contains(item)) _lstDocStyles.Items.Add(item);
                }
            };

            _btnDelete.Click += (s, e) =>
            {
                if (_lstDocStyles.Focused && _lstDocStyles.SelectedIndex >= 0)
                {
                    _lstDocStyles.Items.RemoveAt(_lstDocStyles.SelectedIndex);
                }
                else if (_lstNormalStyles.Focused && _lstNormalStyles.SelectedIndex >= 0)
                {
                    _lstNormalStyles.Items.RemoveAt(_lstNormalStyles.SelectedIndex);
                }
            };

            _btnRename.Click += (s, e) =>
            {
                string curName = _lstDocStyles.SelectedItem != null ? _lstDocStyles.SelectedItem.ToString() : "";
                if (!string.IsNullOrEmpty(curName))
                {
                    using (Form inputDlg = new Form())
                    {
                        inputDlg.Text = "Rename Style";
                        inputDlg.Size = new Size(320, 150);
                        inputDlg.StartPosition = FormStartPosition.CenterParent;
                        inputDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                        inputDlg.MaximizeBox = false;
                        inputDlg.MinimizeBox = false;

                        Label lbl = new Label { Text = "New name for style '" + curName + "':", Location = new Point(14, 14), AutoSize = true };
                        TextBox txt = new TextBox { Text = curName, Location = new Point(14, 38), Size = new Size(276, 23) };
                        Button okBtn = new Button { Text = "OK", Location = new Point(134, 75), Size = new Size(75, 25), DialogResult = DialogResult.OK };
                        Button cancelBtn = new Button { Text = "Cancel", Location = new Point(215, 75), Size = new Size(75, 25), DialogResult = DialogResult.Cancel };

                        inputDlg.Controls.Add(lbl);
                        inputDlg.Controls.Add(txt);
                        inputDlg.Controls.Add(okBtn);
                        inputDlg.Controls.Add(cancelBtn);
                        inputDlg.AcceptButton = okBtn;
                        inputDlg.CancelButton = cancelBtn;

                        if (inputDlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                        {
                            int idx = _lstDocStyles.SelectedIndex;
                            _lstDocStyles.Items[idx] = txt.Text.Trim();
                        }
                    }
                }
            };

            pnl.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(Color.FromArgb(220, 220, 220)))
                {
                    g.DrawLine(p, 80, 288, pnl.Width - 12, 288);
                }
            };

            return pnl;
        }

        private Panel BuildMacrosPanel()
        {
            Panel pnl = new Panel { BackColor = Color.White };

            // Left Side: In Document1:
            Label lblDoc = new Label { Text = "In Document1:", Location = new Point(12, 10), AutoSize = true };
            pnl.Controls.Add(lblDoc);

            _lstDocMacros = new ListBox
            {
                Location = new Point(12, 30),
                Size = new Size(224, 160),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };
            pnl.Controls.Add(_lstDocMacros);

            Label lblDocScope = new Label { Text = "Macro Project Items available in:", Location = new Point(12, 198), AutoSize = true };
            pnl.Controls.Add(lblDocScope);

            _cboDocMacrosScope = new ComboBox
            {
                Location = new Point(12, 218),
                Size = new Size(224, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboDocMacrosScope.Items.Add("Document1 (Document)");
            _cboDocMacrosScope.SelectedIndex = 0;
            pnl.Controls.Add(_cboDocMacrosScope);

            Button btnCloseDocFile = new Button
            {
                Text = "Close File",
                Location = new Point(161, 247),
                Size = new Size(75, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnCloseDocFile);

            // Middle Action Buttons
            Button btnCopy = new Button
            {
                Text = "Copy",
                Location = new Point(246, 30),
                Size = new Size(80, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnCopy);

            Button btnDelete = new Button
            {
                Text = "Delete",
                Location = new Point(246, 60),
                Size = new Size(80, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnDelete);

            Button btnRename = new Button
            {
                Text = "Rename...",
                Location = new Point(246, 90),
                Size = new Size(80, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnRename);

            // Right Side: In Normal:
            Label lblNormal = new Label { Text = "In Normal:", Location = new Point(336, 10), AutoSize = true };
            pnl.Controls.Add(lblNormal);

            _lstNormalMacros = new ListBox
            {
                Location = new Point(336, 30),
                Size = new Size(224, 160),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.0f)
            };
            _lstNormalMacros.Items.Add("NewMacros");
            _lstNormalMacros.SelectedIndex = 0;
            pnl.Controls.Add(_lstNormalMacros);

            Label lblNormalScope = new Label { Text = "Macro Project Items available in:", Location = new Point(336, 198), AutoSize = true };
            pnl.Controls.Add(lblNormalScope);

            _cboNormalMacrosScope = new ComboBox
            {
                Location = new Point(336, 218),
                Size = new Size(224, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboNormalMacrosScope.Items.Add("Normal.dotm (global template)");
            _cboNormalMacrosScope.SelectedIndex = 0;
            pnl.Controls.Add(_cboNormalMacrosScope);

            Button btnCloseNormalFile = new Button
            {
                Text = "Close File",
                Location = new Point(485, 247),
                Size = new Size(75, 24),
                UseVisualStyleBackColor = true
            };
            pnl.Controls.Add(btnCloseNormalFile);

            // Bottom Description Area
            Label lblDescHeader = new Label { Text = "Description", Location = new Point(12, 280), AutoSize = true, ForeColor = Color.FromArgb(70, 70, 70) };
            pnl.Controls.Add(lblDescHeader);

            Label lblDesc = new Label
            {
                Location = new Point(12, 302),
                Size = new Size(548, 70),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(40, 40, 40),
                Text = "Standard VBA Macro Module container."
            };
            pnl.Controls.Add(lblDesc);

            pnl.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(Color.FromArgb(220, 220, 220)))
                {
                    g.DrawLine(p, 80, 288, pnl.Width - 12, 288);
                }
            };

            return pnl;
        }
    }
}
