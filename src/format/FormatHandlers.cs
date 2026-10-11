using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    public class DocxMetadata
    {
        public string TextContent { get; set; }
        public List<FormattedRun> FormattedRuns { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public bool ShowPageNumbers { get; set; }
        public string PageNumberFormat { get; set; }
        public int PageNumberStart { get; set; }
        public int FooterAlignment { get; set; } // 0: Left, 1: Center, 2: Right
        public bool DifferentOddEvenPages { get; set; }
        public int FooterEvenAlignment { get; set; } // 0: Left, 1: Center, 2: Right
        public string FooterEvenText { get; set; }
        public int HeaderEvenAlignment { get; set; } // 0: Left, 1: Center, 2: Right
        public string HeaderEvenText { get; set; }
        public string FooterFontName { get; set; }
        public float FooterFontSize { get; set; }
        public Color FooterColor { get; set; }
        public bool FooterBold { get; set; }
        public bool FooterItalic { get; set; }
        public int HeaderAlignment { get; set; }
        public string HeaderFontName { get; set; }
        public float HeaderFontSize { get; set; }
        public Color HeaderColor { get; set; }
        public bool HeaderBold { get; set; }
        public bool HeaderItalic { get; set; }
        public bool IsLandscape { get; set; }
        public int MarginMode { get; set; } // 0: Normal, 1: Narrow, 2: Wide, 3: Custom
        public int TopMarginPt { get; set; }
        public int RightMarginPt { get; set; }
        public int BottomMarginPt { get; set; }
        public int LeftMarginPt { get; set; }
        public int PageWidthPt { get; set; }
        public int PageHeightPt { get; set; }
        public Dictionary<int, int> ParagraphAlignments { get; set; }
        public Dictionary<int, Color> ParagraphBottomBorders { get; set; }
        public Dictionary<int, ParagraphBorderInfo> ParagraphBorders { get; set; }
        public Dictionary<int, float> ParagraphLineSpacings { get; set; }
        public Dictionary<int, float> ParagraphSpacingsBefore { get; set; }
        public Dictionary<int, float> ParagraphSpacingsAfter { get; set; }
        public Dictionary<int, float> ParagraphFirstLineIndents { get; set; }
        public Dictionary<int, float> ParagraphLeftIndents { get; set; }
        public Dictionary<int, float> ParagraphRightIndents { get; set; }
        public Dictionary<int, float> ParagraphHangingIndents { get; set; }
        public List<ZenQOffice.UI.DocTableObject> Tables { get; set; }

        public DocxMetadata()
        {
            Tables = new List<ZenQOffice.UI.DocTableObject>();
            TextContent = "";
            FormattedRuns = new List<FormattedRun>();
            HeaderText = "";
            FooterText = "";
            ShowPageNumbers = false;
            PageNumberFormat = "decimal";
            PageNumberStart = 1;
            FooterAlignment = 1;
            DifferentOddEvenPages = false;
            FooterEvenAlignment = 0;
            FooterEvenText = "";
            HeaderEvenAlignment = 0;
            HeaderEvenText = "";
            FooterFontName = "Times New Roman";
            FooterFontSize = 9.0f;
            FooterColor = Color.Empty;
            FooterBold = false;
            FooterItalic = false;
            HeaderAlignment = 0;
            HeaderFontName = "Times New Roman";
            HeaderFontSize = 9.0f;
            HeaderColor = Color.Empty;
            HeaderBold = false;
            HeaderItalic = false;
            IsLandscape = false;
            MarginMode = 0;
            TopMarginPt = 72;
            RightMarginPt = 72;
            BottomMarginPt = 72;
            LeftMarginPt = 72;
            PageWidthPt = 595;
            PageHeightPt = 842;
            ParagraphAlignments = new Dictionary<int, int>();
            ParagraphBottomBorders = new Dictionary<int, Color>();
            ParagraphBorders = new Dictionary<int, ParagraphBorderInfo>();
            ParagraphLineSpacings = new Dictionary<int, float>();
            ParagraphSpacingsBefore = new Dictionary<int, float>();
            ParagraphSpacingsAfter = new Dictionary<int, float>();
            ParagraphFirstLineIndents = new Dictionary<int, float>();
            ParagraphLeftIndents = new Dictionary<int, float>();
            ParagraphRightIndents = new Dictionary<int, float>();
            ParagraphHangingIndents = new Dictionary<int, float>();
        }
    }

    public class DocxHandler
    {
        public static bool SaveTextFile(string filepath, string text)
        {
            try
            {
                File.WriteAllText(filepath, text ?? "", Encoding.UTF8);
                return true;
            }
            catch { return false; }
        }

        public static bool LoadTextFile(string filepath, out string text)
        {
            text = "";
            try
            {
                if (File.Exists(filepath))
                {
                    text = File.ReadAllText(filepath, Encoding.UTF8);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// Reads any .docx (standard OpenXML ZIP archive) with complete fidelity:
        /// Fonts, sizes, bold, italic, underline, strike, colors, highlights, subscripts, superscripts,
        /// tables, bulleted/numbered lists, images, headers, footers, page numbers and page setup.
        /// </summary>
        public static bool LoadDocxFile(string filepath, out DocxMetadata meta)
        {
            meta = new DocxMetadata();

            if (!File.Exists(filepath)) return false;

            try
            {
                byte[] header = new byte[4];
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Read(header, 0, 4);
                }

                bool isZip = (header[0] == 0x50 && header[1] == 0x4B);

                if (isZip)
                {
                    using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        try { SlideFontManager.ExtractAndRegisterDocxFonts(archive); } catch { }

                        // 1. Extract image media relationships
                        Dictionary<string, string> relsToTarget = new Dictionary<string, string>();
                        ZipArchiveEntry relsEntry = FindZipEntry(archive, "word/_rels/document.xml.rels") ?? archive.GetEntry("word/_rels/document.xml.rels");
                        if (relsEntry != null)
                        {
                            try
                            {
                                XmlDocument relsDoc = new XmlDocument();
                                using (Stream s = relsEntry.Open()) relsDoc.Load(s);
                                foreach (XmlNode rel in relsDoc.GetElementsByTagName("Relationship"))
                                {
                                    if (rel.Attributes["Id"] != null && rel.Attributes["Target"] != null)
                                    {
                                        relsToTarget[rel.Attributes["Id"].Value] = rel.Attributes["Target"].Value;
                                    }
                                }
                            }
                            catch { }
                        }

                        // 2. Extract Header / Footer (supporting odd/even footers & headers)
                        string defaultFooterTarget = null;
                        string evenFooterTarget = null;
                        string defaultHeaderTarget = null;
                        string evenHeaderTarget = null;

                        ZipArchiveEntry docRefEntry = FindZipEntry(archive, "word/document.xml") ?? archive.GetEntry("word/document.xml");
                        if (docRefEntry != null)
                        {
                            try
                            {
                                using (Stream s = docRefEntry.Open())
                                using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                                {
                                    string docXmlPreview = sr.ReadToEnd();
                                    Match mEvenF = Regex.Match(docXmlPreview, @"<w:footerReference[^>]+w:type=""even""[^>]+r:id=""([^""]+)""");
                                    if (!mEvenF.Success) mEvenF = Regex.Match(docXmlPreview, @"<w:footerReference[^>]+r:id=""([^""]+)""[^>]+w:type=""even""");
                                    if (mEvenF.Success && relsToTarget.ContainsKey(mEvenF.Groups[1].Value))
                                    {
                                        evenFooterTarget = relsToTarget[mEvenF.Groups[1].Value];
                                        meta.DifferentOddEvenPages = true;
                                    }

                                    Match mDefF = Regex.Match(docXmlPreview, @"<w:footerReference[^>]+w:type=""default""[^>]+r:id=""([^""]+)""");
                                    if (!mDefF.Success) mDefF = Regex.Match(docXmlPreview, @"<w:footerReference[^>]+r:id=""([^""]+)""[^>]+w:type=""default""");
                                    if (mDefF.Success && relsToTarget.ContainsKey(mDefF.Groups[1].Value))
                                    {
                                        defaultFooterTarget = relsToTarget[mDefF.Groups[1].Value];
                                    }

                                    Match mEvenH = Regex.Match(docXmlPreview, @"<w:headerReference[^>]+w:type=""even""[^>]+r:id=""([^""]+)""");
                                    if (!mEvenH.Success) mEvenH = Regex.Match(docXmlPreview, @"<w:headerReference[^>]+r:id=""([^""]+)""[^>]+w:type=""even""");
                                    if (mEvenH.Success && relsToTarget.ContainsKey(mEvenH.Groups[1].Value))
                                    {
                                        evenHeaderTarget = relsToTarget[mEvenH.Groups[1].Value];
                                        meta.DifferentOddEvenPages = true;
                                    }

                                    Match mDefH = Regex.Match(docXmlPreview, @"<w:headerReference[^>]+w:type=""default""[^>]+r:id=""([^""]+)""");
                                    if (!mDefH.Success) mDefH = Regex.Match(docXmlPreview, @"<w:headerReference[^>]+r:id=""([^""]+)""[^>]+w:type=""default""");
                                    if (mDefH.Success && relsToTarget.ContainsKey(mDefH.Groups[1].Value))
                                    {
                                        defaultHeaderTarget = relsToTarget[mDefH.Groups[1].Value];
                                    }
                                }
                            }
                            catch { }
                        }

                        foreach (var entry in archive.Entries)
                        {
                            string entryFileName = Path.GetFileName(entry.FullName);
                            if (entry.FullName.StartsWith("word/header") && entry.FullName.EndsWith(".xml"))
                            {
                                try
                                {
                                    XmlDocument hDoc = new XmlDocument();
                                    using (Stream s = entry.Open()) hDoc.Load(s);
                                    bool isEven = (evenHeaderTarget != null && entryFileName.Equals(Path.GetFileName(evenHeaderTarget), StringComparison.OrdinalIgnoreCase));
                                    ParseHeaderFooterXml(hDoc, false, meta, isEven);
                                }
                                catch { }
                            }
                            else if (entry.FullName.StartsWith("word/footer") && entry.FullName.EndsWith(".xml"))
                            {
                                try
                                {
                                    XmlDocument fDoc = new XmlDocument();
                                    using (Stream s = entry.Open()) fDoc.Load(s);
                                    bool isEven = (evenFooterTarget != null && entryFileName.Equals(Path.GetFileName(evenFooterTarget), StringComparison.OrdinalIgnoreCase));
                                    ParseHeaderFooterXml(fDoc, true, meta, isEven);
                                }
                                catch { }
                            }
                        }

                        // 3. Extract Main Document XML
                        ZipArchiveEntry docEntry = FindZipEntry(archive, "word/document.xml") ?? archive.GetEntry("word/document.xml") ?? archive.GetEntry("word/document2.xml");
                        if (docEntry != null)
                        {
                            using (Stream s = docEntry.Open())
                            using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                            {
                                string xmlContent = sr.ReadToEnd();
                                return ParseDocumentXmlFull(xmlContent, archive, relsToTarget, meta);
                            }
                        }
                    }
                }

                // Fallback: If not ZIP or plain XML / text
                string rawText = File.ReadAllText(filepath, Encoding.UTF8);
                if (rawText.Contains("<w:document") || rawText.Contains("<w:body"))
                {
                    return ParseDocumentXmlFull(rawText, null, null, meta);
                }

                meta.TextContent = rawText;
                meta.FormattedRuns.Add(new FormattedRun(0, meta.TextContent.Length, "Times New Roman", 11.0f, false, false, false, false, Color.Black, Color.Transparent));
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    meta.TextContent = File.ReadAllText(filepath, Encoding.UTF8);
                    meta.FormattedRuns.Add(new FormattedRun(0, meta.TextContent.Length, "Times New Roman", 11.0f, false, false, false, false, Color.Black, Color.Transparent));
                    return true;
                }
                catch
                {
                    meta.TextContent = "Lỗi khi đọc tệp: " + ex.Message;
                    return false;
                }
            }
        }

        public static bool LoadDocxFile(string filepath, out string textContent, out List<FormattedRun> runs)
        {
            DocxMetadata meta;
            bool ok = LoadDocxFile(filepath, out meta);
            textContent = meta.TextContent;
            runs = meta.FormattedRuns;
            return ok;
        }

        private static string ExtractAllTextFromXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlNodeList tNodes = doc.GetElementsByTagName("w:t");
            if (tNodes != null)
            {
                foreach (XmlNode t in tNodes) sb.Append(t.InnerText + " ");
            }
            return sb.ToString().Trim();
        }

        private static void ParseHeaderFooterXml(XmlDocument doc, bool isFooter, DocxMetadata meta, bool isEven = false)
        {
            if (doc == null || doc.DocumentElement == null) return;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            nsmgr.AddNamespace("wp", "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing");

            // 1. Paragraph alignment
            int align = -1;
            XmlNode jcNode = doc.SelectSingleNode("//w:pPr/w:jc", nsmgr);
            if (jcNode != null && jcNode.Attributes["w:val"] != null)
            {
                string jcVal = jcNode.Attributes["w:val"].Value.ToLowerInvariant();
                if (jcVal == "center") align = 1;
                else if (jcVal == "right") align = 2;
                else if (jcVal == "left") align = 0;
            }

            // 1.1 Textbox DrawingML absolute horizontal position
            if (align == -1)
            {
                XmlNode posOffsetNode = doc.SelectSingleNode("//wp:positionH/wp:posOffset", nsmgr);
                if (posOffsetNode == null)
                {
                    posOffsetNode = doc.SelectSingleNode("//*[local-name()='positionH']/*[local-name()='posOffset']");
                }
                if (posOffsetNode != null)
                {
                    long posVal;
                    if (long.TryParse(posOffsetNode.InnerText.Trim(), out posVal))
                    {
                        // 1 inch = 914,400 EMUs. A4 width = 7,560,000 EMUs.
                        if (posVal > 4000000) align = 2; // Right
                        else if (posVal < 2000000) align = 0; // Left
                        else align = 1; // Center
                    }
                }
            }

            // 1.2 VML shape margin-left style
            if (align == -1)
            {
                XmlNode shapeNode = doc.SelectSingleNode("//*[local-name()='shape'][@style]");
                if (shapeNode != null && shapeNode.Attributes["style"] != null)
                {
                    string style = shapeNode.Attributes["style"].Value;
                    Match m = Regex.Match(style, @"margin-left\s*:\s*([0-9.]+)\s*pt", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        float ml;
                        if (float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out ml))
                        {
                            if (ml > 300) align = 2; // Right
                            else if (ml < 150) align = 0; // Left
                            else align = 1; // Center
                        }
                    }
                }
            }

            // 1.3 Tab characters before PAGE field
            if (align == -1)
            {
                XmlNode pageFld = doc.SelectSingleNode("//w:fldSimple[contains(@w:instr, 'PAGE')]", nsmgr);
                if (pageFld == null) pageFld = doc.SelectSingleNode("//w:instrText[contains(text(), 'PAGE')]", nsmgr);
                if (pageFld != null)
                {
                    XmlNode parentP = pageFld.SelectSingleNode("ancestor::w:p[1]", nsmgr);
                    if (parentP != null)
                    {
                        int tabsBeforePage = 0;
                        foreach (XmlNode pChild in parentP.ChildNodes)
                        {
                            if (pChild.Name == "w:r")
                            {
                                if (pChild.SelectSingleNode("w:tab", nsmgr) != null) tabsBeforePage++;
                                if (pChild.SelectSingleNode(".//w:instrText[contains(text(), 'PAGE')]", nsmgr) != null) break;
                            }
                            else if (pChild == pageFld) break;
                        }
                        if (tabsBeforePage >= 2) align = 2; // Right
                        else if (tabsBeforePage == 1) align = 1; // Center
                    }
                }
            }

            if (align == -1)
            {
                align = isFooter ? 1 : 0;
            }

            if (isFooter)
            {
                if (isEven) meta.FooterEvenAlignment = align;
                else meta.FooterAlignment = align;
            }
            else
            {
                if (isEven) meta.HeaderEvenAlignment = align;
                else meta.HeaderAlignment = align;
            }

            // 2. Run properties (Font, Size, Color, Bold, Italic)
            XmlNode rFonts = doc.SelectSingleNode("//w:rPr/w:rFonts", nsmgr);
            if (rFonts != null)
            {
                string fn = null;
                if (rFonts.Attributes["w:ascii"] != null) fn = rFonts.Attributes["w:ascii"].Value;
                else if (rFonts.Attributes["w:hAnsi"] != null) fn = rFonts.Attributes["w:hAnsi"].Value;
                if (!string.IsNullOrEmpty(fn))
                {
                    if (isFooter) meta.FooterFontName = fn;
                    else meta.HeaderFontName = fn;
                }
            }

            XmlNode szNode = doc.SelectSingleNode("//w:rPr/w:sz", nsmgr);
            if (szNode != null && szNode.Attributes["w:val"] != null)
            {
                float szHalf;
                if (float.TryParse(szNode.Attributes["w:val"].Value, out szHalf) && szHalf > 0)
                {
                    if (isFooter) meta.FooterFontSize = szHalf / 2.0f;
                    else meta.HeaderFontSize = szHalf / 2.0f;
                }
            }

            XmlNode colorNode = doc.SelectSingleNode("//w:rPr/w:color", nsmgr);
            if (colorNode != null && colorNode.Attributes["w:val"] != null)
            {
                string colHex = colorNode.Attributes["w:val"].Value;
                if (!string.IsNullOrEmpty(colHex) && colHex != "auto" && colHex.Length >= 6)
                {
                    try
                    {
                        int r = Convert.ToInt32(colHex.Substring(0, 2), 16);
                        int g = Convert.ToInt32(colHex.Substring(2, 2), 16);
                        int b = Convert.ToInt32(colHex.Substring(4, 2), 16);
                        Color c = Color.FromArgb(r, g, b);
                        if (isFooter) meta.FooterColor = c;
                        else meta.HeaderColor = c;
                    }
                    catch { }
                }
            }

            XmlNode bNode = doc.SelectSingleNode("//w:rPr/w:b", nsmgr);
            if (bNode != null)
            {
                bool isB = true;
                if (bNode.Attributes["w:val"] != null && (bNode.Attributes["w:val"].Value == "0" || bNode.Attributes["w:val"].Value == "false")) isB = false;
                if (isFooter) meta.FooterBold = isB;
                else meta.HeaderBold = isB;
            }

            XmlNode iNode = doc.SelectSingleNode("//w:rPr/w:i", nsmgr);
            if (iNode != null)
            {
                bool isI = true;
                if (iNode.Attributes["w:val"] != null && (iNode.Attributes["w:val"].Value == "0" || iNode.Attributes["w:val"].Value == "false")) isI = false;
                if (isFooter) meta.FooterItalic = isI;
                else meta.HeaderItalic = isI;
            }

            // 3. Text & Dynamic Fields (PAGE, NUMPAGES)
            bool hasPageField = false;
            StringBuilder sb = new StringBuilder();

            XmlNodeList pNodes = doc.SelectNodes("//w:p", nsmgr);
            if (pNodes != null && pNodes.Count > 0)
            {
                foreach (XmlNode p in pNodes)
                {
                    bool insidePageField = false;
                    bool insideNumPagesField = false;

                    foreach (XmlNode child in p.ChildNodes)
                    {
                        if (child.Name == "w:fldSimple")
                        {
                            string instr = child.Attributes["w:instr"] != null ? child.Attributes["w:instr"].Value.ToUpperInvariant() : "";
                            if (instr.Contains("NUMPAGES"))
                            {
                                sb.Append("{NUMPAGES}");
                            }
                            else if (instr.Contains("PAGE"))
                            {
                                hasPageField = true;
                                sb.Append("{PAGE}");
                            }
                        }
                        else if (child.Name == "w:r")
                        {
                            XmlNode instrText = child.SelectSingleNode("w:instrText", nsmgr);
                            if (instrText != null)
                            {
                                string itxt = instrText.InnerText.ToUpperInvariant();
                                if (itxt.Contains("NUMPAGES"))
                                {
                                    insideNumPagesField = true;
                                    sb.Append("{NUMPAGES}");
                                }
                                else if (itxt.Contains("PAGE"))
                                {
                                    insidePageField = true;
                                    hasPageField = true;
                                    sb.Append("{PAGE}");
                                }
                            }

                            XmlNode fldChar = child.SelectSingleNode("w:fldChar", nsmgr);
                            if (fldChar != null && fldChar.Attributes["w:fldCharType"] != null)
                            {
                                string fType = fldChar.Attributes["w:fldCharType"].Value.ToLowerInvariant();
                                if (fType == "end")
                                {
                                    insidePageField = false;
                                    insideNumPagesField = false;
                                }
                            }

                            if (!insidePageField && !insideNumPagesField)
                            {
                                XmlNodeList tList = child.SelectNodes("w:t", nsmgr);
                                if (tList != null)
                                {
                                    foreach (XmlNode t in tList) sb.Append(t.InnerText);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                sb.Append(ExtractAllTextFromXml(doc));
            }

            string text = sb.ToString().Trim();

            if (doc.InnerXml.Contains("PAGE") || doc.InnerXml.Contains("w:fldSimple"))
            {
                hasPageField = true;
            }

            if (hasPageField)
            {
                if (Regex.IsMatch(text, @"Trang\s+\d+\s*/\s*\d+", RegexOptions.IgnoreCase))
                {
                    text = Regex.Replace(text, @"Trang\s+\d+\s*/\s*\d+", "Trang {PAGE} / {NUMPAGES}", RegexOptions.IgnoreCase);
                }
                else if (Regex.IsMatch(text, @"Trang\s+\d+", RegexOptions.IgnoreCase) && !text.Contains("{PAGE}"))
                {
                    text = Regex.Replace(text, @"Trang\s+\d+", "Trang {PAGE}", RegexOptions.IgnoreCase);
                }
                else if (Regex.IsMatch(text, @"Page\s+\d+\s+of\s+\d+", RegexOptions.IgnoreCase))
                {
                    text = Regex.Replace(text, @"Page\s+\d+\s+of\s+\d+", "Page {PAGE} of {NUMPAGES}", RegexOptions.IgnoreCase);
                }
                else if (Regex.IsMatch(text, @"Page\s+\d+\s*/\s*\d+", RegexOptions.IgnoreCase))
                {
                    text = Regex.Replace(text, @"Page\s+\d+\s*/\s*\d+", "Page {PAGE} / {NUMPAGES}", RegexOptions.IgnoreCase);
                }
                else if (Regex.IsMatch(text, @"Page\s+\d+", RegexOptions.IgnoreCase) && !text.Contains("{PAGE}"))
                {
                    text = Regex.Replace(text, @"Page\s+\d+", "Page {PAGE}", RegexOptions.IgnoreCase);
                }

                if (text.IndexOf("{PAGE}") != text.LastIndexOf("{PAGE}") && text.EndsWith("{PAGE}"))
                {
                    int lastIdx = text.LastIndexOf("{PAGE}");
                    text = text.Substring(0, lastIdx).Trim();
                }

                if (!text.Contains("{PAGE}"))
                {
                    if (string.IsNullOrEmpty(text)) text = "{PAGE}";
                    else text = text + "  {PAGE}";
                }
            }

            if (isFooter)
            {
                if (isEven)
                {
                    meta.FooterEvenText = text;
                }
                else
                {
                    meta.FooterText = text;
                    meta.ShowPageNumbers = hasPageField;
                }
            }
            else
            {
                if (isEven)
                {
                    meta.HeaderEvenText = text;
                }
                else
                {
                    meta.HeaderText = text;
                }
            }
        }
        private class DocxStyleInfo
        {
            public string StyleId;
            public string Name;
            public string Type;
            public string BasedOn;
            public string FontName;
            public float FontSize;
            public bool? Bold;
            public bool? Italic;
            public bool? Underline;
            public Color? Color;
            public int? Alignment;
            public float? LineSpacing;
            public float? SpacingBefore;
            public float? SpacingAfter;
            public float? IndentFirstLine;
            public float? IndentHanging;
            public float? IndentLeft;
            public float? IndentRight;
            public bool? HasBottomBorder;
            public Color? BottomBorderColor;
            public float? CharWidthScale;
            public float? CharSpacingPt;

            public DocxStyleInfo()
            {
                FontSize = 0f;
            }
        }

        private static float ApplyOpenXmlCharacterScaling(float fontSize, float widthScale, float charSpacingPt)
        {
            if (fontSize <= 0f) return 11.0f;
            float adjusted = fontSize;
            if (widthScale > 0.1f && Math.Abs(widthScale - 1.0f) > 0.02f)
            {
                float scale = (widthScale < 1.0f)
                    ? (0.20f + 0.80f * widthScale)
                    : (1.0f + 0.50f * (widthScale - 1.0f));
                adjusted *= Math.Max(0.65f, Math.Min(1.35f, scale));
            }
            if (charSpacingPt < -0.2f)
            {
                float spcFactor = Math.Max(0.88f, 1.0f + (charSpacingPt / Math.Max(8.0f, adjusted)) * 0.45f);
                adjusted *= spcFactor;
            }
            adjusted = (float)Math.Round(adjusted * 4.0f) / 4.0f;
            return Math.Max(7.5f, adjusted);
        }

        private static string ToRoman(int number)
        {
            if (number <= 0) return number.ToString();
            int[] values = new int[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] numerals = new string[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                while (number >= values[i])
                {
                    sb.Append(numerals[i]);
                    number -= values[i];
                }
            }
            return sb.ToString();
        }

        private static string ToLetter(int number)
        {
            if (number <= 0) return "";
            string result = "";
            while (number > 0)
            {
                number--;
                result = (char)('a' + (number % 26)) + result;
                number /= 26;
            }
            return result;
        }

        private static void ApplyIndentFromNode(XmlNode indNode, ref float? firstLine, ref float? hanging, ref float? left, ref float? right)
        {
            if (indNode == null) return;
            string leftStr = GetWAttr(indNode, "left");
            if (!string.IsNullOrEmpty(leftStr))
            {
                int val;
                if (int.TryParse(leftStr, out val)) left = val / 20.0f;
            }
            string rightStr = GetWAttr(indNode, "right");
            if (!string.IsNullOrEmpty(rightStr))
            {
                int val;
                if (int.TryParse(rightStr, out val)) right = val / 20.0f;
            }
            string hangStr = GetWAttr(indNode, "hanging");
            string flStr = GetWAttr(indNode, "firstLine");
            if (!string.IsNullOrEmpty(hangStr))
            {
                int val;
                if (int.TryParse(hangStr, out val))
                {
                    hanging = val / 20.0f;
                    firstLine = null;
                }
            }
            else if (!string.IsNullOrEmpty(flStr))
            {
                int val;
                if (int.TryParse(flStr, out val))
                {
                    firstLine = val / 20.0f;
                    hanging = null;
                }
            }
        }

        private class DocxNumLvlInfo
        {
            public int Start = 1;
            public string NumFmt = "decimal";
            public string LvlText = "%1.";
            public float? IndentLeft;
            public float? IndentHanging;
            public float? IndentFirstLine;
            public float? IndentRight;
            public string FontName;
            public float FontSize;
            public bool? Bold;
            public bool? Italic;
            public Color? Color;
        }

        private class DocxNumberingDef
        {
            public Dictionary<int, DocxNumLvlInfo> Levels = new Dictionary<int, DocxNumLvlInfo>();
        }

        private static Dictionary<string, DocxNumberingDef> ParseNumberingXml(ZipArchive archive, XmlNamespaceManager nsmgr)
        {
            var result = new Dictionary<string, DocxNumberingDef>();
            if (archive == null) return result;
            ZipArchiveEntry entry = archive.GetEntry("word/numbering.xml") ?? FindZipEntry(archive, "word/numbering.xml");
            if (entry == null) return result;

            try
            {
                XmlDocument numDoc = new XmlDocument();
                using (Stream s = entry.Open()) numDoc.Load(s);

                var abstractMap = new Dictionary<string, DocxNumberingDef>();
                XmlNodeList absList = numDoc.SelectNodes("//w:abstractNum", nsmgr);
                if (absList != null)
                {
                    foreach (XmlNode ab in absList)
                    {
                        var abIdAttr = ab.Attributes["w:abstractNumId"];
                        if (abIdAttr == null) continue;
                        string abId = abIdAttr.Value;
                        var def = new DocxNumberingDef();

                        XmlNodeList lvls = ab.SelectNodes("w:lvl", nsmgr);
                        if (lvls != null)
                        {
                            foreach (XmlNode lvl in lvls)
                            {
                                var ilvlAttr = lvl.Attributes["w:ilvl"];
                                if (ilvlAttr == null) continue;
                                int ilvl;
                                if (!int.TryParse(ilvlAttr.Value, out ilvl)) continue;

                                var lInfo = new DocxNumLvlInfo();
                                var startNode = lvl.SelectSingleNode("w:start", nsmgr);
                                if (startNode != null && startNode.Attributes["w:val"] != null)
                                {
                                    int.TryParse(startNode.Attributes["w:val"].Value, out lInfo.Start);
                                }
                                var fmtNode = lvl.SelectSingleNode("w:numFmt", nsmgr);
                                if (fmtNode != null && fmtNode.Attributes["w:val"] != null)
                                {
                                    lInfo.NumFmt = fmtNode.Attributes["w:val"].Value;
                                }
                                var txtNode = lvl.SelectSingleNode("w:lvlText", nsmgr);
                                if (txtNode != null && txtNode.Attributes["w:val"] != null)
                                {
                                    lInfo.LvlText = txtNode.Attributes["w:val"].Value;
                                }

                                var indNode = lvl.SelectSingleNode("w:pPr/w:ind", nsmgr);
                                if (indNode != null)
                                {
                                    ApplyIndentFromNode(indNode, ref lInfo.IndentFirstLine, ref lInfo.IndentHanging, ref lInfo.IndentLeft, ref lInfo.IndentRight);
                                }

                                var rPrNode = lvl.SelectSingleNode("w:rPr", nsmgr);
                                if (rPrNode != null)
                                {
                                    var rF = rPrNode.SelectSingleNode("w:rFonts", nsmgr);
                                    if (rF != null)
                                    {
                                        if (rF.Attributes["w:ascii"] != null) lInfo.FontName = rF.Attributes["w:ascii"].Value;
                                        else if (rF.Attributes["w:hAnsi"] != null) lInfo.FontName = rF.Attributes["w:hAnsi"].Value;
                                        else if (rF.Attributes["w:cs"] != null) lInfo.FontName = rF.Attributes["w:cs"].Value;
                                    }
                                    var szNode = rPrNode.SelectSingleNode("w:sz", nsmgr) ?? rPrNode.SelectSingleNode("w:szCs", nsmgr);
                                    if (szNode != null && szNode.Attributes["w:val"] != null)
                                    {
                                        int hp;
                                        if (int.TryParse(szNode.Attributes["w:val"].Value, out hp) && hp > 0) lInfo.FontSize = hp / 2.0f;
                                    }
                                    var bNode = rPrNode.SelectSingleNode("w:b", nsmgr);
                                    if (bNode != null)
                                    {
                                        var bv = bNode.Attributes["w:val"];
                                        lInfo.Bold = (bv == null || (bv.Value != "0" && bv.Value.ToLower() != "false"));
                                    }
                                    var iNode = rPrNode.SelectSingleNode("w:i", nsmgr);
                                    if (iNode != null)
                                    {
                                        var iv = iNode.Attributes["w:val"];
                                        lInfo.Italic = (iv == null || (iv.Value != "0" && iv.Value.ToLower() != "false"));
                                    }
                                    var cNode = rPrNode.SelectSingleNode("w:color", nsmgr);
                                    if (cNode != null && cNode.Attributes["w:val"] != null)
                                    {
                                        string hex = cNode.Attributes["w:val"].Value;
                                        if (hex.Length == 6)
                                        {
                                            try { lInfo.Color = Color.FromArgb(Convert.ToInt32(hex.Substring(0, 2), 16), Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16)); } catch { }
                                        }
                                    }
                                }

                                def.Levels[ilvl] = lInfo;
                            }
                        }
                        abstractMap[abId] = def;
                    }
                }

                XmlNodeList numList = numDoc.SelectNodes("//w:num", nsmgr);
                if (numList != null)
                {
                    foreach (XmlNode num in numList)
                    {
                        var nIdAttr = num.Attributes["w:numId"];
                        if (nIdAttr == null) continue;
                        string numId = nIdAttr.Value;
                        var abRef = num.SelectSingleNode("w:abstractNumId", nsmgr);
                        if (abRef != null && abRef.Attributes["w:val"] != null)
                        {
                            string abId = abRef.Attributes["w:val"].Value;
                            if (abstractMap.ContainsKey(abId))
                            {
                                result[numId] = abstractMap[abId];
                            }
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        private static string GetWAttr(XmlNode node, string name)
        {
            if (node == null || node.Attributes == null) return null;
            XmlAttribute attr = node.Attributes["w:" + name] ?? node.Attributes[name];
            return attr != null ? attr.Value : null;
        }

        private static void ApplySpacingFromNode(XmlNode sp, DocxStyleInfo sInfo)
        {
            if (sp == null || sInfo == null) return;
            string lineStr = GetWAttr(sp, "line");
            if (!string.IsNullOrEmpty(lineStr))
            {
                int lVal;
                if (int.TryParse(lineStr, out lVal))
                    sInfo.LineSpacing = (float)Math.Round((double)lVal / 240.0, 2);
            }
            string beforeStr = GetWAttr(sp, "before");
            if (!string.IsNullOrEmpty(beforeStr))
            {
                int bVal;
                if (int.TryParse(beforeStr, out bVal))
                    sInfo.SpacingBefore = (float)Math.Round((double)bVal / 20.0, 1);
            }
            string afterStr = GetWAttr(sp, "after");
            if (!string.IsNullOrEmpty(afterStr))
            {
                int aVal;
                if (int.TryParse(afterStr, out aVal))
                    sInfo.SpacingAfter = (float)Math.Round((double)aVal / 20.0, 1);
            }
        }

        private static void ApplySpacingToParagraphMaps(XmlNode spNode, int paraStartIndex, Dictionary<int, float> paraLineSpacings, Dictionary<int, float> paraSpacingsBefore, Dictionary<int, float> paraSpacingsAfter)
        {
            if (spNode == null) return;
            string lineStr = GetWAttr(spNode, "line");
            if (!string.IsNullOrEmpty(lineStr) && paraLineSpacings != null)
            {
                int lineVal;
                if (int.TryParse(lineStr, out lineVal))
                {
                    paraLineSpacings[paraStartIndex] = (float)Math.Round((double)lineVal / 240.0, 2);
                }
            }
            string beforeStr = GetWAttr(spNode, "before");
            if (!string.IsNullOrEmpty(beforeStr) && paraSpacingsBefore != null)
            {
                int beforeVal;
                if (int.TryParse(beforeStr, out beforeVal))
                {
                    paraSpacingsBefore[paraStartIndex] = (float)Math.Round((double)beforeVal / 20.0, 1);
                }
            }
            string afterStr = GetWAttr(spNode, "after");
            if (!string.IsNullOrEmpty(afterStr) && paraSpacingsAfter != null)
            {
                int afterVal;
                if (int.TryParse(afterStr, out afterVal))
                {
                    paraSpacingsAfter[paraStartIndex] = (float)Math.Round((double)afterVal / 20.0, 1);
                }
            }
        }

        private static bool ParseDocumentXmlFull(string xmlContent, ZipArchive archive, Dictionary<string, string> relsToTarget, DocxMetadata meta)
        {
            StringBuilder sb = new StringBuilder();
            meta.FormattedRuns = new List<FormattedRun>();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                nsmgr.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                nsmgr.AddNamespace("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
                nsmgr.AddNamespace("wp", "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing");
                nsmgr.AddNamespace("pic", "http://schemas.openxmlformats.org/drawingml/2006/picture");
                nsmgr.AddNamespace("v", "urn:schemas-microsoft-com:vml");
                nsmgr.AddNamespace("o", "urn:schemas-microsoft-com:office:office");
                nsmgr.AddNamespace("m", "http://schemas.openxmlformats.org/officeDocument/2006/math");
                nsmgr.AddNamespace("wps", "http://schemas.microsoft.com/office/word/2010/wordprocessingShape");
                nsmgr.AddNamespace("wpg", "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup");
                nsmgr.AddNamespace("wp14", "http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing");
                nsmgr.AddNamespace("dgm", "http://schemas.openxmlformats.org/drawingml/2006/diagram");
                nsmgr.AddNamespace("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");

                XmlNode bodyNode = doc.SelectSingleNode("//w:body", nsmgr) ?? doc.DocumentElement;
                if (bodyNode == null) return false;

                // 1. Parse Section Properties (Page setup, Landscape, Margins)
                XmlNode sectPr = bodyNode.SelectSingleNode(".//w:sectPr", nsmgr);
                if (sectPr != null)
                {
                    XmlNode pgSz = sectPr.SelectSingleNode("w:pgSz", nsmgr);
                    if (pgSz != null)
                    {
                        if (GetWAttr(pgSz, "orient") == "landscape")
                        {
                            meta.IsLandscape = true;
                        }
                        int pgWTwips = 0, pgHTwips = 0;
                        int.TryParse(GetWAttr(pgSz, "w") ?? "", out pgWTwips);
                        int.TryParse(GetWAttr(pgSz, "h") ?? "", out pgHTwips);
                        if (pgWTwips > 0) meta.PageWidthPt = (int)(pgWTwips / 20f);
                        if (pgHTwips > 0) meta.PageHeightPt = (int)(pgHTwips / 20f);
                    }

                    XmlNode pgMar = sectPr.SelectSingleNode("w:pgMar", nsmgr);
                    if (pgMar != null)
                    {
                        int topTwips = 1440, rightTwips = 1440, bottomTwips = 1440, leftTwips = 1440;
                        int.TryParse(GetWAttr(pgMar, "top") ?? "1440", out topTwips);
                        int.TryParse(GetWAttr(pgMar, "right") ?? "1440", out rightTwips);
                        int.TryParse(GetWAttr(pgMar, "bottom") ?? "1440", out bottomTwips);
                        int.TryParse(GetWAttr(pgMar, "left") ?? "1440", out leftTwips);

                        meta.TopMarginPt = (int)(topTwips / 20f);
                        meta.RightMarginPt = (int)(rightTwips / 20f);
                        meta.BottomMarginPt = (int)(bottomTwips / 20f);
                        meta.LeftMarginPt = (int)(leftTwips / 20f);

                        if (Math.Abs(topTwips - 720) <= 40 && Math.Abs(rightTwips - 720) <= 40 && Math.Abs(leftTwips - 720) <= 40) meta.MarginMode = 1; // Narrow
                        else if (Math.Abs(topTwips - 1440) <= 40 && Math.Abs(rightTwips - 1440) <= 40 && Math.Abs(leftTwips - 1440) <= 40) meta.MarginMode = 0; // Normal
                        else if (Math.Abs(rightTwips - 2880) <= 80) meta.MarginMode = 3; // Wide
                        else meta.MarginMode = 4; // Custom
                    }

                    XmlNode pgNumType = sectPr.SelectSingleNode("w:pgNumType", nsmgr);
                    if (pgNumType != null)
                    {
                        string fmt = GetWAttr(pgNumType, "fmt");
                        if (!string.IsNullOrEmpty(fmt)) meta.PageNumberFormat = fmt;

                        string startVal = GetWAttr(pgNumType, "start");
                        int startNum;
                        if (!string.IsNullOrEmpty(startVal) && int.TryParse(startVal, out startNum))
                        {
                            meta.PageNumberStart = startNum;
                        }
                    }
                }

                Dictionary<string, DocxStyleInfo> stylesDict = new Dictionary<string, DocxStyleInfo>(StringComparer.OrdinalIgnoreCase);
                string docDefaultFont = "Times New Roman";
                float docDefaultFontSize = 11.0f;

                if (archive != null)
                {
                    ZipArchiveEntry stylesEntry = archive.GetEntry("word/styles.xml");
                    if (stylesEntry != null)
                    {
                        try
                        {
                            XmlDocument sDoc = new XmlDocument();
                            using (Stream s = stylesEntry.Open()) sDoc.Load(s);

                            // 1. DocDefaults
                            XmlNode defRPr = sDoc.SelectSingleNode("//w:docDefaults/w:rPrDefault/w:rPr", nsmgr)
                                          ?? sDoc.SelectSingleNode("//w:docDefaults//w:rPr", nsmgr);
                            if (defRPr != null)
                            {
                                XmlNode rF = defRPr.SelectSingleNode("w:rFonts", nsmgr);
                                if (rF != null)
                                {
                                    if (rF.Attributes["w:ascii"] != null && !string.IsNullOrEmpty(rF.Attributes["w:ascii"].Value)) docDefaultFont = rF.Attributes["w:ascii"].Value;
                                    else if (rF.Attributes["w:hAnsi"] != null && !string.IsNullOrEmpty(rF.Attributes["w:hAnsi"].Value)) docDefaultFont = rF.Attributes["w:hAnsi"].Value;
                                    else if (rF.Attributes["w:cs"] != null && !string.IsNullOrEmpty(rF.Attributes["w:cs"].Value)) docDefaultFont = rF.Attributes["w:cs"].Value;
                                }
                                XmlNode sz = defRPr.SelectSingleNode("w:sz", nsmgr) ?? defRPr.SelectSingleNode("w:szCs", nsmgr);
                                if (sz != null && sz.Attributes["w:val"] != null)
                                {
                                    int halfPts;
                                    if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                                    {
                                        docDefaultFontSize = halfPts / 2.0f;
                                    }
                                }
                            }

                            // 1b. Paragraph docDefaults (spacing) — Word uses these when a paragraph has no explicit spacing
                            XmlNode defPPr = sDoc.SelectSingleNode("//w:docDefaults/w:pPrDefault/w:pPr", nsmgr)
                                          ?? sDoc.SelectSingleNode("//w:docDefaults//w:pPr", nsmgr);
                            if (defPPr != null)
                            {
                                DocxStyleInfo defPara = new DocxStyleInfo();
                                ApplySpacingFromNode(defPPr.SelectSingleNode("w:spacing", nsmgr), defPara);
                                stylesDict["__docDefaults"] = defPara;
                            }

                            string defaultParaStyleId = null;

                            // 2. Parse all <w:style> definitions
                            XmlNodeList styleNodes = sDoc.SelectNodes("//w:style", nsmgr);
                            if (styleNodes != null)
                            {
                                foreach (XmlNode sNode in styleNodes)
                                {
                                    if (sNode.Attributes["w:styleId"] == null) continue;
                                    string sId = sNode.Attributes["w:styleId"].Value;
                                    string sType = sNode.Attributes["w:type"] != null ? sNode.Attributes["w:type"].Value : "paragraph";

                                    if (sType == "paragraph" && sNode.Attributes["w:default"] != null)
                                    {
                                        string defVal = sNode.Attributes["w:default"].Value.ToLower();
                                        if (defVal == "1" || defVal == "true") defaultParaStyleId = sId;
                                    }

                                    DocxStyleInfo sInfo = new DocxStyleInfo();
                                    sInfo.StyleId = sId;
                                    sInfo.Type = sType;

                                    XmlNode nameNode = sNode.SelectSingleNode("w:name", nsmgr);
                                    if (nameNode != null && nameNode.Attributes["w:val"] != null) sInfo.Name = nameNode.Attributes["w:val"].Value;

                                    XmlNode basedOnNode = sNode.SelectSingleNode("w:basedOn", nsmgr);
                                    if (basedOnNode != null && basedOnNode.Attributes["w:val"] != null) sInfo.BasedOn = basedOnNode.Attributes["w:val"].Value;

                                    // Run properties inside style (<w:rPr>)
                                    XmlNode rPr = sNode.SelectSingleNode("w:rPr", nsmgr);
                                    if (rPr != null)
                                    {
                                        XmlNode rF = rPr.SelectSingleNode("w:rFonts", nsmgr);
                                        if (rF != null)
                                        {
                                            if (rF.Attributes["w:ascii"] != null && !string.IsNullOrEmpty(rF.Attributes["w:ascii"].Value)) sInfo.FontName = rF.Attributes["w:ascii"].Value;
                                            else if (rF.Attributes["w:hAnsi"] != null && !string.IsNullOrEmpty(rF.Attributes["w:hAnsi"].Value)) sInfo.FontName = rF.Attributes["w:hAnsi"].Value;
                                            else if (rF.Attributes["w:cs"] != null && !string.IsNullOrEmpty(rF.Attributes["w:cs"].Value)) sInfo.FontName = rF.Attributes["w:cs"].Value;
                                        }
                                        XmlNode sz = rPr.SelectSingleNode("w:sz", nsmgr) ?? rPr.SelectSingleNode("w:szCs", nsmgr);
                                        if (sz != null && sz.Attributes["w:val"] != null)
                                        {
                                            int halfPts;
                                            if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                                            {
                                                sInfo.FontSize = halfPts / 2.0f;
                                            }
                                        }
                                        XmlNode wNode = rPr.SelectSingleNode("w:w", nsmgr);
                                        if (wNode != null && wNode.Attributes["w:val"] != null)
                                        {
                                            int wPct;
                                            if (int.TryParse(wNode.Attributes["w:val"].Value, out wPct) && wPct > 10 && wPct <= 200)
                                            {
                                                sInfo.CharWidthScale = wPct / 100.0f;
                                            }
                                        }
                                        XmlNode spcNode = rPr.SelectSingleNode("w:spacing", nsmgr);
                                        if (spcNode != null && spcNode.Attributes["w:val"] != null)
                                        {
                                            int spcTwips;
                                            if (int.TryParse(spcNode.Attributes["w:val"].Value, out spcTwips))
                                            {
                                                sInfo.CharSpacingPt = spcTwips / 20.0f;
                                            }
                                        }
                                        XmlNode b = rPr.SelectSingleNode("w:b", nsmgr);
                                        if (b != null)
                                        {
                                            var v = b.Attributes["w:val"];
                                            sInfo.Bold = (v == null || (v.Value != "0" && v.Value.ToLower() != "false"));
                                        }
                                        XmlNode i = rPr.SelectSingleNode("w:i", nsmgr);
                                        if (i != null)
                                        {
                                            var v = i.Attributes["w:val"];
                                            sInfo.Italic = (v == null || (v.Value != "0" && v.Value.ToLower() != "false"));
                                        }
                                        XmlNode u = rPr.SelectSingleNode("w:u", nsmgr);
                                        if (u != null)
                                        {
                                            var v = u.Attributes["w:val"];
                                            sInfo.Underline = (v == null || (v.Value != "0" && v.Value != "none"));
                                        }
                                        XmlNode col = rPr.SelectSingleNode("w:color", nsmgr);
                                        if (col != null && col.Attributes["w:val"] != null)
                                        {
                                            string hex = col.Attributes["w:val"].Value;
                                            if (hex.Length == 6)
                                            {
                                                try
                                                {
                                                    sInfo.Color = Color.FromArgb(
                                                        Convert.ToInt32(hex.Substring(0, 2), 16),
                                                        Convert.ToInt32(hex.Substring(2, 2), 16),
                                                        Convert.ToInt32(hex.Substring(4, 2), 16)
                                                    );
                                                }
                                                catch { }
                                            }
                                        }
                                    }

                                    // Paragraph properties inside style (<w:pPr>)
                                    XmlNode pPr = sNode.SelectSingleNode("w:pPr", nsmgr);
                                    if (pPr != null)
                                    {
                                        XmlNode jc = pPr.SelectSingleNode("w:jc", nsmgr);
                                        if (jc != null && jc.Attributes["w:val"] != null)
                                        {
                                            string jcVal = jc.Attributes["w:val"].Value.ToLower();
                                            if (jcVal == "center") sInfo.Alignment = 1;
                                            else if (jcVal == "right") sInfo.Alignment = 2;
                                            else if (jcVal == "both" || jcVal == "justify") sInfo.Alignment = 3;
                                            else sInfo.Alignment = 0;
                                        }
                                        XmlNode sp = pPr.SelectSingleNode("w:spacing", nsmgr);
                                        if (sp != null)
                                        {
                                            ApplySpacingFromNode(sp, sInfo);
                                        }
                                        XmlNode ind = pPr.SelectSingleNode("w:ind", nsmgr);
                                        if (ind != null)
                                        {
                                            ApplyIndentFromNode(ind, ref sInfo.IndentFirstLine, ref sInfo.IndentHanging, ref sInfo.IndentLeft, ref sInfo.IndentRight);
                                        }
                                        XmlNode pBdr = pPr.SelectSingleNode("w:pBdr", nsmgr) ?? pPr.SelectSingleNode(".//*[local-name()='pBdr']");
                                        if (pBdr != null)
                                        {
                                            XmlNode bBdr = pBdr.SelectSingleNode("w:bottom", nsmgr) ?? pBdr.SelectSingleNode(".//*[local-name()='bottom']");
                                            if (bBdr != null && bBdr.Attributes["w:val"] != null && bBdr.Attributes["w:val"].Value != "none" && bBdr.Attributes["w:val"].Value != "nil")
                                            {
                                                sInfo.HasBottomBorder = true;
                                                ParagraphBorderInfo tempB = new ParagraphBorderInfo();
                                                ParseBorderProps(bBdr, tempB);
                                                sInfo.BottomBorderColor = tempB.Color;
                                            }
                                        }
                                    }

                                    stylesDict[sId] = sInfo;
                                    if (!string.IsNullOrEmpty(sInfo.Name)) stylesDict[sInfo.Name] = sInfo;
                                }
                            }

                            // 2b. Resolve BasedOn style inheritance across all styles
                            foreach (var kvp in stylesDict)
                            {
                                DocxStyleInfo st = kvp.Value;
                                if (st == null || string.IsNullOrEmpty(st.BasedOn)) continue;
                                string curBaseId = st.BasedOn;
                                int depth = 0;
                                while (!string.IsNullOrEmpty(curBaseId) && depth < 10)
                                {
                                    DocxStyleInfo baseSt;
                                    if (!stylesDict.TryGetValue(curBaseId, out baseSt) || baseSt == null || baseSt == st) break;
                                    if (string.IsNullOrEmpty(st.FontName) && !string.IsNullOrEmpty(baseSt.FontName)) st.FontName = baseSt.FontName;
                                    if (st.FontSize <= 0 && baseSt.FontSize > 0) st.FontSize = baseSt.FontSize;
                                    if (!st.Bold.HasValue && baseSt.Bold.HasValue) st.Bold = baseSt.Bold;
                                    if (!st.Italic.HasValue && baseSt.Italic.HasValue) st.Italic = baseSt.Italic;
                                    if (!st.Underline.HasValue && baseSt.Underline.HasValue) st.Underline = baseSt.Underline;
                                    if (!st.Color.HasValue && baseSt.Color.HasValue) st.Color = baseSt.Color;
                                    if (!st.CharWidthScale.HasValue && baseSt.CharWidthScale.HasValue) st.CharWidthScale = baseSt.CharWidthScale;
                                    if (!st.CharSpacingPt.HasValue && baseSt.CharSpacingPt.HasValue) st.CharSpacingPt = baseSt.CharSpacingPt;
                                    if (!st.Alignment.HasValue && baseSt.Alignment.HasValue) st.Alignment = baseSt.Alignment;
                                    if (!st.LineSpacing.HasValue && baseSt.LineSpacing.HasValue) st.LineSpacing = baseSt.LineSpacing;
                                    if (!st.SpacingBefore.HasValue && baseSt.SpacingBefore.HasValue) st.SpacingBefore = baseSt.SpacingBefore;
                                    if (!st.SpacingAfter.HasValue && baseSt.SpacingAfter.HasValue) st.SpacingAfter = baseSt.SpacingAfter;
                                    if (!st.IndentFirstLine.HasValue && baseSt.IndentFirstLine.HasValue) st.IndentFirstLine = baseSt.IndentFirstLine;
                                    if (!st.IndentHanging.HasValue && baseSt.IndentHanging.HasValue) st.IndentHanging = baseSt.IndentHanging;
                                    if (!st.IndentLeft.HasValue && baseSt.IndentLeft.HasValue) st.IndentLeft = baseSt.IndentLeft;
                                    if (!st.IndentRight.HasValue && baseSt.IndentRight.HasValue) st.IndentRight = baseSt.IndentRight;
                                    if (!st.HasBottomBorder.HasValue && baseSt.HasBottomBorder.HasValue)
                                    {
                                        st.HasBottomBorder = baseSt.HasBottomBorder;
                                        st.BottomBorderColor = baseSt.BottomBorderColor;
                                    }
                                    curBaseId = baseSt.BasedOn;
                                    depth++;
                                }
                            }

                            // Also resolve Normal / default paragraph style defaults
                            DocxStyleInfo nStyle = null;
                            if (!string.IsNullOrEmpty(defaultParaStyleId) && stylesDict.ContainsKey(defaultParaStyleId))
                                nStyle = stylesDict[defaultParaStyleId];
                            else if (stylesDict.ContainsKey("Normal"))
                                nStyle = stylesDict["Normal"];

                            if (nStyle != null)
                            {
                                if (!string.IsNullOrEmpty(nStyle.FontName)) docDefaultFont = nStyle.FontName;
                                if (nStyle.FontSize > 0) docDefaultFontSize = nStyle.FontSize;
                            }
                        }
                        catch { }
                    }
                }

                // 1c. Parse Numbering Definitions (<word/numbering.xml>)
                Dictionary<string, DocxNumberingDef> numberingDefs = ParseNumberingXml(archive, nsmgr);
                Dictionary<string, int> numCounters = new Dictionary<string, int>();

                // 2. Iterate direct children of body: Paragraphs (<w:p>) and Tables (<w:tbl>)
                foreach (XmlNode child in bodyNode.ChildNodes)
                {
                    if (child.LocalName == "p")
                    {
                        ParseParagraphNode(child, nsmgr, archive, relsToTarget, sb, meta.FormattedRuns, docDefaultFont, docDefaultFontSize, stylesDict, meta.ParagraphAlignments, meta.ParagraphBottomBorders, meta.ParagraphBorders, meta.ParagraphLineSpacings, meta.ParagraphSpacingsBefore, meta.ParagraphSpacingsAfter, numberingDefs, numCounters, meta.ParagraphFirstLineIndents, meta.ParagraphLeftIndents, meta.ParagraphRightIndents, meta.ParagraphHangingIndents);
                    }
                    else if (child.LocalName == "tbl")
                    {
                        ParseTableNode(child, nsmgr, sb, meta.FormattedRuns, meta, archive, relsToTarget, stylesDict, docDefaultFont, docDefaultFontSize);
                    }
                }

                meta.TextContent = sb.ToString();
                if (meta.FormattedRuns.Count == 0 && meta.TextContent.Length > 0)
                {
                    meta.FormattedRuns.Add(new FormattedRun(0, meta.TextContent.Length, docDefaultFont, docDefaultFontSize, false, false, false, false, Color.Black, Color.Transparent));
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ParseDocumentXmlFull Exception: " + ex);
                meta.TextContent = xmlContent;
                return false;
            }
        }

        private static void ParseBorderProps(XmlNode node, ParagraphBorderInfo bdr)
        {
            if (node == null || bdr == null) return;
            if (node.Attributes["w:color"] != null && node.Attributes["w:color"].Value != "auto")
            {
                string hex = node.Attributes["w:color"].Value;
                if (hex.Length == 6)
                {
                    try
                    {
                        int rC = Convert.ToInt32(hex.Substring(0, 2), 16);
                        int gC = Convert.ToInt32(hex.Substring(2, 2), 16);
                        int bC = Convert.ToInt32(hex.Substring(4, 2), 16);
                        bdr.Color = Color.FromArgb(rC, gC, bC);
                    }
                    catch { }
                }
            }
            if (node.Attributes["w:sz"] != null)
            {
                float sz;
                if (float.TryParse(node.Attributes["w:sz"].Value, out sz))
                {
                    bdr.SizePt = sz / 8.0f; // in 1/8 pt
                }
            }
        }

        private static void ParseParagraphNode(
            XmlNode p,
            XmlNamespaceManager nsmgr,
            ZipArchive archive,
            Dictionary<string, string> relsToTarget,
            StringBuilder sb,
            List<FormattedRun> runs,
            string docDefaultFont,
            float docDefaultFontSize,
            Dictionary<string, DocxStyleInfo> stylesDict,
            Dictionary<int, int> paraAlignments,
            Dictionary<int, Color> paraBottomBorders,
            Dictionary<int, ParagraphBorderInfo> paraBorders = null,
            Dictionary<int, float> paraLineSpacings = null,
            Dictionary<int, float> paraSpacingsBefore = null,
            Dictionary<int, float> paraSpacingsAfter = null,
            Dictionary<string, DocxNumberingDef> numberingDefs = null,
            Dictionary<string, int> numCounters = null,
            Dictionary<int, float> paraFirstLineIndents = null,
            Dictionary<int, float> paraLeftIndents = null,
            Dictionary<int, float> paraRightIndents = null,
            Dictionary<int, float> paraHangingIndents = null)
        {
            int paraStartIndex = sb.Length;

            // Initialize with document defaults
            string paraFont = docDefaultFont;
            float paraFontSize = docDefaultFontSize;
            bool paraBold = false;
            bool paraItalic = false;
            bool paraUnderline = false;
            Color paraColor = Color.Black;
            float paraWidthScale = 1.0f;
            float paraCharSpacingPt = 0f;

            // 1. Inherit from Paragraph Style (<w:pPr/w:pStyle>) if present
            DocxStyleInfo sInfo = null;
            XmlNode pStyleNode = p.SelectSingleNode("w:pPr/w:pStyle", nsmgr);
            if (pStyleNode != null && pStyleNode.Attributes["w:val"] != null && stylesDict != null)
            {
                string sVal = pStyleNode.Attributes["w:val"].Value;
                if (stylesDict.TryGetValue(sVal, out sInfo))
                {
                    if (!string.IsNullOrEmpty(sInfo.FontName)) paraFont = sInfo.FontName;
                    if (sInfo.FontSize > 0) paraFontSize = sInfo.FontSize;
                    if (sInfo.Bold.HasValue) paraBold = sInfo.Bold.Value;
                    if (sInfo.Italic.HasValue) paraItalic = sInfo.Italic.Value;
                    if (sInfo.Underline.HasValue) paraUnderline = sInfo.Underline.Value;
                    if (sInfo.Color.HasValue) paraColor = sInfo.Color.Value;
                    if (sInfo.CharWidthScale.HasValue) paraWidthScale = sInfo.CharWidthScale.Value;
                    if (sInfo.CharSpacingPt.HasValue) paraCharSpacingPt = sInfo.CharSpacingPt.Value;
                    if (sInfo.Alignment.HasValue && paraAlignments != null && !paraAlignments.ContainsKey(paraStartIndex))
                    {
                        paraAlignments[paraStartIndex] = sInfo.Alignment.Value;
                    }
                    if (sInfo.LineSpacing.HasValue && paraLineSpacings != null && !paraLineSpacings.ContainsKey(paraStartIndex))
                    {
                        paraLineSpacings[paraStartIndex] = sInfo.LineSpacing.Value;
                    }
                    if (sInfo.SpacingBefore.HasValue && paraSpacingsBefore != null && !paraSpacingsBefore.ContainsKey(paraStartIndex))
                    {
                        paraSpacingsBefore[paraStartIndex] = sInfo.SpacingBefore.Value;
                    }
                    if (sInfo.SpacingAfter.HasValue && paraSpacingsAfter != null && !paraSpacingsAfter.ContainsKey(paraStartIndex))
                    {
                        paraSpacingsAfter[paraStartIndex] = sInfo.SpacingAfter.Value;
                    }
                }
            }

            // 2. Check Paragraph Alignment (<w:pPr/w:jc>)
            XmlNode jcNode = p.SelectSingleNode("w:pPr/w:jc", nsmgr);
            if (jcNode != null && jcNode.Attributes["w:val"] != null)
            {
                string jcVal = jcNode.Attributes["w:val"].Value.ToLower();
                int align = 0;
                if (jcVal == "center") align = 1;
                else if (jcVal == "right") align = 2;
                else if (jcVal == "both" || jcVal == "justify") align = 3;
                else if (jcVal == "left" || jcVal == "start") align = 0;
                if (paraAlignments != null)
                {
                    paraAlignments[paraStartIndex] = align;
                }
            }

            // 3. Check Paragraph Spacing (<w:pPr/w:spacing>)
            XmlNode spNode = p.SelectSingleNode("w:pPr/w:spacing", nsmgr);
            if (spNode != null)
            {
                ApplySpacingToParagraphMaps(spNode, paraStartIndex, paraLineSpacings, paraSpacingsBefore, paraSpacingsAfter);
            }

            // Paragraphs without explicit spacing inherit docDefaults (not the ribbon 8pt fallback)
            if (stylesDict != null)
            {
                DocxStyleInfo defPara;
                if (stylesDict.TryGetValue("__docDefaults", out defPara) && defPara != null)
                {
                    if (paraLineSpacings != null && !paraLineSpacings.ContainsKey(paraStartIndex) && defPara.LineSpacing.HasValue)
                        paraLineSpacings[paraStartIndex] = defPara.LineSpacing.Value;
                    if (paraSpacingsBefore != null && !paraSpacingsBefore.ContainsKey(paraStartIndex) && defPara.SpacingBefore.HasValue)
                        paraSpacingsBefore[paraStartIndex] = defPara.SpacingBefore.Value;
                    if (paraSpacingsAfter != null && !paraSpacingsAfter.ContainsKey(paraStartIndex) && defPara.SpacingAfter.HasValue)
                        paraSpacingsAfter[paraStartIndex] = defPara.SpacingAfter.Value;
                }
            }
            if (paraSpacingsAfter != null && !paraSpacingsAfter.ContainsKey(paraStartIndex))
                paraSpacingsAfter[paraStartIndex] = 0f;
            if (paraSpacingsBefore != null && !paraSpacingsBefore.ContainsKey(paraStartIndex))
                paraSpacingsBefore[paraStartIndex] = 0f;

            // 4. Check Paragraph Borders (<w:pPr/w:pBdr>)
            ParagraphBorderInfo bdrInfo = null;
            XmlNode pBdrNode = p.SelectSingleNode("w:pPr/w:pBdr", nsmgr);
            if (pBdrNode != null)
            {
                bdrInfo = new ParagraphBorderInfo();
                bool hasAny = false;

                XmlNode topBdr = pBdrNode.SelectSingleNode("w:top", nsmgr);
                if (topBdr != null && topBdr.Attributes["w:val"] != null && topBdr.Attributes["w:val"].Value != "none" && topBdr.Attributes["w:val"].Value != "nil")
                {
                    bdrInfo.Top = true; hasAny = true;
                    ParseBorderProps(topBdr, bdrInfo);
                }

                XmlNode botBdr = pBdrNode.SelectSingleNode("w:bottom", nsmgr);
                if (botBdr != null && botBdr.Attributes["w:val"] != null && botBdr.Attributes["w:val"].Value != "none" && botBdr.Attributes["w:val"].Value != "nil")
                {
                    bdrInfo.Bottom = true; hasAny = true;
                    ParseBorderProps(botBdr, bdrInfo);
                }

                XmlNode leftBdr = pBdrNode.SelectSingleNode("w:left", nsmgr);
                if (leftBdr != null && leftBdr.Attributes["w:val"] != null && leftBdr.Attributes["w:val"].Value != "none" && leftBdr.Attributes["w:val"].Value != "nil")
                {
                    bdrInfo.Left = true; hasAny = true;
                    ParseBorderProps(leftBdr, bdrInfo);
                }

                XmlNode rightBdr = pBdrNode.SelectSingleNode("w:right", nsmgr);
                if (rightBdr != null && rightBdr.Attributes["w:val"] != null && rightBdr.Attributes["w:val"].Value != "none" && rightBdr.Attributes["w:val"].Value != "nil")
                {
                    bdrInfo.Right = true; hasAny = true;
                    ParseBorderProps(rightBdr, bdrInfo);
                }

                if (hasAny)
                {
                    if (paraBorders != null) paraBorders[paraStartIndex] = bdrInfo;
                    if (paraBottomBorders != null && bdrInfo.Bottom && !bdrInfo.Top && !bdrInfo.Left && !bdrInfo.Right)
                    {
                        paraBottomBorders[paraStartIndex] = bdrInfo.Color;
                    }
                }
            }
            else if (sInfo != null && sInfo.HasBottomBorder.HasValue && sInfo.HasBottomBorder.Value)
            {
                bdrInfo = new ParagraphBorderInfo
                {
                    Bottom = true,
                    Color = sInfo.BottomBorderColor ?? Color.FromArgb(27, 54, 93)
                };
            }

            // 7. Check direct paragraph run properties in <w:pPr/w:rPr>
            XmlNode pRPr = p.SelectSingleNode("w:pPr/w:rPr", nsmgr);
            if (pRPr != null)
            {
                XmlNode bNode = pRPr.SelectSingleNode("w:b", nsmgr);
                if (bNode != null)
                {
                    var val = bNode.Attributes["w:val"];
                    paraBold = (val == null || (val.Value != "0" && val.Value.ToLower() != "false" && val.Value.ToLower() != "none"));
                }
                XmlNode iNode = pRPr.SelectSingleNode("w:i", nsmgr);
                if (iNode != null)
                {
                    var val = iNode.Attributes["w:val"];
                    paraItalic = (val == null || (val.Value != "0" && val.Value.ToLower() != "false" && val.Value.ToLower() != "none"));
                }
                XmlNode rFonts = pRPr.SelectSingleNode("w:rFonts", nsmgr);
                if (rFonts != null)
                {
                    if (rFonts.Attributes["w:ascii"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:ascii"].Value)) paraFont = rFonts.Attributes["w:ascii"].Value;
                    else if (rFonts.Attributes["w:hAnsi"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:hAnsi"].Value)) paraFont = rFonts.Attributes["w:hAnsi"].Value;
                    else if (rFonts.Attributes["w:cs"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:cs"].Value)) paraFont = rFonts.Attributes["w:cs"].Value;
                }
                XmlNode sz = pRPr.SelectSingleNode("w:sz", nsmgr) ?? pRPr.SelectSingleNode("w:szCs", nsmgr);
                if (sz != null && sz.Attributes["w:val"] != null)
                {
                    int halfPts;
                    if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0) paraFontSize = halfPts / 2.0f;
                }
                XmlNode wNode = pRPr.SelectSingleNode("w:w", nsmgr);
                if (wNode != null && wNode.Attributes["w:val"] != null)
                {
                    int wPct;
                    if (int.TryParse(wNode.Attributes["w:val"].Value, out wPct) && wPct > 10 && wPct <= 200)
                        paraWidthScale = wPct / 100.0f;
                }
                XmlNode spcNode = pRPr.SelectSingleNode("w:spacing", nsmgr);
                if (spcNode != null && spcNode.Attributes["w:val"] != null)
                {
                    int spcTwips;
                    if (int.TryParse(spcNode.Attributes["w:val"].Value, out spcTwips))
                        paraCharSpacingPt = spcTwips / 20.0f;
                }
                XmlNode col = pRPr.SelectSingleNode("w:color", nsmgr);
                if (col != null && col.Attributes["w:val"] != null)
                {
                    string hex = col.Attributes["w:val"].Value;
                    if (hex.Length == 6)
                    {
                        try
                        {
                            paraColor = Color.FromArgb(
                                Convert.ToInt32(hex.Substring(0, 2), 16),
                                Convert.ToInt32(hex.Substring(2, 2), 16),
                                Convert.ToInt32(hex.Substring(4, 2), 16)
                            );
                        }
                        catch { }
                    }
                }
            }

            // 7.5. Check Paragraph Numbering (<w:pPr/w:numPr>)
            DocxNumLvlInfo matchedLvl = null;
            XmlNode numPr = p.SelectSingleNode("w:pPr/w:numPr", nsmgr);
            if (numPr != null && numberingDefs != null)
            {
                XmlNode numIdNode = numPr.SelectSingleNode("w:numId", nsmgr);
                XmlNode ilvlNode = numPr.SelectSingleNode("w:ilvl", nsmgr);
                if (numIdNode != null && numIdNode.Attributes["w:val"] != null)
                {
                    string numId = numIdNode.Attributes["w:val"].Value;
                    int ilvl = 0;
                    if (ilvlNode != null && ilvlNode.Attributes["w:val"] != null)
                    {
                        int.TryParse(ilvlNode.Attributes["w:val"].Value, out ilvl);
                    }

                    DocxNumberingDef numDef;
                    if (numberingDefs.TryGetValue(numId, out numDef) && numDef != null)
                    {
                        if (numDef.Levels.TryGetValue(ilvl, out matchedLvl) && matchedLvl != null)
                        {
                            if (numCounters != null)
                            {
                                // Reset deeper levels for this numId
                                for (int subLvl = ilvl + 1; subLvl <= 9; subLvl++)
                                {
                                    numCounters.Remove(numId + ":" + subLvl);
                                }

                                string cKey = numId + ":" + ilvl;
                                int curVal = matchedLvl.Start;
                                if (numCounters.ContainsKey(cKey))
                                {
                                    curVal = numCounters[cKey];
                                }
                                numCounters[cKey] = curVal + 1;

                                string fmt = matchedLvl.NumFmt ?? "decimal";
                                string numStr;
                                if (fmt == "upperRoman") numStr = ToRoman(curVal);
                                else if (fmt == "lowerRoman") numStr = ToRoman(curVal).ToLower();
                                else if (fmt == "lowerLetter") numStr = ToLetter(curVal);
                                else if (fmt == "upperLetter") numStr = ToLetter(curVal).ToUpper();
                                else if (fmt == "bullet") numStr = matchedLvl.LvlText ?? "•";
                                else numStr = curVal.ToString();

                                string template = matchedLvl.LvlText ?? ("%" + (ilvl + 1) + ".");
                                string placeholder = "%" + (ilvl + 1);
                                string marker;
                                if (template.Contains(placeholder)) marker = template.Replace(placeholder, numStr);
                                else if (fmt == "bullet") marker = template;
                                else marker = numStr + ".";

                                string prefixText = marker + " ";
                                int pfxStart = sb.Length;
                                sb.Append(prefixText);

                                string pfxFont = !string.IsNullOrEmpty(matchedLvl.FontName) ? matchedLvl.FontName : paraFont;
                                float pfxSize = matchedLvl.FontSize > 0 ? matchedLvl.FontSize : paraFontSize;
                                pfxSize = ApplyOpenXmlCharacterScaling(pfxSize, paraWidthScale, paraCharSpacingPt);
                                bool pfxBold = matchedLvl.Bold.HasValue ? matchedLvl.Bold.Value : paraBold;
                                bool pfxItalic = matchedLvl.Italic.HasValue ? matchedLvl.Italic.Value : paraItalic;
                                Color pfxColor = matchedLvl.Color.HasValue ? matchedLvl.Color.Value : paraColor;

                                runs.Add(new FormattedRun(pfxStart, prefixText.Length, pfxFont, pfxSize, pfxBold, pfxItalic, false, false, pfxColor, Color.Transparent));
                            }
                        }
                    }
                }
            }

            // 7.6. Check Paragraph Indentation (<w:pPr/w:ind>)
            float? resolvedFirstLine = null;
            float? resolvedHanging = null;
            float? resolvedLeft = null;
            float? resolvedRight = null;

            // Inherit from Style
            if (pStyleNode != null && pStyleNode.Attributes["w:val"] != null && stylesDict != null)
            {
                string sVal = pStyleNode.Attributes["w:val"].Value;
                DocxStyleInfo indStyle;
                if (stylesDict.TryGetValue(sVal, out indStyle) && indStyle != null)
                {
                    if (indStyle.IndentFirstLine.HasValue) resolvedFirstLine = indStyle.IndentFirstLine;
                    if (indStyle.IndentHanging.HasValue) resolvedHanging = indStyle.IndentHanging;
                    if (indStyle.IndentLeft.HasValue) resolvedLeft = indStyle.IndentLeft;
                    if (indStyle.IndentRight.HasValue) resolvedRight = indStyle.IndentRight;
                }
            }

            // Inherit from Numbering lvl
            if (matchedLvl != null)
            {
                if (matchedLvl.IndentFirstLine.HasValue) resolvedFirstLine = matchedLvl.IndentFirstLine;
                if (matchedLvl.IndentHanging.HasValue) resolvedHanging = matchedLvl.IndentHanging;
                if (matchedLvl.IndentLeft.HasValue) resolvedLeft = matchedLvl.IndentLeft;
                if (matchedLvl.IndentRight.HasValue) resolvedRight = matchedLvl.IndentRight;
            }

            // Direct paragraph indents (<w:pPr/w:ind>) override
            XmlNode pIndNode = p.SelectSingleNode("w:pPr/w:ind", nsmgr);
            if (pIndNode != null)
            {
                ApplyIndentFromNode(pIndNode, ref resolvedFirstLine, ref resolvedHanging, ref resolvedLeft, ref resolvedRight);
            }

            float finalFLine = 0f;
            float finalLeft = resolvedLeft ?? 0f;
            float finalRight = resolvedRight ?? 0f;
            if (resolvedHanging.HasValue && resolvedHanging.Value > 0)
            {
                finalFLine = -resolvedHanging.Value;
            }
            else if (resolvedFirstLine.HasValue)
            {
                finalFLine = resolvedFirstLine.Value;
            }

            if (finalFLine != 0f && paraFirstLineIndents != null)
            {
                paraFirstLineIndents[paraStartIndex] = finalFLine;
            }
            if (finalLeft != 0f && paraLeftIndents != null)
            {
                paraLeftIndents[paraStartIndex] = finalLeft;
            }
            if (finalRight != 0f && paraRightIndents != null)
            {
                paraRightIndents[paraStartIndex] = finalRight;
            }

            // 8. Iterate paragraph child nodes (Runs, Drawings, Hyperlinks, etc.)
            float currentParaFontSize = ApplyOpenXmlCharacterScaling(paraFontSize, paraWidthScale, paraCharSpacingPt);
            string currentParaFont = paraFont;

            XmlNodeList children = p.SelectNodes("w:r | w:hyperlink | w:smartTag | w:drawing | w:pict | m:oMath | m:oMathPara | mc:AlternateContent | *[local-name()='AlternateContent']", nsmgr);
            if (children != null && children.Count > 0)
            {
                foreach (XmlNode r in children)
                {
                    if (r.LocalName == "AlternateContent")
                    {
                        XmlNode drawingInAlt = r.SelectSingleNode(".//w:drawing | .//w:pict | .//*[local-name()='drawing'] | .//*[local-name()='pict']", nsmgr);
                        if (drawingInAlt != null)
                        {
                            ExtractDrawingImage(drawingInAlt, nsmgr, archive, relsToTarget, sb, runs);
                        }
                        continue;
                    }

                    if (r.LocalName == "oMath" || r.LocalName == "oMathPara")
                    {
                        string mStr = ExtractMathNodeText(r, nsmgr);
                        if (!string.IsNullOrEmpty(mStr))
                        {
                            int start = sb.Length;
                            sb.Append(mStr);
                            runs.Add(new FormattedRun(start, mStr.Length, "Cambria Math", currentParaFontSize, false, true, false, false, Color.Black, Color.Transparent));
                        }
                        continue;
                    }

                    if (r.LocalName == "drawing" || r.LocalName == "pict")
                    {
                        ExtractDrawingImage(r, nsmgr, archive, relsToTarget, sb, runs);
                        continue;
                    }

                    XmlNode innerDrawing = r.SelectSingleNode(".//w:drawing | .//w:pict | .//*[local-name()='drawing'] | .//*[local-name()='pict']", nsmgr);
                    if (innerDrawing != null)
                    {
                        ExtractDrawingImage(innerDrawing, nsmgr, archive, relsToTarget, sb, runs);
                    }

                    // Check for embedded MathType equation (<w:object>)
                    XmlNode objNode = r.SelectSingleNode(".//w:object | .//*[local-name()='object']", nsmgr);
                    if (objNode != null)
                    {
                        ExtractDrawingImage(objNode, nsmgr, archive, relsToTarget, sb, runs);
                    }

                    // Extract run properties (inheriting paragraph defaults cleanly without run-to-run bleed)
                    string fontName = paraFont;
                    float fontSize = paraFontSize;
                    float runWidthScale = paraWidthScale;
                    float runCharSpacingPt = paraCharSpacingPt;
                    bool bold = paraBold;
                    bool italic = paraItalic;
                    bool underline = paraUnderline;
                    bool strike = false;
                    bool subscript = false, superscript = false;
                    Color color = paraColor;
                    Color highlight = Color.Transparent;

                    XmlNode rPr = r.SelectSingleNode("w:rPr", nsmgr) ?? r.SelectSingleNode(".//w:r/w:rPr", nsmgr);
                    if (rPr != null)
                    {
                        // 8.1. Check character style <w:rStyle>
                        XmlNode rStyleNode = rPr.SelectSingleNode("w:rStyle", nsmgr);
                        if (rStyleNode != null && rStyleNode.Attributes["w:val"] != null && stylesDict != null)
                        {
                            DocxStyleInfo crStyle = null;
                            if (stylesDict.TryGetValue(rStyleNode.Attributes["w:val"].Value, out crStyle))
                            {
                                if (!string.IsNullOrEmpty(crStyle.FontName)) fontName = crStyle.FontName;
                                if (crStyle.FontSize > 0) fontSize = crStyle.FontSize;
                                if (crStyle.Bold.HasValue) bold = crStyle.Bold.Value;
                                if (crStyle.Italic.HasValue) italic = crStyle.Italic.Value;
                                if (crStyle.Underline.HasValue) underline = crStyle.Underline.Value;
                                if (crStyle.Color.HasValue) color = crStyle.Color.Value;
                                if (crStyle.CharWidthScale.HasValue) runWidthScale = crStyle.CharWidthScale.Value;
                                if (crStyle.CharSpacingPt.HasValue) runCharSpacingPt = crStyle.CharSpacingPt.Value;
                            }
                        }

                        // 8.2. Bold / Italic / Underline / Strike
                        XmlNode bNode = rPr.SelectSingleNode("w:b", nsmgr);
                        if (bNode != null)
                        {
                            var val = bNode.Attributes["w:val"];
                            bold = (val == null || (val.Value != "0" && val.Value.ToLower() != "false" && val.Value.ToLower() != "none"));
                        }

                        XmlNode iNode = rPr.SelectSingleNode("w:i", nsmgr);
                        if (iNode != null)
                        {
                            var val = iNode.Attributes["w:val"];
                            italic = (val == null || (val.Value != "0" && val.Value.ToLower() != "false" && val.Value.ToLower() != "none"));
                        }

                        if (rPr.SelectSingleNode("w:u", nsmgr) != null)
                        {
                            var uVal = rPr.SelectSingleNode("w:u", nsmgr).Attributes["w:val"];
                            underline = (uVal == null || (uVal.Value != "none" && uVal.Value != "0"));
                        }
                        if (rPr.SelectSingleNode("w:strike | w:dstrike", nsmgr) != null) strike = true;

                        // 8.3. Subscript / Superscript
                        XmlNode vertAlign = rPr.SelectSingleNode("w:vertAlign", nsmgr);
                        if (vertAlign != null && vertAlign.Attributes["w:val"] != null)
                        {
                            string va = vertAlign.Attributes["w:val"].Value.ToLower();
                            if (va == "subscript") subscript = true;
                            else if (va == "superscript") superscript = true;
                        }

                        // 8.4. Fonts: prefer ascii/hAnsi, then cs, then eastAsia
                        XmlNode rFonts = rPr.SelectSingleNode("w:rFonts", nsmgr);
                        if (rFonts != null)
                        {
                            if (rFonts.Attributes["w:ascii"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:ascii"].Value))
                                fontName = rFonts.Attributes["w:ascii"].Value;
                            else if (rFonts.Attributes["w:hAnsi"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:hAnsi"].Value))
                                fontName = rFonts.Attributes["w:hAnsi"].Value;
                            else if (rFonts.Attributes["w:cs"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:cs"].Value))
                                fontName = rFonts.Attributes["w:cs"].Value;
                            else if (rFonts.Attributes["w:eastAsia"] != null && !string.IsNullOrEmpty(rFonts.Attributes["w:eastAsia"].Value))
                                fontName = rFonts.Attributes["w:eastAsia"].Value;
                        }

                        // 8.5. Font size: check both w:sz (Latin) and w:szCs (Complex Script / Vietnamese)
                        XmlNode sz = rPr.SelectSingleNode("w:sz", nsmgr);
                        XmlNode szCs = rPr.SelectSingleNode("w:szCs", nsmgr);
                        if (sz != null && sz.Attributes["w:val"] != null)
                        {
                            int halfPts;
                            if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                            {
                                fontSize = halfPts / 2.0f;
                            }
                        }
                        else if (szCs != null && szCs.Attributes["w:val"] != null)
                        {
                            int halfPts;
                            if (int.TryParse(szCs.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                            {
                                fontSize = halfPts / 2.0f;
                            }
                        }

                        // 8.5b. Horizontal character width scale (<w:w>) and condensed character spacing (<w:spacing>)
                        XmlNode wNode = rPr.SelectSingleNode("w:w", nsmgr);
                        if (wNode != null && wNode.Attributes["w:val"] != null)
                        {
                            int wPct;
                            if (int.TryParse(wNode.Attributes["w:val"].Value, out wPct) && wPct > 10 && wPct <= 200)
                            {
                                runWidthScale = wPct / 100.0f;
                            }
                        }
                        XmlNode spcNode = rPr.SelectSingleNode("w:spacing", nsmgr);
                        if (spcNode != null && spcNode.Attributes["w:val"] != null)
                        {
                            int spcTwips;
                            if (int.TryParse(spcNode.Attributes["w:val"].Value, out spcTwips))
                            {
                                runCharSpacingPt = spcTwips / 20.0f;
                            }
                        }

                        // 8.6. Color
                        XmlNode col = rPr.SelectSingleNode("w:color", nsmgr);
                        if (col != null && col.Attributes["w:val"] != null)
                        {
                            string hex = col.Attributes["w:val"].Value;
                            if (hex.Equals("auto", StringComparison.OrdinalIgnoreCase))
                            {
                                color = Color.Black;
                            }
                            else if (hex.Length == 6)
                            {
                                try
                                {
                                    color = Color.FromArgb(
                                        Convert.ToInt32(hex.Substring(0, 2), 16),
                                        Convert.ToInt32(hex.Substring(2, 2), 16),
                                        Convert.ToInt32(hex.Substring(4, 2), 16)
                                    );
                                }
                                catch { }
                            }
                        }

                        // 8.7. Highlight or Shading
                        XmlNode hl = rPr.SelectSingleNode("w:highlight", nsmgr);
                        if (hl != null && hl.Attributes["w:val"] != null)
                        {
                            highlight = MapHighlightColor(hl.Attributes["w:val"].Value);
                        }
                        else
                        {
                            XmlNode shd = rPr.SelectSingleNode("w:shd", nsmgr);
                            if (shd != null && shd.Attributes["w:fill"] != null)
                            {
                                string fillHex = shd.Attributes["w:fill"].Value;
                                if (fillHex.Length == 6)
                                {
                                    try
                                    {
                                        highlight = Color.FromArgb(
                                            Convert.ToInt32(fillHex.Substring(0, 2), 16),
                                            Convert.ToInt32(fillHex.Substring(2, 2), 16),
                                            Convert.ToInt32(fillHex.Substring(4, 2), 16)
                                        );
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    fontSize = ApplyOpenXmlCharacterScaling(fontSize, runWidthScale, runCharSpacingPt);

                    // 8.8. Extract text tokens
                    XmlNodeList tList = r.SelectNodes(".//w:t | .//w:tab | .//w:br | .//w:cr | .//w:sym | .//w:noBreakHyphen", nsmgr);
                    if (tList != null)
                    {
                        foreach (XmlNode tNode in tList)
                        {
                            string str = "";
                            if (tNode.LocalName == "t") str = tNode.InnerText;
                            else if (tNode.LocalName == "br")
                            {
                                var typeAttr = tNode.Attributes["w:type"] ?? tNode.Attributes["type"];
                                if (typeAttr != null && typeAttr.Value.ToLower() == "page")
                                {
                                    str = "\n[--- PAGE BREAK ---]\n";
                                }
                                else
                                {
                                    str = "\n";
                                }
                            }
                            else if (tNode.LocalName == "cr") str = "\n";
                            else if (tNode.LocalName == "noBreakHyphen") str = "-";
                            else if (tNode.LocalName == "sym" && tNode.Attributes["w:char"] != null)
                            {
                                string charHex = tNode.Attributes["w:char"].Value;
                                int cCode = Convert.ToInt32(charHex, 16);
                                str = ((char)cCode).ToString();
                            }

                            if (!string.IsNullOrEmpty(str))
                            {
                                int start = sb.Length;
                                sb.Append(str);

                                // Merge with previous run if formatting is identical
                                if (runs.Count > 0)
                                {
                                    var lastRun = runs[runs.Count - 1];
                                    if (lastRun.StartIndex + lastRun.Length == start &&
                                        lastRun.FontName == fontName &&
                                        Math.Abs(lastRun.FontSize - fontSize) < 0.01f &&
                                        lastRun.Bold == bold &&
                                        lastRun.Italic == italic &&
                                        lastRun.Underline == underline &&
                                        lastRun.Strike == strike &&
                                        lastRun.Color == color &&
                                        lastRun.Highlight == highlight &&
                                        lastRun.Subscript == subscript &&
                                        lastRun.Superscript == superscript)
                                    {
                                        lastRun.Length += str.Length;
                                        continue;
                                    }
                                }

                                runs.Add(new FormattedRun(start, str.Length, fontName, fontSize, bold, italic, underline, strike, color, highlight, subscript, superscript));
                            }
                        }
                    }
                }
            }
            else
            {
                XmlNode pDrawing = p.SelectSingleNode(".//w:drawing | .//w:pict | .//*[local-name()='drawing'] | .//*[local-name()='pict']", nsmgr);
                if (pDrawing != null)
                {
                    ExtractDrawingImage(pDrawing, nsmgr, archive, relsToTarget, sb, runs);
                }
            }
            if (sb.Length == paraStartIndex)
            {
                runs.Add(new FormattedRun(paraStartIndex, 1, paraFont, paraFontSize, paraBold, paraItalic, paraUnderline, false, paraColor, Color.Transparent));
            }
            sb.Append('\n');
            if (bdrInfo != null && bdrInfo.Bottom)
            {
                Color c = (bdrInfo.Color != Color.Empty && bdrInfo.Color.A > 0) ? bdrInfo.Color : Color.FromArgb(27, 54, 93);
                string hexClr = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
                string divTag = "![DIVIDER:COLOR=" + hexClr + "]\n";
                int dStart = sb.Length;
                sb.Append(divTag);
                runs.Add(new FormattedRun(dStart, divTag.Length, "Segoe UI", 9f, false, false, false, false, Color.Black, Color.Transparent));
            }

            // 9. Section Break in Paragraph Properties (<w:pPr/w:sectPr>)
            XmlNode pSectPr = p.SelectSingleNode("w:pPr/w:sectPr", nsmgr) ?? p.SelectSingleNode("*[local-name()='pPr']/*[local-name()='sectPr']");
            if (pSectPr != null)
            {
                XmlNode typeNode = pSectPr.SelectSingleNode("w:type", nsmgr) ?? pSectPr.SelectSingleNode("*[local-name()='type']");
                string sType = "nextpage";
                if (typeNode != null)
                {
                    var valAttr = typeNode.Attributes["w:val"] ?? typeNode.Attributes["val"];
                    if (valAttr != null) sType = valAttr.Value.ToLower();
                }
                if (sType != "continuous")
                {
                    int pbStart = sb.Length;
                    sb.Append("[--- PAGE BREAK ---]\n");
                    runs.Add(new FormattedRun(pbStart, 21, "Times New Roman", 10.5f, false, false, false, false, Color.Gray, Color.Transparent));
                }
            }
        }

        private static ZipArchiveEntry FindZipEntry(ZipArchive archive, string path)
        {
            if (archive == null || string.IsNullOrEmpty(path)) return null;
            string normalized = path.TrimStart('/', '\\').Replace('\\', '/').ToLower();
            if (!normalized.StartsWith("word/") && !normalized.StartsWith("customxml/"))
            {
                normalized = "word/" + normalized;
            }
            foreach (var entry in archive.Entries)
            {
                string entryNorm = entry.FullName.TrimStart('/', '\\').Replace('\\', '/').ToLower();
                if (entryNorm == normalized || entryNorm.EndsWith("/" + Path.GetFileName(normalized)))
                {
                    return entry;
                }
            }
            return null;
        }

        private class DiagShapeInfo
        {
            public long X, Y, W, H;
            public string Geom;
            public Color FillColor;
            public Color LineColor;
            public float LineWidth;
            public string Text;
            public bool IsConnector;
            public bool HasFill;
            public XmlNode CustGeomNode;
            public string ModelId;
            public string StartCxnId;
            public string EndCxnId;
        }

        private static GraphicsPath GetDocShapeRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateDrawingMLPath(XmlNode custGeom, RectangleF rf, float defaultW = 0, float defaultH = 0)
        {
            if (custGeom == null || rf.Width <= 0 || rf.Height <= 0) return null;
            try
            {
                XmlNode pathNode = custGeom.SelectSingleNode(".//*[local-name()='path']");
                if (pathNode == null) return null;

                float pw = 0f, ph = 0f;
                if (pathNode.Attributes != null)
                {
                    if (pathNode.Attributes["w"] != null) float.TryParse(pathNode.Attributes["w"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out pw);
                    if (pathNode.Attributes["h"] != null) float.TryParse(pathNode.Attributes["h"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out ph);
                }
                if (pw <= 0) pw = defaultW > 0 ? defaultW : 21600f;
                if (ph <= 0) ph = defaultH > 0 ? defaultH : 21600f;

                GraphicsPath gPath = new GraphicsPath();
                PointF curPt = new PointF(rf.X, rf.Y);

                foreach (XmlNode elem in pathNode.ChildNodes)
                {
                    string ln = elem.LocalName;
                    if (ln == "moveTo")
                    {
                        XmlNode pt = elem.SelectSingleNode(".//*[local-name()='pt']");
                        if (pt != null && pt.Attributes != null)
                        {
                            float x = 0, y = 0;
                            if (pt.Attributes["x"] != null) float.TryParse(pt.Attributes["x"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out x);
                            if (pt.Attributes["y"] != null) float.TryParse(pt.Attributes["y"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out y);
                            curPt = new PointF(rf.X + (x / pw) * rf.Width, rf.Y + (y / ph) * rf.Height);
                            gPath.StartFigure();
                        }
                    }
                    else if (ln == "lnTo")
                    {
                        XmlNode pt = elem.SelectSingleNode(".//*[local-name()='pt']");
                        if (pt != null && pt.Attributes != null)
                        {
                            float x = 0, y = 0;
                            if (pt.Attributes["x"] != null) float.TryParse(pt.Attributes["x"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out x);
                            if (pt.Attributes["y"] != null) float.TryParse(pt.Attributes["y"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out y);
                            PointF nextPt = new PointF(rf.X + (x / pw) * rf.Width, rf.Y + (y / ph) * rf.Height);
                            gPath.AddLine(curPt, nextPt);
                            curPt = nextPt;
                        }
                    }
                    else if (ln == "arcTo")
                    {
                        if (elem.Attributes != null)
                        {
                            long st = 0, sw = 0;
                            float wR = 0, hR = 0;
                            if (elem.Attributes["wR"] != null) float.TryParse(elem.Attributes["wR"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out wR);
                            if (elem.Attributes["hR"] != null) float.TryParse(elem.Attributes["hR"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out hR);
                            if (elem.Attributes["stAng"] != null) long.TryParse(elem.Attributes["stAng"].Value, out st);
                            if (elem.Attributes["swAng"] != null) long.TryParse(elem.Attributes["swAng"].Value, out sw);
                            float stDeg = st / 60000.0f;
                            float swDeg = sw / 60000.0f;

                            float rx = (wR > 0) ? (wR / pw) * rf.Width : (rf.Width / 2.0f);
                            float ry = (hR > 0) ? (hR / ph) * rf.Height : (rf.Height / 2.0f);

                            double stRad = stDeg * Math.PI / 180.0;
                            float cx = curPt.X - rx * (float)Math.Cos(stRad);
                            float cy = curPt.Y - ry * (float)Math.Sin(stRad);

                            RectangleF arcRect = new RectangleF(cx - rx, cy - ry, 2 * rx, 2 * ry);
                            gPath.AddArc(arcRect, stDeg, swDeg);

                            double endRad = (stDeg + swDeg) * Math.PI / 180.0;
                            curPt = new PointF(cx + rx * (float)Math.Cos(endRad), cy + ry * (float)Math.Sin(endRad));
                        }
                    }
                    else if (ln == "cubicBezTo")
                    {
                        XmlNodeList pts = elem.SelectNodes(".//*[local-name()='pt']");
                        if (pts != null && pts.Count >= 3)
                        {
                            PointF[] bezPts = new PointF[3];
                            for (int bi = 0; bi < 3; bi++)
                            {
                                float x = 0, y = 0;
                                if (pts[bi].Attributes["x"] != null) float.TryParse(pts[bi].Attributes["x"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out x);
                                if (pts[bi].Attributes["y"] != null) float.TryParse(pts[bi].Attributes["y"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out y);
                                bezPts[bi] = new PointF(rf.X + (x / pw) * rf.Width, rf.Y + (y / ph) * rf.Height);
                            }
                            gPath.AddBezier(curPt, bezPts[0], bezPts[1], bezPts[2]);
                            curPt = bezPts[2];
                        }
                    }
                    else if (ln == "close")
                    {
                        gPath.CloseFigure();
                    }
                }

                if (gPath.PointCount > 0) return gPath;
                gPath.Dispose();
            }
            catch { }
            return null;
        }

        private static GraphicsPath CreatePresetGeometryPath(string geom, RectangleF rf)
        {
            GraphicsPath path = new GraphicsPath();
            string g = (geom ?? "").ToLowerInvariant();
            if (g == "circulararrow" || g == "leftcirculararrow")
            {
                path.Dispose();
                return SmartArtRenderer.CreateCircularArrowPath(g == "circulararrow", rf);
            }
            if (g == "roundrect")
            {
                int radius = Math.Max(4, Math.Min(20, (int)(rf.Height * 0.16f)));
                path.Dispose();
                return GetDocShapeRoundedPath(Rectangle.Round(rf), radius);
            }
            if (g == "ellipse")
            {
                path.AddEllipse(rf);
                return path;
            }
            if (g == "chevron")
            {
                float chW = Math.Min(rf.Width * 0.35f, rf.Height * 0.45f);
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left, rf.Top),
                    new PointF(rf.Right - chW, rf.Top),
                    new PointF(rf.Right, rf.Top + rf.Height * 0.5f),
                    new PointF(rf.Right - chW, rf.Bottom),
                    new PointF(rf.Left, rf.Bottom),
                    new PointF(rf.Left + chW, rf.Top + rf.Height * 0.5f)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "hexagon")
            {
                float hx = rf.Width * 0.2f;
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left + hx, rf.Top),
                    new PointF(rf.Right - hx, rf.Top),
                    new PointF(rf.Right, rf.Top + rf.Height * 0.5f),
                    new PointF(rf.Right - hx, rf.Bottom),
                    new PointF(rf.Left + hx, rf.Bottom),
                    new PointF(rf.Left, rf.Top + rf.Height * 0.5f)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "trapezoid")
            {
                float tz = rf.Width * 0.2f;
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left + tz, rf.Top),
                    new PointF(rf.Right - tz, rf.Top),
                    new PointF(rf.Right, rf.Bottom),
                    new PointF(rf.Left, rf.Bottom)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "triangle" || g == "isocelestriangle")
            {
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left + rf.Width * 0.5f, rf.Top),
                    new PointF(rf.Right, rf.Bottom),
                    new PointF(rf.Left, rf.Bottom)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "rightarrow")
            {
                float aw = rf.Width * 0.35f;
                float ah = rf.Height * 0.25f;
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left, rf.Top + ah),
                    new PointF(rf.Right - aw, rf.Top + ah),
                    new PointF(rf.Right - aw, rf.Top),
                    new PointF(rf.Right, rf.Top + rf.Height * 0.5f),
                    new PointF(rf.Right - aw, rf.Bottom),
                    new PointF(rf.Right - aw, rf.Bottom - ah),
                    new PointF(rf.Left, rf.Bottom - ah)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "leftarrow")
            {
                float aw = rf.Width * 0.35f;
                float ah = rf.Height * 0.25f;
                PointF[] pts = new PointF[] {
                    new PointF(rf.Right, rf.Top + ah),
                    new PointF(rf.Left + aw, rf.Top + ah),
                    new PointF(rf.Left + aw, rf.Top),
                    new PointF(rf.Left, rf.Top + rf.Height * 0.5f),
                    new PointF(rf.Left + aw, rf.Bottom),
                    new PointF(rf.Left + aw, rf.Bottom - ah),
                    new PointF(rf.Right, rf.Bottom - ah)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "diamond")
            {
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left + rf.Width * 0.5f, rf.Top),
                    new PointF(rf.Right, rf.Top + rf.Height * 0.5f),
                    new PointF(rf.Left + rf.Width * 0.5f, rf.Bottom),
                    new PointF(rf.Left, rf.Top + rf.Height * 0.5f)
                };
                path.AddPolygon(pts);
                return path;
            }
            if (g == "star5")
            {
                PointF[] pts = new PointF[10];
                double cx = rf.Left + rf.Width * 0.5;
                double cy = rf.Top + rf.Height * 0.5;
                double rx = rf.Width * 0.5;
                double ry = rf.Height * 0.5;
                for (int i = 0; i < 10; i++)
                {
                    double a = -Math.PI / 2.0 + i * Math.PI / 5.0;
                    double r = (i % 2 == 0) ? 1.0 : 0.45;
                    pts[i] = new PointF((float)(cx + rx * r * Math.Cos(a)), (float)(cy + ry * r * Math.Sin(a)));
                }
                path.AddPolygon(pts);
                return path;
            }
            if (g == "parallelogram")
            {
                float skew = rf.Width * 0.2f;
                PointF[] pts = new PointF[] {
                    new PointF(rf.Left + skew, rf.Top),
                    new PointF(rf.Right, rf.Top),
                    new PointF(rf.Right - skew, rf.Bottom),
                    new PointF(rf.Left, rf.Bottom)
                };
                path.AddPolygon(pts);
                return path;
            }
            path.AddRectangle(rf);
            return path;
        }

        private static void DrawDocShapeAutoFitText(Graphics g, string text, Rectangle rect, Color color)
        {
            if (string.IsNullOrEmpty(text) || rect.Width <= 4 || rect.Height <= 4) return;
            float fSize = Math.Max(7f, Math.Min(16f, rect.Height * 0.28f));
            while (fSize > 7f)
            {
                using (Font testF = new Font("Segoe UI", fSize, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(text, testF, rect.Width - 6);
                    if (sz.Height <= rect.Height - 6) break;
                }
                fSize -= 0.5f;
            }
            using (Font f = new Font("Segoe UI", fSize, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(color))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
            {
                g.DrawString(text, f, b, rect, sf);
            }
        }

        private static string RenderDirectDrawingMLDiagram(ZipArchive archive, ZipArchiveEntry drawEntry, ZipArchiveEntry dmEntry, ZipArchiveEntry themeEntry, int targetW, int targetH, out string outNodesMeta)
        {
            Color[] dummyPalette;
            Rectangle dummyBounds;
            return RenderDirectDrawingMLDiagram(archive, drawEntry, dmEntry, themeEntry, targetW, targetH, out outNodesMeta, out dummyPalette, out dummyBounds);
        }

        private static string RenderDirectDrawingMLDiagram(ZipArchive archive, ZipArchiveEntry drawEntry, ZipArchiveEntry dmEntry, ZipArchiveEntry themeEntry, int targetW, int targetH, out string outNodesMeta, out Color[] outPalette, out Rectangle outEmuBounds)
        {
            outNodesMeta = "";
            outPalette = null;
            outEmuBounds = Rectangle.Empty;
            if (drawEntry == null || dmEntry == null) return null;
            try
            {
                // 1. Build Theme Palette
                Dictionary<string, Color> palette = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
                palette["accent1"] = Color.FromArgb(68, 114, 196); // Office Blue (#4472C4)
                palette["accent2"] = Color.FromArgb(237, 125, 49);
                palette["accent3"] = Color.FromArgb(165, 165, 165);
                palette["accent4"] = Color.FromArgb(255, 192, 0);
                palette["accent5"] = Color.FromArgb(91, 155, 213);
                palette["accent6"] = Color.FromArgb(112, 173, 71);
                palette["lt1"] = Color.White;
                palette["lt2"] = Color.FromArgb(238, 236, 225);
                palette["dk1"] = Color.FromArgb(30, 30, 30);
                palette["dk2"] = Color.FromArgb(31, 73, 125);

                if (themeEntry != null)
                {
                    try
                    {
                        using (Stream ts = themeEntry.Open())
                        {
                            XmlDocument tDoc = new XmlDocument();
                            tDoc.Load(ts);
                            for (int a = 1; a <= 6; a++)
                            {
                                string accName = "accent" + a;
                                XmlNode clrNode = tDoc.SelectSingleNode("//*[local-name()='" + accName + "']/*[local-name()='srgbClr']");
                                if (clrNode != null && clrNode.Attributes != null && clrNode.Attributes["val"] != null)
                                {
                                    int rgb;
                                    if (int.TryParse(clrNode.Attributes["val"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                                    {
                                        palette[accName] = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                                    }
                                }
                            }

                            string[] sysClrNames = new string[] { "dk1", "dk2", "lt1", "lt2" };
                            foreach (string scName in sysClrNames)
                            {
                                XmlNode scNode = tDoc.SelectSingleNode("//*[local-name()='" + scName + "']/*[local-name()='srgbClr' or local-name()='sysClr']");
                                if (scNode != null && scNode.Attributes != null)
                                {
                                    var valAttr = scNode.Attributes["lastClr"] ?? scNode.Attributes["val"];
                                    if (valAttr != null)
                                    {
                                        int rgb;
                                        if (int.TryParse(valAttr.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                                        {
                                            palette[scName] = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                outPalette = new Color[] {
                    palette["accent1"], palette["accent2"], palette["accent3"],
                    palette["accent4"], palette["accent5"], palette["accent6"]
                };

                // 2. Parse dataX.xml
                Dictionary<string, string> dataTexts = new Dictionary<string, string>();
                Dictionary<string, string> presToData = new Dictionary<string, string>();
                using (Stream ds = dmEntry.Open())
                {
                    XmlDocument dmDoc = new XmlDocument();
                    dmDoc.Load(ds);

                    XmlNodeList ptList = dmDoc.SelectNodes("//*[local-name()='pt']");
                    if (ptList != null)
                    {
                        foreach (XmlNode pt in ptList)
                        {
                            string modelId = (pt.Attributes != null && pt.Attributes["modelId"] != null) ? pt.Attributes["modelId"].Value : null;
                            if (string.IsNullOrEmpty(modelId)) continue;

                            List<string> runs = new List<string>();
                            XmlNodeList aTList = pt.SelectNodes(".//*[local-name()='t' and not(*)]");
                            if (aTList != null)
                            {
                                foreach (XmlNode at in aTList)
                                {
                                    string val = (at.InnerText ?? "").Trim();
                                    if (!string.IsNullOrEmpty(val) && !val.Contains("<") && !val.Contains("bodyPr"))
                                        runs.Add(val);
                                }
                            }

                            string ptText = string.Join(" ", runs.ToArray()).Trim();
                            if (string.IsNullOrEmpty(ptText))
                            {
                                XmlNode prSet = pt.SelectSingleNode(".//*[local-name()='prSet']");
                                if (prSet != null && prSet.Attributes != null && prSet.Attributes["phldrT"] != null)
                                {
                                    string ph = (prSet.Attributes["phldrT"].Value ?? "").Trim();
                                    if (!string.IsNullOrEmpty(ph) && !ph.Contains("<") && !ph.Contains("bodyPr"))
                                        ptText = ph;
                                }
                            }

                            if (!string.IsNullOrEmpty(ptText))
                            {
                                dataTexts[modelId] = ptText;
                            }
                        }
                    }

                    XmlNodeList cxnList = dmDoc.SelectNodes("//*[local-name()='cxn']");
                    if (cxnList != null)
                    {
                        foreach (XmlNode cxn in cxnList)
                        {
                            string cType = (cxn.Attributes != null && cxn.Attributes["type"] != null) ? cxn.Attributes["type"].Value : "";
                            string srcId = (cxn.Attributes != null && cxn.Attributes["srcId"] != null) ? cxn.Attributes["srcId"].Value : null;
                            string destId = (cxn.Attributes != null && cxn.Attributes["destId"] != null) ? cxn.Attributes["destId"].Value : null;
                            if (cType == "presOf" && !string.IsNullOrEmpty(destId) && !string.IsNullOrEmpty(srcId))
                            {
                                presToData[destId] = srcId;
                            }
                        }
                    }
                }

                // 3. Parse drawingX.xml
                List<DiagShapeInfo> shapes = new List<DiagShapeInfo>();
                using (Stream drs = drawEntry.Open())
                {
                    XmlDocument drawDoc = new XmlDocument();
                    drawDoc.Load(drs);

                    XmlNodeList spList = drawDoc.SelectNodes("//*[local-name()='sp' or local-name()='cxnSp']");
                    if (spList != null)
                    {
                        foreach (XmlNode sp in spList)
                        {
                            XmlNode xfrm = sp.SelectSingleNode(".//*[local-name()='xfrm']");
                            if (xfrm == null) continue;
                            XmlNode off = xfrm.SelectSingleNode(".//*[local-name()='off']");
                            XmlNode ext = xfrm.SelectSingleNode(".//*[local-name()='ext']");
                            if (off == null || ext == null || off.Attributes == null || ext.Attributes == null) continue;

                            long sx = 0, sy = 0, scx = 0, scy = 0;
                            if (off.Attributes["x"] != null) long.TryParse(off.Attributes["x"].Value, out sx);
                            if (off.Attributes["y"] != null) long.TryParse(off.Attributes["y"].Value, out sy);
                            if (ext.Attributes["cx"] != null) long.TryParse(ext.Attributes["cx"].Value, out scx);
                            if (ext.Attributes["cy"] != null) long.TryParse(ext.Attributes["cy"].Value, out scy);
                            if (scx <= 0 || scy <= 0) continue;

                            string mid = (sp.Attributes != null && sp.Attributes["modelId"] != null) ? sp.Attributes["modelId"].Value : "";
                            XmlNode stCxn = sp.SelectSingleNode(".//*[local-name()='stCxn']");
                            XmlNode endCxn = sp.SelectSingleNode(".//*[local-name()='endCxn']");
                            string stId = (stCxn != null && stCxn.Attributes != null && stCxn.Attributes["id"] != null) ? stCxn.Attributes["id"].Value : null;
                            string endId = (endCxn != null && endCxn.Attributes != null && endCxn.Attributes["id"] != null) ? endCxn.Attributes["id"].Value : null;

                            string sText = "";
                            List<string> sRuns = new List<string>();
                            XmlNodeList txRuns = sp.SelectNodes(".//*[local-name()='txBody']//*[local-name()='t' and not(*)]");
                            if (txRuns != null)
                            {
                                foreach (XmlNode tr in txRuns)
                                {
                                    string tv = (tr.InnerText ?? "").Trim();
                                    if (!string.IsNullOrEmpty(tv) && !tv.Contains("<") && !tv.Contains("bodyPr"))
                                        sRuns.Add(tv);
                                }
                            }
                            sText = string.Join(" ", sRuns.ToArray()).Trim();
                            if (string.IsNullOrEmpty(sText) && !string.IsNullOrEmpty(mid))
                            {
                                string dId;
                                if (presToData.TryGetValue(mid, out dId) && dataTexts.ContainsKey(dId))
                                    sText = dataTexts[dId];
                                else if (dataTexts.ContainsKey(mid))
                                    sText = dataTexts[mid];
                            }

                            XmlNode spPr = sp.SelectSingleNode(".//*[local-name()='spPr']");
                            XmlNode prstGeom = spPr != null ? spPr.SelectSingleNode(".//*[local-name()='prstGeom']") : sp.SelectSingleNode(".//*[local-name()='prstGeom']");
                            string geomName = (prstGeom != null && prstGeom.Attributes != null && prstGeom.Attributes["prst"] != null) ? prstGeom.Attributes["prst"].Value : "";
                            XmlNode custGeomNode = spPr != null ? spPr.SelectSingleNode(".//*[local-name()='custGeom']") : sp.SelectSingleNode(".//*[local-name()='custGeom']");

                            bool isCxnTag = sp.LocalName == "cxnSp" || (sp.Name != null && sp.Name.EndsWith("cxnSp"));
                            bool isLineGeom = geomName == "line" || geomName.StartsWith("straightConnector") || geomName.StartsWith("bentConnector") || geomName.StartsWith("curvedConnector");
                            bool isCircArrow = geomName == "circularArrow" || geomName == "leftCircularArrow";
                            bool hasNoFill = spPr != null && spPr.SelectSingleNode("./*[local-name()='noFill']") != null;
                            XmlNode solidFillNode = spPr != null ? spPr.SelectSingleNode("./*[local-name()='solidFill']") : null;
                            XmlNode gradFillNode = spPr != null ? spPr.SelectSingleNode("./*[local-name()='gradFill']") : null;
                            bool hasFill = !hasNoFill && (solidFillNode != null || gradFillNode != null);
                            bool isConn = isCxnTag || isLineGeom || (!hasFill && string.IsNullOrEmpty(sText) && !isCircArrow);

                            Color fColor = palette["accent1"];
                            if (solidFillNode != null)
                            {
                                XmlNode sc = solidFillNode.SelectSingleNode(".//*[local-name()='schemeClr']");
                                if (sc != null && sc.Attributes != null && sc.Attributes["val"] != null && palette.ContainsKey(sc.Attributes["val"].Value))
                                    fColor = palette[sc.Attributes["val"].Value];
                                else
                                {
                                    XmlNode srgb = solidFillNode.SelectSingleNode(".//*[local-name()='srgbClr']");
                                    if (srgb != null && srgb.Attributes != null && srgb.Attributes["val"] != null)
                                    {
                                        int rgb;
                                        if (int.TryParse(srgb.Attributes["val"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                                            fColor = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                                    }
                                }

                                XmlNode tintNode = solidFillNode.SelectSingleNode(".//*[local-name()='tint']");
                                if (tintNode != null && tintNode.Attributes != null && tintNode.Attributes["val"] != null)
                                {
                                    int tintVal;
                                    if (int.TryParse(tintNode.Attributes["val"].Value, out tintVal) && tintVal > 0 && tintVal < 100000)
                                    {
                                        float factor = tintVal / 100000.0f;
                                        int tr = (int)(fColor.R * factor + 255 * (1.0f - factor));
                                        int tg = (int)(fColor.G * factor + 255 * (1.0f - factor));
                                        int tb = (int)(fColor.B * factor + 255 * (1.0f - factor));
                                        fColor = Color.FromArgb(255, Math.Max(0, Math.Min(255, tr)), Math.Max(0, Math.Min(255, tg)), Math.Max(0, Math.Min(255, tb)));
                                    }
                                }
                            }

                            Color lColor = ControlPaint.Dark(fColor, 0.2f);
                            float lWidth = 1.0f;
                            XmlNode ln = spPr != null ? spPr.SelectSingleNode("./*[local-name()='ln']") : sp.SelectSingleNode(".//*[local-name()='ln']");
                            if (ln != null)
                            {
                                XmlNode lsc = ln.SelectSingleNode(".//*[local-name()='schemeClr']");
                                if (lsc != null && lsc.Attributes != null && lsc.Attributes["val"] != null && palette.ContainsKey(lsc.Attributes["val"].Value))
                                    lColor = palette[lsc.Attributes["val"].Value];
                                else
                                {
                                    XmlNode lsrgb = ln.SelectSingleNode(".//*[local-name()='srgbClr']");
                                    if (lsrgb != null && lsrgb.Attributes != null && lsrgb.Attributes["val"] != null)
                                    {
                                        int lrgb;
                                        if (int.TryParse(lsrgb.Attributes["val"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lrgb))
                                            lColor = Color.FromArgb(255, (lrgb >> 16) & 0xFF, (lrgb >> 8) & 0xFF, lrgb & 0xFF);
                                    }
                                }
                                if (ln.Attributes != null && ln.Attributes["w"] != null)
                                {
                                    long lw;
                                    if (long.TryParse(ln.Attributes["w"].Value, out lw)) lWidth = Math.Max(1.0f, lw / 12700.0f);
                                }
                            }

                            shapes.Add(new DiagShapeInfo
                            {
                                X = sx, Y = sy, W = scx, H = scy,
                                Geom = geomName,
                                FillColor = fColor,
                                LineColor = lColor,
                                LineWidth = lWidth,
                                Text = sText,
                                IsConnector = isConn,
                                HasFill = hasFill,
                                CustGeomNode = custGeomNode,
                                ModelId = mid,
                                StartCxnId = stId,
                                EndCxnId = endId
                            });
                        }
                    }
                }

                if (shapes.Count == 0) return null;

                long minX = long.MaxValue, minY = long.MaxValue, maxX = long.MinValue, maxY = long.MinValue;
                foreach (DiagShapeInfo s in shapes)
                {
                    if (s.X < minX) minX = s.X;
                    if (s.Y < minY) minY = s.Y;
                    if (s.X + s.W > maxX) maxX = s.X + s.W;
                    if (s.Y + s.H > maxY) maxY = s.Y + s.H;
                }
                long totalW = maxX - minX;
                long totalH = maxY - minY;
                if (totalW <= 0 || totalH <= 0) return null;

                outEmuBounds = new Rectangle((int)Math.Max(0, minX), (int)Math.Max(0, minY), (int)totalW, (int)totalH);

                int bmpW = Math.Max(240, targetW);
                int bmpH = Math.Max(160, targetH);
                int renderW = (int)(bmpW * 1.5f);
                int renderH = (int)(bmpH * 1.5f);

                float pad = 20.0f;
                float scale = Math.Min((renderW - 2 * pad) / (float)totalW, (renderH - 2 * pad) / (float)totalH);
                float shiftX = pad + (renderW - 2 * pad - totalW * scale) / 2.0f;
                float shiftY = pad + (renderH - 2 * pad - totalH * scale) / 2.0f;

                Bitmap bmp = new Bitmap(renderW, renderH);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // 1. Connectors and circular arrows
                    foreach (DiagShapeInfo s in shapes)
                    {
                        if (s.IsConnector || s.Geom == "circularArrow" || s.Geom == "leftCircularArrow")
                        {
                            float rx = shiftX + (s.X - minX) * scale;
                            float ry = shiftY + (s.Y - minY) * scale;
                            float rw = s.W * scale;
                            float rh = s.H * scale;
                            RectangleF rf = new RectangleF(rx, ry, Math.Max(2, rw), Math.Max(2, rh));

                            if (s.Geom == "circularArrow" || s.Geom == "leftCircularArrow")
                            {
                                using (GraphicsPath caPath = SmartArtRenderer.CreateCircularArrowPath(s.Geom == "circularArrow", rf))
                                {
                                    if (caPath != null)
                                    {
                                        if (s.HasFill)
                                        {
                                            using (SolidBrush cfb = new SolidBrush(s.FillColor))
                                                g.FillPath(cfb, caPath);
                                        }
                                        using (Pen cp = new Pen(s.LineColor, Math.Max(1.0f, s.LineWidth)))
                                            g.DrawPath(cp, caPath);
                                        continue;
                                    }
                                }
                            }

                            using (Pen cp = new Pen(s.LineColor, Math.Max(1.5f, s.LineWidth * 1.5f)))
                            {
                                if (s.CustGeomNode != null)
                                {
                                    using (GraphicsPath cpPath = CreateDrawingMLPath(s.CustGeomNode, rf, s.W, s.H))
                                    {
                                        if (cpPath != null)
                                        {
                                            if (s.HasFill)
                                            {
                                                using (SolidBrush cfb = new SolidBrush(s.FillColor))
                                                    g.FillPath(cfb, cpPath);
                                            }
                                            g.DrawPath(cp, cpPath);
                                            continue;
                                        }
                                    }
                                }
                                if (s.Geom == "arc" || (s.W > 0 && s.H > 0 && Math.Abs(s.W - s.H) < s.W * 0.35f && s.IsConnector))
                                {
                                    g.DrawArc(cp, rf, 0, 180);
                                }
                                else
                                {
                                    // If connector has start and end IDs, connect them accurately:
                                    DiagShapeInfo startShape = null;
                                    DiagShapeInfo endShape = null;
                                    if (!string.IsNullOrEmpty(s.StartCxnId) && !string.IsNullOrEmpty(s.EndCxnId))
                                    {
                                        foreach (var sh in shapes)
                                        {
                                            if (sh.ModelId == s.StartCxnId) startShape = sh;
                                            if (sh.ModelId == s.EndCxnId) endShape = sh;
                                        }
                                    }
                                    if (startShape != null && endShape != null)
                                    {
                                        float sx = shiftX + (startShape.X + startShape.W * 0.5f - minX) * scale;
                                        float sy = shiftY + (startShape.Y + startShape.H * 0.5f - minY) * scale;
                                        float ex = shiftX + (endShape.X + endShape.W * 0.5f - minX) * scale;
                                        float ey = shiftY + (endShape.Y + endShape.H * 0.5f - minY) * scale;
                                        using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(4, 4))
                                        {
                                            cp.CustomEndCap = arrowCap;
                                            g.DrawLine(cp, sx, sy, ex, ey);
                                        }
                                    }
                                    else
                                    {
                                        g.DrawLine(cp, rf.Left, rf.Top + rf.Height * 0.5f, rf.Right, rf.Top + rf.Height * 0.5f);
                                    }
                                }
                            }
                        }
                    }

                    // 2. Nodes
                    foreach (DiagShapeInfo s in shapes)
                    {
                        if (!s.IsConnector && s.Geom != "circularArrow" && s.Geom != "leftCircularArrow")
                        {
                            float rx = shiftX + (s.X - minX) * scale;
                            float ry = shiftY + (s.Y - minY) * scale;
                            float rw = s.W * scale;
                            float rh = s.H * scale;
                            RectangleF rf = new RectangleF(rx, ry, Math.Max(2, rw), Math.Max(2, rh));

                            GraphicsPath geomPath = null;
                            if (s.CustGeomNode != null)
                            {
                                geomPath = CreateDrawingMLPath(s.CustGeomNode, rf, s.W, s.H);
                            }
                            if (geomPath == null)
                            {
                                geomPath = CreatePresetGeometryPath(s.Geom, rf);
                            }

                            using (geomPath)
                            using (SolidBrush fb = new SolidBrush(s.FillColor))
                            using (Pen lp = new Pen(s.LineColor, Math.Max(1f, s.LineWidth)))
                            {
                                if (s.HasFill)
                                {
                                    g.FillPath(fb, geomPath);
                                }
                                g.DrawPath(lp, geomPath);
                            }

                            if (!string.IsNullOrEmpty(s.Text))
                            {
                                Color tColor = (s.FillColor.R * 0.299 + s.FillColor.G * 0.587 + s.FillColor.B * 0.114) > 160 ? Color.FromArgb(30, 30, 30) : Color.White;
                                DrawDocShapeAutoFitText(g, s.Text, Rectangle.Round(rf), tColor);
                            }
                        }
                    }
                }

                // Build relative bounds & colors for sub-nodes
                StringBuilder sbMeta = new StringBuilder();
                foreach (DiagShapeInfo s in shapes)
                {
                    if (!s.IsConnector && s.Geom != "circularArrow" && s.Geom != "leftCircularArrow")
                    {
                        float relX = (s.X - minX) / (float)totalW;
                        float relY = (s.Y - minY) / (float)totalH;
                        float relW = s.W / (float)totalW;
                        float relH = s.H / (float)totalH;
                        string hexCol = string.Format("{0:X2}{1:X2}{2:X2}", s.FillColor.R, s.FillColor.G, s.FillColor.B);
                        string gName = !string.IsNullOrEmpty(s.Geom) ? s.Geom : "roundRect";
                        if (sbMeta.Length > 0) sbMeta.Append("|");
                        sbMeta.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3},{3:F3},{4},{5}", relX, relY, relW, relH, hexCol, gName);
                    }
                }
                outNodesMeta = sbMeta.ToString();

                string tempPath = Path.Combine(Path.GetTempPath(), "genoffice_sa_draw_" + Guid.NewGuid().ToString("N") + ".png");
                bmp.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();
                return tempPath;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryParseSmartArtDiagram(XmlNode drawingNode, XmlNamespaceManager nsmgr, ZipArchive archive, Dictionary<string, string> relsToTarget, int targetW, int targetH, out string saType, out string saTitle, out string saText, out string preRenderedPath, out string nodesMeta)
        {
            bool arLock;
            Rectangle bounds;
            Color[] palette;
            return TryParseSmartArtDiagram(drawingNode, nsmgr, archive, relsToTarget, targetW, targetH, out saType, out saTitle, out saText, out preRenderedPath, out nodesMeta, out arLock, out bounds, out palette);
        }

        private static bool TryParseSmartArtDiagram(XmlNode drawingNode, XmlNamespaceManager nsmgr, ZipArchive archive, Dictionary<string, string> relsToTarget, int targetW, int targetH, out string saType, out string saTitle, out string saText, out string preRenderedPath, out string nodesMeta, out bool aspectRatioLock, out Rectangle origBoundsEmu, out Color[] resolvedPalette)
        {
            saType = "List_BasicBlock";
            saTitle = "Sơ đồ SmartArt";
            saText = "";
            preRenderedPath = null;
            nodesMeta = "";
            aspectRatioLock = false;
            origBoundsEmu = Rectangle.Empty;
            resolvedPalette = null;
            if (drawingNode == null) return false;

            // 1. Check docPr descr for GenOffice SmartArt format
            XmlNode docPr = drawingNode.SelectSingleNode(".//wp:docPr", nsmgr) ?? drawingNode.SelectSingleNode(".//*[local-name()='docPr']");
            if (docPr != null && docPr.Attributes != null && docPr.Attributes["descr"] != null && docPr.Attributes["descr"].Value.StartsWith("SmartArt|"))
            {
                string descr = docPr.Attributes["descr"].Value;
                string[] parts = descr.Split('|');
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) saType = parts[1];
                if (parts.Length > 2) saTitle = parts[2];
                if (parts.Length > 3)
                {
                    List<string> tParts = new List<string>();
                    for (int pi = 3; pi < parts.Length; pi++) tParts.Add(parts[pi]);
                    saText = string.Join("|", tParts.ToArray());
                }
                aspectRatioLock = OpenXmlSmartArtModel.IsCycleOrRadialLayout(saType);
                return true;
            }

            // 2. Check for native DrawingML diagram / relIds
            XmlNode relIdsNode = drawingNode.SelectSingleNode(".//*[local-name()='relIds']");
            bool hasDiagramUri = false;
            XmlNode gd = drawingNode.SelectSingleNode(".//*[local-name()='graphicData']");
            if (gd != null && gd.Attributes != null && gd.Attributes["uri"] != null && gd.Attributes["uri"].Value.IndexOf("diagram", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hasDiagramUri = true;
            }

            if (relIdsNode != null || hasDiagramUri)
            {
                string dmRelId = null;
                string loRelId = null;
                if (relIdsNode != null && relIdsNode.Attributes != null)
                {
                    foreach (XmlAttribute attr in relIdsNode.Attributes)
                    {
                        if (attr.LocalName == "dm") dmRelId = attr.Value;
                        if (attr.LocalName == "lo") loRelId = attr.Value;
                    }
                }

                if (string.IsNullOrEmpty(dmRelId) && relsToTarget != null)
                {
                    foreach (var kv in relsToTarget)
                    {
                        if (kv.Value != null && kv.Value.IndexOf("data", StringComparison.OrdinalIgnoreCase) >= 0 && kv.Value.IndexOf("diagram", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            dmRelId = kv.Key;
                            break;
                        }
                    }
                }

                ZipArchiveEntry dmEntry = null;
                if (!string.IsNullOrEmpty(dmRelId) && relsToTarget != null && relsToTarget.ContainsKey(dmRelId))
                {
                    dmEntry = FindZipEntry(archive, relsToTarget[dmRelId]);
                }
                if (dmEntry == null && archive != null)
                {
                    foreach (var entry in archive.Entries)
                    {
                        string fn = entry.FullName.ToLower();
                        if (fn.Contains("diagrams/data") || fn.Contains("diagramdata"))
                        {
                            dmEntry = entry;
                            break;
                        }
                    }
                }

                ZipArchiveEntry drawEntry = null;
                if (dmEntry != null && archive != null)
                {
                    string drawPath = dmEntry.FullName.Replace("data", "drawing");
                    drawEntry = archive.GetEntry(drawPath) ?? FindZipEntry(archive, drawPath);
                }
                if (drawEntry == null && archive != null)
                {
                    foreach (var entry in archive.Entries)
                    {
                        string fn = entry.FullName.ToLower();
                        if (fn.Contains("diagrams/drawing") || fn.Contains("diagramdrawing"))
                        {
                            drawEntry = entry;
                            break;
                        }
                    }
                }

                ZipArchiveEntry themeEntry = (archive != null) ? (archive.GetEntry("word/theme/theme1.xml") ?? FindZipEntry(archive, "theme1.xml")) : null;

                List<string> nodeTexts = new List<string>();
                string layoutInfo = "";
                if (dmEntry != null)
                {
                    try
                    {
                        string dmXml = "";
                        using (Stream s = dmEntry.Open())
                        using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                        {
                            dmXml = sr.ReadToEnd();
                        }

                        try
                        {
                            string safeXml = Regex.Replace(dmXml, @"&(?!(amp|lt|gt|quot|apos|#\d+|#x[0-9a-fA-F]+);)", "&amp;");
                            XmlDocument dmDoc = new XmlDocument();
                            dmDoc.LoadXml(safeXml);

                            // Collect layout info from dataModel if present
                            XmlNode prSetDoc = dmDoc.SelectSingleNode("//*[local-name()='pt'][@type='doc']/*[local-name()='prSet']");
                            if (prSetDoc != null && prSetDoc.Attributes != null)
                            {
                                if (prSetDoc.Attributes["loCatId"] != null) layoutInfo += " locat:" + prSetDoc.Attributes["loCatId"].Value.ToLower();
                                if (prSetDoc.Attributes["loTypeId"] != null) layoutInfo += " lotype:" + prSetDoc.Attributes["loTypeId"].Value.ToLower();
                            }

                            XmlNodeList ptList = dmDoc.SelectNodes("//*[local-name()='pt']");
                            if (ptList != null)
                            {
                                foreach (XmlNode pt in ptList)
                                {
                                    string ptType = (pt.Attributes != null && pt.Attributes["type"] != null) ? pt.Attributes["type"].Value : "";
                                    if (ptType == "doc" || ptType == "parTrans" || ptType == "sibTrans" || ptType == "pres")
                                        continue;

                                    List<string> runs = new List<string>();
                                    XmlNodeList tNodes = pt.SelectNodes(".//*[local-name()='t' and not(*)]");
                                    if (tNodes != null)
                                    {
                                        foreach (XmlNode tn in tNodes)
                                        {
                                            string txt = (tn.InnerText ?? "").Trim();
                                            if (!string.IsNullOrEmpty(txt) && !txt.Contains("<") && !txt.Contains("bodyPr") && !txt.Contains("lstStyle"))
                                            {
                                                runs.Add(txt);
                                            }
                                        }
                                    }

                                    string itemText = string.Join(" ", runs.ToArray()).Trim();
                                    if (string.IsNullOrEmpty(itemText))
                                    {
                                        XmlNode prSet = pt.SelectSingleNode(".//*[local-name()='prSet']");
                                        if (prSet != null && prSet.Attributes != null && prSet.Attributes["phldrT"] != null)
                                        {
                                            string ph = (prSet.Attributes["phldrT"].Value ?? "").Trim();
                                            if (!string.IsNullOrEmpty(ph) && !ph.Contains("<") && !ph.Contains("bodyPr"))
                                                itemText = ph;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(itemText))
                                    {
                                        nodeTexts.Add(itemText);
                                    }
                                }
                            }
                        }
                        catch { }

                        if (nodeTexts.Count == 0)
                        {
                            MatchCollection mc = Regex.Matches(dmXml, @"<[^:]*:?t[^>]*>([^<]+)</[^:]*:?t>", RegexOptions.Singleline);
                            foreach (Match m in mc)
                            {
                                string txt = m.Groups[1].Value.Trim();
                                txt = txt.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");
                                if (!string.IsNullOrEmpty(txt) && !txt.Contains("<") && !txt.Contains("bodyPr") && !txt.Contains("lstStyle") && !nodeTexts.Contains(txt))
                                {
                                    nodeTexts.Add(txt);
                                }
                            }
                        }
                    }
                    catch { }
                }

                // If still empty, provide clean default steps
                if (nodeTexts.Count == 0)
                {
                    nodeTexts.Add("Mục 1");
                    nodeTexts.Add("Mục 2");
                    nodeTexts.Add("Mục 3");
                    nodeTexts.Add("Mục 4");
                    nodeTexts.Add("Mục 5");
                }

                // Layout entry
                ZipArchiveEntry loEntry = null;
                if (!string.IsNullOrEmpty(loRelId) && relsToTarget != null && relsToTarget.ContainsKey(loRelId))
                {
                    loEntry = FindZipEntry(archive, relsToTarget[loRelId]);
                }
                if (loEntry == null && archive != null)
                {
                    foreach (var entry in archive.Entries)
                    {
                        string fn = entry.FullName.ToLower();
                        if (fn.Contains("diagrams/layout") || fn.Contains("diagramlayout"))
                        {
                            loEntry = entry;
                            break;
                        }
                    }
                }

                if (loEntry != null)
                {
                    layoutInfo += " " + loEntry.FullName.ToLower();
                    try
                    {
                        string loXml = "";
                        using (Stream s = loEntry.Open())
                        using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                        {
                            loXml = sr.ReadToEnd();
                        }
                        try
                        {
                            string safeLoXml = Regex.Replace(loXml, @"&(?!(amp|lt|gt|quot|apos|#\d+|#x[0-9a-fA-F]+);)", "&amp;");
                            XmlDocument loDoc = new XmlDocument();
                            loDoc.LoadXml(safeLoXml);

                            XmlNode titleNode = loDoc.SelectSingleNode("//*[local-name()='title']");
                            if (titleNode != null && titleNode.Attributes != null && titleNode.Attributes["val"] != null)
                            {
                                layoutInfo += " " + titleNode.Attributes["val"].Value.ToLower();
                            }
                            XmlNode layoutDefNode = loDoc.SelectSingleNode("//*[local-name()='layoutDef']");
                            if (layoutDefNode != null && layoutDefNode.Attributes != null && layoutDefNode.Attributes["uniqueId"] != null)
                            {
                                layoutInfo += " " + layoutDefNode.Attributes["uniqueId"].Value.ToLower();
                            }
                            XmlNodeList catNodes = loDoc.SelectNodes("//*[local-name()='cat']");
                            if (catNodes != null)
                            {
                                foreach (XmlNode cn in catNodes)
                                {
                                    if (cn.Attributes != null && cn.Attributes["type"] != null)
                                        layoutInfo += " cat:" + cn.Attributes["type"].Value.ToLower();
                                }
                            }
                        }
                        catch
                        {
                            Match mTitle = Regex.Match(loXml, @"<[^:]*:?title[^>]+val=""([^""]+)""");
                            if (mTitle.Success) layoutInfo += " " + mTitle.Groups[1].Value.ToLower();
                            Match mUid = Regex.Match(loXml, @"uniqueId=""([^""]+)""");
                            if (mUid.Success) layoutInfo += " " + mUid.Groups[1].Value.ToLower();
                        }
                    }
                    catch { }
                }

                string detectedType = "List_BasicBlock";
                if (layoutInfo.Contains("balance") || layoutInfo.Contains("counterbalance"))
                {
                    detectedType = "Rel_Balance";
                }
                else if (layoutInfo.Contains("funnel"))
                {
                    detectedType = "Rel_Funnel";
                }
                else if (layoutInfo.Contains("gear"))
                {
                    detectedType = "Process_Gear";
                }
                else if (layoutInfo.Contains("target"))
                {
                    detectedType = "Rel_BasicTarget";
                }
                else if (layoutInfo.Contains("venn"))
                {
                    detectedType = "Rel_BasicVenn";
                }
                else if (layoutInfo.Contains("pie"))
                {
                    detectedType = "Cycle_BasicPie";
                }
                else if (layoutInfo.Contains("chevron"))
                {
                    detectedType = "Process_BasicChevron";
                }
                else if (layoutInfo.Contains("timeline"))
                {
                    detectedType = "Process_BasicTimeline";
                }
                else if (layoutInfo.Contains("cycle") || layoutInfo.Contains("ring"))
                {
                    if (layoutInfo.Contains("pie")) detectedType = "Cycle_BasicPie";
                    else if (layoutInfo.Contains("radial")) detectedType = "Cycle_Radial";
                    else detectedType = "Rel_NondirectionalCycle";
                }
                else if (layoutInfo.Contains("default") || layoutInfo.Contains("list") || layoutInfo.Contains("block") || layoutInfo.Contains("bullet"))
                {
                    detectedType = "List_BasicBlock";
                }
                else if (layoutInfo.Contains("hprocess4") || layoutInfo.Contains("alternatingflow") || layoutInfo.Contains("alternating"))
                {
                    detectedType = "Process_AlternatingFlow";
                }
                else if (layoutInfo.Contains("process") || layoutInfo.Contains("step") || layoutInfo.Contains("linear") || layoutInfo.Contains("arrow"))
                {
                    detectedType = "Process_Basic";
                }
                else if (layoutInfo.Contains("hier") || layoutInfo.Contains("tree") || layoutInfo.Contains("org"))
                {
                    detectedType = "Hierarchy_Tree";
                }
                else if (layoutInfo.Contains("matrix") || layoutInfo.Contains("grid"))
                {
                    detectedType = "Matrix_Grid";
                }
                else if (layoutInfo.Contains("pyramid"))
                {
                    detectedType = "Pyramid_Basic";
                }
                else
                {
                    detectedType = "List_BasicBlock";
                }

                saType = detectedType;
                saTitle = nodeTexts.Count > 0 ? nodeTexts[0] : "Sơ đồ SmartArt";
                saText = (nodeTexts.Count > 0) ? string.Join("|", nodeTexts.ToArray()) : "";
                aspectRatioLock = OpenXmlSmartArtModel.IsCycleOrRadialLayout(saType);

                // Try Direct DrawingML high-fidelity rendering if drawing entry exists
                preRenderedPath = null;
                nodesMeta = "";
                if (drawEntry != null && dmEntry != null)
                {
                    try
                    {
                        preRenderedPath = RenderDirectDrawingMLDiagram(archive, drawEntry, dmEntry, themeEntry, targetW, targetH, out nodesMeta, out resolvedPalette, out origBoundsEmu);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("RenderDirectDrawingMLDiagram EXCEPTION: " + ex);
                    }
                }

                // Fallback for original bounds in EMU from wp:extent if empty
                if (origBoundsEmu == Rectangle.Empty || origBoundsEmu.Width <= 0 || origBoundsEmu.Height <= 0)
                {
                    XmlNode extNode = drawingNode.SelectSingleNode(".//wp:extent", nsmgr) ?? drawingNode.SelectSingleNode(".//*[local-name()='extent']");
                    if (extNode != null && extNode.Attributes != null && extNode.Attributes["cx"] != null && extNode.Attributes["cy"] != null)
                    {
                        int cxVal, cyVal;
                        if (int.TryParse(extNode.Attributes["cx"].Value, out cxVal) && int.TryParse(extNode.Attributes["cy"].Value, out cyVal) && cxVal > 0 && cyVal > 0)
                        {
                            origBoundsEmu = new Rectangle(0, 0, cxVal, cyVal);
                        }
                    }
                }

                // Fallback for resolved theme palette if not parsed
                if (resolvedPalette == null || resolvedPalette.Length == 0)
                {
                    resolvedPalette = new Color[]
                    {
                        Color.FromArgb(68, 114, 196),  // Office Blue (Accent 1)
                        Color.FromArgb(237, 125, 49),  // Accent 2
                        Color.FromArgb(165, 165, 165), // Accent 3
                        Color.FromArgb(255, 192, 0),   // Accent 4
                        Color.FromArgb(91, 155, 213),  // Accent 5
                        Color.FromArgb(112, 173, 71)   // Accent 6
                    };
                }

                Console.WriteLine("TryParseSmartArt: drawEntry=" + (drawEntry != null ? drawEntry.FullName : "NULL") + " dmEntry=" + (dmEntry != null ? dmEntry.FullName : "NULL") + " preRenderedPath=" + (preRenderedPath ?? "NULL"));

                return true;
            }

            preRenderedPath = null;
            return false;
        }

        private static bool TryParseShapeMetadata(XmlNode drawingNode, XmlNamespaceManager nsmgr, out string shapeType, out string textContent, out float adj1, out float adj2, out float adj3)
        {
            shapeType = "Rectangle";
            textContent = "";
            adj1 = 0.2f; adj2 = 0.4f; adj3 = 0.8f;
            if (drawingNode == null) return false;

            bool found = false;

            // 1. Try Legacy docPr descr mode
            XmlNode docPr = drawingNode.SelectSingleNode(".//wp:docPr", nsmgr) ?? drawingNode.SelectSingleNode(".//*[local-name()='docPr']");
            if (docPr != null && docPr.Attributes != null && docPr.Attributes["descr"] != null && docPr.Attributes["descr"].Value.StartsWith("Shape|"))
            {
                string descr = docPr.Attributes["descr"].Value;
                string[] parts = descr.Split('|');
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) shapeType = parts[1];
                if (parts.Length > 2) textContent = parts[2];
                if (parts.Length > 3) float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out adj1);
                if (parts.Length > 4) float.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out adj2);
                if (parts.Length > 5) float.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out adj3);
                return true;
            }

            // 2. Map standard OpenXML prstGeom
            XmlNode prstGeom = drawingNode.SelectSingleNode(".//*[local-name()='prstGeom']");
            if (prstGeom != null && prstGeom.Attributes != null && prstGeom.Attributes["prst"] != null)
            {
                string prst = prstGeom.Attributes["prst"].Value;
                shapeType = MapOpenXmlGeomToShapeRenderer(prst);
                found = true;
            }

            // 3. Extract text content if present
            XmlNode tNode = drawingNode.SelectSingleNode(".//*[local-name()='t']");
            if (tNode != null)
            {
                textContent = tNode.InnerText;
                found = true;
            }

            return found;
        }

        private static string MapOpenXmlGeomToShapeRenderer(string prst)
        {
            switch (prst)
            {
                case "rect": return "Rectangle";
                case "roundRect": return "RoundedRectangle";
                case "ellipse": return "Oval";
                case "triangle": return "Triangle";
                case "rtTriangle": return "RightTriangle";
                case "parallelogram": return "Parallelogram";
                case "trapezoid": return "Trapezoid";
                case "diamond": return "Diamond";
                case "pentagon": return "Pentagon";
                case "hexagon": return "Hexagon";
                case "octagon": return "Octagon";
                case "star5": return "Star5";
                case "star4": return "Star4";
                case "smileyFace": return "SmileFace";
                case "heart": return "Heart";
                case "sun": return "Sun";
                case "moon": return "Moon";
                case "lightningBolt": return "Lightning";
                case "can": return "Cylinder";
                case "cube": return "Cube";
                case "donut": return "Donut";
                case "blockArc": return "Arc";
                case "noSmoking": return "NotEqual";
                case "mathPlus": return "Plus";
                case "mathMinus": return "Minus";
                case "mathMultiply": return "Multiply";
                case "mathDivide": return "Divide";
                case "mathEqual": return "Equal";
                case "leftArrow": return "LeftArrow";
                case "rightArrow": return "RightArrow";
                case "upArrow": return "UpArrow";
                case "downArrow": return "DownArrow";
                case "leftRightArrow": return "LeftRightArrow";
                case "upDownArrow": return "UpDownArrow";
                case "quadArrow": return "QuadArrow";
                case "bentArrow": return "BentArrow";
                case "snip1Rect": return "SingleCornerSnipped";
                case "snip2SameRect": return "TopCornersSnipped";
                case "snip2DiagRect": return "SnippedAndRounded";
                case "round1Rect": return "RoundSingleCorner";
                case "round2SameRect": return "RoundSameSideCorner";
                case "round2DiagRect": return "RoundDiagonalCorner";
                default: return "Rectangle";
            }
        }

        private static void ExtractDrawingImage(XmlNode drawingNode, XmlNamespaceManager nsmgr, ZipArchive archive, Dictionary<string, string> relsToTarget, StringBuilder sb, List<FormattedRun> runs)
        {
            if (archive == null || drawingNode == null) return;
            try
            {
                // 1. Find blip or imagedata if available
                XmlNode blip = drawingNode.SelectSingleNode(".//a:blip", nsmgr)
                            ?? drawingNode.SelectSingleNode(".//*[local-name()='blip']")
                            ?? drawingNode.SelectSingleNode(".//*[local-name()='imagedata']");

                string rId = null;
                if (blip != null && blip.Attributes != null)
                {
                    if (blip.Attributes["r:embed"] != null) rId = blip.Attributes["r:embed"].Value;
                    else if (blip.Attributes["embed"] != null) rId = blip.Attributes["embed"].Value;
                    else if (blip.Attributes["r:id"] != null) rId = blip.Attributes["r:id"].Value;
                    else if (blip.Attributes["id"] != null) rId = blip.Attributes["id"].Value;
                    else if (blip.Attributes["r:link"] != null) rId = blip.Attributes["r:link"].Value;
                    else if (blip.Attributes["src"] != null) rId = blip.Attributes["src"].Value;
                }

                ZipArchiveEntry imgEntry = null;
                if (!string.IsNullOrEmpty(rId) && relsToTarget != null && relsToTarget.ContainsKey(rId))
                {
                    imgEntry = FindZipEntry(archive, relsToTarget[rId]);
                }
                if (imgEntry == null && relsToTarget != null)
                {
                    foreach (var kv in relsToTarget)
                    {
                        if (kv.Key == rId || kv.Value.Contains(rId ?? "___"))
                        {
                            imgEntry = FindZipEntry(archive, kv.Value);
                            if (imgEntry != null) break;
                        }
                    }
                }

                string tempFile = null;
                if (imgEntry != null)
                {
                    string ext = Path.GetExtension(imgEntry.FullName);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                    tempFile = Path.Combine(Path.GetTempPath(), "genoffice_img_" + Guid.NewGuid().ToString("N") + ext);
                    using (Stream s = imgEntry.Open())
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create))
                    {
                        s.CopyTo(fs);
                    }
                }

                // 2. Extract Width & Height from wp:extent or a:ext
                int w = 480, h = 270;
                int wrapMode = 0; // default: inline
                int x = 80, y = 160;
                int yRel = 0; // Default absolute (page/margin)

                // Check for w:dxaOrig and w:dyaOrig on <w:object>
                string dxaStr = null, dyaStr = null;
                if (drawingNode.Attributes != null)
                {
                    foreach (XmlAttribute attr in drawingNode.Attributes)
                    {
                        if (attr.LocalName == "dxaOrig") dxaStr = attr.Value;
                        if (attr.LocalName == "dyaOrig") dyaStr = attr.Value;
                    }
                }
                if (dxaStr != null && dyaStr != null)
                {
                    int dxa, dya;
                    if (int.TryParse(dxaStr, out dxa)) w = dxa / 15;
                    if (int.TryParse(dyaStr, out dya)) h = dya / 15;
                }

                XmlNode extent = drawingNode.SelectSingleNode(".//wp:extent", nsmgr)
                              ?? drawingNode.SelectSingleNode(".//*[local-name()='extent']")
                              ?? drawingNode.SelectSingleNode(".//*[local-name()='ext']");
                if (extent != null && extent.Attributes != null)
                {
                    long cx, cy;
                    if (extent.Attributes["cx"] != null && long.TryParse(extent.Attributes["cx"].Value, out cx))
                        w = (int)(cx / 9525L);
                    if (extent.Attributes["cy"] != null && long.TryParse(extent.Attributes["cy"].Value, out cy))
                        h = (int)(cy / 9525L);
                }
                else
                {
                    XmlNode vShape = drawingNode.SelectSingleNode(".//v:shape | .//*[local-name()='shape']", nsmgr) ?? drawingNode;
                    if (vShape != null && vShape.Attributes != null && vShape.Attributes["style"] != null)
                    {
                        string styleStr = vShape.Attributes["style"].Value;
                        string[] parts = styleStr.Split(';');
                        foreach (string part in parts)
                        {
                            string pTrim = part.Trim().ToLower();
                            if (pTrim.StartsWith("width:"))
                            {
                                string wStr = pTrim.Substring(6).Replace("pt", "").Replace("px", "").Trim();
                                float wPt;
                                if (float.TryParse(wStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out wPt))
                                    w = (int)(wPt * 1.333f);
                            }
                            else if (pTrim.StartsWith("height:"))
                            {
                                string hStr = pTrim.Substring(7).Replace("pt", "").Replace("px", "").Trim();
                                float hPt;
                                if (float.TryParse(hStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out hPt))
                                    h = (int)(hPt * 1.333f);
                            }
                            else if (pTrim.StartsWith("left:") || pTrim.StartsWith("margin-left:"))
                            {
                                string xStr = pTrim.Substring(pTrim.IndexOf(":") + 1).Replace("pt", "").Replace("px", "").Trim();
                                float xPt;
                                if (float.TryParse(xStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out xPt))
                                    x = (int)(xPt * 1.333f);
                            }
                            else if (pTrim.StartsWith("top:") || pTrim.StartsWith("margin-top:"))
                            {
                                string yStr = pTrim.Substring(pTrim.IndexOf(":") + 1).Replace("pt", "").Replace("px", "").Trim();
                                float yPt;
                                if (float.TryParse(yStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out yPt))
                                    y = (int)(yPt * 1.333f);
                            }
                        }
                    }
                }

                // Constrain maximum width to page printable area (602px)
                if (w > 602 && w > 0 && h > 0)
                {
                    float ratio = (float)h / w;
                    w = 602;
                    h = (int)(602 * ratio);
                }

                // Detect anchor (floating) vs inline
                bool isAnchor = drawingNode.SelectSingleNode(".//wp:anchor", nsmgr) != null
                             || drawingNode.SelectSingleNode(".//*[local-name()='anchor']") != null;

                if (isAnchor)
                {
                    if (drawingNode.SelectSingleNode(".//wp:wrapTopAndBottom", nsmgr) != null
                     || drawingNode.SelectSingleNode(".//*[local-name()='wrapTopAndBottom']") != null)
                        wrapMode = 4;
                    else if (drawingNode.SelectSingleNode(".//wp:wrapTight", nsmgr) != null
                          || drawingNode.SelectSingleNode(".//*[local-name()='wrapTight']") != null)
                        wrapMode = 2;
                    else if (drawingNode.SelectSingleNode(".//wp:wrapThrough", nsmgr) != null
                          || drawingNode.SelectSingleNode(".//*[local-name()='wrapThrough']") != null)
                        wrapMode = 3;
                    else if (drawingNode.SelectSingleNode(".//wp:wrapNone", nsmgr) != null
                          || drawingNode.SelectSingleNode(".//*[local-name()='wrapNone']") != null)
                        wrapMode = 6;
                    else if (drawingNode.SelectSingleNode(".//wp:wrapBehind", nsmgr) != null
                          || drawingNode.SelectSingleNode(".//*[local-name()='wrapBehind']") != null)
                        wrapMode = 5;
                    else
                        wrapMode = 1;

                    XmlNode posHNode = drawingNode.SelectSingleNode(".//wp:positionH/wp:posOffset", nsmgr)
                                    ?? drawingNode.SelectSingleNode(".//*[local-name()='positionH']/*[local-name()='posOffset']");
                    XmlNode posVParent = drawingNode.SelectSingleNode(".//wp:positionV", nsmgr) 
                                      ?? drawingNode.SelectSingleNode(".//*[local-name()='positionV']");
                    XmlNode posVNode = posVParent != null ? posVParent.SelectSingleNode(".//wp:posOffset", nsmgr) ?? posVParent.SelectSingleNode(".//*[local-name()='posOffset']") : null;
                    
                    if (posHNode != null)
                    {
                        long emu;
                        if (long.TryParse(posHNode.InnerText.Trim(), out emu))
                            x = Math.Max(0, (int)(emu / 9525L));
                    }
                    
                    if (posVParent != null && posVParent.Attributes != null)
                    {
                        XmlAttribute relAttr = posVParent.Attributes["relativeFrom"];
                        if (relAttr != null && (relAttr.Value == "paragraph" || relAttr.Value == "line"))
                        {
                            yRel = 1;
                        }
                    }
                    
                    if (posVNode != null)
                    {
                        long emu;
                        if (long.TryParse(posVNode.InnerText.Trim(), out emu))
                            y = Math.Max(0, (int)(emu / 9525L));
                    }
                }
                else
                {
                    if (w == 480 && h == 270 && !string.IsNullOrEmpty(tempFile))
                    {
                        try
                        {
                            using (var img = Image.FromFile(tempFile))
                            {
                                if (img.Width > 0 && img.Height > 0)
                                {
                                    w = img.Width;
                                    h = img.Height;
                                }
                            }
                        }
                        catch { }
                    }
                    if (h > 850 && w > 0 && h > 0)
                    {
                        float ratio = (float)w / h;
                        h = 850;
                        w = Math.Max(12, (int)(850 * ratio));
                    }
                }

                // 3. SmartArt Diagram Detection
                string saType = null, saTitle = null, saText = null, preRenderedPath = null, nodesMeta = null;
                bool saArLock = false;
                Rectangle saOrigBoundsEmu = Rectangle.Empty;
                Color[] saResolvedPalette = null;
                bool isSmartArt = TryParseSmartArtDiagram(drawingNode, nsmgr, archive, relsToTarget, w, h, out saType, out saTitle, out saText, out preRenderedPath, out nodesMeta, out saArLock, out saOrigBoundsEmu, out saResolvedPalette);

                // 4. Custom Vector Shape Detection
                string shpType = null, shpText = null;
                float shpA1 = 0.2f, shpA2 = 0.4f, shpA3 = 0.8f;
                bool isCustomShape = false;
                if (!isSmartArt)
                {
                    isCustomShape = TryParseShapeMetadata(drawingNode, nsmgr, out shpType, out shpText, out shpA1, out shpA2, out shpA3);
                    if (isCustomShape && !string.IsNullOrEmpty(tempFile) && string.IsNullOrEmpty(shpText) && (shpType == "Rectangle" || string.IsNullOrEmpty(shpType)))
                    {
                        isCustomShape = false;
                    }
                }

                string tag = null;
                if (isSmartArt)
                {
                    if (!string.IsNullOrEmpty(preRenderedPath) && File.Exists(preRenderedPath))
                    {
                        tempFile = preRenderedPath;
                    }
                    else if (string.IsNullOrEmpty(tempFile))
                    {
                        try
                        {
                            int saW = (w > 0) ? w : 440;
                            int saH = (h > 0) ? h : 200;
                            DocShapeObject saObj = new DocShapeObject("SmartArt", x, y, saW, saH);
                            saObj.SmartArtType = saType;
                            saObj.SmartArtTitle = saTitle;
                            saObj.TextContent = saText;
                            saObj.StylePresetIndex = 1;
                            saObj.AspectRatioLock = saArLock;
                            saObj.OriginalBoundsEmu = saOrigBoundsEmu;
                            saObj.ResolvedThemePalette = saResolvedPalette;
                            tempFile = Path.Combine(Path.GetTempPath(), "genoffice_sa_" + Guid.NewGuid().ToString("N") + ".png");
                            using (Bitmap saBmp = SmartArtRenderer.RenderToBitmap(saObj, saW, saH, 1.5f))
                            {
                                saBmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        catch { }
                    }

                    string safeTitle = (saTitle ?? "").Replace("[", "(").Replace("]", ")").Replace(":", " ").Trim();
                    string safeText = (saText ?? "").Replace("[", "(").Replace("]", ")").Replace(":", " ").Trim();
                    if (string.IsNullOrEmpty(safeTitle)) safeTitle = "Văn bản";
                    if (string.IsNullOrEmpty(safeText)) safeText = "Văn bản 1|Văn bản 2|Văn bản 3";

                    string shapeId = "sa_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    tag = string.Format("![SHAPE:ID={0}:TYPE=SmartArt:SMARTART={1}:TITLE={2}:TEXT={3}:STYLE=1:PATH={4}:NODES={5}:X={6}:Y={7}:W={8}:H={9}:WRAP={10}:PAGE=-1:YREL={11}:ARLOCK={12}:EMUX={13}:EMUY={14}:EMUW={15}:EMUH={16}]",
                        shapeId, saType ?? "Cycle_Radial", safeTitle, safeText, tempFile ?? "", nodesMeta ?? "", x, y, w, h, wrapMode, yRel,
                        saArLock ? "1" : "0", saOrigBoundsEmu.X, saOrigBoundsEmu.Y, saOrigBoundsEmu.Width, saOrigBoundsEmu.Height);

                    DocShapeTagProperties prop = new DocShapeTagProperties
                    {
                        SmartArtType = saType,
                        SmartArtTitle = safeTitle,
                        TextContent = safeText,
                        StylePresetIndex = 1,
                        NodesMeta = nodesMeta,
                        AspectRatioLock = saArLock,
                        OriginalBoundsEmu = saOrigBoundsEmu,
                        ResolvedThemePalette = saResolvedPalette
                    };
                    DocShapeObject.TagCache[shapeId] = prop;
                }
                else if (isCustomShape)
                {
                    if (string.IsNullOrEmpty(tempFile))
                    {
                        try
                        {
                            DocShapeObject shObj = new DocShapeObject(shpType, x, y, Math.Max(80, w), Math.Max(60, h));
                            shObj.TextContent = shpText;
                            shObj.AdjustValue1 = shpA1;
                            shObj.AdjustValue2 = shpA2;
                            shObj.AdjustValue3 = shpA3;
                            tempFile = Path.Combine(Path.GetTempPath(), "genoffice_shp_" + Guid.NewGuid().ToString("N") + ".png");
                            using (Bitmap shBmp = SmartArtRenderer.RenderShapeToBitmap(shObj, Math.Max(80, w), Math.Max(60, h), 1.5f))
                            {
                                shBmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        catch { }
                    }

                    string shapeId = "shp_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    tag = string.Format("![SHAPE:ID={0}:TYPE={1}:TEXT={2}:ADJ1={3}:ADJ2={4}:ADJ3={5}:PATH={6}:X={7}:Y={8}:W={9}:H={10}:WRAP={11}:PAGE=-1:YREL={12}]",
                        shapeId, shpType ?? "Rectangle", (shpText ?? "").Replace(":", " "), shpA1.ToString("F3", CultureInfo.InvariantCulture), shpA2.ToString("F3", CultureInfo.InvariantCulture), shpA3.ToString("F3", CultureInfo.InvariantCulture), tempFile ?? "", x, y, w, h, wrapMode, yRel);

                    DocShapeTagProperties prop = new DocShapeTagProperties
                    {
                        TextContent = shpText,
                        AdjustValue1 = shpA1,
                        AdjustValue2 = shpA2,
                        AdjustValue3 = shpA3
                    };
                    DocShapeObject.TagCache[shapeId] = prop;
                }
                else
                {
                    if (string.IsNullOrEmpty(tempFile))
                    {
                        // Cannot resolve regular image
                        return;
                    }

                    string shapeId = "img_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    tag = string.Format("![SHAPE:ID={0}:TYPE=Image:PATH={1}:X={2}:Y={3}:W={4}:H={5}:WRAP={6}:PAGE=-1:YREL={7}]",
                        shapeId, tempFile, x, y, w, h, wrapMode, yRel);
                }

                if (!string.IsNullOrEmpty(tag))
                {
                    if (isAnchor)
                    {
                        if (sb.Length > 0 && !sb.ToString().EndsWith("\n"))
                            sb.AppendLine();
                        int start = sb.Length;
                        sb.AppendLine(tag);
                        runs.Add(new FormattedRun(start, tag.Length + 1, "Times New Roman", 11f, false, false, false, false, Color.Black, Color.Transparent));
                    }
                    else
                    {
                        int start = sb.Length;
                        sb.Append(tag);
                        runs.Add(new FormattedRun(start, tag.Length, "Times New Roman", 11f, false, false, false, false, Color.Black, Color.Transparent));
                    }
                }
            }
            catch { }
        }

        private class WmfTextPiece
        {
            public int X;
            public int Y;
            public string Text;
        }

        private static string ExtractWmfMathText(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24) return "";
            List<WmfTextPiece> pieces = new List<WmfTextPiece>();
            int pos = 0;
            if (bytes.Length > 4 && bytes[0] == 0xD7 && bytes[1] == 0xCD && bytes[2] == 0xC6 && bytes[3] == 0x9A)
                pos = 22; // Skip Aldus placeable header
            if (pos + 18 < bytes.Length) pos += 18;

            while (pos < bytes.Length - 6)
            {
                int recordSizeWords = bytes[pos] | (bytes[pos + 1] << 8) | (bytes[pos + 2] << 16) | (bytes[pos + 3] << 24);
                int func = bytes[pos + 4] | (bytes[pos + 5] << 8);
                if (recordSizeWords <= 3) break;

                if (func == 0x0A32) // META_EXTTEXTOUT
                {
                    int y = (short)(bytes[pos + 6] | (bytes[pos + 7] << 8));
                    int x = (short)(bytes[pos + 8] | (bytes[pos + 9] << 8));
                    int strLen = bytes[pos + 10] | (bytes[pos + 11] << 8);
                    int fwOpts = bytes[pos + 12] | (bytes[pos + 13] << 8);
                    int strOffset = pos + 14;
                    if ((fwOpts & 0x0006) != 0) strOffset += 8;

                    if (strLen > 0 && strOffset + strLen <= bytes.Length)
                    {
                        string s = Encoding.Default.GetString(bytes, strOffset, strLen);
                        if (!string.IsNullOrEmpty(s))
                            pieces.Add(new WmfTextPiece { X = x, Y = y, Text = s });
                    }
                }
                else if (func == 0x0521) // META_TEXTOUT
                {
                    int strLen = bytes[pos + 6] | (bytes[pos + 7] << 8);
                    int y = (short)(bytes[pos + 8] | (bytes[pos + 9] << 8));
                    int x = (short)(bytes[pos + 10] | (bytes[pos + 11] << 8));
                    if (strLen > 0 && pos + 12 + strLen <= bytes.Length)
                    {
                        string s = Encoding.Default.GetString(bytes, pos + 12, strLen);
                        if (!string.IsNullOrEmpty(s))
                            pieces.Add(new WmfTextPiece { X = x, Y = y, Text = s });
                    }
                }
                pos += recordSizeWords * 2;
            }

            if (pieces.Count == 0) return "";
            pieces.Sort((a, b) => {
                if (Math.Abs(a.Y - b.Y) > 20) return a.Y.CompareTo(b.Y);
                return a.X.CompareTo(b.X);
            });
            StringBuilder sb = new StringBuilder();
            foreach (var p in pieces) sb.Append(p.Text);
            return sb.ToString().Trim();
        }

        private static string ExtractMathNodeText(XmlNode node, XmlNamespaceManager nsmgr)
        {
            if (node == null) return "";
            StringBuilder sb = new StringBuilder();
            ExtractMathNodeRecursive(node, nsmgr, sb);
            return sb.ToString().Trim();
        }

        private static string ToSuperscript(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            StringBuilder res = new StringBuilder();
            for (int idx = 0; idx < text.Length; idx++)
            {
                char c = text[idx];
                switch (c)
                {
                    case '0': res.Append('⁰'); break;
                    case '1': res.Append('¹'); break;
                    case '2': res.Append('²'); break;
                    case '3': res.Append('³'); break;
                    case '4': res.Append('⁴'); break;
                    case '5': res.Append('⁵'); break;
                    case '6': res.Append('⁶'); break;
                    case '7': res.Append('⁷'); break;
                    case '8': res.Append('⁸'); break;
                    case '9': res.Append('⁹'); break;
                    case '+': res.Append('⁺'); break;
                    case '-': case '−': res.Append('⁻'); break;
                    case '=': res.Append('⁼'); break;
                    case '(': res.Append('⁽'); break;
                    case ')': res.Append('⁾'); break;
                    case 'a': res.Append('ᵃ'); break;
                    case 'b': res.Append('ᵇ'); break;
                    case 'c': res.Append('ᶜ'); break;
                    case 'd': res.Append('ᵈ'); break;
                    case 'e': res.Append('ᵉ'); break;
                    case 'f': res.Append('ᶠ'); break;
                    case 'g': res.Append('ᵍ'); break;
                    case 'h': res.Append('ʰ'); break;
                    case 'i': res.Append('ⁱ'); break;
                    case 'j': res.Append('ʲ'); break;
                    case 'k': res.Append('ᵏ'); break;
                    case 'l': res.Append('ˡ'); break;
                    case 'm': res.Append('ᵐ'); break;
                    case 'n': res.Append('ⁿ'); break;
                    case 'o': res.Append('ᵒ'); break;
                    case 'p': res.Append('ᵖ'); break;
                    case 'r': res.Append('ʳ'); break;
                    case 's': res.Append('ˢ'); break;
                    case 't': res.Append('ᵗ'); break;
                    case 'u': res.Append('ᵘ'); break;
                    case 'v': res.Append('ᵛ'); break;
                    case 'w': res.Append('ʷ'); break;
                    case 'x': res.Append('ˣ'); break;
                    case 'y': res.Append('ʸ'); break;
                    case 'z': res.Append('ᶻ'); break;
                    case 'A': res.Append('ᴬ'); break;
                    case 'B': res.Append('ᴮ'); break;
                    case 'D': res.Append('ᴰ'); break;
                    case 'E': res.Append('ᴱ'); break;
                    case 'G': res.Append('ᴳ'); break;
                    case 'H': res.Append('ᴴ'); break;
                    case 'I': res.Append('ᴵ'); break;
                    case 'J': res.Append('ᴶ'); break;
                    case 'K': res.Append('ᴷ'); break;
                    case 'L': res.Append('ᴸ'); break;
                    case 'M': res.Append('ᴹ'); break;
                    case 'N': res.Append('ᴺ'); break;
                    case 'O': res.Append('ᴼ'); break;
                    case 'P': res.Append('ᴾ'); break;
                    case 'R': res.Append('ᴿ'); break;
                    case 'T': res.Append('ᵀ'); break;
                    case 'U': res.Append('ᵁ'); break;
                    case 'V': res.Append('ⱽ'); break;
                    case 'W': res.Append('ᵂ'); break;
                    case '∘': case '°': res.Append('°'); break;
                    default: res.Append(c); break;
                }
            }
            return res.ToString();
        }

        private static string ToSubscript(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            StringBuilder res = new StringBuilder();
            for (int idx = 0; idx < text.Length; idx++)
            {
                char c = text[idx];
                switch (c)
                {
                    case '0': res.Append('₀'); break;
                    case '1': res.Append('₁'); break;
                    case '2': res.Append('₂'); break;
                    case '3': res.Append('₃'); break;
                    case '4': res.Append('₄'); break;
                    case '5': res.Append('₅'); break;
                    case '6': res.Append('₆'); break;
                    case '7': res.Append('₇'); break;
                    case '8': res.Append('₈'); break;
                    case '9': res.Append('₉'); break;
                    case '+': res.Append('₊'); break;
                    case '-': case '−': res.Append('₋'); break;
                    case '=': res.Append('₌'); break;
                    case '(': res.Append('₍'); break;
                    case ')': res.Append('₎'); break;
                    case 'a': res.Append('ₐ'); break;
                    case 'e': res.Append('ₑ'); break;
                    case 'h': res.Append('ₕ'); break;
                    case 'i': res.Append('ᵢ'); break;
                    case 'j': res.Append('ⱼ'); break;
                    case 'k': res.Append('ₖ'); break;
                    case 'l': res.Append('ₗ'); break;
                    case 'm': res.Append('ₘ'); break;
                    case 'n': res.Append('ₙ'); break;
                    case 'o': res.Append('ₒ'); break;
                    case 'p': res.Append('ₚ'); break;
                    case 'r': res.Append('ᵣ'); break;
                    case 's': res.Append('ₛ'); break;
                    case 't': res.Append('ₜ'); break;
                    case 'u': res.Append('ᵤ'); break;
                    case 'v': res.Append('ᵥ'); break;
                    case 'x': res.Append('ₓ'); break;
                    default: res.Append(c); break;
                }
            }
            return res.ToString();
        }

        private static void ExtractMathNodeRecursive(XmlNode node, XmlNamespaceManager nsmgr, StringBuilder sb)
        {
            if (node == null) return;
            string localName = node.LocalName;

            if (localName == "t")
            {
                string tText = node.InnerText;
                if (!string.IsNullOrEmpty(tText))
                {
                    // Normalize common math symbol representations
                    tText = tText.Replace("<=", "≤").Replace(">=", "≥").Replace("!=", "≠").Replace("+-", "±").Replace("->", "→").Replace("...", "…");
                    sb.Append(tText);
                }
                return;
            }
            if (localName == "f") // Fraction
            {
                XmlNode num = node.SelectSingleNode("m:num | .//*[local-name()='num']", nsmgr);
                XmlNode den = node.SelectSingleNode("m:den | .//*[local-name()='den']", nsmgr);
                if (num != null && den != null)
                {
                    StringBuilder numSb = new StringBuilder();
                    ExtractMathNodeRecursive(num, nsmgr, numSb);
                    string nStr = numSb.ToString().Trim();

                    StringBuilder denSb = new StringBuilder();
                    ExtractMathNodeRecursive(den, nsmgr, denSb);
                    string dStr = denSb.ToString().Trim();

                    // Check vulgar fraction representations
                    if (nStr == "1" && dStr == "2") { sb.Append("½"); return; }
                    if (nStr == "1" && dStr == "3") { sb.Append("⅓"); return; }
                    if (nStr == "2" && dStr == "3") { sb.Append("⅔"); return; }
                    if (nStr == "1" && dStr == "4") { sb.Append("¼"); return; }
                    if (nStr == "3" && dStr == "4") { sb.Append("¾"); return; }
                    if (nStr == "1" && dStr == "5") { sb.Append("⅕"); return; }
                    if (nStr == "2" && dStr == "5") { sb.Append("⅖"); return; }
                    if (nStr == "3" && dStr == "5") { sb.Append("⅗"); return; }
                    if (nStr == "4" && dStr == "5") { sb.Append("⅘"); return; }
                    if (nStr == "1" && dStr == "6") { sb.Append("⅙"); return; }
                    if (nStr == "5" && dStr == "6") { sb.Append("⅚"); return; }
                    if (nStr == "1" && dStr == "8") { sb.Append("⅛"); return; }
                    if (nStr == "3" && dStr == "8") { sb.Append("⅜"); return; }
                    if (nStr == "5" && dStr == "8") { sb.Append("⅝"); return; }
                    if (nStr == "7" && dStr == "8") { sb.Append("⅞"); return; }

                    XmlNode fPr = node.SelectSingleNode("m:fPr | .//*[local-name()='fPr']", nsmgr);
                    string fType = "bar";
                    if (fPr != null)
                    {
                        XmlNode typeNode = fPr.SelectSingleNode("m:type | .//*[local-name()='type']", nsmgr);
                        if (typeNode != null && typeNode.Attributes != null && typeNode.Attributes["m:val"] != null)
                            fType = typeNode.Attributes["m:val"].Value;
                    }

                    if (fType == "nobar")
                    {
                        sb.Append(nStr + " " + dStr);
                        return;
                    }

                    bool complexNum = nStr.Contains(" ") || nStr.Contains("+") || nStr.Contains("-");
                    bool complexDen = dStr.Contains(" ") || dStr.Contains("+") || dStr.Contains("-");

                    if (complexNum && !nStr.StartsWith("(") && !nStr.EndsWith(")")) nStr = "(" + nStr + ")";
                    if (complexDen && !dStr.StartsWith("(") && !dStr.EndsWith(")")) dStr = "(" + dStr + ")";

                    sb.Append(nStr + "/" + dStr);
                    return;
                }
            }
            if (localName == "sSup") // Superscript
            {
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                XmlNode sup = node.SelectSingleNode("m:sup | .//*[local-name()='sup']", nsmgr);
                if (e != null) ExtractMathNodeRecursive(e, nsmgr, sb);
                if (sup != null)
                {
                    StringBuilder supSb = new StringBuilder();
                    ExtractMathNodeRecursive(sup, nsmgr, supSb);
                    sb.Append(ToSuperscript(supSb.ToString()));
                }
                return;
            }
            if (localName == "sSub") // Subscript
            {
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                XmlNode sub = node.SelectSingleNode("m:sub | .//*[local-name()='sub']", nsmgr);
                if (e != null) ExtractMathNodeRecursive(e, nsmgr, sb);
                if (sub != null)
                {
                    StringBuilder subSb = new StringBuilder();
                    ExtractMathNodeRecursive(sub, nsmgr, subSb);
                    sb.Append(ToSubscript(subSb.ToString()));
                }
                return;
            }
            if (localName == "sSubSup") // Subscript and Superscript
            {
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                XmlNode sub = node.SelectSingleNode("m:sub | .//*[local-name()='sub']", nsmgr);
                XmlNode sup = node.SelectSingleNode("m:sup | .//*[local-name()='sup']", nsmgr);
                if (e != null) ExtractMathNodeRecursive(e, nsmgr, sb);
                if (sub != null)
                {
                    StringBuilder subSb = new StringBuilder();
                    ExtractMathNodeRecursive(sub, nsmgr, subSb);
                    sb.Append(ToSubscript(subSb.ToString()));
                }
                if (sup != null)
                {
                    StringBuilder supSb = new StringBuilder();
                    ExtractMathNodeRecursive(sup, nsmgr, supSb);
                    sb.Append(ToSuperscript(supSb.ToString()));
                }
                return;
            }
            if (localName == "rad") // Radical / Root
            {
                XmlNode radPr = node.SelectSingleNode("m:radPr | .//*[local-name()='radPr']", nsmgr);
                bool degHide = false;
                if (radPr != null)
                {
                    XmlNode dh = radPr.SelectSingleNode("m:degHide | .//*[local-name()='degHide']", nsmgr);
                    if (dh != null)
                    {
                        if (dh.Attributes["m:val"] == null || dh.Attributes["m:val"].Value == "1" || dh.Attributes["m:val"].Value.Equals("on", StringComparison.OrdinalIgnoreCase) || dh.Attributes["m:val"].Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                            degHide = true;
                    }
                }

                XmlNode deg = node.SelectSingleNode("m:deg | .//*[local-name()='deg']", nsmgr);
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                string degStr = "";
                if (deg != null && !degHide)
                {
                    StringBuilder degSb = new StringBuilder();
                    ExtractMathNodeRecursive(deg, nsmgr, degSb);
                    degStr = degSb.ToString().Trim();
                }

                if (degHide || string.IsNullOrEmpty(degStr)) sb.Append("√");
                else if (degStr == "3") sb.Append("∛");
                else if (degStr == "4") sb.Append("∜");
                else sb.Append(ToSuperscript(degStr) + "√");

                if (e != null)
                {
                    StringBuilder eSb = new StringBuilder();
                    ExtractMathNodeRecursive(e, nsmgr, eSb);
                    string eStr = eSb.ToString().Trim();
                    bool complex = eStr.Contains("+") || eStr.Contains("-") || eStr.Contains(" ");
                    if (complex && !eStr.StartsWith("(") && !eStr.EndsWith(")"))
                        sb.Append("(" + eStr + ")");
                    else
                        sb.Append(eStr);
                }
                return;
            }
            if (localName == "d") // Delimiter
            {
                string begChr = "(";
                string endChr = ")";
                string sepChr = ",";
                XmlNode dPr = node.SelectSingleNode("m:dPr | .//*[local-name()='dPr']", nsmgr);
                if (dPr != null)
                {
                    XmlNode begNode = dPr.SelectSingleNode("m:begChr | .//*[local-name()='begChr']", nsmgr);
                    if (begNode != null && begNode.Attributes["m:val"] != null)
                        begChr = begNode.Attributes["m:val"].Value;
                    XmlNode endNode = dPr.SelectSingleNode("m:endChr | .//*[local-name()='endChr']", nsmgr);
                    if (endNode != null && endNode.Attributes["m:val"] != null)
                        endChr = endNode.Attributes["m:val"].Value;
                    XmlNode sepNode = dPr.SelectSingleNode("m:sepChr | .//*[local-name()='sepChr']", nsmgr);
                    if (sepNode != null && sepNode.Attributes["m:val"] != null)
                        sepChr = sepNode.Attributes["m:val"].Value;
                }
                sb.Append(begChr);
                XmlNodeList eList = node.SelectNodes("m:e | .//*[local-name()='e']", nsmgr);
                if (eList != null)
                {
                    for (int i = 0; i < eList.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(sepChr);
                            if (sepChr.Trim().Length > 0 && !sepChr.EndsWith(" ")) sb.Append(" ");
                        }
                        ExtractMathNodeRecursive(eList[i], nsmgr, sb);
                    }
                }
                sb.Append(endChr);
                return;
            }
            if (localName == "func") // Function (sin, cos, tan, etc.)
            {
                XmlNode fName = node.SelectSingleNode("m:fName | .//*[local-name()='fName']", nsmgr);
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                if (fName != null)
                {
                    ExtractMathNodeRecursive(fName, nsmgr, sb);
                    sb.Append(" ");
                }
                if (e != null) ExtractMathNodeRecursive(e, nsmgr, sb);
                return;
            }
            if (localName == "nary") // N-ary operator (integral, sum, product, etc.)
            {
                XmlNode chrNode = node.SelectSingleNode(".//m:naryPr/m:chr | .//*[local-name()='naryPr']/*[local-name()='chr']", nsmgr);
                string chr = (chrNode != null && chrNode.Attributes["m:val"] != null) ? chrNode.Attributes["m:val"].Value : "∫";
                sb.Append(chr);
                XmlNode sub = node.SelectSingleNode("m:sub | .//*[local-name()='sub']", nsmgr);
                XmlNode sup = node.SelectSingleNode("m:sup | .//*[local-name()='sup']", nsmgr);
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                if (sub != null)
                {
                    StringBuilder subSb = new StringBuilder();
                    ExtractMathNodeRecursive(sub, nsmgr, subSb);
                    sb.Append(ToSubscript(subSb.ToString()));
                }
                if (sup != null)
                {
                    StringBuilder supSb = new StringBuilder();
                    ExtractMathNodeRecursive(sup, nsmgr, supSb);
                    sb.Append(ToSuperscript(supSb.ToString()));
                }
                if (e != null)
                {
                    sb.Append(" ");
                    ExtractMathNodeRecursive(e, nsmgr, sb);
                }
                return;
            }
            if (localName == "m") // Matrix
            {
                sb.Append("[");
                XmlNodeList rows = node.SelectNodes("m:mr | .//*[local-name()='mr']", nsmgr);
                if (rows != null && rows.Count > 0)
                {
                    for (int r = 0; r < rows.Count; r++)
                    {
                        if (r > 0) sb.Append("; ");
                        XmlNodeList cells = rows[r].SelectNodes("m:e | .//*[local-name()='e']", nsmgr);
                        if (cells != null)
                        {
                            for (int c = 0; c < cells.Count; c++)
                            {
                                if (c > 0) sb.Append(", ");
                                ExtractMathNodeRecursive(cells[c], nsmgr, sb);
                            }
                        }
                    }
                }
                sb.Append("]");
                return;
            }
            if (localName == "limLow") // Limit lower
            {
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                XmlNode lim = node.SelectSingleNode("m:lim | .//*[local-name()='lim']", nsmgr);
                if (e != null) ExtractMathNodeRecursive(e, nsmgr, sb);
                if (lim != null)
                {
                    StringBuilder limSb = new StringBuilder();
                    ExtractMathNodeRecursive(lim, nsmgr, limSb);
                    sb.Append(ToSubscript(limSb.ToString()));
                }
                return;
            }
            if (localName == "limUpp") // Limit upper
            {
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                XmlNode lim = node.SelectSingleNode("m:lim | .//*[local-name()='lim']", nsmgr);
                if (e != null) ExtractMathNodeRecursive(e, nsmgr, sb);
                if (lim != null)
                {
                    StringBuilder limSb = new StringBuilder();
                    ExtractMathNodeRecursive(lim, nsmgr, limSb);
                    sb.Append(ToSuperscript(limSb.ToString()));
                }
                return;
            }
            if (localName == "acc") // Accent (hat, tilde, vec, etc.)
            {
                XmlNode accPr = node.SelectSingleNode("m:accPr | .//*[local-name()='accPr']", nsmgr);
                string chr = "^";
                if (accPr != null)
                {
                    XmlNode chrNode = accPr.SelectSingleNode("m:chr | .//*[local-name()='chr']", nsmgr);
                    if (chrNode != null && chrNode.Attributes["m:val"] != null)
                        chr = chrNode.Attributes["m:val"].Value;
                }
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                if (e != null)
                {
                    StringBuilder eSb = new StringBuilder();
                    ExtractMathNodeRecursive(e, nsmgr, eSb);
                    sb.Append(eSb.ToString() + chr);
                }
                return;
            }
            if (localName == "bar") // Overbar / Underbar
            {
                XmlNode barPr = node.SelectSingleNode("m:barPr | .//*[local-name()='barPr']", nsmgr);
                string pos = "top";
                if (barPr != null)
                {
                    XmlNode posNode = barPr.SelectSingleNode("m:pos | .//*[local-name()='pos']", nsmgr);
                    if (posNode != null && posNode.Attributes["m:val"] != null)
                        pos = posNode.Attributes["m:val"].Value;
                }
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                if (e != null)
                {
                    StringBuilder eSb = new StringBuilder();
                    ExtractMathNodeRecursive(e, nsmgr, eSb);
                    string comb = (pos == "bot") ? "\u0332" : "\u0305";
                    sb.Append(eSb.ToString() + comb);
                }
                return;
            }
            if (localName == "borderBox") // Border Box
            {
                XmlNode e = node.SelectSingleNode("m:e | .//*[local-name()='e']", nsmgr);
                if (e != null)
                {
                    sb.Append("[");
                    ExtractMathNodeRecursive(e, nsmgr, sb);
                    sb.Append("]");
                }
                return;
            }
            if (localName == "eqArr") // Equation array
            {
                XmlNodeList eList = node.SelectNodes("m:e | .//*[local-name()='e']", nsmgr);
                if (eList != null)
                {
                    for (int i = 0; i < eList.Count; i++)
                    {
                        if (i > 0) sb.Append("\n");
                        ExtractMathNodeRecursive(eList[i], nsmgr, sb);
                    }
                }
                return;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                ExtractMathNodeRecursive(child, nsmgr, sb);
            }
        }

        private static void ParseBorderAttributes(XmlNode borderNode, out bool hasBorder, out string style, out float widthPt, out Color color, Color defaultColor)
        {
            hasBorder = true;
            style = "single";
            widthPt = 0.5f;
            color = defaultColor;

            if (borderNode == null)
            {
                return;
            }

            XmlAttribute valAttr = borderNode.Attributes["val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"] ?? borderNode.Attributes["w:val"] ?? borderNode.Attributes["val"];
            if (valAttr == null && borderNode.Attributes != null)
            {
                for (int i = 0; i < borderNode.Attributes.Count; i++)
                {
                    if (string.Equals(borderNode.Attributes[i].LocalName, "val", StringComparison.OrdinalIgnoreCase))
                    {
                        valAttr = borderNode.Attributes[i];
                        break;
                    }
                }
            }
            if (valAttr != null)
            {
                string v = valAttr.Value;
                if (string.Equals(v, "nil", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "none", StringComparison.OrdinalIgnoreCase))
                {
                    hasBorder = false;
                    style = "none";
                }
                else
                {
                    hasBorder = true;
                    style = v;
                }
            }

            XmlAttribute szAttr = borderNode.Attributes["sz", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"] ?? borderNode.Attributes["w:sz"] ?? borderNode.Attributes["sz"];
            if (szAttr == null && borderNode.Attributes != null)
            {
                for (int i = 0; i < borderNode.Attributes.Count; i++)
                {
                    if (string.Equals(borderNode.Attributes[i].LocalName, "sz", StringComparison.OrdinalIgnoreCase))
                    {
                        szAttr = borderNode.Attributes[i];
                        break;
                    }
                }
            }
            if (szAttr != null)
            {
                int szVal;
                if (int.TryParse(szAttr.Value, out szVal) && szVal > 0)
                {
                    widthPt = szVal / 8.0f;
                }
            }

            XmlAttribute clrAttr = borderNode.Attributes["color", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"] ?? borderNode.Attributes["w:color"] ?? borderNode.Attributes["color"];
            if (clrAttr == null && borderNode.Attributes != null)
            {
                for (int i = 0; i < borderNode.Attributes.Count; i++)
                {
                    if (string.Equals(borderNode.Attributes[i].LocalName, "color", StringComparison.OrdinalIgnoreCase))
                    {
                        clrAttr = borderNode.Attributes[i];
                        break;
                    }
                }
            }
            if (clrAttr != null)
            {
                string cStr = clrAttr.Value.Trim();
                if (!string.IsNullOrEmpty(cStr))
                {
                    if (string.Equals(cStr, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        color = Color.Black;
                    }
                    else
                    {
                        try
                        {
                            if (!cStr.StartsWith("#") && (cStr.Length == 6 || cStr.Length == 8))
                                cStr = "#" + cStr;
                            color = ColorTranslator.FromHtml(cStr);
                        }
                        catch { }
                    }
                }
            }
        }

        private static void ParseTableNode(XmlNode tbl, XmlNamespaceManager nsmgr, StringBuilder sb, List<FormattedRun> runs, DocxMetadata meta, ZipArchive archive, Dictionary<string, string> relsToTarget, Dictionary<string, DocxStyleInfo> stylesDict = null, string docDefaultFont = "Times New Roman", float docDefaultFontSize = 11.0f)
        {
            XmlNodeList trList = tbl.SelectNodes(".//w:tr", nsmgr);
            if (trList == null || trList.Count == 0) return;

            // Parse Table Grid Column Widths (<w:tblGrid><w:gridCol w:w="..."/>...)
            List<int> gridColWidthsPt = new List<int>();
            XmlNode tblGrid = tbl.SelectSingleNode(".//w:tblGrid", nsmgr) ?? tbl.SelectSingleNode(".//*[local-name()='tblGrid']");
            if (tblGrid != null)
            {
                XmlNodeList gridCols = tblGrid.SelectNodes(".//w:gridCol | .//*[local-name()='gridCol']", nsmgr);
                if (gridCols != null)
                {
                    foreach (XmlNode gc in gridCols)
                    {
                        int gcw = 0;
                        if (gc.Attributes["w:w"] != null && int.TryParse(gc.Attributes["w:w"].Value, out gcw))
                        {
                            gridColWidthsPt.Add(gcw / 20); // 20 twips = 1 pt
                        }
                    }
                }
            }

            if (sb.Length > 0 && !sb.ToString().EndsWith("\n"))
                sb.Append('\n');
            int rowIndex = 0;
            int numCols = 0;

            ZenQOffice.UI.DocTableObject tableObj = new ZenQOffice.UI.DocTableObject();
            XmlNode tblPr = tbl.SelectSingleNode(".//w:tblPr", nsmgr) ?? tbl.SelectSingleNode(".//*[local-name()='tblPr']");
            if (tblPr != null)
            {
                XmlNode jc = tblPr.SelectSingleNode(".//w:jc", nsmgr) ?? tblPr.SelectSingleNode(".//*[local-name()='jc']");
                if (jc != null && jc.Attributes["w:val"] != null)
                {
                    string jcVal = jc.Attributes["w:val"].Value.ToLower();
                    if (jcVal == "center") tableObj.Alignment = 1;
                    else if (jcVal == "right") tableObj.Alignment = 2;
                    else tableObj.Alignment = 0;
                }
                XmlNode tblInd = tblPr.SelectSingleNode(".//w:tblInd", nsmgr) ?? tblPr.SelectSingleNode(".//*[local-name()='tblInd']");
                if (tblInd != null && tblInd.Attributes["w:w"] != null)
                {
                    int indDxa;
                    if (int.TryParse(tblInd.Attributes["w:w"].Value, out indDxa))
                    {
                        tableObj.IndentPt = (int)(indDxa / 20f);
                    }
                }
                XmlNode tblWNode = tblPr.SelectSingleNode(".//w:tblW", nsmgr) ?? tblPr.SelectSingleNode(".//*[local-name()='tblW']");
                if (tblWNode != null)
                {
                    if (tblWNode.Attributes["w:type"] != null) tableObj.WidthType = tblWNode.Attributes["w:type"].Value;
                    if (tblWNode.Attributes["w:w"] != null)
                    {
                        int tw;
                        if (int.TryParse(tblWNode.Attributes["w:w"].Value, out tw) && tw > 0)
                        {
                            if (tableObj.WidthType == "dxa") tableObj.WidthPt = (int)(tw / 20f);
                            else if (tableObj.WidthType == "pct") tableObj.WidthPct = tw;
                            else tableObj.WidthPt = (int)(tw / 20f);
                        }
                    }
                }
            }

            XmlNode tblBorders = tblPr != null ? (tblPr.SelectSingleNode(".//w:tblBorders", nsmgr) ?? tblPr.SelectSingleNode(".//*[local-name()='tblBorders']")) : null;
            Color defBorderColor = Color.Black;

            bool tblHasTop = true, tblHasBottom = true, tblHasLeft = true, tblHasRight = true;
            bool tblHasInsideH = true, tblHasInsideV = true;
            string tblTopStyle = "single", tblBottomStyle = "single", tblLeftStyle = "single", tblRightStyle = "single", tblIHStyle = "single", tblIVStyle = "single";
            float tblTopW = 0.5f, tblBottomW = 0.5f, tblLeftW = 0.5f, tblRightW = 0.5f, tblIHW = 0.5f, tblIVW = 0.5f;
            Color tblTopClr = defBorderColor, tblBottomClr = defBorderColor, tblLeftClr = defBorderColor, tblRightClr = defBorderColor, tblIHClr = defBorderColor, tblIVClr = defBorderColor;

            if (tblBorders != null)
            {
                XmlNode bTop = tblBorders.SelectSingleNode(".//w:top", nsmgr) ?? tblBorders.SelectSingleNode(".//*[local-name()='top']");
                ParseBorderAttributes(bTop, out tblHasTop, out tblTopStyle, out tblTopW, out tblTopClr, defBorderColor);

                XmlNode bBottom = tblBorders.SelectSingleNode(".//w:bottom", nsmgr) ?? tblBorders.SelectSingleNode(".//*[local-name()='bottom']");
                ParseBorderAttributes(bBottom, out tblHasBottom, out tblBottomStyle, out tblBottomW, out tblBottomClr, defBorderColor);

                XmlNode bLeft = tblBorders.SelectSingleNode(".//w:left", nsmgr) ?? tblBorders.SelectSingleNode(".//*[local-name()='left']");
                ParseBorderAttributes(bLeft, out tblHasLeft, out tblLeftStyle, out tblLeftW, out tblLeftClr, defBorderColor);

                XmlNode bRight = tblBorders.SelectSingleNode(".//w:right", nsmgr) ?? tblBorders.SelectSingleNode(".//*[local-name()='right']");
                ParseBorderAttributes(bRight, out tblHasRight, out tblRightStyle, out tblRightW, out tblRightClr, defBorderColor);

                XmlNode bIH = tblBorders.SelectSingleNode(".//w:insideH", nsmgr) ?? tblBorders.SelectSingleNode(".//*[local-name()='insideH']");
                ParseBorderAttributes(bIH, out tblHasInsideH, out tblIHStyle, out tblIHW, out tblIHClr, defBorderColor);

                XmlNode bIV = tblBorders.SelectSingleNode(".//w:insideV", nsmgr) ?? tblBorders.SelectSingleNode(".//*[local-name()='insideV']");
                ParseBorderAttributes(bIV, out tblHasInsideV, out tblIVStyle, out tblIVW, out tblIVClr, defBorderColor);
            }

            tableObj.HasBorderTop = tblHasTop;
            tableObj.BorderTopStyle = tblTopStyle;
            tableObj.BorderTopWidthPt = tblTopW;
            tableObj.BorderTopColor = tblTopClr;

            tableObj.HasBorderBottom = tblHasBottom;
            tableObj.BorderBottomStyle = tblBottomStyle;
            tableObj.BorderBottomWidthPt = tblBottomW;
            tableObj.BorderBottomColor = tblBottomClr;

            tableObj.HasBorderLeft = tblHasLeft;
            tableObj.BorderLeftStyle = tblLeftStyle;
            tableObj.BorderLeftWidthPt = tblLeftW;
            tableObj.BorderLeftColor = tblLeftClr;

            tableObj.HasBorderRight = tblHasRight;
            tableObj.BorderRightStyle = tblRightStyle;
            tableObj.BorderRightWidthPt = tblRightW;
            tableObj.BorderRightColor = tblRightClr;

            tableObj.HasBorderInsideH = tblHasInsideH;
            tableObj.BorderInsideHStyle = tblIHStyle;
            tableObj.BorderInsideHWidthPt = tblIHW;
            tableObj.BorderInsideHColor = tblIHClr;

            tableObj.HasBorderInsideV = tblHasInsideV;
            tableObj.BorderInsideVStyle = tblIVStyle;
            tableObj.BorderInsideVWidthPt = tblIVW;
            tableObj.BorderInsideVColor = tblIVClr;

            int totalTrCount = trList.Count;
            for (int rIdx = 0; rIdx < totalTrCount; rIdx++)
            {
                XmlNode tr = trList[rIdx];
                XmlNodeList tcList = tr.SelectNodes(".//w:tc", nsmgr);
                if (tcList == null || tcList.Count == 0) continue;

                ZenQOffice.UI.DocTableRow tableRow = new ZenQOffice.UI.DocTableRow();
                XmlNode trPr = tr.SelectSingleNode(".//w:trPr", nsmgr) ?? tr.SelectSingleNode(".//*[local-name()='trPr']");
                if (trPr != null)
                {
                    XmlNode trHeight = trPr.SelectSingleNode(".//w:trHeight", nsmgr) ?? trPr.SelectSingleNode(".//*[local-name()='trHeight']");
                    if (trHeight != null)
                    {
                        var hVal = trHeight.Attributes["w:val"] ?? trHeight.Attributes["val"];
                        if (hVal != null)
                        {
                            int hTwips;
                            if (int.TryParse(hVal.Value, out hTwips) && hTwips > 0)
                            {
                                tableRow.HeightPt = (int)(hTwips / 20f);
                            }
                        }
                    }
                }

                List<string> cellTexts = new List<string>();

                for (int tcIdx = 0; tcIdx < tcList.Count; tcIdx++)
                {
                    XmlNode tc = tcList[tcIdx];
                    ZenQOffice.UI.DocTableCell cellObj = new ZenQOffice.UI.DocTableCell();

                    cellObj.HasBorderTop = (rIdx == 0) ? tblHasTop : tblHasInsideH;
                    cellObj.BorderTopStyle = (rIdx == 0) ? tblTopStyle : tblIHStyle;
                    cellObj.BorderTopWidthPt = (rIdx == 0) ? tblTopW : tblIHW;
                    cellObj.BorderTopColor = (rIdx == 0) ? tblTopClr : tblIHClr;

                    cellObj.HasBorderBottom = (rIdx == totalTrCount - 1) ? tblHasBottom : tblHasInsideH;
                    cellObj.BorderBottomStyle = (rIdx == totalTrCount - 1) ? tblBottomStyle : tblIHStyle;
                    cellObj.BorderBottomWidthPt = (rIdx == totalTrCount - 1) ? tblBottomW : tblIHW;
                    cellObj.BorderBottomColor = (rIdx == totalTrCount - 1) ? tblBottomClr : tblIHClr;

                    cellObj.HasBorderLeft = (tcIdx == 0) ? tblHasLeft : tblHasInsideV;
                    cellObj.BorderLeftStyle = (tcIdx == 0) ? tblLeftStyle : tblIVStyle;
                    cellObj.BorderLeftWidthPt = (tcIdx == 0) ? tblLeftW : tblIVW;
                    cellObj.BorderLeftColor = (tcIdx == 0) ? tblLeftClr : tblIVClr;

                    cellObj.HasBorderRight = (tcIdx == tcList.Count - 1) ? tblHasRight : tblHasInsideV;
                    cellObj.BorderRightStyle = (tcIdx == tcList.Count - 1) ? tblRightStyle : tblIVStyle;
                    cellObj.BorderRightWidthPt = (tcIdx == tcList.Count - 1) ? tblRightW : tblIVW;
                    cellObj.BorderRightColor = (tcIdx == tcList.Count - 1) ? tblRightClr : tblIVClr;

                    XmlNode tcPr = tc.SelectSingleNode(".//w:tcPr", nsmgr) ?? tc.SelectSingleNode(".//*[local-name()='tcPr']");
                    if (tcPr != null)
                    {
                        XmlNode tcBorders = tcPr.SelectSingleNode(".//w:tcBorders", nsmgr) ?? tcPr.SelectSingleNode(".//*[local-name()='tcBorders']");
                        if (tcBorders != null)
                        {
                            XmlNode topB = tcBorders.SelectSingleNode(".//w:top", nsmgr) ?? tcBorders.SelectSingleNode(".//*[local-name()='top']");
                            if (topB != null)
                            {
                                bool hB; string st; float wPt; Color clr;
                                ParseBorderAttributes(topB, out hB, out st, out wPt, out clr, cellObj.BorderTopColor);
                                cellObj.HasBorderTop = hB;
                                cellObj.BorderTopStyle = st;
                                cellObj.BorderTopWidthPt = wPt;
                                cellObj.BorderTopColor = clr;
                            }
                            XmlNode bottomB = tcBorders.SelectSingleNode(".//w:bottom", nsmgr) ?? tcBorders.SelectSingleNode(".//*[local-name()='bottom']");
                            if (bottomB != null)
                            {
                                bool hB; string st; float wPt; Color clr;
                                ParseBorderAttributes(bottomB, out hB, out st, out wPt, out clr, cellObj.BorderBottomColor);
                                cellObj.HasBorderBottom = hB;
                                cellObj.BorderBottomStyle = st;
                                cellObj.BorderBottomWidthPt = wPt;
                                cellObj.BorderBottomColor = clr;
                            }
                            XmlNode leftB = tcBorders.SelectSingleNode(".//w:left", nsmgr) ?? tcBorders.SelectSingleNode(".//*[local-name()='left']");
                            if (leftB != null)
                            {
                                bool hB; string st; float wPt; Color clr;
                                ParseBorderAttributes(leftB, out hB, out st, out wPt, out clr, cellObj.BorderLeftColor);
                                cellObj.HasBorderLeft = hB;
                                cellObj.BorderLeftStyle = st;
                                cellObj.BorderLeftWidthPt = wPt;
                                cellObj.BorderLeftColor = clr;
                            }
                            XmlNode rightB = tcBorders.SelectSingleNode(".//w:right", nsmgr) ?? tcBorders.SelectSingleNode(".//*[local-name()='right']");
                            if (rightB != null)
                            {
                                bool hB; string st; float wPt; Color clr;
                                ParseBorderAttributes(rightB, out hB, out st, out wPt, out clr, cellObj.BorderRightColor);
                                cellObj.HasBorderRight = hB;
                                cellObj.BorderRightStyle = st;
                                cellObj.BorderRightWidthPt = wPt;
                                cellObj.BorderRightColor = clr;
                            }
                        }

                        XmlNode vAlign = tcPr.SelectSingleNode(".//w:vAlign", nsmgr) ?? tcPr.SelectSingleNode(".//*[local-name()='vAlign']");
                        if (vAlign != null)
                        {
                            var v = vAlign.Attributes["w:val"] ?? vAlign.Attributes["val"];
                            if (v != null)
                            {
                                string vStr = v.Value.ToLower();
                                if (vStr == "center") cellObj.VerticalAlignment = 1;
                                else if (vStr == "bottom") cellObj.VerticalAlignment = 2;
                                else cellObj.VerticalAlignment = 0;
                            }
                        }
                    }

                    // Parse Background Color
                    XmlNode shd = tc.SelectSingleNode(".//w:shd", nsmgr);
                    if (shd != null && shd.Attributes["w:fill"] != null)
                    {
                        string fillVal = shd.Attributes["w:fill"].Value;
                        if (fillVal != "auto" && fillVal.Length == 6)
                        {
                            try { cellObj.BackgroundColor = ColorTranslator.FromHtml("#" + fillVal); } catch { }
                        }
                    }

                    // Parse Column Width (w:tcW) or fall back to w:tblGrid
                    XmlNode tcw = tc.SelectSingleNode(".//w:tcW", nsmgr);
                    if (tcw != null && tcw.Attributes["w:w"] != null)
                    {
                        int wtw;
                        if (int.TryParse(tcw.Attributes["w:w"].Value, out wtw) && wtw > 0)
                        {
                            cellObj.WidthPt = (int)(wtw / 20f); // 20 twips = 1 pt
                        }
                    }
                    if (cellObj.WidthPt <= 0 && tcIdx < gridColWidthsPt.Count)
                    {
                        cellObj.WidthPt = gridColWidthsPt[tcIdx];
                    }

                    // Default font and size from document defaults
                    cellObj.FontName = docDefaultFont;
                    cellObj.FontSize = docDefaultFontSize;

                    // Parse Cell Alignment & Formatting
                    XmlNode tcJc = tc.SelectSingleNode(".//w:p/w:pPr/w:jc", nsmgr);
                    if (tcJc != null && tcJc.Attributes["w:val"] != null)
                    {
                        string jcVal = tcJc.Attributes["w:val"].Value.ToLower();
                        if (jcVal == "center") cellObj.Alignment = 1;
                        else if (jcVal == "right") cellObj.Alignment = 2;
                        else if (jcVal == "both" || jcVal == "justify") cellObj.Alignment = 3;
                        else cellObj.Alignment = 0;
                    }

                    StringBuilder cellSb = new StringBuilder();
                    XmlNodeList pList = tc.SelectNodes(".//w:p", nsmgr);
                    if (pList != null)
                    {
                        foreach (XmlNode p in pList)
                        {
                            StringBuilder pSb = new StringBuilder();
                            foreach (XmlNode child in p.ChildNodes)
                            {
                                if (child.LocalName == "r")
                                {
                                    foreach (XmlNode rChild in child.ChildNodes)
                                    {
                                        if (rChild.LocalName == "t") pSb.Append(rChild.InnerText);
                                        else if (rChild.LocalName == "sym")
                                        {
                                            XmlAttribute charAttr = rChild.Attributes["w:char"];
                                            if (charAttr != null)
                                            {
                                                try { pSb.Append((char)Convert.ToInt32(charAttr.Value, 16)); }
                                                catch { pSb.Append(rChild.InnerText); }
                                            }
                                        }
                                        else if (rChild.LocalName == "br" || rChild.LocalName == "cr") pSb.Append("\n");
                                    }
                                }
                                else if (child.LocalName == "oMath" || child.LocalName == "oMathPara")
                                {
                                    ExtractMathNodeRecursive(child, nsmgr, pSb);
                                }
                            }

                            // Extract drawing and object nodes within table cells
                            XmlNodeList inlineNodes = p.SelectNodes(".//w:drawing | .//w:pict | .//w:object | .//*[local-name()='drawing'] | .//*[local-name()='pict'] | .//*[local-name()='object']", nsmgr);
                            if (inlineNodes != null)
                            {
                                foreach (XmlNode inlineNode in inlineNodes)
                                {
                                    List<FormattedRun> dummyRuns = new List<FormattedRun>();
                                    ExtractDrawingImage(inlineNode, nsmgr, archive, relsToTarget, pSb, dummyRuns);
                                }
                            }
                            string pStr = pSb.ToString();
                            if (pStr.TrimEnd().Length > 0 || pStr.Contains("\n"))
                            {
                                if (cellSb.Length > 0) cellSb.Append("\n");
                                cellSb.Append(pStr.TrimEnd());

                                // Extract paragraph formatting for this paragraph starting from document defaults
                                string pFont = docDefaultFont;
                                float pFontSize = docDefaultFontSize;
                                float pWidthScale = 1.0f;
                                float pCharSpacingPt = 0f;
                                bool pBold = false;
                                bool pItalic = false;
                                Color pColor = Color.Black;
                                int pAlign = 0;
                                float pSpacingBefore = 0;
                                float pSpacingAfter = 0;
                                float pLineSpacing = 1.0f;

                                XmlNode pPr = p.SelectSingleNode("w:pPr", nsmgr);
                                if (pPr != null)
                                {
                                    XmlNode pStyle = pPr.SelectSingleNode("w:pStyle", nsmgr);
                                    if (pStyle != null && pStyle.Attributes["w:val"] != null && stylesDict != null)
                                    {
                                        string sVal = pStyle.Attributes["w:val"].Value;
                                        DocxStyleInfo sInfo;
                                        if (stylesDict.TryGetValue(sVal, out sInfo))
                                        {
                                            if (!string.IsNullOrEmpty(sInfo.FontName)) pFont = sInfo.FontName;
                                            if (sInfo.FontSize > 0) pFontSize = sInfo.FontSize;
                                            if (sInfo.Bold.HasValue) pBold = sInfo.Bold.Value;
                                            if (sInfo.Italic.HasValue) pItalic = sInfo.Italic.Value;
                                            if (sInfo.Color.HasValue) pColor = sInfo.Color.Value;
                                            if (sInfo.CharWidthScale.HasValue) pWidthScale = sInfo.CharWidthScale.Value;
                                            if (sInfo.CharSpacingPt.HasValue) pCharSpacingPt = sInfo.CharSpacingPt.Value;
                                            if (sInfo.Alignment.HasValue) pAlign = sInfo.Alignment.Value;
                                            if (sInfo.SpacingBefore.HasValue) pSpacingBefore = sInfo.SpacingBefore.Value;
                                            if (sInfo.SpacingAfter.HasValue) pSpacingAfter = sInfo.SpacingAfter.Value;
                                            if (sInfo.LineSpacing.HasValue) pLineSpacing = sInfo.LineSpacing.Value;
                                        }
                                    }

                                    XmlNode jc = pPr.SelectSingleNode("w:jc", nsmgr);
                                    if (jc != null && jc.Attributes["w:val"] != null)
                                    {
                                        string jcVal = jc.Attributes["w:val"].Value.ToLower();
                                        if (jcVal == "center") pAlign = 1;
                                        else if (jcVal == "right") pAlign = 2;
                                        else if (jcVal == "both" || jcVal == "justify") pAlign = 3;
                                        else pAlign = 0;
                                    }

                                    XmlNode sp = pPr.SelectSingleNode("w:spacing", nsmgr);
                                    if (sp != null)
                                    {
                                        if (sp.Attributes["w:before"] != null)
                                        {
                                            int val;
                                            if (int.TryParse(sp.Attributes["w:before"].Value, out val)) pSpacingBefore = val / 20.0f;
                                        }
                                        if (sp.Attributes["w:after"] != null)
                                        {
                                            int val;
                                            if (int.TryParse(sp.Attributes["w:after"].Value, out val)) pSpacingAfter = val / 20.0f;
                                        }
                                        if (sp.Attributes["w:line"] != null)
                                        {
                                            int val;
                                            if (int.TryParse(sp.Attributes["w:line"].Value, out val) && val > 0)
                                            {
                                                string lRule = sp.Attributes["w:lineRule"] != null ? sp.Attributes["w:lineRule"].Value.ToLower() : "auto";
                                                if (lRule == "auto") pLineSpacing = val / 240.0f;
                                                else pLineSpacing = (val / 20.0f) / 12.0f;
                                            }
                                        }
                                    }

                                    XmlNode pRPr = pPr.SelectSingleNode("w:rPr", nsmgr);
                                    if (pRPr != null)
                                    {
                                        XmlNode b = pRPr.SelectSingleNode("w:b", nsmgr);
                                        if (b != null)
                                        {
                                            var v = b.Attributes["w:val"];
                                            pBold = (v == null || (v.Value != "0" && v.Value.ToLower() != "false"));
                                        }
                                        XmlNode i = pRPr.SelectSingleNode("w:i", nsmgr);
                                        if (i != null)
                                        {
                                            var v = i.Attributes["w:val"];
                                            pItalic = (v == null || (v.Value != "0" && v.Value.ToLower() != "false"));
                                        }
                                        XmlNode sz = pRPr.SelectSingleNode("w:sz", nsmgr) ?? pRPr.SelectSingleNode("w:szCs", nsmgr);
                                        if (sz != null && sz.Attributes["w:val"] != null)
                                        {
                                            int halfPts;
                                            if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                                                pFontSize = halfPts / 2.0f;
                                        }
                                        XmlNode wNode = pRPr.SelectSingleNode("w:w", nsmgr);
                                        if (wNode != null && wNode.Attributes["w:val"] != null)
                                        {
                                            int wPct;
                                            if (int.TryParse(wNode.Attributes["w:val"].Value, out wPct) && wPct > 10 && wPct <= 200)
                                                pWidthScale = wPct / 100.0f;
                                        }
                                        XmlNode spcNode = pRPr.SelectSingleNode("w:spacing", nsmgr);
                                        if (spcNode != null && spcNode.Attributes["w:val"] != null)
                                        {
                                            int spcTwips;
                                            if (int.TryParse(spcNode.Attributes["w:val"].Value, out spcTwips))
                                                pCharSpacingPt = spcTwips / 20.0f;
                                        }
                                        XmlNode rF = pRPr.SelectSingleNode("w:rFonts", nsmgr);
                                        if (rF != null)
                                        {
                                            if (rF.Attributes["w:ascii"] != null && !string.IsNullOrEmpty(rF.Attributes["w:ascii"].Value)) pFont = rF.Attributes["w:ascii"].Value;
                                            else if (rF.Attributes["w:hAnsi"] != null && !string.IsNullOrEmpty(rF.Attributes["w:hAnsi"].Value)) pFont = rF.Attributes["w:hAnsi"].Value;
                                            else if (rF.Attributes["w:cs"] != null && !string.IsNullOrEmpty(rF.Attributes["w:cs"].Value)) pFont = rF.Attributes["w:cs"].Value;
                                        }
                                    }
                                }

                                // Check runs in paragraph p using character-length weighting so a short bold label doesn't bleed into the entire paragraph
                                string basePFont = pFont;
                                float basePFontSize = pFontSize;
                                float basePWidthScale = pWidthScale;
                                float basePCharSpacingPt = pCharSpacingPt;
                                bool basePBold = pBold;
                                bool basePItalic = pItalic;
                                Color basePColor = pColor;

                                int totalChars = 0;
                                int boldChars = 0;
                                int italicChars = 0;
                                int bestRunWeight = -1;
                                string domFont = basePFont;
                                float domFontSize = ApplyOpenXmlCharacterScaling(basePFontSize, basePWidthScale, basePCharSpacingPt);
                                Color domColor = basePColor;

                                XmlNodeList rList = p.SelectNodes(".//w:r", nsmgr);
                                if (rList != null && rList.Count > 0)
                                {
                                    foreach (XmlNode rNode in rList)
                                    {
                                        int rLen = 0;
                                        XmlNodeList rtNodes = rNode.SelectNodes(".//w:t", nsmgr);
                                        if (rtNodes != null)
                                        {
                                            foreach (XmlNode rtn in rtNodes)
                                            {
                                                if (!string.IsNullOrWhiteSpace(rtn.InnerText))
                                                    rLen += rtn.InnerText.Trim().Length;
                                            }
                                        }
                                        if (rLen <= 0) continue;

                                        string rFont = basePFont;
                                        float rSize = basePFontSize;
                                        float rWScale = basePWidthScale;
                                        float rCSpacing = basePCharSpacingPt;
                                        bool rBold = basePBold;
                                        bool rItalic = basePItalic;
                                        Color rColor = basePColor;

                                        XmlNode rPr = rNode.SelectSingleNode("w:rPr", nsmgr);
                                        if (rPr != null)
                                        {
                                            XmlNode rStyleNode = rPr.SelectSingleNode("w:rStyle", nsmgr);
                                            if (rStyleNode != null && rStyleNode.Attributes["w:val"] != null && stylesDict != null)
                                            {
                                                DocxStyleInfo crStyle;
                                                if (stylesDict.TryGetValue(rStyleNode.Attributes["w:val"].Value, out crStyle))
                                                {
                                                    if (!string.IsNullOrEmpty(crStyle.FontName)) rFont = crStyle.FontName;
                                                    if (crStyle.FontSize > 0) rSize = crStyle.FontSize;
                                                    if (crStyle.Bold.HasValue) rBold = crStyle.Bold.Value;
                                                    if (crStyle.Italic.HasValue) rItalic = crStyle.Italic.Value;
                                                    if (crStyle.Color.HasValue) rColor = crStyle.Color.Value;
                                                    if (crStyle.CharWidthScale.HasValue) rWScale = crStyle.CharWidthScale.Value;
                                                    if (crStyle.CharSpacingPt.HasValue) rCSpacing = crStyle.CharSpacingPt.Value;
                                                }
                                            }
                                            XmlNode b = rPr.SelectSingleNode("w:b", nsmgr);
                                            if (b != null)
                                            {
                                                var v = b.Attributes["w:val"];
                                                rBold = (v == null || (v.Value != "0" && v.Value.ToLower() != "false" && v.Value.ToLower() != "none"));
                                            }
                                            XmlNode i = rPr.SelectSingleNode("w:i", nsmgr);
                                            if (i != null)
                                            {
                                                var v = i.Attributes["w:val"];
                                                rItalic = (v == null || (v.Value != "0" && v.Value.ToLower() != "false" && v.Value.ToLower() != "none"));
                                            }
                                            XmlNode sz = rPr.SelectSingleNode("w:sz", nsmgr) ?? rPr.SelectSingleNode("w:szCs", nsmgr);
                                            if (sz != null && sz.Attributes["w:val"] != null)
                                            {
                                                int halfPts;
                                                if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                                                    rSize = halfPts / 2.0f;
                                            }
                                            XmlNode wNode = rPr.SelectSingleNode("w:w", nsmgr);
                                            if (wNode != null && wNode.Attributes["w:val"] != null)
                                            {
                                                int wPct;
                                                if (int.TryParse(wNode.Attributes["w:val"].Value, out wPct) && wPct > 10 && wPct <= 200)
                                                    rWScale = wPct / 100.0f;
                                            }
                                            XmlNode spcNode = rPr.SelectSingleNode("w:spacing", nsmgr);
                                            if (spcNode != null && spcNode.Attributes["w:val"] != null)
                                            {
                                                int spcTwips;
                                                if (int.TryParse(spcNode.Attributes["w:val"].Value, out spcTwips))
                                                    rCSpacing = spcTwips / 20.0f;
                                            }
                                            XmlNode rF = rPr.SelectSingleNode("w:rFonts", nsmgr);
                                            if (rF != null)
                                            {
                                                if (rF.Attributes["w:ascii"] != null && !string.IsNullOrEmpty(rF.Attributes["w:ascii"].Value)) rFont = rF.Attributes["w:ascii"].Value;
                                                else if (rF.Attributes["w:hAnsi"] != null && !string.IsNullOrEmpty(rF.Attributes["w:hAnsi"].Value)) rFont = rF.Attributes["w:hAnsi"].Value;
                                                else if (rF.Attributes["w:cs"] != null && !string.IsNullOrEmpty(rF.Attributes["w:cs"].Value)) rFont = rF.Attributes["w:cs"].Value;
                                            }
                                            XmlNode col = rPr.SelectSingleNode("w:color", nsmgr);
                                            if (col != null && col.Attributes["w:val"] != null)
                                            {
                                                string hex = col.Attributes["w:val"].Value;
                                                if (hex.Length == 6)
                                                {
                                                    try { rColor = ColorTranslator.FromHtml("#" + hex); } catch { }
                                                }
                                            }
                                        }

                                        float effRSize = ApplyOpenXmlCharacterScaling(rSize, rWScale, rCSpacing);
                                        totalChars += rLen;
                                        if (rBold) boldChars += rLen;
                                        if (rItalic) italicChars += rLen;
                                        if (rLen > bestRunWeight)
                                        {
                                            bestRunWeight = rLen;
                                            domFont = rFont;
                                            domFontSize = effRSize;
                                            domColor = rColor;
                                        }
                                    }
                                }

                                if (totalChars > 0)
                                {
                                    pFont = domFont;
                                    pFontSize = domFontSize;
                                    pColor = domColor;
                                    pBold = basePBold || (boldChars * 2 > totalChars);
                                    pItalic = basePItalic || (italicChars * 2 > totalChars);
                                }
                                else
                                {
                                    pFontSize = ApplyOpenXmlCharacterScaling(pFontSize, pWidthScale, pCharSpacingPt);
                                }

                                DocCellParagraph cellPara = new DocCellParagraph
                                {
                                    Text = pStr.TrimEnd(),
                                    FontName = pFont,
                                    FontSize = pFontSize,
                                    Bold = pBold,
                                    Italic = pItalic,
                                    TextColor = pColor,
                                    Alignment = pAlign,
                                    SpacingBeforePt = pSpacingBefore,
                                    SpacingAfterPt = pSpacingAfter,
                                    LineSpacing = pLineSpacing
                                };
                                cellObj.Paragraphs.Add(cellPara);

                                if (cellObj.Paragraphs.Count == 1)
                                {
                                    cellObj.FontName = pFont;
                                    cellObj.FontSize = pFontSize;
                                    cellObj.Bold = pBold;
                                    cellObj.Italic = pItalic;
                                    cellObj.TextColor = pColor;
                                    cellObj.Alignment = pAlign;
                                    cellObj.SpacingBeforePt = pSpacingBefore;
                                    cellObj.SpacingAfterPt = pSpacingAfter;
                                    cellObj.LineSpacing = pLineSpacing;
                                }
                            }
                        }
                    }
                    string fullCellText = cellSb.ToString().Trim().Replace("|", "/");
                    cellObj.Text = fullCellText;
                    tableRow.Cells.Add(cellObj);

                    string singleLineStr = fullCellText.Replace("\r", " ").Replace("\n", " ");
                    cellTexts.Add(string.IsNullOrEmpty(singleLineStr) ? " " : singleLineStr);
                }

                tableObj.Rows.Add(tableRow);

                numCols = Math.Max(numCols, cellTexts.Count);
                string rowLine = "| " + string.Join(" | ", cellTexts.ToArray()) + " |";
                int start = sb.Length;
                sb.AppendLine(rowLine);
                float rowFontSize = (tableRow.Cells.Count > 0 && tableRow.Cells[0].FontSize > 0) ? tableRow.Cells[0].FontSize : docDefaultFontSize;
                string rowFontName = (tableRow.Cells.Count > 0 && !string.IsNullOrEmpty(tableRow.Cells[0].FontName)) ? tableRow.Cells[0].FontName : docDefaultFont;
                bool rowItalic = (tableRow.Cells.Count > 0) && tableRow.Cells[0].Italic;
                runs.Add(new FormattedRun(start, rowLine.Length + 2, rowFontName, rowFontSize, (rowIndex == 0), rowItalic, false, false, Color.Black, Color.Transparent));

                // After header row, output markdown table separator
                if (rowIndex == 0)
                {
                    List<string> seps = new List<string>();
                    for (int c = 0; c < numCols; c++) seps.Add("-------");
                    string sepLine = "| " + string.Join(" | ", seps.ToArray()) + " |";
                    int sepStart = sb.Length;
                    sb.AppendLine(sepLine);
                    runs.Add(new FormattedRun(sepStart, sepLine.Length + 2, rowFontName, rowFontSize, false, false, false, false, Color.Gray, Color.Transparent));
                }
                rowIndex++;
            }
            meta.Tables.Add(tableObj);
        }

        private static Color MapHighlightColor(string val)
        {
            switch (val.ToLower())
            {
                case "yellow": return Color.Yellow;
                case "green": return Color.Lime;
                case "cyan": return Color.Cyan;
                case "magenta": return Color.Magenta;
                case "blue": return Color.Blue;
                case "red": return Color.Red;
                case "darkblue": return Color.Navy;
                case "darkcyan": return Color.DarkCyan;
                case "darkgreen": return Color.DarkGreen;
                case "darkmagenta": return Color.DarkMagenta;
                case "darkred": return Color.DarkRed;
                case "darkyellow": return Color.DarkGoldenrod;
                case "darkgray": return Color.Gray;
                case "lightgray": return Color.LightGray;
                case "black": return Color.Black;
                default: return Color.Yellow;
            }
        }

        private class DocxMediaItem
        {
            public string RelId;
            public string TargetPath;
            public byte[] Data;
            public string Extension;
            public string ContentType;
        }

        /// <summary>
        /// Saves document as standard OpenXML (.docx) ZIP Package compatible with MS Word, LibreOffice, etc.
        /// </summary>
        public static bool SaveDocxFile(
            string filepath,
            string textContent,
            List<FormattedRun> runs = null,
            bool isLandscape = false,
            int marginMode = 0,
            string headerText = "",
            string footerText = "",
            bool showPageNumbers = false,
            List<DocShapeObject> shapes = null,
            List<DocChartObject> charts = null,
            Dictionary<int, int> paraAlignments = null,
            Dictionary<int, float> paraLineSpacings = null,
            Dictionary<int, float> paraSpacingsBefore = null,
            Dictionary<int, float> paraSpacingsAfter = null,
            Dictionary<int, ParagraphBorderInfo> paraBorders = null,
            List<ZenQOffice.UI.DocTableObject> tables = null,
            Dictionary<int, float> paraFirstLineIndents = null,
            Dictionary<int, float> paraLeftIndents = null,
            Dictionary<int, float> paraRightIndents = null,
            Dictionary<int, float> paraHangingIndents = null)
        {
            try
            {
                if (File.Exists(filepath)) File.Delete(filepath);

                List<DocxMediaItem> mediaItems = new List<DocxMediaItem>();
                int mediaCount = 0;

                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // 1. [Content_Types].xml
                    var ctEntry = archive.CreateEntry("[Content_Types].xml");
                    using (StreamWriter sw = new StreamWriter(ctEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                        sw.WriteLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                        sw.WriteLine("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                        sw.WriteLine("  <Default Extension=\"png\" ContentType=\"image/png\"/>");
                        sw.WriteLine("  <Default Extension=\"jpeg\" ContentType=\"image/jpeg\"/>");
                        sw.WriteLine("  <Default Extension=\"jpg\" ContentType=\"image/jpeg\"/>");
                        sw.WriteLine("  <Default Extension=\"gif\" ContentType=\"image/gif\"/>");
                        sw.WriteLine("  <Default Extension=\"bmp\" ContentType=\"image/bmp\"/>");
                        sw.WriteLine("  <Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>");
                        if (!string.IsNullOrEmpty(headerText)) sw.WriteLine("  <Override PartName=\"/word/header1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml\"/>");
                        if (!string.IsNullOrEmpty(footerText) || showPageNumbers) sw.WriteLine("  <Override PartName=\"/word/footer1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml\"/>");
                        sw.WriteLine("</Types>");
                    }

                    // 2. _rels/.rels
                    var relsEntry = archive.CreateEntry("_rels/.rels");
                    using (StreamWriter sw = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                        sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>");
                        sw.WriteLine("</Relationships>");
                    }

                    // 4. word/header1.xml (if any)
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        var hEntry = archive.CreateEntry("word/header1.xml");
                        using (StreamWriter sw = new StreamWriter(hEntry.Open(), Encoding.UTF8))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
                            sw.WriteLine("  <w:p><w:r><w:rPr><w:rFonts w:ascii=\"Times New Roman\"/><w:sz w:val=\"18\"/></w:rPr><w:t>" + EscapeXml(headerText) + "</w:t></w:r></w:p>");
                            sw.WriteLine("</w:hdr>");
                        }
                    }

                    // 5. word/footer1.xml (if any)
                    if (!string.IsNullOrEmpty(footerText) || showPageNumbers)
                    {
                        var fEntry = archive.CreateEntry("word/footer1.xml");
                        using (StreamWriter sw = new StreamWriter(fEntry.Open(), Encoding.UTF8))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
                            sw.WriteLine("  <w:p>");
                            sw.WriteLine("    <w:pPr><w:jc w:val=\"center\"/></w:pPr>");
                            if (!string.IsNullOrEmpty(footerText))
                            {
                                sw.WriteLine("    <w:r><w:rPr><w:rFonts w:ascii=\"Times New Roman\"/><w:sz w:val=\"18\"/></w:rPr><w:t>" + EscapeXml(footerText) + " </w:t></w:r>");
                            }
                            if (showPageNumbers)
                            {
                                sw.WriteLine("    <w:fldSimple w:instr=\"PAGE\"/>");
                            }
                            sw.WriteLine("  </w:p>");
                            sw.WriteLine("</w:ftr>");
                        }
                    }

                    // 6. word/styles.xml
                    var stylesEntry = archive.CreateEntry("word/styles.xml");
                    using (StreamWriter sw = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
                        sw.WriteLine("  <w:docDefaults>");
                        sw.WriteLine("    <w:rPrDefault>");
                        sw.WriteLine("      <w:rPr>");
                        sw.WriteLine("        <w:rFonts w:ascii=\"Times New Roman\" w:hAnsi=\"Times New Roman\" w:eastAsia=\"Times New Roman\" w:cs=\"Times New Roman\"/>");
                        sw.WriteLine("        <w:sz w:val=\"24\"/>");
                        sw.WriteLine("        <w:szCs w:val=\"24\"/>");
                        sw.WriteLine("      </w:rPr>");
                        sw.WriteLine("    </w:rPrDefault>");
                        sw.WriteLine("  </w:docDefaults>");
                        sw.WriteLine("</w:styles>");
                    }

                    // 7. word/document.xml
                    var docEntry = archive.CreateEntry("word/document.xml");
                    using (StreamWriter sw = new StreamWriter(docEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                        sw.WriteLine("  <w:body>");

                        string[] paras = (textContent ?? "").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                        int charOffset = 0;
                        string pendingFloatingDrawing = null;
                        int tblIdx = 0;

                        for (int p = 0; p < paras.Length; p++)
                        {
                            string para = paras[p];

                            // 7.1 Page Break
                            if (para.Trim() == "[--- PAGE BREAK ---]" || para.Contains("TRANG TRẮNG MỚI"))
                            {
                                sw.WriteLine("    <w:p><w:r><w:br w:type=\"page\"/></w:r></w:p>");
                                charOffset += para.Length + 1;
                                continue;
                            }

                            if (para.Trim() == "[--- COVER PAGE START ---]" || para.Trim() == "[--- COVER PAGE END ---]")
                            {
                                charOffset += para.Length + 1;
                                continue;
                            }

                            // 7.2 Tables
                            if (!string.IsNullOrWhiteSpace(para) && para.Trim().StartsWith("|"))
                            {
                                List<string> tableLines = new List<string>();
                                int tblTotalLen = 0;
                                while (p < paras.Length && !string.IsNullOrWhiteSpace(paras[p]) && paras[p].Trim().StartsWith("|"))
                                {
                                    tableLines.Add(paras[p]);
                                    tblTotalLen += paras[p].Length + 1;
                                    p++;
                                }
                                p--;
                                charOffset += tblTotalLen;

                                List<List<string>> rows = new List<List<string>>();
                                foreach (string tLine in tableLines)
                                {
                                    if (tLine.Contains("---") || tLine.Contains("===")) continue;
                                    string[] rawCells = tLine.Split('|');
                                    List<string> rowCells = new List<string>();
                                    for (int c = 1; c < rawCells.Length - 1; c++)
                                    {
                                        rowCells.Add(rawCells[c].Trim());
                                    }
                                    if (rowCells.Count > 0) rows.Add(rowCells);
                                }

                                if (rows.Count > 0)
                                {
                                    ZenQOffice.UI.DocTableObject docTbl = (tables != null && tblIdx < tables.Count) ? tables[tblIdx] : null;
                                    tblIdx++;

                                    string tblWVal = (docTbl != null && docTbl.WidthPt > 0) ? (docTbl.WidthPt * 20).ToString() : "0";
                                    string tblWType = (docTbl != null && !string.IsNullOrEmpty(docTbl.WidthType)) ? docTbl.WidthType : "auto";
                                    string tblJc = "center";
                                    if (docTbl != null)
                                    {
                                        if (docTbl.Alignment == 0) tblJc = "left";
                                        else if (docTbl.Alignment == 2) tblJc = "right";
                                    }
                                    string bColor = (docTbl != null && docTbl.BorderColor != Color.Empty && docTbl.BorderColor != Color.Transparent) 
                                        ? string.Format("{0:X2}{1:X2}{2:X2}", docTbl.BorderColor.R, docTbl.BorderColor.G, docTbl.BorderColor.B) 
                                        : "CBD5E1";

                                    sw.WriteLine("    <w:tbl>");
                                    sw.WriteLine("      <w:tblPr>");
                                    sw.WriteLine(string.Format("        <w:tblW w:w=\"{0}\" w:type=\"{1}\"/>", tblWVal, tblWType));
                                    sw.WriteLine(string.Format("        <w:jc w:val=\"{0}\"/>", tblJc));
                                    sw.WriteLine("        <w:tblBorders>");
                                    sw.WriteLine(string.Format("          <w:top w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"{0}\"/>", bColor));
                                    sw.WriteLine(string.Format("          <w:left w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"{0}\"/>", bColor));
                                    sw.WriteLine(string.Format("          <w:bottom w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"{0}\"/>", bColor));
                                    sw.WriteLine(string.Format("          <w:right w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"{0}\"/>", bColor));
                                    sw.WriteLine(string.Format("          <w:insideH w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"{0}\"/>", bColor));
                                    sw.WriteLine(string.Format("          <w:insideV w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"{0}\"/>", bColor));
                                    sw.WriteLine("        </w:tblBorders>");
                                    sw.WriteLine("        <w:tblCellMar>");
                                    sw.WriteLine("          <w:top w:w=\"120\" w:type=\"dxa\"/>");
                                    sw.WriteLine("          <w:bottom w:w=\"120\" w:type=\"dxa\"/>");
                                    sw.WriteLine("          <w:left w:w=\"160\" w:type=\"dxa\"/>");
                                    sw.WriteLine("          <w:right w:w=\"160\" w:type=\"dxa\"/>");
                                    sw.WriteLine("        </w:tblCellMar>");
                                    sw.WriteLine("      </w:tblPr>");
                                    for (int rIdx = 0; rIdx < rows.Count; rIdx++)
                                    {
                                        var row = rows[rIdx];
                                        bool isHeaderRow = (rIdx == 0);
                                        int rowHPt = (docTbl != null && rIdx < docTbl.Rows.Count) ? docTbl.Rows[rIdx].HeightPt : 0;

                                        sw.WriteLine("      <w:tr>");
                                        if (isHeaderRow || rowHPt > 0)
                                        {
                                            sw.WriteLine("        <w:trPr>");
                                            if (isHeaderRow) sw.WriteLine("          <w:tblHeader/>");
                                            if (rowHPt > 0) sw.WriteLine(string.Format("          <w:trHeight w:val=\"{0}\" w:hRule=\"atLeast\"/>", rowHPt * 20));
                                            sw.WriteLine("        </w:trPr>");
                                        }

                                        for (int cIdx = 0; cIdx < row.Count; cIdx++)
                                        {
                                            string cell = row[cIdx];
                                            ZenQOffice.UI.DocTableCell cObj = (docTbl != null && rIdx < docTbl.Rows.Count && cIdx < docTbl.Rows[rIdx].Cells.Count) ? docTbl.Rows[rIdx].Cells[cIdx] : null;

                                            string shdHex = null;
                                            if (cObj != null && cObj.BackgroundColor != Color.Transparent && cObj.BackgroundColor != Color.Empty && cObj.BackgroundColor.A > 0)
                                                shdHex = string.Format("{0:X2}{1:X2}{2:X2}", cObj.BackgroundColor.R, cObj.BackgroundColor.G, cObj.BackgroundColor.B);
                                            else
                                                shdHex = isHeaderRow ? "2563EB" : (rIdx % 2 == 0 ? "F8FAFC" : "FFFFFF");

                                            string textHex = null;
                                            if (cObj != null && cObj.TextColor != Color.Empty && cObj.TextColor != Color.Transparent)
                                                textHex = string.Format("{0:X2}{1:X2}{2:X2}", cObj.TextColor.R, cObj.TextColor.G, cObj.TextColor.B);
                                            else
                                                textHex = isHeaderRow ? "FFFFFF" : "0F172A";

                                            string fontName = (cObj != null && !string.IsNullOrEmpty(cObj.FontName)) ? cObj.FontName : "Segoe UI";
                                            int halfPtSize = (cObj != null && cObj.FontSize > 0) ? (int)Math.Round(cObj.FontSize * 2) : (isHeaderRow ? 22 : 21);
                                            bool isBold = (cObj != null) ? cObj.Bold : isHeaderRow;

                                            string alignVal = "left";
                                            if (cObj != null)
                                            {
                                                if (cObj.Alignment == 1) alignVal = "center";
                                                else if (cObj.Alignment == 2) alignVal = "right";
                                                else alignVal = "left";
                                            }
                                            else
                                            {
                                                decimal dummyD;
                                                alignVal = isHeaderRow ? "center" : (cIdx == 0 ? "left" : (decimal.TryParse(cell.Replace(",", "").Replace("%", "").Trim(), out dummyD) ? "right" : "left"));
                                            }

                                            string vAlign = "center";
                                            if (cObj != null)
                                            {
                                                if (cObj.VerticalAlignment == 0) vAlign = "top";
                                                else if (cObj.VerticalAlignment == 2) vAlign = "bottom";
                                            }

                                            sw.WriteLine("        <w:tc>");
                                            sw.WriteLine("          <w:tcPr>");
                                            if (cObj != null && cObj.WidthPt > 0)
                                            {
                                                sw.WriteLine(string.Format("            <w:tcW w:w=\"{0}\" w:type=\"dxa\"/>", cObj.WidthPt * 20));
                                            }
                                            sw.WriteLine(string.Format("            <w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{0}\"/>", shdHex));
                                            sw.WriteLine(string.Format("            <w:vAlign w:val=\"{0}\"/>", vAlign));
                                            sw.WriteLine("          </w:tcPr>");
                                            sw.WriteLine("          <w:p>");
                                            sw.WriteLine(string.Format("            <w:pPr><w:jc w:val=\"{0}\"/></w:pPr>", alignVal));
                                            sw.WriteLine("            <w:r>");
                                            sw.WriteLine("              <w:rPr>");
                                            sw.WriteLine(string.Format("                <w:rFonts w:ascii=\"{0}\" w:hAnsi=\"{0}\" w:cs=\"{0}\"/>", EscapeXml(fontName)));
                                            sw.WriteLine(string.Format("                <w:sz w:val=\"{0}\"/>", halfPtSize));
                                            sw.WriteLine(string.Format("                <w:szCs w:val=\"{0}\"/>", halfPtSize));
                                            if (isBold) { sw.WriteLine("                <w:b/>"); sw.WriteLine("                <w:bCs/>"); }
                                            sw.WriteLine(string.Format("                <w:color w:val=\"{0}\"/>", textHex));
                                            sw.WriteLine("              </w:rPr>");
                                            string cellText = (cObj != null && !string.IsNullOrEmpty(cObj.Text)) ? cObj.Text : cell;
                                            sw.WriteLine("              <w:t xml:space=\"preserve\">" + EscapeXml(cellText) + "</w:t>");
                                            sw.WriteLine("            </w:r>");
                                            sw.WriteLine("          </w:p>");
                                            sw.WriteLine("        </w:tc>");
                                        }
                                        sw.WriteLine("      </w:tr>");
                                    }
                                    sw.WriteLine("    </w:tbl>");
                                    continue;
                                }
                            }

                            // 7.3 Real Image / Shape Export (DrawingML)
                            if (para.StartsWith("![SHAPE:") || para.StartsWith("![IMAGE:") || (para.StartsWith("![IMAGE](") && para.EndsWith(")")) || para.Contains("HÌNH KHỐI VECTOR") || para.Contains("HÌNH ÁNH DỰ ÁN"))
                            {
                                string shapeId = "";
                                string shapeType = "Image";
                                string imgPath = "";
                                int tagX = -1, tagY = -1, tagW = -1, tagH = -1, tagWrap = -1, tagPage = -1, tagStyle = -1, tagYRel = 0;

                                DocShapeObject.ParseShapeTag(para, out shapeId, out shapeType, out imgPath, out tagX, out tagY, out tagW, out tagH, out tagWrap, out tagPage, out tagStyle, out tagYRel);

                                DocShapeObject shapeObj = (shapes != null) ? shapes.Find(s => s.Id == shapeId) : null;
                                if (shapeObj == null && !string.IsNullOrEmpty(shapeId))
                                {
                                    shapeObj = new DocShapeObject(shapeType, tagX > 0 ? tagX : 50, tagY > 0 ? tagY : 50, tagW > 0 ? tagW : 240, tagH > 0 ? tagH : 160);
                                    shapeObj.Id = shapeId;
                                    shapeObj.ApplyCachedTagMetadata();
                                }
                                if (shapeObj != null && !string.IsNullOrEmpty(shapeObj.ImagePath) && string.IsNullOrEmpty(imgPath))
                                {
                                    imgPath = shapeObj.ImagePath;
                                }

                                byte[] imgBytes = null;
                                string ext = "png";

                                if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
                                {
                                    try
                                    {
                                        imgBytes = File.ReadAllBytes(imgPath);
                                        ext = Path.GetExtension(imgPath).TrimStart('.').ToLower();
                                        if (string.IsNullOrEmpty(ext)) ext = "png";
                                    }
                                    catch { }
                                }
                                else if (shapeObj != null && shapeObj.CachedImage != null)
                                {
                                    try
                                    {
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            shapeObj.CachedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                            imgBytes = ms.ToArray();
                                            ext = "png";
                                        }
                                    }
                                    catch { }
                                }

                                // Fallback for SmartArt & Vector Shapes
                                if (imgBytes == null && shapeObj != null)
                                {
                                    if (shapeObj.ShapeType == "SmartArt" || !string.IsNullOrEmpty(shapeObj.SmartArtType) || shapeType == "SmartArt")
                                    {
                                        shapeObj.ShapeType = "SmartArt";
                                        int renderW = Math.Max(240, tagW > 0 ? tagW : ((shapeObj.BaseWidth + shapeObj.WidthDelta) > 0 ? (shapeObj.BaseWidth + shapeObj.WidthDelta) : 480));
                                        int renderH = Math.Max(160, tagH > 0 ? tagH : ((shapeObj.BaseHeight + shapeObj.HeightDelta) > 0 ? (shapeObj.BaseHeight + shapeObj.HeightDelta) : 240));
                                        try
                                        {
                                            using (Bitmap saBmp = SmartArtRenderer.RenderToBitmap(shapeObj, renderW, renderH, 2.0f))
                                            using (MemoryStream ms = new MemoryStream())
                                            {
                                                saBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                                imgBytes = ms.ToArray();
                                                ext = "png";
                                            }
                                        }
                                        catch { }
                                    }
                                    else if (shapeObj.ShapeType != "Image")
                                    {
                                        int renderW = Math.Max(60, tagW > 0 ? tagW : ((shapeObj.BaseWidth + shapeObj.WidthDelta) > 0 ? (shapeObj.BaseWidth + shapeObj.WidthDelta) : 160));
                                        int renderH = Math.Max(40, tagH > 0 ? tagH : ((shapeObj.BaseHeight + shapeObj.HeightDelta) > 0 ? (shapeObj.BaseHeight + shapeObj.HeightDelta) : 100));
                                        try
                                        {
                                            using (Bitmap shBmp = SmartArtRenderer.RenderShapeToBitmap(shapeObj, renderW, renderH, 2.0f))
                                            using (MemoryStream ms = new MemoryStream())
                                            {
                                                shBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                                imgBytes = ms.ToArray();
                                                ext = "png";
                                            }
                                        }
                                        catch { }
                                    }
                                }

                                if (imgBytes != null && imgBytes.Length > 0)
                                {
                                    mediaCount++;
                                    string relId = "rIdMedia" + mediaCount;
                                    string targetPath = "media/image" + mediaCount + "." + ext;
                                    mediaItems.Add(new DocxMediaItem
                                    {
                                        RelId = relId,
                                        TargetPath = targetPath,
                                        Data = imgBytes,
                                        Extension = ext,
                                        ContentType = "image/" + (ext == "jpg" ? "jpeg" : ext)
                                    });

                                    int wPx = tagW > 0 ? tagW : ((shapeObj != null && (shapeObj.BaseWidth + shapeObj.WidthDelta) > 0) ? (shapeObj.BaseWidth + shapeObj.WidthDelta) : 480);
                                    int hPx = tagH > 0 ? tagH : ((shapeObj != null && (shapeObj.BaseHeight + shapeObj.HeightDelta) > 0) ? (shapeObj.BaseHeight + shapeObj.HeightDelta) : 320);
                                    int wrapMode = tagWrap >= 0 ? tagWrap : ((shapeObj != null) ? shapeObj.WrapMode : 0);
                                    int pageX = tagX >= 0 ? tagX : ((shapeObj != null) ? shapeObj.PageX : 50);
                                    int pageY = tagY >= 0 ? tagY : ((shapeObj != null) ? shapeObj.PageY : 50);

                                    // Scale to fit within standard A4 printable margin in Word (~580px max)
                                    int maxWordW = 580;
                                    long scaledW = wPx;
                                    long scaledH = hPx;
                                    if (scaledW > maxWordW)
                                    {
                                        scaledH = (long)((double)scaledH * maxWordW / scaledW);
                                        scaledW = maxWordW;
                                    }
                                    long cxEmu = Math.Max(95250L, scaledW * 9525L);
                                    long cyEmu = Math.Max(95250L, scaledH * 9525L);

                                    string docPrName = "Picture " + mediaCount;
                                    string docPrDescr = "";

                                    if (shapeObj != null && (shapeObj.ShapeType == "SmartArt" || !string.IsNullOrEmpty(shapeObj.SmartArtType) || shapeType == "SmartArt"))
                                    {
                                        docPrName = "SmartArt " + mediaCount;
                                        string sType = !string.IsNullOrEmpty(shapeObj.SmartArtType) ? shapeObj.SmartArtType : "Cycle_Radial";
                                        string sTitle = shapeObj.SmartArtTitle ?? "";
                                        string sText = shapeObj.TextContent ?? "";
                                        docPrDescr = string.Format("SmartArt|{0}|{1}|{2}", sType, sTitle, sText);
                                    }
                                    else if (shapeObj != null && shapeObj.ShapeType != "Image")
                                    {
                                        docPrName = "Shape " + mediaCount;
                                        string sType = shapeObj.ShapeType ?? "Rectangle";
                                        string sText = shapeObj.TextContent ?? "";
                                        docPrDescr = string.Format("Shape|{0}|{1}|{2}|{3}|{4}", sType, sText, shapeObj.AdjustValue1.ToString("F3", CultureInfo.InvariantCulture), shapeObj.AdjustValue2.ToString("F3", CultureInfo.InvariantCulture), shapeObj.AdjustValue3.ToString("F3", CultureInfo.InvariantCulture));
                                    }

                                    string escapedDescrAttr = !string.IsNullOrEmpty(docPrDescr)
                                        ? string.Format(" descr=\"{0}\"", docPrDescr.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;"))
                                        : "";

                                    if (wrapMode == 0) // In-Line with Text
                                    {
                                        sw.WriteLine("    <w:p>");
                                        sw.WriteLine("      <w:pPr><w:jc w:val=\"center\"/></w:pPr>");
                                        sw.WriteLine("      <w:r>");
                                        sw.WriteLine("        <w:drawing>");
                                        sw.WriteLine("          <wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">");
                                        sw.WriteLine(string.Format("            <wp:extent cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                        sw.WriteLine("            <wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>");
                                        sw.WriteLine(string.Format("            <wp:docPr id=\"{0}\" name=\"{1}\"{2}/>", mediaCount, docPrName, escapedDescrAttr));
                                        sw.WriteLine("            <wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>");
                                        sw.WriteLine("            <a:graphic>");
                                        sw.WriteLine("              <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                                        sw.WriteLine("                <pic:pic>");
                                        sw.WriteLine("                  <pic:nvPicPr>");
                                        sw.WriteLine(string.Format("                    <pic:cNvPr id=\"{0}\" name=\"{1}\"{2}/>", mediaCount, docPrName, escapedDescrAttr));
                                        sw.WriteLine("                    <pic:cNvPicPr/>");
                                        sw.WriteLine("                  </pic:nvPicPr>");
                                        sw.WriteLine("                  <pic:blipFill>");
                                        sw.WriteLine(string.Format("                    <a:blip r:embed=\"{0}\"/>", relId));
                                        sw.WriteLine("                    <a:stretch><a:fillRect/></a:stretch>");
                                        sw.WriteLine("                  </pic:blipFill>");
                                        sw.WriteLine("                  <pic:spPr>");
                                        sw.WriteLine("                    <a:xfrm>");
                                        sw.WriteLine("                      <a:off x=\"0\" y=\"0\"/>");
                                        sw.WriteLine(string.Format("                      <a:ext cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                        sw.WriteLine("                    </a:xfrm>");
                                        sw.WriteLine("                    <a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
                                        sw.WriteLine("                  </pic:spPr>");
                                        sw.WriteLine("                </pic:pic>");
                                        sw.WriteLine("              </a:graphicData>");
                                        sw.WriteLine("            </a:graphic>");
                                        sw.WriteLine("          </wp:inline>");
                                        sw.WriteLine("        </w:drawing>");
                                        sw.WriteLine("      </w:r>");
                                        sw.WriteLine("    </w:p>");
                                        charOffset += para.Length + 1;
                                        continue;
                                    }
                                    else // Floating Anchor (Square, Tight, Through, TopBottom, Behind, InFront)
                                    {
                                        long xOffEmu = Math.Max(0L, pageX * 9525L);
                                        long yOffEmu = Math.Max(0L, pageY * 9525L);
                                        int behindDoc = (wrapMode == 5) ? 1 : 0;
                                        string wrapXml = "<wp:wrapSquare wrapText=\"bothSides\"/>";
                                        if (wrapMode == 2) wrapXml = "<wp:wrapTight wrapText=\"bothSides\"><wp:wrapPolygon edited=\"0\"><wp:start x=\"0\" y=\"0\"/><wp:lineTo x=\"0\" y=\"21600\"/><wp:lineTo x=\"21600\" y=\"21600\"/><wp:lineTo x=\"21600\" y=\"0\"/><wp:lineTo x=\"0\" y=\"0\"/></wp:wrapPolygon></wp:wrapTight>";
                                        else if (wrapMode == 3) wrapXml = "<wp:wrapThrough wrapText=\"bothSides\"><wp:wrapPolygon edited=\"0\"><wp:start x=\"0\" y=\"0\"/><wp:lineTo x=\"0\" y=\"21600\"/><wp:lineTo x=\"21600\" y=\"21600\"/><wp:lineTo x=\"21600\" y=\"0\"/><wp:lineTo x=\"0\" y=\"0\"/></wp:wrapPolygon></wp:wrapThrough>";
                                        else if (wrapMode == 4) wrapXml = "<wp:wrapTopAndBottom/>";
                                        else if (wrapMode == 5 || wrapMode == 6) wrapXml = "<wp:wrapNone/>";

                                        StringBuilder sbDraw = new StringBuilder();
                                        sbDraw.AppendLine("      <w:r>");
                                        sbDraw.AppendLine("        <w:drawing>");
                                        sbDraw.AppendLine(string.Format("          <wp:anchor distT=\"0\" distB=\"0\" distL=\"114300\" distR=\"114300\" simplePos=\"0\" relativeHeight=\"251658240\" behindDoc=\"{0}\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\">", behindDoc));
                                        sbDraw.AppendLine("            <wp:simplePos x=\"0\" y=\"0\"/>");
                                        sbDraw.AppendLine("            <wp:positionH relativeFrom=\"margin\">");
                                        sbDraw.AppendLine(string.Format("              <wp:posOffset>{0}</wp:posOffset>", xOffEmu));
                                        sbDraw.AppendLine("            </wp:positionH>");
                                        sbDraw.AppendLine("            <wp:positionV relativeFrom=\"margin\">");
                                        sbDraw.AppendLine(string.Format("              <wp:posOffset>{0}</wp:posOffset>", yOffEmu));
                                        sbDraw.AppendLine("            </wp:positionV>");
                                        sbDraw.AppendLine(string.Format("            <wp:extent cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                        sbDraw.AppendLine("            <wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>");
                                        sbDraw.AppendLine("            " + wrapXml);
                                        sbDraw.AppendLine(string.Format("            <wp:docPr id=\"{0}\" name=\"{1}\"{2}/>", mediaCount, docPrName, escapedDescrAttr));
                                        sbDraw.AppendLine("            <wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>");
                                        sbDraw.AppendLine("            <a:graphic>");
                                        sbDraw.AppendLine("              <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                                        sbDraw.AppendLine("                <pic:pic>");
                                        sbDraw.AppendLine("                  <pic:nvPicPr>");
                                        sbDraw.AppendLine(string.Format("                    <pic:cNvPr id=\"{0}\" name=\"{1}\"{2}/>", mediaCount, docPrName, escapedDescrAttr));
                                        sbDraw.AppendLine("                    <pic:cNvPicPr/>");
                                        sbDraw.AppendLine("                  </pic:nvPicPr>");
                                        sbDraw.AppendLine("                  <pic:blipFill>");
                                        sbDraw.AppendLine(string.Format("                    <a:blip r:embed=\"{0}\"/>", relId));
                                        sbDraw.AppendLine("                    <a:stretch><a:fillRect/></a:stretch>");
                                        sbDraw.AppendLine("                  </pic:blipFill>");
                                        sbDraw.AppendLine("                  <pic:spPr>");
                                        sbDraw.AppendLine("                    <a:xfrm>");
                                        sbDraw.AppendLine("                      <a:off x=\"0\" y=\"0\"/>");
                                        sbDraw.AppendLine(string.Format("                      <a:ext cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                        sbDraw.AppendLine("                    </a:xfrm>");
                                        sbDraw.AppendLine("                    <a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
                                        sbDraw.AppendLine("                  </pic:spPr>");
                                        sbDraw.AppendLine("                </pic:pic>");
                                        sbDraw.AppendLine("              </a:graphicData>");
                                        sbDraw.AppendLine("            </a:graphic>");
                                        sbDraw.AppendLine("          </wp:anchor>");
                                        sbDraw.AppendLine("        </w:drawing>");
                                        sbDraw.AppendLine("      </w:r>");

                                        pendingFloatingDrawing = sbDraw.ToString();
                                        charOffset += para.Length + 1;
                                        continue;
                                    }
                                }
                            }

                            // 7.4 Interactive Chart Export
                            if (para.StartsWith("![CHART:") || para.Contains("BIỂU ĐỒ DOANH THU") || para.Contains("BIỂU ĐỒ SỐ LIỆU"))
                            {
                                string chartId = "";
                                if (para.StartsWith("![CHART:"))
                                {
                                    string inner = para.Substring(8).TrimEnd(']', '\r', '\n');
                                    string[] parts = inner.Split(':');
                                    foreach (var pItem in parts)
                                    {
                                        if (pItem.StartsWith("ID=")) chartId = pItem.Substring(3);
                                    }
                                }
                                DocChartObject chartObj = (charts != null) ? charts.Find(c => c.Id == chartId) : null;
                                if (chartObj != null)
                                {
                                    try
                                    {
                                        int cW = Math.Max(300, chartObj.BaseWidth + chartObj.WidthDelta);
                                        int cH = Math.Max(180, chartObj.BaseHeight + chartObj.HeightDelta);
                                        using (Bitmap chartBmp = new Bitmap(cW, cH))
                                        {
                                            using (Graphics gChart = Graphics.FromImage(chartBmp))
                                            {
                                                gChart.Clear(Color.White);
                                                // Basic chart outline rendering for MS Word snapshot
                                                using (Pen borderPen = new Pen(Color.FromArgb(200, 200, 200), 1.2f))
                                                {
                                                    gChart.DrawRectangle(borderPen, 1, 1, cW - 2, cH - 2);
                                                }
                                                using (Font cTitleF = new Font("Segoe UI", 12f, FontStyle.Bold))
                                                {
                                                    gChart.DrawString(chartObj.Data.Title ?? "Biểu đồ", cTitleF, Brushes.Black, 15, 12);
                                                }
                                            }

                                            using (MemoryStream ms = new MemoryStream())
                                            {
                                                chartBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                                byte[] chartBytes = ms.ToArray();
                                                mediaCount++;
                                                string relId = "rIdMedia" + mediaCount;
                                                string targetPath = "media/image" + mediaCount + ".png";
                                                mediaItems.Add(new DocxMediaItem
                                                {
                                                    RelId = relId,
                                                    TargetPath = targetPath,
                                                    Data = chartBytes,
                                                    Extension = "png",
                                                    ContentType = "image/png"
                                                });

                                                long cxEmu = cW * 9525L;
                                                long cyEmu = cH * 9525L;
                                                sw.WriteLine("    <w:p>");
                                                sw.WriteLine("      <w:pPr><w:jc w:val=\"center\"/></w:pPr>");
                                                sw.WriteLine("      <w:r>");
                                                sw.WriteLine("        <w:drawing>");
                                                sw.WriteLine("          <wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">");
                                                sw.WriteLine(string.Format("            <wp:extent cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                                sw.WriteLine("            <wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>");
                                                sw.WriteLine(string.Format("            <wp:docPr id=\"{0}\" name=\"Chart {0}\"/>", mediaCount));
                                                sw.WriteLine("            <wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>");
                                                sw.WriteLine("            <a:graphic>");
                                                sw.WriteLine("              <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                                                sw.WriteLine("                <pic:pic>");
                                                sw.WriteLine("                  <pic:nvPicPr>");
                                                sw.WriteLine(string.Format("                    <pic:cNvPr id=\"{0}\" name=\"Chart {0}\"/>", mediaCount));
                                                sw.WriteLine("                    <pic:cNvPicPr/>");
                                                sw.WriteLine("                  </pic:nvPicPr>");
                                                sw.WriteLine("                  <pic:blipFill>");
                                                sw.WriteLine(string.Format("                    <a:blip r:embed=\"{0}\"/>", relId));
                                                sw.WriteLine("                    <a:stretch><a:fillRect/></a:stretch>");
                                                sw.WriteLine("                  </pic:blipFill>");
                                                sw.WriteLine("                  <pic:spPr>");
                                                sw.WriteLine("                    <a:xfrm>");
                                                sw.WriteLine("                      <a:off x=\"0\" y=\"0\"/>");
                                                sw.WriteLine(string.Format("                      <a:ext cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                                sw.WriteLine("                    </a:xfrm>");
                                                sw.WriteLine("                    <a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
                                                sw.WriteLine("                  </pic:spPr>");
                                                sw.WriteLine("                </pic:pic>");
                                                sw.WriteLine("              </a:graphicData>");
                                                sw.WriteLine("            </a:graphic>");
                                                sw.WriteLine("          </wp:inline>");
                                                sw.WriteLine("        </w:drawing>");
                                                sw.WriteLine("      </w:r>");
                                                sw.WriteLine("    </w:p>");

                                                charOffset += para.Length + 1;
                                                continue;
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }

                            // 7.5 Standard Text Paragraph
                            sw.WriteLine("    <w:p>");
                            sw.WriteLine("      <w:pPr>");

                            // Alignment
                            int pAlign = 3; // default both / justify
                            if (paraAlignments != null && paraAlignments.ContainsKey(charOffset))
                            {
                                pAlign = paraAlignments[charOffset];
                            }
                            string jcVal = "both";
                            if (pAlign == 0) jcVal = "left";
                            else if (pAlign == 1) jcVal = "center";
                            else if (pAlign == 2) jcVal = "right";
                            sw.WriteLine(string.Format("        <w:jc w:val=\"{0}\"/>", jcVal));

                            // Spacing (Line spacing + before/after)
                            float lSpacing = 1.15f;
                            float sBefore = 0.0f;
                            float sAfter = 0.0f;
                            bool hasCustomSpacing = false;
                            if (paraLineSpacings != null && paraLineSpacings.ContainsKey(charOffset))
                            {
                                lSpacing = paraLineSpacings[charOffset];
                                hasCustomSpacing = true;
                            }
                            if (paraSpacingsBefore != null && paraSpacingsBefore.ContainsKey(charOffset))
                            {
                                sBefore = paraSpacingsBefore[charOffset];
                                hasCustomSpacing = true;
                            }
                            if (paraSpacingsAfter != null && paraSpacingsAfter.ContainsKey(charOffset))
                            {
                                sAfter = paraSpacingsAfter[charOffset];
                                hasCustomSpacing = true;
                            }
                            if (hasCustomSpacing)
                            {
                                int lineTwips = (int)(lSpacing * 240);
                                int beforeTwips = (int)(sBefore * 20);
                                int afterTwips = (int)(sAfter * 20);
                                sw.WriteLine(string.Format("        <w:spacing w:line=\"{0}\" w:lineRule=\"auto\" w:before=\"{1}\" w:after=\"{2}\"/>", lineTwips, beforeTwips, afterTwips));
                            }

                            // Paragraph Borders (<w:pBdr>)
                            if (paraBorders != null && paraBorders.ContainsKey(charOffset))
                            {
                                ParagraphBorderInfo bdr = paraBorders[charOffset];
                                if (bdr != null && bdr.HasAnyBorder)
                                {
                                    sw.WriteLine("        <w:pBdr>");
                                    int sz = Math.Max(4, (int)(bdr.SizePt * 8)); // 1/8 pt
                                    string colHex = string.Format("{0:X2}{1:X2}{2:X2}", bdr.Color.R, bdr.Color.G, bdr.Color.B);
                                    if (bdr.IsHorizontalLine)
                                    {
                                        sw.WriteLine(string.Format("          <w:bottom w:val=\"single\" w:sz=\"{0}\" w:space=\"1\" w:color=\"{1}\"/>", Math.Max(6, sz), colHex));
                                    }
                                    else
                                    {
                                        if (bdr.Top) sw.WriteLine(string.Format("          <w:top w:val=\"single\" w:sz=\"{0}\" w:space=\"1\" w:color=\"{1}\"/>", sz, colHex));
                                        if (bdr.Left) sw.WriteLine(string.Format("          <w:left w:val=\"single\" w:sz=\"{0}\" w:space=\"4\" w:color=\"{1}\"/>", sz, colHex));
                                        if (bdr.Bottom) sw.WriteLine(string.Format("          <w:bottom w:val=\"single\" w:sz=\"{0}\" w:space=\"1\" w:color=\"{1}\"/>", sz, colHex));
                                        if (bdr.Right) sw.WriteLine(string.Format("          <w:right w:val=\"single\" w:sz=\"{0}\" w:space=\"4\" w:color=\"{1}\"/>", sz, colHex));
                                    }
                                    sw.WriteLine("        </w:pBdr>");
                                }
                            }

                            // Paragraph Indentation (<w:ind>)
                            float pFL = (paraFirstLineIndents != null && paraFirstLineIndents.ContainsKey(charOffset)) ? paraFirstLineIndents[charOffset] : 0f;
                            float pL = (paraLeftIndents != null && paraLeftIndents.ContainsKey(charOffset)) ? paraLeftIndents[charOffset] : 0f;
                            float pR = (paraRightIndents != null && paraRightIndents.ContainsKey(charOffset)) ? paraRightIndents[charOffset] : 0f;
                            if (pFL != 0f || pL != 0f || pR != 0f)
                            {
                                int leftTwips = (int)Math.Round(pL * 20);
                                int rightTwips = (int)Math.Round(pR * 20);
                                StringBuilder indSb = new StringBuilder("        <w:ind");
                                if (leftTwips != 0) indSb.Append(string.Format(" w:left=\"{0}\"", leftTwips));
                                if (rightTwips != 0) indSb.Append(string.Format(" w:right=\"{0}\"", rightTwips));
                                if (pFL < 0)
                                {
                                    indSb.Append(string.Format(" w:hanging=\"{0}\"", (int)Math.Round(-pFL * 20)));
                                }
                                else if (pFL > 0)
                                {
                                    indSb.Append(string.Format(" w:firstLine=\"{0}\"", (int)Math.Round(pFL * 20)));
                                }
                                indSb.Append("/>");
                                sw.WriteLine(indSb.ToString());
                            }

                            sw.WriteLine("      </w:pPr>");

                            if (!string.IsNullOrEmpty(pendingFloatingDrawing))
                            {
                                sw.Write(pendingFloatingDrawing);
                                pendingFloatingDrawing = null;
                            }

                            if (string.IsNullOrEmpty(para))
                            {
                                sw.WriteLine("      <w:r><w:t></w:t></w:r>");
                            }
                            else
                            {
                                int pIdx = 0;
                                while (pIdx < para.Length)
                                {
                                    FormattedRun fmt = (runs != null) ? GetFormatAt(runs, charOffset + pIdx) : null;
                                    int runLen = 1;
                                    while (pIdx + runLen < para.Length && (runs == null || GetFormatAt(runs, charOffset + pIdx + runLen) == fmt))
                                    {
                                        runLen++;
                                    }

                                    string runText = para.Substring(pIdx, runLen);
                                    string font = (fmt != null && !string.IsNullOrEmpty(fmt.FontName)) ? fmt.FontName : "Times New Roman";
                                    int halfPts = (fmt != null && fmt.FontSize > 0) ? (int)(fmt.FontSize * 2) : 24;
                                    bool b = fmt != null && fmt.Bold;
                                    bool it = fmt != null && fmt.Italic;
                                    bool u = fmt != null && fmt.Underline;
                                    bool strk = fmt != null && fmt.Strike;
                                    bool sub = fmt != null && fmt.Subscript;
                                    bool sup = fmt != null && fmt.Superscript;
                                    Color c = (fmt != null) ? fmt.Color : Color.Black;

                                    sw.WriteLine("      <w:r>");
                                    sw.WriteLine("        <w:rPr>");
                                    sw.WriteLine(string.Format("          <w:rFonts w:ascii=\"{0}\" w:hAnsi=\"{0}\" w:cs=\"{0}\" w:eastAsia=\"{0}\"/>", EscapeXml(font)));
                                    sw.WriteLine(string.Format("          <w:sz w:val=\"{0}\"/>", halfPts));
                                    sw.WriteLine(string.Format("          <w:szCs w:val=\"{0}\"/>", halfPts));
                                    if (b) { sw.WriteLine("          <w:b/>"); sw.WriteLine("          <w:bCs/>"); }
                                    if (it) { sw.WriteLine("          <w:i/>"); sw.WriteLine("          <w:iCs/>"); }
                                    if (u) sw.WriteLine("          <w:u w:val=\"single\"/>");
                                    if (strk) sw.WriteLine("          <w:strike/>");
                                    if (sub) sw.WriteLine("          <w:vertAlign w:val=\"subscript\"/>");
                                    else if (sup) sw.WriteLine("          <w:vertAlign w:val=\"superscript\"/>");
                                    if (c != Color.Black && c != Color.Empty && c.A > 0)
                                    {
                                        sw.WriteLine(string.Format("          <w:color w:val=\"{0:X2}{1:X2}{2:X2}\"/>", c.R, c.G, c.B));
                                    }
                                    sw.WriteLine("        </w:rPr>");
                                    sw.WriteLine("        <w:t xml:space=\"preserve\">" + EscapeXml(runText) + "</w:t>");
                                    sw.WriteLine("      </w:r>");

                                    pIdx += runLen;
                                }
                            }

                            sw.WriteLine("    </w:p>");
                            charOffset += para.Length + 1;
                        }

                        if (!string.IsNullOrEmpty(pendingFloatingDrawing))
                        {
                            sw.WriteLine("    <w:p>");
                            sw.Write(pendingFloatingDrawing);
                            sw.WriteLine("    </w:p>");
                            pendingFloatingDrawing = null;
                        }

                        // Section Properties
                        sw.WriteLine("    <w:sectPr>");
                        if (!string.IsNullOrEmpty(headerText)) sw.WriteLine("      <w:headerReference r:id=\"rIdHeader1\" w:type=\"default\"/>");
                        if (!string.IsNullOrEmpty(footerText) || showPageNumbers) sw.WriteLine("      <w:footerReference r:id=\"rIdFooter1\" w:type=\"default\"/>");

                        if (isLandscape)
                        {
                            sw.WriteLine("      <w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/>");
                        }
                        else
                        {
                            sw.WriteLine("      <w:pgSz w:w=\"11906\" w:h=\"16838\"/>");
                        }

                        int topM = 1440, rightM = 1440, bottomM = 1440, leftM = 1440;
                        if (marginMode == 1) { topM = 720; rightM = 720; bottomM = 720; leftM = 720; }
                        else if (marginMode == 2) { topM = 1440; rightM = 2880; bottomM = 1440; leftM = 2880; }
                        sw.WriteLine(string.Format("      <w:pgMar w:top=\"{0}\" w:right=\"{1}\" w:bottom=\"{2}\" w:left=\"{3}\" w:header=\"720\" w:footer=\"720\" w:gutter=\"0\"/>", topM, rightM, bottomM, leftM));
                        sw.WriteLine("    </w:sectPr>");

                        sw.WriteLine("  </w:body>");
                        sw.WriteLine("</w:document>");
                    }

                    // 8. word/_rels/document.xml.rels
                    var docRelsEntry = archive.CreateEntry("word/_rels/document.xml.rels");
                    using (StreamWriter sw = new StreamWriter(docRelsEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                        sw.WriteLine("  <Relationship Id=\"rIdStyles\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
                        if (!string.IsNullOrEmpty(headerText))
                        {
                            sw.WriteLine("  <Relationship Id=\"rIdHeader1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/header\" Target=\"header1.xml\"/>");
                        }
                        if (!string.IsNullOrEmpty(footerText) || showPageNumbers)
                        {
                            sw.WriteLine("  <Relationship Id=\"rIdFooter1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer\" Target=\"footer1.xml\"/>");
                        }
                        foreach (var m in mediaItems)
                        {
                            sw.WriteLine(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"{1}\"/>", m.RelId, m.TargetPath));
                        }
                        sw.WriteLine("</Relationships>");
                    }

                    // 9. Write Media Files to word/media/
                    foreach (var m in mediaItems)
                    {
                        var mEntry = archive.CreateEntry("word/" + m.TargetPath);
                        using (var entryStream = mEntry.Open())
                        {
                            entryStream.Write(m.Data, 0, m.Data.Length);
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static FormattedRun GetFormatAt(List<FormattedRun> runs, int charIndex)
        {
            if (runs == null) return null;
            for (int i = runs.Count - 1; i >= 0; i--)
            {
                var r = runs[i];
                if (charIndex >= r.StartIndex && charIndex < r.StartIndex + r.Length)
                    return r;
            }
            return null;
        }

        private static string EscapeXml(string input)
        {
            return (input ?? "")
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }



    public class PptxHandler
    {
        public static bool SavePptx(string filepath, PresentationRoot pres)
        {
            if (filepath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return SavePptxXml(filepath, pres);
            }

            try
            {
                if (File.Exists(filepath)) File.Delete(filepath);

                // Initialize with standard Microsoft PowerPoint template
                byte[] templateBytes = PptxTemplate.GetBaseTemplateBytes();
                File.WriteAllBytes(filepath, templateBytes);

                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    bool isWide = pres.IsWidescreen;
                    long cxEmu = (long)(pres.CanvasWidth * 9525.0f);
                    long cyEmu = (long)(pres.CanvasHeight * 9525.0f);
                    if (cxEmu <= 0) cxEmu = isWide ? 18288000L : 13716000L;
                    if (cyEmu <= 0) cyEmu = 10287000L;
                    float exportScaleX = 1.0f;
                    float exportScaleY = 1.0f;

                    // 1. Remove old default slide1 if present
                    var oldS1 = archive.GetEntry("ppt/slides/slide1.xml");
                    if (oldS1 != null) oldS1.Delete();
                    var oldS1Rels = archive.GetEntry("ppt/slides/_rels/slide1.xml.rels");
                    if (oldS1Rels != null) oldS1Rels.Delete();

                    // 2. Update [Content_Types].xml
                    var ctEntry = archive.GetEntry("[Content_Types].xml");
                    if (ctEntry != null) ctEntry.Delete();
                    ctEntry = archive.CreateEntry("[Content_Types].xml");
                    using (StreamWriter sw = new StreamWriter(ctEntry.Open(), new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                        sw.WriteLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                        sw.WriteLine("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                        sw.WriteLine("  <Default Extension=\"png\" ContentType=\"image/png\"/>");
                        sw.WriteLine("  <Default Extension=\"jpg\" ContentType=\"image/jpeg\"/>");
                        sw.WriteLine("  <Default Extension=\"jpeg\" ContentType=\"image/jpeg\"/>");
                        sw.WriteLine("  <Default Extension=\"gif\" ContentType=\"image/gif\"/>");
                        sw.WriteLine("  <Default Extension=\"svg\" ContentType=\"image/svg+xml\"/>");
                        sw.WriteLine("  <Default Extension=\"mp4\" ContentType=\"video/mp4\"/>");
                        sw.WriteLine("  <Default Extension=\"mov\" ContentType=\"video/quicktime\"/>");
                        sw.WriteLine("  <Default Extension=\"wmv\" ContentType=\"video/x-ms-wmv\"/>");
                        sw.WriteLine("  <Default Extension=\"avi\" ContentType=\"video/x-msvideo\"/>");
                        sw.WriteLine("  <Default Extension=\"webm\" ContentType=\"video/webm\"/>");
                        sw.WriteLine("  <Default Extension=\"mkv\" ContentType=\"video/x-matroska\"/>");
                        sw.WriteLine("  <Default Extension=\"mp3\" ContentType=\"audio/mpeg\"/>");
                        sw.WriteLine("  <Default Extension=\"wav\" ContentType=\"audio/wav\"/>");
                        sw.WriteLine("  <Default Extension=\"m4a\" ContentType=\"audio/mp4\"/>");
                        sw.WriteLine("  <Default Extension=\"wma\" ContentType=\"audio/x-ms-wma\"/>");
                        sw.WriteLine("  <Default Extension=\"aac\" ContentType=\"audio/aac\"/>");
                        sw.WriteLine("  <Default Extension=\"ogg\" ContentType=\"audio/ogg\"/>");
                        if (SlideFontManager.EmbeddedFontFiles.Count > 0)
                        {
                            sw.WriteLine("  <Default Extension=\"fntdata\" ContentType=\"application/x-font-data\"/>");
                        }
                        sw.WriteLine("  <Override PartName=\"/ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/slideMasters/slideMaster1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml\"/>");
                        for (int lay = 1; lay <= 11; lay++)
                        {
                            sw.WriteLine(string.Format("  <Override PartName=\"/ppt/slideLayouts/slideLayout{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml\"/>", lay));
                        }
                        sw.WriteLine("  <Override PartName=\"/ppt/theme/theme1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.theme+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/tableStyles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.tableStyles+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/presProps.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presProps+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/viewProps.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.viewProps+xml\"/>");
                        for (int i = 0; i < pres.Slides.Count; i++)
                        {
                            sw.WriteLine(string.Format("  <Override PartName=\"/ppt/slides/slide{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>", i + 1));
                            if (!string.IsNullOrEmpty(pres.Slides[i].SpeakerNotes))
                            {
                                sw.WriteLine(string.Format("  <Override PartName=\"/ppt/notesSlides/notesSlide{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.notesSlide+xml\"/>", i + 1));
                            }
                        }
                        sw.WriteLine("</Types>");
                    }

                    // 2.1 Write embedded fonts into ppt/fonts/
                    Dictionary<string, string> fontRels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    int fontRIdCounter = 100;
                    foreach (var kvp in SlideFontManager.EmbeddedFontFiles)
                    {
                        string fontZipPath = "ppt/fonts/" + kvp.Key;
                        var oldF = archive.GetEntry(fontZipPath);
                        if (oldF != null) oldF.Delete();
                        var fEntry = archive.CreateEntry(fontZipPath);
                        using (Stream fsStream = fEntry.Open())
                        {
                            fsStream.Write(kvp.Value, 0, kvp.Value.Length);
                        }
                        string rId = "rIdFont" + (fontRIdCounter++);
                        fontRels[kvp.Key] = rId;
                    }

                    // 3. Update ppt/_rels/presentation.xml.rels
                    var presRels = archive.GetEntry("ppt/_rels/presentation.xml.rels");
                    if (presRels != null) presRels.Delete();
                    presRels = archive.CreateEntry("ppt/_rels/presentation.xml.rels");
                    using (StreamWriter sw = new StreamWriter(presRels.Open(), new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                        sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster\" Target=\"slideMasters/slideMaster1.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdPresProps\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/presProps\" Target=\"presProps.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdViewProps\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/viewProps\" Target=\"viewProps.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdTheme\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"theme/theme1.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdTableStyles\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/tableStyles\" Target=\"tableStyles.xml\"/>");
                        for (int i = 0; i < pres.Slides.Count; i++)
                        {
                            sw.WriteLine(string.Format("  <Relationship Id=\"rIdSlide{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide{1}.xml\"/>", i + 1, i + 1));
                        }
                        foreach (var kvp in fontRels)
                        {
                            sw.WriteLine(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/font\" Target=\"fonts/{1}\"/>", kvp.Value, kvp.Key));
                        }
                        sw.WriteLine("</Relationships>");
                    }

                    // 4. Update ppt/presentation.xml
                    var presEntry = archive.GetEntry("ppt/presentation.xml");
                    if (presEntry != null) presEntry.Delete();
                    presEntry = archive.CreateEntry("ppt/presentation.xml");
                    using (StreamWriter sw = new StreamWriter(presEntry.Open(), new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<p:presentation xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                        sw.WriteLine("  <p:sldMasterIdLst>");
                        sw.WriteLine("    <p:sldMasterId id=\"2147483648\" r:id=\"rId1\"/>");
                        sw.WriteLine("  </p:sldMasterIdLst>");
                        sw.WriteLine("  <p:sldIdLst>");
                        for (int i = 0; i < pres.Slides.Count; i++)
                        {
                            sw.WriteLine(string.Format("    <p:sldId id=\"{0}\" r:id=\"rIdSlide{1}\"/>", 256 + i, i + 1));
                        }
                        sw.WriteLine("  </p:sldIdLst>");
                        sw.WriteLine(string.Format("  <p:sldSz cx=\"{0}\" cy=\"{1}\" type=\"{2}\"/>", cxEmu, cyEmu, isWide ? "screen16x9" : "screen4x3"));
                        sw.WriteLine("  <p:notesSz cx=\"6858000\" cy=\"9144000\"/>");
                        if (fontRels.Count > 0)
                        {
                            sw.WriteLine("  <p:embeddedFontLst>");
                            foreach (var kvp in fontRels)
                            {
                                string tf = "";
                                if (!SlideFontManager.EmbeddedFontTypefaces.TryGetValue(kvp.Key, out tf) || string.IsNullOrEmpty(tf))
                                {
                                    tf = Path.GetFileNameWithoutExtension(kvp.Key);
                                }
                                sw.WriteLine(string.Format("    <p:embeddedFont><p:font typeface=\"{0}\"/><p:regular r:id=\"{1}\"/></p:embeddedFont>", EscapeXml(tf), kvp.Value));
                            }
                            sw.WriteLine("  </p:embeddedFontLst>");
                        }
                        sw.WriteLine("</p:presentation>");
                    }

                    // 8. Slides and their media/rels
                    for (int i = 0; i < pres.Slides.Count; i++)
                    {
                        SlideNode slide = pres.Slides[i];
                        int slideIndex = i + 1;

                        // Discover images and media on slide and write media files
                        Dictionary<ShapeNode, string> shapeImgRels = new Dictionary<ShapeNode, string>();
                        Dictionary<ShapeNode, KeyValuePair<string, string>> shapeVideoRels = new Dictionary<ShapeNode, KeyValuePair<string, string>>();
                        Dictionary<ShapeNode, KeyValuePair<string, string>> shapeAudioRels = new Dictionary<ShapeNode, KeyValuePair<string, string>>();
                        List<KeyValuePair<string, string>> imgRelsList = new List<KeyValuePair<string, string>>();
                        List<string> mediaRelXmlLines = new List<string>();
                        string bgImgRelId = null;
                        int imgCounter = 1;
                        int mediaCounter = 1;

                        if (slide.BgFillType == BackgroundFillType.Picture && slide.CachedBgImage != null)
                        {
                            try
                            {
                                byte[] bgBytes = null;
                                using (MemoryStream bgMs = new MemoryStream())
                                {
                                    try
                                    {
                                        slide.CachedBgImage.Save(bgMs, System.Drawing.Imaging.ImageFormat.Png);
                                    }
                                    catch
                                    {
                                        bgMs.SetLength(0);
                                        using (Bitmap bmpCopy = new Bitmap(slide.CachedBgImage))
                                        {
                                            bmpCopy.Save(bgMs, System.Drawing.Imaging.ImageFormat.Png);
                                        }
                                    }
                                    if (bgMs.Length > 0) bgBytes = bgMs.ToArray();
                                }
                                if (bgBytes != null && bgBytes.Length > 0)
                                {
                                    string bgFilename = string.Format("bg_s{0}.png", slideIndex);
                                    string bgZipPath = "ppt/media/" + bgFilename;
                                    var oldBg = archive.GetEntry(bgZipPath);
                                    if (oldBg != null) oldBg.Delete();
                                    ZipArchiveEntry bgZip = archive.CreateEntry(bgZipPath);
                                    using (Stream bs = bgZip.Open())
                                    {
                                        bs.Write(bgBytes, 0, bgBytes.Length);
                                    }
                                    bgImgRelId = "rIdBg1";
                                    imgRelsList.Add(new KeyValuePair<string, string>(bgImgRelId, "../media/" + bgFilename));
                                }
                            }
                            catch { }
                        }

                        foreach (ShapeNode shape in slide.Shapes)
                        {
                            bool isVideoShape = (shape.Type == ShapeType.Video || !string.IsNullOrEmpty(shape.VideoPath));
                            bool isAudioShape = (shape.Type == ShapeType.Audio || !string.IsNullOrEmpty(shape.AudioPath));

                            if (shape.Type == ShapeType.Image || isVideoShape || isAudioShape || shape.CachedImage != null || !string.IsNullOrEmpty(shape.ImagePath))
                            {
                                Image img = shape.CachedImage;
                                bool disposeLoadedImg = false;
                                if (img == null && !string.IsNullOrEmpty(shape.ImagePath) && File.Exists(shape.ImagePath))
                                {
                                    try
                                    {
                                        using (Image tmpImg = Image.FromFile(shape.ImagePath))
                                        {
                                            img = new Bitmap(tmpImg);
                                            disposeLoadedImg = true;
                                        }
                                    }
                                    catch { }
                                }
                                if (img == null && isVideoShape && !string.IsNullOrEmpty(shape.VideoPath) && File.Exists(shape.VideoPath))
                                {
                                    try
                                    {
                                        img = VideoThumbnailHelper.GetVideoPosterFrame(shape.VideoPath);
                                        shape.CachedImage = img;
                                    }
                                    catch { }
                                }
                                if (img == null && (isVideoShape || isAudioShape))
                                {
                                    try
                                    {
                                        Bitmap fallbackBmp = new Bitmap(64, 64);
                                        using (Graphics fg = Graphics.FromImage(fallbackBmp))
                                        {
                                            fg.Clear(isVideoShape ? Color.Black : Color.FromArgb(240, 244, 248));
                                        }
                                        img = fallbackBmp;
                                        disposeLoadedImg = true;
                                    }
                                    catch { }
                                }
                                if (img != null)
                                {
                                    string imgFilename = string.Format("image_s{0}_{1}.png", slideIndex, imgCounter);
                                    string imgZipPath = "ppt/media/" + imgFilename;
                                    try
                                    {
                                        byte[] pngBytes = null;
                                        using (MemoryStream pngMs = new MemoryStream())
                                        {
                                            try
                                            {
                                                img.Save(pngMs, System.Drawing.Imaging.ImageFormat.Png);
                                            }
                                            catch
                                            {
                                                pngMs.SetLength(0);
                                                using (Bitmap bmpCopy = new Bitmap(img))
                                                {
                                                    bmpCopy.Save(pngMs, System.Drawing.Imaging.ImageFormat.Png);
                                                }
                                            }
                                            if (pngMs.Length > 0) pngBytes = pngMs.ToArray();
                                        }
                                        if (pngBytes != null && pngBytes.Length > 0)
                                        {
                                            var oldImg = archive.GetEntry(imgZipPath);
                                            if (oldImg != null) oldImg.Delete();
                                            ZipArchiveEntry imgZip = archive.CreateEntry(imgZipPath);
                                            using (Stream isStream = imgZip.Open())
                                            {
                                                isStream.Write(pngBytes, 0, pngBytes.Length);
                                            }
                                            string rId = string.Format("rIdPic{0}", imgCounter);
                                            shapeImgRels[shape] = rId;
                                            imgRelsList.Add(new KeyValuePair<string, string>(rId, "../media/" + imgFilename));
                                            imgCounter++;
                                        }
                                    }
                                    catch { }
                                    finally
                                    {
                                        if (disposeLoadedImg && img != null)
                                        {
                                            try { img.Dispose(); } catch { }
                                        }
                                    }
                                }
                            }

                            if (isVideoShape && !string.IsNullOrEmpty(shape.VideoPath) && File.Exists(shape.VideoPath))
                            {
                                try
                                {
                                    string vExt = Path.GetExtension(shape.VideoPath);
                                    if (string.IsNullOrEmpty(vExt)) vExt = ".mp4";
                                    string vFilename = string.Format("media_s{0}_{1}{2}", slideIndex, mediaCounter, vExt.ToLowerInvariant());
                                    string vZipPath = "ppt/media/" + vFilename;
                                    var oldV = archive.GetEntry(vZipPath);
                                    if (oldV != null) oldV.Delete();
                                    ZipArchiveEntry vZip = archive.CreateEntry(vZipPath, CompressionLevel.NoCompression);
                                    using (FileStream vIn = new FileStream(shape.VideoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (Stream vOut = vZip.Open())
                                    {
                                        vIn.CopyTo(vOut);
                                    }
                                    string rIdVid = string.Format("rIdVid{0}", mediaCounter);
                                    string rIdMedia = string.Format("rIdMedia{0}", mediaCounter);
                                    shapeVideoRels[shape] = new KeyValuePair<string, string>(rIdVid, rIdMedia);
                                    mediaRelXmlLines.Add(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/video\" Target=\"../media/{1}\"/>", rIdVid, vFilename));
                                    mediaRelXmlLines.Add(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.microsoft.com/office/2007/relationships/media\" Target=\"../media/{1}\"/>", rIdMedia, vFilename));
                                    mediaCounter++;
                                }
                                catch { }
                            }
                            else if (isAudioShape && !string.IsNullOrEmpty(shape.AudioPath) && File.Exists(shape.AudioPath))
                            {
                                try
                                {
                                    string aExt = Path.GetExtension(shape.AudioPath);
                                    if (string.IsNullOrEmpty(aExt)) aExt = ".mp3";
                                    string aFilename = string.Format("audio_s{0}_{1}{2}", slideIndex, mediaCounter, aExt.ToLowerInvariant());
                                    string aZipPath = "ppt/media/" + aFilename;
                                    var oldA = archive.GetEntry(aZipPath);
                                    if (oldA != null) oldA.Delete();
                                    ZipArchiveEntry aZip = archive.CreateEntry(aZipPath, CompressionLevel.NoCompression);
                                    using (FileStream aIn = new FileStream(shape.AudioPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (Stream aOut = aZip.Open())
                                    {
                                        aIn.CopyTo(aOut);
                                    }
                                    string rIdAud = string.Format("rIdAud{0}", mediaCounter);
                                    string rIdMedia = string.Format("rIdMedia{0}", mediaCounter);
                                    shapeAudioRels[shape] = new KeyValuePair<string, string>(rIdAud, rIdMedia);
                                    mediaRelXmlLines.Add(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/audio\" Target=\"../media/{1}\"/>", rIdAud, aFilename));
                                    mediaRelXmlLines.Add(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.microsoft.com/office/2007/relationships/media\" Target=\"../media/{1}\"/>", rIdMedia, aFilename));
                                    mediaCounter++;
                                }
                                catch { }
                            }
                        }

                        bool hasNotes = !string.IsNullOrEmpty(slide.SpeakerNotes);

                        // Write slide rels
                        string sRelPath = string.Format("ppt/slides/_rels/slide{0}.xml.rels", slideIndex);
                        var oldSRel = archive.GetEntry(sRelPath);
                        if (oldSRel != null) oldSRel.Delete();
                        ZipArchiveEntry sRelEntry = archive.CreateEntry(sRelPath);
                        using (StreamWriter sw = new StreamWriter(sRelEntry.Open(), new UTF8Encoding(false)))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                            sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout1.xml\"/>");
                            if (hasNotes)
                            {
                                sw.WriteLine(string.Format("  <Relationship Id=\"rIdNotes1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesSlide\" Target=\"../notesSlides/notesSlide{0}.xml\"/>", slideIndex));
                            }
                            foreach (var ir in imgRelsList)
                            {
                                sw.WriteLine(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"{1}\"/>", ir.Key, ir.Value));
                            }
                            foreach (string mRelLine in mediaRelXmlLines)
                            {
                                sw.WriteLine(mRelLine);
                            }
                            sw.WriteLine("</Relationships>");
                        }

                        if (hasNotes)
                        {
                            string notesPath = string.Format("ppt/notesSlides/notesSlide{0}.xml", slideIndex);
                            var oldNotes = archive.GetEntry(notesPath);
                            if (oldNotes != null) oldNotes.Delete();
                            ZipArchiveEntry notesEntry = archive.CreateEntry(notesPath);
                            using (StreamWriter sw = new StreamWriter(notesEntry.Open(), new UTF8Encoding(false)))
                            {
                                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                                sw.WriteLine("<p:notes xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">");
                                sw.WriteLine("  <p:cSld><p:spTree>");
                                sw.WriteLine("    <p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
                                sw.WriteLine("    <p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");
                                sw.WriteLine("    <p:sp>");
                                sw.WriteLine("      <p:nvSpPr><p:cNvPr id=\"2\" name=\"Notes Placeholder 1\"/><p:cNvSpPr><a:spLocks noGrp=\"1\"/></p:cNvSpPr><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>");
                                sw.WriteLine("      <p:spPr/>");
                                sw.WriteLine("      <p:txBody><a:bodyPr/><a:lstStyle/>");
                                string[] noteLines = slide.SpeakerNotes.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                                foreach (string nLine in noteLines)
                                {
                                    sw.WriteLine(string.Format("        <a:p><a:r><a:rPr lang=\"vi-VN\" sz=\"1200\"/><a:t>{0}</a:t></a:r></a:p>", EscapeXml(nLine)));
                                }
                                sw.WriteLine("      </p:txBody>");
                                sw.WriteLine("    </p:sp>");
                                sw.WriteLine("  </p:spTree></p:cSld>");
                                sw.WriteLine("</p:notes>");
                            }
                        }

                        // Write slide XML
                        string sXmlPath = string.Format("ppt/slides/slide{0}.xml", slideIndex);
                        var oldSXml = archive.GetEntry(sXmlPath);
                        if (oldSXml != null) oldSXml.Delete();
                        ZipArchiveEntry slideEntry = archive.CreateEntry(sXmlPath);
                        using (StreamWriter sw = new StreamWriter(slideEntry.Open(), new UTF8Encoding(false)))
                        {
                            WriteSlideXml(sw, slide, slideIndex, exportScaleX, exportScaleY, shapeImgRels, pres, shapeVideoRels, shapeAudioRels, bgImgRelId);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXPORT PPTX ERROR: " + ex.Message + "\n" + ex.StackTrace);
                return SavePptxXml(filepath, pres);
            }
        }

        private static void WriteThemeXml(StreamWriter sw)
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sw.WriteLine("<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Office Theme\">");
            sw.WriteLine("  <a:themeElements>");
            sw.WriteLine("    <a:clrScheme name=\"Office\">");
            sw.WriteLine("      <a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1>");
            sw.WriteLine("      <a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>");
            sw.WriteLine("      <a:dk2><a:srgbClr val=\"44546A\"/></a:dk2>");
            sw.WriteLine("      <a:lt2><a:srgbClr val=\"E7E6E6\"/></a:lt2>");
            sw.WriteLine("      <a:accent1><a:srgbClr val=\"4472C4\"/></a:accent1>");
            sw.WriteLine("      <a:accent2><a:srgbClr val=\"ED7D31\"/></a:accent2>");
            sw.WriteLine("      <a:accent3><a:srgbClr val=\"A5A5A5\"/></a:accent3>");
            sw.WriteLine("      <a:accent4><a:srgbClr val=\"FFC000\"/></a:accent4>");
            sw.WriteLine("      <a:accent5><a:srgbClr val=\"5B9BD5\"/></a:accent5>");
            sw.WriteLine("      <a:accent6><a:srgbClr val=\"70AD47\"/></a:accent6>");
            sw.WriteLine("      <a:hlink><a:srgbClr val=\"0563C1\"/></a:hlink>");
            sw.WriteLine("      <a:folHlink><a:srgbClr val=\"954F72\"/></a:folHlink>");
            sw.WriteLine("    </a:clrScheme>");
            sw.WriteLine("    <a:fontScheme name=\"Office\">");
            sw.WriteLine("      <a:majorFont><a:latin typeface=\"Segoe UI\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:majorFont>");
            sw.WriteLine("      <a:minorFont><a:latin typeface=\"Segoe UI\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:minorFont>");
            sw.WriteLine("    </a:fontScheme>");
            sw.WriteLine("    <a:fmtScheme name=\"Office\">");
            sw.WriteLine("      <a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:gradFill rotWithShape=\"1\"><a:gsLst><a:gs pos=\"0\"><a:schemeClr val=\"phClr\"><a:lumMod val=\"110000\"/><a:satMod val=\"105000\"/><a:tint val=\"67000\"/></a:schemeClr></a:gs><a:gs pos=\"50000\"><a:schemeClr val=\"phClr\"><a:lumMod val=\"105000\"/><a:satMod val=\"103000\"/><a:tint val=\"73000\"/></a:schemeClr></a:gs><a:gs pos=\"100000\"><a:schemeClr val=\"phClr\"><a:lumMod val=\"105000\"/><a:satMod val=\"109000\"/><a:tint val=\"81000\"/></a:schemeClr></a:gs></a:gsLst><a:lin ang=\"5400000\" scaled=\"0\"/></a:gradFill></a:fillStyleLst>");
            sw.WriteLine("      <a:lnStyleLst><a:ln w=\"6350\" cmpd=\"s\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln><a:ln w=\"12700\" cmpd=\"s\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln><a:ln w=\"19050\" cmpd=\"s\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln></a:lnStyleLst>");
            sw.WriteLine("      <a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst>");
            sw.WriteLine("      <a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:bgFillStyleLst>");
            sw.WriteLine("    </a:fmtScheme>");
            sw.WriteLine("  </a:themeElements>");
            sw.WriteLine("</a:theme>");
        }

        private static void WriteSlideMasterXml(StreamWriter sw)
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sw.WriteLine("<p:sldMaster xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">");
            sw.WriteLine("  <p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld>");
            sw.WriteLine("  <p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>");
            sw.WriteLine("  <p:sldLayoutIdLst>");
            for (int lay = 1; lay <= 11; lay++)
            {
                sw.WriteLine(string.Format("    <p:sldLayoutId id=\"{0}\" r:id=\"rId{1}\"/>", 2147483648L + lay, lay));
            }
            sw.WriteLine("  </p:sldLayoutIdLst>");
            sw.WriteLine("  <p:txStyles>");
            sw.WriteLine("    <p:titleStyle><a:lvl1pPr algn=\"l\"><a:defRPr sz=\"4400\" kern=\"1200\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill><a:latin typeface=\"+mj-lt\"/></a:defRPr></a:lvl1pPr></p:titleStyle>");
            sw.WriteLine("    <p:bodyStyle><a:lvl1pPr algn=\"l\"><a:defRPr sz=\"2800\" kern=\"1200\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill><a:latin typeface=\"+mn-lt\"/></a:defRPr></a:lvl1pPr></p:bodyStyle>");
            sw.WriteLine("    <p:otherStyle><a:defPPr><a:defRPr lang=\"en-US\"/></a:defPPr><a:lvl1pPr algn=\"l\"><a:defRPr sz=\"1800\" kern=\"1200\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill><a:latin typeface=\"+mn-lt\"/></a:defRPr></a:lvl1pPr></p:otherStyle>");
            sw.WriteLine("  </p:txStyles>");
            sw.WriteLine("</p:sldMaster>");
        }

        private static void WriteSlideLayoutXml(StreamWriter sw)
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sw.WriteLine("<p:sldLayout xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" type=\"blank\" preserve=\"1\">");
            sw.WriteLine("  <p:cSld name=\"Blank\"><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld>");
            sw.WriteLine("  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>");
            sw.WriteLine("</p:sldLayout>");
        }

        private static void WriteSlideXml(
            StreamWriter sw,
            SlideNode slide,
            int slideIndex,
            float exportScaleX,
            float exportScaleY,
            Dictionary<ShapeNode, string> shapeImgRels,
            PresentationRoot pres,
            Dictionary<ShapeNode, KeyValuePair<string, string>> shapeVideoRels = null,
            Dictionary<ShapeNode, KeyValuePair<string, string>> shapeAudioRels = null,
            string bgImgRelId = null)
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sw.WriteLine("<p:sld xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">");
            sw.WriteLine("  <p:cSld>");

            // 1. Slide Background
            if (!string.IsNullOrEmpty(bgImgRelId))
            {
                sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:blipFill><a:blip r:embed=\"{0}\"/><a:stretch><a:fillRect/></a:stretch></a:blipFill></p:bgPr></p:bg>", bgImgRelId));
            }
            else if (slide.IsGradientBg)
            {
                int c1 = slide.BgColor;
                int c2 = slide.BgColor2;
                int ang = (int)(slide.BgGradientAngle * 60000);
                if (slide.BgGradientStops != null && slide.BgGradientStops.Count >= 2)
                {
                    sw.WriteLine("    <p:bg><p:bgPr><a:gradFill rotWithShape=\"0\"><a:gsLst>");
                    foreach (var st in slide.BgGradientStops)
                    {
                        int pos = (int)Math.Round(st.Position * 100000.0f);
                        float a = 1.0f - st.Transparency;
                        int aVal = (int)Math.Round(Math.Max(0.0f, Math.Min(1.0f, a)) * 100000.0f);
                        if (aVal < 100000)
                        {
                            sw.WriteLine(string.Format("      <a:gs pos=\"{0}\"><a:srgbClr val=\"{1:X6}\"><a:alpha val=\"{2}\"/></a:srgbClr></a:gs>", pos, st.Color & 0xFFFFFF, aVal));
                        }
                        else
                        {
                            sw.WriteLine(string.Format("      <a:gs pos=\"{0}\"><a:srgbClr val=\"{1:X6}\"/></a:gs>", pos, st.Color & 0xFFFFFF));
                        }
                    }
                    sw.WriteLine(string.Format("    </a:gsLst><a:lin ang=\"{0}\"/></a:gradFill></p:bgPr></p:bg>", ang));
                }
                else
                {
                    sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:gradFill rotWithShape=\"0\"><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"{0:X6}\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"{1:X6}\"/></a:gs></a:gsLst><a:lin ang=\"{2}\"/></a:gradFill></p:bgPr></p:bg>", c1 & 0xFFFFFF, c2 & 0xFFFFFF, ang));
                }
            }
            else
            {
                sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"{0:X6}\"/></a:solidFill></p:bgPr></p:bg>", slide.BgColor & 0xFFFFFF));
            }

            // 2. Shape Tree
            sw.WriteLine("    <p:spTree>");
            sw.WriteLine("      <p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
            sw.WriteLine("      <p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");

            float normScale = 1.0f;

            Dictionary<ShapeNode, int> shapeIdMap = new Dictionary<ShapeNode, int>();

            for (int s = 0; s < slide.Shapes.Count; s++)
            {
                ShapeNode shape = slide.Shapes[s];
                int spId = s + 2;
                shapeIdMap[shape] = spId;
                string shapeName = !string.IsNullOrEmpty(shape.MorphName) ? shape.MorphName : (!string.IsNullOrEmpty(shape.Name) ? shape.Name : ("Shape " + spId));

                long offX = (long)(shape.Transform.X * exportScaleX * 9525.0f);
                long offY = (long)(shape.Transform.Y * exportScaleY * 9525.0f);
                long extW = Math.Max(9525L, (long)(shape.Transform.Width * exportScaleX * 9525.0f));
                long extH = Math.Max(9525L, (long)(shape.Transform.Height * exportScaleY * 9525.0f));

                // Case A: Pictures & Media (<p:pic>)
                if (shapeImgRels.ContainsKey(shape))
                {
                    string rId = shapeImgRels[shape];
                    string nvPrXml = "<p:nvPr/>";
                    if (shapeVideoRels != null && shapeVideoRels.ContainsKey(shape))
                    {
                        var vr = shapeVideoRels[shape];
                        nvPrXml = string.Format("<p:nvPr><a:videoFile r:link=\"{0}\"/><p:extLst><p:ext uri=\"{{DAA4B4D4-6D71-4841-9C94-3DE7FCFB9230}}\"><p14:media xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" r:embed=\"{1}\"/></p:ext></p:extLst></p:nvPr>", vr.Key, vr.Value);
                    }
                    else if (shapeAudioRels != null && shapeAudioRels.ContainsKey(shape))
                    {
                        var ar = shapeAudioRels[shape];
                        nvPrXml = string.Format("<p:nvPr><a:audioFile r:link=\"{0}\"/><p:extLst><p:ext uri=\"{{DAA4B4D4-6D71-4841-9C94-3DE7FCFB9230}}\"><p14:media xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" r:embed=\"{1}\"/></p:ext></p:extLst></p:nvPr>", ar.Key, ar.Value);
                    }
                    sw.WriteLine("      <p:pic>");
                    sw.WriteLine(string.Format("        <p:nvPicPr><p:cNvPr id=\"{0}\" name=\"{1}\"/><p:cNvPicPr><a:picLocks noChangeAspect=\"0\"/></p:cNvPicPr>{2}</p:nvPicPr>", spId, EscapeXml(shapeName), nvPrXml));
                    string srcRectXml = "";
                    if (shape.CropLeft > 0.0001f || shape.CropTop > 0.0001f || shape.CropRight > 0.0001f || shape.CropBottom > 0.0001f)
                    {
                        srcRectXml = string.Format("<a:srcRect l=\"{0}\" t=\"{1}\" r=\"{2}\" b=\"{3}\"/>",
                            (int)(shape.CropLeft * 100000), (int)(shape.CropTop * 100000),
                            (int)(shape.CropRight * 100000), (int)(shape.CropBottom * 100000));
                    }
                    string fillRectXml = "<a:fillRect/>";
                    if (Math.Abs(shape.FillRectLeft) > 0.0001f || Math.Abs(shape.FillRectTop) > 0.0001f || Math.Abs(shape.FillRectRight) > 0.0001f || Math.Abs(shape.FillRectBottom) > 0.0001f)
                    {
                        fillRectXml = string.Format("<a:fillRect l=\"{0}\" t=\"{1}\" r=\"{2}\" b=\"{3}\"/>",
                            (int)(shape.FillRectLeft * 100000), (int)(shape.FillRectTop * 100000),
                            (int)(shape.FillRectRight * 100000), (int)(shape.FillRectBottom * 100000));
                    }
                    string blipAlphaXml = "";
                    if (shape.FillOpacity < 0.999f)
                    {
                        blipAlphaXml = string.Format("<a:alphaModFix amt=\"{0}\"/>", (int)(shape.FillOpacity * 100000));
                    }
                    sw.WriteLine(string.Format("        <p:blipFill><a:blip r:embed=\"{0}\">{1}</a:blip>{2}<a:stretch>{3}</a:stretch></p:blipFill>", rId, blipAlphaXml, srcRectXml, fillRectXml));
                    sw.WriteLine("        <p:spPr>");
                    sw.WriteLine("          <a:xfrm>");
                    sw.WriteLine(string.Format("            <a:off x=\"{0}\" y=\"{1}\"/>", offX, offY));
                    sw.WriteLine(string.Format("            <a:ext cx=\"{0}\" cy=\"{1}\"/>", extW, extH));
                    if (Math.Abs(shape.Transform.Rotation) > 0.01f)
                    {
                        sw.WriteLine(string.Format("            <a:rot rot=\"{0}\"/>", (int)(shape.Transform.Rotation * 60000)));
                    }
                    sw.WriteLine("          </a:xfrm>");
                    sw.WriteLine("          <a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
                    if (shape.SoftEdgeRadius > 0.01f || shape.BlurRadius > 0.01f || shape.HasShadow)
                    {
                        sw.WriteLine("          <a:effectLst>");
                        if (shape.SoftEdgeRadius > 0.01f)
                        {
                            sw.WriteLine(string.Format("            <a:softEdge rad=\"{0}\"/>", (long)(shape.SoftEdgeRadius * 12700.0f)));
                        }
                        if (shape.BlurRadius > 0.01f)
                        {
                            sw.WriteLine(string.Format("            <a:blur rad=\"{0}\"/>", (long)(shape.BlurRadius * 12700.0f)));
                        }
                        if (shape.HasShadow)
                        {
                            float dist = (float)Math.Sqrt(shape.ShadowOffsetX * shape.ShadowOffsetX + shape.ShadowOffsetY * shape.ShadowOffsetY);
                            float angleRad = (float)Math.Atan2(shape.ShadowOffsetY, shape.ShadowOffsetX);
                            long dirVal = (long)((angleRad * 180.0 / Math.PI) * 60000);
                            if (dirVal < 0) dirVal += 360 * 60000;
                            long distEmu = (long)(dist * 9525.0f);
                            long blurEmu = (long)(shape.ShadowBlur * 9525.0f);
                            int alphaVal = (int)((((shape.ShadowColor >> 24) & 0xFF) / 255.0f) * 100000);
                            if (alphaVal <= 0) alphaVal = 35000;
                            sw.WriteLine(string.Format("            <a:outerShdw blurRad=\"{0}\" dist=\"{1}\" dir=\"{2}\" algn=\"ctr\"><a:srgbClr val=\"{3:X6}\"><a:alpha val=\"{4}\"/></a:srgbClr></a:outerShdw>",
                                blurEmu, distEmu, dirVal, shape.ShadowColor & 0xFFFFFF, alphaVal));
                        }
                        sw.WriteLine("          </a:effectLst>");
                    }
                    sw.WriteLine("        </p:spPr>");
                    sw.WriteLine("      </p:pic>");
                }
                // Case B: Tables (<p:graphicFrame>)
                else if (shape.Type == ShapeType.Table && shape.TableData != null && shape.TableData.Count > 0)
                {
                    int numRows = shape.TableData.Count;
                    int numCols = shape.TableData[0].Count;
                    long colW = numCols > 0 ? (extW / numCols) : extW;
                    long rowH = numRows > 0 ? (extH / numRows) : extH;

                    sw.WriteLine("      <p:graphicFrame>");
                    sw.WriteLine(string.Format("        <p:nvGraphicFramePr><p:cNvPr id=\"{0}\" name=\"{1}\"/><p:cNvGraphicFramePr><a:graphicFrameLocks noGrp=\"1\"/></p:cNvGraphicFramePr><p:nvPr/></p:nvGraphicFramePr>", spId, EscapeXml(shapeName)));
                    sw.WriteLine(string.Format("        <p:xfrm><a:off x=\"{0}\" y=\"{1}\"/><a:ext cx=\"{2}\" cy=\"{3}\"/></p:xfrm>", offX, offY, extW, extH));
                    sw.WriteLine("        <a:graphic>");
                    sw.WriteLine("          <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">");
                    sw.WriteLine(string.Format("            <a:tbl><a:tblPr firstRow=\"{0}\" bandRow=\"{1}\"/>", shape.TableHeaderRow ? "1" : "0", shape.TableBandedRows ? "1" : "0"));
                    sw.WriteLine("              <a:tblGrid>");
                    for (int c = 0; c < numCols; c++) sw.WriteLine(string.Format("                <a:gridCol w=\"{0}\"/>", colW));
                    sw.WriteLine("              </a:tblGrid>");
                    for (int r = 0; r < numRows; r++)
                    {
                        sw.WriteLine(string.Format("              <a:tr h=\"{0}\">", rowH));
                        for (int c = 0; c < numCols; c++)
                        {
                            string cellTxt = (c < shape.TableData[r].Count) ? shape.TableData[r][c] : "";
                            string cellKey = string.Format("{0}_{1}", r, c);
                            int cellBg = 0xFFFFFF;
                            bool hasCustomBg = false;
                            if (shape.TableCellFillColors != null && shape.TableCellFillColors.ContainsKey(cellKey))
                            {
                                cellBg = shape.TableCellFillColors[cellKey];
                                hasCustomBg = true;
                            }
                            else if (r == 0 && shape.TableHeaderRow)
                            {
                                cellBg = 0x2563EB;
                                hasCustomBg = true;
                            }
                            else if (shape.TableBandedRows && r % 2 == 1)
                            {
                                cellBg = 0xF8FAFC;
                                hasCustomBg = true;
                            }

                            int cellTxtClr = (r == 0 && shape.TableHeaderRow) ? 0xFFFFFF : 0x000000;
                            if (shape.TableCellTextColors != null && shape.TableCellTextColors.ContainsKey(cellKey))
                                cellTxtClr = shape.TableCellTextColors[cellKey];

                            bool isCellBold = (r == 0 && shape.TableHeaderRow);
                            if (shape.TableCellBolds != null && shape.TableCellBolds.ContainsKey(cellKey))
                                isCellBold = shape.TableCellBolds[cellKey];

                            sw.WriteLine("                <a:tc>");
                            sw.WriteLine("                  <a:txBody>");
                            sw.WriteLine("                    <a:bodyPr anchor=\"ctr\"/>");
                            sw.WriteLine("                    <a:lstStyle/>");
                            sw.WriteLine("                    <a:p>");
                            sw.WriteLine("                      <a:pPr algn=\"ctr\"/>");
                            sw.WriteLine("                      <a:r>");
                            sw.WriteLine(string.Format("                        <a:rPr sz=\"1400\" b=\"{0}\"><a:solidFill><a:srgbClr val=\"{1:X6}\"/></a:solidFill><a:latin typeface=\"{2}\"/></a:rPr>", isCellBold ? "1" : "0", cellTxtClr & 0xFFFFFF, EscapeXml(shape.FontName ?? "Calibri")));
                            sw.WriteLine(string.Format("                        <a:t>{0}</a:t>", EscapeXml(cellTxt)));
                            sw.WriteLine("                      </a:r>");
                            sw.WriteLine("                    </a:p>");
                            sw.WriteLine("                  </a:txBody>");
                            if (hasCustomBg)
                            {
                                sw.WriteLine(string.Format("                  <a:tcPr><a:solidFill><a:srgbClr val=\"{0:X6}\"/></a:solidFill></a:tcPr>", cellBg & 0xFFFFFF));
                            }
                            else
                            {
                                sw.WriteLine("                  <a:tcPr/>");
                            }
                            sw.WriteLine("                </a:tc>");
                        }
                        sw.WriteLine("              </a:tr>");
                    }
                    sw.WriteLine("            </a:tbl>");
                    sw.WriteLine("          </a:graphicData>");
                    sw.WriteLine("        </a:graphic>");
                    sw.WriteLine("      </p:graphicFrame>");
                }
                // Case C: Shapes & Text Boxes (<p:sp>)
                else
                {
                    sw.WriteLine("      <p:sp>");
                    sw.WriteLine("        <p:nvSpPr>");
                    sw.WriteLine(string.Format("          <p:cNvPr id=\"{0}\" name=\"{1}\"/>", spId, EscapeXml(shapeName)));
                    sw.WriteLine("          <p:cNvSpPr/>");
                    sw.WriteLine("          <p:nvPr/>");
                    sw.WriteLine("        </p:nvSpPr>");
                    sw.WriteLine("        <p:spPr>");
                    sw.WriteLine("          <a:xfrm>");
                    sw.WriteLine(string.Format("            <a:off x=\"{0}\" y=\"{1}\"/>", offX, offY));
                    sw.WriteLine(string.Format("            <a:ext cx=\"{0}\" cy=\"{1}\"/>", extW, extH));
                    if (Math.Abs(shape.Transform.Rotation) > 0.01f)
                    {
                        sw.WriteLine(string.Format("            <a:rot rot=\"{0}\"/>", (int)(shape.Transform.Rotation * 60000)));
                    }
                    sw.WriteLine("          </a:xfrm>");

                    // Geometry
                    if (shape.Type == ShapeType.CustomPath && shape.CustomPath != null)
                    {
                        WriteCustomPathXml(sw, shape.CustomPath, shape.PathWidth, shape.PathHeight);
                    }
                    else
                    {
                        sw.WriteLine(string.Format("          <a:prstGeom prst=\"{0}\"><a:avLst/></a:prstGeom>", GetOpenXmlGeomName(shape.Type)));
                    }

                    // Fill
                    if (shape.HasGradientFill)
                    {
                        int ang = (int)(shape.GradientAngle * 60000);
                        if (shape.GradientStops != null && shape.GradientStops.Count >= 2)
                        {
                            sw.WriteLine("          <a:gradFill rotWithShape=\"1\"><a:gsLst>");
                            foreach (var gst in shape.GradientStops)
                            {
                                int pPos = (int)Math.Round(gst.Position * 100000.0f);
                                float a = 1.0f - gst.Transparency;
                                int aVal = (int)Math.Round(Math.Max(0.0f, Math.Min(1.0f, a)) * 100000.0f);
                                if (aVal < 100000)
                                {
                                    sw.WriteLine(string.Format("            <a:gs pos=\"{0}\"><a:srgbClr val=\"{1:X6}\"><a:alpha val=\"{2}\"/></a:srgbClr></a:gs>", pPos, gst.Color & 0xFFFFFF, aVal));
                                }
                                else
                                {
                                    sw.WriteLine(string.Format("            <a:gs pos=\"{0}\"><a:srgbClr val=\"{1:X6}\"/></a:gs>", pPos, gst.Color & 0xFFFFFF));
                                }
                            }
                            sw.WriteLine(string.Format("          </a:gsLst><a:lin ang=\"{0}\"/></a:gradFill>", ang));
                        }
                        else
                        {
                            int c1 = shape.FillColor;
                            int c2 = shape.GradientColor2;
                            sw.WriteLine(string.Format("          <a:gradFill rotWithShape=\"1\"><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"{0:X6}\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"{1:X6}\"/></a:gs></a:gsLst><a:lin ang=\"{2}\"/></a:gradFill>", c1 & 0xFFFFFF, c2 & 0xFFFFFF, ang));
                        }
                    }
                    else if (shape.FillColor != 0x00FFFFFF)
                    {
                        if (shape.FillOpacity < 0.999f)
                        {
                            int alphaVal = Math.Max(0, Math.Min(100000, (int)(shape.FillOpacity * 100000)));
                            sw.WriteLine(string.Format("          <a:solidFill><a:srgbClr val=\"{0:X6}\"><a:alpha val=\"{1}\"/></a:srgbClr></a:solidFill>", shape.FillColor & 0xFFFFFF, alphaVal));
                        }
                        else
                        {
                            sw.WriteLine(string.Format("          <a:solidFill><a:srgbClr val=\"{0:X6}\"/></a:solidFill>", shape.FillColor & 0xFFFFFF));
                        }
                    }
                    else
                    {
                        sw.WriteLine("          <a:noFill/>");
                    }

                    // Stroke
                    if (shape.StrokeWidth > 0.01f)
                    {
                        sw.WriteLine(string.Format("          <a:ln w=\"{0}\"><a:solidFill><a:srgbClr val=\"{1:X6}\"/></a:solidFill></a:ln>", (int)(shape.StrokeWidth * exportScaleX * 12700.0f), shape.StrokeColor & 0xFFFFFF));
                    }

                    if (shape.SoftEdgeRadius > 0.01f || shape.BlurRadius > 0.01f || shape.HasShadow)
                    {
                        sw.WriteLine("          <a:effectLst>");
                        if (shape.SoftEdgeRadius > 0.01f)
                        {
                            sw.WriteLine(string.Format("            <a:softEdge rad=\"{0}\"/>", (long)(shape.SoftEdgeRadius * 12700.0f)));
                        }
                        if (shape.BlurRadius > 0.01f)
                        {
                            sw.WriteLine(string.Format("            <a:blur rad=\"{0}\"/>", (long)(shape.BlurRadius * 12700.0f)));
                        }
                        if (shape.HasShadow)
                        {
                            float dist = (float)Math.Sqrt(shape.ShadowOffsetX * shape.ShadowOffsetX + shape.ShadowOffsetY * shape.ShadowOffsetY);
                            float angleRad = (float)Math.Atan2(shape.ShadowOffsetY, shape.ShadowOffsetX);
                            long dirVal = (long)((angleRad * 180.0 / Math.PI) * 60000);
                            if (dirVal < 0) dirVal += 360 * 60000;
                            long distEmu = (long)(dist * 9525.0f);
                            long blurEmu = (long)(shape.ShadowBlur * 9525.0f);
                            int alphaVal = (int)((((shape.ShadowColor >> 24) & 0xFF) / 255.0f) * 100000);
                            if (alphaVal <= 0) alphaVal = 35000;
                            sw.WriteLine(string.Format("            <a:outerShdw blurRad=\"{0}\" dist=\"{1}\" dir=\"{2}\" algn=\"ctr\"><a:srgbClr val=\"{3:X6}\"><a:alpha val=\"{4}\"/></a:srgbClr></a:outerShdw>",
                                blurEmu, distEmu, dirVal, shape.ShadowColor & 0xFFFFFF, alphaVal));
                        }
                        sw.WriteLine("          </a:effectLst>");
                    }

                    sw.WriteLine("        </p:spPr>");

                    // Text
                    if (!string.IsNullOrEmpty(shape.Text))
                    {
                        string ancStr = "t";
                        if (shape.VerticalAlignment == VerticalTextAlignment.Bottom) ancStr = "b";
                        else if (shape.VerticalAlignment == VerticalTextAlignment.Middle) ancStr = "ctr";

                        string algnStr = "l";
                        if (shape.Alignment == TextAlignment.Center) algnStr = "ctr";
                        else if (shape.Alignment == TextAlignment.Right) algnStr = "r";
                        else if (shape.Alignment == TextAlignment.Justify) algnStr = "just";

                        string lnSpcXml = "";
                        if (shape.LineSpacing < 0)
                        {
                            int pct = (int)(-shape.LineSpacing * 100000);
                            lnSpcXml = string.Format("<a:lnSpc><a:spcPct val=\"{0}\"/></a:lnSpc>", pct);
                        }
                        else if (shape.LineSpacing > 4.0f)
                        {
                            float pts = (shape.LineSpacing / Math.Max(0.001f, exportScaleX)) * (72.0f / 96.0f);
                            int pts100 = (int)(pts * 100);
                            lnSpcXml = string.Format("<a:lnSpc><a:spcPts val=\"{0}\"/></a:lnSpc>", pts100);
                        }

                        float fontPt = (shape.FontSize / Math.Max(0.001f, normScale)) * (72.0f / 96.0f);
                        int szPt100 = Math.Max(400, (int)(fontPt * 100.0f));

                        int lInsEmu = shape.MarginLeft.HasValue ? (int)(shape.MarginLeft.Value * exportScaleX * 9525.0f) : 91440;
                        int rInsEmu = shape.MarginRight.HasValue ? (int)(shape.MarginRight.Value * exportScaleX * 9525.0f) : 91440;
                        int tInsEmu = shape.MarginTop.HasValue ? (int)(shape.MarginTop.Value * exportScaleY * 9525.0f) : 45720;
                        int bInsEmu = shape.MarginBottom.HasValue ? (int)(shape.MarginBottom.Value * exportScaleY * 9525.0f) : 45720;

                        sw.WriteLine("        <p:txBody>");
                        sw.WriteLine(string.Format("          <a:bodyPr anchor=\"{0}\" wrap=\"square\" lIns=\"{1}\" rIns=\"{2}\" tIns=\"{3}\" bIns=\"{4}\"/>", ancStr, lInsEmu, rInsEmu, tInsEmu, bInsEmu));
                        sw.WriteLine("          <a:lstStyle/>");
                        if (shape.TextRuns != null && shape.TextRuns.Count > 0)
                        {
                            List<List<SlideTextRun>> paragraphs = new List<List<SlideTextRun>>();
                            List<SlideTextRun> currentP = new List<SlideTextRun>();
                            foreach (var r in shape.TextRuns)
                            {
                                if (string.IsNullOrEmpty(r.Text)) continue;
                                string rText = r.Text;
                                string[] parts = rText.Split('\n');
                                for (int pi = 0; pi < parts.Length; pi++)
                                {
                                    if (pi > 0)
                                    {
                                        paragraphs.Add(currentP);
                                        currentP = new List<SlideTextRun>();
                                    }
                                    if (!string.IsNullOrEmpty(parts[pi]))
                                    {
                                        currentP.Add(new SlideTextRun(parts[pi], r.TextColor, r.FontSize, r.Bold, r.FontName)
                                        {
                                            Italic = r.Italic,
                                            Underline = r.Underline,
                                            HasHighlight = r.HasHighlight,
                                            HighlightColor = r.HighlightColor,
                                            HasGradient = r.HasGradient,
                                            GradientColor2 = r.GradientColor2,
                                            GradientAngle = r.GradientAngle,
                                            GradientStops = r.GradientStops
                                        });
                                    }
                                }
                            }
                            if (currentP.Count > 0) paragraphs.Add(currentP);

                            foreach (var p in paragraphs)
                            {
                                sw.WriteLine("          <a:p>");
                                sw.WriteLine(string.Format("            <a:pPr algn=\"{0}\">{1}</a:pPr>", algnStr, lnSpcXml));
                                foreach (var run in p)
                                {
                                    float rFontPt = (run.FontSize / Math.Max(0.001f, normScale)) * (72.0f / 96.0f);
                                    int rSz = Math.Max(400, (int)(rFontPt * 100.0f));
                                    sw.WriteLine("            <a:r>");
                                    if (run.HasGradient)
                                    {
                                        sw.WriteLine(string.Format("              <a:rPr sz=\"{0}\" b=\"{1}\" i=\"{2}\" u=\"{3}\">",
                                            rSz, run.Bold ? "1" : "0", run.Italic ? "1" : "0", run.Underline ? "sng" : "none"));
                                        if (run.GradientStops != null && run.GradientStops.Count >= 2)
                                        {
                                            sw.WriteLine("                <a:gradFill><a:gsLst>");
                                            foreach (var gst in run.GradientStops)
                                            {
                                                int pPos = (int)Math.Round(gst.Position * 100000.0f);
                                                float a = 1.0f - gst.Transparency;
                                                int aVal = (int)Math.Round(Math.Max(0.0f, Math.Min(1.0f, a)) * 100000.0f);
                                                if (aVal < 100000)
                                                {
                                                    sw.WriteLine(string.Format("                  <a:gs pos=\"{0}\"><a:srgbClr val=\"{1:X6}\"><a:alpha val=\"{2}\"/></a:srgbClr></a:gs>", pPos, gst.Color & 0xFFFFFF, aVal));
                                                }
                                                else
                                                {
                                                    sw.WriteLine(string.Format("                  <a:gs pos=\"{0}\"><a:srgbClr val=\"{1:X6}\"/></a:gs>", pPos, gst.Color & 0xFFFFFF));
                                                }
                                            }
                                            sw.WriteLine(string.Format("                </a:gsLst><a:lin ang=\"{0}\"/></a:gradFill>", (int)(run.GradientAngle * 60000)));
                                        }
                                        else
                                        {
                                            sw.WriteLine(string.Format("                <a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"{0:X6}\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"{1:X6}\"/></a:gs></a:gsLst><a:lin ang=\"{2}\"/></a:gradFill>",
                                                run.TextColor & 0xFFFFFF, run.GradientColor2 & 0xFFFFFF, (int)(run.GradientAngle * 60000)));
                                        }
                                        if (run.HasHighlight)
                                        {
                                            sw.WriteLine(string.Format("                <a:highlight><a:srgbClr val=\"{0:X6}\"/></a:highlight>", run.HighlightColor & 0xFFFFFF));
                                        }
                                        sw.WriteLine(string.Format("                <a:latin typeface=\"{0}\"/>", EscapeXml(run.FontName ?? shape.FontName ?? "Calibri")));
                                        sw.WriteLine("              </a:rPr>");
                                    }
                                    else
                                    {
                                        string hlTag = run.HasHighlight ? string.Format("<a:highlight><a:srgbClr val=\"{0:X6}\"/></a:highlight>", run.HighlightColor & 0xFFFFFF) : "";
                                        sw.WriteLine(string.Format("              <a:rPr sz=\"{0}\" b=\"{1}\" i=\"{2}\" u=\"{3}\"><a:solidFill><a:srgbClr val=\"{4:X6}\"/></a:solidFill>{5}<a:latin typeface=\"{6}\"/></a:rPr>",
                                            rSz,
                                            run.Bold ? "1" : "0",
                                            run.Italic ? "1" : "0",
                                            run.Underline ? "sng" : "none",
                                            run.TextColor & 0xFFFFFF,
                                            hlTag,
                                            EscapeXml(run.FontName ?? shape.FontName ?? "Calibri")));
                                    }
                                    sw.WriteLine(string.Format("              <a:t>{0}</a:t>", EscapeXml(run.Text)));
                                    sw.WriteLine("            </a:r>");
                                }
                                sw.WriteLine("          </a:p>");
                            }
                        }
                        else
                        {
                            string[] lines = shape.Text.Split('\n');
                            foreach (string line in lines)
                            {
                                sw.WriteLine("          <a:p>");
                                sw.WriteLine(string.Format("            <a:pPr algn=\"{0}\">{1}</a:pPr>", algnStr, lnSpcXml));
                                sw.WriteLine("            <a:r>");
                                sw.WriteLine(string.Format("              <a:rPr sz=\"{0}\" b=\"{1}\" i=\"{2}\" u=\"{3}\"><a:solidFill><a:srgbClr val=\"{4:X6}\"/></a:solidFill><a:latin typeface=\"{5}\"/></a:rPr>",
                                    szPt100,
                                    shape.Bold ? "1" : "0",
                                    shape.Italic ? "1" : "0",
                                    shape.Underline ? "sng" : "none",
                                    shape.TextColor & 0xFFFFFF,
                                    EscapeXml(shape.FontName ?? "Calibri")));
                                sw.WriteLine(string.Format("              <a:t>{0}</a:t>", EscapeXml(line)));
                                sw.WriteLine("            </a:r>");
                                sw.WriteLine("          </a:p>");
                            }
                        }
                        sw.WriteLine("        </p:txBody>");
                    }

                    sw.WriteLine("      </p:sp>");
                }
            }

            sw.WriteLine("    </p:spTree>");
            sw.WriteLine("  </p:cSld>");
            sw.WriteLine("  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>");

            // 3. Transitions
            WriteTransitionXml(sw, slide);

            // 4. Timing / Animations
            if (slide.Animations != null && slide.Animations.Count > 0)
            {
                WriteTimingXml(sw, slide, shapeIdMap);
            }

            sw.WriteLine("</p:sld>");
        }

        private static void WriteCustomPathXml(StreamWriter sw, GraphicsPath gp, float pathW, float pathH)
        {
            if (pathW <= 0) pathW = 100f;
            if (pathH <= 0) pathH = 100f;
            sw.WriteLine("          <a:custGeom><a:avLst/><a:gdLst/><a:ahLst/><a:cxnLst/><a:rect l=\"l\" t=\"t\" r=\"r\" b=\"b\"/><a:pathLst>");
            sw.WriteLine(string.Format("            <a:path w=\"{0}\" h=\"{1}\">", (long)Math.Round(pathW), (long)Math.Round(pathH)));
            try
            {
                PointF[] pts = gp.PathPoints;
                byte[] types = gp.PathTypes;
                int i = 0;
                while (i < pts.Length)
                {
                    byte bType = (byte)(types[i] & 0x07);
                    bool isClose = (types[i] & 0x80) != 0;
                    if (bType == 0) // Start
                    {
                        sw.WriteLine(string.Format("              <a:moveTo><a:pt x=\"{0}\" y=\"{1}\"/></a:moveTo>", (long)Math.Round(pts[i].X), (long)Math.Round(pts[i].Y)));
                        i++;
                    }
                    else if (bType == 1) // Line
                    {
                        sw.WriteLine(string.Format("              <a:lnTo><a:pt x=\"{0}\" y=\"{1}\"/></a:lnTo>", (long)Math.Round(pts[i].X), (long)Math.Round(pts[i].Y)));
                        if (isClose) sw.WriteLine("              <a:close/>");
                        i++;
                    }
                    else if (bType == 3 && i + 2 < pts.Length) // Cubic Bezier
                    {
                        bool closeBez = (types[i + 2] & 0x80) != 0;
                        sw.WriteLine(string.Format("              <a:cubicBezTo><a:pt x=\"{0}\" y=\"{1}\"/><a:pt x=\"{2}\" y=\"{3}\"/><a:pt x=\"{4}\" y=\"{5}\"/></a:cubicBezTo>",
                            (long)Math.Round(pts[i].X), (long)Math.Round(pts[i].Y), (long)Math.Round(pts[i + 1].X), (long)Math.Round(pts[i + 1].Y), (long)Math.Round(pts[i + 2].X), (long)Math.Round(pts[i + 2].Y)));
                        if (closeBez) sw.WriteLine("              <a:close/>");
                        i += 3;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch { }
            sw.WriteLine("            </a:path>");
            sw.WriteLine("          </a:pathLst></a:custGeom>");
        }

        private static void WriteTransitionXml(StreamWriter sw, SlideNode slide)
        {
            if (slide.Transition == TransitionType.None) return;

            string advAttr = string.Format("advClick=\"{0}\"", slide.AdvanceOnClick ? "1" : "0");
            if (slide.AutoAdvance)
            {
                advAttr += string.Format(" advTm=\"{0}\"", (int)(slide.AutoAdvanceSeconds * 1000));
            }

            if (slide.Transition == TransitionType.Morph)
            {
                string modeStr = (slide.MorphMode == MorphType.Words) ? "byWord" : ((slide.MorphMode == MorphType.Characters) ? "byChar" : "byObject");
                sw.WriteLine("  <mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">");
                sw.WriteLine("    <mc:Choice xmlns:p159=\"http://schemas.microsoft.com/office/powerpoint/2015/09/main\" Requires=\"p159\">");
                sw.WriteLine(string.Format("      <p:transition spd=\"med\" xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" p14:dur=\"2000\" {0}>", advAttr));
                sw.WriteLine(string.Format("        <p159:morph option=\"{0}\"/>", modeStr));
                sw.WriteLine("      </p:transition>");
                sw.WriteLine("    </mc:Choice>");
                sw.WriteLine("    <mc:Fallback>");
                sw.WriteLine(string.Format("      <p:transition spd=\"med\" {0}>", advAttr));
                sw.WriteLine("        <p:fade/>");
                sw.WriteLine("      </p:transition>");
                sw.WriteLine("    </mc:Fallback>");
                sw.WriteLine("  </mc:AlternateContent>");
                return;
            }

            // PPT 2010 Transitions wrapped in AlternateContent for maximum reliability
            string p14Name = null;
            switch (slide.Transition)
            {
                case TransitionType.Flash: p14Name = "flash"; break;
                case TransitionType.Curtains: p14Name = "curtains"; break;
                case TransitionType.Wind: p14Name = "wind"; break;
                case TransitionType.Prestige: p14Name = "prestige"; break;
                case TransitionType.Fracture: p14Name = "fracture"; break;
                case TransitionType.Crush: p14Name = "crush"; break;
                case TransitionType.PeelOff: p14Name = "peel"; break;
                case TransitionType.PageCurl: p14Name = "pageCurl"; break;
                case TransitionType.Airplane: p14Name = "airplane"; break;
                case TransitionType.Origami: p14Name = "origami"; break;
                case TransitionType.Ripple: p14Name = "ripple"; break;
                case TransitionType.Honeycomb: p14Name = "honeycomb"; break;
                case TransitionType.Glitter: p14Name = "glitter"; break;
                case TransitionType.Vortex: p14Name = "vortex"; break;
                case TransitionType.Shred: p14Name = "shred pattern=\"strip\""; break;
                case TransitionType.Switch: p14Name = "switch"; break;
                case TransitionType.Flip: p14Name = "flip"; break;
                case TransitionType.Gallery: p14Name = "gallery"; break;
                case TransitionType.Cube: p14Name = "cube"; break;
                case TransitionType.Doors: p14Name = "doors"; break;
                case TransitionType.Box: p14Name = "box"; break;
                case TransitionType.Pan: p14Name = "pan"; break;
                case TransitionType.FerrisWheel: p14Name = "ferris"; break;
                case TransitionType.Conveyor: p14Name = "conveyor"; break;
                case TransitionType.Rotate: p14Name = "rotate"; break;
                case TransitionType.Window: p14Name = "window"; break;
                case TransitionType.Orbit: p14Name = "orbit"; break;
                case TransitionType.FlyThrough: p14Name = "flythrough"; break;
                // GenOffice exclusives
                case TransitionType.GenOffice_CinematicPrism: p14Name = "glitter"; break;
                case TransitionType.GenOffice_LiquidFluid: p14Name = "ripple"; break;
                case TransitionType.GenOffice_MatrixRain: p14Name = "dissolve"; break;
                case TransitionType.GenOffice_NeonPulse: p14Name = "flash"; break;
                case TransitionType.GenOffice_PortalWarp: p14Name = "vortex"; break;
                case TransitionType.GenOffice_Supernova: p14Name = "glitter"; break;
                case TransitionType.GenOffice_HologramGlitch: p14Name = "shred pattern=\"strip\""; break;
                case TransitionType.GenOffice_Kaleidoscope: p14Name = "honeycomb"; break;
                case TransitionType.GenOffice_Starlight: p14Name = "flythrough"; break;
                case TransitionType.GenOffice_SmoothFlow: p14Name = "ripple"; break;
            }

            if (p14Name != null)
            {
                sw.WriteLine("  <mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">");
                sw.WriteLine("    <mc:Choice xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" Requires=\"p14\">");
                sw.WriteLine(string.Format("      <p:transition spd=\"med\" {0}>", advAttr));
                sw.WriteLine(string.Format("        <p14:{0}/>", p14Name));
                sw.WriteLine("      </p:transition>");
                sw.WriteLine("    </mc:Choice>");
                sw.WriteLine("    <mc:Fallback>");
                sw.WriteLine(string.Format("      <p:transition spd=\"med\" {0}>", advAttr));
                sw.WriteLine("        <p:fade/>");
                sw.WriteLine("      </p:transition>");
                sw.WriteLine("    </mc:Fallback>");
                sw.WriteLine("  </mc:AlternateContent>");
                return;
            }

            // Standard PPT 2007 Transitions
            sw.WriteLine(string.Format("  <p:transition spd=\"med\" {0}>", advAttr));
            switch (slide.Transition)
            {
                case TransitionType.Fade:
                    sw.WriteLine("    <p:fade/>");
                    break;
                case TransitionType.Push:
                    sw.WriteLine("    <p:push dir=\"r\"/>");
                    break;
                case TransitionType.Wipe:
                case TransitionType.GenOffice_GoldenRibbon:
                    sw.WriteLine("    <p:wipe dir=\"l\"/>");
                    break;
                case TransitionType.Split:
                    sw.WriteLine("    <p:split orient=\"horz\" dir=\"out\"/>");
                    break;
                case TransitionType.Reveal:
                    sw.WriteLine("    <p:fade/>");
                    break;
                case TransitionType.Wheel:
                    sw.WriteLine("    <p:wheel spokes=\"4\"/>");
                    break;
                case TransitionType.RandomBars:
                    sw.WriteLine("    <p:randomBar/>");
                    break;
                case TransitionType.Zoom:
                    sw.WriteLine("    <p:zoom dir=\"in\"/>");
                    break;
                case TransitionType.Cut:
                    sw.WriteLine("    <p:cut/>");
                    break;
                case TransitionType.Shape:
                    sw.WriteLine("    <p:circle/>");
                    break;
                case TransitionType.Cover:
                    sw.WriteLine("    <p:cover dir=\"l\"/>");
                    break;
                case TransitionType.Uncover:
                    sw.WriteLine("    <p:uncover dir=\"l\"/>");
                    break;
                case TransitionType.Dissolve:
                    sw.WriteLine("    <p:dissolve/>");
                    break;
                case TransitionType.Checker:
                    sw.WriteLine("    <p:checker orient=\"horz\"/>");
                    break;
                case TransitionType.Blinds:
                    sw.WriteLine("    <p:blinds orient=\"vert\"/>");
                    break;
                case TransitionType.Clock:
                    sw.WriteLine("    <p:wheel spokes=\"1\"/>");
                    break;
                case TransitionType.Comb:
                    sw.WriteLine("    <p:comb orient=\"horz\"/>");
                    break;
                default:
                    sw.WriteLine("    <p:fade/>");
                    break;
            }
            sw.WriteLine("  </p:transition>");
        }

        private static void WriteTimingXml(StreamWriter sw, SlideNode slide, Dictionary<ShapeNode, int> shapeIdMap)
        {
            sw.WriteLine("  <p:timing>");
            sw.WriteLine("    <p:tnLst>");
            sw.WriteLine("      <p:par>");
            sw.WriteLine("        <p:cTn id=\"1\" dur=\"indefinite\" restart=\"never\" nodeType=\"tmRoot\">");
            sw.WriteLine("          <p:childTnLst>");
            sw.WriteLine("            <p:seq concurrent=\"1\" nextAc=\"seek\">");
            sw.WriteLine("              <p:cTn id=\"2\" dur=\"indefinite\" nodeType=\"mainSeq\">");
            sw.WriteLine("                <p:childTnLst>");

            for (int i = 0; i < slide.Animations.Count; i++)
            {
                ShapeAnimation anim = slide.Animations[i];
                int targetSpId = 2;
                if (!string.IsNullOrEmpty(anim.ShapeId))
                {
                    foreach (var kvp in shapeIdMap)
                    {
                        if (kvp.Key.Id == anim.ShapeId)
                        {
                            targetSpId = kvp.Value;
                            break;
                        }
                    }
                }

                int durMs = Math.Max(100, (int)(anim.Duration * 1000));
                int delayMs = (int)(anim.Delay * 1000);
                string filterName = "fade";
                string transitionMode = anim.IsExit ? "out" : "in";

                switch (anim.Type)
                {
                    case AnimationType.FlyIn: filterName = "fly"; transitionMode = "in"; break;
                    case AnimationType.FlyOut: filterName = "fly"; transitionMode = "out"; break;
                    case AnimationType.WipeIn: filterName = "wipe"; transitionMode = "in"; break;
                    case AnimationType.WipeOut: filterName = "wipe"; transitionMode = "out"; break;
                    case AnimationType.ZoomIn: filterName = "zoom"; transitionMode = "in"; break;
                    case AnimationType.ZoomOut: filterName = "zoom"; transitionMode = "out"; break;
                    case AnimationType.SplitIn: filterName = "split"; transitionMode = "in"; break;
                    case AnimationType.SplitOut: filterName = "split"; transitionMode = "out"; break;
                    case AnimationType.WheelIn: filterName = "wheel"; transitionMode = "in"; break;
                    case AnimationType.WheelOut: filterName = "wheel"; transitionMode = "out"; break;
                    case AnimationType.Bounce: filterName = "bounce"; transitionMode = "in"; break;
                    case AnimationType.FadeOut: filterName = "fade"; transitionMode = "out"; break;
                    default: filterName = "fade"; break;
                }

                int baseId = 10 + i * 5;
                sw.WriteLine("                  <p:par>");
                sw.WriteLine(string.Format("                    <p:cTn id=\"{0}\" fill=\"hold\">", baseId));
                sw.WriteLine("                      <p:stCondLst>");
                sw.WriteLine(string.Format("                        <p:cond delay=\"{0}\"/>", delayMs));
                sw.WriteLine("                      </p:stCondLst>");
                sw.WriteLine("                      <p:childTnLst>");
                sw.WriteLine("                        <p:par>");
                sw.WriteLine(string.Format("                          <p:cTn id=\"{0}\" fill=\"hold\">", baseId + 1));
                sw.WriteLine("                            <p:stCondLst><p:cond delay=\"0\"/></p:stCondLst>");
                sw.WriteLine("                            <p:childTnLst>");
                sw.WriteLine(string.Format("                              <p:animEffect transition=\"{0}\" filter=\"{1}\">", transitionMode, filterName));
                sw.WriteLine("                                <p:cBhvr>");
                sw.WriteLine(string.Format("                                  <p:cTn id=\"{0}\" dur=\"{1}\"/>", baseId + 2, durMs));
                sw.WriteLine("                                  <p:tgtEl>");
                sw.WriteLine(string.Format("                                    <p:spTgt spid=\"{0}\"/>", targetSpId));
                sw.WriteLine("                                  </p:tgtEl>");
                sw.WriteLine("                                </p:cBhvr>");
                sw.WriteLine("                              </p:animEffect>");
                sw.WriteLine("                            </p:childTnLst>");
                sw.WriteLine("                          </p:cTn>");
                sw.WriteLine("                        </p:par>");
                sw.WriteLine("                      </p:childTnLst>");
                sw.WriteLine("                    </p:cTn>");
                sw.WriteLine("                  </p:par>");
            }

            sw.WriteLine("                </p:childTnLst>");
            sw.WriteLine("              </p:cTn>");
            sw.WriteLine("              <p:prevCondLst><p:cond evt=\"onPrev\" delay=\"0\"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:prevCondLst>");
            sw.WriteLine("              <p:nextCondLst><p:cond evt=\"onNext\" delay=\"0\"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:nextCondLst>");
            sw.WriteLine("            </p:seq>");
            sw.WriteLine("          </p:childTnLst>");
            sw.WriteLine("        </p:cTn>");
            sw.WriteLine("      </p:par>");
            sw.WriteLine("    </p:tnLst>");
            sw.WriteLine("  </p:timing>");
        }

        private static string GetOpenXmlGeomName(ShapeType type)
        {
            switch (type)
            {
                case ShapeType.TextBox: return "rect";
                case ShapeType.Rectangle: return "rect";
                case ShapeType.RoundedRectangle: return "roundRect";
                case ShapeType.Ellipse: return "ellipse";
                case ShapeType.Triangle: return "triangle";
                case ShapeType.Diamond: return "diamond";
                case ShapeType.Star: return "star5";
                case ShapeType.Arrow: return "rightArrow";
                case ShapeType.Line: return "line";
                case ShapeType.Hexagon: return "hexagon";
                case ShapeType.Heart: return "heart";
                case ShapeType.Callout: return "wedgeRectCallout";
                case ShapeType.Chevron: return "chevron";
                case ShapeType.RightTriangle: return "rtTriangle";
                case ShapeType.Parallelogram: return "parallelogram";
                case ShapeType.Trapezoid: return "trapezoid";
                case ShapeType.Octagon: return "octagon";
                case ShapeType.Pentagon: return "pentagon";
                case ShapeType.Cloud: return "cloud";
                case ShapeType.BlockArrowRight: return "rightArrow";
                case ShapeType.BlockArrowLeft: return "leftArrow";
                case ShapeType.BlockArrowUp: return "upArrow";
                case ShapeType.BlockArrowDown: return "downArrow";
                case ShapeType.Lightning: return "lightningBolt";
                case ShapeType.Sun: return "sun";
                case ShapeType.Moon: return "moon";
                case ShapeType.Smiley: return "smileyFace";
                case ShapeType.Donut: return "donut";
                case ShapeType.Cross: return "mathPlus";
                case ShapeType.Cube: return "cube";
                case ShapeType.Can: return "can";
                case ShapeType.Leaf: return "tear";
                case ShapeType.Shield: return "shield";
                case ShapeType.Banner: return "wave";
                case ShapeType.Starburst: return "star8";
                case ShapeType.Arch: return "arc";
                case ShapeType.Gem: return "diamond";
                case ShapeType.LuxuryHexagon: return "hexagon";
                default: return "rect";
            }
        }

        public static PresentationRoot LoadPptx(string filepath)
        {
            PresentationRoot pres;
            if (LoadPptx(filepath, out pres))
            {
                return pres;
            }
            if (LoadPptxXml(filepath, out pres))
            {
                return pres;
            }
            return new PresentationRoot();
        }

        public static bool LoadPptx(string filepath, out PresentationRoot pres)
        {
            pres = new PresentationRoot();
            if (!File.Exists(filepath)) return false;

            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    // 0. Extract and register embedded fonts
                    SlideFontManager.ExtractAndRegisterFonts(archive);

                    // 1. Read presentation.xml to get exact canvas size
                    float rawWidth = 1280.0f;
                    float rawHeight = 720.0f;
                    ZipArchiveEntry presEntry = archive.GetEntry("ppt/presentation.xml");
                    if (presEntry != null)
                    {
                        XmlDocument presDoc = new XmlDocument();
                        using (Stream s = presEntry.Open()) presDoc.Load(s);

                        XmlNode sldSz = presDoc.SelectSingleNode("//*[local-name()='sldSz']");
                        if (sldSz != null && sldSz.Attributes["cx"] != null && sldSz.Attributes["cy"] != null)
                        {
                            long cxEmu, cyEmu;
                            if (long.TryParse(sldSz.Attributes["cx"].Value, out cxEmu) && long.TryParse(sldSz.Attributes["cy"].Value, out cyEmu))
                            {
                                rawWidth = cxEmu / 9525.0f;
                                rawHeight = cyEmu / 9525.0f;
                            }
                        }
                    }

                    bool isWide = (rawWidth / Math.Max(1.0f, rawHeight) >= 1.5f);
                    pres.IsWidescreen = isWide;
                    pres.CanvasWidth = isWide ? 1920.0f : 1440.0f;
                    pres.CanvasHeight = 1080.0f;

                    float normScaleX = pres.CanvasWidth / Math.Max(1.0f, rawWidth);
                    float normScaleY = pres.CanvasHeight / Math.Max(1.0f, rawHeight);

                    // 2. Discover slide entries in official presentation order
                    List<ZipArchiveEntry> slideEntries = new List<ZipArchiveEntry>();
                    ZipArchiveEntry presRelsEntry = archive.GetEntry("ppt/_rels/presentation.xml.rels");
                    if (presEntry != null && presRelsEntry != null)
                    {
                        try
                        {
                            XmlDocument pDoc = new XmlDocument();
                            using (Stream ps = presEntry.Open()) pDoc.Load(ps);
                            XmlDocument prDoc = new XmlDocument();
                            using (Stream prs = presRelsEntry.Open()) prDoc.Load(prs);

                            XmlNodeList sldIdList = pDoc.SelectNodes("//*[local-name()='sldIdLst']/*[local-name()='sldId']");
                            if (sldIdList != null && sldIdList.Count > 0)
                            {
                                foreach (XmlNode sldId in sldIdList)
                                {
                                    string rId = sldId.Attributes["r:id"] != null ? sldId.Attributes["r:id"].Value : (sldId.Attributes["id"] != null ? sldId.Attributes["id"].Value : "");
                                    if (string.IsNullOrEmpty(rId))
                                    {
                                        foreach (XmlAttribute a in sldId.Attributes)
                                        {
                                            if (a.LocalName == "id" && a.Value.StartsWith("rId")) { rId = a.Value; break; }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(rId))
                                    {
                                        XmlNode relNode = prDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                                        if (relNode != null && relNode.Attributes["Target"] != null)
                                        {
                                            string target = relNode.Attributes["Target"].Value.Replace('\\', '/');
                                            if (!target.StartsWith("ppt/")) target = "ppt/" + target;
                                            ZipArchiveEntry se = archive.GetEntry(target);
                                            if (se != null && !slideEntries.Contains(se)) slideEntries.Add(se);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    if (slideEntries.Count == 0)
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string fn = entry.FullName.Replace('\\', '/');
                            if (fn.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) && fn.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                slideEntries.Add(entry);
                            }
                        }

                        slideEntries.Sort(new Comparison<ZipArchiveEntry>((a, b) => {
                            return ExtractSlideNum(a.FullName).CompareTo(ExtractSlideNum(b.FullName));
                        }));
                    }

                    // Read Theme (Major/Minor Fonts)
                    string majorFont = "Calibri Light";
                    string minorFont = "Calibri";
                    XmlDocument thDoc = null;
                    ZipArchiveEntry themeEntry = archive.GetEntry("ppt/theme/theme1.xml");
                    if (themeEntry != null)
                    {
                        try
                        {
                            thDoc = new XmlDocument();
                            using (Stream ths = themeEntry.Open()) thDoc.Load(ths);
                            XmlNode majLatin = thDoc.SelectSingleNode("//*[local-name()='fontScheme']/*[local-name()='majorFont']/*[local-name()='latin']");
                            if (majLatin != null && majLatin.Attributes["typeface"] != null && !string.IsNullOrEmpty(majLatin.Attributes["typeface"].Value))
                                majorFont = majLatin.Attributes["typeface"].Value;
                            XmlNode minLatin = thDoc.SelectSingleNode("//*[local-name()='fontScheme']/*[local-name()='minorFont']/*[local-name()='latin']");
                            if (minLatin != null && minLatin.Attributes["typeface"] != null && !string.IsNullOrEmpty(minLatin.Attributes["typeface"].Value))
                                minorFont = minLatin.Attributes["typeface"].Value;

                            XmlNode clrScheme = thDoc.SelectSingleNode("//*[local-name()='clrScheme']");
                            if (clrScheme != null)
                            {
                                foreach (XmlNode cNode in clrScheme.ChildNodes)
                                {
                                    string cName = cNode.LocalName;
                                    XmlNode srgb = cNode.SelectSingleNode(".//*[local-name()='srgbClr']");
                                    if (srgb != null && srgb.Attributes["val"] != null)
                                    {
                                        int rgbVal;
                                        if (int.TryParse(srgb.Attributes["val"].Value, System.Globalization.NumberStyles.HexNumber, null, out rgbVal))
                                            pres.ThemeColors[cName] = rgbVal;
                                    }
                                    else
                                    {
                                        XmlNode sysClr = cNode.SelectSingleNode(".//*[local-name()='sysClr']");
                                        if (sysClr != null && sysClr.Attributes["lastClr"] != null)
                                        {
                                            int rgbVal;
                                            if (int.TryParse(sysClr.Attributes["lastClr"].Value, System.Globalization.NumberStyles.HexNumber, null, out rgbVal))
                                                pres.ThemeColors[cName] = rgbVal;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    if (slideEntries.Count > 0)
                    {
                        pres.Slides.Clear();
                        for (int sIdx = 0; sIdx < slideEntries.Count; sIdx++)
                        {
                            ZipArchiveEntry sEntry = slideEntries[sIdx];
                            SlideNode slide = new SlideNode();
                            slide.Title = "Slide " + (sIdx + 1);
                            slide.Transition = TransitionType.None;

                            XmlDocument sDoc = new XmlDocument() { PreserveWhitespace = true };
                            using (Stream s = sEntry.Open()) sDoc.Load(s);

                            // Parse Transition (Morph, Fade, Push, Wipe, Split, Zoom, etc.)
                            ParseSlideTransition(sDoc, slide);

                            // Read Slide Layout & Master & their Rels
                            XmlDocument layoutDoc = null;
                            XmlDocument masterDoc = null;
                            ZipArchiveEntry layoutRelEntry = null;
                            string slideFileName = System.IO.Path.GetFileName(sEntry.FullName);
                            string relPath = "ppt/slides/_rels/" + slideFileName + ".rels";
                            ZipArchiveEntry relEntry = archive.GetEntry(relPath);
                            if (relEntry != null)
                            {
                                try
                                {
                                    XmlDocument rDoc = new XmlDocument();
                                    using (Stream rs = relEntry.Open()) rDoc.Load(rs);
                                    XmlNode lRel = rDoc.SelectSingleNode("//*[local-name()='Relationship'][contains(@Type, 'slideLayout')]");
                                    if (lRel != null && lRel.Attributes["Target"] != null)
                                    {
                                        string lTarget = lRel.Attributes["Target"].Value.Replace('\\', '/');
                                        if (lTarget.StartsWith("../")) lTarget = "ppt/" + lTarget.Substring(3);
                                        else if (!lTarget.StartsWith("ppt/")) lTarget = "ppt/slides/" + lTarget;
                                        ZipArchiveEntry lEntry = archive.GetEntry(lTarget);
                                        if (lEntry != null)
                                        {
                                            layoutDoc = new XmlDocument() { PreserveWhitespace = true };
                                            using (Stream ls = lEntry.Open()) layoutDoc.Load(ls);

                                            string lFileName = System.IO.Path.GetFileName(lTarget);
                                            string lRelPath = "ppt/slideLayouts/_rels/" + lFileName + ".rels";
                                            layoutRelEntry = archive.GetEntry(lRelPath);

                                            if (layoutRelEntry != null)
                                            {
                                                try
                                                {
                                                    XmlDocument lrDoc = new XmlDocument();
                                                    using (Stream lrs = layoutRelEntry.Open()) lrDoc.Load(lrs);
                                                    XmlNode mRel = lrDoc.SelectSingleNode("//*[local-name()='Relationship'][contains(@Type, 'slideMaster')]");
                                                    if (mRel != null && mRel.Attributes["Target"] != null)
                                                    {
                                                        string mTarget = mRel.Attributes["Target"].Value.Replace('\\', '/');
                                                        if (mTarget.StartsWith("../")) mTarget = "ppt/" + mTarget.Substring(3);
                                                        else if (!mTarget.StartsWith("ppt/")) mTarget = "ppt/slideLayouts/" + mTarget;
                                                        ZipArchiveEntry mEntry = archive.GetEntry(mTarget);
                                                        if (mEntry != null)
                                                        {
                                                            masterDoc = new XmlDocument() { PreserveWhitespace = true };
                                                            using (Stream ms = mEntry.Open()) masterDoc.Load(ms);
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                    }

                                    // Load Speaker Notes if linked via notesSlide relationship
                                    XmlNode notesRel = rDoc.SelectSingleNode("//*[local-name()='Relationship'][contains(@Type, 'notesSlide')]");
                                    if (notesRel != null && notesRel.Attributes["Target"] != null)
                                    {
                                        string nTarget = notesRel.Attributes["Target"].Value.Replace('\\', '/');
                                        if (nTarget.StartsWith("../")) nTarget = "ppt/" + nTarget.Substring(3);
                                        else if (!nTarget.StartsWith("ppt/")) nTarget = "ppt/slides/" + nTarget;
                                        ZipArchiveEntry nEntry = archive.GetEntry(nTarget);
                                        if (nEntry != null)
                                        {
                                            try
                                            {
                                                XmlDocument nDoc = new XmlDocument() { PreserveWhitespace = true };
                                                using (Stream ns = nEntry.Open()) nDoc.Load(ns);
                                                XmlNodeList nShapes = nDoc.SelectNodes("//*[local-name()='sp']");
                                                if (nShapes != null)
                                                {
                                                    StringBuilder notesSb = new StringBuilder();
                                                    foreach (XmlNode nSp in nShapes)
                                                    {
                                                        XmlNode phNode = nSp.SelectSingleNode(".//*[local-name()='ph']");
                                                        string phType = (phNode != null && phNode.Attributes["type"] != null) ? phNode.Attributes["type"].Value : "";
                                                        if (phType == "sldImg" || phType == "sldNum" || phType == "dt" || phType == "hdr" || phType == "ftr")
                                                            continue;
                                                        XmlNode txBody = nSp.SelectSingleNode(".//*[local-name()='txBody']");
                                                        if (txBody != null)
                                                        {
                                                            XmlNodeList pNodes = txBody.SelectNodes(".//*[local-name()='p']");
                                                            if (pNodes != null && pNodes.Count > 0)
                                                            {
                                                                List<string> pLines = new List<string>();
                                                                foreach (XmlNode pNode in pNodes)
                                                                {
                                                                    StringBuilder pSb = new StringBuilder();
                                                                    XmlNodeList tNodes = pNode.SelectNodes(".//*[local-name()='t']");
                                                                    if (tNodes != null)
                                                                    {
                                                                        foreach (XmlNode tN in tNodes) pSb.Append(tN.InnerText);
                                                                    }
                                                                    pLines.Add(pSb.ToString());
                                                                }
                                                                string joined = string.Join("\n", pLines.ToArray()).Trim('\r', '\n');
                                                                if (!string.IsNullOrEmpty(joined))
                                                                {
                                                                    if (notesSb.Length > 0) notesSb.Append("\n");
                                                                    notesSb.Append(joined);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if (notesSb.Length > 0)
                                                    {
                                                        slide.SpeakerNotes = notesSb.ToString();
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                                catch { }
                            }

                            // Layer 1: Import Slide Layout non-placeholder decorative shapes, mockups, frames, and pictures
                            if (layoutDoc != null)
                            {
                                XmlNode lSpTree = layoutDoc.SelectSingleNode("//*[local-name()='spTree']");
                                if (lSpTree != null)
                                {
                                    foreach (XmlNode child in lSpTree.ChildNodes)
                                    {
                                        string ln = child.LocalName;
                                        if (ln == "sp" || ln == "cxnSp")
                                        {
                                            if (child.SelectSingleNode(".//*[local-name()='ph']") != null) continue;
                                            ShapeNode lShape = ExtractShapeFromSpNode(child, null, null, pres, 0, 0, normScaleX, normScaleY, majorFont, minorFont, archive, layoutRelEntry);
                                            if (lShape != null) slide.AddShape(lShape);
                                        }
                                        else if (ln == "pic")
                                        {
                                            ShapeNode lPicShape = ExtractPictureNode(child, archive, layoutRelEntry, 0, 0, normScaleX, normScaleY);
                                            if (lPicShape != null) slide.AddShape(lPicShape);
                                        }
                                        else if (ln == "graphicFrame")
                                        {
                                            ShapeNode lGfShape = ExtractGraphicFrameNode(child, pres, 0, 0, normScaleX, normScaleY, majorFont, minorFont, archive, layoutRelEntry);
                                            if (lGfShape != null) slide.AddShape(lGfShape);
                                        }
                                        else if (ln == "grpSp")
                                        {
                                            List<ShapeNode> grpShapes = new List<ShapeNode>();
                                            ExtractGroupShapes(child, grpShapes, layoutDoc, masterDoc, pres, archive, layoutRelEntry, 0, 0, normScaleX, normScaleY, majorFont, minorFont);
                                            foreach (ShapeNode gs in grpShapes) slide.AddShape(gs);
                                        }
                                    }
                                }
                            }

                            // Layer 2: Import Slide's own Elements in exact spTree document order
                            XmlNode spTree = sDoc.SelectSingleNode("//*[local-name()='spTree']");
                            if (spTree != null)
                            {
                                foreach (XmlNode child in spTree.ChildNodes)
                                {
                                    string ln = child.LocalName;
                                    if (ln == "sp" || ln == "cxnSp")
                                    {
                                        ShapeNode shape = ExtractShapeFromSpNode(child, layoutDoc, masterDoc, pres, 0, 0, normScaleX, normScaleY, majorFont, minorFont, archive, relEntry);
                                        if (shape != null) slide.AddShape(shape);
                                    }
                                    else if (ln == "pic")
                                    {
                                        ShapeNode picShape = ExtractPictureNode(child, archive, relEntry, 0, 0, normScaleX, normScaleY);
                                        if (picShape != null) slide.AddShape(picShape);
                                    }
                                    else if (ln == "graphicFrame")
                                    {
                                        ShapeNode gfShape = ExtractGraphicFrameNode(child, pres, 0, 0, normScaleX, normScaleY, majorFont, minorFont, archive, relEntry);
                                        if (gfShape != null) slide.AddShape(gfShape);
                                    }
                                    else if (ln == "grpSp")
                                    {
                                        List<ShapeNode> grpShapes = new List<ShapeNode>();
                                        ExtractGroupShapes(child, grpShapes, layoutDoc, masterDoc, pres, archive, relEntry, 0, 0, normScaleX, normScaleY, majorFont, minorFont);
                                        foreach (ShapeNode gs in grpShapes) slide.AddShape(gs);
                                    }
                                }
                            }

                            // 2d. Import OpenXML Slide Animations & Timing (<p:timing>)
                            ParseSlideAnimations(sDoc, slide);

                            // 2e. Import Slide Background (<p:bg>)
                            ParseSlideBackground(sDoc, layoutDoc, masterDoc, slide, archive, relEntry, thDoc, pres);

                            pres.Slides.Add(slide);
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ZIP LOAD EXCEPTION: " + ex.Message + "\n" + ex.StackTrace);
            }

            return LoadPptxXml(filepath, out pres);
        }

        private static string ResolveTypeface(string typeface, string majorFont, string minorFont)
        {
            if (string.IsNullOrEmpty(typeface)) return "Segoe UI";
            if (typeface.StartsWith("+mj", StringComparison.OrdinalIgnoreCase)) return majorFont;
            if (typeface.StartsWith("+mn", StringComparison.OrdinalIgnoreCase)) return minorFont;
            if (SlideFontManager.HasFont(typeface)) return typeface;
            if (typeface.Equals("Anton", StringComparison.OrdinalIgnoreCase)) return "Impact";
            if (typeface.StartsWith("Muli", StringComparison.OrdinalIgnoreCase)) return "Segoe UI";
            if (typeface.StartsWith("Proxima Nova", StringComparison.OrdinalIgnoreCase)) return "Segoe UI";
            return typeface;
        }

        #region OpenXML PPTX Parsing Helpers
        private static float ParseAlphaVal(XmlNode parentNode, float defaultAlpha = 1.0f)
        {
            if (parentNode == null) return defaultAlpha;
            XmlNode alphaNode = parentNode.SelectSingleNode(".//*[local-name()='alpha']");
            if (alphaNode != null && alphaNode.Attributes["val"] != null)
            {
                int val;
                if (int.TryParse(alphaNode.Attributes["val"].Value, out val))
                {
                    return Math.Max(0.0f, Math.Min(1.0f, val / 100000.0f));
                }
            }
            return defaultAlpha;
        }

        private static void ParseSlideBackground(XmlDocument sDoc, XmlDocument layoutDoc, XmlDocument masterDoc, SlideNode slide, ZipArchive archive = null, ZipArchiveEntry relEntry = null, XmlDocument themeDoc = null, PresentationRoot pres = null)
        {
            XmlNode bgNode = sDoc != null ? sDoc.SelectSingleNode("//*[local-name()='bg']") : null;
            if (bgNode == null && layoutDoc != null) bgNode = layoutDoc.SelectSingleNode("//*[local-name()='bg']");
            if (bgNode == null && masterDoc != null) bgNode = masterDoc.SelectSingleNode("//*[local-name()='bg']");

            if (bgNode != null)
            {
                // Background Picture Fill (<a:blipFill>)
                XmlNode blipFill = bgNode.SelectSingleNode(".//*[local-name()='blipFill']");
                if (blipFill != null && archive != null && relEntry != null)
                {
                    XmlNode blip = blipFill.SelectSingleNode(".//*[local-name()='blip']");
                    if (blip != null)
                    {
                        string rId = "";
                        foreach (XmlAttribute attr in blip.Attributes)
                        {
                            if (attr.LocalName == "embed") { rId = attr.Value; break; }
                        }
                        if (!string.IsNullOrEmpty(rId))
                        {
                            try
                            {
                                XmlDocument rDoc = new XmlDocument();
                                using (Stream rs = relEntry.Open()) rDoc.Load(rs);
                                XmlNode relNode = rDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                                if (relNode != null && relNode.Attributes["Target"] != null)
                                {
                                    string target = relNode.Attributes["Target"].Value.Replace('\\', '/');
                                    if (target.StartsWith("../")) target = "ppt/" + target.Substring(3);
                                    else if (!target.StartsWith("ppt/")) target = "ppt/slides/" + target;

                                    ZipArchiveEntry imgZip = archive.GetEntry(target);
                                    if (imgZip != null)
                                    {
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            using (Stream isStream = imgZip.Open()) isStream.CopyTo(ms);
                                            ms.Position = 0;
                                            using (Image rawImg = Image.FromStream(ms))
                                            {
                                                slide.CachedBgImage = new Bitmap(rawImg);
                                            }
                                            slide.BgFillType = BackgroundFillType.Picture;
                                            return;
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                // Theme Background Reference (<p:bgRef>)
                XmlNode bgRef = bgNode.SelectSingleNode(".//*[local-name()='bgRef']");
                if (bgRef != null && themeDoc != null)
                {
                    int refIdx = 1;
                    if (bgRef.Attributes["idx"] != null)
                    {
                        int parsedIdx;
                        if (int.TryParse(bgRef.Attributes["idx"].Value, out parsedIdx))
                        {
                            refIdx = parsedIdx >= 1001 ? (parsedIdx - 1000) : parsedIdx;
                        }
                    }
                    int phColorVal = ParseColorVal(bgRef, 0xFFFFFF, pres);

                    XmlNode bgFillStyleLst = themeDoc.SelectSingleNode("//*[local-name()='fmtScheme']/*[local-name()='bgFillStyleLst']")
                                          ?? themeDoc.SelectSingleNode(".//*[local-name()='bgFillStyleLst']");
                    if (bgFillStyleLst != null && bgFillStyleLst.ChildNodes.Count > 0)
                    {
                        int targetIdx = Math.Clamp(refIdx - 1, 0, bgFillStyleLst.ChildNodes.Count - 1);
                        XmlNode styleNode = bgFillStyleLst.ChildNodes[targetIdx];
                        if (styleNode != null)
                        {
                            XmlNode thGradFill = styleNode.LocalName == "gradFill" ? styleNode : styleNode.SelectSingleNode(".//*[local-name()='gradFill']");
                            XmlNode thSolidFill = styleNode.LocalName == "solidFill" ? styleNode : styleNode.SelectSingleNode(".//*[local-name()='solidFill']");

                            if (thGradFill != null)
                            {
                                slide.IsGradientBg = true;
                                slide.BgFillType = BackgroundFillType.Gradient;
                                var gsList = thGradFill.SelectNodes(".//*[local-name()='gs']");
                                List<GradientStop> stops = new List<GradientStop>();
                                if (gsList != null && gsList.Count > 0)
                                {
                                    foreach (XmlNode gs in gsList)
                                    {
                                        int col = ParseColorVal(gs, phColorVal, pres, phColorVal);
                                        float pos = 0f;
                                        if (gs.Attributes["pos"] != null)
                                        {
                                            float pVal;
                                            if (float.TryParse(gs.Attributes["pos"].Value, out pVal)) pos = pVal / 100000f;
                                        }
                                        float alpha = ParseAlphaVal(gs, 1.0f);
                                        stops.Add(new GradientStop { Color = col, Position = pos, Transparency = 1.0f - alpha });
                                    }
                                }
                                if (stops.Count >= 2)
                                {
                                    slide.BgColor = stops[0].Color;
                                    slide.BgColor2 = stops[stops.Count - 1].Color;
                                    slide.BgGradientStops = stops;
                                }
                                else
                                {
                                    slide.BgColor = phColorVal;
                                    slide.BgColor2 = phColorVal;
                                    slide.BgGradientStops = new List<GradientStop> { new GradientStop(phColorVal, 0f), new GradientStop(phColorVal, 1f) };
                                }

                                XmlNode lin = thGradFill.SelectSingleNode(".//*[local-name()='lin']");
                                if (lin != null && lin.Attributes["ang"] != null)
                                {
                                    int angVal;
                                    if (int.TryParse(lin.Attributes["ang"].Value, out angVal)) slide.BgGradientAngle = angVal / 60000.0f;
                                }
                                return;
                            }
                            else if (thSolidFill != null)
                            {
                                int col = ParseColorVal(thSolidFill, phColorVal, pres, phColorVal);
                                slide.IsGradientBg = false;
                                slide.BgFillType = BackgroundFillType.Solid;
                                slide.BgColor = col;
                                slide.BgGradientStops = new List<GradientStop> { new GradientStop(col, 0f), new GradientStop(col, 1f) };
                                return;
                            }
                        }
                    }
                }

                XmlNode gradFill = bgNode.SelectSingleNode(".//*[local-name()='gradFill']");
                if (gradFill != null)
                {
                    slide.IsGradientBg = true;
                    slide.BgFillType = BackgroundFillType.Gradient;
                    var gsList = gradFill.SelectNodes(".//*[local-name()='gs']");
                    List<GradientStop> stops = new List<GradientStop>();
                    if (gsList != null && gsList.Count > 0)
                    {
                        foreach (XmlNode gs in gsList)
                        {
                            int col = ParseColorVal(gs, 0xFFFFFF, pres);
                            float pos = 0f;
                            if (gs.Attributes["pos"] != null)
                            {
                                float pVal;
                                if (float.TryParse(gs.Attributes["pos"].Value, out pVal)) pos = pVal / 100000f;
                            }
                            float alpha = ParseAlphaVal(gs, 1.0f);
                            stops.Add(new GradientStop { Color = col, Position = pos, Transparency = 1.0f - alpha });
                        }
                    }

                    if (stops.Count >= 2)
                    {
                        slide.BgColor = stops[0].Color;
                        slide.BgColor2 = stops[stops.Count - 1].Color;
                        slide.BgGradientStops = stops;
                    }
                    else
                    {
                        int c1 = ParseColorVal(gradFill, 0x0B0F19, pres);
                        slide.BgColor = c1;
                        slide.BgColor2 = c1;
                        slide.BgGradientStops = new List<GradientStop> { new GradientStop(c1, 0f), new GradientStop(c1, 1f) };
                    }

                    XmlNode lin = gradFill.SelectSingleNode(".//*[local-name()='lin']");
                    if (lin != null && lin.Attributes["ang"] != null)
                    {
                        int angVal;
                        if (int.TryParse(lin.Attributes["ang"].Value, out angVal))
                        {
                            slide.BgGradientAngle = angVal / 60000.0f;
                        }
                    }
                    return;
                }

                XmlNode solidFill = bgNode.SelectSingleNode(".//*[local-name()='solidFill']");
                if (solidFill != null)
                {
                    int col = ParseColorVal(solidFill, 0xFFFFFF, pres);
                    slide.IsGradientBg = false;
                    slide.BgFillType = BackgroundFillType.Solid;
                    slide.BgColor = col;
                    slide.BgGradientStops = new List<GradientStop> { new GradientStop(col, 0f), new GradientStop(col, 1f) };
                    return;
                }
            }

            if (pres != null && pres.ThemeColors != null && pres.ThemeColors.TryGetValue("bg1", out int bg1Clr))
            {
                slide.BgColor = bg1Clr;
                slide.IsGradientBg = false;
                slide.BgFillType = BackgroundFillType.Solid;
            }
        }

        private static int ParseColorVal(XmlNode parentNode, int defaultColor = 0x000000, PresentationRoot pres = null, int? phClr = null)
        {
            if (parentNode == null) return defaultColor;
            XmlNode srgbClr = parentNode.SelectSingleNode(".//*[local-name()='srgbClr']");
            if (srgbClr != null && srgbClr.Attributes["val"] != null)
            {
                int rgb;
                if (int.TryParse(srgbClr.Attributes["val"].Value, System.Globalization.NumberStyles.HexNumber, null, out rgb))
                {
                    return rgb;
                }
            }
            XmlNode schemeClr = parentNode.SelectSingleNode(".//*[local-name()='schemeClr']");
            if (schemeClr != null && schemeClr.Attributes["val"] != null)
            {
                string sVal = schemeClr.Attributes["val"].Value.ToLower();
                int clr = defaultColor;
                if (sVal == "phclr" && phClr.HasValue)
                {
                    clr = phClr.Value;
                }
                else if (pres != null && pres.ThemeColors != null && pres.ThemeColors.TryGetValue(sVal, out int thClr))
                {
                    clr = thClr;
                }
                else
                {
                    if (sVal == "bg1" || sVal == "lt1") clr = 0xFFFFFF;
                    else if (sVal == "bg2" || sVal == "lt2") clr = 0xF2F2F2;
                    else if (sVal == "tx1" || sVal == "dk1") clr = 0x000000;
                    else if (sVal == "tx2" || sVal == "dk2") clr = 0x595959;
                    else if (sVal == "accent1") clr = 0x5B9BD5;
                    else if (sVal == "accent2") clr = 0xED7D31;
                    else if (sVal == "accent3") clr = 0xA5A5A5;
                    else if (sVal == "accent4") clr = 0xFFC000;
                    else if (sVal == "accent5") clr = 0x4472C4;
                    else if (sVal == "accent6") clr = 0x70AD47;
                }

                XmlNode lumMod = schemeClr.SelectSingleNode(".//*[local-name()='lumMod']");
                if (lumMod != null && lumMod.Attributes["val"] != null)
                {
                    int lm;
                    if (int.TryParse(lumMod.Attributes["val"].Value, out lm))
                    {
                        int r = (clr >> 16) & 0xFF;
                        int g = (clr >> 8) & 0xFF;
                        int b = clr & 0xFF;
                        r = Math.Clamp((int)(r * (lm / 100000.0)), 0, 255);
                        g = Math.Clamp((int)(g * (lm / 100000.0)), 0, 255);
                        b = Math.Clamp((int)(b * (lm / 100000.0)), 0, 255);
                        clr = (r << 16) | (g << 8) | b;
                    }
                }
                XmlNode tint = schemeClr.SelectSingleNode(".//*[local-name()='tint']");
                if (tint != null && tint.Attributes["val"] != null)
                {
                    int tv;
                    if (int.TryParse(tint.Attributes["val"].Value, out tv))
                    {
                        int r = (clr >> 16) & 0xFF;
                        int g = (clr >> 8) & 0xFF;
                        int b = clr & 0xFF;
                        float f = tv / 100000.0f;
                        r = Math.Clamp((int)(r * f + 255 * (1f - f)), 0, 255);
                        g = Math.Clamp((int)(g * f + 255 * (1f - f)), 0, 255);
                        b = Math.Clamp((int)(b * f + 255 * (1f - f)), 0, 255);
                        clr = (r << 16) | (g << 8) | b;
                    }
                }
                XmlNode shade = schemeClr.SelectSingleNode(".//*[local-name()='shade']");
                if (shade != null && shade.Attributes["val"] != null)
                {
                    int sv;
                    if (int.TryParse(shade.Attributes["val"].Value, out sv))
                    {
                        int r = (clr >> 16) & 0xFF;
                        int g = (clr >> 8) & 0xFF;
                        int b = clr & 0xFF;
                        float f = sv / 100000.0f;
                        r = Math.Clamp((int)(r * f), 0, 255);
                        g = Math.Clamp((int)(g * f), 0, 255);
                        b = Math.Clamp((int)(b * f), 0, 255);
                        clr = (r << 16) | (g << 8) | b;
                    }
                }
                return clr;
            }

            XmlNode prstClr = parentNode.SelectSingleNode(".//*[local-name()='prstClr']");
            if (prstClr != null && prstClr.Attributes["val"] != null)
            {
                string cName = prstClr.Attributes["val"].Value;
                Color named = Color.FromName(cName);
                if (named.IsKnownColor) return named.ToArgb() & 0x00FFFFFF;
            }

            XmlNode sysClr = parentNode.SelectSingleNode(".//*[local-name()='sysClr']");
            if (sysClr != null && sysClr.Attributes["lastClr"] != null)
            {
                int lRgb;
                if (int.TryParse(sysClr.Attributes["lastClr"].Value, System.Globalization.NumberStyles.HexNumber, null, out lRgb))
                    return lRgb;
            }

            return defaultColor;
        }

        private static GraphicsPath ParseCustGeom(XmlNode custGeomNode, out float pathW, out float pathH)
        {
            pathW = 100f;
            pathH = 100f;
            if (custGeomNode == null) return null;
            XmlNodeList pathNodes = custGeomNode.SelectNodes(".//*[local-name()='path']");
            if (pathNodes == null || pathNodes.Count == 0) return null;

            GraphicsPath gp = new GraphicsPath();

            foreach (XmlNode pathNode in pathNodes)
            {
                float curW = 100f, curH = 100f;
                if (pathNode.Attributes["w"] != null) float.TryParse(pathNode.Attributes["w"].Value, out curW);
                if (pathNode.Attributes["h"] != null) float.TryParse(pathNode.Attributes["h"].Value, out curH);
                if (curW > pathW) pathW = curW;
                if (curH > pathH) pathH = curH;

                PointF currentPt = PointF.Empty;

                foreach (XmlNode cmd in pathNode.ChildNodes)
                {
                    string ln = cmd.LocalName;
                    if (ln == "moveTo")
                    {
                        XmlNode pt = cmd.SelectSingleNode("./*[local-name()='pt']");
                        if (pt != null && pt.Attributes["x"] != null && pt.Attributes["y"] != null)
                        {
                            float x = 0, y = 0;
                            float.TryParse(pt.Attributes["x"].Value, out x);
                            float.TryParse(pt.Attributes["y"].Value, out y);
                            currentPt = new PointF(x, y);
                            gp.StartFigure();
                        }
                    }
                    else if (ln == "lnTo")
                    {
                        XmlNode pt = cmd.SelectSingleNode("./*[local-name()='pt']");
                        if (pt != null && pt.Attributes["x"] != null && pt.Attributes["y"] != null)
                        {
                            float x = 0, y = 0;
                            float.TryParse(pt.Attributes["x"].Value, out x);
                            float.TryParse(pt.Attributes["y"].Value, out y);
                            PointF newPt = new PointF(x, y);
                            gp.AddLine(currentPt, newPt);
                            currentPt = newPt;
                        }
                    }
                    else if (ln == "cubicBezTo")
                    {
                        XmlNodeList pts = cmd.SelectNodes("./*[local-name()='pt']");
                        if (pts != null && pts.Count >= 3)
                        {
                            float x1 = 0, y1 = 0, x2 = 0, y2 = 0, x3 = 0, y3 = 0;
                            float.TryParse(pts[0].Attributes["x"].Value, out x1);
                            float.TryParse(pts[0].Attributes["y"].Value, out y1);
                            float.TryParse(pts[1].Attributes["x"].Value, out x2);
                            float.TryParse(pts[1].Attributes["y"].Value, out y2);
                            float.TryParse(pts[2].Attributes["x"].Value, out x3);
                            float.TryParse(pts[2].Attributes["y"].Value, out y3);
                            gp.AddBezier(currentPt, new PointF(x1, y1), new PointF(x2, y2), new PointF(x3, y3));
                            currentPt = new PointF(x3, y3);
                        }
                    }
                    else if (ln == "quadBezTo")
                    {
                        XmlNodeList pts = cmd.SelectNodes("./*[local-name()='pt']");
                        if (pts != null && pts.Count >= 2)
                        {
                            float qx1 = 0, qy1 = 0, qx2 = 0, qy2 = 0;
                            float.TryParse(pts[0].Attributes["x"].Value, out qx1);
                            float.TryParse(pts[0].Attributes["y"].Value, out qy1);
                            float.TryParse(pts[1].Attributes["x"].Value, out qx2);
                            float.TryParse(pts[1].Attributes["y"].Value, out qy2);
                            PointF cp1 = new PointF(currentPt.X + 2f / 3f * (qx1 - currentPt.X), currentPt.Y + 2f / 3f * (qy1 - currentPt.Y));
                            PointF cp2 = new PointF(qx2 + 2f / 3f * (qx1 - qx2), qy2 + 2f / 3f * (qy1 - qy2));
                            PointF endPt = new PointF(qx2, qy2);
                            gp.AddBezier(currentPt, cp1, cp2, endPt);
                            currentPt = endPt;
                        }
                    }
                    else if (ln == "arcTo")
                    {
                        float wR = 0, hR = 0, stAng = 0, swAng = 0;
                        if (cmd.Attributes["wR"] != null) float.TryParse(cmd.Attributes["wR"].Value, out wR);
                        if (cmd.Attributes["hR"] != null) float.TryParse(cmd.Attributes["hR"].Value, out hR);
                        if (cmd.Attributes["stAng"] != null) float.TryParse(cmd.Attributes["stAng"].Value, out stAng);
                        if (cmd.Attributes["swAng"] != null) float.TryParse(cmd.Attributes["swAng"].Value, out swAng);
                        if (wR > 0 && hR > 0)
                        {
                            float startDeg = stAng / 60000.0f;
                            float sweepDeg = swAng / 60000.0f;
                            RectangleF arcRect = new RectangleF(currentPt.X - wR, currentPt.Y - hR, wR * 2, hR * 2);
                            gp.AddArc(arcRect, startDeg, sweepDeg);
                        }
                    }
                    else if (ln == "close")
                    {
                        gp.CloseFigure();
                    }
                }
            }

            if (pathW <= 0) pathW = 100f;
            if (pathH <= 0) pathH = 100f;
            return gp;
        }

        private static ShapeNode ExtractShapeFromSpNode(XmlNode spNode, XmlDocument layoutDoc, XmlDocument masterDoc, PresentationRoot pres, float offsetX, float offsetY, float scaleX, float scaleY, string majorFont = "Calibri Light", string minorFont = "Calibri", ZipArchive archive = null, ZipArchiveEntry relEntry = null)
        {
            ShapeNode shape = new ShapeNode();
            shape.FillColor = 0x00FFFFFF; // Transparent by default
            shape.StrokeWidth = 0.0f;     // No border by default
            shape.StrokeColor = 0x000000;
            shape.TextColor = 0x000000;   // Default black text
            shape.FontName = minorFont;
            shape.FontSize = 14.0f * (96.0f / 72.0f) * scaleX;
            shape.Bold = false;
            shape.Alignment = TextAlignment.Left;
            shape.VerticalAlignment = VerticalTextAlignment.Top;

            XmlNode cNvPr = spNode.SelectSingleNode(".//*[local-name()='cNvPr']");
            if (cNvPr != null)
            {
                if (cNvPr.Attributes["id"] != null)
                {
                    shape.Id = cNvPr.Attributes["id"].Value;
                }
                if (cNvPr.Attributes["name"] != null)
                {
                    string n = cNvPr.Attributes["name"].Value;
                    shape.Name = n;
                    if (n.StartsWith("!!")) shape.MorphName = n;
                }
            }

            // Geometry Type: Check Custom Geometry vs Preset Geometry
            XmlNode custGeom = spNode.SelectSingleNode(".//*[local-name()='custGeom']");
            if (custGeom != null)
            {
                float pw, ph;
                GraphicsPath gp = ParseCustGeom(custGeom, out pw, out ph);
                if (gp != null)
                {
                    shape.Type = ShapeType.CustomPath;
                    shape.CustomPath = gp;
                    shape.PathWidth = pw;
                    shape.PathHeight = ph;
                }
            }
            else if (spNode.LocalName == "cxnSp")
            {
                shape.Type = ShapeType.Line;
            }
            else
            {
                XmlNode prstGeom = spNode.SelectSingleNode(".//*[local-name()='prstGeom']");
                if (prstGeom != null && prstGeom.Attributes["prst"] != null)
                {
                    string pGeom = prstGeom.Attributes["prst"].Value;
                    if (pGeom == "roundRect" || pGeom.StartsWith("round") || pGeom.StartsWith("snip")) shape.Type = ShapeType.RoundedRectangle;
                    else if (pGeom == "ellipse") shape.Type = ShapeType.Ellipse;
                    else if (pGeom == "triangle") shape.Type = ShapeType.Triangle;
                    else if (pGeom == "rtTriangle") shape.Type = ShapeType.RightTriangle;
                    else if (pGeom == "diamond") shape.Type = ShapeType.Diamond;
                    else if (pGeom == "parallelogram") shape.Type = ShapeType.Parallelogram;
                    else if (pGeom == "trapezoid") shape.Type = ShapeType.Trapezoid;
                    else if (pGeom == "hexagon") shape.Type = ShapeType.Hexagon;
                    else if (pGeom == "pentagon" || pGeom == "heptagon") shape.Type = ShapeType.Pentagon;
                    else if (pGeom == "octagon" || pGeom == "decagon" || pGeom == "dodecagon") shape.Type = ShapeType.Octagon;
                    else if (pGeom.StartsWith("star")) shape.Type = ShapeType.Star;
                    else if (pGeom == "rightArrow") shape.Type = ShapeType.BlockArrowRight;
                    else if (pGeom == "leftArrow") shape.Type = ShapeType.BlockArrowLeft;
                    else if (pGeom == "upArrow") shape.Type = ShapeType.BlockArrowUp;
                    else if (pGeom == "downArrow") shape.Type = ShapeType.BlockArrowDown;
                    else if (pGeom.EndsWith("Arrow")) shape.Type = ShapeType.Arrow;
                    else if (pGeom == "heart") shape.Type = ShapeType.Heart;
                    else if (pGeom.Contains("Callout") || pGeom == "callout") shape.Type = ShapeType.Callout;
                    else if (pGeom.StartsWith("cloud")) shape.Type = ShapeType.Cloud;
                    else if (pGeom == "chevron" || pGeom == "homePlate") shape.Type = ShapeType.Chevron;
                    else if (pGeom == "lightningBolt") shape.Type = ShapeType.Lightning;
                    else if (pGeom == "sun") shape.Type = ShapeType.Sun;
                    else if (pGeom == "moon") shape.Type = ShapeType.Moon;
                    else if (pGeom == "smileyFace") shape.Type = ShapeType.Smiley;
                    else if (pGeom == "donut") shape.Type = ShapeType.Donut;
                    else if (pGeom == "mathPlus" || pGeom == "plus" || pGeom == "cross") shape.Type = ShapeType.Cross;
                    else if (pGeom == "cube") shape.Type = ShapeType.Cube;
                    else if (pGeom == "can") shape.Type = ShapeType.Can;
                    else if (pGeom == "arc") shape.Type = ShapeType.Arch;
                    else if (pGeom == "line" || pGeom.Contains("Connector")) shape.Type = ShapeType.Line;
                    else
                    {
                        XmlNode cNvSpPr = spNode.SelectSingleNode(".//*[local-name()='cNvSpPr']");
                        bool isTxBox = (cNvSpPr != null && cNvSpPr.Attributes["txBox"] != null &&
                            (cNvSpPr.Attributes["txBox"].Value == "1" || cNvSpPr.Attributes["txBox"].Value.Equals("true", StringComparison.OrdinalIgnoreCase)));
                        shape.Type = isTxBox ? ShapeType.TextBox : ShapeType.Rectangle;
                    }
                }
            }

            // Check arrowheads on lines/connectors
            XmlNode tailEnd = spNode.SelectSingleNode(".//*[local-name()='tailEnd']");
            XmlNode headEnd = spNode.SelectSingleNode(".//*[local-name()='headEnd']");
            if ((tailEnd != null && tailEnd.Attributes["type"] != null && tailEnd.Attributes["type"].Value != "none") ||
                (headEnd != null && headEnd.Attributes["type"] != null && headEnd.Attributes["type"].Value != "none"))
            {
                if (shape.Type == ShapeType.Line) shape.Type = ShapeType.Arrow;
            }

            // Transform Coordinates, Rotation & Flips
            XmlNode xfrm = spNode.SelectSingleNode("./*[local-name()='spPr']/*[local-name()='xfrm']") 
                        ?? spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='xfrm']");
            if (xfrm == null) xfrm = spNode.SelectSingleNode(".//*[local-name()='xfrm']");
            XmlNode off = (xfrm != null) ? (xfrm.SelectSingleNode("./*[local-name()='off']") ?? xfrm.SelectSingleNode(".//*[local-name()='off']")) : null;
            XmlNode ext = (xfrm != null) ? (xfrm.SelectSingleNode("./*[local-name()='ext']") ?? xfrm.SelectSingleNode(".//*[local-name()='ext'][@cx]")) : null;
            bool hasExplicitCoords = false;

            if (off != null && off.Attributes["x"] != null && off.Attributes["y"] != null)
            {
                long xEmu, yEmu;
                if (long.TryParse(off.Attributes["x"].Value, out xEmu)) shape.Transform.X = offsetX + (xEmu / 9525.0f) * scaleX;
                if (long.TryParse(off.Attributes["y"].Value, out yEmu)) shape.Transform.Y = offsetY + (yEmu / 9525.0f) * scaleY;
                hasExplicitCoords = true;
            }
            if (ext != null && ext.Attributes["cx"] != null && ext.Attributes["cy"] != null)
            {
                long cxEmu, cyEmu;
                if (long.TryParse(ext.Attributes["cx"].Value, out cxEmu)) shape.Transform.Width = (cxEmu / 9525.0f) * scaleX;
                if (long.TryParse(ext.Attributes["cy"].Value, out cyEmu)) shape.Transform.Height = (cyEmu / 9525.0f) * scaleY;
                hasExplicitCoords = true;
            }

            // Corner radius adjustment (<a:avLst>)
            XmlNode avLst = spNode.SelectSingleNode(".//*[local-name()='avLst']");
            if (avLst != null)
            {
                XmlNode gd = avLst.SelectSingleNode(".//*[local-name()='gd'][@name='adj']");
                if (gd != null && gd.Attributes["fmla"] != null)
                {
                    string fmla = gd.Attributes["fmla"].Value;
                    string[] parts = fmla.Split(' ');
                    if (parts.Length == 2 && long.TryParse(parts[1], out long adjVal))
                    {
                        float ratio = adjVal / 100000.0f;
                        shape.CornerRadius = Math.Min(shape.Transform.Width, shape.Transform.Height) * ratio;
                    }
                }
            }

            // Rotation and Flips from xfrm
            if (xfrm != null)
            {
                if (xfrm.Attributes["rot"] != null)
                {
                    int rotVal;
                    if (int.TryParse(xfrm.Attributes["rot"].Value, out rotVal))
                    {
                        shape.Transform.Rotation = rotVal / 60000.0f;
                    }
                }
                if (xfrm.Attributes["flipH"] != null)
                {
                    string fh = xfrm.Attributes["flipH"].Value;
                    shape.Transform.FlipHorizontal = (fh == "1" || fh.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                if (xfrm.Attributes["flipV"] != null)
                {
                    string fv = xfrm.Attributes["flipV"].Value;
                    shape.Transform.FlipVertical = (fv == "1" || fv.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
            }

            // Shape Picture Fill (<a:blipFill>)
            XmlNode blipFill = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='blipFill']");
            if (blipFill == null) blipFill = spNode.SelectSingleNode(".//*[local-name()='blipFill']");
            if (blipFill != null && archive != null && relEntry != null)
            {
                XmlNode srcRectNode = blipFill.SelectSingleNode(".//*[local-name()='srcRect']");
                if (srcRectNode != null)
                {
                    float val;
                    if (srcRectNode.Attributes["l"] != null && float.TryParse(srcRectNode.Attributes["l"].Value, out val))
                        shape.CropLeft = Math.Max(0f, val / 100000.0f);
                    if (srcRectNode.Attributes["t"] != null && float.TryParse(srcRectNode.Attributes["t"].Value, out val))
                        shape.CropTop = Math.Max(0f, val / 100000.0f);
                    if (srcRectNode.Attributes["r"] != null && float.TryParse(srcRectNode.Attributes["r"].Value, out val))
                        shape.CropRight = Math.Max(0f, val / 100000.0f);
                    if (srcRectNode.Attributes["b"] != null && float.TryParse(srcRectNode.Attributes["b"].Value, out val))
                        shape.CropBottom = Math.Max(0f, val / 100000.0f);
                }

                XmlNode fillRectNode = blipFill.SelectSingleNode(".//*[local-name()='fillRect']");
                if (fillRectNode != null)
                {
                    float val;
                    if (fillRectNode.Attributes["l"] != null && float.TryParse(fillRectNode.Attributes["l"].Value, out val))
                        shape.FillRectLeft = val / 100000.0f;
                    if (fillRectNode.Attributes["t"] != null && float.TryParse(fillRectNode.Attributes["t"].Value, out val))
                        shape.FillRectTop = val / 100000.0f;
                    if (fillRectNode.Attributes["r"] != null && float.TryParse(fillRectNode.Attributes["r"].Value, out val))
                        shape.FillRectRight = val / 100000.0f;
                    if (fillRectNode.Attributes["b"] != null && float.TryParse(fillRectNode.Attributes["b"].Value, out val))
                        shape.FillRectBottom = val / 100000.0f;
                }

                XmlNode blip = blipFill.SelectSingleNode(".//*[local-name()='blip']");
                if (blip != null)
                {
                    XmlNode alphaMod = blipFill.SelectSingleNode(".//*[local-name()='alphaModFix']");
                    if (alphaMod != null && alphaMod.Attributes["amt"] != null)
                    {
                        float amtVal;
                        if (float.TryParse(alphaMod.Attributes["amt"].Value, out amtVal))
                        {
                            shape.FillOpacity = amtVal / 100000.0f;
                        }
                    }
                    else
                    {
                        XmlNode alphaNode = blipFill.SelectSingleNode(".//*[local-name()='alpha']");
                        if (alphaNode != null && alphaNode.Attributes["val"] != null)
                        {
                            float aVal;
                            if (float.TryParse(alphaNode.Attributes["val"].Value, out aVal))
                            {
                                shape.FillOpacity = aVal / 100000.0f;
                            }
                        }
                    }

                    string rId = "";
                    foreach (XmlAttribute attr in blip.Attributes)
                    {
                        if (attr.LocalName == "embed") { rId = attr.Value; break; }
                    }
                    if (!string.IsNullOrEmpty(rId))
                    {
                        try
                        {
                            XmlDocument rDoc = new XmlDocument();
                            using (Stream rs = relEntry.Open()) rDoc.Load(rs);
                            XmlNode relNode = rDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                            if (relNode != null && relNode.Attributes["Target"] != null)
                            {
                                string target = relNode.Attributes["Target"].Value.Replace('\\', '/');
                                if (target.StartsWith("../")) target = "ppt/" + target.Substring(3);
                                else if (!target.StartsWith("ppt/")) target = "ppt/slides/" + target;

                                ZipArchiveEntry imgZip = archive.GetEntry(target);
                                if (imgZip != null)
                                {
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        using (Stream isStream = imgZip.Open()) isStream.CopyTo(ms);
                                        ms.Position = 0;
                                        if (target.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                                        {
                                            byte[] svgBytes = ms.ToArray();
                                            string svgText = Encoding.UTF8.GetString(svgBytes);
                                            int renderW = Math.Max(1200, (int)Math.Max(shape.Transform.Width * 2, 800));
                                            int renderH = Math.Max(1200, (int)Math.Max(shape.Transform.Height * 2, 800));
                                            shape.CachedImage = ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(svgText, renderW, renderH);
                                            shape.IsSvg = true;
                                            shape.IsGraphic = true;
                                            try
                                            {
                                                string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOffice", "Media");
                                                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                                                string tempSvgPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".svg");
                                                File.WriteAllBytes(tempSvgPath, svgBytes);
                                                shape.ImagePath = tempSvgPath;
                                            }
                                            catch { }
                                            if (shape.Type == ShapeType.Rectangle) shape.Type = ShapeType.Image;
                                        }
                                        else
                                        {
                                            using (Image rawImg = Image.FromStream(ms))
                                            {
                                                shape.CachedImage = new Bitmap(rawImg);
                                            }
                                            if (shape.Type == ShapeType.Rectangle) shape.Type = ShapeType.Image;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }

            // Shape Gradient Fill (<a:gradFill>)
            XmlNode shpGradFill = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='gradFill']");
            if (shpGradFill != null)
            {
                shape.HasGradientFill = true;
                var gsList = shpGradFill.SelectNodes(".//*[local-name()='gs']");
                if (gsList != null && gsList.Count >= 2)
                {
                    shape.GradientStops = new List<GradientStop>();
                    foreach (XmlNode gsNode in gsList)
                    {
                        int clr = ParseColorVal(gsNode, 0x00FFFFFF, pres);
                        float pos = 0f;
                        if (gsNode.Attributes["pos"] != null)
                        {
                            int pVal;
                            if (int.TryParse(gsNode.Attributes["pos"].Value, out pVal)) pos = pVal / 100000.0f;
                        }
                        float trans = 0f;
                        XmlNode alphaNode = gsNode.SelectSingleNode(".//*[local-name()='alpha']");
                        if (alphaNode != null && alphaNode.Attributes["val"] != null)
                        {
                            int aVal;
                            if (int.TryParse(alphaNode.Attributes["val"].Value, out aVal))
                            {
                                trans = 1.0f - (aVal / 100000.0f);
                            }
                        }
                        shape.GradientStops.Add(new GradientStop(clr, pos) { Transparency = trans });
                    }
                    shape.FillColor = shape.GradientStops[0].Color;
                    shape.GradientColor2 = shape.GradientStops[shape.GradientStops.Count - 1].Color;
                }
                XmlNode lin = shpGradFill.SelectSingleNode(".//*[local-name()='lin']");
                if (lin != null && lin.Attributes["ang"] != null)
                {
                    int angVal;
                    if (int.TryParse(lin.Attributes["ang"].Value, out angVal))
                    {
                        shape.GradientAngle = angVal / 60000.0f;
                    }
                }
            }

            // Shape Solid Fill
            XmlNode solidFill = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='solidFill']");
            if (solidFill != null)
            {
                shape.FillColor = ParseColorVal(solidFill, 0x00FFFFFF, pres);
                shape.FillOpacity = ParseAlphaVal(solidFill, 1.0f);
            }

            // Shape Border
            XmlNode lnNode = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='ln']");
            if (lnNode != null)
            {
                bool hasNoFill = lnNode.SelectSingleNode("./*[local-name()='noFill']") != null;
                XmlNode lnSolidFill = lnNode.SelectSingleNode(".//*[local-name()='solidFill']");
                XmlNode lnGradFill = lnNode.SelectSingleNode(".//*[local-name()='gradFill']");
                bool isLineType = (shape.Type == ShapeType.Line || shape.Type == ShapeType.Arrow);

                if (!hasNoFill && (lnSolidFill != null || lnGradFill != null || isLineType))
                {
                    if (lnNode.Attributes["w"] != null)
                    {
                        long lnW;
                        if (long.TryParse(lnNode.Attributes["w"].Value, out lnW))
                        {
                            shape.StrokeWidth = Math.Max(0.5f, lnW / 12700.0f);
                        }
                    }
                    else
                    {
                        shape.StrokeWidth = 1.0f;
                    }

                    XmlNode prstDash = lnNode.SelectSingleNode(".//*[local-name()='prstDash']");
                    if (prstDash != null && prstDash.Attributes["val"] != null)
                    {
                        string dVal = prstDash.Attributes["val"].Value.ToLowerInvariant();
                        if (dVal == "dash" || dVal == "sysdash" || dVal == "lgdash") shape.DashStyle = DashStyle.Dash;
                        else if (dVal == "dot" || dVal == "sysdot") shape.DashStyle = DashStyle.Dot;
                        else if (dVal == "dashdot" || dVal == "sysdashdot" || dVal == "lgdashdot") shape.DashStyle = DashStyle.DashDot;
                        else if (dVal == "lgdashdotdot" || dVal == "sysdashdotdot") shape.DashStyle = DashStyle.DashDotDot;
                        else shape.DashStyle = DashStyle.Solid;
                    }

                    if (lnSolidFill != null)
                    {
                        shape.StrokeColor = ParseColorVal(lnSolidFill, isLineType ? 0x4472C4 : 0x000000, pres);
                    }
                    else if (lnGradFill != null)
                    {
                        shape.StrokeColor = ParseColorVal(lnGradFill, 0x000000, pres);
                    }
                    else
                    {
                        shape.StrokeColor = ParseColorVal(lnNode, isLineType ? 0x4472C4 : 0x000000, pres);
                    }
                }
                else
                {
                    shape.StrokeWidth = 0.0f;
                    shape.StrokeColor = 0x00FFFFFF;
                }
            }
            else
            {
                if (shape.Type == ShapeType.Line || shape.Type == ShapeType.Arrow)
                {
                    shape.StrokeWidth = 1.5f;
                    shape.StrokeColor = 0x4472C4;
                }
            }

            // Shape Effects (<a:effectLst>)
            XmlNode spEffectLst = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='effectLst']");
            if (spEffectLst == null) spEffectLst = spNode.SelectSingleNode(".//*[local-name()='effectLst']");
            if (spEffectLst != null)
            {
                XmlNode softEdge = spEffectLst.SelectSingleNode(".//*[local-name()='softEdge']");
                if (softEdge != null && softEdge.Attributes["rad"] != null)
                {
                    long rad;
                    if (long.TryParse(softEdge.Attributes["rad"].Value, out rad))
                        shape.SoftEdgeRadius = rad / 12700.0f;
                }
                XmlNode blur = spEffectLst.SelectSingleNode(".//*[local-name()='blur']");
                if (blur != null && blur.Attributes["rad"] != null)
                {
                    long rad;
                    if (long.TryParse(blur.Attributes["rad"].Value, out rad))
                        shape.BlurRadius = rad / 12700.0f;
                }
                XmlNode outerShdw = spEffectLst.SelectSingleNode(".//*[local-name()='outerShdw']");
                if (outerShdw != null)
                {
                    shape.HasShadow = true;
                    shape.ShadowColor = ParseColorVal(outerShdw, 0x000000, pres);
                    long dist = 0, blurVal = 0, dir = 0;
                    if (outerShdw.Attributes["dist"] != null) long.TryParse(outerShdw.Attributes["dist"].Value, out dist);
                    if (outerShdw.Attributes["blurRad"] != null) long.TryParse(outerShdw.Attributes["blurRad"].Value, out blurVal);
                    if (outerShdw.Attributes["dir"] != null) long.TryParse(outerShdw.Attributes["dir"].Value, out dir);

                    float distPx = dist / 9525.0f;
                    float angleRad = (float)((dir / 60000.0) * Math.PI / 180.0);
                    shape.ShadowOffsetX = distPx * (float)Math.Cos(angleRad);
                    shape.ShadowOffsetY = distPx * (float)Math.Sin(angleRad);
                    shape.ShadowBlur = blurVal / 9525.0f;
                }
            }

            // Parse Text & Run Properties
            XmlNode txBody = spNode.SelectSingleNode(".//*[local-name()='txBody']");
            bool hasExplicitFontSize = false;
            bool hasExplicitAlignment = false;
            bool hasExplicitVerticalAlignment = false;
            bool hasExplicitFontName = false;
            bool hasExplicitBold = false;

            if (txBody != null)
            {
                XmlNode bodyPr = txBody.SelectSingleNode(".//*[local-name()='bodyPr']");
                if (bodyPr != null)
                {
                    if (bodyPr.Attributes["anchor"] != null)
                    {
                        string anc = bodyPr.Attributes["anchor"].Value;
                        if (anc == "b") { shape.VerticalAlignment = VerticalTextAlignment.Bottom; hasExplicitVerticalAlignment = true; }
                        else if (anc == "ctr") { shape.VerticalAlignment = VerticalTextAlignment.Middle; hasExplicitVerticalAlignment = true; }
                        else if (anc == "t") { shape.VerticalAlignment = VerticalTextAlignment.Top; hasExplicitVerticalAlignment = true; }
                    }
                    long insVal;
                    if (bodyPr.Attributes["lIns"] != null && long.TryParse(bodyPr.Attributes["lIns"].Value, out insVal))
                        shape.MarginLeft = (insVal / 9525.0f) * scaleX;
                    if (bodyPr.Attributes["rIns"] != null && long.TryParse(bodyPr.Attributes["rIns"].Value, out insVal))
                        shape.MarginRight = (insVal / 9525.0f) * scaleX;
                    if (bodyPr.Attributes["tIns"] != null && long.TryParse(bodyPr.Attributes["tIns"].Value, out insVal))
                        shape.MarginTop = (insVal / 9525.0f) * scaleY;
                    if (bodyPr.Attributes["bIns"] != null && long.TryParse(bodyPr.Attributes["bIns"].Value, out insVal))
                        shape.MarginBottom = (insVal / 9525.0f) * scaleY;
                }

                StringBuilder sbText = new StringBuilder();
                XmlNodeList pNodes = txBody.SelectNodes(".//*[local-name()='p']");
                if (pNodes != null)
                {
                    for (int pIdx = 0; pIdx < pNodes.Count; pIdx++)
                    {
                        XmlNode pNode = pNodes[pIdx];
                        // Paragraph Alignment & Line Spacing
                        TextAlignment? pAlignment = null;
                        float? pLineSpacing = null;
                        float? pSpaceBefore = null;
                        float? pSpaceAfter = null;

                        float pFontSize = shape.FontSize;
                        bool pBold = shape.Bold;
                        bool pItalic = shape.Italic;
                        bool pUnderline = shape.Underline;
                        int pTextColor = shape.TextColor;
                        string pFontName = shape.FontName;

                        XmlNode pPr = pNode.SelectSingleNode(".//*[local-name()='pPr']");
                        if (pPr != null)
                        {
                            if (pPr.Attributes["algn"] != null)
                            {
                                string algnStr = pPr.Attributes["algn"].Value;
                                if (algnStr == "ctr") pAlignment = TextAlignment.Center;
                                else if (algnStr == "r") pAlignment = TextAlignment.Right;
                                else if (algnStr == "just") pAlignment = TextAlignment.Justify;
                                else if (algnStr == "l") pAlignment = TextAlignment.Left;
                            }
                            XmlNode lnSpc = pPr.SelectSingleNode(".//*[local-name()='lnSpc']");
                            if (lnSpc != null)
                            {
                                XmlNode spcPts = lnSpc.SelectSingleNode(".//*[local-name()='spcPts']");
                                if (spcPts != null && spcPts.Attributes["val"] != null)
                                {
                                    int ptsVal;
                                    if (int.TryParse(spcPts.Attributes["val"].Value, out ptsVal))
                                    {
                                        pLineSpacing = (ptsVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                    }
                                }
                                XmlNode spcPct = lnSpc.SelectSingleNode(".//*[local-name()='spcPct']");
                                if (spcPct != null && spcPct.Attributes["val"] != null)
                                {
                                    int pctVal;
                                    if (int.TryParse(spcPct.Attributes["val"].Value, out pctVal))
                                    {
                                        pLineSpacing = -(pctVal / 100000.0f);
                                    }
                                }
                            }
                            XmlNode spcBef = pPr.SelectSingleNode(".//*[local-name()='spcBef']");
                            if (spcBef != null)
                            {
                                XmlNode spcPts = spcBef.SelectSingleNode(".//*[local-name()='spcPts']");
                                if (spcPts != null && spcPts.Attributes["val"] != null)
                                {
                                    int ptsVal;
                                    if (int.TryParse(spcPts.Attributes["val"].Value, out ptsVal))
                                    {
                                        pSpaceBefore = (ptsVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                    }
                                }
                            }
                            XmlNode spcAft = pPr.SelectSingleNode(".//*[local-name()='spcAft']");
                            if (spcAft != null)
                            {
                                XmlNode spcPts = spcAft.SelectSingleNode(".//*[local-name()='spcPts']");
                                if (spcPts != null && spcPts.Attributes["val"] != null)
                                {
                                    int ptsVal;
                                    if (int.TryParse(spcPts.Attributes["val"].Value, out ptsVal))
                                    {
                                        pSpaceAfter = (ptsVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                    }
                                }
                            }
                            XmlNode defRPr = pPr.SelectSingleNode(".//*[local-name()='defRPr']");
                            if (defRPr != null)
                            {
                                if (defRPr.Attributes["sz"] != null)
                                {
                                    int szVal;
                                    if (int.TryParse(defRPr.Attributes["sz"].Value, out szVal))
                                    {
                                        pFontSize = (szVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                        if (!hasExplicitFontSize) { shape.FontSize = pFontSize; hasExplicitFontSize = true; }
                                    }
                                }
                                if (defRPr.Attributes["b"] != null)
                                {
                                    pBold = (defRPr.Attributes["b"].Value == "1" || defRPr.Attributes["b"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                    if (!hasExplicitBold) { shape.Bold = pBold; hasExplicitBold = true; }
                                }
                                if (defRPr.Attributes["i"] != null)
                                {
                                    pItalic = (defRPr.Attributes["i"].Value == "1" || defRPr.Attributes["i"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                    shape.Italic = pItalic;
                                }
                                if (defRPr.Attributes["u"] != null)
                                {
                                    pUnderline = (defRPr.Attributes["u"].Value != "none");
                                    shape.Underline = pUnderline;
                                }
                                XmlNode defSolid = defRPr.SelectSingleNode(".//*[local-name()='solidFill']");
                                if (defSolid != null)
                                {
                                    pTextColor = ParseColorVal(defSolid, shape.TextColor, pres);
                                    if (shape.TextRuns.Count == 0) shape.TextColor = pTextColor;
                                }
                                XmlNode lLatin = defRPr.SelectSingleNode(".//*[local-name()='latin']");
                                if (lLatin != null && lLatin.Attributes["typeface"] != null)
                                {
                                    pFontName = ResolveTypeface(lLatin.Attributes["typeface"].Value, majorFont, minorFont);
                                    if (!hasExplicitFontName) { shape.FontName = pFontName; hasExplicitFontName = true; }
                                }
                            }
                        }

                        if (pAlignment.HasValue && !hasExplicitAlignment)
                        {
                            shape.Alignment = pAlignment.Value;
                            hasExplicitAlignment = true;
                        }
                        if (pLineSpacing.HasValue && Math.Abs(shape.LineSpacing - 1.2f) < 0.01f)
                        {
                            shape.LineSpacing = pLineSpacing.Value;
                        }

                        // Process runs and line breaks in document order
                        int runsAddedInP = 0;
                        XmlNodeList rNodes = pNode.SelectNodes(".//*[local-name()='r' or local-name()='br']");
                        if (rNodes != null && rNodes.Count > 0)
                        {
                            foreach (XmlNode rNode in rNodes)
                            {
                                if (rNode.LocalName == "br")
                                {
                                    sbText.Append("\n");
                                    shape.TextRuns.Add(new SlideTextRun("\n", pTextColor, pFontSize, pBold, pFontName)
                                    {
                                        Italic = pItalic,
                                        Underline = pUnderline,
                                        ParaLineSpacing = pLineSpacing,
                                        ParaSpaceBefore = pSpaceBefore,
                                        ParaSpaceAfter = pSpaceAfter,
                                        ParaAlignment = pAlignment
                                    });
                                    runsAddedInP++;
                                    continue;
                                }

                                float rFontSize = pFontSize;
                                bool rBold = pBold;
                                bool rItalic = pItalic;
                                bool rUnderline = pUnderline;
                                int rTextColor = pTextColor;
                                string rFontName = pFontName;
                                bool rHasGradient = false;
                                int rGradientColor2 = pTextColor;
                                float rGradientAngle = 0f;
                                List<GradientStop> rGradientStops = null;
                                bool rHasHighlight = false;
                                int rHighlightColor = 0;

                                XmlNode rPr = rNode.SelectSingleNode(".//*[local-name()='rPr']");
                                if (rPr != null)
                                {
                                    if (rPr.Attributes["sz"] != null)
                                    {
                                        int szVal;
                                        if (int.TryParse(rPr.Attributes["sz"].Value, out szVal))
                                        {
                                            rFontSize = (szVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                            if (!hasExplicitFontSize) { shape.FontSize = rFontSize; hasExplicitFontSize = true; }
                                        }
                                    }
                                    if (rPr.Attributes["b"] != null)
                                    {
                                        rBold = (rPr.Attributes["b"].Value == "1" || rPr.Attributes["b"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                        if (!hasExplicitBold) { shape.Bold = rBold; hasExplicitBold = true; }
                                    }
                                    if (rPr.Attributes["i"] != null)
                                    {
                                        rItalic = (rPr.Attributes["i"].Value == "1" || rPr.Attributes["i"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                        shape.Italic = rItalic;
                                    }
                                    if (rPr.Attributes["u"] != null)
                                    {
                                        rUnderline = (rPr.Attributes["u"].Value != "none");
                                        shape.Underline = rUnderline;
                                    }

                                    XmlNode txtClr = rPr.SelectSingleNode(".//*[local-name()='solidFill']");
                                    if (txtClr != null)
                                    {
                                        rTextColor = ParseColorVal(txtClr, pTextColor, pres);
                                        if (shape.TextRuns.Count == 0) shape.TextColor = rTextColor;
                                    }
                                    else
                                    {
                                        XmlNode txtGrad = rPr.SelectSingleNode(".//*[local-name()='gradFill']");
                                        if (txtGrad != null)
                                        {
                                            var txtGsList = txtGrad.SelectNodes(".//*[local-name()='gs']");
                                            if (txtGsList != null && txtGsList.Count > 0)
                                            {
                                                rTextColor = ParseColorVal(txtGsList[0], pTextColor, pres);
                                                rGradientStops = new List<GradientStop>();
                                                foreach (XmlNode gs in txtGsList)
                                                {
                                                    int clr = ParseColorVal(gs, pTextColor, pres);
                                                    float pos = 0f;
                                                    if (gs.Attributes["pos"] != null)
                                                    {
                                                        int pVal;
                                                        if (int.TryParse(gs.Attributes["pos"].Value, out pVal)) pos = pVal / 100000.0f;
                                                    }
                                                    float trans = 0f;
                                                    XmlNode alphaNode = gs.SelectSingleNode(".//*[local-name()='alpha']");
                                                    if (alphaNode != null && alphaNode.Attributes["val"] != null)
                                                    {
                                                        int aVal;
                                                        if (int.TryParse(alphaNode.Attributes["val"].Value, out aVal))
                                                        {
                                                            trans = Math.Max(0.0f, Math.Min(1.0f, 1.0f - (aVal / 100000.0f)));
                                                        }
                                                    }
                                                    rGradientStops.Add(new GradientStop(clr, pos) { Transparency = trans });
                                                }
                                                if (txtGsList.Count >= 2)
                                                {
                                                    rHasGradient = true;
                                                    rGradientColor2 = ParseColorVal(txtGsList[txtGsList.Count - 1], pTextColor, pres);
                                                }
                                                if (shape.TextRuns.Count == 0) shape.TextColor = rTextColor;
                                            }
                                            XmlNode txtLin = txtGrad.SelectSingleNode(".//*[local-name()='lin']");
                                            if (txtLin != null && txtLin.Attributes["ang"] != null)
                                            {
                                                int aVal;
                                                if (int.TryParse(txtLin.Attributes["ang"].Value, out aVal)) rGradientAngle = aVal / 60000.0f;
                                            }
                                        }
                                    }

                                    XmlNode hlNode = rPr.SelectSingleNode(".//*[local-name()='highlight']");
                                    if (hlNode != null)
                                    {
                                        rHasHighlight = true;
                                        rHighlightColor = ParseColorVal(hlNode, 0xFFFF00, pres);
                                    }

                                    XmlNode latin = rPr.SelectSingleNode(".//*[local-name()='latin']");
                                    if (latin != null && latin.Attributes["typeface"] != null)
                                    {
                                        rFontName = ResolveTypeface(latin.Attributes["typeface"].Value, majorFont, minorFont);
                                        if (!hasExplicitFontName) { shape.FontName = rFontName; hasExplicitFontName = true; }
                                    }
                                }

                                XmlNode t = rNode.SelectSingleNode(".//*[local-name()='t']");
                                if (t != null && !string.IsNullOrEmpty(t.InnerText))
                                {
                                    sbText.Append(t.InnerText);
                                    shape.TextRuns.Add(new SlideTextRun(t.InnerText, rTextColor, rFontSize, rBold, rFontName)
                                    {
                                        Italic = rItalic,
                                        Underline = rUnderline,
                                        HasGradient = rHasGradient,
                                        GradientColor2 = rGradientColor2,
                                        GradientAngle = rGradientAngle,
                                        GradientStops = rGradientStops,
                                        HasHighlight = rHasHighlight,
                                        HighlightColor = rHighlightColor,
                                        ParaLineSpacing = pLineSpacing,
                                        ParaSpaceBefore = pSpaceBefore,
                                        ParaSpaceAfter = pSpaceAfter,
                                        ParaAlignment = pAlignment
                                    });
                                    runsAddedInP++;
                                }
                            }
                        }
                        else
                        {
                            XmlNodeList tNodes = pNode.SelectNodes(".//*[local-name()='t']");
                            if (tNodes != null)
                            {
                                foreach (XmlNode tNode in tNodes)
                                {
                                    if (!string.IsNullOrEmpty(tNode.InnerText))
                                    {
                                        sbText.Append(tNode.InnerText);
                                        shape.TextRuns.Add(new SlideTextRun(tNode.InnerText, pTextColor, pFontSize, pBold, pFontName)
                                        {
                                            Italic = pItalic,
                                            Underline = pUnderline,
                                            ParaLineSpacing = pLineSpacing,
                                            ParaSpaceBefore = pSpaceBefore,
                                            ParaSpaceAfter = pSpaceAfter,
                                            ParaAlignment = pAlignment
                                        });
                                        runsAddedInP++;
                                    }
                                }
                            }
                        }

                        if (pIdx < pNodes.Count - 1)
                        {
                            sbText.Append("\n");
                            shape.TextRuns.Add(new SlideTextRun("\n", pTextColor, pFontSize, pBold, pFontName)
                            {
                                Italic = pItalic,
                                Underline = pUnderline,
                                ParaLineSpacing = pLineSpacing,
                                ParaSpaceBefore = pSpaceBefore,
                                ParaSpaceAfter = pSpaceAfter,
                                ParaAlignment = pAlignment
                            });
                        }
                    }
                }
                shape.Text = sbText.ToString().TrimEnd('\r', '\n');
            }

            // Resolve Placeholder Layout & Master Inheritance
            XmlNode phNode = spNode.SelectSingleNode(".//*[local-name()='ph']");
            string phType = (phNode != null && phNode.Attributes["type"] != null) ? phNode.Attributes["type"].Value : "";
            string phIdx = (phNode != null && phNode.Attributes["idx"] != null) ? phNode.Attributes["idx"].Value : "";
            bool isSubtitle = (phType.Equals("subTitle", StringComparison.OrdinalIgnoreCase) || (string.IsNullOrEmpty(phType) && phIdx == "1") || shape.Name.ToLower().Contains("subtitle"));
            bool isTitle = (!isSubtitle && (phType.Equals("ctrTitle", StringComparison.OrdinalIgnoreCase) || phType.Equals("title", StringComparison.OrdinalIgnoreCase) || (string.IsNullOrEmpty(phType) && phIdx == "0") || shape.Name.ToLower().Contains("title")));
            bool isPlaceholder = (phNode != null || isTitle || isSubtitle);

            if (isPlaceholder)
            {
                // Search layoutDoc first, then masterDoc
                XmlDocument[] placeholderDocs = new XmlDocument[] { layoutDoc, masterDoc };
                foreach (XmlDocument doc in placeholderDocs)
                {
                    if (doc == null) continue;
                    XmlNodeList lSpNodes = doc.SelectNodes("//*[local-name()='sp']");
                    if (lSpNodes == null) continue;

                    foreach (XmlNode lSp in lSpNodes)
                    {
                        XmlNode lPh = lSp.SelectSingleNode(".//*[local-name()='ph']");
                        if (lPh == null) continue;

                        string lt = lPh.Attributes["type"] != null ? lPh.Attributes["type"].Value : "";
                        string li = lPh.Attributes["idx"] != null ? lPh.Attributes["idx"].Value : "";

                        bool isMatch = false;
                        if (!string.IsNullOrEmpty(phType) && lt.Equals(phType, StringComparison.OrdinalIgnoreCase)) isMatch = true;
                        else if (!string.IsNullOrEmpty(phIdx) && li.Equals(phIdx, StringComparison.OrdinalIgnoreCase)) isMatch = true;
                        else if (isTitle && (lt.Equals("ctrTitle", StringComparison.OrdinalIgnoreCase) || lt.Equals("title", StringComparison.OrdinalIgnoreCase) || li == "0")) isMatch = true;
                        else if (isSubtitle && (lt.Equals("subTitle", StringComparison.OrdinalIgnoreCase) || li == "1")) isMatch = true;

                        if (isMatch)
                        {
                            if (!hasExplicitCoords)
                            {
                                XmlNode lXfrm = lSp.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='xfrm']");
                                if (lXfrm == null) lXfrm = lSp.SelectSingleNode(".//*[local-name()='xfrm']");
                                XmlNode lOff = (lXfrm != null) ? lXfrm.SelectSingleNode(".//*[local-name()='off']") : null;
                                XmlNode lExt = (lXfrm != null) ? lXfrm.SelectSingleNode(".//*[local-name()='ext']") : null;
                                if (lOff != null && lOff.Attributes["x"] != null && lOff.Attributes["y"] != null)
                                {
                                    long xE, yE;
                                    if (long.TryParse(lOff.Attributes["x"].Value, out xE)) shape.Transform.X = offsetX + (xE / 9525.0f) * scaleX;
                                    if (long.TryParse(lOff.Attributes["y"].Value, out yE)) shape.Transform.Y = offsetY + (yE / 9525.0f) * scaleY;
                                    hasExplicitCoords = true;
                                }
                                if (lExt != null && lExt.Attributes["cx"] != null && lExt.Attributes["cy"] != null)
                                {
                                    long cxE, cyE;
                                    if (long.TryParse(lExt.Attributes["cx"].Value, out cxE)) shape.Transform.Width = (cxE / 9525.0f) * scaleX;
                                    if (long.TryParse(lExt.Attributes["cy"].Value, out cyE)) shape.Transform.Height = (cyE / 9525.0f) * scaleY;
                                    hasExplicitCoords = true;
                                }
                            }

                            XmlNode lBodyPr = lSp.SelectSingleNode(".//*[local-name()='bodyPr']");
                            if (lBodyPr != null)
                            {
                                if (!hasExplicitVerticalAlignment && lBodyPr.Attributes["anchor"] != null)
                                {
                                    string lanc = lBodyPr.Attributes["anchor"].Value;
                                    if (lanc == "b") { shape.VerticalAlignment = VerticalTextAlignment.Bottom; hasExplicitVerticalAlignment = true; }
                                    else if (lanc == "ctr") { shape.VerticalAlignment = VerticalTextAlignment.Middle; hasExplicitVerticalAlignment = true; }
                                    else if (lanc == "t") { shape.VerticalAlignment = VerticalTextAlignment.Top; hasExplicitVerticalAlignment = true; }
                                }
                                long lInsVal;
                                if (!shape.MarginLeft.HasValue && lBodyPr.Attributes["lIns"] != null && long.TryParse(lBodyPr.Attributes["lIns"].Value, out lInsVal))
                                    shape.MarginLeft = (lInsVal / 9525.0f) * scaleX;
                                if (!shape.MarginRight.HasValue && lBodyPr.Attributes["rIns"] != null && long.TryParse(lBodyPr.Attributes["rIns"].Value, out lInsVal))
                                    shape.MarginRight = (lInsVal / 9525.0f) * scaleX;
                                if (!shape.MarginTop.HasValue && lBodyPr.Attributes["tIns"] != null && long.TryParse(lBodyPr.Attributes["tIns"].Value, out lInsVal))
                                    shape.MarginTop = (lInsVal / 9525.0f) * scaleY;
                                if (!shape.MarginBottom.HasValue && lBodyPr.Attributes["bIns"] != null && long.TryParse(lBodyPr.Attributes["bIns"].Value, out lInsVal))
                                    shape.MarginBottom = (lInsVal / 9525.0f) * scaleY;
                            }

                            XmlNode lPpr = lSp.SelectSingleNode(".//*[local-name()='lvl1pPr']");
                            if (lPpr != null)
                            {
                                if (!hasExplicitAlignment && lPpr.Attributes["algn"] != null)
                                {
                                    string lalgn = lPpr.Attributes["algn"].Value;
                                    if (lalgn == "ctr") { shape.Alignment = TextAlignment.Center; hasExplicitAlignment = true; }
                                    else if (lalgn == "r") { shape.Alignment = TextAlignment.Right; hasExplicitAlignment = true; }
                                    else if (lalgn == "just") { shape.Alignment = TextAlignment.Justify; hasExplicitAlignment = true; }
                                    else if (lalgn == "l") { shape.Alignment = TextAlignment.Left; hasExplicitAlignment = true; }
                                }

                                XmlNode lDefRpr = lPpr.SelectSingleNode(".//*[local-name()='defRPr']");
                                if (lDefRpr != null)
                                {
                                    if (!hasExplicitFontSize && lDefRpr.Attributes["sz"] != null)
                                    {
                                        int lSz;
                                        if (int.TryParse(lDefRpr.Attributes["sz"].Value, out lSz))
                                        {
                                            // sz is in 1/100 pt → convert to canvas pixels: pt × (96/72) × normScaleX
                                            shape.FontSize = (lSz / 100.0f) * (96.0f / 72.0f) * scaleX;
                                            hasExplicitFontSize = true;
                                        }
                                    }
                                    if (!hasExplicitBold && lDefRpr.Attributes["b"] != null)
                                    {
                                        shape.Bold = (lDefRpr.Attributes["b"].Value == "1" || lDefRpr.Attributes["b"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                        hasExplicitBold = true;
                                    }
                                    if (!hasExplicitFontName)
                                    {
                                        XmlNode lLatin = lDefRpr.SelectSingleNode(".//*[local-name()='latin']");
                                        if (lLatin != null && lLatin.Attributes["typeface"] != null)
                                        {
                                            shape.FontName = ResolveTypeface(lLatin.Attributes["typeface"].Value, majorFont, minorFont);
                                            hasExplicitFontName = true;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                // Fallbacks
                float ptToCanvas = (96.0f / 72.0f) * scaleX;
                if (!hasExplicitFontName)
                {
                    shape.FontName = isTitle ? majorFont : minorFont;
                }
                if (!hasExplicitBold)
                {
                    shape.Bold = false;
                }

                if (isTitle)
                {
                    bool isCenteredLayout = phType.Equals("ctrTitle", StringComparison.OrdinalIgnoreCase) || phIdx == "0" || string.IsNullOrEmpty(phType);
                    if (!hasExplicitCoords)
                    {
                        shape.Transform.X = pres.CanvasWidth * 0.08f;
                        shape.Transform.Y = isCenteredLayout ? (pres.CanvasHeight * 0.25f) : (pres.CanvasHeight * 0.08f);
                        shape.Transform.Width = pres.CanvasWidth * 0.84f;
                        shape.Transform.Height = isCenteredLayout ? (pres.CanvasHeight * 0.25f) : (pres.CanvasHeight * 0.16f);
                    }
                    if (!hasExplicitFontSize)
                    {
                        shape.FontSize = (isCenteredLayout ? 60.0f : 44.0f) * ptToCanvas;
                    }
                    if (!hasExplicitAlignment)
                    {
                        shape.Alignment = isCenteredLayout ? TextAlignment.Center : TextAlignment.Left;
                    }
                    if (!hasExplicitVerticalAlignment)
                    {
                        shape.VerticalAlignment = VerticalTextAlignment.Bottom;
                    }
                }
                else if (isSubtitle)
                {
                    if (!hasExplicitCoords)
                    {
                        shape.Transform.X = pres.CanvasWidth * 0.08f;
                        shape.Transform.Y = pres.CanvasHeight * 0.52f;
                        shape.Transform.Width = pres.CanvasWidth * 0.84f;
                        shape.Transform.Height = pres.CanvasHeight * 0.20f;
                    }
                    if (!hasExplicitFontSize) shape.FontSize = 24.0f * ptToCanvas;
                    if (!hasExplicitAlignment) shape.Alignment = TextAlignment.Center;
                    if (!hasExplicitVerticalAlignment) shape.VerticalAlignment = VerticalTextAlignment.Top;
                }
                else if (phType == "body" || phType == "obj")
                {
                    if (!hasExplicitCoords)
                    {
                        shape.Transform.X = pres.CanvasWidth * 0.08f;
                        shape.Transform.Y = pres.CanvasHeight * 0.26f;
                        shape.Transform.Width = pres.CanvasWidth * 0.84f;
                        shape.Transform.Height = pres.CanvasHeight * 0.64f;
                    }
                    if (!hasExplicitFontSize) shape.FontSize = 18.0f * ptToCanvas;
                    if (!hasExplicitAlignment) shape.Alignment = TextAlignment.Left;
                    if (!hasExplicitVerticalAlignment) shape.VerticalAlignment = VerticalTextAlignment.Top;
                }
            }

            TryDetectAndExtractMedia(spNode, shape, archive, relEntry);
            return shape;
        }

        private static void TryDetectAndExtractMedia(XmlNode node, ShapeNode shape, ZipArchive archive, ZipArchiveEntry relEntry)
        {
            if (node == null || shape == null || archive == null || relEntry == null) return;

            try
            {
                // 1. Look for explicit video/audio/media XML elements
                XmlNode videoNode = node.SelectSingleNode(".//*[local-name()='videoFile'] | .//*[local-name()='quickTimeFile']");
                XmlNode audioNode = node.SelectSingleNode(".//*[local-name()='audioFile'] | .//*[local-name()='audioCd']");
                XmlNode mediaNode = node.SelectSingleNode(".//*[local-name()='media']");

                List<string> candidateRelIds = new List<string>();

                Action<XmlNode> addRelFromNode = (n) =>
                {
                    if (n == null || n.Attributes == null) return;
                    foreach (XmlAttribute attr in n.Attributes)
                    {
                        if (attr.Value != null && attr.Value.StartsWith("rId") && !candidateRelIds.Contains(attr.Value))
                        {
                            candidateRelIds.Add(attr.Value);
                        }
                    }
                };

                addRelFromNode(videoNode);
                addRelFromNode(audioNode);
                addRelFromNode(mediaNode);

                // Also check all descendant nodes within nvPr / nvPicPr / nvSpPr
                XmlNode nvPr = node.SelectSingleNode(".//*[local-name()='nvPr'] | .//*[local-name()='nvPicPr'] | .//*[local-name()='nvSpPr']");
                if (nvPr != null)
                {
                    XmlNodeList allDesc = nvPr.SelectNodes(".//*");
                    if (allDesc != null)
                    {
                        foreach (XmlNode d in allDesc)
                        {
                            if (d.Attributes != null)
                            {
                                foreach (XmlAttribute a in d.Attributes)
                                {
                                    if (a.Value != null && a.Value.StartsWith("rId") && !candidateRelIds.Contains(a.Value))
                                    {
                                        candidateRelIds.Add(a.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (candidateRelIds.Count == 0) return;

                XmlDocument rDoc = new XmlDocument();
                using (Stream rs = relEntry.Open()) rDoc.Load(rs);

                string[] videoExts = { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".webm", ".flv", ".m4v", ".mpg", ".mpeg", ".3gp", ".ts" };
                string[] audioExts = { ".mp3", ".wav", ".wma", ".m4a", ".aac", ".ogg", ".flac", ".mid", ".midi", ".aiff" };

                foreach (string rId in candidateRelIds)
                {
                    XmlNode relNode = rDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                    if (relNode == null || relNode.Attributes["Target"] == null) continue;

                    string target = relNode.Attributes["Target"].Value.Replace('\\', '/');
                    string relType = (relNode.Attributes["Type"] != null) ? relNode.Attributes["Type"].Value : "";

                    bool isVideo = false;
                    bool isAudio = false;

                    if (videoNode != null || relType.EndsWith("/video", StringComparison.OrdinalIgnoreCase))
                    {
                        isVideo = true;
                    }
                    else if (audioNode != null || relType.EndsWith("/audio", StringComparison.OrdinalIgnoreCase))
                    {
                        isAudio = true;
                    }
                    else
                    {
                        string ext = Path.GetExtension(target).ToLowerInvariant();
                        if (Array.IndexOf(videoExts, ext) >= 0) isVideo = true;
                        else if (Array.IndexOf(audioExts, ext) >= 0) isAudio = true;
                    }

                    if (!isVideo && !isAudio) continue;

                    string mediaPath = null;
                    if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        mediaPath = target;
                    }
                    else
                    {
                        string zipTargetPath = target;
                        if (zipTargetPath.StartsWith("../")) zipTargetPath = "ppt/" + zipTargetPath.Substring(3);
                        else if (!zipTargetPath.StartsWith("ppt/")) zipTargetPath = "ppt/slides/" + zipTargetPath;

                        ZipArchiveEntry mEntry = archive.GetEntry(zipTargetPath);
                        if (mEntry == null)
                        {
                            foreach (var e in archive.Entries)
                            {
                                if (e.FullName.Equals(zipTargetPath, StringComparison.OrdinalIgnoreCase) ||
                                    e.FullName.EndsWith(Path.GetFileName(zipTargetPath), StringComparison.OrdinalIgnoreCase))
                                {
                                    mEntry = e;
                                    break;
                                }
                            }
                        }

                        if (mEntry != null)
                        {
                            try
                            {
                                string mediaDir = Path.Combine(Path.GetTempPath(), "ZenQOffice", "Media");
                                if (!Directory.Exists(mediaDir)) Directory.CreateDirectory(mediaDir);
                                string mExt = Path.GetExtension(mEntry.FullName);
                                string safeName = Path.GetFileNameWithoutExtension(mEntry.FullName);
                                string destFile = Path.Combine(mediaDir, string.Format("{0}_{1}_{2:X8}{3}", safeName, mEntry.Length, mEntry.Crc32, mExt));
                                if (!File.Exists(destFile) || new FileInfo(destFile).Length != mEntry.Length)
                                {
                                    using (Stream inS = mEntry.Open())
                                    using (FileStream outS = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                    {
                                        inS.CopyTo(outS);
                                    }
                                }
                                mediaPath = destFile;
                            }
                            catch { }
                        }
                    }

                    if (!string.IsNullOrEmpty(mediaPath))
                    {
                        if (isVideo)
                        {
                            shape.Type = ShapeType.Video;
                            shape.VideoPath = mediaPath;
                            shape.VideoStartMode = VideoStartMode.InClickSequence;
                            shape.VideoVolume = VideoVolumeLevel.Medium;
                            shape.VideoVolumeLevel = "Medium";
                            if (shape.VideoBookmarks == null) shape.VideoBookmarks = new List<float>();
                            if (shape.CachedImage == null && File.Exists(mediaPath))
                            {
                                try { shape.CachedImage = VideoThumbnailHelper.GetVideoPosterFrame(mediaPath); } catch { }
                            }
                            break;
                        }
                        else if (isAudio)
                        {
                            shape.Type = ShapeType.Audio;
                            shape.AudioPath = mediaPath;
                            shape.AudioStartMode = VideoStartMode.InClickSequence;
                            shape.AudioVolume = VideoVolumeLevel.Medium;
                            if (shape.AudioBookmarks == null) shape.AudioBookmarks = new List<float>();
                            try { SlideAudioPlayerManager.Instance.InspectAudioDuration(shape); } catch { }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Media extraction error: " + ex.Message);
            }
        }

        private static ShapeNode ExtractPictureNode(XmlNode picNode, ZipArchive archive, ZipArchiveEntry relEntry, float offsetX, float offsetY, float scaleX, float scaleY)
        {
            ShapeNode picShape = new ShapeNode();
            picShape.Type = ShapeType.Image;
            picShape.FillColor = 0x00FFFFFF;
            picShape.StrokeWidth = 0.0f;

            XmlNode cNvPr = picNode.SelectSingleNode(".//*[local-name()='cNvPr']");
            if (cNvPr != null)
            {
                if (cNvPr.Attributes["id"] != null) picShape.Id = cNvPr.Attributes["id"].Value;
                if (cNvPr.Attributes["name"] != null) picShape.Name = cNvPr.Attributes["name"].Value;
            }

            XmlNode pxFrm = picNode.SelectSingleNode("./*[local-name()='spPr']/*[local-name()='xfrm']") 
                         ?? picNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='xfrm']") 
                         ?? picNode.SelectSingleNode(".//*[local-name()='xfrm']");
            XmlNode off = (pxFrm != null) ? (pxFrm.SelectSingleNode("./*[local-name()='off']") ?? pxFrm.SelectSingleNode(".//*[local-name()='off']")) 
                                          : picNode.SelectSingleNode(".//*[local-name()='off']");
            XmlNode ext = (pxFrm != null) ? (pxFrm.SelectSingleNode("./*[local-name()='ext']") ?? pxFrm.SelectSingleNode(".//*[local-name()='ext'][@cx]")) 
                                          : picNode.SelectSingleNode(".//*[local-name()='ext'][@cx]");

            if (off != null && off.Attributes["x"] != null && off.Attributes["y"] != null)
            {
                long xEmu, yEmu;
                if (long.TryParse(off.Attributes["x"].Value, out xEmu)) picShape.Transform.X = offsetX + (xEmu / 9525.0f) * scaleX;
                if (long.TryParse(off.Attributes["y"].Value, out yEmu)) picShape.Transform.Y = offsetY + (yEmu / 9525.0f) * scaleY;
            }
            if (ext != null && ext.Attributes["cx"] != null && ext.Attributes["cy"] != null)
            {
                long cxEmu, cyEmu;
                if (long.TryParse(ext.Attributes["cx"].Value, out cxEmu)) picShape.Transform.Width = (cxEmu / 9525.0f) * scaleX;
                if (long.TryParse(ext.Attributes["cy"].Value, out cyEmu)) picShape.Transform.Height = (cyEmu / 9525.0f) * scaleY;
            }

            if (pxFrm != null)
            {
                if (pxFrm.Attributes["rot"] != null)
                {
                    int rVal;
                    if (int.TryParse(pxFrm.Attributes["rot"].Value, out rVal)) picShape.Transform.Rotation = rVal / 60000.0f;
                }
                if (pxFrm.Attributes["flipH"] != null)
                {
                    string fh = pxFrm.Attributes["flipH"].Value;
                    picShape.Transform.FlipHorizontal = (fh == "1" || fh.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                if (pxFrm.Attributes["flipV"] != null)
                {
                    string fv = pxFrm.Attributes["flipV"].Value;
                    picShape.Transform.FlipVertical = (fv == "1" || fv.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
            }

            // Image Crop (<a:srcRect>) and Fill Rect (<a:fillRect>)
            XmlNode blipFill = picNode.SelectSingleNode(".//*[local-name()='blipFill']");
            if (blipFill != null)
            {
                XmlNode srcRectNode = blipFill.SelectSingleNode(".//*[local-name()='srcRect']");
                if (srcRectNode != null)
                {
                    float val;
                    if (srcRectNode.Attributes["l"] != null && float.TryParse(srcRectNode.Attributes["l"].Value, out val))
                        picShape.CropLeft = Math.Max(0f, val / 100000.0f);
                    if (srcRectNode.Attributes["t"] != null && float.TryParse(srcRectNode.Attributes["t"].Value, out val))
                        picShape.CropTop = Math.Max(0f, val / 100000.0f);
                    if (srcRectNode.Attributes["r"] != null && float.TryParse(srcRectNode.Attributes["r"].Value, out val))
                        picShape.CropRight = Math.Max(0f, val / 100000.0f);
                    if (srcRectNode.Attributes["b"] != null && float.TryParse(srcRectNode.Attributes["b"].Value, out val))
                        picShape.CropBottom = Math.Max(0f, val / 100000.0f);
                }

                XmlNode fillRectNode = blipFill.SelectSingleNode(".//*[local-name()='fillRect']");
                if (fillRectNode != null)
                {
                    float val;
                    if (fillRectNode.Attributes["l"] != null && float.TryParse(fillRectNode.Attributes["l"].Value, out val))
                        picShape.FillRectLeft = val / 100000.0f;
                    if (fillRectNode.Attributes["t"] != null && float.TryParse(fillRectNode.Attributes["t"].Value, out val))
                        picShape.FillRectTop = val / 100000.0f;
                    if (fillRectNode.Attributes["r"] != null && float.TryParse(fillRectNode.Attributes["r"].Value, out val))
                        picShape.FillRectRight = val / 100000.0f;
                    if (fillRectNode.Attributes["b"] != null && float.TryParse(fillRectNode.Attributes["b"].Value, out val))
                        picShape.FillRectBottom = val / 100000.0f;
                }
            }

            // Image Effects (<a:effectLst>)
            XmlNode effectLst = picNode.SelectSingleNode(".//*[local-name()='effectLst']");
            if (effectLst != null)
            {
                XmlNode softEdge = effectLst.SelectSingleNode(".//*[local-name()='softEdge']");
                if (softEdge != null && softEdge.Attributes["rad"] != null)
                {
                    long rad;
                    if (long.TryParse(softEdge.Attributes["rad"].Value, out rad))
                        picShape.SoftEdgeRadius = rad / 12700.0f;
                }
                XmlNode blur = effectLst.SelectSingleNode(".//*[local-name()='blur']");
                if (blur != null && blur.Attributes["rad"] != null)
                {
                    long rad;
                    if (long.TryParse(blur.Attributes["rad"].Value, out rad))
                        picShape.BlurRadius = rad / 12700.0f;
                }
                XmlNode outerShdw = effectLst.SelectSingleNode(".//*[local-name()='outerShdw']");
                if (outerShdw != null)
                {
                    picShape.HasShadow = true;
                    long dist = 0, blurVal = 0, dir = 0;
                    if (outerShdw.Attributes["dist"] != null) long.TryParse(outerShdw.Attributes["dist"].Value, out dist);
                    if (outerShdw.Attributes["blurRad"] != null) long.TryParse(outerShdw.Attributes["blurRad"].Value, out blurVal);
                    if (outerShdw.Attributes["dir"] != null) long.TryParse(outerShdw.Attributes["dir"].Value, out dir);

                    float distPx = dist / 9525.0f;
                    float angleRad = (float)((dir / 60000.0) * Math.PI / 180.0);
                    picShape.ShadowOffsetX = distPx * (float)Math.Cos(angleRad);
                    picShape.ShadowOffsetY = distPx * (float)Math.Sin(angleRad);
                    picShape.ShadowBlur = blurVal / 9525.0f;
                }
            }

            // Extract image from rels & zip
            XmlNode blip = picNode.SelectSingleNode(".//*[local-name()='blip']");
            if (blip != null)
            {
                XmlNode alphaMod = picNode.SelectSingleNode(".//*[local-name()='alphaModFix']");
                if (alphaMod != null && alphaMod.Attributes["amt"] != null)
                {
                    float amtVal;
                    if (float.TryParse(alphaMod.Attributes["amt"].Value, out amtVal))
                    {
                        picShape.FillOpacity = amtVal / 100000.0f;
                    }
                }
                else
                {
                    XmlNode alphaNode = picNode.SelectSingleNode(".//*[local-name()='alpha']");
                    if (alphaNode != null && alphaNode.Attributes["val"] != null)
                    {
                        float aVal;
                        if (float.TryParse(alphaNode.Attributes["val"].Value, out aVal))
                        {
                            picShape.FillOpacity = aVal / 100000.0f;
                        }
                    }
                }

                string rId = "";
                foreach (XmlAttribute attr in blip.Attributes)
                {
                    if (attr.LocalName == "embed") { rId = attr.Value; break; }
                }

                if (!string.IsNullOrEmpty(rId) && relEntry != null)
                {
                    try
                    {
                        XmlDocument rDoc = new XmlDocument();
                        using (Stream rs = relEntry.Open()) rDoc.Load(rs);
                        XmlNode relNode = rDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                        if (relNode != null && relNode.Attributes["Target"] != null)
                        {
                            string target = relNode.Attributes["Target"].Value.Replace('\\', '/');
                            if (target.StartsWith("../")) target = "ppt/" + target.Substring(3);
                            else if (!target.StartsWith("ppt/")) target = "ppt/slides/" + target;

                            ZipArchiveEntry imgZip = archive.GetEntry(target);
                            if (imgZip != null)
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    using (Stream isStream = imgZip.Open()) isStream.CopyTo(ms);
                                    ms.Position = 0;
                                    if (target.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                                    {
                                        byte[] svgBytes = ms.ToArray();
                                        string svgText = Encoding.UTF8.GetString(svgBytes);
                                        int renderW = Math.Max(1200, (int)Math.Max(picShape.Transform.Width * 2, 800));
                                        int renderH = Math.Max(1200, (int)Math.Max(picShape.Transform.Height * 2, 800));
                                        picShape.CachedImage = ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(svgText, renderW, renderH);
                                        picShape.IsSvg = true;
                                        picShape.IsGraphic = true;
                                        try
                                        {
                                            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOffice", "Media");
                                            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                                            string tempSvgPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".svg");
                                            File.WriteAllBytes(tempSvgPath, svgBytes);
                                            picShape.ImagePath = tempSvgPath;
                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                        using (Image rawImg = Image.FromStream(ms))
                                        {
                                            picShape.CachedImage = new Bitmap(rawImg);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            TryDetectAndExtractMedia(picNode, picShape, archive, relEntry);
            return picShape;
        }

        private static void ExtractGroupShapes(XmlNode grpNode, List<ShapeNode> outShapes, XmlDocument layoutDoc, XmlDocument masterDoc, PresentationRoot pres, ZipArchive archive, ZipArchiveEntry relEntry, float parentOffX, float parentOffY, float parentScaleX, float parentScaleY, string majorFont = "Calibri Light", string minorFont = "Calibri")
        {
            if (grpNode == null) return;

            XmlNode xfrm = grpNode.SelectSingleNode("./*[local-name()='grpSpPr']/*[local-name()='xfrm']");
            if (xfrm == null) xfrm = grpNode.SelectSingleNode(".//*[local-name()='xfrm']");

            float gx = 0, gy = 0, gw = 100, gh = 100;
            float cx = 0, cy = 0, cw = 100, ch = 100;
            float gRot = 0f;
            bool gFlipH = false, gFlipV = false;

            if (xfrm != null)
            {
                XmlNode off = xfrm.SelectSingleNode("./*[local-name()='off']");
                XmlNode ext = xfrm.SelectSingleNode("./*[local-name()='ext']");
                XmlNode chOff = xfrm.SelectSingleNode("./*[local-name()='chOff']");
                XmlNode chExt = xfrm.SelectSingleNode("./*[local-name()='chExt']");

                if (off != null && off.Attributes["x"] != null && off.Attributes["y"] != null)
                {
                    long xE, yE;
                    if (long.TryParse(off.Attributes["x"].Value, out xE)) gx = xE / 9525.0f;
                    if (long.TryParse(off.Attributes["y"].Value, out yE)) gy = yE / 9525.0f;
                }
                if (ext != null && ext.Attributes["cx"] != null && ext.Attributes["cy"] != null)
                {
                    long cxE, cyE;
                    if (long.TryParse(ext.Attributes["cx"].Value, out cxE)) gw = cxE / 9525.0f;
                    if (long.TryParse(ext.Attributes["cy"].Value, out cyE)) gh = cyE / 9525.0f;
                }
                if (chOff != null && chOff.Attributes["x"] != null && chOff.Attributes["y"] != null)
                {
                    long xE, yE;
                    if (long.TryParse(chOff.Attributes["x"].Value, out xE)) cx = xE / 9525.0f;
                    if (long.TryParse(chOff.Attributes["y"].Value, out yE)) cy = yE / 9525.0f;
                }
                if (chExt != null && chExt.Attributes["cx"] != null && chExt.Attributes["cy"] != null)
                {
                    long cxE, cyE;
                    if (long.TryParse(chExt.Attributes["cx"].Value, out cxE)) cw = cxE / 9525.0f;
                    if (long.TryParse(chExt.Attributes["cy"].Value, out cyE)) ch = cyE / 9525.0f;
                }

                if (xfrm.Attributes["rot"] != null)
                {
                    int rVal;
                    if (int.TryParse(xfrm.Attributes["rot"].Value, out rVal))
                    {
                        gRot = rVal / 60000.0f;
                    }
                }
                if (xfrm.Attributes["flipH"] != null)
                {
                    string fh = xfrm.Attributes["flipH"].Value;
                    gFlipH = (fh == "1" || fh.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                if (xfrm.Attributes["flipV"] != null)
                {
                    string fv = xfrm.Attributes["flipV"].Value;
                    gFlipV = (fv == "1" || fv.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (cw <= 0) cw = gw;
            if (ch <= 0) ch = gh;

            float sx = (gw / cw);
            float sy = (gh / ch);
            float localScaleX = parentScaleX * sx;
            float localScaleY = parentScaleY * sy;

            // Extract child shapes, pictures, tables, and nested groups
            List<ShapeNode> localShapes = new List<ShapeNode>();
            foreach (XmlNode child in grpNode.ChildNodes)
            {
                string ln = child.LocalName;
                if (ln == "sp" || ln == "cxnSp")
                {
                    ShapeNode s = ExtractShapeFromSpNode(child, layoutDoc, masterDoc, pres, 0, 0, localScaleX, localScaleY, majorFont, minorFont, archive, relEntry);
                    if (s != null) localShapes.Add(s);
                }
                else if (ln == "pic")
                {
                    ShapeNode p = ExtractPictureNode(child, archive, relEntry, 0, 0, localScaleX, localScaleY);
                    if (p != null) localShapes.Add(p);
                }
                else if (ln == "graphicFrame")
                {
                    ShapeNode gf = ExtractGraphicFrameNode(child, pres, 0, 0, localScaleX, localScaleY, majorFont, minorFont, archive, relEntry);
                    if (gf != null) localShapes.Add(gf);
                }
                else if (ln == "grpSp")
                {
                    ExtractGroupShapes(child, localShapes, layoutDoc, masterDoc, pres, archive, relEntry, 0, 0, localScaleX, localScaleY, majorFont, minorFont);
                }
            }

            // Apply this group's position, scale, rotation, and flips
            float gcx = gx + gw / 2.0f;
            float gcy = gy + gh / 2.0f;
            double rad = gRot * Math.PI / 180.0;
            double cosR = Math.Cos(rad);
            double sinR = Math.Sin(rad);

            foreach (ShapeNode s in localShapes)
            {
                if (s == null || s.Transform == null) continue;

                // Child center in child coordinate space
                float childCenterX_in_child = (s.Transform.X + s.Transform.Width / 2.0f) / Math.Max(0.0001f, localScaleX);
                float childCenterY_in_child = (s.Transform.Y + s.Transform.Height / 2.0f) / Math.Max(0.0001f, localScaleY);

                float normX = (childCenterX_in_child - cx) / Math.Max(0.0001f, cw);
                float normY = (childCenterY_in_child - cy) / Math.Max(0.0001f, ch);

                if (gFlipH)
                {
                    normX = 1.0f - normX;
                    s.Transform.FlipHorizontal = !s.Transform.FlipHorizontal;
                }
                if (gFlipV)
                {
                    normY = 1.0f - normY;
                    s.Transform.FlipVertical = !s.Transform.FlipVertical;
                }

                float posX = gx + normX * gw;
                float posY = gy + normY * gh;

                float rotatedCenterX, rotatedCenterY;
                if (Math.Abs(gRot) > 0.001f)
                {
                    double dx = posX - gcx;
                    double dy = posY - gcy;
                    rotatedCenterX = (float)(gcx + dx * cosR - dy * sinR);
                    rotatedCenterY = (float)(gcy + dx * sinR + dy * cosR);
                }
                else
                {
                    rotatedCenterX = posX;
                    rotatedCenterY = posY;
                }

                float finalCenterX = parentOffX + rotatedCenterX * parentScaleX;
                float finalCenterY = parentOffY + rotatedCenterY * parentScaleY;

                s.Transform.X = finalCenterX - s.Transform.Width / 2.0f;
                s.Transform.Y = finalCenterY - s.Transform.Height / 2.0f;
                s.Transform.Rotation += gRot;

                outShapes.Add(s);
            }
        }

        private static ShapeNode ExtractGraphicFrameNode(XmlNode gfNode, PresentationRoot pres, float offsetX, float offsetY, float scaleX, float scaleY, string majorFont = "Calibri Light", string minorFont = "Calibri", ZipArchive archive = null, ZipArchiveEntry relEntry = null)
        {
            if (gfNode == null) return null;

            ShapeNode shape = new ShapeNode();
            shape.Id = Guid.NewGuid().ToString();
            shape.Name = "Bảng";

            XmlNode cNvPr = gfNode.SelectSingleNode(".//*[local-name()='cNvPr']");
            if (cNvPr != null)
            {
                if (cNvPr.Attributes["id"] != null) shape.Id = cNvPr.Attributes["id"].Value;
                if (cNvPr.Attributes["name"] != null) shape.Name = cNvPr.Attributes["name"].Value;
            }

            XmlNode xfrm = gfNode.SelectSingleNode("./*[local-name()='xfrm']") ?? gfNode.SelectSingleNode(".//*[local-name()='xfrm']");
            if (xfrm != null)
            {
                XmlNode off = xfrm.SelectSingleNode("./*[local-name()='off']") ?? xfrm.SelectSingleNode(".//*[local-name()='off']");
                XmlNode ext = xfrm.SelectSingleNode("./*[local-name()='ext']") ?? xfrm.SelectSingleNode(".//*[local-name()='ext'][@cx]");

                if (off != null && off.Attributes["x"] != null && off.Attributes["y"] != null)
                {
                    long xEmu, yEmu;
                    if (long.TryParse(off.Attributes["x"].Value, out xEmu)) shape.Transform.X = offsetX + (xEmu / 9525.0f) * scaleX;
                    if (long.TryParse(off.Attributes["y"].Value, out yEmu)) shape.Transform.Y = offsetY + (yEmu / 9525.0f) * scaleY;
                }
                if (ext != null && ext.Attributes["cx"] != null && ext.Attributes["cy"] != null)
                {
                    long cxEmu, cyEmu;
                    if (long.TryParse(ext.Attributes["cx"].Value, out cxEmu)) shape.Transform.Width = (cxEmu / 9525.0f) * scaleX;
                    if (long.TryParse(ext.Attributes["cy"].Value, out cyEmu)) shape.Transform.Height = (cyEmu / 9525.0f) * scaleY;
                }
                if (xfrm.Attributes["rot"] != null)
                {
                    int rVal;
                    if (int.TryParse(xfrm.Attributes["rot"].Value, out rVal)) shape.Transform.Rotation = rVal / 60000.0f;
                }
            }

            XmlNode tblNode = gfNode.SelectSingleNode(".//*[local-name()='tbl']");
            if (tblNode != null)
            {
                shape.Type = ShapeType.Table;
                shape.TableData = new List<List<string>>();
                shape.TableCellFillColors = new Dictionary<string, int>();
                shape.TableCellTextColors = new Dictionary<string, int>();
                shape.TableCellBolds = new Dictionary<string, bool>();
                shape.TableHeaderRow = true;
                shape.TableBandedRows = true;
                shape.FontSize = 14.0f * (96.0f / 72.0f) * scaleX;
                shape.FontName = minorFont ?? "Segoe UI";

                string tableStyleId = "";
                XmlNode tblPr = tblNode.SelectSingleNode("./*[local-name()='tblPr']");
                if (tblPr != null)
                {
                    XmlNode styleIdNode = tblPr.SelectSingleNode("./*[local-name()='tableStyleId']");
                    if (styleIdNode != null) tableStyleId = styleIdNode.InnerText;

                    if (tblPr.Attributes["firstRow"] != null)
                    {
                        string fr = tblPr.Attributes["firstRow"].Value;
                        shape.TableHeaderRow = (fr == "1" || fr.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                    if (tblPr.Attributes["bandRow"] != null)
                    {
                        string br = tblPr.Attributes["bandRow"].Value;
                        shape.TableBandedRows = (br == "1" || br.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                }

                int accent2Color = (pres != null) ? pres.GetThemeColor("accent2", 0xED7D31) : 0xED7D31;
                int accent1Color = (pres != null) ? pres.GetThemeColor("accent1", 0x5B9BD5) : 0x5B9BD5;

                bool isBasicGrid = string.IsNullOrEmpty(tableStyleId) || tableStyleId.Contains("5940675A");
                bool isLightStyle = tableStyleId.Contains("5DA37D80") || tableStyleId.IndexOf("light", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isBasicGrid)
                {
                    shape.TableHasHeaderFill = false;
                    shape.TableHasBandedFill = false;
                    shape.StrokeColor = 0x000000; // Line đen
                    shape.FillColor = 0x00FFFFFF; // Nền trong
                }
                else if (isLightStyle)
                {
                    shape.TableHasHeaderFill = false;
                    shape.TableHasBandedFill = true; // Hàng xen kẽ có màu nhạt chuẩn PPT
                    if (tableStyleId.Contains("5DA37D80") || tableStyleId.IndexOf("accent2", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shape.StrokeColor = accent2Color; // Accent 2 border (ED7D31)
                        shape.FillColor = 0x00FFFFFF;
                    }
                    else
                    {
                        shape.StrokeColor = accent1Color;
                        shape.FillColor = 0x00FFFFFF;
                    }
                }
                else
                {
                    // Medium / Dark / Themed table styles (e.g. Slide 21: 8A107856)
                    shape.TableHasHeaderFill = true;
                    shape.TableHasBandedFill = true;
                    if (tableStyleId.Contains("8A107856") || tableStyleId.IndexOf("accent2", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shape.StrokeColor = accent2Color;
                        shape.FillColor = accent2Color;
                    }
                    else
                    {
                        shape.StrokeColor = accent1Color;
                        shape.FillColor = accent1Color;
                    }
                }

                XmlNodeList trNodes = tblNode.SelectNodes("./*[local-name()='tr']");
                if (trNodes == null || trNodes.Count == 0)
                {
                    trNodes = tblNode.SelectNodes(".//*[local-name()='tr']");
                }

                int maxCols = 0;
                int rIdx = 0;
                if (trNodes != null)
                {
                    foreach (XmlNode tr in trNodes)
                    {
                        List<string> rowTexts = new List<string>();
                        XmlNodeList tcNodes = tr.SelectNodes("./*[local-name()='tc']");
                        if (tcNodes == null || tcNodes.Count == 0)
                        {
                            tcNodes = tr.SelectNodes(".//*[local-name()='tc']");
                        }

                        int cIdx = 0;
                        if (tcNodes != null)
                        {
                            foreach (XmlNode tc in tcNodes)
                            {
                                System.Text.StringBuilder cellSb = new System.Text.StringBuilder();
                                XmlNodeList tNodes = tc.SelectNodes(".//*[local-name()='t']");
                                if (tNodes != null)
                                {
                                    foreach (XmlNode t in tNodes)
                                    {
                                        if (!string.IsNullOrEmpty(t.InnerText))
                                        {
                                            cellSb.Append(t.InnerText);
                                        }
                                    }
                                }
                                rowTexts.Add(cellSb.ToString());

                                string cellKey = rIdx + "," + cIdx;

                                XmlNode pPr = tc.SelectSingleNode(".//*[local-name()='pPr']");
                                if (pPr != null && pPr.Attributes["algn"] != null)
                                {
                                    string algn = pPr.Attributes["algn"].Value;
                                    if (algn == "ctr") shape.TableCellAlignments[cellKey] = TextAlignment.Center;
                                    else if (algn == "r") shape.TableCellAlignments[cellKey] = TextAlignment.Right;
                                    else if (algn == "just") shape.TableCellAlignments[cellKey] = TextAlignment.Justify;
                                    else shape.TableCellAlignments[cellKey] = TextAlignment.Left;
                                }

                                XmlNode solidFill = tc.SelectSingleNode(".//*[local-name()='tcPr']/*[local-name()='solidFill']");
                                if (solidFill != null)
                                {
                                    shape.TableCellFillColors[cellKey] = ParseColorVal(solidFill, 0xFFFFFF, pres);
                                }

                                XmlNode rPr = tc.SelectSingleNode(".//*[local-name()='rPr']");
                                if (rPr != null)
                                {
                                    if (rPr.Attributes["b"] != null)
                                    {
                                        string bVal = rPr.Attributes["b"].Value;
                                        shape.TableCellBolds[cellKey] = (bVal == "1" || bVal.Equals("true", StringComparison.OrdinalIgnoreCase));
                                    }
                                    XmlNode rSolidFill = rPr.SelectSingleNode("./*[local-name()='solidFill']");
                                    if (rSolidFill != null)
                                    {
                                        shape.TableCellTextColors[cellKey] = ParseColorVal(rSolidFill, 0x000000, pres);
                                    }
                                }

                                cIdx++;
                            }
                        }

                        if (rowTexts.Count > maxCols) maxCols = rowTexts.Count;
                        shape.TableData.Add(rowTexts);
                        rIdx++;
                    }
                }

                XmlNodeList gridCols = tblNode.SelectNodes(".//*[local-name()='gridCol']");
                if (gridCols != null && gridCols.Count > maxCols) maxCols = gridCols.Count;

                shape.TableRows = shape.TableData.Count;
                shape.TableColumns = Math.Max(1, maxCols);

                foreach (var row in shape.TableData)
                {
                    while (row.Count < shape.TableColumns) row.Add("");
                }

                if (gridCols != null && gridCols.Count == shape.TableColumns)
                {
                    float totalW = 0f;
                    List<float> rawColWidths = new List<float>();
                    foreach (XmlNode gc in gridCols)
                    {
                        long wEmu = 0;
                        if (gc.Attributes["w"] != null && long.TryParse(gc.Attributes["w"].Value, out wEmu))
                        {
                            float w = (wEmu / 9525.0f) * scaleX;
                            rawColWidths.Add(w);
                            totalW += w;
                        }
                        else
                        {
                            rawColWidths.Add(100f);
                            totalW += 100f;
                        }
                    }
                    if (totalW > 0 && shape.Transform.Width > 0)
                    {
                        float ratio = shape.Transform.Width / totalW;
                        foreach (float cw in rawColWidths) shape.TableColumnWidths.Add(cw * ratio);
                    }
                    else
                    {
                        shape.TableColumnWidths.AddRange(rawColWidths);
                    }
                }

                if (trNodes != null && trNodes.Count == shape.TableRows)
                {
                    float totalH = 0f;
                    List<float> rawRowHeights = new List<float>();
                    foreach (XmlNode tr in trNodes)
                    {
                        long hEmu = 0;
                        if (tr.Attributes["h"] != null && long.TryParse(tr.Attributes["h"].Value, out hEmu))
                        {
                            float h = (hEmu / 9525.0f) * scaleY;
                            rawRowHeights.Add(h);
                            totalH += h;
                        }
                        else
                        {
                            rawRowHeights.Add(30f);
                            totalH += 30f;
                        }
                    }
                    if (totalH > 0 && shape.Transform.Height > 0)
                    {
                        float ratio = shape.Transform.Height / totalH;
                        foreach (float rh in rawRowHeights) shape.TableRowHeights.Add(rh * ratio);
                    }
                    else
                    {
                        shape.TableRowHeights.AddRange(rawRowHeights);
                    }
                }

                return shape;
            }

            // Check Chart (<c:chart>)
            XmlNode chartNode = gfNode.SelectSingleNode(".//*[local-name()='chart']");
            if (chartNode != null && archive != null && relEntry != null)
            {
                string rId = chartNode.Attributes["r:id"] != null ? chartNode.Attributes["r:id"].Value : "";
                if (string.IsNullOrEmpty(rId))
                {
                    foreach (XmlAttribute a in chartNode.Attributes)
                    {
                        if (a.LocalName == "id" && a.Value.StartsWith("rId")) { rId = a.Value; break; }
                    }
                }
                if (!string.IsNullOrEmpty(rId))
                {
                    try
                    {
                        XmlDocument rDoc = new XmlDocument();
                        using (Stream rs = relEntry.Open()) rDoc.Load(rs);
                        XmlNode relNode = rDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                        if (relNode != null && relNode.Attributes["Target"] != null)
                        {
                            string target = relNode.Attributes["Target"].Value.Replace('\\', '/');
                            if (target.StartsWith("../")) target = "ppt/" + target.Substring(3);
                            else if (!target.StartsWith("ppt/")) target = "ppt/slides/" + target;

                            DocChartData cData = ParseOpenXmlChart(archive, target);
                            if (cData != null)
                            {
                                shape.Type = ShapeType.Chart;
                                shape.ChartData = cData;
                                if (string.IsNullOrEmpty(shape.Name) || shape.Name == "Bảng")
                                    shape.Name = !string.IsNullOrEmpty(cData.Title) ? cData.Title : "Biểu đồ";
                                return shape;
                            }
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        private static DocChartData ParseOpenXmlChart(ZipArchive archive, string chartPath)
        {
            if (archive == null || string.IsNullOrEmpty(chartPath)) return null;
            ZipArchiveEntry entry = archive.GetEntry(chartPath);
            if (entry == null) return null;

            try
            {
                XmlDocument doc = new XmlDocument();
                using (Stream s = entry.Open()) doc.Load(s);

                DocChartData data = new DocChartData();
                data.Categories = new List<string>();
                data.SeriesNames = new List<string>();
                data.Values = new List<List<double>>();
                data.ShowTitle = true;
                data.ShowLegend = true;

                // Title
                XmlNode titleNode = doc.SelectSingleNode("//*[local-name()='chart']/*[local-name()='title']");
                if (titleNode != null)
                {
                    XmlNode t = titleNode.SelectSingleNode(".//*[local-name()='t']");
                    if (t != null && !string.IsNullOrEmpty(t.InnerText))
                    {
                        data.Title = t.InnerText;
                    }
                }

                XmlNode plotArea = doc.SelectSingleNode("//*[local-name()='plotArea']");
                if (plotArea == null) return null;

                XmlNode chartTypeNode = null;
                string detectedType = "Column_Clustered";

                foreach (XmlNode child in plotArea.ChildNodes)
                {
                    string ln = child.LocalName;
                    if (ln.EndsWith("Chart", StringComparison.OrdinalIgnoreCase))
                    {
                        chartTypeNode = child;
                        if (ln.Equals("barChart", StringComparison.OrdinalIgnoreCase))
                        {
                            XmlNode barDir = child.SelectSingleNode(".//*[local-name()='barDir']");
                            string dir = (barDir != null && barDir.Attributes["val"] != null) ? barDir.Attributes["val"].Value : "col";
                            detectedType = (dir == "bar") ? "Bar_Clustered" : "Column_Clustered";
                        }
                        else if (ln.Equals("lineChart", StringComparison.OrdinalIgnoreCase)) detectedType = "Line";
                        else if (ln.Equals("pieChart", StringComparison.OrdinalIgnoreCase)) detectedType = "Pie";
                        else if (ln.Equals("doughnutChart", StringComparison.OrdinalIgnoreCase)) detectedType = "Doughnut";
                        else if (ln.Equals("areaChart", StringComparison.OrdinalIgnoreCase)) detectedType = "Area";
                        else if (ln.Equals("scatterChart", StringComparison.OrdinalIgnoreCase)) detectedType = "Scatter";
                        else if (ln.Equals("radarChart", StringComparison.OrdinalIgnoreCase)) detectedType = "Radar";
                        break;
                    }
                }

                if (chartTypeNode == null) return null;
                data.ChartType = detectedType;

                XmlNodeList serNodes = chartTypeNode.SelectNodes("./*[local-name()='ser']");
                if (serNodes == null || serNodes.Count == 0)
                {
                    serNodes = chartTypeNode.SelectNodes(".//*[local-name()='ser']");
                }
                if (serNodes == null || serNodes.Count == 0) return null;

                List<List<double>> seriesValuesList = new List<List<double>>();

                foreach (XmlNode ser in serNodes)
                {
                    string serName = "Series " + (data.SeriesNames.Count + 1);
                    XmlNode txV = ser.SelectSingleNode(".//*[local-name()='tx']//*[local-name()='v']");
                    if (txV != null && !string.IsNullOrEmpty(txV.InnerText)) serName = txV.InnerText;
                    data.SeriesNames.Add(serName);

                    if (data.Categories.Count == 0)
                    {
                        XmlNodeList catPts = ser.SelectNodes(".//*[local-name()='cat']//*[local-name()='pt']");
                        if (catPts != null && catPts.Count > 0)
                        {
                            SortedDictionary<int, string> catMap = new SortedDictionary<int, string>();
                            foreach (XmlNode pt in catPts)
                            {
                                int idx = 0;
                                if (pt.Attributes["idx"] != null && int.TryParse(pt.Attributes["idx"].Value, out idx))
                                {
                                    XmlNode v = pt.SelectSingleNode(".//*[local-name()='v']");
                                    catMap[idx] = (v != null) ? v.InnerText : "";
                                }
                            }
                            foreach (var kvp in catMap) data.Categories.Add(kvp.Value);
                        }
                    }

                    SortedDictionary<int, double> valMap = new SortedDictionary<int, double>();
                    XmlNodeList valPts = ser.SelectNodes(".//*[local-name()='val']//*[local-name()='pt']");
                    if (valPts != null && valPts.Count > 0)
                    {
                        foreach (XmlNode pt in valPts)
                        {
                            int idx = 0;
                            if (pt.Attributes["idx"] != null && int.TryParse(pt.Attributes["idx"].Value, out idx))
                            {
                                XmlNode v = pt.SelectSingleNode(".//*[local-name()='v']");
                                double numVal = 0.0;
                                if (v != null && double.TryParse(v.InnerText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numVal))
                                {
                                    valMap[idx] = numVal;
                                }
                                else
                                {
                                    valMap[idx] = 0.0;
                                }
                            }
                        }
                    }
                    List<double> sVals = new List<double>();
                    foreach (var kvp in valMap) sVals.Add(kvp.Value);
                    seriesValuesList.Add(sVals);
                }

                int maxCount = 0;
                foreach (var sv in seriesValuesList) if (sv.Count > maxCount) maxCount = sv.Count;
                while (data.Categories.Count < maxCount)
                {
                    data.Categories.Add("Mục " + (data.Categories.Count + 1));
                }

                for (int c = 0; c < data.Categories.Count; c++)
                {
                    List<double> rowVals = new List<double>();
                    for (int s = 0; s < data.SeriesNames.Count; s++)
                    {
                        if (s < seriesValuesList.Count && c < seriesValuesList[s].Count)
                            rowVals.Add(seriesValuesList[s][c]);
                        else
                            rowVals.Add(0.0);
                    }
                    data.Values.Add(rowVals);
                }

                data.SeriesVisible = new List<bool>();
                for (int s = 0; s < data.SeriesNames.Count; s++) data.SeriesVisible.Add(true);
                data.CategoriesVisible = new List<bool>();
                for (int c = 0; c < data.Categories.Count; c++) data.CategoriesVisible.Add(true);

                return data;
            }
            catch { }
            return null;
        }
        #endregion

        private static void ParseSlideTransition(XmlDocument doc, SlideNode slide)
        {
            slide.Transition = TransitionType.None;

            XmlNode morphNode = doc.SelectSingleNode("//*[local-name()='morph']");
            if (morphNode != null)
            {
                slide.Transition = TransitionType.Morph;
                string m = "";
                if (morphNode.Attributes != null)
                {
                    foreach (XmlAttribute attr in morphNode.Attributes)
                    {
                        if (attr.LocalName.Equals("option", StringComparison.OrdinalIgnoreCase) ||
                            attr.LocalName.Equals("morphMode", StringComparison.OrdinalIgnoreCase) ||
                            attr.Name.EndsWith("morphMode", StringComparison.OrdinalIgnoreCase))
                        {
                            m = attr.Value.ToLower();
                            break;
                        }
                    }
                }
                if (m.Contains("word")) slide.MorphMode = MorphType.Words;
                else if (m.Contains("char")) slide.MorphMode = MorphType.Characters;
                else slide.MorphMode = MorphType.Objects;
                return;
            }

            XmlNode transNode = doc.SelectSingleNode("//*[local-name()='transition']");
            if (transNode != null)
            {
                if (transNode.SelectSingleNode(".//*[local-name()='fade']") != null) slide.Transition = TransitionType.Fade;
                else if (transNode.SelectSingleNode(".//*[local-name()='push']") != null) slide.Transition = TransitionType.Push;
                else if (transNode.SelectSingleNode(".//*[local-name()='wipe']") != null) slide.Transition = TransitionType.Wipe;
                else if (transNode.SelectSingleNode(".//*[local-name()='split']") != null) slide.Transition = TransitionType.Split;
                else if (transNode.SelectSingleNode(".//*[local-name()='reveal']") != null) slide.Transition = TransitionType.Reveal;
                else if (transNode.SelectSingleNode(".//*[local-name()='wheel']") != null) slide.Transition = TransitionType.Wheel;
                else if (transNode.SelectSingleNode(".//*[local-name()='randomBar']") != null) slide.Transition = TransitionType.RandomBars;
                else if (transNode.SelectSingleNode(".//*[local-name()='zoom']") != null) slide.Transition = TransitionType.Zoom;
                else if (transNode.SelectSingleNode(".//*[local-name()='cut']") != null) slide.Transition = TransitionType.Cut;
                else if (transNode.SelectSingleNode(".//*[local-name()='circle']") != null) slide.Transition = TransitionType.Shape;
                else if (transNode.SelectSingleNode(".//*[local-name()='cover']") != null) slide.Transition = TransitionType.Cover;
                else if (transNode.SelectSingleNode(".//*[local-name()='uncover']") != null) slide.Transition = TransitionType.Uncover;
                else if (transNode.SelectSingleNode(".//*[local-name()='dissolve']") != null) slide.Transition = TransitionType.Dissolve;
                else if (transNode.SelectSingleNode(".//*[local-name()='checker']") != null) slide.Transition = TransitionType.Checker;
                else if (transNode.SelectSingleNode(".//*[local-name()='blinds']") != null) slide.Transition = TransitionType.Blinds;
                else if (transNode.SelectSingleNode(".//*[local-name()='comb']") != null) slide.Transition = TransitionType.Comb;
                else if (transNode.SelectSingleNode(".//*[local-name()='flash']") != null) slide.Transition = TransitionType.Flash;
                else if (transNode.SelectSingleNode(".//*[local-name()='curtains']") != null) slide.Transition = TransitionType.Curtains;
                else if (transNode.SelectSingleNode(".//*[local-name()='wind']") != null) slide.Transition = TransitionType.Wind;
                else if (transNode.SelectSingleNode(".//*[local-name()='prestige']") != null) slide.Transition = TransitionType.Prestige;
                else if (transNode.SelectSingleNode(".//*[local-name()='fracture']") != null) slide.Transition = TransitionType.Fracture;
                else if (transNode.SelectSingleNode(".//*[local-name()='crush']") != null) slide.Transition = TransitionType.Crush;
                else if (transNode.SelectSingleNode(".//*[local-name()='peel']") != null) slide.Transition = TransitionType.PeelOff;
                else if (transNode.SelectSingleNode(".//*[local-name()='pageCurl']") != null) slide.Transition = TransitionType.PageCurl;
                else if (transNode.SelectSingleNode(".//*[local-name()='airplane']") != null) slide.Transition = TransitionType.Airplane;
                else if (transNode.SelectSingleNode(".//*[local-name()='origami']") != null) slide.Transition = TransitionType.Origami;
                else if (transNode.SelectSingleNode(".//*[local-name()='ripple']") != null) slide.Transition = TransitionType.Ripple;
                else if (transNode.SelectSingleNode(".//*[local-name()='honeycomb']") != null) slide.Transition = TransitionType.Honeycomb;
                else if (transNode.SelectSingleNode(".//*[local-name()='glitter']") != null) slide.Transition = TransitionType.Glitter;
                else if (transNode.SelectSingleNode(".//*[local-name()='vortex']") != null) slide.Transition = TransitionType.Vortex;
                else if (transNode.SelectSingleNode(".//*[local-name()='shred']") != null) slide.Transition = TransitionType.Shred;
                else if (transNode.SelectSingleNode(".//*[local-name()='switch']") != null) slide.Transition = TransitionType.Switch;
                else if (transNode.SelectSingleNode(".//*[local-name()='flip']") != null) slide.Transition = TransitionType.Flip;
                else if (transNode.SelectSingleNode(".//*[local-name()='gallery']") != null) slide.Transition = TransitionType.Gallery;
                else if (transNode.SelectSingleNode(".//*[local-name()='cube']") != null) slide.Transition = TransitionType.Cube;
                else if (transNode.SelectSingleNode(".//*[local-name()='doors']") != null) slide.Transition = TransitionType.Doors;
                else if (transNode.SelectSingleNode(".//*[local-name()='box']") != null) slide.Transition = TransitionType.Box;
                else if (transNode.SelectSingleNode(".//*[local-name()='pan']") != null) slide.Transition = TransitionType.Pan;
                else if (transNode.SelectSingleNode(".//*[local-name()='ferris']") != null) slide.Transition = TransitionType.FerrisWheel;
                else if (transNode.SelectSingleNode(".//*[local-name()='conveyor']") != null) slide.Transition = TransitionType.Conveyor;
                else if (transNode.SelectSingleNode(".//*[local-name()='rotate']") != null) slide.Transition = TransitionType.Rotate;
                else if (transNode.SelectSingleNode(".//*[local-name()='window']") != null) slide.Transition = TransitionType.Window;
                else if (transNode.SelectSingleNode(".//*[local-name()='orbit']") != null) slide.Transition = TransitionType.Orbit;
                else if (transNode.SelectSingleNode(".//*[local-name()='flythrough']") != null) slide.Transition = TransitionType.FlyThrough;
                else slide.Transition = TransitionType.None;
            }
        }

        private static void ParseSlideAnimations(XmlDocument doc, SlideNode slide)
        {
            if (doc == null || slide == null) return;
            slide.Animations.Clear();

            XmlNode timingNode = doc.SelectSingleNode("//*[local-name()='timing']");
            if (timingNode == null) return;

            XmlNodeList cTnNodes = timingNode.SelectNodes(".//*[local-name()='cTn'][@presetClass or @presetID]");

            if (cTnNodes != null && cTnNodes.Count > 0)
            {
                foreach (XmlNode cTn in cTnNodes)
                {
                    string pClass = (cTn.Attributes["presetClass"] != null) ? cTn.Attributes["presetClass"].Value.ToLower() : "entr";
                    string pIdStr = (cTn.Attributes["presetID"] != null) ? cTn.Attributes["presetID"].Value : "1";
                    int pId = 1;
                    int.TryParse(pIdStr, out pId);

                    string nodeType = (cTn.Attributes["nodeType"] != null) ? cTn.Attributes["nodeType"].Value : "";

                    float dur = 0.5f;
                    if (cTn.Attributes["dur"] != null)
                    {
                        string durStr = cTn.Attributes["dur"].Value;
                        if (durStr != "indefinite")
                        {
                            long durMs;
                            if (long.TryParse(durStr, out durMs) && durMs > 0) dur = durMs / 1000.0f;
                        }
                    }

                    float delay = 0.0f;
                    XmlNode condNode = cTn.SelectSingleNode(".//*[local-name()='cond']");
                    if (condNode != null && condNode.Attributes["delay"] != null)
                    {
                        long delMs;
                        if (long.TryParse(condNode.Attributes["delay"].Value, out delMs) && delMs > 0) delay = delMs / 1000.0f;
                    }

                    XmlNode spTgt = cTn.SelectSingleNode(".//*[local-name()='spTgt']");
                    string spid = (spTgt != null && spTgt.Attributes["spid"] != null) ? spTgt.Attributes["spid"].Value : "";

                    string targetShapeId = "";
                    if (!string.IsNullOrEmpty(spid))
                    {
                        foreach (ShapeNode sh in slide.Shapes)
                        {
                            if (sh.Id == spid || sh.Name == spid || sh.Name == "Shape " + spid || (sh.Name != null && sh.Name.EndsWith(" " + spid)))
                            {
                                targetShapeId = sh.Id;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(targetShapeId))
                    {
                        continue;
                    }

                    bool isMediaTargetShape = false;
                    foreach (ShapeNode sh in slide.Shapes)
                    {
                        if (sh.Id == targetShapeId)
                        {
                            if (sh.Type == ShapeType.Video || sh.Type == ShapeType.Audio || !string.IsNullOrEmpty(sh.VideoPath) || !string.IsNullOrEmpty(sh.AudioPath))
                            {
                                isMediaTargetShape = true;
                            }
                            if (pClass == "mediacall")
                            {
                                XmlNode durNode = cTn.SelectSingleNode(".//*[local-name()='cBhvr']/*[local-name()='cTn'][@dur]");
                                if (durNode != null && durNode.Attributes["dur"] != null)
                                {
                                    long durVal;
                                    if (long.TryParse(durNode.Attributes["dur"].Value, out durVal) && durVal > 0)
                                    {
                                        float tSec = durVal / 1000.0f;
                                        if (sh.Type == ShapeType.Video) sh.VideoTotalTime = tSec;
                                        else if (sh.Type == ShapeType.Audio) sh.AudioTotalTime = tSec;
                                    }
                                }
                                if (nodeType == "clickEffect")
                                {
                                    sh.VideoStartMode = VideoStartMode.InClickSequence;
                                    sh.AudioStartMode = VideoStartMode.InClickSequence;
                                }
                                else if (nodeType.IndexOf("with", StringComparison.OrdinalIgnoreCase) >= 0 || nodeType.IndexOf("after", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    sh.VideoStartMode = VideoStartMode.Automatically;
                                    sh.AudioStartMode = VideoStartMode.Automatically;
                                }
                            }
                            break;
                        }
                    }

                    if (pClass == "mediacall" || isMediaTargetShape)
                    {
                        continue;
                    }

                    AnimationType aType = MapOpenXmlPresetToAnimationType(pClass, pId, cTn);
                    AnimationTrigger trigger = AnimationTrigger.OnClick;
                    if (nodeType.IndexOf("with", StringComparison.OrdinalIgnoreCase) >= 0) trigger = AnimationTrigger.WithPrevious;
                    else if (nodeType.IndexOf("after", StringComparison.OrdinalIgnoreCase) >= 0) trigger = AnimationTrigger.AfterPrevious;
                    else if (slide.Animations.Count > 0 && delay > 0 && trigger == AnimationTrigger.OnClick) trigger = AnimationTrigger.AfterPrevious;

                    ShapeAnimation anim = new ShapeAnimation
                    {
                        ShapeId = targetShapeId,
                        Type = aType,
                        Trigger = trigger,
                        Duration = Math.Max(0.1f, dur),
                        Delay = delay,
                        OrderIndex = slide.Animations.Count
                    };
                    slide.Animations.Add(anim);
                }
            }
            else
            {
                XmlNodeList animEffectNodes = timingNode.SelectNodes(".//*[local-name()='animEffect' or local-name()='animMotion' or local-name()='animScale' or local-name()='animRot' or local-name()='set' or local-name()='anim']");
                if (animEffectNodes != null && animEffectNodes.Count > 0)
                {
                    foreach (XmlNode aNode in animEffectNodes)
                    {
                        string filter = (aNode.Attributes["filter"] != null) ? aNode.Attributes["filter"].Value.ToLower() : "";
                        string trans = (aNode.Attributes["transition"] != null) ? aNode.Attributes["transition"].Value.ToLower() : "in";

                        XmlNode spTgt = aNode.SelectSingleNode(".//*[local-name()='spTgt']");
                        if (spTgt == null && aNode.ParentNode != null) spTgt = aNode.ParentNode.SelectSingleNode(".//*[local-name()='spTgt']");
                        string spid = (spTgt != null && spTgt.Attributes["spid"] != null) ? spTgt.Attributes["spid"].Value : "";

                        string targetShapeId = "";
                        bool isMediaTargetShape = false;
                        if (!string.IsNullOrEmpty(spid))
                        {
                            foreach (ShapeNode sh in slide.Shapes)
                            {
                                if (sh.Id == spid || sh.Name == spid || sh.Name == "Shape " + spid || (sh.Name != null && sh.Name.EndsWith(" " + spid)))
                                {
                                    targetShapeId = sh.Id;
                                    if (sh.Type == ShapeType.Video || sh.Type == ShapeType.Audio || !string.IsNullOrEmpty(sh.VideoPath) || !string.IsNullOrEmpty(sh.AudioPath))
                                    {
                                        isMediaTargetShape = true;
                                    }
                                    break;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(targetShapeId) || isMediaTargetShape)
                        {
                            continue;
                        }

                        AnimationType aType = AnimationType.FadeIn;
                        if (filter.Contains("fade")) aType = (trans == "out") ? AnimationType.FadeOut : AnimationType.FadeIn;
                        else if (filter.Contains("fly") || filter.Contains("move")) aType = (trans == "out") ? AnimationType.FlyOut : AnimationType.FlyIn;
                        else if (filter.Contains("zoom")) aType = (trans == "out") ? AnimationType.ZoomOut : AnimationType.ZoomIn;
                        else if (filter.Contains("wipe")) aType = (trans == "out") ? AnimationType.WipeOut : AnimationType.WipeIn;
                        else if (filter.Contains("split")) aType = (trans == "out") ? AnimationType.SplitOut : AnimationType.SplitIn;
                        else if (filter.Contains("wheel")) aType = (trans == "out") ? AnimationType.WheelOut : AnimationType.WheelIn;
                        else if (filter.Contains("bounce")) aType = (trans == "out") ? AnimationType.BounceOut : AnimationType.Bounce;
                        else if (filter.Contains("swivel")) aType = (trans == "out") ? AnimationType.SwivelOut : AnimationType.Swivel;
                        else if (filter.Contains("randombar")) aType = (trans == "out") ? AnimationType.RandomBarsOut : AnimationType.RandomBarsIn;
                        else if (aNode.LocalName == "animMotion") aType = AnimationType.MotionLine;
                        else if (aNode.LocalName == "animRot") aType = AnimationType.Spin;
                        else if (aNode.LocalName == "animScale") aType = AnimationType.GrowShrink;

                        slide.Animations.Add(new ShapeAnimation
                        {
                            ShapeId = targetShapeId,
                            Type = aType,
                            Trigger = (slide.Animations.Count == 0) ? AnimationTrigger.OnClick : AnimationTrigger.AfterPrevious,
                            Duration = 0.6f,
                            OrderIndex = slide.Animations.Count
                        });
                    }
                }
            }

            // 3. Parse <p:video> and <p:audio> media settings (<p:cMediaNode>)
            try
            {
                XmlNodeList cMediaNodes = timingNode.SelectNodes(".//*[local-name()='video' or local-name()='audio']/*[local-name()='cMediaNode']");
                if (cMediaNodes != null)
                {
                    foreach (XmlNode mNode in cMediaNodes)
                    {
                        XmlNode spT = mNode.SelectSingleNode(".//*[local-name()='spTgt']");
                        string spid = (spT != null && spT.Attributes["spid"] != null) ? spT.Attributes["spid"].Value : "";
                        if (!string.IsNullOrEmpty(spid))
                        {
                            foreach (ShapeNode mShape in slide.Shapes)
                            {
                                if (mShape.Id == spid || mShape.Name == spid || mShape.Name == "Shape " + spid)
                                {
                                    if (mNode.Attributes["vol"] != null)
                                    {
                                        int v;
                                        if (int.TryParse(mNode.Attributes["vol"].Value, out v))
                                        {
                                            VideoVolumeLevel vol = (v <= 0) ? VideoVolumeLevel.Mute : (v <= 33333) ? VideoVolumeLevel.Low : (v <= 66666) ? VideoVolumeLevel.Medium : VideoVolumeLevel.High;
                                            mShape.VideoVolume = vol;
                                            mShape.AudioVolume = vol;
                                        }
                                    }
                                    if (mNode.Attributes["fullScrn"] != null && (mNode.Attributes["fullScrn"].Value == "1" || mNode.Attributes["fullScrn"].Value.Equals("true", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        mShape.VideoPlayFullScreen = true;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static AnimationType MapOpenXmlPresetToAnimationType(string pClass, int pId, XmlNode cTn)
        {
            if (pClass == "entr")
            {
                switch (pId)
                {
                    case 1: return AnimationType.Appear;
                    case 2: return AnimationType.FlyIn;
                    case 3: return AnimationType.FloatIn;
                    case 9: return AnimationType.SplitIn;
                    case 10: return AnimationType.FadeIn;
                    case 13: return AnimationType.RandomBarsIn;
                    case 15: return AnimationType.WheelIn;
                    case 16: return AnimationType.WipeIn;
                    case 18: return AnimationType.ZoomIn;
                    case 20: return AnimationType.Swivel;
                    case 21: return AnimationType.Bounce;
                    case 22: return AnimationType.GrowAndTurn;
                    default: return AnimationType.FadeIn;
                }
            }
            else if (pClass == "emph")
            {
                switch (pId)
                {
                    case 23: return AnimationType.Spin;
                    case 24: return AnimationType.GrowShrink;
                    case 25: return AnimationType.Transparency;
                    case 26: return AnimationType.Teeter;
                    case 27: return AnimationType.ColorPulse;
                    case 28: return AnimationType.Pulse;
                    case 29: return AnimationType.Desaturate;
                    case 30: return AnimationType.Darken;
                    case 31: return AnimationType.Lighten;
                    case 32: return AnimationType.ObjectColor;
                    case 33: return AnimationType.ComplementaryColor;
                    case 34: return AnimationType.LineColor;
                    case 35: return AnimationType.FillColor;
                    case 36: return AnimationType.BrushColor;
                    case 37: return AnimationType.FontColor;
                    case 38: return AnimationType.Underline;
                    case 39: return AnimationType.BoldFlash;
                    case 40: return AnimationType.BoldReveal;
                    case 41: return AnimationType.Wave;
                    default: return AnimationType.Pulse;
                }
            }
            else if (pClass == "exit")
            {
                switch (pId)
                {
                    case 1: return AnimationType.Disappear;
                    case 2: return AnimationType.FlyOut;
                    case 3: return AnimationType.FloatOut;
                    case 9: return AnimationType.SplitOut;
                    case 10: return AnimationType.FadeOut;
                    case 13: return AnimationType.RandomBarsOut;
                    case 15: return AnimationType.WheelOut;
                    case 16: return AnimationType.WipeOut;
                    case 18: return AnimationType.ZoomOut;
                    case 20: return AnimationType.SwivelOut;
                    case 21: return AnimationType.BounceOut;
                    case 22: return AnimationType.ShrinkAndTurn;
                    default: return AnimationType.FadeOut;
                }
            }
            else if (pClass == "path")
            {
                switch (pId)
                {
                    case 1: return AnimationType.MotionLine;
                    case 2: return AnimationType.MotionArc;
                    case 3: return AnimationType.MotionTurn;
                    case 4: return AnimationType.MotionShape;
                    case 5: return AnimationType.MotionLoop;
                    default: return AnimationType.MotionCustom;
                }
            }
            return AnimationType.FadeIn;
        }

        private static int ExtractSlideNum(string name)
        {
            try
            {
                string fn = System.IO.Path.GetFileNameWithoutExtension(name);
                string numStr = fn.Replace("slide", "").Replace("Slide", "");
                int n;
                if (int.TryParse(numStr, out n)) return n;
            }
            catch { }
            return 0;
        }

        public static bool SavePptxXml(string filepath, PresentationRoot pres)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<p:presentation xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\">");
                    sw.WriteLine(string.Format("  <p:sldSz cx=\"{0}\" cy=\"{1}\"/>", (int)pres.CanvasWidth, (int)pres.CanvasHeight));
                    sw.WriteLine("  <p:sldIdLst>");

                    for (int i = 0; i < pres.Slides.Count; i++)
                    {
                        SlideNode slide = pres.Slides[i];
                        sw.WriteLine(string.Format("    <p:sldId id=\"{0}\" title=\"{1}\" transition=\"{2}\" notes=\"{3}\">", 256 + i, EscapeXml(slide.Title), slide.Transition.ToString(), EscapeXml(slide.SpeakerNotes ?? "")));
                        sw.WriteLine("      <p:spTree>");

                        foreach (ShapeNode shape in slide.Shapes)
                        {
                            sw.WriteLine("        <p:sp>");
                            sw.WriteLine("          <p:spPr>");
                            sw.WriteLine("            <a:xfrm>");
                            sw.WriteLine(string.Format("              <a:off x=\"{0}\" y=\"{1}\"/>", (int)shape.Transform.X, (int)shape.Transform.Y));
                            sw.WriteLine(string.Format("              <a:ext cx=\"{0}\" cy=\"{1}\"/>", (int)shape.Transform.Width, (int)shape.Transform.Height));
                            if (Math.Abs(shape.Transform.Rotation) > 0.01f)
                            {
                                sw.WriteLine(string.Format("              <a:rot rot=\"{0}\"/>", (int)(shape.Transform.Rotation * 60000)));
                            }
                            sw.WriteLine("            </a:xfrm>");
                            sw.WriteLine(string.Format("            <a:prstGeom prst=\"{0}\"/>", shape.Type.ToString().ToLower()));
                            sw.WriteLine(string.Format("            <a:solidFill color=\"#{0:X6}\"/>", shape.FillColor & 0xFFFFFF));
                            sw.WriteLine(string.Format("            <a:ln width=\"{0}\" color=\"#{1:X6}\"/>", (int)shape.StrokeWidth, shape.StrokeColor & 0xFFFFFF));
                            sw.WriteLine("          </p:spPr>");
                            sw.WriteLine("          <p:txBody>");
                            sw.WriteLine(string.Format("            <a:p><a:r><a:t font=\"{0}\" size=\"{1}\">{2}</a:t></a:r></a:p>",
                                EscapeXml(shape.FontName),
                                (int)shape.FontSize,
                                EscapeXml(shape.Text)));
                            sw.WriteLine("          </p:txBody>");
                            sw.WriteLine("        </p:sp>");
                        }

                        sw.WriteLine("      </p:spTree>");
                        sw.WriteLine("    </p:sldId>");
                    }

                    sw.WriteLine("  </p:sldIdLst>");
                    sw.WriteLine("</p:presentation>");
                }
                return true;
            }
            catch { return false; }
        }

        public static bool LoadPptxXml(string filepath, out PresentationRoot pres)
        {
            pres = new PresentationRoot();
            try
            {
                if (!File.Exists(filepath)) return false;

                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);

                XmlNode sldSzNode = doc.SelectSingleNode("//*[local-name()='sldSz']");
                if (sldSzNode != null && sldSzNode.Attributes["cx"] != null)
                {
                    float cx, cy;
                    if (float.TryParse(sldSzNode.Attributes["cx"].Value, out cx)) pres.CanvasWidth = cx;
                    if (float.TryParse(sldSzNode.Attributes["cy"].Value, out cy)) pres.CanvasHeight = cy;
                }

                XmlNodeList sldNodes = doc.SelectNodes("//*[local-name()='sldId']");
                if (sldNodes != null && sldNodes.Count > 0)
                {
                    pres.Slides.Clear();
                    foreach (XmlNode sldNode in sldNodes)
                    {
                        SlideNode slide = new SlideNode();
                        if (sldNode.Attributes["title"] != null)
                        {
                            slide.Title = sldNode.Attributes["title"].Value;
                        }
                        if (sldNode.Attributes["transition"] != null)
                        {
                            string tStr = sldNode.Attributes["transition"].Value;
                            try { slide.Transition = (TransitionType)Enum.Parse(typeof(TransitionType), tStr, true); }
                            catch { }
                        }
                        if (sldNode.Attributes["notes"] != null)
                        {
                            slide.SpeakerNotes = sldNode.Attributes["notes"].Value;
                        }

                        XmlNodeList spNodes = sldNode.SelectNodes(".//*[local-name()='sp']");
                        if (spNodes != null)
                        {
                            foreach (XmlNode spNode in spNodes)
                            {
                                ShapeNode shape = new ShapeNode();
                                XmlNode offNode = spNode.SelectSingleNode(".//*[local-name()='off']");
                                XmlNode extNode = spNode.SelectSingleNode(".//*[local-name()='ext']");
                                XmlNode tNode = spNode.SelectSingleNode(".//*[local-name()='t']");

                                if (offNode != null && offNode.Attributes["x"] != null)
                                {
                                    float x, y;
                                    float.TryParse(offNode.Attributes["x"].Value, out x);
                                    float.TryParse(offNode.Attributes["y"].Value, out y);
                                    shape.Transform.X = x;
                                    shape.Transform.Y = y;
                                }

                                if (extNode != null && extNode.Attributes["cx"] != null)
                                {
                                    float cx, cy;
                                    float.TryParse(extNode.Attributes["cx"].Value, out cx);
                                    float.TryParse(extNode.Attributes["cy"].Value, out cy);
                                    shape.Transform.Width = cx;
                                    shape.Transform.Height = cy;
                                }

                                if (tNode != null)
                                {
                                    shape.Text = tNode.InnerText;
                                }

                                slide.AddShape(shape);
                            }
                        }

                        pres.Slides.Add(slide);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EscapeXml(string input)
        {
            return (input ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
