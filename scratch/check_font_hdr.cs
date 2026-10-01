using System;
using System.IO;
using System.IO.Compression;

class CheckFontHeader
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            var entry = zip.GetEntry("ppt/fonts/font38.fntdata");
            if (entry != null)
            {
                using (var s = entry.Open())
                {
                    byte[] buf = new byte[64];
                    int read = s.Read(buf, 0, buf.Length);
                    Console.Write("First 32 bytes: ");
                    for (int i = 0; i < 32; i++) Console.Write(buf[i].ToString("X2") + " ");
                    Console.WriteLine();

                    // Check if standard TTF header (00 01 00 00) or OTTO (4F 54 54 4F)
                    if (buf[0] == 0x00 && buf[1] == 0x01 && buf[2] == 0x00 && buf[3] == 0x00)
                        Console.WriteLine("Standard TrueType (raw TTF)!");
                    else if (buf[0] == 'O' && buf[1] == 'T' && buf[2] == 'T' && buf[3] == 'O')
                        Console.WriteLine("Standard OpenType (OTTO)!");
                    else
                        Console.WriteLine("Obfuscated or compressed font!");
                }
            }
        }
    }
}
