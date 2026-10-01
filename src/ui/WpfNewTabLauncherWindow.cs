using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Interop;
using GenOffice.UI;

namespace GenOffice.UI
{
    /// <summary>
    /// Modern, hardware-accelerated WPF Popover Launcher for creating new documents (+).
    /// Features Apple-inspired fluid motion, 120fps GPU animations, and Fluent Design materials.
    /// </summary>
    public class WpfNewTabLauncherWindow : Window
    {
        private static WpfNewTabLauncherWindow _currentInstance;
        private Action<OfficeAppMode> _onSelect;
        private bool _isClosing = false;
        private DateTime _openedTime;
        private Border _cardContainer;
        private TranslateTransform _translateTransform;
        private ScaleTransform _scaleTransform;
        private int _focusedIndex = -1;
        private Border[] _itemBorders;

        public static bool IsCurrentlyOpen => _currentInstance != null && !_currentInstance._isClosing;

        public static void ShowLauncher(System.Drawing.Point clientPlusCenter, AppForm ownerForm, Action<OfficeAppMode> onSelect)
        {
            if (_currentInstance != null)
            {
                _currentInstance.CloseSmoothly();
                return;
            }

            try
            {
                var win = new WpfNewTabLauncherWindow(clientPlusCenter, ownerForm, onSelect);
                _currentInstance = win;
                win.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WPF Launcher Error: " + ex.Message);
            }
        }

        public static void CloseActive()
        {
            if (_currentInstance != null)
            {
                _currentInstance.CloseSmoothly();
            }
        }

        public WpfNewTabLauncherWindow(System.Drawing.Point clientPlusCenter, AppForm ownerForm, Action<OfficeAppMode> onSelect)
        {
            _onSelect = onSelect;
            _openedTime = DateTime.UtcNow;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (ownerForm != null && ownerForm.IsHandleCreated)
            {
                new WindowInteropHelper(this).Owner = ownerForm.Handle;
            }

            PositionWindow(clientPlusCenter, ownerForm);
            BuildUI(clientPlusCenter, ownerForm);

            Deactivated += (s, e) =>
            {
                if ((DateTime.UtcNow - _openedTime).TotalMilliseconds < 450) return;
                CloseSmoothly();
            };
            KeyDown += OnWindowKeyDown;
        }

        private void BuildUI(System.Drawing.Point clientPlusCenter, AppForm ownerForm)
        {
            // Root Grid with generous padding to prevent DropShadow clipping
            Grid rootGrid = new Grid
            {
                Margin = new Thickness(20)
            };

            // Main popover card container
            _cardContainer = new Border
            {
                Width = 270,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                SnapsToDevicePixels = true,
                RenderTransformOrigin = new System.Windows.Point(0.18, 0),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 26,
                    ShadowDepth = 6,
                    Direction = 270,
                    Color = Color.FromRgb(15, 23, 42),
                    Opacity = 0.16
                }
            };

            // Transforms for Apple-style fluid entrance
            TransformGroup transformGroup = new TransformGroup();
            _scaleTransform = new ScaleTransform(0.92, 0.92);
            _translateTransform = new TranslateTransform(0, -12);
            transformGroup.Children.Add(_scaleTransform);
            transformGroup.Children.Add(_translateTransform);
            _cardContainer.RenderTransform = transformGroup;
            _cardContainer.Opacity = 0;

