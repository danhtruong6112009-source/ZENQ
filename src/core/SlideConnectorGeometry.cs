using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ZenQOffice.Core
{
 public static class SlideConnectorGeometry
 {
  public static GraphicsPath Path(ShapeNode shape,float width,float height)
  {
   var path=new GraphicsPath();string kind=shape.ConnectorGeometry??"line";
   float Adjust(int index)=>shape.ConnectorAdjustments!=null&&index<shape.ConnectorAdjustments.Count?shape.ConnectorAdjustments[index]:.5f;
   if(kind.StartsWith("curvedConnector"))path.AddBezier(0,0,width*Adjust(0),0,width*Adjust(0),height,width,height);
   else if(kind=="bentConnector2")path.AddLines(new[]{new PointF(0,0),new PointF(width,0),new PointF(width,height)});
   else if(kind.StartsWith("bentConnector"))
   {
    var points=new List<PointF>{new(0,0),new(width*Adjust(0),0)};
    if(kind=="bentConnector4"||kind=="bentConnector5")
    {
     points.Add(new(width*Adjust(0),height*Adjust(1)));points.Add(new(kind=="bentConnector5"?width*Adjust(2):width,height*Adjust(1)));
     if(kind=="bentConnector5")points.Add(new(width*Adjust(2),height));
    }
    else points.Add(new(width*Adjust(0),height));
    points.Add(new(width,height));path.AddLines(points.ToArray());
   }
   else path.AddLine(0,0,width,height);
   return path;
  }
 }
}
