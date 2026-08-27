using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class FunctionArgumentInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsOptional { get; set; }
        public string DefaultValue { get; set; }

        public FunctionArgumentInfo(string name, string desc, bool optional = false, string defVal = "")
        {
            Name = name;
            Description = desc;
            IsOptional = optional;
            DefaultValue = defVal;
        }
    }

    public class FunctionMetadata
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Syntax { get; set; }
        public List<FunctionArgumentInfo> Arguments { get; set; }

        public FunctionMetadata(string name, string category, string desc, string syntax, params FunctionArgumentInfo[] args)
        {
            Name = name;
            Category = category;
            Description = desc;
            Syntax = syntax;
            Arguments = new List<FunctionArgumentInfo>(args);
        }
    }

    /// <summary>
    /// Authentic Excel "Insert Function" ($fx$) and "Function Arguments" Dialog
    /// </summary>
    public class InsertFunctionDialog : Form
    {
        private CalcView _calcView;
        private List<FunctionMetadata> _allFunctions;
        private List<FunctionMetadata> _filteredFunctions;

        // UI Controls - Step 1: Select Function
        private Panel pnlSelect;
        private TextBox txtSearch;
        private ComboBox cboCategory;
        private ListBox lstFunctions;
        private Label lblSyntax;
        private Label lblDescription;
        private Button btnNextArgs;

        // UI Controls - Step 2: Function Arguments
        private Panel pnlArgs;
        private Label lblArgsTitle;
        private Label lblArgsSyntax;
        private Panel pnlArgsInputs;
        private List<TextBox> _argTextBoxes = new List<TextBox>();
        private Label lblArgDesc;
        private Label lblFormulaResult;
        private Button btnBack;
        private Button btnOk;
        private Button btnCancel;

        private FunctionMetadata _selectedFunc;

        public string ResultFormula { get; private set; }

        public InsertFunctionDialog(CalcView calcView, string preselectedFunction = null)
        {
            _calcView = calcView;
            InitFunctionDatabase();

            InitializeComponent();

            if (!string.IsNullOrEmpty(preselectedFunction))
            {
                FunctionMetadata fm = _allFunctions.Find(f => f.Name.Equals(preselectedFunction, StringComparison.OrdinalIgnoreCase));
                if (fm != null)
                {
                    ShowArgumentsPanel(fm);
                }
            }
        }

        private void InitFunctionDatabase()
        {
            _allFunctions = new List<FunctionMetadata>
            {
                // Math & Stats
                new FunctionMetadata("SUM", "Toán học", "Tính tổng tất cả các số trong dải ô được chọn.", "SUM(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Số thứ nhất, tham chiếu ô hoặc dải ô muốn tính tổng (Bắt buộc).", false, "A1:A10"),
                    new FunctionArgumentInfo("number2", "Số thứ 2 đến 255 hoặc dải ô bổ sung muốn cộng thêm (Tùy chọn).", true)),

                new FunctionMetadata("AVERAGE", "Thống kê", "Tính giá trị trung bình cộng của các đối số.", "AVERAGE(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Số thứ nhất, ô hoặc dải ô cần tính trung bình (Bắt buộc).", false, "A1:A10"),
                    new FunctionArgumentInfo("number2", "Số thứ hai hoặc dải ô bổ sung (Tùy chọn).", true)),

                new FunctionMetadata("COUNT", "Thống kê", "Đếm số lượng ô có chứa giá trị số trong dải ô.", "COUNT(value1, [value2], ...)",
                    new FunctionArgumentInfo("value1", "Mục thứ nhất, tham chiếu ô hoặc dải ô cần đếm số (Bắt buộc).", false, "A1:A10")),

                new FunctionMetadata("COUNTA", "Thống kê", "Đếm số ô không rỗng (chứa văn bản, số, logic) trong dải ô.", "COUNTA(value1, [value2], ...)",
                    new FunctionArgumentInfo("value1", "Dải ô cần đếm các ô có dữ liệu (Bắt buộc).", false, "A1:A10")),

                new FunctionMetadata("MAX", "Thống kê", "Trả về giá trị lớn nhất trong tập hợp các giá trị.", "MAX(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô hoặc các số cần tìm giá trị lớn nhất (Bắt buộc).", false, "A1:A10")),

                new FunctionMetadata("MIN", "Thống kê", "Trả về giá trị nhỏ nhất trong tập hợp các giá trị.", "MIN(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô hoặc các số cần tìm giá trị nhỏ nhất (Bắt buộc).", false, "A1:A10")),

                new FunctionMetadata("PRODUCT", "Toán học", "Nhân tất cả các số được đưa vào làm đối số và trả về tích.", "PRODUCT(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Số hoặc dải ô thứ nhất cần nhân.", false, "A1:A5")),

                new FunctionMetadata("ROUND", "Toán học", "Làm tròn một số đến số chữ số thập phân đã xác định.", "ROUND(number, num_digits)",
                    new FunctionArgumentInfo("number", "Số thực cần làm tròn (Bắt buộc).", false, "A1"),
                    new FunctionArgumentInfo("num_digits", "Số chữ số thập phân muốn làm tròn tới (ví dụ: 2, 0).", false, "2")),

                new FunctionMetadata("SUMPRODUCT", "Toán học", "Nhân các phần tử tương ứng trong các mảng và trả về tổng các tích.", "SUMPRODUCT(array1, [array2], ...)",
                    new FunctionArgumentInfo("array1", "Mảng hoặc dải ô thứ nhất có các phần tử muốn nhân rồi cộng.", false, "A1:A10"),
                    new FunctionArgumentInfo("array2", "Mảng hoặc dải ô thứ hai cùng kích thước.", false, "B1:B10")),

                // Logic
                new FunctionMetadata("IF", "Logic", "Kiểm tra xem điều kiện có được đáp ứng hay không và trả về giá trị tương ứng.", "IF(logical_test, value_if_true, [value_if_false])",
                    new FunctionArgumentInfo("logical_test", "Bất kỳ giá trị hoặc biểu thức nào có thể đánh giá là TRUE hoặc FALSE (ví dụ: A1>50).", false, "A1>=50"),
                    new FunctionArgumentInfo("value_if_true", "Giá trị trả về nếu logical_test là TRUE.", false, "\"Đạt\""),
                    new FunctionArgumentInfo("value_if_false", "Giá trị trả về nếu logical_test là FALSE.", true, "\"Chưa đạt\"")),

                new FunctionMetadata("IFS", "Logic", "Kiểm tra xem một hoặc nhiều điều kiện có được thỏa mãn và trả về giá trị tương ứng với điều kiện TRUE đầu tiên.", "IFS(logical_test1, value_if_true1, ...)",
                    new FunctionArgumentInfo("logical_test1", "Điều kiện logic thứ nhất cần kiểm tra.", false, "A1>=90"),
                    new FunctionArgumentInfo("value_if_true1", "Giá trị trả về nếu điều kiện 1 TRUE.", false, "\"Xuất sắc\""),
                    new FunctionArgumentInfo("logical_test2", "Điều kiện logic thứ hai.", true, "A1>=80"),
                    new FunctionArgumentInfo("value_if_true2", "Giá trị trả về nếu điều kiện 2 TRUE.", true, "\"Giỏi\"")),

                new FunctionMetadata("AND", "Logic", "Kiểm tra xem tất cả các đối số có là TRUE hay không, trả về TRUE nếu tất cả đều đúng.", "AND(logical1, [logical2], ...)",
                    new FunctionArgumentInfo("logical1", "Điều kiện thứ nhất cần kiểm tra (Bắt buộc).", false, "A1>0"),
                    new FunctionArgumentInfo("logical2", "Điều kiện thứ hai cần kiểm tra (Tùy chọn).", true, "B1>0")),

                new FunctionMetadata("OR", "Logic", "Kiểm tra xem có bất kỳ đối số nào là TRUE hay không, trả về TRUE nếu có ít nhất 1 điều kiện đúng.", "OR(logical1, [logical2], ...)",
                    new FunctionArgumentInfo("logical1", "Điều kiện thứ nhất.", false, "A1>0"),
                    new FunctionArgumentInfo("logical2", "Điều kiện thứ hai.", true, "B1>0")),

                new FunctionMetadata("NOT", "Logic", "Đảo ngược giá trị logic của đối số (TRUE thành FALSE và ngược lại).", "NOT(logical)",
                    new FunctionArgumentInfo("logical", "Biểu thức hoặc giá trị logic cần phủ định.", false, "A1=0")),

                new FunctionMetadata("IFERROR", "Logic", "Trả về giá trị thay thế nếu công thức bị lỗi, ngược lại trả về kết quả công thức.", "IFERROR(value, value_if_error)",
                    new FunctionArgumentInfo("value", "Biểu thức hoặc ô cần kiểm tra lỗi (Bắt buộc).", false, "A1/B1"),
                    new FunctionArgumentInfo("value_if_error", "Giá trị trả về nếu biểu thức bị lỗi (#DIV/0!, #N/A...).", false, "0")),

                new FunctionMetadata("SWITCH", "Logic", "Đánh giá một biểu thức dựa trên danh sách các giá trị và trả về kết quả tương ứng.", "SWITCH(expression, val1, result1, ...)",
                    new FunctionArgumentInfo("expression", "Biểu thức hoặc giá trị cần so khớp.", false, "A1"),
                    new FunctionArgumentInfo("val1", "Giá trị thứ nhất so khớp với biểu thức.", false, "1"),
                    new FunctionArgumentInfo("result1", "Kết quả trả về nếu khớp giá trị 1.", false, "\"Thứ Hai\"")),

                // Text
                new FunctionMetadata("CONCAT", "Văn bản", "Nối chuỗi danh sách các dải ô hoặc chuỗi văn bản.", "CONCAT(text1, [text2], ...)",
                    new FunctionArgumentInfo("text1", "Văn bản, tham chiếu ô hoặc dải ô cần nối lại.", false, "A1"),
                    new FunctionArgumentInfo("text2", "Văn bản hoặc ô thứ hai muốn nối tiếp.", true, "\" \""),
                    new FunctionArgumentInfo("text3", "Văn bản hoặc ô thứ ba.", true, "B1")),

                new FunctionMetadata("TEXTJOIN", "Văn bản", "Nối nhiều chuỗi hoặc dải ô kèm ký tự phân cách giữa các mục.", "TEXTJOIN(delimiter, ignore_empty, text1, ...)",
                    new FunctionArgumentInfo("delimiter", "Ký tự phân cách chèn giữa các đoạn chữ (ví dụ: \", \" hoặc \"-\").", false, "\", \""),
                    new FunctionArgumentInfo("ignore_empty", "TRUE để bỏ qua ô trống, FALSE để bao gồm ô trống.", false, "TRUE"),
                    new FunctionArgumentInfo("text1", "Dải ô hoặc văn bản cần nối.", false, "A1:A5")),

                new FunctionMetadata("LEFT", "Văn bản", "Trả về số lượng ký tự đầu tiên từ bên trái của chuỗi văn bản.", "LEFT(text, [num_chars])",
                    new FunctionArgumentInfo("text", "Chuỗi văn bản chứa ký tự muốn trích xuất.", false, "A1"),
                    new FunctionArgumentInfo("num_chars", "Số ký tự muốn lấy từ bên trái (mặc định là 1).", true, "3")),

                new FunctionMetadata("RIGHT", "Văn bản", "Trả về số lượng ký tự cuối cùng từ bên phải của chuỗi văn bản.", "RIGHT(text, [num_chars])",
                    new FunctionArgumentInfo("text", "Chuỗi văn bản chứa ký tự muốn trích xuất.", false, "A1"),
                    new FunctionArgumentInfo("num_chars", "Số ký tự muốn lấy từ bên phải.", true, "3")),

                new FunctionMetadata("MID", "Văn bản", "Trả về các ký tự từ giữa chuỗi văn bản, bắt đầu tại vị trí chỉ định.", "MID(text, start_num, num_chars)",
                    new FunctionArgumentInfo("text", "Chuỗi văn bản muốn trích xuất ký tự.", false, "A1"),
                    new FunctionArgumentInfo("start_num", "Vị trí bắt đầu lấy ký tự đầu tiên (tính từ 1).", false, "2"),
                    new FunctionArgumentInfo("num_chars", "Số ký tự muốn lấy.", false, "4")),

                new FunctionMetadata("LEN", "Văn bản", "Trả về số lượng ký tự có trong chuỗi văn bản.", "LEN(text)",
                    new FunctionArgumentInfo("text", "Chuỗi văn bản cần đếm chiều dài.", false, "A1")),

                new FunctionMetadata("TRIM", "Văn bản", "Xóa bỏ tất cả khoảng trắng thừa ở hai đầu và giữa các từ.", "TRIM(text)",
                    new FunctionArgumentInfo("text", "Văn bản muốn loại bỏ khoảng trống thừa.", false, "A1")),

                new FunctionMetadata("UPPER", "Văn bản", "Chuyển đổi toàn bộ các chữ cái trong chuỗi văn bản thành chữ in HOA.", "UPPER(text)",
                    new FunctionArgumentInfo("text", "Văn bản cần chuyển thành chữ in hoa.", false, "A1")),

                new FunctionMetadata("LOWER", "Văn bản", "Chuyển đổi toàn bộ các chữ cái trong chuỗi văn bản thành chữ thường.", "LOWER(text)",
                    new FunctionArgumentInfo("text", "Văn bản cần chuyển thành chữ thường.", false, "A1")),

                // Lookup & Reference
                new FunctionMetadata("VLOOKUP", "Tra cứu & Tham chiếu", "Tìm kiếm một giá trị ở cột đầu tiên bên trái của bảng và trả về giá trị ở cột cùng hàng.", "VLOOKUP(lookup_value, table_array, col_index_num, [range_lookup])",
                    new FunctionArgumentInfo("lookup_value", "Giá trị cần tìm kiếm ở cột đầu tiên của bảng dữ liệu.", false, "A2"),
                    new FunctionArgumentInfo("table_array", "Bảng dữ liệu hai chiều chứa dữ liệu cần tra cứu (ví dụ: D1:F50).", false, "D1:F50"),
                    new FunctionArgumentInfo("col_index_num", "Số thứ tự của cột chứa kết quả cần lấy (cột 1, 2, 3...).", false, "2"),
                    new FunctionArgumentInfo("range_lookup", "FALSE (hoặc 0) cho khớp chính xác, TRUE (hoặc 1) cho khớp xấp xỉ.", true, "FALSE")),

                new FunctionMetadata("HLOOKUP", "Tra cứu & Tham chiếu", "Tìm kiếm một giá trị ở hàng đầu tiên của bảng và trả về giá trị ở hàng chỉ định.", "HLOOKUP(lookup_value, table_array, row_index_num, [range_lookup])",
                    new FunctionArgumentInfo("lookup_value", "Giá trị cần tìm ở hàng đầu tiên.", false, "A1"),
                    new FunctionArgumentInfo("table_array", "Bảng dữ liệu tra cứu.", false, "A1:Z5"),
                    new FunctionArgumentInfo("row_index_num", "Số thứ tự hàng chứa kết quả cần lấy.", false, "2"),
                    new FunctionArgumentInfo("range_lookup", "FALSE cho khớp chính xác, TRUE cho khớp xấp xỉ.", true, "FALSE")),

                new FunctionMetadata("XLOOKUP", "Tra cứu & Tham chiếu", "Tìm kiếm dữ liệu hai chiều hiện đại và trả về giá trị tương ứng từ dải ô kết quả.", "XLOOKUP(lookup_value, lookup_array, return_array, [if_not_found])",
                    new FunctionArgumentInfo("lookup_value", "Giá trị muốn tìm kiếm.", false, "A2"),
                    new FunctionArgumentInfo("lookup_array", "Dải ô hoặc mảng cần quét tìm kiếm.", false, "D2:D100"),
                    new FunctionArgumentInfo("return_array", "Dải ô chứa kết quả trả về tương ứng.", false, "E2:E100"),
                    new FunctionArgumentInfo("if_not_found", "Giá trị trả về nếu không tìm thấy (ví dụ: \"Không tìm thấy\").", true, "\"N/A\"")),

                new FunctionMetadata("INDEX", "Tra cứu & Tham chiếu", "Trả về giá trị của một ô trong bảng tại giao điểm giữa hàng và cột chỉ định.", "INDEX(array, row_num, [column_num])",
                    new FunctionArgumentInfo("array", "Dải ô hoặc bảng dữ liệu.", false, "A1:C10"),
                    new FunctionArgumentInfo("row_num", "Số thứ tự hàng trong mảng cần lấy giá trị.", false, "2"),
                    new FunctionArgumentInfo("column_num", "Số thứ tự cột trong mảng cần lấy giá trị.", true, "3")),

                new FunctionMetadata("MATCH", "Tra cứu & Tham chiếu", "Trả về vị trí tương đối của một mục trong dải ô khớp với giá trị chỉ định.", "MATCH(lookup_value, lookup_array, [match_type])",
                    new FunctionArgumentInfo("lookup_value", "Giá trị cần tìm vị trí trong mảng.", false, "A2"),
                    new FunctionArgumentInfo("lookup_array", "Dải ô một chiều chứa các giá trị.", false, "D1:D100"),
                    new FunctionArgumentInfo("match_type", "0 cho khớp chính xác, 1 hoặc -1 cho khớp xấp xỉ.", true, "0")),

                new FunctionMetadata("OFFSET", "Tra cứu & Tham chiếu", "Trả về tham chiếu đến một vùng ô bị dịch chuyển một số hàng và cột từ ô mốc.", "OFFSET(reference, rows, cols, [height], [width])",
                    new FunctionArgumentInfo("reference", "Ô gốc làm mốc bắt đầu dịch chuyển.", false, "A1"),
                    new FunctionArgumentInfo("rows", "Số hàng cần dịch xuống dưới (hoặc âm để dịch lên).", false, "1"),
                    new FunctionArgumentInfo("cols", "Số cột cần dịch sang phải (hoặc âm để dịch sang trái).", false, "2")),

                new FunctionMetadata("INDIRECT", "Tra cứu & Tham chiếu", "Trả về tham chiếu ô được chỉ định bởi một chuỗi văn bản.", "INDIRECT(ref_text)",
                    new FunctionArgumentInfo("ref_text", "Chuỗi văn bản chứa địa chỉ ô (ví dụ: \"A1\" hoặc B1 chứa tên ô).", false, "\"A1\"")),

                // Date & Time
                new FunctionMetadata("TODAY", "Ngày & Giờ", "Trả về ngày hiện tại của hệ thống (không có giờ).", "TODAY()"),
                new FunctionMetadata("NOW", "Ngày & Giờ", "Trả về ngày và giờ hiện tại chính xác của hệ thống.", "NOW()"),
                new FunctionMetadata("DATE", "Ngày & Giờ", "Tạo một ngày hợp lệ từ các số năm, tháng, ngày.", "DATE(year, month, day)",
                    new FunctionArgumentInfo("year", "Năm (từ 1900 đến 9999).", false, "2026"),
                    new FunctionArgumentInfo("month", "Tháng (từ 1 đến 12).", false, "8"),
                    new FunctionArgumentInfo("day", "Ngày trong tháng (từ 1 đến 31).", false, "27")),
                new FunctionMetadata("DAY", "Ngày & Giờ", "Trả về ngày trong tháng (từ 1 đến 31) từ một ngày.", "DAY(serial_number)",
                    new FunctionArgumentInfo("serial_number", "Ngày cần lấy phần ngày.", false, "TODAY()")),
                new FunctionMetadata("MONTH", "Ngày & Giờ", "Trả về tháng trong năm (từ 1 đến 12) từ một ngày.", "MONTH(serial_number)",
                    new FunctionArgumentInfo("serial_number", "Ngày cần lấy phần tháng.", false, "TODAY()")),
                new FunctionMetadata("YEAR", "Ngày & Giờ", "Trả về năm (từ 1900 đến 9999) từ một ngày.", "YEAR(serial_number)",
                    new FunctionArgumentInfo("serial_number", "Ngày cần lấy phần năm.", false, "TODAY()"))
            };
            _filteredFunctions = new List<FunctionMetadata>(_allFunctions);
        }

        private void InitializeComponent()
        {
            Text = "Chèn Hàm (Insert Function - fx)";
            Size = new Size(520, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            BackColor = Color.FromArgb(243, 244, 246);

            // ==================== PANEL 1: SELECT FUNCTION ====================
            pnlSelect = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            Label lblSearchPrompt = new Label
            {
                Text = "Tìm kiếm hàm (Gõ từ khóa để tìm):",
                Location = new Point(16, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            txtSearch = new TextBox
            {
                Location = new Point(16, 34),
                Size = new Size(380, 24)
            };
            txtSearch.TextChanged += (s, e) => FilterFunctions();

            Button btnGo = new Button
            {
                Text = "Tìm",
                Location = new Point(404, 33),
                Size = new Size(80, 26),
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.System
            };
            btnGo.Click += (s, e) => FilterFunctions();

            Label lblCat = new Label
            {
                Text = "Hoặc chọn một danh mục hàm:",
                Location = new Point(16, 68),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            cboCategory = new ComboBox
            {
                Location = new Point(16, 90),
                Size = new Size(468, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCategory.Items.AddRange(new object[] {
                "Tất cả (All)",
                "Toán học (Math & Trig)",
                "Thống kê (Statistical)",
                "Logic (Logical)",
                "Văn bản (Text)",
                "Tra cứu & Tham chiếu (Lookup & Reference)",
                "Ngày & Giờ (Date & Time)"
            });
            cboCategory.SelectedIndex = 0;
            cboCategory.SelectedIndexChanged += (s, e) => FilterFunctions();

            Label lblList = new Label
            {
                Text = "Chọn một hàm từ danh sách:",
                Location = new Point(16, 124),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            lstFunctions = new ListBox
            {
                Location = new Point(16, 146),
                Size = new Size(468, 140),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular)
            };
            lstFunctions.SelectedIndexChanged += LstFunctions_SelectedIndexChanged;
            lstFunctions.DoubleClick += (s, e) =>
            {
                if (_selectedFunc != null) ShowArgumentsPanel(_selectedFunc);
            };

            lblSyntax = new Label
            {
                Location = new Point(16, 296),
                Size = new Size(468, 22),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 65)
            };

            lblDescription = new Label
            {
                Location = new Point(16, 320),
                Size = new Size(468, 80),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            btnNextArgs = new Button
            {
                Text = "Trợ giúp đối số >>",
                Location = new Point(270, 420),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNextArgs.FlatAppearance.BorderSize = 0;
            btnNextArgs.Click += (s, e) =>
            {
                if (_selectedFunc != null) ShowArgumentsPanel(_selectedFunc);
            };

            Button btnCancelSelect = new Button
            {
                Text = "Hủy (Cancel)",
                Location = new Point(408, 420),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelSelect.FlatAppearance.BorderSize = 0;
            btnCancelSelect.Click += (s, e) => Close();

            pnlSelect.Controls.AddRange(new Control[] {
                lblSearchPrompt, txtSearch, btnGo, lblCat, cboCategory, lblList, lstFunctions,
                lblSyntax, lblDescription, btnNextArgs, btnCancelSelect
            });

            // ==================== PANEL 2: FUNCTION ARGUMENTS ====================
            pnlArgs = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };

            lblArgsTitle = new Label
            {
                Location = new Point(16, 10),
                Size = new Size(468, 24),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 65)
            };
            lblArgsSyntax = new Label
            {
                Location = new Point(16, 36),
                Size = new Size(468, 20),
                Font = new Font("Consolas", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            pnlArgsInputs = new Panel
            {
                Location = new Point(16, 62),
                Size = new Size(468, 180),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            lblArgDesc = new Label
            {
                Location = new Point(16, 252),
                Size = new Size(468, 60),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            lblFormulaResult = new Label
            {
                Location = new Point(16, 320),
                Size = new Size(468, 30),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            btnBack = new Button
            {
                Text = "<< Chọn hàm khác",
                Location = new Point(16, 420),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => ShowSelectPanel();

            btnOk = new Button
            {
                Text = "OK (Áp dụng)",
                Location = new Point(290, 420),
                Size = new Size(104, 30),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => ApplyFunctionFormula();

            btnCancel = new Button
            {
                Text = "Hủy (Cancel)",
                Location = new Point(402, 420),
                Size = new Size(84, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => Close();

            pnlArgs.Controls.AddRange(new Control[] {
                lblArgsTitle, lblArgsSyntax, pnlArgsInputs, lblArgDesc, lblFormulaResult,
                btnBack, btnOk, btnCancel
            });

            Controls.AddRange(new Control[] { pnlSelect, pnlArgs });

            FilterFunctions();
        }

        private void FilterFunctions()
        {
            string q = (txtSearch.Text ?? "").Trim().ToUpper();
            int catIdx = cboCategory.SelectedIndex;

            _filteredFunctions = _allFunctions.FindAll(f =>
            {
                bool matchCat = true;
                switch (catIdx)
                {
                    case 1: matchCat = f.Category == "Toán học"; break;
                    case 2: matchCat = f.Category == "Thống kê"; break;
                    case 3: matchCat = f.Category == "Logic"; break;
                    case 4: matchCat = f.Category == "Văn bản"; break;
                    case 5: matchCat = f.Category == "Tra cứu & Tham chiếu"; break;
                    case 6: matchCat = f.Category == "Ngày & Giờ"; break;
                }
                if (!matchCat) return false;

                if (string.IsNullOrEmpty(q)) return true;
                return f.Name.ToUpper().Contains(q) || f.Description.ToUpper().Contains(q) || f.Category.ToUpper().Contains(q);
            });

            lstFunctions.Items.Clear();
            foreach (var f in _filteredFunctions)
            {
                lstFunctions.Items.Add(f.Name);
            }

            if (lstFunctions.Items.Count > 0)
            {
                lstFunctions.SelectedIndex = 0;
            }
            else
            {
                _selectedFunc = null;
                lblSyntax.Text = "";
                lblDescription.Text = "(Không tìm thấy hàm phù hợp)";
            }
        }

        private void LstFunctions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFunctions.SelectedIndex >= 0 && lstFunctions.SelectedIndex < _filteredFunctions.Count)
            {
                _selectedFunc = _filteredFunctions[lstFunctions.SelectedIndex];
                lblSyntax.Text = _selectedFunc.Syntax;
                lblDescription.Text = _selectedFunc.Description;
            }
        }

        private void ShowSelectPanel()
        {
            pnlArgs.Visible = false;
            pnlSelect.Visible = true;
            AcceptButton = btnNextArgs;
        }

        private void ShowArgumentsPanel(FunctionMetadata func)
        {
            _selectedFunc = func;
            pnlSelect.Visible = false;
            pnlArgs.Visible = true;
            AcceptButton = btnOk;

            lblArgsTitle.Text = func.Name;
            lblArgsSyntax.Text = "=" + func.Syntax;

            // Generate Input Rows for each argument
            pnlArgsInputs.Controls.Clear();
            _argTextBoxes.Clear();

            int curY = 10;
            for (int i = 0; i < func.Arguments.Count; i++)
            {
                var arg = func.Arguments[i];

                Label lblArg = new Label
                {
                    Text = arg.Name + (arg.IsOptional ? " [tùy chọn]:" : " (bắt buộc):"),
                    Location = new Point(10, curY + 3),
                    Size = new Size(130, 20),
                    Font = new Font("Segoe UI", 8.5f, arg.IsOptional ? FontStyle.Regular : FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59)
                };

                TextBox txtVal = new TextBox
                {
                    Location = new Point(145, curY),
                    Size = new Size(280, 24),
                    Text = arg.DefaultValue
                };
                int argIndex = i;
                txtVal.Enter += (s, e) =>
                {
                    lblArgDesc.Text = string.Format("【{0}】: {1}", arg.Name, arg.Description);
                };
                txtVal.TextChanged += (s, e) => UpdateFormulaResultPreview();

                pnlArgsInputs.Controls.Add(lblArg);
                pnlArgsInputs.Controls.Add(txtVal);
                _argTextBoxes.Add(txtVal);

                curY += 34;
            }

            if (func.Arguments.Count == 0)
            {
                Label lblNoArgs = new Label
                {
                    Text = "Hàm này không yêu cầu đối số.",
                    Location = new Point(15, 20),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(71, 85, 105)
                };
                pnlArgsInputs.Controls.Add(lblNoArgs);
                lblArgDesc.Text = func.Description;
            }
            else
            {
                lblArgDesc.Text = string.Format("【{0}】: {1}", func.Arguments[0].Name, func.Arguments[0].Description);
            }

            UpdateFormulaResultPreview();
        }

        private void UpdateFormulaResultPreview()
        {
            if (_selectedFunc == null) return;

            List<string> args = new List<string>();
            for (int i = 0; i < _argTextBoxes.Count; i++)
            {
                string val = (_argTextBoxes[i].Text ?? "").Trim();
                if (!string.IsNullOrEmpty(val) || !_selectedFunc.Arguments[i].IsOptional)
                {
                    args.Add(val);
                }
            }

            string formulaStr = string.Format("={0}({1})", _selectedFunc.Name, string.Join(", ", args.ToArray()));
            ResultFormula = formulaStr;

            // Evaluate in real-time
            try
            {
                if (_calcView != null && _calcView.CurrentSheet != null)
                {
                    CellData evalRes = FormulaEngine.EvaluateFormula(formulaStr, _calcView.CurrentSheet);
                    string resDisplay = !string.IsNullOrEmpty(evalRes.EvaluatedStr) ? evalRes.EvaluatedStr : evalRes.NumericValue.ToString();
                    lblFormulaResult.Text = string.Format("Kết quả công thức (Formula result) = {0}", resDisplay);
                }
                else
                {
                    lblFormulaResult.Text = "Cú pháp: " + formulaStr;
                }
            }
            catch (Exception)
            {
                lblFormulaResult.Text = "Kết quả công thức = (Đang kiểm tra cú pháp...)";
            }
        }

        private void ApplyFunctionFormula()
        {
            UpdateFormulaResultPreview();
            if (_calcView != null && !string.IsNullOrEmpty(ResultFormula))
            {
                _calcView.SaveUndoState();
                _calcView.CurrentSheet.SetCellText(_calcView.SelectedRow, _calcView.SelectedCol, ResultFormula);
                FormulaEngine.EvaluateSheet(_calcView.CurrentSheet);
            }
            Close();
        }
    }
}
