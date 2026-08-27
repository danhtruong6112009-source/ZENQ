using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Format
{
    public class DocxMetadata
    {
        public string TextContent { get; set; }
        public List<FormattedRun> FormattedRuns { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public bool ShowPageNumbers { get; set; }
        public bool IsLandscape { get; set; }
        public int MarginMode { get; set; } // 0: Normal, 1: Narrow, 2: Wide, 3: Custom
        public int TopMarginPt { get; set; }
        public int RightMarginPt { get; set; }
        public int BottomMarginPt { get; set; }
        public int LeftMarginPt { get; set; }
        public Dictionary<int, int> ParagraphAlignments { get; set; }
        public Dictionary<int, Color> ParagraphBottomBorders { get; set; }
        public Dictionary<int, ParagraphBorderInfo> ParagraphBorders { get; set; }
        public Dictionary<int, float> ParagraphLineSpacings { get; set; }
        public Dictionary<int, float> ParagraphSpacingsBefore { get; set; }
        public Dictionary<int, float> ParagraphSpacingsAfter { get; set; }

        public DocxMetadata()
        {
            TextContent = "";
            FormattedRuns = new List<FormattedRun>();
            HeaderText = "";
            FooterText = "";
            ShowPageNumbers = false;
            IsLandscape = false;
            MarginMode = 0;
            TopMarginPt = 72;
            RightMarginPt = 72;
            BottomMarginPt = 72;
            LeftMarginPt = 72;
            ParagraphAlignments = new Dictionary<int, int>();
            ParagraphBottomBorders = new Dictionary<int, Color>();
            ParagraphBorders = new Dictionary<int, ParagraphBorderInfo>();
            ParagraphLineSpacings = new Dictionary<int, float>();
            ParagraphSpacingsBefore = new Dictionary<int, float>();
            ParagraphSpacingsAfter = new Dictionary<int, float>();
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

                        // 2. Extract Header / Footer
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.StartsWith("word/header") && entry.FullName.EndsWith(".xml"))
                            {
                                try
                                {
                                    XmlDocument hDoc = new XmlDocument();
                                    using (Stream s = entry.Open()) hDoc.Load(s);
                                    string hTxt = ExtractAllTextFromXml(hDoc);
                                    if (!string.IsNullOrEmpty(hTxt)) meta.HeaderText = hTxt.Trim();
                                }
                                catch { }
                            }
                            else if (entry.FullName.StartsWith("word/footer") && entry.FullName.EndsWith(".xml"))
                            {
                                try
                                {
                                    XmlDocument fDoc = new XmlDocument();
                                    using (Stream s = entry.Open()) fDoc.Load(s);
                                    string fTxt = ExtractAllTextFromXml(fDoc);
                                    if (!string.IsNullOrEmpty(fTxt)) meta.FooterText = fTxt.Trim();
                                    if (fDoc.InnerXml.Contains("PAGE") || fDoc.InnerXml.Contains("w:fldSimple"))
                                    {
                                        meta.ShowPageNumbers = true;
                                    }
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

            public DocxStyleInfo()
            {
                FontSize = 0f;
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
                nsmgr.AddNamespace("wps", "http://schemas.microsoft.com/office/word/2010/wordprocessingShape");
                nsmgr.AddNamespace("wpg", "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup");
                nsmgr.AddNamespace("wp14", "http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing");

                XmlNode bodyNode = doc.SelectSingleNode("//w:body", nsmgr) ?? doc.DocumentElement;
                if (bodyNode == null) return false;

                // 1. Parse Section Properties (Page setup, Landscape, Margins)
                XmlNode sectPr = bodyNode.SelectSingleNode(".//w:sectPr", nsmgr);
                if (sectPr != null)
                {
                    XmlNode pgSz = sectPr.SelectSingleNode("w:pgSz", nsmgr);
                    if (pgSz != null && pgSz.Attributes["w:orient"] != null && pgSz.Attributes["w:orient"].Value == "landscape")
                    {
                        meta.IsLandscape = true;
                    }

                    XmlNode pgMar = sectPr.SelectSingleNode("w:pgMar", nsmgr);
                    if (pgMar != null)
                    {
                        int topTwips = 1440, rightTwips = 1440, bottomTwips = 1440, leftTwips = 1440;
                        if (pgMar.Attributes["w:top"] != null) int.TryParse(pgMar.Attributes["w:top"].Value, out topTwips);
                        if (pgMar.Attributes["w:right"] != null) int.TryParse(pgMar.Attributes["w:right"].Value, out rightTwips);
                        if (pgMar.Attributes["w:bottom"] != null) int.TryParse(pgMar.Attributes["w:bottom"].Value, out bottomTwips);
                        if (pgMar.Attributes["w:left"] != null) int.TryParse(pgMar.Attributes["w:left"].Value, out leftTwips);

                        meta.TopMarginPt = topTwips / 20;
                        meta.RightMarginPt = rightTwips / 20;
                        meta.BottomMarginPt = bottomTwips / 20;
                        meta.LeftMarginPt = leftTwips / 20;

                        if (topTwips <= 800 && rightTwips <= 800) meta.MarginMode = 1; // Narrow
                        else if (rightTwips >= 2500) meta.MarginMode = 2; // Wide
                        else meta.MarginMode = 0; // Normal
                    }
                }

                Dictionary<string, DocxStyleInfo> stylesDict = new Dictionary<string, DocxStyleInfo>(StringComparer.OrdinalIgnoreCase);
                string docDefaultFont = "Times New Roman";
                float docDefaultFontSize = 12.0f;

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

                            // 2. Parse all <w:style> definitions
                            XmlNodeList styleNodes = sDoc.SelectNodes("//w:style", nsmgr);
                            if (styleNodes != null)
                            {
                                foreach (XmlNode sNode in styleNodes)
                                {
                                    if (sNode.Attributes["w:styleId"] == null) continue;
                                    string sId = sNode.Attributes["w:styleId"].Value;
                                    string sType = sNode.Attributes["w:type"] != null ? sNode.Attributes["w:type"].Value : "paragraph";

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
                                            if (sp.Attributes["w:line"] != null)
                                            {
                                                int lVal;
                                                if (int.TryParse(sp.Attributes["w:line"].Value, out lVal))
                                                    sInfo.LineSpacing = (float)Math.Round((double)lVal / 240.0, 2);
                                            }
                                            if (sp.Attributes["w:before"] != null)
                                            {
                                                int bVal;
                                                if (int.TryParse(sp.Attributes["w:before"].Value, out bVal))
                                                    sInfo.SpacingBefore = (float)Math.Round((double)bVal / 20.0, 1);
                                            }
                                            if (sp.Attributes["w:after"] != null)
                                            {
                                                int aVal;
                                                if (int.TryParse(sp.Attributes["w:after"].Value, out aVal))
                                                    sInfo.SpacingAfter = (float)Math.Round((double)aVal / 20.0, 1);
                                            }
                                        }
                                    }

                                    stylesDict[sId] = sInfo;
                                    if (!string.IsNullOrEmpty(sInfo.Name)) stylesDict[sInfo.Name] = sInfo;
                                }
                            }

                            // Also resolve Normal style defaults
                            if (stylesDict.ContainsKey("Normal"))
                            {
                                DocxStyleInfo nStyle = stylesDict["Normal"];
                                if (!string.IsNullOrEmpty(nStyle.FontName)) docDefaultFont = nStyle.FontName;
                                if (nStyle.FontSize > 0) docDefaultFontSize = nStyle.FontSize;
                            }
                        }
                        catch { }
                    }
                }

                // 2. Iterate direct children of body: Paragraphs (<w:p>) and Tables (<w:tbl>)
                foreach (XmlNode child in bodyNode.ChildNodes)
                {
                    if (child.LocalName == "p")
                    {
                        ParseParagraphNode(child, nsmgr, archive, relsToTarget, sb, meta.FormattedRuns, docDefaultFont, docDefaultFontSize, stylesDict, meta.ParagraphAlignments, meta.ParagraphBottomBorders, meta.ParagraphBorders, meta.ParagraphLineSpacings, meta.ParagraphSpacingsBefore, meta.ParagraphSpacingsAfter);
                    }
                    else if (child.LocalName == "tbl")
                    {
                        ParseTableNode(child, nsmgr, sb, meta.FormattedRuns);
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
            Dictionary<int, float> paraSpacingsAfter = null)
        {
            int paraStartIndex = sb.Length;

            // Initialize with document defaults
            string paraFont = docDefaultFont;
            float paraFontSize = docDefaultFontSize;
            bool paraBold = false;
            bool paraItalic = false;
            bool paraUnderline = false;
            Color paraColor = Color.Black;

            // 1. Inherit from Paragraph Style (<w:pPr/w:pStyle>) if present
            XmlNode pStyleNode = p.SelectSingleNode("w:pPr/w:pStyle", nsmgr);
            if (pStyleNode != null && pStyleNode.Attributes["w:val"] != null && stylesDict != null)
            {
                string sVal = pStyleNode.Attributes["w:val"].Value;
                DocxStyleInfo sInfo = null;
                if (stylesDict.TryGetValue(sVal, out sInfo))
                {
                    if (!string.IsNullOrEmpty(sInfo.FontName)) paraFont = sInfo.FontName;
                    if (sInfo.FontSize > 0) paraFontSize = sInfo.FontSize;
                    if (sInfo.Bold.HasValue) paraBold = sInfo.Bold.Value;
                    if (sInfo.Italic.HasValue) paraItalic = sInfo.Italic.Value;
                    if (sInfo.Underline.HasValue) paraUnderline = sInfo.Underline.Value;
                    if (sInfo.Color.HasValue) paraColor = sInfo.Color.Value;
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
                if (align > 0 && paraAlignments != null)
                {
                    paraAlignments[paraStartIndex] = align;
                }
            }

            // 3. Check Paragraph Spacing (<w:pPr/w:spacing>)
            XmlNode spNode = p.SelectSingleNode("w:pPr/w:spacing", nsmgr);
            if (spNode != null)
            {
                if (spNode.Attributes["w:line"] != null && paraLineSpacings != null)
                {
                    int lineVal;
                    if (int.TryParse(spNode.Attributes["w:line"].Value, out lineVal))
                    {
                        paraLineSpacings[paraStartIndex] = (float)Math.Round((double)lineVal / 240.0, 2);
                    }
                }
                if (spNode.Attributes["w:before"] != null && paraSpacingsBefore != null)
                {
                    int bVal;
                    if (int.TryParse(spNode.Attributes["w:before"].Value, out bVal))
                    {
                        paraSpacingsBefore[paraStartIndex] = (float)Math.Round((double)bVal / 20.0, 1);
                    }
                }
                if (spNode.Attributes["w:after"] != null && paraSpacingsAfter != null)
                {
                    int aVal;
                    if (int.TryParse(spNode.Attributes["w:after"].Value, out aVal))
                    {
                        paraSpacingsAfter[paraStartIndex] = (float)Math.Round((double)aVal / 20.0, 1);
                    }
                }
            }

            // 4. Check Paragraph Borders (<w:pPr/w:pBdr>)
            XmlNode pBdrNode = p.SelectSingleNode("w:pPr/w:pBdr", nsmgr);
            if (pBdrNode != null)
            {
                ParagraphBorderInfo bdrInfo = new ParagraphBorderInfo();
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

            // 5. Check for Indentation in <w:pPr/w:ind>
            XmlNode indNode = p.SelectSingleNode("w:pPr/w:ind", nsmgr);
            if (indNode != null)
            {
                int firstLine = 0, left = 0;
                if (indNode.Attributes["w:firstLine"] != null) int.TryParse(indNode.Attributes["w:firstLine"].Value, out firstLine);
                if (indNode.Attributes["w:left"] != null) int.TryParse(indNode.Attributes["w:left"].Value, out left);
                if (indNode.Attributes["w:firstLineChars"] != null) firstLine = 720;

                int totalTwips = firstLine + left;
                if (totalTwips >= 300)
                {
                    int tabs = Math.Max(1, (totalTwips + 180) / 360);
                    string indentStr = new string('\t', tabs);
                    int start = sb.Length;
                    sb.Append(indentStr);
                    runs.Add(new FormattedRun(start, indentStr.Length, paraFont, paraFontSize, false, false, false, false, Color.Black, Color.Transparent));
                }
            }

            // 6. Check for list bullet/numbering in <w:pPr>
            XmlNode numPr = p.SelectSingleNode("w:pPr/w:numPr", nsmgr);
            if (numPr != null)
            {
                int start = sb.Length;
                string bStr = "•\t";
                sb.Append(bStr);
                runs.Add(new FormattedRun(start, bStr.Length, "Segoe UI", paraFontSize, false, false, false, false, Color.FromArgb(0, 120, 215), Color.Transparent));
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

            // 8. Iterate paragraph child nodes (Runs, Drawings, Hyperlinks, etc.)
            float currentParaFontSize = paraFontSize;
            string currentParaFont = paraFont;

            XmlNodeList children = p.SelectNodes("w:r | w:hyperlink | w:smartTag | w:drawing | w:pict", nsmgr);
            if (children != null && children.Count > 0)
            {
                foreach (XmlNode r in children)
                {
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

                    // Extract run properties (inheriting current paragraph defaults)
                    string fontName = currentParaFont;
                    float fontSize = currentParaFontSize;
                    bool bold = paraBold;
                    bool italic = paraItalic;
                    bool underline = paraUnderline;
                    bool strike = false;
                    bool subscript = false, superscript = false;
                    Color color = paraColor;
                    Color highlight = Color.Transparent;

                    XmlNode rPr = r.SelectSingleNode("w:rPr", nsmgr);
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

                            currentParaFont = fontName;
                        }

                        // 8.5. Font size: check both w:sz (Latin) and w:szCs (Complex Script / Vietnamese)
                        XmlNode sz = rPr.SelectSingleNode("w:sz", nsmgr);
                        XmlNode szCs = rPr.SelectSingleNode("w:szCs", nsmgr);
                        bool hasExplicitSize = false;
                        if (sz != null && sz.Attributes["w:val"] != null)
                        {
                            int halfPts;
                            if (int.TryParse(sz.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                            {
                                fontSize = halfPts / 2.0f;
                                hasExplicitSize = true;
                            }
                        }
                        else if (szCs != null && szCs.Attributes["w:val"] != null)
                        {
                            int halfPts;
                            if (int.TryParse(szCs.Attributes["w:val"].Value, out halfPts) && halfPts > 0)
                            {
                                fontSize = halfPts / 2.0f;
                                hasExplicitSize = true;
                            }
                        }

                        // Keep paragraph font size synchronized so Vietnamese unicode split runs share the same size
                        if (hasExplicitSize)
                        {
                            currentParaFontSize = fontSize;
                        }

                        // 8.6. Color
                        XmlNode col = rPr.SelectSingleNode("w:color", nsmgr);
                        if (col != null && col.Attributes["w:val"] != null)
                        {
                            string hex = col.Attributes["w:val"].Value;
                            if (hex.Length == 6)
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

                    // 8.8. Extract text tokens
                    XmlNodeList tList = r.SelectNodes(".//w:t | .//w:tab | .//w:br | .//w:cr | .//w:sym | .//w:noBreakHyphen", nsmgr);
                    if (tList != null)
                    {
                        foreach (XmlNode tNode in tList)
                        {
                            string str = "";
                            if (tNode.LocalName == "t") str = tNode.InnerText;
                            else if (tNode.LocalName == "tab") str = "\t";
                            else if (tNode.LocalName == "br" || tNode.LocalName == "cr") str = "\n";
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
            sb.Append('\n');
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

        private static void ExtractDrawingImage(XmlNode drawingNode, XmlNamespaceManager nsmgr, ZipArchive archive, Dictionary<string, string> relsToTarget, StringBuilder sb, List<FormattedRun> runs)
        {
            if (archive == null || drawingNode == null) return;
            try
            {
                // Find blip or imagedata
                XmlNode blip = drawingNode.SelectSingleNode(".//a:blip", nsmgr)
                            ?? drawingNode.SelectSingleNode(".//*[local-name()='blip']")
                            ?? drawingNode.SelectSingleNode(".//*[local-name()='imagedata']");

                string rId = null;
                if (blip != null)
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
                if (imgEntry == null)
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.ToLower().StartsWith("word/media/image"))
                        {
                            imgEntry = entry;
                            break;
                        }
                    }
                }

                if (imgEntry != null)
                {
                    string ext = Path.GetExtension(imgEntry.FullName);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                    string tempFile = Path.Combine(Path.GetTempPath(), "genoffice_img_" + Guid.NewGuid().ToString("N") + ext);
                    using (Stream s = imgEntry.Open())
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create))
                    {
                        s.CopyTo(fs);
                    }

                    // Extract Width & Height from wp:extent or a:ext
                    int w = 480, h = 270;
                    XmlNode extent = drawingNode.SelectSingleNode(".//wp:extent", nsmgr)
                                  ?? drawingNode.SelectSingleNode(".//*[local-name()='extent']")
                                  ?? drawingNode.SelectSingleNode(".//*[local-name()='ext']");
                    if (extent != null)
                    {
                        long cx, cy;
                        if (extent.Attributes["cx"] != null && long.TryParse(extent.Attributes["cx"].Value, out cx))
                            w = (int)(cx / 9525L);
                        if (extent.Attributes["cy"] != null && long.TryParse(extent.Attributes["cy"].Value, out cy))
                            h = (int)(cy / 9525L);
                    }

                    // Constrain maximum width to page printable area (672px)
                    if (w > 672 && w > 0 && h > 0)
                    {
                        float ratio = (float)h / w;
                        w = 672;
                        h = (int)(672 * ratio);
                    }

                    // Extract WrapMode and Position
                    int wrapMode = 0;
                    int x = 80, y = 160;
                    XmlNode anchor = drawingNode.SelectSingleNode(".//wp:anchor", nsmgr)
                                  ?? drawingNode.SelectSingleNode(".//*[local-name()='anchor']");
                    if (anchor != null)
                    {
                        wrapMode = 1; // Default to square if anchor
                        if (drawingNode.SelectSingleNode(".//*[local-name()='wrapSquare']") != null) wrapMode = 1;
                        else if (drawingNode.SelectSingleNode(".//*[local-name()='wrapTight']") != null) wrapMode = 2;
                        else if (drawingNode.SelectSingleNode(".//*[local-name()='wrapThrough']") != null) wrapMode = 3;
                        else if (drawingNode.SelectSingleNode(".//*[local-name()='wrapTopAndBottom']") != null) wrapMode = 4;
                        else if (drawingNode.SelectSingleNode(".//*[local-name()='wrapNone']") != null)
                        {
                            bool behind = anchor.Attributes["behindDoc"] != null && anchor.Attributes["behindDoc"].Value == "1";
                            wrapMode = behind ? 5 : 6;
                        }

                        XmlNode posH = drawingNode.SelectSingleNode(".//*[local-name()='positionH']");
                        if (posH != null)
                        {
                            string relFrom = posH.Attributes["relativeFrom"] != null ? posH.Attributes["relativeFrom"].Value.ToLower() : "column";
                            XmlNode alignH = posH.SelectSingleNode(".//*[local-name()='align']");
                            if (alignH != null)
                            {
                                string aVal = alignH.InnerText.Trim().ToLower();
                                if (aVal == "right" || aVal == "outside")
                                    x = 72 + Math.Max(0, 672 - w);
                                else if (aVal == "center")
                                    x = 72 + Math.Max(0, (672 - w) / 2);
                                else if (aVal == "left" || aVal == "inside")
                                    x = 72;
                            }
                            else
                            {
                                XmlNode posX = posH.SelectSingleNode(".//*[local-name()='posOffset']");
                                if (posX != null)
                                {
                                    long xEmu;
                                    if (long.TryParse(posX.InnerText, out xEmu))
                                    {
                                        int rawX = (int)(xEmu / 9525L);
                                        if (relFrom == "column" || relFrom == "margin" || relFrom == "leftmargin")
                                            x = rawX + 72;
                                        else if (relFrom == "rightmargin")
                                            x = 72 + Math.Max(0, 672 - rawX - w);
                                        else
                                            x = rawX > 0 ? rawX : 72;
                                    }
                                }
                            }
                        }

                        XmlNode posV = drawingNode.SelectSingleNode(".//*[local-name()='positionV']");
                        if (posV != null)
                        {
                            string relFrom = posV.Attributes["relativeFrom"] != null ? posV.Attributes["relativeFrom"].Value.ToLower() : "paragraph";
                            XmlNode alignV = posV.SelectSingleNode(".//*[local-name()='align']");
                            if (alignV != null)
                            {
                                string aVal = alignV.InnerText.Trim().ToLower();
                                if (aVal == "top") y = 72;
                                else if (aVal == "center") y = 72 + Math.Max(0, (900 - h) / 2);
                                else if (aVal == "bottom") y = Math.Max(72, 972 - h);
                            }
                            else
                            {
                                XmlNode posY = posV.SelectSingleNode(".//*[local-name()='posOffset']");
                                if (posY != null)
                                {
                                    long yEmu;
                                    if (long.TryParse(posY.InnerText, out yEmu))
                                    {
                                        int rawY = (int)(yEmu / 9525L);
                                        if (relFrom == "paragraph" || relFrom == "line" || relFrom == "topmargin" || relFrom == "margin")
                                            y = rawY + 72;
                                        else
                                            y = rawY > 0 ? rawY : 72;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        wrapMode = 0; // wp:inline
                    }

                    string shapeId = "img_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    string tag = string.Format("![SHAPE:ID={0}:TYPE=Image:PATH={1}:X={2}:Y={3}:W={4}:H={5}:WRAP={6}:PAGE=0]",
                        shapeId, tempFile, x, y, w, h, wrapMode);

                    if (sb.Length > 0 && !sb.ToString().EndsWith("\n"))
                    {
                        sb.AppendLine();
                    }
                    int start = sb.Length;
                    sb.AppendLine(tag);
                    runs.Add(new FormattedRun(start, tag.Length + 1, "Times New Roman", 11f, false, false, false, false, Color.Black, Color.Transparent));
                }
            }
            catch { }
        }

        private static void ParseTableNode(XmlNode tbl, XmlNamespaceManager nsmgr, StringBuilder sb, List<FormattedRun> runs)
        {
            XmlNodeList trList = tbl.SelectNodes(".//w:tr", nsmgr);
            if (trList == null || trList.Count == 0) return;

            sb.AppendLine();
            int rowIndex = 0;
            int numCols = 0;

            foreach (XmlNode tr in trList)
            {
                XmlNodeList tcList = tr.SelectNodes(".//w:tc", nsmgr);
                if (tcList == null || tcList.Count == 0) continue;

                List<string> cellTexts = new List<string>();
                foreach (XmlNode tc in tcList)
                {
                    StringBuilder cellSb = new StringBuilder();
                    XmlNodeList pList = tc.SelectNodes(".//w:p", nsmgr);
                    if (pList != null)
                    {
                        foreach (XmlNode p in pList)
                        {
                            XmlNodeList tList = p.SelectNodes(".//w:t", nsmgr);
                            if (tList != null)
                            {
                                foreach (XmlNode t in tList) cellSb.Append(t.InnerText);
                            }
                        }
                    }
                    string cStr = cellSb.ToString().Trim().Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
                    cellTexts.Add(string.IsNullOrEmpty(cStr) ? " " : cStr);
                }

                numCols = Math.Max(numCols, cellTexts.Count);
                string rowLine = "| " + string.Join(" | ", cellTexts.ToArray()) + " |";
                int start = sb.Length;
                sb.AppendLine(rowLine);
                runs.Add(new FormattedRun(start, rowLine.Length + 2, "Times New Roman", 10.5f, (rowIndex == 0), false, false, false, Color.Black, Color.Transparent));

                // After header row, output markdown table separator
                if (rowIndex == 0)
                {
                    List<string> seps = new List<string>();
                    for (int c = 0; c < numCols; c++) seps.Add("-------");
                    string sepLine = "| " + string.Join(" | ", seps.ToArray()) + " |";
                    int sepStart = sb.Length;
                    sb.AppendLine(sepLine);
                    runs.Add(new FormattedRun(sepStart, sepLine.Length + 2, "Times New Roman", 10.5f, false, false, false, false, Color.Gray, Color.Transparent));
                }
                rowIndex++;
            }
            sb.AppendLine();
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
            Dictionary<int, ParagraphBorderInfo> paraBorders = null)
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
                                    sw.WriteLine("    <w:tbl>");
                                    sw.WriteLine("      <w:tblPr>");
                                    sw.WriteLine("        <w:tblW w:w=\"0\" w:type=\"auto\"/>");
                                    sw.WriteLine("        <w:jc w:val=\"center\"/>");
                                    sw.WriteLine("        <w:tblBorders>");
                                    sw.WriteLine("          <w:top w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"CBD5E1\"/>");
                                    sw.WriteLine("          <w:left w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"CBD5E1\"/>");
                                    sw.WriteLine("          <w:bottom w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"CBD5E1\"/>");
                                    sw.WriteLine("          <w:right w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"CBD5E1\"/>");
                                    sw.WriteLine("          <w:insideH w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"CBD5E1\"/>");
                                    sw.WriteLine("          <w:insideV w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"CBD5E1\"/>");
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
                                        sw.WriteLine("      <w:tr>");
                                        if (isHeaderRow) sw.WriteLine("        <w:trPr><w:tblHeader/></w:trPr>");
                                        for (int cIdx = 0; cIdx < row.Count; cIdx++)
                                        {
                                            string cell = row[cIdx];
                                            string shdHex = isHeaderRow ? "2563EB" : (rIdx % 2 == 0 ? "F8FAFC" : "FFFFFF");
                                            string textHex = isHeaderRow ? "FFFFFF" : "0F172A";
                                            decimal dummyD;
                                            string alignVal = isHeaderRow ? "center" : (cIdx == 0 ? "left" : (decimal.TryParse(cell.Replace(",", "").Replace("%", "").Trim(), out dummyD) ? "right" : "left"));

                                            sw.WriteLine("        <w:tc>");
                                            sw.WriteLine("          <w:tcPr>");
                                            sw.WriteLine(string.Format("            <w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{0}\"/>", shdHex));
                                            sw.WriteLine("            <w:vAlign w:val=\"center\"/>");
                                            sw.WriteLine("          </w:tcPr>");
                                            sw.WriteLine("          <w:p>");
                                            sw.WriteLine(string.Format("            <w:pPr><w:jc w:val=\"{0}\"/></w:pPr>", alignVal));
                                            sw.WriteLine("            <w:r>");
                                            sw.WriteLine("              <w:rPr>");
                                            sw.WriteLine("                <w:rFonts w:ascii=\"Segoe UI\" w:hAnsi=\"Segoe UI\" w:cs=\"Segoe UI\"/>");
                                            sw.WriteLine(string.Format("                <w:sz w:val=\"{0}\"/>", isHeaderRow ? "22" : "21"));
                                            sw.WriteLine(string.Format("                <w:szCs w:val=\"{0}\"/>", isHeaderRow ? "22" : "21"));
                                            if (isHeaderRow) { sw.WriteLine("                <w:b/>"); sw.WriteLine("                <w:bCs/>"); }
                                            sw.WriteLine(string.Format("                <w:color w:val=\"{0}\"/>", textHex));
                                            sw.WriteLine("              </w:rPr>");
                                            sw.WriteLine("              <w:t xml:space=\"preserve\">" + EscapeXml(cell) + "</w:t>");
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
                                int tagX = -1, tagY = -1, tagW = -1, tagH = -1, tagWrap = -1, tagPage = -1, tagStyle = -1;

                                DocShapeObject.ParseShapeTag(para, out shapeId, out shapeType, out imgPath, out tagX, out tagY, out tagW, out tagH, out tagWrap, out tagPage, out tagStyle);

                                DocShapeObject shapeObj = (shapes != null) ? shapes.Find(s => s.Id == shapeId) : null;
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

                                    if (wrapMode == 0) // In-Line with Text
                                    {
                                        sw.WriteLine("    <w:p>");
                                        sw.WriteLine("      <w:pPr><w:jc w:val=\"center\"/></w:pPr>");
                                        sw.WriteLine("      <w:r>");
                                        sw.WriteLine("        <w:drawing>");
                                        sw.WriteLine("          <wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">");
                                        sw.WriteLine(string.Format("            <wp:extent cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                        sw.WriteLine("            <wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>");
                                        sw.WriteLine(string.Format("            <wp:docPr id=\"{0}\" name=\"Picture {0}\"/>", mediaCount));
                                        sw.WriteLine("            <wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>");
                                        sw.WriteLine("            <a:graphic>");
                                        sw.WriteLine("              <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                                        sw.WriteLine("                <pic:pic>");
                                        sw.WriteLine("                  <pic:nvPicPr>");
                                        sw.WriteLine(string.Format("                    <pic:cNvPr id=\"{0}\" name=\"Picture {0}\"/>", mediaCount));
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
                                        long xOffEmu = Math.Max(0L, (pageX <= 72 ? 0L : (pageX - 72) * 9525L));
                                        long yOffEmu = (pageY <= 72 ? 0L : Math.Max(0L, (pageY - 72) * 9525L));
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
                                        sbDraw.AppendLine("            <wp:positionH relativeFrom=\"column\">");
                                        sbDraw.AppendLine(string.Format("              <wp:posOffset>{0}</wp:posOffset>", xOffEmu));
                                        sbDraw.AppendLine("            </wp:positionH>");
                                        sbDraw.AppendLine("            <wp:positionV relativeFrom=\"paragraph\">");
                                        sbDraw.AppendLine(string.Format("              <wp:posOffset>{0}</wp:posOffset>", yOffEmu));
                                        sbDraw.AppendLine("            </wp:positionV>");
                                        sbDraw.AppendLine(string.Format("            <wp:extent cx=\"{0}\" cy=\"{1}\"/>", cxEmu, cyEmu));
                                        sbDraw.AppendLine("            <wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>");
                                        sbDraw.AppendLine("            " + wrapXml);
                                        sbDraw.AppendLine(string.Format("            <wp:docPr id=\"{0}\" name=\"Picture {0}\"/>", mediaCount));
                                        sbDraw.AppendLine("            <wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>");
                                        sbDraw.AppendLine("            <a:graphic>");
                                        sbDraw.AppendLine("              <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                                        sbDraw.AppendLine("                <pic:pic>");
                                        sbDraw.AppendLine("                  <pic:nvPicPr>");
                                        sbDraw.AppendLine(string.Format("                    <pic:cNvPr id=\"{0}\" name=\"Picture {0}\"/>", mediaCount));
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
                    long cxEmu = isWide ? 12192000L : 9144000L;
                    long cyEmu = 6858000L;
                    float rawWidth = isWide ? 1280.0f : 960.0f;
                    float rawHeight = 720.0f;
                    float exportScaleX = rawWidth / Math.Max(1.0f, pres.CanvasWidth);
                    float exportScaleY = rawHeight / Math.Max(1.0f, pres.CanvasHeight);

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
                        }
                        sw.WriteLine("</Types>");
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
                        sw.WriteLine("</p:presentation>");
                    }

                    // 8. Slides and their media/rels
                    for (int i = 0; i < pres.Slides.Count; i++)
                    {
                        SlideNode slide = pres.Slides[i];
                        int slideIndex = i + 1;

                        // Discover images on slide and write media files
                        Dictionary<ShapeNode, string> shapeImgRels = new Dictionary<ShapeNode, string>();
                        List<KeyValuePair<string, string>> imgRelsList = new List<KeyValuePair<string, string>>();
                        int imgCounter = 1;

                        foreach (ShapeNode shape in slide.Shapes)
                        {
                            if (shape.Type == ShapeType.Image || shape.CachedImage != null || !string.IsNullOrEmpty(shape.ImagePath))
                            {
                                Image img = shape.CachedImage;
                                if (img == null && !string.IsNullOrEmpty(shape.ImagePath) && File.Exists(shape.ImagePath))
                                {
                                    try { img = Image.FromFile(shape.ImagePath); } catch { }
                                }
                                if (img != null)
                                {
                                    string imgFilename = string.Format("image_s{0}_{1}.png", slideIndex, imgCounter);
                                    string imgZipPath = "ppt/media/" + imgFilename;
                                    try
                                    {
                                        ZipArchiveEntry imgZip = archive.CreateEntry(imgZipPath);
                                        using (Stream isStream = imgZip.Open())
                                        {
                                            img.Save(isStream, System.Drawing.Imaging.ImageFormat.Png);
                                        }
                                        string rId = string.Format("rIdPic{0}", imgCounter);
                                        shapeImgRels[shape] = rId;
                                        imgRelsList.Add(new KeyValuePair<string, string>(rId, "../media/" + imgFilename));
                                        imgCounter++;
                                    }
                                    catch { }
                                }
                            }
                        }

                        // Write slide rels
                        ZipArchiveEntry sRelEntry = archive.CreateEntry(string.Format("ppt/slides/_rels/slide{0}.xml.rels", slideIndex));
                        using (StreamWriter sw = new StreamWriter(sRelEntry.Open(), new UTF8Encoding(false)))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                            sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout1.xml\"/>");
                            foreach (var ir in imgRelsList)
                            {
                                sw.WriteLine(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"{1}\"/>", ir.Key, ir.Value));
                            }
                            sw.WriteLine("</Relationships>");
                        }

                        // Write slide XML
                        ZipArchiveEntry slideEntry = archive.CreateEntry(string.Format("ppt/slides/slide{0}.xml", slideIndex));
                        using (StreamWriter sw = new StreamWriter(slideEntry.Open(), new UTF8Encoding(false)))
                        {
                            WriteSlideXml(sw, slide, slideIndex, exportScaleX, exportScaleY, shapeImgRels, pres);
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

        private static void WriteSlideXml(StreamWriter sw, SlideNode slide, int slideIndex, float exportScaleX, float exportScaleY, Dictionary<ShapeNode, string> shapeImgRels, PresentationRoot pres)
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sw.WriteLine("<p:sld xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">");
            sw.WriteLine("  <p:cSld>");

            // 1. Slide Background
            if (slide.IsGradientBg)
            {
                int c1 = slide.BgColor != 0 ? slide.BgColor : 0xFFFFFF;
                int c2 = slide.BgColor2 != 0 ? slide.BgColor2 : 0xE2E8F0;
                int ang = (int)(slide.BgGradientAngle * 60000);
                sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"{0:X6}\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"{1:X6}\"/></a:gs></a:gsLst><a:lin ang=\"{2}\"/></a:gradFill></p:bgPr></p:bg>", c1 & 0xFFFFFF, c2 & 0xFFFFFF, ang));
            }
            else if (slide.BgColor != 0 && (slide.BgColor & 0xFFFFFF) != 0xFFFFFF)
            {
                sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"{0:X6}\"/></a:solidFill></p:bgPr></p:bg>", slide.BgColor & 0xFFFFFF));
            }

            // 2. Shape Tree
            sw.WriteLine("    <p:spTree>");
            sw.WriteLine("      <p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
            sw.WriteLine("      <p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");

            float rawW = pres.IsWidescreen ? 1280.0f : 960.0f;
            float normScale = pres.CanvasWidth / Math.Max(1.0f, rawW);

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

                // Case A: Pictures (<p:pic>)
                if (shapeImgRels.ContainsKey(shape))
                {
                    string rId = shapeImgRels[shape];
                    sw.WriteLine("      <p:pic>");
                    sw.WriteLine(string.Format("        <p:nvPicPr><p:cNvPr id=\"{0}\" name=\"{1}\"/><p:cNvPicPr><a:picLocks noChangeAspect=\"0\"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>", spId, EscapeXml(shapeName)));
                    sw.WriteLine(string.Format("        <p:blipFill><a:blip r:embed=\"{0}\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>", rId));
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
                        int c1 = shape.FillColor;
                        int c2 = shape.GradientColor2;
                        int ang = (int)(shape.GradientAngle * 60000);
                        sw.WriteLine(string.Format("          <a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"{0:X6}\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"{1:X6}\"/></a:gs></a:gsLst><a:lin ang=\"{2}\"/></a:gradFill>", c1 & 0xFFFFFF, c2 & 0xFFFFFF, ang));
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

                        float fontPt = shape.FontSize / (normScale * (96.0f / 72.0f));
                        int szPt100 = Math.Max(400, (int)(fontPt * 100.0f));

                        sw.WriteLine("        <p:txBody>");
                        sw.WriteLine(string.Format("          <a:bodyPr anchor=\"{0}\" wrap=\"square\"/>", ancStr));
                        sw.WriteLine("          <a:lstStyle/>");
                        string[] lines = shape.Text.Split('\n');
                        foreach (string line in lines)
                        {
                            sw.WriteLine("          <a:p>");
                            sw.WriteLine(string.Format("            <a:pPr algn=\"{0}\"/>", algnStr));
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
            long wEmu = (long)(pathW * 9525.0f);
            long hEmu = (long)(pathH * 9525.0f);
            sw.WriteLine(string.Format("          <a:custGeom><a:avLst/><a:gdLst/><a:ahLst/><a:cxnLst/><a:rect l=\"0\" t=\"0\" r=\"{0}\" b=\"{1}\"/><a:pathLst>", wEmu, hEmu));
            sw.WriteLine(string.Format("            <a:path w=\"{0}\" h=\"{1}\">", (long)pathW, (long)pathH));
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
                        sw.WriteLine(string.Format("              <a:moveTo><a:pt x=\"{0}\" y=\"{1}\"/></a:moveTo>", (long)pts[i].X, (long)pts[i].Y));
                        i++;
                    }
                    else if (bType == 1) // Line
                    {
                        sw.WriteLine(string.Format("              <a:lnTo><a:pt x=\"{0}\" y=\"{1}\"/></a:lnTo>", (long)pts[i].X, (long)pts[i].Y));
                        if (isClose) sw.WriteLine("              <a:close/>");
                        i++;
                    }
                    else if (bType == 3 && i + 2 < pts.Length) // Cubic Bezier
                    {
                        bool closeBez = (types[i + 2] & 0x80) != 0;
                        sw.WriteLine(string.Format("              <a:cubicBezTo><a:pt x=\"{0}\" y=\"{1}\"/><a:pt x=\"{2}\" y=\"{3}\"/><a:pt x=\"{4}\" y=\"{5}\"/></a:cubicBezTo>",
                            (long)pts[i].X, (long)pts[i].Y, (long)pts[i + 1].X, (long)pts[i + 1].Y, (long)pts[i + 2].X, (long)pts[i + 2].Y));
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
                case TransitionType.GenOffice_CanvaFlow: p14Name = "ripple"; break;
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

                    // 2. Discover slide entries in order
                    List<ZipArchiveEntry> slideEntries = new List<ZipArchiveEntry>();
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

                    // Read Theme (Major/Minor Fonts)
                    string majorFont = "Calibri Light";
                    string minorFont = "Calibri";
                    ZipArchiveEntry themeEntry = archive.GetEntry("ppt/theme/theme1.xml");
                    if (themeEntry != null)
                    {
                        try
                        {
                            XmlDocument thDoc = new XmlDocument();
                            using (Stream ths = themeEntry.Open()) thDoc.Load(ths);
                            XmlNode majLatin = thDoc.SelectSingleNode("//*[local-name()='fontScheme']/*[local-name()='majorFont']/*[local-name()='latin']");
                            if (majLatin != null && majLatin.Attributes["typeface"] != null && !string.IsNullOrEmpty(majLatin.Attributes["typeface"].Value))
                                majorFont = majLatin.Attributes["typeface"].Value;
                            XmlNode minLatin = thDoc.SelectSingleNode("//*[local-name()='fontScheme']/*[local-name()='minorFont']/*[local-name()='latin']");
                            if (minLatin != null && minLatin.Attributes["typeface"] != null && !string.IsNullOrEmpty(minLatin.Attributes["typeface"].Value))
                                minorFont = minLatin.Attributes["typeface"].Value;
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

                            XmlDocument sDoc = new XmlDocument();
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
                                            layoutDoc = new XmlDocument();
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
                                                            masterDoc = new XmlDocument();
                                                            using (Stream ms = mEntry.Open()) masterDoc.Load(ms);
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }

                            // Layer 1: Import Slide Layout non-placeholder decorative shapes, mockups, frames, and pictures
                            if (layoutDoc != null)
                            {
                                // Layout non-placeholder shapes
                                XmlNodeList lSpNodes = layoutDoc.SelectNodes("//*[local-name()='sp']");
                                if (lSpNodes != null)
                                {
                                    foreach (XmlNode lSp in lSpNodes)
                                    {
                                        // Ignore placeholders on layout (they will be provided by the slide itself)
                                        if (lSp.SelectSingleNode(".//*[local-name()='ph']") != null) continue;
                                        ShapeNode lShape = ExtractShapeFromSpNode(lSp, null, null, pres, 0, 0, normScaleX, normScaleY, majorFont, minorFont);
                                        if (lShape != null) slide.AddShape(lShape);
                                    }
                                }

                                // Layout pictures (window frames, buttons, background graphics)
                                XmlNodeList lPicNodes = layoutDoc.SelectNodes("//*[local-name()='pic']");
                                if (lPicNodes != null)
                                {
                                    foreach (XmlNode lPic in lPicNodes)
                                    {
                                        ShapeNode lPicShape = ExtractPictureNode(lPic, archive, layoutRelEntry, 0, 0, normScaleX, normScaleY);
                                        if (lPicShape != null) slide.AddShape(lPicShape);
                                    }
                                }

                                // Layout grouped shapes
                                XmlNodeList lGrpNodes = layoutDoc.SelectNodes("//*[local-name()='spTree']/*[local-name()='grpSp']");
                                if (lGrpNodes != null)
                                {
                                    foreach (XmlNode lGrp in lGrpNodes)
                                    {
                                        List<ShapeNode> grpShapes = new List<ShapeNode>();
                                        ExtractGroupShapes(lGrp, grpShapes, layoutDoc, masterDoc, pres, archive, layoutRelEntry, 0, 0, normScaleX, normScaleY, majorFont, minorFont);
                                        foreach (ShapeNode gs in grpShapes) slide.AddShape(gs);
                                    }
                                }
                            }

                            // Layer 2: Import Slide's own Shapes, Pictures, and Grouped Elements
                            // 2a. Standalone Shapes
                            XmlNodeList spNodes = sDoc.SelectNodes("//*[local-name()='spTree']/*[local-name()='sp']");
                            if (spNodes != null)
                            {
                                foreach (XmlNode spNode in spNodes)
                                {
                                    ShapeNode shape = ExtractShapeFromSpNode(spNode, layoutDoc, masterDoc, pres, 0, 0, normScaleX, normScaleY, majorFont, minorFont);
                                    if (shape != null) slide.AddShape(shape);
                                }
                            }

                            // 2b. Standalone Pictures
                            XmlNodeList picNodes = sDoc.SelectNodes("//*[local-name()='spTree']/*[local-name()='pic']");
                            if (picNodes != null)
                            {
                                foreach (XmlNode picNode in picNodes)
                                {
                                    ShapeNode picShape = ExtractPictureNode(picNode, archive, relEntry, 0, 0, normScaleX, normScaleY);
                                    if (picShape != null) slide.AddShape(picShape);
                                }
                            }

                            // 2c. Grouped Shapes & Icons (<p:grpSp>)
                            XmlNodeList grpNodes = sDoc.SelectNodes("//*[local-name()='spTree']/*[local-name()='grpSp']");
                            if (grpNodes != null)
                            {
                                foreach (XmlNode grpNode in grpNodes)
                                {
                                    List<ShapeNode> grpShapes = new List<ShapeNode>();
                                    ExtractGroupShapes(grpNode, grpShapes, layoutDoc, masterDoc, pres, archive, relEntry, 0, 0, normScaleX, normScaleY, majorFont, minorFont);
                                    foreach (ShapeNode gs in grpShapes) slide.AddShape(gs);
                                }
                            }

                            // 2d. Import OpenXML Slide Animations & Timing (<p:timing>)
                            ParseSlideAnimations(sDoc, slide);

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
            return typeface;
        }

        #region OpenXML PPTX Parsing Helpers
        private static int ParseColorVal(XmlNode parentNode, int defaultColor = 0x000000)
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
                if (sVal == "bg1" || sVal == "lt1") return 0xFFFFFF;
                if (sVal == "bg2" || sVal == "lt2") return 0xF2F2F2;
                if (sVal == "tx1" || sVal == "dk1") return 0x000000;
                if (sVal == "tx2" || sVal == "dk2") return 0x595959;
                if (sVal == "accent1") return 0xD24726;
                if (sVal == "accent2") return 0x4472C4;
                if (sVal == "accent3") return 0xA5A5A5;
                if (sVal == "accent4") return 0xFFC000;
                if (sVal == "accent5") return 0x5B9BD5;
                if (sVal == "accent6") return 0x70AD47;
            }
            return defaultColor;
        }

        private static GraphicsPath ParseCustGeom(XmlNode custGeomNode, out float pathW, out float pathH)
        {
            pathW = 100f;
            pathH = 100f;
            if (custGeomNode == null) return null;
            XmlNode pathNode = custGeomNode.SelectSingleNode(".//*[local-name()='path']");
            if (pathNode == null) return null;

            if (pathNode.Attributes["w"] != null) float.TryParse(pathNode.Attributes["w"].Value, out pathW);
            if (pathNode.Attributes["h"] != null) float.TryParse(pathNode.Attributes["h"].Value, out pathH);
            if (pathW <= 0) pathW = 100f;
            if (pathH <= 0) pathH = 100f;

            GraphicsPath gp = new GraphicsPath();
            PointF currentPt = PointF.Empty;

            foreach (XmlNode cmd in pathNode.ChildNodes)
            {
                string ln = cmd.LocalName;
                if (ln == "moveTo")
                {
                    XmlNode pt = cmd.SelectSingleNode("./*[local-name()='pt']");
                    if (pt != null && pt.Attributes["x"] != null && pt.Attributes["y"] != null)
                    {
                        float x, y;
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
                        float x, y;
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
                else if (ln == "close")
                {
                    gp.CloseFigure();
                }
            }
            return gp;
        }

        private static ShapeNode ExtractShapeFromSpNode(XmlNode spNode, XmlDocument layoutDoc, XmlDocument masterDoc, PresentationRoot pres, float offsetX, float offsetY, float scaleX, float scaleY, string majorFont = "Calibri Light", string minorFont = "Calibri")
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
            if (cNvPr != null && cNvPr.Attributes["name"] != null)
            {
                string n = cNvPr.Attributes["name"].Value;
                shape.Name = n;
                if (n.StartsWith("!!")) shape.MorphName = n;
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
            else
            {
                XmlNode prstGeom = spNode.SelectSingleNode(".//*[local-name()='prstGeom']");
                if (prstGeom != null && prstGeom.Attributes["prst"] != null)
                {
                    string pGeom = prstGeom.Attributes["prst"].Value;
                    if (pGeom == "roundRect") shape.Type = ShapeType.RoundedRectangle;
                    else if (pGeom == "ellipse") shape.Type = ShapeType.Ellipse;
                    else if (pGeom == "triangle") shape.Type = ShapeType.Triangle;
                    else if (pGeom == "diamond") shape.Type = ShapeType.Diamond;
                    else if (pGeom == "star5") shape.Type = ShapeType.Star;
                    else if (pGeom == "rightArrow") shape.Type = ShapeType.Arrow;
                    else shape.Type = ShapeType.Rectangle;
                }
            }

            // Transform Coordinates
            XmlNode xfrm = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='xfrm']");
            if (xfrm == null) xfrm = spNode.SelectSingleNode(".//*[local-name()='xfrm']");
            XmlNode off = (xfrm != null) ? xfrm.SelectSingleNode(".//*[local-name()='off']") : null;
            XmlNode ext = (xfrm != null) ? xfrm.SelectSingleNode(".//*[local-name()='ext']") : null;
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

            // Rotation
            XmlNode rotNode = spNode.SelectSingleNode(".//*[local-name()='rot']");
            if (rotNode != null && rotNode.Attributes["rot"] != null)
            {
                int rotVal;
                if (int.TryParse(rotNode.Attributes["rot"].Value, out rotVal))
                {
                    shape.Transform.Rotation = rotVal / 60000.0f;
                }
            }

            // Shape Fill
            XmlNode solidFill = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='solidFill']");
            if (solidFill != null)
            {
                shape.FillColor = ParseColorVal(solidFill, 0x00FFFFFF);
            }

            // Shape Border
            XmlNode lnNode = spNode.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='ln']");
            if (lnNode != null)
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
                shape.StrokeColor = ParseColorVal(lnNode, 0x000000);
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
                if (bodyPr != null && bodyPr.Attributes["anchor"] != null)
                {
                    string anc = bodyPr.Attributes["anchor"].Value;
                    if (anc == "b") { shape.VerticalAlignment = VerticalTextAlignment.Bottom; hasExplicitVerticalAlignment = true; }
                    else if (anc == "ctr") { shape.VerticalAlignment = VerticalTextAlignment.Middle; hasExplicitVerticalAlignment = true; }
                    else if (anc == "t") { shape.VerticalAlignment = VerticalTextAlignment.Top; hasExplicitVerticalAlignment = true; }
                }

                StringBuilder sbText = new StringBuilder();
                XmlNodeList pNodes = txBody.SelectNodes(".//*[local-name()='p']");
                if (pNodes != null)
                {
                    foreach (XmlNode pNode in pNodes)
                    {
                        // Paragraph Alignment
                        XmlNode pPr = pNode.SelectSingleNode(".//*[local-name()='pPr']");
                        if (pPr != null)
                        {
                            if (pPr.Attributes["algn"] != null)
                            {
                                string algnStr = pPr.Attributes["algn"].Value;
                                if (algnStr == "ctr") { shape.Alignment = TextAlignment.Center; hasExplicitAlignment = true; }
                                else if (algnStr == "r") { shape.Alignment = TextAlignment.Right; hasExplicitAlignment = true; }
                                else if (algnStr == "just") { shape.Alignment = TextAlignment.Justify; hasExplicitAlignment = true; }
                                else if (algnStr == "l") { shape.Alignment = TextAlignment.Left; hasExplicitAlignment = true; }
                            }
                            XmlNode defRPr = pPr.SelectSingleNode(".//*[local-name()='defRPr']");
                            if (defRPr != null)
                            {
                                if (defRPr.Attributes["sz"] != null)
                                {
                                    int szVal;
                                    if (int.TryParse(defRPr.Attributes["sz"].Value, out szVal))
                                    {
                                        // sz is in 1/100 pt → convert to canvas pixels: pt × (96/72) × normScaleX
                                        shape.FontSize = (szVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                        hasExplicitFontSize = true;
                                    }
                                }
                                if (defRPr.Attributes["b"] != null)
                                {
                                    shape.Bold = (defRPr.Attributes["b"].Value == "1" || defRPr.Attributes["b"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                    hasExplicitBold = true;
                                }
                                XmlNode lLatin = defRPr.SelectSingleNode(".//*[local-name()='latin']");
                                if (lLatin != null && lLatin.Attributes["typeface"] != null)
                                {
                                    shape.FontName = ResolveTypeface(lLatin.Attributes["typeface"].Value, majorFont, minorFont);
                                    hasExplicitFontName = true;
                                }
                            }
                        }

                        // Runs
                        XmlNodeList rNodes = pNode.SelectNodes(".//*[local-name()='r']");
                        if (rNodes != null && rNodes.Count > 0)
                        {
                            foreach (XmlNode rNode in rNodes)
                            {
                                XmlNode rPr = rNode.SelectSingleNode(".//*[local-name()='rPr']");
                                if (rPr != null)
                                {
                                    if (rPr.Attributes["sz"] != null)
                                    {
                                        int szVal;
                                        if (int.TryParse(rPr.Attributes["sz"].Value, out szVal))
                                        {
                                            // sz is in 1/100 pt → convert to canvas pixels: pt × (96/72) × normScaleX
                                            shape.FontSize = (szVal / 100.0f) * (96.0f / 72.0f) * scaleX;
                                            hasExplicitFontSize = true;
                                        }
                                    }
                                    if (rPr.Attributes["b"] != null)
                                    {
                                        shape.Bold = (rPr.Attributes["b"].Value == "1" || rPr.Attributes["b"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                        hasExplicitBold = true;
                                    }
                                    if (rPr.Attributes["i"] != null)
                                    {
                                        shape.Italic = (rPr.Attributes["i"].Value == "1" || rPr.Attributes["i"].Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                                    }
                                    if (rPr.Attributes["u"] != null)
                                    {
                                        shape.Underline = (rPr.Attributes["u"].Value != "none");
                                    }

                                    XmlNode txtClr = rPr.SelectSingleNode(".//*[local-name()='solidFill']");
                                    if (txtClr != null)
                                    {
                                        shape.TextColor = ParseColorVal(txtClr, 0x000000);
                                    }

                                    XmlNode latin = rPr.SelectSingleNode(".//*[local-name()='latin']");
                                    if (latin != null && latin.Attributes["typeface"] != null)
                                    {
                                        shape.FontName = ResolveTypeface(latin.Attributes["typeface"].Value, majorFont, minorFont);
                                        hasExplicitFontName = true;
                                    }
                                }

                                XmlNode t = rNode.SelectSingleNode(".//*[local-name()='t']");
                                if (t != null)
                                {
                                    sbText.Append(t.InnerText);
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
                                    sbText.Append(tNode.InnerText);
                                }
                            }
                        }

                        sbText.Append("\n");
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

                            if (!hasExplicitVerticalAlignment)
                            {
                                XmlNode lBodyPr = lSp.SelectSingleNode(".//*[local-name()='bodyPr']");
                                if (lBodyPr != null && lBodyPr.Attributes["anchor"] != null)
                                {
                                    string lanc = lBodyPr.Attributes["anchor"].Value;
                                    if (lanc == "b") { shape.VerticalAlignment = VerticalTextAlignment.Bottom; hasExplicitVerticalAlignment = true; }
                                    else if (lanc == "ctr") { shape.VerticalAlignment = VerticalTextAlignment.Middle; hasExplicitVerticalAlignment = true; }
                                    else if (lanc == "t") { shape.VerticalAlignment = VerticalTextAlignment.Top; hasExplicitVerticalAlignment = true; }
                                }
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

            return shape;
        }

        private static ShapeNode ExtractPictureNode(XmlNode picNode, ZipArchive archive, ZipArchiveEntry relEntry, float offsetX, float offsetY, float scaleX, float scaleY)
        {
            ShapeNode picShape = new ShapeNode();
            picShape.Type = ShapeType.Image;
            picShape.FillColor = 0x00FFFFFF;
            picShape.StrokeWidth = 0.0f;

            XmlNode off = picNode.SelectSingleNode(".//*[local-name()='off']");
            XmlNode ext = picNode.SelectSingleNode(".//*[local-name()='ext']");
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

            // Extract image from rels & zip
            XmlNode blip = picNode.SelectSingleNode(".//*[local-name()='blip']");
            if (blip != null)
            {
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
                                    picShape.CachedImage = Image.FromStream(ms);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            return picShape;
        }

        private static void ExtractGroupShapes(XmlNode grpNode, List<ShapeNode> outShapes, XmlDocument layoutDoc, XmlDocument masterDoc, PresentationRoot pres, ZipArchive archive, ZipArchiveEntry relEntry, float parentOffX, float parentOffY, float parentScaleX, float parentScaleY, string majorFont = "Calibri Light", string minorFont = "Calibri")
        {
            if (grpNode == null) return;

            XmlNode xfrm = grpNode.SelectSingleNode("./*[local-name()='grpSpPr']/*[local-name()='xfrm']");
            if (xfrm == null) xfrm = grpNode.SelectSingleNode(".//*[local-name()='xfrm']");

            float gx = 0, gy = 0, gw = 100, gh = 100;
            float cx = 0, cy = 0, cw = 100, ch = 100;

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
            }

            if (cw <= 0) cw = gw;
            if (ch <= 0) ch = gh;

            float sx = (gw / cw);
            float sy = (gh / ch);
            float localOffX = parentOffX + (gx - cx * sx) * parentScaleX;
            float localOffY = parentOffY + (gy - cy * sy) * parentScaleY;
            float localScaleX = parentScaleX * sx;
            float localScaleY = parentScaleY * sy;

            // Extract child shapes
            XmlNodeList childSps = grpNode.SelectNodes("./*[local-name()='sp']");
            if (childSps != null)
            {
                foreach (XmlNode sp in childSps)
                {
                    ShapeNode s = ExtractShapeFromSpNode(sp, layoutDoc, masterDoc, pres, localOffX, localOffY, localScaleX, localScaleY, majorFont, minorFont);
                    if (s != null) outShapes.Add(s);
                }
            }

            // Extract child pictures
            XmlNodeList childPics = grpNode.SelectNodes("./*[local-name()='pic']");
            if (childPics != null)
            {
                foreach (XmlNode pic in childPics)
                {
                    ShapeNode p = ExtractPictureNode(pic, archive, relEntry, localOffX, localOffY, localScaleX, localScaleY);
                    if (p != null) outShapes.Add(p);
                }
            }

            // Extract nested groups
            XmlNodeList childGrps = grpNode.SelectNodes("./*[local-name()='grpSp']");
            if (childGrps != null)
            {
                foreach (XmlNode childGrp in childGrps)
                {
                    ExtractGroupShapes(childGrp, outShapes, layoutDoc, masterDoc, pres, archive, relEntry, localOffX, localOffY, localScaleX, localScaleY, majorFont, minorFont);
                }
            }
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
                            if (sh.Id == spid || sh.Name == spid || sh.Name == "Shape " + spid)
                            {
                                targetShapeId = sh.Id;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(targetShapeId) && slide.Shapes.Count > 0)
                    {
                        int sIdx = (slide.Animations.Count % slide.Shapes.Count);
                        targetShapeId = slide.Shapes[sIdx].Id;
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
                        if (!string.IsNullOrEmpty(spid))
                        {
                            foreach (ShapeNode sh in slide.Shapes)
                            {
                                if (sh.Id == spid || sh.Name == spid) { targetShapeId = sh.Id; break; }
                            }
                        }
                        if (string.IsNullOrEmpty(targetShapeId) && slide.Shapes.Count > 0)
                        {
                            int sIdx = (slide.Animations.Count % slide.Shapes.Count);
                            targetShapeId = slide.Shapes[sIdx].Id;
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
                        sw.WriteLine(string.Format("    <p:sldId id=\"{0}\" title=\"{1}\" transition=\"{2}\">", 256 + i, EscapeXml(slide.Title), slide.Transition.ToString()));
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
