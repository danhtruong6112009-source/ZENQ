using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public partial class SlidesView
    {
        private bool _transitionIsReverse;
        private TransitionType _transitionEffectType;
        private float _transitionEffectDuration;
        private bool _transitionAwaitingFirstFrame, _transitionFirstFrameDrawn;
        private SlideNode _transitionTextureFrom, _transitionTextureTo;
        private long _transitionRenderedFrame, _transitionConsumedFrame;
        private float _transitionLastPaintedProgress;
        private double _transitionLastTickSeconds;
        private bool _transitionWarmingLiveScene;
        private readonly HashSet<IntPtr> _transitionWarmedTargets=new();
        private readonly List<(ShapeNode Shape, Image Original)> _transitionCaptureShapes = new();

        private ShapeNode CreateTransitionShapeSnapshot(ShapeNode source)
        {
            var shape=source.CreateRenderSnapshot();
            shape.DisplayCachedImage=null;
            shape.CachedShadowBmp=null;
            shape.CachedFeatheredBmp=null;
            shape.NativeD2DBitmap=null;
            shape.NativeD2DOwnerTarget=IntPtr.Zero;
            shape.NativeGifFrames=null;
            shape.IsGifPlaying=false;
            shape.IsVideoPlaying=source.IsVideoPlaying;
            Bitmap frozen=null;
            if (IsAnimatedGif(source))
            {
                frozen=D2DGraphics.CopyDecodedGifFrame(source.ImagePath,source.NativeGifFrameIndex);
                if (frozen!=null) shape.CachedImage=frozen;
                // Freeze the current frame; do not decode/upload an animation on the capture target.
                if (shape.CachedImage!=null) shape.ImagePath="";
            }
            _transitionCaptureShapes.Add((shape,source.CachedImage));
            return shape;
        }

        private Bitmap CaptureTransitionSlide(SlideNode slide,int width,int height,bool from)
        {
            var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
            try
            {
                using(var graphics=Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.Black);
                    using(var drawing=new D2DGraphics(graphics,width,height))
                        DrawSlideFull(drawing,new RectangleF(0,0,width,height),slide,
                            width/Math.Max(1f,Presentation?.CanvasWidth??1920f),1f,from);
                }
                // GDI text/chart/SmartArt rendering into a DC may write RGB with alpha zero.
                // The slide is an opaque composited scene, even when its constituent images have alpha.
                var data=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);
                try
                {
                    unsafe
                    {
                        for(int y=0;y<height;y++)
                        {
                            uint* row=(uint*)((byte*)data.Scan0+y*data.Stride);
                            for(int x=0;x<width;x++) row[x]|=0xff000000;
                        }
                    }
                }
                finally { bitmap.UnlockBits(data); }
                return bitmap;
            }
            catch { bitmap.Dispose(); throw; }
            finally
            {
                foreach(var captured in _transitionCaptureShapes)
                {
                    captured.Shape.InvalidateFeatherCache();
                    if(!ReferenceEquals(captured.Shape.CachedImage,captured.Original)) captured.Shape.CachedImage?.Dispose();
                }
                _transitionCaptureShapes.Clear();
            }
        }

        private void DrawSoftTransition(D2DGraphics g,Rectangle r,SlideNode from,SlideNode to,float t,float scale,bool split)
        {
            DrawSlideFull(g,r,from,scale,1,true);
            var direction=to.TransitionDirection;
            bool horizontal=split
                ? direction==TransitionDirection.Horizontal || direction==TransitionDirection.Out
                : direction==TransitionDirection.FromTop || direction==TransitionDirection.FromBottom;
            bool inward=split && (direction==TransitionDirection.In || direction==TransitionDirection.Out);
            bool reverse=!split && (direction==TransitionDirection.FromRight || direction==TransitionDirection.FromBottom);
            const float feather=.18f;
            float Coverage(float position)
            {
                float distance=split?Math.Abs(position-.5f)*2:position;
                if(inward) distance=1-distance;
                if(reverse) distance=1-distance;
                return RefSmooth(Math.Clamp((t*(1+feather*2)-feather-distance)/feather,0,1));
            }
            using(var mask=new LinearGradientBrush(r,Color.White,Color.Transparent,horizontal?90f:0f))
            {
                var blend=new ColorBlend(129);
                for(int i=0;i<129;i++) { blend.Positions[i]=i/128f; blend.Colors[i]=Color.FromArgb((int)(255*Coverage(i/128f)),255,255,255); }
                mask.InterpolationColors=blend;
                if(g.DrawImageOpacityMask(_cachedTransBmpB,r,mask)) return;
            }
            const int bands=192;
            for(int i=0;i<bands;i++)
            {
                float position=(i+.5f)/bands;
                float distance=split?Math.Abs(position-.5f)*2:position;
                if (inward) distance=1-distance;
                if (reverse) distance=1-distance;
                float coverage=RefSmooth(Math.Clamp((t*(1+feather*2)-feather-distance)/feather,0,1));
                if (coverage<=0) continue;
                var source=horizontal?new RectangleF(0,r.Height*i/(float)bands,r.Width,r.Height/(float)bands):
                    new RectangleF(r.Width*i/(float)bands,0,r.Width/(float)bands,r.Height);
                var destination=source; destination.Offset(r.Location);
                DrawSlideSlice(g,destination,source,false,coverage);
            }
        }

        private float _presenterGridScrollY;
        private bool _gridRevealActive, _gridDraggingScroll;
        public float PresentationGridScrollOffset => _presenterGridScrollY;
        private void GetPresentationGridLayout(int width,int height,out int cols,out int cardW,out int cardH,out int maxScroll)
        {
            cols=width<600?2:4;
            cardW=Math.Max(30,(width-96-(cols-1)*18)/cols);
            cardH=Math.Max(20,(int)(cardW*(Presentation?.CanvasHeight??1080)/Math.Max(1f,Presentation?.CanvasWidth??1920)));
            int rows=((Presentation?.Slides.Count??0)+cols-1)/cols;
            maxScroll=Math.Max(0,rows*(cardH+36)-Math.Max(1,height-100));
            if (_gridRevealActive)
            {
                _presenterGridScrollY=Math.Clamp((ActiveSlideIndex/cols)*(cardH+36)-(height-100)/2+cardH/2,0,maxScroll);
                _gridRevealActive=false;
            }
            _presenterGridScrollY=Math.Clamp(_presenterGridScrollY,0,maxScroll);
        }
        private Rectangle PresentationGridCard(int index,int cols,int cardW,int cardH) =>
            new Rectangle(40+(index%cols)*(cardW+18),80+(index/cols)*(cardH+36)-(int)_presenterGridScrollY,cardW,cardH);
        public bool ScrollPresentationGrid(int wheelDelta,int width,int height)
        {
            if (!IsSlideSorterModalOpen) return false;
            GetPresentationGridLayout(width,height,out _,out _,out int cardH,out int maxScroll);
            _presenterGridScrollY=Math.Clamp(_presenterGridScrollY-wheelDelta/120f*Math.Max(60,cardH*.75f),0,maxScroll);
            InvalidateCallback?.Invoke();
            return true;
        }
        private void DragPresentationGridScroll(Point mouse,int width,int height)
        {
            GetPresentationGridLayout(width,height,out _,out _,out _,out int maxScroll);
            float fraction=Math.Clamp((mouse.Y-80f)/Math.Max(1,height-100f),0,1);
            _presenterGridScrollY=fraction*maxScroll;
            InvalidateCallback?.Invoke();
        }
        private bool HandlePresentationGridClick(Point mouse,int width,int height)
        {
            if (!IsSlideSorterModalOpen) return false;
            if(mouse.Y>=15 && mouse.Y<=55 && mouse.X>=width-155) { IsSlideSorterModalOpen=false; return true; }
            GetPresentationGridLayout(width,height,out int cols,out int cardW,out int cardH,out int maxScroll);
            if(maxScroll>0 && mouse.X>=width-30 && mouse.Y>=80 && mouse.Y<height-20)
            {
                _gridDraggingScroll=true;
                DragPresentationGridScroll(mouse,width,height);
                return true;
            }
            if(mouse.Y>=80 && mouse.Y<height-20)
                for(int i=0;i<Presentation.Slides.Count;i++)
                    if(PresentationGridCard(i,cols,cardW,cardH).Contains(mouse))
                    {
                        int previous=ActiveSlideIndex;
                        ActiveSlideIndex=i;
                        CurrentAnimationStep=i<previous?GetTotalAnimationSteps(ActiveSlide):0;
                        IsSlideSorterModalOpen=false;
                        if(i!=previous) TriggerTransition(previous,i,false);
                        InvalidateCallback?.Invoke();
                        return true;
                    }
            return true;
        }

        private void DrawPresentationGrid(D2DGraphics g,int width,int height)
        {
            if(Presentation==null) return;
            GetPresentationGridLayout(width,height,out int cols,out int cardW,out int cardH,out int maxScroll);
            using var background=new SolidBrush(Color.FromArgb(20,23,28));
            g.FillRectangle(background,0,0,width,height);
            using var title=new Font("Segoe UI",16f,FontStyle.Bold);
            using var label=new Font("Segoe UI",10f);
            g.DrawString(ZenQOffice.Core.AppL10n.T("Tất cả slide","All Slides"),title,Brushes.White,40,20);
            g.DrawString(ZenQOffice.Core.AppL10n.T("Cuộn để xem thêm • Nhấp vào slide để chuyển đến","Scroll to browse • Click a slide to jump to it"),label,Brushes.LightGray,40,50);
            g.DrawString(ZenQOffice.Core.AppL10n.T("Đóng (Esc)","Close (Esc)"),label,Brushes.White,width-140,28);
            var state=g.Save();
            try
            {
                g.IntersectClip(new Rectangle(32,78,Math.Max(1,width-64),Math.Max(1,height-98)));
                for(int i=0;i<Presentation.Slides.Count;i++)
                {
                    var bounds=PresentationGridCard(i,cols,cardW,cardH);
                    if(bounds.Bottom<80 || bounds.Top>=height-20) continue;
                    var slide=Presentation.Slides[i];
                    EnsureSlideThumbnail(slide);
                    if(slide.ThumbnailCache!=null) g.DrawImage(slide.ThumbnailCache,bounds);
                    else DrawSlideBackground(g,bounds,slide);
                    using var border=new Pen(i==ActiveSlideIndex?Color.FromArgb(96,165,250):Color.FromArgb(90,95,105),i==ActiveSlideIndex?3:1);
                    g.DrawRectangle(border,bounds);
                    string text=(i+1)+" · "+slide.Title;
                    using var format=new StringFormat {Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};
                    g.DrawString(text,label,Brushes.WhiteSmoke,new RectangleF(bounds.X,bounds.Bottom+5,bounds.Width,24),format);
                }
            }
            finally { g.Restore(state); }
            if(maxScroll>0)
            {
                float trackHeight=Math.Max(1,height-100);
                float thumbHeight=Math.Max(26,trackHeight*trackHeight/(trackHeight+maxScroll));
                float top=80+_presenterGridScrollY/maxScroll*(trackHeight-thumbHeight);
                using var track=new SolidBrush(Color.FromArgb(45,50,59));
                using var thumb=new SolidBrush(Color.FromArgb(130,145,163));
                g.FillRectangle(track,width-24,80,8,trackHeight);
                g.FillRectangle(thumb,width-24,top,8,thumbHeight);
            }
        }
    }
}
