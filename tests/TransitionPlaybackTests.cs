using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ZenQOffice.Core;
using ZenQOffice.UI;

class TransitionPlaybackTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool valid,string message) {if(!valid)throw new Exception(message);}
    static Bitmap Frame(SlidesView view,SlideNode a,SlideNode b,float progress,int width=1280,int height=720)
    {
        view.TransitionProgress=progress;
        var image=new Bitmap(width,height);
        using(var graphics=Graphics.FromImage(image))
        using(var drawing=new D2DGraphics(graphics,width,height))
        {
            typeof(SlidesView).GetMethod("RenderTransitionFrame",Private).Invoke(view,new object[]{drawing,new Rectangle(0,0,width,height),a,b,b.Transition,progress,width/1280f});
            Check(drawing.DebugClipDepth==0&&drawing.DebugStateStackCount==0,"Frame leaks render state");
        }
        return image;
    }
    [STAThread] static int Main()
    {
        try {Run();return 0;} catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
    static void Run()
    {
        Directory.CreateDirectory("output/transition-playback");
        var view=new SlidesView{Presentation=new PresentationRoot{CanvasWidth=1280,CanvasHeight=720},IsPresenting=true};
        view.Presentation.Slides.Clear();
        var a=new SlideNode{BgColor=Color.FromArgb(25,75,150).ToArgb()};
        var b=new SlideNode{BgColor=Color.FromArgb(130,30,40).ToArgb(),Transition=TransitionType.Airplane,TransitionDuration=1.25f};
        for(int i=0;i<18;i++)
        {
            a.Shapes.Add(new ShapeNode{Type=ShapeType.Rectangle,Text="SLIDE A "+i,FontSize=24,TextColor=Color.White.ToArgb(),FillColor=Color.FromArgb(25,75,150).ToArgb(),Transform=new Transform2D{X=30+i%6*200,Y=35+i/6*210,Width=185,Height=120}});
            b.Shapes.Add(new ShapeNode{Type=ShapeType.Rectangle,Text="SLIDE B "+i,FontSize=24,FillColor=Color.FromArgb(130,30,40).ToArgb(),Transform=new Transform2D{X=30+i%6*200,Y=35+i/6*210,Width=185,Height=120}});
        }
        view.Presentation.Slides.Add(a);view.Presentation.Slides.Add(b);view.ActiveSlideIndex=1;
        view.TriggerTransition(0,1);
        using(var frame=Frame(view,a,b,0)){}
        view.UpdateAnimationAndTransition();
        float initialProgress=view.TransitionProgress;
        Thread.Sleep(180);
        view.UpdateAnimationAndTransition();
        Check(view.TransitionProgress==initialProgress,"No fast-forward while the requested frame has not painted");
        using(var frame=Frame(view,a,b,view.TransitionProgress)){}
        Thread.Sleep(180);view.UpdateAnimationAndTransition();
        Check(view.TransitionProgress>0&&view.TransitionProgress<.04f,"A delayed frame must not consume the flight");
        int painted=0;
        while(view.IsTransitioning && painted<100)
        {
            using(var frame=Frame(view,a,b,view.TransitionProgress)){}
            typeof(SlidesView).GetField("_transitionLastTickSeconds",Private).SetValue(view,-100d);
            view.UpdateAnimationAndTransition();painted++;
        }
        Check(!view.IsTransitioning&&painted>=35,"Playback paints every stage and its final frame before ending");

        foreach(var effect in new[]{TransitionType.Airplane,TransitionType.Origami,TransitionType.Cube})
        {
            b.Transition=effect;view.TriggerTransition(0,1);
            using(var initial=Frame(view,a,b,0)){} view.UpdateAnimationAndTransition();
            var timings=new List<double>();
            for(int i=1;i<=50;i++)
            {
                var watch=Stopwatch.StartNew();
                using var frame=Frame(view,a,b,i/50f);
                timings.Add(watch.Elapsed.TotalMilliseconds);
                if(i==15||i==40||i==45)frame.Save($"output/transition-playback/{effect}-{i:00}.png");
            }
            Console.WriteLine($"{effect}: 50 HD frames, median {timings.OrderBy(x=>x).ElementAt(25):0.0} ms, tail max {timings.Skip(40).Max():0.0} ms (offscreen including allocation)");
            if(effect!=TransitionType.Cube)
            {
                using var late=Frame(view,a,b,.85f);
                int pixels=0;long xTotal=0,yTotal=0;
                for(int y=0;y<720;y+=2)for(int x=0;x<1280;x+=2)
                {
                    var color=late.GetPixel(x,y);
                    if(color.B>90&&color.R<70){pixels++;xTotal+=x;yTotal+=y;}
                }
                Check(pixels>100,effect+" must still fly near the end");
                Check(xTotal/(float)pixels>1280*.65f&&yTotal/(float)pixels<720*.65f,effect+" must fly to the upper right, not dive down");
            }
        }
        view.IsPresenting=false;view.IsTransitioning=false;b.Transition=TransitionType.None;
        var ribbon=new RibbonBar{CurrentMode=OfficeAppMode.Slides,RibbonTabs=new[]{"Chuyển tiếp"},ActiveRibbonTab=0};
        typeof(RibbonBar).GetMethod("RevealTransitionGallerySelection",Private).Invoke(ribbon,new object[]{TransitionType.None});
        var click=typeof(RibbonBar).GetMethod("HandlePowerPointRibbonClick",Private);
        click.Invoke(ribbon,new object[]{610,95,view,null});
        Check(ribbon.TransitionGalleryOffset==7&&b.Transition==TransitionType.None&&!view.IsTransitioning,"Down arrow scrolls only, without selecting or previewing");
        click.Invoke(ribbon,new object[]{610,75,view,null});
        Check(ribbon.TransitionGalleryOffset==0,"Up arrow returns to previous gallery row");
        for(int i=0;i<30;i++)ribbon.ScrollTransitionGallery(1);
        int index=ribbon.TransitionGalleryOffset;
        Check(index+7==TransitionGalleryCatalog.Entries.Length,"Can scroll to all effects");
        click.Invoke(ribbon,new object[]{100,90,view,null});
        Check(b.Transition==TransitionGalleryCatalog.Entries[index].Type,"A scrolled tile applies the displayed effect");

        using(var sheet=new Bitmap(800,((61+9)/10)*90))
        using(var graphics=Graphics.FromImage(sheet))
        {
            graphics.Clear(Color.White);
            using var font=new Font("Segoe UI",7f);
            var entries=Enum.GetValues<TransitionType>();
            for(int i=0;i<entries.Length;i++)
            {
                var icon=SvgRasterizer.RenderSvgToBitmap($"assets/transitions/{entries[i]}.svg",64,44);
                Check(icon!=null,"Every SVG icon renders: "+entries[i]);
                graphics.DrawImage(icon,i%10*80+8,i/10*90+4,64,44);
                graphics.DrawString(entries[i].ToString().Replace("GenOffice_",""),font,Brushes.Black,new RectangleF(i%10*80,i/10*90+52,80,35));
            }
            sheet.Save("output/transition-playback/icons.png");
        }
        Console.WriteLine("PASS: paint-paced full playback, late flight position, HD 3D frames, arrow scrolling, displayed tile selection, and 61 SVG illustrations");
    }
}
