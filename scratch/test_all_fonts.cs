using System;
using System.IO;
using System.IO.Compression;
using System.Drawing.Text;

class TestAllFonts
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            PrivateFontCollection pfc = new PrivateFontCollection();
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.StartsWith("ppt/fonts/") && entry.FullName.EndsWith(".fntdata"))
                {
                    using (var s = entry.Open())
                    {
                        byte[] raw = new byte[entry.Length];
                        int total = 0;
                        while (total < raw.Length)
                        {
                            int r = s.Read(raw, total, raw.Length - total);
                            if (r <= 0) break;
                            total += r;
                        }

                        // Check EOT offset: search for TTF magic 0x00, 0x01, 0x00, 0x00 or OTTO
                        int offset = -1;
                        for (int i = 0; i < Math.Min(1024, raw.Length - 4); i++)
                        {
                            if ((raw[i] == 0x00 && raw[i+1] == 0x01 && raw[i+2] == 0x00 && raw[i+3] == 0x00) ||
                                (raw[i] == 'O' && raw[i+1] == 'T' && raw[i+2] == 'T' && raw[i+3] == 'O'))
                            {
                                offset = i;
                                break;
                            }
                        }

                        if (offset >= 0)
                        {
                            string tmpFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ttf");
                            int ttfLen = raw.Length - offset;
                            byte[] ttf = new byte[ttfLen];
                            Array.Copy(raw, offset, ttf, 0, ttfLen);
                            File.WriteAllBytes(tmpFile, ttf);
                            try
                            {
                                pfc.AddFontFile(tmpFile);
                                Console.WriteLine(string.Format("Success loading {0} (offset {1}, len {2})", entry.FullName, offset, ttfLen));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(string.Format("Error loading {0}: {1}", entry.FullName, ex.Message));
                            }
                        }
                        else
                        {
                            Console.WriteLine("No TTF/OTTO signature found in " + entry.FullName);
                        }
                    }
                }
            }

            Console.WriteLine("\nTotal Loaded Font Families: " + pfc.Families.Length);
            foreach (var f in pfc.Families)
            {
                Console.WriteLine("  Family: " + f.Name);
            }
        }
    }
}
