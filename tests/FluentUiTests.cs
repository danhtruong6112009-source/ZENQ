using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ZenQOffice.UI;
class FluentUiTests
{
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 [STAThread]static int Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Paint(Control c,string name)
 {
  using var b=new Bitmap(c.Width,c.Height);
  using(var graphics=Graphics.FromImage(b))using(var e=new PaintEventArgs(graphics,c.ClientRectangle))c.GetType().GetMethod("OnPaint",Private).Invoke(c,new object[]{e});
  b.Save("output/fluent-ui/"+name+".png");
 }
 static Form Dialog(MessageBoxButtons buttons,MessageBoxDefaultButton defaultButton=MessageBoxDefaultButton.Button1)
 {
  var type=typeof(FluentMessageBox).Assembly.GetType("ZenQOffice.UI.FluentMessageDialog");
  return (Form)Activator.CreateInstance(type,Private,null,new object[]{"Giữ nội dung đang chỉnh sửa trước khi đóng?","ZenQ Office",buttons,MessageBoxIcon.Question,defaultButton},null);
 }
 static void Run()
 {
  Directory.CreateDirectory("output/fluent-ui");
  using var combo=new FluentComboBox{Size=new Size(240,28),DropDownStyle=ComboBoxStyle.DropDown};
  combo.Items.AddRange(new object[]{"Calibri","Segoe UI","Times New Roman"});combo.SelectedIndex=1;
  Check(combo.SelectedItem.ToString()=="Segoe UI","Combo selection API is unchanged");combo.Text="Font custom";Check(combo.Text=="Font custom","Editable combo retains free text");
  using(var b=new Bitmap(240,160))
  {
   using(var graphics=Graphics.FromImage(b))
   {
    graphics.Clear(Color.White);using var args=new DrawItemEventArgs(graphics,combo.Font,new Rectangle(0,90,240,28),1,DrawItemState.Selected);
    combo.GetType().GetMethod("OnDrawItem",Private).Invoke(combo,new object[]{args});
   }
   Check(b.GetPixel(230,100).B>240&&b.GetPixel(230,100).R<245,"Dropdown rows below closed control height actually render");b.Save("output/fluent-ui/combo-row.png");
  }
  bool customCalled=false;combo.DrawItem+=(s,e)=>{customCalled=true;e.Graphics.FillRectangle(Brushes.Magenta,1,1,3,3);};
  using(var b=new Bitmap(240,40))using(var g=Graphics.FromImage(b))using(var args=new DrawItemEventArgs(g,combo.Font,new Rectangle(0,0,240,28),0,DrawItemState.None))combo.GetType().GetMethod("OnDrawItem",Private).Invoke(combo,new object[]{args});
  Check(customCalled,"Existing custom font/color owner-draw callbacks are preserved");
  using var list=new FluentListBox{SelectionMode=SelectionMode.MultiExtended};list.Items.AddRange(new object[]{"A","B","C"});list.SetSelected(0,true);list.SetSelected(2,true);Check(list.SelectedItems.Count==2,"Multi-selection still works");
  using var tabs=new FluentTabControl{Size=new Size(460,220)};tabs.TabPages.Add("Định dạng");tabs.TabPages.Add("Căn lề");tabs.TabPages.Add("Đường viền");tabs.SelectedIndex=2;var handle=tabs.Handle;Check(tabs.SelectedTab.Text=="Đường viền","Tab navigation/selection retain native state");Paint(tabs,"tabs");
  var themeType=typeof(FluentMenuRenderer).Assembly.GetType("ZenQOffice.UI.FluentUi");var mnemonicMethod=themeType.GetMethod("MnemonicText",BindingFlags.Static|BindingFlags.NonPublic);
  object[] mnemonicArgs={"AT&&T · &Chọn",-1};var plain=(string)mnemonicMethod.Invoke(null,mnemonicArgs);
  Check(plain=="AT&T · Chọn"&&(int)mnemonicArgs[1]==7,"Menu captions preserve escaped ampersands and accelerator positions");
  using var menu=new FluentContextMenuStrip();var root=new ToolStripMenuItem("&Chọn"){CheckOnClick=true};root.DropDownItems.Add("Con");menu.Items.Add(root);menu.Items.Add(new ToolStripMenuItem("Tắt"){Enabled=false});menu.GetType().GetMethod("OnOpening",Private).Invoke(menu,new object[]{new System.ComponentModel.CancelEventArgs()});
  Check(menu.Renderer is FluentMenuRenderer,"Root menu uses the shared renderer; child menus initialize when opened");root.PerformClick();Check(root.Checked,"Menu commands/check states still execute");Check(!menu.Items[1].Enabled,"Disabled actions stay disabled");
  using(var bitmap=new Bitmap(240,100))
  {
   menu.Size=new Size(240,100);using(var g=Graphics.FromImage(bitmap))menu.Renderer.DrawToolStripBackground(new ToolStripRenderEventArgs(g,menu));bitmap.Save("output/fluent-ui/menu-background.png");
  }
  using(var host=new Form{Size=new Size(600,500),ShowInTaskbar=false,Opacity=0})
  using(var popup=new FluentContextMenuStrip())
  {
   var branch=new ToolStripMenuItem("Menu con");branch.DropDownItems.Add("Lệnh con");popup.Items.Add(branch);
   var leaf=new ToolStripMenuItem("Lệnh khác");popup.Items.Add(leaf);
   host.Show();popup.Show(host,new Point(20,20));
   Check(popup.Region!=null&&!popup.Region.IsVisible(popup.Width-1,popup.Height-1)&&!popup.Region.IsVisible(0,0),"Popup clips both opposite corners, including bottom right");
   branch.Select();
   SendMessage(popup.Handle,0x200,IntPtr.Zero,(IntPtr)((branch.Bounds.X+5)|((branch.Bounds.Y+5)<<16)));
   Check(branch.DropDown.Visible,"Hover opens a submenu synchronously without the system menu delay");
   Check(branch.DropDown.Region!=null&&!branch.DropDown.Region.IsVisible(branch.DropDown.Width-1,branch.DropDown.Height-1),"Nested popup clips its bottom-right corner too");
   leaf.Select();
   SendMessage(popup.Handle,0x200,IntPtr.Zero,(IntPtr)((leaf.Bounds.X+5)|((leaf.Bounds.Y+5)<<16)));
   Check(!branch.DropDown.Visible,"Moving to another command closes the previous submenu immediately");
   Check(!popup.DropShadowEnabled&&!branch.DropDown.DropShadowEnabled,"Root and nested menus disable the heavy native offset shadow");
   using(var shot=new Bitmap(popup.Width,popup.Height))
   {
    popup.DrawToBitmap(shot,popup.ClientRectangle);shot.Save("output/fluent-ui/popup-content.png");
    int visibleText=0;for(int y=leaf.Bounds.Top+3;y<leaf.Bounds.Bottom-3;y++)for(int x=32;x<popup.Width-20;x++){var pixel=shot.GetPixel(x,y);if(pixel.R<150&&pixel.G<150&&pixel.B<150)visibleText++;}
    Check(visibleText>20,"Selected leaf command keeps readable dark text on the light hover background");
   }
   popup.Close();host.Close();
  }
  using(var host=new Form{ShowInTaskbar=false,Opacity=0})using(var longMenu=new FluentContextMenuStrip{MaximumSize=new Size(380,320)})
  {
   for(int i=0;i<35;i++)longMenu.Items.Add("Lệnh menu dài "+i);
   host.Show();longMenu.Show(host,new Point(10,10));
   using var shot=new Bitmap(longMenu.Width,longMenu.Height);longMenu.DrawToBitmap(shot,longMenu.ClientRectangle);
   int checkedRows=0;
   foreach(ToolStripItem item in longMenu.Items)
   {
    var r=item.Bounds;if(r.Top<20||r.Bottom>longMenu.Height-20)continue;
    int dark=0;for(int y=r.Top+3;y<r.Bottom-3;y++)for(int x=32;x<longMenu.Width-20;x++){var c=shot.GetPixel(x,y);if(c.R<150&&c.G<150&&c.B<150)dark++;}
    Check(dark>20,"Every visible row of a scrolling menu retains its caption");checkedRows++;
   }
   Check(checkedRows>=4,"The scrolling popup checks several independently rendered rows");shot.Save("output/fluent-ui/scrolling-popup.png");longMenu.Close();host.Close();
  }
  using(var bitmap=new Bitmap(500,40))using(var graphics=Graphics.FromImage(bitmap))
  {
   var args=new ToolStripItemTextRenderEventArgs(graphics,root,"Cached caption",new Rectangle(35,0,400,30),Color.Black,menu.Font,TextFormatFlags.Left);
   graphics.Clear(Color.White);menu.Renderer.DrawItemText(args);
   int ink=0;for(int y=0;y<30;y++)for(int x=35;x<435;x++){var pixel=bitmap.GetPixel(x,y);if(pixel.R<150&&pixel.G<150&&pixel.B<150)ink++;}
   Check(ink>50,"Cached caption retains visible dark glyphs after DC alpha compositing");
   menu.Renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(graphics,root,"",args.TextRectangle,Color.Black,menu.Font,TextFormatFlags.Right));
   menu.Renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(graphics,root,"Ctrl+K",args.TextRectangle,Color.Black,menu.Font,TextFormatFlags.Right));
   int retained=0;for(int y=0;y<30;y++)for(int x=35;x<180;x++){var c=bitmap.GetPixel(x,y);if(c.R<150&&c.G<150&&c.B<150)retained++;}
   Check(retained>50,"Empty and populated shortcut passes cannot erase the command caption");
   bitmap.Save("output/fluent-ui/cached-caption.png");
   var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<100;i++)menu.Renderer.DrawItemText(args);watch.Stop();
   Check(watch.ElapsedMilliseconds<500,"Repeated menu hover redraws reuse rendered text instead of submitting every glyph to the GPU again");
   Console.WriteLine("100 cached menu captions: "+watch.ElapsedMilliseconds+" ms");
  }
  int TextHeight(float dpi)
  {
   using var image=new Bitmap(260,48);image.SetResolution(dpi,dpi);
   using(var graphics=Graphics.FromImage(image))
   {
    graphics.Clear(Color.White);
    menu.Renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(graphics,root,"Menu caption",new Rectangle(8,0,240,48),Color.Black,menu.Font,TextFormatFlags.VerticalCenter));
   }
   int min=48,max=-1;for(int y=0;y<48;y++)for(int x=8;x<245;x++){var c=image.GetPixel(x,y);if(c.R<140&&c.G<140&&c.B<140){min=Math.Min(min,y);max=Math.Max(max,y);}}
   return max-min+1;
  }
  Check(TextHeight(144)>TextHeight(96)*1.25f,"Menu text follows display DPI instead of remaining thin and undersized at high scaling");
  foreach(var buttons in Enum.GetValues<MessageBoxButtons>())
  {
   using var dialog=Dialog(buttons,MessageBoxDefaultButton.Button2);var actions=dialog.Controls.OfType<FlowLayoutPanel>().Single();var actual=actions.Controls.OfType<Button>().Select(b=>b.DialogResult).ToArray();
   Check(actual.Length>=1&&actual.Distinct().Count()==actual.Length,"Message button results remain distinct: "+buttons);
   Check(dialog.AcceptButton!=null,"Every message has the requested default action: "+buttons);
   if(buttons==MessageBoxButtons.YesNo)Check(!dialog.ControlBox&&dialog.CancelButton==null,"Yes/No cannot silently resolve to Cancel");
   if(buttons==MessageBoxButtons.OKCancel)Check(((Button)dialog.CancelButton).DialogResult==DialogResult.Cancel,"Escape keeps Cancel semantics");
   if(buttons==MessageBoxButtons.YesNoCancel)Check(((Button)dialog.AcceptButton).DialogResult==DialogResult.No,"Button2 keeps the caller's default No");
  }
  using(var dialog=Dialog(MessageBoxButtons.OK))
  {
   var close=new FormClosingEventArgs(CloseReason.UserClosing,false);dialog.GetType().GetMethod("OnFormClosing",Private).Invoke(dialog,new object[]{close});Check(dialog.DialogResult==DialogResult.OK,"Closing single OK resolves to OK");
   Paint(dialog,"message-frame");
  }
  using(var dialog=Dialog(MessageBoxButtons.YesNo))
  {
   dialog.DialogResult=DialogResult.Cancel;var close=new FormClosingEventArgs(CloseReason.UserClosing,false);dialog.GetType().GetMethod("OnFormClosing",Private).Invoke(dialog,new object[]{close});Check(close.Cancel&&dialog.DialogResult==DialogResult.None,"Alt-F4 cannot bypass a Yes/No decision");
  }
  using(var dialog=Dialog(MessageBoxButtons.YesNoCancel,MessageBoxDefaultButton.Button2))
  using(var timer=new System.Windows.Forms.Timer{Interval=40})
  {
   dialog.Opacity=0;
   timer.Tick+=(s,e)=>
   {
    timer.Stop();using(var picture=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height))
    {
     using(var graphics=Graphics.FromImage(picture))using(var args=new PaintEventArgs(graphics,dialog.ClientRectangle))dialog.GetType().GetMethod("OnPaint",Private).Invoke(dialog,new object[]{args});
     foreach(Control child in dialog.Controls)if(child.Visible)child.DrawToBitmap(picture,child.Bounds);
     picture.Save("output/fluent-ui/message-complete.png");
    }
    ((Button)dialog.AcceptButton).PerformClick();
   };
   timer.Start();var result=dialog.ShowDialog();Check(result==DialogResult.No,"Real modal lifecycle returns the selected default No without falling through to Yes/Cancel");
  }
  Console.WriteLine("PASS: editable/native combo state, full popup row rendering, owner-draw callbacks, multi-select, tabs, nested menu commands and disabled/check states, all message button sets/defaults/cancel/close semantics");
 }
}
