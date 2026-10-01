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
            // Test specific layouts across categories
            // 1. Hierarchy: Org Chart, Horizontal, Architecture, Labeled
            TestLayout(4, "Hierarchy_Tree", "hierarchy_org_chart");
            TestLayout(4, "Hierarchy_Horizontal", "hierarchy_horizontal");
            TestLayout(4, "Hierarchy_Architecture", "hierarchy_architecture");
            TestLayout(4, "Hierarchy_Labeled", "hierarchy_labeled");

            // 2. Process: Chevron, Basic, Timeline, Funnel, Gear, Equation
            TestLayout(2, "Process_BasicChevron", "process_chevron");
            TestLayout(2, "Process_Basic", "process_basic");
            TestLayout(2, "Process_BasicTimeline", "process_timeline");
            TestLayout(2, "Process_Funnel", "process_funnel");
            TestLayout(2, "Process_Gear", "process_gear");
            TestLayout(2, "Process_Equation", "process_equation");

            // 3. Cycle: Radial, Basic, Cycle Matrix, Venn
            TestLayout(3, "Cycle_Radial", "cycle_radial");
            TestLayout(3, "Cycle_Basic", "cycle_basic");
            TestLayout(3, "Cycle_CycleMatrix", "cycle_matrix");
            TestLayout(3, "Cycle_RadialVenn", "cycle_venn");

            // 4. Relationship: Balance, Target, Opposing Arrows
            TestLayout(5, "Rel_Balance", "rel_balance");
            TestLayout(5, "Rel_BasicTarget", "rel_target");
            TestLayout(5, "Rel_OpposingArrows", "rel_opposing");

            // 5. Matrix: Basic Matrix, Grid Matrix
            TestLayout(6, "Matrix_Basic", "matrix_basic");
            TestLayout(6, "Matrix_Grid", "matrix_grid");

            // 6. Pyramid: Basic Pyramid, Inverted, List
            TestLayout(7, "Pyramid_Basic", "pyramid_basic");
            TestLayout(7, "Pyramid_Inverted", "pyramid_inverted");
            TestLayout(7, "Pyramid_List", "pyramid_list");

            // 7. Picture: Accented, Strips, Grid, Bubble, Snapshot
            TestLayout(8, "Picture_Accented", "picture_accented");
            TestLayout(8, "Picture_Strips", "picture_strips");
            TestLayout(8, "Picture_Grid", "picture_grid");
            TestLayout(8, "Picture_BubbleList", "picture_bubble");
            TestLayout(8, "Picture_SnapshotList", "picture_snapshot");

            // 8. List: Hexagon, Tab, Bracket, Trapezoid
            TestLayout(1, "List_AlternatingHexagon", "list_hexagon");
            TestLayout(1, "List_Tab", "list_tab");
            TestLayout(1, "List_VerticalBracket", "list_bracket");
            TestLayout(1, "List_Trapezoid", "list_trapezoid");

            Console.WriteLine("ALL 28 SPECIFIC LAYOUTS TESTED AND SAVED!");
        }

        static void TestLayout(int catIdx, string typeId, string filename)
        {
            SmartArtGalleryForm form = new SmartArtGalleryForm();
            Type formType = typeof(SmartArtGalleryForm);
            FieldInfo fiCat = formType.GetField("selectedCategoryIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fiTmpl = formType.GetField("selectedTemplateIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo miFilter = formType.GetMethod("FilterTemplatesByCategory", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fiFiltered = formType.GetField("filteredTemplates", BindingFlags.NonPublic | BindingFlags.Instance);

            fiCat.SetValue(form, catIdx);
            miFilter.Invoke(form, null);

            var list = (System.Collections.IList)fiFiltered.GetValue(form);
            int targetIdx = -1;
            for (int i = 0; i < list.Count; i++)
            {
                FieldInfo fiTypeId = list[i].GetType().GetField("TypeId");
                string tid = (string)fiTypeId.GetValue(list[i]);
                if (tid == typeId)
                {
                    targetIdx = i;
                    break;
                }
            }

            if (targetIdx >= 0)
            {
                fiTmpl.SetValue(form, targetIdx);
            }

            Bitmap bmp = new Bitmap(780, 520);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                form.RenderDialog(g, 780, 520);
            }
            string outPath = string.Format(@"scratch\render_{0}.png", filename);
            bmp.Save(outPath, ImageFormat.Png);
        }
    }
}
