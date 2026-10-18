using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ZenQOffice.Core;
using ZenQOffice;

namespace ZenQOffice.UI
{
    public class AppForm : Form
    {
        public static AppForm Instance { get; private set; }
        public RibbonBar Ribbon { get; set; }
        public HomeView HomeView { get; set; }
        public WriterView WriterView { get; set; }
        public CalcView CalcView { get; set; }
        public SlidesView SlidesView { get; set; }
        public PdfView PdfView { get; set; }
        public PhotoView PhotoView { get; set; }
        public WordSuggestionBox WordSuggestions = new WordSuggestionBox();
        public OfficeBackstageView BackstageView { get; set; }
        private AiConfigPanel _aiConfigPanel;
        public AiConfigPanel AiConfigPanel { get { return _aiConfigPanel; } }
        private D2DGraphics _appD2d;

        public void OpenBackstageView(int defaultTab = 0, OfficeAppMode? overrideMode = null)
        {
            if (BackstageView == null) BackstageView = new OfficeBackstageView(this);
            BackstageView.Open(defaultTab, overrideMode);
            Invalidate();
        }

        public void CloseBackstageView()
        {
            if (BackstageView != null) BackstageView.Close();
            Invalidate();
        }

        public void ToggleBackstageView()
        {
            if (BackstageView != null && BackstageView.IsOpen) CloseBackstageView();
            else OpenBackstageView();
        }

        public string StatusText { get; set; }
        public float ZoomLevel { get; set; }

        private Timer _cursorTimer;
        private Timer _animTimer;

        public void ResetCursorBlink()
        {
            if (WriterView != null) WriterView.CursorVisible = true;
            if (_cursorTimer != null)
            {
                _cursorTimer.Stop();
                _cursorTimer.Start();
            }
            Invalidate();
        }

        public void SwitchAppMode(OfficeAppMode mode)
        {
            if (Ribbon == null) return;
            string ext = ".docx";
            string prefix = "Văn bản ";
            if (mode == OfficeAppMode.Slides) { ext = ".pptx"; prefix = "Trình chiếu "; }
            else if (mode == OfficeAppMode.Calc) { ext = ".xlsx"; prefix = "Bảng tính "; }
            else if (mode == OfficeAppMode.Pdf) { ext = ".pdf"; prefix = "Tài liệu PDF "; }
            else if (mode == OfficeAppMode.Photo) { ext = ".svg"; prefix = "Thiết kế "; }
            int count = (Ribbon.DocTabs != null) ? Ribbon.DocTabs.Count : 1;
            Ribbon.AddDocTab(prefix + count + ext, mode, SlidesView, WriterView, CalcView, PdfView, PhotoView);
            SetModeIcon(mode);
            PositionAiConfigPanel();
            Invalidate();
        }

        public void OpenDirectMode(OfficeAppMode mode, string openFilePath = null)
        {
            Ribbon.StandaloneMode = true;
            Ribbon.DocTabs.Clear(); // Standalone: NO Home tab, NO multiple tabs!

            string docTitle = null;
            if (!string.IsNullOrEmpty(openFilePath) && System.IO.File.Exists(openFilePath))
            {
                docTitle = System.IO.Path.GetFileName(openFilePath);
            }
            else
            {
                string ext = ".docx";
                string prefix = "Văn bản 1";
                if (mode == OfficeAppMode.Slides) { ext = ".pptx"; prefix = "Trình chiếu 1"; }
                else if (mode == OfficeAppMode.Calc) { ext = ".xlsx"; prefix = "Bảng tính 1"; }
                else if (mode == OfficeAppMode.Pdf) { ext = ".pdf"; prefix = "Tài liệu PDF 1"; }
                else if (mode == OfficeAppMode.Photo) { ext = ".svg"; prefix = "Thiết kế 1"; }
                docTitle = prefix + ext;
            }

            DocTab newDocTab = new DocTab(docTitle, mode);
            if (!string.IsNullOrEmpty(openFilePath) && System.IO.File.Exists(openFilePath))
            {
                newDocTab.FilePath = openFilePath;
            }
            newDocTab.DraftFile = "";
            Ribbon.DocTabs.Add(newDocTab);
            Ribbon.ActiveDocIndex = 0;
            Ribbon.CurrentMode = mode;
            Ribbon.ActiveRibbonTab = 0;

            if (!string.IsNullOrEmpty(openFilePath) && System.IO.File.Exists(openFilePath))
            {
                try { BackupManager.BackupOriginalFile(openFilePath, mode.ToString()); } catch { }
                if (mode == OfficeAppMode.Writer && WriterView != null)
                {
                    WriterView.LoadDocumentFile(openFilePath, Ribbon);
                }
                else if (mode == OfficeAppMode.Slides && SlidesView != null)
                {
                    SlidesView.Presentation = ZenQOffice.Format.PptxHandler.LoadPptx(openFilePath);
                    SlidesView.ActiveSlideIndex = 0;
                    SlidesView.ZoomLevel = 1.0f;
                    SlidesView.CanvasScrollX = 0;
                    SlidesView.CanvasScrollY = 0;
                    SlidesView.SelectedShapeIndex = -1;
                }
                else if (mode == OfficeAppMode.Calc && CalcView != null)
                {
                    ZenQOffice.Format.XlsxHandler.LoadXlsxOrCsv(openFilePath, CalcView);
                    CalcView.ActiveSheetIndex = 0;
                    CalcView.ScrollRow = 1;
                    CalcView.ScrollCol = 1;
                    CalcView.SelectedRow = 1;
                    CalcView.SelectedCol = 1;
                }
                else if (mode == OfficeAppMode.Pdf && PdfView != null)
                {
                    PdfView.Document = ZenQOffice.Format.PdfHandler.LoadPdf(openFilePath);
                    PdfView.ActivePageIndex = 0;
                }
                else if (mode == OfficeAppMode.Photo && PhotoView != null)
                {
                    PhotoView.Document = PhotoDocument.LoadFromFile(openFilePath);
                    PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                }
            }

            UpdateStandaloneTitle();
            SetModeIcon(mode);
            ApplyWindowAppId();
            PositionAiConfigPanel();
            Invalidate();
        }

