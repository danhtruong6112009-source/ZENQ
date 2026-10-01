using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using GenOffice.UI;

namespace GenOffice.Test
{
    class Program
    {
        static void Main()
        {
            SmartArtGalleryForm form = new SmartArtGalleryForm();
            Type formType = typeof(SmartArtGalleryForm);
            FieldInfo fiCat = formType.GetField("selectedCategoryIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fiTmpl = formType.GetField("selectedTemplateIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo miFilter = formType.GetMethod("FilterTemplatesByCategory", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fiFiltered = formType.GetField("filteredTemplates", BindingFlags.NonPublic | BindingFlags.Instance);

            // Test Cycle
            fiCat.SetValue(form, 3); // Cycle
            miFilter.Invoke(form, null);
            fiTmpl.SetValue(form, 1); // Cycle_BasicPie
            
            var list = (System.Collections.IList)fiFiltered.GetValue(form);
            object item = list[1];
            FieldInfo fiTypeId = item.GetType().GetField("TypeId");
            string tid = (string)fiTypeId.GetValue(item);
            Console.WriteLine("TypeId: " + tid);

            Bitmap bmp = new Bitmap(240, 200);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                MethodInfo miDraw = formType.GetMethod("DrawOfficePreviewDiagram", BindingFlags.NonPublic | BindingFlags.Instance);
                miDraw.Invoke(form, new object[] { g, new Rectangle(10, 10, 220, 180), tid });
            }
            bmp.Save(@"scratch\debug_pie.png", ImageFormat.Png);
            Console.WriteLine("Saved debug_pie.png");
        }
    }
}
