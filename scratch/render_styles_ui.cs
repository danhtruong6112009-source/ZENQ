using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

namespace GenOfficeTest
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Assembly asm = Assembly.LoadFrom("GenOffice.exe");
            Type appFormType = asm.GetType("GenOffice.UI.AppForm");
            if (appFormType == null)
            {
                Console.WriteLine("AppForm not found!");
                return;
            }

            Form form = (Form)Activator.CreateInstance(appFormType);
            form.Width = 1280;
            form.Height = 850;
            form.Show();

            Type modeEnumType = asm.GetType("GenOffice.UI.OfficeAppMode");
            MethodInfo switchMode = appFormType.GetMethod("SwitchAppMode");
            switchMode.Invoke(form, new object[] { Enum.Parse(modeEnumType, "Writer") });

            // Switch to Writer tab
            PropertyInfo ribbonProp = appFormType.GetProperty("Ribbon");
            object ribbon = ribbonProp.GetValue(form, null);
            PropertyInfo writerProp = appFormType.GetProperty("WriterView");
            object writerView = writerProp.GetValue(form, null);

            // Add text and headings
            MethodInfo applyQuickStyle = writerView.GetType().GetMethod("ApplyQuickStyle", new Type[] { typeof(string), ribbon.GetType() });
            PropertyInfo curPosProp = writerView.GetType().GetProperty("CursorPosition");
            PropertyInfo ptProp = writerView.GetType().GetProperty("PieceTable");
            object pt = ptProp.GetValue(writerView, null);
            pt.GetType().GetMethod("Insert").Invoke(pt, new object[] { 0, "Tiêu Đề Báo Cáo Tài Chính 2026\n1. Tổng quan tình hình hoạt động\nNăm 2026 ghi nhận mức tăng trưởng vượt bậc của hệ sinh thái phần mềm văn phòng GenOffice.\n2. Phân tích doanh thu và thị phần\nDoanh thu toàn diện tăng 150% so với cùng kỳ năm trước." });

            // Apply Heading 1 to line 0
            curPosProp.SetValue(writerView, 0, null);
            applyQuickStyle.Invoke(writerView, new object[] { "Title", ribbon });

            // Apply Heading 2 to line 1
            curPosProp.SetValue(writerView, 35, null);
            applyQuickStyle.Invoke(writerView, new object[] { "Heading 1", ribbon });

            // Apply Heading 2 to line 3
            curPosProp.SetValue(writerView, 140, null);
            applyQuickStyle.Invoke(writerView, new object[] { "Heading 2", ribbon });

            // Open Navigation Pane
            PropertyInfo showNavProp = writerView.GetType().GetProperty("ShowNavigationPane");
            if (showNavProp != null) showNavProp.SetValue(writerView, true, null);

            // Toggle Format Painter Active
            MethodInfo toggleFmt = writerView.GetType().GetMethod("ToggleFormatPainter", new Type[] { typeof(bool), ribbon.GetType() });
            toggleFmt.Invoke(writerView, new object[] { false, ribbon });

            // Re-render
            Application.DoEvents();
            System.Threading.Thread.Sleep(200);
            Application.DoEvents();

            Bitmap bmp = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
            bmp.Save("scratch\\verify_styles_ui.png", ImageFormat.Png);
            Console.WriteLine("Saved screenshot to scratch\\verify_styles_ui.png");

            form.Close();
        }
    }
}
