using System;
using System.IO;
using GenOffice.Format;

namespace GenOffice.Test
{
    class CheckText
    {
        static void Main()
        {
            string path = @"scratch\test_smartart_roundtrip.docx";
            DocxMetadata meta;
            if (DocxHandler.LoadDocxFile(path, out meta))
            {
                Console.WriteLine("Text length: " + meta.TextContent.Length);
                Console.WriteLine("Content:\n" + meta.TextContent);
            }
            else
            {
                Console.WriteLine("Failed to load docx!");
            }
        }
    }
}
