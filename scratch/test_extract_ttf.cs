using System;
using System.IO;
using System.IO.Compression;
using System.Drawing.Text;

class TestExtractTtf
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            var entry = zip.GetEntry("ppt/fonts/font38.fntdata");
            using (var s = entry.Open())
            {
                byte[] raw = new byte[entry.Length];
                s.Read(raw, 0, raw.Length);

                int offset = 196;
                int ttfLen = raw.Length - offset;
                byte[] ttf = new byte[ttfLen];
                Array.Copy(raw, offset, ttf, 0, ttfLen);

                File.WriteAllBytes(@"scratch\Anton.ttf", ttf);
                Console.WriteLine("Saved Anton.ttf (" + ttfLen + " bytes)");

                PrivateFontCollection pfc = new PrivateFontCollection();
                pfc.AddFontFile(@"scratch\Anton.ttf");
                Console.WriteLine("Loaded into PrivateFontCollection successfully! Families count: " + pfc.Families.Length);
                foreach (var fam in pfc.Families)
                {
                    Console.WriteLine("  Family: " + fam.Name);
                }
            }
        }
    }
}
