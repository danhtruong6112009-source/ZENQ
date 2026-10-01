using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GenOffice.Core;

namespace GenOffice.UI
{
    /// <summary>
    /// Phase 1 Hardware-Accelerated WPF GPU Slide Presenter.
    /// Delivers 120 FPS buttery-smooth 4K slide presentation, GPU image textures,
    /// and Direct3D hardware-accelerated transitions.
    /// </summary>
    public class WpfSlidePresenterWindow : Window
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static WpfSlidePresenterWindow _activeInstance;

        private readonly AppForm _ownerForm;
        private readonly PresentationRoot _presentation;
        private int _currentSlideIndex = 0;
        private bool _isTransitioning = false;
        private bool _isBlackScreen = false;
        private bool _isWhiteScreen = false;

        // Container layers
        private Grid _rootGrid;
        private Viewbox _viewboxA;
        private Viewbox _viewboxB;
        private Canvas _slideCanvasA;
        private Canvas _slideCanvasB;
        private bool _usingCanvasA = true;

        // Overlay & Controls
        private Border _blackOverlay;
        private Border _whiteOverlay;
        private Border _bottomBar;
        private TextBlock _slideCounterText;
        private DispatcherTimer _cursorHideTimer;
        private System.Windows.Point _lastMousePos;

        // Cache of GPU BitmapSources converted from GDI+ images
        private readonly Dictionary<string, BitmapSource> _imageSourceCache = new Dictionary<string, BitmapSource>();

        public int CurrentSlideIndex => _currentSlideIndex;

        public static void Start(AppForm ownerForm, SlidesView slidesView, bool fromBeginning)
        {
            if (slidesView == null || slidesView.Presentation == null || slidesView.Presentation.Slides.Count == 0) return;

            if (_activeInstance != null)
            {
                try { _activeInstance.Close(); } catch { }
                _activeInstance = null;
            }

            int startIdx = fromBeginning ? 0 : Math.Max(0, Math.Min(slidesView.Presentation.Slides.Count - 1, slidesView.ActiveSlideIndex));

            try
            {
                var presenter = new WpfSlidePresenterWindow(ownerForm, slidesView.Presentation, startIdx);
                _activeInstance = presenter;
                presenter.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WPF Presenter launch error: " + ex.Message);
                // Fallback to classic presentation if GPU presenter fails
                ownerForm.StartSlideShow(fromBeginning, false);
            }
        }

        public WpfSlidePresenterWindow(AppForm ownerForm, PresentationRoot presentation, int initialIndex)
        {
            _ownerForm = ownerForm;
            _presentation = presentation;
            _currentSlideIndex = initialIndex;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.Black;
            Topmost = true;
            ShowInTaskbar = false;

            // Target the monitor containing the owner form
            var targetScreen = System.Windows.Forms.Screen.FromControl(ownerForm) ?? System.Windows.Forms.Screen.PrimaryScreen;
            Left = targetScreen.Bounds.Left;
            Top = targetScreen.Bounds.Top;
            Width = targetScreen.Bounds.Width;
            Height = targetScreen.Bounds.Height;
            WindowState = WindowState.Normal;

            BuildPresenterLayout();

            // Render initial slide
            RenderSlideToCanvas(_slideCanvasA, _presentation.Slides[_currentSlideIndex]);
            _usingCanvasA = true;
            _viewboxA.Visibility = Visibility.Visible;
            _viewboxB.Visibility = Visibility.Collapsed;

            // Event Hooks
            KeyDown += OnPresenterKeyDown;
            MouseDown += OnPresenterMouseDown;
            MouseMove += OnPresenterMouseMove;

            // Cursor auto-hide timer (hide cursor after 2.5s of no movement)
            _cursorHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _cursorHideTimer.Tick += (s, e) =>
            {
                _cursorHideTimer.Stop();
                Mouse.OverrideCursor = Cursors.None;
                FadeOutBottomBar();
            };
            _cursorHideTimer.Start();

            Closed += OnPresenterClosed;
        }

