using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZenQOffice.Core;
using V3 = System.Numerics.Vector3;

namespace ZenQOffice.UI
{
    public partial class SlidesView
    {
        // Reference: local PowerPoint recording, 2026-10-31 16-29-09.mp4.
        // Every moving face samples the slide texture; no replacement airplane/bird artwork.
        private static float RefPhase(float t, float start, float end) => Math.Clamp((t - start) / (end - start), 0f, 1f);
        private static float RefSmooth(float t) => t * t * (3f - 2f * t);
        private static PointF RefLerp(PointF a, PointF b, float t) => new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        private static PointF RefProject(Rectangle r, V3 p)
        {
            float perspective = 1f / Math.Max(0.25f, 1f + p.Z / 2.4f);
            return new PointF(r.X + r.Width * (0.5f + p.X * perspective), r.Y + r.Height * (0.5f + p.Y * perspective));
        }

        private void DrawRefTriangle(D2DGraphics g, bool from, PointF a, PointF b, PointF c,
            PointF da, PointF db, PointF dc, float shade = 0)
        {
            var image = from ? _cachedTransBmpA : _cachedTransBmpB;
            if (image == null) return;
            float determinant = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            float area = (db.X - da.X) * (dc.Y - da.Y) - (db.Y - da.Y) * (dc.X - da.X);
            if (Math.Abs(determinant) < 0.000001f || Math.Abs(area) < 0.01f) return;
            float m11 = ((db.X - da.X) * (c.Y - a.Y) - (dc.X - da.X) * (b.Y - a.Y)) / determinant;
            float m21 = ((dc.X - da.X) * (b.X - a.X) - (db.X - da.X) * (c.X - a.X)) / determinant;
            float m12 = ((db.Y - da.Y) * (c.Y - a.Y) - (dc.Y - da.Y) * (b.Y - a.Y)) / determinant;
            float m22 = ((dc.Y - da.Y) * (b.X - a.X) - (db.Y - da.Y) * (c.X - a.X)) / determinant;
            using var path = new GraphicsPath();
            path.AddPolygon(new[] { da, db, dc });
            using var mapping = new Matrix(m11, m12, m21, m22, da.X - m11 * a.X - m21 * a.Y, da.Y - m12 * a.X - m22 * a.Y);
            if(g.FillTexturedTriangle(image,path,mapping,shade)) return;
            var state = g.Save();
            try
            {
                g.IntersectMeshClip(path);
                g.MultiplyTransform(mapping);
                g.DrawImage(image, new RectangleF(0, 0, 1, 1));
                if (shade > 0)
                {
                    using var brush = new SolidBrush(Color.FromArgb((int)Math.Clamp(shade, 0, 150), 0, 0, 0));
                    g.FillRectangle(brush, new RectangleF(0,0,1,1));
                }
            }
            finally { g.Restore(state); }
        }

        private void DrawRefSurface(D2DGraphics g, Rectangle r, bool from, Func<float, float, V3> surface,
            float shade = 0, int columns = 8, int rows = 4, float sourceLeft = 0, float sourceRight = 1)
        {
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    float u = (float)x / columns, v = (float)y / rows;
                    float u1 = (float)(x + 1) / columns, v1 = (float)(y + 1) / rows;
                    var a = new PointF(u, v); var b = new PointF(u1, v); var c = new PointF(u1, v1); var d = new PointF(u, v1);
                    var da = RefProject(r, surface(u, v)); var db = RefProject(r, surface(u1, v));
                    var dc = RefProject(r, surface(u1, v1)); var dd = RefProject(r, surface(u, v1));
                    PointF Sample(PointF p) => new PointF(sourceLeft + p.X*(sourceRight-sourceLeft),p.Y);
                    DrawRefTriangle(g, from, Sample(a), Sample(b), Sample(c), da, db, dc, shade);
                    DrawRefTriangle(g, from, Sample(a), Sample(c), Sample(d), da, dc, dd, shade);
                }
        }

