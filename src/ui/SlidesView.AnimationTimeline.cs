using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZenQOffice.Core;
namespace ZenQOffice.UI
{
 public partial class SlidesView
 {
  private SlideNode _automaticAnimationSlide;
  private static string GetAnimationDisplayName(AnimationType type)=>type switch
  {
   AnimationType.Appear=>"Xuất hiện",AnimationType.Disappear=>"Biến mất",AnimationType.FadeIn or AnimationType.FadeOut=>"Mờ dần",
   AnimationType.FlyIn or AnimationType.FlyOut=>"Bay",AnimationType.FloatIn or AnimationType.FloatOut=>"Trôi",
   AnimationType.SplitIn or AnimationType.SplitOut=>"Tách đôi",AnimationType.WipeIn or AnimationType.WipeOut=>"Quét",
   AnimationType.ShapeIn or AnimationType.ShapeOut=>"Hình dạng",AnimationType.WheelIn or AnimationType.WheelOut=>"Bánh xe",
   AnimationType.RandomBarsIn or AnimationType.RandomBarsOut=>"Thanh ngẫu nhiên",AnimationType.ZoomIn or AnimationType.ZoomOut=>"Thu phóng",
   AnimationType.Swivel or AnimationType.SwivelOut=>"Xoay",AnimationType.Bounce or AnimationType.BounceOut=>"Nảy",
   AnimationType.Spin=>"Quay tròn",AnimationType.GrowShrink=>"Phóng / thu nhỏ",AnimationType.Pulse=>"Nhịp",_=>type.ToString()
  };
  private static GraphicsPath CreateObjectAnimationMask(ShapeAnimation anim,float progress,float w,float h)
  {
   if(anim==null)return null;
   bool masked=anim.Type is AnimationType.WipeIn or AnimationType.WipeOut or AnimationType.SplitIn or AnimationType.SplitOut or AnimationType.ShapeIn or AnimationType.ShapeOut or AnimationType.WheelIn or AnimationType.WheelOut or AnimationType.RandomBarsIn or AnimationType.RandomBarsOut;
   if(!masked)return null;
   float p=Math.Clamp(anim.IsExit?1-progress:progress,0,1);var path=new GraphicsPath(FillMode.Winding);
   if(p<=0){path.AddRectangle(new RectangleF(0,0,.00001f,.00001f));return path;}
   if(p>=1){path.AddRectangle(new RectangleF(0,0,w,h));return path;}
   if(anim.Type is AnimationType.WheelIn or AnimationType.WheelOut)
   {
    float diameter=(float)Math.Sqrt(w*w+h*h)*2;
    int spokes=anim.SourcePresetId==21&&anim.SourcePresetSubtype>0?Math.Clamp(anim.SourcePresetSubtype,1,8):4;
    for(int i=0;i<spokes;i++)path.AddPie((w-diameter)/2,(h-diameter)/2,diameter,diameter,-90+i*360f/spokes,p*360f/spokes);
   }
   else if(anim.Type is AnimationType.RandomBarsIn or AnimationType.RandomBarsOut)
   {
    bool horizontal=anim.Direction!=TransitionDirection.Vertical;
    for(int i=0;i<32;i++)
    {
     float amount=Math.Clamp((p-((i*13+7)%32)/32f)*8,0,1);if(amount<=0)continue;
     path.AddRectangle(horizontal?new RectangleF(0,h*i/32f,w*amount,h/32f+.5f):new RectangleF(w*i/32f,0,w/32f+.5f,h*amount));
    }
   }
   else if(anim.Type is AnimationType.SplitIn or AnimationType.SplitOut)
   {
    bool horizontal=anim.Direction==TransitionDirection.Horizontal;bool inward=(anim.SourcePresetSubtype&32)==0;
    if(inward)
    {
     if(horizontal){path.AddRectangle(new RectangleF(0,0,w,h*p/2));path.AddRectangle(new RectangleF(0,h-h*p/2,w,h*p/2));}
     else{path.AddRectangle(new RectangleF(0,0,w*p/2,h));path.AddRectangle(new RectangleF(w-w*p/2,0,w*p/2,h));}
    }
    else path.AddRectangle(horizontal?new RectangleF(0,h*(1-p)/2,w,h*p):new RectangleF(w*(1-p)/2,0,w*p,h));
   }
   else if(anim.Type is AnimationType.ShapeIn or AnimationType.ShapeOut)
   {
    bool inward=anim.SourcePresetSubtype==16;
    if(inward){path.FillMode=FillMode.Alternate;path.AddRectangle(new RectangleF(0,0,w,h));p=1-p;}
    if(anim.SourcePresetId==4)path.AddRectangle(new RectangleF(w*(1-p)/2,h*(1-p)/2,w*p,h*p));
    else if(anim.SourcePresetId==8)path.AddPolygon(new[]{new PointF(w/2,h/2-h*p),new PointF(w/2+w*p,h/2),new PointF(w/2,h/2+h*p),new PointF(w/2-w*p,h/2)});
    else if(anim.SourcePresetId==13)
    {
     float a=w*(1-p)/2,b=h*(1-p)/2;
     path.AddPolygon(new[]{new PointF(a,0),new PointF(w-a,0),new PointF(w-a,b),new PointF(w,b),new PointF(w,h-b),new PointF(w-a,h-b),new PointF(w-a,h),new PointF(a,h),new PointF(a,h-b),new PointF(0,h-b),new PointF(0,b),new PointF(a,b)});
    }
    else {float d=(float)Math.Sqrt(w*w+h*h)*p;path.AddEllipse((w-d)/2,(h-d)/2,d,d);}
   }
   else path.AddRectangle(anim.Direction switch{TransitionDirection.FromLeft=>new RectangleF(0,0,w*p,h),TransitionDirection.FromRight=>new RectangleF(w*(1-p),0,w*p,h),TransitionDirection.FromTop=>new RectangleF(0,0,w,h*p),_=>new RectangleF(0,h*(1-p),w,h*p)});
   return path;
  }
  private void EvaluateScheduledAnimations(ShapeNode shape,SlideNode slide,int currentStep,bool preview,float progress,
   out bool visible,out float x,out float y,out float sx,out float sy,out float rotation,out float opacity,out ShapeAnimation active,out float activeProgress, int paragraph=-1, int charStart=-1, int charEnd=-1)
  {
   AnimationTimeline.Build(slide,out var starts,out var durations,out var steps);
   float previewTotal=0;var previewStarts=preview?AnimationTimeline.PreviewStarts(slide,out previewTotal):null;
   float elapsed=progress*(preview?previewTotal:GetCurrentStepMaxDuration(slide,currentStep));
   visible=true;x=y=rotation=0;sx=sy=opacity=1;active=null;activeProgress=0;bool seenEntrance=false;
   for(int i=0;i<slide.Animations.Count;i++)
   {
    var a=slide.Animations[i];if(a.ShapeId!=shape.Id)continue;
    bool Matches(ShapeAnimation candidate)=>candidate.TargetParagraphStart>=0?paragraph>=candidate.TargetParagraphStart&&paragraph<=Math.Max(candidate.TargetParagraphStart,candidate.TargetParagraphEnd):candidate.TargetCharacterStart>=0&&charStart>=candidate.TargetCharacterStart&&charEnd<=candidate.TargetCharacterEnd;
    if(paragraph<0&&a.TargetsText)continue;
    if(paragraph>=0)
    {
     bool hasTarget=slide.Animations.Exists(candidate=>candidate.ShapeId==shape.Id&&Matches(candidate));
     if(hasTarget?(!Matches(a)&&!( !a.TargetsText&&(a.IsExit||a.IsEmphasis||a.IsMotionPath))):a.TargetsText)continue;
    }
    if(a.IsEntrance&&!seenEntrance){visible=false;opacity=0;seenEntrance=true;}
    bool started;float p;
    if(!string.IsNullOrEmpty(a.TriggerShapeId))
    {
     if(!_completedTriggeredShapeIds.Contains(a.TriggerShapeId))continue;
     bool playing=IsPlayingTriggeredAnimation&&_activeTriggeredAnimIndices.Contains(i);float length=0;
     foreach(int index in _activeTriggeredAnimIndices)length=Math.Max(length,starts[index]+durations[index]);
     float time=TriggeredAnimationProgress*Math.Max(.001f,length);
     started=!playing||time>=starts[i];p=playing?Math.Clamp((time-starts[i])/durations[i],0,1):1;
    }
    else if(preview){started=elapsed>=previewStarts[i];p=Math.Clamp((elapsed-previewStarts[i])/durations[i],0,1);}
    else
    {
     bool pending=steps[i]==0&&_automaticAnimationSlide!=slide;
     started=steps[i]<currentStep||steps[i]==currentStep&&!pending&&(!IsPlayingSlideAnimation||elapsed>=starts[i]);
     p=steps[i]<currentStep||!IsPlayingSlideAnimation?1:Math.Clamp((elapsed-starts[i])/durations[i],0,1);
    }
    if(!started)continue;
    if(a.AutoReverse)p=p<=.5f?p*2:(1-p)*2;
    EvaluateAnimationEffect(a,p,out bool v,out float ox,out float oy,out float ax,out float ay,out float r,out float alpha);
    if(a.Type is AnimationType.FlyIn or AnimationType.FlyOut)
    {
     float amount=a.IsExit?p*p*p:(1-p)*(1-p)*(1-p);
     var dir=a.Direction;
     if(a.SourceAnimationType==a.Type&&a.SourceDirection==a.Direction&&a.SourcePresetId==2&&a.SourcePresetSubtype is 3 or 6 or 9 or 12)
     {
      bool left=a.SourcePresetSubtype is 9 or 12,up=a.SourcePresetSubtype is 3 or 9;
      ox=(left?-(shape.Transform.X+shape.Transform.Width):(Presentation.CanvasWidth-shape.Transform.X))*amount;
      oy=(up?-(shape.Transform.Y+shape.Transform.Height):(Presentation.CanvasHeight-shape.Transform.Y))*amount;
     }
     else
     {
      ox=dir==TransitionDirection.FromLeft?-(shape.Transform.X+shape.Transform.Width)*amount:dir==TransitionDirection.FromRight?(Presentation.CanvasWidth-shape.Transform.X)*amount:0;
      oy=dir==TransitionDirection.FromTop?-(shape.Transform.Y+shape.Transform.Height)*amount:dir==TransitionDirection.FromBottom?(Presentation.CanvasHeight-shape.Transform.Y)*amount:0;
     }
    }
    if(a.IsEntrance){visible=v;opacity=alpha;}else{visible&=v;opacity*=alpha;}
    x+=ox;y+=oy;sx*=ax;sy*=ay;rotation+=r;active=a;activeProgress=p;
   }
  }
 }
}
