using System;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                {
                    MessageBox.Show("Chi tiết sự cố:\n" + e.Exception.Message + "\n\nTại:\n" + e.Exception.StackTrace, "GenOffice - Thông báo lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    Exception ex = e.ExceptionObject as Exception;
                    string msg = ex != null ? ex.ToString() : "Lỗi không xác định";
                    MessageBox.Show("Sự cố hệ thống:\n" + msg, "GenOffice - Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                Application.Run(new GenOfficeApplicationContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động GenOffice:\n" + ex.ToString(), "GenOffice Crash Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Điều phối vòng đời ứng dụng GenOffice: Khởi chạy Màn hình Động (Animated Splash Screen) trước,
    /// sau đó chuyển tiếp mượt mà sang Cửa sổ chính AppForm.
    /// </summary>
    public class GenOfficeApplicationContext : ApplicationContext
    {
        private GenOfficeSplashScreen _splash;

        public GenOfficeApplicationContext()
        {
            _splash = new GenOfficeSplashScreen();
            _splash.OnSplashFinished = () =>
            {
                AppForm mainForm = new AppForm();
                MainForm = mainForm;
                mainForm.FormClosed += (s, e) => ExitThread();
                SessionManager.RestoreSessionIntoApp(mainForm);
                mainForm.Show();
                mainForm.Activate();
                mainForm.Focus();
            };
            _splash.Show();
        }
    }
}
