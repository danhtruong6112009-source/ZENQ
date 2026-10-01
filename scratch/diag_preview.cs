using System;
using System.Reflection;
using GenOffice.UI;

namespace GenOffice.Test
{
    class Diag
    {
        static void Main()
        {
            SmartArtGalleryForm form = new SmartArtGalleryForm();
            Type formType = typeof(SmartArtGalleryForm);
            FieldInfo fiCat = formType.GetField("selectedCategoryIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fiTmpl = formType.GetField("selectedTemplateIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo miFilter = formType.GetMethod("FilterTemplatesByCategory", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fiFiltered = formType.GetField("filteredTemplates", BindingFlags.NonPublic | BindingFlags.Instance);

            int[] testCategories = new int[] { 4, 2, 3, 5, 6, 7, 8, 1 };
            string[] names = new string[] { "hierarchy", "process", "cycle", "relationship", "matrix", "pyramid", "picture", "list" };

            for (int i = 0; i < testCategories.Length; i++)
            {
                fiCat.SetValue(form, testCategories[i]);
                miFilter.Invoke(form, null);
                var list = (System.Collections.IList)fiFiltered.GetValue(form);
                int pick = Math.Min(1, list.Count - 1);
                fiTmpl.SetValue(form, pick);
                
                int selIdx = (int)fiTmpl.GetValue(form);
                object tmpl = list[selIdx];
                Type tmplType = tmpl.GetType();
                string typeId = (string)tmplType.GetField("TypeId").GetValue(tmpl);
                string name = (string)tmplType.GetField("Name").GetValue(tmpl);
                Console.WriteLine("{0}: catIdx={1}, selIdx={2}, TypeId={3}, Name={4}", names[i], testCategories[i], selIdx, typeId, name);
            }
        }
    }
}