        private bool RenderReferenceTransition(D2DGraphics g, Rectangle r, SlideNode a, SlideNode b, TransitionType type, float t, float scale)
        {
            switch (type)
            {
                case TransitionType.Airplane:
                case TransitionType.Origami:
                    DrawSlideFull(g, r, b, scale);
                    DrawReferencePaper(g, r, t, type == TransitionType.Origami);
                    return true;
                case TransitionType.PeelOff:
                case TransitionType.PageCurl:
                    DrawSlideFull(g, r, b, scale);
                    DrawReferencePage(g, r, t, type == TransitionType.PeelOff);
                    return true;
                case TransitionType.Curtains:
                case TransitionType.Drape:
                    DrawSlideFull(g, r, b, scale);
                    DrawReferenceCurtain(g, r, t, type == TransitionType.Drape);
                    return true;
                case TransitionType.Fade:
                    // The outgoing slide remains opaque beneath the incoming slide.
                    // Fading both layers simultaneously exposes the workspace background.
                    if (b.TransitionDirection == TransitionDirection.ThroughBlack)
                    {
                        g.FillRectangle(Brushes.Black, r);
                        DrawSlideFull(g, r, t < 0.5f ? a : b, scale, Math.Abs(2 * t - 1), t < 0.5f);
                    }
                    else { DrawSlideFull(g, r, a, scale, 1, true); DrawSlideFull(g, r, b, scale, t); }
                    return true;
                case TransitionType.Reveal:
                    DrawSlideFull(g, r, b, scale);
                    float s = 1f - 0.12f * t;
                    float sign = b.TransitionDirection == TransitionDirection.FromLeft ? -1 : 1;
                    DrawSlideFull(g, new RectangleF(r.X + sign * r.Width * t, r.Y + r.Height * (1-s)/2, r.Width*s, r.Height*s), a, scale, 1, true);
                    return true;
                case TransitionType.Split:
                case TransitionType.Wipe:
                    DrawSoftTransition(g,r,a,b,t,scale,type==TransitionType.Split);
                    return true;
                case TransitionType.RandomBars:
                    DrawSlideFull(g, r, a, scale, 1, true);
                    using (var path = new GraphicsPath(FillMode.Winding))
                    {
                        bool horizontal = b.TransitionDirection == TransitionDirection.Horizontal;
                        for (int i=0;i<64;i++)
                        {
                            float delay = ((i*37)%67)/67f * 0.55f;
                            float p = RefPhase(t,delay,delay+0.45f);
                            if (horizontal) path.AddRectangle(new RectangleF(r.X,r.Y+i*r.Height/64f,r.Width*p,r.Height/64f+0.4f));
                            else path.AddRectangle(new RectangleF(r.X+i*r.Width/64f,r.Y,r.Width/64f+0.4f,r.Height*p));
                        }
                        var state=g.Save();
                        try { g.SetClip(path,CombineMode.Intersect); DrawSlideFull(g,r,b,scale); } finally { g.Restore(state); }
                    }
                    return true;
                case TransitionType.Flip:
                case TransitionType.Rotate:
                case TransitionType.Cube:
                case TransitionType.Box:
                case TransitionType.Doors:
                case TransitionType.Window:
                    DrawReferenceRoom(g,r,t,type,b.TransitionDirection);
                    return true;
                case TransitionType.Gallery:
                case TransitionType.Conveyor:
                case TransitionType.Switch:
                case TransitionType.Orbit:
                case TransitionType.FerrisWheel:
                case TransitionType.Pan:
                    DrawReferenceCards(g,r,t,type,b.TransitionDirection);
                    return true;
                case TransitionType.Ripple:
                    DrawSlideFull(g,r,b,scale);
                    using (var waterClip = new GraphicsPath(FillMode.Alternate))
                    {
                        float radius=t*(float)Math.Sqrt(r.Width*r.Width+r.Height*r.Height)*.65f;
                        waterClip.AddRectangle(r);
                        if(radius>0) waterClip.AddEllipse(r.X+r.Width/2f-radius,r.Y+r.Height/2f-radius,radius*2,radius*2);
                        var state=g.Save();
                        try
                        {
                            g.SetClip(waterClip,CombineMode.Intersect);
                            DrawRefSurface(g,r,true,(u,v) => {
                                float x=u-.5f,y=v-.5f,radial=(float)Math.Sqrt(x*x+y*y);
                                float distance=radial-t*.85f;
                                float wave=(float)Math.Sin(distance*55)*.035f*(float)Math.Exp(-distance*distance*100)*(float)Math.Sin(Math.PI*t);
                                return new V3(x+x/Math.Max(.02f,radial)*wave,y+y/Math.Max(.02f,radial)*wave,wave);
                            },0,16,10);
                        }
                        finally { g.Restore(state); }
                    }
                    return true;
                case TransitionType.FallOver:
                    DrawSlideFull(g,r,b,scale);
                    float angle=t*(float)Math.PI*0.55f;
                    DrawRefSurface(g,r,true,(u,v)=>new V3(u-0.5f,0.5f-(1-v)*(float)Math.Cos(angle),(1-v)*(float)Math.Sin(angle)*-0.8f),t*55);
                    return true;
                case TransitionType.Glitter:
                    DrawSlideFull(g,r,a,scale,1,true);
                    using (var particles=new GraphicsPath(FillMode.Winding))
                    {
                        for(int row=0;row<28;row++)
                            for(int col=0;col<48;col++)
                            {
                                float noise=((col*37+row*73)%101)/101f;
                                float p=RefPhase(t,(col/48f)*.65f+noise*.12f,(col/48f)*.65f+noise*.12f+.23f);
                                if(p<=0)continue;
                                float cw=r.Width/48f,ch=r.Height/28f;
                                float radius=Math.Max(cw,ch)*.8f*p;
                                particles.AddEllipse(r.X+(col+.5f)*cw-radius,r.Y+(row+.5f)*ch-radius,radius*2,radius*2);
                            }
                        var state=g.Save();
                        try { g.SetClip(particles,CombineMode.Intersect);DrawSlideFull(g,r,b,scale); }
                        finally {g.Restore(state);}
                    }
                    return true;
                case TransitionType.Vortex:
                    DrawReferenceVortex(g,r,t);
                    return true;
                case TransitionType.Fracture:
                    DrawSlideFull(g,r,b,scale);
                    DrawReferenceFracture(g,r,t);
                    return true;
                case TransitionType.Shred:
                    DrawReferenceShred(g,r,t);
                    return true;
                case TransitionType.Zoom:
                case TransitionType.FlyThrough:
                    g.FillRectangle(Brushes.Black,r);
                    bool outwards=b.TransitionDirection==TransitionDirection.Out;
                    float sourceScale=type==TransitionType.FlyThrough ? 1+t*4 : (outwards?1-t*.9f:1+t*.4f);
                    float targetScale=type==TransitionType.FlyThrough ? .9f+.1f*t : (outwards?1.3f-.3f*t:.1f+.9f*t);
                    var sourceRect=new RectangleF(r.X+(1-sourceScale)*r.Width/2,r.Y+(1-sourceScale)*r.Height/2,r.Width*sourceScale,r.Height*sourceScale);
                    var targetRect=new RectangleF(r.X+(1-targetScale)*r.Width/2,r.Y+(1-targetScale)*r.Height/2,r.Width*targetScale,r.Height*targetScale);
                    if(outwards && type==TransitionType.Zoom)
                    {
                        DrawSlideFull(g,targetRect,b,scale);
                        DrawSlideFull(g,sourceRect,a,scale,1-t,true);
                    }
                    else
                    {
                        DrawSlideFull(g,sourceRect,a,scale,1-t,true);
                        DrawSlideFull(g,targetRect,b,scale,RefPhase(t,.1f,.9f));
                    }
                    return true;
                case TransitionType.Random:
                    var choice=randomTransitionChoice;
                    if (!RenderReferenceTransition(g,r,a,b,choice,t,scale))
                        RenderPushTransition(g,r,a,b,t,scale);
                    return true;
                default: return false;
            }
        }

