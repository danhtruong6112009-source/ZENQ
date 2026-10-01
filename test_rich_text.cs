using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

class TestRun
{
    public string text;
    public Color col;
    public TestRun(string t, Color c) { text = t; col = c; }
}

class Token
{
    public string Text;
    public Color Color;
    public Font Font;
    public float Width;
}

class Program
{
    static void Main()
    {
        PrivateFontCollection pfc = new PrivateFontCollection();
        pfc.AddFontFile("Anton.ttf");
        FontFamily antonFam = pfc.Families[0];

        Bitmap bmp = new Bitmap(1200, 600);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(14, 134, 252)); // Light blue slide bg
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // Slide 5 runs
            TestRun[] runs = new TestRun[]
            {
                new TestRun("HỘI NHẬP KINH TẾ QUỐC TẾ LÀ QUÁ TRÌNH MỘT QUỐC GIA THỰC HIỆN GẮN KẾT NỀN KINH TẾ CỦA ", Color.FromArgb(0x11, 0x24, 0x7E)),
                new TestRun("MÌNH", Color.White),
                new TestRun(" VỚI NỀN KINH TẾ ", Color.FromArgb(0x11, 0x24, 0x7E)),
                new TestRun("CÁC QUỐC GIA", Color.White),
                new TestRun(" KHÁC TRONG KHU VỰC VÀ TRÊN THẾ GIỚI DỰA TRÊN SỰ CHIA SẺ LỢI ÍCH, ĐỒNG THỜI ", Color.FromArgb(0x11, 0x24, 0x7E)),
                new TestRun("TUÂN THỦ", Color.White),
                new TestRun(" CÁC ", Color.FromArgb(0x11, 0x24, 0x7E)),
                new TestRun("CHUẨN MỰC QUỐC TẾ CHUNG", Color.White),
                new TestRun(".", Color.FromArgb(0x11, 0x24, 0x7E))
            };

            // Font size: 3799 -> 37.99 pt -> 50.6 px
            float fontSizePx = (3799f / 100f) * (96f / 72f);
            Font font = new Font(antonFam, fontSizePx, FontStyle.Regular, GraphicsUnit.Pixel);

            // Break runs into word tokens
            List<Token> tokens = new List<Token>();
            foreach (TestRun r in runs)
            {
                string s = r.text;
                int start = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == ' ' || s[i] == '\n')
                    {
                        if (i > start)
                        {
                            string w = s.Substring(start, i - start);
                            tokens.Add(new Token { Text = w, Color = r.col, Font = font, Width = g.MeasureString(w, font, PointF.Empty, StringFormat.GenericTypographic).Width });
                        }
                        string sp = s.Substring(i, 1);
                        tokens.Add(new Token { Text = sp, Color = r.col, Font = font, Width = (sp == "\n" ? 0 : g.MeasureString(" ", font, PointF.Empty, StringFormat.GenericTypographic).Width) });
                        start = i + 1;
                    }
                }
                if (start < s.Length)
                {
                    string w = s.Substring(start);
                    tokens.Add(new Token { Text = w, Color = r.col, Font = font, Width = g.MeasureString(w, font, PointF.Empty, StringFormat.GenericTypographic).Width });
                }
            }

            // Word wrap into lines
            float maxW = 1000f;
            float lineHeight = font.GetHeight(g) * 1.05f;
            float curX = 50f;
            float curY = 50f;

            foreach (Token tok in tokens)
            {
                if (tok.Text == "\n")
                {
                    curX = 50f;
                    curY += lineHeight;
                    continue;
                }

                if (curX + tok.Width > 50f + maxW && tok.Text != " ")
                {
                    curX = 50f;
                    curY += lineHeight;
                }

                if (tok.Text == " " && curX == 50f) continue; // skip leading space

                using (SolidBrush b = new SolidBrush(tok.Color))
                {
                    g.DrawString(tok.Text, tok.Font, b, curX, curY, StringFormat.GenericTypographic);
                }
                curX += tok.Width;
            }
        }
        bmp.Save("test_rich_text_render.png");
        Console.WriteLine("Saved test_rich_text_render.png");
    }
}
