using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using ZenQOffice.Core;
namespace ZenQOffice.UI {
 public partial class SlidesView {
  static PointF LinePoint(ShapeNode s,float u,float v) {
   var t=s.Transform; if(t.FlipHorizontal)u=1-u;if(t.FlipVertical)v=1-v;
   return RotatePoint(new PointF(t.X+u*t.Width,t.Y+v*t.Height),new PointF(t.X+t.Width/2,t.Y+t.Height/2),t.Rotation);
  }
  static PointF SnapLinePoint(PointF anchor,PointF point,bool constrain) {
   float dx=point.X-anchor.X,dy=point.Y-anchor.Y;
   if(constrain){double angle=Math.Round(Math.Atan2(dy,dx)/(Math.PI/4))*(Math.PI/4);double length=Math.Sqrt(dx*dx+dy*dy);return new PointF(anchor.X+(float)(length*Math.Cos(angle)),anchor.Y+(float)(length*Math.Sin(angle)));}
   return point;
  }
  static void SetLineEndpoints(ShapeNode s,PointF a,PointF b) {
   var t=s.Transform;t.X=Math.Min(a.X,b.X);t.Y=Math.Min(a.Y,b.Y);t.Width=Math.Abs(b.X-a.X);t.Height=Math.Abs(b.Y-a.Y);
   if(t.Width<.001f)t.Width=0;if(t.Height<.001f)t.Height=0;
   t.Rotation=0;t.FlipHorizontal=b.X<a.X;t.FlipVertical=b.Y<a.Y;s.AutoFitHeight=false;s.AutoFitWidth=false;s.InvalidateDisplayCache();
  }
  static PointF[] LineHandles(ShapeNode s) {
   var list=new List<PointF>{LinePoint(s,0,0),LinePoint(s,1,1)};
   if((s.ConnectorGeometry??"").StartsWith("bentConnector")||(s.ConnectorGeometry??"").StartsWith("curvedConnector"))list.Add(LinePoint(s,s.ConnectorAdjustments?.Count>0?s.ConnectorAdjustments[0]:.5f,.5f));
   return list.ToArray();
  }
  void DrawLineHandles(D2DGraphics g,ShapeNode s,float sx,float sy,float scale) {
   var points=LineHandles(s);using var pen=new Pen(Color.Gray,1.2f);
   for(int i=0;i<points.Length;i++){float x=sx+(points[i].X-s.Transform.X)*scale,y=sy+(points[i].Y-s.Transform.Y)*scale;
    if(i<2){g.FillEllipse(Brushes.White,x-4,y-4,8,8);g.DrawEllipse(pen,x-4,y-4,8,8);}else{g.FillRectangle(Brushes.Gold,x-4,y-4,8,8);g.DrawRectangle(pen,x-4,y-4,8,8);}}
  }
  bool BeginLineHandleDrag(ShapeNode s,Point mouse,int slideX,int slideY,float scale) {
   var handles=LineHandles(s);for(int i=0;i<handles.Length;i++){float dx=mouse.X-slideX-handles[i].X*scale,dy=mouse.Y-slideY-handles[i].Y*scale;if(dx*dx+dy*dy>100)continue;
    SaveUndoState();IsResizingShape=true;DragResizeHandleIndex=100+i;DragStartMouse=mouse;DragStartTransform=s.Transform.Clone();return true;}return false;
  }
  void MoveLineHandle(ShapeNode s,Point mouse,int slideX,int slideY,float scale) {
   var p=new PointF((mouse.X-slideX)/scale,(mouse.Y-slideY)/scale);int index=DragResizeHandleIndex-100;
   if(index<2){var other=LinePoint(s,index==0?1:0,index==0?1:0);p=SnapLinePoint(other,p,(Control.ModifierKeys&Keys.Shift)!=0);
    if(Math.Abs(p.X-other.X)*scale<4)p.X=other.X;if(Math.Abs(p.Y-other.Y)*scale<4)p.Y=other.Y;
    if(index==0)SetLineEndpoints(s,p,other);else SetLineEndpoints(s,other,p);
   }else{var t=s.Transform;var q=RotatePoint(p,new PointF(t.X+t.Width/2,t.Y+t.Height/2),-t.Rotation);float u=(q.X-t.X)/Math.Max(.001f,t.Width);if(t.FlipHorizontal)u=1-u;s.ConnectorAdjustments??=new List<float>();if(s.ConnectorAdjustments.Count==0)s.ConnectorAdjustments.Add(u);else s.ConnectorAdjustments[0]=u;s.InvalidateDisplayCache();}
   InvalidateCallback?.Invoke();
  }
 }
}
