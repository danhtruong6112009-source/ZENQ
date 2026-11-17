using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Shared helper to launch the Word 365 Paragraph dialog from both the Ribbon launcher and Context Menu.
    /// </summary>
    public static class WriterParagraphDialogHelper
    {
        public static bool Show(WriterView writerView, RibbonBar ribbon, Form parentForm)
        {
            if (writerView == null) return false;

            int pStart = writerView.GetCurrentParagraphStart();
            int curAlign = ribbon != null ? ribbon.Alignment : 0;
            if (writerView.ParagraphAlignments.ContainsKey(pStart)) curAlign = writerView.ParagraphAlignments[pStart];

            float curLineSp = writerView.GetCurrentParagraphLineSpacing();
            float curBefore = writerView.ParagraphSpacingsBefore.ContainsKey(pStart) ? writerView.ParagraphSpacingsBefore[pStart] : (ribbon != null ? ribbon.ParagraphSpacingBefore : 0f);
            float curAfter = writerView.ParagraphSpacingsAfter.ContainsKey(pStart) ? writerView.ParagraphSpacingsAfter[pStart] : (ribbon != null ? ribbon.ParagraphSpacingAfter : 8f);
            float curLeft = writerView.GetLeftIndent(pStart);
            float curRight = writerView.GetRightIndent(pStart);
            float curFirst = writerView.GetFirstLineIndent(pStart);
            float curHang = writerView.GetHangingIndent(pStart);

            using (OfficeParagraphDialogForm paraDlg = new OfficeParagraphDialogForm(
                curAlign, curLineSp, curBefore, curAfter, curLeft, curRight, curFirst, curHang))
            {
                paraDlg.OutlineLevelResult = writerView.ParagraphOutlineLevels.ContainsKey(pStart) ? writerView.ParagraphOutlineLevels[pStart] : 0;
                paraDlg.CollapsedByDefaultResult = writerView.ParagraphCollapsedByDefault.ContainsKey(pStart) && writerView.ParagraphCollapsedByDefault[pStart];
                paraDlg.MirrorIndentsResult = writerView.ParagraphMirrorIndents.ContainsKey(pStart) && writerView.ParagraphMirrorIndents[pStart];
                paraDlg.DontAddSpaceSameStyleResult = writerView.ParagraphDontAddSpaceSameStyle.ContainsKey(pStart) && writerView.ParagraphDontAddSpaceSameStyle[pStart];
                paraDlg.WidowOrphanResult = !writerView.ParagraphWidowOrphan.ContainsKey(pStart) || writerView.ParagraphWidowOrphan[pStart];
                paraDlg.KeepWithNextResult = writerView.ParagraphKeepWithNext.ContainsKey(pStart) && writerView.ParagraphKeepWithNext[pStart];
                paraDlg.KeepLinesTogetherResult = writerView.ParagraphKeepLinesTogether.ContainsKey(pStart) && writerView.ParagraphKeepLinesTogether[pStart];
                paraDlg.PageBreakBeforeResult = writerView.ParagraphPageBreakBefore.ContainsKey(pStart) && writerView.ParagraphPageBreakBefore[pStart];
                paraDlg.SuppressLineNumbersResult = writerView.ParagraphSuppressLineNumbers.ContainsKey(pStart) && writerView.ParagraphSuppressLineNumbers[pStart];
                paraDlg.DontHyphenateResult = writerView.ParagraphDontHyphenate.ContainsKey(pStart) && writerView.ParagraphDontHyphenate[pStart];
                paraDlg.TightWrapResult = writerView.ParagraphTightWrap.ContainsKey(pStart) ? writerView.ParagraphTightWrap[pStart] : 0;
                paraDlg.TabStopsResult = new List<TabStopInfo>(writerView.GetTabStops(pStart));
                paraDlg.DefaultTabStopInchesResult = writerView.DefaultTabStopInches;
                paraDlg.SyncControlsFromProperties();

                if (paraDlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    if (ribbon != null)
                    {
                        ribbon.Alignment = paraDlg.AlignmentResult;
                        ribbon.ParagraphSpacingBefore = paraDlg.SpaceBeforeResult;
                        ribbon.ParagraphSpacingAfter = paraDlg.SpaceAfterResult;
                        ribbon.LineSpacingFactor = paraDlg.LineSpacingResult;
                    }
                    writerView.ApplyFullParagraphSettings(
                        paraDlg.AlignmentResult,
                        paraDlg.OutlineLevelResult,
                        paraDlg.CollapsedByDefaultResult,
                        paraDlg.LeftIndentResult,
                        paraDlg.RightIndentResult,
                        paraDlg.MirrorIndentsResult,
                        paraDlg.FirstLineIndentResult,
                        paraDlg.HangingIndentResult,
                        paraDlg.SpaceBeforeResult,
                        paraDlg.SpaceAfterResult,
                        paraDlg.LineSpacingResult,
                        paraDlg.DontAddSpaceSameStyleResult,
                        paraDlg.WidowOrphanResult,
                        paraDlg.KeepWithNextResult,
                        paraDlg.KeepLinesTogetherResult,
                        paraDlg.PageBreakBeforeResult,
                        paraDlg.SuppressLineNumbersResult,
                        paraDlg.DontHyphenateResult,
                        paraDlg.TightWrapResult,
                        paraDlg.TabStopsResult,
                        paraDlg.DefaultTabStopInchesResult);

                    if (ribbon != null) writerView.UpdateRibbonState(ribbon);
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }
            }
            return false;
        }
    }

    public class OfficeParagraphDialogForm : Form
    {
        // Results
        public int AlignmentResult { get; private set; }
        public int OutlineLevelResult { get; set; }
        public bool CollapsedByDefaultResult { get; set; }

        public float LeftIndentResult { get; private set; }
        public float RightIndentResult { get; private set; }
        public bool MirrorIndentsResult { get; set; }
        public float FirstLineIndentResult { get; private set; }
        public float HangingIndentResult { get; private set; }

        public float SpaceBeforeResult { get; private set; }
        public float SpaceAfterResult { get; private set; }
        public float LineSpacingResult { get; private set; }
        public bool DontAddSpaceSameStyleResult { get; set; }

        // Tab 2: Line and Page Breaks results
        public bool WidowOrphanResult { get; set; } = true;
        public bool KeepWithNextResult { get; set; }
        public bool KeepLinesTogetherResult { get; set; }
        public bool PageBreakBeforeResult { get; set; }
        public bool SuppressLineNumbersResult { get; set; }
        public bool DontHyphenateResult { get; set; }
        public int TightWrapResult { get; set; }

        // Tabs result
        public List<TabStopInfo> TabStopsResult { get; set; } = new List<TabStopInfo>();
        public float DefaultTabStopInchesResult { get; set; } = 0.5f;

        // UI Controls - Tab 1
        private TabControl _tabs;
        private ComboBox _cboAlign;
        private ComboBox _cboOutline;
        private CheckBox _chkCollapsed;

        private NumericUpDown _numLeftIndent;
        private NumericUpDown _numRightIndent;
        private CheckBox _chkMirrorIndents;
        private ComboBox _cboSpecial;
        private NumericUpDown _numSpecialBy;

        private NumericUpDown _numBefore;
        private NumericUpDown _numAfter;
        private ComboBox _cboLineSpacing;
        private NumericUpDown _numLineAt;
        private CheckBox _chkDontAddSpace;

        // UI Controls - Tab 2
        private CheckBox _chkWidowOrphan;
        private CheckBox _chkKeepWithNext;
        private CheckBox _chkKeepLinesTogether;
        private CheckBox _chkPageBreakBefore;
        private CheckBox _chkSuppressLineNums;
        private CheckBox _chkDontHyphenate;
        private ComboBox _cboTightWrap;

        // Shared Preview Boxes
        private PictureBox _previewBox1;
        private PictureBox _previewBox2;

        public OfficeParagraphDialogForm(int align, float lineSpacing, float beforePt, float afterPt, float leftIndentPt = 0f, float rightIndentPt = 0f, float firstLineIndentPt = 0f, float hangingIndentPt = 0f)
        {
            AlignmentResult = align;
            LineSpacingResult = lineSpacing;
            SpaceBeforeResult = beforePt;
            SpaceAfterResult = afterPt;
            LeftIndentResult = leftIndentPt;
            RightIndentResult = rightIndentPt;
            FirstLineIndentResult = firstLineIndentPt;
            HangingIndentResult = hangingIndentPt;

            InitializeUI();
            SyncControlsFromProperties();
        }

        public void SyncControlsFromProperties()
        {
            if (_cboAlign == null) return;
            _cboAlign.SelectedIndex = Math.Max(0, Math.Min(3, AlignmentResult));
            _cboOutline.SelectedIndex = Math.Max(0, Math.Min(9, OutlineLevelResult));
            _chkCollapsed.Checked = CollapsedByDefaultResult;
            _chkCollapsed.Enabled = (_cboOutline.SelectedIndex > 0);

            _numLeftIndent.Value = Math.Max(-5m, Math.Min(10m, (decimal)Math.Round(LeftIndentResult / 72.0f, 2)));
            _numRightIndent.Value = Math.Max(-5m, Math.Min(10m, (decimal)Math.Round(RightIndentResult / 72.0f, 2)));
            _chkMirrorIndents.Checked = MirrorIndentsResult;

            if (FirstLineIndentResult > 0.5f)
            {
                _cboSpecial.SelectedIndex = 1;
                _numSpecialBy.Value = Math.Max(0.05m, Math.Min(5m, (decimal)Math.Round(FirstLineIndentResult / 72.0f, 2)));
            }
            else if (HangingIndentResult > 0.5f)
            {
                _cboSpecial.SelectedIndex = 2;
                _numSpecialBy.Value = Math.Max(0.05m, Math.Min(5m, (decimal)Math.Round(HangingIndentResult / 72.0f, 2)));
            }
            else
            {
                _cboSpecial.SelectedIndex = 0;
                _numSpecialBy.Value = 0m;
            }
            _numSpecialBy.Enabled = (_cboSpecial.SelectedIndex > 0);

            _numBefore.Value = Math.Max(0m, Math.Min(200m, (decimal)SpaceBeforeResult));
            _numAfter.Value = Math.Max(0m, Math.Min(200m, (decimal)SpaceAfterResult));
            _chkDontAddSpace.Checked = DontAddSpaceSameStyleResult;

            if (Math.Abs(LineSpacingResult - 1.0f) < 0.05f) _cboLineSpacing.SelectedIndex = 0;
            else if (Math.Abs(LineSpacingResult - 1.5f) < 0.05f) _cboLineSpacing.SelectedIndex = 1;
            else if (Math.Abs(LineSpacingResult - 2.0f) < 0.05f) _cboLineSpacing.SelectedIndex = 2;
            else _cboLineSpacing.SelectedIndex = 3;
            _numLineAt.Value = Math.Max(0.5m, Math.Min(10m, (decimal)Math.Round(LineSpacingResult, 2)));

            _chkWidowOrphan.Checked = WidowOrphanResult;
            _chkKeepWithNext.Checked = KeepWithNextResult;
            _chkKeepLinesTogether.Checked = KeepLinesTogetherResult;
            _chkPageBreakBefore.Checked = PageBreakBeforeResult;
            _chkSuppressLineNums.Checked = SuppressLineNumbersResult;
            _chkDontHyphenate.Checked = DontHyphenateResult;
            _cboTightWrap.SelectedIndex = Math.Max(0, Math.Min(_cboTightWrap.Items.Count - 1, TightWrapResult));

            InvalidatePreviews();
        }

        private void InvalidatePreviews()
        {
            _previewBox1?.Invalidate();
            _previewBox2?.Invalidate();
        }

        private void InitializeUI()
        {
            this.Text = AppL10n.T("Đoạn văn (Paragraph)", "Paragraph");
            this.Size = new Size(462, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 246, 248);
            this.Font = new Font("Segoe UI", 9.0f);

            _tabs = new ZenQOffice.UI.FluentTabControl
            {
                Location = new Point(12, 12),
                Size = new Size(424, 464)
            };

            TabPage tabIndents = new TabPage(AppL10n.T("Thụt lề && Giãn cách", "Indents && Spacing")) { BackColor = Color.White };
            TabPage tabBreaks = new TabPage(AppL10n.T("Ngắt dòng && Trang", "Line && Page Breaks")) { BackColor = Color.White };

            BuildIndentsAndSpacingTab(tabIndents);
            BuildLineAndPageBreaksTab(tabBreaks);

            _tabs.TabPages.Add(tabIndents);
            _tabs.TabPages.Add(tabBreaks);
            this.Controls.Add(_tabs);

            // Bottom Action Bar matching Word 365: Tabs... | Set As Default | OK | Cancel
            Button btnTabs = new Button
            {
                Text = "Tab (Tabs...)",
                Location = new Point(12, 486),
                Size = new Size(92, 27),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTabs.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            btnTabs.Click += (s, e) =>
            {
                using (TabsDialogForm tabsDlg = new TabsDialogForm(TabStopsResult, DefaultTabStopInchesResult))
                {
                    if (tabsDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        TabStopsResult = tabsDlg.TabStopsResult;
                        DefaultTabStopInchesResult = tabsDlg.DefaultTabStopInchesResult;
                    }
                }
            };
            this.Controls.Add(btnTabs);

            Button btnSetDefault = new Button
            {
                Text = AppL10n.T(AppL10n.T("Đặt làm mặc định", "Set As Default"), "Set As Default"),
                Location = new Point(110, 486),
                Size = new Size(154, 27),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSetDefault.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            btnSetDefault.Click += (s, e) =>
            {
                SaveValues();
                ZenQOffice.UI.FluentMessageBox.Show(
                    AppL10n.T("Đã lưu thiết lập Đoạn văn hiện tại làm mặc định cho tài liệu này.", "Saved current Paragraph settings as default for this document."),
                    AppL10n.T("Đặt làm mặc định", "Set As Default"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            this.Controls.Add(btnSetDefault);

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(272, 486),
                Size = new Size(78, 27),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { SaveValues(); };
            this.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = AppL10n.T("Hủy", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(356, 486),
                Size = new Size(80, 27),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BuildIndentsAndSpacingTab(TabPage tab)
        {
            // 1. General
            GroupBox grpGeneral = new GroupBox { Text = "Chung (General)", Location = new Point(12, 8), Size = new Size(392, 84) };
            grpGeneral.Controls.Add(new Label { Text = AppL10n.T("Căn chỉnh (Alignment):", "Alignment:"), Location = new Point(14, 24), AutoSize = true });
            _cboAlign = new ZenQOffice.UI.FluentComboBox { Location = new Point(140, 20), Size = new Size(116, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboAlign.Items.AddRange(new object[] { AppL10n.T("Trái (Left)", "Left"), AppL10n.T("Giữa (Centered)", "Centered"), AppL10n.T("Phải (Right)", "Right"), AppL10n.T("Đều (Justified)", "Justified") });
            _cboAlign.SelectedIndexChanged += (s, e) => InvalidatePreviews();
            grpGeneral.Controls.Add(_cboAlign);

            grpGeneral.Controls.Add(new Label { Text = AppL10n.T("Cấp độ dàn ý (Outline):", "Outline level:"), Location = new Point(14, 54), AutoSize = true });
            _cboOutline = new ZenQOffice.UI.FluentComboBox { Location = new Point(140, 50), Size = new Size(116, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboOutline.Items.Add(AppL10n.T("Văn bản (Body Text)", "Body Text"));
            for (int lv = 1; lv <= 9; lv++) _cboOutline.Items.Add(AppL10n.T("Cấp " + lv + " (Level " + lv + ")", "Level " + lv));
            _cboOutline.SelectedIndexChanged += (s, e) =>
            {
                _chkCollapsed.Enabled = (_cboOutline.SelectedIndex > 0);
                if (!_chkCollapsed.Enabled) _chkCollapsed.Checked = false;
            };
            grpGeneral.Controls.Add(_cboOutline);

            _chkCollapsed = new CheckBox { Text = AppL10n.T("Thu gọn mặc định", "Collapsed by default"), Location = new Point(264, 52), AutoSize = true, Enabled = false };
            grpGeneral.Controls.Add(_chkCollapsed);
            tab.Controls.Add(grpGeneral);

            // 2. Indentation
            GroupBox grpIndent = new GroupBox { Text = AppL10n.T("Thụt lề (Indentation - inch)", "Indentation (inches)"), Location = new Point(12, 98), Size = new Size(392, 108) };
            grpIndent.Controls.Add(new Label { Text = AppL10n.T("Trái (Left):", "Left:"), Location = new Point(14, 24), AutoSize = true });
            _numLeftIndent = new NumericUpDown { Location = new Point(92, 22), Size = new Size(68, 22), DecimalPlaces = 2, Increment = 0.1m, Minimum = -5, Maximum = 10 };
            _numLeftIndent.ValueChanged += (s, e) => InvalidatePreviews();
            grpIndent.Controls.Add(_numLeftIndent);

            grpIndent.Controls.Add(new Label { Text = AppL10n.T("Phải (Right):", "Right:"), Location = new Point(14, 54), AutoSize = true });
            _numRightIndent = new NumericUpDown { Location = new Point(92, 52), Size = new Size(68, 22), DecimalPlaces = 2, Increment = 0.1m, Minimum = -5, Maximum = 10 };
            _numRightIndent.ValueChanged += (s, e) => InvalidatePreviews();
            grpIndent.Controls.Add(_numRightIndent);

            _chkMirrorIndents = new CheckBox { Text = AppL10n.T("Đối xứng lề (Mirror indents)", "Mirror indents"), Location = new Point(14, 82), AutoSize = true };
            _chkMirrorIndents.CheckedChanged += (s, e) =>
            {
                _numLeftIndent.Parent.Controls[0].Text = _chkMirrorIndents.Checked ? AppL10n.T("Trong (Inside):", "Inside:") : AppL10n.T("Trái (Left):", "Left:");
                _numLeftIndent.Parent.Controls[2].Text = _chkMirrorIndents.Checked ? AppL10n.T("Ngoài (Outside):", "Outside:") : AppL10n.T("Phải (Right):", "Right:");
            };
            grpIndent.Controls.Add(_chkMirrorIndents);

            grpIndent.Controls.Add(new Label { Text = AppL10n.T("Đặc biệt (Special):", "Special:"), Location = new Point(176, 24), AutoSize = true });
            _cboSpecial = new ZenQOffice.UI.FluentComboBox { Location = new Point(176, 46), Size = new Size(124, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboSpecial.Items.AddRange(new object[] { AppL10n.T("(không có)", "(none)"), AppL10n.T("Thụt đầu dòng (First line)", "First line"), AppL10n.T("Treo (Hanging)", "Hanging") });
            _cboSpecial.SelectedIndexChanged += (s, e) =>
            {
                _numSpecialBy.Enabled = (_cboSpecial.SelectedIndex > 0);
                if (_numSpecialBy.Enabled && _numSpecialBy.Value == 0m) _numSpecialBy.Value = 0.50m;
                InvalidatePreviews();
            };
            grpIndent.Controls.Add(_cboSpecial);

            grpIndent.Controls.Add(new Label { Text = AppL10n.T("Bởi (By):", "By:"), Location = new Point(312, 24), AutoSize = true });
            _numSpecialBy = new NumericUpDown { Location = new Point(312, 46), Size = new Size(66, 22), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0, Maximum = 5, Value = 0.5m };
            _numSpecialBy.ValueChanged += (s, e) => InvalidatePreviews();
            grpIndent.Controls.Add(_numSpecialBy);
            tab.Controls.Add(grpIndent);

            // 3. Spacing
            GroupBox grpSpace = new GroupBox { Text = AppL10n.T("Giãn cách (Spacing)", "Spacing"), Location = new Point(12, 212), Size = new Size(392, 108) };
            grpSpace.Controls.Add(new Label { Text = AppL10n.T("Trước (Before):", "Before:"), Location = new Point(14, 24), AutoSize = true });
            _numBefore = new NumericUpDown { Location = new Point(104, 22), Size = new Size(64, 22), DecimalPlaces = 0, Increment = 6, Minimum = 0, Maximum = 200 };
            _numBefore.ValueChanged += (s, e) => InvalidatePreviews();
            grpSpace.Controls.Add(_numBefore);

            grpSpace.Controls.Add(new Label { Text = AppL10n.T("Sau (After):", "After:"), Location = new Point(14, 54), AutoSize = true });
            _numAfter = new NumericUpDown { Location = new Point(104, 52), Size = new Size(64, 22), DecimalPlaces = 0, Increment = 6, Minimum = 0, Maximum = 200 };
            _numAfter.ValueChanged += (s, e) => InvalidatePreviews();
            grpSpace.Controls.Add(_numAfter);

            grpSpace.Controls.Add(new Label { Text = AppL10n.T("Giãn dòng (Line):", "Line spacing:"), Location = new Point(182, 24), AutoSize = true });
            _cboLineSpacing = new ZenQOffice.UI.FluentComboBox { Location = new Point(182, 46), Size = new Size(122, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboLineSpacing.Items.AddRange(new object[] { AppL10n.T("Đơn (1.0)", "Single"), AppL10n.T("1.5 dòng", "1.5 lines"), AppL10n.T("Đôi (2.0)", "Double"), AppL10n.T("Bội số (Multiple)", "Multiple"), AppL10n.T("Tối thiểu (At least)", "At least"), AppL10n.T("Chính xác (Exactly)", "Exactly") });
            _cboLineSpacing.SelectedIndexChanged += (s, e) =>
            {
                if (_cboLineSpacing.SelectedIndex == 0) _numLineAt.Value = 1.00m;
                else if (_cboLineSpacing.SelectedIndex == 1) _numLineAt.Value = 1.50m;
                else if (_cboLineSpacing.SelectedIndex == 2) _numLineAt.Value = 2.00m;
                else if (_cboLineSpacing.SelectedIndex == 3 && _numLineAt.Value < 1.05m) _numLineAt.Value = 1.15m;
                InvalidatePreviews();
            };
            grpSpace.Controls.Add(_cboLineSpacing);

            grpSpace.Controls.Add(new Label { Text = AppL10n.T("Mức (At):", "At:"), Location = new Point(312, 24), AutoSize = true });
            _numLineAt = new NumericUpDown { Location = new Point(312, 46), Size = new Size(66, 22), DecimalPlaces = 2, Increment = 0.05m, Minimum = 0.5m, Maximum = 10 };
            _numLineAt.ValueChanged += (s, e) => InvalidatePreviews();
            grpSpace.Controls.Add(_numLineAt);

            _chkDontAddSpace = new CheckBox { Text = AppL10n.T("Không thêm khoảng cách giữa các đoạn cùng kiểu (same style)", "Don't add space between paragraphs of the same style"), Location = new Point(14, 82), AutoSize = true };
            _chkDontAddSpace.CheckedChanged += (s, e) => InvalidatePreviews();
            grpSpace.Controls.Add(_chkDontAddSpace);
            tab.Controls.Add(grpSpace);

            // 4. Preview
            GroupBox grpPrev = new GroupBox { Text = AppL10n.T("Xem trước (Preview)", "Preview"), Location = new Point(12, 326), Size = new Size(392, 98) };
            _previewBox1 = CreatePreviewBox();
            grpPrev.Controls.Add(_previewBox1);
            tab.Controls.Add(grpPrev);
        }

        private void BuildLineAndPageBreaksTab(TabPage tab)
        {
            // 1. Pagination
            GroupBox grpPag = new GroupBox { Text = AppL10n.T("Phân trang (Pagination)", "Pagination"), Location = new Point(12, 10), Size = new Size(392, 126) };
            _chkWidowOrphan = new CheckBox { Text = AppL10n.T("Kiểm soát dòng mồ côi (Widow/Orphan control)", "Widow/Orphan control"), Location = new Point(16, 24), AutoSize = true, Checked = true };
            _chkKeepWithNext = new CheckBox { Text = AppL10n.T("Giữ cùng đoạn tiếp theo (Keep with next)", "Keep with next"), Location = new Point(16, 48), AutoSize = true };
            _chkKeepLinesTogether = new CheckBox { Text = AppL10n.T("Giữ các dòng cùng nhau (Keep lines together)", "Keep lines together"), Location = new Point(16, 72), AutoSize = true };
            _chkPageBreakBefore = new CheckBox { Text = AppL10n.T("Ngắt trang trước đoạn (Page break before)", "Page break before"), Location = new Point(16, 96), AutoSize = true };
            _chkWidowOrphan.CheckedChanged += (s, e) => InvalidatePreviews();
            _chkKeepWithNext.CheckedChanged += (s, e) => InvalidatePreviews();
            _chkKeepLinesTogether.CheckedChanged += (s, e) => InvalidatePreviews();
            _chkPageBreakBefore.CheckedChanged += (s, e) => InvalidatePreviews();
            grpPag.Controls.AddRange(new Control[] { _chkWidowOrphan, _chkKeepWithNext, _chkKeepLinesTogether, _chkPageBreakBefore });
            tab.Controls.Add(grpPag);

            // 2. Formatting exceptions
            GroupBox grpExc = new GroupBox { Text = AppL10n.T("Ngoại lệ định dạng (Formatting exceptions)", "Formatting exceptions"), Location = new Point(12, 144), Size = new Size(392, 78) };
            _chkSuppressLineNums = new CheckBox { Text = AppL10n.T("Ẩn số dòng (Suppress line numbers)", "Suppress line numbers"), Location = new Point(16, 24), AutoSize = true };
            _chkDontHyphenate = new CheckBox { Text = AppL10n.T("Không gạch nối tự động (Don't hyphenate)", "Don't hyphenate"), Location = new Point(16, 48), AutoSize = true };
            grpExc.Controls.AddRange(new Control[] { _chkSuppressLineNums, _chkDontHyphenate });
            tab.Controls.Add(grpExc);

            // 3. Textbox options
            GroupBox grpTb = new GroupBox { Text = AppL10n.T("Tùy chọn hộp văn bản (Textbox options)", "Textbox options"), Location = new Point(12, 230), Size = new Size(392, 74) };
            grpTb.Controls.Add(new Label { Text = AppL10n.T("Bao chữ khít (Tight wrap):", "Tight wrap:"), Location = new Point(16, 22), AutoSize = true });
            _cboTightWrap = new ZenQOffice.UI.FluentComboBox { Location = new Point(16, 42), Size = new Size(190, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboTightWrap.Items.AddRange(new object[] { AppL10n.T("Không (None)", "None"), AppL10n.T("Tất cả dòng (All lines)", "All lines"), AppL10n.T("Dòng đầu & cuối", "First and last"), AppL10n.T("Chỉ dòng đầu", "First only"), AppL10n.T("Chỉ dòng cuối", "Last only") });
            _cboTightWrap.SelectedIndex = 0;
            grpTb.Controls.Add(_cboTightWrap);
            tab.Controls.Add(grpTb);

            // 4. Preview
            GroupBox grpPrev2 = new GroupBox { Text = AppL10n.T("Xem trước (Preview)", "Preview"), Location = new Point(12, 326), Size = new Size(392, 98) };
            _previewBox2 = CreatePreviewBox();
            grpPrev2.Controls.Add(_previewBox2);
            tab.Controls.Add(grpPrev2);
        }

        private PictureBox CreatePreviewBox()
        {
            PictureBox box = new PictureBox
            {
                Location = new Point(10, 18),
                Size = new Size(372, 72),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            box.Paint += PreviewBox_Paint;
            return box;
        }

        private void PreviewBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = ((Control)sender).Width;
            int h = ((Control)sender).Height;
            g.Clear(Color.White);

            int padX = 12;
            float leftPx = padX + Math.Max(-8, Math.Min(65, (float)_numLeftIndent.Value * 18f));
            float rightEdge = w - padX - Math.Max(-8, Math.Min(65, (float)_numRightIndent.Value * 18f));
            float availW = Math.Max(40f, rightEdge - leftPx);

            float spByPx = _cboSpecial.SelectedIndex > 0 ? Math.Max(0f, Math.Min(36f, (float)_numSpecialBy.Value * 18f)) : 0f;
            float lineStep = 8f * Math.Max(0.7f, Math.Min(1.8f, (float)_numLineAt.Value));
            float beforeGap = Math.Min(12f, (float)_numBefore.Value * 0.25f);
            float afterGap = _chkDontAddSpace.Checked ? 0f : Math.Min(12f, (float)_numAfter.Value * 0.25f);

            using (Font f = new Font("Segoe UI", 6.5f))
            using (SolidBrush grayB = new SolidBrush(Color.FromArgb(185, 190, 198)))
            using (SolidBrush darkB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                // Previous Paragraph (gray)
                StringFormat sfLeft = new StringFormat { Trimming = StringTrimming.None };
                g.DrawString("Previous Paragraph Previous Paragraph Previous Paragraph", f, grayB, new RectangleF(padX, 4, w - padX * 2, 10), sfLeft);

                float sampleY = 16f + beforeGap * 0.45f;
                StringFormat sfSample = new StringFormat();
                if (_cboAlign.SelectedIndex == 1) sfSample.Alignment = StringAlignment.Center;
                else if (_cboAlign.SelectedIndex == 2) sfSample.Alignment = StringAlignment.Far;
                else sfSample.Alignment = StringAlignment.Near;

                string[] sampleLines = new string[] {
                    "Sample Text Sample Text Sample Text Sample Text",
                    "Sample Text Sample Text Sample Text Sample Text",
                    "Sample Text Sample Text Sample Text"
                };

                for (int i = 0; i < sampleLines.Length; i++)
                {
                    float lx = leftPx;
                    if (_cboSpecial.SelectedIndex == 1 && i == 0) lx += spByPx;
                    else if (_cboSpecial.SelectedIndex == 2 && i > 0) lx += spByPx;
                    float lw = Math.Max(30f, rightEdge - lx);
                    float ly = sampleY + i * Math.Min(9.5f, lineStep);
                    if (ly < h - 14)
                    {
                        g.DrawString(sampleLines[i], f, darkB, new RectangleF(lx, ly, lw, 10), sfSample);
                    }
                }

                float followY = sampleY + 3 * Math.Min(9.5f, lineStep) + afterGap * 0.45f + 2f;
                if (followY < h - 8)
                {
                    g.DrawString("Following Paragraph Following Paragraph Following Paragraph", f, grayB, new RectangleF(padX, followY, w - padX * 2, 10), sfLeft);
                }
            }
        }

        private void SaveValues()
        {
            AlignmentResult = Math.Max(0, _cboAlign.SelectedIndex);
            OutlineLevelResult = Math.Max(0, _cboOutline.SelectedIndex);
            CollapsedByDefaultResult = _chkCollapsed.Checked && OutlineLevelResult > 0;

            LeftIndentResult = (float)_numLeftIndent.Value * 72.0f;
            RightIndentResult = (float)_numRightIndent.Value * 72.0f;
            MirrorIndentsResult = _chkMirrorIndents.Checked;
            float specValPt = (float)_numSpecialBy.Value * 72.0f;
            if (_cboSpecial.SelectedIndex == 1)
            {
                FirstLineIndentResult = specValPt;
                HangingIndentResult = 0f;
            }
            else if (_cboSpecial.SelectedIndex == 2)
            {
                FirstLineIndentResult = 0f;
                HangingIndentResult = specValPt;
            }
            else
            {
                FirstLineIndentResult = 0f;
                HangingIndentResult = 0f;
            }

            SpaceBeforeResult = (float)_numBefore.Value;
            SpaceAfterResult = (float)_numAfter.Value;
            LineSpacingResult = (float)_numLineAt.Value;
            DontAddSpaceSameStyleResult = _chkDontAddSpace.Checked;

            WidowOrphanResult = _chkWidowOrphan.Checked;
            KeepWithNextResult = _chkKeepWithNext.Checked;
            KeepLinesTogetherResult = _chkKeepLinesTogether.Checked;
            PageBreakBeforeResult = _chkPageBreakBefore.Checked;
            SuppressLineNumbersResult = _chkSuppressLineNums.Checked;
            DontHyphenateResult = _chkDontHyphenate.Checked;
            TightWrapResult = Math.Max(0, _cboTightWrap.SelectedIndex);
        }
    }
}
