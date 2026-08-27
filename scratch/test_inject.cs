using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TestInjectSlide
{
    class Program
    {
        static void Main()
        {
            string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
            string testPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\test_inject.pptx";

            if (File.Exists(testPptx)) File.Delete(testPptx);
            File.Copy(refPptx, testPptx);

            using (var zip = ZipFile.Open(testPptx, ZipArchiveMode.Update))
            {
                var s1 = zip.GetEntry("ppt/slides/slide1.xml");
                if (s1 != null) s1.Delete();
                s1 = zip.CreateEntry("ppt/slides/slide1.xml");
                using (var sw = new StreamWriter(s1.Open(), new UTF8Encoding(false)))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<p:sld xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">");
                    sw.WriteLine("  <p:cSld>");
                    sw.WriteLine("    <p:spTree>");
                    sw.WriteLine("      <p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
                    sw.WriteLine("      <p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");
                    sw.WriteLine("      <p:sp>");
                    sw.WriteLine("        <p:nvSpPr><p:cNvPr id=\"2\" name=\"Title\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>");
                    sw.WriteLine("        <p:spPr><a:xfrm><a:off x=\"952500\" y=\"1714500\"/><a:ext cx=\"10287000\" cy=\"1143000\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>");
                    sw.WriteLine("        <p:txBody><a:bodyPr anchor=\"ctr\"/><a:lstStyle/><a:p><a:pPr algn=\"ctr\"/><a:r><a:rPr sz=\"4400\" b=\"1\"><a:solidFill><a:srgbClr val=\"38BDF8\"/></a:solidFill><a:latin typeface=\"Segoe UI\"/></a:rPr><a:t>GENOFFICE SLIDES 2026</a:t></a:r></a:p></p:txBody>");
                    sw.WriteLine("      </p:sp>");
                    sw.WriteLine("    </p:spTree>");
                    sw.WriteLine("  </p:cSld>");
                    sw.WriteLine("  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>");
                    sw.WriteLine("</p:sld>");
                }
            }
            Console.WriteLine("INJECTED_TEST_SLIDE");
        }
    }
}
