using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ZenQOffice.Core;
namespace ZenQOffice.UI
{
 public partial class SlidesView
 {
  public bool ShowAnimationOrderTags { get; set; }
  private readonly List<(RectangleF Bounds,int Index)> _animationOrderTags=new();
  private SlideNode _animationOrderTagSlide;
  private void DrawAnimationOrderTags(D2DGraphics g,float x,float y,float scale,SlideNode slide)
  {
   _animationOrderTags.Clear();
   _animationOrderTagSlide=slide;
   if(!ShowAnimationOrderTags&&! (IsSidebarOpen&&SidebarPanel==SidebarPanelType.AnimationPane)||IsPlayingAnimationPreview||IsPresenting)return;
   AnimationTimeline.Build(slide,out var starts,out var lengths,out var steps);
   var slots=new Dictionary<string,int>();
   using var font=new Font("Segoe UI",8f);
   // Place later effects first so shared rows read from the earliest effect on the left.
   for(int i=slide.Animations.Count-1;i>=0;i--)
   {
    var a=slide.Animations[i];var shape=slide.Shapes.Find(s=>s.Id==a.ShapeId);if(shape==null)continue;
    int paragraph=a.TargetParagraphStart;
    if(paragraph<0&&a.TargetCharacterStart>=0)foreach(var range in SlideTextParagraphs.Ranges(shape.Text))if(a.TargetCharacterStart>=range.Start)paragraph=range.Index;
    float top=y+shape.Transform.Y*scale;
    if(paragraph>=0)
    {
     using var textFont=SlideFontManager.CreateFont(shape.FontName,Math.Max(1,shape.FontSize*scale),shape.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);
     var bounds=GetShapeTextBounds(shape,shape.Transform.Width*scale,shape.Transform.Height*scale,scale);
     var ranges=SlideTextParagraphs.Ranges(shape.Text);var lines=GetParagraphVisualLines(g,shape,shape.Text,textFont,bounds.Width,shape.CharacterSpacing*scale,scale);
     int line=paragraph<ranges.Count?lines.FindIndex(v=>v.StartCharIndex>=ranges[paragraph].Start):0;
     ComputeInlineTextVerticalMetrics(shape,textFont,bounds,Math.Max(1,lines.Count),scale,out float height,out float startY,out _);
     top+=startY+Math.Max(0,line)*height;
    }
    string key=shape.Id+":"+paragraph;slots.TryGetValue(key,out int slot);slots[key]=slot+1;
    var box=new RectangleF(x+shape.Transform.X*scale-25-slot*24,top,22,15);
    while(_animationOrderTags.Exists(tag=>tag.Bounds.IntersectsWith(box)))box.X-=24;
    bool selected=i==SelectedAnimationIndex;using var fill=new SolidBrush(selected?Color.FromArgb(255,221,190):Color.White);using var border=new Pen(selected?Color.FromArgb(220,115,51):Color.FromArgb(165,165,165));
    g.FillRectangle(fill,box);g.DrawRectangle(border,box.X,box.Y,box.Width,box.Height);
    using var text=new SolidBrush(Color.FromArgb(70,70,70));using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
    g.DrawString(steps[i]<0?"↳":steps[i].ToString(),font,text,box,format);_animationOrderTags.Add((box,i));
   }
  }
  private bool _drawingScopedText;
  private static bool HasScopedTextAnimations(ShapeNode shape,SlideNode slide)=>shape!=null&&!string.IsNullOrEmpty(shape.Text)&&slide?.Animations!=null&&slide.Animations.Exists(a=>a.ShapeId==shape.Id&&a.TargetsText);
  private List<VisualLineRunSegment> FilterTextAnimationSegments(D2DGraphics g,ShapeNode shape,List<VisualLineRunSegment> original,int start)
  {
   if(shape.RenderTextStart<0)return original;
   var result=new List<VisualLineRunSegment>();int index=start;
   foreach(var segment in original)
   {
    int end=index+segment.Text.Length;var boundaries=new SortedSet<int>{index,end};
    if(shape.RenderTextStart>index&&shape.RenderTextStart<end)boundaries.Add(shape.RenderTextStart);
    if(shape.RenderTextEnd>index&&shape.RenderTextEnd<end)boundaries.Add(shape.RenderTextEnd);
    var points=boundaries.ToArray();
    for(int i=0;i<points.Length-1;i++)
    {
     var part=segment.Copy();string text=segment.Text.Substring(points[i]-index,points[i+1]-points[i]);part.Width=g.MeasureString(text,part.Font,part.CharacterSpacing).Width;
     bool show=points[i]>=shape.RenderTextStart&&points[i+1]<=shape.RenderTextEnd;
     part.Text=show?text:"";if(!show){part.HasHighlight=false;part.HasGradient=false;part.Color=Color.Transparent;}
     result.Add(part);
    }
    index=end;
   }
   return result;
  }
  private void DrawScopedTextAnimations(D2DGraphics g,float slideX,float slideY,float scale,ShapeNode shape,SlideNode slide=null,int? step=null,bool? preview=null,float? animationProgress=null,float opacityMultiplier=1f)
  {
   slide??=ActiveSlide;if(slide==null)return;_drawingScopedText=true;
   try
   {
    void DrawPart(ShapeNode part,int paragraph=-1,int start=-1,int end=-1)
    {
     EvaluateScheduledAnimations(shape,slide,step??CurrentAnimationStep,preview??IsPlayingAnimationPreview,animationProgress??AnimationProgress,out bool visible,out float x,out float y,out float sx,out float sy,out float rotation,out float opacity,out var active,out float progress,paragraph,start,end);
     if(visible)DrawShape(g,slideX,slideY,scale,part,false,opacity*opacityMultiplier,x,y,sx,sy,rotation,active,progress);
     if(part.CachedVisualLines!=null){shape.CachedVisualLines=part.CachedVisualLines;shape.CachedVisualLinesText=part.CachedVisualLinesText;shape.CachedVisualLinesWidth=part.CachedVisualLinesWidth;shape.CachedVisualLinesFontSize=part.CachedVisualLinesFontSize;shape.CachedVisualLinesCharSpacing=part.CachedVisualLinesCharSpacing;}
    }
    var container=shape.CreateRenderSnapshot();container.Text="";container.TextRuns=null;DrawPart(container);
    foreach(var paragraph in SlideTextParagraphs.Ranges(shape.Text))
    {
     var boundaries=new SortedSet<int>{paragraph.Start,paragraph.End};
     foreach(var animation in slide.Animations)
     {
      if(animation.ShapeId!=shape.Id||animation.TargetCharacterStart<0)continue;
      if(animation.TargetCharacterStart>paragraph.Start&&animation.TargetCharacterStart<paragraph.End)boundaries.Add(animation.TargetCharacterStart);
      if(animation.TargetCharacterEnd>paragraph.Start&&animation.TargetCharacterEnd<paragraph.End)boundaries.Add(animation.TargetCharacterEnd);
     }
     var points=boundaries.ToArray();
     for(int i=0;i<points.Length-1;i++)
     {
      if(points[i]==points[i+1])continue;
      var fragment=shape.CreateRenderSnapshot();fragment.HasFill=false;fragment.HasGradientFill=false;fragment.StrokeWidth=0;fragment.HasShadow=false;
      fragment.RenderTextStart=points[i];fragment.RenderTextEnd=points[i+1];
      fragment.CachedVisualLines=shape.CachedVisualLines;fragment.CachedVisualLinesText=shape.CachedVisualLinesText;fragment.CachedVisualLinesWidth=shape.CachedVisualLinesWidth;fragment.CachedVisualLinesFontSize=shape.CachedVisualLinesFontSize;fragment.CachedVisualLinesCharSpacing=shape.CachedVisualLinesCharSpacing;
      DrawPart(fragment,paragraph.Index,points[i],points[i+1]);
     }
    }
   }
   finally{_drawingScopedText=false;}
  }
 }
}
