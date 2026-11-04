using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ZenQOffice.Core;
using ZenQOffice.UI;

class PresentationFlowTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool valid,string message) { if(!valid) throw new Exception(message); }
    static T Field<T>(SlidesView view,string name)=>(T)typeof(SlidesView).GetField(name,Private).GetValue(view);
    static Bitmap ColorImage(Color color) {var b=new Bitmap(64,48);using var g=Graphics.FromImage(b);g.Clear(color);return b;}
    static ShapeNode Item(ShapeType type,float x,float y,float w,float h)=>new ShapeNode{Type=type,Transform=new Transform2D{X=x,Y=y,Width=w,Height=h},StrokeWidth=0};
    static Bitmap Render(SlidesView view,SlideNode from,SlideNode to,float progress)
    {
        view.TransitionProgress=progress;
        var b=new Bitmap(640,360,PixelFormat.Format32bppArgb);
        using(var graphics=Graphics.FromImage(b))
        using(var drawing=new D2DGraphics(graphics,640,360))
            typeof(SlidesView).GetMethod("RenderTransitionFrame",Private).Invoke(view,new object[]{drawing,new Rectangle(0,0,640,360),from,to,to.Transition,progress,1f});
        return b;
    }
    static int Count(Bitmap bitmap,Rectangle area,Func<Color,bool> predicate)
    {
        int count=0;for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)if(predicate(bitmap.GetPixel(x,y)))count++;return count;
    }
    static double Difference(Bitmap left,Bitmap right)
    {
        double difference=0;for(int y=0;y<left.Height;y+=2)for(int x=0;x<left.Width;x+=2)
        {var a=left.GetPixel(x,y);var b=right.GetPixel(x,y);difference+=Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B);}
        return difference/(left.Width*left.Height/4*3);
    }
    [STAThread] static void Main()
    {
        Directory.CreateDirectory("output/presentation-flow");
        using var green=ColorImage(Color.LimeGreen);using var orange=ColorImage(Color.Orange);
        using var red=ColorImage(Color.Red);
        using(var canvas=new Bitmap(16,16))
        using(var graphics=Graphics.FromImage(canvas))
        using(var drawing=new D2DGraphics(graphics,16,16))
        {
            for(int attempt=0;attempt<100;attempt++)
            {
                var frames=D2DGraphics.LoadGifFramesDirect(drawing.RenderTarget,"tests/fixtures/playback.gif");
                if(frames.frames!=null)break;
                Thread.Sleep(10);
            }
        }
        green.Save("output/presentation-flow/video-poster.png");
        var view=new SlidesView();
        view.Presentation=new PresentationRoot{CanvasWidth=640,CanvasHeight=360};view.Presentation.Slides.Clear();
        var a=new SlideNode{BgColor=Color.FromArgb(25,25,45).ToArgb(),Transition=TransitionType.Wipe};
        var b=new SlideNode{BgColor=Color.FromArgb(90,25,30).ToArgb(),Transition=TransitionType.Airplane,TransitionDuration=1.25f};
        var text=Item(ShapeType.TextBox,20,10,400,75);text.Text="COMPLETE SLIDE";text.TextColor=Color.White.ToArgb();text.FontSize=32;text.FontName="Segoe UI";a.Shapes.Add(text);
        var image=Item(ShapeType.Image,20,100,100,70);image.CachedImage=green;a.Shapes.Add(image);
        var video=Item(ShapeType.Video,140,100,100,70);video.CachedImage=orange;video.VideoPosterFramePath="output/presentation-flow/video-poster.png";video.VideoStartMode=VideoStartMode.InClickSequence;a.Shapes.Add(video);
        var gif=Item(ShapeType.Image,260,100,100,70);gif.CachedImage=red;gif.ImagePath="tests/fixtures/playback.gif";gif.IsGifPlaying=true;gif.NativeGifFrameIndex=2;a.Shapes.Add(gif);
        var smart=Item(ShapeType.RoundedRectangle,380,100,150,70);smart.FillColor=Color.Cyan.ToArgb();smart.Text="SMART ART";smart.SmartArtType="Process";a.Shapes.Add(smart);
        var chart=Item(ShapeType.Chart,20,200,300,145);chart.ChartData=new DocChartData{Title="CHART",Categories=new List<string>{"A","B","C"},SeriesNames=new List<string>{"DATA"},Values=new List<List<double>>{new(){2},new(){5},new(){3}}};a.Shapes.Add(chart);
        view.Presentation.Slides.Add(a);view.Presentation.Slides.Add(b);view.IsPresenting=true;view.ActiveSlideIndex=1;
        view.TriggerTransition(0,1);
        var capture=Field<Bitmap>(view,"_cachedTransBmpA");capture.Save("output/presentation-flow/full-scene.png");
        Check(Count(capture,new Rectangle(20,10,400,75),c=>c.R>190&&c.G>190&&c.B>190)>100,"Captured slide keeps text");
        Check(capture.GetPixel(60,130).G>150,"Captured slide keeps raster image");
        Check(capture.GetPixel(180,130).R>200&&capture.GetPixel(180,130).G>70,"Captured slide keeps video frame");
        Check(capture.GetPixel(300,130).B>180&&capture.GetPixel(300,130).R<80,"Captured slide freezes current GIF frame, not its first frame");
        Check(Count(capture,new Rectangle(380,100,150,70),c=>c.B>100&&c.G>65)>1000,"Captured slide keeps actual SmartArt rendering");
        Check(chart.CreateRenderSnapshot().ChartData.Title=="CHART" && smart.CreateRenderSnapshot().SmartArtType=="Process","Render snapshot preserves chart and SmartArt model data");
        Check(Count(capture,new Rectangle(20,200,300,145),c=>c.R>70||c.G>70||c.B>70)>1000,"Captured slide keeps chart");
        Check(Count(capture,new Rectangle(0,0,640,360),c=>c.A!=255)==0,"Transition capture must be opaque");
        Check(image.DisplayCachedImage==null,"Capturing must not modify live display caches");
        Thread.Sleep(80);view.UpdateAnimationAndTransition();Check(view.TransitionProgress==0,"Clock must wait for first uploaded frame");
        using(var initial=Render(view,a,b,0)){}
        Thread.Sleep(80);view.UpdateAnimationAndTransition();Check(view.TransitionProgress<.02f,"First paint cost must not skip the effect");
        using var forward=Render(view,a,b,.3f);forward.Save("output/presentation-flow/airplane.png");
        b.Transition=TransitionType.Fracture;view.TriggerTransition(0,1);
        using(var fractured=Render(view,a,b,.12f))
        {
            fractured.Save("output/presentation-flow/fracture.png");
            Check(Count(fractured,new Rectangle(0,0,640,360),c=>c.R>190&&c.G>190&&c.B>190)>1000,"Fracture includes composited text and chart pixels");
        }
        b.Transition=TransitionType.Airplane;
        view.ActiveSlideIndex=0;view.TriggerTransition(1,0);
        Check(Field<TransitionType>(view,"_transitionEffectType")==TransitionType.Airplane,"Back navigation uses outgoing slide effect");
        using var reverse=Render(view,b,a,.7f);reverse.Save("output/presentation-flow/airplane-reverse.png");
        Check(Difference(forward,reverse)<1.5,"Reverse is the time inverse of the same transition");
        view.IsPresenting=false;view.StartPresenting(true);
        Check(view.TransitionFromSlideIndex==-1&&view.IsTransitioning,"First slide starts a transition from black");
        Check(Field<Bitmap>(view,"_cachedTransBmpA").GetPixel(300,150).ToArgb()==Color.Black.ToArgb(),"First slide source is black");
        view.IsTransitioning=false;view.ClearTransitionCache();a.Shapes.Clear();a.BgColor=Color.Blue.ToArgb();b.BgColor=Color.Red.ToArgb();
        foreach(var effect in new[]{TransitionType.Wipe,TransitionType.Split})
        {
            b.Transition=effect;b.TransitionDirection=TransitionDirection.FromLeft;view.ActiveSlideIndex=1;view.TriggerTransition(0,1);
            using var blend=Render(view,a,b,.5f);blend.Save("output/presentation-flow/"+effect+"-blend.png");
            var colors=new HashSet<int>();for(int x=0;x<640;x++)colors.Add(blend.GetPixel(x,180).ToArgb());
            Check(colors.Count>20,effect+" has a graduated blend rather than a hard cut");
        }
        view.IsTransitioning=false;view.Presentation.Slides.Clear();
        for(int i=0;i<60;i++)view.Presentation.Slides.Add(new SlideNode{Title="Slide "+(i+1)});
        view.ActiveSlideIndex=0;view.IsSlideSorterModalOpen=true;
        view.ScrollPresentationGrid(-120*100,800,600);
        Check(view.PresentationGridScrollOffset>1000,"Grid scroll reaches slides beyond first page");
        var layout=typeof(SlidesView).GetMethod("GetPresentationGridLayout",Private);object[] args={800,600,0,0,0,0};layout.Invoke(view,args);
        var bounds=(Rectangle)typeof(SlidesView).GetMethod("PresentationGridCard",Private).Invoke(view,new object[]{59,args[2],args[3],args[4]});
        Check(bounds.Top>=80&&bounds.Bottom<600,"Last row is reachable");
        using(var grid=new Bitmap(800,600))
        {
            using(var graphics=Graphics.FromImage(grid))
            using(var drawing=new D2DGraphics(graphics,800,600))
                typeof(SlidesView).GetMethod("DrawSlideSorterModal",Private).Invoke(view,new object[]{drawing,800,600});
            grid.Save("output/presentation-flow/grid-last-page.png");
        }
        view.HandleMouseDown(new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2),800,600,MouseButtons.Left);
        Check(view.ActiveSlideIndex==59&&!view.IsSlideSorterModalOpen,"Click uses scrolled grid coordinates");
        using(var canvasA=new Bitmap(32,32))
        using(var canvasB=new Bitmap(32,32))
        using(var graphicsA=Graphics.FromImage(canvasA))
        using(var graphicsB=Graphics.FromImage(canvasB))
        using(var drawingA=new D2DGraphics(graphicsA,32,32))
        using(var drawingB=new D2DGraphics(graphicsB,32,32))
        {
            var cacheFlag=typeof(D2DGraphics).GetField("_usePersistentBitmapCache",Private);
            cacheFlag.SetValue(drawingA,true);cacheFlag.SetValue(drawingB,true);
            var bitmapMethod=typeof(D2DGraphics).GetMethod("GetOrCreateD2DBitmap",Private);
            var native=bitmapMethod.Invoke(drawingA,new object[]{green});
            drawingB.PrepareImage(green);
            Check(!native.GetType().GetProperty("NativePointer").GetValue(native).Equals(IntPtr.Zero),"Audience target must not evict presenter's textures");
            Check(ReferenceEquals(native,bitmapMethod.Invoke(drawingA,new object[]{green})),"Reuse the same bitmap after rendering the other surface");
        }
        Console.WriteLine("PASS: real complete-slide captures, opaque alpha, text/image/video/GIF/chart/SmartArt, first-frame clock, reverse effect, black opening, soft blends, 60-slide grid scrolling and selection");
    }
}
