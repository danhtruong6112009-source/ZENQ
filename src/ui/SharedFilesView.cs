using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZenQOffice.Core;
using Graphics = ZenQOffice.UI.D2DGraphics;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Màn hình trung tâm "Được chia sẻ" trên Trang chủ (Home):
    /// hiển thị hộp thư đến (tự động tải về máy như Zalo, lưu Cloud 30 ngày),
    /// tệp đã gửi và thanh dung lượng ổ đĩa Cloud 200 MB của người gửi.
    /// </summary>
    public class SharedFilesView
    {
        public int ActiveSubTab { get; set; } = 0; // 0: Đã nhận (Inbox), 1: Đã gửi (Sent), 2: Cộng tác trực tuyến (Rooms)
        public float ScrollY { get; set; } = 0f;

        private RectangleF _sendBtnRect = RectangleF.Empty;
        private RectangleF _joinCodeBtnRect = RectangleF.Empty;
        private RectangleF _refreshBtnRect = RectangleF.Empty;
        private RectangleF _openFolderBtnRect = RectangleF.Empty;
        private RectangleF _inboxTabRect = RectangleF.Empty;
        private RectangleF _sentTabRect = RectangleF.Empty;
        private RectangleF _collabTabRect = RectangleF.Empty;
        private RectangleF _loginCtaBtnRect = RectangleF.Empty;

        private readonly List<(RectangleF Card, RectangleF PrimaryBtn, RectangleF SecondaryBtn, CloudShareItem Item, bool IsSent)> _rowHitBoxes
            = new List<(RectangleF, RectangleF, RectangleF, CloudShareItem, bool)>();
        private readonly List<(RectangleF Card, RectangleF PrimaryBtn, RectangleF CopyBtn, RectangleF LeaveBtn, CollabRoomInfo Room)> _roomHitBoxes
            = new List<(RectangleF, RectangleF, RectangleF, RectangleF, CollabRoomInfo)>();

        public bool HoverSendBtn { get; private set; }
        public bool HoverJoinCodeBtn { get; private set; }
        public bool HoverRefreshBtn { get; private set; }
        public bool HoverOpenFolderBtn { get; private set; }
        public bool HoverInboxTab { get; private set; }
        public bool HoverSentTab { get; private set; }
        public bool HoverCollabTab { get; private set; }
        public bool HoverLoginBtn { get; private set; }
        public int HoverRowIndex { get; private set; } = -1;
        public int HoverRowBtnType { get; private set; } = -1; // 0=Card, 1=Primary, 2=Secondary, 3=Third

        public bool IsHoverHand =>
            HoverSendBtn || HoverJoinCodeBtn || HoverRefreshBtn || HoverOpenFolderBtn ||
            HoverInboxTab || HoverSentTab || HoverCollabTab || HoverLoginBtn || HoverRowIndex >= 0;

        public int GetContentHeight(string searchText)
        {
            if (!DatabaseAuthService.IsLoggedIn) return 380;
            if (ActiveSubTab == 2)
            {
                int count = GetFilteredRooms(searchText).Count;
                return 178 + Math.Max(1, count) * 82 + 40;
            }
            var list = GetFilteredItems(searchText);
            return 178 + Math.Max(1, list.Count) * 82 + 40;
        }

        public List<CollabRoomInfo> GetFilteredRooms(string searchText)
        {
            var src = CollabRoomService.MyRooms ?? new List<CollabRoomInfo>();
            string q = (searchText ?? "").Trim();
            if (string.IsNullOrEmpty(q)) return new List<CollabRoomInfo>(src);
            return src.Where(r =>
                (r.Title != null && r.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.RoomCode != null && r.RoomCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.OwnerUsername != null && r.OwnerUsername.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();
        }

        public List<CloudShareItem> GetFilteredItems(string searchText)
        {
            var source = (ActiveSubTab == 0) ? CloudShareService.InboxItems : CloudShareService.SentItems;
            if (source == null) return new List<CloudShareItem>();
            string q = (searchText ?? "").Trim();
            if (string.IsNullOrEmpty(q)) return new List<CloudShareItem>(source);

            return source.Where(it =>
                (it.FileName != null && it.FileName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (it.SenderUsername != null && it.SenderUsername.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (it.RecipientUsername != null && it.RecipientUsername.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (it.Note != null && it.Note.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();
        }

        public void UpdateHover(int x, int y, string searchText)
        {
            HoverSendBtn = !_sendBtnRect.IsEmpty && _sendBtnRect.Contains(x, y);
            HoverJoinCodeBtn = !_joinCodeBtnRect.IsEmpty && _joinCodeBtnRect.Contains(x, y);
            HoverRefreshBtn = !_refreshBtnRect.IsEmpty && _refreshBtnRect.Contains(x, y);
            HoverOpenFolderBtn = !_openFolderBtnRect.IsEmpty && _openFolderBtnRect.Contains(x, y);
            HoverInboxTab = !_inboxTabRect.IsEmpty && _inboxTabRect.Contains(x, y);
            HoverSentTab = !_sentTabRect.IsEmpty && _sentTabRect.Contains(x, y);
            HoverCollabTab = !_collabTabRect.IsEmpty && _collabTabRect.Contains(x, y);
            HoverLoginBtn = !_loginCtaBtnRect.IsEmpty && _loginCtaBtnRect.Contains(x, y);

            HoverRowIndex = -1;
            HoverRowBtnType = -1;
            if (ActiveSubTab == 2)
            {
                for (int i = 0; i < _roomHitBoxes.Count; i++)
                {
                    var r = _roomHitBoxes[i];
                    if (!r.PrimaryBtn.IsEmpty && r.PrimaryBtn.Contains(x, y)) { HoverRowIndex = i; HoverRowBtnType = 1; break; }
                    if (!r.CopyBtn.IsEmpty && r.CopyBtn.Contains(x, y)) { HoverRowIndex = i; HoverRowBtnType = 2; break; }
                    if (!r.LeaveBtn.IsEmpty && r.LeaveBtn.Contains(x, y)) { HoverRowIndex = i; HoverRowBtnType = 3; break; }
                    if (r.Card.Contains(x, y)) { HoverRowIndex = i; HoverRowBtnType = 0; break; }
                }
                return;
            }
            for (int i = 0; i < _rowHitBoxes.Count; i++)
            {
                var r = _rowHitBoxes[i];
                if (!r.PrimaryBtn.IsEmpty && r.PrimaryBtn.Contains(x, y))
                {
                    HoverRowIndex = i;
                    HoverRowBtnType = 1;
                    break;
                }
                if (!r.SecondaryBtn.IsEmpty && r.SecondaryBtn.Contains(x, y))
                {
                    HoverRowIndex = i;
                    HoverRowBtnType = 2;
                    break;
                }
                if (r.Card.Contains(x, y))
                {
                    HoverRowIndex = i;
                    HoverRowBtnType = 0;
                    break;
                }
            }
        }

        public OfficeAppMode? HandleClick(int x, int y, out string docTitle, out string openFilePath)
        {
            docTitle = null;
            openFilePath = null;

            if (!_loginCtaBtnRect.IsEmpty && _loginCtaBtnRect.Contains(x, y))
            {
                using (var authDlg = new AuthDialogForm())
                {
                    authDlg.ShowDialog(AppForm.Instance);
                }
                _ = CloudShareService.RefreshAndAutoDownloadAsync();
                AppForm.Instance?.Invalidate();
                return null;
            }

            if (!_inboxTabRect.IsEmpty && _inboxTabRect.Contains(x, y))
            {
                ActiveSubTab = 0;
                ScrollY = 0f;
                return null;
            }

            if (!_sentTabRect.IsEmpty && _sentTabRect.Contains(x, y))
            {
                ActiveSubTab = 1;
                ScrollY = 0f;
                return null;
            }

            if (!_collabTabRect.IsEmpty && _collabTabRect.Contains(x, y))
            {
                ActiveSubTab = 2;
                ScrollY = 0f;
                _ = CollabRoomService.RefreshMyRoomsAsync();
                return null;
            }

            if (!_refreshBtnRect.IsEmpty && _refreshBtnRect.Contains(x, y))
            {
                _ = CloudShareService.RefreshAndAutoDownloadAsync();
                _ = CollabRoomService.RefreshMyRoomsAsync();
                return null;
            }

            if (!_joinCodeBtnRect.IsEmpty && _joinCodeBtnRect.Contains(x, y))
            {
                PromptJoinRoomByCode();
                return null;
            }

            if (!_openFolderBtnRect.IsEmpty && _openFolderBtnRect.Contains(x, y))
            {
                try
                {
                    Process.Start("explorer.exe", CloudShareService.GetReceivedFilesDir());
                }
                catch { }
                return null;
            }

            if (!_sendBtnRect.IsEmpty && _sendBtnRect.Contains(x, y))
            {
                if (!DatabaseAuthService.IsLoggedIn)
                {
                    using (var authDlg = new AuthDialogForm())
                    {
                        authDlg.ShowDialog(AppForm.Instance);
                    }
                    if (!DatabaseAuthService.IsLoggedIn) return null;
                }

                if (CloudShareService.SenderUsedBytes >= CloudShareService.CloudQuotaBytes)
                {
                    FluentMessageBox.Show(
                        AppL10n.T(
                            "Ổ đĩa Cloud chia sẻ 200 MB của bạn đã đầy! Hãy thu hồi / xóa bớt tệp trong tab 'Đã gửi' hoặc chờ tự động dọn sau 30 ngày.",
                            "Your 200 MB Cloud Share quota is full! Delete an older item in 'Sent' or wait for the 30-day auto-cleanup."),
                        AppL10n.T("Đầy dung lượng Cloud", "Cloud Quota Full"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                using (var dlg = new CloudShareDialogForm())
                {
                    dlg.ShowDialog(AppForm.Instance);
                }
                AppForm.Instance?.Invalidate();
                return null;
            }

            if (ActiveSubTab == 2)
            {
                for (int i = 0; i < _roomHitBoxes.Count; i++)
                {
                    var r = _roomHitBoxes[i];
                    if (!r.PrimaryBtn.IsEmpty && r.PrimaryBtn.Contains(x, y))
                    {
                        AppForm.Instance?.OpenCollabRoomInTab(r.Room.Id);
                        return null;
                    }
                    if (!r.CopyBtn.IsEmpty && r.CopyBtn.Contains(x, y))
                    {
                        try { Clipboard.SetText(r.Room.RoomCode); } catch { }
                        AppForm.Instance?.ShowCollabToastInfo(string.Format(AppL10n.T("Đã sao chép mã phòng: {0}", "Copied room code: {0}"), r.Room.RoomCode));
                        return null;
                    }
                    if (!r.LeaveBtn.IsEmpty && r.LeaveBtn.Contains(x, y))
                    {
                        bool isOwner = DatabaseAuthService.CurrentUser != null && r.Room.OwnerId == DatabaseAuthService.CurrentUser.Id;
                        string qMsg = isOwner
                            ? string.Format(AppL10n.T("Xóa hẳn phòng \"{0}\" cho tất cả mọi người?", "Delete room \"{0}\" for everyone?"), r.Room.Title)
                            : string.Format(AppL10n.T("Rời khỏi phòng cộng tác \"{0}\"?", "Leave room \"{0}\"?"), r.Room.Title);
                        if (FluentMessageBox.Show(qMsg, AppL10n.T("Xác nhận", "Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _ = CollabRoomService.LeaveOrDeleteRoomAsync(r.Room.Id);
                        }
                        return null;
                    }
                    if (r.Card.Contains(x, y))
                    {
                        AppForm.Instance?.OpenCollabRoomInTab(r.Room.Id);
                        return null;
                    }
                }
                return null;
            }

            for (int i = 0; i < _rowHitBoxes.Count; i++)
            {
                var entry = _rowHitBoxes[i];
                if (!entry.PrimaryBtn.IsEmpty && entry.PrimaryBtn.Contains(x, y))
                {
                    if (entry.IsSent)
                    {
                        var confirm = FluentMessageBox.Show(
                            string.Format(
                                AppL10n.T("Gỡ tệp \"{0}\" khỏi Cloud?\nThao tác này giải phóng {1} dung lượng gửi của bạn.",
                                    "Remove \"{0}\" from Cloud?\nThis frees {1} of your sharing storage."),
                                entry.Item.FileName, CloudShareService.FormatSize(entry.Item.FileSize)),
                            AppL10n.T("Xóa tệp đã gửi", "Delete Sent File"),
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm == DialogResult.Yes)
                        {
                            _ = CloudShareService.DeleteSentShareAsync(entry.Item.Id);
                        }
                        return null;
                    }
                    else
                    {
                        return OpenOrDownloadItem(entry.Item, out docTitle, out openFilePath);
                    }
                }

                if (!entry.SecondaryBtn.IsEmpty && entry.SecondaryBtn.Contains(x, y))
                {
                    if (!entry.IsSent && entry.Item.IsDownloadedLocally)
                    {
                        try
                        {
                            Process.Start("explorer.exe", "/select,\"" + entry.Item.LocalFilePath + "\"");
                        }
                        catch { }
                    }
                    return null;
                }

                if (entry.Card.Contains(x, y) && !entry.IsSent)
                {
                    return OpenOrDownloadItem(entry.Item, out docTitle, out openFilePath);
                }
            }

            return null;
        }

        private OfficeAppMode? OpenOrDownloadItem(CloudShareItem item, out string docTitle, out string openFilePath)
        {
            docTitle = null;
            openFilePath = null;
            if (item == null) return null;

            if (item.IsDownloadedLocally)
            {
                openFilePath = item.LocalFilePath;
                docTitle = item.FileName;
                return CloudShareService.ResolveDocMode(item.FileName, item.DocMode);
            }

            // Trigger on-demand download if local file was deleted or pending
            Task.Run(async () =>
            {
                try
                {
                    string saved = await CloudShareService.ManualDownloadShareAsync(item).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(saved) && AppForm.Instance != null && !AppForm.Instance.IsDisposed)
                    {
                        AppForm.Instance.BeginInvoke((Action)(() =>
                        {
                            AppForm.Instance.OpenSharedFileInTab(saved, item.FileName, CloudShareService.ResolveDocMode(item.FileName, item.DocMode));
                        }));
                    }
                }
                catch { }
            });
            return null;
        }

        public void Draw(Graphics g, float x, float y, float w, float h, float scrollY, string searchText = "")
        {
            ScrollY = scrollY;
            _rowHitBoxes.Clear();
            _roomHitBoxes.Clear();
            _sendBtnRect = RectangleF.Empty;
            _joinCodeBtnRect = RectangleF.Empty;
            _refreshBtnRect = RectangleF.Empty;
            _openFolderBtnRect = RectangleF.Empty;
            _inboxTabRect = RectangleF.Empty;
            _sentTabRect = RectangleF.Empty;
            _collabTabRect = RectangleF.Empty;
            _loginCtaBtnRect = RectangleF.Empty;

            // Top summary / quota banner
            float cardH = 104f;
            RectangleF headerCard = new RectangleF(x, y, w, cardH);
            using (GraphicsPath hp = CreateRoundedRect(headerCard, 14f))
            using (SolidBrush hbg = new SolidBrush(Color.White))
            using (Pen hbd = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillPath(hbg, hp);
                g.DrawPath(hbd, hp);
            }

            using (Font titleFont = new Font("Segoe UI", 14.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush subB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString(AppL10n.T("Được chia sẻ", "Shared"), titleFont, titleB, x + 20f, y + 14f);
                g.DrawString(AppL10n.T("Tự động tải về máy • Lưu giữ trong 30 ngày", "Auto-downloaded on arrival • Retained for 30 days"), subFont, subB, x + 20f, y + 40f);
            }

            // Action buttons on top-right of header card
            float btnY = y + 14f;
            float sendW = 122f;
            _sendBtnRect = new RectangleF(x + w - 20f - sendW, btnY, sendW, 34f);
            bool quotaFull = CloudShareService.SenderUsedBytes >= CloudShareService.CloudQuotaBytes;
            Color sendBgCol = HoverSendBtn ? Color.FromArgb(29, 78, 216) : (quotaFull ? Color.FromArgb(148, 163, 184) : Color.FromArgb(37, 99, 235));
            using (GraphicsPath sp = CreateRoundedRect(_sendBtnRect, 8f))
            using (SolidBrush sb = new SolidBrush(sendBgCol))
            using (Font bf = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush bt = new SolidBrush(Color.White))
            {
                g.FillPath(sb, sp);
                g.DrawString(AppL10n.T("Chia sẻ tệp", "Share File"), bf, bt, _sendBtnRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            float folderW = 112f;
            _openFolderBtnRect = new RectangleF(_sendBtnRect.X - 8f - folderW, btnY, folderW, 34f);
            using (GraphicsPath fp = CreateRoundedRect(_openFolderBtnRect, 8f))
            using (SolidBrush fb = new SolidBrush(HoverOpenFolderBtn ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)))
            using (Pen fpen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (Font ff = new Font("Segoe UI", 8.6f, FontStyle.Regular))
            using (SolidBrush ft = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.FillPath(fb, fp);
                g.DrawPath(fpen, fp);
                g.DrawString(AppL10n.T("Thư mục", "Folder"), ff, ft, _openFolderBtnRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            float refW = 36f;
            _refreshBtnRect = new RectangleF(_openFolderBtnRect.X - 8f - refW, btnY, refW, 34f);
            using (GraphicsPath rp = CreateRoundedRect(_refreshBtnRect, 8f))
            using (SolidBrush rb = new SolidBrush(HoverRefreshBtn ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)))
            using (Pen rpen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillPath(rb, rp);
                g.DrawPath(rpen, rp);
                FluentIconRenderer.DrawGlyph(g, '\uE72C', _refreshBtnRect, Color.FromArgb(71, 85, 105), 14f);
            }

            float joinW = 122f;
            _joinCodeBtnRect = new RectangleF(_refreshBtnRect.X - 8f - joinW, btnY, joinW, 34f);
            using (GraphicsPath jp = CreateRoundedRect(_joinCodeBtnRect, 8f))
            using (SolidBrush jb = new SolidBrush(HoverJoinCodeBtn ? Color.FromArgb(209, 250, 229) : Color.FromArgb(236, 253, 245)))
            using (Pen jpen = new Pen(Color.FromArgb(16, 185, 129), 1.1f))
            using (Font jf = new Font("Segoe UI", 8.6f, FontStyle.Bold))
            using (SolidBrush jt = new SolidBrush(Color.FromArgb(4, 120, 87)))
            {
                g.FillPath(jb, jp);
                g.DrawPath(jpen, jp);
                g.DrawString(AppL10n.T("Vào mã phòng", "Join Code"), jf, jt, _joinCodeBtnRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // 200 MB Sender Cloud Quota bar inside bottom of header card
            long used = DatabaseAuthService.IsLoggedIn ? CloudShareService.SenderUsedBytes : 0L;
            double ratio = Math.Min(1.0, Math.Max(0.0, (double)used / CloudShareService.CloudQuotaBytes));
            float barX = x + 20f;
            float barY = y + 76f;
            float barW = Math.Max(140f, w - 40f);
            using (GraphicsPath tp = CreateRoundedRect(new RectangleF(barX, barY, barW, 8f), 4f))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                g.FillPath(tb, tp);
            }
            if (ratio > 0)
            {
                float fillW = Math.Max(8f, (float)(barW * ratio));
                Color qCol = ratio >= 0.99 ? Color.FromArgb(220, 38, 38) : (ratio >= 0.80 ? Color.FromArgb(217, 119, 6) : Color.FromArgb(37, 99, 235));
                using (GraphicsPath qp = CreateRoundedRect(new RectangleF(barX, barY, fillW, 8f), 4f))
                using (SolidBrush qb = new SolidBrush(qCol))
                {
                    g.FillPath(qb, qp);
                }
            }
            using (Font qFont = new Font("Segoe UI", 8.2f, ratio >= 0.99 ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush qBrush = new SolidBrush(ratio >= 0.99 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(71, 85, 105)))
            {
                string qTxt = DatabaseAuthService.IsLoggedIn
                    ? string.Format(AppL10n.T("Dung lượng Cloud gửi đi: {0} / 200 MB ({1:0.#}%)", "Cloud Share Storage: {0} / 200 MB ({1:0.#}%)"), CloudShareService.FormatSize(used), ratio * 100.0)
                    : AppL10n.T("Đăng nhập để sử dụng 200 MB Cloud chia sẻ", "Sign in to use 200 MB Cloud Share");
                g.DrawString(qTxt, qFont, qBrush, barX, barY - 17f);
            }

            float subTabY = y + cardH + 14f;
            if (!DatabaseAuthService.IsLoggedIn)
            {
                RectangleF guestCard = new RectangleF(x, subTabY, w, 200f);
                using (GraphicsPath gp = CreateRoundedRect(guestCard, 14f))
                using (SolidBrush gb = new SolidBrush(Color.White))
                using (Pen gpen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillPath(gb, gp);
                    g.DrawPath(gpen, gp);
                }
                using (Font gf = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font gsf = new Font("Segoe UI", 9.2f))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(AppL10n.T("Đăng nhập tài khoản ZenQ", "Sign in to ZenQ"), gf, tb, new RectangleF(x + 20f, subTabY + 28f, w - 40f, 26f), sfCenter);
                    g.DrawString(AppL10n.T("Đăng nhập để gửi và tự động nhận tệp từ đồng nghiệp.\nLưu giữ 30 ngày • Tải về không trừ dung lượng người nhận.", "Sign in to send and receive shared files.\nRetained for 30 days • Zero storage cost to recipients."), gsf, sb, new RectangleF(x + 24f, subTabY + 62f, w - 48f, 44f), sfCenter);
                }
                float lw = 190f;
                _loginCtaBtnRect = new RectangleF(x + (w - lw) / 2f, subTabY + 122f, lw, 38f);
                using (GraphicsPath lp = CreateRoundedRect(_loginCtaBtnRect, 8f))
                using (SolidBrush lb = new SolidBrush(HoverLoginBtn ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)))
                using (Font lf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush lt = new SolidBrush(Color.White))
                {
                    g.FillPath(lb, lp);
                    g.DrawString(AppL10n.T("Đăng nhập / Đăng ký", "Sign In / Sign Up"), lf, lt, _loginCtaBtnRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
                return;
            }

            // Sub-tab pills: Đã nhận | Đã gửi | Cộng tác trực tuyến
            int inCount = CloudShareService.InboxItems?.Count ?? 0;
            int outCount = CloudShareService.SentItems?.Count ?? 0;
            int roomCount = CollabRoomService.MyRooms?.Count ?? 0;
            _inboxTabRect = new RectangleF(x, subTabY, 146f, 34f);
            _sentTabRect = new RectangleF(x + 154f, subTabY, 136f, 34f);
            _collabTabRect = new RectangleF(x + 298f, subTabY, 182f, 34f);
            DrawSubTabPill(g, _inboxTabRect, string.Format(AppL10n.T("Đã nhận ({0})", "Received ({0})"), inCount), ActiveSubTab == 0, HoverInboxTab);
            DrawSubTabPill(g, _sentTabRect, string.Format(AppL10n.T("Đã gửi ({0})", "Sent ({0})"), outCount), ActiveSubTab == 1, HoverSentTab);
            DrawSubTabPill(g, _collabTabRect, string.Format(AppL10n.T("Cộng tác ({0})", "Live Rooms ({0})"), roomCount), ActiveSubTab == 2, HoverCollabTab);

            if (CloudShareService.IsRefreshing)
            {
                using Font rf = new Font("Segoe UI", 8.5f, FontStyle.Italic);
                using SolidBrush rb = new SolidBrush(Color.FromArgb(37, 99, 235));
                g.DrawString(AppL10n.T("Đang đồng bộ...", "Syncing..."), rf, rb, _collabTabRect.Right + 14f, subTabY + 8f);
            }
            else if (!string.IsNullOrEmpty(CloudShareService.LastSyncError))
            {
                using Font ef = new Font("Segoe UI", 8.2f);
                using SolidBrush eb = new SolidBrush(Color.FromArgb(185, 28, 28));
                g.DrawString(AppL10n.T("Không thể đồng bộ: ", "Sync unavailable: ") + CloudShareService.LastSyncError, ef, eb, new RectangleF(_collabTabRect.Right + 14f, subTabY + 8f, Math.Max(60f, w - 500f), 20f), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            }

            // Scrollable list viewport
            float listTop = subTabY + 46f;
            float vpH = Math.Max(120f, (y + h) - listTop);
            RectangleF vp = new RectangleF(x - 2f, listTop, w + 4f, vpH);
            var listClip = g.Save();
            g.SetClip(vp, CombineMode.Intersect);
            try
            {
                if (ActiveSubTab == 2)
                {
                    DrawCollabRoomsList(g, x, listTop, w, y + h, scrollY, searchText);
                    return;
                }
                var items = GetFilteredItems(searchText);
                if (items.Count == 0)
                {
                    using Font empF = new Font("Segoe UI", 10f);
                    using SolidBrush empB = new SolidBrush(Color.FromArgb(148, 163, 184));
                    string empTxt = ActiveSubTab == 0
                        ? AppL10n.T("Chưa có tệp nào được gửi tới bạn.", "No shared files received yet.")
                        : AppL10n.T("Bạn chưa gửi tệp nào trong 30 ngày gần đây.", "No files shared in the last 30 days.");
                    g.DrawString(empTxt, empF, empB, new RectangleF(x + 20f, listTop + 36f, w - 40f, 50f), new StringFormat { Alignment = StringAlignment.Center });
                    return;
                }

                bool isSent = (ActiveSubTab == 1);
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    float rowY = listTop + i * 82f - scrollY;
                    if (rowY + 74f < listTop || rowY > y + h) continue;

                    RectangleF rowRect = new RectangleF(x, rowY, w, 74f);
                    bool hovered = (HoverRowIndex == i);

                    using (GraphicsPath rp = CreateRoundedRect(rowRect, 11f))
                    using (SolidBrush rbg = new SolidBrush(hovered ? Color.FromArgb(248, 250, 252) : Color.White))
                    using (Pen rbd = new Pen(hovered ? Color.FromArgb(147, 197, 253) : Color.FromArgb(226, 232, 240), 1f))
                    {
                        g.FillPath(rbg, rp);
                        g.DrawPath(rbd, rp);
                    }

                    // File mode icon
                    OfficeAppMode mode = CloudShareService.ResolveDocMode(item.FileName, item.DocMode);
                    AppIconRenderer.DrawAppIcon(g, rowRect.X + 14f, rowRect.Y + 17f, 40f, mode);

                    // Action buttons on right
                    RectangleF secBtn = RectangleF.Empty;
                    RectangleF priBtn;
                    if (isSent)
                    {
                        priBtn = new RectangleF(rowRect.Right - 108f, rowRect.Y + 21f, 94f, 32f);
                    }
                    else
                    {
                        if (item.IsDownloadedLocally)
                        {
                            secBtn = new RectangleF(rowRect.Right - 102f, rowRect.Y + 21f, 88f, 32f);
                            priBtn = new RectangleF(secBtn.X - 96f, rowRect.Y + 21f, 88f, 32f);
                        }
                        else
                        {
                            priBtn = new RectangleF(rowRect.Right - 108f, rowRect.Y + 21f, 94f, 32f);
                        }
                    }

                    // Title + metadata
                    float textW = Math.Max(120f, priBtn.X - (rowRect.X + 66f) - 12f);
                    using (Font nameF = new Font("Segoe UI", 10f, FontStyle.Bold))
                    using (Font metaF = new Font("Segoe UI", 8.3f))
                    using (SolidBrush nameB = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (SolidBrush metaB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(item.FileName, nameF, nameB, new RectangleF(rowRect.X + 66f, rowRect.Y + 10f, textW, 20f),
                            new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                        string peer = isSent
                            ? string.Format(AppL10n.T("Gửi tới @{0}", "To @{0}"), item.RecipientUsername)
                            : string.Format(AppL10n.T("Từ @{0}", "From @{0}"), item.SenderUsername);

                        string statusPart;
                        if (CloudShareService.DownloadProgress.TryGetValue(item.Id, out int pct))
                            statusPart = string.Format(AppL10n.T("⏳ Đang tải {0}%", "⏳ Downloading {0}%"), pct);
                        else if (isSent)
                            statusPart = item.DownloadedAt.HasValue ? AppL10n.T("✓ Người nhận đã tải", "✓ Downloaded by recipient") : AppL10n.T("⏳ Chờ người nhận online", "⏳ Waiting recipient");
                        else
                            statusPart = item.IsDownloadedLocally ? AppL10n.T("✓ Đã tự tải về máy", "✓ Saved on PC") : AppL10n.T("☁ Có sẵn trên Cloud", "☁ On Cloud");

                        string line2 = string.Format("{0}  •  {1}  •  {2}  •  {3}",
                            peer, CloudShareService.FormatSize(item.FileSize), statusPart,
                            string.Format(AppL10n.T("Còn {0} ngày", "{0}d left"), item.RemainingDays));
                        g.DrawString(line2, metaF, metaB, new RectangleF(rowRect.X + 66f, rowRect.Y + 32f, textW, 17f),
                            new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                        if (!string.IsNullOrWhiteSpace(item.Note))
                        {
                            using SolidBrush noteB = new SolidBrush(Color.FromArgb(71, 85, 105));
                            g.DrawString("“" + item.Note + "”", metaF, noteB, new RectangleF(rowRect.X + 66f, rowRect.Y + 50f, textW, 17f),
                                new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                        }
                    }

                    // Draw action buttons
                    if (isSent)
                    {
                        bool hPri = hovered && HoverRowBtnType == 1;
                        using (GraphicsPath pp = CreateRoundedRect(priBtn, 7f))
                        using (SolidBrush pb = new SolidBrush(hPri ? Color.FromArgb(254, 226, 226) : Color.FromArgb(254, 242, 242)))
                        using (Pen ppen = new Pen(Color.FromArgb(252, 165, 165), 1f))
                        using (Font pf = new Font("Segoe UI", 8.4f, FontStyle.Bold))
                        using (SolidBrush pt = new SolidBrush(Color.FromArgb(185, 28, 28)))
                        {
                            g.FillPath(pb, pp);
                            g.DrawPath(ppen, pp);
                            g.DrawString(AppL10n.T("Xóa Cloud", "Delete"), pf, pt, priBtn, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                        }
                    }
                    else
                    {
                        bool hPri = hovered && HoverRowBtnType == 1;
                        string priLabel = item.IsDownloadedLocally ? AppL10n.T("Mở tệp", "Open") : AppL10n.T("Tải về", "Download");
                        using (GraphicsPath pp = CreateRoundedRect(priBtn, 7f))
                        using (SolidBrush pb = new SolidBrush(hPri ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)))
                        using (Font pf = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        using (SolidBrush pt = new SolidBrush(Color.White))
                        {
                            g.FillPath(pb, pp);
                            g.DrawString(priLabel, pf, pt, priBtn, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                        }
                        if (!secBtn.IsEmpty)
                        {
                            bool hSec = hovered && HoverRowBtnType == 2;
                            using (GraphicsPath sp2 = CreateRoundedRect(secBtn, 7f))
                            using (SolidBrush sb2 = new SolidBrush(hSec ? Color.FromArgb(241, 245, 249) : Color.White))
                            using (Pen spen2 = new Pen(Color.FromArgb(203, 213, 225), 1f))
                            using (Font sf2 = new Font("Segoe UI", 8.3f))
                            using (SolidBrush st2 = new SolidBrush(Color.FromArgb(51, 65, 85)))
                            {
                                g.FillPath(sb2, sp2);
                                g.DrawPath(spen2, sp2);
                                g.DrawString(AppL10n.T("Thư mục", "Folder"), sf2, st2, secBtn, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                            }
                        }
                    }

                    _rowHitBoxes.Add((rowRect, priBtn, secBtn, item, isSent));
                }
            }
            finally
            {
                g.Restore(listClip);
            }
        }

        private static void PromptJoinRoomByCode()
        {
            if (!DatabaseAuthService.IsLoggedIn)
            {
                using (var auth = new AuthDialogForm()) auth.ShowDialog(AppForm.Instance);
                if (!DatabaseAuthService.IsLoggedIn) return;
            }

            using Form dlg = new Form
            {
                Text = AppL10n.T("Tham gia phòng cộng tác", "Join Collaboration Room"),
                Size = new Size(390, 175),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            Label lbl = new Label
            {
                Text = AppL10n.T("Nhập mã phòng (ví dụ: ZQ-A1B2C3):", "Enter room code (e.g. ZQ-A1B2C3):"),
                Location = new Point(18, 18),
                Size = new Size(340, 22),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            TextBox txt = new TextBox
            {
                Location = new Point(18, 46),
                Size = new Size(338, 28),
                CharacterCasing = CharacterCasing.Upper,
                PlaceholderText = "ZQ-XXXXXX"
            };
            Button btnOk = new Button
            {
                Text = AppL10n.T("Vào cộng tác", "Join & Edit"),
                Location = new Point(236, 92),
                Size = new Size(120, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += async (s, e) =>
            {
                btnOk.Enabled = false;
                var res = await CollabRoomService.JoinByRoomCodeAsync(txt.Text);
                if (res.Success && res.Room != null)
                {
                    dlg.Close();
                    AppForm.Instance?.OpenCollabRoomInTab(res.Room.Id);
                }
                else
                {
                    btnOk.Enabled = true;
                    FluentMessageBox.Show(res.Message, AppL10n.T("Không thể tham gia", "Cannot Join"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            dlg.Controls.Add(lbl);
            dlg.Controls.Add(txt);
            dlg.Controls.Add(btnOk);
            dlg.AcceptButton = btnOk;
            dlg.ShowDialog(AppForm.Instance);
        }

        private void DrawCollabRoomsList(Graphics g, float x, float listTop, float w, float bottomY, float scrollY, string searchText)
        {
            var rooms = GetFilteredRooms(searchText);
            if (rooms.Count == 0)
            {
                using Font empF = new Font("Segoe UI", 10f);
                using SolidBrush empB = new SolidBrush(Color.FromArgb(148, 163, 184));
                g.DrawString(
                    AppL10n.T("Chưa có phòng cộng tác trực tuyến nào.\nMở tài liệu bất kỳ rồi bấm 'Chia sẻ' → 'Cộng tác trực tuyến' để mời mọi người sửa chung.", "No live collaboration rooms yet.\nOpen any document and click 'Share' → 'Live Co-Edit'."),
                    empF, empB, new RectangleF(x + 20f, listTop + 34f, w - 40f, 54f), new StringFormat { Alignment = StringAlignment.Center });
                return;
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                float rowY = listTop + i * 82f - scrollY;
                if (rowY + 74f < listTop || rowY > bottomY) continue;

                RectangleF rowRect = new RectangleF(x, rowY, w, 74f);
                bool hovered = (HoverRowIndex == i);

                using (GraphicsPath rp = CreateRoundedRect(rowRect, 11f))
                using (SolidBrush rbg = new SolidBrush(hovered ? Color.FromArgb(248, 250, 252) : Color.White))
                using (Pen rbd = new Pen(hovered ? Color.FromArgb(16, 185, 129) : Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillPath(rbg, rp);
                    g.DrawPath(rbd, rp);
                }

                OfficeAppMode mode = CloudShareService.ResolveDocMode(room.Title, room.DocMode);
                AppIconRenderer.DrawAppIcon(g, rowRect.X + 14f, rowRect.Y + 17f, 40f, mode);

                RectangleF leaveBtn = new RectangleF(rowRect.Right - 72f, rowRect.Y + 21f, 58f, 32f);
                RectangleF copyBtn = new RectangleF(leaveBtn.X - 96f, rowRect.Y + 21f, 88f, 32f);
                RectangleF priBtn = new RectangleF(copyBtn.X - 116f, rowRect.Y + 21f, 108f, 32f);

                float textW = Math.Max(110f, priBtn.X - (rowRect.X + 66f) - 12f);
                using (Font nameF = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (Font metaF = new Font("Segoe UI", 8.3f))
                using (SolidBrush nameB = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (SolidBrush metaB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString(room.Title, nameF, nameB, new RectangleF(rowRect.X + 66f, rowY + 10f, textW, 20f),
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                    string onlinePart = room.OnlineCount > 0
                        ? string.Format(AppL10n.T("● {0} đang sửa", "● {0} active"), room.OnlineCount)
                        : AppL10n.T("○ Sẵn sàng", "○ Ready");
                    string memNames = room.Members != null && room.Members.Count > 0
                        ? string.Join(", ", room.Members.Take(4).Select(m => "@" + m.Username))
                        : "@" + room.OwnerUsername;

                    string line2 = $"{room.RoomCode}  •  {onlinePart}  •  {memNames}";
                    g.DrawString(line2, metaF, metaB, new RectangleF(rowRect.X + 66f, rowY + 32f, textW, 17f),
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                    string line3 = string.Format(
                        AppL10n.T("Chủ phòng: @{0} (độc lập khi thoát)  •  Lưu gần nhất: {1}", "Host: @{0} (stays editable)  •  Saved: {1}"),
                        room.OwnerUsername, room.UpdatedAt.ToLocalTime().ToString("HH:mm dd/MM"));
                    g.DrawString(line3, metaF, new SolidBrush(Color.FromArgb(71, 85, 105)), new RectangleF(rowRect.X + 66f, rowY + 50f, textW, 17f),
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                }

                bool hPri = hovered && HoverRowBtnType == 1;
                using (GraphicsPath pp = CreateRoundedRect(priBtn, 7f))
                using (SolidBrush pb = new SolidBrush(hPri ? Color.FromArgb(4, 120, 87) : Color.FromArgb(5, 150, 105)))
                using (Font pf = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (SolidBrush pt = new SolidBrush(Color.White))
                {
                    g.FillPath(pb, pp);
                    g.DrawString(AppL10n.T("Mở & Sửa", "Open Live"), pf, pt, priBtn, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }

                bool hCopy = hovered && HoverRowBtnType == 2;
                using (GraphicsPath cp = CreateRoundedRect(copyBtn, 7f))
                using (SolidBrush cb = new SolidBrush(hCopy ? Color.FromArgb(241, 245, 249) : Color.White))
                using (Pen cpen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                using (Font cf = new Font("Segoe UI", 8.2f))
                using (SolidBrush ct = new SolidBrush(Color.FromArgb(51, 65, 85)))
                {
                    g.FillPath(cb, cp);
                    g.DrawPath(cpen, cp);
                    g.DrawString(AppL10n.T("Chép mã", "Copy Code"), cf, ct, copyBtn, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }

                bool isOwner = DatabaseAuthService.CurrentUser != null && room.OwnerId == DatabaseAuthService.CurrentUser.Id;
                bool hLeave = hovered && HoverRowBtnType == 3;
                using (GraphicsPath lp = CreateRoundedRect(leaveBtn, 7f))
                using (SolidBrush lb = new SolidBrush(hLeave ? Color.FromArgb(254, 226, 226) : Color.FromArgb(254, 242, 242)))
                using (Pen lpen = new Pen(Color.FromArgb(252, 165, 165), 1f))
                using (Font lf = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                using (SolidBrush lt = new SolidBrush(Color.FromArgb(185, 28, 28)))
                {
                    g.FillPath(lb, lp);
                    g.DrawPath(lpen, lp);
                    g.DrawString(isOwner ? AppL10n.T("Xóa", "Del") : AppL10n.T("Rời", "Leave"), lf, lt, leaveBtn, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }

                _roomHitBoxes.Add((rowRect, priBtn, copyBtn, leaveBtn, room));
            }
        }

        private static void DrawSubTabPill(Graphics g, RectangleF rect, string text, bool active, bool hovered)
        {
            Color bg = active ? Color.FromArgb(239, 246, 255) : (hovered ? Color.FromArgb(241, 245, 249) : Color.White);
            Color bd = active ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240);
            Color fg = active ? Color.FromArgb(29, 78, 216) : Color.FromArgb(71, 85, 105);

            using (GraphicsPath p = CreateRoundedRect(rect, 8f))
            using (SolidBrush b = new SolidBrush(bg))
            using (Pen pen = new Pen(bd, active ? 1.4f : 1f))
            using (Font f = new Font("Segoe UI", 9f, active ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(fg))
            {
                g.FillPath(b, p);
                g.DrawPath(pen, p);
                g.DrawString(text, f, tb, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        private static GraphicsPath CreateRoundedRect(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > bounds.Width) d = bounds.Width;
            if (d > bounds.Height) d = bounds.Height;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
