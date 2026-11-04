using System;
using System.Drawing;
using System.Windows.Forms;
namespace ZenQOffice.UI
{
    public static class FluentMessageBox
    {
        public static DialogResult Show(string text,string caption="",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None,MessageBoxDefaultButton defaultButton=MessageBoxDefaultButton.Button1)
            =>Show(null,text,caption,buttons,icon,defaultButton);
        public static DialogResult Show(IWin32Window owner,string text,string caption="",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None,MessageBoxDefaultButton defaultButton=MessageBoxDefaultButton.Button1)
        {
            var control=owner as Control;
            if(control==null&&Application.OpenForms.Count>0)control=Application.OpenForms[0];
            if(control!=null&&control.InvokeRequired)
                return (DialogResult)control.Invoke(new Func<DialogResult>(()=>Show(control,text,caption,buttons,icon,defaultButton)));
            using var dialog=new FluentMessageDialog(text,caption,buttons,icon,defaultButton);
            return owner!=null?dialog.ShowDialog(owner):control!=null?dialog.ShowDialog(control):dialog.ShowDialog();
        }
    }
    internal sealed class FluentMessageDialog : Form
    {
        private readonly MessageBoxIcon _icon;
        private readonly bool _singleOk;
        private readonly bool _canCancel;
        private readonly Font _messageFont;
        internal readonly FlowLayoutPanel Actions;
        internal static DialogResult[] Results(MessageBoxButtons buttons)=>buttons switch
        {
            MessageBoxButtons.OKCancel=>new[]{DialogResult.OK,DialogResult.Cancel},
            MessageBoxButtons.YesNo=>new[]{DialogResult.Yes,DialogResult.No},
            MessageBoxButtons.YesNoCancel=>new[]{DialogResult.Yes,DialogResult.No,DialogResult.Cancel},
            MessageBoxButtons.RetryCancel=>new[]{DialogResult.Retry,DialogResult.Cancel},
            MessageBoxButtons.AbortRetryIgnore=>new[]{DialogResult.Abort,DialogResult.Retry,DialogResult.Ignore},
            MessageBoxButtons.CancelTryContinue=>new[]{DialogResult.Cancel,DialogResult.TryAgain,DialogResult.Continue},
            _=>new[]{DialogResult.OK}
        };
        internal FluentMessageDialog(string text,string caption,MessageBoxButtons buttons,MessageBoxIcon icon,MessageBoxDefaultButton defaultButton)
        {
            _icon=icon;_singleOk=buttons==MessageBoxButtons.OK;_canCancel=Array.IndexOf(Results(buttons),DialogResult.Cancel)>=0;Text=string.IsNullOrWhiteSpace(caption)?"ZenQ Office":caption;
            FormBorderStyle=FormBorderStyle.FixedDialog;StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;MaximizeBox=MinimizeBox=false;
            BackColor=Color.White;Font=_messageFont=new Font("Segoe UI",10f);AutoScaleMode=AutoScaleMode.Dpi;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            var results=Results(buttons);bool hasCancel=Array.IndexOf(results,DialogResult.Cancel)>=0;
            ControlBox=hasCancel||buttons==MessageBoxButtons.OK;
            int bodyWidth=420;var measured=TextRenderer.MeasureText(text??"",Font,new Size(bodyWidth,1200),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl);
            int bodyHeight=Math.Clamp(measured.Height+18,56,Math.Max(120,Math.Min(460,Screen.FromPoint(Cursor.Position).WorkingArea.Height-200)));
            ClientSize=new Size(520,bodyHeight+100);
            var message=new TextBox{Text=text??"",Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=FluentUi.Ink,Font=Font,ScrollBars=measured.Height>bodyHeight?ScrollBars.Vertical:ScrollBars.None,TabStop=false,Location=new Point(icon==MessageBoxIcon.None?24:72,24),Size=new Size(icon==MessageBoxIcon.None?472:424,bodyHeight)};
            Controls.Add(message);
            Actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=64,Padding=new Padding(16,12,16,10),FlowDirection=FlowDirection.RightToLeft,WrapContents=false,BackColor=Color.FromArgb(247,249,252)};
            Controls.Add(Actions);
            int selected=Math.Clamp((int)defaultButton/256,0,results.Length-1);
            for(int i=results.Length-1;i>=0;i--)
            {
                DialogResult result=results[i];bool primary=i==selected;
                var button=new FluentDialogButton{Text=Label(result),DialogResult=result,Size=new Size(104,34),Margin=new Padding(6,0,0,0),Primary=primary,TabIndex=i};
                Actions.Controls.Add(button);
                if(primary)AcceptButton=button;
                if(result==DialogResult.Cancel)CancelButton=button;
                if(buttons==MessageBoxButtons.OK){AcceptButton=button;CancelButton=button;}
            }
            Shown+=(s,e)=>{FluentUi.RoundWindow(this);(AcceptButton as Control)?.Focus();};
        }
        private static string Label(DialogResult result)=>result switch
        {
            DialogResult.Yes=>"Có",DialogResult.No=>"Không",DialogResult.Cancel=>"Hủy",DialogResult.Retry or DialogResult.TryAgain=>"Thử lại",
            DialogResult.Abort=>"Hủy thao tác",DialogResult.Ignore=>"Bỏ qua",DialogResult.Continue=>"Tiếp tục",_=>"OK"
        };
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if(e.CloseReason==CloseReason.UserClosing)
            {
                if(_singleOk&&(DialogResult==DialogResult.None||DialogResult==DialogResult.Cancel))DialogResult=DialogResult.OK;
                else if(!_canCancel&&(DialogResult==DialogResult.None||DialogResult==DialogResult.Cancel)){e.Cancel=true;DialogResult=DialogResult.None;}
            }
            base.OnFormClosing(e);
        }
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            if(keyData==Keys.Escape&&CancelButton==null)return true;
            var key=keyData&Keys.KeyCode;var modifiers=keyData&Keys.Modifiers;
            if(modifiers==Keys.None||modifiers==Keys.Alt)
            {
                DialogResult target=key switch {Keys.Y=>DialogResult.Yes,Keys.N=>DialogResult.No,Keys.A=>DialogResult.Abort,Keys.R=>DialogResult.Retry,Keys.I=>DialogResult.Ignore,_=>DialogResult.None};
                foreach(Control control in Actions.Controls)
                    if(control is Button button&&target!=DialogResult.None&&button.DialogResult==target){button.PerformClick();return true;}
            }
            return base.ProcessCmdKey(ref msg,keyData);
        }
        protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)_messageFont?.Dispose();}
        protected override void OnPaint(PaintEventArgs e)
        {
            using var g=new D2DGraphics(e.Graphics,Width,Height);g.Clear(Color.White);
            if(_icon==MessageBoxIcon.None)return;
            Color color=_icon==MessageBoxIcon.Error?Color.FromArgb(199,61,65):_icon==MessageBoxIcon.Warning?Color.FromArgb(183,119,22):Color.FromArgb(39,114,188);
            using var fill=new SolidBrush(Color.FromArgb(18,color));g.FillEllipse(fill,22,24,32,32);
            char glyph=_icon==MessageBoxIcon.Error?'\uEA39':_icon==MessageBoxIcon.Warning?'\uE7BA':_icon==MessageBoxIcon.Question?'\uE897':'\uE946';
            FluentIconRenderer.DrawGlyph(g,glyph,new RectangleF(28,30,20,20),color,16);
        }
    }
    internal sealed class FluentDialogButton : Button
    {
        internal bool Primary;
        private bool _hover;
        internal FluentDialogButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);_hover=true;Invalidate();}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);_hover=false;Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            using var g=new D2DGraphics(e.Graphics,Width,Height);g.Clear(Parent?.BackColor??Color.White);
            using var fill=new SolidBrush(Primary?(_hover?Color.FromArgb(27,94,167):Color.FromArgb(36,111,191)):_hover?FluentUi.Hover:Color.White);
            var rect=new RectangleF(.5f,.5f,Width-1,Height-1);g.FillRoundedRectangle(fill,rect,6);
            using var border=new Pen(Primary?Color.FromArgb(36,111,191):FluentUi.Border);g.DrawRoundedRectangle(border,rect,6);
            FluentUi.Text(g,Text,Font,Primary?Color.White:FluentUi.Ink,ClientRectangle,true);
            if(Focused){using var focus=new Pen(Primary?Color.White:Color.FromArgb(36,111,191)){DashStyle=System.Drawing.Drawing2D.DashStyle.Dot};g.DrawRoundedRectangle(focus,new RectangleF(4,4,Width-8,Height-8),3);}
        }
    }
}
