using System;using System.Collections.Generic;using System.Drawing;using ZenQOffice.Core;using Vortice.Direct2D1;
namespace ZenQOffice.UI { public partial class SlidesView {
 bool editorUsesGpuThumbnails;
 sealed class EditorThumbnail:IDisposable { public FlowLayer Layer;public long Revision;public void Dispose(){if(Layer?.Surface!=null)D2DGraphics.ReleaseRenderTargetCaches(Layer.Surface.NativePointer);Layer?.Dispose();} }
 readonly Dictionary<SlideNode,EditorThumbnail> editorThumbnails=new();
 ID2D1RenderTarget editorThumbnailTarget;float editorThumbnailWidth,editorThumbnailHeight;
 void ClearEditorThumbnails(){foreach(var item in editorThumbnails.Values)item.Dispose();editorThumbnails.Clear();editorThumbnailTarget=null;}
 void DrawEditorThumbnail(D2DGraphics g,SlideNode slide,Rectangle rect){
  if(!editorUsesGpuThumbnails){EnsureVisibleSlideThumbnail(slide);if(slide.ThumbnailCache!=null)g.DrawImage(slide.ThumbnailCache,rect);return;}
  float canvasW=Presentation?.CanvasWidth??1920,canvasH=Presentation?.CanvasHeight??1080;
  if(!ReferenceEquals(editorThumbnailTarget,g.RenderTarget)||editorThumbnailWidth!=canvasW||editorThumbnailHeight!=canvasH){ClearEditorThumbnails();editorThumbnailTarget=g.RenderTarget;editorThumbnailWidth=canvasW;editorThumbnailHeight=canvasH;}
  editorThumbnails.TryGetValue(slide,out var cached);
  if((cached==null||cached.Revision!=slide.ThumbnailRevision)&&thumbnailPaintBudget>0){
   thumbnailPaintBudget--;int width=240,height=Math.Max(24,(int)Math.Round(width*(double)canvasH/Math.Max(1,canvasW)));float scale=width/(float)Math.Max(1,canvasW);
   var layer=CaptureFlowLayer(g,new RectangleF(0,0,width,height),drawing=>{
    drawing.Clear(Color.White);DrawSlideBackground(drawing,new Rectangle(0,0,width,height),slide);
    foreach(var shape in slide.Shapes){if(shape==null||!shape.IsVisible||(shape.Name??"").StartsWith("__morph_ghost__")||shape.Type==ShapeType.SlideZoom||shape.Type==ShapeType.SummaryZoom||shape.Type==ShapeType.SectionZoom)continue;
     var snapshot=shape.CreateRenderSnapshot();snapshot.NativeGifFrameIndex=0;DrawShape(drawing,0,0,scale,snapshot,false);
    }
   });
   cached?.Dispose();cached=new EditorThumbnail{Layer=layer,Revision=slide.ThumbnailRevision};editorThumbnails[slide]=cached;
   // Keep a bounded number of GPU thumbnails; managed export previews are generated independently.
   if(editorThumbnails.Count>32){foreach(var old in new List<SlideNode>(editorThumbnails.Keys)){if(!ReferenceEquals(old,slide)){editorThumbnails[old].Dispose();editorThumbnails.Remove(old);break;}}}
   InvalidateCallback?.Invoke();
  }
  if(cached!=null)cached.Layer.Draw(g,rect);else if(slide.ThumbnailCache!=null)g.DrawImage(slide.ThumbnailCache,rect);
 }
}}
