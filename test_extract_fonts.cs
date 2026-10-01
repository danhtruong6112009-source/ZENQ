using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static PrivateFontCollection _pfc = new PrivateFontCollection();
    static Dictionary<string, FontFamily> _fontMap = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);
    static HashSet<string> _loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static void ExtractAndRegisterFonts(ZipArchive archive)
    {
        Dictionary<string, string> rIdToTypeface = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ZipArchiveEntry presEntry = archive.GetEntry("ppt/presentation.xml");
        if (presEntry != null)
        {
            XmlDocument pDoc = new XmlDocument();
            using (Stream s = presEntry.Open()) pDoc.Load(s);
            XmlNodeList efList = pDoc.SelectNodes("//*[local-name()='embeddedFont']");
            if (efList != null)
            {
                foreach (XmlNode ef in efList)
                {
                    XmlNode fNode = ef.SelectSingleNode("./*[local-name()='font']");
                    string typeface = (fNode != null && fNode.Attributes["typeface"] != null) ? fNode.Attributes["typeface"].Value : "";
                    if (string.IsNullOrEmpty(typeface)) continue;

                    foreach (XmlNode rNode in ef.SelectNodes(".//*[local-name()='regular' or local-name()='bold' or local-name()='italic']"))
                    {
                        string rId = (rNode.Attributes["r:id"] != null) ? rNode.Attributes["r:id"].Value : (rNode.Attributes["id"] != null ? rNode.Attributes["id"].Value : "");
                        if (!string.IsNullOrEmpty(rId) && !rIdToTypeface.ContainsKey(rId))
                        {
                            rIdToTypeface[rId] = typeface;
                        }
                    }
                }
            }
        }

        Dictionary<string, string> targetToTypeface = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ZipArchiveEntry presRelsEntry = archive.GetEntry("ppt/_rels/presentation.xml.rels");
        if (presRelsEntry != null)
        {
            XmlDocument prDoc = new XmlDocument();
            using (Stream rs = presRelsEntry.Open()) prDoc.Load(rs);
            foreach (var kvp in rIdToTypeface)
            {
                XmlNode rel = prDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", kvp.Key));
                if (rel != null && rel.Attributes["Target"] != null)
                {
                    string target = rel.Attributes["Target"].Value.Replace('\\', '/');
                    if (!target.StartsWith("ppt/")) target = "ppt/" + target;
                    targetToTypeface[target] = kvp.Value;
                }
            }
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "GenOffice_Fonts");
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string fn = entry.FullName.Replace('\\', '/');
            if (fn.StartsWith("ppt/fonts/", StringComparison.OrdinalIgnoreCase) && fn.EndsWith(".fntdata", StringComparison.OrdinalIgnoreCase))
            {
                string typeface = "";
                targetToTypeface.TryGetValue(fn, out typeface);

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream es = entry.Open()) es.CopyTo(ms);
                    data = ms.ToArray();
                }

                if (data.Length > 36)
                {
                    uint eotSize = BitConverter.ToUInt32(data, 0);
                    uint fontSize = BitConverter.ToUInt32(data, 4);
                    if (eotSize > fontSize && fontSize > 0 && eotSize <= (uint)data.Length)
                    {
                        int offset = (int)(eotSize - fontSize);
                        if (offset >= 0 && offset + fontSize <= data.Length)
                        {
                            bool isOtf = (offset + 4 <= data.Length && data[offset] == 0x4F && data[offset + 1] == 0x54 && data[offset + 2] == 0x54 && data[offset + 3] == 0x4F);
                            string ext = isOtf ? ".otf" : ".ttf";
                            string baseName = Path.GetFileNameWithoutExtension(fn);
                            string outPath = Path.Combine(tempDir, baseName + ext);

                            if (!File.Exists(outPath) || new FileInfo(outPath).Length != fontSize)
                            {
                                using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                                {
                                    fs.Write(data, offset, (int)fontSize);
                                }
                            }

                            if (!_loadedFiles.Contains(outPath))
                            {
                                HashSet<string> before = new HashSet<string>();
                                foreach (var f in _pfc.Families) before.Add(f.Name);

                                _pfc.AddFontFile(outPath);
                                _loadedFiles.Add(outPath);

                                FontFamily newFam = null;
                                foreach (var f in _pfc.Families)
                                {
                                    if (!before.Contains(f.Name))
                                    {
                                        newFam = f;
                                        break;
                                    }
                                }

                                if (newFam != null)
                                {
                                    if (!string.IsNullOrEmpty(typeface))
                                    {
                                        _fontMap[typeface] = newFam;
                                        Console.WriteLine("Mapped: " + typeface + " -> " + newFam.Name);
                                    }
                                    if (!_fontMap.ContainsKey(newFam.Name))
                                    {
                                        _fontMap[newFam.Name] = newFam;
                                    }
                                }
                                else if (!string.IsNullOrEmpty(typeface))
                                {
                                    // Match by name
                                    foreach (var f in _pfc.Families)
                                    {
                                        if (f.Name.IndexOf(typeface, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            typeface.IndexOf(f.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _fontMap[typeface] = f;
                                            Console.WriteLine("Matched: " + typeface + " -> " + f.Name);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    static void Main()
    {
        using (FileStream fs = new FileStream(@"c:\Users\Danh\Desktop\bài 2.pptx", FileMode.Open, FileAccess.Read))
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            ExtractAndRegisterFonts(zip);
        }

        Console.WriteLine("\nTotal mappings in _fontMap: " + _fontMap.Count);
        foreach (var kvp in _fontMap)
        {
            Console.WriteLine("  " + kvp.Key + " => " + kvp.Value.Name);
        }
    }
}
