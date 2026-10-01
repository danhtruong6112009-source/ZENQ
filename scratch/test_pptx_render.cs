using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace TestPptx
{
    class Program
    {
        static void Main(string[] args)
        {
            string pptxPath = @"c:\Users\Danh\Desktop\bài 2.pptx";
            string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";
            
            using (FileStream fs = new FileStream(pptxPath, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                RenderSlide(archive, 3, Path.Combine(outDir, "test_slide3_sim.png"));
                RenderSlide(archive, 1, Path.Combine(outDir, "test_slide1_sim.png"));
                RenderSlide(archive, 2, Path.Combine(outDir, "test_slide2_sim.png"));
            }
            Console.WriteLine("ALL TEST SLIDES RENDERED!");
        }

        static void RenderSlide(ZipArchive archive, int slideNum, string outPath)
        {
            ZipArchiveEntry sEntry = archive.GetEntry("ppt/slides/slide" + slideNum + ".xml");
            ZipArchiveEntry rEntry = archive.GetEntry("ppt/slides/_rels/slide" + slideNum + ".xml.rels");
            if (sEntry == null) return;

            XmlDocument sDoc = new XmlDocument();
            using (Stream s = sEntry.Open()) sDoc.Load(s);

            XmlDocument rDoc = new XmlDocument();
            if (rEntry != null)
            {
                using (Stream rs = rEntry.Open()) rDoc.Load(rs);
            }

            int w = 1280;
            int h = 720;
            float scale = 1280.0f / 1920.0f;

            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // 1. Background
                XmlNode bgNode = sDoc.SelectSingleNode("//*[local-name()='bg']");
                if (bgNode != null)
                {
                    XmlNode gradFill = bgNode.SelectSingleNode(".//*[local-name()='gradFill']");
                    if (gradFill != null)
                    {
                        var gsList = gradFill.SelectNodes(".//*[local-name()='gs']");
                        if (gsList != null && gsList.Count >= 2)
                        {
                            Color c1 = ParseColor(gsList[0]);
                            Color c2 = ParseColor(gsList[gsList.Count - 1]);
                            using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(0, 0, w, h), c1, c2, LinearGradientMode.ForwardDiagonal))
                            {
                                g.FillRectangle(lgb, 0, 0, w, h);
                            }
                        }
                    }
                }

                // 2. Shapes
                XmlNodeList spList = sDoc.SelectNodes("//*[local-name()='spTree']/*[local-name()='sp']");
                if (spList != null)
                {
                    foreach (XmlNode sp in spList)
                    {
                        DrawSp(g, sp, archive, rDoc, scale);
                    }
                }

                bmp.Save(outPath, ImageFormat.Png);
                Console.WriteLine("Rendered slide " + slideNum + " to " + outPath);
            }
        }

        static void DrawSp(Graphics g, XmlNode sp, ZipArchive archive, XmlDocument rDoc, float scale)
        {
            XmlNode xfrm = sp.SelectSingleNode(".//*[local-name()='spPr']/*[local-name()='xfrm']");
            if (xfrm == null) xfrm = sp.SelectSingleNode(".//*[local-name()='xfrm']");
            if (xfrm == null) return;

            XmlNode off = xfrm.SelectSingleNode(".//*[local-name()='off']");
            XmlNode ext = xfrm.SelectSingleNode(".//*[local-name()='ext']");
            if (off == null || ext == null) return;

            long xEmu = long.Parse(off.Attributes["x"].Value);
            long yEmu = long.Parse(off.Attributes["y"].Value);
            long cxEmu = long.Parse(ext.Attributes["cx"].Value);
            long cyEmu = long.Parse(ext.Attributes["cy"].Value);

            float x = (xEmu / 9525.0f) * scale;
            float y = (yEmu / 9525.0f) * scale;
            float sx = (cxEmu / 9525.0f) * scale;
            float sy = (cyEmu / 9525.0f) * scale;

            float rot = 0;
            if (xfrm.Attributes["rot"] != null)
            {
                rot = float.Parse(xfrm.Attributes["rot"].Value) / 60000.0f;
            }

            GraphicsState state = g.Save();
            if (rot != 0)
            {
                g.TranslateTransform(x + sx / 2.0f, y + sy / 2.0f);
                g.RotateTransform(rot);
                g.TranslateTransform(-(x + sx / 2.0f), -(y + sy / 2.0f));
            }

            // Check BlipFill
            XmlNode blipFill = sp.SelectSingleNode(".//*[local-name()='blipFill']");
            if (blipFill != null)
            {
                XmlNode blip = blipFill.SelectSingleNode(".//*[local-name()='blip']");
                if (blip != null)
                {
                    string rId = "";
                    foreach (XmlAttribute attr in blip.Attributes)
                    {
                        if (attr.LocalName == "embed") { rId = attr.Value; break; }
                    }
                    if (!string.IsNullOrEmpty(rId) && rDoc != null)
                    {
                        XmlNode rel = rDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", rId));
                        if (rel != null && rel.Attributes["Target"] != null)
                        {
                            string target = rel.Attributes["Target"].Value.Replace('\\', '/');
                            if (target.StartsWith("../")) target = "ppt/" + target.Substring(3);
                            else if (!target.StartsWith("ppt/")) target = "ppt/slides/" + target;

                            ZipArchiveEntry imgEntry = archive.GetEntry(target);
                            if (imgEntry != null)
                            {
                                using (Stream isStream = imgEntry.Open())
                                using (Image img = Image.FromStream(isStream))
                                {
                                    g.DrawImage(img, new RectangleF(x, y, sx, sy));
                                }
                            }
                        }
                    }
                }
            }

            // Check Text
            XmlNode txBody = sp.SelectSingleNode(".//*[local-name()='txBody']");
            if (txBody != null)
            {
                XmlNodeList pList = txBody.SelectNodes(".//*[local-name()='p']");
                if (pList != null)
                {
                    float curY = y;
                    foreach (XmlNode pNode in pList)
                    {
                        XmlNodeList rList = pNode.SelectNodes(".//*[local-name()='r']");
                        if (rList != null)
                        {
                            foreach (XmlNode rNode in rList)
                            {
                                XmlNode tNode = rNode.SelectSingleNode(".//*[local-name()='t']");
                                if (tNode == null) continue;
                                string text = tNode.InnerText;

                                float fontSize = 24f * scale;
                                Color textCol = Color.White;
                                string fontName = "Impact"; // Fallback for Anton

                                XmlNode rPr = rNode.SelectSingleNode(".//*[local-name()='rPr']");
                                if (rPr != null)
                                {
                                    if (rPr.Attributes["sz"] != null)
                                    {
                                        fontSize = (float.Parse(rPr.Attributes["sz"].Value) / 100.0f) * (96f / 72f) * scale * 0.72f;
                                    }
                                    XmlNode latin = rPr.SelectSingleNode(".//*[local-name()='latin']");
                                    if (latin != null && latin.Attributes["typeface"] != null)
                                    {
                                        string tf = latin.Attributes["typeface"].Value;
                                        if (tf == "Anton") fontName = "Impact";
                                        else fontName = tf;
                                    }
                                    XmlNode sClr = rPr.SelectSingleNode(".//*[local-name()='srgbClr']");
                                    if (sClr != null && sClr.Attributes["val"] != null)
                                    {
                                        textCol = ColorTranslator.FromHtml("#" + sClr.Attributes["val"].Value);
                                    }
                                }

                                using (Font font = new Font(fontName, fontSize, FontStyle.Bold))
                                using (SolidBrush br = new SolidBrush(textCol))
                                {
                                    g.DrawString(text, font, br, new RectangleF(x, curY, sx, sy));
                                    curY += g.MeasureString(text, font, (int)sx).Height;
                                }
                            }
                        }
                    }
                }
            }

            g.Restore(state);
        }

        static Color ParseColor(XmlNode node)
        {
            XmlNode clr = node.SelectSingleNode(".//*[local-name()='srgbClr']");
            if (clr != null && clr.Attributes["val"] != null)
            {
                return ColorTranslator.FromHtml("#" + clr.Attributes["val"].Value);
            }
            return Color.White;
        }
    }
}
