using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Cửa sổ toàn màn hình cho màn hình phụ (Khán giả / Máy chiếu) trong chế độ 2 màn hình (Dual Screen)
    /// </summary>
    public class SlideShowAudienceForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private SlidesView _slidesView;
        private AppForm _appForm;
        private Timer _syncTimer;

        public SlideShowAudienceForm(SlidesView slidesView, AppForm appForm, Screen targetScreen)
        {
            _slidesView = slidesView;
            _appForm = appForm;

            Text = ZenQOffice.Core.AppL10n.T("ZenQ Slide Show - Màn hình Khán giả", "ZenQ Slide Show - Audience View");
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = targetScreen.Bounds;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            try
            {
                SetWindowPos(Handle, (IntPtr)(-1) /* HWND_TOPMOST */, targetScreen.Bounds.X, targetScreen.Bounds.Y, targetScreen.Bounds.Width, targetScreen.Bounds.Height, 0x0040 | 0x0020);
            }
            catch { }

            // Timer kiểm tra trạng thái kết thúc trình chiếu (khung hình 60 FPS được điều phối đồng bộ trực tiếp từ AppForm._animTimer)
            _syncTimer = new Timer();
            _syncTimer.Interval = 250;
            _syncTimer.Tick += (s, e) =>
            {
                if (_slidesView == null || !_slidesView.IsPresenting)
                {
                    Close();
                }
            };
            _syncTimer.Start();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084 /* WM_NCHITTEST */)
            {
                m.Result = (IntPtr)1; // HTCLIENT
                return;
            }
            if (m.Msg == 0x0112 /* WM_SYSCOMMAND */)
            {
                int sc = (int)(m.WParam.ToInt64() & 0xFFF0);
                if (sc == 0xF010 /* SC_MOVE */ || sc == 0xF000 /* SC_SIZE */ || sc == 0xF030 /* SC_MAXIMIZE */ || sc == 0xF120 /* SC_RESTORE */)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;
            if (keyCode == Keys.Escape || keyCode == Keys.Left || keyCode == Keys.Right ||
                keyCode == Keys.Up || keyCode == Keys.Down || keyCode == Keys.PageUp ||
                keyCode == Keys.PageDown || keyCode == Keys.Space || keyCode == Keys.Back ||
                keyCode == Keys.Return || keyCode == Keys.Home || keyCode == Keys.End ||
                keyCode == Keys.B || keyCode == Keys.W || keyCode == Keys.L || keyCode == Keys.P || keyCode == Keys.E)
            {
                if (_appForm != null)
                {
                    _appForm.HandleSlideShowKeyDown(keyCode);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_slidesView == null) return;
            using (D2DGraphics d2d = new D2DGraphics(e.Graphics, ClientSize.Width, ClientSize.Height))
            {
                _slidesView.DrawFullscreenPresenter(d2d, ClientSize.Width, ClientSize.Height, false);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Opaque Direct2D rendering
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_slidesView == null || _appForm == null) return;

            if(_slidesView.IsSlideSorterModalOpen) { _slidesView.HandleMouseDown(e.Location,ClientSize.Width,ClientSize.Height,e.Button,this); Invalidate(); return; }
            if (e.Button == MouseButtons.Left && _slidesView.TryToggleHoveredGif(e.Location, ClientSize.Width, ClientSize.Height, true)) { Invalidate(); return; }
            if (e.Button == MouseButtons.Left)
            {
                _slidesView.AdvancePresenterStep();
            }
            else if (e.Button == MouseButtons.Right)
            {
                _slidesView.PreviousPresenterStep();
            }
            Invalidate();
            Update();
            if (_appForm != null)
            {
                _appForm.Invalidate();
                _appForm.Update();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_slidesView==null) return;
            if(!_slidesView.ScrollPresentationGrid(e.Delta,ClientSize.Width,ClientSize.Height))
            { if(e.Delta<0) _slidesView.AdvancePresenterStep(); else if(e.Delta>0) _slidesView.PreviousPresenterStep(); }
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _slidesView?.HandleMouseUp(e.Location,ClientSize.Width,ClientSize.Height);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _slidesView?.ClearGifHover();
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if(_slidesView?.IsSlideSorterModalOpen==true) _slidesView.HandleMouseMove(e.Location,ClientSize.Width,ClientSize.Height);
            else _slidesView?.UpdateGifHover(e.Location, ClientSize.Width, ClientSize.Height, true);
            Invalidate();
            if (_slidesView != null && _slidesView.LaserPointerMode)
            {
                _slidesView.CurrentLaserPoint = e.Location;
                Invalidate();
                if (_appForm != null) _appForm.Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_appForm != null)
            {
                _appForm.HandleSlideShowKeyDown(e.KeyCode);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_syncTimer != null)
            {
                _syncTimer.Stop();
                _syncTimer.Dispose();
                _syncTimer = null;
            }
            base.OnFormClosed(e);
        }
    }
}
