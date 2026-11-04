using System;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using ZenQOffice.UI;
using ZenQOffice.Core;
class ShapeContextMenuTests
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 [STAThread]static int Main(){Application.EnableVisualStyles();try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/context-menu");
  var deck=new PresentationRoot();deck.Slides.Clear();deck.Slides.Add(new SlideNode());deck.Slides[0].Shapes.Add(new ShapeNode{Type=ShapeType.RoundedRectangle});
  var view=new SlidesView{Presentation=deck,ActiveSlideIndex=0,SelectedShapeIndex=0};
  using var host=new Form{Size=new Size(700,700),ShowInTaskbar=false,Opacity=0};host.Show();
  using(var prewarm=new Bitmap(20,20))using(var graphics=Graphics.FromImage(prewarm))using(var drawing=new D2DGraphics(graphics,20,20))drawing.Clear(Color.White);
  var watch=Stopwatch.StartNew();using var menu=view.BuildShapeContextMenu(host,new Point(20,20),0);watch.Stop();Console.WriteLine("Build: "+watch.ElapsedMilliseconds+" ms");
  for(int trial=0;trial<3;trial++){var timer=Stopwatch.StartNew();using var repeat=view.BuildShapeContextMenu(host,new Point(20,20),0);Console.WriteLine("Warm build "+trial+": "+timer.ElapsedMilliseconds+" ms");}
  Check(menu.Items.Count<24,"Shape menu no longer exposes every advanced command at the root");
  Check(menu.Items[0].Text=="Cắt"&&!menu.Items.OfType<ToolStripControlHost>().Any(h=>h.Control is ToolStrip),"Formatting toolbar is removed from the context menu");
  var more=menu.Items.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Thao tác khác");
  var effects=more.DropDownItems.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Hiệu ứng");
  Check(effects.DropDownItems.OfType<ToolStripMenuItem>().All(item=>item.DropDownItems.Count==1),"Advanced effect options stay unbuilt until requested");
  var paste=menu.Items.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Dán");
  Check(!paste.CanSelect,"Paste group heading cannot receive the menu hover selection");
  Check(paste.Font.Style==menu.Font.Style&&paste.Font.Size==menu.Font.Size,"Paste heading uses the normal command font");
  watch.Restart();menu.Show(host,new Point(20,20));menu.Update();watch.Stop();Console.WriteLine("Open and paint: "+watch.ElapsedMilliseconds+" ms; size "+menu.Size);
  Check(menu.Width<=290&&menu.Height<620,"The root popup fits a normal desktop without the old full-height scroll panel");
  using(var shot=new Bitmap(menu.Width,menu.Height)){menu.DrawToBitmap(shot,menu.ClientRectangle);foreach(var item in menu.Items.OfType<ToolStripControlHost>())item.Control.DrawToBitmap(shot,item.Bounds);shot.Save("output/context-menu/shape-menu.png");}
  var advanced=menu.Items.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Thao tác khác");Check(advanced.DropDownItems.Count==8,"Picture, action, quiz and selection commands remain accessible");
  var format=menu.Items.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Định dạng hình…");format.PerformClick();Check(view.IsSidebarOpen&&view.SidebarPanel==SidebarPanelType.Properties,"Formatting action still opens the properties panel");
  menu.Close();
  var openState=typeof(FluentContextMenuStrip).Assembly.GetType("ZenQOffice.UI.FluentUi").GetProperty("IsMenuOpen",BindingFlags.Static|BindingFlags.NonPublic);
  Check(!(bool)openState.GetValue(null),"Closing root and toolbar menus releases the editor repaint pause");
  for(int trial=0;trial<2;trial++)
  {
   using var repeat=view.BuildShapeContextMenu(host,new Point(20,20),0);watch.Restart();repeat.Show(host,new Point(20,20));repeat.Update();watch.Stop();Console.WriteLine("Warm open and paint "+trial+": "+watch.ElapsedMilliseconds+" ms");repeat.Close();
  }
  using var lazyMenu=new FluentContextMenuStrip();var lazy=new ToolStripMenuItem("Lazy");lazyMenu.Items.Add(lazy);int builds=0;
  typeof(FluentContextMenuStrip).GetMethod("PopulateOnDemand",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{lazy,(Action)(()=>{builds++;lazy.DropDownItems.Add("Real command");})});
  lazyMenu.Show(host,new Point(20,20));Check(builds==0,"Opening the parent never builds the child menu");lazy.ShowDropDown();Check(builds==1&&lazy.DropDownItems[0].Text=="Real command","Opening a child populates commands before display");lazy.HideDropDown();lazy.ShowDropDown();Check(builds==1,"Reopening the same submenu reuses its commands");lazyMenu.Close();host.Close();
  Console.WriteLine("PASS: compact layout without toolbar, aligned normal-size paste heading, preserved commands, deferred effect construction, first-open rendering and single-build submenus");
 }
}