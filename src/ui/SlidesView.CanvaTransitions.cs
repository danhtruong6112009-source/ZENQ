using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using ZenQOffice.Core;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using Rectangle=System.Drawing.Rectangle;
using Size=System.Drawing.Size;
using Color=System.Drawing.Color;
namespace ZenQOffice.UI
{
 public partial class SlidesView
 {
  static float CanvaEase(float t)=>(1-(float)Math.Cos(Math.PI*Math.Clamp(t,0,1)))*.5f;
  static PointF CanvaVector(TransitionDirection direction)=>direction switch
  {
   TransitionDirection.FromLeft=>new PointF(1,0),TransitionDirection.FromTop=>new PointF(0,1),TransitionDirection.FromBottom=>new PointF(0,-1),
   TransitionDirection.FromTopLeft=>new PointF(.70710678f,.70710678f),TransitionDirection.FromTopRight=>new PointF(-.70710678f,.70710678f),
   TransitionDirection.FromBottomLeft=>new PointF(.70710678f,-.70710678f),TransitionDirection.FromBottomRight=>new PointF(-.70710678f,-.70710678f),_=>new PointF(-1,0)
  };
  static float Dot(PointF point,PointF direction)=>point.X*direction.X+point.Y*direction.Y;
  static PointF[] Corners(RectangleF r)=>new[]{new PointF(r.Left,r.Top),new PointF(r.Right,r.Top),new PointF(r.Right,r.Bottom),new PointF(r.Left,r.Bottom)};
  static GraphicsPath CanvaBand(RectangleF rectangle,PointF direction,float min,float max)
  {
   var points=Corners(rectangle).ToList();
   void Clip(float edge,bool above)
   {
    var clipped=new List<PointF>();if(points.Count==0)return;PointF previous=points[^1];float prev=Dot(previous,direction)-edge;bool wasInside=above?prev>=0:prev<=0;
    foreach(var current in points)
    {
     float value=Dot(current,direction)-edge;bool inside=above?value>=0:value<=0;
     if(inside!=wasInside){float t=prev/(prev-value);clipped.Add(new PointF(previous.X+(current.X-previous.X)*t,previous.Y+(current.Y-previous.Y)*t));}
     if(inside)clipped.Add(current);previous=current;prev=value;wasInside=inside;
    }
    points=clipped;
   }
   Clip(min,true);Clip(max,false);var path=new GraphicsPath();if(points.Count>=3)path.AddPolygon(points.ToArray());return path;
  }
  readonly Dictionary<Bitmap,int[]> automaticWipePalettes=new();
  public List<int> GetTransitionPalette(SlideNode slide){
   using var preview=RenderSlideToBitmap(slide,128,72);var colors=ResolveColorWipePalette(slide,preview).ToList();if(preview!=null)automaticWipePalettes.Remove(preview);return colors;
  }
  private int[] ResolveColorWipePalette(SlideNode slide,Bitmap bitmap){
   var specified=slide?.TransitionColors;int[] legacy={0xCFBDA8,0xB18D6C,0xDACBB8,0xE6D9C5};
   if(specified?.Count>=4&&!specified.Take(4).SequenceEqual(legacy))return specified.Take(4).ToArray();
   if(bitmap!=null&&automaticWipePalettes.TryGetValue(bitmap,out var existing))return existing;
   var bins=new Dictionary<int,(int count,long r,long g,long b)>();
   if(bitmap!=null)for(int y=0;y<bitmap.Height;y+=Math.Max(1,bitmap.Height/36))for(int x=0;x<bitmap.Width;x+=Math.Max(1,bitmap.Width/64)){
    var c=bitmap.GetPixel(x,y);int key=(c.R>>5)<<6|(c.G>>5)<<3|(c.B>>5);bins.TryGetValue(key,out var v);bins[key]=(v.count+1,v.r+c.R,v.g+c.G,v.b+c.B);
   }
   var result=new List<int>();foreach(var v in bins.Values.OrderByDescending(v=>v.count)){
    int r=(int)(v.r/v.count),g=(int)(v.g/v.count),b=(int)(v.b/v.count),rgb=r<<16|g<<8|b;
    if(result.All(c=>Math.Pow((c>>16&255)-r,2)+Math.Pow((c>>8&255)-g,2)+Math.Pow((c&255)-b,2)>2500))result.Add(rgb);if(result.Count==4)break;
   }
   if(result.Count==0)result.Add(slide?.BgColor&0xffffff??0);int sourceCount=result.Count;while(result.Count<4)result.Add(result[result.Count%sourceCount]);
   var colors=result.ToArray();if(bitmap!=null)automaticWipePalettes[bitmap]=colors;return colors;
  }
  private void RenderColorWipeTransition(D2DGraphics g,Rectangle rect,SlideNode from,SlideNode to,float progress,float scale)
  {
   var direction=CanvaVector(to?.TransitionDirection??TransitionDirection.FromRight);var projections=Corners(rect).Select(p=>Dot(p,direction)).ToArray();float min=projections.Min(),length=Math.Max(1,projections.Max()-min);
   DrawCachedSlideBitmap(g,rect,_cachedTransBmpB,1);
   float first=2*length*CanvaEase(progress/.8f);
   using(var ahead=CanvaBand(rect,direction,min+first,min+length))if(ahead.PointCount>0){var state=g.Save();try{g.IntersectMeshClip(ahead);DrawCachedSlideBitmap(g,rect,_cachedTransBmpA,1);}finally{g.Restore(state);}}
   var colors=ResolveColorWipePalette(to,_cachedTransBmpB);
   for(int i=0;i<4;i++)
   {
    float front=2*length*CanvaEase((progress-i/15f)/.8f);
    using var band=CanvaBand(rect,direction,min+front-length,min+front);if(band.PointCount==0)continue;
    int color=colors[i];using var fill=new SolidBrush(Color.FromArgb(255,(color>>16)&255,(color>>8)&255,color&255));g.FillPath(fill,band);
   }
  }
  sealed class FlowLayer:IDisposable
  {
   // Keep the backing surface alive for the bitmap's entire playback lifetime.
   public ID2D1Bitmap Native;public ID2D1BitmapRenderTarget Surface;public Bitmap Managed;public RectangleF Bounds;public float TextDelay;
   public void Draw(D2DGraphics g,RectangleF destination,float opacity=1)
   {
    if(opacity<=0)return;
    if(Native!=null&&g.RenderTarget!=null)g.DrawD2DBitmap(Native,destination,null,opacity);
    else if(Managed!=null)g.DrawImage(Managed,destination.X,destination.Y,destination.Width,destination.Height,opacity);
   }
   public void Dispose(){Native?.Dispose();Surface?.Dispose();if(Managed!=null){D2DGraphics.InvalidateBitmapCache(Managed);Managed.Dispose();}}
  }
  sealed class FlowScene:IDisposable
  {
   public FlowLayer Background;public readonly List<FlowLayer> Items=new();
   public void Dispose(){Background?.Dispose();foreach(var layer in Items)layer.Dispose();}
  }
  readonly Dictionary<(ID2D1RenderTarget,int,int,SlideNode,SlideNode),(FlowScene from,FlowScene to)> flowScenes=new();
  FlowScene flowFrom,flowTo;SlideNode flowFromSlide,flowToSlide;Size flowSize;ID2D1RenderTarget flowTarget;
  void ClearCanvaTransitionCache(){foreach(var pair in flowScenes.Values){pair.from.Dispose();pair.to.Dispose();}flowScenes.Clear();flowFrom=flowTo=null;flowFromSlide=flowToSlide=null;flowSize=Size.Empty;flowTarget=null;}
  FlowLayer CaptureFlowLayer(D2DGraphics destination,RectangleF bounds,Action<D2DGraphics> draw)
  {
   int width=Math.Max(1,(int)Math.Ceiling(bounds.Width)),height=Math.Max(1,(int)Math.Ceiling(bounds.Height));
   if(destination.RenderTarget!=null)
   {
    ID2D1BitmapRenderTarget target=null;
    try
    {
     target=destination.RenderTarget.CreateCompatibleRenderTarget(new SizeI(width,height),new SizeI(width,height),new Vortice.DCommon.PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm,Vortice.DCommon.AlphaMode.Premultiplied));target.SetDpi(96,96);
     using(var drawing=new D2DGraphics(target,width,height)){drawing.Clear(Color.Transparent);drawing.TranslateTransform(-bounds.X,-bounds.Y);draw(drawing);}
     return new FlowLayer{Native=target.Bitmap,Surface=target,Bounds=bounds};
    }
    catch(Exception e){target?.Dispose();System.Diagnostics.Debug.WriteLine("Flow texture fallback: "+e.Message);}
   }
   var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
   try{using(var drawing=D2DGraphics.FromImage(bitmap)){drawing.Clear(Color.Transparent);drawing.TranslateTransform(-bounds.X,-bounds.Y);draw(drawing);}return new FlowLayer{Managed=bitmap,Bounds=bounds};}
   catch{bitmap.Dispose();throw;}
  }
  bool IsFlowBackdrop(ShapeNode shape,float width,float height)=>Math.Abs(shape.Transform.Rotation)<.01f&&shape.Transform.X<=1&&shape.Transform.Y<=1&&shape.Transform.Width>=width-2&&shape.Transform.Height>=height-2&&(shape.Type==ShapeType.Image||(shape.Type==ShapeType.CustomPath&&shape.CachedImage!=null)||(shape.Type==ShapeType.Rectangle&&shape.HasFill&&string.IsNullOrEmpty(shape.Text)&&string.IsNullOrEmpty(shape.SmartArtType)&&shape.ChartData==null));
  void DrawFlowSourceShape(D2DGraphics drawing,ShapeNode shape,SlideNode slide,float scale,bool from)
  {
   bool previous=_drawingScopedText;_drawingScopedText=true;
   try
   {
    if(!from&&(IsPresenting||IsPreviewingTransition))
    {
     if(HasScopedTextAnimations(shape,slide)){DrawScopedTextAnimations(drawing,0,0,scale,shape,slide,0,false,0,1);return;}
     GetShapeAnimationTransform(shape,slide,0,true,false,-1,0,out bool visible,out float x,out float y,out float sx,out float sy,out float rotation,out float opacity,out var effect,out float phase);
     if(visible)DrawShape(drawing,0,0,scale,shape,false,opacity,x,y,sx,sy,rotation,effect,phase);
    }
    else DrawShape(drawing,0,0,scale,shape,false);
   }
   finally{_drawingScopedText=previous;}
  }
  FlowScene CaptureFlowScene(D2DGraphics drawing,SlideNode slide,Rectangle viewport,float scale,bool from)
  {
   var scene=new FlowScene();var area=new RectangleF(0,0,viewport.Width,viewport.Height);float width=Presentation?.CanvasWidth??1920,height=Presentation?.CanvasHeight??1080;
   bool wasBuilding=_isBuildingTransitionCache;_isBuildingTransitionCache=true;
   try
   {
    var items=new List<ShapeNode>();if(slide!=null)foreach(var original in slide.Shapes)
    {
     if(original==null||!original.IsVisible||(original.Name??"").StartsWith("__morph_ghost__"))continue;
     var copy=CreateTransitionShapeSnapshot(original);EnsureTextBoxFitsFontSize(copy);items.Add(copy);
    }
    scene.Background=CaptureFlowLayer(drawing,area,g=>{g.Clear(Color.Black);if(slide!=null){DrawSlideBackground(g,area,slide);foreach(var shape in items)if(IsFlowBackdrop(shape,width,height))DrawFlowSourceShape(g,shape,slide,scale,from);}});
    foreach(var shape in items)
    {
     if(IsFlowBackdrop(shape,width,height))continue;
     var tr=shape.Transform;float w=Math.Abs(tr.Width*scale),h=Math.Abs(tr.Height*scale),angle=tr.Rotation*(float)Math.PI/180;
     float rotatedW=Math.Abs(w*(float)Math.Cos(angle))+Math.Abs(h*(float)Math.Sin(angle)),rotatedH=Math.Abs(w*(float)Math.Sin(angle))+Math.Abs(h*(float)Math.Cos(angle));
     float pad=48*scale+3;
     var bounds=(RectangleF)Rectangle.Ceiling(RectangleF.Intersect(area,new RectangleF((tr.X+tr.Width/2)*scale-rotatedW/2-pad,(tr.Y+tr.Height/2)*scale-rotatedH/2-pad,rotatedW+2*pad,rotatedH+2*pad)));
     if(bounds.Width<=0||bounds.Height<=0)continue;
     var layer=CaptureFlowLayer(drawing,bounds,g=>DrawFlowSourceShape(g,shape,slide,scale,from));
     if(shape.Type==ShapeType.TextBox)layer.TextDelay=.04f+.30f*(1-Math.Min(1,GetMaxRunFontSize(shape)/100f));
     scene.Items.Add(layer);
    }
    return scene;
   }
   catch{scene.Dispose();throw;}
   finally
   {
    _isBuildingTransitionCache=wasBuilding;
    foreach(var captured in _transitionCaptureShapes){captured.Shape.InvalidateFeatherCache();if(!ReferenceEquals(captured.Shape.CachedImage,captured.Original))captured.Shape.CachedImage?.Dispose();}
    _transitionCaptureShapes.Clear();
   }
  }
  private void PrepareCanvaFlowScene(D2DGraphics g,Rectangle rect,SlideNode from,SlideNode to,float scale)
  {
   var target=g.RenderTarget;var key=(target,rect.Width,rect.Height,from,to);
   if(!flowScenes.TryGetValue(key,out var scenes)){
    if(flowScenes.Count>=3)ClearCanvaTransitionCache();
    FlowScene first=null;try{first=CaptureFlowScene(g,from,rect,scale,true);scenes=(first,CaptureFlowScene(g,to,rect,scale,false));flowScenes.Add(key,scenes);}catch{first?.Dispose();throw;}
   }
   flowFrom=scenes.from;flowTo=scenes.to;flowSize=rect.Size;flowTarget=target;flowFromSlide=from;flowToSlide=to;
  }

