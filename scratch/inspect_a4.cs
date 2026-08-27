using System;
using System.IO;
using System.IO.Compression;

namespace InspectA4
{
    class Program
    {
        static void Main()
        {
            string zipPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoDoanhThu_2026.xlsx";
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                using (var sr = new StreamReader(entry.Open()))
                {
                    string xml = sr.ReadToEnd();
                    int idx = xml.IndexOf("r=\"A4\"");
                    if (idx >= 0)
                    {
                        int end = xml.IndexOf("</c>", idx);
                        Console.WriteLine("A4 XML: " + xml.Substring(idx, end - idx + 4));
                    }
                }
            }
        }
    }
}
