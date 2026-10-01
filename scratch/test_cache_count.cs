using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("GenOffice.exe");
            Assembly asm = Assembly.Load(bytes);
            Type writerType = asm.GetType("GenOffice.UI.WriterView");
            Type ribbonType = asm.GetType("GenOffice.UI.RibbonBar");

            dynamic w = Activator.CreateInstance(writerType);
            dynamic ribbon = Activator.CreateInstance(ribbonType);

            w.LoadDocumentFile("scratch\\test_math_doc.docx", ribbon);

            Console.WriteLine("DocShapeObject.ImageCache count after load: " + ((Type)asm.GetType("GenOffice.UI.DocShapeObject")).GetField("ImageCache").GetValue(null).ToString());
            var imageCache = (System.Collections.IDictionary)((Type)asm.GetType("GenOffice.UI.DocShapeObject")).GetField("ImageCache").GetValue(null);
            Console.WriteLine("ImageCache entries: " + imageCache.Count);
            Console.WriteLine("DocumentShapes count: " + w.DocumentShapes.Count);
            int nullCount = 0;
            foreach (var sh in w.DocumentShapes)
            {
                if (sh.CachedImage == null) nullCount++;
            }
            Console.WriteLine("DocumentShapes with CachedImage == null: " + nullCount + "/" + w.DocumentShapes.Count);
        }
    }
}