        private void DrawReferencePaper(D2DGraphics g, Rectangle r, float t, bool bird)
        {
            PointF[] source={new PointF(0,0),new PointF(.5f,0),new PointF(1,0),new PointF(1,.5f),new PointF(1,1),new PointF(.5f,1),new PointF(0,1),new PointF(0,.5f)};
            PointF[] folded = bird ? new[]{new PointF(.12f,.18f),new PointF(.42f,.44f),new PointF(.84f,.18f),new PointF(.64f,.48f),new PointF(.92f,.49f),new PointF(.54f,.57f),new PointF(.20f,.83f),new PointF(.38f,.52f)} :
                new[]{new PointF(.15f,.30f),new PointF(.90f,.46f),new PointF(.45f,.49f),new PointF(.90f,.46f),new PointF(.14f,.76f),new PointF(.30f,.54f),new PointF(.08f,.58f),new PointF(.29f,.48f)};
            float fold=RefSmooth(RefPhase(t,0,.42f));
            float flight=(float)Math.Pow(RefPhase(t,.42f,1),2.3);
            float flap=bird ? (float)Math.Sin(RefPhase(t,.42f,1)*Math.PI*5)*.055f*RefPhase(t,.42f,.65f) : 0;
            float shrink=1-flight*.3f;
            float dx=flight*1.25f;
            float dy=-flight*.22f-(float)Math.Sin(flight*Math.PI)*.07f;
            PointF Map(PointF p)
            {
                float angle=bird?-.05f*fold:-.22f*fold;
                float x=p.X-.5f,y=p.Y-.5f;
                return new PointF(r.X+r.Width*(.5f+(x*(float)Math.Cos(angle)-y*(float)Math.Sin(angle))*shrink+dx),
                    r.Y+r.Height*(.5f+(x*(float)Math.Sin(angle)+y*(float)Math.Cos(angle))*shrink+dy));
            }
            var center=new PointF(.5f,.5f);
            for(int i=0;i<8;i++)
            {
                int next=(i+1)%8;
                var targetA=folded[i]; var targetB=folded[next];
                if(bird && (i==0 || i==2)) targetA.Y+=flap;
                if(bird && (next==0 || next==2)) targetB.Y+=flap;
                var da=Map(RefLerp(source[i],targetA,fold)); var db=Map(RefLerp(source[next],targetB,fold));
                DrawRefTriangle(g,true,source[i],source[next],center,da,db,Map(center),fold*(i%2==0?18:65));
            }
        }

