using System;using System.Drawing;using System.Drawing.Drawing2D;
namespace ZenQOffice.UI { public static partial class ShapeRenderer {
 public static string OfficePresetFromOpenXml(string preset){
  if(preset.StartsWith("actionButton"))return "ActionButton"+(preset.Substring(12) switch {"BackPrevious"=>"Back","ForwardNext"=>"Forward",var other=>other});
  if(preset.StartsWith("flowChart"))return "Flowchart"+(preset.Substring(9) switch {"InputOutput"=>"Data","PunchedCard"=>"Card","MagneticDrum"=>"DirectAccessStorage","OfflineStorage"=>"StoredData",var other=>other});
  return "Rectangle";
 }
 static bool TryOfficePreset(string type,Rectangle bounds,out GraphicsPath path){
  var p=new GraphicsPath(FillMode.Alternate);path=p;
  void Poly(params float[] xy){var points=new PointF[xy.Length/2];for(int i=0;i<points.Length;i++)points[i]=new PointF(xy[i*2],xy[i*2+1]);p.StartFigure();p.AddPolygon(points);}
  void Rect(float x,float y,float w,float h){p.StartFigure();p.AddRectangle(new RectangleF(x,y,w,h));}
  void Ell(float x,float y,float w,float h){p.StartFigure();p.AddEllipse(x,y,w,h);}
  void Line(params float[] xy){p.StartFigure();var points=new PointF[xy.Length/2];for(int i=0;i<points.Length;i++)points[i]=new PointF(xy[i*2],xy[i*2+1]);p.AddLines(points);}
  switch(type){
   case "SmileFace":Ell(0,0,1000,1000);Ell(270,280,90,110);Ell(640,280,90,110);p.StartFigure();p.AddArc(220,300,560,480,15,150);break;
   case "Sun":{var points=new PointF[48];for(int i=0;i<points.Length;i++){double a=-Math.PI/2+i*Math.PI/24;float r=i%2==0?500:350;points[i]=new PointF(500+(float)Math.Cos(a)*r,500+(float)Math.Sin(a)*r);}p.AddPolygon(points);break;}
   case "Cloud":case "CalloutCloud":
    p.AddBezier(150,780,-90,700,-40,420,160,400);p.AddBezier(160,400,100,170,330,110,440,240);p.AddBezier(440,240,550,-30,860,80,840,320);p.AddBezier(840,320,1100,290,1100,690,890,730);p.AddBezier(890,730,870,980,600,1020,490,860);p.AddBezier(490,860,350,1050,100,970,150,780);p.CloseFigure();if(type=="CalloutCloud"){Ell(50,910,100,60);Ell(0,980,40,30);}break;
   case "Cube":Poly(220,0,1000,0,1000,780,780,1000,0,1000,0,220);Line(0,220,780,220);Line(780,220,1000,0);Line(780,220,780,1000);break;
   case "SnipSameSideCorner":Poly(200,0,800,0,1000,200,1000,1000,0,1000,0,200);break;
   case "FlowchartAlternateProcess":using(var rounded=CreateShapePath("RoundedRectangle",new Rectangle(0,0,1000,1000)))p.AddPath(rounded,false);break;
   case "FlowchartPredefinedProcess":Rect(0,0,1000,1000);Line(150,0,150,1000);Line(850,0,850,1000);break;
   case "FlowchartInternalStorage":Rect(0,0,1000,1000);Line(150,0,150,1000);Line(0,150,1000,150);break;
   case "FlowchartMultidocument":
    Line(150,0,1000,0);Line(1000,0,1000,780);Line(70,80,920,80);Line(920,80,920,860);
    p.StartFigure();p.AddLines(new[]{new PointF(0,160),new PointF(840,160),new PointF(840,880)});p.AddBezier(840,880,560,690,280,1080,0,880);p.CloseFigure();break;
   case "FlowchartManualInput":Poly(0,220,1000,0,1000,1000,0,1000);break;
   case "FlowchartManualOperation":Poly(0,0,1000,0,780,1000,220,1000);break;
   case "FlowchartConnector":case "FlowchartSummingJunction":case "FlowchartOr":Ell(0,0,1000,1000);if(type=="FlowchartSummingJunction"){Line(150,150,850,850);Line(850,150,150,850);}if(type=="FlowchartOr"){Line(0,500,1000,500);Line(500,0,500,1000);}break;
   case "FlowchartOffpageConnector":Poly(0,0,1000,0,1000,650,500,1000,0,650);break;
   case "FlowchartCard":Poly(200,0,1000,0,1000,1000,0,1000,0,200);break;
   case "FlowchartPunchedTape":p.AddBezier(0,100,330,350,660,-150,1000,100);p.AddLine(1000,100,1000,900);p.AddBezier(1000,900,660,650,330,1150,0,900);p.CloseFigure();break;
   case "FlowchartSort":Poly(500,0,1000,500,500,1000,0,500);Line(0,500,1000,500);break;
   case "FlowchartCollate":Poly(0,0,1000,0,0,1000,1000,1000);break;
   case "FlowchartExtract":Poly(500,0,1000,1000,0,1000);break;
   case "FlowchartMerge":Poly(0,0,1000,0,500,1000);break;
   case "FlowchartDelay":p.AddLine(0,0,500,0);p.AddArc(0,0,1000,1000,270,180);p.AddLine(500,1000,0,1000);p.CloseFigure();break;
   case "FlowchartDisplay":p.AddLines(new[]{new PointF(200,0),new PointF(750,0)});p.AddBezier(750,0,1080,0,1080,1000,750,1000);p.AddLines(new[]{new PointF(750,1000),new PointF(200,1000),new PointF(0,500)});p.CloseFigure();break;
   case "FlowchartStoredData":p.AddBezier(200,0,-70,0,-70,1000,200,1000);p.AddLine(200,1000,1000,1000);p.AddBezier(1000,1000,730,1000,730,0,1000,0);p.CloseFigure();break;
   case "FlowchartSequentialAccessStorage":Ell(0,0,900,1000);Line(450,1000,1000,1000);break;
   case "FlowchartMagneticDisk":using(var disk=CreateShapePath("Cylinder",new Rectangle(0,0,1000,1000)))p.AddPath(disk,false);break;
   case "FlowchartDirectAccessStorage":p.AddArc(0,0,300,1000,90,180);p.AddLine(150,0,850,0);p.AddArc(700,0,300,1000,270,180);p.CloseFigure();Ell(700,0,300,1000);break;
   case "FlowchartPreparation":return FinishAlias("Hexagon",bounds,p,out path);
   case "FlowchartOnlineStorage":return FinishAlias("FlowchartStoredData",bounds,p,out path);
   case "Bevel":Rect(0,0,1000,1000);Rect(150,150,700,700);Line(0,0,150,150);Line(1000,0,850,150);Line(0,1000,150,850);Line(1000,1000,850,850);break;
   case "Heptagon":case "Dodecagon":{int n=type=="Heptagon"?7:12;var points=new PointF[n];for(int i=0;i<n;i++){double a=-Math.PI/2+i*2*Math.PI/n;points[i]=new PointF(500+(float)Math.Cos(a)*500,500+(float)Math.Sin(a)*500);}p.AddPolygon(points);break;}
   case "Pie":p.AddPie(0,0,1000,1000,0,270);break;
   case "Chord":p.AddArc(0,0,1000,1000,0,270);p.CloseFigure();break;
   case "Frame":Rect(0,0,1000,1000);Rect(180,180,640,640);break;
   case "HalfFrame":Poly(0,0,1000,0,800,200,200,200,200,800,0,1000);break;
   case "Corner":Poly(0,0,250,0,250,750,1000,750,1000,1000,0,1000);break;
   case "FoldedCorner":Poly(0,0,1000,0,1000,750,750,1000,0,1000);Line(750,1000,750,750,1000,750);break;
   case "Plaque":p.AddLine(200,0,800,0);p.AddArc(800,-200,400,400,180,-90);p.AddLine(1000,200,1000,800);p.AddArc(800,800,400,400,270,-90);p.AddLine(800,1000,200,1000);p.AddArc(-200,800,400,400,0,-90);p.AddLine(0,800,0,200);p.AddArc(-200,-200,400,400,90,-90);p.CloseFigure();break;
   case "BracePair":case "LeftBrace":case "RightBrace":case "BracketPair":case "LeftBracket":case "RightBracket":{
    bool left=type.Contains("Left")||type.EndsWith("Pair"),right=type.Contains("Right")||type.EndsWith("Pair");
    foreach(bool mirror in new[]{false,true}){if(mirror?!right:!left)continue;using var half=new GraphicsPath();if(type.Contains("Brace")){half.AddBezier(300,0,80,0,300,500,0,500);half.AddBezier(0,500,300,500,80,1000,300,1000);}else half.AddLines(new[]{new PointF(300,0),new PointF(0,0),new PointF(0,1000),new PointF(300,1000)});if(mirror){using var matrix=new Matrix(-1,0,0,1,1000,0);half.Transform(matrix);}p.AddPath(half,false);}break;}
   case "LineCallout3":case "LineCallout1NoBorder":case "LineCallout2NoBorder":case "LineCallout3NoBorder":
    if(!type.EndsWith("NoBorder"))Rect(200,0,800,700);Line(400,300,300,600,100,700,0,1000);break;
   case "RibbonUp":case "RibbonDown":case "RibbonCurvedUp":case "RibbonCurvedDown":
    Poly(0,200,200,200,200,0,800,0,800,200,1000,200,900,550,1000,900,750,900,750,700,250,700,250,900,0,900,100,550);Line(200,200,200,700);Line(200,700,800,700);Line(800,700,800,200);Line(250,700,250,900);Line(750,700,750,900);if(type.EndsWith("Up")){using var flip=new Matrix(1,0,0,-1,0,1000);p.Transform(flip);}break;
   case "StripedRightArrow":Poly(300,200,650,200,650,0,1000,500,650,1000,650,800,300,800);Rect(150,200,90,600);Rect(0,200,80,600);break;
   case "NotchedRightArrow":Poly(0,200,650,200,650,0,1000,500,650,1000,650,800,0,800,200,500);break;
   case "PentagonArrow":Poly(0,0,650,0,1000,500,650,1000,0,1000);break;
   case "LeftRightUpArrow":Poly(0,650,220,430,220,560,430,560,430,220,300,220,500,0,700,220,570,220,570,560,780,560,780,430,1000,650,780,870,780,740,220,740,220,870);break;
   default:
    if(type!=null&&type.StartsWith("ActionButton")){
     Rect(0,0,1000,1000);string action=type.Substring(12);
     if(action=="Back"||action=="Beginning"){Poly(680,180,260,500,680,820);if(action=="Beginning")Rect(160,180,65,640);}
     else if(action=="Forward"||action=="End"){Poly(320,180,740,500,320,820);if(action=="End")Rect(775,180,65,640);}
     else if(action=="Home")Poly(180,460,500,160,820,460,720,460,720,820,550,820,550,620,450,620,450,820,280,820,280,460);
     else if(action=="Document"){Poly(300,180,570,180,740,350,740,820,300,820);Line(570,180,570,350,740,350);}
     else if(action=="Sound"){Poly(180,390,350,390,600,200,600,800,350,610,180,610);p.StartFigure();p.AddArc(300,230,500,540,-55,110);}
     else if(action=="Movie"){Rect(180,230,640,540);for(int n=0;n<4;n++){Rect(220+n*150,260,70,70);Rect(220+n*150,670,70,70);}}
     else if(action=="Return"){Poly(180,400,420,180,420,330,760,330,760,700,400,700,400,580,630,580,630,460,420,460,420,620);}
     else if(action=="Information"||action=="Help"){using var font=new FontFamily("Arial");p.AddString(action=="Help"?"?":"i",font,(int)FontStyle.Bold,850,new PointF(280,0),StringFormat.GenericTypographic);}
    }else{p.Dispose();path=null;return false;}break;
  }
  using var transform=new Matrix(bounds.Width/1000f,0,0,bounds.Height/1000f,bounds.X,bounds.Y);p.Transform(transform);return true;
 }
 static bool FinishAlias(string name,Rectangle bounds,GraphicsPath unused,out GraphicsPath result){unused.Dispose();result=CreateShapePath(name,bounds);return true;}
}}