  private void RenderCanvaFlowTransition(D2DGraphics g,Rectangle rect,SlideNode from,SlideNode to,float progress,float scale)
  {
   PrepareCanvaFlowScene(g,rect,from,to,scale);
   flowFrom.Background.Draw(g,rect);flowTo.Background.Draw(g,rect,CanvaEase(progress));
   var direction=CanvaVector(to?.TransitionDirection??TransitionDirection.FromRight);var corners=Corners(new RectangleF(0,0,rect.Width,rect.Height));float minimum=corners.Min(p=>Dot(p,direction)),length=Math.Max(1,corners.Max(p=>Dot(p,direction))-minimum);
   void Objects(FlowScene scene,bool outgoing)
   {
    foreach(var layer in scene.Items)
    {
     var b=layer.Bounds;float center=Dot(new PointF(b.X+b.Width/2,b.Y+b.Height/2),direction);float order=1-Math.Clamp((center-minimum)/length,0,1);
     float secondary=Math.Abs(direction.X)>=Math.Abs(direction.Y)?(b.Y+b.Height/2)/rect.Height:(b.X+b.Width/2)/rect.Width;secondary=Math.Clamp(secondary,0,1);
     float delay=outgoing?order*.10f+secondary*.045f:Math.Min(.8f,.24f+order*.12f+(1-secondary)*.22f+layer.TextDelay);
     float duration=outgoing?.50f:1-delay;float u=Math.Clamp((progress-delay)/duration,0,1);
     if(outgoing&&u>=1||!outgoing&&u<=0)continue;
     var projected=Corners(b).Select(p=>Dot(p,direction)-minimum).ToArray();
     float distance=outgoing?length-projected.Min()+2:projected.Max()+2;
     float movement=outgoing?CanvaEase(u):-(float)Math.Pow(1-u,3);
     layer.Draw(g,new RectangleF(rect.X+b.X+direction.X*distance*movement,rect.Y+b.Y+direction.Y*distance*movement,b.Width,b.Height));
    }
   }
   Objects(flowFrom,true);Objects(flowTo,false);
  }
 }
}
