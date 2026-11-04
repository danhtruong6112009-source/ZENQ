using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using ZenQOffice.Core;
using ZenQOffice.UI;

class TransitionReferenceTests
{
    static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Field(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    static Bitmap Card(Color color, string title)
    {
        var bitmap = new Bitmap(320,180,PixelFormat.Format32bppPArgb);
        using var g=Graphics.FromImage(bitmap); g.Clear(color);
        using var font=new Font("Segoe UI",32,FontStyle.Bold);
        g.DrawString(title,font,Brushes.White,20,35);
        g.FillRectangle(Brushes.Gold,15,130,100,25);
        g.FillEllipse(Brushes.White,240,20,55,55);
        g.DrawRectangle(Pens.White,2,2,315,175);
        return bitmap;
    }
    [STAThread] static void Main()
    {
        Directory.CreateDirectory("output/transition-checks");
        using (var bitmap=new Bitmap(128,128))
        {
            using(var graphics=Graphics.FromImage(bitmap))
            using(var g=new D2DGraphics(graphics,128,128))
            {
                g.FillRectangle(Brushes.White,0,0,128,128);
                var state=g.Save();
                using var triangle=new GraphicsPath();
                triangle.AddPolygon(new[]{new PointF(10,10),new PointF(110,10),new PointF(10,110)});
                g.SetClip(triangle,CombineMode.Intersect);
                g.FillRectangle(Brushes.Red,0,0,128,128);
                var nested=g.Save(); g.IntersectClip(new Rectangle(0,0,30,30));
                g.FillRectangle(Brushes.Green,0,0,128,128); g.Restore(nested);
                g.Restore(state);
                Check(g.DebugClipDepth==0 && g.DebugStateStackCount==0,"Clip stack must balance layers and axis clips");
            }
            bitmap.Save("output/transition-checks/clip.png");
            Check(bitmap.GetPixel(95,95).ToArgb()==Color.White.ToArgb(),"Triangle clipping must not become a bounding rectangle");
            Check(bitmap.GetPixel(45,25).R>220 && bitmap.GetPixel(45,25).G<40,"Triangle interior must be drawn");
        }
        using (var texture=new Bitmap(20,20))
        using (var frame=new Bitmap(128,128))
        {
            using(var pixels=Graphics.FromImage(texture)) pixels.Clear(Color.Red);
            using(var graphics=Graphics.FromImage(frame))
            using(var g=new D2DGraphics(graphics,128,128))
            {
                g.FillRectangle(Brushes.White,0,0,128,128);
                g.DrawImage(texture,new[]{new PointF(10,10),new PointF(90,25),new PointF(30,100)},new RectangleF(0,0,20,20),GraphicsUnit.Pixel,null);
            }
            Check(frame.GetPixel(13,90).ToArgb()==Color.White.ToArgb(),"Affine image corners must not become an axis-aligned box");
            Check(frame.GetPixel(45,45).R>220 && frame.GetPixel(45,45).G<40,"Affine texture interior");
        }
        var writer=typeof(ZenQOffice.Format.PptxHandler).GetMethod("WriteTransitionXml",BindingFlags.Static|BindingFlags.NonPublic);
        var parser=typeof(ZenQOffice.Format.PptxHandler).GetMethod("ParseSlideTransition",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var effect in new[]{TransitionType.Drape,TransitionType.Random,TransitionType.FallOver,TransitionType.Airplane,TransitionType.Origami})
        {
            using var stream=new MemoryStream();
            using(var xmlWriter=new StreamWriter(stream,System.Text.Encoding.UTF8,1024,true))
            { writer.Invoke(null,new object[]{xmlWriter,new SlideNode{Transition=effect,TransitionDuration=3.25f}}); }
            string xml=System.Text.Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF');
            var doc=new System.Xml.XmlDocument();
            doc.LoadXml("<p:sld xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">"+xml+"</p:sld>");
            var restored=new SlideNode(); parser.Invoke(null,new object[]{doc,restored});
            Check(restored.Transition==effect && Math.Abs(restored.TransitionDuration-3.25f)<.001f,effect+" PPTX type/duration round-trip");
        }
        using var from=Card(Color.FromArgb(25,85,160),"SLIDE A");
        using var to=Card(Color.FromArgb(160,50,65),"SLIDE B");
        var view=new SlidesView();
        view.Presentation=new PresentationRoot {CanvasWidth=320,CanvasHeight=180};
        view.Presentation.Slides.Clear();
        var a=new SlideNode(); var b=new SlideNode();
        a.Shapes.Add(new ShapeNode{Type=ShapeType.Image,CachedImage=from,Transform=new Transform2D{Width=320,Height=180}});
        b.Shapes.Add(new ShapeNode{Type=ShapeType.Image,CachedImage=to,Transform=new Transform2D{Width=320,Height=180}});
        view.Presentation.Slides.Add(a); view.Presentation.Slides.Add(b);
        view.TransitionFromSlideIndex=0; view.TransitionToSlideIndex=1; view.IsTransitioning=true;
        Field(view,"_cachedTransBmpA",from); Field(view,"_cachedTransBmpB",to);
        Field(view,"_cachedTransFromIdx",0); Field(view,"_cachedTransToIdx",1);
        var render=typeof(SlidesView).GetMethod("RenderTransitionFrame",Private);
        int count=0;
        foreach(TransitionType effect in Enum.GetValues<TransitionType>())
        {
            b.Transition=effect;
            foreach(float progress in new[]{0f,.25f,.5f,.75f,1f})
            {
                view.TransitionProgress=progress;
                using var frame=new Bitmap(320,180);
                using(var graphics=Graphics.FromImage(frame))
                using(var g=new D2DGraphics(graphics,320,180))
                {
                    g.FillRectangle(Brushes.Magenta,0,0,320,180);
                    render.Invoke(view,new object[]{g,new Rectangle(0,0,320,180),a,b,effect,progress,1f});
                    Check(g.DebugClipDepth==0 && g.DebugStateStackCount==0,effect+" leaks clip state");
                }
                if(progress==0) Check(frame.GetPixel(180,150).B>120,effect+" must start on source slide");
                if(progress==1) Check(frame.GetPixel(180,150).R>120,effect+" must end on destination slide");
                frame.Save($"output/transition-checks/{effect}-{progress:0.00}.png");
                count++;
            }
        }
        foreach(var pair in new[]{("Airplane","Origami"),("Pan","Push"),("Gallery","Conveyor"),("Flip","Rotate"),("Cube","Box")})
        {
            var left=File.ReadAllBytes($"output/transition-checks/{pair.Item1}-0.50.png");
            var right=File.ReadAllBytes($"output/transition-checks/{pair.Item2}-0.50.png");
            Check(!System.Linq.Enumerable.SequenceEqual(left,right),pair+" must have distinct rendering");
        }
        var watch=System.Diagnostics.Stopwatch.StartNew();
        foreach(var effect in new[]{TransitionType.Airplane,TransitionType.Origami,TransitionType.Cube,TransitionType.Curtains,TransitionType.Vortex})
        {
            b.Transition=effect; view.TransitionProgress=.35f;
            using var frame=new Bitmap(1280,720);
            using var graphics=Graphics.FromImage(frame);
            using var g=new D2DGraphics(graphics,1280,720);
            render.Invoke(view,new object[]{g,new Rectangle(0,0,1280,720),a,b,effect,.35f,4f});
            Check(g.DebugClipDepth==0,effect+" HD render state");
        }
        Console.WriteLine($"HD offscreen smoke: 5 effects in {watch.ElapsedMilliseconds} ms (includes setup/upload)");
        Console.WriteLine($"PASS: geometry clipping, nested clip restoration, {count} rendered frames, all transition endpoints and balanced graphics states");
    }
}
