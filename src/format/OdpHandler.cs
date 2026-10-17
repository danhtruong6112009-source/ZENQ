using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ZenQOffice.Core;

namespace ZenQOffice.Format
{
    /// <summary>
    /// OpenDocument Presentation (.odp) Export Engine.
    /// Standard OASIS OpenDocument v1.2 specification package for presentations.
    /// Compatible with LibreOffice Impress, Apache OpenOffice, and Microsoft PowerPoint.
    /// </summary>
    public static class OdpHandler
    {
        public static bool SaveOdp(PresentationRoot presentation, string filePath)
        {
            if (presentation == null || string.IsNullOrEmpty(filePath)) return false;

            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);

                using (ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Create))
                {
                    // 1. mimetype (Uncompressed first entry in standard ODF)
                    ZipArchiveEntry mimeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                    using (StreamWriter sw = new StreamWriter(mimeEntry.Open(), Encoding.ASCII))
                    {
                        sw.Write("application/vnd.oasis.opendocument.presentation");
                    }

                    // 2. META-INF/manifest.xml
                    ZipArchiveEntry manifestEntry = archive.CreateEntry("META-INF/manifest.xml");
                    using (StreamWriter sw = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\" manifest:version=\"1.2\">");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"/\" manifest:version=\"1.2\" manifest:media-type=\"application/vnd.oasis.opendocument.presentation\"/>");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"content.xml\" manifest:media-type=\"text/xml\"/>");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"styles.xml\" manifest:media-type=\"text/xml\"/>");
                        sw.WriteLine("</manifest:manifest>");
                    }

                    // 3. styles.xml
                    ZipArchiveEntry stylesEntry = archive.CreateEntry("styles.xml");
                    using (StreamWriter sw = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<office:document-styles xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\"");
                        sw.WriteLine("  xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\"");
                        sw.WriteLine("  xmlns:draw=\"urn:oasis:names:tc:opendocument:xmlns:drawing:1.0\"");
                        sw.WriteLine("  xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\"");
                        sw.WriteLine("  office:version=\"1.2\">");
                        sw.WriteLine("  <office:styles/>");
                        sw.WriteLine("</office:document-styles>");
                    }

                    // 4. content.xml
                    ZipArchiveEntry contentEntry = archive.CreateEntry("content.xml");
                    using (StreamWriter sw = new StreamWriter(contentEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<office:document-content xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\"");
                        sw.WriteLine("  xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\"");
                        sw.WriteLine("  xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\"");
                        sw.WriteLine("  xmlns:draw=\"urn:oasis:names:tc:opendocument:xmlns:drawing:1.0\"");
                        sw.WriteLine("  xmlns:presentation=\"urn:oasis:names:tc:opendocument:xmlns:presentation:1.0\"");
                        sw.WriteLine("  xmlns:svg=\"urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0\"");
                        sw.WriteLine("  xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\"");
                        sw.WriteLine("  office:version=\"1.2\">");

                        sw.WriteLine("  <office:automatic-styles>");
                        sw.WriteLine("    <style:style style:name=\"dp1\" style:family=\"drawing-page\">");
                        sw.WriteLine("      <style:drawing-page-properties presentation:background-visible=\"true\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"TitleText\" style:family=\"paragraph\">");
                        sw.WriteLine("      <style:paragraph-properties fo:text-align=\"center\"/>");
                        sw.WriteLine("      <style:text-properties fo:font-size=\"32pt\" fo:font-weight=\"bold\" fo:color=\"#1e293b\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"BodyText\" style:family=\"paragraph\">");
                        sw.WriteLine("      <style:text-properties fo:font-size=\"18pt\" fo:color=\"#334155\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("  </office:automatic-styles>");

                        sw.WriteLine("  <office:body>");
                        sw.WriteLine("    <office:presentation>");

                        int slideIdx = 1;
                        foreach (var slide in presentation.Slides)
                        {
                            string slideTitle = !string.IsNullOrEmpty(slide.Title) ? slide.Title : ("Slide " + slideIdx);
                            sw.WriteLine(string.Format("      <draw:page draw:name=\"{0}\" draw:style-name=\"dp1\">", EscapeXml(slideTitle)));

                            // 1. Title frame
                            if (!string.IsNullOrEmpty(slide.Title))
                            {
                                sw.WriteLine("        <draw:frame presentation:class=\"title\" svg:x=\"2cm\" svg:y=\"1.5cm\" svg:width=\"24cm\" svg:height=\"3cm\">");
                                sw.WriteLine("          <draw:text-box>");
                                sw.WriteLine(string.Format("            <text:p text:style-name=\"TitleText\">{0}</text:p>", EscapeXml(slide.Title)));
                                sw.WriteLine("          </draw:text-box>");
                                sw.WriteLine("        </draw:frame>");
                            }

                            // 2. Shapes
                            if (slide.Shapes != null)
                            {
                                float scaleX = 28f / Math.Max(1f, presentation.CanvasWidth); // cm per px
                                float scaleY = 16f / Math.Max(1f, presentation.CanvasHeight);

                                foreach (var shp in slide.Shapes)
                                {
                                    if (shp == null || shp.Transform == null) continue;
                                    float xCm = shp.Transform.X * scaleX;
                                    float yCm = shp.Transform.Y * scaleY;
                                    float wCm = Math.Max(1f, shp.Transform.Width * scaleX);
                                    float hCm = Math.Max(0.5f, shp.Transform.Height * scaleY);

                                    string text = shp.Text ?? "";
                                    sw.WriteLine(string.Format("        <draw:frame svg:x=\"{0:0.##}cm\" svg:y=\"{1:0.##}cm\" svg:width=\"{2:0.##}cm\" svg:height=\"{3:0.##}cm\">", xCm, yCm, wCm, hCm));
                                    sw.WriteLine("          <draw:text-box>");
                                    sw.WriteLine(string.Format("            <text:p text:style-name=\"BodyText\">{0}</text:p>", EscapeXml(text)));
                                    sw.WriteLine("          </draw:text-box>");
                                    sw.WriteLine("        </draw:frame>");
                                }
                            }

                            sw.WriteLine("      </draw:page>");
                            slideIdx++;
                        }

                        sw.WriteLine("    </office:presentation>");
                        sw.WriteLine("  </office:body>");
                        sw.WriteLine("</office:document-content>");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveOdp Error: " + ex.Message);
                return false;
            }
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }
    }
}
