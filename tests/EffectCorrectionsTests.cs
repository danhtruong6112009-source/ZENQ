using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using ZenQOffice.Core;
using ZenQOffice.UI;
class EffectCorrectionsTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
    static Bitmap Render(SlidesView view,SlideNode a,SlideNode b,float progress)
    {
        view.TransitionProgress=progress;var frame=new Bitmap(640,360);
        using(var graphics=Graphics.FromImage(frame))using(var g=new D2DGraphics(graphics,640,360))
        {typeof(SlidesView).GetMethod("RenderTransitionFrame",Private).Invoke(view,new object[]{g,new Rectangle(0,0,640,360),a,b,b.Transition,progress,640f/view.Presentation.CanvasWidth});Check(g.DebugClipDepth==0,"Balanced rendering state");}
        return frame;
    }
    static int BlueCount(Bitmap frame,int y){int n=0;for(int x=0;x<640;x++){var c=frame.GetPixel(x,y);if(c.B>90&&c.R<70)n++;}return n;}
    static float BlueCenter(Bitmap frame){long total=0;int n=0;for(int y=10;y<350;y+=2)for(int x=0;x<640;x+=2){var c=frame.GetPixel(x,y);if(c.B>90&&c.R<70){total+=x;n++;}}Check(n>50,"Outgoing face remains visible");return total/(float)n;}
    [STAThread] static int Main(){try{Run();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
    static void Run()
    {
        Directory.CreateDirectory("output/effect-corrections");
        var view=new SlidesView{Presentation=new PresentationRoot{CanvasWidth=640,CanvasHeight=360},IsPresenting=true};view.Presentation.Slides.Clear();
        var a=new SlideNode{BgColor=Color.FromArgb(25,75,160).ToArgb()};var b=new SlideNode{BgColor=Color.FromArgb(170,30,45).ToArgb()};view.Presentation.Slides.Add(a);view.Presentation.Slides.Add(b);view.ActiveSlideIndex=1;
        b.Transition=TransitionType.Pan;
        foreach(var direction in new[]{TransitionDirection.FromLeft,TransitionDirection.FromRight,TransitionDirection.FromTop,TransitionDirection.FromBottom})
        {
            b.TransitionDirection=direction;view.TriggerTransition(0,1);
            using var before=Render(view,a,b,.499f);using var after=Render(view,a,b,.501f);
            double difference=0;int dark=0;
            for(int y=0;y<360;y+=2)for(int x=0;x<640;x+=2){var p=before.GetPixel(x,y);var q=after.GetPixel(x,y);difference+=Math.Abs(p.R-q.R)+Math.Abs(p.G-q.G)+Math.Abs(p.B-q.B);if(p.R<20&&p.B<50)dark++;}
            Check(difference/(640*360/4*3)<4,"Pan must not exchange overlapping layers at midpoint: "+direction);
            Check(dark==0,"Pan must cover the viewport without cracks: "+direction);
            after.Save("output/effect-corrections/pan-"+direction+".png");
        }
        b.Transition=TransitionType.Rotate;b.TransitionDirection=TransitionDirection.FromRight;view.TriggerTransition(0,1);
        using(var early=Render(view,a,b,.25f))using(var late=Render(view,a,b,.75f))
        {Check(BlueCenter(late)<BlueCenter(early),"Rotate carries outgoing face away to the left while incoming face arrives from the right");early.Save("output/effect-corrections/rotate-early.png");late.Save("output/effect-corrections/rotate-late.png");}
        b.Transition=TransitionType.Curtains;view.TriggerTransition(0,1);
        using(var curtain=Render(view,a,b,.5f)){Check(BlueCount(curtain,55)>BlueCount(curtain,305)+40,"Curtain must gather with a flared opening, not squeeze uniformly");curtain.Save("output/effect-corrections/curtains.png");}
        using(var finish=Render(view,a,b,.99f)){Check(BlueCount(finish,55)<10,"Curtain exits before completion rather than snapping away");}
        using var alpha=new Bitmap(64,64,PixelFormat.Format32bppArgb);
        using(var graphics=Graphics.FromImage(alpha)){graphics.Clear(Color.Transparent);using var red=new SolidBrush(Color.FromArgb(128,255,0,0));graphics.FillRectangle(red,16,16,48,48);}
        using(var original=new Bitmap(64,64)){using(var graphics=Graphics.FromImage(original))graphics.Clear(Color.Red);original.Save("output/effect-corrections/original.png");}
        a.BgColor=b.BgColor=Color.FromArgb(32,128,64).ToArgb();
        var source=new ShapeNode{Type=ShapeType.Image,CachedImage=alpha,ImagePath="output/effect-corrections/original.png",FillOpacity=.4f,Transform=new Transform2D{X=30,Y=30,Width=80,Height=80}};
        var destination=source.CreateRenderSnapshot();destination.Id=source.Id;destination.FillOpacity=.8f;destination.Transform.X=120;
        a.Shapes.Add(source);b.Shapes.Add(destination);b.Transition=TransitionType.Morph;view.TriggerTransition(0,1);
        using(var frame=Render(view,a,b,.5f))
        {
            var hole=frame.GetPixel(80,35);var translucent=frame.GetPixel(120,75);
            Check(Math.Abs(hole.R-32)<4&&Math.Abs(hole.G-128)<4&&Math.Abs(hole.B-64)<4,"Morph preserves PNG alpha holes rather than reloading opaque source");
            Check(translucent.R>80&&translucent.R<120&&translucent.G>75&&translucent.G<105,"Morph combines per-pixel alpha and interpolated object opacity");
            frame.Save("output/effect-corrections/morph-alpha.png");
        }
        source.FillOpacity=destination.FillOpacity=0;view.TriggerTransition(0,1);
        using(var frame=Render(view,a,b,.5f)){var c=frame.GetPixel(120,75);Check(Math.Abs(c.R-32)<4&&Math.Abs(c.G-128)<4,"Zero-opacity images remain transparent during Morph");}
        var realDeck=ZenQOffice.Format.PptxHandler.LoadPptx("tests/fixtures/morph-canada.pptx");
        view.Presentation=realDeck;
        for(int i=1;i<Math.Min(5,realDeck.Slides.Count);i++)
        {
            a=realDeck.Slides[i-1];b=realDeck.Slides[i];b.Transition=TransitionType.Morph;view.ActiveSlideIndex=i;view.TriggerTransition(i-1,i);
            foreach(float t in new[]{0f,.25f,.5f,.75f,1f})
            {
                using var frame=Render(view,a,b,t);frame.Save($"output/effect-corrections/canada-{i}-{t:F2}.png");
                int white=0;for(int y=0;y<360;y+=3)for(int x=0;x<640;x+=3){var c=frame.GetPixel(x,y);if(c.R>235&&c.G>235&&c.B>235)white++;}
                if(i==1&&t==.25f)
                {
                    int visibleTitle=0;for(int y=136;y<169;y++)for(int x=55;x<205;x++){var c=frame.GetPixel(x,y);if(c.R>180&&c.G>140&&c.B>140)visibleTitle++;}
                    Check(visibleTitle>100,"Outgoing Canada title stays above its opaque card while fading out");
                }
                Check(white<1600,$"Morph cannot cover the Canada deck in a white rectangle: slide {i} at {t}");
            }

        }
        Console.WriteLine("PASS: Pan seam and continuity in four directions, rotating faces, gathering curtain and clean exit, Morph per-pixel alpha and zero/interpolated opacity, four complex Canada slide pairs without white fill");
    }
}
