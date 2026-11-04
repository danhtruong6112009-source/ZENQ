using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace ZenQOffice.UI {
 internal sealed class SettingsInputFrame:Panel {
  public SettingsInputFrame(TextBox input){DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(10,7,10,5);input.Dock=DockStyle.Fill;input.BackColor=Color.White;input.ForeColor=Color.FromArgb(35,45,60);Controls.Add(input);input.GotFocus+=(s,e)=>Invalidate();input.LostFocus+=(s,e)=>Invalidate();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var g=new D2DGraphics(e.Graphics,Width,Height);using var pen=new Pen(ContainsFocus?Color.FromArgb(99,102,241):Color.FromArgb(216,222,230),1);g.DrawRoundedRectangle(pen,new RectangleF(.5f,.5f,Width-1,Height-1),5);}
 }
 internal sealed class SettingsActionButton:Button {
  public bool Primary{get;set;}bool hover;
  public SettingsActionButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Font=new Font("Segoe UI",9.5f);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){bool primary=Primary || DialogResult==DialogResult.OK;using var g=new D2DGraphics(e.Graphics,Width,Height);g.Clear(Parent?.BackColor??Color.White);var rect=new RectangleF(.5f,.5f,Width-1,Height-1);using var fill=new SolidBrush(primary?(hover?Color.FromArgb(67,56,202):Color.FromArgb(79,70,229)):(hover?Color.FromArgb(238,241,246):Color.White));g.FillRoundedRectangle(fill,rect,5);if(!primary){using var border=new Pen(Color.FromArgb(216,222,230));g.DrawRoundedRectangle(border,rect,5);}FluentUi.Text(g,Text,Font,Enabled?(primary?Color.White:Color.FromArgb(45,55,70)):Color.Gray,rect,true);if(Focused){using var focus=new Pen(primary?Color.White:Color.FromArgb(99,102,241)){DashStyle=DashStyle.Dot};g.DrawRoundedRectangle(focus,new RectangleF(3,3,Width-6,Height-6),3);}}
 }
}