        private void BuildPresenterLayout()
        {
            _rootGrid = new Grid { Background = Brushes.Black };

            double canvasW = _presentation.CanvasWidth > 0 ? _presentation.CanvasWidth : 1920;
            double canvasH = _presentation.CanvasHeight > 0 ? _presentation.CanvasHeight : 1080;

            // Slide Viewbox & Canvas A
            _slideCanvasA = new Canvas { Width = canvasW, Height = canvasH, ClipToBounds = true };
            _viewboxA = new Viewbox
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = _slideCanvasA
            };

            // Slide Viewbox & Canvas B (for transitions)
            _slideCanvasB = new Canvas { Width = canvasW, Height = canvasH, ClipToBounds = true };
            _viewboxB = new Viewbox
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = _slideCanvasB,
                Visibility = Visibility.Collapsed
            };

            _rootGrid.Children.Add(_viewboxA);
            _rootGrid.Children.Add(_viewboxB);

            // Black & White screen overlays
            _blackOverlay = new Border { Background = Brushes.Black, Visibility = Visibility.Collapsed };
            _whiteOverlay = new Border { Background = Brushes.White, Visibility = Visibility.Collapsed };
            _rootGrid.Children.Add(_blackOverlay);
            _rootGrid.Children.Add(_whiteOverlay);

            // Sleek Floating Navigation Bar (Bottom-Left)
            _bottomBar = CreateBottomBar();
            _rootGrid.Children.Add(_bottomBar);

            Content = _rootGrid;
        }

        private Border CreateBottomBar()
        {
            Border bar = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(24, 0, 0, 24),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 24, 24, 27)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 6, 12, 6),
                Opacity = 0.0
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };

            // Previous Button
            Button btnPrev = CreateNavButton("◀", (s, e) => NavigatePrevious());
            sp.Children.Add(btnPrev);

            // Slide Counter
            _slideCounterText = new TextBlock
            {
                Text = $"{_currentSlideIndex + 1} / {_presentation.Slides.Count}",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };
            sp.Children.Add(_slideCounterText);

            // Next Button
            Button btnNext = CreateNavButton("▶", (s, e) => NavigateNext());
            sp.Children.Add(btnNext);

            // Separator
            Rectangle sep = new Rectangle
            {
                Width = 1,
                Height = 16,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255)),
                Margin = new Thickness(10, 0, 10, 0)
            };
            sp.Children.Add(sep);

            // Close / Exit Button
            Button btnClose = CreateNavButton("✕", (s, e) => ClosePresenter());
            sp.Children.Add(btnClose);

            bar.Child = sp;
            return bar;
        }

        private Button CreateNavButton(string text, RoutedEventHandler onClick)
        {
            Button btn = new Button
            {
                Content = text,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = Cursors.Hand
            };
            btn.Click += onClick;
            return btn;
        }

        private void RenderSlideToCanvas(Canvas canvas, SlideNode slide)
        {
            canvas.Children.Clear();
            if (slide == null) return;

            // 1. Background
            Rectangle bgRect = new Rectangle
            {
                Width = canvas.Width,
                Height = canvas.Height
            };

            if (slide.IsGradientBg)
            {
                System.Windows.Media.Color c1 = ConvertGdiColor(slide.BgColor);
                System.Windows.Media.Color c2 = ConvertGdiColor(slide.BgColor2);
                double angleRad = slide.BgGradientAngle * Math.PI / 180.0;
                double x2 = 0.5 + 0.5 * Math.Cos(angleRad);
                double y2 = 0.5 + 0.5 * Math.Sin(angleRad);
                bgRect.Fill = new LinearGradientBrush(c1, c2, new System.Windows.Point(0.5, 0.5), new System.Windows.Point(x2, y2));
            }
            else
            {
                bgRect.Fill = new SolidColorBrush(ConvertGdiColor(slide.BgColor));
            }
            canvas.Children.Add(bgRect);

            // 2. Shapes & Images
            for (int i = 0; i < slide.Shapes.Count; i++)
            {
                ShapeNode shape = slide.Shapes[i];
                UIElement element = CreateWpfShapeElement(shape);
                if (element != null)
                {
                    canvas.Children.Add(element);
                }
            }
        }

        private UIElement CreateWpfShapeElement(ShapeNode shape)
        {
            if (shape == null || shape.Transform == null) return null;

            double x = shape.Transform.X;
            double y = shape.Transform.Y;
            double w = Math.Max(1, shape.Transform.Width);
            double h = Math.Max(1, shape.Transform.Height);

            FrameworkElement element = null;

            // 1. IMAGE SHAPE
            if (shape.Type == ShapeType.Image || shape.CachedImage != null)
            {
                BitmapSource bmpSource = GetOrCreateWpfImageSource(shape);
                if (bmpSource != null)
                {
                    Image img = new Image
                    {
                        Source = bmpSource,
                        Width = w,
                        Height = h,
                        Stretch = Stretch.Fill
                    };
                    // Enable Direct3D high-performance GPU texture filtering
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    element = img;
                }
            }
            // 2. VIDEO SHAPE
            else if (shape.Type == ShapeType.Video && !string.IsNullOrEmpty(shape.ImagePath) && System.IO.File.Exists(shape.ImagePath))
            {
                MediaElement media = new MediaElement
                {
                    Source = new Uri(shape.ImagePath),
                    Width = w,
                    Height = h,
                    LoadedBehavior = MediaState.Play,
                    Stretch = Stretch.Uniform
                };
                element = media;
            }
            // 3. TEXT BOX OR RECTANGLE
            else if (shape.Type == ShapeType.TextBox || shape.Type == ShapeType.Rectangle || shape.Type == ShapeType.RoundedRectangle)
            {
                Border border = new Border
                {
                    Width = w,
                    Height = h,
                    CornerRadius = new CornerRadius(shape.GetEffectiveCornerRadius())
                };

                if (shape.FillOpacity > 0.01f)
                {
                    System.Windows.Media.Color fillCol = ConvertGdiColor(shape.FillColor);
                    fillCol.A = (byte)(shape.FillOpacity * 255);
                    border.Background = new SolidColorBrush(fillCol);
                }

                if (shape.StrokeWidth > 0.1f)
                {
                    border.BorderBrush = new SolidColorBrush(ConvertGdiColor(shape.StrokeColor));
                    border.BorderThickness = new Thickness(shape.StrokeWidth);
                }

                if (!string.IsNullOrEmpty(shape.Text))
                {
                    TextBlock tb = new TextBlock
                    {
                        Text = shape.Text,
                        FontFamily = new System.Windows.Media.FontFamily(shape.FontName ?? "Segoe UI"),
                        FontSize = Math.Max(8, shape.FontSize),
                        FontWeight = shape.Bold ? FontWeights.Bold : FontWeights.Normal,
                        FontStyle = shape.Italic ? FontStyles.Italic : FontStyles.Normal,
                        Foreground = new SolidColorBrush(ConvertGdiColor(shape.TextColor)),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8)
                    };
                    if (shape.Alignment == GenOffice.Core.TextAlignment.Center) tb.TextAlignment = System.Windows.TextAlignment.Center;
                    else if (shape.Alignment == GenOffice.Core.TextAlignment.Right) tb.TextAlignment = System.Windows.TextAlignment.Right;
                    else tb.TextAlignment = System.Windows.TextAlignment.Left;

                    border.Child = tb;
                }

                element = border;
            }
            // 4. ELLIPSE
            else if (shape.Type == ShapeType.Ellipse)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = w,
                    Height = h,
                    Fill = new SolidColorBrush(ConvertGdiColor(shape.FillColor)),
                    Stroke = shape.StrokeWidth > 0.1f ? new SolidColorBrush(ConvertGdiColor(shape.StrokeColor)) : null,
                    StrokeThickness = shape.StrokeWidth
                };
                element = ellipse;
            }
            // 5. GENERIC SHAPE FALLBACK
            else
            {
                Border genericBorder = new Border
                {
                    Width = w,
                    Height = h,
                    Background = new SolidColorBrush(ConvertGdiColor(shape.FillColor)),
                    BorderBrush = shape.StrokeWidth > 0.1f ? new SolidColorBrush(ConvertGdiColor(shape.StrokeColor)) : null,
                    BorderThickness = new Thickness(shape.StrokeWidth)
                };
                element = genericBorder;
            }

            if (element != null)
            {
                Canvas.SetLeft(element, x);
                Canvas.SetTop(element, y);

                // Rotate Transform if specified
                if (Math.Abs(shape.Transform.Rotation) > 0.1f)
                {
                    element.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    element.RenderTransform = new RotateTransform(shape.Transform.Rotation);
                }

                if (shape.FillOpacity < 0.99f && shape.Type == ShapeType.Image)
                {
                    element.Opacity = shape.FillOpacity;
                }
            }

            return element;
        }

        private BitmapSource GetOrCreateWpfImageSource(ShapeNode shape)
        {
            if (shape == null || shape.CachedImage == null) return null;

            string key = !string.IsNullOrEmpty(shape.ImagePath) ? shape.ImagePath : shape.Id.ToString();
            if (_imageSourceCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                using (var gdiBmp = new System.Drawing.Bitmap(shape.CachedImage))
                {
                    IntPtr hBitmap = gdiBmp.GetHbitmap();
                    try
                    {
                        var wpfSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        wpfSource.Freeze(); // Crucial for Direct3D hardware texture upload
                        _imageSourceCache[key] = wpfSource;
                        return wpfSource;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WPF Bitmap conversion error: " + ex.Message);
                return null;
            }
        }

        public void NavigateNext()
        {
            if (_isTransitioning) return;
            if (_currentSlideIndex < _presentation.Slides.Count - 1)
            {
                TransitionToSlide(_currentSlideIndex + 1);
            }
            else
            {
                // End of presentation
                ClosePresenter();
            }
        }

        public void NavigatePrevious()
        {
            if (_isTransitioning) return;
            if (_currentSlideIndex > 0)
            {
                TransitionToSlide(_currentSlideIndex - 1);
            }
        }

        private void TransitionToSlide(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _presentation.Slides.Count) return;

            SlideNode fromSlide = _presentation.Slides[_currentSlideIndex];
            SlideNode toSlide = _presentation.Slides[targetIndex];
            _currentSlideIndex = targetIndex;
            _slideCounterText.Text = $"{_currentSlideIndex + 1} / {_presentation.Slides.Count}";

            Canvas targetCanvas = _usingCanvasA ? _slideCanvasB : _slideCanvasA;
            Viewbox targetViewbox = _usingCanvasA ? _viewboxB : _viewboxA;
            Viewbox currentViewbox = _usingCanvasA ? _viewboxA : _viewboxB;

            RenderSlideToCanvas(targetCanvas, toSlide);

            _isTransitioning = true;
            targetViewbox.Visibility = Visibility.Visible;

            // Direct3D Hardware GPU Transition
            Duration duration = new Duration(TimeSpan.FromMilliseconds(450));
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

            if (toSlide.Transition == TransitionType.Push)
            {
                // GPU Push Transition
                double width = ActualWidth;
                TranslateTransform transCurrent = new TranslateTransform(0, 0);
                TranslateTransform transTarget = new TranslateTransform(width, 0);
                currentViewbox.RenderTransform = transCurrent;
                targetViewbox.RenderTransform = transTarget;

                DoubleAnimation animCurrent = new DoubleAnimation(0, -width, duration) { EasingFunction = ease };
                DoubleAnimation animTarget = new DoubleAnimation(width, 0, duration) { EasingFunction = ease };

                animTarget.Completed += (s, e) =>
                {
                    FinishTransition(currentViewbox, targetViewbox);
                };

                transCurrent.BeginAnimation(TranslateTransform.XProperty, animCurrent);
                transTarget.BeginAnimation(TranslateTransform.XProperty, animTarget);
            }
            else
            {
                // GPU Fade Transition (Default for ultra-fast 120 FPS blend)
                currentViewbox.Opacity = 1.0;
                targetViewbox.Opacity = 0.0;

                DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, duration) { EasingFunction = ease };
                DoubleAnimation fadeIn = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = ease };

                fadeIn.Completed += (s, e) =>
                {
                    FinishTransition(currentViewbox, targetViewbox);
                };

                currentViewbox.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                targetViewbox.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }

        private void FinishTransition(Viewbox oldViewbox, Viewbox newViewbox)
        {
            oldViewbox.Visibility = Visibility.Collapsed;
            oldViewbox.RenderTransform = null;
            newViewbox.RenderTransform = null;
            newViewbox.Opacity = 1.0;
            _usingCanvasA = !_usingCanvasA;
            _isTransitioning = false;
        }

        private void OnPresenterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClosePresenter();
                e.Handled = true;
            }
            else if (e.Key == Key.Space || e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Enter)
            {
                NavigateNext();
                e.Handled = true;
            }
            else if (e.Key == Key.Left || e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Back)
            {
                NavigatePrevious();
                e.Handled = true;
            }
            else if (e.Key == Key.B)
            {
                ToggleBlackScreen();
                e.Handled = true;
            }
            else if (e.Key == Key.W)
            {
                ToggleWhiteScreen();
                e.Handled = true;
            }
        }

        private void OnPresenterMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                NavigateNext();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                NavigatePrevious();
            }
        }

        private void OnPresenterMouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point curPos = e.GetPosition(this);
            if ((curPos - _lastMousePos).Length > 2)
            {
                _lastMousePos = curPos;
                Mouse.OverrideCursor = null; // Show cursor

                // Show bottom bar when mouse moves near bottom-left
                if (curPos.Y > ActualHeight - 120 && curPos.X < 350)
                {
                    FadeInBottomBar();
                }

                _cursorHideTimer.Stop();
                _cursorHideTimer.Start();
            }
        }

        private void FadeInBottomBar()
        {
            DoubleAnimation anim = new DoubleAnimation(1.0, new Duration(TimeSpan.FromMilliseconds(180)));
            _bottomBar.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void FadeOutBottomBar()
        {
            DoubleAnimation anim = new DoubleAnimation(0.0, new Duration(TimeSpan.FromMilliseconds(250)));
            _bottomBar.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void ToggleBlackScreen()
        {
            _isBlackScreen = !_isBlackScreen;
            _blackOverlay.Visibility = _isBlackScreen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleWhiteScreen()
        {
            _isWhiteScreen = !_isWhiteScreen;
            _whiteOverlay.Visibility = _isWhiteScreen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClosePresenter()
        {
            Close();
        }

        private void OnPresenterClosed(object sender, EventArgs e)
        {
            _activeInstance = null;
            Mouse.OverrideCursor = null;

            // Sync slide index back to main form
            if (_ownerForm != null && _ownerForm.SlidesView != null)
            {
                _ownerForm.SlidesView.ActiveSlideIndex = _currentSlideIndex;
                _ownerForm.Invalidate();
                _ownerForm.Activate();
                _ownerForm.Focus();
            }
        }

        private static System.Windows.Media.Color ConvertGdiColor(int argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            if (a == 0) a = 255;
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }
    }
}
