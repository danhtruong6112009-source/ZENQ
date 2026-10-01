using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class ReadFontInfo
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            var presEntry = zip.GetEntry("ppt/presentation.xml");
            if (presEntry != null)
            {
                using (var stream = presEntry.Open())
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    XmlNodeList embeddedFontNodes = doc.SelectNodes("//*[local-name()='embeddedFont']");
                    Console.WriteLine("Embedded fonts count: " + (embeddedFontNodes != null ? embeddedFontNodes.Count : 0));
                    if (embeddedFontNodes != null)
                    {
                        foreach (XmlNode ef in embeddedFontNodes)
                        {
                            XmlNode fontNode = ef.SelectSingleNode(".//*[local-name()='font']");
                            string fontName = fontNode != null && fontNode.Attributes["typeface"] != null ? fontNode.Attributes["typeface"].Value : "Unknown";
                            Console.WriteLine("Font: " + fontName + " InnerXml: " + ef.InnerXml);
                        }
                    }
                }
            }
        }
    }
}
