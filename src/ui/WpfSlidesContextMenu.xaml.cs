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
        public event EventHandler SendToBackRequested;
        public event EventHandler GroupRequested;
        public event EventHandler UngroupRequested;
        public event EventHandler FormatShapeRequested;
        
        public event EventHandler BoldRequested;
        public event EventHandler ItalicRequested;
        public event EventHandler UnderlineRequested;
        public event EventHandler ColorRequested;

        public WpfSlidesContextMenu()
        {
            InitializeComponent();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            CutRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuBringFront_Click(object sender, RoutedEventArgs e)
        {
            BringToFrontRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuSendBack_Click(object sender, RoutedEventArgs e)
        {
            SendToBackRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuGroup_Click(object sender, RoutedEventArgs e)
        {
            GroupRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuUngroup_Click(object sender, RoutedEventArgs e)
        {
            UngroupRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void MenuFormat_Click(object sender, RoutedEventArgs e)
        {
            FormatShapeRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            BoldRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            ItalicRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void BtnUnderline_Click(object sender, RoutedEventArgs e)
        {
            UnderlineRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void BtnColor_Click(object sender, RoutedEventArgs e)
        {
            ColorRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
    }
}
