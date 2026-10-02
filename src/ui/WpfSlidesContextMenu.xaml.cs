using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GenOffice.UI
{
    public partial class WpfSlidesContextMenu : Window
    {
        public event EventHandler CutRequested;
        public event EventHandler CopyRequested;
        public event EventHandler PasteRequested;
        public event EventHandler BringToFrontRequested;
        public event EventHandler BringForwardRequested;
        public event EventHandler SendToBackRequested;
        public event EventHandler SendBackwardRequested;
        public event EventHandler GroupRequested;
        public event EventHandler UngroupRequested;
        public event EventHandler FormatShapeRequested;
        
        public event EventHandler BoldRequested;
        public event EventHandler ItalicRequested;
        public event EventHandler UnderlineRequested;
        public event EventHandler ColorRequested;

        private bool _isClosing = false;

        public WpfSlidesContextMenu()
        {
            InitializeComponent();
        }

        private void CloseMenu()
        {
            if (!_isClosing)
            {
                _isClosing = true;
                this.Close();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            CloseMenu();
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            CutRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuBringFront_Click(object sender, RoutedEventArgs e)
        {
            BringToFrontRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuBringForward_Click(object sender, RoutedEventArgs e)
        {
            BringForwardRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuSendBack_Click(object sender, RoutedEventArgs e)
        {
            SendToBackRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuSendBackward_Click(object sender, RoutedEventArgs e)
        {
            SendBackwardRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuGroup_Click(object sender, RoutedEventArgs e)
        {
            GroupRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuUngroup_Click(object sender, RoutedEventArgs e)
        {
            UngroupRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void MenuFormat_Click(object sender, RoutedEventArgs e)
        {
            FormatShapeRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            BoldRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            ItalicRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void BtnUnderline_Click(object sender, RoutedEventArgs e)
        {
            UnderlineRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }

        private void BtnColor_Click(object sender, RoutedEventArgs e)
        {
            ColorRequested?.Invoke(this, EventArgs.Empty);
            CloseMenu();
        }
    }
}
