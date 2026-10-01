using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Verify
{
    class Program
    {
        static void Main()
        {
            Assembly asm = Assembly.LoadFile(Path.GetFullPath("GenOffice-Setup.exe"));
            using (Stream st = asm.GetManifestResourceStream("payload.zip"))
            using (ZipArchive zip = new ZipArchive(st, ZipArchiveMode.Read))
            {
                Console.WriteLine("=== PACKAGED FILES INSIDE GENOFFICE-SETUP.EXE ===");
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Console.WriteLine("PACKAGED FILE: " + entry.FullName + " (" + entry.Length + " bytes)");
                }
                Console.WriteLine("=== TOTAL FILES: " + zip.Entries.Count + " ===");
            }
        }
    }
}
