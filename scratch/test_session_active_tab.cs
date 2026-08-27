using System;
using System.IO;
using GenOffice.Core;
using GenOffice.UI;

namespace TestSessionActiveTab
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // 1. Create AppForm with 3 tabs
                AppForm form1 = new AppForm();
                form1.Ribbon.AddDocTab("TaiLieu1.docx", OfficeAppMode.Writer);
                form1.Ribbon.AddDocTab("BangTinh2.xlsx", OfficeAppMode.Calc);
                form1.Ribbon.AddDocTab("ThuyetTrinh3.pptx", OfficeAppMode.Slides);

                // Make Tab 2 ("BangTinh2.xlsx") active
                form1.Ribbon.SwitchDocTab(2);
                Console.WriteLine("FORM 1 ACTIVE TAB: Index=" + form1.Ribbon.ActiveDocIndex + ", Title=" + form1.Ribbon.DocTabs[form1.Ribbon.ActiveDocIndex].Title);

                // Save session
                SessionManager.SaveSession(form1);
                Console.WriteLine("SESSION SAVED!");

                // 2. Create fresh AppForm and Restore Session
                AppForm form2 = new AppForm();
                SessionManager.RestoreSessionIntoApp(form2);

                Console.WriteLine("FORM 2 RESTORED TABS COUNT: " + form2.Ribbon.DocTabs.Count);
                Console.WriteLine("FORM 2 ACTIVE TAB: Index=" + form2.Ribbon.ActiveDocIndex + ", Title=" + form2.Ribbon.DocTabs[form2.Ribbon.ActiveDocIndex].Title + ", Mode=" + form2.Ribbon.CurrentMode);

                if (form2.Ribbon.ActiveDocIndex == 2 && form2.Ribbon.DocTabs[2].Title == "BangTinh2.xlsx" && form2.Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    Console.WriteLine("[TEST PASSED] The exact last active tab was restored and activated successfully!");
                }
                else
                {
                    Console.WriteLine("[TEST FAILED] Active tab mismatch!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}
