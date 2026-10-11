using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
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
                    new FunctionArgumentInfo("serial_number", "Ngày cần lấy phần năm.", false, "TODAY()")),
                new FunctionMetadata("TIME", "Ngày & Giờ", "Tạo giá trị thời gian từ giờ, phút, giây.", "TIME(hour, minute, second)",
                    new FunctionArgumentInfo("hour", "Giờ (0-23).", false, "14"),
                    new FunctionArgumentInfo("minute", "Phút (0-59).", false, "30"),
                    new FunctionArgumentInfo("second", "Giây (0-59).", false, "0")),
                new FunctionMetadata("HOUR", "Ngày & Giờ", "Lấy giờ (0-23) từ giá trị thời gian.", "HOUR(serial_number)",
                    new FunctionArgumentInfo("serial_number", "Giá trị thời gian.", false, "NOW()")),
                new FunctionMetadata("MINUTE", "Ngày & Giờ", "Lấy phút (0-59) từ giá trị thời gian.", "MINUTE(serial_number)",
                    new FunctionArgumentInfo("serial_number", "Giá trị thời gian.", false, "NOW()")),
                new FunctionMetadata("SECOND", "Ngày & Giờ", "Lấy giây (0-59) từ giá trị thời gian.", "SECOND(serial_number)",
                    new FunctionArgumentInfo("serial_number", "Giá trị thời gian.", false, "NOW()")),
                new FunctionMetadata("WEEKDAY", "Ngày & Giờ", "Trả về thứ trong tuần của một ngày.", "WEEKDAY(serial_number, [return_type])",
                    new FunctionArgumentInfo("serial_number", "Ngày cần kiểm tra.", false, "TODAY()"),
                    new FunctionArgumentInfo("return_type", "Kiểu trả về (1: CN=1..T7=7; 2: T2=1..CN=7).", true, "2")),
                new FunctionMetadata("WEEKNUM", "Ngày & Giờ", "Trả về số thứ tự tuần trong năm.", "WEEKNUM(serial_number, [return_type])",
                    new FunctionArgumentInfo("serial_number", "Ngày cần kiểm tra.", false, "TODAY()")),
                new FunctionMetadata("ISOWEEKNUM", "Ngày & Giờ", "Trả về số tuần theo chuẩn ISO của ngày.", "ISOWEEKNUM(date)",
                    new FunctionArgumentInfo("date", "Ngày cần tính tuần ISO.", false, "TODAY()")),
                new FunctionMetadata("EDATE", "Ngày & Giờ", "Trả về ngày cách ngày mốc một số tháng trước hoặc sau.", "EDATE(start_date, months)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "TODAY()"),
                    new FunctionArgumentInfo("months", "Số tháng cộng/trừ.", false, "3")),
                new FunctionMetadata("EOMONTH", "Ngày & Giờ", "Trả về ngày cuối cùng của tháng sau một số tháng chỉ định.", "EOMONTH(start_date, months)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "TODAY()"),
                    new FunctionArgumentInfo("months", "Số tháng trước hoặc sau (0 là cuối tháng hiện tại).", false, "0")),
                new FunctionMetadata("WORKDAY", "Ngày & Giờ", "Trả về ngày làm việc sau một số ngày làm việc chỉ định (trừ T7, CN).", "WORKDAY(start_date, days)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "TODAY()"),
                    new FunctionArgumentInfo("days", "Số ngày làm việc.", false, "10")),
                new FunctionMetadata("NETWORKDAYS", "Ngày & Giờ", "Đếm tổng số ngày làm việc giữa hai mốc ngày.", "NETWORKDAYS(start_date, end_date)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "A1"),
                    new FunctionArgumentInfo("end_date", "Ngày kết thúc.", false, "B1")),
                new FunctionMetadata("DATEDIF", "Ngày & Giờ", "Tính chênh lệch ngày, tháng hoặc năm giữa hai ngày.", "DATEDIF(start_date, end_date, unit)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "A1"),
                    new FunctionArgumentInfo("end_date", "Ngày kết thúc.", false, "B1"),
                    new FunctionArgumentInfo("unit", "Đơn vị trả về: \"Y\" (năm), \"M\" (tháng), \"D\" (ngày).", false, "\"D\"")),
                new FunctionMetadata("DAYS", "Ngày & Giờ", "Trả về số ngày giữa hai mốc ngày.", "DAYS(end_date, start_date)",
                    new FunctionArgumentInfo("end_date", "Ngày kết thúc.", false, "B1"),
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "A1")),
                new FunctionMetadata("DAYS360", "Ngày & Giờ", "Tính số ngày giữa hai ngày theo năm kế toán 360 ngày.", "DAYS360(start_date, end_date)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "A1"),
                    new FunctionArgumentInfo("end_date", "Ngày kết thúc.", false, "B1")),
                new FunctionMetadata("YEARFRAC", "Ngày & Giờ", "Tính tỷ lệ năm giữa hai mốc ngày.", "YEARFRAC(start_date, end_date)",
                    new FunctionArgumentInfo("start_date", "Ngày bắt đầu.", false, "A1"),
                    new FunctionArgumentInfo("end_date", "Ngày kết thúc.", false, "B1")),

                // Multi-condition Statistical Functions
                new FunctionMetadata("SUMIFS", "Thống kê", "Tính tổng các ô thỏa mãn nhiều điều kiện đồng thời.", "SUMIFS(sum_range, criteria_range1, criteria1, [criteria_range2, criteria2], ...)",
                    new FunctionArgumentInfo("sum_range", "Dải ô thực tế cần tính tổng.", false, "C2:C100"),
                    new FunctionArgumentInfo("criteria_range1", "Dải ô xét điều kiện thứ nhất.", false, "A2:A100"),
                    new FunctionArgumentInfo("criteria1", "Điều kiện thứ nhất.", false, "\">0\""),
                    new FunctionArgumentInfo("criteria_range2", "Dải ô xét điều kiện thứ hai (Tùy chọn).", true, "B2:B100"),
                    new FunctionArgumentInfo("criteria2", "Điều kiện thứ hai (Tùy chọn).", true, "\"Đạt\"")),
                new FunctionMetadata("COUNTIFS", "Thống kê", "Đếm số ô thỏa mãn nhiều điều kiện đồng thời.", "COUNTIFS(criteria_range1, criteria1, [criteria_range2, criteria2], ...)",
                    new FunctionArgumentInfo("criteria_range1", "Dải ô xét điều kiện thứ nhất.", false, "A2:A100"),
                    new FunctionArgumentInfo("criteria1", "Điều kiện thứ nhất.", false, "\">50\""),
                    new FunctionArgumentInfo("criteria_range2", "Dải ô xét điều kiện thứ hai (Tùy chọn).", true, "B2:B100"),
                    new FunctionArgumentInfo("criteria2", "Điều kiện thứ hai (Tùy chọn).", true, "\"Có\"")),
                new FunctionMetadata("AVERAGEIFS", "Thống kê", "Tính trung bình cộng các ô thỏa mãn nhiều điều kiện.", "AVERAGEIFS(average_range, criteria_range1, criteria1, ...)",
                    new FunctionArgumentInfo("average_range", "Dải ô cần tính trung bình.", false, "C2:C100"),
                    new FunctionArgumentInfo("criteria_range1", "Dải ô điều kiện 1.", false, "A2:A100"),
                    new FunctionArgumentInfo("criteria1", "Điều kiện 1.", false, "\">0\"")),
                new FunctionMetadata("MAXIFS", "Thống kê", "Tìm giá trị lớn nhất trong các ô thỏa mãn điều kiện.", "MAXIFS(max_range, criteria_range1, criteria1, ...)",
                    new FunctionArgumentInfo("max_range", "Dải ô cần tìm giá trị lớn nhất.", false, "C2:C100"),
                    new FunctionArgumentInfo("criteria_range1", "Dải ô điều kiện 1.", false, "A2:A100"),
                    new FunctionArgumentInfo("criteria1", "Điều kiện 1.", false, "\"A\"")),
                new FunctionMetadata("MINIFS", "Thống kê", "Tìm giá trị nhỏ nhất trong các ô thỏa mãn điều kiện.", "MINIFS(min_range, criteria_range1, criteria1, ...)",
                    new FunctionArgumentInfo("min_range", "Dải ô cần tìm giá trị nhỏ nhất.", false, "C2:C100"),
                    new FunctionArgumentInfo("criteria_range1", "Dải ô điều kiện 1.", false, "A2:A100"),
                    new FunctionArgumentInfo("criteria1", "Điều kiện 1.", false, "\"A\"")),
                new FunctionMetadata("COUNTBLANK", "Thống kê", "Đếm số ô trống trong một dải ô.", "COUNTBLANK(range)",
                    new FunctionArgumentInfo("range", "Dải ô cần đếm ô trống.", false, "A1:A50")),
                new FunctionMetadata("SUBTOTAL", "Toán học", "Tính tổng phụ trong danh sách hoặc cơ sở dữ liệu (1=AVERAGE, 2=COUNT, 3=COUNTA, 4=MAX, 5=MIN, 9=SUM).", "SUBTOTAL(function_num, ref1, [ref2], ...)",
                    new FunctionArgumentInfo("function_num", "Mã số hàm (9 cho SUM, 1 cho AVERAGE, 2 cho COUNT...).", false, "9"),
                    new FunctionArgumentInfo("ref1", "Dải ô cần tính.", false, "A2:A50")),
                new FunctionMetadata("STDEV.S", "Thống kê", "Tính độ lệch chuẩn mẫu của tập dữ liệu.", "STDEV.S(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô mẫu dữ liệu.", false, "A1:A20")),
                new FunctionMetadata("STDEV.P", "Thống kê", "Tính độ lệch chuẩn tổng thể của tập dữ liệu.", "STDEV.P(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô tổng thể dữ liệu.", false, "A1:A20")),
                new FunctionMetadata("VAR.S", "Thống kê", "Tính phương sai mẫu của tập dữ liệu.", "VAR.S(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô mẫu dữ liệu.", false, "A1:A20")),
                new FunctionMetadata("VAR.P", "Thống kê", "Tính phương sai tổng thể của tập dữ liệu.", "VAR.P(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô tổng thể dữ liệu.", false, "A1:A20")),
                new FunctionMetadata("RANK", "Thống kê", "Xếp hạng một số trong danh sách các số.", "RANK(number, ref, [order])",
                    new FunctionArgumentInfo("number", "Số cần xếp hạng.", false, "A2"),
                    new FunctionArgumentInfo("ref", "Danh sách số tham chiếu.", false, "$A$2:$A$20"),
                    new FunctionArgumentInfo("order", "0: giảm dần (mặc định), 1: tăng dần.", true, "0")),
                new FunctionMetadata("PERCENTILE", "Thống kê", "Trả về phân vị thứ k của các giá trị trong dải ô (k từ 0 đến 1).", "PERCENTILE(array, k)",
                    new FunctionArgumentInfo("array", "Dải dữ liệu.", false, "A1:A20"),
                    new FunctionArgumentInfo("k", "Giá trị phân vị (0..1, ví dụ 0.9).", false, "0.9")),
                new FunctionMetadata("QUARTILE", "Thống kê", "Trả về tứ phân vị của tập dữ liệu (quart từ 0 đến 4).", "QUARTILE(array, quart)",
                    new FunctionArgumentInfo("array", "Dải dữ liệu.", false, "A1:A20"),
                    new FunctionArgumentInfo("quart", "Tứ phân vị (0: Min, 1: 25%, 2: Median, 3: 75%, 4: Max).", false, "1")),
                new FunctionMetadata("MODE", "Thống kê", "Trả về giá trị xuất hiện nhiều nhất trong dải dữ liệu.", "MODE(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Dải ô cần tìm yếu vị.", false, "A1:A20")),
                new FunctionMetadata("CORREL", "Thống kê", "Trả về hệ số tương quan giữa hai tập dữ liệu.", "CORREL(array1, array2)",
                    new FunctionArgumentInfo("array1", "Dải dữ liệu thứ nhất.", false, "A1:A20"),
                    new FunctionArgumentInfo("array2", "Dải dữ liệu thứ hai.", false, "B1:B20")),
                new FunctionMetadata("FORECAST", "Thống kê", "Dự báo giá trị tương lai theo hồi quy tuyến tính.", "FORECAST(x, known_y's, known_x's)",
                    new FunctionArgumentInfo("x", "Điểm dữ liệu cần dự báo.", false, "30"),
                    new FunctionArgumentInfo("known_y's", "Dải dữ liệu phụ thuộc Y.", false, "B2:B20"),
                    new FunctionArgumentInfo("known_x's", "Dải dữ liệu độc lập X.", false, "A2:A20")),

                // Financial Functions
                new FunctionMetadata("PMT", "Tài chính", "Tính khoản thanh toán định kỳ cho khoản vay với lãi suất không đổi.", "PMT(rate, nper, pv, [fv], [type])",
                    new FunctionArgumentInfo("rate", "Lãi suất mỗi kỳ (ví dụ 8%/12).", false, "0.08/12"),
                    new FunctionArgumentInfo("nper", "Tổng số kỳ thanh toán.", false, "60"),
                    new FunctionArgumentInfo("pv", "Giá trị hiện tại (số tiền vay gốc).", false, "100000000"),
                    new FunctionArgumentInfo("fv", "Giá trị tương lai sau kỳ cuối (Tùy chọn, mặc định 0).", true, "0")),
                new FunctionMetadata("IPMT", "Tài chính", "Tính số tiền lãi phải trả trong một kỳ cụ thể của khoản vay.", "IPMT(rate, per, nper, pv, [fv], [type])",
                    new FunctionArgumentInfo("rate", "Lãi suất mỗi kỳ.", false, "0.08/12"),
                    new FunctionArgumentInfo("per", "Kỳ cần tính lãi (1..nper).", false, "1"),
                    new FunctionArgumentInfo("nper", "Tổng số kỳ.", false, "60"),
                    new FunctionArgumentInfo("pv", "Giá trị hiện tại (tiền vay).", false, "100000000")),
                new FunctionMetadata("PPMT", "Tài chính", "Tính số tiền gốc được trả trong một kỳ cụ thể của khoản vay.", "PPMT(rate, per, nper, pv, [fv], [type])",
                    new FunctionArgumentInfo("rate", "Lãi suất mỗi kỳ.", false, "0.08/12"),
                    new FunctionArgumentInfo("per", "Kỳ cần tính gốc (1..nper).", false, "1"),
                    new FunctionArgumentInfo("nper", "Tổng số kỳ.", false, "60"),
                    new FunctionArgumentInfo("pv", "Giá trị hiện tại (tiền vay).", false, "100000000")),
                new FunctionMetadata("FV", "Tài chính", "Tính giá trị tương lai của một khoản đầu tư có lãi suất cố định.", "FV(rate, nper, pmt, [pv], [type])",
                    new FunctionArgumentInfo("rate", "Lãi suất mỗi kỳ.", false, "0.06/12"),
                    new FunctionArgumentInfo("nper", "Tổng số kỳ nộp tiền.", false, "12"),
                    new FunctionArgumentInfo("pmt", "Khoản nộp thêm mỗi kỳ.", false, "-5000000"),
                    new FunctionArgumentInfo("pv", "Số tiền gốc ban đầu (Tùy chọn).", true, "0")),
                new FunctionMetadata("PV", "Tài chính", "Tính giá trị hiện tại của một khoản đầu tư hoặc chuỗi thanh toán.", "PV(rate, nper, pmt, [fv], [type])",
                    new FunctionArgumentInfo("rate", "Lãi suất mỗi kỳ.", false, "0.08/12"),
                    new FunctionArgumentInfo("nper", "Tổng số kỳ.", false, "60"),
                    new FunctionArgumentInfo("pmt", "Số tiền thanh toán mỗi kỳ.", false, "-2000000")),
                new FunctionMetadata("NPV", "Tài chính", "Tính giá trị hiện tại ròng (Net Present Value) của dòng tiền đầu tư.", "NPV(rate, value1, [value2], ...)",
                    new FunctionArgumentInfo("rate", "Lãi suất chiết khấu mỗi kỳ.", false, "0.1"),
                    new FunctionArgumentInfo("value1", "Dải ô chứa dòng tiền các kỳ.", false, "B2:B10")),
                new FunctionMetadata("IRR", "Tài chính", "Tính tỷ suất hoàn vốn nội bộ (Internal Rate of Return) cho chuỗi dòng tiền.", "IRR(values, [guess])",
                    new FunctionArgumentInfo("values", "Dải ô chứa dòng tiền (phải có ít nhất 1 số âm và 1 số dương).", false, "B1:B10"),
                    new FunctionArgumentInfo("guess", "Giá trị ước tính ban đầu (mặc định 0.1).", true, "0.1")),
                new FunctionMetadata("NPER", "Tài chính", "Tính tổng số kỳ thanh toán của một khoản vay hoặc đầu tư.", "NPER(rate, pmt, pv, [fv], [type])",
                    new FunctionArgumentInfo("rate", "Lãi suất mỗi kỳ.", false, "0.01"),
                    new FunctionArgumentInfo("pmt", "Khoản thanh toán mỗi kỳ.", false, "-2000000"),
                    new FunctionArgumentInfo("pv", "Giá trị khoản vay hiện tại.", false, "50000000")),
                new FunctionMetadata("RATE", "Tài chính", "Tính lãi suất mỗi kỳ của một niên kim.", "RATE(nper, pmt, pv, [fv], [type])",
                    new FunctionArgumentInfo("nper", "Tổng số kỳ thanh toán.", false, "48"),
                    new FunctionArgumentInfo("pmt", "Khoản thanh toán mỗi kỳ.", false, "-2000000"),
                    new FunctionArgumentInfo("pv", "Giá trị hiện tại.", false, "80000000")),
                new FunctionMetadata("SLN", "Tài chính", "Tính khấu hao tài sản theo phương pháp đường thẳng.", "SLN(cost, salvage, life)",
                    new FunctionArgumentInfo("cost", "Nguyên giá tài sản.", false, "100000000"),
                    new FunctionArgumentInfo("salvage", "Giá trị thu hồi thanh lý.", false, "10000000"),
                    new FunctionArgumentInfo("life", "Số kỳ khấu hao.", false, "5")),
                new FunctionMetadata("SYD", "Tài chính", "Tính khấu hao theo tổng số thứ tự năm sử dụng.", "SYD(cost, salvage, life, per)",
                    new FunctionArgumentInfo("cost", "Nguyên giá tài sản.", false, "100000000"),
                    new FunctionArgumentInfo("salvage", "Giá trị thanh lý.", false, "10000000"),
                    new FunctionArgumentInfo("life", "Tổng số năm sử dụng.", false, "5"),
                    new FunctionArgumentInfo("per", "Kỳ khấu hao cần tính.", false, "1")),
                new FunctionMetadata("DDB", "Tài chính", "Tính khấu hao theo số dư giảm dần kép.", "DDB(cost, salvage, life, period, [factor])",
                    new FunctionArgumentInfo("cost", "Nguyên giá tài sản.", false, "100000000"),
                    new FunctionArgumentInfo("salvage", "Giá trị thanh lý.", false, "10000000"),
                    new FunctionArgumentInfo("life", "Số kỳ sử dụng.", false, "5"),
                    new FunctionArgumentInfo("period", "Kỳ cần tính khấu hao.", false, "1")),

                // Additional Math, Text, Lookup, Info Functions
                new FunctionMetadata("SIN", "Toán học", "Tính sin của một góc (radian).", "SIN(number)", new FunctionArgumentInfo("number", "Góc tính bằng radian.", false, "1.57")),
                new FunctionMetadata("COS", "Toán học", "Tính cosin của một góc (radian).", "COS(number)", new FunctionArgumentInfo("number", "Góc tính bằng radian.", false, "0")),
                new FunctionMetadata("TAN", "Toán học", "Tính tang của một góc (radian).", "TAN(number)", new FunctionArgumentInfo("number", "Góc tính bằng radian.", false, "0.785")),
                new FunctionMetadata("PI", "Toán học", "Trả về hằng số Pi (3.14159265358979).", "PI()"),
                new FunctionMetadata("EXP", "Toán học", "Tính lũy thừa cơ số tự nhiên e mũ number.", "EXP(number)", new FunctionArgumentInfo("number", "Số mũ.", false, "1")),
                new FunctionMetadata("LN", "Toán học", "Tính logarit tự nhiên (cơ số e) của một số.", "LN(number)", new FunctionArgumentInfo("number", "Số thực dương.", false, "10")),
                new FunctionMetadata("LOG", "Toán học", "Tính logarit của một số theo cơ số chỉ định.", "LOG(number, [base])",
                    new FunctionArgumentInfo("number", "Số thực dương.", false, "100"),
                    new FunctionArgumentInfo("base", "Cơ số (mặc định 10).", true, "10")),
                new FunctionMetadata("LOG10", "Toán học", "Tính logarit cơ số 10 của một số.", "LOG10(number)", new FunctionArgumentInfo("number", "Số thực dương.", false, "100")),
                new FunctionMetadata("FACT", "Toán học", "Tính giai thừa n! của một số không âm.", "FACT(number)", new FunctionArgumentInfo("number", "Số nguyên không âm.", false, "5")),
                new FunctionMetadata("GCD", "Toán học", "Tìm ước số chung lớn nhất của các số.", "GCD(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Số thứ nhất.", false, "24"),
                    new FunctionArgumentInfo("number2", "Số thứ hai.", false, "36")),
                new FunctionMetadata("LCM", "Toán học", "Tìm bội số chung nhỏ nhất của các số.", "LCM(number1, [number2], ...)",
                    new FunctionArgumentInfo("number1", "Số thứ nhất.", false, "12"),
                    new FunctionArgumentInfo("number2", "Số thứ hai.", false, "18")),
                new FunctionMetadata("CEILING", "Toán học", "Làm tròn số lên tới bội số gần nhất.", "CEILING(number, significance)",
                    new FunctionArgumentInfo("number", "Số cần làm tròn.", false, "23.4"),
                    new FunctionArgumentInfo("significance", "Bội số cần làm tròn tới.", false, "5")),
                new FunctionMetadata("FLOOR", "Toán học", "Làm tròn số xuống tới bội số gần nhất.", "FLOOR(number, significance)",
                    new FunctionArgumentInfo("number", "Số cần làm tròn.", false, "23.4"),
                    new FunctionArgumentInfo("significance", "Bội số cần làm tròn tới.", false, "5")),
                new FunctionMetadata("RAND", "Toán học", "Trả về số thực ngẫu nhiên phân bố đều từ 0 đến 1.", "RAND()"),
                new FunctionMetadata("RANDBETWEEN", "Toán học", "Trả về số nguyên ngẫu nhiên nằm giữa hai số chỉ định.", "RANDBETWEEN(bottom, top)",
                    new FunctionArgumentInfo("bottom", "Số nguyên nhỏ nhất.", false, "1"),
                    new FunctionArgumentInfo("top", "Số nguyên lớn nhất.", false, "100")),
                new FunctionMetadata("ROMAN", "Toán học", "Chuyển số tự nhiên sang chữ số La Mã.", "ROMAN(number)", new FunctionArgumentInfo("number", "Số nguyên từ 1 đến 3999.", false, "2026")),
                new FunctionMetadata("TEXTJOIN", "Văn bản", "Nối nhiều chuỗi hoặc dải ô bằng ký tự phân cách.", "TEXTJOIN(delimiter, ignore_empty, text1, ...)",
                    new FunctionArgumentInfo("delimiter", "Chuỗi phân cách (ví dụ \", \").", false, "\", \""),
                    new FunctionArgumentInfo("ignore_empty", "TRUE để bỏ qua ô trống.", false, "TRUE"),
                    new FunctionArgumentInfo("text1", "Dải ô cần nối.", false, "A1:A10")),
                new FunctionMetadata("TEXT", "Văn bản", "Định dạng một số và chuyển thành chuỗi văn bản.", "TEXT(value, format_text)",
                    new FunctionArgumentInfo("value", "Giá trị số hoặc ngày.", false, "A1"),
                    new FunctionArgumentInfo("format_text", "Mẫu định dạng (ví dụ \"#,##0.00\").", false, "\"#,##0\"")),
                new FunctionMetadata("FIND", "Văn bản", "Tìm vị trí chuỗi con (phân biệt chữ hoa/thường).", "FIND(find_text, within_text, [start_num])",
                    new FunctionArgumentInfo("find_text", "Chuỗi cần tìm.", false, "\"A\""),
                    new FunctionArgumentInfo("within_text", "Chuỗi văn bản gốc.", false, "A1")),
                new FunctionMetadata("SEARCH", "Văn bản", "Tìm vị trí chuỗi con (không phân biệt hoa/thường).", "SEARCH(find_text, within_text, [start_num])",
                    new FunctionArgumentInfo("find_text", "Chuỗi cần tìm.", false, "\"a\""),
                    new FunctionArgumentInfo("within_text", "Chuỗi văn bản gốc.", false, "A1")),
                new FunctionMetadata("REPLACE", "Văn bản", "Thay thế một phần chuỗi văn bản dựa trên vị trí và số ký tự.", "REPLACE(old_text, start_num, num_chars, new_text)",
                    new FunctionArgumentInfo("old_text", "Văn bản gốc.", false, "A1"),
                    new FunctionArgumentInfo("start_num", "Vị trí bắt đầu thay.", false, "1"),
                    new FunctionArgumentInfo("num_chars", "Số ký tự cần thay.", false, "3"),
                    new FunctionArgumentInfo("new_text", "Chuỗi mới thay vào.", false, "\"ABC\"")),
                new FunctionMetadata("VALUE", "Văn bản", "Chuyển chuỗi văn bản biểu thị số thành giá trị số.", "VALUE(text)", new FunctionArgumentInfo("text", "Chuỗi số.", false, "A1")),
                new FunctionMetadata("REPT", "Văn bản", "Lặp lại văn bản theo số lần chỉ định.", "REPT(text, number_times)",
                    new FunctionArgumentInfo("text", "Ký tự cần lặp.", false, "\"*\""),
                    new FunctionArgumentInfo("number_times", "Số lần lặp.", false, "5")),
                new FunctionMetadata("ROW", "Tra cứu & Tham chiếu", "Trả về số thứ tự hàng của một tham chiếu ô.", "ROW([reference])", new FunctionArgumentInfo("reference", "Địa chỉ ô.", true, "A5")),
                new FunctionMetadata("COLUMN", "Tra cứu & Tham chiếu", "Trả về số thứ tự cột của một tham chiếu ô.", "COLUMN([reference])", new FunctionArgumentInfo("reference", "Địa chỉ ô.", true, "C1")),
                new FunctionMetadata("ROWS", "Tra cứu & Tham chiếu", "Trả về số lượng hàng trong một dải ô.", "ROWS(array)", new FunctionArgumentInfo("array", "Dải ô.", false, "A1:C10")),
                new FunctionMetadata("COLUMNS", "Tra cứu & Tham chiếu", "Trả về số lượng cột trong một dải ô.", "COLUMNS(array)", new FunctionArgumentInfo("array", "Dải ô.", false, "A1:C10")),
                new FunctionMetadata("ADDRESS", "Tra cứu & Tham chiếu", "Tạo chuỗi địa chỉ ô từ số thứ tự hàng và cột.", "ADDRESS(row_num, column_num, [abs_num])",
                    new FunctionArgumentInfo("row_num", "Số hàng.", false, "1"),
                    new FunctionArgumentInfo("column_num", "Số cột.", false, "1")),
                new FunctionMetadata("UNIQUE", "Tra cứu & Tham chiếu", "Trả về danh sách các giá trị duy nhất trong dải ô.", "UNIQUE(array)", new FunctionArgumentInfo("array", "Dải ô cần lọc trùng.", false, "A1:A20")),
                new FunctionMetadata("IFS", "Logic", "Kiểm tra nhiều điều kiện tuần tự và trả về giá trị của điều kiện đúng đầu tiên.", "IFS(logical_test1, value_if_true1, ...)",
                    new FunctionArgumentInfo("logical_test1", "Điều kiện 1.", false, "A1>=8"),
                    new FunctionArgumentInfo("value_if_true1", "Kết quả nếu điều kiện 1 đúng.", false, "\"Giỏi\""),
                    new FunctionArgumentInfo("logical_test2", "Điều kiện 2.", true, "A1>=5"),
                    new FunctionArgumentInfo("value_if_true2", "Kết quả nếu điều kiện 2 đúng.", true, "\"Đạt\"")),
                new FunctionMetadata("SWITCH", "Logic", "So khớp một biểu thức với danh sách giá trị và trả về kết quả tương ứng.", "SWITCH(expression, value1, result1, [default])",
                    new FunctionArgumentInfo("expression", "Giá trị cần so khớp.", false, "A1"),
                    new FunctionArgumentInfo("value1", "Giá trị khớp 1.", false, "1"),
                    new FunctionArgumentInfo("result1", "Kết quả 1.", false, "\"Một\"")),
                new FunctionMetadata("XOR", "Logic", "Trả về TRUE nếu số lượng điều kiện đúng là số lẻ.", "XOR(logical1, [logical2], ...)",
                    new FunctionArgumentInfo("logical1", "Điều kiện 1.", false, "A1>0"),
                    new FunctionArgumentInfo("logical2", "Điều kiện 2.", false, "B1>0")),
                new FunctionMetadata("IFNA", "Logic", "Trả về giá trị thay thế nếu biểu thức bị lỗi #N/A.", "IFNA(value, value_if_na)",
                    new FunctionArgumentInfo("value", "Biểu thức cần kiểm tra.", false, "VLOOKUP(A1,D1:E10,2,0)"),
                    new FunctionArgumentInfo("value_if_na", "Giá trị trả về khi bị #N/A.", false, "\"Không có\"")),
                new FunctionMetadata("ISBLANK", "Thông tin", "Kiểm tra ô có trống hay không.", "ISBLANK(value)", new FunctionArgumentInfo("value", "Ô cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISNUMBER", "Thông tin", "Kiểm tra giá trị có phải là số hay không.", "ISNUMBER(value)", new FunctionArgumentInfo("value", "Ô cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISTEXT", "Thông tin", "Kiểm tra giá trị có phải là chuỗi văn bản hay không.", "ISTEXT(value)", new FunctionArgumentInfo("value", "Ô cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISERROR", "Thông tin", "Kiểm tra giá trị có phải là bất kỳ lỗi nào hay không.", "ISERROR(value)", new FunctionArgumentInfo("value", "Ô cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISNA", "Thông tin", "Kiểm tra giá trị có phải là lỗi #N/A hay không.", "ISNA(value)", new FunctionArgumentInfo("value", "Ô cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISFORMULA", "Thông tin", "Kiểm tra ô có chứa công thức hay không.", "ISFORMULA(reference)", new FunctionArgumentInfo("reference", "Ô cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISEVEN", "Thông tin", "Trả về TRUE nếu số là số chẵn.", "ISEVEN(number)", new FunctionArgumentInfo("number", "Số cần kiểm tra.", false, "A1")),
                new FunctionMetadata("ISODD", "Thông tin", "Trả về TRUE nếu số là số lẻ.", "ISODD(number)", new FunctionArgumentInfo("number", "Số cần kiểm tra.", false, "A1"))
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
                "Tài chính (Financial)",
                "Logic (Logical)",
                "Văn bản (Text)",
                "Tra cứu & Tham chiếu (Lookup & Reference)",
                "Ngày & Giờ (Date & Time)",
                "Thông tin (Information)"
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
                    case 3: matchCat = f.Category == "Tài chính"; break;
                    case 4: matchCat = f.Category == "Logic"; break;
                    case 5: matchCat = f.Category == "Văn bản"; break;
                    case 6: matchCat = f.Category == "Tra cứu & Tham chiếu"; break;
                    case 7: matchCat = f.Category == "Ngày & Giờ"; break;
                    case 8: matchCat = f.Category == "Thông tin"; break;
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
