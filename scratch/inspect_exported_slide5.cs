using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        string p = @"c:\Users\Danh\Desktop\genofficepy\scratch\test_roundtrip.pptx";
        using (var zip = ZipFile.OpenRead(p))
        {
            var entry = zip.GetEntry("ppt/slides/slide5.xml");
            using (var sr = new StreamReader(entry.Open()))
            {
                string xml = sr.ReadToEnd();
                Console.WriteLine("Length: " + xml.Length);
                int idx = xml.IndexOf("TextBox 23");
                if (idx >= 0)
                {
                    int start = xml.LastIndexOf("<p:sp>", idx);
                    int end = xml.IndexOf("</p:sp>", idx) + 7;
                    Console.WriteLine("TextBox 23 XML:\n" + xml.Substring(start, end - start));
                }
                int idx9 = xml.IndexOf("Freeform 9");
                if (idx9 >= 0)
                {
                    int start = xml.LastIndexOf("<p:sp>", idx9);
                    int end = xml.IndexOf("</p:sp>", idx9) + 7;
                    Console.WriteLine("Freeform 9 XML:\n" + xml.Substring(start, end - start));
                }
                int idx25 = xml.IndexOf("TextBox 25");
                if (idx25 >= 0)
                {
                    int start = xml.LastIndexOf("<p:sp>", idx25);
                    int end = xml.IndexOf("</p:sp>", idx25) + 7;
                    Console.WriteLine("TextBox 25 XML:\n" + xml.Substring(start, end - start));
                }
            }
        }
    }
}
