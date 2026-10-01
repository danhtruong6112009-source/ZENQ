using System;
using System.IO;
using System.IO.Compression;

class Program
{
    static void Main()
    {
        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            var entry = zip.GetEntry("ppt/fonts/font43.fntdata");
            using (var ms = new MemoryStream())
            {
                entry.Open().CopyTo(ms);
                byte[] data = ms.ToArray();
                Console.WriteLine("font43 size: " + data.Length);
                uint eotSize = BitConverter.ToUInt32(data, 0);
                uint fontSize = BitConverter.ToUInt32(data, 4);
                Console.WriteLine("eotSize: " + eotSize + ", fontSize: " + fontSize);
            }
        }
    }
}
