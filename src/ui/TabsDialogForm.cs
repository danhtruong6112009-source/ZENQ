using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Word 365 Tabs dialog opened from the Paragraph dialog ("Tabs...") or ruler.
    /// Edits per-paragraph TabStopInfo list and DefaultTabStopInches.
    /// </summary>
    public class TabsDialogForm : Form
    {
        public List<TabStopInfo> TabStopsResult { get; private set; }
        public float DefaultTabStopInchesResult { get; private set; }

        private TextBox _txtPos;
        private ListBox _lstStops;
        private NumericUpDown _numDefaultTab;
        private Label _lblClearedList;

        private RadioButton _rdoLeft;
        private RadioButton _rdoCenter;
        private RadioButton _rdoRight;
        private RadioButton _rdoDecimal;
        private RadioButton _rdoBar;

        private RadioButton _rdoLeadNone;
        private RadioButton _rdoLeadDots;
        private RadioButton _rdoLeadDashes;
        private RadioButton _rdoLeadUnderline;

        private Button _btnSet;
        private Button _btnClear;
        private Button _btnClearAll;

        private readonly List<TabStopInfo> _workingStops = new List<TabStopInfo>();
        private readonly List<float> _clearedInches = new List<float>();

        public TabsDialogForm(IEnumerable<TabStopInfo> currentStops, float defaultTabInches = 0.5f)
        {
            DefaultTabStopInchesResult = defaultTabInches > 0.05f ? defaultTabInches : 0.5f;
            if (currentStops != null)
            {
                foreach (var ts in currentStops)
                {
                    if (ts != null)
                    {
                        _workingStops.Add(new TabStopInfo(ts.PositionPt, ts.Type, ts.Leader));
                    }
                }
            }
            _workingStops.Sort((a, b) => a.PositionPt.CompareTo(b.PositionPt));
            TabStopsResult = CloneList(_workingStops);

            BuildUI();
            RefreshStopsList();
        }

        private static List<TabStopInfo> CloneList(List<TabStopInfo> src)
        {
            var list = new List<TabStopInfo>();
            if (src == null) return list;
            foreach (var t in src)
            {
                if (t != null) list.Add(new TabStopInfo(t.PositionPt, t.Type, t.Leader));
            }
            return list;
        }

        private void BuildUI()
        {
            Text = AppL10n.T("Điểm dừng Tab (Tabs)", "Tabs");
            Size = new Size(418, 415);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 246, 248);
            Font = new Font("Segoe UI", 9f);

            Label lblPos = new Label { Text = AppL10n.T("Vị trí điểm dừng Tab:", "Tab stop position:"), Location = new Point(14, 14), AutoSize = true };
            Controls.Add(lblPos);

            _txtPos = new TextBox { Location = new Point(14, 34), Size = new Size(196, 23) };
            _txtPos.TextChanged += (s, e) => UpdateActionButtons();
            Controls.Add(_txtPos);

            _lstStops = new ZenQOffice.UI.FluentListBox { Location = new Point(14, 60), Size = new Size(196, 92) };
            _lstStops.SelectedIndexChanged += LstStops_SelectedIndexChanged;
            Controls.Add(_lstStops);

            Label lblDef = new Label { Text = AppL10n.T("Mặc định (Default tabs):", "Default tab stops:"), Location = new Point(226, 14), AutoSize = true };
            Controls.Add(lblDef);

            _numDefaultTab = new NumericUpDown
            {
                Location = new Point(226, 34),
                Size = new Size(162, 23),
                DecimalPlaces = 2,
                Increment = 0.10m,
                Minimum = 0.10m,
                Maximum = 6.00m,
                Value = Math.Max(0.10m, Math.Min(6.00m, (decimal)DefaultTabStopInchesResult))
            };
            Controls.Add(_numDefaultTab);

            Label lblClearedHdr = new Label { Text = AppL10n.T("Điểm dừng sẽ xóa:", "Tab stops to be cleared:"), Location = new Point(226, 66), AutoSize = true };
            Controls.Add(lblClearedHdr);

            _lblClearedList = new Label
            {
                Text = AppL10n.T("(chưa có)", "(none)"),
                Location = new Point(226, 86),
                Size = new Size(162, 64),
                ForeColor = Color.FromArgb(90, 98, 110)
            };
            Controls.Add(_lblClearedList);

            // Alignment GroupBox
            GroupBox grpAlign = new GroupBox { Text = AppL10n.T("Căn chỉnh (Alignment)", "Alignment"), Location = new Point(14, 162), Size = new Size(374, 72) };
            _rdoLeft = new RadioButton { Text = AppL10n.T("Trái (Left)", "Left"), Location = new Point(14, 22), AutoSize = true, Checked = true };
            _rdoCenter = new RadioButton { Text = AppL10n.T("Giữa (Center)", "Center"), Location = new Point(126, 22), AutoSize = true };
            _rdoRight = new RadioButton { Text = AppL10n.T("Phải (Right)", "Right"), Location = new Point(244, 22), AutoSize = true };
            _rdoDecimal = new RadioButton { Text = AppL10n.T("Thập phân (Decimal)", "Decimal"), Location = new Point(14, 46), AutoSize = true };
            _rdoBar = new RadioButton { Text = "Thanh (Bar)", Location = new Point(156, 46), AutoSize = true };
            grpAlign.Controls.AddRange(new Control[] { _rdoLeft, _rdoCenter, _rdoRight, _rdoDecimal, _rdoBar });
            Controls.Add(grpAlign);

            // Leader GroupBox
            GroupBox grpLeader = new GroupBox { Text = AppL10n.T("Ký tự dẫn (Leader)", "Leader"), Location = new Point(14, 240), Size = new Size(374, 68) };
            _rdoLeadNone = new RadioButton { Text = AppL10n.T("1 Không (None)", "1 None"), Location = new Point(14, 20), AutoSize = true, Checked = true };
            _rdoLeadDots = new RadioButton { Text = "2 ........", Location = new Point(140, 20), AutoSize = true };
            _rdoLeadDashes = new RadioButton { Text = "3 --------", Location = new Point(244, 20), AutoSize = true };
            _rdoLeadUnderline = new RadioButton { Text = "4 _____", Location = new Point(14, 44), AutoSize = true };
            grpLeader.Controls.AddRange(new Control[] { _rdoLeadNone, _rdoLeadDots, _rdoLeadDashes, _rdoLeadUnderline });
            Controls.Add(grpLeader);

            // Row of Set / Clear / Clear All buttons
            _btnSet = new Button { Text = AppL10n.T("Đặt (Set)", "Set"), Location = new Point(78, 318), Size = new Size(96, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnSet.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            _btnSet.Click += BtnSet_Click;
            Controls.Add(_btnSet);

            _btnClear = new Button { Text = AppL10n.T("Xóa (Clear)", "Clear"), Location = new Point(184, 318), Size = new Size(96, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnClear.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            _btnClear.Click += BtnClear_Click;
            Controls.Add(_btnClear);

            _btnClearAll = new Button { Text = AppL10n.T("Xóa tất cả", "Clear All"), Location = new Point(290, 318), Size = new Size(98, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            _btnClearAll.Click += BtnClearAll_Click;
            Controls.Add(_btnClearAll);

            // OK / Cancel
            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(218, 350),
                Size = new Size(82, 26),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                CommitPendingInput();
                TabStopsResult = CloneList(_workingStops);
                DefaultTabStopInchesResult = (float)_numDefaultTab.Value;
            };
            Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = AppL10n.T("Hủy", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(306, 350),
                Size = new Size(82, 26),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private TabStopType GetSelectedType()
        {
            if (_rdoCenter.Checked) return TabStopType.Center;
            if (_rdoRight.Checked) return TabStopType.Right;
            if (_rdoDecimal.Checked) return TabStopType.Decimal;
            if (_rdoBar.Checked) return TabStopType.Bar;
            return TabStopType.Left;
        }

        private int GetSelectedLeader()
        {
            if (_rdoLeadDots.Checked) return 1;
            if (_rdoLeadDashes.Checked) return 2;
            if (_rdoLeadUnderline.Checked) return 3;
            return 0;
        }

        private void ApplySelectionToRadios(TabStopInfo info)
        {
            _rdoLeft.Checked = info.Type == TabStopType.Left;
            _rdoCenter.Checked = info.Type == TabStopType.Center;
            _rdoRight.Checked = info.Type == TabStopType.Right;
            _rdoDecimal.Checked = info.Type == TabStopType.Decimal;
            _rdoBar.Checked = info.Type == TabStopType.Bar;

            _rdoLeadNone.Checked = info.Leader == 0;
            _rdoLeadDots.Checked = info.Leader == 1;
            _rdoLeadDashes.Checked = info.Leader == 2;
            _rdoLeadUnderline.Checked = info.Leader == 3;
        }

        private bool TryParsePositionPt(out float posPt)
        {
            posPt = 0f;
            string raw = (_txtPos.Text ?? "").Trim().Replace("\"", "").Replace("in", "").Replace(",", ".");
            if (string.IsNullOrEmpty(raw)) return false;
            float inches;
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out inches)) return false;
            if (inches < 0f || inches > 22f) return false;
            posPt = inches * 72f;
            return true;
        }

        private void CommitPendingInput()
        {
            float pt;
            if (TryParsePositionPt(out pt))
            {
                UpsertTabStop(pt, GetSelectedType(), GetSelectedLeader());
            }
        }

        private void UpsertTabStop(float posPt, TabStopType type, int leader)
        {
            for (int i = 0; i < _workingStops.Count; i++)
            {
                if (Math.Abs(_workingStops[i].PositionPt - posPt) < 1.5f)
                {
                    _workingStops[i].PositionPt = posPt;
                    _workingStops[i].Type = type;
                    _workingStops[i].Leader = leader;
                    _workingStops.Sort((a, b) => a.PositionPt.CompareTo(b.PositionPt));
                    RefreshStopsList();
                    return;
                }
            }
            _workingStops.Add(new TabStopInfo(posPt, type, leader));
            _workingStops.Sort((a, b) => a.PositionPt.CompareTo(b.PositionPt));
            RefreshStopsList();
        }

        private void LstStops_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = _lstStops.SelectedIndex;
            if (idx >= 0 && idx < _workingStops.Count)
            {
                var info = _workingStops[idx];
                _txtPos.Text = (info.PositionPt / 72f).ToString("0.##", CultureInfo.InvariantCulture) + "\"";
                ApplySelectionToRadios(info);
            }
            UpdateActionButtons();
        }

        private void BtnSet_Click(object sender, EventArgs e)
        {
            float pt;
            if (TryParsePositionPt(out pt))
            {
                UpsertTabStop(pt, GetSelectedType(), GetSelectedLeader());
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            int idx = _lstStops.SelectedIndex;
            if (idx >= 0 && idx < _workingStops.Count)
            {
                _clearedInches.Add(_workingStops[idx].PositionPt / 72f);
                _workingStops.RemoveAt(idx);
                _txtPos.Text = "";
                RefreshStopsList();
            }
        }

        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            foreach (var ts in _workingStops)
            {
                _clearedInches.Add(ts.PositionPt / 72f);
            }
            _workingStops.Clear();
            _txtPos.Text = "";
            RefreshStopsList();
        }

        private void RefreshStopsList()
        {
            _lstStops.Items.Clear();
            foreach (var ts in _workingStops)
            {
                _lstStops.Items.Add((ts.PositionPt / 72f).ToString("0.##", CultureInfo.InvariantCulture) + "\"");
            }
            if (_clearedInches.Count > 0)
            {
                List<string> parts = new List<string>();
                foreach (float v in _clearedInches) parts.Add(v.ToString("0.##", CultureInfo.InvariantCulture) + "\"");
                _lblClearedList.Text = string.Join(", ", parts.ToArray());
            }
            else
            {
                _lblClearedList.Text = AppL10n.T("(chưa có)", "(none)");
            }
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            float dummy;
            _btnSet.Enabled = TryParsePositionPt(out dummy);
            _btnClear.Enabled = (_lstStops.SelectedIndex >= 0 && _lstStops.SelectedIndex < _workingStops.Count);
            _btnClearAll.Enabled = (_workingStops.Count > 0);
        }
    }
}
