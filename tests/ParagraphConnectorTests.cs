using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Reflection;
using ZenQOffice.Core;
using ZenQOffice.UI;
using ZenQOffice.Format;
class ParagraphConnectorTests
{
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 static Bitmap Render(SlidesView view,SlideNode slide,int step=0,float progress=1,bool presenting=false)
 {
  view.IsPresenting=presenting;view.CurrentAnimationStep=step;view.AnimationProgress=progress;view.IsPlayingSlideAnimation=presenting&&progress<1;
  var image=new Bitmap(960,540);
  using(var graphics=Graphics.FromImage(image))using(var g=new D2DGraphics(graphics,960,540))
  {
   float scale=960f/view.Presentation.CanvasWidth;
   if(!presenting)typeof(SlidesView).GetMethod("DrawSlideFull",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{g,new RectangleF(0,0,960,540),slide,scale,1f,false});
   else
   {
    view.DrawSlideBackground(g,new RectangleF(0,0,960,540),slide);
    foreach(var shape in slide.Shapes)
    {
     view.GetShapeAnimationTransform(shape,slide,step,true,false,-1,progress,out bool visible,out float x,out float y,out float sx,out float sy,out float rotation,out float opacity,out var effect,out float p);
     if(visible)view.DrawShape(g,0,0,scale,shape,false,opacity,x,y,sx,sy,rotation,effect,p);
    }
   }
  }
  return image;
 }
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception error){Console.WriteLine(error);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/paragraph-connectors");
  var deck=PptxHandler.LoadPptx("tests/fixtures/paragraph-connectors.pptx");var view=new SlidesView{Presentation=deck};
  var first=deck.Slides[0];var title=first.Shapes.First(s=>(s.Text??"").Contains("WEB"));Check(title.StrokeWidth>1&&title.StrokeColor!=0,"Theme line reference restores the missing orange frame");
  using(var image=Render(view,first)){image.Save("output/paragraph-connectors/slide5.png");}
  var example=deck.Slides[1];var text=example.Shapes.First(s=>s.RawSpid=="5");var textEffects=example.Animations.Where(a=>a.ShapeId==text.Id).ToList();
  Check(textEffects.Count==7&&textEffects.Count(a=>a.TargetParagraphStart>=0)==6,"One text box keeps whole-object plus six independent paragraph effects");
  Check(textEffects.Where(a=>a.TargetParagraphStart>=0).Select(a=>a.TargetParagraphStart).OrderBy(i=>i).SequenceEqual(new[]{0,1,2,3,4,5}),"Paragraph ranges retain original order, including paragraph five with a different click group");
  view.ActiveSlideIndex=1;using(var image=Render(view,example)){image.Save("output/paragraph-connectors/slide9.png");}
  using(var image=Render(view,example,2,1,true)){image.Save("output/paragraph-connectors/slide9-step2.png");}
  using(var image=Render(view,example,3,1,true)){image.Save("output/paragraph-connectors/slide9-step3.png");}
  using(var image=Render(view,example,4,.5f,true)){image.Save("output/paragraph-connectors/slide9-step4.png");}
  view.IsPresenting=false;view.IsPlayingSlideAnimation=false;view.ShowAnimationOrderTags=true;
  using(var image=new Bitmap(1120,640))
  {
   using(var graphics=Graphics.FromImage(image))using(var g=new D2DGraphics(graphics,1120,640))
   {
    g.Clear(Color.FromArgb(240,240,240));float scale=960f/deck.CanvasWidth;
    typeof(SlidesView).GetMethod("DrawSlideFull",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{g,new RectangleF(80,50,960,540),example,scale,1f,false});
    typeof(SlidesView).GetMethod("DrawAnimationOrderTags",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{g,80f,50f,scale,example});
   }
   image.Save("output/paragraph-connectors/slide9-order.png");
  }
  var tags=(System.Collections.Generic.List<(RectangleF Bounds,int Index)>)typeof(SlidesView).GetField("_animationOrderTags",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
  Check(tags.Count==example.Animations.Count,"Every animation has its own selectable order badge, including multiple effects on one text box");
  foreach(var badge in tags)Check(!tags.Any(other=>other.Index!=badge.Index&&other.Bounds.IntersectsWith(badge.Bounds)),"Whole-box and first-paragraph badges remain separately visible and selectable");
  int scopedIndex=example.Animations.FindLastIndex(a=>a.ShapeId==text.Id&&a.TargetParagraphStart>=0);var tag=tags.First(t=>t.Index==scopedIndex);
  view.HandleMouseDown(Point.Round(new PointF(tag.Bounds.X+5,tag.Bounds.Y+5)),960,540);
  Check(view.SelectedAnimationIndex==scopedIndex&&view.SelectedShape==text,"Clicking a paragraph order badge selects that exact effect");
  var originalType=example.Animations[scopedIndex].Type;int effectCount=example.Animations.Count;
  view.AddShapeAnimation(AnimationType.Pulse);Check(example.Animations.Count==effectCount&&example.Animations[scopedIndex].Type==AnimationType.Pulse,"Changing an effect edits the selected row without replacing another paragraph's animation");example.Animations[scopedIndex].Type=originalType;
  var form=deck.Slides[2];var list=form.Shapes.First(s=>s.RawSpid=="24");
  Check(list.ParagraphStyles.Count==13&&list.ParagraphStyles.All(p=>!string.IsNullOrEmpty(p.Bullet)),"All thirteen list bullets are imported separately from text");
  Check(list.ParagraphStyles[2].LeftMargin>list.ParagraphStyles[0].LeftMargin&&list.ParagraphStyles[0].BulletFont=="Wingdings","Nested indentation and the square Wingdings bullet survive");
  var elbows=form.Shapes.Where(s=>s.Type==ShapeType.Line&&s.ConnectorGeometry=="bentConnector3").ToList();Check(elbows.Count>=10,"Elbow connectors stay bent connectors");
  foreach(var elbow in elbows){using var path=SlideConnectorGeometry.Path(elbow,elbow.Transform.Width,elbow.Transform.Height);Check(path.PointCount==4,"An elbow retains its four routed points");}
  view.ActiveSlideIndex=2;using(var image=Render(view,form)){image.Save("output/paragraph-connectors/slide10.png");}
  PptxHandler.SavePptx("output/paragraph-connectors/roundtrip.pptx",deck);var restored=PptxHandler.LoadPptx("output/paragraph-connectors/roundtrip.pptx");
  Check(restored.Slides[1].Animations.Count(a=>a.TargetParagraphStart>=0)==6,"Paragraph animation scopes survive PPTX roundtrip");
  Check(restored.Slides[2].Shapes.First(s=>(s.Text??"").StartsWith("label")).ParagraphStyles.All(p=>!string.IsNullOrEmpty(p.Bullet)),"Bullet metadata survives PPTX roundtrip");
  Check(restored.Slides[2].Shapes.Count(s=>s.ConnectorGeometry=="bentConnector3")==elbows.Count,"Elbow geometry survives PPTX roundtrip");
  Check(Math.Abs(restored.Slides[0].Shapes.First(s=>(s.Text??"").Contains("WEB")).StrokeWidth-title.StrokeWidth)<.01f,"Saving and reopening keeps the theme frame's original thickness");
  view.ActiveSlideIndex=0;view.IsPresenting=false;view.SelectedShapeIndices.Clear();view.SelectedShapeIndices.AddRange(new[]{1,2});view.AddNewAdditionalShapeAnimation(AnimationType.FadeIn);view.AddNewAdditionalShapeAnimation(AnimationType.FlyOut);
  foreach(int index in new[]{1,2})Check(first.Animations.Count(a=>a.ShapeId==first.Shapes[index].Id)>=3,"Additional animations append for every marquee-selected object");
  view.SelectedShapeIndices.Clear();view.SelectedShapeIndex=first.Shapes.IndexOf(title);view.IsInlineEditing=true;view.EditingShape=title;view.EditingTextBuffer=title.Text;view.TextSelectionStart=0;view.TextSelectionLength=4;view.AddNewAdditionalShapeAnimation(AnimationType.FadeIn);
  var selected=first.Animations.Last();Check(selected.TargetCharacterStart==0&&selected.TargetCharacterEnd==4,"Text selection adds an independent character-range animation");
  // Independent character ranges on one line stay at their original positions and re-enter after exit.
  var isolated=new PresentationRoot{CanvasWidth=640,CanvasHeight=360};isolated.Slides.Clear();var page=new SlideNode();isolated.Slides.Add(page);
  var box=new ShapeNode{Type=ShapeType.TextBox,Text="AAAA BBBB",FontName="Arial",FontSize=40,HasFill=false,Transform=new Transform2D{X=80,Y=80,Width=450,Height=120}};
  box.TextRuns=new System.Collections.Generic.List<SlideTextRun>{new SlideTextRun("AAAA ",0xff0000,40,false,"Arial"),new SlideTextRun("BBBB",0x0000ff,40,false,"Arial")};page.Shapes.Add(box);
  ShapeAnimation Effect(AnimationType type,int start,int end)=>new ShapeAnimation{ShapeId=box.Id,Type=type,TargetCharacterStart=start,TargetCharacterEnd=end,Trigger=AnimationTrigger.OnClick,Duration=.5f};
  page.Animations.Add(Effect(AnimationType.FadeIn,0,4));page.Animations.Add(Effect(AnimationType.FadeIn,5,9));page.Animations.Add(Effect(AnimationType.FadeOut,5,9));page.Animations.Add(Effect(AnimationType.FadeIn,5,9));
  view.Presentation=isolated;view.ActiveSlideIndex=0;view.IsInlineEditing=false;
  int Pixels(Bitmap image,bool red){int count=0;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){var c=image.GetPixel(x,y);if(red?c.R>150&&c.G<100&&c.B<100:c.B>150&&c.R<100&&c.G<100)count++;}return count;}
  using(var frame=Render(view,page,1,1,true)){Check(Pixels(frame,true)>30&&Pixels(frame,false)==0,"First click shows only the first selected text span");frame.Save("output/paragraph-connectors/text-step1.png");}
  using(var frame=Render(view,page,2,1,true))Check(Pixels(frame,true)>30&&Pixels(frame,false)>30,"Second click adds the second span without losing the first");
  using(var frame=Render(view,page,3,1,true))Check(Pixels(frame,true)>30&&Pixels(frame,false)==0,"Exit hides only its targeted text");
  using(var frame=Render(view,page,4,1,true))Check(Pixels(frame,false)>30,"A later entrance on the same text can show it again");
  foreach(bool from in new[]{false,true})
  {
   using var frame=new Bitmap(960,540);using(var graphics=Graphics.FromImage(frame))using(var g=new D2DGraphics(graphics,960,540))
    typeof(SlidesView).GetMethod("DrawSlideFull",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{g,new RectangleF(0,0,960,540),page,1.5f,1f,from});
   Check(from?Pixels(frame,true)>30&&Pixels(frame,false)>30:Pixels(frame,true)==0&&Pixels(frame,false)==0,"Transition capture keeps the complete outgoing slide and the incoming slide's pre-click text state");
  }
  box.StrokeWidth=2;box.StrokeColor=0x00a000;box.StrokeOpacity=.4f;
  PptxHandler.SavePptx("output/paragraph-connectors/text-ranges.pptx",isolated);var saved=PptxHandler.LoadPptx("output/paragraph-connectors/text-ranges.pptx");
  Check(saved.Slides[0].Animations.Select(a=>(a.TargetCharacterStart,a.TargetCharacterEnd,a.Type,a.Trigger)).SequenceEqual(page.Animations.Select(a=>(a.TargetCharacterStart,a.TargetCharacterEnd,a.Type,a.Trigger))),"Selected character ranges and repeated entrance/exit effects survive PPTX save and reload");
  Check(Math.Abs(saved.Slides[0].Shapes[0].StrokeOpacity-.4f)<.001f,"Border transparency stays independent from the transparent text-box fill");
  var clone=box.Clone();Check(clone.StrokeOpacity==box.StrokeOpacity,"Duplicating a text box retains its border opacity");
  Console.WriteLine("PASS: theme frames, thirteen nested bullets, elbow routing, multiple animations on a text box, paragraph and character targeting, multi-object append, PPTX metadata roundtrip and real slide renders");
 }
}
