using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    /// <summary>
    /// Cửa sổ toàn màn hình cho màn hình phụ (Khán giả / Máy chiếu) trong chế độ 2 màn hình (Dual Screen)
    /// </summary>
    public class SlideShowAudienceForm : Form
    {
        private SlidesView _slidesView;
        private AppForm _appForm;
        private Timer _syncTimer;

        public SlideShowAudienceForm(SlidesView slidesView, AppForm appForm, Screen targetScreen)
        {
            _slidesView = slidesView;
            _appForm = appForm;

            Text = "GenOffice Slide Show - Màn hình Khán giả";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = targetScreen.Bounds;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            // Timer đồng bộ khung hình 60 FPS
            _syncTimer = new Timer();
            _syncTimer.Interval = 16;
            _syncTimer.Tick += (s, e) =>
            {
                if (_slidesView != null && _slidesView.IsPresenting)
                {
                    Invalidate();
                }
                else
                {
                    Close();
                }
            };
            _syncTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (_slidesView != null)
            {
                _slidesView.DrawFullscreenPresenter(g, ClientSize.Width, ClientSize.Height, false);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_slidesView == null || _appForm == null) return;

            if (e.Button == MouseButtons.Left)
            {
                _slidesView.AdvancePresenterStep();
            }
            else if (e.Button == MouseButtons.Right)
            {
                _slidesView.PreviousPresenterStep();
            }
            Invalidate();
            _appForm.Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
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
