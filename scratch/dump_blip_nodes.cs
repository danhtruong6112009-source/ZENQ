using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        string path = Directory.GetFiles(@"C:\Users\Danh\Desktop", "*2.pptx")[0];
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            for (int s = 1; s <= 31; s++)
            {
                var e = zip.GetEntry("ppt/slides/slide" + s + ".xml");
                if (e == null) continue;
                using (Stream stream = e.Open())
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    var srcRects = doc.SelectNodes("//*[local-name()='srcRect']");
                    var fillRects = doc.SelectNodes("//*[local-name()='fillRect']");
                    var blips = doc.SelectNodes("//*[local-name()='blip']");
                    bool nonZeroFill = false;
                    foreach (XmlNode f in fillRects)
                    {
                        if ((f.Attributes["l"] != null && f.Attributes["l"].Value != "0") ||
                            (f.Attributes["t"] != null && f.Attributes["t"].Value != "0") ||
                            (f.Attributes["r"] != null && f.Attributes["r"].Value != "0") ||
                            (f.Attributes["b"] != null && f.Attributes["b"].Value != "0"))
                        {
                            nonZeroFill = true;
                            break;
                        }
                    }
                    if (srcRects.Count > 0 || nonZeroFill)
                    {
                        Console.WriteLine("Slide {0}: blips={1}, srcRects={2}, nonZeroFill={3}", s, blips.Count, srcRects.Count, nonZeroFill);
                        foreach (XmlNode f in fillRects)
                        {
                            Console.WriteLine("   fillRect: " + f.OuterXml);
                        }
                        foreach (XmlNode sr in srcRects)
                        {
                            Console.WriteLine("   srcRect: " + sr.OuterXml);
                        }
                    }
                }
            }
        }
    }
}
