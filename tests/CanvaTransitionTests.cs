using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using ZenQOffice.Core;
using ZenQOffice.UI;
using ZenQOffice.Format;
class CanvaTransitionTests
{
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static Bitmap Frame(SlidesView view,SlideNode a,SlideNode b,float progress,int width=712,int height=402)
 {
  var bitmap=new Bitmap(width,height);
  using(var native=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(native,width,height))
  {
   typeof(SlidesView).GetMethod("RenderTransitionFrameCore",Private).Invoke(view,new object[]{g,new Rectangle(0,0,width,height),a,b,b.Transition,progress,width/view.Presentation.CanvasWidth});
   Check(g.DebugClipDepth==0&&g.DebugStateStackCount==0,"Transition restores all render state");
  }
  return bitmap;
 }
 static double Difference(Bitmap a,Bitmap b)
 {
  double sum=0;int count=0;for(int y=0;y<a.Height;y+=3)for(int x=0;x<a.Width;x+=3){var c=a.GetPixel(x,y);var d=b.GetPixel(x,y);sum+=Math.Abs(c.R-d.R)+Math.Abs(c.G-d.G)+Math.Abs(c.B-d.B);count+=3;}return sum/count;
 }
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/canva-transitions");
  var deck=new PresentationRoot{CanvasWidth=712,CanvasHeight=402};deck.Slides.Clear();
  var a=new SlideNode{BgColor=0xa11525};var b=new SlideNode{BgColor=0x193f73,Transition=TransitionType.GenOffice_ColorWipe,TransitionDirection=TransitionDirection.FromLeft};b.TransitionColors=new List<int>{0xCFBDA9,0xB18D6C,0xDACBB8,0xE6D9C5};deck.Slides.Add(a);deck.Slides.Add(b);
  var view=new SlidesView{Presentation=deck,ActiveSlideIndex=1};
  // Positions measured from the supplied Canva recording (7.65s..8.55s).
  var samples=new[]{(7.84f,new[]{.31f,.15f,.05f,0f}),(7.96f,new[]{.83f,.55f,.33f,.17f}),(8.08f,new[]{1f,1f,.79f,.52f})};
  foreach(var sample in samples)
  {
   using var image=Frame(view,a,b,(sample.Item1-7.65f)/.9f);
   for(int color=0;color<4;color++)
   {
    int rgb=b.TransitionColors[color],last=-1;
    for(int x=0;x<image.Width;x++){var c=image.GetPixel(x,70);if(Math.Abs(c.R-((rgb>>16)&255))<4&&Math.Abs(c.G-((rgb>>8)&255))<4&&Math.Abs(c.B-(rgb&255))<4)last=x;}
    if(sample.Item2[color]>0&&sample.Item2[color]<1)Check(Math.Abs((last+1)/(float)image.Width-sample.Item2[color])<.075,"Color panel timing matches recorded Canva boundaries");
   }
   image.Save("output/canva-transitions/color-reference-"+sample.Item1+".png");
  }
  foreach(var direction in new[]{TransitionDirection.FromLeft,TransitionDirection.FromRight,TransitionDirection.FromTop,TransitionDirection.FromBottom,TransitionDirection.FromTopLeft,TransitionDirection.FromTopRight,TransitionDirection.FromBottomLeft,TransitionDirection.FromBottomRight})
  {
   b.TransitionDirection=direction;
   using var start=Frame(view,a,b,0);using var end=Frame(view,a,b,1);using var tail=Frame(view,a,b,.999f);
   Check(start.GetPixel(350,200).R>140&&end.GetPixel(350,200).B>100,"Each direction retains correct source and destination endpoints");
   Check(Difference(tail,end)<.2,"No color panel remains or pops at completion");
  }
  b.Transition=TransitionType.GenOffice_SmoothFlow;b.TransitionDirection=TransitionDirection.FromRight;
  a.Shapes.Add(new ShapeNode{Type=ShapeType.Rectangle,HasFill=true,FillColor=0xa11525,StrokeWidth=0,Transform=new Transform2D(0,0,712,402)});
  b.Shapes.Add(new ShapeNode{Type=ShapeType.Rectangle,HasFill=true,FillColor=0x193f73,StrokeWidth=0,Transform=new Transform2D(0,0,712,402)});
  a.Shapes.Add(new ShapeNode{Type=ShapeType.TextBox,Text="VIỆT NAM",FontSize=52,TextColor=0xffffff,HasFill=false,StrokeWidth=0,Transform=new Transform2D(30,40,500,90)});
  b.Shapes.Add(new ShapeNode{Type=ShapeType.TextBox,Text="Tổng quan",FontSize=36,TextColor=0xffffff,HasFill=false,StrokeWidth=0,Transform=new Transform2D(40,40,250,80)});
  using var alpha=new Bitmap(100,100);using(var drawing=Graphics.FromImage(alpha)){drawing.Clear(Color.Transparent);using var green=new SolidBrush(Color.FromArgb(128,0,255,0));drawing.FillEllipse(green,10,10,80,80);}
  b.Shapes.Add(new ShapeNode{Type=ShapeType.Image,CachedImage=alpha,Transform=new Transform2D(480,200,130,130)});
  view.ClearTransitionCache();
  using(var first=Frame(view,a,b,0))first.Save("output/canva-transitions/flow-start.png");
  using(var middle=Frame(view,a,b,.55f))
  {
   var c=middle.GetPixel(700,5);Check(c.R>0&&c.B>0,"Flow background remains covered instead of exposing black parallax gaps");
   middle.Save("output/canva-transitions/flow-middle.png");
  }
  Rectangle WhiteInk(Bitmap bitmap){int left=bitmap.Width,top=bitmap.Height,right=-1,bottom=-1;for(int yy=0;yy<bitmap.Height;yy++)for(int xx=0;xx<bitmap.Width;xx++){var pixel=bitmap.GetPixel(xx,yy);if(pixel.R>200&&pixel.G>200&&pixel.B>200){left=Math.Min(left,xx);top=Math.Min(top,yy);right=Math.Max(right,xx);bottom=Math.Max(bottom,yy);}}return right<0?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);}
  using(var live=Frame(view,a,b,1)){
   var finalInk=WhiteInk(live);foreach(float phase in new[]{.85f,.90f,.95f,.99f}){using var moving=Frame(view,a,b,phase);var ink=WhiteInk(moving);Check(Math.Abs(ink.Top-finalInk.Top)<=1&&Math.Abs(ink.Height-finalInk.Height)<=1,"Moving Flow text retains its baseline and ink height through the live-scene handoff: "+phase+" "+ink+" vs "+finalInk);}
  }
  var scene=typeof(SlidesView).GetField("flowTo",Private).GetValue(view);var items=(System.Collections.IList)scene.GetType().GetField("Items").GetValue(scene);
  Check(items.Count==2,"Full-slide background objects stay stationary and foreground objects stay separate");
  Check(items.Cast<object>().All(layer=>layer.GetType().GetField("Native").GetValue(layer)!=null),"Flow uses native premultiplied-alpha GPU textures");
  using(var tail=Frame(view,a,b,.999f))using(var final=Frame(view,a,b,1))
  {
   Check(Difference(tail,final)<2,"Incoming text and alpha images converge to the complete destination without a final-frame jump");
   var center=tail.GetPixel(545,265);Check(center.G>85&&center.R<50,"Semi-transparent images retain alpha over the destination background");
   tail.Save("output/canva-transitions/flow-tail.png");
  }
  D2DGraphics.ResetSharedDcTarget();using(var rebuilt=Frame(view,a,b,.75f))Check(rebuilt.GetPixel(700,5).B>20,"Flow rebuilds textures safely after its render target is recreated");
  view.SetTransitionDuration(.65f);view.SetTransition(TransitionType.GenOffice_SmoothFlow);Check(Math.Abs(b.TransitionDuration-.65f)<.001,"Selecting Flow again does not overwrite a custom duration");
  var duplicate=b.Clone();duplicate.TransitionColors[0]=0;Check(b.TransitionColors[0]!=0,"Duplicated slides own independent transition palettes");
  b.TransitionColors=new List<int>{0x102030,0x405060,0x708090,0xa0b0c0};b.TransitionDirection=TransitionDirection.FromTopLeft;b.TransitionDuration=.65f;
  PptxHandler.SavePptx("output/canva-transitions/flow-roundtrip.pptx",deck);var restored=PptxHandler.LoadPptx("output/canva-transitions/flow-roundtrip.pptx");
  Check(restored.Slides[1].Transition==b.Transition&&restored.Slides[1].TransitionDirection==b.TransitionDirection&&restored.Slides[1].TransitionColors.SequenceEqual(b.TransitionColors),"Flow settings survive PPTX save and reopen");
  b.Transition=TransitionType.GenOffice_ColorWipe;PptxHandler.SavePptx("output/canva-transitions/color-roundtrip.pptx",deck);restored=PptxHandler.LoadPptx("output/canva-transitions/color-roundtrip.pptx");
  Check(restored.Slides[1].Transition==b.Transition&&restored.Slides[1].TransitionColors.SequenceEqual(b.TransitionColors)&&Math.Abs(restored.Slides[1].TransitionDuration-.65f)<.01,"Color order and exact duration survive PPTX");
  string real=@"C:\Users\Danh\Downloads\Đỏ Vàng Bài Thuyết Trình Truyền Thống Việt Nam.pptx";
  if(File.Exists(real))
  {
   var actual=PptxHandler.LoadPptx(real);Check(actual.Slides.Count>=2,"Load the actual presentation from the reference video");
   var actualView=new SlidesView{Presentation=actual,ActiveSlideIndex=1};var old=actual.Slides[0];var next=actual.Slides[1];next.TransitionDirection=TransitionDirection.FromRight;
   foreach(var effect in new[]{TransitionType.GenOffice_ColorWipe,TransitionType.GenOffice_SmoothFlow})
   {
    next.Transition=effect;actualView.ClearTransitionCache();var timings=new List<double>();
    bool preview=Environment.GetEnvironmentVariable("CANVA_PREVIEW")=="1";string previewDir="output/canva-transitions/preview-"+effect;if(preview)Directory.CreateDirectory(previewDir);
    using(var warm=Frame(actualView,old,next,0,960,540)){if(preview)warm.Save(previewDir+"/000.png");}
    if(effect==TransitionType.GenOffice_SmoothFlow)
    {
     using var middle=Frame(actualView,old,next,.5f,960,540);
     var center=middle.GetPixel(480,5);Check(center.R>100&&center.G<70,"Canva's imported custom-path picture background stays fixed through Flow");
    }
    for(int i=1;i<=40;i++){var watch=Stopwatch.StartNew();using var image=Frame(actualView,old,next,i/40f,960,540);timings.Add(watch.Elapsed.TotalMilliseconds);if(i%8==0)image.Save("output/canva-transitions/actual-"+effect+"-"+i+".png");if(preview)image.Save(previewDir+"/"+i.ToString("000")+".png");}
    Console.WriteLine(effect+": real deck "+old.Shapes.Count+"/"+next.Shapes.Count+" objects, warm median "+timings.OrderBy(t=>t).ElementAt(20).ToString("F1")+"ms, tail max "+timings.Skip(30).Max().ToString("F1")+"ms");
   }
   actualView.ClearTransitionCache();
  }
  view.ClearTransitionCache();Console.WriteLine("PASS: recorded color timings, eight directions, native flow layers, complete slides, alpha, native settings roundtrip and real-deck render");
 }
}
