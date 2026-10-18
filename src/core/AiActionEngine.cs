using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    public class SpellCorrectionItem
    {
        public string Original { get; set; }
        public string Replacement { get; set; }
        public string Context { get; set; }

        public SpellCorrectionItem() { }
        public SpellCorrectionItem(string orig, string repl, string ctx = "")
        {
            Original = orig;
            Replacement = repl;
            Context = ctx;
        }
    }

    public class AiActionResult
    {
        public bool HasAction { get; set; }
        public string ActionType { get; set; }
        public string CleanUserMessage { get; set; }
        public string ExecutionSummary { get; set; }
        public bool Success { get; set; }
        public List<SpellCorrectionItem> SpellCorrections { get; set; }

        public AiActionResult()
        {
            HasAction = false;
            ActionType = "";
            CleanUserMessage = "";
            ExecutionSummary = "";
            Success = true;
            SpellCorrections = new List<SpellCorrectionItem>();
        }
    }

    public class AiSlideCardSpec
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconOrBadge { get; set; }
        public string StatNumber { get; set; }
        public string Tag { get; set; }
        public string ShapeGeometry { get; set; }

        public AiSlideCardSpec()
        {
            Title = "";
            Description = "";
            IconOrBadge = "";
            StatNumber = "";
            Tag = "";
            ShapeGeometry = "";
        }
    }

    public class AiSlideDesignSpec
    {
        public string Layout { get; set; } // "TitleSlide", "3Cards", "2Cards", "4Cards", "Grid2x2", "ProcessSteps", "StatMetrics", "Comparison", "SmartArt", "ChartDashboard", "TableMatrix", "HexagonShowcase"
        public string ThemeStyle { get; set; } // "ModernBlue", "TechDark", "EmeraldGreen", "PurpleLuxury", "WarmSunset", "CrimsonRed", "CyberNeon", "GoldLuxury", "MinimalTitanium"
        public string CategoryBadge { get; set; }
        public string SlideTitle { get; set; }
        public string SlideSubtitle { get; set; }
        public string FooterNote { get; set; }
        public List<AiSlideCardSpec> Cards { get; set; }
        public bool CreateNewSlide { get; set; }

        // SmartArt Extended
        public string SmartArtType { get; set; }
        public string SmartArtTitle { get; set; }
        public List<string> SmartArtNodes { get; set; }

        // Chart Extended
        public string ChartType { get; set; }
        public string ChartTitle { get; set; }
        public List<string> ChartCategories { get; set; }
        public List<double> ChartValues { get; set; }

        // Table Extended
        public List<string> TableHeaders { get; set; }
        public List<List<string>> TableRows { get; set; }

        public AiSlideDesignSpec()
        {
            Layout = "3Cards";
            ThemeStyle = "ModernBlue";
            CategoryBadge = "";
            SlideTitle = "";
            SlideSubtitle = "";
            FooterNote = "";
            Cards = new List<AiSlideCardSpec>();
            CreateNewSlide = false;

            SmartArtType = "";
            SmartArtTitle = "";
            SmartArtNodes = new List<string>();

            ChartType = "";
            ChartTitle = "";
            ChartCategories = new List<string>();
            ChartValues = new List<double>();

            TableHeaders = new List<string>();
            TableRows = new List<List<string>>();
        }
    }

    internal class SlidePalette
    {
        public int Bg1;
        public int Bg2;
        public int Primary;
        public int Accent;
        public int TitleColor;
        public int SubtitleColor;
        public int BodyColor;
        public int CardBg;
        public int CardBorder;
        public int BadgeBg;
        public int BadgeFg;
        public int StatBg;
        public int StatBorder;
        public int StatText;
        public bool DarkTheme;
    }

    /// <summary>
    /// Bộ máy thực thi hành động AI 2 chiều siêu năng lực (Autonomous Slide Canvas, Sửa văn bản Word, Điền bảng tính Calc).
    /// </summary>
    public static class AiActionEngine
    {
        private static readonly Regex ActionRegex = new Regex(@"\[(?:[A-Za-z0-9_]*GENOFFICE_ACTION|ACTION):([A-Za-z0-9_]+)\]([\s\S]*?)(?:\[/(?:[A-Za-z0-9_]*GENOFFICE_ACTION|ACTION)\]|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Dictionary<string, string> ParseXmlAttributes(string tagText)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(tagText)) return dict;
            var matches = Regex.Matches(tagText, @"([a-zA-Z0-9_\-]+)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))");
            foreach (Match m in matches)
            {
                string key = m.Groups[1].Value;
                string val = m.Groups[2].Success ? m.Groups[2].Value : (m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value);
                dict[key] = val;
            }
            return dict;
        }

        private static void SetCellSmartValue(CellData cell, string val)
        {
            if (cell == null || val == null) return;
            if (val.StartsWith("="))
            {
                cell.SetFormula(val);
            }
            else
            {
                double d;
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                {
                    cell.SetNumber(d);
                }
                else
                {
                    cell.SetString(val);
                }
            }
        }

        private static double GetDoubleFromStr(string s, double def)
        {
            if (string.IsNullOrEmpty(s)) return def;
            double v;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            return def;
        }

        public static bool ExecuteXmlActions(AppForm appForm, string rawAiResponse, AiActionResult result)
        {
            if (string.IsNullOrWhiteSpace(rawAiResponse) || appForm == null) return false;

            // Kiểm tra sự xuất hiện của các thẻ XML hành động
            bool hasXmlTag = Regex.IsMatch(rawAiResponse, @"<(zenq_action|action|zenq|highlight|to_mau|format|set|cell|dien_o|create_sheet|add_sheet|tao_sheet|write|ghi|clear|xoa|create_slide|tao_slide|slide|insert_text|replace)\b", RegexOptions.IgnoreCase);
            if (!hasXmlTag) return false;

            List<string> summaries = new List<string>();
            bool anyExecuted = false;

            // 1. Xử lý Calc (Bảng tính)
            if (appForm.CalcView != null)
            {
                var calc = appForm.CalcView;
                var sheet = calc.CurrentSheet;

                // 1.1 <create_sheet name="..." /> hoặc <tao_sheet ten="..." />
                var sheetMatches = Regex.Matches(rawAiResponse, @"<(?:create_sheet|add_sheet|tao_sheet)\b([^>]*)/?>", RegexOptions.IgnoreCase);
                foreach (Match m in sheetMatches)
                {
                    var attrs = ParseXmlAttributes(m.Groups[1].Value);
                    string sName = "";
                    if (attrs.ContainsKey("name")) sName = attrs["name"];
                    else if (attrs.ContainsKey("ten")) sName = attrs["ten"];
                    else if (attrs.ContainsKey("title")) sName = attrs["title"];

                    if (!string.IsNullOrWhiteSpace(sName))
                    {
                        calc.SaveUndoState();
                        if (calc.SheetNames == null || !calc.SheetNames.Contains(sName))
                        {
                            calc.AddSheet(sName);
                        }
                        int idx = calc.SheetNames != null ? calc.SheetNames.IndexOf(sName) : -1;
                        if (idx >= 0) calc.ActiveSheetIndex = idx;
                        sheet = calc.CurrentSheet;
                        summaries.Add(string.Format("Đã tạo sheet mới '{0}'", sName));
                        anyExecuted = true;
                    }
                }

                // 1.2 <write sheet="..." cell="...">Nội dung</write> hoặc <ghi ...>...</ghi>
                var writeMatches = Regex.Matches(rawAiResponse, @"<(?:write|ghi)\b([^>]*)>(.*?)</(?:write|ghi)>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match m in writeMatches)
                {
                    var attrs = ParseXmlAttributes(m.Groups[1].Value);
                    string targetSheet = attrs.ContainsKey("sheet") ? attrs["sheet"] : (attrs.ContainsKey("ten_sheet") ? attrs["ten_sheet"] : "");
                    string startCellRef = attrs.ContainsKey("cell") ? attrs["cell"] : (attrs.ContainsKey("ref") ? attrs["ref"] : "A1");
                    string content = m.Groups[2].Value.Trim();

                    SparseMatrix targetMatrix = sheet;
                    if (!string.IsNullOrEmpty(targetSheet))
                    {
                        int sIdx = calc.SheetNames != null ? calc.SheetNames.IndexOf(targetSheet) : -1;
                        if (sIdx >= 0)
                        {
                            calc.ActiveSheetIndex = sIdx;
                            targetMatrix = calc.CurrentSheet;
                        }
                        else
                        {
                            calc.AddSheet(targetSheet);
                            calc.ActiveSheetIndex = calc.Sheets.Count - 1;
                            targetMatrix = calc.CurrentSheet;
                        }
                    }

                    if (targetMatrix != null && !string.IsNullOrEmpty(content))
                    {
                        calc.SaveUndoState();
                        uint startR = 1, startC = 1;
                        if (!SparseMatrix.TryParseCellReference(startCellRef, out startR, out startC))
                        {
                            startR = 1; startC = 1;
                        }

                        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int li = 0; li < lines.Length; li++)
                        {
                            string line = lines[li].Trim();
                            if (line.StartsWith("|") && line.EndsWith("|") && line.Length > 2)
                            {
                                if (Regex.IsMatch(line, @"^\|[\s\-:|]+\|$")) continue;
                                var cols = line.Substring(1, line.Length - 2).Split('|');
                                for (int ci = 0; ci < cols.Length; ci++)
                                {
                                    string cellVal = cols[ci].Trim();
                                    SetCellSmartValue(targetMatrix.GetOrCreateCell(startR + (uint)li, startC + (uint)ci), cellVal);
                                }
                            }
                            else if (line.Contains("\t") || line.Contains(","))
                            {
                                char sep = line.Contains("\t") ? '\t' : ',';
                                var cols = line.Split(sep);
                                for (int ci = 0; ci < cols.Length; ci++)
                                {
                                    SetCellSmartValue(targetMatrix.GetOrCreateCell(startR + (uint)li, startC + (uint)ci), cols[ci].Trim());
                                }
                            }
                            else
                            {
                                SetCellSmartValue(targetMatrix.GetOrCreateCell(startR + (uint)li, startC), line);
                            }
                        }
                        summaries.Add(string.Format("Đã ghi dữ liệu vào ô {0}", startCellRef));
                        anyExecuted = true;
                    }
                }

                // 1.3 <highlight range="A1:A10" color="vàng nhạt" /> hoặc <to_mau ... /> hoặc <format ... />
                var hlMatches = Regex.Matches(rawAiResponse, @"<(?:highlight|to_mau|format)\b([^>]*)/?>", RegexOptions.IgnoreCase);
                foreach (Match m in hlMatches)
                {
                    var attrs = ParseXmlAttributes(m.Groups[1].Value);
                    string rangeStr = attrs.ContainsKey("range") ? attrs["range"] : (attrs.ContainsKey("cell") ? attrs["cell"] : (attrs.ContainsKey("vung") ? attrs["vung"] : ""));
                    string colorStr = attrs.ContainsKey("color") ? attrs["color"] : (attrs.ContainsKey("mau") ? attrs["mau"] : (attrs.ContainsKey("fill") ? attrs["fill"] : (attrs.ContainsKey("bg") ? attrs["bg"] : "vàng nhạt")));

                    uint r1 = 0, c1 = 0, r2 = 0, c2 = 0;
                    bool hasR = TryParseRange(rangeStr, out r1, out c1, out r2, out c2);
                    if (!hasR && SparseMatrix.TryParseCellReference(rangeStr, out r1, out c1))
                    {
                        r2 = r1; c2 = c1; hasR = true;
                    }

                    if (hasR && sheet != null)
                    {
                        calc.SaveUndoState();
                        int bgColor = ParseColorExtended(colorStr, unchecked((int)0xFFFEF9C3));
                        bool isBold = attrs.ContainsKey("bold") && (attrs["bold"] == "true" || attrs["bold"] == "1");

                        uint minR = Math.Min(r1, r2), maxR = Math.Max(r1, r2);
                        uint minC = Math.Min(c1, c2), maxC = Math.Max(c1, c2);

                        for (uint r = minR; r <= maxR; r++)
                        {
                            for (uint c = minC; c <= maxC; c++)
                            {
                                var cell = sheet.GetOrCreateCell(r, c);
                                if (cell.Format == null) cell.Format = new CellFormat();
                                cell.Format.BgColor = bgColor;
                                if (isBold) cell.Format.Bold = true;
                            }
                        }
                        summaries.Add(string.Format("Đã tô màu {0} cho vùng {1}", colorStr, rangeStr));
                        anyExecuted = true;
                    }
                }

                // 1.4 <set cell="A11" formula="=SUM(A1:A10)" /> hoặc <cell ref="B2" value="..." />
                var setMatches = Regex.Matches(rawAiResponse, @"<(?:set|cell|dien_o)\b([^>]*)(?:>(.*?)</(?:set|cell|dien_o)>|/?>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match m in setMatches)
                {
                    var attrs = ParseXmlAttributes(m.Groups[1].Value);
                    string cellRef = attrs.ContainsKey("cell") ? attrs["cell"] : (attrs.ContainsKey("ref") ? attrs["ref"] : (attrs.ContainsKey("o") ? attrs["o"] : ""));
                    string formula = attrs.ContainsKey("formula") ? attrs["formula"] : (attrs.ContainsKey("cong_thuc") ? attrs["cong_thuc"] : "");
                    string val = attrs.ContainsKey("value") ? attrs["value"] : (attrs.ContainsKey("gia_tri") ? attrs["gia_tri"] : "");
                    string inner = m.Groups[2].Success ? m.Groups[2].Value.Trim() : "";

                    if (string.IsNullOrEmpty(formula) && string.IsNullOrEmpty(val) && !string.IsNullOrEmpty(inner))
                    {
                        if (inner.StartsWith("=")) formula = inner;
                        else val = inner;
                    }

                    uint r = 0, c = 0;
                    if (SparseMatrix.TryParseCellReference(cellRef, out r, out c) && sheet != null)
                    {
                        calc.SaveUndoState();
                        var cell = sheet.GetOrCreateCell(r, c);
                        if (!string.IsNullOrEmpty(formula))
                        {
                            if (!formula.StartsWith("=")) formula = "=" + formula;
                            cell.SetFormula(formula);
                            summaries.Add(string.Format("Đã điền công thức {0} vào ô {1}", formula, cellRef));
                            anyExecuted = true;
                        }
                        else if (!string.IsNullOrEmpty(val))
                        {
                            SetCellSmartValue(cell, val);
                            summaries.Add(string.Format("Đã điền giá trị '{0}' vào ô {1}", val, cellRef));
                            anyExecuted = true;
                        }
                    }
                }

                // 1.5 <clear range="A1:C10" /> hoặc <xoa ... />
                var clearMatches = Regex.Matches(rawAiResponse, @"<(?:clear|xoa)\b([^>]*)/?>", RegexOptions.IgnoreCase);
                foreach (Match m in clearMatches)
                {
                    var attrs = ParseXmlAttributes(m.Groups[1].Value);
                    string rangeStr = attrs.ContainsKey("range") ? attrs["range"] : (attrs.ContainsKey("vung") ? attrs["vung"] : "");
                    uint r1 = 0, c1 = 0, r2 = 0, c2 = 0;
                    if (TryParseRange(rangeStr, out r1, out c1, out r2, out c2) && sheet != null)
                    {
                        calc.SaveUndoState();
                        uint minR = Math.Min(r1, r2), maxR = Math.Max(r1, r2);
                        uint minC = Math.Min(c1, c2), maxC = Math.Max(c1, c2);
                        for (uint r = minR; r <= maxR; r++)
                        {
                            for (uint c = minC; c <= maxC; c++)
                            {
                                var cell = sheet.GetOrCreateCell(r, c);
                                cell.Type = CellType.Empty;
                                cell.RawValue = "";
                                cell.FormulaStr = "";
                                cell.NumericValue = 0;
                                cell.EvaluatedStr = "";
                                if (cell.Format != null) cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                            }
                        }
                        summaries.Add(string.Format("Đã xóa dữ liệu vùng {0}", rangeStr));
                        anyExecuted = true;
                    }
                }
            }

            // 2. Xử lý Slides (Tự do tạo slide bằng XML)
            if (appForm.SlidesView != null)
            {
                var slides = appForm.SlidesView;
                var pres = slides.Presentation;

                var slideMatches = Regex.Matches(rawAiResponse, @"<(?:create_slide|tao_slide|slide)\b([^>]*)>(.*?)</(?:create_slide|tao_slide|slide)>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match sm in slideMatches)
                {
                    var sAttrs = ParseXmlAttributes(sm.Groups[1].Value);
                    string title = sAttrs.ContainsKey("title") ? sAttrs["title"] : (sAttrs.ContainsKey("tieu_de") ? sAttrs["tieu_de"] : "Slide mới");
                    string bgStr = sAttrs.ContainsKey("background") ? sAttrs["background"] : (sAttrs.ContainsKey("bg") ? sAttrs["bg"] : (sAttrs.ContainsKey("nen") ? sAttrs["nen"] : "#0B0F19"));

                    slides.SaveUndoState();
                    SlideNode newSlide = new SlideNode
                    {
                        Title = title,
                        Shapes = new List<ShapeNode>(),
                        Transition = TransitionType.Fade,
                        BgFillType = BackgroundFillType.Solid,
                        BgColor = ParseColorExtended(bgStr, unchecked((int)0xFF0B0F19))
                    };

                    string inner = sm.Groups[2].Value;
                    // Parse các thẻ con: <shape ... /> hoặc <text ...>...</text>
                    var childMatches = Regex.Matches(inner, @"<(shape|text|hinh|chu)\b([^>]*)(?:>(.*?)</\1>|/?>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    int elemCount = 0;
                    foreach (Match cm in childMatches)
                    {
                        string tag = cm.Groups[1].Value.ToLowerInvariant();
                        var cAttrs = ParseXmlAttributes(cm.Groups[2].Value);
                        float x = (float)GetDoubleFromStr(cAttrs.ContainsKey("x") ? cAttrs["x"] : "100", 100);
                        float y = (float)GetDoubleFromStr(cAttrs.ContainsKey("y") ? cAttrs["y"] : "150", 150);
                        float w = (float)GetDoubleFromStr(cAttrs.ContainsKey("width") ? cAttrs["width"] : (cAttrs.ContainsKey("w") ? cAttrs["w"] : "400"), 400);
                        float h = (float)GetDoubleFromStr(cAttrs.ContainsKey("height") ? cAttrs["height"] : (cAttrs.ContainsKey("h") ? cAttrs["h"] : "180"), 180);
                        string text = cAttrs.ContainsKey("text") ? cAttrs["text"] : (cm.Groups[3].Success ? cm.Groups[3].Value.Trim() : "");

                        if (tag == "text" || tag == "chu")
                        {
                            float fSize = (float)GetDoubleFromStr(cAttrs.ContainsKey("font_size") ? cAttrs["font_size"] : (cAttrs.ContainsKey("size") ? cAttrs["size"] : "20"), 20);
                            bool isBold = cAttrs.ContainsKey("bold") && (cAttrs["bold"] == "true" || cAttrs["bold"] == "1");
                            int txtColor = ParseColorExtended(cAttrs.ContainsKey("color") ? cAttrs["color"] : "#FFFFFF", unchecked((int)0xFFFFFFFF));
                            var txtNode = CreateCleanTextBox(text, "Segoe UI", fSize, isBold, txtColor, TextAlignment.Left, x, y, w, h);
                            newSlide.Shapes.Add(txtNode);
                            elemCount++;
                        }
                        else
                        {
                            ShapeType shType = ShapeType.RoundedRectangle;
                            string typeStr = cAttrs.ContainsKey("type") ? cAttrs["type"].ToLowerInvariant() : "";
                            if (typeStr == "rectangle" || typeStr == "hinh_chu_nhat") shType = ShapeType.Rectangle;
                            else if (typeStr == "circle" || typeStr == "ellipse" || typeStr == "hinh_tron") shType = ShapeType.Ellipse;

                            int fillColor = ParseColorExtended(cAttrs.ContainsKey("fill") ? cAttrs["fill"] : (cAttrs.ContainsKey("color") ? cAttrs["color"] : "#1E293B"), unchecked((int)0xFF1E293B));
                            int strokeColor = ParseColorExtended(cAttrs.ContainsKey("stroke") ? cAttrs["stroke"] : (cAttrs.ContainsKey("vien") ? cAttrs["vien"] : "#38BDF8"), unchecked((int)0xFF38BDF8));
                            float strokeW = (float)GetDoubleFromStr(cAttrs.ContainsKey("stroke_width") ? cAttrs["stroke_width"] : "1.5", 1.5);

                            ShapeNode shp = new ShapeNode
                            {
                                Type = shType,
                                FillColor = fillColor,
                                FillOpacity = 1.0f,
                                StrokeColor = strokeColor,
                                StrokeWidth = strokeW,
                                Transform = new Transform2D(x, y, w, h),
                                Text = text
                            };
                            newSlide.Shapes.Add(shp);
                            elemCount++;
                        }
                    }

                    if (pres != null)
                    {
                        pres.Slides.Add(newSlide);
                        slides.ActiveSlideIndex = pres.Slides.Count - 1;
                        summaries.Add(string.Format("Đã tạo slide '{0}' với {1} thành phần", title, elemCount));
                        anyExecuted = true;
                    }
                }
            }

            // 3. Xử lý Writer (Chèn văn bản và thay thế từ)
            if (appForm.WriterView != null)
            {
                var writer = appForm.WriterView;
                var insertMatches = Regex.Matches(rawAiResponse, @"<insert_text\b[^>]*>(.*?)</insert_text>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match m in insertMatches)
                {
                    string txt = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(txt))
                    {
                        writer.InsertTextFromAI(txt);
                        summaries.Add("Đã chèn nội dung vào văn bản");
                        anyExecuted = true;
                    }
                }

                var replaceMatches = Regex.Matches(rawAiResponse, @"<replace\b([^>]*)/?>", RegexOptions.IgnoreCase);
                foreach (Match m in replaceMatches)
                {
                    var attrs = ParseXmlAttributes(m.Groups[1].Value);
                    if (attrs.ContainsKey("find") && attrs.ContainsKey("replace"))
                    {
                        string f = attrs["find"];
                        string r = attrs["replace"];
                        int c = writer.ReplaceAll(f, r);
                        summaries.Add(string.Format("Đã thay thế '{0}' thành '{1}' ({2} lần)", f, r, c));
                        anyExecuted = true;
                    }
                }
            }

            if (anyExecuted)
            {
                result.HasAction = true;
                result.ActionType = "XML_ACTION";
                result.Success = true;
                result.ExecutionSummary = "✅ " + string.Join("; ", summaries.ToArray());

                // Làm sạch thẻ XML khỏi message trả lời để hiển thị cho người dùng
                string clean = rawAiResponse;
                clean = Regex.Replace(clean, @"</?(?:zenq_action|action|zenq)\b[^>]*>", "", RegexOptions.IgnoreCase);
                clean = Regex.Replace(clean, @"<(?:highlight|to_mau|format|set|cell|dien_o|create_sheet|add_sheet|tao_sheet|clear|xoa|replace)\b[^>]*\/?>", "", RegexOptions.IgnoreCase);
                clean = Regex.Replace(clean, @"<(?:write|ghi|create_slide|tao_slide|slide|insert_text)\b[^>]*>[\s\S]*?<\/(?:write|ghi|create_slide|tao_slide|slide|insert_text)>", "", RegexOptions.IgnoreCase);
                result.CleanUserMessage = clean.Trim();
                if (string.IsNullOrEmpty(result.CleanUserMessage))
                {
                    result.CleanUserMessage = result.ExecutionSummary;
                }

                appForm.Invalidate();
                return true;
            }

            return false;
        }

        public static AiActionResult ProcessAndExecute(AppForm appForm, string rawAiResponse)
        {
            AiActionResult result = new AiActionResult();
            if (string.IsNullOrEmpty(rawAiResponse))
            {
                result.CleanUserMessage = "";
                return result;
            }

            // 0. Ưu tiên kiểm tra và thực thi các thẻ XML tự do theo yêu cầu người dùng
            if (ExecuteXmlActions(appForm, rawAiResponse, result))
            {
                return result;
            }

            Match match = ActionRegex.Match(rawAiResponse);
            if (!match.Success)
            {
                if (appForm != null && appForm.Ribbon != null)
                {
                    if (appForm.Ribbon.CurrentMode == OfficeAppMode.Slides)
                    {
                        if (rawAiResponse.IndexOf("## Slide", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            rawAiResponse.IndexOf("Slide 1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            rawAiResponse.IndexOf("**[Shape", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            rawAiResponse.IndexOf("**[Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            rawAiResponse.IndexOf("LAYOUT:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            result.HasAction = true;
                            result.ActionType = "DESIGN_SLIDE";
                            ExecuteDesignSlide(appForm, rawAiResponse, result);
                            result.CleanUserMessage = rawAiResponse.Trim();
                            return result;
                        }
                    }
                    else if (appForm.Ribbon.CurrentMode == OfficeAppMode.Calc)
                    {
                        // 1. Check for raw JSON containing "cells" or "ranges"
                        int jsonStart = rawAiResponse.IndexOf("{");
                        int jsonEnd = rawAiResponse.LastIndexOf("}");
                        if (jsonStart >= 0 && jsonEnd > jsonStart)
                        {
                            string jsonCandidate = rawAiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                            if (jsonCandidate.Contains("\"cells\"") || jsonCandidate.Contains("\"ranges\""))
                            {
                                result.HasAction = true;
                                result.ActionType = "SET_CALC_DATA";
                                ExecuteSetCalcData(appForm, jsonCandidate, result);
                                result.CleanUserMessage = rawAiResponse.Substring(0, jsonStart).Trim();
                                return result;
                            }
                        }

                        // 2. Check for Markdown Table (| A | B | C |)
                        if (TryExecuteMarkdownTableToCalc(appForm, rawAiResponse, result))
                        {
                            return result;
                        }
                    }
                }

                result.CleanUserMessage = rawAiResponse.Trim();
                return result;
            }

            result.HasAction = true;
            result.ActionType = match.Groups[1].Value.Trim();
            string actionPayload = match.Groups[2].Value.Trim();

            // Loại bỏ action tag khỏi nội dung hiển thị trong chat
            result.CleanUserMessage = ActionRegex.Replace(rawAiResponse, "").Trim();

            try
            {
                if (appForm == null) return result;
                OfficeAppMode mode = appForm.Ribbon != null ? appForm.Ribbon.CurrentMode : OfficeAppMode.Home;

                if (result.ActionType == "GENERATE_SLIDE_CANVAS" || result.ActionType == "GENERATE_SLIDES_CANVAS" || 
                    result.ActionType == "CUSTOM_SLIDE" || result.ActionType == "CANVAS" || result.ActionType == "SLIDE_CANVAS" ||
                    result.ActionType == "MULTI_SLIDE" || result.ActionType == "GENERATE_SLIDES" || result.ActionType == "CREATE_SLIDES")
                {
                    ExecuteGenerateSlideCanvas(appForm, actionPayload, result);
                }
                else if (result.ActionType == "MODIFY_SLIDE" || result.ActionType == "UPDATE_SLIDE" || 
                         result.ActionType == "EDIT_SLIDE" || result.ActionType == "SET_THEME" || 
                         result.ActionType == "CHANGE_BACKGROUND" || result.ActionType == "THEME")
                {
                    ExecuteModifySlide(appForm, actionPayload, result);
                }
                else if (result.ActionType == "APPLY_SPELL_CORRECTIONS" || result.ActionType == "FIX_SPELLING" || result.ActionType == "SPELL_CHECK" || result.ActionType == "PROOFREAD")
                {
                    var corrs = ExtractSpellCorrections(actionPayload);
                    result.SpellCorrections = corrs;
                    if (corrs.Count > 0)
                    {
                        if (appForm.WriterView != null)
                        {
                            appForm.WriterView.SetPendingAiCorrections(corrs);
                            result.ExecutionSummary = "⏳ Vui lòng kiểm tra và xác nhận sửa lỗi trên trang văn bản.";
                        }
                        else
                        {
                            int applied = ExecuteApplySpellCorrections(appForm, corrs);
                            result.ExecutionSummary = string.Format("✅ Đã sửa {0} lỗi chính tả vào văn bản.", applied > 0 ? applied : corrs.Count);
                        }
                    }
                }
                else if (result.ActionType == "REPLACE_SELECTION" || result.ActionType == "REWRITE_SELECTION" || result.ActionType == "EDIT_SELECTION")
                {
                    ExecuteReplaceSelection(appForm, actionPayload, result);
                }
                else if (result.ActionType == "REPLACE_WORDS")
                {
                    ExecuteReplaceWords(appForm, actionPayload, result);
                }
                else if (result.ActionType == "REPLACE_TEXT")
                {
                    ExecuteReplaceText(appForm, actionPayload, result);
                }
                else if (result.ActionType == "SET_CALC_DATA" || result.ActionType == "SET_CELLS" || 
                         result.ActionType == "CALC" || result.ActionType == "CALC_DATA" || 
                         result.ActionType == "EXCEL" || result.ActionType == "SPREADSHEET")
                {
                    ExecuteSetCalcData(appForm, actionPayload, result);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ExecutionSummary = "Lỗi khi thực thi: " + ex.Message;
            }

            return result;
        }

        public static List<SpellCorrectionItem> ExtractSpellCorrections(string rawText)
        {
            List<SpellCorrectionItem> list = new List<SpellCorrectionItem>();
            if (string.IsNullOrEmpty(rawText)) return list;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Try parsing JSON format
            int jsonStart = rawText.IndexOf("{");
            int jsonEnd = rawText.LastIndexOf("}");
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                string jsonCand = rawText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                try
                {
                    var parsed = JsonLite.Parse(jsonCand) as Dictionary<string, object>;
                    if (parsed != null)
                    {
                        var corrs = JsonLite.GetArray(parsed, "corrections");
                        if (corrs != null && corrs.Count > 0)
                        {
                            foreach (var cObj in corrs)
                            {
                                var cDict = cObj as Dictionary<string, object>;
                                if (cDict != null)
                                {
                                    string orig = JsonLite.GetString(cDict, "original") ?? JsonLite.GetString(cDict, "from") ?? JsonLite.GetString(cDict, "find");
                                    string repl = JsonLite.GetString(cDict, "replacement") ?? JsonLite.GetString(cDict, "to") ?? JsonLite.GetString(cDict, "replace");
                                    string ctx = JsonLite.GetString(cDict, "context") ?? "";
                                    if (!string.IsNullOrEmpty(orig) && repl != null && !seen.Contains(orig))
                                    {
                                        seen.Add(orig);
                                        list.Add(new SpellCorrectionItem(orig, repl, ctx));
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Line by line text / markdown arrow parsing (->, =>, ➔, -->, thành)
            var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string l = line.Trim();
                if (string.IsNullOrEmpty(l) || l.StartsWith("{") || l.StartsWith("}") || l.StartsWith("[GENOFFICE_ACTION")) continue;

                // Pattern: "❌ từ_sai ➔ ✅ từ_đúng" or "từ_sai -> từ_đúng"
                string find = "";
                string replace = "";

                if (l.Contains("->") || l.Contains("=>") || l.Contains("➔") || l.Contains("-->") || l.Contains("→"))
                {
                    var parts = l.Split(new[] { "->", "=>", "➔", "-->", "→" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        find = CleanWordToken(parts[0]);
                        replace = CleanWordToken(parts[1]);
                    }
                }
                else if (l.Contains(" thành ") || l.Contains(" thanh "))
                {
                    var parts = l.Split(new[] { " thành ", " thanh " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        find = CleanWordToken(parts[0]);
                        replace = CleanWordToken(parts[1]);
                    }
                }

                if (!string.IsNullOrEmpty(find) && !string.IsNullOrEmpty(replace) && find != replace && !seen.Contains(find))
                {
                    seen.Add(find);
                    list.Add(new SpellCorrectionItem(find, replace));
                }
            }

            return list;
        }

        private static string CleanWordToken(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw.Trim();
            // Remove emojis & bullet points
            s = s.Replace("❌", "").Replace("✅", "").Replace("⭐", "").Replace("•", "").Replace("-", "").Replace("*", "").Trim();
            // Remove prefixes like "Từ sai:", "Sửa:", "Lỗi:"
            if (s.StartsWith("Từ sai:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(7).Trim();
            if (s.StartsWith("Từ đúng:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(8).Trim();
            if (s.StartsWith("Sửa thành:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(10).Trim();
            if (s.StartsWith("Sửa:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(4).Trim();
            if (s.StartsWith("Lỗi:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(4).Trim();
            if (s.StartsWith("Sai:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(4).Trim();
            if (s.StartsWith("Đúng:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(5).Trim();

            // Extract text in parentheses or before explanation
            if (s.Contains("(") && s.Contains(")"))
            {
                int pStart = s.IndexOf('(');
                if (pStart > 0) s = s.Substring(0, pStart).Trim();
            }

            s = s.Trim('"', '\'', '`', ' ', ',', ':', ';');
            return s;
        }

        public static int ExecuteApplySpellCorrections(AppForm appForm, List<SpellCorrectionItem> corrections)
        {
            if (appForm == null || corrections == null || corrections.Count == 0) return 0;
            int count = 0;

            if (appForm.Ribbon != null && appForm.Ribbon.CurrentMode == OfficeAppMode.Writer && appForm.WriterView != null && appForm.WriterView.PieceTable != null)
            {
                var writer = appForm.WriterView;
                writer.SaveUndoState();

                string selText = writer.GetSelectedText();
                if (!string.IsNullOrEmpty(selText))
                {
                    string newSelText = selText;
                    foreach (var c in corrections)
                    {
                        if (!string.IsNullOrEmpty(c.Original) && c.Replacement != null && newSelText.Contains(c.Original))
                        {
                            newSelText = newSelText.Replace(c.Original, c.Replacement);
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        writer.ReplaceSelectedText(newSelText, true);
                        writer.InvalidateLayout();
                        appForm.Invalidate();
                    }
                }
                else
                {
                    string fullText = writer.PieceTable.GetText();
                    foreach (var c in corrections)
                    {
                        if (!string.IsNullOrEmpty(c.Original) && c.Replacement != null && fullText.Contains(c.Original))
                        {
                            fullText = fullText.Replace(c.Original, c.Replacement);
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        writer.PieceTable.SetInitialText(fullText);
                        writer.InvalidateLayout();
                        appForm.Invalidate();
                    }
                }
            }

            return count;
        }

        private static void ExecuteReplaceSelection(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm == null || appForm.WriterView == null) return;
            var writer = appForm.WriterView;
            string newText = payload != null ? payload.Trim() : "";
            if (!string.IsNullOrEmpty(newText))
            {
                writer.ReplaceSelectedText(newText, true);
                appForm.Invalidate();
                result.ExecutionSummary = "✅ Đã tự động thay thế trực tiếp vào vùng quét khối (áp dụng bôi đen cho đoạn mới).";
            }
        }

        public static string ExtractCleanRewrittenText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return "";
            string stripped = Regex.Replace(rawText, @"\[[A-Za-z0-9_]*ACTION[A-Za-z0-9_:]*\][\s\S]*?(?:\[/[A-Za-z0-9_]*ACTION[A-Za-z0-9_]*\]|$)", "", RegexOptions.IgnoreCase);
            stripped = Regex.Replace(stripped, @"\{[\s
]*""corrections""[\s\S]*?\}", "", RegexOptions.IgnoreCase);

            var lines = stripped.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            List<string> cleanedLines = new List<string>();
            bool skipIntro = true;

            foreach (var line in lines)
            {
                string lTrim = line.Trim();
                if (string.IsNullOrEmpty(lTrim))
                {
                    if (!skipIntro) cleanedLines.Add("");
                    continue;
                }

                if (!skipIntro)
                {
                    string lowerCheck = lTrim.ToLower();
                    if (lowerCheck.StartsWith("các lỗi") || lowerCheck.StartsWith("danh sách lỗi") || 
                        lowerCheck.StartsWith("chi tiết lỗi") || lowerCheck.StartsWith("lỗi đã sửa") ||
                        lowerCheck.StartsWith("từ sai"))
                    {
                        break;
                    }
                }

                if (skipIntro)
                {
                    string lower = lTrim.ToLower();
                    if (lower.Contains("dưới đây") || lower.Contains("đoạn văn") || lower.Contains("bản sửa") || 
                        lower.Contains("kết quả") || lower.Contains("đã sửa") || lower.Contains("sau đây") || lower.Contains("sửa lại") ||
                        lower.StartsWith("chắc chắn") || lower.StartsWith("đây là") || lower.StartsWith("tôi đã") ||
                        lower.EndsWith(":") || lower.StartsWith("["))
                    {
                        continue;
                    }
                    else
                    {
                        skipIntro = false;
                    }
                }

                if (lTrim.StartsWith(">")) lTrim = lTrim.Substring(1).Trim();
                if (lTrim.StartsWith("```")) continue;
                if (lTrim.StartsWith("- ") && (lTrim.Contains("➔") || lTrim.Contains("->") || lTrim.Contains(":"))) continue;

                cleanedLines.Add(lTrim);
            }

            return string.Join("\n", cleanedLines.ToArray()).Trim();
        }

        #region Word & Excel Actions

        private static void ExecuteReplaceWords(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm.WriterView == null || appForm.WriterView.PieceTable == null) return;

            var writer = appForm.WriterView;
            writer.SaveUndoState();

            string fullText = writer.PieceTable.GetText();
            List<string> fixedList = new List<string>();

            var lines = payload.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string l = line.Trim();
                if (string.IsNullOrEmpty(l) || l.StartsWith("{") || l.StartsWith("}")) continue;

                string find = "";
                string replace = "";

                if (l.Contains("->") || l.Contains("=>") || l.Contains("➔") || l.Contains("-->"))
                {
                    var parts = l.Split(new[] { "->", "=>", "➔", "-->" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        find = parts[0].Trim().Trim('"', '\'', '`', ',', '-');
                        replace = parts[1].Trim().Trim('"', '\'', '`', ',', '-');
                    }
                }
                else if (l.Contains(":"))
                {
                    var parts = l.Split(new[] { ':' }, 2);
                    find = parts[0].Trim().Trim('"', '\'', '`', ',');
                    replace = parts[1].Trim().Trim('"', '\'', '`', ',');
                }

                if (!string.IsNullOrEmpty(find))
                {
                    if (fullText.Contains(find))
                    {
                        fullText = fullText.Replace(find, replace);
                        fixedList.Add(string.Format("'{0}' ➔ '{1}'", find, replace));
                    }
                }
            }

            if (fixedList.Count > 0)
            {
                writer.PieceTable.SetInitialText(fullText);
                writer.CursorPosition = Math.Min(writer.CursorPosition, fullText.Length);
                appForm.Invalidate();

                result.ExecutionSummary = string.Format("✅ Đã sửa trực tiếp {0} lỗi trên văn bản: {1}", fixedList.Count, string.Join(", ", fixedList.ToArray()));
            }
            else
            {
                result.ExecutionSummary = "ℹ️ Đã quét xong văn bản.";
            }
        }

        private static void ExecuteReplaceText(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm.WriterView == null || appForm.WriterView.PieceTable == null) return;

            var writer = appForm.WriterView;
            writer.SaveUndoState();

            string cleanText = payload.Trim();
            if (!string.IsNullOrEmpty(cleanText))
            {
                writer.PieceTable.SetInitialText(cleanText);
                writer.CursorPosition = cleanText.Length;
                appForm.Invalidate();

                result.ExecutionSummary = "✅ Đã cập nhật toàn bộ nội dung văn bản mới từ AI.";
            }
        }

        private static void ExecuteSetCalcData(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm == null || appForm.CalcView == null) return;
            var calc = appForm.CalcView;
            calc.SaveUndoState();

            object parsedObj = JsonLite.Parse(payload);
            Dictionary<string, object> dict = parsedObj as Dictionary<string, object>;
            
            SparseMatrix sheet = calc.CurrentSheet;
            int cellsUpdated = 0;
            int formulasApplied = 0;

            if (dict != null)
            {
                string targetSheetName = GetString(dict, "sheet", GetString(dict, "sheetName", ""));
                if (!string.IsNullOrEmpty(targetSheetName))
                {
                    int sheetIdx = calc.SheetNames != null ? calc.SheetNames.IndexOf(targetSheetName) : -1;
                    if (sheetIdx >= 0)
                    {
                        calc.ActiveSheetIndex = sheetIdx;
                        sheet = calc.CurrentSheet;
                    }
                    else
                    {
                        calc.AddSheet(targetSheetName);
                        calc.ActiveSheetIndex = calc.Sheets.Count - 1;
                        sheet = calc.CurrentSheet;
                    }
                }

                if (sheet == null) sheet = calc.CurrentSheet;
                if (sheet == null) return;

                bool clearExisting = GetBool(dict, "clearExisting", GetBool(dict, "clear", false));
                if (clearExisting)
                {
                    sheet.ClearAll();
                }

                // 1. Áp dụng định dạng hàng loạt theo dải ô (ranges)
                if (dict.ContainsKey("ranges") && dict["ranges"] is List<object>)
                {
                    var rangeList = (List<object>)dict["ranges"];
                    foreach (var rObj in rangeList)
                    {
                        if (rObj is Dictionary<string, object>)
                        {
                            var rDict = (Dictionary<string, object>)rObj;
                            string rangeStr = GetString(rDict, "range", "");
                            uint r1, c1, r2, c2;
                            if (TryParseRange(rangeStr, out r1, out c1, out r2, out c2))
                            {
                                ApplyRangeFormatting(sheet, r1, c1, r2, c2, rDict);
                            }
                        }
                    }
                }

                // 2. Điền và định dạng chi tiết từng ô (cells)
                if (dict.ContainsKey("cells") && dict["cells"] is List<object>)
                {
                    var cellList = (List<object>)dict["cells"];
                    foreach (var cObj in cellList)
                    {
                        if (cObj is Dictionary<string, object>)
                        {
                            var cDict = (Dictionary<string, object>)cObj;
                            string cellRef = GetString(cDict, "cell", GetString(cDict, "ref", GetString(cDict, "coord", "")));
                            uint r = 0, c = 0;
                            if (!SparseMatrix.TryParseCellReference(cellRef, out r, out c))
                            {
                                r = (uint)GetInt(cDict, "row", 0);
                                c = (uint)GetInt(cDict, "col", 0);
                            }

                            if (r > 0 && c > 0)
                            {
                                CellData cell = sheet.GetOrCreateCell(r, c);
                                if (cDict.ContainsKey("formula"))
                                {
                                    string f = GetString(cDict, "formula", "");
                                    if (!f.StartsWith("=")) f = "=" + f;
                                    cell.SetFormula(f);
                                    formulasApplied++;
                                }
                                else if (cDict.ContainsKey("value"))
                                {
                                    object valObj = cDict["value"];
                                    if (valObj is double || valObj is int || valObj is long || valObj is float)
                                    {
                                        double dVal = Convert.ToDouble(valObj);
                                        cell.SetNumber(dVal);
                                    }
                                    else
                                    {
                                        string sVal = valObj != null ? valObj.ToString() : "";
                                        if (sVal.StartsWith("="))
                                        {
                                            cell.SetFormula(sVal);
                                            formulasApplied++;
                                        }
                                        else
                                        {
                                            double dParsed;
                                            if (double.TryParse(sVal, NumberStyles.Any, CultureInfo.InvariantCulture, out dParsed))
                                            {
                                                cell.SetNumber(dParsed);
                                            }
                                            else
                                            {
                                                cell.SetString(sVal);
                                            }
                                        }
                                    }
                                }

                                ApplySingleCellFormatting(cell, cDict);

                                if (cDict.ContainsKey("merge"))
                                {
                                    string mRange = GetString(cDict, "merge", "");
                                    uint mr1, mc1, mr2, mc2;
                                    if (TryParseRange(mRange, out mr1, out mc1, out mr2, out mc2))
                                    {
                                        sheet.MergeCells(mr1, mc1, mr2, mc2);
                                    }
                                }

                                cellsUpdated++;
                            }
                        }
                    }
                }
            }
            else
            {
                // Fallback: Xử lý dạng DSL dòng (A1=Title, B2==SUM(A1:A5), v.v.)
                var lines = payload.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string l = line.Trim();
                    if (string.IsNullOrEmpty(l) || l.StartsWith("{") || l.StartsWith("}") || l.StartsWith("\"")) continue;

                    if (l.Contains("="))
                    {
                        var parts = l.Split(new[] { '=' }, 2);
                        string cellRef = parts[0].Trim();
                        string val = parts[1].Trim();

                        uint r = 0, c = 0;
                        if (SparseMatrix.TryParseCellReference(cellRef, out r, out c) && r > 0 && c > 0)
                        {
                            CellData cell = sheet.GetOrCreateCell(r, c);
                            if (val.StartsWith("="))
                            {
                                cell.SetFormula(val);
                                formulasApplied++;
                            }
                            else
                            {
                                double d;
                                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                                    cell.SetNumber(d);
                                else
                                    cell.SetString(val);
                            }
                            cellsUpdated++;
                        }
                    }
                }
            }

            // Tự động tính toán lại toàn bộ công thức trên trang tính
            FormulaEngine.EvaluateSheet(sheet);
            appForm.Invalidate();

            if (formulasApplied > 0)
            {
                result.ExecutionSummary = string.Format("📊 AI đã cập nhật {0} ô tính, tự động áp dụng {1} công thức và tính toán kết quả chính xác!", cellsUpdated, formulasApplied);
            }
            else
            {
                result.ExecutionSummary = string.Format("📊 AI đã cập nhật và định dạng thành công {0} ô tính trên bảng tính!", cellsUpdated);
            }
        }

        private static bool TryExecuteMarkdownTableToCalc(AppForm appForm, string text, AiActionResult result)
        {
            if (string.IsNullOrEmpty(text) || appForm == null || appForm.CalcView == null) return false;
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<List<string>> tableRows = new List<List<string>>();

            foreach (var line in lines)
            {
                string tr = line.Trim();
                if (tr.StartsWith("|") && tr.EndsWith("|") && tr.Length > 2)
                {
                    // Skip markdown separator row like |---|---|
                    if (Regex.IsMatch(tr, @"^\|[\s\-:|]+\|$")) continue;

                    var cols = tr.Substring(1, tr.Length - 2).Split('|');
                    List<string> rowCells = new List<string>();
                    foreach (var c in cols) rowCells.Add(c.Trim());
                    if (rowCells.Count > 0) tableRows.Add(rowCells);
                }
            }

            if (tableRows.Count < 2) return false; // Need at least 1 header + 1 data row

            var calc = appForm.CalcView;
            calc.SaveUndoState();
            var sheet = calc.CurrentSheet;
            if (sheet == null) return false;

            // Tính toán ranh giới của bảng dữ liệu hiện có để định vị thông minh
            uint minR = uint.MaxValue, maxR = 0, minC = uint.MaxValue, maxC = 0;
            var allCells = sheet.GetAllCells();
            foreach (var kvp in allCells)
            {
                var coord = SparseMatrix.DecodeKey(kvp.Key);
                if (coord.Item1 < minR) minR = coord.Item1;
                if (coord.Item1 > maxR) maxR = coord.Item1;
                if (coord.Item2 < minC) minC = coord.Item2;
                if (coord.Item2 > maxC) maxC = coord.Item2;
            }

            uint startRow = 3;
            uint startCol = 1;

            if (maxR > 0 && maxC > 0)
            {
                // Nếu đã có bảng số liệu cũ trên trang tính:
                if (calc.SelectedRow > maxR + 1)
                {
                    // Người dùng chủ động chọn bên dưới: đặt cách 2 hàng trống
                    startRow = Math.Max(calc.SelectedRow, maxR + 3);
                    startCol = (minC != uint.MaxValue && minC > 0) ? minC : 1;
                }
                else if (calc.SelectedCol > maxC + 1)
                {
                    // Người dùng chủ động chọn bên phải
                    startRow = (minR != uint.MaxValue && minR > 0) ? minR : 3;
                    startCol = calc.SelectedCol;
                }
                else
                {
                    // Vị trí con trỏ đang nằm trong hoặc đè lên bảng cũ:
                    // Tự động đặt bảng mới sang BÊN PHẢI bảng cũ, cách đúng 1 cột trống và ngang hàng với tiêu đề bảng cũ!
                    startRow = (minR != uint.MaxValue && minR > 0) ? minR : 3;
                    startCol = maxC + 2; // Cách 1 cột trống
                }
            }
            else
            {
                startRow = Math.Max(1, calc.SelectedRow > 0 ? calc.SelectedRow : 3);
                startCol = Math.Max(1, calc.SelectedCol > 0 ? calc.SelectedCol : 1);
            }

            int cellsCount = 0;
            for (int rIdx = 0; rIdx < tableRows.Count; rIdx++)
            {
                uint r = startRow + (uint)rIdx;
                var rowData = tableRows[rIdx];
                for (int cIdx = 0; cIdx < rowData.Count; cIdx++)
                {
                    uint c = startCol + (uint)cIdx;
                    string val = rowData[cIdx];
                    var cell = sheet.GetOrCreateCell(r, c);

                    if (rIdx == 0) // Header row
                    {
                        cell.SetString(val);
                        if (cell.Format != null)
                        {
                            cell.Format.Bold = true;
                            cell.Format.BgColor = ParseColor("#1E293B", unchecked((int)0xFF1E293B));
                            cell.Format.TextColor = ParseColor("#FFFFFF", unchecked((int)0xFFFFFFFF));
                            cell.Format.Align = 1; // Center
                        }
                    }
                    else
                    {
                        double numVal;
                        if (val.StartsWith("="))
                        {
                            cell.SetFormula(val);
                        }
                        else if (double.TryParse(val.Replace("$", "").Replace(",", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numVal))
                        {
                            cell.SetNumber(numVal);
                            if (cell.Format != null) cell.Format.Align = 2; // Right
                        }
                        else
                        {
                            cell.SetString(val);
                        }
                    }
                    cellsCount++;
                }
            }

            FormulaEngine.EvaluateSheet(sheet);
            appForm.Invalidate();
            if (appForm.Ribbon != null && appForm.Ribbon.ActiveDocIndex >= 0 && appForm.Ribbon.ActiveDocIndex < appForm.Ribbon.DocTabs.Count)
            {
                appForm.Ribbon.DocTabs[appForm.Ribbon.ActiveDocIndex].IsModified = true;
                appForm.Ribbon.SaveTabState(appForm.Ribbon.ActiveDocIndex, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);
                SessionManager.SaveSession(appForm);
            }
            result.HasAction = true;
            result.ActionType = "SET_CALC_DATA";
            result.ExecutionSummary = string.Format("✅ Đã tự động tạo bảng {0} dòng x {1} cột ({2} ô) vào Bảng tính thành công.", tableRows.Count, tableRows[0].Count, cellsCount);
            result.CleanUserMessage = "✅ Đã tự động ghi dữ liệu bảng mới vào Bảng tính.";
            return true;
        }

        private static bool TryParseRange(string rangeStr, out uint r1, out uint c1, out uint r2, out uint c2)
        {
            r1 = 1; c1 = 1; r2 = 1; c2 = 1;
            if (string.IsNullOrWhiteSpace(rangeStr)) return false;
            string clean = rangeStr.Trim().ToUpperInvariant();
            if (clean.Contains(":"))
            {
                var parts = clean.Split(':');
                if (parts.Length == 2)
                {
                    bool ok1 = SparseMatrix.TryParseCellReference(parts[0], out r1, out c1);
                    bool ok2 = SparseMatrix.TryParseCellReference(parts[1], out r2, out c2);
                    return ok1 && ok2;
                }
            }
            else
            {
                bool ok = SparseMatrix.TryParseCellReference(clean, out r1, out c1);
                r2 = r1; c2 = c1;
                return ok;
            }
            return false;
        }

        private static void ApplyRangeFormatting(SparseMatrix sheet, uint r1, uint c1, uint r2, uint c2, Dictionary<string, object> dict)
        {
            if (sheet == null || dict == null) return;
            uint minR = Math.Min(r1, r2);
            uint maxR = Math.Max(r1, r2);
            uint minC = Math.Min(c1, c2);
            uint maxC = Math.Max(c1, c2);

            bool merge = GetBool(dict, "merge", false);
            if (merge)
            {
                sheet.MergeCells(minR, minC, maxR, maxC);
            }

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = sheet.GetOrCreateCell(r, c);
                    ApplySingleCellFormatting(cell, dict);
                }
            }
        }

        private static void ApplySingleCellFormatting(CellData cell, Dictionary<string, object> dict)
        {
            if (cell == null || dict == null || cell.Format == null) return;

            if (dict.ContainsKey("bold")) cell.Format.Bold = GetBool(dict, "bold", false);
            if (dict.ContainsKey("italic")) cell.Format.Italic = GetBool(dict, "italic", false);
            if (dict.ContainsKey("underline")) cell.Format.Underline = GetBool(dict, "underline", false);
            if (dict.ContainsKey("fontSize")) cell.Format.FontSize = (float)GetDouble(dict, "fontSize", 11.0);
            if (dict.ContainsKey("fontName")) cell.Format.FontName = GetString(dict, "fontName", "Calibri");

            if (dict.ContainsKey("bgColor") || dict.ContainsKey("fillColor"))
            {
                string hex = GetString(dict, "bgColor", GetString(dict, "fillColor", ""));
                if (!string.IsNullOrEmpty(hex))
                {
                    cell.Format.BgColor = ParseColor(hex, unchecked((int)0xFFFFFFFF));
                }
            }

            if (dict.ContainsKey("textColor") || dict.ContainsKey("color"))
            {
                string hex = GetString(dict, "textColor", GetString(dict, "color", ""));
                if (!string.IsNullOrEmpty(hex))
                {
                    cell.Format.TextColor = ParseColor(hex, unchecked((int)0xFF000000));
                }
            }

            if (dict.ContainsKey("align"))
            {
                string a = GetString(dict, "align", "").ToLowerInvariant();
                if (a == "center" || a == "middle") cell.Format.Align = 1;
                else if (a == "right") cell.Format.Align = 2;
                else cell.Format.Align = 0;
            }

            if (dict.ContainsKey("valign"))
            {
                string va = GetString(dict, "valign", "").ToLowerInvariant();
                if (va == "top") cell.Format.VAlign = 0;
                else if (va == "bottom") cell.Format.VAlign = 2;
                else cell.Format.VAlign = 1;
            }

            if (dict.ContainsKey("wrapText")) cell.Format.WrapText = GetBool(dict, "wrapText", false);
            if (dict.ContainsKey("numberFormat")) cell.Format.NumberFormat = GetString(dict, "numberFormat", "General");
            if (dict.ContainsKey("decimalPlaces")) cell.Format.DecimalPlaces = GetInt(dict, "decimalPlaces", 2);

            if (dict.ContainsKey("borders"))
            {
                object bObj = dict["borders"];
                if (bObj is Dictionary<string, object>)
                {
                    var bDict = (Dictionary<string, object>)bObj;
                    bool all = GetBool(bDict, "all", false);
                    cell.Format.Borders.Top = all || GetBool(bDict, "top", false);
                    cell.Format.Borders.Bottom = all || GetBool(bDict, "bottom", false);
                    cell.Format.Borders.Left = all || GetBool(bDict, "left", false);
                    cell.Format.Borders.Right = all || GetBool(bDict, "right", false);

                    if (bDict.ContainsKey("color"))
                    {
                        string bColor = GetString(bDict, "color", "");
                        if (!string.IsNullOrEmpty(bColor)) cell.Format.Borders.Color = ParseColor(bColor, unchecked((int)0xFFCBD5E1));
                    }
                    if (bDict.ContainsKey("thickness"))
                    {
                        cell.Format.Borders.Thickness = GetInt(bDict, "thickness", 1);
                    }
                }
                else if (bObj is bool && (bool)bObj)
                {
                    cell.Format.Borders.Top = true;
                    cell.Format.Borders.Bottom = true;
                    cell.Format.Borders.Left = true;
                    cell.Format.Borders.Right = true;
                    cell.Format.Borders.Color = unchecked((int)0xFFCBD5E1);
                    cell.Format.Borders.Thickness = 1;
                }
            }
        }

        #endregion

        #region Theme Palette Resolver

        internal static SlidePalette ResolveThemePalette(string styleName)
        {
            SlidePalette p = new SlidePalette();
            string s = (styleName ?? "").Trim();

            if (s.IndexOf("TechDark", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("CyberMidnight", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Dark", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("tối", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("đen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // TechDark: Midnight Navy & Cyan
                p.Bg1 = 0x070D1B; p.Bg2 = 0x0F1B30;
                p.Primary = 0x00D2FF; p.Accent = 0x38BDF8;
                p.TitleColor = 0xFFFFFF; p.SubtitleColor = 0x94A3B8; p.BodyColor = 0xF1F5F9;
                p.CardBg = 0x111E38; p.CardBorder = 0x1E3A5F;
                p.BadgeBg = 0x162A4A; p.BadgeFg = 0x38BDF8;
                p.StatBg = 0x0A1628; p.StatBorder = 0x0284C7; p.StatText = 0x38BDF8;
                p.DarkTheme = true;
            }
            else if (s.IndexOf("CyberNeon", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Neon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // CyberNeon: Deep Obsidian & Neon Cyan / Magenta
                p.Bg1 = 0x050811; p.Bg2 = 0x0A0F1E;
                p.Primary = 0x00F0FF; p.Accent = 0xF43F5E;
                p.TitleColor = 0xFFFFFF; p.SubtitleColor = 0x67E8F9; p.BodyColor = 0xE2E8F0;
                p.CardBg = 0x0F172A; p.CardBorder = 0x00F0FF;
                p.BadgeBg = 0x1E1B4B; p.BadgeFg = 0x00F0FF;
                p.StatBg = 0x111827; p.StatBorder = 0x38BDF8; p.StatText = 0x00F0FF;
                p.DarkTheme = true;
            }
            else if (s.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("LuxuryDark", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("vàng", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Gold Luxury Dark
                p.Bg1 = 0x0C0A09; p.Bg2 = 0x1C1917;
                p.Primary = 0xF59E0B; p.Accent = 0xD97706;
                p.TitleColor = 0xFFFFFF; p.SubtitleColor = 0xFDE68A; p.BodyColor = 0xF5F5F4;
                p.CardBg = 0x292524; p.CardBorder = 0x78350F;
                p.BadgeBg = 0x1C1917; p.BadgeFg = 0xFBBF24;
                p.StatBg = 0x1C1917; p.StatBorder = 0xB45309; p.StatText = 0xF59E0B;
                p.DarkTheme = true;
            }
            else if (s.IndexOf("Green", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Emerald", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("xanh lá", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("lá cây", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("xanh lục", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Emerald Green & Mint
                p.Bg1 = 0xF0FDF4; p.Bg2 = 0xDCFCE7;
                p.Primary = 0x047857; p.Accent = 0x059669;
                p.TitleColor = 0x064E3B; p.SubtitleColor = 0x065F46; p.BodyColor = 0x022C22;
                p.CardBg = 0xFFFFFF; p.CardBorder = 0x86EFAC;
                p.BadgeBg = 0xD1FAE5; p.BadgeFg = 0x065F46;
                p.StatBg = 0xECFDF5; p.StatBorder = 0x6EE7B7; p.StatText = 0x047857;
                p.DarkTheme = false;
            }
            else if (s.IndexOf("Sunset", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Warm", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("cam", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Warm Sunset & Amber
                p.Bg1 = 0xFFF7ED; p.Bg2 = 0xFFEDD5;
                p.Primary = 0xC2410C; p.Accent = 0xEA580C;
                p.TitleColor = 0x431407; p.SubtitleColor = 0x7C2D12; p.BodyColor = 0x290C04;
                p.CardBg = 0xFFFFFF; p.CardBorder = 0xFDBA74;
                p.BadgeBg = 0xFFEDD5; p.BadgeFg = 0x9A3412;
                p.StatBg = 0xFFF7ED; p.StatBorder = 0xFED7AA; p.StatText = 0xC2410C;
                p.DarkTheme = false;
            }
            else if (s.IndexOf("Crimson", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Red", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Ruby", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("đỏ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Crimson Ruby & Rose
                p.Bg1 = 0xFFF1F2; p.Bg2 = 0xFFE4E6;
                p.Primary = 0xBE123C; p.Accent = 0xE11D48;
                p.TitleColor = 0x4C0519; p.SubtitleColor = 0x881337; p.BodyColor = 0x27020B;
                p.CardBg = 0xFFFFFF; p.CardBorder = 0xFECDD3;
                p.BadgeBg = 0xFFE4E6; p.BadgeFg = 0x9F1239;
                p.StatBg = 0xFFF1F2; p.StatBorder = 0xFDA4AF; p.StatText = 0xBE123C;
                p.DarkTheme = false;
            }
            else if (s.IndexOf("Titanium", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Minimal", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Slate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Minimal Titanium Slate
                p.Bg1 = 0xF1F5F9; p.Bg2 = 0xE2E8F0;
                p.Primary = 0x334155; p.Accent = 0x475569;
                p.TitleColor = 0x0F172A; p.SubtitleColor = 0x334155; p.BodyColor = 0x020617;
                p.CardBg = 0xFFFFFF; p.CardBorder = 0x94A3B8;
                p.BadgeBg = 0xE2E8F0; p.BadgeFg = 0x1E293B;
                p.StatBg = 0xF8FAFC; p.StatBorder = 0xCBD5E1; p.StatText = 0x0F172A;
                p.DarkTheme = false;
            }
            else
            {
                // ModernBlue / Sapphire Executive (Mặc định)
                p.Bg1 = 0xF8FAFC; p.Bg2 = 0xEEF2F6;
                p.Primary = 0x1D4ED8; p.Accent = 0x0284C7;
                p.TitleColor = 0x0F172A; p.SubtitleColor = 0x1E293B; p.BodyColor = 0x020617;
                p.CardBg = 0xFFFFFF; p.CardBorder = 0xCBD5E1;
                p.BadgeBg = 0xDBEAFE; p.BadgeFg = 0x1E40AF;
                p.StatBg = 0xF0F9FF; p.StatBorder = 0xBAE6FD; p.StatText = 0x1D4ED8;
                p.DarkTheme = false;
            }

            return p;
        }

        #endregion

        #region Autonomous Declarative JSON Canvas Engine (GENERATE_SLIDE_CANVAS)

        private static void ExecuteGenerateSlideCanvas(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm.SlidesView == null || appForm.SlidesView.Presentation == null) return;

            var slides = appForm.SlidesView;
            var pres = slides.Presentation;

            float canvasW = pres.CanvasWidth > 0 ? pres.CanvasWidth : 1920.0f;
            float canvasH = pres.CanvasHeight > 0 ? pres.CanvasHeight : 1080.0f;

            object parsedObj = JsonLite.Parse(payload);
            // Case 1: Direct List of Slide Objects [ { ... }, { ... } ]
            if (parsedObj is List<object>)
            {
                var slideList = (List<object>)parsedObj;
                if (slideList.Count > 0)
                {
                    pres.Slides.Clear();
                    foreach (var sObj in slideList)
                    {
                        if (sObj is Dictionary<string, object>)
                        {
                            SlideNode sNode = new SlideNode();
                            RenderSlideFromCanvasDict(sNode, (Dictionary<string, object>)sObj, null, canvasW, canvasH);
                            pres.Slides.Add(sNode);
                        }
                    }
                    slides.ActiveSlideIndex = 0;
                    appForm.Invalidate();
                    result.ExecutionSummary = string.Format("🎨 AI đã kiến tạo thành công bộ {0} Slide thuyết trình hoàn chỉnh!", pres.Slides.Count);
                    return;
                }
            }

            Dictionary<string, object> dict = null;

            if (parsedObj is Dictionary<string, object>)
            {
                dict = (Dictionary<string, object>)parsedObj;

                // Case 2: Multi-Slide Object { "theme": "...", "slides": [ { ... }, { ... } ] }
                if (dict.ContainsKey("slides") && dict["slides"] is List<object>)
                {
                    var slideList = (List<object>)dict["slides"];
                    if (slideList.Count > 0)
                    {
                        string globalStyle = GetString(dict, "style", GetString(dict, "theme", "ModernBlue"));
                        SlidePalette globalPal = ResolveThemePalette(globalStyle);

                        bool append = GetBool(dict, "append", false);
                        if (!append)
                        {
                            pres.Slides.Clear();
                        }

                        foreach (var sObj in slideList)
                        {
                            if (sObj is Dictionary<string, object>)
                            {
                                SlideNode sNode = new SlideNode();
                                RenderSlideFromCanvasDict(sNode, (Dictionary<string, object>)sObj, globalPal, canvasW, canvasH);
                                pres.Slides.Add(sNode);
                            }
                        }

                        slides.ActiveSlideIndex = 0;
                        appForm.Invalidate();
                        result.ExecutionSummary = string.Format("🎨 AI đã kiến tạo thành công bộ {0} Slide thuyết trình hoàn chỉnh theo yêu cầu!", slideList.Count);
                        return;
                    }
                }
            }
            else
            {
                // Fallback: parse raw/DSL payload into dynamic JSON Canvas elements
                AiSlideDesignSpec spec = ParseSlideDesignSpec(payload);
                dict = new Dictionary<string, object>();
                dict["theme"] = spec.ThemeStyle;
                dict["title"] = spec.SlideTitle;

                var elemList = new List<object>();
                float curY = 70f;

                if (!string.IsNullOrEmpty(spec.CategoryBadge))
                {
                    var badge = new Dictionary<string, object>();
                    badge["type"] = "badge";
                    badge["text"] = spec.CategoryBadge;
                    badge["bounds"] = new List<object> { 100.0, (double)curY, 260.0, 38.0 };
                    elemList.Add(badge);
                    curY += 50f;
                }

                if (!string.IsNullOrEmpty(spec.SlideTitle))
                {
                    var titleElem = new Dictionary<string, object>();
                    titleElem["type"] = "title";
                    titleElem["text"] = spec.SlideTitle;
                    titleElem["bounds"] = new List<object> { 100.0, (double)curY, (double)(canvasW - 200f), 64.0 };
                    titleElem["fontSize"] = 46.0;
                    titleElem["bold"] = true;
                    titleElem["align"] = "center";
                    elemList.Add(titleElem);
                    curY += 70f;
                }

                if (!string.IsNullOrEmpty(spec.SlideSubtitle))
                {
                    var subElem = new Dictionary<string, object>();
                    subElem["type"] = "subtitle";
                    subElem["text"] = spec.SlideSubtitle;
                    subElem["bounds"] = new List<object> { 140.0, (double)curY, (double)(canvasW - 280f), 40.0 };
                    subElem["fontSize"] = 21.0;
                    subElem["italic"] = true;
                    subElem["align"] = "center";
                    elemList.Add(subElem);
                    curY += 50f;
                }

                if (spec.Cards.Count > 0)
                {
                    float cardTopY = curY + 20f;
                    int cardCount = spec.Cards.Count;
                    float marginX = 100f;
                    float gap = 28f;
                    float cardW = ((canvasW - marginX * 2f) - (gap * (cardCount - 1))) / cardCount;

                    for (int i = 0; i < cardCount; i++)
                    {
                        var cs = spec.Cards[i];
                        float cx = marginX + i * (cardW + gap);

                        int lines = !string.IsNullOrEmpty(cs.Description) ? cs.Description.Split('\n').Length : 1;
                        float dynamicH = 40f + 36f + (lines * 30f) + 36f;
                        if (!string.IsNullOrEmpty(cs.StatNumber)) dynamicH += 70f;
                        float cardH = Math.Min(340f, Math.Max(160f, dynamicH));

                        var cardElem = new Dictionary<string, object>();
                        cardElem["type"] = "card";
                        cardElem["title"] = cs.Title;
                        cardElem["desc"] = cs.Description;
                        cardElem["stat"] = cs.StatNumber;
                        cardElem["bounds"] = new List<object> { (double)cx, (double)cardTopY, (double)cardW, (double)cardH };
                        elemList.Add(cardElem);
                    }
                }

                dict["elements"] = elemList;
            }

            string styleName = GetString(dict, "style", GetString(dict, "theme", "ModernBlue"));
            SlidePalette pal = ResolveThemePalette(styleName);

            bool createNew = GetBool(dict, "createNewSlide", false);
            SlideNode targetSlide = null;
            if (createNew || pres.Slides.Count == 0)
            {
                targetSlide = new SlideNode();
                pres.Slides.Add(targetSlide);
                slides.ActiveSlideIndex = pres.Slides.Count - 1;
            }
            else
            {
                int curIdx = Math.Max(0, Math.Min(pres.Slides.Count - 1, slides.ActiveSlideIndex));
                targetSlide = pres.Slides[curIdx];
            }

            RenderSlideFromCanvasDict(targetSlide, dict, pal, canvasW, canvasH);

            appForm.Invalidate();
            string slideTitle = GetString(dict, "title", "Slide Tùy Biến Tự Do");
            result.ExecutionSummary = string.Format("🎨 AI đã kiến tạo thành công Slide: '{0}' ({1} phần tử, phong cách: {2})!", slideTitle, targetSlide.Shapes.Count, styleName);
        }

        private static void RenderSlideFromCanvasDict(SlideNode targetSlide, Dictionary<string, object> dict, SlidePalette globalPal, float canvasW, float canvasH)
        {
            if (targetSlide == null || dict == null) return;
            targetSlide.Shapes.Clear();

            string styleName = GetString(dict, "style", GetString(dict, "theme", "ModernBlue"));
            SlidePalette pal = ResolveThemePalette(styleName);
            if (globalPal != null && string.IsNullOrEmpty(GetString(dict, "style", "")) && string.IsNullOrEmpty(GetString(dict, "theme", "")))
            {
                pal = globalPal;
            }

            if (dict.ContainsKey("title") && !string.IsNullOrEmpty(GetString(dict, "title", "")))
            {
                targetSlide.Title = GetString(dict, "title", targetSlide.Title);
            }

            // 1. Cấu hình Nền Canvas & Đồng bộ Gradient Stops
            if (dict.ContainsKey("background") && dict["background"] is Dictionary<string, object>)
            {
                var bgDict = (Dictionary<string, object>)dict["background"];
                string bgType = GetString(bgDict, "type", "gradient");
                if (bgType == "solid")
                {
                    int col = ParseColor(GetString(bgDict, "color", ""), pal.Bg1);
                    targetSlide.IsGradientBg = false;
                    targetSlide.BgFillType = BackgroundFillType.Solid;
                    targetSlide.BgColor = col;
                    targetSlide.BgGradientStops = new List<GradientStop>
                    {
                        new GradientStop(col, 0.0f),
                        new GradientStop(col, 1.0f)
                    };
                }
                else
                {
                    int c1 = ParseColor(GetString(bgDict, "color1", ""), pal.Bg1);
                    int c2 = ParseColor(GetString(bgDict, "color2", ""), pal.Bg2);
                    float ang = (float)GetDouble(bgDict, "angle", 135.0);
                    targetSlide.IsGradientBg = true;
                    targetSlide.BgFillType = BackgroundFillType.Gradient;
                    targetSlide.BgColor = c1;
                    targetSlide.BgColor2 = c2;
                    targetSlide.BgGradientAngle = ang;
                    targetSlide.BgGradientStops = new List<GradientStop>
                    {
                        new GradientStop(c1, 0.0f),
                        new GradientStop(c2, 1.0f)
                    };
                }
            }
            else
            {
                targetSlide.IsGradientBg = true;
                targetSlide.BgFillType = BackgroundFillType.Gradient;
                targetSlide.BgColor = pal.Bg1;
                targetSlide.BgColor2 = pal.Bg2;
                targetSlide.BgGradientAngle = 135f;
                targetSlide.BgGradientStops = new List<GradientStop>
                {
                    new GradientStop(pal.Bg1, 0.0f),
                    new GradientStop(pal.Bg2, 1.0f)
                };
            }

            // 2. Cấu hình Hiệu ứng Chuyển cảnh (Transitions: Morph, Zoom, Push, Wipe, Fade, Cube, Flip, LiquidFluid, etc.)
            string transStr = GetString(dict, "transition", GetString(dict, "trans", GetString(dict, "transitionType", "")));
            if (!string.IsNullOrEmpty(transStr))
            {
                TransitionType tt = ParseTransitionType(transStr);
                targetSlide.Transition = tt;
                targetSlide.TransitionDuration = (float)GetDouble(dict, "transitionDuration", (float)GetDouble(dict, "duration", (tt == TransitionType.Morph ? 1.5f : 1.0f)));
                string morphModeStr = GetString(dict, "morphMode", "Objects");
                if (morphModeStr.Equals("Words", StringComparison.OrdinalIgnoreCase)) targetSlide.MorphMode = MorphType.Words;
                else if (morphModeStr.Equals("Characters", StringComparison.OrdinalIgnoreCase)) targetSlide.MorphMode = MorphType.Characters;
                else targetSlide.MorphMode = MorphType.Objects;
            }

            // 3. Ambient Glow Orbs (chỉ bật khi AI yêu cầu tường minh)
            if (GetBool(dict, "ambientGlow", false))
            {
                var glowOrb1 = new ShapeNode
                {
                    Type = ShapeType.Ellipse,
                    FillColor = pal.Primary,
                    FillOpacity = pal.DarkTheme ? 0.14f : 0.08f,
                    StrokeColor = 0x00FFFFFF,
                    StrokeWidth = 0f,
                    Transform = new Transform2D(canvasW - 480f, -120f, 640f, 640f)
                };
                targetSlide.Shapes.Add(glowOrb1);

                var glowOrb2 = new ShapeNode
                {
                    Type = ShapeType.Ellipse,
                    FillColor = pal.Accent,
                    FillOpacity = pal.DarkTheme ? 0.12f : 0.06f,
                    StrokeColor = 0x00FFFFFF,
                    StrokeWidth = 0f,
                    Transform = new Transform2D(-160f, canvasH - 480f, 560f, 560f)
                };
                targetSlide.Shapes.Add(glowOrb2);
            }

            // 3. Render danh sách các phần tử (hỗ trợ tự động canh tọa độ nếu AI không truyền bounds)
            if (dict.ContainsKey("elements") && dict["elements"] is List<object>)
            {
                var elementsList = (List<object>)dict["elements"];

                // Auto-clamp multi-column card shapes and textboxes if AI specified overly tall bounds (e.g. h=650px for 2-column or 3-column card layouts)
                foreach (var elemObj in elementsList)
                {
                    if (elemObj is Dictionary<string, object>)
                    {
                        var elem = (Dictionary<string, object>)elemObj;
                        if (elem.ContainsKey("bounds") && elem["bounds"] is List<object>)
                        {
                            var bList = (List<object>)elem["bounds"];
                            if (bList.Count >= 4)
                            {
                                double wVal = Convert.ToDouble(bList[2], CultureInfo.InvariantCulture);
                                double hVal = Convert.ToDouble(bList[3], CultureInfo.InvariantCulture);
                                string sTypeStr = GetString(elem, "shapeType", GetString(elem, "shape", GetString(elem, "type", ""))).ToLowerInvariant();

                                if ((sTypeStr == "roundedrectangle" || sTypeStr == "card" || sTypeStr == "shape" || sTypeStr == "rectangle" || sTypeStr == "textbox" || sTypeStr == "text" || sTypeStr == "body") && wVal <= 650.0 && hVal > 280.0)
                                {
                                    bList[3] = 240.0;
                                }
                            }
                        }
                    }
                }

                List<Dictionary<string, object>> cards = new List<Dictionary<string, object>>();
                float nextHeaderY = 90f;

                foreach (var elemObj in elementsList)
                {
                    if (!(elemObj is Dictionary<string, object>)) continue;
                    var elem = (Dictionary<string, object>)elemObj;
                    string type = GetString(elem, "type", "textbox").ToLowerInvariant();

                    if (!elem.ContainsKey("bounds"))
                    {
                        if (type == "badge" || type == "pill")
                        {
                            elem["bounds"] = new List<object> { 100.0, (double)nextHeaderY, 280.0, 44.0 };
                            nextHeaderY += 56f;
                            RenderJsonCanvasElement(targetSlide, elem, pal, canvasW, canvasH);
                        }
                        else if (type == "title" || type == "heading" || (type == "textbox" && GetBool(elem, "bold", false)))
                        {
                            elem["bounds"] = new List<object> { 100.0, (double)nextHeaderY, (double)(canvasW - 200f), 60.0 };
                            nextHeaderY += 70f;
                            RenderJsonCanvasElement(targetSlide, elem, pal, canvasW, canvasH);
                        }
                        else if (type == "subtitle" || type == "quote")
                        {
                            elem["bounds"] = new List<object> { 100.0, (double)nextHeaderY, (double)(canvasW - 200f), 40.0 };
                            nextHeaderY += 50f;
                            RenderJsonCanvasElement(targetSlide, elem, pal, canvasW, canvasH);
                        }
                        else if (type == "card" || type == "kpi" || type == "step" || type == "item")
                        {
                            cards.Add(elem);
                        }
                        else if (type == "smartart" || type == "diagram" || type == "chart" || type == "table")
                        {
                            elem["bounds"] = new List<object> { 100.0, (double)(nextHeaderY + 20f), (double)(canvasW - 200f), (double)(canvasH - nextHeaderY - 100f) };
                            RenderJsonCanvasElement(targetSlide, elem, pal, canvasW, canvasH);
                        }
                        else
                        {
                            RenderJsonCanvasElement(targetSlide, elem, pal, canvasW, canvasH);
                        }
                    }
                    else
                    {
                        RenderJsonCanvasElement(targetSlide, elem, pal, canvasW, canvasH);
                    }
                }

                if (cards.Count > 0)
                {
                    float cardStartY = Math.Max(240f, nextHeaderY + 20f);
                    int maxCardLines = 1;
                    foreach (var c in cards)
                    {
                        string cDesc = GetString(c, "desc", GetString(c, "description", ""));
                        int l = cDesc.Split('\n').Length;
                        if (l > maxCardLines) maxCardLines = l;
                    }
                    float estimatedH = 40f + 40f + (maxCardLines * 32f) + 40f;
                    if (cards.Exists(c => c.ContainsKey("stat") || c.ContainsKey("value"))) estimatedH += 70f;
                    float cardH = Math.Min(340f, Math.Max(180f, estimatedH));

                    int cardCount = cards.Count;
                    float totalW = canvasW - 200f;
                    float gap = 30f;

                    if (cardCount <= 4)
                    {
                        float cardW = (totalW - (gap * (cardCount - 1))) / cardCount;
                        for (int i = 0; i < cardCount; i++)
                        {
                            float cx = 100f + i * (cardW + gap);
                            cards[i]["bounds"] = new List<object> { (double)cx, (double)cardStartY, (double)cardW, (double)cardH };
                            RenderJsonCanvasElement(targetSlide, cards[i], pal, canvasW, canvasH);
                        }
                    }
                    else
                    {
                        int cols = (cardCount + 1) / 2;
                        float cardW = (totalW - (gap * (cols - 1))) / cols;
                        float rowH = (cardH - gap) / 2f;

                        for (int i = 0; i < cardCount; i++)
                        {
                            int row = i / cols;
                            int col = i % cols;
                            float cx = 100f + col * (cardW + gap);
                            float cy = cardStartY + row * (rowH + gap);
                            cards[i]["bounds"] = new List<object> { (double)cx, (double)cy, (double)cardW, (double)rowH };
                            RenderJsonCanvasElement(targetSlide, cards[i], pal, canvasW, canvasH);
                        }
                    }
                }
            }
        }

        private static void RenderJsonCanvasElement(SlideNode targetSlide, Dictionary<string, object> elem, SlidePalette pal, float canvasW, float canvasH)
        {
            string type = GetString(elem, "type", "textbox").ToLowerInvariant();
            Transform2D bounds = ParseBounds(elem.ContainsKey("bounds") ? elem["bounds"] : null, canvasW, canvasH, 100f, 100f, 400f, 200f);

            if (type == "textbox" || type == "text" || type == "title" || type == "subtitle" || type == "heading" || type == "quote" || type == "label" || type == "paragraph" || type == "bullet" || type == "bullets")
            {
                string rawText = GetString(elem, "text", GetString(elem, "content", ""));
                string fontName = GetString(elem, "fontName", GetString(elem, "font", "Segoe UI"));

                float defaultSize = 22f;
                bool defaultBold = false;
                bool defaultItalic = false;
                int defaultColor = pal.BodyColor;
                TextAlignment defaultAlign = TextAlignment.Left;

                if (type == "title" || type == "heading")
                {
                    defaultSize = 44f;
                    defaultBold = true;
                    defaultColor = pal.TitleColor;
                    defaultAlign = TextAlignment.Left;
                }
                else if (type == "subtitle" || type == "quote")
                {
                    defaultSize = 21f;
                    defaultBold = false;
                    defaultItalic = true;
                    defaultColor = pal.SubtitleColor;
                    defaultAlign = TextAlignment.Left;
                }

                float fSize = (float)GetDouble(elem, "fontSize", GetDouble(elem, "size", defaultSize));
                bool isBold = GetBool(elem, "bold", defaultBold);
                bool isItalic = GetBool(elem, "italic", defaultItalic);
                bool isUnderline = GetBool(elem, "underline", false);
                int txtColor = ParseColor(GetString(elem, "color", GetString(elem, "textColor", "")), defaultColor);
                TextAlignment align = ParseAlignment(GetString(elem, "align", GetString(elem, "alignment", defaultAlign == TextAlignment.Center ? "center" : "left")));
                VerticalTextAlignment vAlign = ParseVerticalAlignment(GetString(elem, "verticalAlign", GetString(elem, "vAlign", "top")));

                string morphName = GetString(elem, "morphName", GetString(elem, "morphId", GetString(elem, "id", "")));

                string bgShapeStr = GetString(elem, "shapeType", GetString(elem, "shape", GetString(elem, "bgShape", "")));
                string fillStr = GetString(elem, "fill", GetString(elem, "fillColor", GetString(elem, "bg", "")));
                string strokeStr = GetString(elem, "stroke", GetString(elem, "strokeColor", GetString(elem, "border", "")));

                if (!string.IsNullOrEmpty(bgShapeStr) || !string.IsNullOrEmpty(fillStr) || !string.IsNullOrEmpty(strokeStr) || GetBool(elem, "shadow", false))
                {
                    ShapeType sType = !string.IsNullOrEmpty(bgShapeStr) ? ParseShapeType(bgShapeStr) : ShapeType.RoundedRectangle;
                    int fillCol = ParseColor(fillStr, pal.CardBg);
                    int strokeCol = ParseColor(strokeStr, pal.CardBorder);
                    float strokeW = (float)GetDouble(elem, "strokeWidth", GetDouble(elem, "borderWidth", 1.2));

                    if ((sType == ShapeType.RoundedRectangle || sType == ShapeType.Rectangle) && bounds.Height > 320f && !string.IsNullOrEmpty(rawText) && rawText.Length < 250)
                    {
                        int lines = rawText.Split('\n').Length;
                        if (lines <= 5)
                        {
                            bounds.Height = Math.Min(bounds.Height, Math.Max(180f, 50f + (lines * 32f) + 40f));
                        }
                    }

                    var bgShape = new ShapeNode
                    {
                        Type = sType,
                        FillColor = fillCol,
                        FillOpacity = (float)GetDouble(elem, "fillOpacity", 1.0),
                        StrokeColor = strokeCol,
                        StrokeWidth = strokeW,
                        HasShadow = GetBool(elem, "shadow", false),
                        Transform = bounds,
                        MorphName = morphName
                    };
                    targetSlide.Shapes.Add(bgShape);

                    var txtNode = CreateCleanTextBox(rawText, fontName, fSize, isBold, txtColor, align, bounds.X + 20f, bounds.Y + 16f, bounds.Width - 40f, bounds.Height - 32f, vAlign, isItalic);
                    txtNode.Underline = isUnderline;
                    txtNode.MorphName = morphName;
                    targetSlide.Shapes.Add(txtNode);
                }
                else
                {
                    var txtNode = CreateCleanTextBox(rawText, fontName, fSize, isBold, txtColor, align, bounds.X, bounds.Y, bounds.Width, bounds.Height, vAlign, isItalic);
                    txtNode.Underline = isUnderline;
                    txtNode.MorphName = morphName;
                    targetSlide.Shapes.Add(txtNode);
                }
            }
            else if (type == "smartart" || type == "diagram")
            {
                string saType = GetString(elem, "smartArtType", "Process_Chevron");
                string saTitle = GetString(elem, "title", "Sơ đồ Quy trình");
                string saText = "";

                if (elem.ContainsKey("nodes") && elem["nodes"] is List<object>)
                {
                    List<string> nodeLines = new List<string>();
                    foreach (var n in (List<object>)elem["nodes"])
                    {
                        if (n != null) nodeLines.Add(n.ToString());
                    }
                    saText = string.Join("\n", nodeLines.ToArray());
                }
                else
                {
                    saText = GetString(elem, "text", "Giai đoạn 1\nGiai đoạn 2\nGiai đoạn 3\nGiai đoạn 4");
                }

                ShapeNode smartArt = new ShapeNode
                {
                    Type = ShapeType.Rectangle,
                    Name = "SmartArt - " + saTitle,
                    SmartArtType = saType,
                    SmartArtTitle = saTitle,
                    Text = saText,
                    FillColor = 0x00FFFFFF,
                    StrokeColor = 0x00FFFFFF,
                    PictureStyleIndex = GetInt(elem, "colorPreset", 0),
                    Transform = bounds
                };
                targetSlide.Shapes.Add(smartArt);
            }
            else if (type == "chart")
            {
                string chartType = GetString(elem, "chartType", "Column_Clustered");
                string chartTitle = GetString(elem, "title", "Biểu Đồ Thống Kê");

                DocChartData chartData = DocChartData.CreateDefault(chartType);
                chartData.Title = chartTitle;

                if (elem.ContainsKey("categories") && elem["categories"] is List<object>)
                {
                    chartData.Categories.Clear();
                    foreach (var c in (List<object>)elem["categories"])
                    {
                        if (c != null) chartData.Categories.Add(c.ToString());
                    }
                }

                if (elem.ContainsKey("series") && elem["series"] is List<object>)
                {
                    chartData.SeriesNames.Clear();
                    var sList = (List<object>)elem["series"];
                    List<List<double>> seriesColumns = new List<List<double>>();

                    foreach (var sObj in sList)
                    {
                        if (sObj is Dictionary<string, object>)
                        {
                            var sDict = (Dictionary<string, object>)sObj;
                            chartData.SeriesNames.Add(GetString(sDict, "name", "Chuỗi"));
                            List<double> vals = new List<double>();
                            if (sDict.ContainsKey("values") && sDict["values"] is List<object>)
                            {
                                foreach (var v in (List<object>)sDict["values"])
                                {
                                    vals.Add(Convert.ToDouble(v, CultureInfo.InvariantCulture));
                                }
                            }
                            seriesColumns.Add(vals);
                        }
                    }

                    if (seriesColumns.Count > 0)
                    {
                        int numCats = chartData.Categories.Count > 0 ? chartData.Categories.Count : (seriesColumns[0].Count);
                        chartData.Values = new List<List<double>>();
                        for (int catIdx = 0; catIdx < numCats; catIdx++)
                        {
                            List<double> rowVals = new List<double>();
                            for (int sIdx = 0; sIdx < seriesColumns.Count; sIdx++)
                            {
                                rowVals.Add(catIdx < seriesColumns[sIdx].Count ? seriesColumns[sIdx][catIdx] : 0.0);
                            }
                            chartData.Values.Add(rowVals);
                        }
                    }
                }

                ShapeNode chartShape = new ShapeNode
                {
                    Type = ShapeType.Chart,
                    Name = "Chart - " + chartTitle,
                    ChartData = chartData,
                    Transform = bounds
                };
                targetSlide.Shapes.Add(chartShape);
            }
            else if (type == "table")
            {
                List<string> headers = new List<string>();
                if (elem.ContainsKey("headers") && elem["headers"] is List<object>)
                {
                    foreach (var h in (List<object>)elem["headers"])
                    {
                        if (h != null) headers.Add(h.ToString());
                    }
                }

                List<List<string>> rows = new List<List<string>>();
                if (elem.ContainsKey("rows") && elem["rows"] is List<object>)
                {
                    foreach (var rObj in (List<object>)elem["rows"])
                    {
                        if (rObj is List<object>)
                        {
                            List<string> rowCells = new List<string>();
                            foreach (var c in (List<object>)rObj)
                            {
                                rowCells.Add(c != null ? c.ToString() : "");
                            }
                            rows.Add(rowCells);
                        }
                    }
                }

                int numCols = Math.Max(1, headers.Count > 0 ? headers.Count : (rows.Count > 0 ? rows[0].Count : 3));
                int numRows = (headers.Count > 0 ? 1 : 0) + rows.Count;
                if (numRows == 0) numRows = 3;

                List<List<string>> tableData = new List<List<string>>();
                if (headers.Count > 0)
                {
                    tableData.Add(new List<string>(headers));
                }
                foreach (var r in rows)
                {
                    List<string> padded = new List<string>();
                    for (int c = 0; c < numCols; c++)
                    {
                        padded.Add(c < r.Count ? r[c] : "");
                    }
                    tableData.Add(padded);
                }

                ShapeNode tableShape = new ShapeNode
                {
                    Type = ShapeType.Table,
                    Name = "Table - Bảng Dữ Liệu",
                    TableRows = numRows,
                    TableColumns = numCols,
                    TableData = tableData,
                    TableHeaderRow = headers.Count > 0,
                    TableBandedRows = true,
                    Transform = bounds
                };
                targetSlide.Shapes.Add(tableShape);
            }
            else
            {
                // Hình dạng tự do (40+ loại: Hexagon, LuxuryHexagon, Gem, Shield, Star, Diamond, Chevron, RoundedRectangle...)
                ShapeType sType = ParseShapeType(GetString(elem, "shapeType", GetString(elem, "shape", type)));
                int fill = ParseColor(GetString(elem, "fill", GetString(elem, "fillColor", GetString(elem, "bg", ""))), pal.CardBg);
                int stroke = ParseColor(GetString(elem, "stroke", GetString(elem, "strokeColor", GetString(elem, "border", ""))), pal.CardBorder);
                float strokeW = (float)GetDouble(elem, "strokeWidth", GetDouble(elem, "borderWidth", 1.4));

                string sText = GetString(elem, "text", GetString(elem, "content", ""));
                if ((sType == ShapeType.RoundedRectangle || sType == ShapeType.Rectangle) && bounds.Height > 320f && !string.IsNullOrEmpty(sText) && sText.Length < 250)
                {
                    int lines = sText.Split('\n').Length;
                    if (lines <= 5)
                    {
                        bounds.Height = Math.Min(bounds.Height, Math.Max(180f, 50f + (lines * 32f) + 40f));
                    }
                }

                string morphName = GetString(elem, "morphName", GetString(elem, "morphId", GetString(elem, "id", "")));

                var shp = new ShapeNode
                {
                    Type = sType,
                    FillColor = fill,
                    FillOpacity = (float)GetDouble(elem, "fillOpacity", (float)GetDouble(elem, "opacity", 1.0)),
                    StrokeColor = stroke,
                    StrokeWidth = strokeW,
                    HasShadow = GetBool(elem, "shadow", false),
                    Transform = bounds,
                    MorphName = morphName
                };
                targetSlide.Shapes.Add(shp);

                if (!string.IsNullOrEmpty(sText))
                {
                    float fSize = (float)GetDouble(elem, "fontSize", GetDouble(elem, "size", 22.0));
                    bool isBold = GetBool(elem, "bold", false);
                    bool isItalic = GetBool(elem, "italic", false);
                    int txtCol = ParseColor(GetString(elem, "textColor", GetString(elem, "color", "")), pal.TitleColor);
                    TextAlignment align = ParseAlignment(GetString(elem, "align", "left"));
                    VerticalTextAlignment vAlign = ParseVerticalAlignment(GetString(elem, "verticalAlign", GetString(elem, "vAlign", "top")));

                    var txtNode = CreateCleanTextBox(sText, "Segoe UI", fSize, isBold, txtCol, align, bounds.X + 20f, bounds.Y + 16f, bounds.Width - 40f, bounds.Height - 32f, vAlign, isItalic);
                    txtNode.MorphName = morphName;
                    targetSlide.Shapes.Add(txtNode);
                }
            }
        }

        #endregion

        #region Extended DSL Layout Dispatcher (DESIGN_SLIDE, MODIFY_SLIDE, MULTI_SLIDE)

        private static void ExecuteModifySlide(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm.SlidesView == null || appForm.SlidesView.Presentation == null || appForm.SlidesView.Presentation.Slides.Count == 0) return;

            var slides = appForm.SlidesView;
            var pres = slides.Presentation;
            int curIdx = Math.Max(0, Math.Min(pres.Slides.Count - 1, slides.ActiveSlideIndex));
            var targetSlide = pres.Slides[curIdx];
            slides.SaveUndoState();

            List<string> changesDone = new List<string>();

            // 1. Try parsing JSON format
            object parsedObj = JsonLite.Parse(payload);
            if (parsedObj is Dictionary<string, object>)
            {
                var dict = (Dictionary<string, object>)parsedObj;
                bool allSlides = GetBool(dict, "allSlides", false) || GetBool(dict, "applyAll", false) || payload.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0;
                string themeStr = GetString(dict, "theme", GetString(dict, "style", ""));
                SlidePalette pal = !string.IsNullOrEmpty(themeStr) ? ResolveThemePalette(themeStr) : null;

                List<SlideNode> targetSlides = new List<SlideNode>();
                if (allSlides)
                {
                    targetSlides.AddRange(pres.Slides);
                }
                else if (dict.ContainsKey("slides") && dict["slides"] is List<object>)
                {
                    var list = (List<object>)dict["slides"];
                    foreach (var item in list)
                    {
                        int sNum = 0;
                        if (item is int) sNum = (int)item;
                        else if (item is long) sNum = (int)(long)item;
                        else if (item is double) sNum = (int)(double)item;
                        else if (item != null) int.TryParse(item.ToString(), out sNum);

                        int sIdx = sNum - 1; // 1-based to 0-based
                        if (sIdx >= 0 && sIdx < pres.Slides.Count)
                        {
                            if (!targetSlides.Contains(pres.Slides[sIdx])) targetSlides.Add(pres.Slides[sIdx]);
                        }
                    }
                }
                else if (dict.ContainsKey("slideNumbers") && dict["slideNumbers"] is List<object>)
                {
                    var list = (List<object>)dict["slideNumbers"];
                    foreach (var item in list)
                    {
                        int sNum = 0;
                        if (item is int) sNum = (int)item;
                        else if (item is long) sNum = (int)(long)item;
                        else if (item is double) sNum = (int)(double)item;
                        else if (item != null) int.TryParse(item.ToString(), out sNum);

                        int sIdx = sNum - 1;
                        if (sIdx >= 0 && sIdx < pres.Slides.Count)
                        {
                            if (!targetSlides.Contains(pres.Slides[sIdx])) targetSlides.Add(pres.Slides[sIdx]);
                        }
                    }
                }
                else if (dict.ContainsKey("slideIndices") && dict["slideIndices"] is List<object>)
                {
                    var list = (List<object>)dict["slideIndices"];
                    foreach (var item in list)
                    {
                        int idx = 0;
                        if (item is int) idx = (int)item;
                        else if (item is long) idx = (int)(long)item;
                        else if (item is double) idx = (int)(double)item;
                        else if (item != null) int.TryParse(item.ToString(), out idx);

                        if (idx >= 0 && idx < pres.Slides.Count)
                        {
                            if (!targetSlides.Contains(pres.Slides[idx])) targetSlides.Add(pres.Slides[idx]);
                        }
                    }
                }
                else
                {
                    int singleSlideNum = GetInt(dict, "slide", GetInt(dict, "slideNumber", 0));
                    if (singleSlideNum > 0 && singleSlideNum <= pres.Slides.Count)
                    {
                        targetSlides.Add(pres.Slides[singleSlideNum - 1]);
                    }
                    else
                    {
                        int singleIdx = GetInt(dict, "slideIndex", curIdx);
                        if (singleIdx >= 0 && singleIdx < pres.Slides.Count)
                        {
                            targetSlides.Add(pres.Slides[singleIdx]);
                        }
                    }
                }

                if (targetSlides.Count == 0 && curIdx >= 0 && curIdx < pres.Slides.Count)
                {
                    targetSlides.Add(pres.Slides[curIdx]);
                }

                if (dict.ContainsKey("background") && dict["background"] is Dictionary<string, object>)
                {
                    var bgDict = (Dictionary<string, object>)dict["background"];
                    string bgType = GetString(bgDict, "type", "gradient");
                    foreach (var s in targetSlides)
                    {
                        if (bgType == "solid")
                        {
                            int col = ParseColor(GetString(bgDict, "color", ""), pal != null ? pal.Bg1 : 0x0B1E3B);
                            s.IsGradientBg = false;
                            s.BgFillType = BackgroundFillType.Solid;
                            s.BgColor = col;
                            s.BgGradientStops = new List<GradientStop> { new GradientStop(col, 0f), new GradientStop(col, 1f) };
                        }
                        else
                        {
                            int c1 = ParseColor(GetString(bgDict, "color1", ""), pal != null ? pal.Bg1 : 0x0B1E3B);
                            int c2 = ParseColor(GetString(bgDict, "color2", ""), pal != null ? pal.Bg2 : 0x1E3A8A);
                            float ang = (float)GetDouble(bgDict, "angle", 135.0);
                            s.IsGradientBg = true;
                            s.BgFillType = BackgroundFillType.Gradient;
                            s.BgColor = c1;
                            s.BgColor2 = c2;
                            s.BgGradientAngle = ang;
                            s.BgGradientStops = new List<GradientStop> { new GradientStop(c1, 0f), new GradientStop(c2, 1f) };
                        }
                    }
                    changesDone.Add(string.Format("Đổi màu nền {0} slide", targetSlides.Count));
                }
                else if (pal != null)
                {
                    foreach (var s in targetSlides)
                    {
                        s.IsGradientBg = true;
                        s.BgFillType = BackgroundFillType.Gradient;
                        s.BgColor = pal.Bg1;
                        s.BgColor2 = pal.Bg2;
                        s.BgGradientAngle = 135f;
                        s.BgGradientStops = new List<GradientStop> { new GradientStop(pal.Bg1, 0f), new GradientStop(pal.Bg2, 1f) };
                    }
                    changesDone.Add(string.Format("Áp dụng phong cách '{0}' cho {1} slide", themeStr, targetSlides.Count));
                }

                if (dict.ContainsKey("transition") || dict.ContainsKey("transitionType") || dict.ContainsKey("effect"))
                {
                    string transStr = GetString(dict, "transition", GetString(dict, "transitionType", GetString(dict, "effect", "")));
                    TransitionType tt = ParseTransitionType(transStr);
                    float transDur = (float)GetDouble(dict, "transitionDuration", GetDouble(dict, "duration", (tt == TransitionType.Morph ? 1.8 : 2.2)));
                    string morphModeStr = GetString(dict, "morphMode", "by_object");
                    MorphType mMode = MorphType.Objects;
                    if (morphModeStr.Equals("Words", StringComparison.OrdinalIgnoreCase)) mMode = MorphType.Words;
                    else if (morphModeStr.Equals("Characters", StringComparison.OrdinalIgnoreCase)) mMode = MorphType.Characters;

                    foreach (var s in targetSlides)
                    {
                        s.Transition = tt;
                        s.TransitionDuration = transDur;
                        s.MorphMode = mMode;

                        if (tt == TransitionType.Morph)
                        {
                            int sIdx = pres.Slides.IndexOf(s);
                            if (sIdx > 0)
                            {
                                SlideNode prevSlide = pres.Slides[sIdx - 1];
                                AutoEnhanceMorphTransition(prevSlide, s);
                            }
                        }
                    }
                    changesDone.Add(string.Format("Đổi hiệu ứng chuyển cảnh thành '{0}' cho {1} slide", transStr, targetSlides.Count));
                }

                if (dict.ContainsKey("find") && dict.ContainsKey("replace"))
                {
                    string find = GetString(dict, "find", "");
                    string rep = GetString(dict, "replace", "");
                    if (!string.IsNullOrEmpty(find))
                    {
                        foreach (var s in targetSlides)
                        {
                            foreach (var sh in s.Shapes)
                            {
                                if (!string.IsNullOrEmpty(sh.Text) && sh.Text.IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    int idx = sh.Text.IndexOf(find, StringComparison.OrdinalIgnoreCase);
                                    sh.Text = sh.Text.Substring(0, idx) + rep + sh.Text.Substring(idx + find.Length);
                                    changesDone.Add(string.Format("'{0}' ➔ '{1}'", find, rep));
                                }
                            }
                        }
                    }
                }
            }

            // 2. Also process Line-by-Line DSL if present
            var lines = payload.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("{") || line.StartsWith("}") || line.StartsWith("\"")) continue;

                // Syntax A: FIND -> REPLACE or FIND => REPLACE or FIND ➔ REPLACE
                if (line.Contains("->") || line.Contains("=>") || line.Contains("➔") || line.Contains("-->"))
                {
                    var parts = line.Split(new[] { "->", "=>", "➔", "-->" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        string find = parts[0].Trim().Trim('"', '\'', '`', ',', '-');
                        string replace = parts[1].Trim().Trim('"', '\'', '`', ',', '-');
                        bool found = false;
                        foreach (var shape in targetSlide.Shapes)
                        {
                            if (!string.IsNullOrEmpty(shape.Text) && shape.Text.IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                int idx = shape.Text.IndexOf(find, StringComparison.OrdinalIgnoreCase);
                                shape.Text = shape.Text.Substring(0, idx) + replace + shape.Text.Substring(idx + find.Length);
                                found = true;
                                changesDone.Add(string.Format("'{0}' ➔ '{1}'", find, replace));
                            }
                        }
                        if (!found)
                        {
                            // Try partial word match
                            foreach (var shape in targetSlide.Shapes)
                            {
                                if (!string.IsNullOrEmpty(shape.Text) && (shape.Text.IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0 || find.IndexOf(shape.Text, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    shape.Text = replace;
                                    changesDone.Add(string.Format("'{0}' ➔ '{1}'", find, replace));
                                    break;
                                }
                            }
                        }
                    }
                }
                // Syntax B: KEY: VALUE (e.g. RAM: Tối thiểu 8GB / Title: Cấu hình mới / Style: EmeraldGreen)
                else if (line.Contains(":"))
                {
                    int colonIdx = line.IndexOf(':');
                    string key = line.Substring(0, colonIdx).Trim().ToLowerInvariant();
                    string val = line.Substring(colonIdx + 1).Trim().Trim('"', '\'', '`');

                    if (key.Contains("theme") || key.Contains("style") || key.Contains("màu") || key.Contains("bg"))
                    {
                        SlidePalette pal = ResolveThemePalette(val);
                        bool isAll = line.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0 || payload.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0;
                        var targetList = isAll ? pres.Slides : new List<SlideNode> { targetSlide };
                        foreach (var s in targetList)
                        {
                            s.IsGradientBg = true;
                            s.BgFillType = BackgroundFillType.Gradient;
                            s.BgColor = pal.Bg1;
                            s.BgColor2 = pal.Bg2;
                            s.BgGradientAngle = 135f;
                            s.BgGradientStops = new List<GradientStop> { new GradientStop(pal.Bg1, 0f), new GradientStop(pal.Bg2, 1f) };
                        }
                        changesDone.Add("Đổi màu giao diện sang " + val);
                    }
                    else
                    {
                        bool found = false;
                        foreach (var shape in targetSlide.Shapes)
                        {
                            if (!string.IsNullOrEmpty(shape.Text) && shape.Text.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                shape.Text = val;
                                found = true;
                                changesDone.Add(string.Format("Cập nhật '{0}' ➔ '{1}'", key, val));
                                break;
                            }
                        }
                        if (!found && targetSlide.Shapes.Count > 0)
                        {
                            for (int i = 0; i < targetSlide.Shapes.Count; i++)
                            {
                                var sh = targetSlide.Shapes[i];
                                if (!string.IsNullOrEmpty(sh.Text) && sh.Text.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if (i + 1 < targetSlide.Shapes.Count && targetSlide.Shapes[i + 1].Type == ShapeType.TextBox)
                                    {
                                        targetSlide.Shapes[i + 1].Text = val;
                                        changesDone.Add(string.Format("Cập nhật '{0}' ➔ '{1}'", key, val));
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            appForm.Invalidate();
            if (changesDone.Count > 0)
            {
                result.ExecutionSummary = "✅ " + string.Join("; ", changesDone.ToArray());
            }
            else
            {
                result.ExecutionSummary = "✅ Đã sửa đổi nội dung trên slide.";
            }
        }

        private static void ExecuteMultiSlide(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm.SlidesView == null || appForm.SlidesView.Presentation == null) return;
            var slides = appForm.SlidesView;
            var pres = slides.Presentation;
            slides.SaveUndoState();

            string[] rawSlides = Regex.Split(payload, @"(?:===+\s*SLIDE\s*===+|\[SLIDE\]|(?=##\s*Slide\s*\d+))", RegexOptions.IgnoreCase);
            List<string> validSlideBlocks = new List<string>();
            foreach (var r in rawSlides)
            {
                string t = r.Trim();
                if (!string.IsNullOrEmpty(t) && t.Length > 10)
                {
                    validSlideBlocks.Add(t);
                }
            }

            if (validSlideBlocks.Count == 0)
            {
                validSlideBlocks.Add(payload);
            }

            int count = 0;
            for (int i = 0; i < validSlideBlocks.Count; i++)
            {
                var block = validSlideBlocks[i];
                var subResult = new AiActionResult();
                ExecuteDesignSlide(appForm, block, subResult);
                count++;
            }

            appForm.Invalidate();
            result.ExecutionSummary = string.Format("🎨 Đã tự động tạo hoàn chỉnh {0} Slide theo yêu cầu!", count);
        }

        private static void ExecuteDesignSlide(AppForm appForm, string payload, AiActionResult result)
        {
            if (appForm.SlidesView == null || appForm.SlidesView.Presentation == null) return;
            ExecuteGenerateSlideCanvas(appForm, payload, result);
        }

        #endregion

        #region Helpers & Parsers

        private static string CleanSlideText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string clean = raw;
            clean = Regex.Replace(clean, @"^#{1,6}\s*", "", RegexOptions.Multiline);
            clean = clean.Replace("**", "").Replace("__", "");
            clean = Regex.Replace(clean, @"\[(?:CAPTION|SUBCAPTION|Slide \d+|Shape #\d+[^\]]*)\]", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"^\d+[\.:]\s*", ""); // remove leading "1: "
            return clean.Trim();
        }

        private static string FormatBulletPoints(string rawText, string fallback)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return fallback;
            string clean = CleanSlideText(rawText).Replace("\\n", "\n").Replace("\\r", "").Trim();

            var lines = clean.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var line in lines)
                {
                    string l = line.Trim();
                    if (!string.IsNullOrEmpty(l))
                    {
                        if (l.StartsWith("- ") || l.StartsWith("* ")) l = "• " + l.Substring(2).Trim();
                        else if (!l.StartsWith("•")) l = "• " + l;
                        sb.AppendLine(l);
                    }
                }
                return sb.ToString().TrimEnd();
            }

            if (clean.StartsWith("- ") || clean.StartsWith("* ")) clean = "• " + clean.Substring(2).Trim();
            return clean;
        }

        private static string CleanDisplaySymbols(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("🚀", "★ ")
                       .Replace("✦", "◆ ")
                       .Replace("🌿", "● ")
                       .Replace("📈", "▲ ")
                       .Replace("⚖️", "◆ ")
                       .Replace("⚖", "◆ ")
                       .Replace("👤", "● ")
                       .Replace("⭐", "★ ")
                       .Replace("💡", "◆ ")
                       .Replace("📝", "• ")
                       .Replace("📊", "▲ ")
                       .Trim();
        }

        private static ShapeNode CreateCleanTextBox(string text, string fontName, float fontSize, bool bold, int textColor, TextAlignment align, float x, float y, float w, float h, VerticalTextAlignment vAlign = VerticalTextAlignment.Top, bool italic = false)
        {
            float finalH = h;
            if (!string.IsNullOrEmpty(text) && text.Length < 350 && w <= 750f && h > 260f)
            {
                int lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (lines <= 5)
                {
                    finalH = Math.Min(h, Math.Max(120f, 24f + (lines * (fontSize * 1.45f)) + 24f));
                }
            }

            return new ShapeNode
            {
                Type = ShapeType.TextBox,
                Text = CleanDisplaySymbols(text),
                FontName = fontName ?? "Segoe UI",
                FontSize = fontSize,
                Bold = bold,
                Italic = italic,
                TextColor = textColor,
                Alignment = align,
                VerticalAlignment = vAlign,
                LineSpacing = 1.38f,
                FillColor = 0x00FFFFFF, // Trong suốt 100%
                FillOpacity = 1.0f,
                StrokeColor = 0x00FFFFFF, // Không viền
                StrokeWidth = 0.0f,
                HasShadow = false,
                Transform = new Transform2D(x, y, w, finalH)
            };
        }

        private static VerticalTextAlignment ParseVerticalAlignment(string vAlignStr)
        {
            if (string.IsNullOrEmpty(vAlignStr)) return VerticalTextAlignment.Top;
            string s = vAlignStr.Trim().ToLowerInvariant();
            if (s == "center" || s == "middle") return VerticalTextAlignment.Middle;
            if (s == "bottom") return VerticalTextAlignment.Bottom;
            return VerticalTextAlignment.Top;
        }

        public static int ParseColorExtended(string hexOrName, int fallback)
        {
            if (string.IsNullOrWhiteSpace(hexOrName)) return fallback;
            string s = hexOrName.Trim().ToLowerInvariant();

            // Map màu tiếng Việt và tiếng Anh phổ biến
            if (s.Contains("vàng nhạt") || s.Contains("vang nhat") || s.Contains("light yellow") || s.Contains("lightyellow") || s.Contains("pale yellow") || s.Contains("cream"))
                return unchecked((int)0xFFFEF9C3); // Vàng nhạt pastel chuẩn (#FEF9C3)
            if (s.Contains("vàng chanh") || s.Contains("vang chanh") || s.Contains("lemon"))
                return unchecked((int)0xFFFEF08A);
            if (s.Contains("vàng") || s.Contains("vang") || s.Contains("yellow"))
                return unchecked((int)0xFFFDE047);
            if (s.Contains("xanh lá nhạt") || s.Contains("xanh la nhat") || s.Contains("light green") || s.Contains("lightgreen") || s.Contains("mint"))
                return unchecked((int)0xFFDCFCE7); // Xanh lá nhạt pastel (#DCFCE7)
            if (s.Contains("xanh lá") || s.Contains("xanh la") || s.Contains("green") || s.Contains("luc"))
                return unchecked((int)0xFF22C55E);
            if (s.Contains("xanh dương nhạt") || s.Contains("xanh bien nhat") || s.Contains("light blue") || s.Contains("lightblue") || s.Contains("sky"))
                return unchecked((int)0xFFE0F2FE); // Xanh dương nhạt pastel (#E0F2FE)
            if (s.Contains("xanh dương") || s.Contains("xanh bien") || s.Contains("blue"))
                return unchecked((int)0xFF3B82F6);
            if (s.Contains("đỏ nhạt") || s.Contains("do nhat") || s.Contains("hồng nhạt") || s.Contains("hong nhat") || s.Contains("light red") || s.Contains("pink"))
                return unchecked((int)0xFFFEE2E2); // Đỏ/hồng nhạt pastel (#FEE2E2)
            if (s.Contains("đỏ") || s.Contains("do") || s.Contains("red"))
                return unchecked((int)0xFFEF4444);
            if (s.Contains("cam nhạt") || s.Contains("cam nhat") || s.Contains("light orange") || s.Contains("peach"))
                return unchecked((int)0xFFFFEDD5);
            if (s.Contains("cam") || s.Contains("orange"))
                return unchecked((int)0xFFF97316);
            if (s.Contains("tím nhạt") || s.Contains("tim nhat") || s.Contains("light purple") || s.Contains("lavender"))
                return unchecked((int)0xFFF3E8FF);
            if (s.Contains("tím") || s.Contains("tim") || s.Contains("purple"))
                return unchecked((int)0xFFA855F7);
            if (s.Contains("xám nhạt") || s.Contains("xam nhat") || s.Contains("light gray") || s.Contains("light grey"))
                return unchecked((int)0xFFF1F5F9);
            if (s.Contains("xám") || s.Contains("xam") || s.Contains("gray") || s.Contains("grey"))
                return unchecked((int)0xFF94A3B8);
            if (s.Contains("trắng") || s.Contains("trang") || s.Contains("white"))
                return unchecked((int)0xFFFFFFFF);
            if (s.Contains("đen") || s.Contains("den") || s.Contains("black"))
                return unchecked((int)0xFF000000);

            string c = hexOrName.Trim().TrimStart('#');
            if (c.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) c = c.Substring(2);

            if (c.Length == 6)
            {
                uint rgb;
                if (uint.TryParse(c, NumberStyles.HexNumber, null, out rgb))
                {
                    return unchecked((int)(0xFF000000 | rgb));
                }
            }
            else if (c.Length == 8)
            {
                uint argb;
                if (uint.TryParse(c, NumberStyles.HexNumber, null, out argb))
                {
                    return unchecked((int)argb);
                }
            }
            return fallback;
        }

        private static int ParseColor(string hexOrName, int fallback)
        {
            return ParseColorExtended(hexOrName, fallback);
        }

        private static Transform2D ParseBounds(object boundsObj, float canvasW, float canvasH, float defX, float defY, float defW, float defH)
        {
            if (boundsObj is List<object>)
            {
                var list = (List<object>)boundsObj;
                if (list.Count >= 4)
                {
                    float x = (float)Convert.ToDouble(list[0], CultureInfo.InvariantCulture);
                    float y = (float)Convert.ToDouble(list[1], CultureInfo.InvariantCulture);
                    float w = (float)Convert.ToDouble(list[2], CultureInfo.InvariantCulture);
                    float h = (float)Convert.ToDouble(list[3], CultureInfo.InvariantCulture);

                    if (x >= 0 && y >= 0 && x <= 1.0f && y <= 1.0f && w <= 1.0f && h <= 1.0f && (w > 0 || h > 0))
                    {
                        x *= canvasW;
                        y *= canvasH;
                        w *= canvasW;
                        h *= canvasH;
                    }
                    return new Transform2D(x, y, w, h);
                }
            }
            return new Transform2D(defX, defY, defW, defH);
        }

        private static ShapeType ParseShapeType(string shapeTypeName)
        {
            if (string.IsNullOrEmpty(shapeTypeName)) return ShapeType.RoundedRectangle;
            string s = shapeTypeName.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

            if (s == "roundedrectangle" || s == "roundrect" || s == "card" || s == "image" || s == "picture" || s == "photo" || s == "img") return ShapeType.RoundedRectangle;
            if (s == "rectangle" || s == "rect" || s == "box") return ShapeType.Rectangle;
            if (s == "ellipse" || s == "circle" || s == "oval") return ShapeType.Ellipse;
            if (s == "diamond" || s == "rhombus") return ShapeType.Diamond;
            if (s == "hexagon") return ShapeType.Hexagon;
            if (s == "luxuryhexagon") return ShapeType.LuxuryHexagon;
            if (s == "shield") return ShapeType.Shield;
            if (s == "gem") return ShapeType.Gem;
            if (s == "star" || s == "star5") return ShapeType.Star;
            if (s == "starburst") return ShapeType.Starburst;
            if (s == "banner") return ShapeType.Banner;
            if (s == "chevron") return ShapeType.Chevron;
            if (s == "arrow" || s == "blockarrowright") return ShapeType.BlockArrowRight;
            if (s == "triangle") return ShapeType.Triangle;
            if (s == "righttriangle") return ShapeType.RightTriangle;
            if (s == "callout") return ShapeType.Callout;
            if (s == "heart") return ShapeType.Heart;
            if (s == "cloud") return ShapeType.Cloud;
            if (s == "sprout" || s == "leaf") return ShapeType.Sprout;
            if (s == "cube") return ShapeType.Cube;
            if (s == "can") return ShapeType.Can;
            if (s == "lightning") return ShapeType.Lightning;
            if (s == "line") return ShapeType.Line;

            return ShapeType.RoundedRectangle;
        }

        private static TextAlignment ParseAlignment(string alignStr)
        {
            if (string.IsNullOrEmpty(alignStr)) return TextAlignment.Left;
            string s = alignStr.Trim().ToLowerInvariant();
            if (s == "center" || s == "middle") return TextAlignment.Center;
            if (s == "right") return TextAlignment.Right;
            return TextAlignment.Left;
        }

        private static TransitionType ParseTransitionType(string s)
        {
            if (string.IsNullOrEmpty(s)) return TransitionType.None;
            string clean = s.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

            if (clean == "morph" || clean == "bienhinh") return TransitionType.Morph;
            if (clean == "fade" || clean == "modan") return TransitionType.Fade;
            if (clean == "push" || clean == "day") return TransitionType.Push;
            if (clean == "wipe" || clean == "quet") return TransitionType.Wipe;
            if (clean == "split" || clean == "tach") return TransitionType.Split;
            if (clean == "reveal" || clean == "tietlo") return TransitionType.Reveal;
            if (clean == "cut") return TransitionType.Cut;
            if (clean == "randombars" || clean == "bars") return TransitionType.RandomBars;
            if (clean == "wheel" || clean == "banhxe") return TransitionType.Wheel;
            if (clean == "shape" || clean == "hinhdang") return TransitionType.Shape;
            if (clean == "uncover") return TransitionType.Uncover;
            if (clean == "cover") return TransitionType.Cover;
            if (clean == "flash" || clean == "chopsang") return TransitionType.Flash;
            if (clean == "fallover" || clean == "dosap") return TransitionType.FallOver;
            if (clean == "curtains" || clean == "remcua") return TransitionType.Curtains;
            if (clean == "wind" || clean == "gio") return TransitionType.Wind;
            if (clean == "prestige") return TransitionType.Prestige;
            if (clean == "fracture" || clean == "vovun") return TransitionType.Fracture;
            if (clean == "crush" || clean == "vovien") return TransitionType.Crush;
            if (clean == "peeloff" || clean == "boctach") return TransitionType.PeelOff;
            if (clean == "pagecurl" || clean == "lattrang") return TransitionType.PageCurl;
            if (clean == "airplane" || clean == "maybay") return TransitionType.Airplane;
            if (clean == "origami" || clean == "xepgiay") return TransitionType.Origami;
            if (clean == "dissolve" || clean == "hoatan") return TransitionType.Dissolve;
            if (clean == "checker" || clean == "caro") return TransitionType.Checker;
            if (clean == "blinds" || clean == "mancua") return TransitionType.Blinds;
            if (clean == "clock" || clean == "dongho") return TransitionType.Clock;
            if (clean == "ripple" || clean == "gonsong") return TransitionType.Ripple;
            if (clean == "honeycomb" || clean == "toong") return TransitionType.Honeycomb;
            if (clean == "glitter" || clean == "laplanh") return TransitionType.Glitter;
            if (clean == "vortex" || clean == "vongxoay") return TransitionType.Vortex;
            if (clean == "shred" || clean == "xegiay") return TransitionType.Shred;
            if (clean == "switch") return TransitionType.Switch;
            if (clean == "flip" || clean == "lat") return TransitionType.Flip;
            if (clean == "gallery" || clean == "phongtranh") return TransitionType.Gallery;
            if (clean == "cube" || clean == "lapphuong") return TransitionType.Cube;
            if (clean == "doors" || clean == "cuadoi") return TransitionType.Doors;
            if (clean == "box" || clean == "hop") return TransitionType.Box;
            if (clean == "comb" || clean == "luoc") return TransitionType.Comb;
            if (clean == "zoom" || clean == "thuphong") return TransitionType.Zoom;
            if (clean == "pan" || clean == "lia") return TransitionType.Pan;
            if (clean == "ferriswheel" || clean == "vongquay") return TransitionType.FerrisWheel;
            if (clean == "conveyor" || clean == "bangchuyen") return TransitionType.Conveyor;
            if (clean == "rotate" || clean == "xoay") return TransitionType.Rotate;
            if (clean == "window" || clean == "cuaso") return TransitionType.Window;
            if (clean == "orbit" || clean == "quydao") return TransitionType.Orbit;
            if (clean == "flythrough" || clean == "xuyenkhong") return TransitionType.FlyThrough;

            // Exclusive GenOffice (Ưu tiên phân biệt chính xác Chảy tràn và Thủy dịch)
            if (clean.Contains("smoothflow") || clean.Contains("chaytran") || clean == "flow") return TransitionType.GenOffice_SmoothFlow;
            if (clean.Contains("liquid") || clean.Contains("fluid") || clean.Contains("thuydich")) return TransitionType.GenOffice_LiquidFluid;
            if (clean.Contains("goldenribbon") || clean.Contains("ribbon") || clean.Contains("luavang")) return TransitionType.GenOffice_GoldenRibbon;
            if (clean.Contains("prism") || clean.Contains("langkinh")) return TransitionType.GenOffice_CinematicPrism;
            if (clean.Contains("matrix") || clean.Contains("muaso")) return TransitionType.GenOffice_MatrixRain;
            if (clean.Contains("neon") || clean.Contains("xungneon")) return TransitionType.GenOffice_NeonPulse;
            if (clean.Contains("portal") || clean.Contains("losau")) return TransitionType.GenOffice_PortalWarp;
            if (clean.Contains("supernova") || clean.Contains("sieutantinh")) return TransitionType.GenOffice_Supernova;
            if (clean.Contains("glitch") || clean.Contains("hologram")) return TransitionType.GenOffice_HologramGlitch;
            if (clean.Contains("kaleidoscope") || clean.Contains("vanhoa")) return TransitionType.GenOffice_Kaleidoscope;
            if (clean.Contains("starlight") || clean.Contains("baosao")) return TransitionType.GenOffice_Starlight;

            try
            {
                TransitionType parsed;
                if (Enum.TryParse<TransitionType>(s, true, out parsed)) return parsed;
            }
            catch { }

            return TransitionType.Fade;
        }

        private static string GetString(Dictionary<string, object> dict, string key, string fallback)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                return dict[key].ToString();
            }
            return fallback;
        }

        private static bool GetBool(Dictionary<string, object> dict, string key, bool fallback)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                object v = dict[key];
                if (v is bool) return (bool)v;
                bool b;
                if (bool.TryParse(v.ToString(), out b)) return b;
            }
            return fallback;
        }

        private static double GetDouble(Dictionary<string, object> dict, string key, double fallback)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                try
                {
                    return Convert.ToDouble(dict[key], CultureInfo.InvariantCulture);
                }
                catch { }
            }
            return fallback;
        }

        private static int GetInt(Dictionary<string, object> dict, string key, int fallback)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                try
                {
                    return Convert.ToInt32(dict[key], CultureInfo.InvariantCulture);
                }
                catch { }
            }
            return fallback;
        }

        private static bool IsMetaInstructionLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            string l = line.Trim().ToLowerInvariant();

            if (l.StartsWith("[") && (l.Contains("slide") || l.Contains("tạo") || l.Contains("hiển thị") || l.Contains("logo") || l.Contains("trình chiếu") || l.Contains("hình ảnh") || l.Contains("thông tin")))
                return true;

            if (l.StartsWith("tạo một slide") || l.StartsWith("tạo slide") || l.StartsWith("hiển thị logo") || l.Contains("tạo một slide") || l.Contains("hiển thị logo"))
                return true;

            if (l.StartsWith("[shape") || l.Contains("tọa độ") || l.Contains("textbox:"))
                return true;

            return false;
        }

        private static AiSlideDesignSpec ParseSlideDesignSpec(string payload)
        {
            AiSlideDesignSpec spec = new AiSlideDesignSpec();
            if (string.IsNullOrEmpty(payload)) return spec;

            string cleanPayload = payload.Trim();
            if (cleanPayload.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) cleanPayload = cleanPayload.Substring(7);
            else if (cleanPayload.StartsWith("```")) cleanPayload = cleanPayload.Substring(3);
            if (cleanPayload.EndsWith("```")) cleanPayload = cleanPayload.Substring(0, cleanPayload.Length - 3);
            cleanPayload = cleanPayload.Trim();

            // 1. Try parsing JSON if payload is a JSON object
            int firstBrace = cleanPayload.IndexOf('{');
            int lastBrace = cleanPayload.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                string jsonStr = cleanPayload.Substring(firstBrace, lastBrace - firstBrace + 1);
                jsonStr = Regex.Replace(jsonStr, @"//[^\r\n]*", "");
                try
                {
                    var jsonDict = JsonLite.Parse(jsonStr) as Dictionary<string, object>;
                    if (jsonDict != null)
                    {
                        spec.Layout = GetString(jsonDict, "layout", spec.Layout);
                        spec.ThemeStyle = GetString(jsonDict, "theme", GetString(jsonDict, "style", spec.ThemeStyle));
                        spec.CategoryBadge = GetString(jsonDict, "badge", GetString(jsonDict, "category", spec.CategoryBadge));
                        spec.SlideTitle = GetString(jsonDict, "title", spec.SlideTitle);
                        spec.SlideSubtitle = GetString(jsonDict, "subtitle", spec.SlideSubtitle);
                        spec.FooterNote = GetString(jsonDict, "footer", spec.FooterNote);
                        spec.CreateNewSlide = GetBool(jsonDict, "createNewSlide", spec.CreateNewSlide);

                        if (jsonDict.ContainsKey("cards") && jsonDict["cards"] is List<object>)
                        {
                            foreach (var cObj in (List<object>)jsonDict["cards"])
                            {
                                if (cObj is Dictionary<string, object>)
                                {
                                    var cDict = (Dictionary<string, object>)cObj;
                                    var card = new AiSlideCardSpec
                                    {
                                        Title = GetString(cDict, "title", "Ý chính"),
                                        Description = GetString(cDict, "desc", GetString(cDict, "description", "")),
                                        IconOrBadge = GetString(cDict, "icon", ""),
                                        StatNumber = GetString(cDict, "stat", GetString(cDict, "value", ""))
                                    };
                                    spec.Cards.Add(card);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(spec.SlideTitle) || spec.Cards.Count > 0)
                        {
                            return spec;
                        }
                    }
                }
                catch { }
            }

            // 2. Line-by-line DSL and Markdown parser with hallucination filtering & deduplication
            var lines = cleanPayload.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            AiSlideCardSpec currentCard = null;
            HashSet<string> seenCleanLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (IsMetaInstructionLine(line)) continue;

                if (line.StartsWith("LAYOUT:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.Layout = line.Substring(7).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("STYLE:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("THEME:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.ThemeStyle = line.Substring(line.IndexOf(':') + 1).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("BADGE:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("TAG:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.CategoryBadge = line.Substring(line.IndexOf(':') + 1).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.SlideTitle = line.Substring(6).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("SUBTITLE:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.SlideSubtitle = line.Substring(9).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("FOOTER:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("NOTE:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("AUTHOR:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.FooterNote = line.Substring(line.IndexOf(':') + 1).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("SMARTART:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.SmartArtType = line.Substring(9).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("SMARTART_NODES:", StringComparison.OrdinalIgnoreCase))
                {
                    var nodes = line.Substring(15).Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var n in nodes) spec.SmartArtNodes.Add(n.Trim());
                }
                else if (line.StartsWith("CHART:", StringComparison.OrdinalIgnoreCase))
                {
                    spec.ChartType = line.Substring(6).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("CARD:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("ITEM:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("STEP:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("METRIC:", StringComparison.OrdinalIgnoreCase))
                {
                    currentCard = new AiSlideCardSpec
                    {
                        Title = line.Substring(line.IndexOf(':') + 1).Trim().Trim('"', '\'')
                    };
                    spec.Cards.Add(currentCard);
                }
                else if (line.StartsWith("DESC:", StringComparison.OrdinalIgnoreCase) && currentCard != null)
                {
                    string d = line.Substring(5).Trim().Trim('"', '\'');
                    if (string.IsNullOrEmpty(currentCard.Description))
                        currentCard.Description = d;
                    else
                        currentCard.Description += "\n" + d;
                }
                else if ((line.StartsWith("STAT:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("VALUE:", StringComparison.OrdinalIgnoreCase)) && currentCard != null)
                {
                    currentCard.StatNumber = line.Substring(line.IndexOf(':') + 1).Trim().Trim('"', '\'');
                }
                else if (line.StartsWith("ICON:", StringComparison.OrdinalIgnoreCase) && currentCard != null)
                {
                    currentCard.IconOrBadge = line.Substring(5).Trim().Trim('"', '\'');
                }
                else if (line.IndexOf("[CAPTION]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = line.IndexOf("[CAPTION]", StringComparison.OrdinalIgnoreCase);
                    string t = CleanSlideText(line.Substring(idx + 9));
                    if (!string.IsNullOrEmpty(t)) spec.SlideTitle = t;
                }
                else if (line.IndexOf("[SUBCAPTION]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = line.IndexOf("[SUBCAPTION]", StringComparison.OrdinalIgnoreCase);
                    string t = CleanSlideText(line.Substring(idx + 12));
                    if (!string.IsNullOrEmpty(t)) spec.SlideSubtitle = t;
                }
                else if (line.StartsWith("## Slide", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Slide 1:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Slide 1 -", StringComparison.OrdinalIgnoreCase))
                {
                    string t = CleanSlideText(line.Substring(line.IndexOf(':') >= 0 ? line.IndexOf(':') + 1 : 8).Trim());
                    if (string.IsNullOrEmpty(spec.SlideTitle)) spec.SlideTitle = t;
                }
                else if (line.StartsWith("**[Text:", StringComparison.OrdinalIgnoreCase))
                {
                    string t = CleanSlideText(line.Substring(8).Trim().TrimEnd(']', '*').Trim());
                    if (string.IsNullOrEmpty(spec.SlideTitle)) spec.SlideTitle = t;
                }
                else
                {
                    string cleanLine = CleanSlideText(line);
                    if (!string.IsNullOrEmpty(cleanLine) && !IsMetaInstructionLine(cleanLine) && !seenCleanLines.Contains(cleanLine))
                    {
                        seenCleanLines.Add(cleanLine);
                        if (string.IsNullOrEmpty(spec.SlideTitle))
                        {
                            spec.SlideTitle = cleanLine;
                        }
                        else
                        {
                            if (currentCard == null)
                            {
                                currentCard = new AiSlideCardSpec { Title = "", Description = cleanLine };
                                spec.Cards.Add(currentCard);
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(currentCard.Description))
                                    currentCard.Description = cleanLine;
                                else
                                    currentCard.Description += "\n" + cleanLine;
                            }
                        }
                    }
                }
            }

            // Split title if it contains " - " and subtitle is empty
            if (!string.IsNullOrEmpty(spec.SlideTitle) && string.IsNullOrEmpty(spec.SlideSubtitle))
            {
                var parts = spec.SlideTitle.Split(new[] { " - ", " – ", " — " }, 2, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    spec.SlideTitle = parts[0].Trim();
                    spec.SlideSubtitle = parts[1].Trim();
                }
            }

            // If cards are empty or only contained hallucinated meta descriptions:
            if (spec.Cards.Count == 0 && !string.IsNullOrEmpty(spec.SlideTitle))
            {
                spec.Cards.Add(new AiSlideCardSpec { 
                    Title = "Tầm nhìn & Sứ mệnh", 
                    Description = "• Tiên phong đổi mới và ứng dụng công nghệ hiện đại\n• Mang lại giá trị vững bền và vượt trội cho khách hàng" 
                });
                spec.Cards.Add(new AiSlideCardSpec { 
                    Title = "Giá trị cốt lõi", 
                    Description = "• Đặt chất lượng và uy tín lên hàng đầu\n• Tối ưu hiệu quả hoạt động và đồng hành phát triển" 
                });
            }

            return spec;
        }

        /// <summary>
        /// Tự động liên kết và nâng cấp hiệu ứng Morph giữa 2 slide liên tiếp:
        /// - Đồng bộ MorphName cho tiêu đề và các phần tử tương ứng.
        /// - Áp dụng Kỹ thuật Vật Thể Ngoài Khung Hình (Off-screen Positioning): Tạo bản sao ngoài màn hình
        ///   cho các vật thể mới/cũ để chúng trượt/bay vào hoặc bay ra cực kỳ sống động và mượt mà,
        ///   thay vì chỉ mờ dần (Fade) đơn điệu.
        /// </summary>
        private static void AutoEnhanceMorphTransition(SlideNode prevSlide, SlideNode curSlide)
        {
            if (prevSlide == null || curSlide == null) return;

            HashSet<ShapeNode> usedPrev = new HashSet<ShapeNode>();
            HashSet<ShapeNode> usedCur = new HashSet<ShapeNode>();

            int pairCounter = 1;

            // 1.1 Khớp tiêu đề / Text giống nhau
            for (int i = 0; i < curSlide.Shapes.Count; i++)
            {
                var sCur = curSlide.Shapes[i];
                if (string.IsNullOrWhiteSpace(sCur.Text)) continue;
                for (int j = 0; j < prevSlide.Shapes.Count; j++)
                {
                    var sPrev = prevSlide.Shapes[j];
                    if (usedPrev.Contains(sPrev) || string.IsNullOrWhiteSpace(sPrev.Text)) continue;

                    if (string.Equals(sCur.Text.Trim(), sPrev.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        string mName = "!morph_text_" + pairCounter++;
                        sCur.MorphName = mName;
                        sPrev.MorphName = mName;
                        usedCur.Add(sCur);
                        usedPrev.Add(sPrev);
                        break;
                    }
                }
            }

            // 1.2 Khớp các shape cùng vị trí nửa trên (ví dụ tiêu đề)
            for (int i = 0; i < curSlide.Shapes.Count; i++)
            {
                var sCur = curSlide.Shapes[i];
                if (usedCur.Contains(sCur)) continue;
                for (int j = 0; j < prevSlide.Shapes.Count; j++)
                {
                    var sPrev = prevSlide.Shapes[j];
                    if (usedPrev.Contains(sPrev)) continue;

                    if (sCur.Transform.Y < 250 && sPrev.Transform.Y < 250 && sCur.Type == sPrev.Type)
                    {
                        string mName = "!morph_hdr_" + pairCounter++;
                        sCur.MorphName = mName;
                        sPrev.MorphName = mName;
                        usedCur.Add(sCur);
                        usedPrev.Add(sPrev);
                        break;
                    }
                }
            }

            // 2. KỸ THUẬT VẬT THỂ NGOÀI KHUNG HÌNH (OFF-SCREEN MORPH GLIDE):
            // Dọn sạch các ghost shape cũ nếu có
            curSlide.Shapes.RemoveAll(s => s.Name != null && s.Name.StartsWith("__morph_ghost__"));
            prevSlide.Shapes.RemoveAll(s => s.Name != null && s.Name.StartsWith("__morph_ghost__"));

            // Đối với các shape ở prevSlide mà KHÔNG có trên curSlide:
            // Tạo bản sao trên curSlide nhưng toạ độ CÁCH XA ngoài màn hình để Morph trượt bay ra ngoài!
            List<ShapeNode> ghostShapesCur = new List<ShapeNode>();
            for (int j = 0; j < prevSlide.Shapes.Count; j++)
            {
                var sPrev = prevSlide.Shapes[j];
                if (!usedPrev.Contains(sPrev))
                {
                    string mName = string.IsNullOrEmpty(sPrev.MorphName) ? ("!morph_glide_" + pairCounter++) : sPrev.MorphName;
                    sPrev.MorphName = mName;

                    var ghost = sPrev.Clone();
                    ghost.Id = Guid.NewGuid().ToString();
                    ghost.Name = "__morph_ghost__";
                    ghost.IsVisible = false;
                    ghost.MorphName = mName;

                    float sW = sPrev.Transform.Width > 0 ? sPrev.Transform.Width : 600f;
                    float sH = sPrev.Transform.Height > 0 ? sPrev.Transform.Height : 300f;

                    // Đảm bảo toạ độ cách xa mép canvas tối thiểu 600px để không bao giờ bị lộ vào slide
                    if (sPrev.Transform.Y < 400)
                    {
                        ghost.Transform.Y = -(sH + 600); // Bay vút lên trên
                    }
                    else if (sPrev.Transform.X < 960)
                    {
                        ghost.Transform.X = -(sW + 600); // Bay sang trái
                    }
                    else
                    {
                        ghost.Transform.X = 1920 + 600; // Bay sang phải
                    }

                    ghostShapesCur.Add(ghost);
                }
            }
            curSlide.Shapes.AddRange(ghostShapesCur);

            // Đối với các shape ở curSlide mà KHÔNG có trên prevSlide:
            // Tạo bản sao trên prevSlide nhưng toạ độ CÁCH XA ngoài màn hình để Morph trượt bay vào trong!
            List<ShapeNode> ghostShapesPrev = new List<ShapeNode>();
            for (int i = 0; i < curSlide.Shapes.Count; i++)
            {
                var sCur = curSlide.Shapes[i];
                if (!usedCur.Contains(sCur) && !ghostShapesCur.Contains(sCur))
                {
                    string mName = string.IsNullOrEmpty(sCur.MorphName) ? ("!morph_enter_" + pairCounter++) : sCur.MorphName;
                    sCur.MorphName = mName;

                    var ghost = sCur.Clone();
                    ghost.Id = Guid.NewGuid().ToString();
                    ghost.Name = "__morph_ghost__";
                    ghost.IsVisible = false;
                    ghost.MorphName = mName;

                    float sW = sCur.Transform.Width > 0 ? sCur.Transform.Width : 600f;
                    float sH = sCur.Transform.Height > 0 ? sCur.Transform.Height : 300f;

                    // Xuất phát từ ngoài màn hình bay vào
                    if (sCur.Transform.Y > 500)
                    {
                        ghost.Transform.Y = 1080 + 600; // Bay từ dưới lên
                    }
                    else if (sCur.Transform.X < 960)
                    {
                        ghost.Transform.X = -(sW + 600); // Bay từ trái vào
                    }
                    else
                    {
                        ghost.Transform.X = 1920 + 600; // Bay từ phải vào
                    }

                    ghostShapesPrev.Add(ghost);
                }
            }
            prevSlide.Shapes.AddRange(ghostShapesPrev);
        }

        #endregion
    }
}