            StackPanel mainStack = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 10)
            };

            // Header: "TẠO TÀI LIỆU MỚI"
            TextBlock header = new TextBlock
            {
                Text = "TẠO TÀI LIỆU MỚI",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(16, 6, 16, 8)
            };
            mainStack.Children.Add(header);

            // 4 App Options
            _itemBorders = new Border[4];
            _itemBorders[0] = CreateItemRow(OfficeAppMode.Writer, "Writer", "Văn bản Word", "DOCX",
                Color.FromRgb(37, 99, 235), Color.FromRgb(29, 78, 216),
                Color.FromRgb(239, 246, 255), Color.FromRgb(191, 219, 254), 0);

            _itemBorders[1] = CreateItemRow(OfficeAppMode.Calc, "Calc", "Bảng tính Excel", "XLSX",
                Color.FromRgb(16, 185, 129), Color.FromRgb(5, 150, 105),
                Color.FromRgb(236, 253, 245), Color.FromRgb(167, 243, 208), 1);

            _itemBorders[2] = CreateItemRow(OfficeAppMode.Slides, "Slides", "Trình chiếu PowerPoint", "PPTX",
                Color.FromRgb(249, 115, 22), Color.FromRgb(217, 70, 239),
                Color.FromRgb(255, 247, 237), Color.FromRgb(254, 215, 170), 2);

            _itemBorders[3] = CreateItemRow(OfficeAppMode.Pdf, "PDF", "Tài liệu PDF", "PDF",
                Color.FromRgb(220, 38, 38), Color.FromRgb(185, 28, 28),
                Color.FromRgb(254, 242, 242), Color.FromRgb(254, 202, 202), 3);

            for (int i = 0; i < 4; i++)
            {
                mainStack.Children.Add(_itemBorders[i]);
            }

            _cardContainer.Child = mainStack;
            rootGrid.Children.Add(_cardContainer);
            Content = rootGrid;

            // Compute exact positioning in DIPs anchored below '+' button
            Loaded += (s, e) =>
            {
                PositionWindow(clientPlusCenter, ownerForm);
                PlayEntranceAnimation();
            };
        }

        private Border CreateItemRow(OfficeAppMode mode, string title, string subtitle, string badgeText,
            Color iconStart, Color iconEnd, Color badgeBg, Color badgeBorder, int index)
        {
            Border rowBorder = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(9),
                Margin = new Thickness(8, 2, 8, 2),
                Padding = new Thickness(8, 7, 8, 7),
                Cursor = Cursors.Hand,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };

            ScaleTransform pressScale = new ScaleTransform(1.0, 1.0);
            rowBorder.RenderTransform = pressScale;

            Grid rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 1. Icon Tile (40x40 rounded rect with rich gradient & emblem)
            Border iconBorder = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(9),
                Margin = new Thickness(0, 0, 12, 0),
                Background = new LinearGradientBrush(iconStart, iconEnd, new System.Windows.Point(0, 0), new System.Windows.Point(1, 1))
            };

            UIElement emblem = CreateEmblemElement(mode);
            iconBorder.Child = emblem;
            Grid.SetColumn(iconBorder, 0);
            rowGrid.Children.Add(iconBorder);

            // 2. Titles Stack
            StackPanel textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };
            TextBlock subBlock = new TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 1, 0, 0)
            };
            textStack.Children.Add(titleBlock);
            textStack.Children.Add(subBlock);
            Grid.SetColumn(textStack, 1);
            rowGrid.Children.Add(textStack);

            // 3. Badge on right ([DOCX], [XLSX], [PPTX], [PDF])
            Border badge = new Border
            {
                Background = new SolidColorBrush(badgeBg),
                BorderBrush = new SolidColorBrush(badgeBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 3, 7, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock badgeTextBlock = new TextBlock
            {
                Text = badgeText,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(iconStart)
            };
            badge.Child = badgeTextBlock;
            Grid.SetColumn(badge, 2);
            rowGrid.Children.Add(badge);

            rowBorder.Child = rowGrid;

            // Hover and Press micro-interactions (Apple fluid touch response)
            SolidColorBrush hoverBrush = new SolidColorBrush(Color.FromArgb(160, 241, 245, 249));
            rowBorder.MouseEnter += (s, e) =>
            {
                _focusedIndex = index;
                rowBorder.Background = hoverBrush;
            };
            rowBorder.MouseLeave += (s, e) =>
            {
                rowBorder.Background = Brushes.Transparent;
                pressScale.ScaleX = 1.0;
                pressScale.ScaleY = 1.0;
            };
            rowBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                // Instant physical response: scale 0.97
                pressScale.ScaleX = 0.97;
                pressScale.ScaleY = 0.97;
            };
            rowBorder.PreviewMouseLeftButtonUp += (s, e) =>
            {
                pressScale.ScaleX = 1.0;
                pressScale.ScaleY = 1.0;
                ExecuteSelection(mode);
                e.Handled = true;
            };

            return rowBorder;
        }

        private UIElement CreateEmblemElement(OfficeAppMode mode)
        {
            Canvas canvas = new Canvas
            {
                Width = 22,
                Height = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (mode == OfficeAppMode.Writer)
            {
                // Document shape + lines
                Path page = new Path
                {
                    Data = Geometry.Parse("M3,2 L14,2 L19,7 L19,20 L3,20 Z"),
                    Fill = Brushes.White,
                    Opacity = 0.95
                };
                Path fold = new Path
                {
                    Data = Geometry.Parse("M14,2 L14,7 L19,7 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(200, 220, 255)),
                    Opacity = 0.95
                };
                Path lines = new Path
                {
                    Data = Geometry.Parse("M6,9 L12,9 M6,12 L16,12 M6,15 L14,15 M6,18 L11,18"),
                    Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    StrokeThickness = 1.6,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                canvas.Children.Add(page);
                canvas.Children.Add(fold);
                canvas.Children.Add(lines);
            }
            else if (mode == OfficeAppMode.Calc)
            {
                // Document shape + bar chart
                Path page = new Path
                {
                    Data = Geometry.Parse("M3,2 L14,2 L19,7 L19,20 L3,20 Z"),
                    Fill = Brushes.White,
                    Opacity = 0.95
                };
                Path fold = new Path
                {
                    Data = Geometry.Parse("M14,2 L14,7 L19,7 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(180, 240, 210)),
                    Opacity = 0.95
                };
                // 3 ascending bar chart columns
                Rectangle bar1 = new Rectangle { Width = 2.4, Height = 5, Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)), RadiusX = 1, RadiusY = 1 };
                Canvas.SetLeft(bar1, 6);
                Canvas.SetTop(bar1, 13);

                Rectangle bar2 = new Rectangle { Width = 2.4, Height = 8, Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)), RadiusX = 1, RadiusY = 1 };
                Canvas.SetLeft(bar2, 9.8);
                Canvas.SetTop(bar2, 10);

                Rectangle bar3 = new Rectangle { Width = 2.4, Height = 11, Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)), RadiusX = 1, RadiusY = 1 };
                Canvas.SetLeft(bar3, 13.6);
                Canvas.SetTop(bar3, 7);

                canvas.Children.Add(page);
                canvas.Children.Add(fold);
                canvas.Children.Add(bar1);
                canvas.Children.Add(bar2);
                canvas.Children.Add(bar3);
            }
            else if (mode == OfficeAppMode.Slides)
            {
                // Modern slide easel / pie chart presentation
                Border slideRect = new Border
                {
                    Width = 18,
                    Height = 13,
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(2),
                    Opacity = 0.95
                };
                Canvas.SetLeft(slideRect, 2);
                Canvas.SetTop(slideRect, 3);

                Path pie = new Path
                {
                    Data = Geometry.Parse("M8,8 A3.5,3.5 0 1 0 11.5,11.5 L8,11.5 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(249, 115, 22))
                };
                Path pieSlice = new Path
                {
                    Data = Geometry.Parse("M9,7.5 L12.5,7.5 A3.5,3.5 0 0 1 12.5,10.5 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(217, 70, 239))
                };
                Path stand = new Path
                {
                    Data = Geometry.Parse("M6,18 L11,15 L16,18"),
                    Stroke = Brushes.White,
                    StrokeThickness = 1.6,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                canvas.Children.Add(slideRect);
                canvas.Children.Add(pie);
                canvas.Children.Add(pieSlice);
                canvas.Children.Add(stand);
            }
            else // PDF
            {
                // PDF document with bold 'P'
                Path page = new Path
                {
                    Data = Geometry.Parse("M3,2 L14,2 L19,7 L19,20 L3,20 Z"),
                    Fill = Brushes.White,
                    Opacity = 0.95
                };
                Path fold = new Path
                {
                    Data = Geometry.Parse("M14,2 L14,7 L19,7 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(254, 202, 202)),
                    Opacity = 0.95
                };
                TextBlock pText = new TextBlock
                {
                    Text = "P",
                    FontWeight = FontWeights.Black,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38))
                };
                Canvas.SetLeft(pText, 8.5);
                Canvas.SetTop(pText, 8.5);

                canvas.Children.Add(page);
                canvas.Children.Add(fold);
                canvas.Children.Add(pText);
            }

            return canvas;
        }

        private void PositionWindow(System.Drawing.Point clientPlusCenter, AppForm ownerForm)
        {
            if (ownerForm == null) return;

            // Convert WinForms client coordinates to screen pixels
            System.Drawing.Point screenPt = ownerForm.PointToScreen(clientPlusCenter);

            // Convert screen pixels to WPF DIPs
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }
                else if (ownerForm != null && ownerForm.IsHandleCreated)
                {
                    using (var g = ownerForm.CreateGraphics())
                    {
                        dpiScaleX = g.DpiX / 96.0;
                        dpiScaleY = g.DpiY / 96.0;
                    }
                }
            }
            catch { }

            double dipX = screenPt.X / dpiScaleX;
            double dipY = screenPt.Y / dpiScaleY;

            // Align popover card: anchored directly below the '+' button with 20px padding offset compensation
            double targetLeft = dipX - 44; // aligns neatly with the button
            double targetTop = dipY + 12 - 20; // 20 is the root margin offset

            // Clamp to screen working area
            var screen = System.Windows.Forms.Screen.FromControl(ownerForm) ?? System.Windows.Forms.Screen.PrimaryScreen;
            if (screen != null)
            {
                double waRight = screen.WorkingArea.Right / dpiScaleX;
                double waBottom = screen.WorkingArea.Bottom / dpiScaleY;
                double cardW = 270 + 40;
                double cardH = 260 + 40;

                if (targetLeft + cardW > waRight) targetLeft = waRight - cardW - 10;
                if (targetLeft < 10) targetLeft = 10;
                if (targetTop + cardH > waBottom) targetTop = dipY - cardH;
            }

            Left = targetLeft;
            Top = targetTop;
        }

        private void PlayEntranceAnimation()
        {
            // Apple fluid entrance: 180ms ease-out cubic
            Duration duration = new Duration(TimeSpan.FromMilliseconds(180));
            CubicEase easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

            DoubleAnimation opacityAnim = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = easeOut };
            DoubleAnimation transYAnim = new DoubleAnimation(-12.0, 0.0, duration) { EasingFunction = easeOut };
            DoubleAnimation scaleXAnim = new DoubleAnimation(0.94, 1.0, duration) { EasingFunction = easeOut };
            DoubleAnimation scaleYAnim = new DoubleAnimation(0.94, 1.0, duration) { EasingFunction = easeOut };

            _cardContainer.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, transYAnim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        public void CloseSmoothly()
        {
            if (_isClosing) return;
            _isClosing = true;

            Duration duration = new Duration(TimeSpan.FromMilliseconds(130));
            CubicEase easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            DoubleAnimation opacityAnim = new DoubleAnimation(1.0, 0.0, duration) { EasingFunction = easeIn };
            DoubleAnimation transYAnim = new DoubleAnimation(0.0, -8.0, duration) { EasingFunction = easeIn };
            DoubleAnimation scaleXAnim = new DoubleAnimation(1.0, 0.96, duration) { EasingFunction = easeIn };
            DoubleAnimation scaleYAnim = new DoubleAnimation(1.0, 0.96, duration) { EasingFunction = easeIn };

            opacityAnim.Completed += (s, e) =>
            {
                _currentInstance = null;
                Close();
            };

            _cardContainer.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, transYAnim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        private void ExecuteSelection(OfficeAppMode mode)
        {
            CloseSmoothly();
            try
            {
                _onSelect?.Invoke(mode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Selection error: " + ex.Message);
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseSmoothly();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                _focusedIndex = (_focusedIndex + 1) % 4;
                HighlightFocusedItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                _focusedIndex = (_focusedIndex <= 0) ? 3 : _focusedIndex - 1;
                HighlightFocusedItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (_focusedIndex >= 0 && _focusedIndex < 4)
                {
                    OfficeAppMode[] modes = new[] { OfficeAppMode.Writer, OfficeAppMode.Calc, OfficeAppMode.Slides, OfficeAppMode.Pdf };
                    ExecuteSelection(modes[_focusedIndex]);
                }
                e.Handled = true;
            }
        }

        private void HighlightFocusedItem()
        {
            SolidColorBrush hoverBrush = new SolidColorBrush(Color.FromArgb(160, 241, 245, 249));
            for (int i = 0; i < 4; i++)
            {
                _itemBorders[i].Background = (i == _focusedIndex) ? hoverBrush : Brushes.Transparent;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_currentInstance == this) _currentInstance = null;
        }
    }
}
