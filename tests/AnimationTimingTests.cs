using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Reflection;
using ZenQOffice.Core;
using ZenQOffice.UI;
using ZenQOffice.Format;
class AnimationTimingTests
{
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Near(float a,float b,string name)=>Check(Math.Abs(a-b)<.003f,name+": "+a+" vs "+b);
 static ShapeAnimation Anim(string id,AnimationTrigger trigger,float duration,float delay=0)=>new ShapeAnimation{ShapeId=id,Trigger=trigger,Type=AnimationType.FadeIn,Duration=duration,Delay=delay};
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/animation-timing");
  var deck=PptxHandler.LoadPptx("tests/fixtures/import-web-form.pptx");var real=deck.Slides[0];
  Check(real.Animations.Count==5,"Real slide imports five effects once, without helper set/visibility nodes");
  Check(real.Animations.All(a=>a.Type==AnimationType.SplitIn&&a.Direction==TransitionDirection.Vertical),"Preset 16/21 is Split Vertical In, not Wipe");
  AnimationTimeline.Build(real,out var starts,out var durations,out var steps);
  Check(steps.SequenceEqual(new[]{1,1,1,2,2}),"Real PPTX retains two click groups with concurrent objects");
  Check(durations.All(d=>Math.Abs(d-.5f)<.001f),"Behavior nodes provide the original duration");
  PptxHandler.SavePptx("output/animation-timing/real-roundtrip.pptx",deck);
  var reopened=PptxHandler.LoadPptx("output/animation-timing/real-roundtrip.pptx").Slides[0];
  Check(reopened.Animations.Count==5,"Roundtrip retains all effects");
  AnimationTimeline.Build(reopened,out var rtStarts,out var rtDurations,out var rtSteps);
  Check(rtSteps.SequenceEqual(steps)&&reopened.Animations.All(a=>a.SourcePresetId==16&&a.SourcePresetSubtype==21),"Roundtrip preserves click/with grouping and effect subtype");
  var slide=new SlideNode();
  for(int i=0;i<6;i++)slide.Shapes.Add(new ShapeNode{Id="shape"+i,Type=ShapeType.Rectangle,Transform=new Transform2D{X=20+i*60,Y=80,Width=50,Height=50},FillColor=0x2266cc});
  slide.Animations.Add(Anim("shape0",AnimationTrigger.WithPrevious,1,.2f));
  slide.Animations.Add(Anim("shape1",AnimationTrigger.WithPrevious,2,.1f));
  slide.Animations.Add(Anim("shape2",AnimationTrigger.AfterPrevious,.5f,.4f));
  slide.Animations.Add(Anim("shape3",AnimationTrigger.OnClick,.9f,.75f));
  slide.Animations.Add(Anim("shape4",AnimationTrigger.WithPrevious,.6f,.2f));
  slide.Animations.Add(Anim("shape5",AnimationTrigger.AfterPrevious,.2f,.3f));
  AnimationTimeline.Build(slide,out starts,out durations,out steps);
  Check(steps.SequenceEqual(new[]{0,0,0,1,1,1}),"Automatic slide entry is step zero, not an extra click");
  foreach(var pair in new[]{(0,.2f),(1,.3f),(2,2.7f),(3,.75f),(4,.95f),(5,1.85f)})Near(starts[pair.Item1],pair.Item2,"Start offset "+pair.Item1);
  var preview=AnimationTimeline.PreviewStarts(slide,out float total);
  Near(preview[3],3.95f,"Preview advances click groups after each group's longest effect");
  var view=new SlidesView{Presentation=new PresentationRoot{CanvasWidth=640,CanvasHeight=360},IsPresenting=true,CurrentAnimationStep=1,IsPlayingSlideAnimation=true};view.Presentation.Slides.Clear();view.Presentation.Slides.Add(slide);view.ActiveSlideIndex=0;
  var previewShape=slide.Shapes[1];
  view.GetShapeAnimationTransform(previewShape,slide,0,false,true,0,.8f/total,out bool visible,out float x,out float y,out float sx,out float sy,out float rot,out float alpha);
  Near(alpha,.25f,"With Previous plays concurrently in full preview");
  PptxHandler.SavePptx("output/animation-timing/timing-roundtrip.pptx",view.Presentation);
  var restored=PptxHandler.LoadPptx("output/animation-timing/timing-roundtrip.pptx").Slides[0];AnimationTimeline.Build(restored,out rtStarts,out rtDurations,out rtSteps);
  Check(rtSteps.SequenceEqual(steps),"Automatic and OnClick groups survive PPTX roundtrip");
  for(int i=0;i<starts.Length;i++){Near(rtStarts[i],starts[i],"Roundtrip start "+i);Near(rtDurations[i],durations[i],"Roundtrip duration "+i);}
  Check(restored.Animations[3].Trigger==AnimationTrigger.OnClick,"A delayed click remains OnClick");
  // Independent OpenXML input: duration is nested inside cBhvr, not on the preset wrapper.
  var xml=new System.Xml.XmlDocument();xml.LoadXml("<p:sld xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main'><p:timing><p:tnLst><p:par><p:cTn presetClass='entr' presetID='22' presetSubtype='8' nodeType='clickEffect'><p:stCondLst><p:cond delay='700'/></p:stCondLst><p:childTnLst><p:set><p:cBhvr><p:cTn dur='1'/><p:tgtEl><p:spTgt spid='20'/></p:tgtEl></p:cBhvr></p:set><p:animEffect filter='wipe(left)'><p:cBhvr><p:cTn dur='2500'/><p:tgtEl><p:spTgt spid='20'/></p:tgtEl></p:cBhvr></p:animEffect></p:childTnLst></p:cTn></p:par></p:tnLst></p:timing></p:sld>");
  var synthetic=new SlideNode();synthetic.Shapes.Add(new ShapeNode{Id="target",RawSpid="20"});
  typeof(PptxHandler).GetMethod("ParseSlideAnimations",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{xml,synthetic,640f,360f,null});
  var imported=synthetic.Animations.Single();Near(imported.Duration,2.5f,"Visibility helper cannot override the 2.5 second behavior");Near(imported.Delay,.7f,"Delayed click remains delayed click");
  Check(imported.Type==AnimationType.WipeIn&&imported.Direction==TransitionDirection.FromLeft&&imported.Trigger==AnimationTrigger.OnClick,"Independent native preset/filter/start semantics");
  // Independent curved path keeps its normalized coordinates and samples the actual cubic.
  var pathType=typeof(PptxHandler).Assembly.GetType("ZenQOffice.Format.PptxMotionPath");
  var pathPoints=(System.Collections.Generic.List<PointF>)pathType.GetMethod("Parse",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{"M 0 0 C .1 0 .1 .2 .3 .2 E",640f,360f});
  Near(pathPoints.Last().X,192,"Native path x is relative to slide width");Near(pathPoints.Last().Y,72,"Native path y is relative to slide height");Check(pathPoints.Count>25,"Cubic motion retains its curved trajectory");
  // Interactive groups never add a normal slide click and survive export/import.
  var triggered=Anim("shape0",AnimationTrigger.OnClick,.75f,.2f);triggered.TriggerShapeId="shape5";
  var triggeredAfter=Anim("shape1",AnimationTrigger.AfterPrevious,.5f,.3f);triggeredAfter.TriggerShapeId="shape5";
  slide.Animations.Add(triggered);slide.Animations.Add(triggeredAfter);
  AnimationTimeline.Build(slide,out var ts,out var td,out var ti);Near(ts[7],1.25f,"Interactive After Previous start");Check(ti[6]==-1&&ti[7]==-1,"Interactive effects do not consume slide clicks");
  PptxHandler.SavePptx("output/animation-timing/trigger-roundtrip.pptx",view.Presentation);
  var triggerRestored=PptxHandler.LoadPptx("output/animation-timing/trigger-roundtrip.pptx").Slides[0];
  Check(triggerRestored.Animations.Count(a=>!string.IsNullOrEmpty(a.TriggerShapeId))==2,"Shape click triggers survive native PPTX roundtrip");
  AnimationTimeline.Build(triggerRestored,out ts,out td,out ti);Near(ts[7],1.25f,"Interactive nested timing roundtrip");
  var target=slide.Shapes[0];slide.Animations.Clear();slide.Animations.Add(new ShapeAnimation{ShapeId=target.Id,Type=AnimationType.SplitIn,Direction=TransitionDirection.Vertical,SourcePresetSubtype=21,Trigger=AnimationTrigger.OnClick,Duration=1});
  view.GetShapeAnimationTransform(target,slide,1,true,false,-1,.5f,out visible,out x,out y,out sx,out sy,out rot,out alpha);
  Near(sx,1,"Split must clip content without squashing it");Near(sy,1,"Split retains height");Near(alpha,1,"Split is a reveal rather than a fade");
  var maskMethod=typeof(SlidesView).GetMethod("CreateObjectAnimationMask",BindingFlags.Static|BindingFlags.NonPublic);
  using(var path=(System.Drawing.Drawing2D.GraphicsPath)maskMethod.Invoke(null,new object[]{slide.Animations[0],.5f,200f,100f}))
   Check(path.IsVisible(20,50)&&!path.IsVisible(100,50)&&path.IsVisible(180,50),"Vertical In reveals both outside halves toward the center");
  var grow=new ShapeAnimation{Type=AnimationType.GrowShrink,GrowScaleX=1.6f,GrowScaleY=1.3f,Duration=.5f,AutoReverse=true,Trigger=AnimationTrigger.OnClick,ShapeId=target.Id};
  SlidesView.EvaluateAnimationEffect(grow,1,out visible,out x,out y,out sx,out sy,out rot,out alpha);Near(sx,1.6f,"Grow retains requested final width rather than behaving as Pulse");
  var spin=new ShapeAnimation{Type=AnimationType.Spin,RotationDegrees=75,Duration=.25f,Trigger=AnimationTrigger.AfterPrevious,ShapeId=target.Id};
  SlidesView.EvaluateAnimationEffect(spin,.5f,out visible,out x,out y,out sx,out sy,out rot,out alpha);Near(rot,37.5f,"Spin keeps imported rotation amount");
  slide.Animations.Clear();slide.Animations.Add(grow);slide.Animations.Add(spin);
  AnimationTimeline.Build(slide,out ts,out td,out ti);Near(td[0],1f,"Auto Reverse doubles the total cycle");Near(ts[1],1f,"After Previous waits for the reverse phase too");
  PptxHandler.SavePptx("output/animation-timing/emphasis-roundtrip.pptx",view.Presentation);
  var emphasis=PptxHandler.LoadPptx("output/animation-timing/emphasis-roundtrip.pptx").Slides[0];Check(emphasis.Animations[0].AutoReverse,"Auto Reverse survives PPTX");Near(emphasis.Animations[0].GrowScaleX,1.6f,"Grow amount survives PPTX");Near(emphasis.Animations[1].RotationDegrees,75,"Rotation survives PPTX");
  slide.Animations.Clear();slide.Animations.Add(Anim(target.Id,AnimationTrigger.WithPrevious,1));
  view.CurrentAnimationStep=0;view.IsPlayingSlideAnimation=false;view.UpdateAnimationAndTransition();Check(view.CurrentAnimationStep==0&&view.IsPlayingSlideAnimation,"Slide-entry effects start without consuming a click");
  view.Presentation.Slides[0]=real;view.CurrentAnimationStep=1;view.IsPlayingSlideAnimation=true;
  view.AdvancePresenterStep();Check(view.CurrentAnimationStep==1&&!view.IsPlayingSlideAnimation,"Click while running finishes the current group without skipping the next one");
  view.AdvancePresenterStep();Check(view.CurrentAnimationStep==2&&view.IsPlayingSlideAnimation,"Next click starts the next original PPTX group");
  using(var bitmap=new Bitmap(280,620))
  {
   using(var graphics=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(graphics,280,620))
   using(var f=new Font("Segoe UI",9))using(var b=new Font("Segoe UI",9,FontStyle.Bold))
   {
    g.Clear(Color.White);view.Presentation.Slides[0]=real;
    typeof(SlidesView).GetMethod("DrawAnimationPaneContent",Private).Invoke(view,new object[]{g,0,10,280,610,b,f,b});
   }
   Check(bitmap.GetPixel(5,5).R>240,"Pane rendering actually reaches the output bitmap");
   bitmap.Save("output/animation-timing/pane.png");
  }
  Console.WriteLine("PASS: real PowerPoint presets/subtypes, nested behavior durations, automatic/click/with/after scheduling, simultaneous preview, delays, PPTX roundtrip, unsquashed Split mask, animation pane rendering");
 }
}