        private void DrawReferencePage(D2DGraphics g, Rectangle r, float t, bool diagonal)
        {
            float reach=diagonal ? 2.2f : 1.25f;
            float boundary=(diagonal?2f:1f)-t*reach;
            DrawRefSurface(g,r,true,(u,v)=>{
                float distance=(diagonal?u+v:u)-boundary;
                if(distance<=0) return new V3(u-.5f,v-.5f,0);
                float angle=Math.Min((float)Math.PI,distance*7f);
                float radius=.14f;
                float rolled=radius*(float)Math.Sin(angle);
                float lift=radius*(1-(float)Math.Cos(angle));
                float displacement=distance-rolled;
                return new V3(u-.5f-displacement*(diagonal?.5f:1f),v-.5f-(diagonal?displacement*.5f:0),-lift);
            },t*25,20,diagonal?12:2);
        }

        private void DrawReferenceCurtain(D2DGraphics g, Rectangle r, float t, bool drape)
        {
            if(drape)
            {
                DrawRefSurface(g,r,true,(u,v)=>{
                    float wave=(float)Math.Sin(u*12+v*4)*t*.06f;
                    return new V3(u-.5f+wave,v-.5f+t*t*1.5f*(.35f+u),wave*2);
                },t*28,24,4);
                return;
            }
            float gather=RefSmooth(RefPhase(t,0,.8f));
            float exit=RefSmooth(RefPhase(t,.8f,1));
            const int pleats=24;
            for(int leaf=0;leaf<2;leaf++)
                for(int strip=0;strip<pleats;strip++)
                {
                    int current=leaf,column=strip;
                    float side=current==0?-1:1;
                    float sourceLeft=current*.5f+column/(float)pleats*.5f;
                    float sourceRight=current*.5f+(column+1)/(float)pleats*.5f;
                    float shade=(12+12*(float)Math.Cos((column+.5f)*Math.PI))*gather;
                    DrawRefSurface(g,r,true,(localU,v)=>{
                        float u=sourceLeft+(sourceRight-sourceLeft)*localU;
                        float retained=1-gather*(.6f+.35f*v);
                        float x=side*.5f+(u-.5f-side*.5f)*retained+side*exit*.5f;
                        float depth=(float)Math.Sin((column+localU)*Math.PI)*gather*.025f;
                        float hem=(float)Math.Sin((current==0?u*2:(1-u)*2)*Math.PI)*gather*.06f*v;
                        return new V3(x,v-.5f+hem,depth);
                    },shade,1,4,sourceLeft,sourceRight);
                }
        }