        public void UpdateStandaloneTitle()
        {
            if (Ribbon != null && Ribbon.StandaloneMode)
            {
                string modeStr = Ribbon.CurrentMode.ToString();
                if (Ribbon.CurrentMode == OfficeAppMode.Writer) modeStr = "ZenQ Writer";
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc) modeStr = "ZenQ Calc";
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides) modeStr = "ZenQ Slides";
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf) modeStr = "ZenQ PDF";
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo) modeStr = "ZenQ Vector Designer";

                string docTitle = (Ribbon.DocTabs != null && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title
                    : "";
                bool isMod = (Ribbon.DocTabs != null && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count && Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified);
                this.Text = (string.IsNullOrEmpty(docTitle) ? modeStr : (docTitle + (isMod ? " *" : "") + " - " + modeStr));
            }
        }

        public void PerformQuickSave()
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                string path = (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count) ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try { BackupManager.BackupOriginalFile(path, "Writer"); } catch { }
                    WriterView.SaveDocumentFile(path, Ribbon);
                    Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                    UpdateStandaloneTitle();
                    Invalidate();
                    MessageBox.Show("Đã lưu tài liệu thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    PerformSaveAs();
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                string path = (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count) ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try { BackupManager.BackupOriginalFile(path, "Slides"); } catch { }
                    ZenQOffice.Format.PptxHandler.SavePptx(path, SlidesView.Presentation);
                    Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                    UpdateStandaloneTitle();
                    Invalidate();
                    MessageBox.Show("Đã lưu bản trình chiếu thành công!", "ZenQ Slides", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    PerformSaveAs();
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                string path = (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count) ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try { BackupManager.BackupOriginalFile(path, "Calc"); } catch { }
                    ZenQOffice.Format.XlsxHandler.SaveXlsxOrCsv(path, CalcView);
                    Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                    UpdateStandaloneTitle();
                    Invalidate();
                    MessageBox.Show("Đã lưu bảng tính thành công!", "ZENQ OFFICE Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    PerformSaveAs();
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                string path = (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count) ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try { BackupManager.BackupOriginalFile(path, "Pdf"); } catch { }
                    ZenQOffice.Format.PdfHandler.SavePdf(PdfView.Document, path);
                    Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                    UpdateStandaloneTitle();
                    Invalidate();
                    MessageBox.Show("Đã lưu tệp PDF thành công!", "ZENQ OFFICE PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    PerformSaveAs();
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
            {
                string path = (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count) ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try { BackupManager.BackupOriginalFile(path, "Photo"); } catch { }
                    PhotoView.Document?.SaveToFile(path);
                    Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                    UpdateStandaloneTitle();
                    Invalidate();
                    MessageBox.Show("Đã lưu thiết kế thành công!", "Thiết kế & Ảnh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    PerformSaveAs();
                }
            }
            Invalidate();
        }

        public void PerformQuickOpen()
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Mở tài liệu ZenQ Writer";
                    ofd.Filter = "Tài liệu Word (*.docx)|*.docx|Văn bản OpenOffice Writer (*.odt)|*.odt|Tệp Rich Text (*.rtf)|*.rtf|Trang Web HTML (*.html)|*.html|Tệp Văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try { BackupManager.BackupOriginalFile(ofd.FileName, "Writer"); } catch { }
                        WriterView.LoadDocumentFile(ofd.FileName, Ribbon);
                        string fileName = System.IO.Path.GetFileName(ofd.FileName);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        UpdateStandaloneTitle();
                        Invalidate();
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Mở bảng tính ZenQ Calc";
                    ofd.Filter = "Tất cả bảng tính (*.xlsx;*.xls;*.ods;*.csv;*.tsv)|*.xlsx;*.xls;*.ods;*.csv;*.tsv|Tệp Excel (*.xlsx)|*.xlsx|Tệp Excel 97-2003 (*.xls)|*.xls|Bảng tính OpenDocument (*.ods)|*.ods|Tệp CSV (*.csv)|*.csv|Tệp TSV (*.tsv)|*.tsv|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try { BackupManager.BackupOriginalFile(ofd.FileName, "Calc"); } catch { }
                        LoadSpreadsheetDocument(ofd.FileName);
                        string fileName = System.IO.Path.GetFileName(ofd.FileName);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        UpdateStandaloneTitle();
                        Invalidate();
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Mở bản trình chiếu ZenQ Slides";
                    ofd.Filter = "Tệp PowerPoint (*.pptx;*.ppt)|*.pptx;*.ppt|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try { BackupManager.BackupOriginalFile(ofd.FileName, "Slides"); } catch { }
                        SlidesView.Presentation = ZenQOffice.Format.PptxHandler.LoadPptx(ofd.FileName);
                        string fileName = System.IO.Path.GetFileName(ofd.FileName);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        UpdateStandaloneTitle();
                        Invalidate();
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Mở tệp PDF";
                    ofd.Filter = "Tệp PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try { BackupManager.BackupOriginalFile(ofd.FileName, "Pdf"); } catch { }
                        PdfView.Document = ZenQOffice.Format.PdfHandler.LoadPdf(ofd.FileName);
                        string fileName = System.IO.Path.GetFileName(ofd.FileName);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        UpdateStandaloneTitle();
                        Invalidate();
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Mở tệp thiết kế và hình ảnh";
                    ofd.Filter = "Tệp Đồ họa Vector (*.svg)|*.svg|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try { BackupManager.BackupOriginalFile(ofd.FileName, "Photo"); } catch { }
                        var doc = PhotoDocument.LoadFromFile(ofd.FileName);
                        if (doc != null)
                        {
                            PhotoView.Document = doc;
                            PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                            string fileName = System.IO.Path.GetFileName(ofd.FileName);
                            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                            {
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fileName;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].PhotoData = doc;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                            }
                            UpdateStandaloneTitle();
                            Invalidate();
                        }
                    }
                }
            }
        }

        public void PerformQuickUndo()
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
            {
                WriterView.Undo();
                WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon);
                WriterView.UpdateRibbonState(Ribbon);
                MarkActiveTabModified();
                ResetCursorBlink();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                SlidesView.Undo();
                MarkActiveTabModified();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.Undo();
                MarkActiveTabModified();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.Undo();
                MarkActiveTabModified();
            }
            Invalidate();
        }

        public void PerformQuickRedo()
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
            {
                WriterView.Redo();
                WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon);
                WriterView.UpdateRibbonState(Ribbon);
                MarkActiveTabModified();
                ResetCursorBlink();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                SlidesView.Redo();
                MarkActiveTabModified();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.Redo();
                MarkActiveTabModified();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.Redo();
                MarkActiveTabModified();
            }
            Invalidate();
        }

        public void SetModeIcon(OfficeAppMode mode)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // CHỈ đổi icon ứng dụng con khi ở chế độ Độc lập (StandaloneMode mở từ Shortcut Desktop)
                if (Ribbon != null && Ribbon.StandaloneMode)
                {
                    string iconName = "icon\\writer.ico";
                    if (mode == OfficeAppMode.Calc) iconName = "icon\\calc.ico";
                    else if (mode == OfficeAppMode.Slides) iconName = "icon\\present.ico";
                    else if (mode == OfficeAppMode.Pdf) iconName = "icon\\pdf.ico";
                    else if (mode == OfficeAppMode.Writer) iconName = "icon\\writer.ico";
                    else if (mode == OfficeAppMode.Photo) iconName = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "icon\\vector.ico")) ? "icon\\vector.ico" : "icon\\photo.ico";

                    string iconPath = System.IO.Path.Combine(baseDir, iconName);
                    if (System.IO.File.Exists(iconPath))
                    {
                        this.Icon = new Icon(iconPath);
                        return;
                    }
                }

                // Ứng dụng mẹ GenOffice (Hub đầy đủ tab): LUÔN LUÔN giữ nguyên Icon chính thức ZenQOffice.ico
                string hubIconPath = System.IO.Path.Combine(baseDir, "ZenQOffice.ico");
                if (System.IO.File.Exists(hubIconPath))
                {
                    this.Icon = new Icon(hubIconPath);
                }
                else
                {
                    var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
                    if (stream != null) this.Icon = new Icon(stream);
                    else this.Icon = GenOfficeLogoRenderer.CreateAppIcon(32);
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, out IWindowPropertyStore propertyStore);

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        private interface IWindowPropertyStore
        {
            uint GetCount();
            void GetAt(uint iProp, out WinPropKey pkey);
            void GetValue(ref WinPropKey key, out WinPropVar pv);
            void SetValue(ref WinPropKey key, ref WinPropVar pv);
            void Commit();
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 4)]
        private struct WinPropKey
        {
            public Guid fmtid;
            public uint pid;
            public WinPropKey(Guid guid, uint id) { fmtid = guid; pid = id; }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct WinPropVar
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public ushort vt;
            [System.Runtime.InteropServices.FieldOffset(8)] public IntPtr pwszVal;

            public static WinPropVar FromString(string val)
            {
                WinPropVar pv = new WinPropVar();
                pv.vt = 31; // VT_LPWSTR
                pv.pwszVal = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni(val);
                return pv;
            }

            public void Clear()
            {
                PropVariantClear(ref this);
            }

            [System.Runtime.InteropServices.DllImport("ole32.dll")]
            private static extern int PropVariantClear(ref WinPropVar pvar);
        }

        public void ApplyWindowAppId()
        {
            if (!IsHandleCreated) return;
            try
            {
                string appId = "ZenQOffice.Suite.Hub";
                if (Ribbon != null && Ribbon.StandaloneMode)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer) appId = "ZenQOffice.Suite.Writer";
                    else if (Ribbon.CurrentMode == OfficeAppMode.Calc) appId = "ZenQOffice.Suite.Calc";
                    else if (Ribbon.CurrentMode == OfficeAppMode.Slides) appId = "ZenQOffice.Suite.Slides";
                    else if (Ribbon.CurrentMode == OfficeAppMode.Pdf) appId = "ZenQOffice.Suite.Pdf";
                }

                Guid guidStore = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
                IWindowPropertyStore store;
                if (SHGetPropertyStoreForWindow(this.Handle, ref guidStore, out store) == 0 && store != null)
                {
                    WinPropKey key = new WinPropKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
                    WinPropVar pv = WinPropVar.FromString(appId);
                    store.SetValue(ref key, ref pv);
                    store.Commit();
                    pv.Clear();
                }
            }
            catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyWindowAppId();
            try
            {
                MARGINS margins = new MARGINS { leftWidth = 0, rightWidth = 0, topHeight = 1, bottomHeight = 0 };
                DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
            }
            catch { }
        }

        private int SlideWheelTickCount = 0;
        private int LastSlideWheelDirection = 0;
        private DateTime LastSlideWheelTime = DateTime.MinValue;
        private int _slideWheelDeltaAccumulator = 0;

        private FormWindowState _prevWindowState = FormWindowState.Normal;
        private FormBorderStyle _prevFormBorderStyle = FormBorderStyle.Sizable;
        private Rectangle _prevBounds = Rectangle.Empty;
        private SlideShowAudienceForm _audienceForm = null;
        private bool _isCaptionClosePressed = false;
        private bool _isCaptionMaxPressed = false;
        private bool _isCaptionMinPressed = false;

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        private string _originalTitle = null;

        public void StartSlideShow(bool fromBeginning, bool forcePresenterView = false)
        {
            if (!SlidesView.IsPresenting)
            {
                _prevWindowState = WindowState;
                _prevFormBorderStyle = FormBorderStyle;
                _prevBounds = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;
            }
            if (_originalTitle == null)
            {
                _originalTitle = Text;
            }

            if (!SlidesView.IsPresenting)
            {
                SlidesView.HideSearchBox();
                SlidesView.StartPresenting(fromBeginning);
            }

            Screen primary = Screen.FromControl(this) ?? Screen.PrimaryScreen;
            Screen[] allScreens = Screen.AllScreens;

            if (SlidesView.ShowType == 1)
            {
                // Chế độ 1: Cá nhân xem (Dạng cửa sổ - Windowed presentation)
                // PowerPoint hiển thị bài chiếu trong một cửa sổ có tiêu đề và khung viền điều chỉnh kích thước
                SlidesView.IsPresenterViewMode = false;
                TopMost = false;
                FormBorderStyle = FormBorderStyle.Sizable;
                Text = "Trình chiếu PowerPoint - " + (SlidesView != null && SlidesView.Presentation != null && !string.IsNullOrEmpty(SlidesView.Presentation.Title) ? SlidesView.Presentation.Title : "Trình bày 1");
            }
            else if (SlidesView.ShowType == 2)
            {
                // Chế độ 2: Duyệt tại Kiosk (Toàn màn hình lặp lại - Kiosk Mode)
                SlidesView.IsPresenterViewMode = false;
                SlidesView.LoopUntilEsc = true;
                StartPosition = FormStartPosition.Manual;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = primary.Bounds;
                TopMost = true;
                SetWindowPos(Handle, (IntPtr)(-1) /* HWND_TOPMOST */, primary.Bounds.X, primary.Bounds.Y, primary.Bounds.Width, primary.Bounds.Height, 0x0040 /* SWP_SHOWWINDOW */ | 0x0020 /* SWP_FRAMECHANGED */);
            }
            else
            {
                // Chế độ 0: Người thuyết trình (Toàn màn hình / Speaker Fullscreen)
                if (allScreens.Length > 1 && (SlidesView.UsePresenterView || forcePresenterView))
                {
                    // Multi-Monitor (2 màn hình như PowerPoint):
                    // Màn chính (Screen 1): AppForm mở Chế độ Diễn giả (Presenter View)
                    // Màn phụ (Screen 2 / Máy chiếu): Mở SlideShowAudienceForm toàn màn hình cho khán giả
                    Screen audienceScreen = null;
                    if (SlidesView.SelectedMonitorIndex > 0 && SlidesView.SelectedMonitorIndex <= allScreens.Length)
                    {
                        audienceScreen = allScreens[SlidesView.SelectedMonitorIndex - 1];
                    }
                    else
                    {
                        foreach (Screen s in allScreens)
                        {
                            if (!s.DeviceName.Equals(primary.DeviceName, StringComparison.OrdinalIgnoreCase))
                            {
                                audienceScreen = s;
                                break;
                            }
                        }
                    }
                    if (audienceScreen == null) audienceScreen = allScreens[1];

                    if (_audienceForm != null)
                    {
                        _audienceForm.Close();
                        _audienceForm = null;
                    }
                    _audienceForm = new SlideShowAudienceForm(SlidesView, this, audienceScreen);
                    _audienceForm.Show();

                    SlidesView.IsPresenterViewMode = true;

                    // Tối đa hóa AppForm trên màn hình chính che kín hoàn toàn taskbar
                    TopMost = true;
                    StartPosition = FormStartPosition.Manual;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Normal;
                    Bounds = primary.Bounds;
                    SetWindowPos(Handle, (IntPtr)(-1) /* HWND_TOPMOST */, primary.Bounds.X, primary.Bounds.Y, primary.Bounds.Width, primary.Bounds.Height, 0x0040 /* SWP_SHOWWINDOW */ | 0x0020 /* SWP_FRAMECHANGED */);
                }
                else
                {
                    // Single Screen (1 màn hình):
                    // Nếu forcePresenterView = true (Alt+F5), bật Presenter View trên màn hình duy nhất
                    SlidesView.IsPresenterViewMode = forcePresenterView;
                    StartPosition = FormStartPosition.Manual;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Normal;
                    Bounds = primary.Bounds;
                    TopMost = true;
                    SetWindowPos(Handle, (IntPtr)(-1) /* HWND_TOPMOST */, primary.Bounds.X, primary.Bounds.Y, primary.Bounds.Width, primary.Bounds.Height, 0x0040 /* SWP_SHOWWINDOW */ | 0x0020 /* SWP_FRAMECHANGED */);
                }
            }

            Invalidate();
        }

        public void StopSlideShow()
        {
            if (_audienceForm != null)
            {
                _audienceForm.Close();
                _audienceForm = null;
            }

            SlidesView.IsPresenting = false;
            SlidesView.IsEditingSpeakerNotes = false;
            SlidesView.IsEndSlideShowScreen = false;
            SlidesView.IsSlideSorterModalOpen = false;
            SlidesView.IsSlideZoomActive = false;
            SlideVideoPlayerManager.Instance.Stop();

            TopMost = false;
            FormBorderStyle = (_prevFormBorderStyle != FormBorderStyle.None) ? _prevFormBorderStyle : FormBorderStyle.Sizable;
            if (_prevWindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                if (_prevBounds.Width > 200 && _prevBounds.Height > 200)
                {
                    Bounds = _prevBounds;
                }
            }
            SetWindowPos(Handle, (IntPtr)(-2) /* HWND_NOTOPMOST */, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, 0x0040 /* SWP_SHOWWINDOW */ | 0x0020 /* SWP_FRAMECHANGED */);
            _prevBounds = Rectangle.Empty;
            if (_originalTitle != null)
            {
                Text = _originalTitle;
                _originalTitle = null;
            }

            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (SlidesView != null && SlidesView.IsPresenting)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                if (SlidesView.IsEditingSpeakerNotes)
                {
                    bool isCtrl = (keyData & Keys.Control) == Keys.Control;
                    if (keyCode == Keys.Escape || keyCode == Keys.Left || keyCode == Keys.Right ||
                        keyCode == Keys.Up || keyCode == Keys.Down || keyCode == Keys.Home ||
                        keyCode == Keys.End || keyCode == Keys.Delete ||
                        (isCtrl && (keyCode == Keys.A || keyCode == Keys.C || keyCode == Keys.X || keyCode == Keys.V)))
                    {
                        HandleSlideShowKeyDown(keyCode);
                        return true;
                    }
                    return base.ProcessCmdKey(ref msg, keyData);
                }
                if (keyCode == Keys.Escape || keyCode == Keys.Left || keyCode == Keys.Right ||
                    keyCode == Keys.Up || keyCode == Keys.Down || keyCode == Keys.PageUp ||
                    keyCode == Keys.PageDown || keyCode == Keys.Space || keyCode == Keys.Back ||
                    keyCode == Keys.Return || keyCode == Keys.Home || keyCode == Keys.End ||
                    keyCode == Keys.B || keyCode == Keys.W || keyCode == Keys.L || keyCode == Keys.P ||
                    keyCode == Keys.E || keyCode == Keys.F1 || keyCode == Keys.A || keyCode == Keys.H)
                {
                    HandleSlideShowKeyDown(keyCode);
                    return true;
                }
            }
            if (Ribbon != null && Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null && !SlidesView.IsPresenting)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                if (keyCode == Keys.Delete || keyCode == Keys.Back)
                {
                    bool isWinFormsTextFocused = (ActiveControl is TextBoxBase);
                    if (!isWinFormsTextFocused && !SlidesView.IsInlineEditing && !SlidesView.IsEditingSpeakerNotes && !SlidesView.IsEditingTableCell)
                    {
                        if (SlidesView.SelectedShapeIndex >= 0 || (SlidesView.SelectedShapeIndices != null && SlidesView.SelectedShapeIndices.Count > 0) || SlidesView.SelectedShape != null)
                        {
                            SlidesView.DeleteSelectedShapes();
                            MarkActiveTabModified();
                            Invalidate();
                            return true;
                        }
                    }
                }
            }
            if (Ribbon != null && Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                if (keyCode == Keys.Delete || keyCode == Keys.Back)
                {
                    bool isWinFormsTextFocused = (ActiveControl is TextBoxBase);
                    if (!isWinFormsTextFocused)
                    {
                        PhotoView.HandleKeyDown(Keys.Delete, ModifierKeys);
                        MarkActiveTabModified();
                        Invalidate();
                        return true;
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void HandleSlideShowKeyDown(Keys key)
        {
            SlidesView.HandleKeyDown(key);
            Invalidate();
            if (_audienceForm != null) _audienceForm.Invalidate();
        }

        public void ShowAiConfig()
        {
            Ribbon.CurrentMode = OfficeAppMode.Home;
            if (Ribbon.DocTabs != null && Ribbon.DocTabs.Count > 0)
            {
                for (int i = 0; i < Ribbon.DocTabs.Count; i++)
                {
                    Ribbon.DocTabs[i].Active = (Ribbon.DocTabs[i].Mode == OfficeAppMode.Home);
                    if (Ribbon.DocTabs[i].Active) Ribbon.ActiveDocIndex = i;
                }
            }
            HomeView.ActiveTab = 2; // Cài đặt (nơi tích hợp cấu hình AI)
            RectangleF sTab = HomeView.GetSettingsTabBounds(ClientSize.Width, 0);
            HomeView.IndicatorTargetX = sTab.X;
            HomeView.IndicatorTargetW = sTab.Width;
            if (_aiConfigPanel != null && _aiConfigPanel.Visible)
            {
                _aiConfigPanel.Visible = false;
            }
            Invalidate();
        }

        public void PositionAiConfigPanel()
        {
            if (_aiConfigPanel != null && _aiConfigPanel.Visible)
            {
                _aiConfigPanel.Visible = false;
            }
        }

        public void SyncAiPanels()
        {
            PositionAiConfigPanel();
        }

        public void ToggleAiAssistant()
        {
            AiAssistantForm dlg = new AiAssistantForm(this);
            dlg.Show(this);
        }

        public void RefreshAiChatProfileSelectors() {}
        public void ReloadAiSidebarConfig() {}

        public AppForm()
        {
            Instance = this;
            Text = "ZENQ OFFICE - Bộ Ứng Dụng Văn Phòng Đa Năng";
            Size = new Size(1366, 800);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            AllowDrop = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            try
            {
                TimeBeginPeriod(1);
            }
            catch { }

            Ribbon = new RibbonBar();
            Ribbon.Parent = this;
            Ribbon.HostAppForm = this;
            Ribbon.IsParentMaximized = true;
            NewTabRadialLauncher.TargetAppForm = this;
            HomeView = new HomeView();
            WriterView = new WriterView();
            CalcView = new CalcView();
            CalcView.ParentForm = this;
            SlidesView = new SlidesView();
            SlidesView.ParentControl = this;
            PdfView = new PdfView();
            PhotoView = new PhotoView();
            BackstageView = new OfficeBackstageView(this);

            _aiConfigPanel = new AiConfigPanel(this);
            _aiConfigPanel.Visible = false;
            this.Controls.Add(_aiConfigPanel);

            SlidesView.RequestStartSlideShow += (fromBeginning) =>
            {
                StartSlideShow(fromBeginning);
            };
            SlidesView.RequestStopSlideShow += () =>
            {
                StopSlideShow();
            };
            SlidesView.InvalidateCallback = () =>
            {
                Invalidate();
                if (_audienceForm != null && !_audienceForm.IsDisposed && SlidesView.IsPresenting)
                {
                    _audienceForm.Invalidate();
                }
            };
            SlideVideoPlayerManager.Instance.OnFrameRendered = () =>
            {
                if (SlidesView != null && SlidesView.InvalidateCallback != null)
                {
                    SlidesView.InvalidateCallback();
                }
            };
            SlideAudioPlayerManager.Instance.OnAudioStateChanged = () =>
            {
                if (SlidesView != null && SlidesView.InvalidateCallback != null)
                {
                    SlidesView.InvalidateCallback();
                }
            };
            WriterView.InvalidateCallback = () =>
            {
                Invalidate();
            };
            WriterView.ParentControl = this;
            CalcView.InvalidateCallback = () =>
            {
                Invalidate();
            };

            AppSettings.Instance.SettingsChanged += delegate
            {
                Invalidate();
            };

            StatusText = "Sẵn sàng";
            ZoomLevel = 1.0f;

            // Apply GenOffice Official Master Logo Icon
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string hubIconPath = System.IO.Path.Combine(baseDir, "ZenQOffice.ico");
                if (System.IO.File.Exists(hubIconPath))
                {
                    this.Icon = new Icon(hubIconPath);
                }
                else
                {
                    var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
                    if (stream != null) this.Icon = new Icon(stream);
                    else this.Icon = GenOfficeLogoRenderer.CreateAppIcon(32);
                }
            }
            catch { }

            // Cursor blink timer (530ms like Windows standard)
            _cursorTimer = new Timer();
            _cursorTimer.Interval = 530;
            _cursorTimer.Tick += (s, e) =>
            {
                WriterView.CursorVisible = !WriterView.CursorVisible;
                if (PdfView != null) PdfView.ShowCursor = !PdfView.ShowCursor;
                if ((Ribbon.CurrentMode == OfficeAppMode.Writer || Ribbon.CurrentMode == OfficeAppMode.Pdf) && !SlidesView.IsPresenting)
                    Invalidate();
            };
            _cursorTimer.Start();

            // Ultra-responsive, rock-solid 60+ FPS animation clock with zero interval-resets
            _animTimer = new Timer();
            _animTimer.Interval = 15;
            _animTimer.Tick += (s, e) =>
            {
                if (NewTabRadialLauncher.IsOpen)
                {
                    NewTabRadialLauncher.UpdateAnimation();
                    Invalidate();
                }

                bool anim1 = (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null) ? WriterView.UpdateSmoothScroll() : false;
                bool anim2 = (Ribbon.CurrentMode == OfficeAppMode.Home && HomeView != null) ? HomeView.UpdateSmoothScroll(ClientSize.Height, ClientSize.Width) : false;
                bool anim3 = false;
                if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                {
                    anim3 = SlidesView.UpdateAnimationAndTransition();
                }
                if (Ribbon.IsDraggingTab)
                {
                    Ribbon.ProcessDragAutoScroll(ClientSize.Width);
                }
                bool anim4 = Ribbon.UpdateTabScroll();
                bool anim5 = (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null) && PdfView.UpdateSmoothScroll();
                bool anim6 = (BackstageView != null && BackstageView.IsOpen) && BackstageView.UpdateSmoothScroll(ClientSize.Height, ClientSize.Width);

                if (anim1 || anim2 || anim3 || anim4 || anim5 || anim6)
                {
                    if (anim3 && SlidesView != null && SlidesView.IsPreviewingTransition && !SlidesView.IsPresenting && !anim1 && !anim2 && !anim4 && !anim5 && !anim6)
                    {
                        int leftSidebarW, canvasX, canvasW, contentY, contentH, bottomThumbH;
                        SlidesView.GetEffectiveLayoutMetrics(ClientSize.Width, ClientSize.Height, out leftSidebarW, out canvasX, out canvasW, out contentY, out contentH, out bottomThumbH);
                        Invalidate(new Rectangle(canvasX, contentY, canvasW, contentH));
                    }
                    else
                    {
                        Invalidate();
                    }

                    if (anim3 && _audienceForm != null && SlidesView.IsPresenting)
                    {
                        _audienceForm.Invalidate();
                    }
                }
            };
            _animTimer.Start();

            PeripheralDeviceManager.RefreshBaselineState();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnSystemDisplaySettingsChanged;

            SetupMobileConnectServer();
            CheckAppUpdateOnStartup();
        }

        private void CheckAppUpdateOnStartup()
        {
            try
            {
                UpdateManager.CheckForUpdateAsync((release) =>
                {
                    if (this.IsDisposed) return;
                    if (release != null && release.HasUpdate)
                    {
                        string newVer = release.TagName;
                        // Chỉ thông báo 1 lần duy nhất khi vừa vào app cho phiên bản này:
                        if (!string.IsNullOrEmpty(newVer) && !string.Equals(AppSettings.Instance.LastNotifiedUpdateVersion, newVer, StringComparison.OrdinalIgnoreCase))
                        {
                            AppSettings.Instance.LastNotifiedUpdateVersion = newVer;
                            AppSettings.Instance.Save();

                            if (this.IsHandleCreated)
                            {
                                this.BeginInvoke((MethodInvoker)delegate
                                {
                                    if (this.IsDisposed) return;
                                    using (UpdateDialogForm dlg = new UpdateDialogForm(release))
                                    {
                                        dlg.ShowDialog(this);
                                    }
                                });
                            }
                        }
                    }

                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (!this.IsDisposed && Ribbon.CurrentMode == OfficeAppMode.Home)
                            {
                                Invalidate();
                            }
                        });
                    }
                });
            }
            catch { }
        }

        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCACTIVATE = 0x0086;

        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public RECT[] rgrc;
            public IntPtr lppos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTL
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINTL ptReserved;
            public POINTL ptMaxSize;
            public POINTL ptMaxPosition;
            public POINTL ptMinTrackSize;
            public POINTL ptMaxTrackSize;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Remove WS_CAPTION to prevent DWM from drawing native Windows caption bar
                // (eliminates flash of Windows 7-style buttons during repaints)
                cp.Style &= ~0x00C00000; // Remove WS_CAPTION (WS_BORDER | WS_DLGFRAME)
                cp.Style |= 0x00040000; // WS_THICKFRAME
                cp.Style |= 0x00020000; // WS_MINIMIZEBOX
                cp.Style |= 0x00010000; // WS_MAXIMIZEBOX
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (SlidesView != null && SlidesView.IsPresenting)
            {
                if (SlidesView.ShowType != 1) // Chế độ toàn màn hình (Speakers, Kiosk, Presenter View)
                {
                    // 1. WM_NCHITTEST: Toàn bộ cửa sổ là Client Area -> Không xuất hiện mũi tên co giãn viền (<->), không viền kéo thả
                    if (m.Msg == WM_NCHITTEST)
                    {
                        m.Result = (IntPtr)HTCLIENT;
                        return;
                    }

                    // 2. WM_GETMINMAXINFO: Khóa cứng kích thước cực đại và cực tiểu đúng bằng kích thước toàn màn hình (che kín Taskbar)
                    if (m.Msg == 0x0024 /* WM_GETMINMAXINFO */)
                    {
                        base.WndProc(ref m);
                        try
                        {
                            MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
                            Screen screen = Screen.FromHandle(this.Handle) ?? Screen.PrimaryScreen;
                            if (screen != null)
                            {
                                Rectangle sb = screen.Bounds;
                                mmi.ptMaxPosition.X = 0;
                                mmi.ptMaxPosition.Y = 0;
                                mmi.ptMaxSize.X = sb.Width;
                                mmi.ptMaxSize.Y = sb.Height;
                                mmi.ptMaxTrackSize.X = sb.Width;
                                mmi.ptMaxTrackSize.Y = sb.Height;
                                mmi.ptMinTrackSize.X = sb.Width;
                                mmi.ptMinTrackSize.Y = sb.Height;
                                Marshal.StructureToPtr(mmi, m.LParam, false);
                            }
                        }
                        catch { }
                        return;
                    }

                    // 3. WM_SYSCOMMAND: Chặn tuyệt đối các thao tác di chuyển, co giãn, phóng to, thu nhỏ từ Windows
                    if (m.Msg == 0x0112 /* WM_SYSCOMMAND */)
                    {
                        int sc = (int)(m.WParam.ToInt64() & 0xFFF0);
                        if (sc == 0xF010 /* SC_MOVE */ || sc == 0xF000 /* SC_SIZE */ ||
                            sc == 0xF030 /* SC_MAXIMIZE */ || sc == 0xF120 /* SC_RESTORE */)
                        {
                            m.Result = IntPtr.Zero;
                            return;
                        }
                    }

                    // 4. Triệt tiêu hoàn toàn Non-Client Paint và CalcSize
                    if (m.Msg == WM_NCCALCSIZE || m.Msg == 0x0085 /* WM_NCPAINT */)
                    {
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    if (m.Msg == WM_NCACTIVATE)
                    {
                        m.Result = (IntPtr)1;
                        return;
                    }
                }

                base.WndProc(ref m);
                return;
            }

            // Constrain maximized window bounds to working area (above taskbar)
            if (m.Msg == 0x0024 /* WM_GETMINMAXINFO */)
            {
                base.WndProc(ref m);
                try
                {
                    MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
                    Screen screen = Screen.FromHandle(this.Handle) ?? Screen.PrimaryScreen;
                    if (screen != null)
                    {
                        Rectangle wa = screen.WorkingArea;
                        Rectangle sb = screen.Bounds;
                        mmi.ptMaxPosition.X = wa.Left - sb.Left;
                        mmi.ptMaxPosition.Y = wa.Top - sb.Top;
                        mmi.ptMaxSize.X = wa.Width;
                        mmi.ptMaxSize.Y = wa.Height;
                        Marshal.StructureToPtr(mmi, m.LParam, false);
                    }
                }
                catch { }
                return;
            }

            if (m.Msg == 0x0085 /* WM_NCPAINT */)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_NCCALCSIZE)
            {
                if (m.WParam != IntPtr.Zero)
                {
                    if (WindowState == FormWindowState.Maximized)
                    {
                        try
                        {
                            NCCALCSIZE_PARAMS csp = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
                            Screen screen = Screen.FromHandle(m.HWnd) ?? Screen.PrimaryScreen;
                            if (screen != null)
                            {
                                Rectangle wa = screen.WorkingArea;
                                csp.rgrc[0].Left = wa.Left;
                                csp.rgrc[0].Top = wa.Top;
                                csp.rgrc[0].Right = wa.Right;
                                csp.rgrc[0].Bottom = wa.Bottom;
                                Marshal.StructureToPtr(csp, m.LParam, false);
                            }
                        }
                        catch { }
                    }
                    m.Result = IntPtr.Zero;
                    return;
                }
                else
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            if (m.Msg == WM_NCACTIVATE)
            {
                m.Result = (IntPtr)1;
                return;
            }

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);

                int x = unchecked((short)(long)m.LParam);
                int y = unchecked((short)((long)m.LParam >> 16));
                Point pt = PointToClient(new Point(x, y));

                // 1. Resizing edges (only when Normal window state)
                if (WindowState == FormWindowState.Normal)
                {
                    const int b = 6;
                    bool left = pt.X <= b;
                    bool right = pt.X >= ClientSize.Width - b;
                    bool top = pt.Y <= b;
                    bool bottom = pt.Y >= ClientSize.Height - b;

                    if (top && left) { m.Result = (IntPtr)HTTOPLEFT; return; }
                    if (top && right) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                    if (bottom && left) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                    if (bottom && right) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                    if (top) { m.Result = (IntPtr)HTTOP; return; }
                    if (bottom) { m.Result = (IntPtr)HTBOTTOM; return; }
                    if (left) { m.Result = (IntPtr)HTLEFT; return; }
                    if (right) { m.Result = (IntPtr)HTRIGHT; return; }
                }

                // 2. Custom Title Bar / Tab Bar row
                int rTop = (Ribbon != null && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36;
                if (pt.Y <= rTop)
                {
                    if (Ribbon != null)
                    {
                        // Caption & Tab scroll buttons: HTCLIENT so WinForms handles hover and clicks
                        if ((!Ribbon.CaptionCloseBounds.IsEmpty && Ribbon.CaptionCloseBounds.Contains(pt)) ||
                            (!Ribbon.CaptionMaxBounds.IsEmpty && Ribbon.CaptionMaxBounds.Contains(pt)) ||
                            (!Ribbon.CaptionMinBounds.IsEmpty && Ribbon.CaptionMinBounds.Contains(pt)) ||
                            (!Ribbon.CaptionTouchModeBounds.IsEmpty && Ribbon.CaptionTouchModeBounds.Contains(pt)) ||
                            (!Ribbon.CaptionPeripheralsBounds.IsEmpty && Ribbon.CaptionPeripheralsBounds.Contains(pt)) ||
                            (!Ribbon.CaptionMobileConnectBounds.IsEmpty && Ribbon.CaptionMobileConnectBounds.Contains(pt)) ||
                            (!Ribbon.DocTabScrollLeftBounds.IsEmpty && Ribbon.DocTabScrollLeftBounds.Contains(pt)) ||
                            (!Ribbon.DocTabScrollRightBounds.IsEmpty && Ribbon.DocTabScrollRightBounds.Contains(pt)))
                        {
                            m.Result = (IntPtr)HTCLIENT;
                            return;
                        }

                        // Interactive tabs
                        if (Ribbon.DocTabBounds != null)
                        {
                            for (int i = 0; i < Ribbon.DocTabBounds.Count; i++)
                            {
                                if (Ribbon.DocTabBounds[i].Contains(pt))
                                {
                                    m.Result = (IntPtr)HTCLIENT;
                                    return;
                                }
                            }
                        }

                        // '+' New Tab button
                        if (!Ribbon.DocTabPlusBounds.IsEmpty && Ribbon.DocTabPlusBounds.Contains(pt))
                        {
                            m.Result = (IntPtr)HTCLIENT;
                            return;
                        }

                        // Standalone Quick Access buttons
                        if (Ribbon.StandaloneMode)
                        {
                            if ((!Ribbon.QuickSaveBounds.IsEmpty && Ribbon.QuickSaveBounds.Contains(pt)) ||
                                (!Ribbon.QuickUndoBounds.IsEmpty && Ribbon.QuickUndoBounds.Contains(pt)) ||
                                (!Ribbon.QuickRedoBounds.IsEmpty && Ribbon.QuickRedoBounds.Contains(pt)))
                            {
                                m.Result = (IntPtr)HTCLIENT;
                                return;
                            }
                        }
                    }

                    // Empty title bar area: HTCAPTION for native window dragging and double-click maximize
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }

                m.Result = (IntPtr)HTCLIENT;
                return;
            }

            if (m.Msg == PeripheralDeviceManager.WM_DEVICECHANGE)
            {
                HandleWmDeviceChange(ref m);
            }
            else if (m.Msg == PeripheralDeviceManager.WM_POINTERDOWN ||
                     m.Msg == PeripheralDeviceManager.WM_POINTERUPDATE ||
                     m.Msg == PeripheralDeviceManager.WM_POINTERUP)
            {
                HandleWmPointerInput(ref m);
            }
            else if (m.Msg == PeripheralDeviceManager.WM_GESTURE)
            {
                if (HandleWmGesture(ref m))
                {
                    return;
                }
            }
            else if (m.Msg == PeripheralDeviceManager.WM_MOUSEHWHEEL)
            {
                if (HandleWmMouseHWheel(ref m))
                {
                    return;
                }
            }
            else if (m.Msg == PeripheralDeviceManager.WM_APPCOMMAND)
            {
                if (HandleWmAppCommand(ref m))
                {
                    return;
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Fully handled by Direct2D pipeline - suppress GDI background erase to eliminate flickering
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (e.Graphics == null) return;
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);

            using (D2DGraphics d2d = new D2DGraphics(e.Graphics, w, h))
            {
                RenderFrame(d2d, w, h);
            }
        }

        private void RenderFrame(D2DGraphics g, int clientWidth, int clientHeight)
        {
            if (g == null) return;

            try
            {
                if (SlidesView != null && SlidesView.IsPresenting)
                {
                    SlidesView.DrawFullscreenPresenter(g, clientWidth, clientHeight, true);
                    return;
                }

                if (BackstageView != null && BackstageView.IsOpen)
                {
                    bool isStandalone = (Ribbon != null && Ribbon.StandaloneMode);
                    if (!isStandalone && Ribbon != null)
                    {
                        // Giữ lại thanh tab ứng dụng phía trên (Trang chính | doc 1 | doc 2 | +)
                        Ribbon.DrawTopDocTabBar(g, clientWidth);

                        int bsTopOffset = (Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36;
                        var gState = g.Save();
                        g.IntersectClip(new Rectangle(0, bsTopOffset, clientWidth, Math.Max(1, clientHeight - bsTopOffset)));
                        g.TranslateTransform(0, bsTopOffset);
                        BackstageView.Draw(g, clientWidth, Math.Max(1, clientHeight - bsTopOffset));
                        g.Restore(gState);
                    }
                    else
                    {
                        // Chế độ mở ứng dụng đơn từ shortcut ngoài desktop: che toàn màn hình
                        BackstageView.Draw(g, clientWidth, clientHeight);
                    }
                    return;
                }

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // Ensure top title bar area (0..36px) is solidly prefilled
                bool isIsland = (AppSettings.Instance.RibbonStyle == RibbonStyle.Island);
                Color titleBarColor = (isIsland && Ribbon != null)
                    ? ((Ribbon.CurrentMode == OfficeAppMode.Writer) ? Color.FromArgb(229, 229, 229) :
                       ((Ribbon.CurrentMode == OfficeAppMode.Slides) ? Color.FromArgb(240, 240, 240) :
                       Color.FromArgb(238, 238, 238)))
                    : Color.FromArgb(248, 250, 252);
                int topFillH = (Ribbon != null && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36;
                using (SolidBrush titleBg = new SolidBrush(titleBarColor))
                {
                    g.FillRectangle(titleBg, 0, 0, clientWidth, topFillH);
                }

                if (Ribbon != null)
                {
                    Ribbon.IsParentMaximized = (this.WindowState == FormWindowState.Maximized);
                }

                // 1. Draw Active Viewport (Home / Writer / Calc / Slides / Pdf)
                try
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Home)
                    {
                        HomeView.Draw(g, clientWidth, clientHeight);
                    }
                    else
                    {
                        if (_aiConfigPanel != null && _aiConfigPanel.Visible)
                        {
                            _aiConfigPanel.Visible = false;
                        }

                        if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            WriterView.Draw(g, clientWidth, clientHeight, Ribbon);
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            CalcView.Draw(g, clientWidth, clientHeight);
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            SlidesView.Draw(g, clientWidth, clientHeight);
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            PdfView.Draw(g, clientWidth, clientHeight, Ribbon);
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                        {
                            PhotoView.Draw(g, clientWidth, clientHeight, Ribbon);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AppForm Viewport Draw error: " + ex.Message);
                }

                // 2. Draw Ribbon Bar
                try
                {
                    if (Ribbon != null)
                    {
                        Ribbon.Draw(g, clientWidth, clientHeight, WriterView, SlidesView, CalcView, PdfView, PhotoView);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AppForm Ribbon Draw error: " + ex.Message);
                }

                // 3. Draw Status Bar (Ẩn hoàn toàn khi ở Trang chủ Home)
                if (Ribbon.CurrentMode != OfficeAppMode.Home)
                {
                    DrawStatusBar(g, clientWidth, clientHeight);
                }

                // 3B. Draw Floating Touch Quick Action Bar (khi bật Chế độ Cảm ứng)
                if (!SlidesView.IsPresenting && Ribbon.CurrentMode != OfficeAppMode.Home && AppSettings.Instance.TouchModeEnabled)
                {
                    DrawFloatingTouchActionBar(g, clientWidth, clientHeight);
                }
                else
                {
                    _touchBarBounds = Rectangle.Empty;
                }

                // 3B2. Draw Right-Side Inking Palette
                if (!SlidesView.IsPresenting && Ribbon.CurrentMode != OfficeAppMode.Home)
                {
                    DrawFloatingInkingPalette(g, clientWidth, clientHeight);
                }
                else
                {
                    _inkPaletteBounds = Rectangle.Empty;
                }

                // 3C. Draw Smart Peripheral Plug-and-Play Toast Notification Banner
                DrawPeripheralToastBanner(g, clientWidth, clientHeight);

                // 4. Draw IDE-Style Word Suggestions Popup (Topmost Floating Overlay)
                if (WordSuggestions != null && WordSuggestions.IsVisible)
                {
                    int newX = WordSuggestions.Bounds.X;
                    int newY = WordSuggestions.Bounds.Y;
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && WriterView.CurrentCaretScreenX > 0)
                    {
                        newX = WriterView.CurrentCaretScreenX;
                        newY = WriterView.CurrentCaretScreenY + WriterView.CurrentCaretScreenH + 2;
                    }
                    else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null && CalcView.CurrentCaretScreenX > 0)
                    {
                        newX = CalcView.CurrentCaretScreenX;
                        newY = CalcView.CurrentCaretScreenY;
                    }
                    else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null && SlidesView.CurrentCaretScreenX > 0)
                    {
                        newX = SlidesView.CurrentCaretScreenX;
                        newY = SlidesView.CurrentCaretScreenY;
                    }
                    WordSuggestions.Reposition(newX, newY, clientWidth, clientHeight);
                    WordSuggestions.Draw(g);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AppForm.RenderFrame error: " + ex);
            }
        }

        public int ActiveViewMode = 1; // 0 = Chế độ đọc, 1 = Bố cục in, 2 = Bố cục Web
        public bool IsDraggingZoomSlider = false;

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            int count = 0;
            bool inWord = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetterOrDigit(text[i]))
                {
                    if (!inWord)
                    {
                        count++;
                        inWord = true;
                    }
                }
                else
                {
                    inWord = false;
                }
            }
            return count;
        }

        private void UpdateZoomFromMouseX(int mouseX)
        {
            int w = ClientSize.Width;
            int sliderX = w - 142;
            int sliderW = 70;
            float frac = (float)(mouseX - sliderX) / (float)sliderW;
            frac = Math.Max(0.0f, Math.Min(1.0f, frac));
            if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                // Non-linear mapping: 50% of track maps to 100% zoom (1.0f)
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.10f + (frac / 0.5f) * 0.90f; // 10% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 3.00f; // 100% to 400%
                SlidesView.ZoomLevel = newZoom;
                ZoomLevel = newZoom;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.20f + (frac / 0.5f) * 0.80f; // 20% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 2.00f; // 100% to 300%
                Point centerView = new Point(ClientSize.Width / 2, 155 + (ClientSize.Height - 155 - 26) / 2);
                PdfView.ZoomAt(centerView, newZoom, ClientSize.Width, ClientSize.Height, 155, 26);
                ZoomLevel = PdfView.ZoomLevel;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.10f + (frac / 0.5f) * 0.90f;
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 4.00f;
                PhotoView.Zoom = newZoom;
                ZoomLevel = newZoom;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.20f + (frac / 0.5f) * 0.80f; // 20% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 3.00f; // 100% to 400%
                CalcView.ZoomFactor = newZoom;
                ZoomLevel = newZoom;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.20f + (frac / 0.5f) * 0.80f; // 20% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 1.50f; // 100% to 250%
                Point centerView = new Point(ClientSize.Width / 2, (ClientSize.Height - 155) / 2 + 155);
                WriterView.ZoomTo(newZoom, centerView, Ribbon);
                ZoomLevel = newZoom;
            }
            Invalidate();
        }

        private void DrawStatusBar(D2DGraphics g, int width, int height)
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Home) return;
            int barH = 26;
            Rectangle statusRect = new Rectangle(0, height - barH, width, barH);
            
            // MS Word Signature Status Bar Color (#2B579A Navy Blue)
            Color statusBg = (Ribbon.PrimaryColor == Color.White) ? Color.FromArgb(43, 87, 154) : Ribbon.PrimaryColor;
            using (SolidBrush statusBrush = new SolidBrush(statusBg))
            {
                g.FillRectangle(statusBrush, statusRect);
            }

            // Top subtle accent line
            using (Pen topPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1))
            {
                g.DrawLine(topPen, 0, height - barH, width, height - barH);
            }

            using (Font sFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Font iconFont = new Font("Segoe UI Symbol", 9.0f, FontStyle.Regular))
            {
                // LEFT SECTION: Page count, Word/Char count, Language Proofing
                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    int totalWords = WriterView.TotalWordCount;
                    int totalChars = WriterView.TotalCharCount;
                    int selLen = Math.Abs(WriterView.SelectionEnd - WriterView.SelectionStart);
                    string selStr = (selLen > 0) ? (" (" + selLen + " được chọn)") : "";

                    int curPg = WriterView.CurrentActivePage;
                    int totPgs = WriterView.TotalPageCount;
                    string pageStr = string.Format("Trang {0} / {1}", curPg, totPgs);
                    string wordStr = string.Format("{0:N0} từ, {1:N0} ký tự{2}", totalWords, totalChars, selStr);

                    g.DrawString(pageStr, sFont, Brushes.White, 16, height - 20);
                    g.DrawString(wordStr, sFont, Brushes.White, 110, height - 20);

                    // Proofing / Language icon [📖✓]
                    g.DrawString("📖✓  Tiếng Việt (Unicode)", sFont, Brushes.White, 310, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    string addr = SparseMatrix.ColIndexToName(CalcView.SelectedCol) + CalcView.SelectedRow;
                    string bcBadge = AppSettings.Instance.CalcBarcodeScannerMode ? "   |   📠 Máy quét Mã vạch: BẬT" : "";
                    g.DrawString("Sẵn sàng   |   Ô chọn: " + addr + bcBadge, sFont, Brushes.White, 16, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    string info = "Slide " + (SlidesView.ActiveSlideIndex + 1) + " / " + SlidesView.Presentation.Slides.Count;
                    g.DrawString(info, sFont, Brushes.White, 16, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    int curP = PdfView.ActivePageIndex + 1;
                    int totP = (PdfView.Document != null) ? Math.Max(1, PdfView.Document.Pages.Count) : 1;
                    string pageStr = string.Format("Trang {0} / {1}", curP, totP);
                    g.DrawString(pageStr, sFont, Brushes.White, 16, height - 20);
                    g.DrawString("PDF Editor (Chuẩn PDF 1.7)", sFont, Brushes.White, 140, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    int lCount = (PhotoView.Document != null) ? PhotoView.Document.Layers.Count : 0;
                    int curL = (PhotoView.Document != null && PhotoView.Document.ActiveLayer != null) ? PhotoView.Document.ActiveLayerIndex + 1 : 0;
                    string lStr = string.Format("Lớp {0} / {1}", curL, lCount);
                    g.DrawString(lStr, sFont, Brushes.White, 16, height - 20);
                    int dw = (PhotoView.Document != null) ? PhotoView.Document.Width : 1920;
                    int dh = (PhotoView.Document != null) ? PhotoView.Document.Height : 1080;
                    g.DrawString(string.Format("ZenQ Vector Designer ({0} x {1} px)", dw, dh), sFont, Brushes.White, 120, height - 20);
                }
                else
                {
                    g.DrawString("Sẵn sàng", sFont, Brushes.White, 16, height - 20);
                }

                // RIGHT SECTION: View Mode icons (Read 📖, Print 📄, Web 🌐), Smooth Drag Zoom Slider, Zoom Percentage
                float activeZoom = 1.0f;
                if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                    activeZoom = SlidesView.ZoomLevel;
                else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                    activeZoom = WriterView.ZoomLevel;
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                    activeZoom = PdfView.ZoomLevel;
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                    activeZoom = PhotoView.Zoom;
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                    activeZoom = (CalcView.ZoomFactor > 0) ? CalcView.ZoomFactor : 1.0f;

                int currentZoom = (int)Math.Round(activeZoom * 100);

                // -1. Touch Mode / Peripheral Quick Toggle Pill on Status Bar
                int touchBtnX = (Ribbon.CurrentMode == OfficeAppMode.Slides) ? (width - 442) : (width - 344);
                _statusTouchToggleBounds = new Rectangle(touchBtnX, height - 23, 90, 20);
                bool touchOn = AppSettings.Instance.TouchModeEnabled;
                if (touchOn)
                {
                    using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(85, 255, 255, 255)))
                    {
                        g.FillRectangle(activeBg, _statusTouchToggleBounds);
                    }
                    g.DrawRectangle(Pens.White, _statusTouchToggleBounds);
                }
                string touchGlyph = touchOn
                    ? (PeripheralDeviceManager.LastActivePointerType == PointerInputType.Pen ? "\uED63" : "\uE815")
                    : "\uE815";
                string touchLabel = touchOn
                    ? (PeripheralDeviceManager.LastActivePointerType == PointerInputType.Pen ? "Bút Stylus" : "Cảm ứng")
                    : "Cảm ứng";
                FluentIconRenderer.DrawGlyph(g, touchGlyph, new Rectangle(_statusTouchToggleBounds.X + 4, _statusTouchToggleBounds.Y + 2, 16, 16), Color.White, 8.5f);
                g.DrawString(touchLabel, sFont, Brushes.White, _statusTouchToggleBounds.X + 21, _statusTouchToggleBounds.Y + 2);

                // 0. Speaker Notes Toggle Button ("Ghi chú") for Slides Mode (PowerPoint-style)
                if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                {
                    Rectangle notesBtnRect = new Rectangle(width - 344, height - 23, 68, 20);
                    if (SlidesView.IsNotesPaneOpen)
                    {
                        using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                        {
                            g.FillRectangle(activeBg, notesBtnRect);
                        }
                        g.DrawRectangle(Pens.White, notesBtnRect);
                    }
                    FluentIconRenderer.DrawGlyph(g, "\uE70B", new Rectangle(notesBtnRect.X + 3, notesBtnRect.Y + 2, 16, 16), Color.White, 8.5f);
                    g.DrawString("Ghi chú", sFont, Brushes.White, notesBtnRect.X + 19, notesBtnRect.Y + 2);
                }

                // 1. View Mode & Slide Show Icons
                if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                {
                    // 4 Buttons in Slides mode: Normal, Slide Sorter, Reading View, Slide Show
                    int modeX = width - 270;
                    for (int m = 0; m < 4; m++)
                    {
                        Rectangle mRect = new Rectangle(modeX + m * 24, height - 23, 22, 20);
                        bool isActive = false;
                        if (m == 0 && SlidesView.ViewMode == ImpressViewMode.Normal) isActive = true;
                        else if (m == 1 && SlidesView.ViewMode == ImpressViewMode.SlideSorter) isActive = true;
                        else if (m == 2 && SlidesView.ViewMode == ImpressViewMode.ReadingView) isActive = true;
                        else if (m == 3 && SlidesView.IsPresenting) isActive = true;

                        if (isActive)
                        {
                            using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                            {
                                g.FillRectangle(activeBg, mRect);
                            }
                            g.DrawRectangle(Pens.White, mRect);
                        }
                        DrawSlideStatusBarButton(g, m, mRect);
                    }
                }
                else
                {
                    // View Mode Icons for Writer (Read Mode, Print Layout, Web Layout)
                    int modeX = width - 246;
                    string[] modeGlyphs = new string[] { "\uE736", "\uE8A5", "\uE774" };
                    for (int m = 0; m < 3; m++)
                    {
                        Rectangle mRect = new Rectangle(modeX + m * 24, height - 23, 22, 20);
                        if (ActiveViewMode == m)
                        {
                            using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                            {
                                g.FillRectangle(activeBg, mRect);
                            }
                            g.DrawRectangle(Pens.White, mRect);
                        }
                        FluentIconRenderer.DrawGlyph(g, modeGlyphs[m], mRect, Color.White, 9f);
                    }
                }

                // 2. Zoom Minus [-] Button
                int zoomMinusX = width - 164;
                Rectangle minusRect = new Rectangle(zoomMinusX, height - 23, 18, 20);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("-", sFont, Brushes.White, new RectangleF(minusRect.X, minusRect.Y, minusRect.Width, minusRect.Height), sf);
                }

                // 3. Smooth Continuous Drag Trackbar Line (70px)
                int sliderX = width - 142;
                int sliderW = 70;
                int sliderY = height - 13;
                using (Pen trackPen = new Pen(Color.FromArgb(180, 255, 255, 255), 2))
                {
                    g.DrawLine(trackPen, sliderX, sliderY, sliderX + sliderW, sliderY);
                }

                // 100% Center Notch Tick (at exact 50% midpoint of track)
                int midNotchX = sliderX + sliderW / 2;
                using (Pen notchPen = new Pen(Color.FromArgb(240, 255, 255, 255), 1.5f))
                {
                    g.DrawLine(notchPen, midNotchX, sliderY - 4, midNotchX, sliderY + 4);
                }

                // Slider Handle Knob (Vertical white pill indicator)
                float zoomFrac = 0.5f;
                if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.10f) / 0.90f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 3.00f);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.20f) / 0.80f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 2.00f);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.20f) / 0.80f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 3.00f);
                }
                else
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.20f) / 0.80f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 1.50f);
                }
                zoomFrac = Math.Max(0.0f, Math.Min(1.0f, zoomFrac));
                int knobX = sliderX + (int)Math.Round(zoomFrac * sliderW);
                Rectangle knobRect = new Rectangle(knobX - 3, sliderY - 6, 7, 13);
                g.FillRectangle(Brushes.White, knobRect);
                g.DrawRectangle(Pens.DarkSlateGray, knobRect);

                // 4. Zoom Plus [+] Button
                int zoomPlusX = width - 68;
                Rectangle plusRect = new Rectangle(zoomPlusX, height - 23, 18, 20);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("+", sFont, Brushes.White, new RectangleF(plusRect.X, plusRect.Y, plusRect.Width, plusRect.Height), sf);
                }

                // 5. Zoom Percentage Text (100%)
                string pctStr = currentZoom + "%";
                Rectangle pctRect = new Rectangle(width - 46, height - 23, 42, 20);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(pctStr, sFont, Brushes.White, pctRect, sf);
                }
            }

            if (NewTabRadialLauncher.IsOpen)
            {
                NewTabRadialLauncher.Draw(g, ClientSize.Width, ClientSize.Height);
            }
        }

        private void DrawSlideStatusBarButton(D2DGraphics g, int mode, Rectangle mRect)
        {
            int x = mRect.X + (mRect.Width - 14) / 2;
            int y = mRect.Y + (mRect.Height - 11) / 2;
            using (Pen pen = new Pen(Color.White, 1.2f))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                switch (mode)
                {
                    case 0: // Normal View (Bình thường: Cột thumbnail bên trái & canvas chính)
                        g.DrawRectangle(pen, x, y, 14, 11);
                        g.DrawLine(pen, x + 4, y, x + 4, y + 11);
                        g.FillRectangle(brush, x + 1.5f, y + 2.5f, 1.5f, 2.0f);
                        g.FillRectangle(brush, x + 1.5f, y + 6.0f, 1.5f, 2.0f);
                        g.FillRectangle(brush, x + 6.5f, y + 2.5f, 5.5f, 6.0f);
                        break;
                    case 1: // Slide Sorter (Bộ sắp xếp slide: Lưới 4 ô chuẩn PowerPoint)
                        g.DrawRectangle(pen, x, y, 6, 4.5f);
                        g.DrawRectangle(pen, x + 7.5f, y, 6, 4.5f);
                        g.DrawRectangle(pen, x, y + 6.5f, 6, 4.5f);
                        g.DrawRectangle(pen, x + 7.5f, y + 6.5f, 6, 4.5f);
                        break;
                    case 2: // Reading View (Chế độ xem Đọc: Quyển sách mở)
                        FluentIconRenderer.DrawGlyph(g, "\uE736", mRect, Color.White, 9f);
                        break;
                    case 3: // Slide Show (Trình chiếu: Màn chiếu ba chân với nút Play)
                        g.DrawLine(pen, x, y, x + 14, y);
                        g.DrawRectangle(pen, x + 1, y + 1, 12, 7);
                        g.FillPolygon(brush, new PointF[] {
                            new PointF(x + 5.5f, y + 2.8f),
                            new PointF(x + 9.5f, y + 4.5f),
                            new PointF(x + 5.5f, y + 6.2f)
                        });
                        g.DrawLine(pen, x + 7, y + 8, x + 7, y + 10);
                        g.DrawLine(pen, x + 4, y + 12, x + 7, y + 10);
                        g.DrawLine(pen, x + 10, y + 12, x + 7, y + 10);
                        break;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Ribbon != null && !Ribbon.StandaloneMode)
            {
                int ribbonTopH = (Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36;
                if (e.Y <= ribbonTopH)
                {
                    if (Ribbon.OnMouseWheel(e, ClientSize.Width))
                    {
                        Invalidate();
                        return;
                    }
                }
            }

            if (BackstageView != null && BackstageView.IsOpen)
            {
                int topFillH = (Ribbon != null && !Ribbon.StandaloneMode && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : (Ribbon != null && !Ribbon.StandaloneMode ? 36 : 0);
                MouseEventArgs adjustedE = (topFillH > 0) ? new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y - topFillH, e.Delta) : e;
                BackstageView.OnMouseWheel(adjustedE);
                Invalidate();
                return;
            }

            if (SlidesView != null && SlidesView.IsPresenting)
            {
                if (SlidesView.IsPresenterViewMode)
                {
                    if (SlidesView.LastNotesTextRect.Width > 0 && SlidesView.LastNotesTextRect.Contains(e.X, e.Y))
                    {
                        SlidesView.NotesScrollY = Math.Max(0, SlidesView.NotesScrollY + ((e.Delta > 0) ? -28 : 28));
                        Invalidate();
                        return;
                    }
                    if (e.X >= (int)(ClientSize.Width * 0.62f))
                    {
                        return;
                    }
                }
                if (e.Delta < 0) SlidesView.AdvancePresenterStep();
                else if (e.Delta > 0) SlidesView.PreviousPresenterStep();
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
                return;
            }

            WriterFloatingMiniToolbarForm.CloseCurrent();
            if (WordSuggestions != null && WordSuggestions.IsVisible)
            {
                WordSuggestions.Hide();
            }

            // 0. Khi rê chuột vào đúng vùng thanh kéo zoom ở góc phải thanh trạng thái
            // Lăn chuột phóng to / thu nhỏ tài liệu NGAY LẬP TỨC mà không cần nhấn phím Ctrl!
            bool isOverZoomSlider = (e.X >= ClientSize.Width - 210 && e.X <= ClientSize.Width - 40 && e.Y >= ClientSize.Height - 26);
            if (Ribbon.CurrentMode != OfficeAppMode.Home && isOverZoomSlider)
            {
                float zoomStep = (e.Delta > 0) ? 0.10f : -0.10f;
                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    Point centerView = new Point(ClientSize.Width / 2, (ClientSize.Height - 155) / 2 + 155);
                    WriterView.ZoomAt(centerView, (e.Delta > 0) ? 120 : -120, Ribbon, ClientSize.Width, ClientSize.Height);
                    ZoomLevel = WriterView.ZoomLevel;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                    CalcView.ZoomFactor = Math.Max(0.20f, Math.Min(4.00f, CalcView.ZoomFactor + zoomStep));
                    ZoomLevel = CalcView.ZoomFactor;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    float sStep = (SlidesView.ZoomLevel >= 1.5f || (SlidesView.ZoomLevel > 1.4f && e.Delta > 0)) ? 0.25f : 0.10f;
                    SlidesView.ZoomLevel = Math.Max(0.10f, Math.Min(4.00f, SlidesView.ZoomLevel + ((e.Delta > 0) ? sStep : -sStep)));
                    ZoomLevel = SlidesView.ZoomLevel;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    Point centerView = new Point(ClientSize.Width / 2, 155 + (ClientSize.Height - 155 - 26) / 2);
                    PdfView.ZoomAt(centerView, PdfView.ZoomLevel + zoomStep, ClientSize.Width, ClientSize.Height, 155, 26);
                    ZoomLevel = PdfView.ZoomLevel;
                }
                Invalidate();
                return;
            }

            // Horizontal Mouse Wheel Scroll for Document Tabs on Title Bar (except Pinned Home Tab)
            if (e.Y <= ((Ribbon != null && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36) && !Ribbon.StandaloneMode)
            {
                Ribbon.OnMouseWheel(e, ClientSize.Width);
                Invalidate(new Rectangle(0, 0, ClientSize.Width, (Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36));
                return;
            }

            float wheelNotches = (float)e.Delta / 120.0f;

            if (Ribbon.CurrentMode == OfficeAppMode.Home)
            {
                int maxScroll = HomeView.GetMaxScroll(ClientSize.Height, ClientSize.Width);
                HomeView.TargetScrollY = Math.Max(0, Math.Min(maxScroll, (float)Math.Round(HomeView.TargetScrollY - wheelNotches * 95f)));
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                if (WriterView.ShowSelectionPane && e.X >= ClientSize.Width - 260 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                {
                    WriterView.HandleSelectionPaneMouseWheel(e.Delta);
                    Invalidate();
                    return;
                }

                if (WriterView.ShowNavigationPane && e.X <= 280 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                {
                    WriterView.HandleNavMouseWheel(e.Delta, ClientSize.Height - 155 - 26 - 125);
                    Invalidate();
                    return;
                }

                if ((ModifierKeys & Keys.Control) != 0)
                {
                    WriterView.ZoomAt(e.Location, e.Delta, Ribbon, ClientSize.Width, ClientSize.Height);
                    ZoomLevel = WriterView.ZoomLevel;
                    Invalidate();
                }
                else if ((ModifierKeys & Keys.Shift) != 0)
                {
                    // Shift + MouseWheel: Cuộn ngang tài liệu (Horizontal Scroll)
                    int maxScrollX = WriterView.GetMaxScrollX(ClientSize.Width);
                    if (maxScrollX > 0)
                    {
                        float hDelta = -wheelNotches * 95f;
                        WriterView.TargetScrollX = Math.Max(0f, Math.Min(maxScrollX, WriterView.TargetScrollX + hDelta));
                        Invalidate();
                    }
                }
                else
                {
                    if (WriterView.ViewMode == 0)
                    {
                        if (e.Delta < 0 && WriterView.ReadModePageIndex < WriterView.TotalPageCount - 1)
                        {
                            WriterView.ReadModePageIndex++;
                            Invalidate();
                        }
                        else if (e.Delta > 0 && WriterView.ReadModePageIndex > 0)
                        {
                            WriterView.ReadModePageIndex--;
                            Invalidate();
                        }
                        return;
                    }

                    int maxScroll = WriterView.GetMaxScrollY(ClientSize.Height, Ribbon);
                    float vDelta = -wheelNotches * 95f;
                    WriterView.TargetScrollY = Math.Max(0f, Math.Min(maxScroll, WriterView.TargetScrollY + vDelta));

                    if (WriterView.IsDraggingShape)
                    {
                        WriterView.UpdateShapeDragPosition(e.X, e.Y, ClientSize.Width, Ribbon);
                    }
                    if (WriterView.IsDraggingChart)
                    {
                        WriterView.UpdateChartDragPosition(e.X, e.Y, ClientSize.Width, Ribbon);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                if (ModifierKeys == Keys.Control)
                {
                    float zoomStep = (e.Delta > 0) ? 0.10f : -0.10f;
                    if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                    CalcView.ZoomFactor = Math.Max(0.20f, Math.Min(4.00f, CalcView.ZoomFactor + zoomStep));
                    ZoomLevel = CalcView.ZoomFactor;
                    Invalidate();
                    return;
                }
                else
                {
                    if (e.Delta > 0)
                    {
                        if (CalcView.ScrollRow > 1) CalcView.ScrollRow--;
                    }
                    else
                    {
                        CalcView.ScrollRow++;
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                // ONLY scroll/zoom if cursor is inside the slide area (NOT on Ribbon y <= 155, NOT on Status Bar y >= Height - 26)
                if (e.Y <= 155 || e.Y >= ClientSize.Height - 26)
                {
                    return;
                }

                // 0. Khi lăn chuột trong vùng Ghi chú người thuyết trình (Speaker Notes Pane / Notes Page)
                if (SlidesView != null && SlidesView.LastNotesTextRect.Width > 0 && SlidesView.LastNotesTextRect.Contains(e.X, e.Y))
                {
                    SlidesView.NotesScrollY = Math.Max(0, SlidesView.NotesScrollY + ((e.Delta > 0) ? -24 : 24));
                    Invalidate();
                    return;
                }

                // 1. Khi giữ phím Ctrl + Lăn chuột: Luôn Zoom phóng to / thu nhỏ canvas slide theo vị trí chuột (kể cả khi đang Xóa nền, Cắt ảnh, Sửa đối tượng)
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (SlidesView != null)
                    {
                        SlidesView.ZoomAt(e.Location, e.Delta, ClientSize.Width, ClientSize.Height);
                        ZoomLevel = SlidesView.ZoomLevel;
                        Invalidate();
                    }
                    return;
                }

                // 2. Cuộn chuột điều chỉnh kích thước bút vẽ trong Chế độ Xóa nền PowerPoint (khi không giữ Ctrl)
                if (SlidesView != null && SlidesView.IsRemovingBackground)
                {
                    if (e.Delta > 0) SlidesView.BgBrushRadius = Math.Min(100f, SlidesView.BgBrushRadius + 2f);
                    else SlidesView.BgBrushRadius = Math.Max(3f, SlidesView.BgBrushRadius - 2f);
                    Invalidate();
                    return;
                }
                else
                {
                    int scrollDir = (e.Delta > 0) ? 1 : -1; // 1 = Up (Prev), -1 = Down (Next)

                    int leftSidebarW, canvasX, canvasW, contentY, contentH, bottomThumbH;
                    SlidesView.GetEffectiveLayoutMetrics(ClientSize.Width, ClientSize.Height, out leftSidebarW, out canvasX, out canvasW, out contentY, out contentH, out bottomThumbH);

                    // Khi lăn chuột trên thanh trượt đáy (Bottom Thumbnail Strip)
                    if (bottomThumbH > 0 && e.Y >= contentY + contentH)
                    {
                        int totalBottomWidth = (SlidesView.Presentation != null ? SlidesView.Presentation.Slides.Count : 0) * (86 + 10) + 60;
                        int trackW = canvasW - 16 - 130;
                        int maxBottomScroll = Math.Max(0, totalBottomWidth - trackW);
                        int bottomScrollStep = (e.Delta > 0) ? -60 : 60;
                        float newBottom = Math.Max(0, Math.Min(maxBottomScroll, (float)Math.Round(SlidesView.TargetBottomThumbScrollX + bottomScrollStep)));
                        SlidesView.TargetBottomThumbScrollX = newBottom;
                        return;
                    }

                    if (SlidesView.IsElementsDrawerOpen && e.X <= leftSidebarW)
                    {
                        if (SlidesView.ElementsDrawerSelectedTab == 1 && SlidesView.ShapeTaskPane != null)
                        {
                            SlidesView.ShapeTaskPane.HandleMouseWheel(e.Delta);
                            Invalidate();
                            return;
                        }
                        else
                        {
                            int scrollStep = (e.Delta > 0) ? -60 : 60;
                            float newElem = Math.Max(0, (float)Math.Round(SlidesView.TargetElementsScrollY + scrollStep));
                            SlidesView.TargetElementsScrollY = newElem;
                            return;
                        }
                    }
                    else if (e.X <= leftSidebarW && !SlidesView.IsElementsDrawerOpen)
                    {
                        // Scroll Thumbnails Pane (Cuộn danh sách slide thu nhỏ siêu nhẹ, thanh thoát chuẩn PowerPoint)
                        int maxThumbScroll = SlidesView.GetMaxThumbScrollY();
                        int scrollStep = (e.Delta > 0) ? -48 : 48; // Bước cuộn 48px siêu nhẹ, êm ái
                        float newThumb = Math.Max(0, Math.Min(maxThumbScroll, (float)Math.Round(SlidesView.TargetThumbScrollY + scrollStep)));
                        SlidesView.TargetThumbScrollY = newThumb;
                        return;
                    }
                    else
                    {
                        int rightSidebarW = (SlidesView.IsSidebarOpen && SlidesView.SidebarPanel != SidebarPanelType.None && !SlidesView.IsPresenting) ? 280 : 0;
                        if (rightSidebarW > 0 && e.X >= ClientSize.Width - rightSidebarW)
                        {
                            int scrollStep = (e.Delta > 0) ? 60 : -60;
                            SlidesView.SidebarScrollY = Math.Min(0, SlidesView.SidebarScrollY + scrollStep);
                            Invalidate();
                            return;
                        }
                        canvasW = ClientSize.Width - 210 - rightSidebarW;
                        int canvasH = ClientSize.Height - 155 - 26;
                        int minScrollX, maxScrollX, minScrollY, maxScrollY;
                        SlidesView.GetScrollLimits(canvasW, canvasH, out minScrollX, out maxScrollX, out minScrollY, out maxScrollY);

                        bool isZoomed = (SlidesView.ZoomLevel > 1.05f) || (minScrollX < maxScrollX) || (minScrollY < maxScrollY);

                        if (!isZoomed)
                        {
                            // Chuyển slide mượt mà, phản hồi dồn dập chuẩn MS PowerPoint (không bao giờ bị nuốt phím hay khựng)
                            _slideWheelDeltaAccumulator += e.Delta;
                            int notchThreshold = 120; // 1 nấc con lăn tiêu chuẩn

                            if (Math.Abs(_slideWheelDeltaAccumulator) >= notchThreshold)
                            {
                                double elapsedMs = (DateTime.Now - LastSlideWheelTime).TotalMilliseconds;
                                // Giới hạn phản hồi 40ms vừa vặn 60 FPS (cho phép cuộn dồn dập tới 25 slide/giây cực mượt mà không bao giờ lag hay khựng)
                                if (elapsedMs >= 40)
                                {
                                    LastSlideWheelTime = DateTime.Now;
                                    int direction = (_slideWheelDeltaAccumulator < 0) ? -1 : 1;
                                    _slideWheelDeltaAccumulator = 0;

                                    int prevIdx = SlidesView.ActiveSlideIndex;
                                    if (direction < 0 && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                                    {
                                        SlidesView.ActiveSlideIndex++;
                                        SlidesView.SelectedShapeIndex = -1;
                                        SlidesView.CanvasScrollX = 0;
                                        SlidesView.CanvasScrollY = 0;
                                        SlidesView.SmoothCanvasScrollX = 0f;
                                        SlidesView.TargetCanvasScrollX = 0f;
                                        SlidesView.SmoothCanvasScrollY = 0f;
                                        SlidesView.TargetCanvasScrollY = 0f;
                                        SlidesView.EnsureSlideVisibleInThumbnails(SlidesView.ActiveSlideIndex);
                                        if (SlidesView.IsPresenting && SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                                        {
                                            SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, false);
                                        }
                                        Invalidate();
                                        return;
                                    }
                                    else if (direction > 0 && SlidesView.ActiveSlideIndex > 0)
                                    {
                                        SlidesView.ActiveSlideIndex--;
                                        SlidesView.SelectedShapeIndex = -1;
                                        SlidesView.CanvasScrollX = 0;
                                        SlidesView.CanvasScrollY = 0;
                                        SlidesView.SmoothCanvasScrollX = 0f;
                                        SlidesView.TargetCanvasScrollX = 0f;
                                        SlidesView.SmoothCanvasScrollY = 0f;
                                        SlidesView.TargetCanvasScrollY = 0f;
                                        SlidesView.EnsureSlideVisibleInThumbnails(SlidesView.ActiveSlideIndex);
                                        if (SlidesView.IsPresenting && SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                                        {
                                            SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, true);
                                        }
                                        Invalidate();
                                        return;
                                    }
                                }
                            }
                            return;
                        }
                        else
                        {
                            // 2. Chế độ Phóng to (Zoomed In): Cuộn nội dung slide với bước cuộn êm ái
                            int slideScrollStep = (e.Delta > 0) ? -65 : 65;
                            float newTargetScrollY = Math.Max(minScrollY, Math.Min(maxScrollY, (float)Math.Round(SlidesView.TargetCanvasScrollY + slideScrollStep)));

                            if (Math.Abs(newTargetScrollY - SlidesView.TargetCanvasScrollY) > 0.5f)
                            {
                                SlidesView.TargetCanvasScrollY = newTargetScrollY;
                                SlideWheelTickCount = 0;
                                return;
                            }
                            else
                            {
                                // Khi đã cuộn kịch mép trên hoặc dưới của slide phóng to: Lăn tiếp có chủ ý để chuyển slide
                                if ((DateTime.Now - LastSlideWheelTime).TotalMilliseconds > 400)
                                {
                                    SlideWheelTickCount = 0;
                                }
                                LastSlideWheelTime = DateTime.Now;

                                if (scrollDir == LastSlideWheelDirection)
                                {
                                    SlideWheelTickCount++;
                                }
                                else
                                {
                                    LastSlideWheelDirection = scrollDir;
                                    SlideWheelTickCount = 1;
                                }

                                if (SlideWheelTickCount >= 2)
                                {
                                    SlideWheelTickCount = 0;
                                    int prevIdx = SlidesView.ActiveSlideIndex;

                                    if (scrollDir < 0 && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                                    {
                                        SlidesView.ActiveSlideIndex++;
                                        SlidesView.SelectedShapeIndex = -1;
                                        SlidesView.CanvasScrollY = minScrollY;
                                        SlidesView.SmoothCanvasScrollY = minScrollY;
                                        SlidesView.TargetCanvasScrollY = minScrollY;
                                        SlidesView.EnsureSlideVisibleInThumbnails(SlidesView.ActiveSlideIndex);
                                        if (SlidesView.IsPresenting && SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                                        {
                                            SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, false);
                                        }
                                        Invalidate();
                                        return;
                                    }
                                    else if (scrollDir > 0 && SlidesView.ActiveSlideIndex > 0)
                                    {
                                        SlidesView.ActiveSlideIndex--;
                                        SlidesView.SelectedShapeIndex = -1;
                                        SlidesView.CanvasScrollY = maxScrollY;
                                        SlidesView.SmoothCanvasScrollY = maxScrollY;
                                        SlidesView.TargetCanvasScrollY = maxScrollY;
                                        SlidesView.EnsureSlideVisibleInThumbnails(SlidesView.ActiveSlideIndex);
                                        if (SlidesView.IsPresenting && SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                                        {
                                            SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, true);
                                        }
                                        Invalidate();
                                        return;
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                if ((ModifierKeys & Keys.Control) != 0)
                {
                    float deltaZoom = (float)Math.Round(wheelNotches * 0.10f, 2);
                    if (Math.Abs(deltaZoom) < 0.02f) deltaZoom = (e.Delta > 0 ? 0.02f : -0.02f);
                    PdfView.ZoomAt(e.Location, PdfView.ZoomLevel + deltaZoom, ClientSize.Width, ClientSize.Height, 155, 26);
                    ZoomLevel = PdfView.ZoomLevel;
                    Invalidate();
                }
                else if ((ModifierKeys & Keys.Shift) != 0)
                {
                    // Shift + MouseWheel: Cuộn ngang trang PDF (Horizontal Scroll)
                    int sideOffset = PdfView.ShowThumbnailsSidebar ? PdfView.SidebarWidth : 0;
                    int canvasAreaW = Math.Max(100, ClientSize.Width - sideOffset - 14);
                    int targetPage = Math.Max(0, Math.Min(PdfView.Document != null && PdfView.Document.Pages != null ? PdfView.Document.Pages.Count - 1 : 0, PdfView.ActivePageIndex));
                    float renderW = (PdfView.Document != null && PdfView.Document.Pages != null && targetPage < PdfView.Document.Pages.Count)
                        ? PdfView.Document.Pages[targetPage].Width * PdfView.ZoomLevel : 800f;
                    float maxScrollX = Math.Max(0f, renderW - canvasAreaW + 60f);
                    if (maxScrollX > 0)
                    {
                        float hDelta = -wheelNotches * 95f;
                        PdfView.ScrollX = Math.Max(0f, Math.Min(maxScrollX, PdfView.ScrollX + hDelta));
                        Invalidate();
                    }
                }
                else
                {
                    int viewH = ClientSize.Height - 155 - 26;
                    if (PdfView.ShowThumbnailsSidebar && e.X <= PdfView.SidebarWidth)
                    {
                        // Cuộn thanh trang thu nhỏ Thumbnail ở Sidebar bên trái
                        int maxSidebarScroll = PdfView.GetMaxSidebarScrollY(viewH - 36);
                        float sideScrollAmount = -wheelNotches * 90f;
                        PdfView.TargetSidebarScrollY = Math.Max(0, Math.Min(maxSidebarScroll, PdfView.TargetSidebarScrollY + sideScrollAmount));
                    }
                    else
                    {
                        // Cuộn các trang PDF chính mượt mà qua UpdateSmoothScroll
                        int maxScroll = PdfView.GetMaxScrollY(viewH);
                        float wheelStep = -wheelNotches * 110f;
                        PdfView.TargetScrollY = Math.Max(0, Math.Min(maxScroll, PdfView.TargetScrollY + wheelStep));
                        PdfView.UpdateActivePageFromScroll(ClientSize.Width, ClientSize.Height, 155, 26);
                    }
                    Invalidate();
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                int rH = RibbonBar.RibbonHeight;
                if (e.Y <= rH)
                {
                    if (Ribbon.OnMouseWheel(e, ClientSize.Width))
                    {
                        Invalidate();
                        return;
                    }
                }
                PhotoView.HandleMouseWheel(e.X, e.Y, e.Delta, ModifierKeys, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            WriterFloatingMiniToolbarForm.CloseCurrent();

            if (BackstageView != null && BackstageView.IsOpen)
            {
                int topFillH = (Ribbon != null && !Ribbon.StandaloneMode && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : (Ribbon != null && !Ribbon.StandaloneMode ? 36 : 0);
                if (topFillH > 0 && e.Y < topFillH)
                {
                    if (e.Button == MouseButtons.Left && Ribbon != null)
                    {
                        if (!Ribbon.CaptionCloseBounds.IsEmpty && Ribbon.CaptionCloseBounds.Contains(e.X, e.Y))
                        {
                            _isCaptionClosePressed = true;
                            this.Capture = true;
                            Invalidate(Ribbon.CaptionCloseBounds);
                            return;
                        }
                        if (!Ribbon.CaptionMaxBounds.IsEmpty && Ribbon.CaptionMaxBounds.Contains(e.X, e.Y))
                        {
                            _isCaptionMaxPressed = true;
                            this.Capture = true;
                            Invalidate(Ribbon.CaptionMaxBounds);
                            return;
                        }
                        if (!Ribbon.CaptionMinBounds.IsEmpty && Ribbon.CaptionMinBounds.Contains(e.X, e.Y))
                        {
                            _isCaptionMinPressed = true;
                            this.Capture = true;
                            Invalidate(Ribbon.CaptionMinBounds);
                            return;
                        }
                        if (!Ribbon.CaptionTouchModeBounds.IsEmpty && Ribbon.CaptionTouchModeBounds.Contains(e.X, e.Y))
                        {
                            ToggleTouchMode();
                            return;
                        }
                        if (!Ribbon.CaptionPeripheralsBounds.IsEmpty && Ribbon.CaptionPeripheralsBounds.Contains(e.X, e.Y))
                        {
                            OpenPeripheralHubDialog();
                            return;
                        }
                        if (!Ribbon.CaptionMobileConnectBounds.IsEmpty && Ribbon.CaptionMobileConnectBounds.Contains(e.X, e.Y))
                        {
                            OpenMobileConnectDialog();
                            return;
                        }
                        if (!Ribbon.DocTabScrollLeftBounds.IsEmpty && Ribbon.DocTabScrollLeftBounds.Contains(e.X, e.Y))
                        {
                            Ribbon.ScrollDocTabsLeft(SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            Invalidate();
                            return;
                        }
                        if (!Ribbon.DocTabScrollRightBounds.IsEmpty && Ribbon.DocTabScrollRightBounds.Contains(e.X, e.Y))
                        {
                            Ribbon.ScrollDocTabsRight(SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            Invalidate();
                            return;
                        }

                        Ribbon.UpdateHover(e.X, e.Y);
                        if (Ribbon.HandleClick(e.X, e.Y, WriterView, this, SlidesView, CalcView, PdfView, PhotoView))
                        {
                            CloseBackstageView();
                            Invalidate();
                            return;
                        }
                    }
                    return;
                }

                MouseEventArgs adjustedE = (topFillH > 0) ? new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y - topFillH, e.Delta) : e;
                BackstageView.OnMouseDown(adjustedE);
                Invalidate();
                return;
            }

            if (HandlePeripheralToastMouseDown(e.Location))
            {
                Invalidate();
                return;
            }

            if (!SlidesView.IsPresenting && Ribbon.CurrentMode != OfficeAppMode.Home)
            {
                if (HandleInkPaletteMouseDown(e.Location))
                {
                    Invalidate();
                    return;
                }
                if (AppSettings.Instance.TouchModeEnabled && HandleFloatingTouchBarMouseDown(e.Location))
                {
                    Invalidate();
                    return;
                }
            }

            if (WordSuggestions != null && WordSuggestions.IsVisible)
            {
                if (WordSuggestions.Bounds.Contains(e.X, e.Y))
                {
                    if (WordSuggestions.HandleClick(e.X, e.Y))
                    {
                        Invalidate();
                        return;
                    }
                }
                else
                {
                    WordSuggestions.Hide();
                    Invalidate();
                }
            }

            if (NewTabRadialLauncher.IsOpen && NewTabRadialLauncher.HandleMouseDown(e.Location, Ribbon, (mode) => { SwitchAppMode(mode); }, SlidesView, WriterView, CalcView, PdfView))
            {
                PositionAiConfigPanel();
                Invalidate();
                return;
            }
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                ResetCursorBlink();
            }
            if (!SlidesView.IsPresenting)
            {
                // Status Bar Interactions (Y >= height - 26, chỉ áp dụng cho Writer, Calc, Slides)
                if (Ribbon.CurrentMode != OfficeAppMode.Home && e.Y >= ClientSize.Height - 26)
                {
                    int w = ClientSize.Width;

                    if (!_statusTouchToggleBounds.IsEmpty && _statusTouchToggleBounds.Contains(e.X, e.Y))
                    {
                        if (e.Button == MouseButtons.Right)
                        {
                            OpenPeripheralHubDialog();
                        }
                        else
                        {
                            ToggleTouchMode();
                        }
                        return;
                    }

                    // Speaker Notes Toggle Button ("📝 Ghi chú") in Slides Mode
                    if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null && e.X >= w - 344 && e.X <= w - 276)
                    {
                        SlidesView.IsNotesPaneOpen = !SlidesView.IsNotesPaneOpen;
                        if (!SlidesView.IsNotesPaneOpen)
                        {
                            SlidesView.IsEditingSpeakerNotes = false;
                        }
                        Invalidate();
                        return;
                    }

                    // In Slides Mode: 4 View Mode / Presentation Buttons (Normal, Slide Sorter, Reading View, Slide Show)
                    if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                    {
                        int modeX = w - 270;
                        for (int m = 0; m < 4; m++)
                        {
                            if (e.X >= modeX + m * 24 && e.X <= modeX + m * 24 + 22)
                            {
                                if (m == 0)
                                {
                                    SlidesView.ViewMode = ImpressViewMode.Normal;
                                    SlidesView.SelectedShapeIndex = -1;
                                    if (SlidesView.SelectedShapeIndices != null) SlidesView.SelectedShapeIndices.Clear();
                                    SlidesView.ZoomToFit();
                                }
                                else if (m == 1)
                                {
                                    SlidesView.ViewMode = ImpressViewMode.SlideSorter;
                                    SlidesView.SelectedShapeIndex = -1;
                                    if (SlidesView.SelectedShapeIndices != null) SlidesView.SelectedShapeIndices.Clear();
                                }
                                else if (m == 2)
                                {
                                    SlidesView.ViewMode = ImpressViewMode.ReadingView;
                                    SlidesView.SelectedShapeIndex = -1;
                                    if (SlidesView.SelectedShapeIndices != null) SlidesView.SelectedShapeIndices.Clear();
                                    SlidesView.CanvasScrollX = 0;
                                    SlidesView.CanvasScrollY = 0;
                                    SlidesView.TargetCanvasScrollX = 0f;
                                    SlidesView.TargetCanvasScrollY = 0f;
                                    SlidesView.ZoomLevel = 1.0f;
                                }
                                else if (m == 3)
                                {
                                    StartSlideShow(false);
                                }
                                Invalidate();
                                return;
                            }
                        }
                    }
                    else
                    {
                        // View Mode Icons for Writer (Read Mode 0, Print Layout 1, Web Layout 2)
                        int modeX = w - 246;
                        for (int m = 0; m < 3; m++)
                        {
                            if (e.X >= modeX + m * 24 && e.X <= modeX + m * 24 + 22)
                            {
                                ActiveViewMode = m;
                                WriterView.ViewMode = m;
                                if (m == 0)
                                {
                                    WriterView.ScrollY = 0;
                                    WriterView.TargetScrollY = 0;
                                    WriterView.ReadModePageIndex = Math.Max(0, Math.Min(WriterView.TotalPageCount - 1, WriterView.CurrentActivePage - 1));
                                }
                                Invalidate();
                                return;
                            }
                        }
                    }

                    // Zoom Minus (-)
                    if (e.X >= w - 164 && e.X < w - 144)
                    {
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            float step = (SlidesView.ZoomLevel > 1.5f) ? 0.25f : 0.10f;
                            SlidesView.ZoomLevel = Math.Max(0.10f, SlidesView.ZoomLevel - step);
                            ZoomLevel = SlidesView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            Point centerView = new Point(ClientSize.Width / 2, 155 + (ClientSize.Height - 155 - 26) / 2);
                            PdfView.ZoomAt(centerView, PdfView.ZoomLevel - 0.10f, ClientSize.Width, ClientSize.Height, 155, 26);
                            ZoomLevel = PdfView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                        {
                            PhotoView.Zoom = Math.Max(0.10f, PhotoView.Zoom - 0.10f);
                            ZoomLevel = PhotoView.Zoom;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                            CalcView.ZoomFactor = Math.Max(0.20f, CalcView.ZoomFactor - 0.10f);
                            ZoomLevel = CalcView.ZoomFactor;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            Point centerView = new Point(ClientSize.Width / 2, (ClientSize.Height - 155) / 2 + 155);
                            WriterView.ZoomTo(Math.Max(0.20f, WriterView.ZoomLevel - 0.10f), centerView, Ribbon);
                            ZoomLevel = WriterView.ZoomLevel;
                        }
                        Invalidate();
                        return;
                    }

                    // Zoom Plus (+)
                    if (e.X >= w - 68 && e.X < w - 48)
                    {
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            float step = (SlidesView.ZoomLevel >= 1.5f) ? 0.25f : 0.10f;
                            SlidesView.ZoomLevel = Math.Min(4.00f, SlidesView.ZoomLevel + step);
                            ZoomLevel = SlidesView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            Point centerView = new Point(ClientSize.Width / 2, 155 + (ClientSize.Height - 155 - 26) / 2);
                            PdfView.ZoomAt(centerView, PdfView.ZoomLevel + 0.10f, ClientSize.Width, ClientSize.Height, 155, 26);
                            ZoomLevel = PdfView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                        {
                            PhotoView.Zoom = Math.Min(5.00f, PhotoView.Zoom + 0.10f);
                            ZoomLevel = PhotoView.Zoom;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                            CalcView.ZoomFactor = Math.Min(4.00f, CalcView.ZoomFactor + 0.10f);
                            ZoomLevel = CalcView.ZoomFactor;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            Point centerView = new Point(ClientSize.Width / 2, (ClientSize.Height - 155) / 2 + 155);
                            WriterView.ZoomTo(Math.Min(2.50f, WriterView.ZoomLevel + 0.10f), centerView, Ribbon);
                            ZoomLevel = WriterView.ZoomLevel;
                        }
                        Invalidate();
                        return;
                    }

                    // Zoom Slider Track Bar (70px) - Enable Dragging!
                    if (e.X >= w - 146 && e.X <= w - 68)
                    {
                        IsDraggingZoomSlider = true;
                        UpdateZoomFromMouseX(e.X);
                        return;
                    }

                    // Zoom Percentage Reset (100%)
                    if (e.X >= w - 46 && e.X <= w - 4)
                    {
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            SlidesView.ZoomLevel = 1.0f; // 100% standard fit for slides
                            SlidesView.CanvasScrollX = 0;
                            SlidesView.CanvasScrollY = 0;
                            ZoomLevel = 1.0f;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            PdfView.ZoomActualSize(ClientSize.Width, ClientSize.Height);
                            ZoomLevel = PdfView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                        {
                            PhotoView.Zoom = 1.0f;
                            PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                            ZoomLevel = 1.0f;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            CalcView.ZoomFactor = 1.0f;
                            ZoomLevel = 1.0f;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            Point centerView = new Point(ClientSize.Width / 2, (ClientSize.Height - 155) / 2 + 155);
                            WriterView.ZoomTo(1.0f, centerView, Ribbon);
                            ZoomLevel = 1.0f;
                        }
                        Invalidate();
                        return;
                    }
                }

                int ribbonH = (Ribbon.CurrentMode == OfficeAppMode.Home) ? 64 : 155;
                if (e.Y <= ribbonH)
                {
                    int rTop = (Ribbon != null && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36;
                    if (e.Y <= rTop && e.Button == MouseButtons.Left && Ribbon != null)
                    {
                        if (!Ribbon.CaptionCloseBounds.IsEmpty && Ribbon.CaptionCloseBounds.Contains(e.X, e.Y))
                        {
                            _isCaptionClosePressed = true;
                            this.Capture = true;
                            Invalidate(Ribbon.CaptionCloseBounds);
                            return;
                        }
                        if (!Ribbon.CaptionMaxBounds.IsEmpty && Ribbon.CaptionMaxBounds.Contains(e.X, e.Y))
                        {
                            _isCaptionMaxPressed = true;
                            this.Capture = true;
                            Invalidate(Ribbon.CaptionMaxBounds);
                            return;
                        }
                        if (!Ribbon.CaptionMinBounds.IsEmpty && Ribbon.CaptionMinBounds.Contains(e.X, e.Y))
                        {
                            _isCaptionMinPressed = true;
                            this.Capture = true;
                            Invalidate(Ribbon.CaptionMinBounds);
                            return;
                        }
                        if (!Ribbon.CaptionTouchModeBounds.IsEmpty && Ribbon.CaptionTouchModeBounds.Contains(e.X, e.Y))
                        {
                            ToggleTouchMode();
                            return;
                        }
                        if (!Ribbon.CaptionPeripheralsBounds.IsEmpty && Ribbon.CaptionPeripheralsBounds.Contains(e.X, e.Y))
                        {
                            OpenPeripheralHubDialog();
                            return;
                        }
                        if (!Ribbon.CaptionMobileConnectBounds.IsEmpty && Ribbon.CaptionMobileConnectBounds.Contains(e.X, e.Y))
                        {
                            OpenMobileConnectDialog();
                            return;
                        }
                        if (!Ribbon.DocTabScrollLeftBounds.IsEmpty && Ribbon.DocTabScrollLeftBounds.Contains(e.X, e.Y))
                        {
                            Ribbon.ScrollDocTabsLeft(SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            Invalidate();
                            return;
                        }
                        if (!Ribbon.DocTabScrollRightBounds.IsEmpty && Ribbon.DocTabScrollRightBounds.Contains(e.X, e.Y))
                        {
                            Ribbon.ScrollDocTabsRight(SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            Invalidate();
                            return;
                        }

                        if (!Ribbon.StandaloneMode)
                        {
                            Ribbon.StartTabDrag(e.X, e.Y, ClientSize.Width);
                        }
                    }

                    Ribbon.UpdateHover(e.X, e.Y);
                    bool handled = Ribbon.HandleClick(e.X, e.Y, WriterView, this, SlidesView, CalcView, PdfView, PhotoView);
                    SetModeIcon(Ribbon.CurrentMode);
                    PositionAiConfigPanel();

                    if (!handled && e.Y <= rTop && e.Button == MouseButtons.Left && !Ribbon.IsDraggingTab && Ribbon.DraggingTabIndex < 1)
                    {
                        ReleaseCapture();
                        SendMessage(this.Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
                        return;
                    }

                    Invalidate();
                    return;
                }

                // 1-Finger Touch Pan Mode (Cuộn tay Cảm ứng khi bật TouchPanActive)
                if (e.Button == MouseButtons.Left && AppSettings.Instance.TouchModeEnabled && AppSettings.Instance.TouchPanActive && Ribbon.ActiveDrawTool == 0)
                {
                    _isTouchPanningDrag = true;
                    _touchPanLastPt = e.Location;
                    _touchPanStartPt = e.Location;
                    _touchPanAccumDx = 0;
                    _touchPanAccumDy = 0;
                    Cursor = Cursors.SizeAll;
                    return;
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Home)
                {
                    if (e.Button == MouseButtons.Left && HomeView.HandleScrollBarMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                    {
                        Invalidate();
                        return;
                    }

                    string docTitle;
                    string openFilePath;
                    OfficeAppMode? clickedMode = HomeView.HandleClick(e.X, e.Y, ClientSize.Width, ClientSize.Height, out docTitle, out openFilePath);
                    if (clickedMode.HasValue)
                    {
                        Ribbon.AddDocTab(docTitle, clickedMode.Value, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                        SetModeIcon(clickedMode.Value);
                        int activeIdx = Ribbon.ActiveDocIndex;
                        if (activeIdx >= 0 && activeIdx < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[activeIdx].DraftFile = ""; // Clear draft file pointer for fresh tab
                        }

                        if (!string.IsNullOrEmpty(openFilePath))
                        {
                            if (System.IO.File.Exists(openFilePath))
                            {
                                if (activeIdx >= 0 && activeIdx < Ribbon.DocTabs.Count)
                                {
                                    Ribbon.DocTabs[activeIdx].FilePath = openFilePath;
                                }
                                if (clickedMode.Value == OfficeAppMode.Writer)
                                {
                                    bool ok = WriterView.LoadDocumentFile(openFilePath, Ribbon);
                                    if (!ok)
                                    {
                                        MessageBox.Show("Không thể nạp nội dung của tệp Word:\n" + openFilePath, "Thông báo lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                                else if (clickedMode.Value == OfficeAppMode.Slides && SlidesView != null)
                                {
                                    SlidesView.Presentation = ZenQOffice.Format.PptxHandler.LoadPptx(openFilePath);
                                    SlidesView.ActiveSlideIndex = 0;
                                    SlidesView.ZoomLevel = 1.0f;
                                    SlidesView.CanvasScrollX = 0;
                                    SlidesView.CanvasScrollY = 0;
                                    SlidesView.TargetCanvasScrollX = 0f;
                                    SlidesView.TargetCanvasScrollY = 0f;
                                    SlidesView.SmoothCanvasScrollX = 0f;
                                    SlidesView.SelectedShapeIndex = -1;
                                    SlidesView.ThumbScrollY = 0;
                                    SlidesView.TargetThumbScrollY = 0f;
                                    SlidesView.SmoothThumbScrollY = 0f;
                                    SlidesView.BottomThumbScrollX = 0;
                                    SlidesView.TargetBottomThumbScrollX = 0f;
                                    SlidesView.SmoothBottomThumbScrollX = 0f;
                                    SlidesView.IsElementsDrawerOpen = false;
                                    SlidesView.IsSidebarOpen = false;
                                    SlidesView.SidebarPanel = SidebarPanelType.None;
                                    D2DGraphics.ClearFontCache();
                                    if (activeIdx >= 0 && activeIdx < Ribbon.DocTabs.Count)
                                    {
                                        Ribbon.DocTabs[activeIdx].PresentationData = SlidesView.Presentation;
                                    }
                                    Ribbon.SaveTabState(activeIdx, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                    Ribbon.RefreshCustomFonts();
                                }
                                else if (clickedMode.Value == OfficeAppMode.Calc && CalcView != null)
                                {
                                    ZenQOffice.Format.XlsxHandler.LoadXlsxOrCsv(openFilePath, CalcView);
                                    CalcView.ActiveSheetIndex = 0;
                                    CalcView.ScrollRow = 1;
                                    CalcView.ScrollCol = 1;
                                    CalcView.SelectedRow = 1;
                                    CalcView.SelectedCol = 1;
                                }
                                else if (clickedMode.Value == OfficeAppMode.Pdf && PdfView != null)
                                {
                                    PdfView.Document = ZenQOffice.Format.PdfHandler.LoadPdf(openFilePath);
                                    PdfView.ActivePageIndex = 0;
                                    PdfView.ScrollY = 0f;
                                    PdfView.ZoomLevel = 1.0f;
                                }
                                else if (clickedMode.Value == OfficeAppMode.Photo && PhotoView != null)
                                {
                                    PhotoView.Document = PhotoDocument.LoadFromFile(openFilePath);
                                    PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Không tìm thấy tệp hoặc tệp đã bị di chuyển:\n" + openFilePath, "Không tìm thấy tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                Ribbon.CloseDocTab(activeIdx, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                Invalidate();
                                return;
                            }
                        }
                        else
                        {
                            // 100% BLANK NEW DOCUMENT
                            if (activeIdx >= 0 && activeIdx < Ribbon.DocTabs.Count)
                            {
                                Ribbon.DocTabs[activeIdx].FilePath = "";
                            }
                            if (clickedMode.Value == OfficeAppMode.Writer)
                            {
                                WriterView.CurrentFilePath = "";
                                WriterView.PieceTable = new PieceTable("");
                                WriterView.FormattedRuns.Clear();
                                WriterView.ParagraphAlignments.Clear();
                                WriterView.ParagraphBorders.Clear();
                                WriterView.TextBorders.Clear();
                                WriterView.ParagraphBottomBorders.Clear();
                                WriterView.ParagraphLineSpacings.Clear();
                                WriterView.ParagraphSpacingsBefore.Clear();
                                WriterView.ParagraphSpacingsAfter.Clear();
                                WriterView.ParagraphFirstLineIndents.Clear();
                                WriterView.ParagraphLeftIndents.Clear();
                                WriterView.ParagraphRightIndents.Clear();
                                WriterView.ParagraphHangingIndents.Clear();
                                WriterView.DocumentShapes.Clear();
                                WriterView.DocumentCharts.Clear();
                                WriterView.DocumentTables.Clear();
                                WriterView.HeaderText = "";
                                WriterView.FooterText = "";
                                WriterView.CurrentHeaderPreset = HeaderFooterPreset.None;
                                WriterView.CurrentFooterPreset = HeaderFooterPreset.None;
                                WriterView.IsEditingHeaderFooter = false;
                                WriterView.HeaderTitleText = "";
                                WriterView.ThreeColHeaderLeft = "";
                                WriterView.ThreeColHeaderCenter = "";
                                WriterView.ThreeColHeaderRight = "";
                                WriterView.ThreeColFooterLeft = "";
                                WriterView.ThreeColFooterCenter = "";
                                WriterView.ThreeColFooterRight = "";
                                WriterView.ShowPageNumbers = false;
                                WriterView.DifferentOddEvenPages = false;
                                WriterView.DifferentFirstPage = false;
                                WriterView.CursorPosition = 0;
                                WriterView.ScrollY = 0;
                                WriterView.ClearUndoRedo();
                            }
                            else if (clickedMode.Value == OfficeAppMode.Slides && SlidesView != null)
                            {
                                SlidesView.Presentation = new PresentationRoot();
                                SlidesView.Presentation.Slides.Clear();
                                SlideNode defSlide = new SlideNode
                                {
                                    Title = "Slide 1",
                                    Layout = SlideLayoutType.Blank,
                                    BgColor = 0xFFFFFF,
                                    Shapes = new List<ShapeNode>(),
                                    Transition = TransitionType.None,
                                    TransitionDuration = 0f
                                };
                                SlidesView.Presentation.Slides.Add(defSlide);
                                SlidesView.ActiveSlideIndex = 0;
                            }
                            else if (clickedMode.Value == OfficeAppMode.Calc && CalcView != null)
                            {
                                CalcView.PopulateSampleData(docTitle);
                            }
                            else if (clickedMode.Value == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.Document = new PdfDocument(docTitle);
                            }
                            else if (clickedMode.Value == OfficeAppMode.Photo && PhotoView != null)
                            {
                                PhotoView.Document = new PhotoDocument(1920, 1080, docTitle);
                                PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                            }
                        }
                        if (activeIdx >= 0 && activeIdx < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[activeIdx].IsModified = false;
                        }
                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                    }
                    PositionAiConfigPanel();
                    Invalidate();
                    return;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    if (WriterView.HandleScrollBarMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height, Ribbon))
                    {
                        Invalidate();
                        return;
                    }
                    if (WriterView.HandleHScrollBarMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height, Ribbon))
                    {
                        Invalidate();
                        return;
                    }

                    if (WriterView.ShowSelectionPane && e.X >= ClientSize.Width - 260 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        if (WriterView.HandleSelectionPaneMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                        {
                            Invalidate();
                            return;
                        }
                    }

                    if (WriterView.ShowNavigationPane && e.X <= 280 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        if (WriterView.HandleNavMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                        {
                            Invalidate();
                            return;
                        }
                    }
                    else if (WriterView.ShowNavigationPane && e.X > 280)
                    {
                        WriterView.NavSearchFocused = false;
                    }

                    if (e.Button == MouseButtons.Right)
                    {
                        WriterView.HandleRightClick(e.X, e.Y, this, Ribbon);
                        Invalidate();
                        return;
                    }

                    if (e.Clicks >= 3)
                    {
                        using (Graphics g = CreateGraphics())
                        {
                            WriterView.HandleTripleClick(e.X, e.Y, ClientSize.Width, g, Ribbon);
                        }
                        Invalidate();
                        return;
                    }
                    else if (e.Clicks == 2)
                    {
                        using (Graphics g = CreateGraphics())
                        {
                            WriterView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, g, Ribbon);
                        }
                        Invalidate();
                        return;
                    }

                    try
                    {
                        using (Graphics g = CreateGraphics())
                        {
                            WriterView.HandleMouseDown(e.X, e.Y, ClientSize.Width, g, Ribbon);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("WriterView HandleMouseDown error: " + ex.Message);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        CalcView.HandleRightClick(e.X, e.Y, this, Ribbon);
                        Invalidate();
                        return;
                    }
                    bool handled = CalcView.HandleMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button, this);
                    if (!handled)
                    {
                        CalcView.HandleClick(e.X, e.Y, ClientSize.Height);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleMouseDown(e.Location, ClientSize.Width, ClientSize.Height, e.Button, this);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button, this);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    PhotoView.HandleMouseDown(e.X, e.Y, e.Button, ModifierKeys, ClientSize.Width, ClientSize.Height);
                }
                Invalidate();
            }
            else
            {
                SlidesView.HandleMouseDown(e.Location, ClientSize.Width, ClientSize.Height, e.Button, this);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (BackstageView != null && BackstageView.IsOpen)
            {
                int topFillH = (Ribbon != null && !Ribbon.StandaloneMode && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : (Ribbon != null && !Ribbon.StandaloneMode ? 36 : 0);
                if (topFillH > 0 && e.Y < topFillH)
                {
                    Ribbon.UpdateHover(e.X, e.Y);
                    Cursor = Cursors.Default;
                    Invalidate();
                    return;
                }

                MouseEventArgs adjustedE = (topFillH > 0) ? new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y - topFillH, e.Delta) : e;
                BackstageView.OnMouseMove(adjustedE);
                Cursor = BackstageView.IsHoverHand ? Cursors.Hand : Cursors.Default;
                Invalidate();
                return;
            }

            if (WordSuggestions != null && WordSuggestions.IsVisible)
            {
                if (WordSuggestions.HandleMouseMove(e.X, e.Y))
                {
                    Invalidate();
                }
            }

            if (NewTabRadialLauncher.IsOpen)
            {
                if (NewTabRadialLauncher.HandleMouseMove(e.Location))
                {
                    Invalidate();
                    return;
                }
            }

            if (e.Button == MouseButtons.Left && (Ribbon.IsDraggingTab || Ribbon.DraggingTabIndex >= 1))
            {
                Ribbon.UpdateTabDrag(e.X, ClientSize.Width);
                if (Ribbon.IsDraggingTab)
                {
                    Invalidate(new Rectangle(0, 0, ClientSize.Width, 36));
                    return;
                }
            }

            if (_isTouchPanningDrag && e.Button == MouseButtons.Left)
            {
                int dx = e.X - _touchPanLastPt.X;
                int dy = e.Y - _touchPanLastPt.Y;
                _touchPanLastPt = e.Location;
                _touchPanAccumDx += dx;
                _touchPanAccumDy += dy;
                PerformTouchPanDelta(dx, dy, _touchPanStartPt);
                Cursor = Cursors.SizeAll;
                Invalidate();
                return;
            }

            if (!_inkPaletteBounds.IsEmpty && _inkPaletteBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                Cursor.Current = Cursors.Hand;
                Invalidate(_inkPaletteBounds);
                return;
            }

            if (AppSettings.Instance.TouchModeEnabled && !_touchBarBounds.IsEmpty && _touchBarBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                Cursor.Current = Cursors.Hand;
                Invalidate(_touchBarBounds);
                return;
            }

            if (IsDraggingZoomSlider && e.Button == MouseButtons.Left)
            {
                UpdateZoomFromMouseX(e.X);
                Invalidate();
                return;
            }

            bool hoverMoved = (Ribbon.HoverX != e.X || Ribbon.HoverY != e.Y);
            if (hoverMoved)
            {
                Ribbon.UpdateHover(e.X, e.Y);
                if (e.Y <= 155)
                {
                    Cursor = Cursors.Default;
                    Cursor.Current = Cursors.Default;
                    Invalidate(new Rectangle(0, 0, ClientSize.Width, 155));
                }
            }

            if (e.Y <= 155 || e.Y >= ClientSize.Height - 25)
            {
                Cursor = Cursors.Default;
                Cursor.Current = Cursors.Default;

                if (Ribbon != null)
                {
                    if ((!Ribbon.CaptionCloseBounds.IsEmpty && Ribbon.CaptionCloseBounds.Contains(e.Location)) ||
                        (!Ribbon.CaptionMaxBounds.IsEmpty && Ribbon.CaptionMaxBounds.Contains(e.Location)) ||
                        (!Ribbon.CaptionMinBounds.IsEmpty && Ribbon.CaptionMinBounds.Contains(e.Location)) ||
                        (!Ribbon.CaptionTouchModeBounds.IsEmpty && Ribbon.CaptionTouchModeBounds.Contains(e.Location)) ||
                        (!Ribbon.CaptionPeripheralsBounds.IsEmpty && Ribbon.CaptionPeripheralsBounds.Contains(e.Location)) ||
                        (!Ribbon.CaptionMobileConnectBounds.IsEmpty && Ribbon.CaptionMobileConnectBounds.Contains(e.Location)) ||
                        (!Ribbon.DocTabScrollLeftBounds.IsEmpty && Ribbon.DocTabScrollLeftBounds.Contains(e.Location)) ||
                        (!Ribbon.DocTabScrollRightBounds.IsEmpty && Ribbon.DocTabScrollRightBounds.Contains(e.Location)) ||
                        (!Ribbon.DocTabPlusBounds.IsEmpty && Ribbon.DocTabPlusBounds.Contains(e.Location)))
                    {
                        Cursor = Cursors.Hand;
                        Cursor.Current = Cursors.Hand;
                    }
                    else if (Ribbon.DocTabBounds != null)
                    {
                        for (int t = 0; t < Ribbon.DocTabBounds.Count; t++)
                        {
                            if (Ribbon.DocTabBounds[t].Contains(e.Location))
                            {
                                Cursor = Cursors.Hand;
                                Cursor.Current = Cursors.Hand;
                                break;
                            }
                        }
                    }
                }

                if (e.Y <= 155 && Ribbon != null && Ribbon.CurrentMode != OfficeAppMode.Home)
                {
                    if ((!Ribbon.RibbonToggleStyleBounds.IsEmpty && Ribbon.RibbonToggleStyleBounds.Contains(e.Location)) ||
                        (!Ribbon.RibbonFileBtnBounds.IsEmpty && Ribbon.RibbonFileBtnBounds.Contains(e.Location)))
                    {
                        Cursor = Cursors.Hand;
                        Cursor.Current = Cursors.Hand;
                    }
                }

                if (e.Y >= ClientSize.Height - 25 && Ribbon != null && Ribbon.CurrentMode != OfficeAppMode.Home)
                {
                    int w = ClientSize.Width;
                    if (!_statusTouchToggleBounds.IsEmpty && _statusTouchToggleBounds.Contains(e.Location))
                    {
                        Cursor = Cursors.Hand;
                        Cursor.Current = Cursors.Hand;
                    }
                    else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                    {
                        if ((e.X >= w - 344 && e.X <= w - 276) || (e.X >= w - 270 && e.X <= w - 174) || (e.X >= w - 164 && e.X <= w - 35))
                        {
                            Cursor = Cursors.Hand;
                            Cursor.Current = Cursors.Hand;
                        }
                    }
                    else
                    {
                        if ((e.X >= w - 246 && e.X <= w - 174) || (e.X >= w - 164 && e.X <= w - 35))
                        {
                            Cursor = Cursors.Hand;
                            Cursor.Current = Cursors.Hand;
                        }
                    }
                }
            }

            if (!SlidesView.IsPresenting)
            {
                if (Ribbon.CurrentMode == OfficeAppMode.Home)
                {
                    if (HomeView.HandleScrollBarMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                    {
                        Cursor = Cursors.Default;
                        Cursor.Current = Cursors.Default;
                        Invalidate();
                        return;
                    }
                    if (HomeView.HandleMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                    {
                        Invalidate();
                    }
                    if (HomeView.IsHoverHand || HomeView.HoverTabIndex >= 0 || HomeView.HoverCardIndex >= 0 || HomeView.HoverRowIndex >= 0 || HomeView.HoverHelpBtn)
                    {
                        Cursor = Cursors.Hand;
                        Cursor.Current = Cursors.Hand;
                    }
                    else if (e.Y > 64)
                    {
                        Cursor = Cursors.Default;
                        Cursor.Current = Cursors.Default;
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    if (WriterView.HandleScrollBarMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height, Ribbon))
                    {
                        this.Cursor = Cursor.Current;
                        Invalidate();
                        return;
                    }
                    if (WriterView.HandleHScrollBarMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height, Ribbon))
                    {
                        this.Cursor = Cursor.Current;
                        Invalidate();
                        return;
                    }

                    if (WriterView.ShowSelectionPane && e.X >= ClientSize.Width - 260 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        WriterView.HandleSelectionPaneMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                        Invalidate();
                    }

                    if (WriterView.ShowNavigationPane && e.X <= 280 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        Cursor = Cursors.Default;
                        Cursor.Current = Cursors.Default;
                        return;
                    }

                    try
                    {
                        using (Graphics g = CreateGraphics())
                        {
                            WriterView.HandleMouseMove(e.X, e.Y, ClientSize.Width, g, Ribbon);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("WriterView HandleMouseMove error: " + ex.Message);
                    }
                    this.Cursor = Cursor.Current;
                    if (e.Button == MouseButtons.Left)
                    {
                        Invalidate();
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    CalcView.HandleMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = CalcView.GetCursorForPosition(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleMouseMove(e.Location, ClientSize.Width, ClientSize.Height);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = SlidesView.GetCursorForPosition(e.Location, ClientSize.Width, ClientSize.Height);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = PdfView.GetCursorAtPoint(e.X, e.Y, ClientSize.Width, ClientSize.Height, 155);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    PhotoView.HandleMouseMove(e.X, e.Y, e.Button, ModifierKeys, ClientSize.Width, ClientSize.Height);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = PhotoView.GetCursorAtPoint(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    else
                    {
                        Cursor = Cursors.Default;
                        Cursor.Current = Cursors.Default;
                    }
                    Invalidate();
                }
            }
            else
            {
                SlidesView.HandleMouseMove(e.Location, ClientSize.Width, ClientSize.Height);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left)
            {
                bool wasClose = _isCaptionClosePressed;
                bool wasMax = _isCaptionMaxPressed;
                bool wasMin = _isCaptionMinPressed;

                _isCaptionClosePressed = false;
                _isCaptionMaxPressed = false;
                _isCaptionMinPressed = false;

                if (this.Capture)
                {
                    this.Capture = false;
                }

                if (Ribbon != null)
                {
                    if (wasClose && !Ribbon.CaptionCloseBounds.IsEmpty && Ribbon.CaptionCloseBounds.Contains(e.X, e.Y))
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            this.Close();
                        }));
                        return;
                    }
                    if (wasMax && !Ribbon.CaptionMaxBounds.IsEmpty && Ribbon.CaptionMaxBounds.Contains(e.X, e.Y))
                    {
                        this.WindowState = (this.WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized;
                        return;
                    }
                    if (wasMin && !Ribbon.CaptionMinBounds.IsEmpty && Ribbon.CaptionMinBounds.Contains(e.X, e.Y))
                    {
                        this.WindowState = FormWindowState.Minimized;
                        return;
                    }
                }
            }
            IsDraggingZoomSlider = false;
            if (_isTouchPanningDrag)
            {
                _isTouchPanningDrag = false;
                PerformTouchPanEnd(_touchPanAccumDx, _touchPanAccumDy, _touchPanStartPt);
                Cursor = Cursors.Default;
                Invalidate();
                return;
            }
            if (Ribbon != null)
            {
                Ribbon.UpdateHover(e.X, e.Y);
            }

            if (Ribbon.IsDraggingTab)
            {
                Ribbon.FinishTabDrag(ClientSize.Width);
                Invalidate();
                return;
            }
            else if (Ribbon.DraggingTabIndex >= 1)
            {
                Ribbon.CancelTabDrag();
            }

            if (SlidesView != null && SlidesView.IsPresenting)
            {
                SlidesView.HandleMouseUp(e.Location, ClientSize.Width, ClientSize.Height);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
                return;
            }

            if (Ribbon.CurrentMode == OfficeAppMode.Home)
            {
                HomeView.HandleScrollBarMouseUp();
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                WriterView.HandleScrollBarMouseUp();
                WriterView.HandleHScrollBarMouseUp();
                try
                {
                    using (Graphics g = CreateGraphics())
                    {
                        WriterView.HandleMouseUp(e.X, e.Y, ClientSize.Width, g, Ribbon);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("WriterView HandleMouseUp error: " + ex.Message);
                }
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.HandleMouseUp(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                SlidesView.HandleMouseUp(e.Location, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.HandleMouseUp(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                PhotoView.HandleMouseUp(e.X, e.Y, e.Button, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (SlidesView != null && SlidesView.IsPresenting) return;
            int rTop = (Ribbon != null && Ribbon.RibbonTopHeight > 0) ? Ribbon.RibbonTopHeight : 36;
            if (e.Y <= rTop && e.Button == MouseButtons.Left && Ribbon != null)
            {
                if (!Ribbon.DocTabScrollLeftBounds.IsEmpty && Ribbon.DocTabScrollLeftBounds.Contains(e.X, e.Y))
                {
                    Ribbon.ScrollDocTabsLeft(SlidesView, WriterView, CalcView, PdfView, PhotoView);
                    Invalidate();
                    return;
                }
                if (!Ribbon.DocTabScrollRightBounds.IsEmpty && Ribbon.DocTabScrollRightBounds.Contains(e.X, e.Y))
                {
                    Ribbon.ScrollDocTabsRight(SlidesView, WriterView, CalcView, PdfView, PhotoView);
                    Invalidate();
                    return;
                }

                bool isOverControl = false;
                if ((!Ribbon.CaptionMinBounds.IsEmpty && Ribbon.CaptionMinBounds.Contains(e.X, e.Y)) ||
                    (!Ribbon.CaptionMaxBounds.IsEmpty && Ribbon.CaptionMaxBounds.Contains(e.X, e.Y)) ||
                    (!Ribbon.CaptionCloseBounds.IsEmpty && Ribbon.CaptionCloseBounds.Contains(e.X, e.Y)) ||
                    (!Ribbon.CaptionTouchModeBounds.IsEmpty && Ribbon.CaptionTouchModeBounds.Contains(e.X, e.Y)) ||
                    (!Ribbon.CaptionPeripheralsBounds.IsEmpty && Ribbon.CaptionPeripheralsBounds.Contains(e.X, e.Y)) ||
                    (!Ribbon.CaptionMobileConnectBounds.IsEmpty && Ribbon.CaptionMobileConnectBounds.Contains(e.X, e.Y)))
                {
                    isOverControl = true;
                }
                else if (!Ribbon.DocTabPlusBounds.IsEmpty && Ribbon.DocTabPlusBounds.Contains(e.X, e.Y))
                {
                    isOverControl = true;
                }
                else if (Ribbon.DocTabBounds != null)
                {
                    for (int i = 0; i < Ribbon.DocTabBounds.Count; i++)
                    {
                        if (Ribbon.DocTabBounds[i].Contains(e.X, e.Y)) { isOverControl = true; break; }
                    }
                }
                if (Ribbon.StandaloneMode)
                {
                    if ((!Ribbon.QuickSaveBounds.IsEmpty && Ribbon.QuickSaveBounds.Contains(e.X, e.Y)) ||
                        (!Ribbon.QuickUndoBounds.IsEmpty && Ribbon.QuickUndoBounds.Contains(e.X, e.Y)) ||
                        (!Ribbon.QuickRedoBounds.IsEmpty && Ribbon.QuickRedoBounds.Contains(e.X, e.Y)))
                    {
                        isOverControl = true;
                    }
                }

                if (!isOverControl)
                {
                    this.WindowState = (this.WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized;
                    return;
                }
            }

            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                using (Graphics g = CreateGraphics())
                {
                    WriterView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, g, Ribbon);
                }
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                SlidesView.HandleDoubleClick(e.Location, ClientSize.Width, ClientSize.Height, this);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                PhotoView.HandleDoubleClick(e.X, e.Y, e.Button, ModifierKeys, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (Ribbon != null && (Ribbon.HoverX != -1 || Ribbon.HoverY != -1))
            {
                Ribbon.UpdateHover(-1, -1);
                Invalidate(new Rectangle(0, 0, ClientSize.Width, 155));
            }
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
            {
                drgevent.Effect = DragDropEffects.Copy;
            }
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])drgevent.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string f = files[0];
                    string ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                    if (Ribbon != null && Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null &&
                        (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp" || ext == ".gif" || ext == ".svg" || ext == ".tiff"))
                    {
                        PhotoView.InsertImageFromFile(f, this);
                        Invalidate();
                    }
                    else
                    {
                        OpenOfficeFileFromPeripheral(f);
                        Invalidate();
                    }
                }
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            WriterFloatingMiniToolbarForm.CloseCurrent();

            if (BackstageView != null && BackstageView.IsOpen)
            {
                BackstageView.OnKeyPress(e);
                Invalidate();
                return;
            }
            if (SlidesView != null && SlidesView.IsPresenting && SlidesView.IsEditingSpeakerNotes)
            {
                if (char.IsControl(e.KeyChar) && e.KeyChar != '\r' && e.KeyChar != '\b')
                {
                    return;
                }
                SlidesView.HandleKeyPress(e.KeyChar);
                MarkActiveTabModified();
                Invalidate();
                return;
            }
            if (!SlidesView.IsPresenting)
            {
                if (char.IsControl(e.KeyChar) && e.KeyChar != '\r' && e.KeyChar != '\b')
                {
                    return;
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    WriterView.CursorVisible = true;
                    if (WriterView.ShowNavigationPane && WriterView.NavSearchFocused)
                    {
                        if (e.KeyChar == '\b')
                        {
                            if (WriterView.NavSearchQuery.Length > 0)
                            {
                                WriterView.NavSearchQuery = WriterView.NavSearchQuery.Substring(0, WriterView.NavSearchQuery.Length - 1);
                                WriterView.NavActiveTab = 2;
                                WriterView.PerformNavSearch();
                            }
                        }
                        else if (e.KeyChar == '\r' || e.KeyChar == '\n')
                        {
                            WriterView.SelectNextNavMatch();
                        }
                        else if (!char.IsControl(e.KeyChar))
                        {
                            WriterView.NavSearchQuery += e.KeyChar;
                            WriterView.NavActiveTab = 2;
                            WriterView.PerformNavSearch();
                        }
                        Invalidate();
                        return;
                    }

                    if (WriterView.IsEditingHeaderFooter)
                    {
                        if (e.KeyChar == '\b')
                        {
                            if (WriterView.IsEditingHeaderMode)
                            {
                                if (WriterView.IsHeaderTextSelected)
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0) WriterView.ThreeColHeaderLeft = "";
                                        else if (WriterView.ActiveThreeColIndex == 1) WriterView.ThreeColHeaderCenter = "";
                                        else if (WriterView.ActiveThreeColIndex == 2) WriterView.ThreeColHeaderRight = "";
                                    }
                                    else
                                    {
                                        WriterView.HeaderTitleText = "";
                                        WriterView.HeaderText = "";
                                    }
                                    WriterView.IsHeaderTextSelected = false;
                                }
                                else
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0)
                                        {
                                            if (WriterView.ThreeColHeaderLeft == "[Nhập tại đây]") WriterView.ThreeColHeaderLeft = "";
                                            else if (WriterView.ThreeColHeaderLeft.Length > 0) WriterView.ThreeColHeaderLeft = WriterView.ThreeColHeaderLeft.Substring(0, WriterView.ThreeColHeaderLeft.Length - 1);
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 1)
                                        {
                                            if (WriterView.ThreeColHeaderCenter == "[Nhập tại đây]") WriterView.ThreeColHeaderCenter = "";
                                            else if (WriterView.ThreeColHeaderCenter.Length > 0) WriterView.ThreeColHeaderCenter = WriterView.ThreeColHeaderCenter.Substring(0, WriterView.ThreeColHeaderCenter.Length - 1);
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 2)
                                        {
                                            if (WriterView.ThreeColHeaderRight == "[Nhập tại đây]") WriterView.ThreeColHeaderRight = "";
                                            else if (WriterView.ThreeColHeaderRight.Length > 0) WriterView.ThreeColHeaderRight = WriterView.ThreeColHeaderRight.Substring(0, WriterView.ThreeColHeaderRight.Length - 1);
                                        }
                                    }
                                    else
                                    {
                                        string curT = WriterView.HeaderTitleText;
                                        if (curT == "TIÊU ĐỀ ĐẦU TRANG" || curT == "[Tiêu đề tài liệu]" || curT == "[Nhập tại đây]")
                                        {
                                            WriterView.HeaderTitleText = "";
                                            WriterView.HeaderText = "";
                                        }
                                        else if (WriterView.HeaderTitleText.Length > 0)
                                        {
                                            WriterView.HeaderTitleText = WriterView.HeaderTitleText.Substring(0, WriterView.HeaderTitleText.Length - 1);
                                            WriterView.HeaderText = WriterView.HeaderTitleText;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (WriterView.FooterText.Length > 0) WriterView.FooterText = WriterView.FooterText.Substring(0, WriterView.FooterText.Length - 1);
                            }
                        }
                        else if (e.KeyChar != '\r')
                        {
                            if (WriterView.IsEditingHeaderMode)
                            {
                                if (WriterView.IsHeaderTextSelected)
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0) WriterView.ThreeColHeaderLeft = e.KeyChar.ToString();
                                        else if (WriterView.ActiveThreeColIndex == 1) WriterView.ThreeColHeaderCenter = e.KeyChar.ToString();
                                        else if (WriterView.ActiveThreeColIndex == 2) WriterView.ThreeColHeaderRight = e.KeyChar.ToString();
                                    }
                                    else
                                    {
                                        WriterView.HeaderTitleText = e.KeyChar.ToString();
                                        WriterView.HeaderText = WriterView.HeaderTitleText;
                                    }
                                    WriterView.IsHeaderTextSelected = false;
                                }
                                else
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0)
                                        {
                                            if (WriterView.ThreeColHeaderLeft == "[Nhập tại đây]") WriterView.ThreeColHeaderLeft = "";
                                            WriterView.ThreeColHeaderLeft += e.KeyChar;
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 1)
                                        {
                                            if (WriterView.ThreeColHeaderCenter == "[Nhập tại đây]") WriterView.ThreeColHeaderCenter = "";
                                            WriterView.ThreeColHeaderCenter += e.KeyChar;
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 2)
                                        {
                                            if (WriterView.ThreeColHeaderRight == "[Nhập tại đây]") WriterView.ThreeColHeaderRight = "";
                                            WriterView.ThreeColHeaderRight += e.KeyChar;
                                        }
                                    }
                                    else
                                    {
                                        string curT = WriterView.HeaderTitleText;
                                        if (curT == "TIÊU ĐỀ ĐẦU TRANG" || curT == "[Tiêu đề tài liệu]" || curT == "[Nhập tại đây]")
                                        {
                                            WriterView.HeaderTitleText = "";
                                            WriterView.HeaderText = "";
                                        }
                                        WriterView.HeaderTitleText += e.KeyChar;
                                        WriterView.HeaderText = WriterView.HeaderTitleText;
                                    }
                                }
                            }
                            else
                            {
                                WriterView.FooterText += e.KeyChar;
                            }
                        }
                    }
                    else if (WriterView.ActiveChart != null)
                    {
                        // Khi đang chọn biểu đồ: Không ghi vào trang văn bản
                        e.Handled = true;
                        return;
                    }
                    else if (WriterView.ActiveShape != null)
                    {
                        var sh = WriterView.ActiveShape;
                        if (sh.ShapeType == "Image")
                        {
                            // Khi đang chọn hình ảnh: Không ghi vào trang văn bản
                            e.Handled = true;
                            return;
                        }

                        if (sh.IsSmartArt)
                        {
                            int targetNode = sh.SelectedNodeIndex;
                            if (targetNode < 0)
                            {
                                if (sh.SubShapeRelativeBounds != null && sh.SubShapeRelativeBounds.Count > 0)
                                {
                                    targetNode = 0;
                                    sh.SelectedNodeIndex = 0;
                                }
                                else
                                {
                                    e.Handled = true;
                                    return;
                                }
                            }

                            if (!sh.IsEditingText)
                            {
                                if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r')
                                {
                                    sh.IsEditingText = true;
                                    string initial = sh.GetNodeText(targetNode).Trim();
                                    if (initial.Equals("[Text]", StringComparison.OrdinalIgnoreCase) ||
                                        initial.Equals("(Text)", StringComparison.OrdinalIgnoreCase) ||
                                        initial.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                                        initial.Equals("[Văn bản]", StringComparison.OrdinalIgnoreCase) ||
                                        initial.Equals("(Văn bản)", StringComparison.OrdinalIgnoreCase) ||
                                        initial.Equals("Văn bản", StringComparison.OrdinalIgnoreCase) ||
                                        initial.StartsWith("Văn bản ", StringComparison.OrdinalIgnoreCase))
                                    {
                                        sh.SetNodeText(targetNode, "");
                                        sh.TextCursorIndex = 0;
                                    }
                                    else
                                    {
                                        sh.TextCursorIndex = initial.Length;
                                    }
                                }
                                else
                                {
                                    e.Handled = true;
                                    Invalidate();
                                    return;
                                }
                            }

                            string nodeTxt = sh.GetNodeText(targetNode);
                            int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, nodeTxt.Length));

                            if (e.KeyChar == (char)Keys.Back)
                            {
                                if (cur > 0 && nodeTxt.Length > 0)
                                {
                                    nodeTxt = nodeTxt.Substring(0, cur - 1) + nodeTxt.Substring(cur);
                                    sh.SetNodeText(targetNode, nodeTxt);
                                    sh.TextCursorIndex = cur - 1;
                                }
                            }
                            else if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r' || e.KeyChar == '\n')
                            {
                                string ins = (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                nodeTxt = nodeTxt.Insert(cur, ins);
                                sh.SetNodeText(targetNode, nodeTxt);
                                sh.TextCursorIndex = cur + ins.Length;
                            }
                            WriterView.SyncShapeTagInDocument(sh);
                            MarkActiveTabModified();
                            e.Handled = true;
                            Invalidate();
                            return;
                        }

                        if (!sh.IsEditingText)
                        {
                            if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r')
                            {
                                sh.IsEditingText = true;
                                sh.TextContent = (sh.TextContent ?? "") + (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                sh.TextCursorIndex = sh.TextContent.Length;
                                MarkActiveTabModified();
                            }
                            e.Handled = true;
                            Invalidate();
                            return;
                        }

                        if (sh.TextContent == null) sh.TextContent = "";
                        int curNorm = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                        if (e.KeyChar == (char)Keys.Back)
                        {
                            if (curNorm > 0 && sh.TextContent.Length > 0)
                            {
                                sh.TextContent = sh.TextContent.Substring(0, curNorm - 1) + sh.TextContent.Substring(curNorm);
                                sh.TextCursorIndex = curNorm - 1;
                            }
                        }
                        else if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r' || e.KeyChar == '\n')
                        {
                            string ins = (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                            sh.TextContent = sh.TextContent.Insert(curNorm, ins);
                            sh.TextCursorIndex = curNorm + ins.Length;
                        }
                        MarkActiveTabModified();
                        e.Handled = true;
                        Invalidate();
                        return;
                    }
                    else
                    {
                        WriterView.HandleChar(e.KeyChar);
                        WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon);
                        WriterView.UpdateRibbonState(Ribbon);
                        MarkActiveTabModified();
                        ResetCursorBlink();
                        UpdateWordSuggestions();
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (CalcView.SelectedShape != null)
                    {
                        var sh = CalcView.SelectedShape;
                        if (sh.ShapeType != "Image")
                        {
                            if (!sh.IsEditingText)
                            {
                                if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r')
                                {
                                    sh.IsEditingText = true;
                                    sh.IsSelected = true;
                                    sh.TextContent = (sh.TextContent ?? "") + (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                    sh.TextCursorIndex = sh.TextContent.Length;
                                    MarkActiveTabModified();
                                }
                                e.Handled = true;
                                Invalidate();
                                return;
                            }

                            if (sh.TextContent == null) sh.TextContent = "";
                            int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                            if (e.KeyChar == (char)Keys.Back)
                            {
                                if (cur > 0 && sh.TextContent.Length > 0)
                                {
                                    sh.TextContent = sh.TextContent.Substring(0, cur - 1) + sh.TextContent.Substring(cur);
                                    sh.TextCursorIndex = cur - 1;
                                }
                            }
                            else if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r' || e.KeyChar == '\n')
                            {
                                string ins = (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                sh.TextContent = sh.TextContent.Insert(cur, ins);
                                sh.TextCursorIndex = cur + ins.Length;
                            }
                            MarkActiveTabModified();
                            e.Handled = true;
                            Invalidate();
                            return;
                        }
                    }
                    CalcView.HandleChar(e.KeyChar);
                    MarkActiveTabModified();
                    UpdateWordSuggestions();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleKeyPress(e.KeyChar);
                    MarkActiveTabModified();
                    UpdateWordSuggestions();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleKeyPress(e.KeyChar);
                    MarkActiveTabModified();
                }
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (BackstageView != null && BackstageView.IsOpen)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    CloseBackstageView();
                    e.Handled = true;
                    return;
                }
                BackstageView.OnKeyDown(e);
                Invalidate();
                return;
            }

            // Ignore standalone modifier key presses (Ctrl, Shift, Alt) so they never trigger viewport scroll jumps
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu ||
                e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey ||
                e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey ||
                e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            {
                return;
            }

            // Handle Word Suggestions navigation and selection keys (IntelliSense Autocomplete)
            if (WordSuggestions != null && WordSuggestions.IsVisible)
            {
                if (e.KeyCode == Keys.Down)
                {
                    WordSuggestions.MoveDown();
                    Invalidate();
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Keys.Up)
                {
                    WordSuggestions.MoveUp();
                    Invalidate();
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Enter)
                {
                    if (WordSuggestions.ApplySelected())
                    {
                        Invalidate();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        return;
                    }
                }
                if (e.KeyCode == Keys.Escape)
                {
                    WordSuggestions.Hide();
                    Invalidate();
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                {
                    WordSuggestions.Hide();
                    Invalidate();
                }
            }

            if (NewTabRadialLauncher.IsOpen)
            {
                if (NewTabRadialLauncher.HandleKeyDown(e.KeyCode))
                {
                    Invalidate();
                    e.Handled = true;
                    return;
                }
            }
            if (SlidesView.IsPresenting)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    StopSlideShow();
                    return;
                }
                SlidesView.HandleKeyDown(e.KeyCode);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
            else
            {
                if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    if (e.KeyCode == Keys.Space && !e.Control)
                    {
                        PhotoView.IsSpaceHeld = true;
                        Invalidate();
                        return;
                    }
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && WriterView.ViewMode == 0)
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        WriterView.ViewMode = 1; // Exit Read Mode to Print Layout
                        Invalidate();
                        return;
                    }
                    if (e.KeyCode == Keys.Right || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Space)
                    {
                        if (WriterView.ReadModePageIndex < WriterView.TotalPageCount - 1)
                        {
                            WriterView.ReadModePageIndex++;
                            Invalidate();
                        }
                        e.Handled = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Left || e.KeyCode == Keys.PageUp)
                    {
                        if (WriterView.ReadModePageIndex > 0)
                        {
                            WriterView.ReadModePageIndex--;
                            Invalidate();
                        }
                        e.Handled = true;
                        return;
                    }
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                {
                    if (SlidesView.ViewMode == ImpressViewMode.ReadingView)
                    {
                        if (e.KeyCode == Keys.Escape)
                        {
                            SlidesView.ViewMode = ImpressViewMode.Normal;
                            SlidesView.ZoomToFit();
                            Invalidate();
                            return;
                        }
                        if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                        {
                            if (SlidesView.Presentation != null && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                            {
                                SlidesView.ActiveSlideIndex++;
                                Invalidate();
                            }
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.Back)
                        {
                            if (SlidesView.ActiveSlideIndex > 0)
                            {
                                SlidesView.ActiveSlideIndex--;
                                Invalidate();
                            }
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (SlidesView.ViewMode == ImpressViewMode.SlideSorter)
                    {
                        if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                        {
                            SlidesView.ViewMode = ImpressViewMode.Normal;
                            SlidesView.ZoomToFit();
                            Invalidate();
                            return;
                        }
                        if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down)
                        {
                            if (SlidesView.Presentation != null && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                            {
                                SlidesView.ActiveSlideIndex++;
                                Invalidate();
                            }
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up)
                        {
                            if (SlidesView.ActiveSlideIndex > 0)
                            {
                                SlidesView.ActiveSlideIndex--;
                                Invalidate();
                            }
                            e.Handled = true;
                            return;
                        }
                    }
                }

                if (e.KeyCode == Keys.Escape)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && WriterView.IsFormatPainterActive)
                    {
                        WriterView.CancelFormatPainter();
                        Invalidate();
                        return;
                    }
                    if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null && CalcView.IsFormatPainterActive)
                    {
                        CalcView.IsFormatPainterActive = false;
                        Invalidate();
                        return;
                    }
                }

                if (e.KeyCode == Keys.Tab)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        if (e.Shift) WriterView.Outdent();
                        else WriterView.IndentOrInsertTab();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                // --- Phím chức năng F1, F4, F12, F5 ---
                if (e.KeyCode == Keys.F1)
                {
                    ShowShortcutsHelpDialog();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.F4)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null) WriterView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null) SlidesView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null) CalcView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null) PdfView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null) PhotoView.Document?.Redo();
                    MarkActiveTabModified();
                    Invalidate();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.F12)
                {
                    PerformSaveAs();
                    e.Handled = true;
                    return;
                }

                if (e.Shift && e.KeyCode == Keys.F3)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.ChangeCaseCycle();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Alt && e.Shift && e.KeyCode == Keys.D)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertCurrentDate();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Alt && e.Shift && e.KeyCode == Keys.T)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertCurrentTime();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Alt && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertEquation();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Shift && e.KeyCode == Keys.Enter && !e.Control)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && WriterView.ActiveShape == null && WriterView.ActiveChart == null)
                    {
                        WriterView.InsertSoftLineBreak();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Control && e.Alt && e.KeyCode == Keys.M)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertNewComment();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Control && e.KeyCode == Keys.Space)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.ClearFormatting();
                        WriterView.UpdateRibbonState(Ribbon);
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Control)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null && e.KeyCode != Keys.P && e.KeyCode != Keys.S && e.KeyCode != Keys.O && e.KeyCode != Keys.N && e.KeyCode != Keys.W)
                    {
                        PhotoView.HandleKeyDown(e.KeyCode, ModifierKeys);
                        MarkActiveTabModified();
                        Invalidate();
                        return;
                    }

                    switch (e.KeyCode)
                    {
                        case Keys.P:
                            PerformPrint();
                            return;
                        case Keys.H:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                var frForm = new FindReplaceForm();
                                frForm.OnFindNext += (findText) =>
                                {
                                    string full = WriterView.PieceTable.GetText();
                                    int next = full.IndexOf(findText, WriterView.CursorPosition, StringComparison.OrdinalIgnoreCase);
                                    if (next < 0) next = full.IndexOf(findText, 0, StringComparison.OrdinalIgnoreCase);
                                    if (next >= 0)
                                    {
                                        WriterView.SelectionStart = next;
                                        WriterView.SelectionEnd = next + findText.Length;
                                        WriterView.CursorPosition = WriterView.SelectionEnd;
                                        Invalidate();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Không tìm thấy '" + findText + "' trong tài liệu.", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                };
                                frForm.OnReplace += (findText, repText) =>
                                {
                                    string full = WriterView.PieceTable.GetText();
                                    int selMin = Math.Min(WriterView.SelectionStart, WriterView.SelectionEnd);
                                    int selLen = Math.Abs(WriterView.SelectionEnd - WriterView.SelectionStart);
                                    if (selLen > 0 && full.Substring(selMin, selLen).Equals(findText, StringComparison.OrdinalIgnoreCase))
                                    {
                                        WriterView.SaveUndoState();
                                        WriterView.PieceTable.Remove(selMin, selLen);
                                        WriterView.PieceTable.Insert(selMin, repText);
                                        WriterView.SelectionStart = selMin;
                                        WriterView.SelectionEnd = selMin + repText.Length;
                                        WriterView.CursorPosition = WriterView.SelectionEnd;
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                };
                                frForm.OnReplaceAll += (findText, repText) =>
                                {
                                    string full = WriterView.PieceTable.GetText();
                                    if (full.IndexOf(findText, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        WriterView.SaveUndoState();
                                        int count = 0;
                                        int idx = 0;
                                        while ((idx = full.IndexOf(findText, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                                        {
                                            WriterView.PieceTable.Remove(idx, findText.Length);
                                            WriterView.PieceTable.Insert(idx, repText);
                                            idx += repText.Length;
                                            count++;
                                            full = WriterView.PieceTable.GetText();
                                        }
                                        MarkActiveTabModified();
                                        Invalidate();
                                        MessageBox.Show("Đã thay thế tất cả " + count + " vị trí trùng khớp!", "Tìm kiếm & Thay thế", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                };
                                frForm.Show(this);
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                            {
                                CalcView.OpenFindReplaceDialog(this, true);
                                return;
                            }
                            break;
                        case Keys.K:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                int selMin = Math.Min(WriterView.SelectionStart, WriterView.SelectionEnd);
                                int selLen = Math.Abs(WriterView.SelectionEnd - WriterView.SelectionStart);
                                string selTxt = (selLen > 0) ? WriterView.PieceTable.GetText().Substring(selMin, selLen) : "";
                                using (var hDlg = new InsertHyperlinkForm(selTxt))
                                {
                                    if (hDlg.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.InsertHyperlink(hDlg.DisplayText, hDlg.TargetUrl, hDlg.ScreenTip);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                            {
                                CellData cell = CalcView.CurrentSheet.GetCell(CalcView.SelectedRow, CalcView.SelectedCol);
                                string curTxt = cell != null ? (cell.RawValue ?? "") : "";
                                using (var hDlg = new InsertHyperlinkForm(curTxt))
                                {
                                    if (hDlg.ShowDialog(this) == DialogResult.OK)
                                    {
                                        CalcView.InsertHyperlink(hDlg.DisplayText, hDlg.TargetUrl, hDlg.ScreenTip);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            break;
                        case Keys.G:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ShowNavigationPane = true;
                                WriterView.NavSearchFocused = true;
                                WriterView.NavActiveTab = 0;
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.L:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetParagraphAlignment(0);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.E:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetParagraphAlignment(1);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                            {
                                CalcView.FlashFill();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.R:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetParagraphAlignment(2);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.J:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetParagraphAlignment(3);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D1:
                        case Keys.NumPad1:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetLineSpacing(1.0f);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D2:
                        case Keys.NumPad2:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetLineSpacing(2.0f);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D5:
                        case Keys.NumPad5:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetLineSpacing(1.5f);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D0:
                        case Keys.NumPad0:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleSpaceBeforeParagraph();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.ZoomActualSize();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                            {
                                PhotoView.Zoom = 1.0f;
                                PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.M:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                if (e.Shift) WriterView.Outdent();
                                else WriterView.IndentOrInsertTab();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.T:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetHangingIndent(!e.Shift);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Q:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ClearParagraphFormatting();
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.OemCloseBrackets: // Ctrl + ] (Tăng 1pt)
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.IncreaseFontSize(1f);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.OemOpenBrackets: // Ctrl + [ (Giảm 1pt)
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.DecreaseFontSize(1f);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.OemPeriod: // Ctrl + Shift + >
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.IncreaseFontSizePreset();
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Oemcomma: // Ctrl + Shift + <
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.DecreaseFontSizePreset();
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.F:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                // Ctrl + Shift + F: Font Dialog
                                using (FontDialog fd = new FontDialog())
                                {
                                    fd.Font = new Font(!string.IsNullOrEmpty(WriterView.TypingFont) ? WriterView.TypingFont : "Times New Roman", WriterView.TypingFontSize > 0 ? WriterView.TypingFontSize : 12f);
                                    fd.ShowColor = true;
                                    fd.Color = WriterView.TypingColor;
                                    if (fd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.ApplyFontToSelectionOrTyping(fd.Font.Name);
                                        WriterView.ApplyFontSizeToSelectionOrTyping(fd.Font.SizeInPoints);
                                        WriterView.ApplyBoldToSelectionOrTyping(fd.Font.Bold);
                                        WriterView.ApplyItalicToSelectionOrTyping(fd.Font.Italic);
                                        WriterView.ApplyUnderlineToSelectionOrTyping(fd.Font.Underline);
                                        WriterView.ApplyStrikeToSelectionOrTyping(fd.Font.Strikeout);
                                        WriterView.ApplyFontColorToSelectionOrTyping(fd.Color);
                                        WriterView.UpdateRibbonState(Ribbon);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                WriterView.ShowNavigationPane = true;
                                WriterView.NavSearchFocused = true;
                                WriterView.NavActiveTab = 2; // Results tab
                                WriterView.PerformNavSearch();
                                Invalidate();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                PdfView.ShowSearchBar = !PdfView.ShowSearchBar;
                                Invalidate();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                            {
                                CalcView.OpenFindReplaceDialog(this, false);
                            }
                            return;
                        case Keys.Z:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.Undo();
                                WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Undo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                CalcView.Undo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                PdfView.Undo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                            {
                                PhotoView.Document?.Undo();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.Y:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.Redo();
                                WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Redo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                CalcView.Redo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                PdfView.Redo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                            {
                                PhotoView.Document?.Redo();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.C:
                            if (e.Shift)
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null) WriterView.CopyFormatting();
                                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null) CalcView.IsFormatPainterActive = true;
                            }
                            else
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Copy();
                                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                                {
                                    if (SlidesView.IsEditingSpeakerNotes) SlidesView.HandleKeyDown(Keys.C);
                                    else SlidesView.Copy();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null) CalcView.Copy();
                                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.EditingText != null)
                                {
                                    try { if (!string.IsNullOrEmpty(PdfView.EditingBuffer)) Clipboard.SetText(PdfView.EditingBuffer); } catch { }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.Enter:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                if (e.Shift) WriterView.InsertColumnBreak();
                                else WriterView.InsertPageBreak();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.D8:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ShowFormattingMarks = !WriterView.ShowFormattingMarks;
                                Invalidate();
                            }
                            return;
                        case Keys.X:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                WriterView.Cut();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                if (SlidesView.IsEditingSpeakerNotes) SlidesView.HandleKeyDown(Keys.X);
                                else SlidesView.Cut();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                            {
                                CalcView.Cut();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.V:
                            if (e.Shift)
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                                {
                                    WriterView.PasteFormatting();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                                {
                                    CalcView.PasteFormatting();
                                    MarkActiveTabModified();
                                }
                            }
                            else
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                                {
                                    WriterView.Paste();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                                {
                                    if (SlidesView.IsEditingSpeakerNotes) SlidesView.HandleKeyDown(Keys.V);
                                    else SlidesView.Paste();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                                {
                                    CalcView.Paste();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.EditingText != null)
                                {
                                    try
                                    {
                                        if (Clipboard.ContainsText())
                                        {
                                            string pasteTxt = Clipboard.GetText();
                                            if (!string.IsNullOrEmpty(pasteTxt))
                                            {
                                                if (PdfView.EditingBuffer == null) PdfView.EditingBuffer = "";
                                                int cur = Math.Max(0, Math.Min(PdfView.CursorPosition, PdfView.EditingBuffer.Length));
                                                PdfView.EditingBuffer = PdfView.EditingBuffer.Insert(cur, pasteTxt);
                                                PdfView.CursorPosition = cur + pasteTxt.Length;
                                                PdfView.EditingText.Text = PdfView.EditingBuffer;
                                                PdfView.Document.IsModified = true;
                                                MarkActiveTabModified();
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.D:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleDoubleUnderline();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                using (FontDialog fd = new FontDialog())
                                {
                                    fd.Font = new Font(!string.IsNullOrEmpty(WriterView.TypingFont) ? WriterView.TypingFont : "Times New Roman", WriterView.TypingFontSize > 0 ? WriterView.TypingFontSize : 12f);
                                    fd.ShowColor = true;
                                    fd.Color = WriterView.TypingColor;
                                    if (fd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.ApplyFontToSelectionOrTyping(fd.Font.Name);
                                        WriterView.ApplyFontSizeToSelectionOrTyping(fd.Font.SizeInPoints);
                                        WriterView.ApplyBoldToSelectionOrTyping(fd.Font.Bold);
                                        WriterView.ApplyItalicToSelectionOrTyping(fd.Font.Italic);
                                        WriterView.ApplyUnderlineToSelectionOrTyping(fd.Font.Underline);
                                        WriterView.ApplyStrikeToSelectionOrTyping(fd.Font.Strikeout);
                                        WriterView.ApplyFontColorToSelectionOrTyping(fd.Color);
                                        WriterView.UpdateRibbonState(Ribbon);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Duplicate();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.A:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ApplyAllCaps();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                WriterView.SelectAll();
                                WriterView.UpdateRibbonState(Ribbon);
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                            {
                                SlidesView.HandleKeyDown(Keys.A);
                            }
                            Invalidate();
                            return;
                        case Keys.B:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleBold(Ribbon);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView.SelectedShape != null)
                            {
                                SlidesView.SelectedShape.Bold = !SlidesView.SelectedShape.Bold;
                            }
                            Invalidate();
                            return;
                        case Keys.I:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleItalic(Ribbon);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView.SelectedShape != null)
                            {
                                SlidesView.SelectedShape.Italic = !SlidesView.SelectedShape.Italic;
                            }
                            Invalidate();
                            return;
                        case Keys.U:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleUnderline(Ribbon);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                ResetCursorBlink();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView.SelectedShape != null)
                            {
                                SlidesView.SelectedShape.Underline = !SlidesView.SelectedShape.Underline;
                            }
                            Invalidate();
                            return;
                        case Keys.S:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                Ribbon.HandleClick(64, 80, WriterView, this);
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Filter = "Tệp Trình Chiếu PowerPoint (*.pptx)|*.pptx|Tệp XML Presentation (*.xml)|*.xml";
                                    sfd.Title = "Lưu Bản Trình Chiếu Microsoft PowerPoint 2016-365";
                                    if (sfd.ShowDialog() == DialogResult.OK)
                                    {
                                        ZenQOffice.Format.PptxHandler.SavePptx(sfd.FileName, SlidesView.Presentation);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        MessageBox.Show("Đã lưu bản trình chiếu chuẩn .pptx thành công!", "ZenQ Slides", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Title = "Lưu Bảng tính ZENQ OFFICE Calc (Chuẩn Microsoft Excel .xlsx / .ods / .pdf / .html / .csv / .tsv)";
                                    sfd.Filter = "Tệp Excel (*.xlsx)|*.xlsx|Bảng tính OpenDocument (*.ods)|*.ods|Tệp PDF (*.pdf)|*.pdf|Trang Web HTML (*.html)|*.html|Tệp CSV (*.csv)|*.csv|Tệp TSV (*.tsv)|*.tsv|Tất cả tệp (*.*)|*.*";
                                    if (sfd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        SaveSpreadsheetDocument(sfd.FileName);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        MessageBox.Show("Đã lưu bảng tính thành công!", "ZENQ OFFICE Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Title = "Lưu tệp PDF chuẩn 1.7";
                                    sfd.Filter = "Tệp PDF (*.pdf)|*.pdf";
                                    sfd.DefaultExt = "pdf";
                                    sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                                    if (sfd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        bool ok = ZenQOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                                        if (ok)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            Invalidate();
                                            MessageBox.Show("Đã lưu tệp PDF '" + sfd.FileName + "' thành công!", "Tài liệu PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                }
                            }

                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Title = "Lưu tệp thiết kế PSD";
                                    sfd.Filter = "Tệp Đồ họa Vector (*.svg)|*.svg|Hình ảnh PNG (*.png)|*.png|Hình ảnh JPG (*.jpg)|*.jpg";
                                    sfd.DefaultExt = "psd";
                                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                    {
                                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                                    }
                                    if (sfd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        bool ok = PhotoView.Document != null && PhotoView.Document.SaveToFile(sfd.FileName);
                                        if (ok)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            Invalidate();
                                            MessageBox.Show("Đã lưu tệp PSD thành công!", "ZenQ Vector Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.O:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở tài liệu ZENQ OFFICE Writer (Chuẩn Microsoft Word OpenXML .docx)";
                                    ofd.Filter = "Tài liệu Word (*.docx)|*.docx|Văn bản OpenOffice Writer (*.odt)|*.odt|Tệp Rich Text (*.rtf)|*.rtf|Trang Web HTML (*.html)|*.html|Tệp Văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.LoadDocumentFile(ofd.FileName, Ribbon);
                                        string fName = System.IO.Path.GetFileName(ofd.FileName);
                                        Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                        Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                        Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                        Invalidate();
                                        MessageBox.Show("Đã mở tài liệu '" + fName + "' thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở bản trình chiếu PowerPoint (.pptx)";
                                    ofd.Filter = "Tệp Trình Chiếu PowerPoint (*.pptx;*.xml)|*.pptx;*.xml|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        SlidesView.Presentation = ZenQOffice.Format.PptxHandler.LoadPptx(ofd.FileName);
                                        SlidesView.ActiveSlideIndex = 0;
                                        SlidesView.SelectedShapeIndex = -1;
                                        SlidesView.CanvasScrollX = 0;
                                        SlidesView.CanvasScrollY = 0;
                                        SlidesView.TargetCanvasScrollX = 0f;
                                        SlidesView.TargetCanvasScrollY = 0f;
                                        SlidesView.SmoothCanvasScrollX = 0f;
                                        SlidesView.SmoothCanvasScrollY = 0f;
                                        SlidesView.ZoomLevel = 1.0f;
                                        SlidesView.ThumbScrollY = 0;
                                        SlidesView.TargetThumbScrollY = 0f;
                                        SlidesView.SmoothThumbScrollY = 0f;
                                        SlidesView.BottomThumbScrollX = 0;
                                        SlidesView.TargetBottomThumbScrollX = 0f;
                                        SlidesView.SmoothBottomThumbScrollX = 0f;
                                        SlidesView.IsElementsDrawerOpen = false;
                                        SlidesView.IsSidebarOpen = false;
                                        SlidesView.SidebarPanel = SidebarPanelType.None;
                                        D2DGraphics.ClearFontCache();
                                        string fName = System.IO.Path.GetFileName(ofd.FileName);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].PresentationData = SlidesView.Presentation;
                                        }
                                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                        Ribbon.RefreshCustomFonts();
                                        Invalidate();
                                        MessageBox.Show("Đã mở bản trình chiếu '" + fName + "' thành công!", "ZenQ Slides", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở Bảng tính ZenQ Calc (Chuẩn Microsoft Excel .xlsx / .xls / .ods / .csv / .tsv)";
                                    ofd.Filter = "Tất cả bảng tính (*.xlsx;*.xls;*.ods;*.csv;*.tsv)|*.xlsx;*.xls;*.ods;*.csv;*.tsv|Tệp Excel (*.xlsx)|*.xlsx|Tệp Excel 97-2003 (*.xls)|*.xls|Bảng tính OpenDocument (*.ods)|*.ods|Tệp CSV (*.csv)|*.csv|Tệp TSV (*.tsv)|*.tsv|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        bool success = LoadSpreadsheetDocument(ofd.FileName);
                                        if (success)
                                        {
                                            string fName = System.IO.Path.GetFileName(ofd.FileName);
                                            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                            {
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            }
                                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                            Invalidate();
                                            MessageBox.Show("Đã mở bảng tính '" + fName + "' thành công!", "ZenQ Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở tệp PDF";
                                    ofd.Filter = "Tệp PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        var loadedDoc = ZenQOffice.Format.PdfHandler.LoadPdf(ofd.FileName);
                                        PdfView.Document = loadedDoc;
                                        PdfView.ActivePageIndex = 0;
                                        string fName = System.IO.Path.GetFileName(ofd.FileName);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].PdfData = loadedDoc;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                        Invalidate();
                                        MessageBox.Show("Đã mở tệp PDF '" + fName + "' thành công!", "ZenQ PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở tệp Đồ họa Vector (SVG)";
                                    ofd.Filter = "Tệp Đồ họa Vector (*.svg)|*.svg|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        var loadedDoc = PhotoDocument.LoadFromFile(ofd.FileName);
                                        if (loadedDoc != null)
                                        {
                                            PhotoView.Document = loadedDoc;
                                            PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                                            string fName = System.IO.Path.GetFileName(ofd.FileName);
                                            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                            {
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].PhotoData = loadedDoc;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            }
                                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                                            Invalidate();
                                            MessageBox.Show("Đã mở tệp thiết kế '" + fName + "' thành công!", "ZenQ Vector Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                }
                            }
                            UpdateStandaloneTitle();
                            Invalidate();
                            return;
                        case Keys.N:
                            if (Ribbon.StandaloneMode)
                            {
                                string arg = "-" + Ribbon.CurrentMode.ToString().ToLowerInvariant();
                                try { System.Diagnostics.Process.Start(Application.ExecutablePath, arg); } catch { }
                                return;
                            }
                            if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                                Ribbon.AddDocTab("Trình chiếu " + Ribbon.DocTabs.Count + ".pptx", OfficeAppMode.Slides, SlidesView, WriterView, CalcView, PdfView);
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                                Ribbon.AddDocTab("Bảng tính " + Ribbon.DocTabs.Count + ".xlsx", OfficeAppMode.Calc, SlidesView, WriterView, CalcView, PdfView);
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                                Ribbon.AddDocTab("Tài liệu PDF " + Ribbon.DocTabs.Count + ".pdf", OfficeAppMode.Pdf, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
                                Ribbon.AddDocTab("Thiết kế " + Ribbon.DocTabs.Count + ".svg", OfficeAppMode.Photo, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            else
                                Ribbon.AddDocTab("Văn bản " + Ribbon.DocTabs.Count + ".docx", OfficeAppMode.Writer, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            SetModeIcon(Ribbon.CurrentMode);
                            Invalidate();
                            return;
                        case Keys.W:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleWordsUnderline();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            if (Ribbon.StandaloneMode)
                            {
                                this.Close();
                                return;
                            }
                            Ribbon.CloseDocTab(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
                            PositionAiConfigPanel();
                            Invalidate();
                            return;
                        case Keys.Add:
                        case Keys.Oemplus:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                if (e.Shift) WriterView.ToggleSuperscript(); // Ctrl + Shift + +
                                else WriterView.ToggleSubscript();           // Ctrl + =
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.ZoomIn();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                            {
                                PhotoView.Zoom = Math.Min(10.0f, PhotoView.Zoom * 1.25f);
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Subtract:
                        case Keys.OemMinus:
                            if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.ZoomOut();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                            {
                                PhotoView.Zoom = Math.Max(0.1f, PhotoView.Zoom / 1.25f);
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Home:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.CursorPosition = 0;
                                if (e.Shift)
                                {
                                    WriterView.SelectionEnd = 0;
                                }
                                else
                                {
                                    WriterView.SelectionStart = 0;
                                    WriterView.SelectionEnd = 0;
                                }
                                WriterView.ScrollY = 0;
                                WriterView.TargetScrollY = 0;
                                ResetCursorBlink();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            break;
                        case Keys.End:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.CursorPosition = WriterView.PieceTable.Length;
                                if (e.Shift)
                                {
                                    WriterView.SelectionEnd = WriterView.CursorPosition;
                                }
                                else
                                {
                                    WriterView.SelectionStart = WriterView.CursorPosition;
                                    WriterView.SelectionEnd = WriterView.CursorPosition;
                                }
                                int maxScroll = WriterView.GetMaxScrollY(ClientSize.Height, Ribbon);
                                WriterView.ScrollY = maxScroll;
                                WriterView.TargetScrollY = maxScroll;
                                ResetCursorBlink();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            break;
                    }
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.Document != null)
                {
                    if (e.KeyCode == Keys.PageDown)
                    {
                        if (PdfView.ActivePageIndex < PdfView.Document.Pages.Count - 1)
                        {
                            PdfView.ActivePageIndex++;
                            PdfView.ScrollToPage(PdfView.ActivePageIndex, ClientSize.Width, ClientSize.Height);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (e.KeyCode == Keys.PageUp)
                    {
                        if (PdfView.ActivePageIndex > 0)
                        {
                            PdfView.ActivePageIndex--;
                            PdfView.ScrollToPage(PdfView.ActivePageIndex, ClientSize.Width, ClientSize.Height);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (e.KeyCode == Keys.Home && !e.Control)
                    {
                        PdfView.ActivePageIndex = 0;
                        PdfView.ScrollToPage(0, ClientSize.Width, ClientSize.Height);
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                    else if (e.KeyCode == Keys.End && !e.Control)
                    {
                        PdfView.ActivePageIndex = Math.Max(0, PdfView.Document.Pages.Count - 1);
                        PdfView.ScrollToPage(PdfView.ActivePageIndex, ClientSize.Width, ClientSize.Height);
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        PdfView.HandleKeyDown(e.KeyCode);
                        if (PdfView.Document.IsModified) MarkActiveTabModified();
                        Invalidate();
                        return;
                    }
                }

                if (e.KeyCode == Keys.F5 && Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    if (e.Alt)
                    {
                        StartSlideShow(false, true); // Alt+F5 -> Presenter View
                    }
                    else
                    {
                        StartSlideShow(e.Shift ? false : true); // F5 -> from beginning, Shift+F5 -> from current
                    }
                    e.Handled = true;
                    return;
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    // 1. Khi đang chọn Biểu đồ (Chart)
                    if (WriterView.ActiveChart != null)
                    {
                        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                        {
                            WriterView.DeleteActiveChart();
                            MarkActiveTabModified();
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        int moveStep = e.Shift ? 10 : 2;
                        if (e.KeyCode == Keys.Left) { WriterView.ActiveChart.PageX -= moveStep; Invalidate(); e.Handled = true; return; }
                        if (e.KeyCode == Keys.Right) { WriterView.ActiveChart.PageX += moveStep; Invalidate(); e.Handled = true; return; }
                        if (e.KeyCode == Keys.Up) { WriterView.ActiveChart.PageY -= moveStep; Invalidate(); e.Handled = true; return; }
                        if (e.KeyCode == Keys.Down) { WriterView.ActiveChart.PageY += moveStep; Invalidate(); e.Handled = true; return; }

                        e.Handled = true;
                        return;
                    }

                    // 2. Khi đang chọn Hình khối hoặc Hình ảnh (Shape / Image)
                    if (WriterView.ActiveShape != null)
                    {
                        var sh = WriterView.ActiveShape;

                        // SmartArt Shape Key Handling
                        if (sh.IsSmartArt && sh.SelectedNodeIndex >= 0)
                        {
                            int targetNode = sh.SelectedNodeIndex;
                            if (!sh.IsEditingText)
                            {
                                if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                                {
                                    WriterView.DeleteSelectedSmartArtNode(sh);
                                    MarkActiveTabModified();
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Insert)
                                {
                                    WriterView.AddSmartArtNode(sh);
                                    MarkActiveTabModified();
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter)
                                {
                                    sh.IsEditingText = true;
                                    string nt = sh.GetNodeText(targetNode);
                                    sh.TextCursorIndex = nt.Length;
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Tab)
                                {
                                    if (sh.SubShapeRelativeBounds != null && sh.SubShapeRelativeBounds.Count > 0)
                                    {
                                        sh.SelectedNodeIndex = (targetNode + 1) % sh.SubShapeRelativeBounds.Count;
                                        Invalidate();
                                        e.Handled = true;
                                        return;
                                    }
                                }
                                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up)
                                {
                                    if (sh.SubShapeRelativeBounds != null && sh.SubShapeRelativeBounds.Count > 0)
                                    {
                                        sh.SelectedNodeIndex = Math.Max(0, targetNode - 1);
                                        Invalidate();
                                        e.Handled = true;
                                        return;
                                    }
                                }
                                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down)
                                {
                                    if (sh.SubShapeRelativeBounds != null && sh.SubShapeRelativeBounds.Count > 0)
                                    {
                                        sh.SelectedNodeIndex = Math.Min(sh.SubShapeRelativeBounds.Count - 1, targetNode + 1);
                                        Invalidate();
                                        e.Handled = true;
                                        return;
                                    }
                                }
                                if (e.KeyCode == Keys.Escape)
                                {
                                    sh.SelectedNodeIndex = -1;
                                    sh.IsSelected = false;
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                e.Handled = true;
                                return;
                            }
                            else
                            {
                                string nodeTxt = sh.GetNodeText(targetNode);
                                int nodeCur = Math.Max(0, Math.Min(sh.TextCursorIndex, nodeTxt.Length));

                                if (e.KeyCode == Keys.Left)
                                {
                                    sh.TextCursorIndex = Math.Max(0, nodeCur - 1);
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Right)
                                {
                                    sh.TextCursorIndex = Math.Min(nodeTxt.Length, nodeCur + 1);
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Home)
                                {
                                    sh.TextCursorIndex = 0;
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.End)
                                {
                                    sh.TextCursorIndex = nodeTxt.Length;
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Delete)
                                {
                                    if (nodeCur < nodeTxt.Length)
                                    {
                                        nodeTxt = nodeTxt.Substring(0, nodeCur) + nodeTxt.Substring(nodeCur + 1);
                                        sh.SetNodeText(targetNode, nodeTxt);
                                        WriterView.SyncShapeTagInDocument(sh);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Escape)
                                {
                                    sh.IsEditingText = false;
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Enter)
                                {
                                    if (e.Shift)
                                    {
                                        if (nodeCur <= nodeTxt.Length)
                                        {
                                            nodeTxt = nodeTxt.Substring(0, nodeCur) + "\n" + nodeTxt.Substring(nodeCur);
                                            sh.SetNodeText(targetNode, nodeTxt);
                                            sh.TextCursorIndex = nodeCur + 1;
                                            WriterView.SyncShapeTagInDocument(sh);
                                            MarkActiveTabModified();
                                            Invalidate();
                                        }
                                    }
                                    else
                                    {
                                        sh.IsEditingText = false;
                                        WriterView.AddSmartArtNode(sh);
                                        sh.IsEditingText = true;
                                        sh.TextCursorIndex = 0;
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                    e.Handled = true;
                                    return;
                                }
                                if (e.KeyCode == Keys.Tab)
                                {
                                    sh.IsEditingText = false;
                                    if (sh.SubShapeRelativeBounds != null && sh.SubShapeRelativeBounds.Count > 0)
                                    {
                                        sh.SelectedNodeIndex = (targetNode + 1) % sh.SubShapeRelativeBounds.Count;
                                    }
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                e.Handled = true;
                                return;
                            }
                        }

                        // Trường hợp chọn hình/ảnh ngoài (chưa ở chế độ gõ văn bản bên trong shape)
                        if (sh.ShapeType == "Image" || !sh.IsEditingText)
                        {
                            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                            {
                                WriterView.DeleteActiveShape();
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            int moveStep = e.Shift ? 10 : 2;
                            if (e.KeyCode == Keys.Left) { sh.PageX -= moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Right) { sh.PageX += moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Up) { sh.PageY -= moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Down) { sh.PageY += moveStep; Invalidate(); e.Handled = true; return; }

                            if (sh.ShapeType != "Image" && (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter))
                            {
                                sh.IsEditingText = true;
                                sh.TextCursorIndex = sh.TextContent != null ? sh.TextContent.Length : 0;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            e.Handled = true;
                            return;
                        }

                        // Trường hợp đang gõ văn bản bên trong Shape
                        if (sh.TextContent == null) sh.TextContent = "";
                        int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                        if (e.KeyCode == Keys.Left)
                        {
                            sh.TextCursorIndex = Math.Max(0, cur - 1);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Right)
                        {
                            sh.TextCursorIndex = Math.Min(sh.TextContent.Length, cur + 1);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Home)
                        {
                            sh.TextCursorIndex = 0;
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.End)
                        {
                            sh.TextCursorIndex = sh.TextContent.Length;
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Delete)
                        {
                            if (cur < sh.TextContent.Length)
                            {
                                sh.TextContent = sh.TextContent.Substring(0, cur) + sh.TextContent.Substring(cur + 1);
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            else if (sh.TextContent.Length == 0)
                            {
                                WriterView.DeleteActiveShape();
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                        }
                        if (e.KeyCode == Keys.Escape)
                        {
                            sh.IsEditingText = false;
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        e.Handled = true;
                        return;
                    }

                    ResetCursorBlink();
                    bool handled = WriterView.HandleKeyDown(e.KeyData, Ribbon, ClientSize.Height);
                    if (handled) e.Handled = true;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (CalcView.SelectedShape != null)
                    {
                        var sh = CalcView.SelectedShape;

                        // Nếu đang gõ văn bản bên trong Shape
                        if (sh.IsEditingText && sh.ShapeType != "Image")
                        {
                            if (sh.TextContent == null) sh.TextContent = "";
                            int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                            if (e.KeyCode == Keys.Left)
                            {
                                sh.TextCursorIndex = Math.Max(0, cur - 1);
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.Right)
                            {
                                sh.TextCursorIndex = Math.Min(sh.TextContent.Length, cur + 1);
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.Home)
                            {
                                sh.TextCursorIndex = 0;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.End)
                            {
                                sh.TextCursorIndex = sh.TextContent.Length;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.Delete)
                            {
                                if (cur < sh.TextContent.Length)
                                {
                                    sh.TextContent = sh.TextContent.Substring(0, cur) + sh.TextContent.Substring(cur + 1);
                                    MarkActiveTabModified();
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                else if (sh.TextContent.Length == 0)
                                {
                                    CalcView.Shapes.Remove(sh);
                                    CalcView.SelectedShape = null;
                                    MarkActiveTabModified();
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                            }
                            if (e.KeyCode == Keys.Escape)
                            {
                                sh.IsEditingText = false;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            // Đang chọn Shape nhưng chưa ở chế độ gõ
                            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                            {
                                CalcView.Shapes.Remove(CalcView.SelectedShape);
                                CalcView.SelectedShape = null;
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (sh.ShapeType != "Image" && (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter))
                            {
                                sh.IsEditingText = true;
                                sh.IsSelected = true;
                                sh.TextCursorIndex = sh.TextContent != null ? sh.TextContent.Length : 0;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            int moveStep = e.Shift ? 10 : 2;
                            if (e.KeyCode == Keys.Left) { CalcView.SelectedShape.PageX = Math.Max(0, CalcView.SelectedShape.PageX - moveStep); Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Right) { CalcView.SelectedShape.PageX += moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Up) { CalcView.SelectedShape.PageY = Math.Max(0, CalcView.SelectedShape.PageY - moveStep); Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Down) { CalcView.SelectedShape.PageY += moveStep; Invalidate(); e.Handled = true; return; }
                            e.Handled = true;
                            return;
                        }
                    }
                    CalcView.HandleKeyDown(e.KeyCode);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleKeyDown(e.KeyCode);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleKeyDown(e.KeyCode);
                    MarkActiveTabModified();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    PhotoView.HandleKeyDown(e.KeyCode, ModifierKeys);
                    MarkActiveTabModified();
                }
            }
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (WordSuggestions != null && WordSuggestions.IsVisible)
            {
                switch (keyData)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Enter:
                    case Keys.Tab:
                    case Keys.Escape:
                        return true;
                }
            }

            // Allow Up/Down/Left/Right/Home/End/Enter/Escape/Tab to be processed by OnKeyDown in Writer, Slides, Calc, Pdf, and Photo mode
            if (Ribbon != null && (Ribbon.CurrentMode == OfficeAppMode.Writer || Ribbon.CurrentMode == OfficeAppMode.Slides || Ribbon.CurrentMode == OfficeAppMode.Calc || Ribbon.CurrentMode == OfficeAppMode.Pdf || Ribbon.CurrentMode == OfficeAppMode.Photo))
            {
                switch (keyData)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.Enter:
                    case Keys.Tab:
                    case Keys.Escape:
                        return true;
                }
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (Ribbon != null && Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                if (e.KeyCode == Keys.Space)
                {
                    PhotoView.IsSpaceHeld = false;
                    Invalidate();
                }
            }
        }

        public void ShowShortcutsHelpDialog()
        {
            try
            {
                using (var helpForm = new ShortcutsHelpForm())
                {
                    helpForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở bảng tra cứu phím tắt: " + ex.Message, "Trợ giúp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void PerformSaveAs()
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu Tài liệu ZENQ OFFICE Writer với tên khác (Save As)";
                    sfd.Filter = "Tài liệu Word (*.docx)|*.docx|Tài liệu PDF (*.pdf)|*.pdf|Văn bản OpenDocument (*.odt)|*.odt|Tệp Rich Text (*.rtf)|*.rtf|Trang Web HTML (*.html;*.htm)|*.html;*.htm|Tệp Văn bản (*.txt)|*.txt|Tệp Markdown (*.md)|*.md|Tất cả tệp (*.*)|*.*";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        if (ext == ".pdf")
                        {
                            WriterView.ExportToPdf(sfd.FileName, Ribbon);
                        }
                        else
                        {
                            WriterView.SaveDocumentFile(sfd.FileName, Ribbon);
                            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                            {
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                            }
                        }
                        Invalidate();
                        MessageBox.Show("Đã lưu tài liệu '" + sfd.FileName + "' thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Bản trình chiếu PowerPoint (*.pptx)|*.pptx|Tài liệu PDF (*.pdf)|*.pdf|Hình ảnh Slide PNG (*.png)|*.png|Hình ảnh Slide JPEG (*.jpg)|*.jpg|Tệp XML Presentation (*.xml)|*.xml|Bản trình chiếu OpenDocument (*.odp)|*.odp|Tất cả tệp (*.*)|*.*";
                    sfd.Title = "Lưu Bản Trình Chiếu với tên khác (Save As)";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        bool ok = false;
                        if (ext == ".pdf")
                        {
                            ok = SlidesView.ExportToPdf(sfd.FileName);
                        }
                        else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        {
                            ok = SlidesView.ExportToImages(sfd.FileName, allSlides: false, isJpeg: (ext == ".jpg" || ext == ".jpeg"));
                        }
                        else if (ext == ".odp")
                        {
                            ok = ZenQOffice.Format.OdpHandler.SaveOdp(SlidesView.Presentation, sfd.FileName);
                        }
                        else if (ext == ".xml")
                        {
                            ok = ZenQOffice.Format.PptxHandler.SavePptxXml(sfd.FileName, SlidesView.Presentation);
                        }
                        else
                        {
                            ok = ZenQOffice.Format.PptxHandler.SavePptx(sfd.FileName, SlidesView.Presentation);
                            if (ok && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                            {
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                            }
                        }
                        Invalidate();
                        if (ok) MessageBox.Show("Đã lưu bản trình chiếu thành công!", "ZenQ Slides", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể lưu bản trình chiếu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu Bảng tính với tên khác (Save As)";
                    sfd.Filter = "Sổ làm việc Excel (*.xlsx)|*.xlsx|Tài liệu PDF (*.pdf)|*.pdf|Tệp CSV (Dấu phẩy phân cách) (*.csv)|*.csv|Tệp TSV (Tab phân cách) (*.tsv)|*.tsv|Bảng tính OpenDocument (*.ods)|*.ods|Trang Web HTML (*.html;*.htm)|*.html;*.htm|Tệp Văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        bool ok = SaveSpreadsheetDocument(sfd.FileName);
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        if (ok && (ext == ".xlsx" || ext == ".csv" || ext == ".ods"))
                        {
                            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                            {
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                            }
                        }
                        Invalidate();
                        if (ok) MessageBox.Show("Đã lưu bảng tính thành công!", "ZenQ Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể lưu bảng tính.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu tệp PDF với tên khác (Save As)";
                    sfd.Filter = "Tệp PDF (*.pdf)|*.pdf|Tài liệu Word (*.docx)|*.docx|Bảng tính Excel (*.xlsx)|*.xlsx|Tệp Văn bản thuần (*.txt)|*.txt";
                    sfd.DefaultExt = "pdf";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        bool ok = false;
                        if (ext == ".docx")
                        {
                            ok = ZenQOffice.Core.PdfConverterEngine.ExportToDocx(PdfView.Document, sfd.FileName);
                        }
                        else if (ext == ".xlsx")
                        {
                            ok = ZenQOffice.Core.PdfConverterEngine.ExportToXlsx(PdfView.Document, sfd.FileName);
                        }
                        else if (ext == ".txt")
                        {
                            ok = ZenQOffice.Core.PdfConverterEngine.ExportToTxt(PdfView.Document, sfd.FileName);
                        }
                        else
                        {
                            ok = ZenQOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                            if (ok && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                            {
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                            }
                        }
                        Invalidate();
                        if (ok) MessageBox.Show("Đã lưu tệp PDF thành công!", "ZenQ PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể lưu tệp PDF.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu thiết kế với tên khác (Save As)";
                    sfd.Filter = "Tệp Đồ họa Vector (*.svg)|*.svg|Tệp hình ảnh PNG (*.png)|*.png|Tệp JPEG (*.jpg;*.jpeg)|*.jpg|Tài liệu PDF (*.pdf)|*.pdf|Tệp Photoshop PSD (*.psd)|*.psd|Tệp ảnh Bitmap BMP (*.bmp)|*.bmp|Tệp ảnh TIFF (*.tiff)|*.tiff|Tất cả tệp (*.*)|*.*";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        bool ok = PhotoView.Document != null && PhotoView.Document.SaveToFile(sfd.FileName);
                        if (ok && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        Invalidate();
                        if (ok) MessageBox.Show("Đã lưu thiết kế thành công!", "Thiết kế & Ảnh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể lưu thiết kế.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public void PerformExport()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Xuất tài liệu đa định dạng (Export)";
                string curTitle = "TaiLieu";
                if (Ribbon != null && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                {
                    curTitle = System.IO.Path.GetFileNameWithoutExtension(Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", ""));
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    sfd.Filter = "Tài liệu PDF (*.pdf)|*.pdf|Tài liệu Word (*.docx)|*.docx|Tệp Rich Text (*.rtf)|*.rtf|Văn bản OpenDocument (*.odt)|*.odt|Trang Web HTML (*.html;*.htm)|*.html;*.htm|Tệp Markdown (*.md)|*.md|Bộ sưu tập ảnh PNG từng trang (*.png)|*.png|Bộ sưu tập ảnh JPEG từng trang (*.jpg)|*.jpg|Tệp Văn bản thuần (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                    sfd.DefaultExt = "pdf";
                    sfd.FileName = curTitle + ".pdf";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        bool ok = false;
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        {
                            var dlgRes = MessageBox.Show(this, "Bạn muốn xuất TẤT CẢ các trang (mỗi trang thành 1 file ảnh) hay chỉ xuất trang hiện tại?\n\n• Chọn [Yes] để xuất TẤT CẢ trang\n• Chọn [No] để chỉ xuất TRANG HIỆN TẠI", "Tùy chọn xuất ảnh trang tài liệu", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                            if (dlgRes == DialogResult.Cancel) return;
                            bool all = (dlgRes == DialogResult.Yes);
                            ok = WriterView.ExportToImages(sfd.FileName, Ribbon, allPages: all, isJpeg: (ext == ".jpg" || ext == ".jpeg"));
                        }
                        else if (ext == ".pdf")
                        {
                            ok = WriterView.ExportToPdf(sfd.FileName, Ribbon);
                        }
                        else
                        {
                            ok = WriterView.SaveDocumentFile(sfd.FileName, Ribbon, false);
                        }

                        if (ok) MessageBox.Show("Đã xuất tài liệu '" + sfd.FileName + "' thành công!", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể xuất tài liệu. Vui lòng kiểm tra quyền truy cập.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    sfd.Filter = "Tài liệu PDF (*.pdf)|*.pdf|Sổ làm việc Excel (*.xlsx)|*.xlsx|Tệp CSV (Dấu phẩy phân cách) (*.csv)|*.csv|Tệp TSV (Tab phân cách) (*.tsv)|*.tsv|Trang Web HTML (*.html;*.htm)|*.html;*.htm|Bảng tính OpenDocument (*.ods)|*.ods|Tệp Văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                    sfd.DefaultExt = "pdf";
                    sfd.FileName = curTitle + ".pdf";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        bool ok = SaveSpreadsheetDocument(sfd.FileName);
                        if (ok) MessageBox.Show("Đã xuất bảng tính '" + sfd.FileName + "' thành công!", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể xuất bảng tính. Vui lòng kiểm tra quyền truy cập.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    sfd.Filter = "Tài liệu PDF (*.pdf)|*.pdf|Bộ sưu tập ảnh PNG từng Slide (*.png)|*.png|Bộ sưu tập ảnh JPEG từng Slide (*.jpg)|*.jpg|Bản trình chiếu PowerPoint (*.pptx)|*.pptx|Bản trình chiếu OpenDocument (*.odp)|*.odp|Tệp XML Presentation (*.xml)|*.xml|Tất cả tệp (*.*)|*.*";
                    sfd.DefaultExt = "pdf";
                    sfd.FileName = curTitle + ".pdf";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        bool ok = false;
                        if (ext == ".pdf")
                        {
                            ok = SlidesView.ExportToPdf(sfd.FileName);
                        }
                        else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        {
                            var dlgRes = MessageBox.Show(this, "Bạn muốn xuất TẤT CẢ các slide (mỗi slide thành 1 file ảnh) hay chỉ xuất slide hiện tại?\n\n• Chọn [Yes] để xuất TẤT CẢ slide\n• Chọn [No] để chỉ xuất SLIDE HIỆN TẠI", "Tùy chọn xuất ảnh slide", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                            if (dlgRes == DialogResult.Cancel) return;
                            bool all = (dlgRes == DialogResult.Yes);
                            ok = SlidesView.ExportToImages(sfd.FileName, allSlides: all, isJpeg: (ext == ".jpg" || ext == ".jpeg"));
                        }
                        else if (ext == ".odp")
                        {
                            ok = ZenQOffice.Format.OdpHandler.SaveOdp(SlidesView.Presentation, sfd.FileName);
                        }
                        else if (ext == ".xml")
                        {
                            ok = ZenQOffice.Format.PptxHandler.SavePptxXml(sfd.FileName, SlidesView.Presentation);
                        }
                        else
                        {
                            ok = ZenQOffice.Format.PptxHandler.SavePptx(sfd.FileName, SlidesView.Presentation);
                        }

                        if (ok) MessageBox.Show("Đã xuất bản trình chiếu '" + sfd.FileName + "' thành công!", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể xuất bản trình chiếu. Vui lòng kiểm tra lại quyền truy cập.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    sfd.Filter = "Tài liệu Word (*.docx)|*.docx|Bảng tính Excel (*.xlsx)|*.xlsx|Bộ sưu tập ảnh PNG (*.png)|*.png|Bộ sưu tập ảnh JPEG (*.jpg)|*.jpg|Trích xuất văn bản thuần (*.txt)|*.txt|Tài liệu PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*";
                    sfd.DefaultExt = "docx";
                    sfd.FileName = curTitle + ".docx";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        bool ok = false;
                        if (ext == ".docx")
                        {
                            ok = ZenQOffice.Core.PdfConverterEngine.ExportToDocx(PdfView.Document, sfd.FileName);
                        }
                        else if (ext == ".xlsx")
                        {
                            ok = ZenQOffice.Core.PdfConverterEngine.ExportToXlsx(PdfView.Document, sfd.FileName);
                        }
                        else if (ext == ".txt")
                        {
                            ok = ZenQOffice.Core.PdfConverterEngine.ExportToTxt(PdfView.Document, sfd.FileName);
                        }
                        else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        {
                            var dlgRes = MessageBox.Show(this, "Bạn muốn xuất TẤT CẢ các trang (mỗi trang thành 1 file ảnh) hay chỉ xuất trang hiện tại?\n\n• Chọn [Yes] để xuất TẤT CẢ trang\n• Chọn [No] để chỉ xuất TRANG HIỆN TẠI", "Tùy chọn xuất ảnh PDF", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                            if (dlgRes == DialogResult.Cancel) return;
                            if (dlgRes == DialogResult.Yes)
                            {
                                string folder = System.IO.Path.GetDirectoryName(sfd.FileName);
                                var list = ZenQOffice.Core.PdfConverterEngine.ExportToImages(PdfView.Document, folder, (ext == ".jpg" || ext == ".jpeg") ? "JPG" : "PNG", 150);
                                ok = list != null && list.Count > 0;
                            }
                            else
                            {
                                using (var bmp = ZenQOffice.Core.PdfConverterEngine.RenderPageBitmap(PdfView.Document, Math.Max(0, PdfView.ActivePageIndex), 150f))
                                {
                                    bmp.Save(sfd.FileName, (ext == ".jpg" || ext == ".jpeg") ? System.Drawing.Imaging.ImageFormat.Jpeg : System.Drawing.Imaging.ImageFormat.Png);
                                    ok = true;
                                }
                            }
                        }
                        else
                        {
                            ok = ZenQOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                        }

                        if (ok) MessageBox.Show("Đã xuất tệp thành công:\n'" + sfd.FileName + "'", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể xuất tệp. Vui lòng kiểm tra lại quyền truy cập.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo)
                {
                    sfd.Filter = "Đồ họa Vector Scalable SVG (*.svg)|*.svg|Hình ảnh PNG trong suốt (*.png)|*.png|Hình ảnh JPEG chất lượng cao (*.jpg;*.jpeg)|*.jpg;*.jpeg|Tài liệu PDF (*.pdf)|*.pdf|Tệp Photoshop PSD (*.psd)|*.psd|Tệp ảnh Bitmap BMP (*.bmp)|*.bmp|Tệp ảnh TIFF (*.tiff;*.tif)|*.tiff;*.tif|Tất cả tệp (*.*)|*.*";
                    sfd.DefaultExt = "svg";
                    sfd.FileName = curTitle + ".svg";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        bool ok = PhotoView.Document != null && PhotoView.Document.SaveToFile(sfd.FileName);
                        if (ok) MessageBox.Show("Đã xuất thiết kế '" + sfd.FileName + "' thành công!", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Không thể xuất thiết kế. Vui lòng kiểm tra quyền truy cập.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public void PerformExportPdf()
        {
            PerformExport();
        }

        public void OpenBackupManagerDialog()
        {
            using (var dlg = new BackupManagerDialogForm((filePath) =>
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    OpenOfficeFileFromPeripheral(filePath);
                }
            }))
            {
                dlg.ShowDialog(this);
            }
        }

        public void PerformPrint()
        {
            try
            {
                using (PrintDialog pd = new PrintDialog())
                {
                    pd.UseEXDialog = true;
                    if (pd.ShowDialog(this) == DialogResult.OK)
                    {
                        MessageBox.Show("Đã gửi lệnh in tài liệu đến máy in: " + pd.PrinterSettings.PrinterName, "In Ấn - ZENQ OFFICE Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động trình quản lý in ấn: " + ex.Message, "In ấn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MarkActiveTabModified()
        {
            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
            {
                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = true;
            }
            UpdateStandaloneTitle();
        }

        public void UpdateWordSuggestions()
        {
            if (!AppSettings.Instance.WordPredictionEnabled)
            {
                if (WordSuggestions != null && WordSuggestions.IsVisible) WordSuggestions.Hide();
                return;
            }

            string wordBefore = "";
            int caretX = 0;
            int caretY = 0;
            int clientW = ClientSize.Width;
            int clientH = ClientSize.Height;
            int wordStart = -1;

            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                if (WriterView != null)
                {
                    wordBefore = WriterView.GetCurrentWordBeforeCursor(out wordStart);
                    caretX = WriterView.CurrentCaretScreenX;
                    caretY = WriterView.CurrentCaretScreenY + WriterView.CurrentCaretScreenH + 2;
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                if (CalcView != null && CalcView.IsEditing && !CalcView.EditText.StartsWith("="))
                {
                    wordBefore = CalcView.GetCurrentWordBeforeCursor(out wordStart);
                    caretX = CalcView.CurrentCaretScreenX;
                    caretY = CalcView.CurrentCaretScreenY;
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                if (SlidesView != null && (SlidesView.IsInlineEditing || SlidesView.IsEditingTableCell))
                {
                    wordBefore = SlidesView.GetCurrentWordBeforeCursor(out wordStart);
                    caretX = SlidesView.CurrentCaretScreenX;
                    caretY = SlidesView.CurrentCaretScreenY;
                }
            }

            if (string.IsNullOrEmpty(wordBefore) || wordBefore.Length < 1)
            {
                if (WordSuggestions != null && WordSuggestions.IsVisible) WordSuggestions.Hide();
                return;
            }

            // Bỏ qua nếu là số thuần túy
            if (char.IsDigit(wordBefore[0]))
            {
                if (WordSuggestions != null && WordSuggestions.IsVisible) WordSuggestions.Hide();
                return;
            }

            var suggestions = VietnameseWordPredictor.GetSuggestions(wordBefore, 3);
            if (suggestions != null && suggestions.Count > 0)
            {
                WordSuggestions.Show(caretX, caretY, clientW, clientH, wordBefore, suggestions, (chosen) =>
                {
                    ApplyWordSuggestion(chosen);
                });
            }
            else
            {
                if (WordSuggestions != null && WordSuggestions.IsVisible) WordSuggestions.Hide();
            }
        }

        public void ApplyWordSuggestion(string chosen)
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
            {
                WriterView.ReplaceWordAtCursor(chosen + " ");
                WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon);
                WriterView.UpdateRibbonState(Ribbon);
                ResetCursorBlink();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
            {
                CalcView.ReplaceWordAtCursor(chosen);
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
            {
                SlidesView.ReplaceWordAtCursor(chosen);
            }
            MarkActiveTabModified();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            WpfNewTabLauncherWindow.CloseActive();
            if (Ribbon != null)
            {
                Ribbon.IsParentMaximized = (this.WindowState == FormWindowState.Maximized);
            }
            if (_appD2d != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                try { _appD2d.Resize(ClientSize.Width, ClientSize.Height); } catch { }
            }
            PositionAiConfigPanel();
            Invalidate();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_appD2d != null)
            {
                try { _appD2d.Dispose(); } catch { }
                _appD2d = null;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            WpfNewTabLauncherWindow.CloseActive();
        }

        private bool LoadSpreadsheetDocument(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || CalcView == null) return false;
            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".ods")
            {
                return ZenQOffice.Format.OdsHandler.LoadOds(CalcView, filePath);
            }
            else if (ext == ".xls")
            {
                if (ZenQOffice.Format.XlsBiff8Handler.LoadXls(CalcView, filePath))
                    return true;
                return ZenQOffice.Format.XlsxHandler.LoadXlsxOrCsv(filePath, CalcView);
            }
            return ZenQOffice.Format.XlsxHandler.LoadXlsxOrCsv(filePath, CalcView);
        }

        private bool SaveSpreadsheetDocument(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || CalcView == null) return false;
            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".pdf")
            {
                return CalcView.ExportToPdf(filePath);
            }
            else if (ext == ".ods")
            {
                return ZenQOffice.Format.OdsHandler.SaveOds(CalcView, filePath);
            }
            else if (ext == ".html" || ext == ".htm")
            {
                return ZenQOffice.Format.DelimitedAndHtmlHandlers.ExportHtml(CalcView, filePath);
            }
            else if (ext == ".tsv")
            {
                return ZenQOffice.Format.DelimitedAndHtmlHandlers.ExportTsv(CalcView, filePath);
            }
            return ZenQOffice.Format.XlsxHandler.SaveXlsxOrCsv(filePath, CalcView);
        }

        // =========================================================================================
        // TOUCH MODE, STYLUS / PEN, MULTI-TOUCH GESTURES & PERIPHERAL DEVICE ECOSYSTEM
        // =========================================================================================
        private Rectangle _statusTouchToggleBounds = Rectangle.Empty;
        private Rectangle _touchBarBounds = Rectangle.Empty;
        private List<Rectangle> _touchBarBtnBounds = new List<Rectangle>();
        private bool _isTouchPanningDrag = false;
        private Point _touchPanLastPt = Point.Empty;
        private Point _touchPanStartPt = Point.Empty;
        private int _touchPanAccumDx = 0;
        private int _touchPanAccumDy = 0;
        private int _savedPenDrawTool = 1;
        private ulong _gestureLastZoomDist = 0;
        private Point _gestureLastPanPt = Point.Empty;
        private Point _gestureStartPanPt = Point.Empty;
        private int _gestureAccumPanDx = 0;
        private int _gestureAccumPanDy = 0;

        // Right-Side Inking Palette (Bảng màu tròn dọc, cỡ bút, tự nắn thẳng)
        private Rectangle _inkPaletteBounds = Rectangle.Empty;
        private Rectangle _inkModeBtnBounds = Rectangle.Empty;
        private List<Rectangle> _inkColorBtnBounds = new List<Rectangle>();
        private List<Rectangle> _inkSizeBtnBounds = new List<Rectangle>();
        private Rectangle _inkStraightenBtnBounds = Rectangle.Empty;

        private static readonly Color[] _highlighterPaletteColors = new Color[]
        {
            Color.FromArgb(250, 204, 21),  // Vàng (Yellow #FACC15)
            Color.FromArgb(74, 222, 128),  // Xanh lá neon (Green #4ADE80)
            Color.FromArgb(56, 189, 248),  // Xanh biển / Cyan (#38BDF8)
            Color.FromArgb(244, 114, 182), // Hồng (#F472B6)
            Color.FromArgb(251, 146, 60),  // Cam (#FB923C)
            Color.FromArgb(192, 132, 252)  // Tím (#C084FC)
        };

        private static readonly Color[] _penPaletteColors = new Color[]
        {
            Color.FromArgb(15, 23, 42),    // Đen (#0F172A)
            Color.FromArgb(37, 99, 235),   // Xanh dương (#2563EB)
            Color.FromArgb(220, 38, 38),   // Đỏ (#DC2626)
            Color.FromArgb(22, 163, 74),   // Xanh lá (#16A34A)
            Color.FromArgb(147, 51, 234),  // Tím (#9333EA)
            Color.FromArgb(234, 88, 12)    // Cam (#EA580C)
        };

        // Smart Peripheral Toast Banner State
        private string _peripheralToastTitle = null;
        private string _peripheralToastActionText = null;
        private Action _peripheralToastActionCallback = null;
        private DateTime _peripheralToastExpiry = DateTime.MinValue;
        private Rectangle _peripheralToastBounds = Rectangle.Empty;
        private Rectangle _peripheralToastActionBounds = Rectangle.Empty;
        private Rectangle _peripheralToastCloseBounds = Rectangle.Empty;
        private char _peripheralToastIcon = '\0';
        private char _peripheralToastActionIcon = '\0';

        private static string CleanToastText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsSurrogate(c)) continue;
                if (c >= '\uFE00' && c <= '\uFE0F') continue;
                if ((c >= '\u2600' && c <= '\u27BF') || (c >= '\u2B00' && c <= '\u2BFF') || (c >= '\u2300' && c <= '\u23FF')) continue;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static char DetectToastIcon(string rawTitle)
        {
            if (string.IsNullOrEmpty(rawTitle)) return '\uE946';
            if (rawTitle.IndexOf("Cảm ứng", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("Stylus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("chạm", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uED5E';
            if (rawTitle.IndexOf("Điện thoại", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("Chữ ký", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE8EA';
            if (rawTitle.IndexOf("Scan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("ảnh", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE722';
            if (rawTitle.IndexOf("màn hình", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("Máy chiếu", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE7F4';
            if (rawTitle.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("ổ đĩa", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE88E';
            if (rawTitle.IndexOf("bàn phím", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("phím ảo", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE765';
            if (rawTitle.IndexOf("nét vẽ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawTitle.IndexOf("bút", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uEDC6';
            return '\uE946';
        }

        private static char DetectToastActionIcon(string rawActionText)
        {
            if (string.IsNullOrEmpty(rawActionText)) return '\0';
            if (rawActionText.IndexOf("Cấu hình", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawActionText.IndexOf("Cài đặt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawActionText.IndexOf("Thiết bị", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE713';
            if (rawActionText.IndexOf("Presenter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawActionText.IndexOf("Chiếu", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE714';
            if (rawActionText.IndexOf("Sao lưu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawActionText.IndexOf("Lưu", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uE74E';
            if (rawActionText.IndexOf("Cảm ứng", StringComparison.OrdinalIgnoreCase) >= 0)
                return '\uED5E';
            return '\uE713';
        }

        public void ToggleTouchMode()
        {
            AppSettings.Instance.TouchModeEnabled = !AppSettings.Instance.TouchModeEnabled;
            if (!AppSettings.Instance.TouchModeEnabled)
            {
                AppSettings.Instance.TouchPanActive = false;
                _isTouchPanningDrag = false;
            }
            AppSettings.Instance.Save();

            if (AppSettings.Instance.TouchModeEnabled)
            {
                ShowPeripheralToast(
                    "Đã bật Chế độ Cảm ứng & Bút Stylus — Khoảng cách nút rộng hơn & Thanh công cụ nổi đã sẵn sàng",
                    "Cấu hình Thiết bị",
                    () => OpenPeripheralHubDialog(),
                    5,
                    '\uED5E',
                    '\uE713');
            }
            Invalidate();
        }

        public string GetActiveDocumentFilePath()
        {
            if (Ribbon != null && Ribbon.DocTabs != null && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
            {
                string p = Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath;
                if (!string.IsNullOrEmpty(p)) return p;
            }
            if (Ribbon != null && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && !string.IsNullOrEmpty(WriterView.CurrentFilePath))
            {
                return WriterView.CurrentFilePath;
            }
            return null;
        }

        public void OpenMobileConnectDialog()
        {
            using (ZenQConnectDialog dlg = new ZenQConnectDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        public void InsertImageFromMobile(Bitmap mobileBmp)
        {
            if (mobileBmp == null) return;
            InsertScannedDocumentBitmap(mobileBmp);
            ShowPeripheralToast("Đã chèn Chữ ký / Hình vẽ từ Điện thoại vào tài liệu!", null, null, 5, '\uE8EA');
        }

        public void ApplyZoomDelta(float deltaZoom)
        {
            ApplyPinchZoomDelta(deltaZoom, new Point(Width / 2, Height / 2));
            Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnSystemDisplaySettingsChanged;
            }
            catch { }
            try
            {
                TimeEndPeriod(1);
            }
            catch { }
            try
            {
                if (Ribbon == null || !Ribbon.StandaloneMode)
                {
                    SessionManager.SaveSession(this);
                }
            }
            catch { }
            try
            {
                SlideVideoPlayerManager.Instance.Stop();
            }
            catch { }
            try
            {
                SlideAudioPlayerManager.Instance.Stop();
            }
            catch { }
            try
            {
                ZenQOffice.Core.ZenQConnectServer.Instance.Stop();
            }
            catch { }
            base.OnFormClosing(e);
        }

        private void SetupMobileConnectServer()
        {
            var server = ZenQOffice.Core.ZenQConnectServer.Instance;
            server.ActionDispatchCustomCommand = (cmd) =>
            {
                BeginInvoke(new Action(() =>
                {
                    switch (cmd)
                    {
                        case "undo": PerformQuickUndo(); break;
                        case "redo": PerformQuickRedo(); break;
                        case "save": PerformQuickSave(); break;
                        case "zoom_in": ApplyZoomDelta(0.10f); break;
                        case "zoom_out": ApplyZoomDelta(-0.10f); break;
                        case "f5": StartSlideShow(true); break;
                        case "shift_f5": StartSlideShow(false); break;
                        case "esc":
                            if (SlidesView != null && SlidesView.IsPresenting) StopSlideShow();
                            else SendKeys.SendWait("{ESC}");
                            break;
                        case "slide_next":
                            if (SlidesView != null)
                            {
                                if (SlidesView.IsPresenting) SlidesView.AdvancePresenterStep();
                                else if (SlidesView.Presentation != null && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                                {
                                    SlidesView.ActiveSlideIndex++;
                                    Invalidate();
                                }
                            }
                            break;
                        case "slide_prev":
                            if (SlidesView != null)
                            {
                                if (SlidesView.IsPresenting) SlidesView.PreviousPresenterStep();
                                else if (SlidesView.ActiveSlideIndex > 0)
                                {
                                    SlidesView.ActiveSlideIndex--;
                                    Invalidate();
                                }
                            }
                            break;
                        case "slide_start": StartSlideShow(true); break;
                        case "slide_stop": StopSlideShow(); break;
                        case "slide_black":
                            HandleSlideShowKeyDown(Keys.B);
                            break;
                        case "slide_white":
                            HandleSlideShowKeyDown(Keys.W);
                            break;
                    }
                }));
            };

            server.ActionLaserPointer = (normX, normY, active) =>
            {
                BeginInvoke(new Action(() =>
                {
                    if (SlidesView != null)
                    {
                        SlidesView.LaserPointerMode = active;
                        if (active)
                        {
                            int w = ClientSize.Width;
                            int h = ClientSize.Height;
                            SlidesView.CurrentLaserPoint = new Point((int)(normX * w), (int)(normY * h));
                        }
                        SlidesView.Invalidate();
                        Invalidate();
                    }
                }));
            };

            server.ActionInsertSignature = (bmp) =>
            {
                BeginInvoke(new Action(() =>
                {
                    InsertImageFromMobile(bmp);
                }));
            };

            server.FuncGetAppStatus = () =>
            {
                string mode = (Ribbon != null) ? Ribbon.CurrentMode.ToString() : "Writer";
                bool presenting = (SlidesView != null && SlidesView.IsPresenting);
                int slideIdx = (SlidesView != null) ? SlidesView.ActiveSlideIndex + 1 : 1;
                int slideTotal = (SlidesView != null && SlidesView.Presentation != null && SlidesView.Presentation.Slides.Count > 0) ? SlidesView.Presentation.Slides.Count : 1;
                string notes = (SlidesView != null && SlidesView.ActiveSlide != null) ? (SlidesView.ActiveSlide.SpeakerNotes ?? "") : "";
                string title = (Ribbon != null && Ribbon.DocTabs != null && Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    ? Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title
                    : "ZenQ Office";

                return new System.Collections.Generic.Dictionary<string, object>
                {
                    { "appMode", mode },
                    { "isPresenting", presenting },
                    { "slideIndex", slideIdx },
                    { "slideCount", slideTotal },
                    { "speakerNotes", notes },
                    { "docTitle", title }
                };
            };

            server.OnClientCountChanged += (count) =>
            {
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        Invalidate();
                    }));
                }
                catch { }
            };

            System.Threading.Tasks.Task.Run(() => server.Start());
        }

        public void OpenPeripheralHubDialog()
        {
            using (PeripheralHubDialog dlg = new PeripheralHubDialog(() => GetActiveDocumentFilePath()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(dlg.SelectedUsbFileToOpen) && System.IO.File.Exists(dlg.SelectedUsbFileToOpen))
                    {
                        OpenOfficeFileFromPeripheral(dlg.SelectedUsbFileToOpen);
                    }
                    else if (dlg.ScannedDocumentBitmapToInsert != null)
                    {
                        InsertScannedDocumentBitmap(dlg.ScannedDocumentBitmapToInsert);
                    }
                    else if (dlg.RequestStartPresenterView)
                    {
                        if (Ribbon.CurrentMode != OfficeAppMode.Slides)
                        {
                            SwitchAppMode(OfficeAppMode.Slides);
                        }
                        StartSlideShow(false, true);
                    }
                    Invalidate();
                }
            }
        }

        public void OpenOfficeFileFromPeripheral(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            OfficeAppMode targetMode = OfficeAppMode.Writer;
            if (ext == ".xlsx" || ext == ".xls" || ext == ".ods" || ext == ".csv" || ext == ".tsv")
                targetMode = OfficeAppMode.Calc;
            else if (ext == ".pptx" || ext == ".ppt")
                targetMode = OfficeAppMode.Slides;
            else if (ext == ".pdf")
                targetMode = OfficeAppMode.Pdf;
            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".svg" || ext == ".webp" || ext == ".psd")
                targetMode = OfficeAppMode.Photo;

            try { BackupManager.BackupOriginalFile(filePath, targetMode.ToString()); } catch { }

            string title = System.IO.Path.GetFileName(filePath);
            if (Ribbon.StandaloneMode)
            {
                OpenDirectMode(targetMode, filePath);
                return;
            }

            Ribbon.AddDocTab(title, targetMode, SlidesView, WriterView, CalcView, PdfView, PhotoView);
            SetModeIcon(targetMode);
            int idx = Ribbon.ActiveDocIndex;
            if (idx >= 0 && idx < Ribbon.DocTabs.Count)
            {
                Ribbon.DocTabs[idx].FilePath = filePath;
                Ribbon.DocTabs[idx].IsModified = false;
            }

            if (targetMode == OfficeAppMode.Writer && WriterView != null)
                WriterView.LoadDocumentFile(filePath, Ribbon);
            else if (targetMode == OfficeAppMode.Calc && CalcView != null)
                LoadSpreadsheetDocument(filePath);
            else if (targetMode == OfficeAppMode.Slides && SlidesView != null)
                SlidesView.Presentation = ZenQOffice.Format.PptxHandler.LoadPptx(filePath);
            else if (targetMode == OfficeAppMode.Pdf && PdfView != null)
                PdfView.Document = ZenQOffice.Format.PdfHandler.LoadPdf(filePath);
            else if (targetMode == OfficeAppMode.Photo && PhotoView != null)
                PhotoView.Document = PhotoDocument.LoadFromFile(filePath);

            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView, PhotoView);
            Invalidate();
        }

        private void InsertScannedDocumentBitmap(Bitmap scannedBmp)
        {
            if (scannedBmp == null) return;
            try
            {
                Clipboard.SetImage(scannedBmp);
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ZenQOffice_Scans");
                if (!System.IO.Directory.Exists(tempDir)) System.IO.Directory.CreateDirectory(tempDir);
                string tempFile = System.IO.Path.Combine(tempDir, "Scan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                scannedBmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);

                if (Ribbon.CurrentMode == OfficeAppMode.Home)
                {
                    SwitchAppMode(OfficeAppMode.Writer);
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                {
                    WriterView.Paste();
                    MarkActiveTabModified();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null && SlidesView.Presentation != null && SlidesView.Presentation.Slides.Count > 0)
                {
                    var slide = SlidesView.Presentation.Slides[SlidesView.ActiveSlideIndex];
                    ShapeNode imgShape = new ShapeNode
                    {
                        Type = ShapeType.Image,
                        Name = "Tài liệu Scan",
                        ImagePath = tempFile,
                        CachedImage = new Bitmap(scannedBmp)
                    };
                    imgShape.Transform.X = 120;
                    imgShape.Transform.Y = 80;
                    imgShape.Transform.Width = Math.Min(520, scannedBmp.Width);
                    imgShape.Transform.Height = Math.Min(380, scannedBmp.Height);
                    slide.Shapes.Add(imgShape);
                    SlidesView.SelectedShapeIndex = slide.Shapes.Count - 1;
                    MarkActiveTabModified();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                {
                    CalcView.Paste();
                    MarkActiveTabModified();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    PhotoView.Document = PhotoDocument.LoadFromFile(tempFile);
                    PhotoView.CenterCanvas(ClientSize.Width, ClientSize.Height);
                }

                ShowPeripheralToast("Đã làm nét & chèn ảnh Scan tài liệu vào trang làm việc (Đã lưu vào Clipboard)", null, null, 5, '\uE722');
            }
            catch { }
        }

        public void ShowPeripheralToast(string title, string actionText = null, Action actionCallback = null, int durationSeconds = 7, char icon = '\0', char actionIcon = '\0')
        {
            if (!AppSettings.Instance.PeripheralAutoNotify && actionCallback != null) return;
            _peripheralToastIcon = (icon != '\0') ? icon : DetectToastIcon(title);
            _peripheralToastActionIcon = (actionIcon != '\0') ? actionIcon : DetectToastActionIcon(actionText);
            _peripheralToastTitle = CleanToastText(title);
            _peripheralToastActionText = CleanToastText(actionText);
            _peripheralToastActionCallback = actionCallback;
            _peripheralToastExpiry = DateTime.UtcNow.AddSeconds(durationSeconds);
            Invalidate();
        }

        private void OnSystemDisplaySettingsChanged(object sender, EventArgs e)
        {
            try
            {
                int newCount;
                if (PeripheralDeviceManager.CheckMonitorCountChanged(out newCount))
                {
                    if (newCount > 1)
                    {
                        ShowPeripheralToast(
                            string.Format("Đã phát hiện màn hình phụ / Máy chiếu mới ({0} màn hình đang kết nối)", newCount),
                            "Bật Presenter View",
                            () =>
                            {
                                if (Ribbon.CurrentMode != OfficeAppMode.Slides)
                                    SwitchAppMode(OfficeAppMode.Slides);
                                StartSlideShow(false, true);
                            },
                            9,
                            '\uE7F4',
                            '\uE714');
                    }
                    else
                    {
                        ShowPeripheralToast("Đã ngắt kết nối màn hình phụ / Máy chiếu — Chuyển về 1 màn hình chính", null, null, 4, '\uE7F4');
                    }
                }
            }
            catch { }
        }

        private void HandleWmDeviceChange(ref Message m)
        {
            try
            {
                int evt = m.WParam.ToInt32();
                if (evt == PeripheralDeviceManager.DBT_DEVICEARRIVAL || evt == PeripheralDeviceManager.DBT_DEVICEREMOVECOMPLETE)
                {
                    string removedDrive;
                    UsbDriveSummary newUsb = PeripheralDeviceManager.CheckForNewlyArrivedUsbDrive(out removedDrive);
                    if (newUsb != null)
                    {
                        ShowPeripheralToast(
                            "Đã cắm ổ đĩa USB: " + newUsb.DisplayName,
                            "Sao lưu / Mở tệp USB",
                            () => OpenPeripheralHubDialog(),
                            9,
                            '\uE88E',
                            '\uE74E');
                    }
                    else if (!string.IsNullOrEmpty(removedDrive))
                    {
                        ShowPeripheralToast("Đã rút ổ đĩa USB (" + removedDrive.TrimEnd('\\') + ")", null, null, 4, '\uE88E');
                    }
                }
            }
            catch { }
        }

        private void HandleWmPointerInput(ref Message m)
        {
            PointerInputType pType;
            float pressureNorm;
            bool isEraser;
            if (!PeripheralDeviceManager.TryInspectPointerMessage(m.WParam, out pType, out pressureNorm, out isEraser))
                return;

            if (pType == PointerInputType.Pen)
            {
                if (AppSettings.Instance.AutoSwitchPenInk && Ribbon != null && Ribbon.CurrentMode != OfficeAppMode.Home)
                {
                    if (isEraser)
                    {
                        if (Ribbon.ActiveDrawTool != 6)
                        {
                            if (Ribbon.ActiveDrawTool > 0) _savedPenDrawTool = Ribbon.ActiveDrawTool;
                            Ribbon.ActiveDrawTool = 6; // Tự động chuyển sang Tẩy khi quay đầu bút cảm ứng!
                            Invalidate(new Rectangle(0, 0, ClientSize.Width, 155));
                        }
                    }
                    else
                    {
                        if (Ribbon.ActiveDrawTool == 0 || Ribbon.ActiveDrawTool == 6)
                        {
                            Ribbon.ActiveDrawTool = (_savedPenDrawTool > 0 && _savedPenDrawTool != 6) ? _savedPenDrawTool : 1;
                            Invalidate(new Rectangle(0, 0, ClientSize.Width, 155));
                        }
                    }
                }

                if (AppSettings.Instance.PenPressureSensitivity && Ribbon != null && !isEraser)
                {
                    // Điều chỉnh độ dày nét bút mượt mà theo lực nhấn (1024 mức áp lực)
                    Ribbon.CustomPenWidth = Math.Max(1.0f, Math.Min(12.0f, 0.8f + pressureNorm * 6.0f));
                }
            }
            else if (pType == PointerInputType.Touch && m.Msg == PeripheralDeviceManager.WM_POINTERDOWN)
            {
                if (!AppSettings.Instance.TouchModeEnabled)
                {
                    // Tự động gợi ý hoặc kích hoạt giao diện thân thiện cảm ứng khi chạm tay trực tiếp
                    ShowPeripheralToast(
                        "Phát hiện thao tác chạm màn hình cảm ứng",
                        "Bật Chế độ Cảm ứng",
                        () => ToggleTouchMode(),
                        6,
                        '\uED5E',
                        '\uED5E');
                }
            }
        }

        private bool HandleWmGesture(ref Message m)
        {
            try
            {
                PeripheralDeviceManager.GESTUREINFO gi = new PeripheralDeviceManager.GESTUREINFO();
                gi.cbSize = Marshal.SizeOf(typeof(PeripheralDeviceManager.GESTUREINFO));
                if (!PeripheralDeviceManager.GetGestureInfo(m.LParam, ref gi))
                    return false;

                try
                {
                    if (gi.dwID == PeripheralDeviceManager.GID_ZOOM)
                    {
                        if ((gi.dwFlags & PeripheralDeviceManager.GF_BEGIN) != 0)
                        {
                            _gestureLastZoomDist = gi.ullArguments;
                            return true;
                        }
                        if (_gestureLastZoomDist > 0 && gi.ullArguments > 0)
                        {
                            double ratio = (double)gi.ullArguments / (double)_gestureLastZoomDist;
                            if (Math.Abs(ratio - 1.0) > 0.02)
                            {
                                float deltaZoom = (float)((ratio - 1.0) * 0.85);
                                ApplyPinchZoomDelta(deltaZoom, new Point(gi.ptsLocation.x, gi.ptsLocation.y));
                                _gestureLastZoomDist = gi.ullArguments;
                            }
                        }
                        return true;
                    }
                    else if (gi.dwID == PeripheralDeviceManager.GID_PAN)
                    {
                        Point curPt = new Point(gi.ptsLocation.x, gi.ptsLocation.y);
                        if ((gi.dwFlags & PeripheralDeviceManager.GF_BEGIN) != 0)
                        {
                            _gestureLastPanPt = curPt;
                            _gestureStartPanPt = curPt;
                            _gestureAccumPanDx = 0;
                            _gestureAccumPanDy = 0;
                            return true;
                        }
                        int dx = curPt.X - _gestureLastPanPt.X;
                        int dy = curPt.Y - _gestureLastPanPt.Y;
                        _gestureLastPanPt = curPt;
                        _gestureAccumPanDx += dx;
                        _gestureAccumPanDy += dy;

                        Point clientStartPt = PointToClient(_gestureStartPanPt.IsEmpty ? curPt : _gestureStartPanPt);
                        PerformTouchPanDelta(dx, dy, clientStartPt);

                        if ((gi.dwFlags & PeripheralDeviceManager.GF_END) != 0)
                        {
                            PerformTouchPanEnd(_gestureAccumPanDx, _gestureAccumPanDy, clientStartPt);
                        }
                        Invalidate();
                        return true;
                    }
                    else if (gi.dwID == PeripheralDeviceManager.GID_TWOFINGERTAP || gi.dwID == PeripheralDeviceManager.GID_PRESSANDTAP)
                    {
                        if ((gi.dwFlags & PeripheralDeviceManager.GF_BEGIN) != 0)
                        {
                            Point clientPt = PointToClient(new Point(gi.ptsLocation.x, gi.ptsLocation.y));
                            OnMouseDown(new MouseEventArgs(MouseButtons.Right, 1, clientPt.X, clientPt.Y, 0));
                            return true;
                        }
                    }
                }
                finally
                {
                    PeripheralDeviceManager.CloseGestureInfoHandle(m.LParam);
                }
            }
            catch { }
            return false;
        }

        private bool HandleWmMouseHWheel(ref Message m)
        {
            try
            {
                short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                if (delta == 0) return false;

                if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                {
                    int colStep = (delta > 0) ? 1 : -1;
                    CalcView.ScrollCol = (uint)Math.Max(1, (int)CalcView.ScrollCol + colStep);
                    Invalidate();
                    return true;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                {
                    SlidesView.TargetCanvasScrollX += (delta > 0) ? 45f : -45f;
                    SlidesView.CanvasScrollX = (int)SlidesView.TargetCanvasScrollX;
                    Invalidate();
                    return true;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
                {
                    PhotoView.PanX -= (delta > 0) ? 40f : -40f;
                    Invalidate();
                    return true;
                }
            }
            catch { }
            return false;
        }

        private bool HandleWmAppCommand(ref Message m)
        {
            try
            {
                int cmd = (int)((m.LParam.ToInt64() >> 16) & 0xFFF);
                switch (cmd)
                {
                    case PeripheralDeviceManager.APPCOMMAND_MEDIA_NEXTTRACK:
                    case PeripheralDeviceManager.APPCOMMAND_BROWSER_FORWARD:
                        if (SlidesView != null && SlidesView.IsPresenting)
                        {
                            SlidesView.AdvancePresenterStep();
                            Invalidate();
                            if (_audienceForm != null) _audienceForm.Invalidate();
                            return true;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null && SlidesView.Presentation != null)
                        {
                            if (SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                            {
                                SlidesView.ActiveSlideIndex++;
                                Invalidate();
                            }
                            return true;
                        }
                        break;

                    case PeripheralDeviceManager.APPCOMMAND_MEDIA_PREVIOUSTRACK:
                    case PeripheralDeviceManager.APPCOMMAND_BROWSER_BACKWARD:
                        if (SlidesView != null && SlidesView.IsPresenting)
                        {
                            SlidesView.PreviousPresenterStep();
                            Invalidate();
                            if (_audienceForm != null) _audienceForm.Invalidate();
                            return true;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                        {
                            if (SlidesView.ActiveSlideIndex > 0)
                            {
                                SlidesView.ActiveSlideIndex--;
                                Invalidate();
                            }
                            return true;
                        }
                        break;

                    case PeripheralDeviceManager.APPCOMMAND_MEDIA_PLAY_PAUSE:
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                        {
                            if (SlidesView.IsPresenting) StopSlideShow();
                            else StartSlideShow(false);
                            return true;
                        }
                        break;

                    case PeripheralDeviceManager.APPCOMMAND_SAVE:
                        PerformQuickSave();
                        return true;

                    case PeripheralDeviceManager.APPCOMMAND_PRINT:
                        PerformPrint();
                        return true;

                    case PeripheralDeviceManager.APPCOMMAND_UNDO:
                        PerformQuickUndo();
                        return true;

                    case PeripheralDeviceManager.APPCOMMAND_REDO:
                        PerformQuickRedo();
                        return true;
                }
            }
            catch { }
            return false;
        }

        private void ApplyPinchZoomDelta(float deltaZoom, Point screenPt)
        {
            Point clientPt = PointToClient(screenPt);
            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
            {
                float target = Math.Max(0.20f, Math.Min(2.50f, WriterView.ZoomLevel + deltaZoom));
                WriterView.ZoomTo(target, clientPt, Ribbon);
                ZoomLevel = WriterView.ZoomLevel;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
            {
                if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                CalcView.ZoomFactor = Math.Max(0.20f, Math.Min(4.00f, CalcView.ZoomFactor + deltaZoom));
                ZoomLevel = CalcView.ZoomFactor;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
            {
                SlidesView.ZoomLevel = Math.Max(0.10f, Math.Min(4.00f, SlidesView.ZoomLevel + deltaZoom));
                ZoomLevel = SlidesView.ZoomLevel;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
            {
                Point centerView = new Point(ClientSize.Width / 2, 155 + (ClientSize.Height - 155 - 26) / 2);
                PdfView.ZoomAt(centerView, PdfView.ZoomLevel + deltaZoom, ClientSize.Width, ClientSize.Height, 155, 26);
                ZoomLevel = PdfView.ZoomLevel;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                PhotoView.Zoom = Math.Max(0.10f, Math.Min(5.00f, PhotoView.Zoom + deltaZoom));
                ZoomLevel = PhotoView.Zoom;
            }
            Invalidate();
        }

        private void PerformTouchPanDelta(int dx, int dy, Point startPt = default(Point))
        {
            Point pt = startPt.IsEmpty ? _touchPanStartPt : startPt;

            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
            {
                int maxScrollY = WriterView.GetMaxScrollY(ClientSize.Height, Ribbon);
                WriterView.TargetScrollY = Math.Max(0, Math.Min(maxScrollY, WriterView.TargetScrollY - dy * 1.4f));
                WriterView.ScrollY = (int)WriterView.TargetScrollY;
                int maxScrollX = WriterView.GetMaxScrollX(ClientSize.Width);
                if (maxScrollX > 0)
                {
                    WriterView.TargetScrollX = Math.Max(0f, Math.Min(maxScrollX, WriterView.TargetScrollX - dx * 1.4f));
                    WriterView.ScrollX = WriterView.TargetScrollX;
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
            {
                if (Math.Abs(dy) >= 12)
                {
                    int rStep = -dy / 14;
                    CalcView.ScrollRow = (uint)Math.Max(1, (int)CalcView.ScrollRow + rStep);
                }
                if (Math.Abs(dx) >= 24)
                {
                    int cStep = -dx / 28;
                    CalcView.ScrollCol = (uint)Math.Max(1, (int)CalcView.ScrollCol + cStep);
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
            {
                int leftSidebarW, canvasX, canvasW, contentY, contentH, bottomThumbH;
                SlidesView.GetEffectiveLayoutMetrics(ClientSize.Width, ClientSize.Height, out leftSidebarW, out canvasX, out canvasW, out contentY, out contentH, out bottomThumbH);

                if (pt.X < canvasX)
                {
                    // Touch initiated in Left Thumbnail Sidebar or Elements Drawer
                    if (SlidesView.IsElementsDrawerOpen)
                    {
                        SlidesView.TargetElementsScrollY = Math.Max(0, SlidesView.TargetElementsScrollY - dy * 1.5f);
                        SlidesView.ElementsScrollY = (int)SlidesView.TargetElementsScrollY;
                    }
                    else
                    {
                        int maxThumb = SlidesView.GetMaxThumbScrollY();
                        SlidesView.TargetThumbScrollY = Math.Max(0, Math.Min(maxThumb, SlidesView.TargetThumbScrollY - dy * 1.5f));
                        SlidesView.ThumbScrollY = (int)SlidesView.TargetThumbScrollY;
                    }
                }
                else if (bottomThumbH > 0 && pt.Y >= ClientSize.Height - 26 - bottomThumbH)
                {
                    // Touch initiated in Bottom Filmstrip
                    SlidesView.TargetBottomThumbScrollX = Math.Max(0, SlidesView.TargetBottomThumbScrollX - dx * 1.5f);
                    SlidesView.BottomThumbScrollX = (int)SlidesView.TargetBottomThumbScrollX;
                }
                else if (SlidesView.GetEffectiveNotesPaneHeight() > 0 && pt.Y >= ClientSize.Height - 26 - bottomThumbH - SlidesView.GetEffectiveNotesPaneHeight())
                {
                    // Touch initiated in Speaker Notes pane
                    SlidesView.NotesScrollY = Math.Max(0, SlidesView.NotesScrollY - dy);
                }
                else
                {
                    // Touch initiated on main slide canvas
                    int minX, maxX, minY, maxY;
                    SlidesView.GetScrollLimits(canvasW, contentH, out minX, out maxX, out minY, out maxY);
                    bool isZoomed = (minX != 0 || maxX != 0 || minY != 0 || maxY != 0);

                    if (isZoomed)
                    {
                        SlidesView.TargetCanvasScrollX = Math.Max(minX, Math.Min(maxX, SlidesView.TargetCanvasScrollX - dx));
                        SlidesView.TargetCanvasScrollY = Math.Max(minY, Math.Min(maxY, SlidesView.TargetCanvasScrollY - dy));
                        SlidesView.CanvasScrollX = (int)SlidesView.TargetCanvasScrollX;
                        SlidesView.CanvasScrollY = (int)SlidesView.TargetCanvasScrollY;
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
            {
                int viewH = Math.Max(100, ClientSize.Height - 155 - 26);
                if (PdfView.ShowThumbnailsSidebar && pt.X <= PdfView.SidebarWidth)
                {
                    int maxSidebarScroll = PdfView.GetMaxSidebarScrollY(viewH - 36);
                    PdfView.TargetSidebarScrollY = Math.Max(0, Math.Min(maxSidebarScroll, PdfView.TargetSidebarScrollY - dy * 1.4f));
                    PdfView.SidebarScrollY = PdfView.TargetSidebarScrollY;
                }
                else
                {
                    int maxScroll = PdfView.GetMaxScrollY(viewH);
                    PdfView.TargetScrollY = Math.Max(0, Math.Min(maxScroll, PdfView.TargetScrollY - dy * 1.4f));
                    PdfView.ScrollY = PdfView.TargetScrollY;
                    PdfView.UpdateActivePageFromScroll(ClientSize.Width, ClientSize.Height, 155, 26);
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
            {
                PhotoView.PanX += dx;
                PhotoView.PanY += dy;
            }
        }

        private void PerformTouchPanEnd(int accumDx, int accumDy, Point startPt = default(Point))
        {
            Point pt = startPt.IsEmpty ? _touchPanStartPt : startPt;
            if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
            {
                int leftSidebarW, canvasX, canvasW, contentY, contentH, bottomThumbH;
                SlidesView.GetEffectiveLayoutMetrics(ClientSize.Width, ClientSize.Height, out leftSidebarW, out canvasX, out canvasW, out contentY, out contentH, out bottomThumbH);

                if (pt.X >= canvasX && (bottomThumbH == 0 || pt.Y < ClientSize.Height - 26 - bottomThumbH))
                {
                    int minX, maxX, minY, maxY;
                    SlidesView.GetScrollLimits(canvasW, contentH, out minX, out maxX, out minY, out maxY);
                    bool isZoomed = (minX != 0 || maxX != 0 || minY != 0 || maxY != 0);

                    // When canvas is not zoomed in, horizontal swipe switches slides (Slide Transition)
                    if (!isZoomed && Math.Abs(accumDx) >= 45 && Math.Abs(accumDx) > Math.Abs(accumDy))
                    {
                        int prevIdx = SlidesView.ActiveSlideIndex;
                        if (accumDx < 0 && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                        {
                            // Swipe Left -> Next Slide
                            SlidesView.ActiveSlideIndex++;
                            SlidesView.SelectedShapeIndex = -1;
                            if (SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                            {
                                SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, false);
                            }
                            Invalidate();
                        }
                        else if (accumDx > 0 && SlidesView.ActiveSlideIndex > 0)
                        {
                            // Swipe Right -> Previous Slide
                            SlidesView.ActiveSlideIndex--;
                            SlidesView.SelectedShapeIndex = -1;
                            if (SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                            {
                                SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, true);
                            }
                            Invalidate();
                        }
                    }
                }
            }
        }

        private void DrawFloatingTouchActionBar(D2DGraphics g, int width, int height)
        {
            string[] glyphs = new string[]
            {
                "\uE815", // TouchPointer (Cuộn tay)
                "\uE8B0", // Select / Touch (Chọn)
                "\uED63", // Pen (Bút vẽ)
                "\uE7E6", // Highlight (Dạ quang)
                "\uE75C", // EraseTool (Tẩy)
                "\uE765", // KeyboardStandard (Phím ảo)
                "\uE71F", // ZoomOut (-)
                "\uE8A3", // ZoomIn (+)
                "\uE7A7", // Undo (Hoàn tác)
                "\uE772"  // Devices (Thiết bị)
            };

            int btnW = 38;
            int btnH = 34;
            int pad = 5;
            int gap = 4;
            int totalBtnCount = glyphs.Length;

            int barW = btnW + pad * 2; // 48px
            int barH = pad * 2 + totalBtnCount * btnH + (totalBtnCount - 1) * gap; // ~386px
            int barX = width - barW - 32;

            int availTop = (Ribbon != null && Ribbon.ShowRuler ? 190 : 165);
            int availBottom = height - 26;
            int barY = Math.Max(availTop + 8, (availTop + availBottom - barH) / 2);
            if (barY + barH > height - 28) barY = Math.Max(availTop, height - 28 - barH);

            _touchBarBounds = new Rectangle(barX, barY, barW, barH);
            _touchBarBtnBounds.Clear();

            System.Drawing.Drawing2D.SmoothingMode oldSm = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Soft shadow + dark translucent vertical pill dock
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
            using (SolidBrush pillBg = new SolidBrush(Color.FromArgb(238, 15, 23, 42)))
            using (Pen pillBorder = new Pen(Color.FromArgb(90, 148, 163, 184), 1f))
            {
                g.FillRectangle(shadow, barX + 2, barY + 3, barW, barH);
                g.FillRectangle(pillBg, _touchBarBounds);
                g.DrawRectangle(pillBorder, _touchBarBounds);
            }

            int curY = barY + pad;
            int btnX = barX + pad;
            Point mousePt = PointToClient(Cursor.Position);

            for (int i = 0; i < totalBtnCount; i++)
            {
                Rectangle r = new Rectangle(btnX, curY, btnW, btnH);
                _touchBarBtnBounds.Add(r);

                bool isActive = false;
                if (i == 0) isActive = AppSettings.Instance.TouchPanActive && Ribbon.ActiveDrawTool == 0;
                else if (i == 1) isActive = !AppSettings.Instance.TouchPanActive && Ribbon.ActiveDrawTool == 0;
                else if (i == 2) isActive = (Ribbon.ActiveDrawTool == 1 || Ribbon.ActiveDrawTool == 2 || Ribbon.ActiveDrawTool == 3 || (Ribbon.ActiveDrawTool == 7 && !Ribbon.CustomPenIsHighlighter));
                else if (i == 3) isActive = (Ribbon.ActiveDrawTool == 4 || Ribbon.ActiveDrawTool == 5 || (Ribbon.ActiveDrawTool == 7 && Ribbon.CustomPenIsHighlighter));
                else if (i == 4) isActive = (Ribbon.ActiveDrawTool == 6);

                bool isHover = r.Contains(mousePt);
                Color btnBg = isActive
                    ? Color.FromArgb(37, 99, 235)
                    : (isHover ? Color.FromArgb(51, 65, 85) : Color.FromArgb(30, 41, 59));

                using (SolidBrush bBg = new SolidBrush(btnBg))
                {
                    g.FillRectangle(bBg, r);
                    if (isActive)
                    {
                        using (Pen pAct = new Pen(Color.FromArgb(147, 197, 253), 1.2f))
                        {
                            g.DrawRectangle(pAct, r);
                        }
                    }

                    FluentIconRenderer.DrawGlyph(g, glyphs[i], r, Color.White, 11f);
                }

                curY += btnH + gap;
            }

            g.SmoothingMode = oldSm;
        }

        private bool HandleFloatingTouchBarMouseDown(Point pt)
        {
            if (_touchBarBounds.IsEmpty || !_touchBarBounds.Contains(pt)) return false;
            for (int i = 0; i < _touchBarBtnBounds.Count; i++)
            {
                if (_touchBarBtnBounds[i].Contains(pt))
                {
                    switch (i)
                    {
                        case 0: // 👆 Cuộn tay (Touch Pan)
                            AppSettings.Instance.TouchPanActive = !AppSettings.Instance.TouchPanActive;
                            if (AppSettings.Instance.TouchPanActive) Ribbon.ActiveDrawTool = 0;
                            break;
                        case 1: // 🎯 Chọn (Select)
                            AppSettings.Instance.TouchPanActive = false;
                            Ribbon.ActiveDrawTool = 0;
                            Cursor = Cursors.Default;
                            break;
                        case 2: // 🖊️ Bút vẽ (Pen)
                            AppSettings.Instance.TouchPanActive = false;
                            Ribbon.ActiveDrawTool = 1;
                            Ribbon.CustomPenIsHighlighter = false;
                            Cursor = OfficeDrawingCursorHelper.GetPenCursor(Color.Black);
                            Invalidate();
                            break;
                        case 3: // 🖍️ Dạ quang (Highlighter) - Sửa lỗi: Kích hoạt đúng công cụ dạ quang phát sáng
                            AppSettings.Instance.TouchPanActive = false;
                            Ribbon.ActiveDrawTool = 4;
                            Ribbon.CustomPenIsHighlighter = true;
                            Cursor = OfficeDrawingCursorHelper.GetHighlighterCursor(Color.FromArgb(250, 204, 21));
                            Invalidate();
                            break;
                        case 4: // 🧹 Tẩy (Eraser)
                            AppSettings.Instance.TouchPanActive = false;
                            Ribbon.ActiveDrawTool = 6;
                            Cursor = OfficeDrawingCursorHelper.GetEraserCursor();
                            Invalidate();
                            break;
                        case 5: // Phím ảo (Touch Keyboard)
                            PeripheralDeviceManager.ToggleTouchKeyboard();
                            ShowPeripheralToast("Đang mở bàn phím ảo (On-Screen Keyboard)...", null, null, 2, '\uE765');
                            break;
                        case 6: // 🔍 -
                            ApplyPinchZoomDelta(-0.10f, PointToScreen(new Point(ClientSize.Width / 2, ClientSize.Height / 2)));
                            break;
                        case 7: // 🔍 +
                            ApplyPinchZoomDelta(0.10f, PointToScreen(new Point(ClientSize.Width / 2, ClientSize.Height / 2)));
                            break;
                        case 8: // ↩️ Hoàn tác (Undo)
                            PerformQuickUndo();
                            break;
                        case 9: // 🔌 Thiết bị (Peripheral Hub)
                            OpenPeripheralHubDialog();
                            break;
                    }
                    return true;
                }
            }
            return true;
        }

        private void DrawFloatingInkingPalette(D2DGraphics g, int width, int height)
        {
            bool isPenActive = (Ribbon.ActiveDrawTool == 1 || Ribbon.ActiveDrawTool == 2 || Ribbon.ActiveDrawTool == 3 || (Ribbon.ActiveDrawTool == 7 && !Ribbon.CustomPenIsHighlighter));
            bool isHighlighterActive = (Ribbon.ActiveDrawTool == 4 || Ribbon.ActiveDrawTool == 5 || (Ribbon.ActiveDrawTool == 7 && Ribbon.CustomPenIsHighlighter));

            if (!isPenActive && !isHighlighterActive)
            {
                _inkPaletteBounds = Rectangle.Empty;
                _inkColorBtnBounds.Clear();
                _inkSizeBtnBounds.Clear();
                _inkStraightenBtnBounds = Rectangle.Empty;
                _inkModeBtnBounds = Rectangle.Empty;
                return;
            }

            int btnW = 38;
            int pad = 5;
            int palW = btnW + pad * 2; // 48px

            // Align on the right side: left of touch bar if touch bar is visible, otherwise docked to right edge
            int palX = (!_touchBarBounds.IsEmpty) ? _touchBarBounds.Left - palW - 8 : (width - palW - 32);

            int modeH = 34;
            int colorItemH = 30;
            int sepH = 6;
            int sizeItemH = 26;
            int straightenH = 34;

            int palH = pad * 2 + modeH + 4 + (6 * colorItemH) + sepH + (4 * sizeItemH) + sepH + straightenH; // ~374px

            int availTop = (Ribbon != null && Ribbon.ShowRuler ? 190 : 165);
            int availBottom = height - 26;
            int palY = (!_touchBarBounds.IsEmpty) ? _touchBarBounds.Top : Math.Max(availTop + 8, (availTop + availBottom - palH) / 2);
            if (palY + palH > height - 28) palY = Math.Max(availTop, height - 28 - palH);

            _inkPaletteBounds = new Rectangle(palX, palY, palW, palH);
            _inkColorBtnBounds.Clear();
            _inkSizeBtnBounds.Clear();

            System.Drawing.Drawing2D.SmoothingMode oldSm = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Soft shadow + dark translucent vertical palette dock
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
            using (SolidBrush pillBg = new SolidBrush(Color.FromArgb(238, 15, 23, 42)))
            using (Pen pillBorder = new Pen(Color.FromArgb(90, 148, 163, 184), 1f))
            {
                g.FillRectangle(shadow, palX + 2, palY + 3, palW, palH);
                g.FillRectangle(pillBg, _inkPaletteBounds);
                g.DrawRectangle(pillBorder, _inkPaletteBounds);
            }

            Point mousePt = PointToClient(Cursor.Position);
            int curY = palY + pad;
            int itemX = palX + pad;

            // 1. Tool Mode Indicator / Switch (🖊️ Pen or 🖍️ Highlighter)
            _inkModeBtnBounds = new Rectangle(itemX, curY, btnW, modeH);
            bool modeHover = _inkModeBtnBounds.Contains(mousePt);
            using (SolidBrush mBg = new SolidBrush(modeHover ? Color.FromArgb(51, 65, 85) : Color.FromArgb(30, 41, 59)))
            using (Pen mBorder = new Pen(isHighlighterActive ? Color.FromArgb(250, 204, 21) : Color.FromArgb(59, 130, 246), 1.2f))
            {
                g.FillRectangle(mBg, _inkModeBtnBounds);
                g.DrawRectangle(mBorder, _inkModeBtnBounds);
                string modeGlyph = isHighlighterActive ? "\uE7E6" : "\uED63";
                Color glyphColor = isHighlighterActive ? Color.FromArgb(250, 204, 21) : Color.FromArgb(96, 165, 250);
                FluentIconRenderer.DrawGlyph(g, modeGlyph, _inkModeBtnBounds, glyphColor, 12f);
            }
            curY += modeH + 4;

            // 2. Circular Color Swatches (Bảng màu ô tròn dọc từ trên xuống)
            Color[] colors = isHighlighterActive ? _highlighterPaletteColors : _penPaletteColors;
            for (int i = 0; i < colors.Length; i++)
            {
                Rectangle cRect = new Rectangle(itemX, curY, btnW, colorItemH);
                _inkColorBtnBounds.Add(cRect);

                bool isSelected = false;
                if (isHighlighterActive)
                {
                    if (Ribbon.ActiveDrawTool == 4 && i == 0) isSelected = true;
                    else if (Ribbon.ActiveDrawTool == 5 && i == 1) isSelected = true;
                    else if (Ribbon.ActiveDrawTool == 7 && Ribbon.CustomPenColor.ToArgb() == colors[i].ToArgb()) isSelected = true;
                }
                else
                {
                    if (Ribbon.ActiveDrawTool == 1 && i == 0) isSelected = true;
                    else if (Ribbon.ActiveDrawTool == 3 && i == 1) isSelected = true;
                    else if (Ribbon.ActiveDrawTool == 2 && i == 2) isSelected = true;
                    else if (Ribbon.ActiveDrawTool == 7 && Ribbon.CustomPenColor.ToArgb() == colors[i].ToArgb()) isSelected = true;
                }

                bool cHover = cRect.Contains(mousePt);
                if (cHover)
                {
                    using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    {
                        g.FillRectangle(hBrush, cRect);
                    }
                }

                // Center of the slot
                float cx = cRect.X + cRect.Width / 2.0f;
                float cy = cRect.Y + cRect.Height / 2.0f;
                float r = 10f; // 20px diameter

                using (SolidBrush dotBrush = new SolidBrush(colors[i]))
                {
                    g.FillEllipse(dotBrush, cx - r, cy - r, r * 2, r * 2);
                }

                // Circle outline
                using (Pen dotPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.0f))
                {
                    g.DrawEllipse(dotPen, cx - r, cy - r, r * 2, r * 2);
                }

                // If selected: prominent white outer ring
                if (isSelected)
                {
                    using (Pen ringPen = new Pen(Color.White, 2.0f))
                    {
                        g.DrawEllipse(ringPen, cx - r - 3.5f, cy - r - 3.5f, (r + 3.5f) * 2, (r + 3.5f) * 2);
                    }
                }

                curY += colorItemH;
            }

            // Divider 1
            curY += 2;
            using (Pen divPen = new Pen(Color.FromArgb(70, 148, 163, 184), 1f))
            {
                g.DrawLine(divPen, itemX + 4, curY, itemX + btnW - 4, curY);
            }
            curY += 4;

            // 3. Pen Size Selector (Cỡ bút)
            float[] sizes = isHighlighterActive ? new float[] { 10f, 16f, 24f, 32f } : new float[] { 1.5f, 2.5f, 4.0f, 6.0f };
            float curWidth = isHighlighterActive
                ? (Ribbon.ActiveDrawTool == 4 || Ribbon.ActiveDrawTool == 5 ? 18.0f : (Ribbon.CustomPenWidth > 0 ? Ribbon.CustomPenWidth : 18.0f))
                : (Ribbon.ActiveDrawTool <= 3 ? 2.0f : (Ribbon.CustomPenWidth > 0 ? Ribbon.CustomPenWidth : 2.5f));

            int activeSizeIdx = 0;
            float minDiff = float.MaxValue;
            for (int s = 0; s < sizes.Length; s++)
            {
                float diff = Math.Abs(sizes[s] - curWidth);
                if (diff < minDiff) { minDiff = diff; activeSizeIdx = s; }
            }

            float[] dotRadii = new float[] { 2.5f, 4.0f, 6.0f, 8.0f };
            for (int s = 0; s < sizes.Length; s++)
            {
                Rectangle sRect = new Rectangle(itemX, curY, btnW, sizeItemH);
                _inkSizeBtnBounds.Add(sRect);

                bool isSizeSelected = (s == activeSizeIdx);
                bool sHover = sRect.Contains(mousePt);

                if (isSizeSelected)
                {
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    {
                        g.FillRectangle(selBg, sRect);
                    }
                }
                else if (sHover)
                {
                    using (SolidBrush hBg = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    {
                        g.FillRectangle(hBg, sRect);
                    }
                }

                float cx = sRect.X + sRect.Width / 2.0f;
                float cy = sRect.Y + sRect.Height / 2.0f;
                float dotR = dotRadii[s];

                using (SolidBrush dotBrush = new SolidBrush(isSizeSelected ? Color.White : Color.FromArgb(203, 213, 225)))
                {
                    g.FillEllipse(dotBrush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);
                }

                curY += sizeItemH;
            }

            // Divider 2
            curY += 2;
            using (Pen divPen = new Pen(Color.FromArgb(70, 148, 163, 184), 1f))
            {
                g.DrawLine(divPen, itemX + 4, curY, itemX + btnW - 4, curY);
            }
            curY += 4;

            // 4. Auto-Straighten Toggle (Tự nắn thẳng nét nếu vẽ hơi lệch)
            _inkStraightenBtnBounds = new Rectangle(itemX, curY, btnW, straightenH);
            bool isStraightenOn = AppSettings.Instance.AutoStraightenInk;
            bool stHover = _inkStraightenBtnBounds.Contains(mousePt);

            Color stBg = isStraightenOn
                ? Color.FromArgb(37, 99, 235)
                : (stHover ? Color.FromArgb(51, 65, 85) : Color.FromArgb(30, 41, 59));

            using (SolidBrush stBrush = new SolidBrush(stBg))
            {
                g.FillRectangle(stBrush, _inkStraightenBtnBounds);
                if (isStraightenOn)
                {
                    using (Pen stBorder = new Pen(Color.FromArgb(147, 197, 253), 1.2f))
                    {
                        g.DrawRectangle(stBorder, _inkStraightenBtnBounds);
                    }
                }

                // Render straight line snap / ruler icon
                float cx = _inkStraightenBtnBounds.X + _inkStraightenBtnBounds.Width / 2.0f;
                float cy = _inkStraightenBtnBounds.Y + _inkStraightenBtnBounds.Height / 2.0f;
                Color lineCol = isStraightenOn ? Color.White : Color.FromArgb(148, 163, 184);

                using (Pen linePen = new Pen(lineCol, 2.2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                {
                    g.DrawLine(linePen, cx - 11, cy + 3, cx + 11, cy - 3);
                }
                using (SolidBrush epBrush = new SolidBrush(lineCol))
                {
                    g.FillEllipse(epBrush, cx - 13, cy + 1, 4, 4);
                    g.FillEllipse(epBrush, cx + 9, cy - 5, 4, 4);
                }
                using (Pen sqPen = new Pen(lineCol, 1.2f))
                {
                    g.DrawRectangle(sqPen, cx - 4, cy - 9, 8, 8);
                }
            }

            g.SmoothingMode = oldSm;
        }

        private bool HandleInkPaletteMouseDown(Point pt)
        {
            if (_inkPaletteBounds.IsEmpty || !_inkPaletteBounds.Contains(pt)) return false;

            bool isHighlighterActive = (Ribbon.ActiveDrawTool == 4 || Ribbon.ActiveDrawTool == 5 || (Ribbon.ActiveDrawTool == 7 && Ribbon.CustomPenIsHighlighter));

            // Mode switch clicked (toggle between Pen and Highlighter)
            if (!_inkModeBtnBounds.IsEmpty && _inkModeBtnBounds.Contains(pt))
            {
                if (isHighlighterActive)
                {
                    Ribbon.ActiveDrawTool = 1;
                    Ribbon.CustomPenIsHighlighter = false;
                    Cursor = OfficeDrawingCursorHelper.GetPenCursor(Ribbon.CustomPenColor.IsEmpty ? Color.Black : Ribbon.CustomPenColor);
                }
                else
                {
                    Ribbon.ActiveDrawTool = 4;
                    Ribbon.CustomPenIsHighlighter = true;
                    Cursor = OfficeDrawingCursorHelper.GetHighlighterCursor(Ribbon.CustomPenColor.IsEmpty ? Color.FromArgb(250, 204, 21) : Ribbon.CustomPenColor);
                }
                Invalidate();
                return true;
            }

            // Color circle clicked
            for (int i = 0; i < _inkColorBtnBounds.Count; i++)
            {
                if (_inkColorBtnBounds[i].Contains(pt))
                {
                    Color[] colors = isHighlighterActive ? _highlighterPaletteColors : _penPaletteColors;
                    if (i < colors.Length)
                    {
                        Ribbon.ActiveDrawTool = 7;
                        Ribbon.CustomPenColor = colors[i];
                        Ribbon.CustomPenIsHighlighter = isHighlighterActive;
                        if (isHighlighterActive && Ribbon.CustomPenWidth <= 0) Ribbon.CustomPenWidth = 18.0f;
                        else if (!isHighlighterActive && Ribbon.CustomPenWidth <= 0) Ribbon.CustomPenWidth = 2.5f;

                        Cursor = isHighlighterActive
                            ? OfficeDrawingCursorHelper.GetHighlighterCursor(colors[i])
                            : OfficeDrawingCursorHelper.GetPenCursor(colors[i]);
                        Invalidate();
                    }
                    return true;
                }
            }

            // Size dot clicked
            for (int s = 0; s < _inkSizeBtnBounds.Count; s++)
            {
                if (_inkSizeBtnBounds[s].Contains(pt))
                {
                    float[] sizes = isHighlighterActive ? new float[] { 10f, 16f, 24f, 32f } : new float[] { 1.5f, 2.5f, 4.0f, 6.0f };
                    if (s < sizes.Length)
                    {
                        Ribbon.ActiveDrawTool = 7;
                        Ribbon.CustomPenWidth = sizes[s];
                        Ribbon.CustomPenIsHighlighter = isHighlighterActive;
                    }
                    return true;
                }
            }

            // Auto-Straighten toggle clicked
            if (!_inkStraightenBtnBounds.IsEmpty && _inkStraightenBtnBounds.Contains(pt))
            {
                AppSettings.Instance.AutoStraightenInk = !AppSettings.Instance.AutoStraightenInk;
                string status = AppSettings.Instance.AutoStraightenInk
                    ? "BẬT (Tự động nắn thẳng nét vẽ)"
                    : "TẮT (Nét vẽ tự do)";
                ShowPeripheralToast("Tự nắn nét vẽ: " + status, null, null, 3, '\uEDC6');
                return true;
            }

            return true;
        }

        private void DrawPeripheralToastBanner(D2DGraphics g, int width, int height)
        {
            if (string.IsNullOrEmpty(_peripheralToastTitle) || DateTime.UtcNow > _peripheralToastExpiry)
            {
                _peripheralToastTitle = null;
                _peripheralToastBounds = Rectangle.Empty;
                _peripheralToastActionBounds = Rectangle.Empty;
                _peripheralToastCloseBounds = Rectangle.Empty;
                return;
            }

            using (Font fTitle = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold))
            using (Font fBtn = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
            {
                int iconW = (_peripheralToastIcon != '\0') ? 20 : 0;
                int iconGap = (_peripheralToastIcon != '\0') ? 8 : 0;
                int textW = (int)Math.Ceiling(g.MeasureString(_peripheralToastTitle, fTitle).Width);

                int actIconW = (_peripheralToastActionIcon != '\0') ? 14 : 0;
                int actIconGap = (_peripheralToastActionIcon != '\0') ? 6 : 0;
                int actTextW = !string.IsNullOrEmpty(_peripheralToastActionText)
                    ? (int)Math.Ceiling(g.MeasureString(_peripheralToastActionText, fBtn).Width)
                    : 0;
                int actW = (actTextW > 0) ? (actIconW + actIconGap + actTextW + 20) : 0;

                int closeW = 28;
                int contentW = iconW + iconGap + textW + (actW > 0 ? (16 + actW) : 0) + 12 + closeW;
                int bannerW = Math.Min(width - 40, contentW + 28);
                int bannerH = 40;
                int bannerX = Math.Max(20, (width - bannerW) / 2);
                int ribbonH = (Ribbon != null && Ribbon.CurrentMode == OfficeAppMode.Home) ? 64 : 155;
                int bannerY = ribbonH + 10;

                _peripheralToastBounds = new Rectangle(bannerX, bannerY, bannerW, bannerH);

                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(245, 15, 23, 42)))
                using (Pen border = new Pen(Color.FromArgb(59, 130, 246), 1.5f))
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    g.FillRectangle(shadow, bannerX + 2, bannerY + 3, bannerW, bannerH);
                    g.FillRectangle(bg, _peripheralToastBounds);
                    g.DrawRectangle(border, _peripheralToastBounds);

                    int curX = bannerX + 14;
                    if (_peripheralToastIcon != '\0')
                    {
                        RectangleF iconRect = new RectangleF(curX, bannerY + (bannerH - 18) / 2f, 18, 18);
                        FluentIconRenderer.DrawGlyph(g, _peripheralToastIcon, iconRect, Color.FromArgb(96, 165, 250), 15f);
                        curX += iconW + iconGap;
                    }

                    float titleY = bannerY + (bannerH - g.MeasureString(_peripheralToastTitle, fTitle).Height) / 2f;
                    g.DrawString(_peripheralToastTitle, fTitle, white, curX, titleY);

                    int rightX = bannerX + bannerW - 8;
                    _peripheralToastCloseBounds = new Rectangle(rightX - closeW, bannerY + 6, closeW, 28);

                    RectangleF closeIconRect = new RectangleF(
                        _peripheralToastCloseBounds.X + (_peripheralToastCloseBounds.Width - 12) / 2f,
                        _peripheralToastCloseBounds.Y + (_peripheralToastCloseBounds.Height - 12) / 2f,
                        12, 12);
                    FluentIconRenderer.DrawGlyph(g, '\uE8BB', closeIconRect, Color.FromArgb(203, 213, 225), 10f);

                    rightX -= (closeW + 8);
                    if (actW > 0 && !string.IsNullOrEmpty(_peripheralToastActionText))
                    {
                        _peripheralToastActionBounds = new Rectangle(rightX - actW, bannerY + 6, actW, 28);
                        using (SolidBrush actBg = new SolidBrush(Color.FromArgb(37, 99, 235)))
                        using (Pen actBorder = new Pen(Color.FromArgb(59, 130, 246), 1f))
                        {
                            g.FillRectangle(actBg, _peripheralToastActionBounds);
                            g.DrawRectangle(actBorder, _peripheralToastActionBounds);

                            int btnCurX = _peripheralToastActionBounds.X + 10;
                            if (_peripheralToastActionIcon != '\0')
                            {
                                RectangleF actIconRect = new RectangleF(
                                    btnCurX,
                                    _peripheralToastActionBounds.Y + (_peripheralToastActionBounds.Height - 14) / 2f,
                                    14, 14);
                                FluentIconRenderer.DrawGlyph(g, _peripheralToastActionIcon, actIconRect, Color.White, 12f);
                                btnCurX += actIconW + actIconGap;
                            }

                            float btnTextY = _peripheralToastActionBounds.Y + (_peripheralToastActionBounds.Height - g.MeasureString(_peripheralToastActionText, fBtn).Height) / 2f;
                            g.DrawString(_peripheralToastActionText, fBtn, white, btnCurX, btnTextY);
                        }
                    }
                    else
                    {
                        _peripheralToastActionBounds = Rectangle.Empty;
                    }
                }
            }
        }

        private bool HandlePeripheralToastMouseDown(Point pt)
        {
            if (_peripheralToastBounds.IsEmpty || !_peripheralToastBounds.Contains(pt))
                return false;

            if (!_peripheralToastCloseBounds.IsEmpty && _peripheralToastCloseBounds.Contains(pt))
            {
                _peripheralToastTitle = null;
                _peripheralToastExpiry = DateTime.MinValue;
                Invalidate();
                return true;
            }

            if (!_peripheralToastActionBounds.IsEmpty && _peripheralToastActionBounds.Contains(pt))
            {
                Action cb = _peripheralToastActionCallback;
                _peripheralToastTitle = null;
                _peripheralToastExpiry = DateTime.MinValue;
                Invalidate();
                if (cb != null) cb();
                return true;
            }

            return true;
        }
    }
}
