using System;
using System.IO;
using System.IO.Compression;

class ReadDraft
{
    static void Main()
    {
        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "AutoSave", "draft_w_1.docx");
        if (File.Exists(p))
        {
            using (ZipArchive zip = ZipFile.OpenRead(p))
            {
                ZipArchiveEntry entry = zip.GetEntry("word/document.xml");
                if (entry != null)
                {
                    using (StreamReader sr = new StreamReader(entry.Open()))
                    {
                        Console.WriteLine("draft_w_1 content:\n" + sr.ReadToEnd());
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("draft_w_1 does not exist!");
        }
    }
}