        private static V3 RefRotateY(float x,float y,float z,float angle) => new V3(x*(float)Math.Cos(angle)+z*(float)Math.Sin(angle),y,-x*(float)Math.Sin(angle)+z*(float)Math.Cos(angle));
        private void DrawReferenceRoom(D2DGraphics g, Rectangle r,float t,TransitionType type,TransitionDirection direction)
        {
            g.FillRectangle(Brushes.Black,r);
            float sign=(direction==TransitionDirection.FromLeft?-1:1)*(type==TransitionType.Cube?-1:1);
            bool vertical=direction==TransitionDirection.FromTop || direction==TransitionDirection.FromBottom;
            float theta=t*(float)Math.PI/2;
            if(type==TransitionType.Rotate)
            {
                float turn=RefSmooth(t)*(float)Math.PI/2;
                float cosine=(float)Math.Cos(turn),sine=(float)Math.Sin(turn);
                float depth=(float)Math.Sin(Math.PI*t)*.55f;
                V3 Panel(float u,float v,bool from)
                {
                    float local=sign<0?1-u:u;
                    float x=from?(local-.5f)*cosine-.5f*sine:(local-.5f)*sine+.5f*cosine;
                    return new V3(x*sign,v-.5f,(from?local:1-local)*depth);
                }
                DrawRefSurface(g,r,true,(u,v)=>Panel(u,v,true),t*35);
                DrawRefSurface(g,r,false,(u,v)=>Panel(u,v,false),(1-t)*35);
                return;
            }
            if(type==TransitionType.Flip)
            {
                bool from=t<.5f;
                float angle=t*(float)Math.PI;
                if(!from) angle-=(float)Math.PI;
                DrawRefSurface(g,r,from,(u,v)=>RefRotateY(u-.5f,v-.5f,0,angle*sign),Math.Abs((float)Math.Sin(angle))*65);
                return;
            }
            if(type==TransitionType.Doors || type==TransitionType.Window)
            {
                g.DrawImage(_cachedTransBmpB,r);
                // Each leaf keeps its own source rectangle and outer hinge.
                for(int leaf=0;leaf<2;leaf++)
                {
                    int current=leaf;
                    DrawRefSurface(g,r,true,(localU,v)=>{
                        float u=(current+localU)/2;
                        float side=current==0?-1:1,hinge=side*.5f;
                        var p=RefRotateY(u-.5f-hinge,v-.5f,0,-side*theta);
                        p.X+=hinge;
                        if(type==TransitionType.Window) { p.X*=1-t*.18f; p.Y*=1-t*.18f; p.Z+=t*.8f; p.X+=side*t*t*.35f; }
                        return p;
                    },t*55,6,4,current*.5f,(current+1)*.5f);
                }
                return;
            }
            bool inside=type==TransitionType.Box;
            V3 Map(float u,float v,bool from)
            {
                float x=u-.5f,y=v-.5f;
                if(vertical) { float tmp=x;x=y;y=tmp; }
                float ang=(from?theta:theta-(float)Math.PI/2)*sign;
                var p=RefRotateY(x,y,(inside?-.5f:.5f),ang);
                p.Z-=inside?-.5f:.5f;
                p.Z+=(float)Math.Sin(Math.PI*t)*.3f;
                if(vertical) { float tmp=p.X;p.X=p.Y;p.Y=tmp; }
                return p;
            }
            if(t<.5f) { DrawRefSurface(g,r,false,(u,v)=>Map(u,v,false),(1-t)*45); DrawRefSurface(g,r,true,(u,v)=>Map(u,v,true),t*45); }
            else { DrawRefSurface(g,r,true,(u,v)=>Map(u,v,true),t*45); DrawRefSurface(g,r,false,(u,v)=>Map(u,v,false),(1-t)*45); }
        }

