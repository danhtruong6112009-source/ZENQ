using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public partial class SlidesView
    {
        Rectangle catalogViewport, catalogTrack, catalogThumb;
        int catalogContentHeight;
        bool catalogDragging;
        int catalogDragOffset;
        TextBox catalogSearchBox;
        string utilityCategory = "Tất cả";
        string utilityStatus = "";
        bool _extensionCatalogRequested = false;

        static string CatalogKey(string value)
        {
            var normalized = (value ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c == 'đ' ? 'd' : c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public bool ScrollElementsCatalog(int delta)
        {
            if (!IsElementsDrawerOpen || ElementsDrawerSelectedTab == 1) return false;
            if (catalogDragging) return true;
            SetCatalogScroll(ElementsScrollY - delta / 120f * 36, true);
            InvalidateCallback?.Invoke();
            return true;
        }

        void SetCatalogScroll(float value, bool immediate)
        {
            TargetElementsScrollY = Math.Clamp(value, 0, Math.Max(0, catalogContentHeight - catalogViewport.Height));
            if (immediate)
            {
                ElementsScrollY = (int)TargetElementsScrollY;
                SmoothElementsScrollY = TargetElementsScrollY;
            }
        }

        void ResetCatalogScroll()
        {
            if (catalogDragging && ParentControl != null) ParentControl.Capture = false;
            ElementsScrollY = 0;
            SmoothElementsScrollY = TargetElementsScrollY = 0;
            catalogDragging = false;
        }

        bool BeginCatalogScroll(Point point)
        {
            if (!catalogTrack.Contains(point)) return false;
            catalogDragging = true;
            if (ParentControl != null) ParentControl.Capture = true;
            catalogDragOffset = catalogThumb.Contains(point) ? point.Y - catalogThumb.Y : catalogThumb.Height / 2;
            MoveCatalogScroll(point);
            return true;
        }

        void MoveCatalogScroll(Point point)
        {
            float range = Math.Max(1, catalogTrack.Height - catalogThumb.Height);
            int previous = ElementsScrollY;
            SetCatalogScroll((point.Y - catalogTrack.Y - catalogDragOffset) / range * Math.Max(0, catalogContentHeight - catalogViewport.Height), true);
            if (previous != ElementsScrollY) InvalidateCallback?.Invoke();
        }

        void PaintCatalogScrollbar(D2DGraphics g)
        {
            SetCatalogScroll(TargetElementsScrollY, false);
            catalogTrack = catalogThumb = Rectangle.Empty;
            if (catalogContentHeight <= catalogViewport.Height) return;
            catalogTrack = new Rectangle(catalogViewport.Right - 8, catalogViewport.Y + 3, 5, catalogViewport.Height - 6);
            int h = Math.Min(catalogTrack.Height, Math.Max(28, catalogTrack.Height * catalogViewport.Height / Math.Max(1, catalogContentHeight)));
            catalogThumb = new Rectangle(catalogTrack.X, catalogTrack.Y + (int)(ElementsScrollY / (float)Math.Max(1, catalogContentHeight - catalogViewport.Height) * (catalogTrack.Height - h)), 5, h);
            using var brush = new SolidBrush(Color.FromArgb(180, 189, 202));
            g.FillRoundedRectangle(brush, catalogThumb, 2.5f);
        }

        void UpdateCatalogSearch()
        {
            if (ParentControl == null) return;
            bool show = IsElementsDrawerOpen && !IsElementsDrawerCollapsed && !IsPresenting && ElementsDrawerSelectedTab != 1 && !IsImageBlenderActive && Math.Abs(_leftSidebarAnimW - 280f) <= 4f;
            if (ParentControl is AppForm app && (app.BackstageView?.IsOpen == true || app.Ribbon?.CurrentMode != OfficeAppMode.Slides)) show = false;
            if (!show)
            {
                if (catalogSearchBox != null) catalogSearchBox.Visible = false;
                return;
            }
            if (catalogSearchBox == null)
            {
                catalogSearchBox = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(248, 250, 252),
                    ForeColor = Color.FromArgb(38, 48, 65),
                    Font = new Font("Segoe UI", 9.5f),
                    PlaceholderText = AppL10n.T("Tìm tiện ích...", "Search add-ins...")
                };
                catalogSearchBox.TextChanged += (s, e) =>
                {
                    ElementsSearchQuery = catalogSearchBox.Text;
                    ResetCatalogScroll();
                    InvalidateCallback?.Invoke();
                };
                catalogSearchBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        catalogSearchBox.Clear();
                        ParentControl.Focus();
                        e.SuppressKeyPress = true;
                    }
                };
                catalogSearchBox.MouseWheel += (s, e) =>
                {
                    ScrollElementsCatalog(e.Delta);
                    if (e is HandledMouseEventArgs handled) handled.Handled = true;
                };
                ParentControl.Controls.Add(catalogSearchBox);
            }
            if (_shapeSearchBox != null) _shapeSearchBox.Visible = false;
            var bounds = new Rectangle(TxtSearchBoxBounds.X + 27, TxtSearchBoxBounds.Y + 7, Math.Max(20, TxtSearchBoxBounds.Width - 37), 20);
            if (catalogSearchBox.Bounds != bounds) catalogSearchBox.Bounds = bounds;
            if (!catalogSearchBox.Visible)
            {
                catalogSearchBox.Visible = true;
                catalogSearchBox.BringToFront();
            }
        }

        bool CatalogMatch(string value) => CatalogKey(value).Contains(CatalogKey(ElementsSearchQuery));

        void CatalogText(D2DGraphics g, string value, Font font, Color color, RectangleF bounds, bool center = false)
        {
            using var brush = new SolidBrush(color);
            using var format = new StringFormat
            {
                Alignment = center ? StringAlignment.Center : StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            var state = g.Save();
            try
            {
                g.IntersectClip(bounds);
                g.DrawString(value, font, brush, bounds, format);
            }
            finally
            {
                g.Restore(state);
            }
        }

        void CatalogHit(Rectangle rect, string action)
        {
            if (rect.IntersectsWith(catalogViewport))
            {
                DrawerItemBounds.Add(rect);
                DrawerItemActions.Add(action);
            }
        }

        void DrawUtilityCatalog(D2DGraphics g, int left, int top, int width, int height)
        {
            if (IsImageBlenderActive)
            {
                DrawImageBlenderTaskPane(g, left, top, width, height);
                return;
            }

            int y = top - ElementsScrollY;
            using var title = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var subHead = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var label = new Font("Segoe UI", 8.5f);
            using var miniFont = new Font("Segoe UI", 8f);

            if (ElementsDrawerSelectedTab == 7)
            {
                if (!_extensionCatalogRequested)
                {
                    _extensionCatalogRequested = true;
                    _ = Task.Run(async () =>
                    {
                        await ZenQExtensionManager.RefreshCatalogAsync();
                        ParentControl?.BeginInvoke(new Action(() => InvalidateCallback?.Invoke()));
                    });
                }

                int pad = 10;
                int availW = Math.Max(120, width - pad * 2);

                if (!string.IsNullOrEmpty(utilityStatus))
                {
                    CatalogText(g, utilityStatus, label, Color.FromArgb(99, 62, 192), new RectangleF(left + pad, y, availW, 24));
                    y += 28;
                }

                var currentUser = DatabaseAuthService.CurrentUser;
                bool isCreatorOrAdmin = currentUser != null && (currentUser.IsCreator || currentUser.IsAdmin) && !currentUser.Ban;

                // Nút thao tác gọn gàng, vừa khít 100% trong khung
                int btnH = 32;
                if (isCreatorOrAdmin)
                {
                    int gap = 6;
                    int btnW = (availW - gap) / 2;

                    // Nút Đăng tiện ích (bỏ dấu ngoặc đơn, ghi ngắn gọn)
                    var btnPublish = new Rectangle(left + pad, y, btnW, btnH);
                    using (var pubGrad = new LinearGradientBrush(btnPublish, Color.FromArgb(99, 102, 241), Color.FromArgb(139, 92, 246), 45f))
                    {
                        g.FillRoundedRectangle(pubGrad, btnPublish, 6);
                    }
                    CatalogText(g, AppL10n.T("✨ Đăng tiện ích", "✨ Publish Add-in"), subHead, Color.White, btnPublish, true);
                    CatalogHit(btnPublish, "ext:publish");

                    // Nút Cài từ tệp (bỏ dấu ngoặc đơn, ghi ngắn gọn)
                    var btnInstallFile = new Rectangle(left + pad + btnW + gap, y, btnW, btnH);
                    using (var insFill = new SolidBrush(Color.FromArgb(243, 244, 246)))
                    using (var insBorder = new Pen(Color.FromArgb(209, 213, 219), 1f))
                    {
                        g.FillRoundedRectangle(insFill, btnInstallFile, 6);
                        g.DrawRoundedRectangle(insBorder, btnInstallFile, 6);
                    }
                    CatalogText(g, AppL10n.T("📥 Cài từ tệp", "📥 From File"), label, Color.FromArgb(55, 65, 81), btnInstallFile, true);
                    CatalogHit(btnInstallFile, "ext:install_file");
                }
                else
                {
                    // Người dùng thường: 1 nút Cài từ tệp chiếm trọn chiều rộng
                    var btnInstallFile = new Rectangle(left + pad, y, availW, btnH);
                    using (var insFill = new SolidBrush(Color.FromArgb(243, 244, 246)))
                    using (var insBorder = new Pen(Color.FromArgb(209, 213, 219), 1f))
                    {
                        g.FillRoundedRectangle(insFill, btnInstallFile, 6);
                        g.DrawRoundedRectangle(insBorder, btnInstallFile, 6);
                    }
                    CatalogText(g, AppL10n.T("📥 Cài đặt từ tệp tiện ích", "📥 Install Add-in from File"), label, Color.FromArgb(55, 65, 81), btnInstallFile, true);
                    CatalogHit(btnInstallFile, "ext:install_file");
                }
                y += 40;

                // Bộ lọc danh mục ngắn gọn: Tất cả, Đã cài, Kho online
                string[] categories = { "Tất cả", "Đã cài", "Kho online" };
                string[] categoryLabels = { AppL10n.T("Tất cả", "All"), AppL10n.T("Đã cài", "Installed"), AppL10n.T("Kho online", "Online") };
                int chipX = left + pad;
                int chipGap = 5;
                int[] chipWidths = { 56, 62, 76 };

                for (int c = 0; c < categories.Length; c++)
                {
                    string cat = categories[c];
                    int cW = chipWidths[c];
                    var chip = new Rectangle(chipX, y, cW, 25);
                    bool isSelected = (cat == utilityCategory) ||
                                      (cat == "Đã cài" && utilityCategory == "Đã cài đặt") ||
                                      (cat == "Kho online" && (utilityCategory == "Kho trực tuyến" || utilityCategory == "Kho online"));

                    using var fill = new SolidBrush(isSelected ? Color.FromArgb(238, 233, 255) : Color.FromArgb(245, 247, 250));
                    using var border = new Pen(isSelected ? Color.FromArgb(167, 139, 250) : Color.FromArgb(226, 232, 240), 1f);
                    g.FillRoundedRectangle(fill, chip, 12);
                    g.DrawRoundedRectangle(border, chip, 12);

                    CatalogText(g, categoryLabels[c], label, isSelected ? Color.FromArgb(99, 62, 192) : Color.FromArgb(88, 99, 115), chip, true);
                    CatalogHit(chip, "appfilter:" + cat);
                    chipX += cW + chipGap;
                }
                y += 36;

                // Lấy danh sách tiện ích
                var allExts = ZenQExtensionManager.GetAllExtensions();
                IEnumerable<ZenQExtensionItem> filtered = allExts;

                if (utilityCategory == "Đã cài" || utilityCategory == "Đã cài đặt")
                {
                    filtered = filtered.Where(x => x.isInstalled);
                }
                else if (utilityCategory == "Kho online" || utilityCategory == "Kho trực tuyến")
                {
                    filtered = filtered.Where(x => !string.IsNullOrEmpty(x.downloadUrl));
                }

                if (!string.IsNullOrWhiteSpace(ElementsSearchQuery))
                {
                    filtered = filtered.Where(x => CatalogMatch((x.name ?? "") + " " + (x.description ?? "") + " " + (x.category ?? "") + " " + (x.authorName ?? "")));
                }

                var list = filtered.ToList();

                if (list.Count == 0)
                {
                    // Khung Empty State ngắn gọn, không tràn chữ, không chứa dấu ngoặc ()
                    var emptyBox = new Rectangle(left + pad, y + 6, availW, 130);
                    using (var emptyBg = new SolidBrush(Color.FromArgb(250, 250, 252)))
                    using (var emptyBorder = new Pen(Color.FromArgb(226, 232, 240), 1.2f) { DashStyle = DashStyle.Dash })
                    {
                        g.FillRoundedRectangle(emptyBg, emptyBox, 8);
                        g.DrawRoundedRectangle(emptyBorder, emptyBox, 8);
                    }

                    int textY = emptyBox.Y + 22;
                    if (utilityCategory == "Đã cài" || utilityCategory == "Đã cài đặt")
                    {
                        CatalogText(g, AppL10n.T("Chưa cài tiện ích nào", "No add-ins installed"), title, Color.FromArgb(30, 41, 59), new RectangleF(emptyBox.X + 8, textY, emptyBox.Width - 16, 20), true);
                        textY += 26;
                        CatalogText(g, AppL10n.T("Hãy cài đặt từ tệp trên máy", "Install from a local file"), label, Color.FromArgb(100, 116, 139), new RectangleF(emptyBox.X + 8, textY, emptyBox.Width - 16, 18), true);
                        textY += 20;
                        CatalogText(g, AppL10n.T("hoặc khám phá Kho online.", "or browse the online store."), label, Color.FromArgb(100, 116, 139), new RectangleF(emptyBox.X + 8, textY, emptyBox.Width - 16, 18), true);
                    }
                    else
                    {
                        CatalogText(g, AppL10n.T("Kho online đang trống", "Online store is empty"), title, Color.FromArgb(30, 41, 59), new RectangleF(emptyBox.X + 8, textY, emptyBox.Width - 16, 20), true);
                        textY += 26;
                        CatalogText(g, AppL10n.T("Tiện ích mới sẽ hiện ở đây.", "New add-ins will appear here."), label, Color.FromArgb(100, 116, 139), new RectangleF(emptyBox.X + 8, textY, emptyBox.Width - 16, 18), true);
                        textY += 22;
                        if (isCreatorOrAdmin)
                        {
                            CatalogText(g, AppL10n.T("Nhấn 'Đăng tiện ích' để tải lên.", "Click 'Publish Add-in' to upload."), miniFont, Color.FromArgb(99, 102, 241), new RectangleF(emptyBox.X + 8, textY, emptyBox.Width - 16, 18), true);
                        }
                    }
                    y += 148;
                }
                else
                {
                    foreach (var ext in list)
                    {
                        var card = new Rectangle(left + pad, y, availW, 94);
                        if (card.IntersectsWith(catalogViewport))
                        {
                            bool isHover = card.Contains(CurrentMouseCanvasPos);
                            using var fill = new SolidBrush(isHover ? Color.FromArgb(249, 250, 252) : Color.White);
                            using var border = new Pen(isHover ? Color.FromArgb(165, 180, 252) : Color.FromArgb(226, 232, 240), 1.2f);
                            g.FillRoundedRectangle(fill, card, 8);
                            g.DrawRoundedRectangle(border, card, 8);

                            // Icon mảnh ghép tiện ích
                            var iconBox = new Rectangle(card.X + 8, card.Y + 10, 36, 36);
                            using (var iconBg = new SolidBrush(Color.FromArgb(238, 233, 255)))
                            {
                                g.FillRoundedRectangle(iconBg, iconBox, 6);
                            }
                            CatalogText(g, "🧩", new Font("Segoe UI Emoji", 15f), Color.FromArgb(99, 102, 241), iconBox, true);

                            // Tên + Version
                            int textLeft = card.X + 50;
                            int textW = card.Width - 56;
                            CatalogText(g, ext.name, subHead, Color.FromArgb(30, 41, 59), new RectangleF(textLeft, card.Y + 8, textW - 48, 18));

                            using (var vBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
                            using (var vBg = new SolidBrush(Color.FromArgb(238, 233, 255)))
                            {
                                var vRect = new Rectangle(card.Right - 46, card.Y + 8, 40, 16);
                                g.FillRoundedRectangle(vBg, vRect, 3);
                                CatalogText(g, "v" + ext.version, miniFont, Color.FromArgb(99, 102, 241), vRect, true);
                            }

                            // Tác giả & Danh mục
                            string authorTxt = $"👤 {ext.authorName} • {ext.category}";
                            CatalogText(g, authorTxt, miniFont, Color.FromArgb(79, 70, 229), new RectangleF(textLeft, card.Y + 28, textW, 16));

                            // Mô tả ngắn gọn
                            string descTxt = string.IsNullOrWhiteSpace(ext.description) ? AppL10n.T("Tiện ích mở rộng ZenQ Office.", "ZenQ Office extension.") : ext.description;
                            CatalogText(g, descTxt, miniFont, Color.FromArgb(100, 116, 139), new RectangleF(textLeft, card.Y + 44, textW, 16));

                            // Nút thao tác
                            if (!ext.isInstalled)
                            {
                                var btnAct = new Rectangle(textLeft, card.Y + 64, 68, 22);
                                using var bFill = new SolidBrush(Color.FromArgb(238, 233, 255));
                                g.FillRoundedRectangle(bFill, btnAct, 4);
                                CatalogText(g, AppL10n.T("📥 Cài đặt", "📥 Install"), miniFont, Color.FromArgb(99, 62, 192), btnAct, true);
                                CatalogHit(btnAct, "ext:install:" + ext.id);
                            }
                            else
                            {
                                // Nút Mở tiện ích
                                var btnOpen = new Rectangle(textLeft, card.Y + 64, 56, 22);
                                using var oFill = new SolidBrush(Color.FromArgb(245, 243, 255));
                                using var oBorder = new Pen(Color.FromArgb(167, 139, 250), 1f);
                                g.FillRoundedRectangle(oFill, btnOpen, 4);
                                g.DrawRoundedRectangle(oBorder, btnOpen, 4);
                                CatalogText(g, AppL10n.T("▶ Mở", "▶ Open"), miniFont, Color.FromArgb(109, 40, 217), btnOpen, true);
                                string openAct = string.IsNullOrEmpty(ext.actionId) ? ("ext:run:" + ext.id) : ext.actionId;
                                CatalogHit(btnOpen, openAct);

                                // Nút Gỡ
                                var btnUnins = new Rectangle(textLeft + 62, card.Y + 64, 62, 22);
                                using var uFill = new SolidBrush(Color.FromArgb(243, 244, 246));
                                g.FillRoundedRectangle(uFill, btnUnins, 4);
                                CatalogText(g, AppL10n.T("Gỡ", "Remove"), miniFont, Color.FromArgb(107, 114, 128), btnUnins, true);
                                CatalogHit(btnUnins, "ext:uninstall:" + ext.id);

                                // Cho phép click vào phần thân thẻ để Mở tiện ích
                                var cardClick = new Rectangle(card.X, card.Y, card.Width, 60);
                                CatalogHit(cardClick, openAct);
                            }

                            // Nút Xóa (Dành cho Creator tạo tiện ích đó hoặc Admin)
                            bool canDelete = currentUser != null && (currentUser.IsAdmin || (ext.authorId > 0 && ext.authorId == currentUser.Id));
                            if (canDelete)
                            {
                                var btnDel = new Rectangle(card.Right - 56, card.Y + 64, 50, 22);
                                using var dFill = new SolidBrush(Color.FromArgb(254, 242, 242));
                                using var dBorder = new Pen(Color.FromArgb(254, 202, 202), 1f);
                                g.FillRoundedRectangle(dFill, btnDel, 4);
                                g.DrawRoundedRectangle(dBorder, btnDel, 4);
                                CatalogText(g, AppL10n.T("🗑 Xóa", "🗑 Del"), miniFont, Color.FromArgb(220, 38, 38), btnDel, true);
                                CatalogHit(btnDel, "ext:delete_cloud:" + ext.id);
                            }
                        }
                        y += 102;
                    }
                }
            }
            else
            {
                // Tab 2: Phông chữ & Văn bản
                var import = new Rectangle(left + 12, y + 2, width - 28, 34);
                using (var fill = new SolidBrush(Color.FromArgb(242, 238, 255)))
                    g.FillRoundedRectangle(fill, import, 7);
                CatalogText(g, AppL10n.T("+ Nhập phông chữ", "+ Import Font"), label, Color.FromArgb(99, 62, 192), import, true);
                CatalogHit(import, "font:import");
                y += 50;

                CatalogText(g, AppL10n.T("Chọn phông chữ", "Choose Font"), title, Color.FromArgb(32, 42, 60), new RectangleF(left + 14, y, width - 28, 24));
                y += 32;

                var fonts = (SlideFontManager.GetCustomFontNames() ?? new List<string>())
                    .Concat(new[] { "Segoe UI", "Arial", "Calibri", "Times New Roman", "Cambria", "Georgia" })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(name => !name.Contains("Icons", StringComparison.OrdinalIgnoreCase) && !name.Contains("MDL2", StringComparison.OrdinalIgnoreCase) && !name.Contains("Wingdings", StringComparison.OrdinalIgnoreCase) && CatalogMatch(name))
                    .ToList();

                foreach (string name in fonts)
                {
                    var card = new Rectangle(left + 12, y, width - 28, 68);
                    if (card.IntersectsWith(catalogViewport))
                    {
                        using var fill = new SolidBrush(SelectedShape?.FontName == name ? Color.FromArgb(246, 242, 255) : Color.White);
                        using var border = new Pen(SelectedShape?.FontName == name ? Color.FromArgb(167, 139, 250) : Color.FromArgb(230, 234, 240));
                        g.FillRoundedRectangle(fill, card, 8);
                        g.DrawRoundedRectangle(border, card, 8);
                        CatalogText(g, name, label, Color.FromArgb(54, 65, 84), new RectangleF(card.X + 12, card.Y + 5, card.Width - 24, 21));
                        using var sample = SlideFontManager.CreateFont(name, 17, FontStyle.Regular, GraphicsUnit.Pixel);
                        CatalogText(g, AppL10n.T("Cùng tạo điều tuyệt vời", "Create something amazing"), sample, Color.FromArgb(82, 93, 111), new RectangleF(card.X + 12, card.Y + 29, card.Width - 24, 29));
                        CatalogHit(card, "font:apply:" + name);
                    }
                    y += 76;
                }

                if (fonts.Count == 0)
                {
                    CatalogText(g, AppL10n.T("Không có phông chữ phù hợp.", "No matching fonts found."), label, Color.SlateGray, new RectangleF(left + 16, y, width - 32, 40));
                    y += 48;
                }

                CatalogText(g, AppL10n.T("Thêm văn bản", "Add Text"), title, Color.FromArgb(32, 42, 60), new RectangleF(left + 14, y, width - 28, 24));
                y += 32;

                foreach (var item in new[] { (AppL10n.T("Tiêu đề", "Add a heading"), "text:title"), (AppL10n.T("Tiêu đề phụ", "Add a subheading"), "text:subtitle"), (AppL10n.T("Nội dung", "Add body text"), "text:body") })
                {
                    var rect = new Rectangle(left + 12, y, width - 28, 38);
                    using var fill = new SolidBrush(Color.FromArgb(246, 248, 251));
                    g.FillRoundedRectangle(fill, rect, 6);
                    CatalogText(g, item.Item1, label, Color.FromArgb(46, 55, 73), new RectangleF(rect.X + 12, rect.Y, rect.Width - 24, rect.Height));
                    CatalogHit(rect, item.Item2);
                    y += 46;
                }
            }

            catalogContentHeight = Math.Max(0, y + ElementsScrollY - top + 14);
        }

        bool ExecuteUtility(string action, Control parent)
        {
            if (action == "blender:exit")
            {
                ExitImageBlender(false);
                return true;
            }

            if (action == "blender:apply")
            {
                ExitImageBlender(true);
                return true;
            }

            if (action == "blender:reset")
            {
                ResetImageBlenderSettings();
                return true;
            }

            if (action == "blender:mode:linear")
            {
                CurrentBlenderFadeMode = BlenderFadeMode.Linear;
                ApplyBlenderMask();
                InvalidateCallback?.Invoke();
                return true;
            }

            if (action == "blender:mode:radial")
            {
                CurrentBlenderFadeMode = BlenderFadeMode.Radial;
                ApplyBlenderMask();
                InvalidateCallback?.Invoke();
                return true;
            }

            if (action == "blender:slider")
            {
                IsDraggingBlenderSlider = true;
                if (ParentControl != null) ParentControl.Capture = true;
                Point pt = ParentControl != null ? ParentControl.PointToClient(Cursor.Position) : CurrentMouseCanvasPos;
                UpdateBlenderSliderFromMouse(pt);
                return true;
            }

            if (action == "blender:open" || action == "ext:run:zenq.image.blender")
            {
                OpenImageBlender();
                return true;
            }

            if (action.StartsWith("ext:run:"))
            {
                string id = action.Substring(8);
                var ext = ZenQExtensionManager.GetAllExtensions().FirstOrDefault(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (ext != null && !string.IsNullOrEmpty(ext.actionId))
                {
                    return ExecuteUtility(ext.actionId, parent);
                }
                return true;
            }

            if (action.StartsWith("appfilter:"))
            {
                utilityCategory = action.Substring(10);
                ResetCatalogScroll();
                return true;
            }

            if (action == "ext:publish")
            {
                var user = DatabaseAuthService.CurrentUser;
                if (user == null)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T("Vui lòng đăng nhập tài khoản trước khi đăng tải tiện ích.", "Please sign in before publishing an add-in."), AppL10n.T("Yêu cầu đăng nhập", "Sign In Required"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                if (user.Ban)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T("Tài khoản của bạn đã bị khóa tính năng. Không thể đăng tải tiện ích.", "This account feature is disabled. Cannot publish add-ins."), AppL10n.T("Tài khoản bị khóa", "Account Restricted"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }
                if (!user.IsCreator && !user.IsAdmin)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T("Bạn cần vai trò Nhà sáng tạo (Creator) hoặc Quản trị viên (Admin) để đăng tải tiện ích mở rộng.", "You need the Creator or Admin role to publish add-ins."), AppL10n.T("Chưa đủ quyền", "Insufficient Permissions"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }
                using (var dlg = new ExtensionPublishDialogForm())
                {
                    if (dlg.ShowDialog(parent) == DialogResult.OK)
                    {
                        utilityStatus = AppL10n.T("Đã đăng tải tiện ích mới thành công!", "Add-in published successfully!");
                        utilityCategory = "Kho online";
                        ResetCatalogScroll();
                        InvalidateCallback?.Invoke();
                    }
                }
                return true;
            }

            if (action == "ext:install_file")
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = AppL10n.T("Gói tiện ích ZenQ (*.zqe)|*.zqe|Tệp Zip (*.zip)|*.zip|Tất cả tệp (*.*)|*.*", "ZenQ Add-in Package (*.zqe)|*.zqe|Zip Archive (*.zip)|*.zip|All Files (*.*)|*.*");
                    ofd.Title = AppL10n.T("Cài đặt tiện ích mở rộng từ máy tính", "Install Add-in from Computer");
                    if (ofd.ShowDialog(parent) == DialogResult.OK)
                    {
                        _ = Task.Run(async () =>
                        {
                            var (ok, err, installed) = await ZenQExtensionManager.InstallFromZqeFileAsync(ofd.FileName);
                            if (parent != null && !parent.IsDisposed)
                            {
                                parent.BeginInvoke(new Action(() =>
                                {
                                    if (ok)
                                    {
                                        ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T($"Đã cài đặt thành công tiện ích '{installed?.name}'!", $"Successfully installed add-in '{installed?.name}'!"), AppL10n.T("Cài đặt thành công", "Installed"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        utilityStatus = AppL10n.T($"Đã cài: {installed?.name}", $"Installed: {installed?.name}");
                                        utilityCategory = "Đã cài";
                                    }
                                    else
                                    {
                                        ZenQOffice.UI.FluentMessageBox.Show(parent, err, AppL10n.T("Lỗi cài đặt", "Install Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    InvalidateCallback?.Invoke();
                                }));
                            }
                        });
                    }
                }
                return true;
            }

            if (action.StartsWith("ext:install:"))
            {
                string extId = action.Substring(12);
                var ext = ZenQExtensionManager.GetAllExtensions().FirstOrDefault(x => string.Equals(x.id, extId, StringComparison.OrdinalIgnoreCase));
                if (ext != null)
                {
                    utilityStatus = AppL10n.T($"Đang tải và cài '{ext.name}'...", $"Downloading and installing '{ext.name}'...");
                    InvalidateCallback?.Invoke();
                    _ = Task.Run(async () =>
                    {
                        var (ok, err) = await ZenQExtensionManager.InstallExtensionAsync(ext);
                        if (parent != null && !parent.IsDisposed)
                        {
                            parent.BeginInvoke(new Action(() =>
                            {
                                if (ok)
                                {
                                    utilityStatus = AppL10n.T($"Đã cài thành công: {ext.name}", $"Installed: {ext.name}");
                                    ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T($"Đã cài đặt thành công tiện ích '{ext.name}'!", $"Successfully installed '{ext.name}'!"), AppL10n.T("Cài đặt hoàn tất", "Installation Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    utilityStatus = AppL10n.T("Lỗi cài đặt: ", "Install error: ") + err;
                                    ZenQOffice.UI.FluentMessageBox.Show(parent, err, AppL10n.T("Lỗi cài đặt", "Install Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                InvalidateCallback?.Invoke();
                            }));
                        }
                    });
                }
                return true;
            }

            if (action.StartsWith("ext:uninstall:"))
            {
                string extId = action.Substring(14);
                var (ok, err) = ZenQExtensionManager.UninstallExtension(extId);
                if (ok)
                {
                    utilityStatus = AppL10n.T("Đã gỡ cài đặt tiện ích.", "Add-in removed.");
                    ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T("Đã gỡ cài đặt tiện ích thành công.", "Add-in uninstalled successfully."), AppL10n.T("Gỡ cài đặt", "Uninstall"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    utilityStatus = AppL10n.T("Lỗi: ", "Error: ") + err;
                }
                InvalidateCallback?.Invoke();
                return true;
            }

            if (action.StartsWith("ext:delete_cloud:"))
            {
                string extId = action.Substring(17);
                var ext = ZenQExtensionManager.GetAllExtensions().FirstOrDefault(x => string.Equals(x.id, extId, StringComparison.OrdinalIgnoreCase));
                if (ext != null)
                {
                    var user = DatabaseAuthService.CurrentUser;
                    if (user == null || (!user.IsAdmin && ext.authorId != user.Id))
                    {
                        ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T("Bạn chỉ có quyền xóa tiện ích do chính mình đăng tải.", "You can only delete add-ins you published."), AppL10n.T("Không có quyền", "Permission Denied"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return true;
                    }
                    if (ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T($"Bạn có chắc chắn muốn xóa vĩnh viễn tiện ích '{ext.name}' khỏi Kho online không?\n(Gói tệp tiện ích trên GitHub sẽ bị xóa)", $"Permanently delete add-in '{ext.name}' from the online store?\n(The add-in package on GitHub will be deleted)"), AppL10n.T("Xác nhận xóa tiện ích", "Confirm Delete Add-in"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        utilityStatus = AppL10n.T($"Đang xóa tiện ích '{ext.name}'...", $"Deleting '{ext.name}'...");
                        InvalidateCallback?.Invoke();
                        _ = Task.Run(async () =>
                        {
                            var (ok, err) = await ZenQExtensionManager.DeleteExtensionFromCloudAsync(ext);
                            if (parent != null && !parent.IsDisposed)
                            {
                                parent.BeginInvoke(new Action(() =>
                                {
                                    if (ok)
                                    {
                                        utilityStatus = AppL10n.T($"Đã xóa '{ext.name}' khỏi Kho online.", $"Deleted '{ext.name}' from online store.");
                                        ZenQOffice.UI.FluentMessageBox.Show(parent, AppL10n.T($"Đã xóa tiện ích '{ext.name}' thành công khỏi Kho online!", $"Add-in '{ext.name}' deleted from online store!"), AppL10n.T("Xóa thành công", "Deleted"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        utilityStatus = AppL10n.T("Lỗi xóa: ", "Delete error: ") + err;
                                        ZenQOffice.UI.FluentMessageBox.Show(parent, err, AppL10n.T("Lỗi khi xóa", "Delete Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    InvalidateCallback?.Invoke();
                                }));
                            }
                        });
                    }
                }
                return true;
            }

            return false;
        }
    }
}
