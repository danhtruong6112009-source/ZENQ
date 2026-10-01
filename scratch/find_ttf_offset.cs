using System;
using System.IO;
using System.IO.Compression;

class FindTtfOffset
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            var entry = zip.GetEntry("ppt/fonts/font38.fntdata");
            using (var s = entry.Open())
            {
                byte[] data = new byte[entry.Length];
                int total = 0;
                while (total < data.Length)
                {
                    int r = s.Read(data, total, data.Length - total);
                    if (r <= 0) break;
                    total += r;
                }

                Console.WriteLine("Total size: " + data.Length);
                for (int i = 0; i < data.Length - 4; i++)
                {
                    if (data[i] == 0x00 && data[i+1] == 0x01 && data[i+2] == 0x00 && data[i+3] == 0x00)
                    {
                        Console.WriteLine("Possible TTF header at offset: " + i);
                    }
                    if (data[i] == 'O' && data[i+1] == 'T' && data[i+2] == 'T' && data[i+3] == 'O')
                    {
                        Console.WriteLine("Possible OTTO header at offset: " + i);
                    }
                }
            }
        }
    }
}
