using System;using System.Drawing;using System.Reflection;using ZenQOffice.UI;using ZenQOffice.Core;
class Test {
 static object Call(string name,params object[] args)=>typeof(SlidesView).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 [STAThread]static void Main(){
 var s=new ShapeNode{Type=ShapeType.Line};
 foreach(var b in new[]{new PointF(200,40),new PointF(20,180),new PointF(-100,-80),new PointF(20,40)}){
  var a=new PointF(20,40);Call("SetLineEndpoints",s,a,b);var start=(PointF)Call("LinePoint",s,0f,0f);var end=(PointF)Call("LinePoint",s,1f,1f);Check(Math.Abs(start.X-a.X)<.01&&Math.Abs(start.Y-a.Y)<.01&&Math.Abs(end.X-b.X)<.01&&Math.Abs(end.Y-b.Y)<.01,"Endpoint order in all quadrants");
 }
 Call("SetLineEndpoints",s,new PointF(0,20),new PointF(200,20));Check(s.Transform.Height==0,"Horizontal line has zero height");Check(((PointF[])Call("LineHandles",s)).Length==2,"Line has exactly two handles");
 s.ConnectorGeometry="bentConnector3";s.ConnectorAdjustments.Add(.3f);Check(((PointF[])Call("LineHandles",s)).Length==3,"Elbow has adjustment handle");using(var path=SlideConnectorGeometry.Path(s,200,100)){Check(path.PointCount==4,"Three elbow segments");Check(Math.Abs(path.PathPoints[1].X-60)<.01,"Adjustable elbow position");}
 var snap=(PointF)Call("SnapLinePoint",PointF.Empty,new PointF(100,3),true);Check(Math.Abs(snap.Y)<.001,"Shift locks horizontal direction");
 var view=new SlidesView();view.StartShapeDrawMode(ShapeType.Arrow);Check(view.PendingShapeType==ShapeType.Line,"Arrow tool uses line editing");view.StartConnectorDrawMode("ElbowDoubleArrowConnector");Check(view.PendingShapeType==ShapeType.Line,"Elbow tool uses line editing");
 Console.WriteLine("PASS: endpoints, reverse direction, zero-height line, two handles, elbow adjustment and Shift snapping");
 }}