        private void DrawReferenceCards(D2DGraphics g,Rectangle r,float t,TransitionType type,TransitionDirection direction)
        {
            if(type==TransitionType.Pan)
            {
                DrawReferencePan(g,r,RefSmooth(t),direction);
                return;
            }
            g.FillRectangle(Brushes.Black,r);
            bool vertical=direction==TransitionDirection.FromBottom || direction==TransitionDirection.FromTop;
            float sign=direction==TransitionDirection.FromLeft || direction==TransitionDirection.FromTop?-1:1;
            V3 Card(float u,float v,bool from)
            {
                float p=from?t:t-1;
                float x=u-.5f,y=v-.5f,z=0;
                if(type==TransitionType.FerrisWheel)
                {
                    float angle=p*(float)Math.PI/2;
                    x-= (float)Math.Sin(angle)*1.25f;
                    y+=(1-(float)Math.Cos(angle))*1.5f;
                    z=Math.Abs((float)Math.Sin(angle))*.4f;
                }
                else if(type==TransitionType.Pan)
                {
                    float zoom=1+.12f*(float)Math.Sin(Math.PI*t);
                    x*=zoom; y*=zoom;
                    if(vertical)y-=p*sign; else x-=p*sign;
                }
                else if(type==TransitionType.Orbit)
                {
                    float angle=p*(float)Math.PI;
                    x-=(float)Math.Sin(angle)*1.1f;
                    z+=(1-(float)Math.Cos(angle))*1.3f;
                }
                else if(type==TransitionType.Switch)
                {
                    float bend=(float)Math.Sin(Math.PI*t);
                    x+=(from?-1:1)*bend*.6f;
                    z+=(from?t:1-t)*.8f;
                    var p3=RefRotateY(x,y,z,(from?-1:1)*bend*.22f);
                    return p3;
                }
                else
                {
                    float angle=p*(type==TransitionType.Gallery?.4f:.65f);
                    var p3=RefRotateY(x,y,0,angle);
                    x=p3.X-p*1.35f;z=p3.Z+Math.Abs(p)*.5f;
                    if(type==TransitionType.Conveyor)y+=Math.Abs(p)*.16f;
                }
                return new V3(x,y,z);
            }
            bool incomingOnTop=t>.5f;
            DrawRefSurface(g,r,incomingOnTop,(u,v)=>Card(u,v,incomingOnTop),0,6,3);
            DrawRefSurface(g,r,!incomingOnTop,(u,v)=>Card(u,v,!incomingOnTop),0,6,3);
        }

        private void DrawReferencePan(D2DGraphics g,Rectangle r,float t,TransitionDirection direction)
        {
            bool vertical=direction==TransitionDirection.FromTop||direction==TransitionDirection.FromBottom;
            bool incomingPositive=direction!=TransitionDirection.FromLeft && direction!=TransitionDirection.FromTop;
            float distance=vertical?r.Height:r.Width;
            float sign=incomingPositive?-1:1;
            float outgoing=sign*distance*t,incoming=-sign*distance*(1-t);
            float edge=vertical?(incomingPositive?r.Bottom-distance*t:r.Top+distance*t):(incomingPositive?r.Right-distance*t:r.Left+distance*t);
            edge=(float)Math.Round(edge);
            float zoom=1+.08f*(float)Math.Sin(Math.PI*t);
            for(int i=0;i<2;i++)
            {
                bool from=i==0;
                RectangleF clip;
                bool low=from==incomingPositive;
                if(vertical) clip=low?new RectangleF(r.X,r.Y,r.Width,Math.Max(0,edge-r.Top)):new RectangleF(r.X,edge,r.Width,Math.Max(0,r.Bottom-edge));
                else clip=low?new RectangleF(r.X,r.Y,Math.Max(0,edge-r.Left),r.Height):new RectangleF(edge,r.Y,Math.Max(0,r.Right-edge),r.Height);
                if(clip.Width<=0||clip.Height<=0)continue;
                var state=g.Save();
                try
                {
                    g.IntersectClip(clip);
                    float offset=from?outgoing:incoming;
                    var dest=new RectangleF(r.X-r.Width*(zoom-1)/2+(vertical?0:offset),r.Y-r.Height*(zoom-1)/2+(vertical?offset:0),r.Width*zoom,r.Height*zoom);
                    DrawCachedSlideBitmap(g,dest,from?_cachedTransBmpA:_cachedTransBmpB,1);
                }
                finally {g.Restore(state);}
            }
        }

