using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw |
                          ControlStyles.Selectable, true);
            this.UpdateStyles();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            this.Invalidate();
        }
    }

    public class SmartArtGalleryForm : Form
    {
        public string SelectedSmartArtType { get; private set; }
        public string SelectedSmartArtName { get; private set; }

        private DoubleBufferedPanel leftNavPanel;
        private DoubleBufferedPanel centerCardsPanel;
        private DoubleBufferedPanel rightPreviewPanel;
        private Panel bottomActionBar;

        private List<string> categories = new List<string> {
            "All", "List", "Process", "Cycle", "Hierarchy", "Relationship", "Matrix", "Pyramid", "Picture", "Office.com"
        };

        private List<string> categoryDisplayNames = new List<string> {
            "Tất cả", "Danh sách", "Quy trình", "Chu trình", "Cơ cấu phân cấp", "Quan hệ", "Ma trận", "Hình tháp", "Hình ảnh", "Office.com"
        };

        private int selectedCategoryIndex = 0; // Default to All (Tất cả) like MS Office

        public class SmartArtTemplate
        {
            public string Category;
            public string TypeId;
            public string Name;
            public string Description;
            public Rectangle Bounds;

            public SmartArtTemplate(string cat, string id, string name, string desc)
            {
                Category = cat;
                TypeId = id;
                Name = name;
                Description = desc;
            }
        }

        private static List<SmartArtTemplate> allTemplates = new List<SmartArtTemplate>();

        static SmartArtGalleryForm()
        {
            InitializeCatalog();
        }

        private static void InitializeCatalog()
        {
            allTemplates = new List<SmartArtTemplate>
            {
                // ==========================================
                // 1. LIST — 38 layout
                // ==========================================
                new SmartArtTemplate("List", "List_AlternatingHexagon", "Alternating Hexagon List", "Chuỗi ý tưởng liên kết nhau dạng lục giác xen kẽ, chữ cấp 1 nằm trong hình, cấp 2 nằm ngoài."),
                new SmartArtTemplate("List", "List_BasicBlock", "Basic Block List", "Khối thông tin không theo trình tự, tận dụng tối đa không gian ngang lẫn dọc."),
                new SmartArtTemplate("List", "List_BendingPictureAccent", "Bending Picture Accent List", "Danh sách kèm ảnh tròn nhỏ, tốt cho cả hai cấp chữ."),
                new SmartArtTemplate("List", "List_ContinuousPicture", "Continuous Picture List", "Nhóm thông tin liên kết, các hình tròn dùng để chèn ảnh."),
                new SmartArtTemplate("List", "List_DescendingBlock", "Descending Block List", "Danh sách khối giảm dần chiều cao, chữ cấp 1 hiển thị dọc."),
                new SmartArtTemplate("List", "List_DetailedProcess", "Detailed Process", "Diễn giải các giai đoạn với lượng chữ cấp 2 lớn."),
                new SmartArtTemplate("List", "List_Grouped", "Grouped List", "Nhóm/nhóm con, hoặc bước/bước con trong quy trình."),
                new SmartArtTemplate("List", "List_Hierarchy", "Hierarchy List", "Quan hệ phân cấp theo từng nhóm, cũng dùng để liệt kê thường."),
                new SmartArtTemplate("List", "List_HorizontalBullet", "Horizontal Bullet List", "Danh sách ngang, hợp với lượng chữ lớn, không phân biệt thứ tự."),
                new SmartArtTemplate("List", "List_HorizontalPicture", "Horizontal Picture List", "Danh sách ngang nhấn mạnh ảnh ở các ô trên."),
                new SmartArtTemplate("List", "List_IncreasingCircle", "Increasing Circle Process", "Chuỗi bước với vòng tròn to dần, tối đa 7 bước cấp 1."),
                new SmartArtTemplate("List", "List_Lined", "Lined List", "Danh mục/tiểu mục nhiều cấp, ngăn cách bằng đường kẻ."),
                new SmartArtTemplate("List", "List_PictureAccent", "Picture Accent List", "Nhóm thông tin có ảnh nhỏ góc trên, nhấn chữ cấp 2."),
                new SmartArtTemplate("List", "List_PictureCaption", "Picture Caption List", "Khối ảnh có chú thích ngắn bên dưới."),
                new SmartArtTemplate("List", "List_PieProcess", "Pie Process", "Các bước dạng lát bánh (pie) tăng dần kích thước, tối đa 7 bước."),
                new SmartArtTemplate("List", "List_PlusMinus", "Plus and Minus", "So sánh ưu – nhược của 2 ý, giới hạn 2 mục cấp 1."),
                new SmartArtTemplate("List", "List_Pyramid", "Pyramid List", "Quan hệ tỉ lệ/phân cấp, chữ nằm trong khối chữ nhật trên nền kim tự tháp."),
                new SmartArtTemplate("List", "List_Reverse", "Reverse List", "Thể hiện chuyển đổi/đảo thứ tự giữa 2 mục."),
                new SmartArtTemplate("List", "List_SegmentedProcess", "Segmented Process", "Từng bước một hộp riêng, nhấn chữ cấp 2."),
                new SmartArtTemplate("List", "List_Stacked", "Stacked List", "Vòng tròn chứa chữ cấp 1, hình chữ nhật kèm theo chứa chữ cấp 2."),
                new SmartArtTemplate("List", "List_SquareAccent", "Square Accent List", "Danh sách phân loại, chữ cấp 2 cạnh ô vuông nhỏ."),
                new SmartArtTemplate("List", "List_Tab", "Tab List", "Danh sách dạng tab, hợp chữ cấp 1 ngắn."),
                new SmartArtTemplate("List", "List_TableHierarchy", "Table Hierarchy", "Nhóm thông tin từ trên xuống, không có đường nối."),
                new SmartArtTemplate("List", "List_Table", "Table List", "Thông tin nhóm/liên quan giá trị ngang nhau dạng bảng."),
                new SmartArtTemplate("List", "List_Target", "Target List", "Thông tin liên quan/chồng lấn, tối đa 7 mục cấp 1."),
                new SmartArtTemplate("List", "List_Trapezoid", "Trapezoid List", "Thông tin nhóm giá trị ngang nhau, hợp lượng chữ lớn."),
                new SmartArtTemplate("List", "List_VaryingWidth", "Varying Width List", "Nhấn các mục có \"trọng số\" khác nhau, độ rộng ô tự co theo chữ."),
                new SmartArtTemplate("List", "List_VerticalBox", "Vertical Box List", "Nhiều nhóm thông tin, hợp chữ cấp 2 dài — danh sách gạch đầu dòng."),
                new SmartArtTemplate("List", "List_VerticalBlock", "Vertical Block List", "Nhóm thông tin/bước quy trình, ý chính kèm nhiều ý phụ."),
                new SmartArtTemplate("List", "List_VerticalBracket", "Vertical Bracket List", "Khối thông tin nhóm, hợp chữ cấp 2 dài."),
                new SmartArtTemplate("List", "List_VerticalBullet", "Vertical Bullet List", "Danh sách dọc, hợp tiêu đề/thông tin cấp 1 dài."),
                new SmartArtTemplate("List", "List_VerticalChevron", "Vertical Chevron List", "Bước tuần tự dạng mũi tên dọc, nhấn chữ cấp 2."),
                new SmartArtTemplate("List", "List_VerticalPictureAccent", "Vertical Picture Accent List", "Danh sách dọc kèm vòng tròn nhỏ chứa ảnh."),
                new SmartArtTemplate("List", "List_VerticalPicture", "Vertical Picture List", "Danh sách dọc, ô nhỏ bên trái chứa ảnh."),
                new SmartArtTemplate("List", "List_VerticalAccent", "Vertical Accent List", "Chữ cấp 2 trong khối chữ nhật trên nền mũi tên chevron dọc."),
                new SmartArtTemplate("List", "List_VerticalArrow", "Vertical Arrow List", "Các bước hướng tới mục tiêu chung, dạng mũi tên dọc."),
                new SmartArtTemplate("List", "List_VerticalCircle", "Vertical Circle List", "Dữ liệu tuần tự/nhóm, hình tròn lớn cho chữ cấp 1."),
                new SmartArtTemplate("List", "List_VerticalCurved", "Vertical Curved List", "Danh sách theo đường cong, có thể chèn ảnh vào hình tròn nhấn."),

                // ==========================================
                // 2. PROCESS — 46 layout
                // ==========================================
                new SmartArtTemplate("Process", "Process_Accent", "Accent Process", "Tiến trình/timeline/bước quy trình, tốt cho cả 2 cấp chữ."),
                new SmartArtTemplate("Process", "Process_AlternatingFlow", "Alternating Flow", "Nhóm thông tin/bước quy trình, nhấn quan hệ tương tác giữa các nhóm."),
                new SmartArtTemplate("Process", "Process_ArrowRibbon", "Arrow Ribbon", "Hai ý liên quan hoặc đối lập có kết nối (VD: 2 lực đối nghịch)."),
                new SmartArtTemplate("Process", "Process_AscendingPictureAccent", "Ascending Picture Accent Process", "Chuỗi ảnh tăng dần kèm mô tả ngắn."),
                new SmartArtTemplate("Process", "Process_BasicBending", "Basic Bending Process", "Bước quy trình dạng gấp khúc, tận dụng không gian."),
                new SmartArtTemplate("Process", "Process_BasicChevron", "Basic Chevron Process", "Tiến trình/timeline dạng mũi tên chevron, nhấn hướng đi."),
                new SmartArtTemplate("Process", "Process_Basic", "Basic Process", "Quy trình các bước tuần tự cơ bản, đơn giản nhất."),
                new SmartArtTemplate("Process", "Process_BasicTimeline", "Basic Timeline", "Các mốc/bước theo thời gian, hợp cả 2 cấp chữ."),
                new SmartArtTemplate("Process", "Process_ChevronAccent", "Chevron Accent Process", "Bước tuần tự nhấn hướng đi, chữ ngắn gọn."),
                new SmartArtTemplate("Process", "Process_ChevronList", "Chevron List", "Nhiều quy trình con hợp thành một quy trình lớn, hoặc so sánh các quy trình."),
                new SmartArtTemplate("Process", "Process_CircularBending", "Circular Bending Process", "Chuỗi bước dài/không tuyến tính, chỉ hợp chữ cấp 1."),
                new SmartArtTemplate("Process", "Process_ClosedChevron", "Closed Chevron Process", "Tiến trình/timeline nhấn hình khởi đầu, chỉ hợp chữ cấp 1."),
                new SmartArtTemplate("Process", "Process_CircleAccentTimeline", "Circle Accent Timeline", "Sự kiện/mốc thời gian với vòng tròn lớn – nhỏ phân biệt cấp chữ."),
                new SmartArtTemplate("Process", "Process_CircleArrow", "Circle Arrow Process", "Các bước tuần tự kèm chữ mô tả, hợp chữ cấp 1 ngắn."),
                new SmartArtTemplate("Process", "Process_ContinuousArrow", "Continuous Arrow Process", "Timeline/quy trình dạng mũi tên liên tục, chữ cấp 1 nằm trong mũi tên."),
                new SmartArtTemplate("Process", "Process_ContinuousBlock", "Continuous Block Process", "Quy trình liên tục dạng khối, hợp chữ ngắn cả 2 cấp."),
                new SmartArtTemplate("Process", "Process_ContinuousPictureList", "Continuous Picture List", "Nhóm thông tin liên kết có thể chèn ảnh dạng tròn."),
                new SmartArtTemplate("Process", "Process_ConvergingArrows", "Converging Arrows", "Các ý hội tụ về một điểm trung tâm."),
                new SmartArtTemplate("Process", "Process_ConvergingText", "Converging Text", "Nhiều bước/phần hợp thành một, tối đa 5 nhánh cấp 2."),
                new SmartArtTemplate("Process", "Process_Descending", "Descending Process", "Chuỗi sự kiện giảm dần, tối đa 7 mục cấp 1."),
                new SmartArtTemplate("Process", "Process_Detailed", "Detailed Process", "Diễn giải tiến trình với lượng chữ cấp 2 lớn."),
                new SmartArtTemplate("Process", "Process_DivergingArrows", "Diverging Arrows", "Ý tưởng lan toả ra từ một nguồn trung tâm."),
                new SmartArtTemplate("Process", "Process_Equation", "Equation", "Các bước/nhiệm vụ dẫn tới kết quả, hiển thị như phép toán (=)."),
                new SmartArtTemplate("Process", "Process_Funnel", "Funnel", "Lọc thông tin, các phần hợp lại thành kết quả cuối (phễu)."),
                new SmartArtTemplate("Process", "Process_Gear", "Gear", "Ý tưởng liên kết chặt như bánh răng ăn khớp, tối đa 3 mục."),
                new SmartArtTemplate("Process", "Process_IncreasingArrows", "Increasing Arrows Process", "Bước tuần tự chồng lấn tăng dần, tối đa 5 mục cấp 1."),
                new SmartArtTemplate("Process", "Process_InterconnectedBlock", "Interconnected Block Process", "Các bước liên kết nhau, hợp chữ cấp 1 ngắn – cấp 2 vừa."),
                new SmartArtTemplate("Process", "Process_OpposingArrows", "Opposing Arrows", "Hai ý đối lập, mỗi ý một mũi tên."),
                new SmartArtTemplate("Process", "Process_Phased", "Phased Process", "3 giai đoạn của một quy trình, mỗi giai đoạn có mục con."),
                new SmartArtTemplate("Process", "Process_PictureAccent", "Picture Accent Process", "Bước quy trình có nền hình chữ nhật để chèn ảnh."),
                new SmartArtTemplate("Process", "Process_PieProcess", "Pie Process", "Các bước dạng lát bánh tăng dần."),
                new SmartArtTemplate("Process", "Process_ProcessArrows", "Process Arrows", "Quy trình/luồng công việc, chữ cấp 1 trong hình tròn – cấp 2 trong mũi tên."),
                new SmartArtTemplate("Process", "Process_ProcessList", "Process List", "Nhiều nhóm bước/bước con, nhấn cấu trúc bước cha–con."),
                new SmartArtTemplate("Process", "Process_RandomToResult", "Random to Result Process", "Từ nhiều ý hỗn loạn dẫn tới một mục tiêu thống nhất."),
                new SmartArtTemplate("Process", "Process_RepeatingBending", "Repeating Bending Process", "Bước quy trình gấp khúc lặp lại, tận dụng không gian."),
                new SmartArtTemplate("Process", "Process_Segmented", "Segmented Process", "Mỗi bước một hộp riêng, nhấn chữ cấp 2."),
                new SmartArtTemplate("Process", "Process_Staggered", "Staggered Process", "Tiến trình đi xuống theo bậc thang, tối đa 5 mục."),
                new SmartArtTemplate("Process", "Process_StepDown", "Step Down Process", "Quy trình giảm dần nhiều bước/bước con."),
                new SmartArtTemplate("Process", "Process_StepUp", "Step Up Process", "Chuỗi bước tăng dần."),
                new SmartArtTemplate("Process", "Process_SubStep", "Sub-Step Process", "Quy trình nhiều bước có bước con xen giữa, tối đa 7 bước chính."),
                new SmartArtTemplate("Process", "Process_TimelinePipe", "Timeline Pipe", "Bước theo thời gian, chữ cấp 1 nổi bật hơn cấp 2."),
                new SmartArtTemplate("Process", "Process_UpwardArrow", "Upward Arrow", "Các bước có xu hướng đi lên, tối đa 5 mục."),
                new SmartArtTemplate("Process", "Process_VerticalArrowList", "Vertical Arrow List", "Bước hướng tới mục tiêu chung dạng mũi tên dọc."),
                new SmartArtTemplate("Process", "Process_VerticalBending", "Vertical Bending Process", "Bước quy trình gấp khúc dọc, nhấn quan hệ giữa các bước hơn là hướng đi."),
                new SmartArtTemplate("Process", "Process_VerticalChevronList", "Vertical Chevron List", "Bước tuần tự dọc, nhấn chữ cấp 2."),
                new SmartArtTemplate("Process", "Process_VerticalEquation", "Vertical Equation", "Các bước dẫn tới kết quả, hiển thị dạng phép toán theo chiều dọc."),
                new SmartArtTemplate("Process", "Process_Vertical", "Vertical Process", "Quy trình từ trên xuống, hợp chữ cấp 1 vì không gian dọc hạn chế."),

                // ==========================================
                // 3. CYCLE — 16 layout
                // ==========================================
                new SmartArtTemplate("Cycle", "Cycle_Basic", "Basic Cycle", "Chuỗi giai đoạn/sự kiện lặp lại theo vòng tròn, nhấn từng bước."),
                new SmartArtTemplate("Cycle", "Cycle_BasicPie", "Basic Pie", "Các phần hợp thành một tổng thể, tối đa 7 lát bánh."),
                new SmartArtTemplate("Cycle", "Cycle_BasicRadial", "Basic Radial", "Quan hệ với một ý trung tâm trong chu trình."),
                new SmartArtTemplate("Cycle", "Cycle_Block", "Block Cycle", "Chuỗi giai đoạn lặp lại dạng khối, nhấn từng bước."),
                new SmartArtTemplate("Cycle", "Cycle_CircleArrowProcess", "Circle Arrow Process", "Các mục tuần tự kèm mô tả, hợp chữ cấp 1 ngắn."),
                new SmartArtTemplate("Cycle", "Cycle_Continuous", "Continuous Cycle", "Chu trình liên tục, nhấn sự kết nối giữa tất cả thành phần."),
                new SmartArtTemplate("Cycle", "Cycle_CycleMatrix", "Cycle Matrix", "Quan hệ với ý trung tâm theo dạng chu kỳ, tối đa 4 lát."),
                new SmartArtTemplate("Cycle", "Cycle_DivergingRadial", "Diverging Radial", "Quan hệ với ý trung tâm, nhấn các vòng tròn xung quanh hơn tâm."),
                new SmartArtTemplate("Cycle", "Cycle_Gear", "Gear", "Ý tưởng ăn khớp như bánh răng, tối đa 3 mục."),
                new SmartArtTemplate("Cycle", "Cycle_Multidirectional", "Multidirectional Cycle", "Chuỗi giai đoạn có thể diễn ra theo nhiều hướng."),
                new SmartArtTemplate("Cycle", "Cycle_Nondirectional", "Nondirectional Cycle", "Chu trình mà mỗi bước có tầm quan trọng như nhau, không cần chỉ hướng."),
                new SmartArtTemplate("Cycle", "Cycle_RadialCluster", "Radial Cluster", "Dữ liệu liên quan tới một chủ đề trung tâm, tối đa 7 nhánh."),
                new SmartArtTemplate("Cycle", "Cycle_Radial", "Radial Cycle", "Quan hệ với ý trung tâm, nhấn cả tâm lẫn vòng ngoài."),
                new SmartArtTemplate("Cycle", "Cycle_RadialVenn", "Radial Venn", "Vừa giao thoa vừa có quan hệ với ý trung tâm."),
                new SmartArtTemplate("Cycle", "Cycle_Segmented", "Segmented Cycle", "Giai đoạn nối tiếp dạng lát bánh, tối đa 7 mục."),
                new SmartArtTemplate("Cycle", "Cycle_Text", "Text Cycle", "Chu trình cơ bản, nhấn mũi tên/luồng hơn là từng bước."),

                // ==========================================
                // 4. HIERARCHY — 15 layout
                // ==========================================
                new SmartArtTemplate("Hierarchy", "Hierarchy_Tree", "Organization Chart", "Sơ đồ tổ chức chuẩn — layout org chart phổ biến nhất. Dùng để thể hiện mối quan hệ phân cấp giữa lãnh đạo và các phòng ban nhân sự."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_PictureOrgChart", "Picture Organization Chart", "Sơ đồ tổ chức kèm ảnh từng người. Thích hợp để giới thiệu cơ cấu lãnh đạo doanh nghiệp cùng hình ảnh chân dung trực quan."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_NameTitleOrg", "Name and Title Organization Chart", "Sơ đồ tổ chức có ô tên + chức danh riêng. Phân tách rõ ràng giữa họ tên nhân sự và vị trí công tác cụ thể."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_HalfCircleOrg", "Half Circle Organization Chart", "Sơ đồ tổ chức dạng nửa vòng tròn, hỗ trợ shape trợ lý và phân bố nhân viên theo đường bán nguyệt hài hòa."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_CirclePicture", "Circle Picture Hierarchy", "Sơ đồ tổ chức có ảnh trong hình tròn. Kết hợp ảnh đại diện hình tròn tinh tế với các đường nhánh phân cấp tổ chức."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_Architecture", "Architecture Layout", "Quan hệ phân cấp xây từ dưới lên, hợp mô tả kiến trúc hệ thống, hạ tầng công nghệ hoặc các thành phần nền tảng."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_Labeled", "Labeled Hierarchy", "Use to show hierarchical relationships progressing from top to bottom and grouped hierarchically. Emphasizes heading or level 1 text. The first line of Level 1 text appears in the shape at the beginning of the hierarchy, and all subsequent lines of Level 1 text appear to the left of the rectangles."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_Table", "Table Hierarchy", "Nhóm thông tin từ trên xuống dạng bảng phân tầng, các khối xếp sát nhau không sử dụng đường nối phân nhánh."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_Horizontal", "Horizontal Hierarchy", "Phân cấp theo chiều ngang từ trái sang phải, thích hợp cho cây quyết định, luồng phân loại hoặc gia phả nằm ngang."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_HorizontalOrg", "Horizontal Organization Chart", "Sơ đồ tổ chức ngang, hỗ trợ vị trí trợ lý và hiển thị thông tin nhân sự trải đều theo không gian ngang."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_HorizontalLabeled", "Horizontal Labeled Hierarchy", "Phân cấp ngang có nhãn nhóm, nhấn mạnh các đề mục lớn cấp 1 ở bên trái các khối nội dung chi tiết."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_HorizontalMultiLevel", "Horizontal Multi-Level Hierarchy", "Phân cấp ngang nhiều tầng phức tạp, hiển thị đầy đủ các nhánh con và mắt xích liên kết đa cấp độ."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_List", "Hierarchy List", "Quan hệ phân cấp theo từng nhóm danh mục, vừa dùng để biểu diễn cấu trúc tổ chức vừa dùng để liệt kê danh mục nhiều cấp."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_LinedList", "Lined List", "Danh mục nhiều cấp ngăn cách bằng đường kẻ phân đoạn thanh mảnh, tạo phong cách tối giản và hiện đại."),
                new SmartArtTemplate("Hierarchy", "Hierarchy_Basic", "Hierarchy", "Quan hệ phân cấp từ trên xuống — layout sơ đồ cây cơ bản nhất, hiển thị rõ ràng quan hệ trực thuộc giữa các cấp độ."),

                // ==========================================
                // 5. RELATIONSHIP — 34 layout
                // ==========================================
                new SmartArtTemplate("Relationship", "Rel_Balance", "Balance", "So sánh quan hệ cân bằng giữa 2 ý (dạng cái cân)."),
                new SmartArtTemplate("Relationship", "Rel_RadialCluster", "Radial Cluster", "Dữ liệu liên quan tới một chủ đề trung tâm, tối đa 7 nhánh."),
                new SmartArtTemplate("Relationship", "Rel_Funnel", "Funnel", "Lọc thông tin, các phần hợp lại thành kết quả cuối (phễu)."),
                new SmartArtTemplate("Relationship", "Rel_Gear", "Gear", "Ý tưởng ăn khớp như bánh răng, tối đa 3 mục."),
                new SmartArtTemplate("Relationship", "Rel_HexagonCluster", "Hexagon Cluster", "Ảnh + mô tả, hình lục giác nhỏ đánh dấu từng cặp."),
                new SmartArtTemplate("Relationship", "Rel_CounterbalanceArrows", "Counterbalance Arrows", "Hai ý đối lập dạng mũi tên cân bằng."),
                new SmartArtTemplate("Relationship", "Rel_OpposingIdeas", "Opposing Ideas", "Hai ý đối lập/tương phản, mỗi ý có nhiều ý phụ."),
                new SmartArtTemplate("Relationship", "Rel_ArrowRibbon", "Arrow Ribbon", "Hai ý liên quan hoặc đối lập có kết nối (VD: 2 lực đối nghịch)."),
                new SmartArtTemplate("Relationship", "Rel_BasicPie", "Basic Pie", "Các phần hợp thành tổng thể."),
                new SmartArtTemplate("Relationship", "Rel_BasicRadial", "Basic Radial", "Quan hệ với ý trung tâm."),
                new SmartArtTemplate("Relationship", "Rel_BasicTarget", "Basic Target", "Quan hệ chứa đựng/thứ bậc, dạng vòng tròn đồng tâm."),
                new SmartArtTemplate("Relationship", "Rel_BasicVenn", "Basic Venn", "Quan hệ giao thoa/liên kết dạng Venn."),
                new SmartArtTemplate("Relationship", "Rel_CircleRelationship", "Circle Relationship", "Quan hệ hướng tới/từ một ý trung tâm, tối đa 5 mục."),
                new SmartArtTemplate("Relationship", "Rel_ContinuousPictureList", "Continuous Picture List", "Nhóm thông tin liên kết chứa ảnh."),
                new SmartArtTemplate("Relationship", "Rel_ConvergingArrows", "Converging Arrows", "Các ý hội tụ về một điểm."),
                new SmartArtTemplate("Relationship", "Rel_ConvergingRadial", "Converging Radial", "Quan hệ của các thành phần với ý trung tâm dạng chu kỳ."),
                new SmartArtTemplate("Relationship", "Rel_CycleMatrix", "Cycle Matrix", "Quan hệ với ý trung tâm theo chu kỳ."),
                new SmartArtTemplate("Relationship", "Rel_DivergingArrows", "Diverging Arrows", "Ý lan toả từ nguồn trung tâm."),
                new SmartArtTemplate("Relationship", "Rel_DivergingRadial", "Diverging Radial", "Quan hệ với ý trung tâm, nhấn vòng ngoài."),
                new SmartArtTemplate("Relationship", "Rel_Equation", "Equation", "Các bước dẫn tới kết quả dạng phép toán."),
                new SmartArtTemplate("Relationship", "Rel_GroupedList", "Grouped List", "Nhóm/nhóm con thông tin."),
                new SmartArtTemplate("Relationship", "Rel_HierarchyList", "Hierarchy List", "Quan hệ phân cấp theo nhóm."),
                new SmartArtTemplate("Relationship", "Rel_LinearVenn", "Linear Venn", "Quan hệ giao thoa theo trình tự thẳng hàng."),
                new SmartArtTemplate("Relationship", "Rel_NestedTarget", "Nested Target", "Quan hệ chứa đựng lồng nhau, tối đa 3 mục."),
                new SmartArtTemplate("Relationship", "Rel_NondirectionalCycle", "Nondirectional Cycle", "Chu trình không cần chỉ hướng, các bước ngang tầm."),
                new SmartArtTemplate("Relationship", "Rel_OpposingArrows", "Opposing Arrows", "Hai ý đối lập."),
                new SmartArtTemplate("Relationship", "Rel_PictureAccentList", "Picture Accent List", "Thông tin nhóm có ảnh nhỏ đi kèm."),
                new SmartArtTemplate("Relationship", "Rel_RadialCycle", "Radial Cycle", "Quan hệ với ý trung tâm và vòng ngoài."),
                new SmartArtTemplate("Relationship", "Rel_RadialList", "Radial List", "Quan hệ với ý trung tâm dạng chu kỳ, tâm có thể chèn ảnh."),
                new SmartArtTemplate("Relationship", "Rel_RadialVenn", "Radial Venn", "Vừa giao thoa vừa có ý trung tâm."),
                new SmartArtTemplate("Relationship", "Rel_SegmentedPyramid", "Segmented Pyramid", "Quan hệ chứa đựng/tỉ lệ dạng tam giác, tối đa 9 mục."),
                new SmartArtTemplate("Relationship", "Rel_StackedVenn", "Stacked Venn", "Quan hệ giao thoa nhấn sự tăng trưởng/phân bậc."),
                new SmartArtTemplate("Relationship", "Rel_TableHierarchy", "Table Hierarchy", "Nhóm thông tin từ trên xuống."),
                new SmartArtTemplate("Relationship", "Rel_TargetList", "Target List", "Thông tin liên quan/chồng lấn."),

                // ==========================================
                // 6. MATRIX — 4 layout
                // ==========================================
                new SmartArtTemplate("Matrix", "Matrix_Basic", "Basic Matrix", "Quan hệ 4 thành phần với tổng thể, dạng 4 góc phần tư đối xứng."),
                new SmartArtTemplate("Matrix", "Matrix_Cycle", "Cycle Matrix", "Quan hệ với ý trung tâm theo chu kỳ, tối đa 4 lát bánh."),
                new SmartArtTemplate("Matrix", "Matrix_Grid", "Grid Matrix", "Vị trí các khái niệm theo 2 trục tọa độ, nhấn từng thành phần."),
                new SmartArtTemplate("Matrix", "Matrix_Titled", "Titled Matrix", "Quan hệ 4 góc phần tư với một ý trung tâm chung ở chính giữa."),

                // ==========================================
                // 7. PYRAMID — 4 layout
                // ==========================================
                new SmartArtTemplate("Pyramid", "Pyramid_Basic", "Basic Pyramid", "Quan hệ tỉ lệ/thứ bậc, to nhất ở đáy, thu nhỏ dần lên đỉnh."),
                new SmartArtTemplate("Pyramid", "Pyramid_Inverted", "Inverted Pyramid", "Ngược lại Basic Pyramid: to nhất ở đỉnh, thu nhỏ dần xuống đáy nhọn."),
                new SmartArtTemplate("Pyramid", "Pyramid_List", "Pyramid List", "Quan hệ tỉ lệ/thứ bậc, chữ nằm trong ô chữ nhật trên nền kim tự tháp."),
                new SmartArtTemplate("Pyramid", "Pyramid_Segmented", "Segmented Pyramid", "Quan hệ chứa đựng/tỉ lệ, tối đa 9 mục dạng tam giác phân đoạn."),

                // ==========================================
                // 8. PICTURE — 31 layout
                // ==========================================
                new SmartArtTemplate("Picture", "Picture_Accented", "Accented Picture", "Một ảnh trung tâm kèm các ý liên quan xung quanh."),
                new SmartArtTemplate("Picture", "Picture_AlternatingBlocks", "Alternating Picture Blocks", "Chuỗi ảnh từ trên xuống, chữ xen kẽ trái/phải."),
                new SmartArtTemplate("Picture", "Picture_AlternatingCircles", "Alternating Picture Circles", "Chuỗi ảnh + chữ trong vòng tròn, ảnh xen kẽ trái/phải."),
                new SmartArtTemplate("Picture", "Picture_AscendingAccentProcess", "Ascending Picture Accent Process", "Chuỗi ảnh tăng dần kèm mô tả ngắn."),
                new SmartArtTemplate("Picture", "Picture_BendingAccentList", "Bending Picture Accent List", "Danh sách kèm ảnh tròn nhỏ."),
                new SmartArtTemplate("Picture", "Picture_BendingBlocks", "Bending Picture Blocks", "Chuỗi ảnh, hộp chữ nhỏ ở góc dưới."),
                new SmartArtTemplate("Picture", "Picture_BendingCaption", "Bending Picture Caption", "Chuỗi ảnh tuần tự, chú thích ở góc dưới."),
                new SmartArtTemplate("Picture", "Picture_BendingCaptionList", "Bending Picture Caption List", "Chuỗi ảnh, tiêu đề + mô tả trong khung chú thích dưới ảnh."),
                new SmartArtTemplate("Picture", "Picture_BendingSemiTransparent", "Bending Picture Semi-Transparent Text", "Chuỗi ảnh có lớp chữ mờ phủ phần dưới ảnh."),
                new SmartArtTemplate("Picture", "Picture_BubbleList", "Bubble Picture List", "Chuỗi ảnh dạng bong bóng, tối đa 8 ảnh."),
                new SmartArtTemplate("Picture", "Picture_Captioned", "Captioned Pictures", "Ảnh kèm nhiều cấp chữ mô tả."),
                new SmartArtTemplate("Picture", "Picture_CircleHierarchy", "Circle Picture Hierarchy", "Sơ đồ tổ chức/phân cấp có ảnh trong vòng tròn."),
                new SmartArtTemplate("Picture", "Picture_CircularCallout", "Circular Picture Callout", "Ý trung tâm + ý phụ, chữ phủ lên phần dưới ảnh chính."),
                new SmartArtTemplate("Picture", "Picture_ContinuousList", "Continuous Picture List", "Nhóm ảnh liên kết dạng tròn."),
                new SmartArtTemplate("Picture", "Picture_FramedText", "Framed Text Picture", "Ảnh kèm chữ cấp 1 trong khung."),
                new SmartArtTemplate("Picture", "Picture_HexagonCluster", "Hexagon Cluster", "Ảnh + mô tả, hình lục giác nhỏ đánh dấu từng cặp."),
                new SmartArtTemplate("Picture", "Picture_HorizontalList", "Horizontal Picture List", "Danh sách ngang nhấn ảnh."),
                new SmartArtTemplate("Picture", "Picture_AccentBlocks", "Picture Accent Blocks", "Nhóm ảnh dạng khối từ góc, chữ hiển thị dọc."),
                new SmartArtTemplate("Picture", "Picture_AccentList", "Picture Accent List", "Nhóm thông tin có ảnh nhỏ góc trên."),
                new SmartArtTemplate("Picture", "Picture_AccentProcess", "Picture Accent Process", "Bước quy trình có nền để chèn ảnh."),
                new SmartArtTemplate("Picture", "Picture_CaptionList", "Picture Caption List", "Khối ảnh kèm chú thích ngắn."),
                new SmartArtTemplate("Picture", "Picture_Grid", "Picture Grid", "Ảnh xếp theo lưới vuông."),
                new SmartArtTemplate("Picture", "Picture_Lineup", "Picture Lineup", "Chuỗi ảnh cạnh nhau, chữ trên và dưới ảnh."),
                new SmartArtTemplate("Picture", "Picture_Strips", "Picture Strips", "Chuỗi ảnh từ trên xuống, chữ cấp 1 bên cạnh."),
                new SmartArtTemplate("Picture", "Picture_SnapshotList", "Snapshot Picture List", "Ảnh kèm chữ giải thích, hợp danh sách dài ở cấp 2."),
                new SmartArtTemplate("Picture", "Picture_Spiral", "Spiral Picture", "Tối đa 5 ảnh xoắn ốc vào tâm, kèm chú thích."),
                new SmartArtTemplate("Picture", "Picture_TitledAccentList", "Titled Picture Accent List", "Danh sách có ảnh nhấn cho từng mục cấp 2, tiêu đề riêng ở trên."),
                new SmartArtTemplate("Picture", "Picture_TitleLineup", "Title Picture Lineup", "Chuỗi ảnh, mỗi ảnh có tiêu đề + mô tả riêng."),
                new SmartArtTemplate("Picture", "Picture_TitledBlocks", "Titled Picture Blocks", "Chuỗi ảnh, tiêu đề trên ảnh, mô tả cạnh ảnh."),
                new SmartArtTemplate("Picture", "Picture_VerticalAccentList", "Vertical Picture Accent List", "Danh sách dọc kèm vòng tròn nhỏ chứa ảnh."),
                new SmartArtTemplate("Picture", "Picture_VerticalList", "Vertical Picture List", "Danh sách dọc, ô nhỏ bên trái chứa ảnh."),

                // ==========================================
                // 9. OFFICE.COM — 10 layout
                // ==========================================
                new SmartArtTemplate("Office.com", "Office_CircleProcess", "Circle Process", "Bước tuần tự, tối đa 11 mục cấp 1."),
                new SmartArtTemplate("Office.com", "Office_ConvergingText", "Converging Text", "Nhiều bước hợp thành một, tối đa 5 nhánh."),
                new SmartArtTemplate("Office.com", "Office_HexagonRadial", "Hexagon Radial", "Quy trình tuần tự quanh một ý trung tâm, tối đa 6 nhánh."),
                new SmartArtTemplate("Office.com", "Office_InterconnectedBlockProcess", "Interconnected Block Process", "Bước liên kết nhau dạng khối."),
                new SmartArtTemplate("Office.com", "Office_PictureFrame", "Picture Frame", "Ảnh + chữ cấp 1 trong khung lệch."),
                new SmartArtTemplate("Office.com", "Office_RadialPictureList", "Radial Picture List", "Ý trung tâm là chữ, các nhánh quanh là ảnh, tối đa 4 ảnh."),
                new SmartArtTemplate("Office.com", "Office_TabList", "Tab List", "Danh sách dạng tab."),
                new SmartArtTemplate("Office.com", "Office_ThemePictureAccent", "Theme Picture Accent", "Nhóm ảnh, ảnh đầu tiên to nhất ở giữa, tối đa 6 ảnh."),
                new SmartArtTemplate("Office.com", "Office_ThemeAlternatingAccent", "Theme Picture Alternating Accent", "Nhóm ảnh xen kẽ vuông/chữ nhật, tối đa 9 ảnh."),
                new SmartArtTemplate("Office.com", "Office_ThemePictureGrid", "Theme Picture Grid", "Nhóm ảnh, ảnh đầu tiên to nhất, tối đa 5 ảnh.")
            };
        }

        
        private Dictionary<string, Action<Graphics, Rectangle>> previewDispatch;

        private void InitPreviewDispatch()
        {
            previewDispatch = new Dictionary<string, Action<Graphics, Rectangle>>();
            previewDispatch["List_AlternatingHexagon"] = delegate(Graphics _g, Rectangle _r) { DrawHexagonPreview(_g, _r, "List_AlternatingHexagon"); };
            previewDispatch["List_BasicBlock"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_BasicBlock"); };
            previewDispatch["List_BendingPictureAccent"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_BendingPictureAccent"); };
            previewDispatch["List_ContinuousPicture"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_ContinuousPicture"); };
            previewDispatch["List_DescendingBlock"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "List_DescendingBlock"); };
            previewDispatch["List_DetailedProcess"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "List_DetailedProcess"); };
            previewDispatch["List_Grouped"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Grouped(_g, _r); };
            previewDispatch["List_Hierarchy"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "List_Hierarchy"); };
            previewDispatch["List_HorizontalBullet"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_HorizontalBullet"); };
            previewDispatch["List_HorizontalPicture"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_HorizontalPicture"); };
            previewDispatch["List_IncreasingCircle"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_IncreasingCircles(_g, _r); };
            previewDispatch["List_Lined"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Lined(_g, _r); };
            previewDispatch["List_PictureAccent"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_PictureAccent"); };
            previewDispatch["List_PictureCaption"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_PictureCaption"); };
            previewDispatch["List_PieProcess"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Pie(_g, _r); };
            previewDispatch["List_PlusMinus"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_PlusMinus(_g, _r); };
            previewDispatch["List_Pyramid"] = delegate(Graphics _g, Rectangle _r) { DrawPyramidPreview(_g, _r, "List_Pyramid"); };
            previewDispatch["List_Reverse"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Reverse(_g, _r); };
            previewDispatch["List_SegmentedProcess"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "List_SegmentedProcess"); };
            previewDispatch["List_Stacked"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Stacked(_g, _r); };
            previewDispatch["List_SquareAccent"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_SquareAccent(_g, _r); };
            previewDispatch["List_Tab"] = delegate(Graphics _g, Rectangle _r) { DrawTabPreview(_g, _r); };
            previewDispatch["List_TableHierarchy"] = delegate(Graphics _g, Rectangle _r) { DrawTabPreview(_g, _r); };
            previewDispatch["List_Table"] = delegate(Graphics _g, Rectangle _r) { DrawTabPreview(_g, _r); };
            previewDispatch["List_Target"] = delegate(Graphics _g, Rectangle _r) { DrawTargetPreview(_g, _r); };
            previewDispatch["List_Trapezoid"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_Trapezoid"); };
            previewDispatch["List_VaryingWidth"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_VaryingWidth(_g, _r); };
            previewDispatch["List_VerticalBox"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_VerticalBox"); };
            previewDispatch["List_VerticalBlock"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_VerticalBlock"); };
            previewDispatch["List_VerticalBracket"] = delegate(Graphics _g, Rectangle _r) { DrawBracketPreview(_g, _r); };
            previewDispatch["List_VerticalBullet"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_VerticalBullet"); };
            previewDispatch["List_VerticalChevron"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "List_VerticalChevron"); };
            previewDispatch["List_VerticalPictureAccent"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_VerticalPictureAccent"); };
            previewDispatch["List_VerticalPicture"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "List_VerticalPicture"); };
            previewDispatch["List_VerticalAccent"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "List_VerticalAccent"); };
            previewDispatch["List_VerticalArrow"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_ContinuousArrow(_g, _r); };
            previewDispatch["List_VerticalCircle"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_IncreasingCircles(_g, _r); };
            previewDispatch["List_VerticalCurved"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "List_VerticalCurved"); };
            previewDispatch["Process_Accent"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_Accent"); };
            previewDispatch["Process_AlternatingFlow"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_AlternatingFlow"); };
            previewDispatch["Process_ArrowRibbon"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Process_ArrowRibbon"); };
            previewDispatch["Process_AscendingPictureAccent"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_AscendingPictureAccent"); };
            previewDispatch["Process_BasicBending"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_BasicBending"); };
            previewDispatch["Process_BasicChevron"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "Process_BasicChevron"); };
            previewDispatch["Process_Basic"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_Basic"); };
            previewDispatch["Process_BasicTimeline"] = delegate(Graphics _g, Rectangle _r) { DrawTimelinePreview(_g, _r, "Process_BasicTimeline"); };
            previewDispatch["Process_ChevronAccent"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "Process_ChevronAccent"); };
            previewDispatch["Process_ChevronList"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "Process_ChevronList"); };
            previewDispatch["Process_CircularBending"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_CircularBending"); };
            previewDispatch["Process_ClosedChevron"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "Process_ClosedChevron"); };
            previewDispatch["Process_CircleAccentTimeline"] = delegate(Graphics _g, Rectangle _r) { DrawTimelinePreview(_g, _r, "Process_CircleAccentTimeline"); };
            previewDispatch["Process_CircleArrow"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_CircleArrow(_g, _r); };
            previewDispatch["Process_ContinuousArrow"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_ContinuousArrow(_g, _r); };
            previewDispatch["Process_ContinuousBlock"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_ContinuousBlock"); };
            previewDispatch["Process_ContinuousPictureList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Process_ContinuousPictureList"); };
            previewDispatch["Process_ConvergingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Process_ConvergingArrows"); };
            previewDispatch["Process_ConvergingText"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Process_ConvergingText"); };
            previewDispatch["Process_Descending"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_Descending"); };
            previewDispatch["Process_Detailed"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_Detailed"); };
            previewDispatch["Process_DivergingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Process_DivergingArrows"); };
            previewDispatch["Process_Equation"] = delegate(Graphics _g, Rectangle _r) { DrawEquationPreview(_g, _r, "Process_Equation"); };
            previewDispatch["Process_Funnel"] = delegate(Graphics _g, Rectangle _r) { DrawFunnelPreview(_g, _r); };
            previewDispatch["Process_Gear"] = delegate(Graphics _g, Rectangle _r) { DrawGearPreview(_g, _r); };
            previewDispatch["Process_IncreasingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_IncreasingArrows"); };
            previewDispatch["Process_InterconnectedBlock"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_InterconnectedBlock"); };
            previewDispatch["Process_OpposingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Process_OpposingArrows"); };
            previewDispatch["Process_Phased"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Phased(_g, _r); };
            previewDispatch["Process_PictureAccent"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Process_PictureAccent"); };
            previewDispatch["Process_PieProcess"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Pie(_g, _r); };
            previewDispatch["Process_ProcessArrows"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_ProcessArrows"); };
            previewDispatch["Process_ProcessList"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_ProcessList"); };
            previewDispatch["Process_RandomToResult"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_RandomToResult"); };
            previewDispatch["Process_RepeatingBending"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_RepeatingBending"); };
            previewDispatch["Process_Segmented"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_Segmented"); };
            previewDispatch["Process_Staggered"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_Staggered"); };
            previewDispatch["Process_StepDown"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_StepDown"); };
            previewDispatch["Process_StepUp"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_StepUp"); };
            previewDispatch["Process_SubStep"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_SubStep"); };
            previewDispatch["Process_TimelinePipe"] = delegate(Graphics _g, Rectangle _r) { DrawTimelinePreview(_g, _r, "Process_TimelinePipe"); };
            previewDispatch["Process_UpwardArrow"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Process_UpwardArrow"); };
            previewDispatch["Process_VerticalArrowList"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_VerticalProcess(_g, _r); };
            previewDispatch["Process_VerticalBending"] = delegate(Graphics _g, Rectangle _r) { DrawProcessPreview(_g, _r, "Process_VerticalBending"); };
            previewDispatch["Process_VerticalChevronList"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "Process_VerticalChevronList"); };
            previewDispatch["Process_VerticalEquation"] = delegate(Graphics _g, Rectangle _r) { DrawEquationPreview(_g, _r, "Process_VerticalEquation"); };
            previewDispatch["Process_Vertical"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_VerticalProcess(_g, _r); };
            previewDispatch["Cycle_Basic"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Basic"); };
            previewDispatch["Cycle_BasicPie"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_BasicPie"); };
            previewDispatch["Cycle_BasicRadial"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_BasicRadial"); };
            previewDispatch["Cycle_Block"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Block"); };
            previewDispatch["Cycle_CircleArrowProcess"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_CircleArrow(_g, _r); };
            previewDispatch["Cycle_Continuous"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Continuous"); };
            previewDispatch["Cycle_CycleMatrix"] = delegate(Graphics _g, Rectangle _r) { DrawMatrixPreview(_g, _r, "Cycle_CycleMatrix"); };
            previewDispatch["Cycle_DivergingRadial"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Cycle_DivergingRadial"); };
            previewDispatch["Cycle_Gear"] = delegate(Graphics _g, Rectangle _r) { DrawGearPreview(_g, _r); };
            previewDispatch["Cycle_Multidirectional"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Multidirectional"); };
            previewDispatch["Cycle_Nondirectional"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Nondirectional"); };
            previewDispatch["Cycle_RadialCluster"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_RadialCluster"); };
            previewDispatch["Cycle_Radial"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Radial"); };
            previewDispatch["Cycle_RadialVenn"] = delegate(Graphics _g, Rectangle _r) { DrawVennPreview(_g, _r, "Cycle_RadialVenn"); };
            previewDispatch["Cycle_Segmented"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Segmented"); };
            previewDispatch["Cycle_Text"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Cycle_Text"); };
            previewDispatch["Hierarchy_Tree"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_Tree"); };
            previewDispatch["Hierarchy_PictureOrgChart"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_PictureOrgChart"); };
            previewDispatch["Hierarchy_NameTitleOrg"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_NameTitleOrg"); };
            previewDispatch["Hierarchy_HalfCircleOrg"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_HalfCircleOrg"); };
            previewDispatch["Hierarchy_CirclePicture"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_CirclePicture"); };
            previewDispatch["Hierarchy_Architecture"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_Architecture"); };
            previewDispatch["Hierarchy_Labeled"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_Labeled"); };
            previewDispatch["Hierarchy_Table"] = delegate(Graphics _g, Rectangle _r) { DrawTabPreview(_g, _r); };
            previewDispatch["Hierarchy_Horizontal"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_Horizontal"); };
            previewDispatch["Hierarchy_HorizontalOrg"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_HorizontalOrg"); };
            previewDispatch["Hierarchy_HorizontalLabeled"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_HorizontalLabeled"); };
            previewDispatch["Hierarchy_HorizontalMultiLevel"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_HorizontalMultiLevel"); };
            previewDispatch["Hierarchy_List"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_List"); };
            previewDispatch["Hierarchy_LinedList"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_LinedList"); };
            previewDispatch["Hierarchy_Basic"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Hierarchy_Basic"); };
            previewDispatch["Rel_Balance"] = delegate(Graphics _g, Rectangle _r) { DrawBalancePreview(_g, _r); };
            previewDispatch["Rel_RadialCluster"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Rel_RadialCluster"); };
            previewDispatch["Rel_Funnel"] = delegate(Graphics _g, Rectangle _r) { DrawFunnelPreview(_g, _r); };
            previewDispatch["Rel_Gear"] = delegate(Graphics _g, Rectangle _r) { DrawGearPreview(_g, _r); };
            previewDispatch["Rel_HexagonCluster"] = delegate(Graphics _g, Rectangle _r) { DrawHexagonPreview(_g, _r, "Rel_HexagonCluster"); };
            previewDispatch["Rel_CounterbalanceArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_CounterbalanceArrows"); };
            previewDispatch["Rel_OpposingIdeas"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_OpposingIdeas"); };
            previewDispatch["Rel_ArrowRibbon"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_ArrowRibbon"); };
            previewDispatch["Rel_BasicPie"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Rel_BasicPie"); };
            previewDispatch["Rel_BasicRadial"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Rel_BasicRadial"); };
            previewDispatch["Rel_BasicTarget"] = delegate(Graphics _g, Rectangle _r) { DrawTargetPreview(_g, _r); };
            previewDispatch["Rel_BasicVenn"] = delegate(Graphics _g, Rectangle _r) { DrawVennPreview(_g, _r, "Rel_BasicVenn"); };
            previewDispatch["Rel_CircleRelationship"] = delegate(Graphics _g, Rectangle _r) { DrawListPreview(_g, _r, "Rel_CircleRelationship"); };
            previewDispatch["Rel_ContinuousPictureList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Rel_ContinuousPictureList"); };
            previewDispatch["Rel_ConvergingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_ConvergingArrows"); };
            previewDispatch["Rel_ConvergingRadial"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_ConvergingRadial"); };
            previewDispatch["Rel_CycleMatrix"] = delegate(Graphics _g, Rectangle _r) { DrawMatrixPreview(_g, _r, "Rel_CycleMatrix"); };
            previewDispatch["Rel_DivergingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_DivergingArrows"); };
            previewDispatch["Rel_DivergingRadial"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_DivergingRadial"); };
            previewDispatch["Rel_Equation"] = delegate(Graphics _g, Rectangle _r) { DrawEquationPreview(_g, _r, "Rel_Equation"); };
            previewDispatch["Rel_GroupedList"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Grouped(_g, _r); };
            previewDispatch["Rel_HierarchyList"] = delegate(Graphics _g, Rectangle _r) { DrawHierarchyPreview(_g, _r, "Rel_HierarchyList"); };
            previewDispatch["Rel_LinearVenn"] = delegate(Graphics _g, Rectangle _r) { DrawVennPreview(_g, _r, "Rel_LinearVenn"); };
            previewDispatch["Rel_NestedTarget"] = delegate(Graphics _g, Rectangle _r) { DrawTargetPreview(_g, _r); };
            previewDispatch["Rel_NondirectionalCycle"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Rel_NondirectionalCycle"); };
            previewDispatch["Rel_OpposingArrows"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Rel_OpposingArrows"); };
            previewDispatch["Rel_PictureAccentList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Rel_PictureAccentList"); };
            previewDispatch["Rel_RadialCycle"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Rel_RadialCycle"); };
            previewDispatch["Rel_RadialList"] = delegate(Graphics _g, Rectangle _r) { DrawCyclePreview(_g, _r, "Rel_RadialList"); };
            previewDispatch["Rel_RadialVenn"] = delegate(Graphics _g, Rectangle _r) { DrawVennPreview(_g, _r, "Rel_RadialVenn"); };
            previewDispatch["Rel_SegmentedPyramid"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_SegmentedPyramid(_g, _r); };
            previewDispatch["Rel_StackedVenn"] = delegate(Graphics _g, Rectangle _r) { DrawVennPreview(_g, _r, "Rel_StackedVenn"); };
            previewDispatch["Rel_TableHierarchy"] = delegate(Graphics _g, Rectangle _r) { DrawTabPreview(_g, _r); };
            previewDispatch["Rel_TargetList"] = delegate(Graphics _g, Rectangle _r) { DrawTargetPreview(_g, _r); };
            previewDispatch["Matrix_Basic"] = delegate(Graphics _g, Rectangle _r) { DrawMatrixPreview(_g, _r, "Matrix_Basic"); };
            previewDispatch["Matrix_Cycle"] = delegate(Graphics _g, Rectangle _r) { DrawMatrixPreview(_g, _r, "Matrix_Cycle"); };
            previewDispatch["Matrix_Grid"] = delegate(Graphics _g, Rectangle _r) { DrawMatrixPreview(_g, _r, "Matrix_Grid"); };
            previewDispatch["Matrix_Titled"] = delegate(Graphics _g, Rectangle _r) { DrawMatrixPreview(_g, _r, "Matrix_Titled"); };
            previewDispatch["Pyramid_Basic"] = delegate(Graphics _g, Rectangle _r) { DrawPyramidPreview(_g, _r, "Pyramid_Basic"); };
            previewDispatch["Pyramid_Inverted"] = delegate(Graphics _g, Rectangle _r) { DrawPyramidPreview(_g, _r, "Pyramid_Inverted"); };
            previewDispatch["Pyramid_List"] = delegate(Graphics _g, Rectangle _r) { DrawPyramidPreview(_g, _r, "Pyramid_List"); };
            previewDispatch["Pyramid_Segmented"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_SegmentedPyramid(_g, _r); };
            previewDispatch["Picture_Accented"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_Accented"); };
            previewDispatch["Picture_AlternatingBlocks"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_AlternatingBlocks(_g, _r); };
            previewDispatch["Picture_AlternatingCircles"] = delegate(Graphics _g, Rectangle _r) { DrawBubblePreview(_g, _r); };
            previewDispatch["Picture_AscendingAccentProcess"] = delegate(Graphics _g, Rectangle _r) { DrawStepPreview(_g, _r, "Picture_AscendingAccentProcess"); };
            previewDispatch["Picture_BendingAccentList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_BendingAccentList"); };
            previewDispatch["Picture_BendingBlocks"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_BendingBlocks"); };
            previewDispatch["Picture_BendingCaption"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_BendingCaption"); };
            previewDispatch["Picture_BendingCaptionList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_BendingCaptionList"); };
            previewDispatch["Picture_BendingSemiTransparent"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_BendingSemiTransparent"); };
            previewDispatch["Picture_BubbleList"] = delegate(Graphics _g, Rectangle _r) { DrawBubblePreview(_g, _r); };
            previewDispatch["Picture_Captioned"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_Captioned"); };
            previewDispatch["Picture_CircleHierarchy"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_CircleHierarchy"); };
            previewDispatch["Picture_CircularCallout"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_CircularCallout"); };
            previewDispatch["Picture_ContinuousList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_ContinuousList"); };
            previewDispatch["Picture_FramedText"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_FramedText"); };
            previewDispatch["Picture_HexagonCluster"] = delegate(Graphics _g, Rectangle _r) { DrawHexagonPreview(_g, _r, "Picture_HexagonCluster"); };
            previewDispatch["Picture_HorizontalList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_HorizontalList"); };
            previewDispatch["Picture_AccentBlocks"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_AccentBlocks"); };
            previewDispatch["Picture_AccentList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_AccentList"); };
            previewDispatch["Picture_AccentProcess"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_AccentProcess"); };
            previewDispatch["Picture_CaptionList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_CaptionList"); };
            previewDispatch["Picture_Grid"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_Grid"); };
            previewDispatch["Picture_Lineup"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_Lineup"); };
            previewDispatch["Picture_Strips"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_VerticalPictureStrips(_g, _r); };
            previewDispatch["Picture_SnapshotList"] = delegate(Graphics _g, Rectangle _r) { DrawSnapshotPreview(_g, _r); };
            previewDispatch["Picture_Spiral"] = delegate(Graphics _g, Rectangle _r) { DrawPreview_Spiral(_g, _r); };
            previewDispatch["Picture_TitledAccentList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_TitledAccentList"); };
            previewDispatch["Picture_TitleLineup"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_TitleLineup"); };
            previewDispatch["Picture_TitledBlocks"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_TitledBlocks"); };
            previewDispatch["Picture_VerticalAccentList"] = delegate(Graphics _g, Rectangle _r) { DrawChevronPreview(_g, _r, "Picture_VerticalAccentList"); };
            previewDispatch["Picture_VerticalList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Picture_VerticalList"); };
            previewDispatch["Office_CircleProcess"] = delegate(Graphics _g, Rectangle _r) { DrawOfficeSpecificPreview(_g, _r, "Office_CircleProcess"); };
            previewDispatch["Office_ConvergingText"] = delegate(Graphics _g, Rectangle _r) { DrawArrowsPreview(_g, _r, "Office_ConvergingText"); };
            previewDispatch["Office_HexagonRadial"] = delegate(Graphics _g, Rectangle _r) { DrawHexagonPreview(_g, _r, "Office_HexagonRadial"); };
            previewDispatch["Office_InterconnectedBlockProcess"] = delegate(Graphics _g, Rectangle _r) { DrawOfficeSpecificPreview(_g, _r, "Office_InterconnectedBlockProcess"); };
            previewDispatch["Office_PictureFrame"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Office_PictureFrame"); };
            previewDispatch["Office_RadialPictureList"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Office_RadialPictureList"); };
            previewDispatch["Office_TabList"] = delegate(Graphics _g, Rectangle _r) { DrawTabPreview(_g, _r); };
            previewDispatch["Office_ThemePictureAccent"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Office_ThemePictureAccent"); };
            previewDispatch["Office_ThemeAlternatingAccent"] = delegate(Graphics _g, Rectangle _r) { DrawOfficeSpecificPreview(_g, _r, "Office_ThemeAlternatingAccent"); };
            previewDispatch["Office_ThemePictureGrid"] = delegate(Graphics _g, Rectangle _r) { DrawPicturePreview(_g, _r, "Office_ThemePictureGrid"); };
        }

        private List<SmartArtTemplate> filteredTemplates = new List<SmartArtTemplate>();
        private int selectedTemplateIndex = 6; // Default to Labeled Hierarchy in Hierarchy group
        private int hoveredTemplateIndex = -1;
        private TextBox txtDescription;

        public SmartArtGalleryForm()
        {
            this.Text = "Choose a SmartArt Graphic";
            this.Size = new Size(780, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.HelpButton = true;
            this.BackColor = Color.FromArgb(246, 248, 250);
            this.Font = new Font("Segoe UI", 9.0f);
            this.DoubleBuffered = true;

            // 1. Bottom Action Bar
            bottomActionBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(240, 242, 245)
            };
            bottomActionBar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 224, 230), 1), 0, 0, bottomActionBar.Width, 0);
            };
            this.Controls.Add(bottomActionBar);

            Button btnOk = new Button
            {
                Text = "OK",
                Location = new Point(570, 11),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (selectedTemplateIndex >= 0 && selectedTemplateIndex < filteredTemplates.Count)
                {
                    SelectedSmartArtType = filteredTemplates[selectedTemplateIndex].TypeId;
                    SelectedSmartArtName = filteredTemplates[selectedTemplateIndex].Name;
                }
            };
            bottomActionBar.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(675, 11),
                Size = new Size(80, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomActionBar.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            // 2. Left Navigation Panel (Categories)
            leftNavPanel = new DoubleBufferedPanel
            {
                Location = new Point(16, 16),
                Size = new Size(150, 436),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            leftNavPanel.Paint += LeftNavPanel_Paint;
            leftNavPanel.MouseClick += LeftNavPanel_MouseClick;
            leftNavPanel.MouseMove += (s, e) => {
                int itemH = 34;
                int hovIdx = e.Y / itemH;
                if (hovIdx != (int)leftNavPanel.Tag)
                {
                    leftNavPanel.Tag = hovIdx;
                    leftNavPanel.Invalidate();
                }
            };
            leftNavPanel.Tag = -1;
            leftNavPanel.MouseLeave += (s, e) => {
                leftNavPanel.Tag = -1;
                leftNavPanel.Invalidate();
            };
            this.Controls.Add(leftNavPanel);

            // 3. Middle Card Grid Panel
            centerCardsPanel = new DoubleBufferedPanel
            {
                Location = new Point(176, 16),
                Size = new Size(340, 436),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            centerCardsPanel.Paint += CenterCardsPanel_Paint;
            centerCardsPanel.MouseMove += CenterCardsPanel_MouseMove;
            centerCardsPanel.MouseEnter += (s, e) => { centerCardsPanel.Focus(); };
            centerCardsPanel.MouseWheel += (s, e) => { centerCardsPanel.Invalidate(); };
            centerCardsPanel.MouseLeave += (s, e) => {
                if (hoveredTemplateIndex != -1)
                {
                    hoveredTemplateIndex = -1;
                    centerCardsPanel.Invalidate();
                }
            };
            centerCardsPanel.MouseClick += CenterCardsPanel_MouseClick;
            this.Controls.Add(centerCardsPanel);

            // 4. Right Preview Panel
            rightPreviewPanel = new DoubleBufferedPanel
            {
                Location = new Point(526, 16),
                Size = new Size(238, 436),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            rightPreviewPanel.Paint += RightPreviewPanel_Paint;
            this.Controls.Add(rightPreviewPanel);

            // 5. Scrollable Description Box in Right Panel
            txtDescription = new TextBox
            {
                Location = new Point(12, 236),
                Size = new Size(214, 188),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(80, 85, 95),
                Font = new Font("Segoe UI", 8.5f),
                ScrollBars = ScrollBars.Vertical
            };
            rightPreviewPanel.Controls.Add(txtDescription);

            FilterTemplatesByCategory();
        }

        private void FilterTemplatesByCategory()
        {
            filteredTemplates.Clear();
            string selectedCat = categories[selectedCategoryIndex];

            foreach (var t in allTemplates)
            {
                if (selectedCat == "All" || t.Category == selectedCat)
                {
                    filteredTemplates.Add(t);
                }
            }

            // Layout cards inside centerCardsPanel
            int cardW = 70;
            int cardH = 64;
            int gap = 8;
            int cols = 4;
            int leftMargin = 10;
            int topMargin = 10;

            for (int i = 0; i < filteredTemplates.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                int x = leftMargin + c * (cardW + gap);
                int y = topMargin + r * (cardH + gap);
                filteredTemplates[i].Bounds = new Rectangle(x, y, cardW, cardH);
            }

            int totalRows = (filteredTemplates.Count + cols - 1) / cols;
            int totalHeight = topMargin + totalRows * (cardH + gap) + 16;
            centerCardsPanel.AutoScrollPosition = new Point(0, 0);
            centerCardsPanel.AutoScrollMinSize = new Size(0, totalHeight);

            if (selectedTemplateIndex >= filteredTemplates.Count)
            {
                selectedTemplateIndex = 0;
            }
            if (filteredTemplates.Count > 0 && selectedCategoryIndex == 4) // Hierarchy default
            {
                int labeledIdx = filteredTemplates.FindIndex(t => t.TypeId == "Hierarchy_Labeled");
                if (labeledIdx >= 0) selectedTemplateIndex = labeledIdx;
            }

            UpdateDescriptionBox();
            centerCardsPanel.Invalidate();
            rightPreviewPanel.Invalidate();
        }

        private void UpdateDescriptionBox()
        {
            if (selectedTemplateIndex >= 0 && selectedTemplateIndex < filteredTemplates.Count)
            {
                txtDescription.Text = filteredTemplates[selectedTemplateIndex].Description;
            }
            else
            {
                txtDescription.Text = "";
            }
        }

        // =========================================================================
        // LEFT NAV PANEL PAINTING (MONOLINE VECTOR ICONS)
        // =========================================================================
        private void LeftNavPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int hoveredIndex = (leftNavPanel.Tag is int) ? (int)leftNavPanel.Tag : -1;
            int itemH = 34;

            for (int i = 0; i < categoryDisplayNames.Count; i++)
            {
                Rectangle itemRect = new Rectangle(0, i * itemH, leftNavPanel.Width, itemH);
                bool isSelected = (i == selectedCategoryIndex);
                bool isHover = (i == hoveredIndex) && !isSelected;

                if (isSelected)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(204, 232, 255)))
                    {
                        g.FillRectangle(b, itemRect);
                    }
                    using (Pen p = new Pen(Color.FromArgb(0, 120, 215), 3.0f))
                    {
                        g.DrawLine(p, 2, itemRect.Top + 2, 2, itemRect.Bottom - 2);
                    }
                }
                else if (isHover)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(240, 244, 249)))
                    {
                        g.FillRectangle(b, itemRect);
                    }
                }

                // Draw Monoline Vector Icon for Category at (10, itemRect.Top + 8)
                DrawCategoryIcon(g, 10, itemRect.Top + 8, categories[i], isSelected);

                // Draw Text
                using (Font f = new Font("Segoe UI", 9.0f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(isSelected ? Color.FromArgb(0, 51, 102) : Color.FromArgb(40, 40, 40)))
                {
                    g.DrawString(categoryDisplayNames[i], f, tb, 34, itemRect.Top + 8);
                }
            }
        }

        private void DrawCategoryIcon(Graphics g, int x, int y, string cat, bool isSelected)
        {
            Color strokeColor = isSelected ? Color.FromArgb(0, 90, 170) : Color.FromArgb(90, 90, 90);
            using (Pen pen = new Pen(strokeColor, 1.2f))
            using (Brush brush = new SolidBrush(strokeColor))
            using (Brush fillLight = new SolidBrush(Color.FromArgb(235, 238, 242)))
            {
                switch (cat)
                {
                    case "All":
                        // 3 stacked sheets
                        g.DrawRectangle(pen, x + 5, y + 1, 9, 11);
                        g.FillRectangle(Brushes.White, x + 3, y + 3, 9, 11);
                        g.DrawRectangle(pen, x + 3, y + 3, 9, 11);
                        g.FillRectangle(Brushes.White, x + 1, y + 5, 9, 11);
                        g.DrawRectangle(pen, x + 1, y + 5, 9, 11);
                        g.DrawLine(pen, x + 3, y + 8, x + 8, y + 8);
                        g.DrawLine(pen, x + 3, y + 11, x + 8, y + 11);
                        break;

                    case "List":
                        // 3 bullets with horizontal lines
                        for (int k = 0; k < 3; k++)
                        {
                            int ly = y + 2 + k * 5;
                            g.DrawRectangle(pen, x + 1, ly, 3, 3);
                            g.DrawLine(pen, x + 6, ly + 1.5f, x + 16, ly + 1.5f);
                        }
                        break;

                    case "Process":
                        // 3 consecutive horizontal chevrons
                        for (int k = 0; k < 3; k++)
                        {
                            int cx = x + 1 + k * 5;
                            PointF[] pts = new PointF[] {
                                new PointF(cx, y + 3),
                                new PointF(cx + 3, y + 3),
                                new PointF(cx + 5, y + 8),
                                new PointF(cx + 3, y + 13),
                                new PointF(cx, y + 13),
                                new PointF(cx + 2, y + 8)
                            };
                            g.DrawPolygon(pen, pts);
                        }
                        break;

                    case "Cycle":
                        // Circular arrows
                        g.DrawArc(pen, x + 1, y + 1, 14, 14, 45, 270);
                        // Arrow head
                        PointF[] head = new PointF[] {
                            new PointF(x + 13, y + 4),
                            new PointF(x + 16, y + 8),
                            new PointF(x + 11, y + 8)
                        };
                        g.FillPolygon(brush, head);
                        break;

                    case "Hierarchy":
                        // Root node with 3 children tree
                        g.DrawRectangle(pen, x + 6, y + 1, 5, 3);
                        g.DrawLine(pen, x + 8.5f, y + 4, x + 8.5f, y + 7);
                        g.DrawLine(pen, x + 3, y + 7, x + 14, y + 7);
                        g.DrawRectangle(pen, x + 1, y + 8, 4, 4);
                        g.DrawRectangle(pen, x + 6.5f, y + 8, 4, 4);
                        g.DrawRectangle(pen, x + 12, y + 8, 4, 4);
                        break;

                    case "Relationship":
                        // Box with opposing horizontal arrows
                        g.DrawRectangle(pen, x + 1, y + 2, 15, 12);
                        g.DrawLine(pen, x + 4, y + 5, x + 12, y + 5);
                        g.DrawLine(pen, x + 10, y + 3, x + 12, y + 5);
                        g.DrawLine(pen, x + 10, y + 7, x + 12, y + 5);
                        g.DrawLine(pen, x + 12, y + 11, x + 4, y + 11);
                        g.DrawLine(pen, x + 6, y + 9, x + 4, y + 11);
                        g.DrawLine(pen, x + 6, y + 13, x + 4, y + 11);
                        break;

                    case "Matrix":
                        // 4 quadrants cross
                        g.DrawRectangle(pen, x + 1, y + 1, 6, 6);
                        g.DrawRectangle(pen, x + 9, y + 1, 6, 6);
                        g.DrawRectangle(pen, x + 1, y + 9, 6, 6);
                        g.DrawRectangle(pen, x + 9, y + 9, 6, 6);
                        break;

                    case "Pyramid":
                        // Tiered triangle
                        PointF[] pyr = new PointF[] {
                            new PointF(x + 8.5f, y + 1),
                            new PointF(x + 16, y + 14),
                            new PointF(x + 1, y + 14)
                        };
                        g.DrawPolygon(pen, pyr);
                        g.DrawLine(pen, x + 6, y + 6, x + 11, y + 6);
                        g.DrawLine(pen, x + 3.5f, y + 10, x + 13.5f, y + 10);
                        break;

                    case "Picture":
                        // Picture frame with mountain & sun
                        g.DrawRectangle(pen, x + 1, y + 2, 15, 12);
                        g.DrawEllipse(pen, x + 11, y + 4, 2.5f, 2.5f);
                        PointF[] mtn = new PointF[] {
                            new PointF(x + 3, y + 12),
                            new PointF(x + 7, y + 7),
                            new PointF(x + 10, y + 10),
                            new PointF(x + 12, y + 8),
                            new PointF(x + 14, y + 12)
                        };
                        g.DrawLines(pen, mtn);
                        break;

                    case "Office.com":
                        // Globe
                        g.DrawEllipse(pen, x + 1, y + 1, 14, 14);
                        g.DrawLine(pen, x + 1, y + 8, x + 15, y + 8);
                        g.DrawEllipse(pen, x + 4.5f, y + 1, 7, 14);
                        break;
                }
            }
        }

        private void LeftNavPanel_MouseClick(object sender, MouseEventArgs e)
        {
            int itemH = 34;
            int clickedIdx = e.Y / itemH;
            if (clickedIdx >= 0 && clickedIdx < categories.Count)
            {
                selectedCategoryIndex = clickedIdx;
                leftNavPanel.Invalidate();
                FilterTemplatesByCategory();
            }
        }

        // =========================================================================
        // CENTER CARDS PANEL PAINTING (VECTOR WIREFRAMES)
        // =========================================================================
        private void CenterCardsPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.TranslateTransform(centerCardsPanel.AutoScrollPosition.X, centerCardsPanel.AutoScrollPosition.Y);

            for (int i = 0; i < filteredTemplates.Count; i++)
            {
                var tmpl = filteredTemplates[i];
                Rectangle b = tmpl.Bounds;
                bool isSel = (i == selectedTemplateIndex);
                bool isHov = (i == hoveredTemplateIndex) && !isSel;

                // Background
                Color bg = isSel ? Color.FromArgb(240, 240, 240) : (isHov ? Color.FromArgb(243, 242, 241) : Color.White);
                using (SolidBrush bb = new SolidBrush(bg))
                {
                    g.FillRectangle(bb, b);
                }

                // Border
                if (isSel)
                {
                    // MS Office double-border styling for selected item
                    using (Pen outerPen = new Pen(Color.FromArgb(138, 136, 134), 1.5f))
                    using (Pen innerPen = new Pen(Color.FromArgb(138, 136, 134), 1.0f))
                    {
                        g.DrawRectangle(outerPen, b.X, b.Y, b.Width - 1, b.Height - 1);
                        g.DrawRectangle(innerPen, b.X + 2, b.Y + 2, b.Width - 5, b.Height - 5);
                    }
                }
                else if (isHov)
                {
                    using (Pen hovPen = new Pen(Color.FromArgb(180, 180, 180), 1.0f))
                    {
                        g.DrawRectangle(hovPen, b.X, b.Y, b.Width - 1, b.Height - 1);
                    }
                }
                else
                {
                    using (Pen normPen = new Pen(Color.FromArgb(230, 230, 230), 1.0f))
                    {
                        g.DrawRectangle(normPen, b.X, b.Y, b.Width - 1, b.Height - 1);
                    }
                }

                // Architectural Wireframe Icon inside card
                Rectangle mini = new Rectangle(b.X + 6, b.Y + 6, b.Width - 12, b.Height - 12);
                DrawArchitecturalWireframe(g, mini, tmpl.TypeId);
            }
        }

        private void DrawArchitecturalWireframe(Graphics g, Rectangle r, string typeId)
        {
            Color fillNeutral = Color.FromArgb(225, 223, 221); // #E1DFDD
            Color outNeutral = Color.FromArgb(96, 94, 92);     // #605E5C
            Color divWhite = Color.FromArgb(255, 255, 255);    // #FFFFFF
            Color stemNeutral = Color.FromArgb(121, 119, 117);  // #797775

            using (Brush fillBrush = new SolidBrush(fillNeutral))
            using (Pen outPen = new Pen(outNeutral, 1.2f))
            using (Pen divPen = new Pen(divWhite, 1.5f))
            using (Pen stemPen = new Pen(stemNeutral, 1.2f))
            {
                if (typeId.Contains("Gear"))
                {
                    DrawGearWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Hexagon"))
                {
                    DrawHexagonWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Funnel"))
                {
                    DrawFunnelWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Balance"))
                {
                    DrawBalanceWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Target"))
                {
                    DrawTargetWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Venn"))
                {
                    DrawVennWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Equation"))
                {
                    DrawEquationWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Tab"))
                {
                    DrawTabWireframe(g, r, fillBrush, outPen, divPen);
                }
                else if (typeId.Contains("Bracket"))
                {
                    DrawBracketWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Snapshot"))
                {
                    DrawSnapshotWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Bubble") || typeId.Contains("CirclePicture") || typeId.Contains("AlternatingCircles"))
                {
                    DrawBubbleWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Pyramid"))
                {
                    DrawPyramidWireframe(g, r, typeId, fillBrush, outPen, divPen);
                }
                else if (typeId.Contains("Matrix"))
                {
                    DrawMatrixWireframe(g, r, typeId, fillBrush, outPen, divPen, stemPen);
                }
                else if (typeId.Contains("Timeline"))
                {
                    DrawTimelineWireframe(g, r, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Chevron"))
                {
                    DrawChevronWireframe(g, r, typeId, fillBrush, outPen);
                }
                else if (typeId.Contains("Step") || typeId.Contains("Staggered") || typeId.Contains("Descending") || typeId.Contains("Ascending"))
                {
                    DrawStepWireframe(g, r, typeId, fillBrush, outPen, stemPen);
                }
                else if (typeId.Contains("Opposing") || typeId.Contains("Converging") || typeId.Contains("Diverging") || typeId.Contains("Ribbon"))
                {
                    DrawArrowsWireframe(g, r, typeId, fillBrush, outPen, stemPen);
                }
                else if (typeId.StartsWith("Hierarchy_") || typeId.Contains("Org") || typeId.Contains("Tree") || typeId == "List_Hierarchy" || typeId == "Rel_HierarchyList")
                {
                    DrawHierarchyWireframe(g, r, typeId, fillBrush, outPen, stemPen, divPen);
                }
                else if (typeId.StartsWith("Picture_") || typeId.Contains("Picture"))
                {
                    DrawPictureWireframe(g, r, typeId, fillBrush, outPen, stemPen, divPen);
                }
                else if (typeId.StartsWith("Cycle_") || typeId.Contains("Cycle") || typeId.Contains("Radial") || typeId.Contains("Pie"))
                {
                    DrawCycleWireframe(g, r, typeId, fillBrush, outPen, stemPen);
                }
                else if (typeId.StartsWith("Process_") || typeId.Contains("Flow") || typeId.Contains("Bending"))
                {
                    DrawProcessWireframe(g, r, typeId, fillBrush, outPen, divPen, stemPen);
                }
                else
                {
                    DrawListWireframe(g, r, typeId, fillBrush, outPen, divPen, stemPen);
                }
            }
        }

        private void DrawGearWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            // 3 interlocking gears
            DrawGearShape(g, r.X + 18, r.Y + 30, 13, 9, 8, fillBrush, outPen);
            DrawGearShape(g, r.X + 36, r.Y + 17, 11, 7, 7, fillBrush, outPen);
            DrawGearShape(g, r.X + 44, r.Y + 36, 9, 6, 6, fillBrush, outPen);
        }

        private void DrawGearShape(Graphics g, float cx, float cy, float outerR, float innerR, int teeth, Brush fillBrush, Pen borderPen)
        {
            int totalPoints = teeth * 4;
            PointF[] pts = new PointF[totalPoints];
            double step = Math.PI * 2.0 / totalPoints;

            for (int i = 0; i < totalPoints; i++)
            {
                double ang = i * step;
                float r = (i % 4 == 1 || i % 4 == 2) ? outerR : innerR;
                pts[i] = new PointF((float)(cx + r * Math.Cos(ang)), (float)(cy + r * Math.Sin(ang)));
            }

            g.FillPolygon(fillBrush, pts);
            g.DrawPolygon(borderPen, pts);
            // Center axle hole
            float holeR = innerR * 0.45f;
            g.FillEllipse(Brushes.White, cx - holeR, cy - holeR, holeR * 2, holeR * 2);
            g.DrawEllipse(borderPen, cx - holeR, cy - holeR, holeR * 2, holeR * 2);
        }

        private void DrawHexagonWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            // 3 interlocking hexagons
            DrawHexagonShape(g, r.X + 16, r.Y + 20, 11, fillBrush, outPen);
            DrawHexagonShape(g, r.X + 32, r.Y + 32, 11, fillBrush, outPen);
            DrawHexagonShape(g, r.X + 46, r.Y + 19, 11, fillBrush, outPen);
        }

        private void DrawHexagonShape(Graphics g, float cx, float cy, float radius, Brush fillBrush, Pen borderPen)
        {
            PointF[] pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double ang = i * Math.PI / 3.0;
                pts[i] = new PointF((float)(cx + radius * Math.Cos(ang)), (float)(cy + radius * Math.Sin(ang)));
            }
            g.FillPolygon(fillBrush, pts);
            g.DrawPolygon(borderPen, pts);
        }

        private void DrawFunnelWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            // Top rim
            g.FillEllipse(fillBrush, r.X + 6, r.Y + 4, r.Width - 12, 10);
            g.DrawEllipse(outPen, r.X + 6, r.Y + 4, r.Width - 12, 10);

            // Conical body & nozzle
            PointF[] body = new PointF[] {
                new PointF(r.X + 6, r.Y + 9),
                new PointF(r.Right - 6, r.Y + 9),
                new PointF(r.X + r.Width / 2f + 5, r.Bottom - 12),
                new PointF(r.X + r.Width / 2f + 5, r.Bottom - 4),
                new PointF(r.X + r.Width / 2f - 5, r.Bottom - 4),
                new PointF(r.X + r.Width / 2f - 5, r.Bottom - 12)
            };
            g.FillPolygon(fillBrush, body);
            g.DrawPolygon(outPen, body);

            // 2 small circles entering top
            g.FillEllipse(Brushes.White, r.X + 18, r.Y + 2, 5, 5);
            g.DrawEllipse(outPen, r.X + 18, r.Y + 2, 5, 5);
            g.FillEllipse(Brushes.White, r.X + 32, r.Y + 3, 5, 5);
            g.DrawEllipse(outPen, r.X + 32, r.Y + 3, 5, 5);
        }

        private void DrawBalanceWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + 12;

            // Triangular fulcrum base at bottom
            PointF[] baseTri = new PointF[] {
                new PointF(cx, r.Bottom - 14),
                new PointF(cx - 10, r.Bottom - 4),
                new PointF(cx + 10, r.Bottom - 4)
            };
            g.FillPolygon(fillBrush, baseTri);
            g.DrawPolygon(outPen, baseTri);

            // Vertical pillar
            g.DrawLine(stemPen, cx, cy, cx, r.Bottom - 14);

            // Tilted balance beam
            g.DrawLine(outPen, r.X + 6, cy - 3, r.Right - 6, cy + 3);

            // Left pan (higher)
            g.DrawLine(stemPen, r.X + 10, cy - 2, r.X + 6, cy + 14);
            g.DrawLine(stemPen, r.X + 18, cy - 1, r.X + 22, cy + 14);
            g.FillRectangle(fillBrush, r.X + 5, cy + 14, 18, 4);
            g.DrawRectangle(outPen, r.X + 5, cy + 14, 18, 4);
            g.FillRectangle(Brushes.White, r.X + 10, cy + 8, 8, 6);
            g.DrawRectangle(outPen, r.X + 10, cy + 8, 8, 6);

            // Right pan (lower)
            g.DrawLine(stemPen, r.Right - 18, cy + 2, r.Right - 22, cy + 20);
            g.DrawLine(stemPen, r.Right - 10, cy + 3, r.Right - 6, cy + 20);
            g.FillRectangle(fillBrush, r.Right - 23, cy + 20, 18, 4);
            g.DrawRectangle(outPen, r.Right - 23, cy + 20, 18, 4);
            g.FillRectangle(Brushes.White, r.Right - 19, cy + 14, 10, 6);
            g.DrawRectangle(outPen, r.Right - 19, cy + 14, 10, 6);
        }

        private void DrawTargetWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int cx = r.X + r.Width / 2 - 4;
            int cy = r.Y + r.Height / 2;

            // Outer ring
            g.FillEllipse(fillBrush, cx - 20, cy - 20, 40, 40);
            g.DrawEllipse(outPen, cx - 20, cy - 20, 40, 40);

            // Middle ring
            g.FillEllipse(Brushes.White, cx - 13, cy - 13, 26, 26);
            g.DrawEllipse(outPen, cx - 13, cy - 13, 26, 26);

            // Bullseye
            g.FillEllipse(fillBrush, cx - 6, cy - 6, 12, 12);
            g.DrawEllipse(outPen, cx - 6, cy - 6, 12, 12);

            // Pointer line & label box
            g.DrawLine(stemPen, cx + 18, cy - 10, r.Right - 4, cy - 16);
            g.FillRectangle(Brushes.White, r.Right - 12, cy - 20, 10, 6);
            g.DrawRectangle(outPen, r.Right - 12, cy - 20, 10, 6);
        }

        private void DrawVennWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            // 3 overlapping circles
            g.DrawEllipse(outPen, cx - 16, cy - 12, 22, 22);
            g.DrawEllipse(outPen, cx - 6, cy - 12, 22, 22);
            g.DrawEllipse(outPen, cx - 11, cy - 2, 22, 22);
        }

        private void DrawEquationWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int bw = 13;
            int bh = 15;
            int by = r.Y + (r.Height - bh) / 2;

            // Box A
            g.FillRectangle(fillBrush, r.X + 2, by, bw, bh);
            g.DrawRectangle(outPen, r.X + 2, by, bw, bh);

            // Plus '+'
            int px = r.X + 20;
            int py = by + bh / 2;
            g.DrawLine(outPen, px - 3, py, px + 3, py);
            g.DrawLine(outPen, px, py - 3, px, py + 3);

            // Box B
            g.FillRectangle(fillBrush, r.X + 26, by, bw, bh);
            g.DrawRectangle(outPen, r.X + 26, by, bw, bh);

            // Equals '='
            int ex = r.X + 44;
            g.DrawLine(outPen, ex - 3, py - 2, ex + 3, py - 2);
            g.DrawLine(outPen, ex - 3, py + 2, ex + 3, py + 2);

            // Box C (Result)
            g.FillRectangle(fillBrush, r.Right - bw - 2, by - 2, bw + 2, bh + 4);
            g.DrawRectangle(outPen, r.Right - bw - 2, by - 2, bw + 2, bh + 4);
        }

        private void DrawTabWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen divPen)
        {
            int count = 3;
            int th = (r.Height - 12) / count;

            for (int k = 0; k < count; k++)
            {
                int ty = r.Y + 4 + k * (th + 2);
                // Protruding tab ear
                g.FillRectangle(fillBrush, r.X + 4 + k * 14, ty, 14, 4);
                g.DrawRectangle(outPen, r.X + 4 + k * 14, ty, 14, 4);
                // Body
                g.FillRectangle(fillBrush, r.X + 4, ty + 3, r.Width - 8, th - 2);
                g.DrawRectangle(outPen, r.X + 4, ty + 3, r.Width - 8, th - 2);
                g.DrawLine(divPen, r.X + 8, ty + 3 + (th - 2) / 2, r.X + 24, ty + 3 + (th - 2) / 2);
            }
        }

        private void DrawBracketWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            // Curly bracket on left
            int bx = r.X + 10;
            int topY = r.Y + 6;
            int botY = r.Bottom - 6;
            int midY = (topY + botY) / 2;

            g.DrawLine(stemPen, bx + 5, topY, bx, topY);
            g.DrawLine(stemPen, bx, topY, bx, midY - 3);
            g.DrawLine(stemPen, bx, midY - 3, bx - 4, midY);
            g.DrawLine(stemPen, bx - 4, midY, bx, midY + 3);
            g.DrawLine(stemPen, bx, midY + 3, bx, botY);
            g.DrawLine(stemPen, bx, botY, bx + 5, botY);

            // 3 horizontal blocks on right
            int rh = 9;
            for (int k = 0; k < 3; k++)
            {
                int ry = r.Y + 7 + k * 14;
                g.FillRectangle(fillBrush, bx + 8, ry, r.Right - bx - 12, rh);
                g.DrawRectangle(outPen, bx + 8, ry, r.Right - bx - 12, rh);
            }
        }

        private void DrawSnapshotWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            // 2 overlapping Polaroid frames
            // Frame 1 (behind, tilted)
            g.FillRectangle(fillBrush, r.X + 8, r.Y + 6, 26, 32);
            g.DrawRectangle(outPen, r.X + 8, r.Y + 6, 26, 32);
            g.FillRectangle(Brushes.White, r.X + 11, r.Y + 9, 20, 20);
            g.DrawRectangle(outPen, r.X + 11, r.Y + 9, 20, 20);

            // Frame 2 (in front)
            g.FillRectangle(fillBrush, r.X + 22, r.Y + 12, 28, 34);
            g.DrawRectangle(outPen, r.X + 22, r.Y + 12, 28, 34);
            g.FillRectangle(Brushes.White, r.X + 25, r.Y + 15, 22, 22);
            g.DrawRectangle(outPen, r.X + 25, r.Y + 15, 22, 22);
            // Sun & Mountain in front frame
            g.DrawEllipse(outPen, r.X + 41, r.Y + 18, 3, 3);
            g.DrawLine(outPen, r.X + 27, r.Y + 33, r.X + 34, r.Y + 25);
            g.DrawLine(outPen, r.X + 34, r.Y + 25, r.X + 44, r.Y + 33);
        }

        private void DrawBubbleWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            // 4 circular bubbles of varying sizes
            g.FillEllipse(fillBrush, r.X + 6, r.Y + 14, 18, 18);
            g.DrawEllipse(outPen, r.X + 6, r.Y + 14, 18, 18);

            g.FillEllipse(fillBrush, r.X + 26, r.Y + 6, 15, 15);
            g.DrawEllipse(outPen, r.X + 26, r.Y + 6, 15, 15);

            g.FillEllipse(fillBrush, r.X + 36, r.Y + 24, 17, 17);
            g.DrawEllipse(outPen, r.X + 36, r.Y + 24, 17, 17);

            g.FillEllipse(fillBrush, r.X + 20, r.Y + 32, 12, 12);
            g.DrawEllipse(outPen, r.X + 20, r.Y + 32, 12, 12);

            // Connecting lines
            g.DrawLine(stemPen, r.X + 22, r.Y + 20, r.X + 27, r.Y + 16);
            g.DrawLine(stemPen, r.X + 38, r.Y + 18, r.X + 42, r.Y + 25);
            g.DrawLine(stemPen, r.X + 38, r.Y + 34, r.X + 31, r.Y + 36);
        }

        private void DrawPyramidWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen divPen)
        {
            if (typeId.Contains("Inverted"))
            {
                // Inverted pyramid: apex pointing down
                PointF[] pyr = new PointF[] {
                    new PointF(r.X + 6, r.Y + 6),
                    new PointF(r.Right - 6, r.Y + 6),
                    new PointF(r.X + r.Width / 2f, r.Bottom - 6)
                };
                g.FillPolygon(fillBrush, pyr);
                g.DrawPolygon(outPen, pyr);

                int h = r.Height - 12;
                g.DrawLine(divPen, r.X + r.Width * 0.22f, r.Y + 6 + h * 0.35f, r.X + r.Width * 0.78f, r.Y + 6 + h * 0.35f);
                g.DrawLine(divPen, r.X + r.Width * 0.35f, r.Y + 6 + h * 0.68f, r.X + r.Width * 0.65f, r.Y + 6 + h * 0.68f);
            }
            else if (typeId.Contains("List"))
            {
                // Pyramid on left + 3 boxes on right
                PointF[] pyr = new PointF[] {
                    new PointF(r.X + 16, r.Y + 6),
                    new PointF(r.X + 28, r.Bottom - 6),
                    new PointF(r.X + 4, r.Bottom - 6)
                };
                g.FillPolygon(fillBrush, pyr);
                g.DrawPolygon(outPen, pyr);

                int bh = (r.Height - 14) / 3;
                for (int k = 0; k < 3; k++)
                {
                    int by = r.Y + 6 + k * (bh + 2);
                    g.FillRectangle(fillBrush, r.X + 32, by, r.Right - r.X - 36, bh);
                    g.DrawRectangle(outPen, r.X + 32, by, r.Right - r.X - 36, bh);
                }
            }
            else
            {
                // Basic upright pyramid
                PointF[] pyr = new PointF[] {
                    new PointF(r.X + r.Width / 2f, r.Y + 5),
                    new PointF(r.Right - 6, r.Bottom - 6),
                    new PointF(r.X + 6, r.Bottom - 6)
                };
                g.FillPolygon(fillBrush, pyr);
                g.DrawPolygon(outPen, pyr);

                int h = r.Height - 11;
                g.DrawLine(divPen, r.X + r.Width * 0.35f, r.Y + 5 + h * 0.35f, r.X + r.Width * 0.65f, r.Y + 5 + h * 0.35f);
                g.DrawLine(divPen, r.X + r.Width * 0.22f, r.Y + 5 + h * 0.68f, r.X + r.Width * 0.78f, r.Y + 5 + h * 0.68f);
            }
        }

        private void DrawMatrixWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen divPen, Pen stemPen)
        {
            int halfW = (r.Width - 10) / 2;
            int halfH = (r.Height - 10) / 2;

            g.FillRectangle(fillBrush, r.X + 4, r.Y + 4, halfW, halfH);
            g.DrawRectangle(outPen, r.X + 4, r.Y + 4, halfW, halfH);

            g.FillRectangle(fillBrush, r.X + 6 + halfW, r.Y + 4, halfW, halfH);
            g.DrawRectangle(outPen, r.X + 6 + halfW, r.Y + 4, halfW, halfH);

            g.FillRectangle(fillBrush, r.X + 4, r.Y + 6 + halfH, halfW, halfH);
            g.DrawRectangle(outPen, r.X + 4, r.Y + 6 + halfH, halfW, halfH);

            g.FillRectangle(fillBrush, r.X + 6 + halfW, r.Y + 6 + halfH, halfW, halfH);
            g.DrawRectangle(outPen, r.X + 6 + halfW, r.Y + 6 + halfH, halfW, halfH);

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            if (typeId.Contains("Grid"))
            {
                // Axis cross lines with arrows
                g.DrawLine(stemPen, r.X + 2, cy, r.Right - 2, cy);
                g.DrawLine(stemPen, cx, r.Y + 2, cx, r.Bottom - 2);
            }
            else if (typeId.Contains("Titled"))
            {
                // Central diamond
                PointF[] diamond = new PointF[] {
                    new PointF(cx, cy - 7),
                    new PointF(cx + 7, cy),
                    new PointF(cx, cy + 7),
                    new PointF(cx - 7, cy)
                };
                g.FillPolygon(Brushes.White, diamond);
                g.DrawPolygon(outPen, diamond);
            }
            else if (typeId.Contains("Cycle"))
            {
                // Central circular arrow
                g.DrawArc(stemPen, cx - 7, cy - 7, 14, 14, 45, 270);
            }
        }

        private void DrawTimelineWireframe(Graphics g, Rectangle r, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int midY = r.Y + r.Height / 2;
            g.DrawLine(stemPen, r.X + 4, midY, r.Right - 4, midY);

            for (int k = 0; k < 3; k++)
            {
                int mx = r.X + 11 + k * 18;
                g.FillEllipse(fillBrush, mx - 4, midY - 4, 8, 8);
                g.DrawEllipse(outPen, mx - 4, midY - 4, 8, 8);

                bool isUp = (k % 2 == 0);
                int flagY = isUp ? midY - 18 : midY + 8;
                g.DrawLine(stemPen, mx, midY, mx, isUp ? midY - 6 : midY + 6);
                g.FillRectangle(fillBrush, mx - 6, flagY, 12, 6);
                g.DrawRectangle(outPen, mx - 6, flagY, 12, 6);
            }
        }

        private void DrawChevronWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen)
        {
            if (typeId.Contains("Vertical"))
            {
                int arrH = (r.Height - 8) / 3;
                for (int i = 0; i < 3; i++)
                {
                    int ay = r.Y + 4 + i * (arrH + 1);
                    PointF[] pts = new PointF[] {
                        new PointF(r.X + 6, ay),
                        new PointF(r.Right - 6, ay),
                        new PointF(r.Right - 6, ay + arrH - 4),
                        new PointF(r.X + r.Width / 2f, ay + arrH),
                        new PointF(r.X + 6, ay + arrH - 4),
                        new PointF(r.X + r.Width / 2f, ay)
                    };
                    g.FillPolygon(fillBrush, pts);
                    g.DrawPolygon(outPen, pts);
                }
            }
            else
            {
                int arrW = (r.Width - 6) / 3;
                for (int i = 0; i < 3; i++)
                {
                    int ax = r.X + 3 + i * arrW;
                    PointF[] pts = new PointF[] {
                        new PointF(ax, r.Y + 8),
                        new PointF(ax + arrW - 3, r.Y + 8),
                        new PointF(ax + arrW, r.Y + r.Height / 2f),
                        new PointF(ax + arrW - 3, r.Bottom - 8),
                        new PointF(ax, r.Bottom - 8),
                        new PointF(ax + 3, r.Y + r.Height / 2f)
                    };
                    g.FillPolygon(fillBrush, pts);
                    g.DrawPolygon(outPen, pts);
                }
            }
        }

        private void DrawStepWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int count = 3;
            int sw = (r.Width - 10) / count;
            bool isDown = typeId.Contains("StepDown") || typeId.Contains("Descending");

            for (int k = 0; k < count; k++)
            {
                int sx = r.X + 5 + k * sw;
                int sh = 10 + (isDown ? (count - 1 - k) : k) * 11;
                int sy = r.Bottom - 6 - sh;

                g.FillRectangle(fillBrush, sx, sy, sw - 2, sh);
                g.DrawRectangle(outPen, sx, sy, sw - 2, sh);
            }

            // Trend arrow line
            if (isDown)
            {
                g.DrawLine(stemPen, r.X + 6, r.Y + 12, r.Right - 8, r.Bottom - 10);
            }
            else
            {
                g.DrawLine(stemPen, r.X + 6, r.Bottom - 10, r.Right - 8, r.Y + 12);
            }
        }

        private void DrawArrowsWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            if (typeId.Contains("Opposing") || typeId.Contains("Ribbon"))
            {
                // 2 opposing arrows: top right, bottom left
                int ah = 14;
                // Top arrow right
                PointF[] topArrow = new PointF[] {
                    new PointF(r.X + 4, r.Y + 9),
                    new PointF(r.Right - 12, r.Y + 9),
                    new PointF(r.Right - 12, r.Y + 5),
                    new PointF(r.Right - 4, r.Y + 9 + ah / 2f),
                    new PointF(r.Right - 12, r.Y + 13 + ah),
                    new PointF(r.Right - 12, r.Y + 9 + ah),
                    new PointF(r.X + 4, r.Y + 9 + ah)
                };
                g.FillPolygon(fillBrush, topArrow);
                g.DrawPolygon(outPen, topArrow);

                // Bottom arrow left
                PointF[] botArrow = new PointF[] {
                    new PointF(r.Right - 4, r.Bottom - 9 - ah),
                    new PointF(r.X + 12, r.Bottom - 9 - ah),
                    new PointF(r.X + 12, r.Bottom - 13 - ah),
                    new PointF(r.X + 4, r.Bottom - 9 - ah / 2f),
                    new PointF(r.X + 12, r.Bottom - 5),
                    new PointF(r.X + 12, r.Bottom - 9),
                    new PointF(r.Right - 4, r.Bottom - 9)
                };
                g.FillPolygon(fillBrush, botArrow);
                g.DrawPolygon(outPen, botArrow);
            }
            else if (typeId.Contains("Converging"))
            {
                // 3 arrows pointing to center
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;
                g.FillEllipse(fillBrush, cx - 7, cy - 7, 14, 14);
                g.DrawEllipse(outPen, cx - 7, cy - 7, 14, 14);

                // Inward arrows
                g.DrawLine(outPen, r.X + 6, cy, cx - 8, cy);
                g.DrawLine(outPen, cx - 11, cy - 3, cx - 8, cy);
                g.DrawLine(outPen, cx - 11, cy + 3, cx - 8, cy);

                g.DrawLine(outPen, r.Right - 6, cy, cx + 8, cy);
                g.DrawLine(outPen, cx + 11, cy - 3, cx + 8, cy);
                g.DrawLine(outPen, cx + 11, cy + 3, cx + 8, cy);

                g.DrawLine(outPen, cx, r.Y + 6, cx, cy - 8);
                g.DrawLine(outPen, cx - 3, cy - 11, cx, cy - 8);
                g.DrawLine(outPen, cx + 3, cy - 11, cx, cy - 8);
            }
            else
            {
                // Diverging: center circle with arrows pointing out
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;
                g.FillEllipse(fillBrush, cx - 7, cy - 7, 14, 14);
                g.DrawEllipse(outPen, cx - 7, cy - 7, 14, 14);

                g.DrawLine(outPen, cx - 8, cy, r.X + 6, cy);
                g.DrawLine(outPen, r.X + 9, cy - 3, r.X + 6, cy);
                g.DrawLine(outPen, r.X + 9, cy + 3, r.X + 6, cy);

                g.DrawLine(outPen, cx + 8, cy, r.Right - 6, cy);
                g.DrawLine(outPen, r.Right - 9, cy - 3, r.Right - 6, cy);
                g.DrawLine(outPen, r.Right - 9, cy + 3, r.Right - 6, cy);

                g.DrawLine(outPen, cx, cy - 8, cx, r.Y + 6);
                g.DrawLine(outPen, cx - 3, r.Y + 9, cx, r.Y + 6);
                g.DrawLine(outPen, cx + 3, r.Y + 9, cx, r.Y + 6);
            }
        }

        private void DrawHierarchyWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen stemPen, Pen divPen)
        {
            if (typeId == "Hierarchy_Labeled")
            {
                // 3 horizontal bands behind, tree in front
                int bandH = (r.Height - 6) / 3;
                for (int b = 0; b < 3; b++)
                {
                    int by = r.Y + 2 + b * (bandH + 1);
                    g.FillRectangle(fillBrush, r.X + 2, by, r.Width - 4, bandH);
                    g.DrawRectangle(outPen, r.X + 2, by, r.Width - 4, bandH);
                    g.DrawLine(outPen, r.X + 5, by + bandH / 2, r.X + 11, by + bandH / 2);
                }
                // Root node on band 0
                int topW = 16;
                int topH = bandH - 2;
                int topX = r.X + r.Width - topW - 6;
                g.FillRectangle(Brushes.White, topX, r.Y + 3, topW, topH);
                g.DrawRectangle(outPen, topX, r.Y + 3, topW, topH);

                // Middle nodes on band 1
                int midW = 14;
                int midX1 = topX - 10;
                int midX2 = topX + 6;
                g.FillRectangle(Brushes.White, midX1, r.Y + 3 + bandH + 1, midW, topH);
                g.DrawRectangle(outPen, midX1, r.Y + 3 + bandH + 1, midW, topH);
                g.FillRectangle(Brushes.White, midX2, r.Y + 3 + bandH + 1, midW, topH);
                g.DrawRectangle(outPen, midX2, r.Y + 3 + bandH + 1, midW, topH);
            }
            else if (typeId.Contains("Horizontal"))
            {
                // Horizontal hierarchy (Root on left, children on right)
                int nw = 12;
                int nh = 8;
                int ry = r.Y + (r.Height - nh) / 2;
                g.FillRectangle(fillBrush, r.X + 4, ry, nw, nh);
                g.DrawRectangle(outPen, r.X + 4, ry, nw, nh);

                int midX = r.X + 22;
                g.DrawLine(stemPen, r.X + 4 + nw, ry + nh / 2, midX, ry + nh / 2);
                g.DrawLine(stemPen, midX, r.Y + 8, midX, r.Bottom - 8);

                int[] cys = new int[] { r.Y + 6, ry, r.Bottom - 14 };
                for (int k = 0; k < 3; k++)
                {
                    g.DrawLine(stemPen, midX, cys[k] + nh / 2, midX + 6, cys[k] + nh / 2);
                    g.FillRectangle(fillBrush, midX + 6, cys[k], nw + 2, nh);
                    g.DrawRectangle(outPen, midX + 6, cys[k], nw + 2, nh);
                }
            }
            else if (typeId.Contains("Architecture"))
            {
                // Architecture: 3 stacked tiers from bottom up
                // Foundation (wide block at bottom)
                g.FillRectangle(fillBrush, r.X + 4, r.Bottom - 14, r.Width - 8, 10);
                g.DrawRectangle(outPen, r.X + 4, r.Bottom - 14, r.Width - 8, 10);

                // Platform (2 blocks in middle)
                int pw = (r.Width - 12) / 2;
                g.FillRectangle(fillBrush, r.X + 4, r.Bottom - 28, pw, 10);
                g.DrawRectangle(outPen, r.X + 4, r.Bottom - 28, pw, 10);
                g.FillRectangle(fillBrush, r.X + 8 + pw, r.Bottom - 28, pw, 10);
                g.DrawRectangle(outPen, r.X + 8 + pw, r.Bottom - 28, pw, 10);

                // Application (3 blocks on top)
                int tw = (r.Width - 16) / 3;
                for (int k = 0; k < 3; k++)
                {
                    g.FillRectangle(fillBrush, r.X + 4 + k * (tw + 4), r.Y + 8, tw, 10);
                    g.DrawRectangle(outPen, r.X + 4 + k * (tw + 4), r.Y + 8, tw, 10);
                }
            }
            else if (typeId.Contains("HalfCircle"))
            {
                // Half circle org
                int cx = r.X + r.Width / 2;
                g.FillRectangle(fillBrush, cx - 8, r.Y + 4, 16, 8);
                g.DrawRectangle(outPen, cx - 8, r.Y + 4, 16, 8);

                // 4 nodes along semi-circle
                for (int k = 0; k < 4; k++)
                {
                    double ang = Math.PI * 0.15 + k * (Math.PI * 0.7 / 3.0);
                    int nx = (int)(cx + 20 * Math.Cos(ang));
                    int ny = (int)(r.Y + 12 + 22 * Math.Sin(ang));
                    g.DrawLine(stemPen, cx, r.Y + 12, nx, ny);
                    g.FillEllipse(fillBrush, nx - 4, ny - 4, 8, 8);
                    g.DrawEllipse(outPen, nx - 4, ny - 4, 8, 8);
                }
            }
            else if (typeId.Contains("Table"))
            {
                // Table hierarchy: grid without stems
                int colW = (r.Width - 8) / 3;
                g.FillRectangle(fillBrush, r.X + 4, r.Y + 6, r.Width - 8, 10);
                g.DrawRectangle(outPen, r.X + 4, r.Y + 6, r.Width - 8, 10);

                for (int k = 0; k < 3; k++)
                {
                    g.FillRectangle(fillBrush, r.X + 4 + k * colW, r.Y + 18, colW - 1, 24);
                    g.DrawRectangle(outPen, r.X + 4 + k * colW, r.Y + 18, colW - 1, 24);
                }
            }
            else
            {
                // Classic Org Chart with assistant node
                int nw = 15;
                int nh = 8;
                int rx = r.X + (r.Width - nw) / 2;
                g.FillRectangle(fillBrush, rx, r.Y + 4, nw, nh);
                g.DrawRectangle(outPen, rx, r.Y + 4, nw, nh);

                // Stem down
                int midY = r.Y + 22;
                g.DrawLine(stemPen, r.X + r.Width / 2, r.Y + 12, r.X + r.Width / 2, midY);

                // Assistant node on left
                g.DrawLine(stemPen, r.X + r.Width / 2, r.Y + 16, r.X + 16, r.Y + 16);
                g.FillRectangle(fillBrush, r.X + 6, r.Y + 12, 10, 7);
                g.DrawRectangle(outPen, r.X + 6, r.Y + 12, 10, 7);

                // Horizontal bus line
                g.DrawLine(stemPen, r.X + 8, midY, r.Right - 8, midY);

                // 3 children
                int cw = 12;
                int ch = 8;
                int c1x = r.X + 4;
                int c2x = r.X + (r.Width - cw) / 2;
                int c3x = r.Right - 4 - cw;
                int cy = midY + 4;

                g.DrawLine(stemPen, c1x + cw / 2, midY, c1x + cw / 2, cy);
                g.DrawLine(stemPen, c2x + cw / 2, midY, c2x + cw / 2, cy);
                g.DrawLine(stemPen, c3x + cw / 2, midY, c3x + cw / 2, cy);

                g.FillRectangle(fillBrush, c1x, cy, cw, ch);
                g.DrawRectangle(outPen, c1x, cy, cw, ch);
                g.FillRectangle(fillBrush, c2x, cy, cw, ch);
                g.DrawRectangle(outPen, c2x, cy, cw, ch);
                g.FillRectangle(fillBrush, c3x, cy, cw, ch);
                g.DrawRectangle(outPen, c3x, cy, cw, ch);
            }
        }

        private void DrawPictureWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen stemPen, Pen divPen)
        {
            if (typeId.Contains("Grid"))
            {
                // 2x2 picture grid
                int pw = (r.Width - 10) / 2;
                int ph = (r.Height - 10) / 2;
                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        int px = r.X + 4 + col * (pw + 2);
                        int py = r.Y + 4 + row * (ph + 2);
                        g.FillRectangle(fillBrush, px, py, pw, ph);
                        g.DrawRectangle(outPen, px, py, pw, ph);
                        g.DrawEllipse(outPen, px + pw - 6, py + 3, 3, 3);
                    }
                }
            }
            else if (typeId.Contains("Strips") || typeId.Contains("Lineup") || typeId.Contains("Horizontal"))
            {
                // 3 horizontal strips with photo square on left
                int sh = (r.Height - 10) / 3;
                for (int k = 0; k < 3; k++)
                {
                    int sy = r.Y + 4 + k * (sh + 1);
                    // Photo square
                    g.FillRectangle(fillBrush, r.X + 4, sy, sh, sh);
                    g.DrawRectangle(outPen, r.X + 4, sy, sh, sh);
                    g.DrawEllipse(outPen, r.X + sh - 2, sy + 2, 2, 2);
                    // Text line on right
                    g.FillRectangle(fillBrush, r.X + 6 + sh, sy + 2, r.Right - r.X - 10 - sh, sh - 4);
                    g.DrawRectangle(outPen, r.X + 6 + sh, sy + 2, r.Right - r.X - 10 - sh, sh - 4);
                }
            }
            else
            {
                // Picture frame with sun & mountain
                int pw = r.Width - 12;
                int ph = r.Height - 14;
                g.FillRectangle(fillBrush, r.X + 6, r.Y + 6, pw, ph);
                g.DrawRectangle(outPen, r.X + 6, r.Y + 6, pw, ph);

                // Sun
                g.DrawEllipse(outPen, r.X + pw - 4, r.Y + 9, 4, 4);

                // Mountains
                PointF[] mtn = new PointF[] {
                    new PointF(r.X + 8, r.Y + ph + 4),
                    new PointF(r.X + 18, r.Y + ph - 8),
                    new PointF(r.X + 26, r.Y + ph),
                    new PointF(r.X + 34, r.Y + ph - 5),
                    new PointF(r.X + pw + 4, r.Y + ph + 4)
                };
                g.DrawLines(outPen, mtn);
            }
        }

        private void DrawCycleWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen stemPen)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int rad = 14;

            if (typeId.Contains("Radial"))
            {
                g.FillEllipse(fillBrush, cx - 6, cy - 6, 12, 12);
                g.DrawEllipse(outPen, cx - 6, cy - 6, 12, 12);
                for (int k = 0; k < 4; k++)
                {
                    double ang = k * Math.PI / 2.0;
                    int sx = (int)(cx + rad * Math.Cos(ang));
                    int sy = (int)(cy + rad * Math.Sin(ang));
                    g.DrawLine(stemPen, cx, cy, sx, sy);
                    g.FillEllipse(fillBrush, sx - 4, sy - 4, 8, 8);
                    g.DrawEllipse(outPen, sx - 4, sy - 4, 8, 8);
                }
            }
            else if (typeId.Contains("Pie"))
            {
                // Segmented pie chart
                g.FillEllipse(fillBrush, cx - 16, cy - 16, 32, 32);
                g.DrawEllipse(outPen, cx - 16, cy - 16, 32, 32);
                g.DrawLine(outPen, cx, cy - 16, cx, cy + 16);
                g.DrawLine(outPen, cx - 16, cy, cx + 16, cy);
            }
            else
            {
                // Circular nodes with curved arrow arcs
                for (int k = 0; k < 4; k++)
                {
                    double ang = k * Math.PI / 2.0 - Math.PI / 4.0;
                    int sx = (int)(cx + rad * Math.Cos(ang));
                    int sy = (int)(cy + rad * Math.Sin(ang));
                    g.FillEllipse(fillBrush, sx - 4, sy - 4, 8, 8);
                    g.DrawEllipse(outPen, sx - 4, sy - 4, 8, 8);
                }
                g.DrawArc(stemPen, cx - rad + 2, cy - rad + 2, (rad - 2) * 2, (rad - 2) * 2, 0, 360);
            }
        }

        private void DrawProcessWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen divPen, Pen stemPen)
        {
            if (typeId.Contains("Bending"))
            {
                // S-curved pipeline
                int bw = 12;
                int bh = 10;
                g.FillRectangle(fillBrush, r.X + 6, r.Y + 8, bw, bh);
                g.DrawRectangle(outPen, r.X + 6, r.Y + 8, bw, bh);

                g.FillRectangle(fillBrush, r.Right - 18, r.Y + 20, bw, bh);
                g.DrawRectangle(outPen, r.Right - 18, r.Y + 20, bw, bh);

                g.FillRectangle(fillBrush, r.X + 6, r.Bottom - 18, bw, bh);
                g.DrawRectangle(outPen, r.X + 6, r.Bottom - 18, bw, bh);

                g.DrawLine(stemPen, r.X + 18, r.Y + 13, r.Right - 18, r.Y + 13);
                g.DrawLine(stemPen, r.Right - 12, r.Y + 13, r.Right - 12, r.Y + 20);
                g.DrawLine(stemPen, r.Right - 18, r.Y + 30, r.X + 12, r.Y + 30);
                g.DrawLine(stemPen, r.X + 12, r.Y + 30, r.X + 12, r.Bottom - 18);
            }
            else
            {
                // 3 blocks with arrows
                int bw = 13;
                int bh = 18;
                int by = r.Y + (r.Height - bh) / 2;
                for (int k = 0; k < 3; k++)
                {
                    int bx = r.X + 4 + k * (bw + 7);
                    g.FillRectangle(fillBrush, bx, by, bw, bh);
                    g.DrawRectangle(outPen, bx, by, bw, bh);
                    if (k < 2)
                    {
                        g.DrawLine(stemPen, bx + bw, by + bh / 2, bx + bw + 6, by + bh / 2);
                        g.DrawLine(stemPen, bx + bw + 4, by + bh / 2 - 2, bx + bw + 6, by + bh / 2);
                        g.DrawLine(stemPen, bx + bw + 4, by + bh / 2 + 2, bx + bw + 6, by + bh / 2);
                    }
                }
            }
        }

        private void DrawListWireframe(Graphics g, Rectangle r, string typeId, Brush fillBrush, Pen outPen, Pen divPen, Pen stemPen)
        {
            if (typeId.Contains("Trapezoid"))
            {
                int count = 3;
                int th = (r.Height - 10) / count;
                for (int k = 0; k < count; k++)
                {
                    int ty = r.Y + 4 + k * (th + 1);
                    PointF[] trap = new PointF[] {
                        new PointF(r.X + 4 + k * 3, ty),
                        new PointF(r.Right - 4 - k * 3, ty),
                        new PointF(r.Right - 7 - k * 3, ty + th),
                        new PointF(r.X + 7 + k * 3, ty + th)
                    };
                    g.FillPolygon(fillBrush, trap);
                    g.DrawPolygon(outPen, trap);
                }
            }
            else
            {
                int count = 3;
                int bh = (r.Height - 12) / count;
                for (int k = 0; k < count; k++)
                {
                    int by = r.Y + 4 + k * (bh + 2);
                    g.FillRectangle(fillBrush, r.X + 4, by, r.Width - 8, bh);
                    g.DrawRectangle(outPen, r.X + 4, by, r.Width - 8, bh);
                    g.DrawLine(divPen, r.X + 8, by + bh / 2, r.X + 16, by + bh / 2);
                }
            }
        }

        private void CenterCardsPanel_MouseMove(object sender, MouseEventArgs e)
        {
            Point unscrollPt = new Point(e.X - centerCardsPanel.AutoScrollPosition.X, e.Y - centerCardsPanel.AutoScrollPosition.Y);
            int hit = -1;
            for (int i = 0; i < filteredTemplates.Count; i++)
            {
                if (filteredTemplates[i].Bounds.Contains(unscrollPt))
                {
                    hit = i;
                    break;
                }
            }

            if (hoveredTemplateIndex != hit)
            {
                hoveredTemplateIndex = hit;
                centerCardsPanel.Invalidate();
            }
        }

        private void CenterCardsPanel_MouseClick(object sender, MouseEventArgs e)
        {
            Point unscrollPt = new Point(e.X - centerCardsPanel.AutoScrollPosition.X, e.Y - centerCardsPanel.AutoScrollPosition.Y);
            for (int i = 0; i < filteredTemplates.Count; i++)
            {
                if (filteredTemplates[i].Bounds.Contains(unscrollPt))
                {
                    selectedTemplateIndex = i;
                    centerCardsPanel.Invalidate();
                    rightPreviewPanel.Invalidate();
                    UpdateDescriptionBox();
                    break;
                }
            }
        }

        // =========================================================================
        // RIGHT PREVIEW PANEL PAINTING (3 BANDS + MULTI-TIER GRADIENTS)
        // =========================================================================
        private void RightPreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (selectedTemplateIndex < 0 || selectedTemplateIndex >= filteredTemplates.Count) return;
            var tmpl = filteredTemplates[selectedTemplateIndex];

            // 1. Render MS Office 3-tier authentic gradient preview matching user screenshot
            Rectangle previewArea = new Rectangle(10, 12, rightPreviewPanel.Width - 20, 180);
            DrawOfficePreviewDiagram(g, previewArea, tmpl.TypeId);

            // 2. Divider Line
            g.DrawLine(new Pen(Color.FromArgb(225, 228, 235), 1), 12, 202, rightPreviewPanel.Width - 12, 202);

            // 3. Title
            using (Font titleF = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(0, 51, 102)))
            {
                g.DrawString(tmpl.Name, titleF, tb, 12, 210);
            }
        }

        private void DrawOfficePreviewDiagram(Graphics g, Rectangle r, string typeId)
        {
            if (previewDispatch == null) InitPreviewDispatch();
            Action<Graphics, Rectangle> drawer;
            if (previewDispatch.TryGetValue(typeId, out drawer))
            {
                drawer(g, r);
            }
            else
            {
                // Fallback for any unknown type
                DrawListPreview(g, r, typeId);
            }
        }

        // =========================================================================
        // SPECIFIC OFFICE PREVIEW RENDERERS
        // =========================================================================

        private void DrawGearPreview(Graphics g, Rectangle r)
        {
            // 3 interlocking mechanical gears in theme colors
            Color cBlueTop = Color.FromArgb(91, 155, 213);
            Color cBlueBot = Color.FromArgb(43, 91, 136);
            Color cBlueBorder = Color.FromArgb(31, 78, 121);

            Color cOrangeTop = Color.FromArgb(237, 125, 49);
            Color cOrangeBot = Color.FromArgb(198, 89, 17);
            Color cOrangeBorder = Color.FromArgb(162, 70, 11);

            Color cGreenTop = Color.FromArgb(112, 173, 71);
            Color cGreenBot = Color.FromArgb(83, 129, 53);
            Color cGreenBorder = Color.FromArgb(55, 86, 35);

            // Gear 1 (Large, Blue)
            DrawGear(g, r.X + 64, r.Y + 110, 36, 26, 12, cBlueTop, cBlueBot, cBlueBorder);
            // Gear 2 (Medium, Orange)
            DrawGear(g, r.X + 118, r.Y + 56, 30, 21, 10, cOrangeTop, cOrangeBot, cOrangeBorder);
            // Gear 3 (Small, Green)
            DrawGear(g, r.X + 164, r.Y + 112, 24, 16, 8, cGreenTop, cGreenBot, cGreenBorder);
        }

        private void DrawFunnelPreview(Graphics g, Rectangle r)
        {
            // 3D-styled tapered funnel
            int cx = r.X + r.Width / 2;

            // Top ellipse opening
            Rectangle topEllipse = new Rectangle(cx - 70, r.Y + 16, 140, 30);
            using (LinearGradientBrush lgb = new LinearGradientBrush(topEllipse, Color.FromArgb(68, 114, 196), Color.FromArgb(31, 78, 121), LinearGradientMode.Horizontal))
            {
                g.FillEllipse(lgb, topEllipse);
            }
            using (Pen p = new Pen(Color.FromArgb(31, 78, 121), 1.5f))
            {
                g.DrawEllipse(p, topEllipse);
            }

            // Funnel conical body
            PointF[] body = new PointF[] {
                new PointF(cx - 70, r.Y + 31),
                new PointF(cx + 70, r.Y + 31),
                new PointF(cx + 18, r.Bottom - 38),
                new PointF(cx + 18, r.Bottom - 12),
                new PointF(cx - 18, r.Bottom - 12),
                new PointF(cx - 18, r.Bottom - 38)
            };
            Rectangle bodyRect = new Rectangle(cx - 70, r.Y + 30, 140, r.Height - 42);
            using (LinearGradientBrush lgb = new LinearGradientBrush(bodyRect, Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), LinearGradientMode.Vertical))
            {
                g.FillPolygon(lgb, body);
            }
            using (Pen p = new Pen(Color.FromArgb(31, 78, 121), 1.5f))
            {
                g.DrawPolygon(p, body);
            }

            // 4 colored spheres entering funnel
            DrawSpecularSphere(g, cx - 35, r.Y + 12, 14, Color.FromArgb(237, 125, 49)); // Orange
            DrawSpecularSphere(g, cx, r.Y + 4, 16, Color.FromArgb(91, 155, 213));      // Blue
            DrawSpecularSphere(g, cx + 35, r.Y + 14, 13, Color.FromArgb(112, 173, 71)); // Green
            DrawSpecularSphere(g, cx - 10, r.Y + 28, 12, Color.FromArgb(255, 192, 0));  // Gold

            // Refined drop at nozzle bottom
            DrawSpecularSphere(g, cx, r.Bottom - 8, 8, Color.FromArgb(0, 176, 240));
        }

        private void DrawSpecularSphere(Graphics g, float cx, float cy, float radius, Color baseColor)
        {
            RectangleF sRect = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(sRect);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterPoint = new PointF(cx - radius * 0.3f, cy - radius * 0.3f);
                    pgb.CenterColor = Color.White;
                    pgb.SurroundColors = new Color[] { baseColor };
                    g.FillEllipse(pgb, sRect);
                }
            }
            using (Pen p = new Pen(Color.FromArgb(100, 0, 0, 0), 1.0f))
            {
                g.DrawEllipse(p, sRect);
            }
        }

        private void DrawBalancePreview(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + 24;

            // Triangular fulcrum base at bottom
            PointF[] baseTri = new PointF[] {
                new PointF(cx, r.Bottom - 38),
                new PointF(cx - 28, r.Bottom - 10),
                new PointF(cx + 28, r.Bottom - 10)
            };
            Rectangle baseRect = new Rectangle(cx - 28, r.Bottom - 38, 56, 28);
            using (LinearGradientBrush lgb = new LinearGradientBrush(baseRect, Color.FromArgb(166, 166, 166), Color.FromArgb(89, 89, 89), LinearGradientMode.Vertical))
            {
                g.FillPolygon(lgb, baseTri);
            }
            using (Pen p = new Pen(Color.FromArgb(59, 59, 59), 1.5f))
            {
                g.DrawPolygon(p, baseTri);
            }

            // Center pillar
            using (Pen stemPen = new Pen(Color.FromArgb(120, 120, 120), 4.0f))
            {
                g.DrawLine(stemPen, cx, cy, cx, r.Bottom - 38);
            }
            // Pivot cap
            g.FillEllipse(Brushes.Gold, cx - 6, cy - 6, 12, 12);
            g.DrawEllipse(Pens.DarkGoldenrod, cx - 6, cy - 6, 12, 12);

            // Tilted balance beam (tilted ~8 degrees)
            using (Pen beamPen = new Pen(Color.FromArgb(80, 80, 80), 3.5f))
            {
                g.DrawLine(beamPen, r.X + 16, cy - 10, r.Right - 16, cy + 10);
            }

            // Left pan (higher)
            using (Pen cordPen = new Pen(Color.FromArgb(140, 140, 140), 1.5f))
            {
                g.DrawLine(cordPen, r.X + 22, cy - 9, r.X + 14, cy + 34);
                g.DrawLine(cordPen, r.X + 46, cy - 6, r.X + 54, cy + 34);
            }
            Rectangle leftPan = new Rectangle(r.X + 12, cy + 34, 44, 8);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(127, 127, 127)))
            {
                g.FillRectangle(b, leftPan);
            }
            g.DrawRectangle(Pens.DimGray, leftPan);
            // Blue weight on left pan
            DrawGradientNode(g, new Rectangle(r.X + 22, cy + 14, 24, 20),
                Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 3, true);

            // Right pan (lower)
            using (Pen cordPen = new Pen(Color.FromArgb(140, 140, 140), 1.5f))
            {
                g.DrawLine(cordPen, r.Right - 46, cy + 6, r.Right - 54, cy + 50);
                g.DrawLine(cordPen, r.Right - 22, cy + 9, r.Right - 14, cy + 50);
            }
            Rectangle rightPan = new Rectangle(r.Right - 56, cy + 50, 44, 8);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(127, 127, 127)))
            {
                g.FillRectangle(b, rightPan);
            }
            g.DrawRectangle(Pens.DimGray, rightPan);
            // Orange weight on right pan
            DrawGradientNode(g, new Rectangle(r.Right - 46, cy + 30, 24, 20),
                Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 3, true);
        }

        private void DrawTargetPreview(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2 - 12;
            int cy = r.Y + r.Height / 2;

            // Outer ring (Blue)
            Rectangle r1 = new Rectangle(cx - 65, cy - 65, 130, 130);
            using (LinearGradientBrush lgb = new LinearGradientBrush(r1, Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), LinearGradientMode.ForwardDiagonal))
            {
                g.FillEllipse(lgb, r1);
            }
            g.DrawEllipse(Pens.White, r1);

            // Middle ring (Orange)
            Rectangle r2 = new Rectangle(cx - 44, cy - 44, 88, 88);
            using (LinearGradientBrush lgb = new LinearGradientBrush(r2, Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), LinearGradientMode.ForwardDiagonal))
            {
                g.FillEllipse(lgb, r2);
            }
            g.DrawEllipse(Pens.White, r2);

            // Inner bullseye (Gold/Red)
            Rectangle r3 = new Rectangle(cx - 24, cy - 24, 48, 48);
            using (LinearGradientBrush lgb = new LinearGradientBrush(r3, Color.FromArgb(255, 192, 0), Color.FromArgb(197, 144, 0), LinearGradientMode.ForwardDiagonal))
            {
                g.FillEllipse(lgb, r3);
            }
            g.DrawEllipse(Pens.White, r3);

            // White center dot
            g.FillEllipse(Brushes.White, cx - 5, cy - 5, 10, 10);

            // Callout lines & label cards on right
            using (Pen p = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
            {
                g.DrawLine(p, cx + 55, cy - 36, r.Right - 8, cy - 36);
                g.DrawLine(p, cx + 38, cy, r.Right - 8, cy);
                g.DrawLine(p, cx + 20, cy + 36, r.Right - 8, cy + 36);
            }
            DrawGradientNode(g, new Rectangle(r.Right - 36, cy - 46, 32, 20), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 3, true);
            DrawGradientNode(g, new Rectangle(r.Right - 36, cy - 10, 32, 20), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 3, true);
            DrawGradientNode(g, new Rectangle(r.Right - 36, cy + 26, 32, 20), Color.FromArgb(255, 192, 0), Color.FromArgb(197, 144, 0), Color.FromArgb(150, 110, 0), 3, true);
        }

        private void DrawVennPreview(Graphics g, Rectangle r, string typeId)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int rad = 44;

            // 3 overlapping translucent circles
            // Blue circle (Left)
            using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 68, 114, 196)))
            {
                g.FillEllipse(b, cx - rad - 14, cy - rad + 6, rad * 2, rad * 2);
            }
            using (Pen p = new Pen(Color.FromArgb(43, 91, 136), 1.5f))
            {
                g.DrawEllipse(p, cx - rad - 14, cy - rad + 6, rad * 2, rad * 2);
            }

            // Orange circle (Right)
            using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 237, 125, 49)))
            {
                g.FillEllipse(b, cx - rad + 14, cy - rad + 6, rad * 2, rad * 2);
            }
            using (Pen p = new Pen(Color.FromArgb(198, 89, 17), 1.5f))
            {
                g.DrawEllipse(p, cx - rad + 14, cy - rad + 6, rad * 2, rad * 2);
            }

            // Green circle (Bottom)
            using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 112, 173, 71)))
            {
                g.FillEllipse(b, cx - rad, cy - rad + 36, rad * 2, rad * 2);
            }
            using (Pen p = new Pen(Color.FromArgb(83, 129, 53), 1.5f))
            {
                g.DrawEllipse(p, cx - rad, cy - rad + 36, rad * 2, rad * 2);
            }

            // Center highlight
            g.FillEllipse(Brushes.White, cx - 4, cy + 12, 8, 8);
        }

        private void DrawEquationPreview(Graphics g, Rectangle r, string typeId)
        {
            int bw = 48;
            int bh = 48;
            int by = r.Y + (r.Height - bh) / 2;

            // Block A (Blue)
            DrawGradientNode(g, new Rectangle(r.X + 8, by, bw, bh),
                Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 6, true);

            // Plus '+'
            int px = r.X + 8 + bw + 12;
            int py = by + bh / 2;
            using (Pen signPen = new Pen(Color.FromArgb(90, 90, 90), 3.0f))
            {
                g.DrawLine(signPen, px - 6, py, px + 6, py);
                g.DrawLine(signPen, px, py - 6, px, py + 6);
            }

            // Block B (Orange)
            DrawGradientNode(g, new Rectangle(px + 12, by, bw, bh),
                Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 6, true);

            // Equals '='
            int ex = px + 12 + bw + 12;
            using (Pen signPen = new Pen(Color.FromArgb(90, 90, 90), 3.0f))
            {
                g.DrawLine(signPen, ex - 6, py - 4, ex + 6, py - 4);
                g.DrawLine(signPen, ex - 6, py + 4, ex + 6, py + 4);
            }

            // Block C (Green, slightly larger)
            int cw = 54;
            int ch = 54;
            DrawGradientNode(g, new Rectangle(ex + 12, by - 3, cw, ch),
                Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 6, true);
        }

        private void DrawTabPreview(Graphics g, Rectangle r)
        {
            int count = 3;
            int th = 40;
            int gap = 8;
            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

            for (int k = 0; k < count; k++)
            {
                int ty = r.Y + 14 + k * (th + gap);
                // Protruding tab ear
                Rectangle earRect = new Rectangle(r.X + 12 + k * 44, ty, 42, 12);
                DrawGradientNode(g, earRect, tops[k], bots[k], borders[k], 4, false);

                // Body
                Rectangle bodyRect = new Rectangle(r.X + 12, ty + 10, r.Width - 24, th - 10);
                DrawGradientNode(g, bodyRect, tops[k], bots[k], borders[k], 4, true);
            }
        }

        private void DrawBracketPreview(Graphics g, Rectangle r)
        {
            int bx = r.X + 24;
            int topY = r.Y + 16;
            int botY = r.Bottom - 16;
            int midY = (topY + botY) / 2;

            // Large curly bracket
            using (Pen bp = new Pen(Color.FromArgb(91, 155, 213), 3.0f))
            {
                g.DrawLine(bp, bx + 12, topY, bx, topY);
                g.DrawLine(bp, bx, topY, bx, midY - 6);
                g.DrawLine(bp, bx, midY - 6, bx - 8, midY);
                g.DrawLine(bp, bx - 8, midY, bx, midY + 6);
                g.DrawLine(bp, bx, midY + 6, bx, botY);
                g.DrawLine(bp, bx, botY, bx + 12, botY);
            }

            // 3 stacked cards on right
            int rh = 34;
            int gap = 8;
            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

            for (int k = 0; k < 3; k++)
            {
                int ry = r.Y + 18 + k * (rh + gap);
                Rectangle cardRect = new Rectangle(bx + 18, ry, r.Right - bx - 30, rh);
                DrawGradientNode(g, cardRect, tops[k], bots[k], borders[k], 4, true);
            }
        }

        private void DrawSnapshotPreview(Graphics g, Rectangle r)
        {
            // 3 Polaroid photo cards tilted slightly
            int pw = 68;
            int ph = 84;

            // Photo 1 (Left, tilted -6 deg)
            DrawPolaroidCard(g, r.X + 16, r.Y + 36, pw, ph, -6f, Color.FromArgb(91, 155, 213));

            // Photo 3 (Right, tilted +6 deg)
            DrawPolaroidCard(g, r.Right - pw - 16, r.Y + 40, pw, ph, 6f, Color.FromArgb(112, 173, 71));

            // Photo 2 (Center, in front, straight)
            DrawPolaroidCard(g, r.X + (r.Width - pw) / 2, r.Y + 22, pw, ph, 0f, Color.FromArgb(237, 125, 49));
        }

        private void DrawPolaroidCard(Graphics g, float x, float y, float w, float h, float angle, Color accentColor)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(x + w / 2f, y + h / 2f);
            g.RotateTransform(angle);
            RectangleF cardRect = new RectangleF(-w / 2f, -h / 2f, w, h);

            // Drop shadow
            RectangleF shadowRect = new RectangleF(cardRect.X + 2, cardRect.Y + 3, w, h);
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillRectangle(sb, shadowRect);
            }

            // White Polaroid body
            g.FillRectangle(Brushes.White, cardRect);
            using (Pen p = new Pen(Color.FromArgb(210, 210, 210), 1.0f))
            {
                g.DrawRectangle(p, cardRect.X, cardRect.Y, cardRect.Width, cardRect.Height);
            }

            // Inner photo square
            float pad = 6;
            RectangleF photoRect = new RectangleF(cardRect.X + pad, cardRect.Y + pad, w - pad * 2, h - pad * 2 - 16);
            DrawPicturePlaceholder(g, Rectangle.Round(photoRect), false);

            // Caption line in bottom margin
            using (Pen p = new Pen(accentColor, 2.0f))
            {
                g.DrawLine(p, cardRect.X + pad + 6, cardRect.Bottom - 10, cardRect.Right - pad - 6, cardRect.Bottom - 10);
            }

            g.Restore(state);
        }

        private void DrawBubblePreview(Graphics g, Rectangle r)
        {
            // 4 circular bubbles of varying sizes with connections
            DrawPicturePlaceholder(g, new Rectangle(r.X + 16, r.Y + 40, 60, 60), true);
            DrawPicturePlaceholder(g, new Rectangle(r.X + 80, r.Y + 16, 50, 50), true);
            DrawPicturePlaceholder(g, new Rectangle(r.X + 138, r.Y + 54, 56, 56), true);
            DrawPicturePlaceholder(g, new Rectangle(r.X + 88, r.Y + 98, 42, 42), true);

            // Curved connecting line
            using (Pen p = new Pen(Color.FromArgb(160, 160, 160), 1.5f))
            {
                p.DashStyle = DashStyle.Dash;
                g.DrawArc(p, r.X + 36, r.Y + 30, 130, 90, 20, 220);
            }
        }

        private void DrawHexagonPreview(Graphics g, Rectangle r, string typeId)
        {
            // 4 interlocking alternating hexagons
            float rad = 32;
            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53), Color.FromArgb(197, 144, 0) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35), Color.FromArgb(150, 110, 0) };

            float startX = r.X + 36;
            float stepX = 46;

            for (int k = 0; k < 4; k++)
            {
                float hx = startX + k * stepX;
                float hy = (k % 2 == 0) ? r.Y + 60 : r.Y + 115;
                DrawHexagon(g, hx, hy, rad, tops[k], bots[k], borders[k]);
            }
        }

        private void DrawPyramidPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("Inverted"))
            {
                // Inverted pyramid: apex at bottom
                int h = 140;
                int topY = r.Y + 16;
                PointF[] pyr = new PointF[] {
                    new PointF(r.X + 16, topY),
                    new PointF(r.Right - 16, topY),
                    new PointF(r.X + r.Width / 2f, topY + h)
                };
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 16, topY, r.Width - 32, h),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(255, 192, 0), LinearGradientMode.Vertical))
                {
                    g.FillPolygon(lgb, pyr);
                }
                g.DrawPolygon(new Pen(Color.FromArgb(31, 78, 121), 1.5f), pyr);

                // Tier divider lines & text dashes
                using (Pen divPen = new Pen(Color.White, 2.0f))
                {
                    g.DrawLine(divPen, r.X + 38, topY + h * 0.33f, r.Right - 38, topY + h * 0.33f);
                    g.DrawLine(divPen, r.X + 62, topY + h * 0.66f, r.Right - 62, topY + h * 0.66f);
                }
            }
            else if (typeId.Contains("List"))
            {
                // Pyramid on left + 3 horizontal cards on right
                int topY = r.Y + 18;
                int h = 136;
                PointF[] pyr = new PointF[] {
                    new PointF(r.X + 44, topY),
                    new PointF(r.X + 80, topY + h),
                    new PointF(r.X + 8, topY + h)
                };
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 8, topY, 72, h),
                    Color.FromArgb(255, 192, 0), Color.FromArgb(43, 91, 136), LinearGradientMode.Vertical))
                {
                    g.FillPolygon(lgb, pyr);
                }
                g.DrawPolygon(new Pen(Color.FromArgb(31, 78, 121), 1.5f), pyr);

                // 3 cards on right
                int ch = 36;
                int gap = 12;
                Color[] tops = new Color[] { Color.FromArgb(255, 192, 0), Color.FromArgb(237, 125, 49), Color.FromArgb(91, 155, 213) };
                Color[] bots = new Color[] { Color.FromArgb(197, 144, 0), Color.FromArgb(198, 89, 17), Color.FromArgb(43, 91, 136) };
                Color[] borders = new Color[] { Color.FromArgb(150, 110, 0), Color.FromArgb(162, 70, 11), Color.FromArgb(31, 78, 121) };

                for (int k = 0; k < 3; k++)
                {
                    int cy = topY + 6 + k * (ch + gap);
                    Rectangle cr = new Rectangle(r.X + 90, cy, r.Right - r.X - 98, ch);
                    DrawGradientNode(g, cr, tops[k], bots[k], borders[k], 4, true);
                    using (Pen p = new Pen(Color.FromArgb(140, 140, 140), 1.5f))
                    {
                        g.DrawLine(p, r.X + 64, cy + ch / 2, r.X + 90, cy + ch / 2);
                    }
                }
            }
            else
            {
                // Basic upright pyramid
                int topY = r.Y + 16;
                int h = 140;
                PointF[] pyr = new PointF[] {
                    new PointF(r.X + r.Width / 2f, topY),
                    new PointF(r.Right - 16, topY + h),
                    new PointF(r.X + 16, topY + h)
                };
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 16, topY, r.Width - 32, h),
                    Color.FromArgb(255, 192, 0), Color.FromArgb(43, 91, 136), LinearGradientMode.Vertical))
                {
                    g.FillPolygon(lgb, pyr);
                }
                g.DrawPolygon(new Pen(Color.FromArgb(31, 78, 121), 1.5f), pyr);

                // Tier dividers
                using (Pen divPen = new Pen(Color.White, 2.0f))
                {
                    g.DrawLine(divPen, r.X + 62, topY + h * 0.33f, r.Right - 62, topY + h * 0.33f);
                    g.DrawLine(divPen, r.X + 38, topY + h * 0.66f, r.Right - 38, topY + h * 0.66f);
                }
            }
        }

        private void DrawMatrixPreview(Graphics g, Rectangle r, string typeId)
        {
            int halfW = (r.Width - 32) / 2;
            int halfH = (r.Height - 32) / 2;

            // 4 Quadrants
            // Top-Left (Blue)
            DrawGradientNode(g, new Rectangle(r.X + 12, r.Y + 12, halfW, halfH),
                Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

            // Top-Right (Orange)
            DrawGradientNode(g, new Rectangle(r.X + 20 + halfW, r.Y + 12, halfW, halfH),
                Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);

            // Bottom-Left (Green)
            DrawGradientNode(g, new Rectangle(r.X + 12, r.Y + 20 + halfH, halfW, halfH),
                Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 4, true);

            // Bottom-Right (Gold)
            DrawGradientNode(g, new Rectangle(r.X + 20 + halfW, r.Y + 20 + halfH, halfW, halfH),
                Color.FromArgb(255, 192, 0), Color.FromArgb(197, 144, 0), Color.FromArgb(150, 110, 0), 4, true);

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            if (typeId.Contains("Grid"))
            {
                // Axis cross lines
                using (Pen p = new Pen(Color.FromArgb(60, 60, 60), 2.5f))
                {
                    p.EndCap = LineCap.ArrowAnchor;
                    g.DrawLine(p, cx, cy, r.Right - 4, cy);
                    g.DrawLine(p, cx, cy, r.X + 4, cy);
                    g.DrawLine(p, cx, cy, cx, r.Y + 4);
                    g.DrawLine(p, cx, cy, cx, r.Bottom - 4);
                }
            }
            else if (typeId.Contains("Titled"))
            {
                // Central diamond
                PointF[] diamond = new PointF[] {
                    new PointF(cx, cy - 18),
                    new PointF(cx + 18, cy),
                    new PointF(cx, cy + 18),
                    new PointF(cx - 18, cy)
                };
                g.FillPolygon(Brushes.White, diamond);
                g.DrawPolygon(new Pen(Color.FromArgb(43, 91, 136), 2.0f), diamond);
            }
            else if (typeId.Contains("Cycle"))
            {
                // Central swirling circular arrow
                using (Pen p = new Pen(Color.FromArgb(43, 91, 136), 3.0f))
                {
                    p.EndCap = LineCap.ArrowAnchor;
                    g.DrawArc(p, cx - 18, cy - 18, 36, 36, 45, 270);
                }
            }
        }

        private void DrawTimelinePreview(Graphics g, Rectangle r, string typeId)
        {
            int midY = r.Y + r.Height / 2;

            // Horizontal timeline bar
            using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 12, midY - 3, r.Width - 24, 6),
                Color.FromArgb(91, 155, 213), Color.FromArgb(112, 173, 71), LinearGradientMode.Horizontal))
            {
                g.FillRectangle(lgb, r.X + 12, midY - 3, r.Width - 36, 6);
            }
            // Arrow tip on right
            PointF[] tip = new PointF[] {
                new PointF(r.Right - 24, midY - 7),
                new PointF(r.Right - 10, midY),
                new PointF(r.Right - 24, midY + 7)
            };
            g.FillPolygon(new SolidBrush(Color.FromArgb(112, 173, 71)), tip);

            // 4 milestones
            Color[] colors = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0) };
            int count = 4;
            int stepX = (r.Width - 60) / (count - 1);

            for (int k = 0; k < count; k++)
            {
                int mx = r.X + 22 + k * stepX;
                bool isUp = (k % 2 == 0);

                // Milestone circle
                g.FillEllipse(new SolidBrush(colors[k]), mx - 7, midY - 7, 14, 14);
                g.DrawEllipse(Pens.White, mx - 7, midY - 7, 14, 14);

                // Stem & callout box
                int boxY = isUp ? midY - 48 : midY + 16;
                using (Pen p = new Pen(colors[k], 1.5f))
                {
                    g.DrawLine(p, mx, isUp ? midY - 7 : midY + 7, mx, isUp ? boxY + 28 : boxY);
                }
                Rectangle callout = new Rectangle(mx - 20, boxY, 40, 28);
                DrawGradientNode(g, callout, colors[k], Color.FromArgb(colors[k].R / 2, colors[k].G / 2, colors[k].B / 2), Color.DarkGray, 3, true);
            }
        }

        private void DrawChevronPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("Vertical"))
            {
                // 3 vertical chevrons pointing down
                int ch = 44;
                int gap = 8;
                Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
                Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
                Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

                for (int k = 0; k < 3; k++)
                {
                    int cy = r.Y + 12 + k * (ch + gap);
                    PointF[] pts = new PointF[] {
                        new PointF(r.X + 20, cy),
                        new PointF(r.Right - 20, cy),
                        new PointF(r.Right - 20, cy + ch - 12),
                        new PointF(r.X + r.Width / 2f, cy + ch),
                        new PointF(r.X + 20, cy + ch - 12),
                        new PointF(r.X + r.Width / 2f, cy)
                    };
                    using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 20, cy, r.Width - 40, ch), tops[k], bots[k], LinearGradientMode.Vertical))
                    {
                        g.FillPolygon(lgb, pts);
                    }
                    using (Pen p = new Pen(borders[k], 1.5f))
                    {
                        g.DrawPolygon(p, pts);
                    }
                }
            }
            else
            {
                // 3 horizontal chevrons pointing right
                int cw = 64;
                int ch = 56;
                int cy = r.Y + (r.Height - ch) / 2;
                int notch = 14;
                Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
                Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
                Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

                for (int k = 0; k < 3; k++)
                {
                    int cx = r.X + 12 + k * (cw - notch + 6);
                    DrawChevronShape(g, new Rectangle(cx, cy, cw, ch), tops[k], bots[k], borders[k], notch);
                }
            }
        }

        private void DrawStepPreview(Graphics g, Rectangle r, string typeId)
        {
            int count = 3;
            int sw = 56;
            int gap = 8;
            bool isDown = typeId.Contains("StepDown") || typeId.Contains("Descending");

            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

            for (int k = 0; k < count; k++)
            {
                int sx = r.X + 16 + k * (sw + gap);
                int sh = 40 + (isDown ? (count - 1 - k) : k) * 36;
                int sy = r.Bottom - 18 - sh;

                Rectangle stepRect = new Rectangle(sx, sy, sw, sh);
                DrawGradientNode(g, stepRect, tops[k], bots[k], borders[k], 4, true);
            }

            // Ascending / Descending trend arrow
            using (Pen p = new Pen(Color.FromArgb(80, 80, 80), 2.5f))
            {
                p.EndCap = LineCap.ArrowAnchor;
                if (isDown)
                    g.DrawLine(p, r.X + 24, r.Y + 28, r.Right - 20, r.Bottom - 26);
                else
                    g.DrawLine(p, r.X + 24, r.Bottom - 26, r.Right - 20, r.Y + 28);
            }
        }

        private void DrawArrowsPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("Opposing") || typeId.Contains("Ribbon"))
            {
                // 2 large opposing horizontal arrows
                int ah = 44;
                // Top arrow right (Blue)
                PointF[] topArrow = new PointF[] {
                    new PointF(r.X + 16, r.Y + 28),
                    new PointF(r.Right - 42, r.Y + 28),
                    new PointF(r.Right - 42, r.Y + 16),
                    new PointF(r.Right - 16, r.Y + 28 + ah / 2f),
                    new PointF(r.Right - 42, r.Y + 40 + ah),
                    new PointF(r.Right - 42, r.Y + 28 + ah),
                    new PointF(r.X + 16, r.Y + 28 + ah)
                };
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 16, r.Y + 16, r.Width - 32, ah + 24),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), LinearGradientMode.Horizontal))
                {
                    g.FillPolygon(lgb, topArrow);
                }
                g.DrawPolygon(new Pen(Color.FromArgb(31, 78, 121), 1.5f), topArrow);

                // Bottom arrow left (Orange)
                PointF[] botArrow = new PointF[] {
                    new PointF(r.Right - 16, r.Bottom - 28 - ah),
                    new PointF(r.X + 42, r.Bottom - 28 - ah),
                    new PointF(r.X + 42, r.Bottom - 40 - ah),
                    new PointF(r.X + 16, r.Bottom - 28 - ah / 2f),
                    new PointF(r.X + 42, r.Bottom - 16),
                    new PointF(r.X + 42, r.Bottom - 28),
                    new PointF(r.Right - 16, r.Bottom - 28)
                };
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 16, r.Bottom - 40 - ah, r.Width - 32, ah + 24),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), LinearGradientMode.Horizontal))
                {
                    g.FillPolygon(lgb, botArrow);
                }
                g.DrawPolygon(new Pen(Color.FromArgb(162, 70, 11), 1.5f), botArrow);
            }
            else
            {
                // Converging / Diverging
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;
                DrawGradientNode(g, new Rectangle(cx - 24, cy - 24, 48, 48),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 24, true);

                Color[] colors = new Color[] { Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0), Color.FromArgb(112, 48, 160) };
                for (int k = 0; k < 4; k++)
                {
                    double ang = k * Math.PI / 2.0;
                    float sx = (float)(cx + 56 * Math.Cos(ang));
                    float sy = (float)(cy + 56 * Math.Sin(ang));
                    using (Pen p = new Pen(colors[k], 3.0f))
                    {
                        p.EndCap = typeId.Contains("Converging") ? LineCap.ArrowAnchor : LineCap.Round;
                        p.StartCap = typeId.Contains("Converging") ? LineCap.Round : LineCap.ArrowAnchor;
                        g.DrawLine(p, sx, sy, (float)(cx + 28 * Math.Cos(ang)), (float)(cy + 28 * Math.Sin(ang)));
                    }
                }
            }
        }

        private void DrawHierarchyPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId == "Hierarchy_Labeled")
            {
                // Exact Labeled Hierarchy: 3 salmon bands + tree in front
                Color bandBg = Color.FromArgb(247, 229, 222);
                int bandH = 34;
                int gap = 8;
                int radius = 6;

                for (int b = 0; b < 3; b++)
                {
                    int by = r.Y + 4 + b * (bandH + gap);
                    Rectangle bandRect = new Rectangle(r.X + 2, by, r.Width - 4, bandH);
                    FillRoundedRectangle(g, bandBg, bandRect, radius);
                    using (Pen dashPen = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                    {
                        g.DrawLine(dashPen, r.X + 12, by + bandH / 2, r.X + 20, by + bandH / 2);
                    }
                }

                // Connector lines
                using (Pen connPen = new Pen(Color.FromArgb(237, 125, 49), 1.5f))
                {
                    int topNodeBottomY = r.Y + 4 + bandH;
                    int midNodeTopY = r.Y + 4 + bandH + gap;
                    int midNodeBottomY = midNodeTopY + bandH;
                    int botNodeTopY = r.Y + 4 + (bandH + gap) * 2;

                    int topX = r.X + r.Width - 40;
                    int midX1 = topX - 32;
                    int midX2 = topX + 16;

                    g.DrawLine(connPen, topX, topNodeBottomY - 4, topX, midNodeTopY + 4);
                    g.DrawLine(connPen, midX1, midNodeTopY - 2, midX2, midNodeTopY - 2);
                    g.DrawLine(connPen, midX1, midNodeTopY - 2, midX1, midNodeTopY + 4);
                    g.DrawLine(connPen, midX2, midNodeTopY - 2, midX2, midNodeTopY + 4);

                    int botX1 = midX1 - 16;
                    int botX2 = midX1 + 14;
                    int botX3 = midX2 + 14;
                    g.DrawLine(connPen, midX1, midNodeBottomY - 4, midX1, botNodeTopY + 4);
                    g.DrawLine(connPen, botX1, botNodeTopY - 2, botX3, botNodeTopY - 2);
                    g.DrawLine(connPen, botX1, botNodeTopY - 2, botX1, botNodeTopY + 4);
                    g.DrawLine(connPen, botX2, botNodeTopY - 2, botX2, botNodeTopY + 4);
                    g.DrawLine(connPen, botX3, botNodeTopY - 2, botX3, botNodeTopY + 4);
                }

                // Tier 1 (Top Blue)
                int nodeW = 38;
                int nodeH = 26;
                int topNodeX = r.X + r.Width - 40 - nodeW / 2;
                int topNodeY = r.Y + 4 + (bandH - nodeH) / 2;
                DrawGradientNode(g, new Rectangle(topNodeX, topNodeY, nodeW, nodeH),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

                // Tier 2 (Middle Orange)
                int midY = r.Y + 4 + bandH + gap + (bandH - nodeH) / 2;
                int mid1X = r.X + r.Width - 40 - 32 - nodeW / 2;
                int mid2X = r.X + r.Width - 40 + 16 - nodeW / 2;
                DrawGradientNode(g, new Rectangle(mid1X, midY, nodeW, nodeH),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);
                DrawGradientNode(g, new Rectangle(mid2X, midY, nodeW, nodeH),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);

                // Tier 3 (Bottom Gray)
                int botY = r.Y + 4 + (bandH + gap) * 2 + (bandH - nodeH) / 2;
                int bot1X = mid1X - 16;
                int bot2X = mid1X + 14;
                int bot3X = mid2X + 14;
                int botW = 32;
                DrawGradientNode(g, new Rectangle(bot1X, botY, botW, nodeH),
                    Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), 4, true);
                DrawGradientNode(g, new Rectangle(bot2X, botY, botW, nodeH),
                    Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), 4, true);
                DrawGradientNode(g, new Rectangle(bot3X, botY, botW, nodeH),
                    Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), 4, true);
            }
            else if (typeId.Contains("Horizontal"))
            {
                // Horizontal hierarchy from left to right
                int nw = 46;
                int nh = 26;
                int ry = r.Y + (r.Height - nh) / 2;
                // Root on left
                DrawGradientNode(g, new Rectangle(r.X + 12, ry, nw, nh),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

                // Bus stem
                int midX = r.X + 78;
                using (Pen p = new Pen(Color.FromArgb(91, 155, 213), 1.5f))
                {
                    g.DrawLine(p, r.X + 12 + nw, ry + nh / 2, midX, ry + nh / 2);
                    g.DrawLine(p, midX, r.Y + 24, midX, r.Bottom - 24);
                }

                // 3 middle nodes
                int[] cys = new int[] { r.Y + 18, ry, r.Bottom - 44 };
                for (int k = 0; k < 3; k++)
                {
                    using (Pen p = new Pen(Color.FromArgb(237, 125, 49), 1.5f))
                    {
                        g.DrawLine(p, midX, cys[k] + nh / 2, midX + 16, cys[k] + nh / 2);
                    }
                    DrawGradientNode(g, new Rectangle(midX + 16, cys[k], nw, nh),
                        Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);

                    // Leaves on right
                    int leafX = midX + 16 + nw + 16;
                    using (Pen p = new Pen(Color.FromArgb(112, 173, 71), 1.5f))
                    {
                        g.DrawLine(p, midX + 16 + nw, cys[k] + nh / 2, leafX, cys[k] + nh / 2);
                    }
                    DrawGradientNode(g, new Rectangle(leafX, cys[k] + 2, 40, nh - 4),
                        Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 3, true);
                }
            }
            else if (typeId.Contains("Architecture"))
            {
                // Architecture layout (stacked from bottom up)
                // Foundation (bottom)
                DrawGradientNode(g, new Rectangle(r.X + 14, r.Bottom - 40, r.Width - 28, 30),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

                // Platform (middle 2 blocks)
                int pw = (r.Width - 36) / 2;
                DrawGradientNode(g, new Rectangle(r.X + 14, r.Bottom - 78, pw, 28),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);
                DrawGradientNode(g, new Rectangle(r.X + 22 + pw, r.Bottom - 78, pw, 28),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);

                // Applications (top 3 blocks)
                int tw = (r.Width - 44) / 3;
                for (int k = 0; k < 3; k++)
                {
                    DrawGradientNode(g, new Rectangle(r.X + 14 + k * (tw + 8), r.Y + 28, tw, 26),
                        Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 4, true);
                }
            }
            else if (typeId.Contains("HalfCircle"))
            {
                // Half circle org
                int cx = r.X + r.Width / 2;
                DrawGradientNode(g, new Rectangle(cx - 26, r.Y + 14, 52, 26),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

                Color[] nodeColors = new Color[] {
                    Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(112, 48, 160), Color.FromArgb(255, 192, 0), Color.FromArgb(0, 176, 240)
                };
                for (int k = 0; k < 5; k++)
                {
                    double ang = Math.PI * 0.12 + k * (Math.PI * 0.76 / 4.0);
                    int nx = (int)(cx + 72 * Math.Cos(ang));
                    int ny = (int)(r.Y + 36 + 74 * Math.Sin(ang));
                    using (Pen p = new Pen(nodeColors[k], 1.5f))
                    {
                        g.DrawLine(p, cx, r.Y + 40, nx, ny);
                    }
                    DrawGradientNode(g, new Rectangle(nx - 18, ny - 12, 36, 24),
                        nodeColors[k], Color.FromArgb(nodeColors[k].R / 2, nodeColors[k].G / 2, nodeColors[k].B / 2), Color.DarkGray, 4, true);
                }
            }
            else if (typeId.Contains("Table"))
            {
                // Table hierarchy
                DrawGradientNode(g, new Rectangle(r.X + 12, r.Y + 16, r.Width - 24, 28),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

                int colW = (r.Width - 32) / 3;
                for (int k = 0; k < 3; k++)
                {
                    int colX = r.X + 12 + k * (colW + 4);
                    DrawGradientNode(g, new Rectangle(colX, r.Y + 50, colW, 26),
                        Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 3, true);
                    DrawGradientNode(g, new Rectangle(colX, r.Y + 80, colW, 22),
                        Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), 3, true);
                    DrawGradientNode(g, new Rectangle(colX, r.Y + 106, colW, 22),
                        Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), 3, true);
                }
            }
            else
            {
                // Classic Org Chart with assistant node
                int nw = 54;
                int nh = 26;
                int rx = r.X + (r.Width - nw) / 2;
                // Executive root node
                DrawGradientNode(g, new Rectangle(rx, r.Y + 14, nw, nh),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);

                // Stem down
                int midY = r.Y + 68;
                using (Pen p = new Pen(Color.FromArgb(91, 155, 213), 1.5f))
                {
                    g.DrawLine(p, r.X + r.Width / 2, r.Y + 40, r.X + r.Width / 2, midY);
                    // Assistant branch
                    g.DrawLine(p, r.X + r.Width / 2, r.Y + 52, r.X + 38, r.Y + 52);
                }
                // Assistant node
                DrawGradientNode(g, new Rectangle(r.X + 12, r.Y + 42, 42, 22),
                    Color.FromArgb(255, 192, 0), Color.FromArgb(197, 144, 0), Color.FromArgb(150, 110, 0), 3, true);

                // Horizontal bus line
                using (Pen p = new Pen(Color.FromArgb(237, 125, 49), 1.5f))
                {
                    g.DrawLine(p, r.X + 22, midY, r.Right - 22, midY);
                }

                // 3 department managers
                int mw = 46;
                int mh = 24;
                int[] mxs = new int[] { r.X + 12, r.X + (r.Width - mw) / 2, r.Right - 12 - mw };
                for (int k = 0; k < 3; k++)
                {
                    using (Pen p = new Pen(Color.FromArgb(237, 125, 49), 1.5f))
                    {
                        g.DrawLine(p, mxs[k] + mw / 2, midY, mxs[k] + mw / 2, midY + 10);
                    }
                    DrawGradientNode(g, new Rectangle(mxs[k], midY + 10, mw, mh),
                        Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 3, true);

                    // Sub-node below
                    using (Pen p = new Pen(Color.FromArgb(112, 173, 71), 1.5f))
                    {
                        g.DrawLine(p, mxs[k] + mw / 2, midY + 10 + mh, mxs[k] + mw / 2, midY + 10 + mh + 10);
                    }
                    DrawGradientNode(g, new Rectangle(mxs[k] + 4, midY + 20 + mh, mw - 8, mh - 6),
                        Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 3, true);
                }
            }
        }

        private void DrawPicturePreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("Grid"))
            {
                // 2x2 picture grid
                int pw = (r.Width - 36) / 2;
                int ph = (r.Height - 36) / 2;
                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        int px = r.X + 12 + col * (pw + 12);
                        int py = r.Y + 12 + row * (ph + 12);
                        DrawPicturePlaceholder(g, new Rectangle(px, py, pw, ph), false);
                    }
                }
            }
            else if (typeId.Contains("Strips") || typeId.Contains("Lineup") || typeId.Contains("Horizontal"))
            {
                // 3 horizontal strips with picture on left
                int sh = 40;
                int gap = 10;
                Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
                Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
                Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

                for (int k = 0; k < 3; k++)
                {
                    int sy = r.Y + 16 + k * (sh + gap);
                    DrawPicturePlaceholder(g, new Rectangle(r.X + 12, sy, sh, sh), false);
                    DrawGradientNode(g, new Rectangle(r.X + 16 + sh, sy + 4, r.Right - r.X - 28 - sh, sh - 8),
                        tops[k], bots[k], borders[k], 4, true);
                }
            }
            else
            {
                // Large accented picture
                DrawPicturePlaceholder(g, new Rectangle(r.X + 14, r.Y + 18, 90, 80), false);
                DrawGradientNode(g, new Rectangle(r.X + 114, r.Y + 18, r.Right - r.X - 128, 28),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);
                DrawGradientNode(g, new Rectangle(r.X + 114, r.Y + 54, r.Right - r.X - 128, 28),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);
                DrawGradientNode(g, new Rectangle(r.X + 14, r.Y + 108, r.Width - 28, 32),
                    Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 4, true);
            }
        }

        private void DrawCyclePreview(Graphics g, Rectangle r, string typeId)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            if (typeId.Contains("Radial"))
            {
                // Central hub + 5 satellites
                DrawGradientNode(g, new Rectangle(cx - 24, cy - 24, 48, 48),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 24, true);

                Color[] colors = new Color[] {
                    Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0), Color.FromArgb(112, 48, 160), Color.FromArgb(0, 176, 240)
                };
                for (int k = 0; k < 5; k++)
                {
                    double ang = k * Math.PI * 2.0 / 5.0 - Math.PI / 2.0;
                    float sx = (float)(cx + 62 * Math.Cos(ang));
                    float sy = (float)(cy + 62 * Math.Sin(ang));
                    using (Pen p = new Pen(colors[k], 1.5f))
                    {
                        g.DrawLine(p, cx, cy, sx, sy);
                    }
                    DrawGradientNode(g, new Rectangle((int)sx - 15, (int)sy - 15, 30, 30),
                        colors[k], Color.FromArgb(colors[k].R / 2, colors[k].G / 2, colors[k].B / 2), Color.DarkGray, 15, true);
                }
            }
            else if (typeId.Contains("Pie"))
            {
                // Exploded pie chart
                int pr = 54;
                Color[] colors = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0) };
                for (int k = 0; k < 4; k++)
                {
                    double midAng = (k * 90 + 45) * Math.PI / 180.0;
                    float ox = (float)(6 * Math.Cos(midAng));
                    float oy = (float)(6 * Math.Sin(midAng));
                    Rectangle pieRect = new Rectangle((int)(cx + ox - pr), (int)(cy + oy - pr), pr * 2, pr * 2);
                    using (SolidBrush b = new SolidBrush(colors[k]))
                    {
                        g.FillPie(b, pieRect, k * 90, 90);
                    }
                    g.DrawPie(Pens.White, pieRect, k * 90, 90);
                }
            }
            else
            {
                // Word Nondirectional Cycle & Ring: Circular outline ring + uniform Office Blue rounded rectangles
                int ringRad = 46;
                using (Pen p = new Pen(Color.FromArgb(68, 114, 196), 1.8f))
                {
                    g.DrawEllipse(p, cx - ringRad, cy - ringRad, ringRad * 2, ringRad * 2);
                }

                Color topC = Color.FromArgb(68, 114, 196);
                Color botC = Color.FromArgb(43, 87, 154);
                Color brdC = Color.FromArgb(31, 73, 125);
                int count = 5;
                for (int k = 0; k < count; k++)
                {
                    double ang = k * Math.PI * 2.0 / count - Math.PI / 2.0;
                    float sx = (float)(cx + ringRad * Math.Cos(ang));
                    float sy = (float)(cy + ringRad * Math.Sin(ang));
                    DrawGradientNode(g, new Rectangle((int)sx - 18, (int)sy - 11, 36, 22),
                        topC, botC, brdC, 4, true);
                }
            }
        }

        private void DrawProcessPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("Bending"))
            {
                // S-curved pipeline process
                int bw = 46;
                int bh = 32;
                DrawGradientNode(g, new Rectangle(r.X + 16, r.Y + 22, bw, bh),
                    Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), 4, true);
                DrawGradientNode(g, new Rectangle(r.Right - 62, r.Y + 68, bw, bh),
                    Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), 4, true);
                DrawGradientNode(g, new Rectangle(r.X + 16, r.Bottom - 54, bw, bh),
                    Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.FromArgb(55, 86, 35), 4, true);

                using (Pen p = new Pen(Color.FromArgb(91, 155, 213), 2.5f))
                {
                    g.DrawLine(p, r.X + 16 + bw, r.Y + 38, r.Right - 39, r.Y + 38);
                    g.DrawLine(p, r.Right - 39, r.Y + 38, r.Right - 39, r.Y + 68);
                    g.DrawLine(p, r.Right - 62, r.Y + 84, r.X + 39, r.Y + 84);
                    g.DrawLine(p, r.X + 39, r.Y + 84, r.X + 39, r.Bottom - 54);
                }
            }
            else
            {
                // 3 rounded rectangular blocks with arrow connectors
                int bw = 52;
                int bh = 46;
                int by = r.Y + (r.Height - bh) / 2;
                Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
                Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
                Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

                for (int k = 0; k < 3; k++)
                {
                    int bx = r.X + 14 + k * (bw + 20);
                    DrawGradientNode(g, new Rectangle(bx, by, bw, bh), tops[k], bots[k], borders[k], 6, true);

                    if (k < 2)
                    {
                        PointF[] arrow = new PointF[] {
                            new PointF(bx + bw + 4, by + bh / 2 - 6),
                            new PointF(bx + bw + 15, by + bh / 2),
                            new PointF(bx + bw + 4, by + bh / 2 + 6)
                        };
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 120, 120)))
                        {
                            g.FillPolygon(b, arrow);
                        }
                    }
                }
            }
        }

        private void DrawRelationshipPreview(Graphics g, Rectangle r, string typeId)
        {
            // Obsolete, replaced by exact dispatch
        }

        private void DrawOfficeSpecificPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("CircleProcess"))
            {
                // Numbered circle process
                int count = 4;
                int cr = 18;
                int stepX = (r.Width - 40) / count;
                for (int k = 0; k < count; k++)
                {
                    int cx = r.X + 24 + k * stepX;
                    int cy = r.Y + r.Height / 2;
                    DrawGradientNode(g, new Rectangle(cx - cr, cy - cr, cr * 2, cr * 2),
                        Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.FromArgb(31, 78, 121), cr, false);
                    using (Font f = new Font("Segoe UI", 10.0f, FontStyle.Bold))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString((k + 1).ToString(), f, Brushes.White, new RectangleF(cx - cr, cy - cr, cr * 2, cr * 2), sf);
                    }
                    if (k < count - 1)
                    {
                        using (Pen p = new Pen(Color.FromArgb(91, 155, 213), 2.0f))
                        {
                            g.DrawLine(p, cx + cr, cy, cx + stepX - cr, cy);
                        }
                    }
                }
            }
            else
            {
                DrawHierarchyPreview(g, r, typeId);
            }
        }

        private void DrawListPreview(Graphics g, Rectangle r, string typeId)
        {
            if (typeId.Contains("Trapezoid"))
            {
                // 3 stacked trapezoids
                int th = 34;
                int gap = 8;
                Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
                Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
                Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

                for (int k = 0; k < 3; k++)
                {
                    int ty = r.Y + 22 + k * (th + gap);
                    PointF[] trap = new PointF[] {
                        new PointF(r.X + 16 + k * 8, ty),
                        new PointF(r.Right - 16 - k * 8, ty),
                        new PointF(r.Right - 24 - k * 8, ty + th),
                        new PointF(r.X + 24 + k * 8, ty + th)
                    };
                    using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(r.X + 16, ty, r.Width - 32, th), tops[k], bots[k], LinearGradientMode.Vertical))
                    {
                        g.FillPolygon(lgb, trap);
                    }
                    using (Pen p = new Pen(borders[k], 1.5f))
                    {
                        g.DrawPolygon(p, trap);
                    }
                }
            }
            else
            {
                // 3 colorful rectangular blocks with title bar and text dashes
                int count = 3;
                int bh = 36;
                int gap = 10;
                Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
                Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
                Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

                for (int k = 0; k < count; k++)
                {
                    int by = r.Y + 20 + k * (bh + gap);
                    Rectangle blockRect = new Rectangle(r.X + 16, by, r.Width - 32, bh);
                    DrawGradientNode(g, blockRect, tops[k], bots[k], borders[k], 4, true);
                }
            }
        }


        private void DrawPreview_IncreasingCircles(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(91, 155, 213)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(237, 125, 49)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(112, 173, 71)))
            {
                g.FillEllipse(b1, cx - 60, cy - 10, 20, 20);
                g.FillEllipse(b2, cx - 30, cy - 15, 30, 30);
                g.FillEllipse(b3, cx + 10, cy - 20, 40, 40);
            }
        }

        private void DrawPreview_PlusMinus(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 20, r.Width / 2 - 25, 40), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
            DrawGradientNode(g, new Rectangle(r.X + r.Width / 2 + 5, r.Y + 20, r.Width / 2 - 25, 40), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.Black, 2, false);
            using (Font f = new Font("Segoe UI", 16, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.White))
            {
                g.DrawString("+", f, b, r.X + 35, r.Y + 25);
                g.DrawString("-", f, b, r.X + r.Width / 2 + 20, r.Y + 25);
            }
        }

        private void DrawPreview_Reverse(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 30, 60, 40), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 5, false);
            DrawGradientNode(g, new Rectangle(r.X + 120, r.Y + 30, 60, 40), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.Black, 5, false);
            using (Pen p = new Pen(Color.Gray, 3))
            {
                g.DrawLine(p, r.X + 85, r.Y + 40, r.X + 115, r.Y + 40);
                g.DrawLine(p, r.X + 85, r.Y + 60, r.X + 115, r.Y + 60);
            }
        }

        private void DrawPreview_Stacked(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 15 + i * 25;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
                {
                    g.FillEllipse(b, r.X + 20, y, 20, 20);
                }
                DrawGradientNode(g, new Rectangle(r.X + 45, y, 100, 20), Color.FromArgb(225, 225, 225), Color.LightGray, Color.Gray, 2, false);
            }
        }

        private void DrawPreview_Lined(Graphics g, Rectangle r)
        {
            using (Pen p = new Pen(Color.Gray, 2))
            {
                for (int i = 0; i < 4; i++)
                {
                    int y = r.Y + 20 + i * 20;
                    g.DrawLine(p, r.X + 20, y, r.X + 180, y);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
                    {
                        g.FillRectangle(b, r.X + 25, y + 2, 80, 10);
                    }
                }
            }
        }

        private void DrawPreview_Grouped(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 20, 70, 70), Color.LightGray, Color.Gray, Color.DarkGray, 5, false);
            DrawGradientNode(g, new Rectangle(r.X + 110, r.Y + 20, 70, 70), Color.LightGray, Color.Gray, Color.DarkGray, 5, false);
            for(int i=0; i<2; i++)
            {
                for(int j=0; j<3; j++)
                {
                    DrawGradientNode(g, new Rectangle(r.X + 25 + i*90, r.Y + 25 + j*20, 60, 15), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
                }
            }
        }

        private void DrawPreview_SquareAccent(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 20 + i * 30;
                using (SolidBrush b = new SolidBrush(i == 0 ? Color.FromArgb(91, 155, 213) : (i == 1 ? Color.FromArgb(237, 125, 49) : Color.FromArgb(112, 173, 71))))
                {
                    g.FillRectangle(b, r.X + 20, y, 20, 20);
                }
                DrawGradientNode(g, new Rectangle(r.X + 45, y, 120, 20), Color.FromArgb(240, 240, 240), Color.LightGray, Color.Gray, 0, false);
            }
        }

        private void DrawPreview_VaryingWidth(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 20, 140, 20), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 50, 100, 20), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.Black, 2, false);
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 80, 160, 20), Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.Black, 2, false);
        }

        private void DrawPreview_CircleArrow(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int radius = 54;

            // Thick circular arrow arc
            using (Pen p = new Pen(Color.FromArgb(180, 205, 230), 6.0f))
            {
                g.DrawArc(p, cx - radius, cy - radius, radius * 2, radius * 2, 20, 310);
            }

            // Arrow tip on arc
            double arrowAng = 330 * Math.PI / 180.0;
            float atx = (float)(cx + radius * Math.Cos(arrowAng));
            float aty = (float)(cy + radius * Math.Sin(arrowAng));
            PointF[] arrowTip = new PointF[] {
                new PointF(atx + 8, aty - 6),
                new PointF(atx + 12, aty + 6),
                new PointF(atx - 2, aty + 2)
            };
            using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
                g.FillPolygon(b, arrowTip);

            // 4 numbered circular stage nodes
            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53), Color.FromArgb(197, 144, 0) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35), Color.FromArgb(150, 110, 0) };

            for (int i = 0; i < 4; i++)
            {
                double ang = i * Math.PI / 2.0 - Math.PI / 4.0;
                int nx = (int)(cx + radius * Math.Cos(ang));
                int ny = (int)(cy + radius * Math.Sin(ang));
                Rectangle nr = new Rectangle(nx - 16, ny - 16, 32, 32);

                DrawGradientNode(g, nr, tops[i], bots[i], borders[i], 16, false);
                using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString((i + 1).ToString(), f, Brushes.White, nr, sf);
                }
            }
        }

        private void DrawPreview_Phased(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int x = r.X + 10 + i * 60;
                DrawGradientNode(g, new Rectangle(x, r.Y + 30, 50, 50), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 3, false);
                using (Pen p = new Pen(Color.Gray, 2))
                {
                    g.DrawLine(p, x + 10, r.Y + 90, x + 40, r.Y + 90);
                }
            }
        }

        private void DrawPreview_Pie(Graphics g, Rectangle r)
        {
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(91, 155, 213)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(237, 125, 49)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(112, 173, 71)))
            {
                g.FillPie(b1, r.X + 50, r.Y + 20, 100, 100, 0, 120);
                g.FillPie(b2, r.X + 45, r.Y + 15, 100, 100, 120, 120);
                g.FillPie(b3, r.X + 55, r.Y + 15, 100, 100, 240, 120);
            }
        }

        private void DrawPreview_VerticalProcess(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 10 + i * 30;
                DrawGradientNode(g, new Rectangle(r.X + 50, y, 100, 20), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
                if (i < 2)
                {
                    using (SolidBrush b = new SolidBrush(Color.Gray))
                    {
                        g.FillPolygon(b, new PointF[] { new PointF(r.X + 100, y + 20), new PointF(r.X + 95, y + 28), new PointF(r.X + 105, y + 28) });
                    }
                }
            }
        }

        private void DrawPreview_ContinuousArrow(Graphics g, Rectangle r)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
            {
                g.FillPolygon(b, new PointF[] { 
                    new PointF(r.X + 20, r.Y + 40), 
                    new PointF(r.X + 160, r.Y + 40), 
                    new PointF(r.X + 160, r.Y + 30),
                    new PointF(r.X + 190, r.Y + 50),
                    new PointF(r.X + 160, r.Y + 70),
                    new PointF(r.X + 160, r.Y + 60),
                    new PointF(r.X + 20, r.Y + 60)
                });
            }
            using (SolidBrush wt = new SolidBrush(Color.White))
            using (Font f = new Font("Segoe UI", 8))
            {
                g.DrawString("Step 1 -> Step 2 -> Step 3", f, wt, r.X + 30, r.Y + 42);
            }
        }

        private void DrawPreview_SegmentedPyramid(Graphics g, Rectangle r)
        {
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(91, 155, 213)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(237, 125, 49)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(112, 173, 71)))
            {
                g.FillPolygon(b1, new PointF[] { new PointF(r.X + 100, r.Y + 10), new PointF(r.X + 85, r.Y + 40), new PointF(r.X + 115, r.Y + 40) });
                g.FillPolygon(b2, new PointF[] { new PointF(r.X + 80, r.Y + 45), new PointF(r.X + 120, r.Y + 45), new PointF(r.X + 135, r.Y + 75), new PointF(r.X + 65, r.Y + 75) });
                g.FillPolygon(b3, new PointF[] { new PointF(r.X + 60, r.Y + 80), new PointF(r.X + 140, r.Y + 80), new PointF(r.X + 155, r.Y + 110), new PointF(r.X + 45, r.Y + 110) });
            }
        }

        
        private void DrawPreview_VerticalPictureStrips(Graphics g, Rectangle r)
        {
            int colW = 48;
            int gap = 12;
            int startX = r.X + (r.Width - (colW * 3 + gap * 2)) / 2;
            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

            for (int i = 0; i < 3; i++)
            {
                int x = startX + i * (colW + gap);
                // Photo on upper 70%
                Rectangle photoRect = new Rectangle(x, r.Y + 16, colW, 88);
                DrawPicturePlaceholder(g, photoRect, false);
                // Caption bar on lower 30%
                Rectangle capRect = new Rectangle(x, r.Y + 104, colW, 30);
                DrawGradientNode(g, capRect, tops[i], bots[i], borders[i], 3, true);
            }
        }

        private void DrawPreview_Spiral(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 4; i++)
            {
                int x = r.X + 80 + (int)(Math.Cos(i * 1.5) * i * 15);
                int y = r.Y + 50 + (int)(Math.Sin(i * 1.5) * i * 15);
                DrawPicturePlaceholder(g, new Rectangle(x, y, 30, 30), false);
            }
        }


        private void DrawPreview_AlternatingBlocks(Graphics g, Rectangle r)
        {
            int rowH = 38;
            int gap = 8;
            Color[] tops = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(112, 173, 71) };
            Color[] bots = new Color[] { Color.FromArgb(43, 91, 136), Color.FromArgb(198, 89, 17), Color.FromArgb(83, 129, 53) };
            Color[] borders = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(162, 70, 11), Color.FromArgb(55, 86, 35) };

            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 12 + i * (rowH + gap);
                int photoW = 46;
                int textW = r.Width - photoW - 36;
                bool photoLeft = (i % 2 == 0);

                Rectangle photoRect = photoLeft 
                    ? new Rectangle(r.X + 12, y, photoW, rowH)
                    : new Rectangle(r.Right - 12 - photoW, y, photoW, rowH);
                Rectangle textRect = photoLeft
                    ? new Rectangle(r.X + 16 + photoW, y, textW, rowH)
                    : new Rectangle(r.X + 12, y, textW, rowH);

                DrawPicturePlaceholder(g, photoRect, false);
                DrawGradientNode(g, textRect, tops[i], bots[i], borders[i], 4, true);
            }
        }
        // =========================================================================
        // VECTOR GRAPHICS HELPERS (C# 5 COMPATIBLE)
        // =========================================================================

        private void DrawGradientNode(Graphics g, Rectangle r, Color cTop, Color cBot, Color cBorder, int radius, bool drawDash)
        {
            using (GraphicsPath path = GetRoundedPath(r, radius))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, cTop, cBot, LinearGradientMode.Vertical))
                {
                    g.FillPath(lgb, path);
                }
                using (Pen p = new Pen(cBorder, 1.0f))
                {
                    g.DrawPath(p, path);
                }
            }
            if (drawDash)
            {
                using (Pen whitePen = new Pen(Color.FromArgb(230, 255, 255, 255), 2.0f))
                {
                    whitePen.StartCap = LineCap.Round;
                    whitePen.EndCap = LineCap.Round;
                    g.DrawLine(whitePen, r.X + 8, r.Y + r.Height / 2, r.Right - 8, r.Y + r.Height / 2);
                }
            }
        }

        private void DrawGear(Graphics g, float cx, float cy, float outerR, float innerR, int teeth, Color cTop, Color cBot, Color cBorder)
        {
            int totalPoints = teeth * 4;
            PointF[] pts = new PointF[totalPoints];
            double step = Math.PI * 2.0 / totalPoints;

            for (int i = 0; i < totalPoints; i++)
            {
                double ang = i * step;
                float r = (i % 4 == 1 || i % 4 == 2) ? outerR : innerR;
                pts[i] = new PointF((float)(cx + r * Math.Cos(ang)), (float)(cy + r * Math.Sin(ang)));
            }

            RectangleF gearBounds = new RectangleF(cx - outerR, cy - outerR, outerR * 2, outerR * 2);
            using (LinearGradientBrush lgb = new LinearGradientBrush(gearBounds, cTop, cBot, LinearGradientMode.ForwardDiagonal))
            {
                g.FillPolygon(lgb, pts);
            }
            using (Pen p = new Pen(cBorder, 1.2f))
            {
                g.DrawPolygon(p, pts);
            }

            // Axle hole with metallic cap
            float holeR = innerR * 0.45f;
            RectangleF holeBounds = new RectangleF(cx - holeR, cy - holeR, holeR * 2, holeR * 2);
            using (LinearGradientBrush lgb = new LinearGradientBrush(holeBounds, Color.FromArgb(240, 240, 240), Color.FromArgb(160, 160, 160), LinearGradientMode.Vertical))
            {
                g.FillEllipse(lgb, holeBounds);
            }
            using (Pen p = new Pen(cBorder, 1.0f))
            {
                g.DrawEllipse(p, holeBounds);
            }
        }

        private void DrawHexagon(Graphics g, float cx, float cy, float radius, Color cTop, Color cBot, Color cBorder)
        {
            PointF[] pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double ang = i * Math.PI / 3.0;
                pts[i] = new PointF((float)(cx + radius * Math.Cos(ang)), (float)(cy + radius * Math.Sin(ang)));
            }

            RectangleF hBounds = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
            using (LinearGradientBrush lgb = new LinearGradientBrush(hBounds, cTop, cBot, LinearGradientMode.Vertical))
            {
                g.FillPolygon(lgb, pts);
            }
            using (Pen p = new Pen(cBorder, 1.2f))
            {
                g.DrawPolygon(p, pts);
            }
            using (Pen whitePen = new Pen(Color.FromArgb(230, 255, 255, 255), 2.0f))
            {
                whitePen.StartCap = LineCap.Round;
                whitePen.EndCap = LineCap.Round;
                g.DrawLine(whitePen, cx - radius * 0.5f, cy, cx + radius * 0.5f, cy);
            }
        }

        private void DrawChevronShape(Graphics g, Rectangle r, Color cTop, Color cBot, Color cBorder, int notch)
        {
            PointF[] pts = new PointF[] {
                new PointF(r.X, r.Y),
                new PointF(r.Right - notch, r.Y),
                new PointF(r.Right, r.Y + r.Height / 2f),
                new PointF(r.Right - notch, r.Bottom),
                new PointF(r.X, r.Bottom),
                new PointF(r.X + notch, r.Y + r.Height / 2f)
            };

            using (LinearGradientBrush lgb = new LinearGradientBrush(r, cTop, cBot, LinearGradientMode.Vertical))
            {
                g.FillPolygon(lgb, pts);
            }
            using (Pen p = new Pen(cBorder, 1.5f))
            {
                g.DrawPolygon(p, pts);
            }

            using (Pen whitePen = new Pen(Color.FromArgb(230, 255, 255, 255), 2.0f))
            {
                whitePen.StartCap = LineCap.Round;
                whitePen.EndCap = LineCap.Round;
                g.DrawLine(whitePen, r.X + notch + 8, r.Y + r.Height / 2 - 4, r.Right - notch - 4, r.Y + r.Height / 2 - 4);
                g.DrawLine(whitePen, r.X + notch + 8, r.Y + r.Height / 2 + 4, r.Right - notch - 12, r.Y + r.Height / 2 + 4);
            }
        }

        private void DrawPicturePlaceholder(Graphics g, Rectangle r, bool isCircle)
        {
            // Drop shadow
            Rectangle shadowRect = new Rectangle(r.X + 2, r.Y + 2, r.Width, r.Height);
            if (isCircle)
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    g.FillEllipse(sb, shadowRect);
            }
            else
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    g.FillRectangle(sb, shadowRect);
            }

            // Sky gradient
            using (LinearGradientBrush skyBrush = new LinearGradientBrush(r, Color.FromArgb(180, 215, 245), Color.FromArgb(220, 235, 250), LinearGradientMode.Vertical))
            {
                if (isCircle) g.FillEllipse(skyBrush, r);
                else g.FillRectangle(skyBrush, r);
            }

            // Sun
            g.FillEllipse(Brushes.Gold, r.Right - r.Width * 0.35f, r.Y + r.Height * 0.15f, r.Width * 0.2f, r.Width * 0.2f);

            // Mountain peaks
            PointF[] mtn1 = new PointF[] {
                new PointF(r.X + r.Width * 0.05f, r.Bottom),
                new PointF(r.X + r.Width * 0.45f, r.Y + r.Height * 0.45f),
                new PointF(r.X + r.Width * 0.85f, r.Bottom)
            };
            using (SolidBrush mtnBrush1 = new SolidBrush(Color.FromArgb(90, 135, 80)))
            {
                g.FillPolygon(mtnBrush1, mtn1);
            }

            PointF[] mtn2 = new PointF[] {
                new PointF(r.X + r.Width * 0.35f, r.Bottom),
                new PointF(r.X + r.Width * 0.7f, r.Y + r.Height * 0.55f),
                new PointF(r.Right, r.Bottom)
            };
            using (SolidBrush mtnBrush2 = new SolidBrush(Color.FromArgb(120, 165, 100)))
            {
                g.FillPolygon(mtnBrush2, mtn2);
            }

            // Border
            using (Pen p = new Pen(Color.FromArgb(160, 180, 200), 1.2f))
            {
                if (isCircle) g.DrawEllipse(p, r);
                else g.DrawRectangle(p, r);
            }
        }

        private void FillRoundedRectangle(Graphics g, Color color, Rectangle r, int radius)
        {
            using (GraphicsPath path = GetRoundedPath(r, radius))
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPath(b, path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void RenderDialog(Graphics g, int width, int height)
        {
            // Headless render method for testing / validation
            g.Clear(Color.FromArgb(246, 248, 250));
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw left nav
            Rectangle leftR = new Rectangle(16, 16, 150, height - 84);
            using (SolidBrush leftBg = new SolidBrush(Color.White))
            {
                g.FillRectangle(leftBg, leftR);
            }
            g.DrawRectangle(new Pen(Color.FromArgb(200, 205, 215), 1), leftR);
            int itemH = 34;
            for (int i = 0; i < categoryDisplayNames.Count; i++)
            {
                Rectangle itemRect = new Rectangle(leftR.X, leftR.Y + i * itemH, leftR.Width, itemH);
                bool isSelected = (i == selectedCategoryIndex);
                if (isSelected)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(204, 232, 255)))
                        g.FillRectangle(b, itemRect);
                    using (Pen p = new Pen(Color.FromArgb(0, 120, 215), 3.0f))
                        g.DrawLine(p, leftR.X + 2, itemRect.Top + 2, leftR.X + 2, itemRect.Bottom - 2);
                }
                DrawCategoryIcon(g, leftR.X + 10, itemRect.Top + 8, categories[i], isSelected);
                using (Font f = new Font("Segoe UI", 9.0f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(isSelected ? Color.FromArgb(0, 51, 102) : Color.FromArgb(40, 40, 40)))
                {
                    g.DrawString(categoryDisplayNames[i], f, tb, leftR.X + 34, itemRect.Top + 8);
                }
            }

            // Draw center grid
            Rectangle centerR = new Rectangle(176, 16, 340, height - 84);
            using (SolidBrush centerBg = new SolidBrush(Color.White))
            {
                g.FillRectangle(centerBg, centerR);
            }
            g.DrawRectangle(new Pen(Color.FromArgb(200, 205, 215), 1), centerR);
            for (int i = 0; i < filteredTemplates.Count; i++)
            {
                var tmpl = filteredTemplates[i];
                Rectangle b = new Rectangle(centerR.X + tmpl.Bounds.X, centerR.Y + tmpl.Bounds.Y, tmpl.Bounds.Width, tmpl.Bounds.Height);
                bool isSel = (i == selectedTemplateIndex);
                Color bg = isSel ? Color.FromArgb(240, 240, 240) : Color.White;
                using (SolidBrush bb = new SolidBrush(bg)) g.FillRectangle(bb, b);
                if (isSel)
                {
                    using (Pen outerPen = new Pen(Color.FromArgb(138, 136, 134), 1.5f))
                    using (Pen innerPen = new Pen(Color.FromArgb(138, 136, 134), 1.0f))
                    {
                        g.DrawRectangle(outerPen, b.X, b.Y, b.Width - 1, b.Height - 1);
                        g.DrawRectangle(innerPen, b.X + 2, b.Y + 2, b.Width - 5, b.Height - 5);
                    }
                }
                else
                {
                    using (Pen normPen = new Pen(Color.FromArgb(230, 230, 230), 1.0f))
                        g.DrawRectangle(normPen, b.X, b.Y, b.Width - 1, b.Height - 1);
                }
                Rectangle mini = new Rectangle(b.X + 6, b.Y + 6, b.Width - 12, b.Height - 12);
                DrawArchitecturalWireframe(g, mini, tmpl.TypeId);
            }

            // Draw right preview
            Rectangle rightR = new Rectangle(526, 16, width - 542, height - 84);
            using (SolidBrush rightBg = new SolidBrush(Color.White))
            {
                g.FillRectangle(rightBg, rightR);
            }
            g.DrawRectangle(new Pen(Color.FromArgb(200, 205, 215), 1), rightR);

            if (selectedTemplateIndex >= 0 && selectedTemplateIndex < filteredTemplates.Count)
            {
                var tmpl = filteredTemplates[selectedTemplateIndex];
                Rectangle previewArea = new Rectangle(rightR.X + 10, rightR.Y + 12, rightR.Width - 20, 180);
                DrawOfficePreviewDiagram(g, previewArea, tmpl.TypeId);
                g.DrawLine(new Pen(Color.FromArgb(225, 228, 235), 1), rightR.X + 12, rightR.Y + 202, rightR.Right - 12, rightR.Y + 202);
                using (Font titleF = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(0, 51, 102)))
                {
                    g.DrawString(tmpl.Name, titleF, tb, rightR.X + 12, rightR.Y + 210);
                }
                using (Font descF = new Font("Segoe UI", 8.5f))
                using (SolidBrush db = new SolidBrush(Color.FromArgb(80, 85, 95)))
                {
                    RectangleF descRect = new RectangleF(rightR.X + 12, rightR.Y + 236, rightR.Width - 24, rightR.Height - 244);
                    g.DrawString(tmpl.Description, descF, db, descRect);
                }
            }

            // Bottom bar
            int botY = height - 52;
            using (SolidBrush botBrush = new SolidBrush(Color.FromArgb(240, 242, 245)))
                g.FillRectangle(botBrush, 0, botY, width, 52);
            g.DrawLine(new Pen(Color.FromArgb(220, 224, 230), 1), 0, botY, width, botY);
            Rectangle okRect = new Rectangle(width - 210, botY + 11, 95, 30);
            using (SolidBrush okBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                g.FillRectangle(okBrush, okRect);
            using (Font btnFont = new Font("Segoe UI", 9.0f))
            {
                StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("OK", btnFont, Brushes.White, okRect, sfCenter);
                Rectangle cancelRect = new Rectangle(width - 105, botY + 11, 80, 30);
                using (SolidBrush cancelBrush = new SolidBrush(Color.White))
                    g.FillRectangle(cancelBrush, cancelRect);
                g.DrawRectangle(new Pen(Color.FromArgb(200, 205, 215), 1), cancelRect);
                g.DrawString("Cancel", btnFont, Brushes.Black, cancelRect, sfCenter);
            }
        }
    }
}
