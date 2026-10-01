using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using GenOffice.Core;
using GenOffice.Format;

class TestS8StandaloneFix3
{
    static void Main()
    {
        string msPath = @"scratch\ms365_slide8.png";
        if (!File.Exists(msPath)) { Console.WriteLine("ms365_slide8.png not found"); return; }

        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];

        XmlDocument doc = new XmlDocument() { PreserveWhitespace = true };
        doc.Load(@"scratch\slide8_dump.xml");

        using (Bitmap bGen = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bGen))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            GenOffice.UI.SlidesView sv = new GenOffice.UI.SlidesView();
            sv.Presentation = pres;
            sv.DrawSlideBackground(g, new Rectangle(0, 0, 1920, 1080), s8);

            for (int i = 0; i < s8.Shapes.Count; i++)
            {
                if (i == 8) continue;
                ShapeNode sh = s8.Shapes[i];
                if (sh == null || !sh.IsVisible) continue;
                sv.DrawShape(g, 0, 0, 1.0f, sh, false);
            }

            XmlNode sp12 = doc.SelectSingleNode("//*[local-name()='cNvPr'][@name='TextBox 12']/../..");
            DrawTextBox12(g, sp12);

            bGen.Save(@"scratch\s8_fixed_test3.png", ImageFormat.Png);
        }

        using (Bitmap b1 = new Bitmap(msPath))
        using (Bitmap b2 = new Bitmap(@"scratch\s8_fixed_test3.png"))
        {
            int diffCount = 0;
            for (int y = 0; y < 1080; y += 2)
            {
                for (int x = 0; x < 1920; x += 2)
                {
                    Color c1 = b1.GetPixel(x, y);
                    Color c2 = b2.GetPixel(x, y);
                    if (Math.Abs(c1.R - c2.R) > 15 || Math.Abs(c1.G - c2.G) > 15 || Math.Abs(c1.B - c2.B) > 15)
                    {
                        diffCount++;
                    }
                }
            }
            double totalSampled = (1920 / 2) * (1080 / 2);
            Console.WriteLine(string.Format("FIXED 3 Slide 8 Diff: {0} / {1} ({2:F2}%)",
                diffCount, totalSampled, diffCount * 100.0 / totalSampled));
        }

        using (Bitmap bMs = new Bitmap(msPath))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test3.png"))
        using (Bitmap cmp = new Bitmap(1920 * 2, 1080))
        using (Graphics g = Graphics.FromImage(cmp))
        {
            g.DrawImage(bMs, 0, 0, 1920, 1080);
            g.DrawImage(bGen, 1920, 0, 1920, 1080);
            cmp.Save(@"scratch\cmp_s8_fixed3.png", ImageFormat.Png);
            Console.WriteLine("Saved cmp_s8_fixed3.png");
        }
    }

    class TestVisualLine
    {
        public List<ShapeTextToken> Tokens = new List<ShapeTextToken>();
        public bool EndsWithNewline;
        public float LineSpacing;
    }

    static void DrawTextBox12(Graphics g, XmlNode sp)
    {
        XmlNode xfrm = sp.SelectSingleNode(".//*[local-name()='xfrm']");
        XmlNode off = xfrm.SelectSingleNode(".//*[local-name()='off']");
        XmlNode ext = xfrm.SelectSingleNode(".//*[local-name()='ext']");
        float x = (long.Parse(off.Attributes["x"].Value) / 9525.0f);
        float y = (long.Parse(off.Attributes["y"].Value) / 9525.0f) - 5.0f; // -5px firstLineShift
        float w = (long.Parse(ext.Attributes["cx"].Value) / 9525.0f);
        float h = (long.Parse(ext.Attributes["cy"].Value) / 9525.0f);

        XmlNode txBody = sp.SelectSingleNode(".//*[local-name()='txBody']");
        XmlNodeList pList = txBody.SelectNodes(".//*[local-name()='p']");

        List<ShapeTextToken> tokens = new List<ShapeTextToken>();

        foreach (XmlNode p in pList)
        {
            float pLnSpc = 66.07f;
            XmlNode lnSpc = p.SelectSingleNode(".//*[local-name()='lnSpc']/*[local-name()='spcPts']");
            if (lnSpc != null && lnSpc.Attributes["val"] != null)
            {
                pLnSpc = (float.Parse(lnSpc.Attributes["val"].Value) / 100.0f) * (96.0f / 72.0f);
            }

            XmlNodeList rList = p.SelectNodes(".//*[local-name()='r']");
            int runsInP = 0;
            if (rList != null && rList.Count > 0)
            {
                foreach (XmlNode r in rList)
                {
                    XmlNode t = r.SelectSingleNode(".//*[local-name()='t']");
                    if (t == null || string.IsNullOrEmpty(t.InnerText)) continue;

                    XmlNode rPr = r.SelectSingleNode(".//*[local-name()='rPr']");
                    float fSize = 47.18667f;
                    if (rPr != null && rPr.Attributes["sz"] != null)
                    {
                        fSize = (float.Parse(rPr.Attributes["sz"].Value) / 100.0f) * (96.0f / 72.0f);
                    }

                    Font font = SlideFontManager.CreateFont("Anton", fSize, FontStyle.Regular, GraphicsUnit.Pixel);

                    string s = t.InnerText;
                    int start = 0;
                    for (int i = 0; i < s.Length; i++)
                    {
                        if (s[i] == ' ')
                        {
                            if (i > start)
                            {
                                string word = s.Substring(start, i - start);
                                float wW = g.MeasureString(word, font, PointF.Empty, StringFormat.GenericTypographic).Width;
                                tokens.Add(new ShapeTextToken
                                {
                                    Text = word,
                                    Font = font,
                                    Width = wW,
                                    IsSpace = false,
                                    IsNewline = false,
                                    HasGradient = true,
                                    Color = Color.FromArgb(10, 28, 52),
                                    GradientColor2 = Color.FromArgb(40, 71, 228),
                                    GradientAngle = pLnSpc
                                });
                            }
                            float spW = g.MeasureString("A B", font, PointF.Empty, StringFormat.GenericTypographic).Width - g.MeasureString("AB", font, PointF.Empty, StringFormat.GenericTypographic).Width;
                            if (spW <= 0) spW = fSize * 0.24f;
                            tokens.Add(new ShapeTextToken
                            {
                                Text = " ",
                                Font = font,
                                Width = spW,
                                IsSpace = true,
                                IsNewline = false,
                                GradientAngle = pLnSpc
                            });
                            start = i + 1;
                        }
                    }
                    if (start < s.Length)
                    {
                        string word = s.Substring(start);
                        float wW = g.MeasureString(word, font, PointF.Empty, StringFormat.GenericTypographic).Width;
                        tokens.Add(new ShapeTextToken
                        {
                            Text = word,
                            Font = font,
                            Width = wW,
                            IsSpace = false,
                            IsNewline = false,
                            HasGradient = true,
                            Color = Color.FromArgb(10, 28, 52),
                            GradientColor2 = Color.FromArgb(40, 71, 228),
                            GradientAngle = pLnSpc
                        });
                    }
                    runsInP++;
                }
            }

            tokens.Add(new ShapeTextToken
            {
                Text = "\n",
                IsNewline = true,
                Width = 0,
                GradientAngle = pLnSpc
            });
        }

        List<TestVisualLine> lines = new List<TestVisualLine>();
        TestVisualLine curLine = new TestVisualLine();
        float curW = 0f;
        bool isFirstLine = true;

        foreach (var tok in tokens)
        {
            if (tok.IsNewline)
            {
                curLine.EndsWithNewline = true;
                curLine.LineSpacing = tok.GradientAngle;
                lines.Add(curLine);
                curLine = new TestVisualLine();
                curW = 0f;
                isFirstLine = true;
                continue;
            }

            if (curW + tok.Width > w && curLine.Tokens.Count > 0 && !tok.IsSpace)
            {
                curLine.LineSpacing = tok.GradientAngle;
                lines.Add(curLine);
                curLine = new TestVisualLine();
                curW = 0f;
                isFirstLine = false;
            }

            if (!isFirstLine && tok.IsSpace && curLine.Tokens.Count == 0) continue;
            curLine.Tokens.Add(tok);
            curW += tok.Width;
        }
        if (curLine.Tokens.Count > 0)
        {
            curLine.LineSpacing = 66.07f;
            lines.Add(curLine);
        }

        float curY = y;
        for (int li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            float lW = 0f;
            int spCount = 0;
            foreach (var t in line.Tokens)
            {
                lW += t.Width;
                if (t.IsSpace) spCount++;
            }

            bool isJust = (!line.EndsWithNewline && spCount > 0);
            float extraPerSp = isJust ? Math.Min((w - lW) / spCount, line.LineSpacing * 0.8f) : 0f;

            float curX = x;
            foreach (var tok in line.Tokens)
            {
                float tokW = tok.Width + (tok.IsSpace && isJust ? extraPerSp : 0f);
                if (!tok.IsSpace && !tok.IsNewline)
                {
                    RectangleF gradRect = new RectangleF(x, curY, Math.Max(1f, w), Math.Max(1f, line.LineSpacing));
                    using (LinearGradientBrush lgb = new LinearGradientBrush(gradRect, tok.Color, tok.GradientColor2, 0f))
                    {
                        ColorBlend cb = new ColorBlend(3);
                        cb.Positions = new float[] { 0f, 0.5f, 1f };
                        cb.Colors = new Color[] { Color.FromArgb(10, 28, 52), Color.FromArgb(18, 37, 132), Color.FromArgb(40, 71, 228) };
                        lgb.InterpolationColors = cb;
                        g.DrawString(tok.Text, tok.Font, lgb, curX, curY, StringFormat.GenericTypographic);
                    }
                }
                curX += tokW;
            }
            curY += line.LineSpacing;
        }
    }
}