        private void DrawReferenceVortex(D2DGraphics g,Rectangle r,float t)
        {
            g.FillRectangle(Brushes.Black,r);
            bool from=t<.5f;
            float p=from?t*2:(1-t)*2;
            const int cols=40,rows=24;
            float cw=r.Width/(float)cols,ch=r.Height/(float)rows;
            for(int row=0;row<rows;row++)
                for(int col=0;col<cols;col++)
                {
                    float random=((col*73+row*37)%101)/101f;
                    float drift=RefPhase(p,random*.16f,1);
                    float x=(col+.5f)/cols-.5f,y=(row+.5f)/rows-.5f;
                    float angle=drift*(4+random*4);
                    float radius=1-drift;
                    float px=(x*(float)Math.Cos(angle)-y*(float)Math.Sin(angle))*radius;
                    float py=(x*(float)Math.Sin(angle)+y*(float)Math.Cos(angle))*radius;
                    float size=Math.Max(.01f,1-drift*.95f);
                    var dest=new RectangleF(r.X+r.Width*(.5f+px)-cw*size/2,r.Y+r.Height*(.5f+py)-ch*size/2,cw*size,ch*size);
                    DrawSlideSlice(g,dest,new RectangleF(col*cw,row*ch,cw,ch),from,1-drift*.5f);
                }
        }
        private void DrawReferenceFracture(D2DGraphics g,Rectangle r,float t)
        {
            var center=new PointF(.5f,.5f);
            var ring=new PointF[24];
            for(int i=0;i<24;i++)
            {
                int side=i/6;float p=(i%6)/6f;
                ring[i]=side==0?new PointF(p,0):side==1?new PointF(1,p):side==2?new PointF(1-p,1):new PointF(0,1-p);
            }
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];
                var pivot=new PointF((a.X+b.X+center.X)/3,(a.Y+b.Y+center.Y)/3);
                float p=RefPhase(t,.08f+(i%5)*.025f,1);
                float angle=p*((i%2==0?1:-1)*1.2f);
                float dx=(pivot.X-.5f)*p*.75f,dy=p*p*1.7f;
                PointF Map(PointF source)
                {
                    float x=source.X-pivot.X,y=source.Y-pivot.Y;
                    return new PointF(r.X+r.Width*(pivot.X+dx+x*(float)Math.Cos(angle)-y*(float)Math.Sin(angle)),r.Y+r.Height*(pivot.Y+dy+x*(float)Math.Sin(angle)+y*(float)Math.Cos(angle)));
                }
                DrawRefTriangle(g,true,a,b,center,Map(a),Map(b),Map(center),p*30);
            }
        }

        private void DrawReferenceShred(D2DGraphics g,Rectangle r,float t)
        {
            g.FillRectangle(Brushes.Black,r);
            bool from=t<.5f;
            float p=from?t*2:(1-t)*2;
            for(int i=0;i<24;i++)
            {
                int strip=i;
                float delay=(i%4)*.08f;
                float amount=RefPhase(p,delay,1);
                DrawRefSurface(g,r,from,(u,v)=>{
                    float actualU=(strip+u)/24f;
                    float lean=(float)Math.Sin(strip*2.13f)*.22f*amount;
                    return new V3(actualU-.5f+lean*(1-v),v-.5f+amount*(1.3f+(strip%5)*.08f),amount*.5f);
                },amount*45,1,1,strip/24f,(strip+1)/24f);
            }
        }
    }
}
