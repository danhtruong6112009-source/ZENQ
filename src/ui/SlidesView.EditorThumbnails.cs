using System;using System.Collections.Generic;using System.Drawing;using ZenQOffice.Core;using Vortice.Direct2D1;
namespace ZenQOffice.UI { public partial class SlidesView {
 bool editorUsesGpuThumbnails;
 const int MaxEditorGpuThumbnails = 128;
 ulong _thumbnailAccessSeq;
 ulong _thumbnailPaintPass;
 sealed class EditorThumbnail:IDisposable { public FlowLayer Layer;public long Revision;public ulong LastAccessSeq;public ulong LastPaintPass;public void Dispose(){if(Layer?.Surface!=null)D2DGraphics.ReleaseRenderTargetCaches(Layer.Surface.NativePointer);Layer?.Dispose();} }
 readonly Dictionary<SlideNode,EditorThumbnail> editorThumbnails=new();
 ID2D1RenderTarget editorThumbnailTarget;float editorThumbnailWidth,editorThumbnailHeight;
 void ClearEditorThumbnails(){foreach(var item in editorThumbnails.Values)item.Dispose();editorThumbnails.Clear();editorThumbnailTarget=null;}
 void EvictOldestEditorThumbnailIfNeeded(SlideNode keepSlide){
  while(editorThumbnails.Count>MaxEditorGpuThumbnails){
   SlideNode victim=null;ulong minSeq=ulong.MaxValue;
   foreach(var kv in editorThumbnails){
    if(ReferenceEquals(kv.Key,keepSlide)||kv.Value.LastPaintPass==_thumbnailPaintPass)continue;
    if(kv.Value.LastAccessSeq<minSeq){minSeq=kv.Value.LastAccessSeq;victim=kv.Key;}
   }
   if(victim==null)break;
   editorThumbnails[victim].Dispose();
   editorThumbnails.Remove(victim);
  }
 }
 void DrawEditorThumbnail(D2DGraphics g,SlideNode slide,Rectangle rect){
  if(slide==null)return;
  if(!editorUsesGpuThumbnails){EnsureVisibleSlideThumbnail(slide);if(slide.ThumbnailCache!=null)g.DrawImage(slide.ThumbnailCache,rect);return;}
  float canvasW=Presentation?.CanvasWidth??1920,canvasH=Presentation?.CanvasHeight??1080;
  if(!ReferenceEquals(editorThumbnailTarget,g.RenderTarget)||editorThumbnailWidth!=canvasW||editorThumbnailHeight!=canvasH){ClearEditorThumbnails();editorThumbnailTarget=g.RenderTarget;editorThumbnailWidth=canvasW;editorThumbnailHeight=canvasH;}
  editorThumbnails.TryGetValue(slide,out var cached);
  if(cached==null||cached.Revision!=slide.ThumbnailRevision){
   if(thumbnailPaintBudget>0){
    thumbnailPaintBudget--;int width=240,height=Math.Max(24,(int)Math.Round(width*(double)canvasH/Math.Max(1,canvasW)));float scale=width/(float)Math.Max(1,canvasW);
    var layer=CaptureFlowLayer(g,new RectangleF(0,0,width,height),drawing=>{
     drawing.Clear(Color.White);DrawSlideBackground(drawing,new Rectangle(0,0,width,height),slide);
     if(slide.Shapes!=null){
      foreach(var shape in slide.Shapes){
       if(shape==null||!shape.IsVisible||(shape.Name??"").StartsWith("__morph_ghost__"))continue;
       if(shape.Type==ShapeType.SlideZoom||shape.Type==ShapeType.SummaryZoom||shape.Type==ShapeType.SectionZoom){
        float zw=shape.Transform.Width*scale,zh=shape.Transform.Height*scale,zx=shape.Transform.X*scale,zy=shape.Transform.Y*scale;
        using(SolidBrush zbg=new SolidBrush(Color.FromArgb(241,245,249)))using(Pen zp=new Pen(Color.FromArgb(203,213,225),1f)){drawing.FillRectangle(zbg,new RectangleF(zx,zy,zw,zh));drawing.DrawRectangle(zp,zx,zy,zw,zh);}
        continue;
       }
       try{
        var snapshot=shape.CreateRenderSnapshot();snapshot.NativeGifFrameIndex=0;snapshot.IsGifPlaying=false;if(IsAnimatedGif(shape))snapshot.ImagePath="";
        try{DrawShape(drawing,0,0,scale,snapshot,false);}finally{snapshot.InvalidateFeatherCache();}
       }catch{}
      }
     }
    });
    cached?.Dispose();cached=new EditorThumbnail{Layer=layer,Revision=slide.ThumbnailRevision,LastAccessSeq=++_thumbnailAccessSeq,LastPaintPass=_thumbnailPaintPass};editorThumbnails[slide]=cached;
    EvictOldestEditorThumbnailIfNeeded(slide);
   }else{
    _hasPendingEditorThumbnails=true;
   }
  }
  if(cached!=null){cached.LastAccessSeq=++_thumbnailAccessSeq;cached.LastPaintPass=_thumbnailPaintPass;cached.Layer.Draw(g,rect);}
  else if(slide.ThumbnailCache!=null){g.DrawImage(slide.ThumbnailCache,rect);}
 }
}}
