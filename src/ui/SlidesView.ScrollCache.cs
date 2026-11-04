using System;using System.Drawing;using System.Linq;using ZenQOffice.Core;using Vortice.Direct2D1;
namespace ZenQOffice.UI { public partial class SlidesView {
 FlowLayer editorScrollLayer;SlideNode editorScrollSlide;ID2D1RenderTarget editorScrollTarget;float editorScrollScale;string editorScrollSelection;int editorScrollShapeCount;bool editorScrollThumbnailDirty;
 void ClearEditorScrollCache(){editorScrollLayer?.Dispose();editorScrollLayer=null;editorScrollSlide=null;editorScrollTarget=null;}
 bool DrawEditorScrollCache(D2DGraphics g,SlideNode slide,int slideX,int slideY,float scale,Rectangle viewport){
  bool moving=DeferThumbnailWork();
  if(IsInlineEditing||IsDraggingShape||IsResizingShape||IsRotatingShape||IsDrawingActive||IsRemovingBackground||IsScanningActive||IsCropping||IsPlayingAnimationPreview||IsEditingShapePoints||ViewMode!=ImpressViewMode.Normal||g.RenderTarget==null){ClearEditorScrollCache();return false;}
  if(slide.Shapes.Any(s=>s.Type==ShapeType.Video||s.Type==ShapeType.Audio||s.NativeGifFrames?.Length>1||(s.ImagePath??"").EndsWith(".gif",StringComparison.OrdinalIgnoreCase))){ClearEditorScrollCache();return false;}
  string selection=SelectedShapeIndex+":"+string.Join(",",SelectedShapeIndices??new System.Collections.Generic.List<int>());
  var visible=new RectangleF(viewport.X-slideX,viewport.Y-slideY,viewport.Width,viewport.Height);
  bool valid=editorScrollLayer!=null&&editorScrollSlide==slide&&ReferenceEquals(editorScrollTarget,g.RenderTarget)&&editorScrollScale==scale&&editorScrollSelection==selection&&editorScrollShapeCount==slide.Shapes.Count&&editorScrollThumbnailDirty==slide.IsThumbnailDirty&&editorScrollLayer.Bounds.Contains(visible);
  if(!moving && valid) return false; // Paint edits normally while retaining a ready texture for the next wheel event.
  if(editorScrollLayer==null||editorScrollSlide!=slide||!ReferenceEquals(editorScrollTarget,g.RenderTarget)||editorScrollScale!=scale||editorScrollSelection!=selection||editorScrollShapeCount!=slide.Shapes.Count||editorScrollThumbnailDirty!=slide.IsThumbnailDirty||!editorScrollLayer.Bounds.Contains(visible)){
   ClearEditorScrollCache();var bounds=visible;bounds.Inflate(256,256);
   // Bound temporary GPU memory even on very large displays; retain the regular renderer there.
   if(bounds.Width>4096||bounds.Height>4096)return false;
   editorScrollLayer=CaptureFlowLayer(g,bounds,drawing=>{
    for(int i=0;i<slide.Shapes.Count;i++){var shape=slide.Shapes[i];if(shape==null||!shape.IsVisible||(shape.Name??"").StartsWith("__morph_ghost__"))continue;
     bool selected=i==SelectedShapeIndex||(SelectedShapeIndices?.Contains(i)??false);DrawShape(drawing,0,0,scale,shape,selected);
    }
   });editorScrollSlide=slide;editorScrollTarget=g.RenderTarget;editorScrollScale=scale;editorScrollSelection=selection;editorScrollShapeCount=slide.Shapes.Count;editorScrollThumbnailDirty=slide.IsThumbnailDirty;
  }
  var r=editorScrollLayer.Bounds;editorScrollLayer.Draw(g,new RectangleF(slideX+r.X,slideY+r.Y,r.Width,r.Height));return true;
 }
}}
