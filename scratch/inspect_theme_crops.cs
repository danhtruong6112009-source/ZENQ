using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        string path = @"sdkjs-master\slide\themes\src\13_alliance.pptx";
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            foreach (var e in zip.Entries)
            {
                if (e.FullName.EndsWith(".xml"))
                {
                    using (Stream s = e.Open())
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(s);
                        var srcRects = doc.SelectNodes("//*[local-name()='srcRect']");
                        if (srcRects.Count > 0)
                        {
                            Console.WriteLine("{0}: {1} srcRects", e.FullName, srcRects.Count);
                            foreach (XmlNode sr in srcRects)
                            {
                                Console.WriteLine("  " + sr.OuterXml);
                            }
                        }
                    }
                }
            }
        }
    }
}
