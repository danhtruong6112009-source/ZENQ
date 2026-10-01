using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

class TestS8RenderCompare
{
    static void Main()
    {
        string msPath = @"scratch\ms365_slide8.png";
        if (!File.Exists(msPath)) { Console.WriteLine("ms365_slide8.png not found"); return; }

        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];

        // Let s check shapes and render TextBox 12
        ShapeNode sh = s8.Shapes[8];

        Console.WriteLine("Testing paragraph-aware rendering of Slide 8 TextBox 12...");
    }
}
