using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        string p = @"c:\Users\Danh\Desktop\bài 2.pptx";
        using (var zip = ZipFile.OpenRead(p))
        {
            var entry = zip.GetEntry("ppt/slides/slide5.xml");
            var doc = new XmlDocument();
            doc.Load(entry.Open());
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("p", "http://schemas.openxmlformats.org/presentationml/2006/main");
            nsmgr.AddNamespace("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            var tNodes = doc.SelectNodes("//a:t", nsmgr);
            foreach (XmlNode t in tNodes)
            {
                if (t.InnerText.IndexOf("KH", StringComparison.OrdinalIgnoreCase) >= 0 || t.InnerText.IndexOf("NI", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    XmlNode sp = t;
                    while (sp != null && sp.LocalName != "sp") sp = sp.ParentNode;
                    if (sp != null)
                    {
                        var cNvPr = sp.SelectSingleNode(".//p:cNvPr", nsmgr);
                        Console.WriteLine("Found text '" + t.InnerText + "' in shape: " + (cNvPr != null ? cNvPr.Attributes["name"].Value : ""));
                        Console.WriteLine(sp.OuterXml);
                    }
                }
            }
        }
    }
}
