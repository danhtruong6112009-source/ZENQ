using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Windows.Forms;using ZenQOffice.UI;using ZenQOffice.Core;
class Test {
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static object Field(SlidesView v,string name)=>typeof(SlidesView).GetField(name,Private).GetValue(v);
 static void Action(SlidesView v,string action)=>typeof(SlidesView).GetMethod("ExecuteDrawerAction",Private).Invoke(v,new object[]{action,null});
 static Bitmap Paint(SlidesView v,int width=280,int height=620){var image=new Bitmap(width+100,height,PixelFormat.Format32bppRgb);using(var graphics=Graphics.FromImage(image))using(var g=new D2DGraphics(graphics,image.Width,image.Height)){g.Clear(Color.FromArgb(240,240,240));typeof(SlidesView).GetMethod("DrawElementsDrawerPanel",Private).Invoke(v,new object[]{g,width,0,height});Check(g.DebugClipDepth==0&&g.DebugStateStackCount==0,"Catalog restores drawing state");}return image;}
 [STAThread]static void Main(){Directory.CreateDirectory("output/utility-catalog");using var host=new Form{ClientSize=new Size(900,700)};var view=new SlidesView{Presentation=new PresentationRoot(),ActiveSlideIndex=0,IsElementsDrawerOpen=true,ElementsDrawerSelectedTab=7,ParentControl=host};
 using(var first=Paint(view))first.Save("output/utility-catalog/apps.png");Check((int)Field(view,"catalogContentHeight")>530,"Apps extend beyond the visible region");view.ScrollElementsCatalog(-12000);for(int i=0;i<35;i++)view.UpdateSmoothScroll();using(var last=Paint(view))last.Save("output/utility-catalog/apps-bottom.png");Check(view.DrawerItemActions.Contains("app:draw"),"Can scroll to last utility");float end=view.TargetElementsScrollY;view.ScrollElementsCatalog(-12000);Check(view.TargetElementsScrollY==end,"Scrolling clamps to content end");
 var search=(TextBox)Field(view,"catalogSearchBox");Check(search.BackColor==Color.FromArgb(248,250,252),"Search editor matches its rounded field without a white rectangle");view.ScrollElementsCatalog(12000);view.ScrollElementsCatalog(-120);Check(view.ElementsScrollY==36&&view.TargetElementsScrollY==36&&view.SmoothElementsScrollY==36,"Catalog wheel moves 36 pixels and stops immediately");search.Text="cong thuc";using(var filtered=Paint(view))filtered.Save("output/utility-catalog/search.png");Check(view.ElementsScrollY==0&&view.DrawerItemActions.Contains("app:equation")&&!view.DrawerItemActions.Contains("app:chart"),"Accent-insensitive search filters real apps and resets scroll");
 search.Clear();view.ElementsDrawerSelectedTab=2;using(var fonts=Paint(view)){fonts.Save("output/utility-catalog/fonts.png");for(int y=100;y<600;y+=5)Check(fonts.GetPixel(290,y).R==240,"Font preview never spills into workspace");}Check(!view.DrawerItemActions.Any(a=>a.Contains("Icons")),"Icon-only fonts are omitted from text previews");view.ScrollElementsCatalog(-12000);for(int i=0;i<35;i++)view.UpdateSmoothScroll();using(var fontBottom=Paint(view))fontBottom.Save("output/utility-catalog/fonts-bottom.png");Check(view.DrawerItemActions.Contains("text:body"),"Font list reaches text insertion controls");
 Action(view,"app:chart");Check(view.SelectedShape.ChartData!=null,"Chart app inserts editable chart data");Action(view,"app:table");Check(view.SelectedShape.Type==ShapeType.Table,"Table app inserts a real table");Action(view,"app:shadow");Check(view.SelectedShape.HasShadow,"Shadow utility applies a real shadow");using(var photo=new Bitmap(100,60))view.InsertImageBitmap(photo,"Test image");view.SelectedShape.Type=ShapeType.CustomPath;view.SelectedShape.CustomPath=new System.Drawing.Drawing2D.GraphicsPath();view.SelectedShape.CustomPath.AddRectangle(new RectangleF(0,0,100,60));view.SelectedShape.PathWidth=100;view.SelectedShape.PathHeight=60;Action(view,"app:frame");Check(view.SelectedShape.CropShapeType=="RoundedRectangle"&&view.SelectedShape.CustomPath==null&&view.SelectedShape.Type==ShapeType.Image,"Frame utility changes imported picture-fill framing");Action(view,"app:gradient");Check(view.IsSidebarOpen&&view.SidebarPanel==SidebarPanelType.ColorPalettePicker,"Gradient utility opens the editing pane");Action(view,"font:apply:Arial");Check(view.SelectedShape.FontName=="Arial"&&view.SelectedShape.Type==ShapeType.TextBox&&!view.SelectedShape.HasFill,"Font selection inserts unfilled editable text when an image was selected");
 // Repainting the full slide view must not hide/show the native search editor.
 host.Show();view.IsSidebarOpen=false;view.ElementsDrawerSelectedTab=7;
 using(var frame=new Bitmap(900,700))using(var graphics=Graphics.FromImage(frame))using(var g=new D2DGraphics(graphics,900,700))view.Draw(g,900,700);
 int visibilityChanges=0;search.VisibleChanged+=(s,e)=>visibilityChanges++;
 for(int i=0;i<4;i++){using var frame=new Bitmap(900,700);using var graphics=Graphics.FromImage(frame);using var g=new D2DGraphics(graphics,900,700);view.Draw(g,900,700);}
 Check(search.Visible&&visibilityChanges==0,"Full repaint keeps the search control visible without flicker");
 view.ElementsDrawerSelectedTab=1;Check(!search.Visible,"Switching to Graphics immediately hides the catalog search");
 view.ShapeTaskPane=new WpfShapeTaskPane{Mode=ShapePaneMode.GraphicsOnly};using(var frame=Paint(view))frame.Save("output/utility-catalog/graphics-switch.png");
 var graphicsSearch=(TextBox)typeof(SlidesView).GetField("_shapeSearchBox",Private).GetValue(view);Check(!search.Visible&&graphicsSearch.Visible,"Only the Graphics search remains visible");
 view.ElementsDrawerSelectedTab=7;using(var frame=Paint(view)){}Check(search.Visible&&!graphicsSearch.Visible,"Switching back restores only the catalog search");
 // Captured drag clamps both extremes; wheel and pending smooth ticks cannot fight it.
 using(var feed=new VirtualChatFeedPanel{Size=new Size(220,300)}){
 host.Controls.Add(feed);feed.CreateControl();var type=typeof(VirtualChatFeedPanel);
 type.GetField("_totalContentHeight",Private).SetValue(feed,2000);
 void Mouse(string method,int y)=>type.GetMethod(method,Private).Invoke(feed,new object[]{new MouseEventArgs(MouseButtons.Left,1,218,y,0)});
 float Position()=>(float)type.GetField("_targetScrollY",Private).GetValue(feed);
 Mouse("OnMouseDown",8);Check(feed.Capture,"AI scrollbar captures drag outside the panel");
 Mouse("OnMouseMove",100000);Check(Position()==1700,"AI drag clamps at bottom");
 for(int i=0;i<20;i++){feed.DoMouseWheel(120);Mouse("OnMouseMove",100000+i);}Check(Position()==1700,"Overshoot and wheel events keep the bottom position stable");
 Mouse("OnMouseMove",-100000);Check(Position()==0,"AI drag clamps at top");Mouse("OnMouseUp",-100000);Check(!feed.Capture,"Mouse-up outside releases AI capture");host.Controls.Remove(feed);
 }
 view.IsElementsDrawerOpen=false;view.ZoomLevel=2;view.GetEffectiveLayoutMetrics(900,700,out var sidebar,out var cx,out var cw,out var cy,out var ch,out var strip);
 view.GetScrollLimits(cw,ch,out var minX,out var maxX,out var minY,out var maxY);
 view.IsDraggingVScrollBar=true;view.ScrollBarDragStartMouseY=300;view.ScrollBarDragStartScrollY=0;
 for(int i=0;i<12;i++){view.HandleMouseMove(new Point(890,100000+i),900,700);view.UpdateSmoothScroll();Check(view.CanvasScrollY==maxY&&view.TargetCanvasScrollY==maxY,"Slide scrollbar stays pinned at bottom outside its track");}
 for(int i=0;i<12;i++){view.HandleMouseMove(new Point(890,-100000-i),900,700);view.UpdateSmoothScroll();Check(view.CanvasScrollY==minY&&view.TargetCanvasScrollY==minY,"Slide scrollbar stays pinned at top outside its track");}
 view.HandleMouseUp(new Point(890,-100000),900,700);Check(!view.IsDraggingVScrollBar,"Slide drag ends outside the viewport");
 host.Hide();
 Console.WriteLine("PASS: stable search visibility across full repaints and Graphics switches; captured AI and Slide drags remain clamped outside both ends; catalog and utility actions");
 }
}