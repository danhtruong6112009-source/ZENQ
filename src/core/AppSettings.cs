using System;
using System.IO;
using System.Xml;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    public enum RibbonStyle
    {
        Island = 0,   // Dạng Đảo (Floating Island Ribbon phong cách hiện đại)
        Standard = 1  // Tiêu chuẩn (Classic Full-Width Ribbon)
    }

    public enum AppTheme
    {
        Light = 0,    // Giao diện Sáng (Thanh lịch)
        Dark = 1,     // Giao diện Tối (Dịu mắt)
        Glass = 2     // Kính lỏng (Liquid Glass / Acrylic)
    }

    public enum AppLanguage
    {
        Vietnamese = 0, // Tiếng Việt
        English = 1     // English (US)
    }

    public class AppSettings
    {
        private static AppSettings _instance;
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public RibbonStyle RibbonStyle { get; set; }
        public AppTheme Theme { get; set; }
        public AppLanguage Language { get; set; }
        public bool AutoSaveEnabled { get; set; }
        public int AutoSaveIntervalSeconds { get; set; }
        public bool EnableSmoothScroll { get; set; }
        public bool ShowRuler { get; set; }
        public bool ShowFormattingMarks { get; set; }
        public float DefaultZoom { get; set; }
        public bool AiCopilotEnabled { get; set; }
        public int SelectedAiProvider { get; set; }
        public bool AutoCorrectVietnamese { get; set; }
        public bool WordPredictionEnabled { get; set; }
        public bool TouchModeEnabled { get; set; }
        public bool TouchPanActive { get; set; }
        public bool AutoSwitchPenInk { get; set; }
        public bool PenPressureSensitivity { get; set; }
        public bool AutoSummonTouchKeyboard { get; set; }
        public bool CalcBarcodeScannerMode { get; set; }
        public bool PeripheralAutoNotify { get; set; }
        public bool AutoStraightenInk { get; set; } = true;
        public bool LaunchAsStandaloneBranch { get; set; } = true;
        public OfficeAppMode DefaultBranchApp { get; set; } = OfficeAppMode.Writer;
        public bool RegisterFileAssociationsOnStartup { get; set; } = false;
        public string LastNotifiedUpdateVersion { get; set; } = "";

        public event EventHandler SettingsChanged;

        public AppSettings()
        {
            RibbonStyle = RibbonStyle.Island; // Mặc định kiểu Dạng Đảo đẹp mắt theo yêu cầu
            Theme = AppTheme.Light;
            Language = AppLanguage.Vietnamese;
            AutoSaveEnabled = true;
            AutoSaveIntervalSeconds = 60;
            EnableSmoothScroll = true;
            ShowRuler = true;
            ShowFormattingMarks = false;
            DefaultZoom = 1.0f;
            AiCopilotEnabled = true;
            SelectedAiProvider = 0;
            AutoCorrectVietnamese = false;
            WordPredictionEnabled = false;
            TouchModeEnabled = false;
            TouchPanActive = false;
            AutoSwitchPenInk = true;
            PenPressureSensitivity = true;
            AutoSummonTouchKeyboard = true;
            CalcBarcodeScannerMode = false;
            PeripheralAutoNotify = true;
            AutoStraightenInk = true;
            LaunchAsStandaloneBranch = true;
            DefaultBranchApp = OfficeAppMode.Writer;
            RegisterFileAssociationsOnStartup = false;
        }

        public static string GetSettingsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "settings.xml");
        }

        public void NotifyChanged()
        {
            if (SettingsChanged != null)
            {
                SettingsChanged(this, EventArgs.Empty);
            }
        }

        public void Save()
        {
            try
            {
                string path = GetSettingsFilePath();
                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("GenOfficeSettings");
                doc.AppendChild(root);

                XmlElement elRibbon = doc.CreateElement("RibbonStyle");
                elRibbon.InnerText = ((int)RibbonStyle).ToString();
                root.AppendChild(elRibbon);

                XmlElement elTheme = doc.CreateElement("Theme");
                elTheme.InnerText = ((int)Theme).ToString();
                root.AppendChild(elTheme);

                XmlElement elLang = doc.CreateElement("Language");
                elLang.InnerText = ((int)Language).ToString();
                root.AppendChild(elLang);

                XmlElement elAutoSave = doc.CreateElement("AutoSaveEnabled");
                elAutoSave.InnerText = AutoSaveEnabled.ToString();
                root.AppendChild(elAutoSave);

                XmlElement elAutoInt = doc.CreateElement("AutoSaveIntervalSeconds");
                elAutoInt.InnerText = AutoSaveIntervalSeconds.ToString();
                root.AppendChild(elAutoInt);

                XmlElement elSmooth = doc.CreateElement("EnableSmoothScroll");
                elSmooth.InnerText = EnableSmoothScroll.ToString();
                root.AppendChild(elSmooth);

                XmlElement elRuler = doc.CreateElement("ShowRuler");
                elRuler.InnerText = ShowRuler.ToString();
                root.AppendChild(elRuler);

                XmlElement elMarks = doc.CreateElement("ShowFormattingMarks");
                elMarks.InnerText = ShowFormattingMarks.ToString();
                root.AppendChild(elMarks);

                XmlElement elZoom = doc.CreateElement("DefaultZoom");
                elZoom.InnerText = DefaultZoom.ToString(System.Globalization.CultureInfo.InvariantCulture);
                root.AppendChild(elZoom);

                XmlElement elAiCopilot = doc.CreateElement("AiCopilotEnabled");
                elAiCopilot.InnerText = AiCopilotEnabled.ToString();
                root.AppendChild(elAiCopilot);

                XmlElement elAiProv = doc.CreateElement("SelectedAiProvider");
                elAiProv.InnerText = SelectedAiProvider.ToString();
                root.AppendChild(elAiProv);

                XmlElement elAutoCorr = doc.CreateElement("AutoCorrectVietnamese");
                elAutoCorr.InnerText = AutoCorrectVietnamese.ToString();
                root.AppendChild(elAutoCorr);

                XmlElement elWordPred = doc.CreateElement("WordPredictionEnabled");
                elWordPred.InnerText = WordPredictionEnabled.ToString();
                root.AppendChild(elWordPred);

                XmlElement elTouchMode = doc.CreateElement("TouchModeEnabled");
                elTouchMode.InnerText = TouchModeEnabled.ToString();
                root.AppendChild(elTouchMode);

                XmlElement elPenInk = doc.CreateElement("AutoSwitchPenInk");
                elPenInk.InnerText = AutoSwitchPenInk.ToString();
                root.AppendChild(elPenInk);

                XmlElement elPenPressure = doc.CreateElement("PenPressureSensitivity");
                elPenPressure.InnerText = PenPressureSensitivity.ToString();
                root.AppendChild(elPenPressure);

                XmlElement elTouchKbd = doc.CreateElement("AutoSummonTouchKeyboard");
                elTouchKbd.InnerText = AutoSummonTouchKeyboard.ToString();
                root.AppendChild(elTouchKbd);

                XmlElement elBarcode = doc.CreateElement("CalcBarcodeScannerMode");
                elBarcode.InnerText = CalcBarcodeScannerMode.ToString();
                root.AppendChild(elBarcode);

                XmlElement elPeriphNotify = doc.CreateElement("PeripheralAutoNotify");
                elPeriphNotify.InnerText = PeripheralAutoNotify.ToString();
                root.AppendChild(elPeriphNotify);

                XmlElement elLaunchBranch = doc.CreateElement("LaunchAsStandaloneBranch");
                elLaunchBranch.InnerText = LaunchAsStandaloneBranch.ToString();
                root.AppendChild(elLaunchBranch);

                XmlElement elBranchApp = doc.CreateElement("DefaultBranchApp");
                elBranchApp.InnerText = ((int)DefaultBranchApp).ToString();
                root.AppendChild(elBranchApp);

                XmlElement elRegFiles = doc.CreateElement("RegisterFileAssociationsOnStartup");
                elRegFiles.InnerText = RegisterFileAssociationsOnStartup.ToString();
                root.AppendChild(elRegFiles);

                XmlElement elLastNotif = doc.CreateElement("LastNotifiedUpdateVersion");
                elLastNotif.InnerText = LastNotifiedUpdateVersion ?? "";
                root.AppendChild(elLastNotif);

                doc.Save(path);
                NotifyChanged();
            }
            catch { }
        }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    XmlNode root = doc.SelectSingleNode("GenOfficeSettings");
                    if (root != null)
                    {
                        XmlNode nRibbon = root.SelectSingleNode("RibbonStyle");
                        if (nRibbon != null) { int v; if (int.TryParse(nRibbon.InnerText, out v)) s.RibbonStyle = (RibbonStyle)v; }

                        XmlNode nTheme = root.SelectSingleNode("Theme");
                        if (nTheme != null) { int v; if (int.TryParse(nTheme.InnerText, out v)) s.Theme = (AppTheme)v; }

                        XmlNode nLang = root.SelectSingleNode("Language");
                        if (nLang != null) { int v; if (int.TryParse(nLang.InnerText, out v)) s.Language = (AppLanguage)v; }

                        XmlNode nAutoSave = root.SelectSingleNode("AutoSaveEnabled");
                        if (nAutoSave != null) { bool v; if (bool.TryParse(nAutoSave.InnerText, out v)) s.AutoSaveEnabled = v; }

                        XmlNode nAutoInt = root.SelectSingleNode("AutoSaveIntervalSeconds");
                        if (nAutoInt != null) { int v; if (int.TryParse(nAutoInt.InnerText, out v)) s.AutoSaveIntervalSeconds = v; }

                        XmlNode nSmooth = root.SelectSingleNode("EnableSmoothScroll");
                        if (nSmooth != null) { bool v; if (bool.TryParse(nSmooth.InnerText, out v)) s.EnableSmoothScroll = v; }

                        XmlNode nRuler = root.SelectSingleNode("ShowRuler");
                        if (nRuler != null) { bool v; if (bool.TryParse(nRuler.InnerText, out v)) s.ShowRuler = v; }

                        XmlNode nMarks = root.SelectSingleNode("ShowFormattingMarks");
                        if (nMarks != null) { bool v; if (bool.TryParse(nMarks.InnerText, out v)) s.ShowFormattingMarks = v; }

                        XmlNode nZoom = root.SelectSingleNode("DefaultZoom");
                        if (nZoom != null) { float v; if (float.TryParse(nZoom.InnerText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) s.DefaultZoom = v; }

                        XmlNode nAiCopilot = root.SelectSingleNode("AiCopilotEnabled");
                        if (nAiCopilot != null) { bool v; if (bool.TryParse(nAiCopilot.InnerText, out v)) s.AiCopilotEnabled = v; }

                        XmlNode nAiProv = root.SelectSingleNode("SelectedAiProvider");
                        if (nAiProv != null) { int v; if (int.TryParse(nAiProv.InnerText, out v)) s.SelectedAiProvider = v; }

                        XmlNode nAutoCorr = root.SelectSingleNode("AutoCorrectVietnamese");
                        if (nAutoCorr != null) { bool v; if (bool.TryParse(nAutoCorr.InnerText, out v)) s.AutoCorrectVietnamese = v; }

                        XmlNode nWordPred = root.SelectSingleNode("WordPredictionEnabled");
                        if (nWordPred != null) { bool v; if (bool.TryParse(nWordPred.InnerText, out v)) s.WordPredictionEnabled = v; }

                        XmlNode nTouchMode = root.SelectSingleNode("TouchModeEnabled");
                        if (nTouchMode != null) { bool v; if (bool.TryParse(nTouchMode.InnerText, out v)) s.TouchModeEnabled = v; }

                        XmlNode nPenInk = root.SelectSingleNode("AutoSwitchPenInk");
                        if (nPenInk != null) { bool v; if (bool.TryParse(nPenInk.InnerText, out v)) s.AutoSwitchPenInk = v; }

                        XmlNode nPenPressure = root.SelectSingleNode("PenPressureSensitivity");
                        if (nPenPressure != null) { bool v; if (bool.TryParse(nPenPressure.InnerText, out v)) s.PenPressureSensitivity = v; }

                        XmlNode nTouchKbd = root.SelectSingleNode("AutoSummonTouchKeyboard");
                        if (nTouchKbd != null) { bool v; if (bool.TryParse(nTouchKbd.InnerText, out v)) s.AutoSummonTouchKeyboard = v; }

                        XmlNode nBarcode = root.SelectSingleNode("CalcBarcodeScannerMode");
                        if (nBarcode != null) { bool v; if (bool.TryParse(nBarcode.InnerText, out v)) s.CalcBarcodeScannerMode = v; }

                        XmlNode nPeriphNotify = root.SelectSingleNode("PeripheralAutoNotify");
                        if (nPeriphNotify != null) { bool v; if (bool.TryParse(nPeriphNotify.InnerText, out v)) s.PeripheralAutoNotify = v; }

                        XmlNode nLaunchBranch = root.SelectSingleNode("LaunchAsStandaloneBranch");
                        if (nLaunchBranch != null) { bool v; if (bool.TryParse(nLaunchBranch.InnerText, out v)) s.LaunchAsStandaloneBranch = v; }

                        XmlNode nBranchApp = root.SelectSingleNode("DefaultBranchApp");
                        if (nBranchApp != null) { int v; if (int.TryParse(nBranchApp.InnerText, out v)) s.DefaultBranchApp = (OfficeAppMode)v; }

                        XmlNode nRegFiles = root.SelectSingleNode("RegisterFileAssociationsOnStartup");
                        if (nRegFiles != null) { bool v; if (bool.TryParse(nRegFiles.InnerText, out v)) s.RegisterFileAssociationsOnStartup = v; }

                        XmlNode nLastNotif = root.SelectSingleNode("LastNotifiedUpdateVersion");
                        if (nLastNotif != null) { s.LastNotifiedUpdateVersion = nLastNotif.InnerText ?? ""; }
                    }
                }
            }
            catch { }
            return s;
        }
    }
}
