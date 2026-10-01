# Toàn tập về Shapes trong Microsoft 365 (Word / Excel / PowerPoint / Outlook)

## 1. Cách chèn Shape

**Đường dẫn:** Ribbon **Insert → Illustrations → Shapes** (Word/Excel/PowerPoint/Outlook đều có).

Sau khi bấm Shapes, một thư viện (gallery) hiện ra. Chọn xong một shape, con trỏ chuột đổi thành dấu `+`:
- **Kéo-thả (click and drag):** vẽ shape theo kích thước tuỳ ý, kéo càng dài shape càng lớn.
- **Click một lần:** shape được chèn với kích thước mặc định vào giữa vùng nhìn thấy.
- **Giữ Shift khi kéo:** giữ tỉ lệ vuông/tròn đều (hình chữ nhật → vuông, oval → tròn).
- **Giữ Alt (Windows) khi kéo:** tắt tính năng "snap to grid", cho phép đặt shape ở vị trí bất kỳ, không bị hút theo lưới.

## 2. Cách ghi chữ vào Shape — có phải double-click không?

Đúng, **double-click vào shape** là một trong các cách để bắt đầu gõ chữ, nhưng không phải cách duy nhất. Cụ thể có 3 cách:

1. **Double-click** trực tiếp lên shape → con trỏ nhập liệu xuất hiện ở giữa shape → gõ chữ luôn.
2. **Click 1 lần để chọn shape, rồi gõ luôn** (không cần double-click) — PowerPoint/Word/Excel đều hiểu ngay đó là bạn muốn nhập text vào shape.
3. **Click phải (right-click) vào shape → Edit Text** — cách này chắc chắn nhất khi shape đã có sẵn nội dung khác hoặc bạn không chắc đã chọn đúng shape.

**Lưu ý khi gõ chữ trong shape:**
- Chữ sẽ tự căn giữa theo mặc định (Center/Middle), có thể đổi qua **Format Shape → Text Options → Text Box** để chỉnh căn lề, hướng chữ (xoay 90°/270°), thu nhỏ chữ tự động theo shape (Shrink text on overflow) hoặc cho shape tự phình theo chữ (Resize shape to fit text).
- Muốn xuống dòng trong shape: nhấn **Enter** bình thường (không như ô Excel cần Alt+Enter).
- Click ra ngoài shape (hoặc nhấn **Esc** hai lần) để thoát chế độ gõ chữ.
- Trong **Excel**, một số shape (đặc biệt TextBox) không cho gõ trực tiếp lên ô lưới — luôn cần double-click hoặc chọn rồi gõ như trên; công thức (formula) không gõ được vào Shape, chỉ gõ được văn bản/liên kết ô qua thanh Formula Bar (chọn shape → gõ `=A1` vào formula bar để shape hiển thị nội dung ô A1, tự cập nhật theo).

## 3. Danh sách đầy đủ các nhóm Shape (Shapes Gallery)

Thư viện Shapes trong Microsoft 365 chia thành các nhóm sau (tên nhóm hiện khi bấm Insert → Shapes):

### 3.1 Lines (Đường thẳng/đường nối) — 9 dạng
| Shape | Chức năng |
|---|---|
| Line | Đường thẳng đơn giản |
| Elbow Connector | Đường nối gấp khúc vuông góc, dùng nối 2 shape |
| Elbow Arrow Connector | Như trên nhưng có mũi tên ở đầu |
| Elbow Double-Arrow Connector | Đường nối gấp khúc, mũi tên 2 đầu |
| Curved Connector | Đường nối cong, dùng nối 2 shape mềm mại hơn |
| Curved Arrow Connector | Đường nối cong có mũi tên 1 đầu |
| Curved Double-Arrow Connector | Đường nối cong, mũi tên 2 đầu |
| Freeform | Vẽ hình tự do bằng cách click nhiều điểm neo, đóng hình bằng double-click |
| Scribble | Vẽ nét tự do như bút, giữ chuột kéo tới đâu vẽ tới đó |

**Đặc biệt:** Các Connector (đường nối) khi rê gần một shape khác sẽ hiện các chấm xanh lá — bấm giữ vào chấm đó để **gắn cố định (glue)** đầu đường nối vào shape; khi bạn di chuyển shape, đường nối sẽ tự kéo theo.

### 3.1 Lines (Đường thẳng và đường nối) — 12 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Line** (`line`) | Đường thẳng đơn | Kẻ đường phân cách nội dung hoặc trục toạ độ đơn giản. | Không |
| **Arrow** (`line`) | Mũi tên 1 đầu | Chỉ thị hướng đi, luồng di chuyển hoặc trỏ đích danh đối tượng. | Không |
| **Double Arrow** (`line`) | Mũi tên 2 đầu | Biểu thị khoảng cách, kích thước 2 chiều hoặc quan hệ song phương. | Không |
| **Elbow Connector** (`bentConnector2`) | Đường nối gấp khúc | Nối 2 shape bằng đường bẻ góc 90° vuông vắn, không có mũi tên. | 1 chấm (dời vị trí gập góc) |
| **Elbow Arrow Connector** (`bentConnector3`) | Đường nối gấp khúc 1 mũi tên | Nối các khối trong sơ đồ thuật toán, luồng dữ liệu tiêu chuẩn. | 1 chấm (dời vị trí gập góc) |
| **Elbow Double-Arrow** (`bentConnector4`/`5`) | Đường nối gấp khúc 2 mũi tên | Thể hiện luồng trao đổi dữ liệu qua lại giữa 2 hệ thống. | 1–2 chấm |
| **Curved Connector** (`curvedConnector2`) | Đường nối cong mềm | Tạo liên kết cong mượt mà, hay dùng trong sơ đồ tư duy (Mindmap). | 1 chấm (chỉnh độ võng cung cong) |
| **Curved Arrow Connector** (`curvedConnector3`) | Đường nối cong 1 mũi tên | Luồng chuyển động mềm mại, chỉ hướng xoay vòng hoặc hồi tiếp. | 1 chấm (chỉnh độ võng) |
| **Curved Double-Arrow** (`curvedConnector4`/`5`) | Đường nối cong 2 mũi tên | Tương tác qua lại 2 chiều mềm mại giữa 2 đối tượng đồ họa. | 1 chấm (chỉnh độ võng) |
| **Curve** (`curve`) | Đường cong đa điểm | Nhấp chuột tại các đỉnh để tạo đường lượn sóng tự do mượt mà. | Edit Points |
| **Freeform: Shape** (`freeform`) | Đa giác khép kín tự do | Nhấp nhiều điểm neo liên tiếp để tạo hình học bất kỳ tùy biến. | Edit Points |
| **Freeform: Scribble** (`scribble`) | Nét vẽ bút chì tự do | Giữ chuột kéo rê để viết chữ hoặc phác thảo như vẽ tay trên giấy. | Edit Points |

> **Đặc biệt (Glue Connectors):** Khi rê chuột gần mép một shape khác, các điểm neo màu xanh lá cây hoặc xám sẽ xuất hiện. Thả đầu đường nối vào chấm đó để **dán chặt (glue)**; khi bạn di dời shape đi đâu, đường nối sẽ tự động co giãn và bám theo đến đó.

---

### 3.2 Rectangles (Hình chữ nhật & Các biến thể góc) — 9 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Rectangle** (`rect`) | Hình chữ nhật góc vuông | Khối hộp nền tảng để tạo thẻ card, banner, khung chứa nội dung. | Không |
| **Rounded Rectangle** (`roundRect`) | Chữ nhật bo tròn 4 góc | Dạng phổ biến nhất trong thiết kế UI hiện đại (nút bấm, thẻ thông tin). | 1 chấm (chỉnh bán kính bo góc) |
| **Single Corner Snipped** (`snip1Rect`) | Vát 1 góc trên bên phải | Phong cách thiết kế công nghệ, thẻ chip, bảng mã vạch. | 1 chấm (độ sâu vết cắt) |
| **Top Corners Snipped** (`snip2SameRect`) | Vát 2 góc cùng phía trên | Làm tab thư mục, nhãn kẹp hồ sơ, phần đầu card giao diện. | 2 chấm (độ sâu cắt từng góc) |
| **Diagonal Corners Snipped** (`snip2DiagRect`) | Vát 2 góc chéo nhau | Phong cách bảng thông số game, tương lai, nhãn tem sắc cạnh. | 2 chấm (độ sâu 2 góc chéo) |
| **Snip and Round Single Corner** (`snipRoundRect`) | 1 góc vát phẳng, 1 góc bo tròn | Thiết kế bất đối xứng sáng tạo cho thẻ bài, nhãn giá. | 2 chấm (1 cắt vát, 1 bo tròn) |
| **Round Single Corner** (`round1Rect`) | Bo tròn 1 góc duy nhất | Làm mũi chỉ định hướng hoặc thẻ ghi chú treo góc. | 1 chấm (bán kính góc bo) |
| **Same Side Corner Rounded** (`round2SameRect`) | Bo tròn 2 góc cùng 1 cạnh | Dùng làm nút bấm đáy phẳng, tab trình duyệt, mái vòm thẻ. | 2 chấm (bán kính 2 góc) |
| **Diagonal Corner Rounded** (`round2DiagRect`) | Bo tròn 2 góc đối xứng chéo | Tạo hình chiếc lá hoặc khối hình học cách điệu nghệ thuật. | 2 chấm (bán kính 2 góc chéo) |

---

### 3.3 Basic Shapes (Hình học cơ bản) — 42 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Text Box** (`txBox`) | Hộp văn bản | Vùng chứa chữ không nền, không viền, đặt tự do trên trang. | Không |
| **Oval** (`ellipse`) | Hình bầu dục / Hình tròn | Vẽ hình elip hoặc giữ Shift để vẽ hình tròn hoàn hảo 1:1. | Không |
| **Isosceles Triangle** (`triangle`) | Tam giác cân | Chỉ hướng lên trên, làm kim tự tháp cấp bậc phân tầng. | 1 chấm (dời vị trí đỉnh nhọn) |
| **Right Triangle** (`rtTriangle`) | Tam giác vuông | Minh họa toán học góc vuông, đường dốc, góc cua kỹ thuật. | Không |
| **Parallelogram** (`parallelogram`) | Hình bình hành | Dùng trong flowchart (nhập/xuất dữ liệu) hoặc thẻ nghiêng tốc độ. | 1 chấm (độ nghiêng cạnh bên) |
| **Trapezoid** (`trapezoid`) | Hình thang | Vẽ hình mái nhà, phễu lọc chuyển đổi dữ liệu (Funnel). | 1 chấm (độ vát đáy trên) |
| **Diamond** (`diamond`) | Hình thoi | Biểu tượng quyết định điều kiện rẽ nhánh (Decision) trong lưu đồ. | Không |
| **Regular Pentagon** (`pentagon`) | Ngũ giác đều (5 cạnh) | Sơ đồ ma trận 5 yếu tố, huy hiệu 5 sao. | Không |
| **Hexagon** (`hexagon`) | Lục giác đều (6 cạnh) | Tạo tổ ong dữ liệu, biểu tượng hóa học, ma trận công nghệ. | 1 chấm (chiều sâu góc nhọn bên) |
| **Heptagon** (`heptagon`) | Thất giác đều (7 cạnh) | Biểu thị chu kỳ 7 ngày trong tuần, 7 nguyên tắc cốt lõi. | Không |
| **Octagon** (`octagon`) | Bát giác đều (8 cạnh) | Biển báo STOP dừng lại, kiến trúc 8 phương hướng. | 1 chấm (độ vát 4 góc) |
| **Decagon** (`decagon`) | Thập giác đều (10 cạnh) | Đồ thị 10 tiêu chí đánh giá KPI, bánh đà tròn đa cạnh. | Không |
| **Dodecagon** (`dodecagon`) | Hình 12 cạnh đều | Tượng trưng cho 12 tháng trong năm, mặt đồng hồ 12 giờ. | Không |
| **Pie** (`pie`) | Miếng bánh tròn / Quạt tròn | Vẽ lát cắt thị phần biểu đồ tròn, tiến độ công việc dạng bánh quạt. | 2 chấm (góc bắt đầu & góc mở) |
| **Chord** (`chord`) | Hình cung tròn chắn dây | Vẽ phân đoạn hình học cung tròn có đáy phẳng. | 2 chấm (điểm đầu và điểm cuối) |
| **Teardrop** (`teardrop`) | Hình giọt nước | Biểu tượng định vị bản đồ (Map pin pinpoint), giọt nước, dầu khí. | 1 chấm (độ nhọn đuôi giọt nước) |
| **Frame** (`frame`) | Khung tranh rỗng ruột | Khung viền bao quanh ảnh chân dung hoặc khối nổi bật. | 1 chấm (độ dày đường viền khung) |
| **Half Frame** (`halfFrame`) | Khung góc chữ L đối xứng | Khung ngắm tiêu điểm ảnh, góc định vị bố cục. | 2 chấm (độ dày 2 cạnh thước) |
| **L-Shape** (`corner`) | Khối góc vuông chữ L | Kẻ góc chia vùng màn hình hoặc giá đỡ đồ họa. | 2 chấm (độ dày nhánh ngang/dọc) |
| **Diagonal Stripe** (`diagStripe`) | Dải băng chéo | Nhãn tem giảm giá "HOT/NEW" đè chéo ở góc trang/ảnh. | 1 chấm (độ rộng dải băng) |
| **Cross** (`cross`) | Dấu cộng / Chữ thập đỏ | Biểu tượng y tế, cứu thương, thao tác cộng hợp nhất. | 1 chấm (độ dày thanh chữ thập) |
| **Plaque** (`plaque`) | Biển phù hiệu bảng tên | Bảng tên chức danh cổ điển, thẻ bài khen thưởng vinh danh. | 1 chấm (độ lượn góc viền) |
| **Can** (`can`) | Hình trụ đứng (Cylinder) | Mô phỏng cơ sở dữ liệu (Database), bình chứa, lon nước giải khát. | 1 chấm (độ lồi elip nắp trên) |
| **Cube** (`cube`) | Khối lập phương 3D | Minh họa sản phẩm đóng gói, khối linh kiện kiến trúc. | 1 chấm (chiều sâu & góc nhìn mặt trên) |
| **Bevel** (`bevel`) | Nút khối vát cạnh | Tạo cảm giác nút bấm vật lý 3D nổi gờ khỏi mặt phẳng. | 1 chấm (bề rộng đường vát mép) |
| **Donut** (`donut`) | Hình xuyến bánh vòng | Biểu đồ vòng tròn tiến độ rỗng ruột, đồng hồ đếm ngược. | 1 chấm (đường kính lỗ rỗng giữa) |
| **"No" Symbol** (`noSmoking`) | Biển báo cấm (Gạch chéo) | Biển báo cấm đoán, hạn chế, vô hiệu hóa tính năng. | 1 chấm (độ dày viền và nét gạch) |
| **Block Arc** (`blockArc`) | Vòng cung khối dày | Vẽ thước đo đồng hồ tốc độ (Gauge), vòng cung tiến độ phần trăm. | 3 chấm (bán kính trong, góc bắt đầu, quét) |
| **Folded Corner** (`folderCorner`) | Trang giấy gấp góc | Biểu tượng tệp tài liệu, trang văn bản đang mở trang mới. | 1 chấm (kích thước nếp gấp góc) |
| **Smiley Face** (`smileyFace`) | Mặt cười ngộ nghĩnh | Khảo sát mức độ hài lòng khách hàng (CSAT/NPS). | 1 chấm (kéo lên = cười, kéo xuống = mếu) |
| **Heart** (`heart`) | Trái tim tình cảm | Biểu tượng yêu thích (Like/Favorite), y tế tim mạch, từ thiện. | Không |
| **Lightning Bolt** (`lightningBolt`) | Tia chớp sấm sét | Biểu tượng cảnh báo điện áp cao, tốc độ sạc nhanh, đột phá năng lượng. | Không |
| **Sun** (`sun`) | Mặt trời tỏa sáng | Thời tiết nắng ấm, trung tâm năng lượng, sáng tạo rực rỡ. | 1 chấm (tỷ lệ vòng tròn tâm / tia nắng) |
| **Moon** (`moon`) | Mặt trăng lưỡi liềm | Chế độ ban đêm (Dark mode), chu kỳ âm lịch, thời gian ngủ. | 1 chấm (độ khuyết lưỡi liềm) |
| **Cloud** (`cloud`) | Đám mây lưu trữ | Biểu tượng điện toán đám mây (Cloud storage/SaaS), dự báo thời tiết. | Không |
| **Arc** (`arc`) | Cung tròn mở (Nét đơn) | Thước đo góc toán học, quỹ đạo bay vệ tinh. | 2 chấm (điểm đầu và điểm cuối cung) |
| **Double Bracket** (`bracketPair`) | Cặp dấu ngoặc vuông `[ ]` | Nhóm gộp một tập hợp nhiều dòng dữ liệu, công thức ma trận. | 1 chấm (độ cong góc ngoặc) |
| **Double Brace** (`bracePair`) | Cặp dấu ngoặc nhọn `{ }` | Nhóm các thành phần danh mục con vào một tiêu đề cha. | 2 chấm (độ dày & vị trí mũi nhọn giữa) |
| **Left Bracket** (`leftBracket`) | Dấu ngoặc vuông mở `[` | Gộp nhóm danh mục từ cạnh trái. | 1 chấm (độ cong mép) |
| **Right Bracket** (`rightBracket`) | Dấu ngoặc vuông đóng `]` | Gộp nhóm danh mục dẫn về kết quả cạnh phải. | 1 chấm (độ cong mép) |
| **Left Brace** (`leftBrace`) | Dấu ngoặc nhọn mở `{` | Dùng rất phổ biến trong trình bày mục con phân rã ý tưởng. | 2 chấm (độ cong & đỉnh nhọn) |
| **Right Brace** (`rightBrace`) | Dấu ngoặc nhọn đóng `}` | Dùng kết luận quy tụ nhiều nguyên nhân về một kết quả. | 2 chấm (độ cong & đỉnh nhọn) |

---

### 3.4 Block Arrows (Mũi tên khối) — 28 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Right Arrow** (`rightArrow`) | Mũi tên sang phải | Luồng tiến trình bước tiếp theo (Next step), phát triển tương lai. | 2 chấm (độ nở đầu & bề dày thân) |
| **Left Arrow** (`leftArrow`) | Mũi tên sang trái | Quay lại bước trước (Previous), luồng quá khứ, thoái lui. | 2 chấm (độ nở đầu & bề dày thân) |
| **Up Arrow** (`upArrow`) | Mũi tên hướng lên | Tăng trưởng doanh thu, thăng tiến vị trí, tối ưu hóa hiệu suất. | 2 chấm (độ nở đầu & bề dày thân) |
| **Down Arrow** (`downArrow`) | Mũi tên hướng xuống | Giảm chi phí, tỷ lệ lỗi sụt giảm, chỉ dẫn phân tích cấp dưới. | 2 chấm (độ nở đầu & bề dày thân) |
| **Left-Right Arrow** (`leftRightArrow`) | Mũi tên 2 đầu ngang | So sánh tương quan 2 thực thể, giao dịch hoán đổi ngang hàng. | 2 chấm (độ nở đầu & bề dày thân) |
| **Up-Down Arrow** (`upDownArrow`) | Mũi tên 2 đầu dọc | Dao động tăng giảm giá trị, cân bằng cung cầu trên dưới. | 2 chấm (độ nở đầu & bề dày thân) |
| **Quad Arrow** (`quadArrow`) | Mũi tên chữ thập 4 hướng | Điều hướng 4 phương, mở rộng quy mô toàn diện, phân tán lực. | 3 chấm (độ nở đầu, độ dày, cổ trục) |
| **Left-Right-Up Arrow** (`leftRightUpArrow`) | Mũi tên rẽ 3 ngả | Phân luồng lựa chọn 3 hướng đi khác nhau từ một xuất phát điểm. | 3 chấm (điều chỉnh kích thước các nhánh) |
| **Bent Arrow** (`bentArrow`) | Mũi tên gập góc 90° | Chuyển hướng dòng chảy đột ngột sang một ngách chức năng mới. | 2 chấm (vị trí bẻ góc & kích thước đầu) |
| **U-Turn Arrow** (`uturnArrow`) | Mũi tên quay đầu chữ U | Vòng quay phản hồi (Feedback loop), cơ chế hồi quy, quay xe. | 4 chấm (khoảng cách 2 nhánh, đầu tên, độ cong) |
| **Left-Up Arrow** (`leftUpArrow`) | Mũi tên nhánh chéo lên | Nâng cấp vượt bậc hoặc phân nhánh phụ lên trên. | 3 chấm (độ dày nhánh & góc nhọn) |
| **Bent-Up Arrow** (`bentUpArrow`) | Mũi tên uốn khúc hướng lên | Dòng dữ liệu từ hạ tầng vươn lên tầng ứng dụng. | 2 chấm (vị trí gập & đầu mũi tên) |
| **Curved Right Arrow** (`curvedRightArrow`) | Mũi tên cong lượn phải | Dẫn dắt ánh nhìn người đọc lướt qua trang tiếp theo nhẹ nhàng. | 3 chấm (bán kính cong, độ dày thân, đầu tên) |
| **Curved Left Arrow** (`curvedLeftArrow`) | Mũi tên cong lượn trái | Vòng lặp phản xạ, liên kết ngược trở lại tiền đề ban đầu. | 3 chấm (bán kính cong, độ dày thân, đầu tên) |
| **Curved Up Arrow** (`curvedUpArrow`) | Mũi tên cong vút lên | Tăng trưởng ngoạn mục hình parabol, bứt phá giới hạn. | 3 chấm (độ cong vút & kích thước đầu) |
| **Curved Down Arrow** (`curvedDownArrow`) | Mũi tên cong vòng xuống | Hạ cánh an toàn, luồng suy giảm có kiểm soát. | 3 chấm (độ cong & kích thước đầu) |
| **Striped Right Arrow** (`stripedRightArrow`) | Mũi tên sọc đuôi | Thể hiện tốc độ tăng tốc, chuyển động nhanh và uy lực. | 2 chấm (khoảng cách các vạch rãnh đuôi) |
| **Notched Right Arrow** (`notchedRightArrow`) | Mũi tên đuôi én | Hình khối trang trí quy trình hiện đại, khuyết đuôi chữ V. | 2 chấm (độ khuyết đuôi & đầu mũi tên) |
| **Pentagon (Home Plate)** (`homePlate`) | Mũi tên ngắn / Ngũ giác | Nút bước 1, 2, 3 trong thanh tiến trình giai đoạn dự án. | 1 chấm (độ nhọn đỉnh đầu) |
| **Chevron** (`chevron`) | Mũi tên góc chữ V | Dùng xếp nối tiếp nhau thành chuỗi tiến trình cung ứng (Supply chain). | 1 chấm (độ sâu góc lõm chữ V) |
| **Circular Arrow** (`circularArrow`) | Mũi tên xoay vòng tròn | Quy trình PDCA (Plan-Do-Check-Act), tuần hoàn tái chế khép kín. | 3 chấm (bán kính trong, góc quét, đầu mũi tên) |
| **Right Arrow Callout** (`rightArrowCallout`) | Hộp chú thích mũi tên phải | Thẻ thông tin vừa ghi chữ vừa chỉ đích danh vị trí bên phải. | 3 chấm (kích thước hộp & vị trí đuôi tên) |
| **Down Arrow Callout** (`downArrowCallout`) | Hộp chú thích mũi tên dưới | Chú giải số liệu cho biểu đồ cột hoặc bảng ở bên dưới. | 3 chấm (kích thước hộp & vị trí đuôi tên) |
| **Left Arrow Callout** (`leftArrowCallout`) | Hộp chú thích mũi tên trái | Chú giải chi tiết cho một đối tượng ở cạnh trái màn hình. | 3 chấm (kích thước hộp & vị trí đuôi tên) |
| **Up Arrow Callout** (`upArrowCallout`) | Hộp chú thích mũi tên trên | Chú giải cho tiêu đề hoặc phần tử nằm ngay phía trên. | 3 chấm (kích thước hộp & vị trí đuôi tên) |
| **Left-Right Arrow Callout** (`leftRightArrowCallout`) | Hộp chú thích 2 đầu ngang | Nằm ở giữa giải thích sự kết nối trung gian giữa 2 đối tác. | 3 chấm (kích thước hộp & độ dài 2 mũi tên) |
| **Quad Arrow Callout** (`quadArrowCallout`) | Hộp chú thích 4 hướng | Hộp trung tâm tỏa thông điệp ra 4 khối bao quanh. | 4 chấm (kích thước thân và 4 mũi tên) |
| **Swoosh Arrow** (`swooshArrow`) | Mũi tên lượn nghệ thuật | Tạo đường cong nghệ thuật bắt mắt cho slide tiếp thị sản phẩm. | 2 chấm (độ xoắn lượn & đầu tên) |

---

### 3.5 Equation Shapes (Ký hiệu toán học khối) — 6 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Plus** (`mathPlus`) | Dấu cộng khối | Thể hiện phép tính cộng, tích hợp tính năng, ưu điểm cộng thêm. | 1 chấm (độ dày nét chữ thập) |
| **Minus** (`mathMinus`) | Dấu trừ khối | Phép trừ, tiết kiệm chi phí, loại bỏ rủi ro, danh mục giảm trừ. | 1 chấm (độ dày thanh ngang) |
| **Multiply** (`mathMultiply`) | Dấu nhân khối (Chữ X) | Hiệu ứng nhân đôi sức mạnh, cấp số nhân, liên doanh hợp tác. | 1 chấm (độ dày nét chéo) |
| **Divide** (`mathDivide`) | Dấu chia khối | Phép chia tỷ lệ, phân bổ ngân sách, chia sẻ tài nguyên đồng đều. | 3 chấm (độ dày thanh & 2 chấm tròn) |
| **Equal** (`mathEqual`) | Dấu bằng khối | Kết quả cân bằng, đẳng thức giá trị, hiệu quả tương đương. | 1 chấm (khoảng cách giữa 2 thanh song song) |
| **Not Equal** (`mathNotEqual`) | Dấu khác khối | Sự khác biệt đẳng cấp, bài toán phân biệt đối trọng phi đối xứng. | 2 chấm (độ nghiêng nét gạch chéo) |

---

### 3.6 Flowchart (Lưu đồ quy trình chuẩn ISO/ANSI) — 28 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Chuẩn quy ước nghiệp vụ quốc tế |
|---|---|---|
| **Flowchart: Process** (`flowChartProcess`) | Bước xử lý (Hình chữ nhật) | Thực hiện một tác vụ cụ thể, tính toán công thức hoặc hành động đơn lẻ. |
| **Flowchart: Alternate Process** (`flowChartAlternateProcess`) | Xử lý thay thế (Chữ nhật bo góc) | Đường lối xử lý dự phòng nếu bước chính gặp sự cố hoặc điều kiện ngoại lệ. |
| **Flowchart: Decision** (`flowChartDecision`) | Ra quyết định (Hình thoi) | Điểm rẽ nhánh điều kiện logic (Đúng/Sai, Có/Không, Đạt/Trượt). |
| **Flowchart: Data** (`flowChartInputOutput`) | Dữ liệu Nhập/Xuất (Hình bình hành) | Nhận dữ liệu đầu vào hoặc xuất kết quả ra cho người dùng/hệ thống khác. |
| **Flowchart: Predefined Process** (`flowChartPredefinedProcess`) | Tiến trình con (Chữ nhật 2 vạch) | Gọi một hàm, thủ tục hoặc quy trình con đã được định nghĩa ở tài liệu khác. |
| **Flowchart: Internal Storage** (`flowChartInternalStorage`) | Bộ nhớ nội bộ (Chữ nhật vạch góc) | Lưu trữ tạm dữ liệu trong bộ nhớ RAM của chương trình đang chạy. |
| **Flowchart: Document** (`flowChartDocument`) | Tài liệu in ấn (Đáy lượn sóng) | Sinh ra chứng từ in, hóa đơn, báo cáo giấy gửi cho khách hàng. |
| **Flowchart: Multidocument** (`flowChartMultidocument`) | Xấp tài liệu (Nhiều lớp sóng) | Tập hợp nhiều hồ sơ, tập tài liệu hợp đồng nhiều trang. |
| **Flowchart: Terminator** (`flowChartTerminator`) | Điểm đầu/cuối (Oval con thoi) | Khởi đầu (Start) hoặc Kết thúc (End) toàn bộ chuỗi quy trình. |
| **Flowchart: Preparation** (`flowChartPreparation`) | Chuẩn bị (Lục giác ngang) | Khởi tạo thông số ban đầu, đặt giá trị mặc định cho biến đếm vòng lặp. |
| **Flowchart: Manual Input** (`flowChartManualInput`) | Nhập thủ công (Vát đỉnh) | Người thao tác gõ dữ liệu trực tiếp từ bàn phím, quét mã barcode. |
| **Flowchart: Manual Operation** (`flowChartManualOperation`) | Tác vụ thủ công (Hình thang ngược) | Bất kỳ thao tác vật lý nào con người tự làm bằng tay không qua máy tính. |
| **Flowchart: Connector** (`flowChartConnector`) | Điểm nối trong trang (Tròn nhỏ) | Nối các đường luồng chéo nhau trên cùng một trang giấy để tránh vẽ rối mắt. |
| **Flowchart: Off-page Connector** (`flowChartOffpageConnector`) | Điểm nối sang trang (Ngũ giác nhọn) | Chỉ thị luồng quy trình sẽ tiếp tục chạy ở trang số tiếp theo. |
| **Flowchart: Card** (`flowChartPunchedCard`) | Thẻ đục lỗ (Vát 1 góc trên) | Thiết bị thẻ từ, thẻ chấm công, thẻ đục lỗ lịch sử máy tính. |
| **Flowchart: Punched Tape** (`flowChartPunchedTape`) | Băng đục lỗ (Sóng 2 đầu) | Băng từ lưu trữ dữ liệu viễn thông cổ điển. |
| **Flowchart: Summing Junction** (`flowChartSummingJunction`) | Điểm hội tụ (Vòng tròn chữ X) | Nơi nhiều tín hiệu hoặc luồng dữ liệu độc lập cùng hội tụ về một mối. |
| **Flowchart: Or** (`flowChartOr`) | Cổng logic OR (Vòng tròn dấu +) | Luồng chỉ tiếp tục nếu có ít nhất một trong các nhánh điều kiện kích hoạt. |
| **Flowchart: Collate** (`flowChartCollate`) | Đối chiếu (Đồng hồ cát) | Thu thập và sắp xếp các tài liệu theo đúng định dạng mẫu quy định. |
| **Flowchart: Sort** (`flowChartSort`) | Phân loại (Hình thoi chia đôi) | Sắp xếp các bản ghi danh sách theo thứ tự bảng chữ cái hoặc số tăng dần. |
| **Flowchart: Extract** (`flowChartExtract`) | Trích xuất (Tam giác nhọn trên) | Tách một tập hợp lớn thành các nhóm nhỏ theo tiêu chí phân loại. |
| **Flowchart: Merge** (`flowChartMerge`) | Hợp nhất (Tam giác ngược) | Gộp các danh mục riêng lẻ thành một bộ cơ sở dữ liệu chung duy nhất. |
| **Flowchart: Stored Data** (`flowChartOnlineStorage`) | Dữ liệu lưu trữ (Đáy tròn trái) | Bất kỳ cơ sở dữ liệu điện tử nào đang lưu trên máy chủ trực tuyến. |
| **Flowchart: Delay** (`flowChartDelay`) | Trì hoãn / Chờ đợi (Bán nguyệt dài) | Khoảng thời gian chờ đợi phản hồi từ đối tác, khách hàng hoặc hệ thống. |
| **Flowchart: Sequential Access** (`flowChartMagneticTape`) | Bộ nhớ tuần tự (Tròn có đuôi) | Băng từ sao lưu (Tape backup) phải đọc tuần tự từ đầu đến cuối. |
| **Flowchart: Magnetic Disk** (`flowChartMagneticDisk`) | Đĩa từ / Ổ cứng (Hình trụ đứng) | Cơ sở dữ liệu quan hệ (SQL/Server) truy xuất ngẫu nhiên tốc độ cao. |
| **Flowchart: Direct Access** (`flowChartMagneticDrum`) | Trống từ (Hình trụ ngang) | Bộ nhớ trống từ cổ điển trong ngành khoa học máy tính. |
| **Flowchart: Display** (`flowChartDisplay`) | Màn hình hiển thị (Đầu tròn phải) | Xuất thông tin hiển thị lên màn hình monitor, TV hoặc máy chiếu. |

---

### 3.7 Stars and Banners (Ngôi sao & Băng rôn huy hiệu) — 18 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Explosion 1** (`star4`) | Ngôi sao nổ 4 cánh | Tạo điểm nhấn cho ưu đãi sốc, thông báo chớp nhoáng "SALE!". | 1 chấm (độ sâu cánh sao) |
| **Explosion 2** (`star8`) | Ngôi sao nổ 8 cánh | Nhãn dán giảm giá sốc, quà tặng kèm theo sản phẩm. | 1 chấm (độ nhọn cánh nổ) |
| **4-Point Star** (`star4`) | Ngôi sao 4 cánh la bàn | Biểu thị phương hướng Đông-Tây-Nam-Bắc, hoa tiêu định vị. | 1 chấm (độ sâu cánh sao) |
| **5-Point Star** (`star5`) | Ngôi sao 5 cánh | Quốc kỳ, xếp hạng sao đánh giá chất lượng (1★ đến 5★). | 1 chấm (độ mập/ốm của cánh sao) |
| **6-Point Star** (`star6`) | Ngôi sao 6 cánh (David) | Biểu tượng văn hóa, huy hiệu cảnh sát trưởng (Sheriff). | 1 chấm (chiều sâu cánh sao) |
| **7-Point Star** (`star7`) | Ngôi sao 7 cánh | Biểu tượng thần thoại, 7 kỳ quan thế giới, 7 sắc cầu vồng. | 1 chấm (chiều sâu cánh sao) |
| **8-Point Star** (`star8`) | Ngôi sao 8 cánh đều | Họa tiết gạch trang trí kiến trúc, huy chương hiệp sĩ. | 1 chấm (chiều sâu cánh sao) |
| **10-Point Star** (`star10`) | Ngôi sao 10 cánh | Đánh giá thang điểm 10/10 tuyệt đối, thành tích xuất sắc. | 1 chấm (chiều sâu cánh sao) |
| **12-Point Star** (`star12`) | Ngôi sao 12 cánh | 12 cung hoàng đạo, đồng hồ thời gian thế kỷ. | 1 chấm (chiều sâu cánh sao) |
| **16-Point Star** (`star16`) | Ngôi sao 16 cánh | Mặt trời mọc rực rỡ, tem chứng nhận nguồn gốc xuất xứ. | 1 chấm (chiều sâu cánh sao) |
| **24-Point Star** (`star24`) | Ngôi sao 24 cánh | Bánh xe luân hồi pháp luân, mặt trống đồng Đông Sơn. | 1 chấm (chiều sâu cánh sao) |
| **32-Point Star** (`star32`) | Ngôi sao 32 cánh | Con dấu nổi niêm phong sáp bảo mật, chứng chỉ ISO cao cấp. | 1 chấm (chiều sâu cánh sao) |
| **Up Ribbon** (`ribbon`) | Băng rôn uốn cong lên | Biểu ngữ vinh danh danh hiệu đứng đầu, tiêu đề kỷ niệm. | 2 chấm (chiều sâu võng & độ dài đuôi) |
| **Down Ribbon** (`ribbon2`) | Băng rôn uốn cong xuống | Tiêu đề chân trang vinh danh, chân dung người sáng lập. | 2 chấm (chiều sâu võng & độ dài đuôi) |
| **Curved Up Ribbon** (`ellipseRibbon`) | Băng rôn vòng cung lên | Uốn quanh viền trên của logo hình tròn hoặc huy hiệu. | 3 chấm (độ cong elip & 2 đuôi nheo) |
| **Curved Down Ribbon** (`ellipseRibbon2`)| Băng rôn vòng cung xuống | Uốn đỡ lấy chân logo hình tròn truyền thống. | 3 chấm (độ cong elip & 2 đuôi nheo) |
| **Horizontal Scroll** (`horizontalScroll`)| Cuộn giấy chỉ dụ ngang | Trình bày bản tuyên ngôn, chỉ dụ hoàng gia, sắc lệnh công bố. | 2 chấm (độ cuộn 2 đầu giấy) |
| **Vertical Scroll** (`verticalScroll`) | Cuộn giấy thánh thư dọc | Trình bày thư pháp, gia phả dòng họ, bản cam kết lịch sử. | 2 chấm (độ cuộn 2 mép trên dưới) |
| **Wave** (`wave`) | Ngọn sóng đơn | Phân cách chuyển tiếp mềm giữa 2 khối nội dung slide. | 1 chấm (biên độ dao động sóng) |
| **Double Wave** (`doubleWave`) | Ngọn sóng kép song song | Minh họa nhịp điệu sinh thái, dòng chảy đại dương êm dịu. | 1 chấm (biên độ sóng kép) |

---

### 3.8 Callouts (Bong bóng thoại & Chú thích) — 16 dạng

| Tên Shape (Preset) | Tên tiếng Việt | Công năng & Đặc điểm thực tế | Chấm vàng (Adjustment) |
|---|---|---|:---:|
| **Rectangular Callout** (`wedgeRectCallout`) | Bong bóng thoại chữ nhật | Lời thoại nhân vật truyện tranh, chú thích văn bản trực diện. | 2 chấm (vị trí gốc & mũi trỏ) |
| **Rounded Rectangular Callout** (`wedgeRoundRectCallout`)| Bong bóng thoại bo góc | Lời thoại tin nhắn chat hiện đại (Zalo, Messenger, iMessage). | 3 chấm (bo góc + mũi trỏ thoại) |
| **Oval Callout** (`wedgeEllipseCallout`) | Bong bóng thoại bầu dục | Tiếng nói chuyện trực tiếp trong hội thoại truyện tranh manga. | 2 chấm (tọa độ mũi trỏ thoại) |
| **Cloud Callout** (`cloudCallout`) | Đám mây suy nghĩ | Tiếng lòng suy nghĩ nội tâm của nhân vật, ước mơ, tưởng tượng. | 2 chấm (vị trí các bong bóng dẫn) |
| **Line Callout 1** (`borderCallout1`) | Chỉ dẫn 1 đoạn thẳng | Đường trỏ thẳng tắp có khung hộp chữ nhật bao bọc. | 3 chấm (điểm chạm, góc ngoặc, khung) |
| **Line Callout 2** (`borderCallout2`) | Chỉ dẫn 2 đoạn góc vuông | Đường dẫn bẻ góc 90° chỉ vào chi tiết máy móc phức tạp. | 4 chấm (các khớp gãy khúc & khung) |
| **Line Callout 3** (`borderCallout3`) | Chỉ dẫn 3 đoạn thước đo | Đường dẫn kỹ thuật tiêu chuẩn bản vẽ cơ khí AutoCAD. | 4 chấm (tọa độ từng đoạn thước) |
| **Line Callout 4** (`borderCallout4`) | Chỉ dẫn xiên góc có khung | Đường dẫn vát chéo vào một vị trí trọng yếu trên bức ảnh. | 3 chấm (độ dốc & khung chứa) |
| **Line Callout 1 (No Border)** (`callout1`) | Chỉ dẫn 1 đoạn không viền | Phong cách chú thích tối giản, chữ nằm nổi không khung hộp. | 2 chấm (điểm chạm & vị trí chữ) |
| **Line Callout 2 (No Border)** (`callout2`) | Chỉ dẫn 2 đoạn không viền | Tối giản thanh lịch, phù hợp slide thuyết trình công nghệ. | 3 chấm (khớp bẻ góc & điểm đặt chữ) |
| **Line Callout 3 (No Border)** (`callout3`) | Chỉ dẫn 3 đoạn không viền | Đoạn thẳng có thanh gác ngang nâng đỡ dòng chữ thuyết minh. | 3 chấm (điểm trỏ & thanh gác) |
| **Line Callout 4 (No Border)** (`callout4`) | Chỉ dẫn xiên không viền | Đường chỉ xiên tinh gọn vào vị trí linh kiện giải phẫu. | 2 chấm (độ xiên & chân chữ) |
| **Line Callout 1 (Accent Bar)** (`accentCallout1`)| Chỉ dẫn có vạch nhấn 1 đoạn | Có thêm một vạch đứng nổi bật ở đầu dòng chữ để thu hút mắt nhìn. | 3 chấm (điểm chạm & vạch nhấn) |
| **Line Callout 2 (Accent Bar)** (`accentCallout2`)| Chỉ dẫn có vạch nhấn 2 đoạn | Phong cách tạp chí khoa học chuyên nghiệp. | 4 chấm (khớp nối & vạch nhấn) |
| **Line Callout 3 (Accent Bar)** (`accentCallout3`)| Chỉ dẫn có vạch nhấn 3 đoạn | Đường gióng kỹ thuật có thanh chặn nổi bật. | 4 chấm (khớp gãy & thanh chặn) |
| **Line Callout 4 (Accent Bar)** (`accentCallout4`)| Chỉ dẫn có vạch nhấn xiên | Nêu bật thông số kỹ thuật cốt lõi của một sản phẩm phần cứng. | 3 chấm (độ xiên & thanh vạch) |

---

### 3.9 Action Buttons (Nút điều hướng trình chiếu — PowerPoint) — 12 dạng

Các nút bấm này đi kèm hộp thoại **Action Settings** để tương tác trực tiếp khi đang Slide Show:

| Tên Nút Action (Preset) | Biểu tượng mặc định | Hành động tương tác mặc định (Khi Click Chuột) |
|---|:---:|---|
| **Action Button: Back or Previous** (`actionButtonBackPrevious`) | ◀ (Tam giác trái) | Nhảy ngay về Slide đứng liền trước đó. |
| **Action Button: Forward or Next** (`actionButtonForwardNext`) | ▶ (Tam giác phải) | Chuyển tiếp ngay sang Slide tiếp theo. |
| **Action Button: Beginning** (`actionButtonBeginning`) | ⏮ (Mũi tên chạm vạch) | Quay trở về Slide đầu tiên của bài thuyết trình. |
| **Action Button: End** (`actionButtonEnd`) | ⏭ (Mũi tên chạm vạch) | Nhảy vọt tới Slide cuối cùng của bài trình chiếu. |
| **Action Button: Home** (`actionButtonHome`) | 🏠 (Ngôi nhà) | Trở về Slide Menu chính / Trang chủ điều hướng. |
| **Action Button: Information** (`actionButtonInformation`) | ℹ (Chữ i trong tròn) | Mở trang thông tin trợ giúp hoặc hiển thị popup giải thích. |
| **Action Button: Return** (`actionButtonReturn`) | ↩ (Mũi tên uốn ngược)| Trở về Slide vừa xem trước đó (dù có vừa nhảy cách xa hàng chục slide). |
| **Action Button: Movie** (`actionButtonMovie`) | 🎥 (Máy quay phim) | Kích hoạt phát đoạn video minh họa nhúng trong bài. |
| **Action Button: Document** (`actionButtonDocument`) | 📄 (Trang giấy) | Mở một tệp tài liệu PDF, DOCX hoặc Excel đính kèm từ bên ngoài. |
| **Action Button: Sound** (`actionButtonSound`) | 🔊 (Chiếc loa) | Phát hiệu ứng âm thanh hoặc tệp nhạc audio minh họa. |
| **Action Button: Help** (`actionButtonHelp`) | ❓ (Dấu hỏi chấm) | Mở trang quy tắc trò chơi, tài liệu hướng dẫn sử dụng. |
| **Action Button: Custom** (`actionButtonBlank`) | 🔲 (Nút trắng trống) | Nút tự do — cho phép người dùng tự gõ chữ/vẽ icon rồi tự gán macro/liên kết. |

---

## 4. Các thao tác/phương thức làm việc với Shape

| Thao tác | Cách làm |
|---|---|
| Đổi kích thước | Kéo 8 chấm/hình vuông quanh viền shape khi đã chọn; giữ Shift để giữ tỉ lệ |
| Xoay shape | Kéo biểu tượng mũi tên cong phía trên shape; hoặc **Format Shape → Size → Rotation** để nhập số độ chính xác |
| Chỉnh hình dáng riêng | Kéo **chấm vàng (adjustment handle)** xuất hiện trên một số shape (VD: bo góc bo tròn, độ nhọn mũi tên, độ cong callout) |
| Chỉnh từng điểm neo | Click phải → **Edit Points** → kéo từng nút đen để tạo hình tự do, thêm/xoá điểm neo |
| Đổi sang shape khác mà giữ nguyên nội dung/định dạng | Chọn shape → **Shape Format → Edit Shape → Change Shape** → chọn shape mới |
| Đổ màu nền | **Shape Format → Shape Fill** (màu đặc, gradient, texture, hình ảnh, không màu) |
| Đổi viền | **Shape Format → Shape Outline** (màu, độ dày nét, kiểu nét đứt) |
| Hiệu ứng | **Shape Format → Shape Effects** (đổ bóng, phát sáng, bo cạnh mềm, bevel nổi khối, xoay 3D, phản chiếu) |
| Copy định dạng | **Format Painter** (biểu tượng cây cọ ở tab Home) để áp định dạng của shape này sang shape khác |
| Canh chỉnh nhiều shape | Chọn nhiều shape (giữ Shift/Ctrl click) → **Shape Format → Align** (căn trái/phải/giữa, dàn đều khoảng cách) |
| Nhóm/rã nhóm | Chọn nhiều shape → **Ctrl+G** để gộp nhóm (di chuyển/resize cùng lúc), **Ctrl+Shift+G** để rã |
| Sắp lớp trước–sau | Click phải → **Bring to Front / Send to Back / Bring Forward / Send Backward** khi các shape đè lên nhau |
| Bọc chữ quanh shape (Word) | Chọn shape → **Layout Options** → chọn kiểu bọc chữ (Square, Tight, Through, Behind Text, In Front of Text...) |
| Neo shape theo ô (Excel) | Click phải shape → **Size and Properties** → chọn "Move and size with cells" nếu muốn shape di chuyển theo khi chèn/xoá dòng-cột |
| Đặt làm shape mặc định | Click phải shape đã định dạng ưng ý → **Set as Default Shape** → các shape vẽ sau sẽ tự mang định dạng này |

## 5. Merge Shapes (Union, Combine, Fragment, Intersect, Subtract) — chỉ có ở PowerPoint

Đây chính là tính năng "kết hợp hình" (bạn gọi là "Union") — tên đầy đủ là **Merge Shapes**, nằm trong tab **Shape Format → nhóm Insert Shapes → Merge Shapes**. Tính năng này **chỉ có trên PowerPoint** (Word/Excel/Outlook không có nút này), dùng để tạo icon, logo, hình khối tuỳ chỉnh từ các shape có sẵn.

### Cách thực hiện
1. Chèn từ 2 shape trở lên, cho chúng chồng lấn (overlap) một phần lên nhau.
2. Chọn lần lượt từng shape theo đúng thứ tự bạn muốn (thứ tự chọn ảnh hưởng tới định dạng của hình kết quả và ảnh hưởng tới Subtract) — giữ **Shift** hoặc **Ctrl** để chọn nhiều shape cùng lúc.
3. Vào **Shape Format → Merge Shapes** → chọn 1 trong 5 lệnh bên dưới.
4. Hình kết quả sẽ **mang màu/viền/hiệu ứng của shape được chọn đầu tiên**, nên nếu muốn kiểm soát màu sắc, hãy định dạng shape đầu tiên trước khi merge.

### 5 kiểu Merge Shapes

| Lệnh | Kết quả |
|---|---|
| **Union** | Gộp toàn bộ các shape đã chọn thành **một shape duy nhất**, đường viền là bao ngoài của toàn bộ vùng các shape che phủ (kể cả phần không chồng lấn). Dùng khi muốn "dán" nhiều hình lại làm một khối liền mạch. |
| **Combine** | Gộp các shape thành một, nhưng **phần diện tích chồng lấn giữa các shape sẽ bị khoét rỗng** (trở thành lỗ trống nhìn xuyên thấu). Hay dùng để tạo hiệu ứng "cắt lỗ" — ví dụ 2 hình tròn chồng nhau tạo hình lưỡi liềm. |
| **Fragment** | **Tách toàn bộ vùng chồng lấn và không chồng lấn thành các mảnh riêng biệt**, mỗi mảnh là một shape độc lập (bạn có thể kéo tách rời từng mảnh ra sau khi Fragment). Dùng khi muốn "mổ xẻ" một vùng giao nhau ra thành nhiều phần để tô màu riêng từng phần. |
| **Intersect** | Chỉ **giữ lại đúng phần diện tích chồng lấn** giữa các shape đã chọn, mọi phần không giao nhau đều bị xoá. Dùng để tạo hình dạng đặc biệt từ phần giao của 2 hình (VD: 2 hình tròn giao nhau tạo hình "mắt lá" – Vesica). |
| **Subtract** | **Lấy shape chọn đầu tiên làm nền, cắt bỏ phần bị shape chọn sau đè lên**. Thứ tự chọn rất quan trọng — shape chọn trước là shape "giữ lại", shape chọn sau là "dao cắt". Dùng để khoét lỗ, tạo hình chữ/icon phức tạp (như số 8, chữ O) từ hình đơn giản. |

### Ứng dụng thực tế của Merge Shapes
- Tự thiết kế **icon/logo** không có sẵn trong thư viện (ví dụ ghép hình tròn + tam giác để tạo icon "play có viền").
- Tạo **khung ảnh dạng tuỳ chỉnh** (dùng Subtract cắt lỗ hình oval/ngôi sao giữa 1 hình chữ nhật, rồi đổ ảnh vào lỗ đó).
- Vẽ **bản đồ, biểu đồ Venn tuỳ chỉnh, sơ đồ kỹ thuật** cần hình dạng đặc thù mà 3.1–3.8 ở trên không có sẵn.
- Sau khi merge xong, hình kết quả vẫn là 1 **shape bình thường** — vẫn đổ màu, thêm chữ, thêm hiệu ứng 3D như mọi shape khác được.

### Lưu ý khi dùng Merge Shapes
- Có thể merge shape với **text đã Convert to Shape**, ảnh (Picture) đã chèn, hoặc icon SVG — không chỉ giới hạn ở Shapes gallery.
- Nếu dùng bản Office cũ (2013–2016), nút Merge Shapes có thể **chưa có sẵn trên ribbon** — cần vào **File → Options → Customize Ribbon/Quick Access Toolbar**, chọn "All Commands", tìm "Merge Shapes" rồi thêm thủ công. Từ Office 2019/365 trở đi, nút này có sẵn mặc định khi chọn ≥2 shape.
- Kết quả Merge **không thể tách ngược lại** về các shape gốc (trừ Fragment vẫn giữ các mảnh tách rời được, còn Union/Combine/Intersect/Subtract sẽ hợp nhất vĩnh viễn) — nên nếu cần giữ bản gốc, hãy **Copy (Ctrl+C) các shape trước khi merge**.

---

## 6. Selection Pane (Ngăn quản lý danh sách & phân lớp Shape)

**Đường dẫn:** Ribbon **Home → Editing → Select → Selection Pane** hoặc tab **Shape Format → Selection Pane** (Phím tắt: **Alt + F10** trong PowerPoint, Word, Excel).

Selection Pane là bảng điều khiển bên phải liệt kê toàn bộ các đối tượng hình khối, ảnh, biểu đồ trên trang/slide:

| Tính năng trong Selection Pane | Cách sử dụng & Tác dụng |
|---|---|
| **Đổi tên Shape (Rename)** | Nhấp đúp (hoặc click chậm 2 lần) vào tên shape → gõ tên mới (VD: `btn_Submit`, `Card_Header`, `Logo_Icon`). Cực kỳ hữu ích khi viết mã VBA/Macro hoặc thiết lập Animation. |
| **Ẩn / Hiện Shape (👁️)** | Bấm vào biểu tượng con mắt bên cạnh tên shape để tạm thời ẩn đi mà không cần xóa. Nút **Show All** và **Hide All** ở trên cùng giúp ẩn/hiện toàn bộ trang nhanh chóng. |
| **Khóa Shape (Lock) 🔒** | *(Tính năng có từ Microsoft 365)* Bấm biểu tượng ổ khóa để cố định shape, ngăn không cho vô tình click trúng, di chuyển hoặc thay đổi kích thước khi thao tác các lớp xung quanh. |
| **Sắp xếp thứ tự lớp (Z-Order) bằng kéo-thả** | Kéo tên shape lên trên hoặc xuống dưới trong danh sách để thay đổi vị trí lớp trước/sau ngay tức thì, thay vì phải click phải chuột chọn *Bring Forward / Send Backward* nhiều lần. |
| **Chọn các Shape bị che khuất** | Khi một shape nhỏ bị shape lớn che phủ hoàn toàn, chỉ cần click tên shape đó trong Selection Pane là đối tượng được chọn ngay lập tức. |

---

## 7. Edit Points (Chỉnh sửa điểm neo Vector & đường cong Bézier)

**Đường dẫn:** Click phải vào Shape → **Edit Points** (hoặc **Shape Format → Edit Shape → Edit Points**).

Chế độ Edit Points cho phép bạn can thiệp trực tiếp vào từng đường cong vector của hình khối:

### 7.1 Phân biệt 2 loại điểm điều khiển
- **Điểm neo màu đen (Anchor Point / Vertex):** Nằm trực tiếp trên chu vi hình dạng, xác định vị trí cố định của góc hoặc đoạn uốn cong.
- **Điểm tay nắm màu trắng (Control Handle / Bézier Handle):** Xuất hiện khi click vào một điểm neo đen. Kéo tay nắm này để chỉnh bán kính cong và hướng tiếp tuyến của đường viền.

### 7.2 Ba kiểu điểm neo (Right-click vào điểm đen để đổi kiểu)
1. **Smooth Point (Điểm uốn mượt):** Hai tay nắm trắng luôn nằm thẳng hàng đối xứng 180° qua điểm neo. Khi kéo một tay nắm, tay nắm bên kia tự động quay theo, đảm bảo đường cong đi qua điểm neo luôn trơn láng tuyệt đối.
2. **Straight Point (Điểm tiếp tuyến tự do):** Hai tay nắm vẫn nằm trên một đường thẳng 180°, nhưng độ dài mỗi bên có thể khác nhau độc lập. Dùng khi muốn một bên cong sâu, một bên cong nông nhưng vẫn chuyển tiếp mượt.
3. **Corner Point (Điểm góc nhọn):** Hai tay nắm hoàn toàn độc lập về cả góc quay lẫn độ dài. Dùng để tạo góc gập nhọn, khúc cua gấp hoặc răng cưa.

### 7.3 Thao tác nhanh với đường vector
- **Thêm điểm neo mới:** Rê chuột lên đoạn viền giữa 2 điểm neo (con trỏ chuột biến thành dấu chữ thập uốn cong `⌖`) → Click và kéo nhẹ ra ngoài.
- **Xóa điểm neo:** Giữ phím **Ctrl** và click chuột trái vào điểm đen muốn xóa (con trỏ biến thành dấu nhân `×`).
- **Đóng / Mở đường viền:** Click phải lên một điểm neo hoặc đoạn viền → chọn **Close Path** (nối liền điểm đầu và điểm cuối thành hình kín) hoặc **Open Path** (ngắt hở hình kín thành đường hở).

---

## 8. Chấm vàng (Adjustment Handles) — Tinh chỉnh tham số hình học đặc thù

Nhiều shape trong thư viện có từ **1 đến 4 chấm vàng (yellow handle)**. Khác với 8 chấm trắng ở góc để co giãn kích thước, chấm vàng dùng để **thay đổi tỷ lệ cấu trúc hình học** riêng biệt của từng hình:

| Shape | Vị trí chấm vàng & Tác dụng |
|---|---|
| **Rounded Rectangle** | Nằm ở góc trên bên trái: kéo sang phải để tăng bán kính bo tròn góc (từ góc vuông 90° đến bo tròn tối đa dạng hình viên thuốc / pill). |
| **Snip Corner Rectangle** | Chỉnh độ sâu của vết cắt vát chéo ở các góc hình chữ nhật. |
| **Block Arrow (Mũi tên khối)** | Thường có 2 chấm vàng: 1 chấm chỉnh bề dày thân mũi tên, 1 chấm chỉnh chiều rộng và độ nhọn của đầu tam giác mũi tên. |
| **Smiley Face (Mặt cười)** | Nằm ngay tại khóe miệng: kéo lên là **mặt cười tươi 😊**, kéo ngang là **mặt bình thản 😐**, kéo chúc xuống dưới biến thành **mặt mếu buồn ☹️**. |
| **Donut (Hình vành khăn)** | Nằm trên vòng tròn trong: kéo ra ngoài để làm vành mỏng hơn, kéo vào trong để làm vành dày đặc gần như hình tròn. |
| **Pie / Arc / Block Arc** | Có 2 chấm vàng: 1 chấm chỉnh góc bắt đầu (Start Angle), 1 chấm chỉnh góc quét (Sweep Angle) của hình quạt / cung tròn. |
| **Callouts (Bong bóng thoại)** | Nằm ở đỉnh nhọn của đuôi thoại: bấm kéo để dời vị trí đuôi thoại chỉ chính xác vào nhân vật hoặc đối tượng cần ghi chú. |
| **Star (Ngôi sao 4, 5, 8, 12 cánh)** | Nằm tại đáy góc lõm giữa 2 cánh sao: kéo vào tâm làm các cánh sao nhọn hoắt, kéo ra ngoài làm ngôi sao mập béo tròn trịa. |
| **Can / Cylinder (Hình trụ)** | Nằm trên mép elip đỉnh: kéo để thay đổi độ nghiêng phối cảnh của mặt đáy trên. |
| **Folded Corner (Trang giấy gấp góc)** | Nằm ở góc gấp: kéo để tăng giảm kích thước nếp gấp giấy ở góc trên bên phải. |

---

## 9. Bảng định dạng Format Shape chuyên sâu (Task Pane)

**Cách mở:** Click chuột phải vào Shape → **Format Shape...** (hoặc nhấn **Ctrl + 1** trong Excel/PowerPoint, hoặc double-click vào đường viền shape). Bảng Task Pane hiện ra ở cạnh phải với 2 khu vực lớn:

### 9.1 Shape Options (Tùy chọn Hình khối)

#### A. Fill (Tô màu nền)
- **No fill:** Trong suốt hoàn toàn, nhìn xuyên thấu xuống các đối tượng hoặc văn bản nằm bên dưới.
- **Solid fill:** Tô màu đơn sắc. Có thanh trượt **Transparency** (0% – 100%) để làm mờ nền xuyên thấu.
- **Gradient fill:** Tô dải màu chuyển tiếp:
  - *Type:* **Linear** (tuyến tính theo góc xoay), **Radial** (tỏa tròn từ tâm hoặc góc), **Rectangular** (tỏa chữ nhật), **Path** (chuyển màu theo hình bao ngoài).
  - *Gradient Stops:* Các chốt màu trên thanh trượt. Có thể bấm thêm (+) hoặc xóa (×) chốt màu, đổi màu và độ trong suốt riêng cho từng chốt.
- **Picture or texture fill:** Chèn trực tiếp ảnh từ máy tính (File), khay nhớ tạm (Clipboard), hoặc thư viện Online vào làm vân nền cho shape. Tích chọn *Tile picture as texture* để ảnh tự lặp lại dạng gạch hoa.
- **Pattern fill:** Tô các họa tiết sọc chéo, bàn cờ, chấm bi, mắt lưới bằng sự kết hợp giữa màu Foreground và Background.

#### B. Line (Đường viền)
- **Width:** Độ dày viền tính bằng đơn vị Point (pt).
- **Compound type:** Kiểu đường phức hợp (nét đơn `Single`, nét đôi mảnh `Double`, một nét đậm một nét thanh `Thick Thin`...).
- **Dash type:** Kiểu gạch đứt đoạn (Solid, Round Dot, Square Dot, Dash, Long Dash...).
- **Cap type & Join type:** Kiểu bo đầu mút và góc nối (Round bo cong mềm mại, Miter góc nhọn vuông vức, Bevel vát chéo).
- **Arrows:** Đối với đường thẳng và đường nối, cho phép chọn kiểu mũi tên ở đầu (`Begin Arrow type`) và cuối (`End Arrow type`) cùng kích cỡ mũi tên (`Arrow size`).

#### C. Effects (Hiệu ứng thị giác)
- **Shadow (Đổ bóng):** Gồm bóng ngoài (*Outer*), bóng bên trong lòng khối (*Inner*), và bóng ngả trên mặt đất phối cảnh (*Perspective*). Chỉnh được màu bóng, góc chiếu sáng (*Angle*), khoảng cách (*Distance*), độ nhòe (*Blur*) và kích thước phóng bóng (*Size*).
- **Reflection (Phản chiếu):** Tạo bóng gương lộn ngược bên dưới shape như đặt trên mặt kính ướt.
- **Glow (Phát sáng):** Tạo vầng sáng neon tỏa đều xung quanh các cạnh của hình khối.
- **Soft Edges (Viền mềm):** Làm nhòe và hòa tan đường viền từ 1 pt đến 100 pt, giúp hình khối hòa trộn êm dịu vào ảnh nền mà không lộ mép cắt thô cứng.
- **3D Format & 3D Rotation:** 
  - *Bevel (Vát khối):* Tạo độ nổi gờ mép cong, vát dốc hoặc gờ tròn.
  - *Depth:* Chiều sâu khối 3D đùn ra sau.
  - *Material (Chất liệu):* Bề mặt mờ (*Matte*), nhựa bóng (*Plastic*), kim loại sáng loáng (*Metal*), hoặc vẽ nét trong suốt (*Wireframe*).
  - *Lighting (Ánh sáng):* Chọn góc chiếu sáng tự nhiên, ấm áp, lạnh hoặc phẳng.
  - *3D Rotation:* Xoay khối trong không gian 3 chiều theo trục X, Y, Z hoặc chọn các góc nhìn Isometric phối cảnh có sẵn.

---

## 10. Văn bản nâng cao trong Shape (Text Box Options)

Trong bảng **Format Shape**, chuyển sang tab **Text Options → Text Box** để làm chủ toàn diện văn bản bên trong hình khối:

### 10.1 Căn chỉnh vị trí & Hướng chữ
- **Vertical alignment:** Canh chữ theo chiều dọc bên trong shape: **Top** (trên đỉnh), **Middle** (chính giữa), **Bottom** (dưới đáy), **Top Centered**, **Middle Centered**, **Bottom Centered**.
- **Text direction:**
  - *Horizontal:* Chữ chạy ngang bình thường.
  - *Rotate all text 90°:* Xoay toàn bộ chữ dựng đứng từ trên xuống.
  - *Rotate all text 270°:* Xoay toàn bộ chữ dựng đứng từ dưới lên.
  - *Stacked:* Chữ xếp chồng từng chữ cái thẳng đứng (thích hợp làm nhãn gáy sách).

### 10.2 Lề văn bản (Internal Margins)
Mặc định shape luôn có khoảng đệm giữa chữ và viền: Left (0.1"), Right (0.1"), Top (0.05"), Bottom (0.05"). Khi cần chữ nằm sát mép hình khối hoặc thiết kế icon nhỏ, hãy **đặt toàn bộ 4 lề về 0 cm** để tránh văn bản bị rớt dòng ngoài ý muốn.

### 10.3 Quy tắc tự động co giãn (AutoFit)
- **Do not Autofit:** Giữ nguyên kích cỡ shape và cỡ chữ; nếu văn bản quá dài, chữ sẽ tràn qua mép ngoài của shape.
- **Shrink text on overflow:** Tự động hạ cỡ chữ (giảm font size) vừa khít bên trong shape khi bạn gõ nhiều chữ, đảm bảo chữ không bao giờ bị tràn ra ngoài.
- **Resize shape to fit text:** Tự động mở rộng chiều cao và chiều rộng của shape nở to ra theo lượng văn bản bạn gõ vào.

### 10.4 Chia cột chữ trong Shape (Columns)
Nhấn nút **Columns** trong hộp thoại Text Box để chia đoạn văn bên trong một hình chữ nhật thành **2, 3 hoặc 4 cột** kèm khoảng cách giãn cách giữa các cột (*Spacing*). Tính năng này giúp trình bày danh sách so sánh hoặc bài báo bên trong một khối nền duy nhất mà không cần phải chèn nhiều Text Box rời rạc.

---

## 11. Drawing Canvas trong Microsoft Word (Khung vẽ sơ đồ thông minh)

Trong Word, khi vẽ nhiều shape rời trên trang tài liệu, chỉ cần gõ thêm một dòng văn bản là các hình có thể bị nhảy lộn xộn sang trang khác. Để giải quyết việc này, Word cung cấp **Drawing Canvas**:

- **Cách tạo:** Ribbon **Insert → Shapes → New Drawing Canvas** (nằm ở dòng cuối cùng của bảng Shapes).
- **Lợi ích vượt trội của Drawing Canvas:**
  1. **Đóng gói sơ đồ (Container):** Toàn bộ các shape vẽ bên trong Canvas trở thành một khối thống nhất. Khi di chuyển Canvas, toàn bộ sơ đồ chạy theo nguyên vẹn.
  2. **Bắt dính đầu nối (Glue Connectors):** Trong văn bản Word thông thường, các đường nối (Elbow / Curved Connector) không thể tự hút dính vào các hình khác. **Chỉ khi vẽ trong Drawing Canvas**, các chấm xanh kết nối mới hiện ra, giúp đường nối tự động bám chặt và uốn cong theo khi bạn di chuyển các khối.
  3. **Thu phóng cả sơ đồ (Scale Drawing):** Nhấp chuột phải vào viền Canvas → chọn **Scale Drawing** → kéo núm góc để phóng to/thu nhỏ toàn bộ mọi shape và chữ bên trong cùng một tỷ lệ chuẩn xác.

---

## 12. Ứng dụng Shape trong Excel (Dashboard, Nút bấm & Tự động hóa)

Shape trong Excel không chỉ để vẽ trang trí mà còn là công cụ xây dựng giao diện Dashboard tương tác chuyên nghiệp:

### 12.1 Liên kết Text của Shape với Ô dữ liệu động
1. Vẽ một shape bất kỳ trên trang tính (VD: Hình chữ nhật bo góc).
2. Click chọn shape → Nhấp chuột vào **thanh công thức (Formula Bar)** ở trên cùng.
3. Gõ dấu bằng `=` và click vào ô dữ liệu cần lấy giá trị (VD: `=Sheet1!$B$4` hoặc `=D10`).
4. Nhấn **Enter**.
5. *Kết quả:* Chữ bên trong Shape sẽ tự động phản ánh nội dung ô đó. Khi số liệu tính toán trong bảng Excel thay đổi, thẻ Shape sẽ tự động cập nhật số liệu theo thời gian thực (ứng dụng làm thẻ KPI Card).

### 12.2 Biến Shape thành Nút bấm Macro (Action Button)
1. Vẽ và định dạng shape đẹp mắt (đổ màu gradient, bóng đổ 3D như nút bấm).
2. Click chuột phải vào Shape → chọn **Assign Macro...**.
3. Chọn tên Macro VBA bạn đã viết sẵn (VD: `InBaoCao`, `LocDuLieu`, `ResetForm`) → Bấm **OK**.
4. Khi rê chuột qua shape, con trỏ đổi thành hình bàn tay 👆; click chuột vào shape sẽ lập tức kích hoạt chạy macro.

### 12.3 Neo giữ vị trí Shape theo ô tính (Properties)
Click phải vào Shape → **Format Shape → Size & Properties → Properties**:
- **Move and size with cells:** Shape sẽ tự động co giãn kích thước và di chuyển khi bạn kéo rộng/hẹp cột hoặc ẩn/hiện các dòng bên dưới.
- **Move but don't size with cells:** Shape chỉ di chuyển tịnh tiến theo vị trí ô nhưng kích thước hình giữ nguyên không đổi.
- **Don't move or size with cells:** Shape đứng cố định tuyệt đối trên màn hình, hoàn toàn độc lập với việc thêm/xóa dòng cột của bảng tính.

> **Mẹo Snap to Grid trong Excel:** Khi vẽ hoặc di chuyển shape, hãy **giữ phím Alt**. Các cạnh của shape sẽ tự động hút dính tuyệt đối vào các đường lưới biên của hàng và cột Excel.

---

## 13. Shape Animations & Morph trong PowerPoint (Motion Graphics)

### 13.1 Kỹ thuật biến hình Morph với Shape
Hiệu ứng **Morph Transition** trong PowerPoint cho phép chuyển đổi mượt mà một hình dạng từ trạng thái này sang trạng thái khác giữa 2 slide liên tiếp:
1. Ở **Slide 1**, vẽ một hình chữ nhật bo góc màu xanh nhỏ ở góc trái.
2. Nhân bản slide (**Ctrl + D**) để tạo **Slide 2**.
3. Ở **Slide 2**, đổi màu hình thành màu cam, phóng to ra giữa màn hình, hoặc click phải chọn **Change Shape** đổi thành hình tròn.
4. Vào tab **Transitions** của Slide 2 → Chọn hiệu ứng **Morph**.
5. Khi trình chiếu, PowerPoint sẽ tự động tạo hoạt cảnh chuyển động biến hình mượt mà giữa 2 slide.
6. *Mẹo ép biến hình chính xác:* Mở **Selection Pane (Alt + F10)**, đặt tên shape ở cả 2 slide có tiền tố `!!` giống nhau (VD: `!!MyCard` ở Slide 1 và `!!MyCard` ở Slide 2). PowerPoint sẽ hiểu và biến hình chính xác các điểm vector kể cả khi hai hình hoàn toàn khác loại.

### 13.2 Chuyển động theo đường dẫn (Motion Paths)
Vào tab **Animations → Add Animation → nhóm Motion Paths** (chọn *Lines, Arcs, Turns* hoặc *Custom Path*):
- Bạn có thể vẽ một đường cong bất kỳ trên slide.
- Khi bấm chuột, shape sẽ tự động lướt theo đúng đường dẫn đó với gia tốc mượt mà (*Smooth Start / Smooth End*).

---

## 14. Bảng tổng hợp phím tắt quyền lực khi làm việc với Shape

| Phím tắt | Tác dụng |
|---|---|
| **Shift + Kéo chuột khi vẽ/resize** | Giữ tỷ lệ vuông / tròn đều 1:1, hoặc vẽ đường thẳng chuẩn góc 0°, 45°, 90°. |
| **Ctrl + Kéo chuột khi resize** | Co giãn đối xứng lấy tâm làm gốc (cả 2 bên cùng nở ra hoặc thu vào đều đặn). |
| **Ctrl + Shift + Kéo chuột** | Vừa giữ tỷ lệ vuông/tròn 1:1 vừa nở đều từ tâm hình học ra xung quanh. |
| **Alt + Kéo chuột (Windows)** | Tắt tạm thời tính năng bắt lưới (Snap), cho phép dịch chuyển hoặc co giãn mượt mà từng pixel. |
| **Ctrl + D** | Nhân bản (Duplicate) shape nhanh chóng. Nếu kéo shape mới ra một khoảng rồi bấm Ctrl+D tiếp, các shape sau sẽ tự cách đều nhau theo đúng khoảng cách đó. |
| **Ctrl + Kéo-thả shape** | Tạo bản sao nhanh của shape tại vị trí mới. |
| **Ctrl + Shift + C** | Sao chép toàn bộ định dạng của shape (màu fill, màu viền, bóng đổ, kiểu chữ). |
| **Ctrl + Shift + V** | Dán định dạng vừa copy sang một hoặc nhiều shape khác đang chọn. |
| **Các phím mũi tên (↑ ↓ ← →)** | Di chuyển shape từng nấc nhỏ (nudge). |
| **Ctrl + Các phím mũi tên** | Di chuyển vi mô siêu chính xác (micro-nudge: 1 pixel mỗi lần bấm). |
| **Alt + ← / →** | Xoay shape theo chiều kim đồng hồ hoặc ngược lại từng nấc 15°. |
| **Ctrl + G** | Gộp nhóm (Group) các shape đang chọn lại thành một khối thống nhất. |
| **Ctrl + Shift + G** | Rã nhóm (Ungroup) khối shape trở lại các hình độc lập. |
| **Alt + F10** | Mở hoặc đóng thanh quản lý danh sách hình khối (**Selection Pane**). |
| **Tab** / **Shift + Tab** | Chuyển vùng chọn tuần tự tiến / lùi giữa các shape trên trang mà không cần rê chuột. |
| **Esc** | Nhấn 1 lần: Thoát chế độ gõ chữ trong shape; Nhấn lần 2: Bỏ chọn hoàn toàn shape. |

---

## 15. Đối chiếu tính năng Shape: GenOffice vs Microsoft 365

| Nhóm tính năng | Microsoft 365 | GenOffice (C# / .NET) |
|---|:---:|:---:|
| **Thư viện Shape (Rectangles, Arrows, Flowcharts...)** | Đầy đủ | Hỗ trợ hơn 180+ preset shape DrawingML |
| **SmartArt vector nguyên bản** | Đầy đủ | Dựng trực tiếp từ XML gốc (100% vector, không vỡ nét) |
| **Gõ chữ trực tiếp trong Shape & SmartArt** | Trực tiếp trên canvas | Trực tiếp trên canvas với con trỏ nhấp nháy `|` (không dùng TextBox phụ) |
| **Chấm vàng Adjustment Handles** | 1–4 chấm tùy shape | Có sẵn 3 thanh tinh chỉnh AdjustValue1, 2, 3 tương tác thời gian thực |
| **8 núm kéo Resize & Núm xoay Rotation** | Đầy đủ | Đầy đủ 8 handles tròn + núm xoay phía trên đỉnh |
| **Đổi màu riêng từng nút SmartArt** | Đầy đủ | Đầy đủ qua Ribbon Format Tab / Palette |
| **Bọc chữ quanh hình (Text Wrapping)** | InLine, Square, Tight, Behind, In Front | Đầy đủ WrapMode 0 đến 6 chuẩn Word |
| **Bảo toàn sơ đồ khi lưu DOCX** | Chuẩn ECMA-376 | Lưu giữ nguyên thẻ DrawingML và mã màu RGB/SchemeClr gốc |

---

## 16. SmartArt vs Shape — Phân biệt bản chất kiến trúc & Chuyển đổi qua lại

Nhiều người dùng thường nhầm lẫn giữa **Shape độc lập** và **SmartArt Graphic**. Dưới đây là sự khác biệt cốt lõi về mặt cấu trúc và hành vi:

| Tiêu chí | Shape Đơn Lẻ (Single Shape) | SmartArt Graphic (DrawingML Diagram) |
|---|---|---|
| **Bản chất kiến trúc** | Là 1 đối tượng hình học độc lập (`<a:sp>`). | Là một hệ thống sơ đồ phức hợp gồm cây quan hệ dữ liệu (`data1.xml`), luật bố cục (`layout1.xml`), kiểu hiển thị (`style1.xml`) và bảng phối màu (`colors1.xml`). |
| **Hành vi khi thêm bớt nội dung** | Phải tự vẽ thêm hình, tự canh lề, tự nối lại đường line thủ công. | Tự động co giãn, tính toán lại tỷ lệ và dàn đều các nút con khi bạn thêm/bớt mục trong bảng Text Pane. |
| **Khả năng tùy biến tự do** | Tự do tuyệt đối: xoay, đổi điểm neo (Edit Points), Merge Shapes, chỉnh kích thước từng cạnh. | Bị ràng buộc bởi thuật toán bố cục tự động của mẫu SmartArt; khó kéo lệch một hình mà không ảnh hưởng tổng thể. |
| **Chỉnh sửa chữ** | Gõ thẳng vào hình hoặc hộp thoại thuộc tính. | Gõ trực tiếp trên sơ đồ hoặc gõ qua cửa sổ cây phân cấp **Text Pane** (Bullet points). |

### Khi nào nên dùng SmartArt? Khi nào nên dùng Shape?
- **Dùng SmartArt khi:** Cần trình bày nhanh ý tưởng, cấu trúc tổ chức, quy trình tuần tự, ma trận 4 góc, hình tròn chu kỳ... mà không muốn mất thời gian căn chỉnh thước đo thủ công.
- **Dùng Shape khi:** Thiết kế logo, icon, sơ đồ mạng lưới đặc thù phi chuẩn, giao diện ứng dụng (UI Mockup), hoặc các hình khối phức tạp cần cắt ghép.

### Tuyệt chiêu: Chuyển đổi SmartArt thành Shape (Convert to Shapes)
Khi bạn đã nhập xong nội dung vào SmartArt nhưng muốn phá vỡ sự gò bó của thuật toán bố cục để tự do kéo dãn, tô màu riêng từng chi tiết, hoặc dùng tính năng **Merge Shapes** (PowerPoint), hãy chuyển đổi SmartArt thành các Shape độc lập:
1. **Cách 1 (Chuẩn Ribbon):** Chọn khối SmartArt → Vào tab **SmartArt Design → Convert → Convert to Shapes**.
2. **Cách 2 (Dùng phím tắt Ungroup 2 lần):**
   - Chọn khung viền ngoài của SmartArt.
   - Nhấn tổ hợp phím **Ctrl + Shift + G** (Ungroup) lần 1: Khối SmartArt biến thành một nhóm Shape (Group of Shapes).
   - Nhấn **Ctrl + Shift + G** lần 2: Toàn bộ các hộp, mũi tên, biểu tượng bên trong được rã thành từng shape riêng biệt 100%.

---

## 17. Bóc tách cấu trúc kỹ thuật OpenXML DrawingML (`<a:sp>`) và VML

Về mặt kỹ thuật bên trong tệp tin `.docx`, `.pptx` hoặc `.xlsx` (bản chất là file nén ZIP tuân thủ chuẩn **ECMA-376 OpenXML**), một Shape không phải là hình ảnh tĩnh mà là một đoạn mã XML mô tả toán học vector:

### 17.1 Cấu trúc thẻ Shape trong DrawingML (`<a:sp>`)
Mỗi hình vẽ chuẩn DrawingML trong tệp XML đều bao gồm 4 khối chính:

```xml
<p:sp> <!-- hoặc w:drawing / xdr:sp -->
  <!-- 1. Thông tin định danh đối tượng -->
  <p:nvSpPr>
    <p:cNvPr id="5" name="Rounded Rectangle 4"/>
    <p:cNvSpPr/>
    <p:nvPr/>
  </p:nvSpPr>

  <!-- 2. Thuộc tính hình học, màu sắc, viền, hiệu ứng -->
  <p:spPr>
    <a:xfrm rot="0">
      <a:off x="1524000" y="1143000"/> <!-- Tọa độ X, Y theo đơn vị EMU (1 inch = 914,400 EMU) -->
      <a:ext cx="2743200" cy="1371600"/> <!-- Chiều rộng W, chiều cao H -->
    </a:xfrm>
    <!-- Hình mẫu có sẵn (Preset Geometry) cùng tham số chấm vàng -->
    <a:prstGeom prst="roundRect">
      <a:avLst>
        <a:gd name="adj" fmla="val 16667"/> <!-- Tỷ lệ bo góc 16.667% -->
      </a:avLst>
    </a:prstGeom>
    <!-- Màu nền Solid Fill -->
    <a:solidFill>
      <a:srgbClr val="0078D7"/>
    </a:solidFill>
    <!-- Đường viền Line -->
    <a:ln w="12700">
      <a:solidFill><a:srgbClr val="004C87"/></a:solidFill>
    </a:ln>
    <!-- Hiệu ứng bóng đổ Outer Shadow -->
    <a:effectLst>
      <a:outerShdw blurRad="50800" dist="25400" dir="5400000" algn="b">
        <a:srgbClr val="000000"><a:alpha val="35000"/></a:srgbClr>
      </a:outerShdw>
    </a:effectLst>
  </p:spPr>

  <!-- 3. Nội dung văn bản bên trong Shape -->
  <p:txBody>
    <a:bodyPr vert="horz" anchor="ctr"/>
    <a:p>
      <a:pPr algn="ctr"/>
      <a:r>
        <a:rPr lang="en-US" sz="1400" b="1">
          <a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill>
        </a:rPr>
        <a:t>GenOffice Vector Shape</a:t>
      </a:r>
    </a:p>
  </p:txBody>
</p:sp>
```

### 17.2 Hình vẽ tự do Custom Geometry (`<a:custGeom>`)
Khi bạn dùng lệnh **Edit Points** để uốn nắn một shape, Office sẽ chuyển đổi thẻ `<a:prstGeom>` thành `<a:custGeom>`. Khối này lưu các lệnh đồ họa vector tương tự mã SVG Path:
- `<a:moveTo>`: Nhấc bút di chuyển đến tọa độ `(x, y)`.
- `<a:lnTo>`: Kẻ đoạn thẳng tới tọa độ đích.
- `<a:arcTo>`: Vẽ cung tròn/ellipse với bán kính trục `wR, hR`, góc bắt đầu `stAng` và góc quét `swAng`.
- `<a:cubicBezTo>`: Vẽ đường cong Bézier bậc 3 mượt mà với 2 điểm điều khiển (Control Points) và 1 điểm neo kết thúc.
- `<a:close>`: Khép kín đường bao trở về điểm bắt đầu để đổ màu nền.

### 17.3 So sánh DrawingML hiện đại và VML cổ điển (Word 97-2003)
- **VML (Vector Markup Language):** Định dạng XML thời kỳ đầu (thẻ `<v:shape>`, `<v:rect>`, `<v:path>`), toạ độ dựa trên hệ số nguyên 21600. Vẫn xuất hiện trong file Word `.doc` cũ hoặc phần Header/Footer của một số tài liệu.
- **DrawingML (DML):** Chuẩn đồ họa vector hiện đại của OpenXML từ Office 2007 đến Microsoft 365, hỗ trợ gradient đa điểm, ánh sáng 3D, bóng mờ thực tế, độ trong suốt Alpha Channel mượt mà và hệ tọa độ độ nét cao EMU (English Metric Units: 914,400 đơn vị / inch).

---

## 18. Cơ chế Text Wrapping & Anchor (Cây Mỏ Neo) chuyên sâu trong Microsoft Word

Trong Microsoft Word, việc chèn hình khối luôn đi kèm khái niệm **Text Wrapping (Bọc chữ quanh hình)** và **Anchor (Cây mỏ neo)**. Đây là một trong những phần người dùng dễ lúng túng nhất khi soạn thảo tài liệu dài.

### 18.1 Biểu tượng Cây Mỏ Neo (⚓ Anchor) là gì?
Khi chọn một Shape trong Word (ở chế độ bọc chữ trôi nổi - Floating), bạn sẽ thấy một biểu tượng chiếc mỏ neo nhỏ màu xám hoặc xanh lá cây hiện ở lề bên trái của một đoạn văn bản gần đó.
- **Ý nghĩa:** Biểu tượng này cho biết shape đang được gắn vào đoạn văn nào. Nếu bạn chèn thêm văn bản ở phía trên làm đoạn văn đó dạt xuống trang tiếp theo, shape sẽ **tự động nhảy theo đoạn văn đó** sang trang sau!
- **Khóa Mỏ Neo (Lock Anchor):**
  - Click phải vào Shape → Chọn **More Layout Options → tab Position**.
  - Tích chọn **Lock anchor**: Giữ mỏ neo cố định tại đoạn văn bản hiện tại. Ngay cả khi bạn kéo thả shape đi khắp trang, mỏ neo vẫn không bị trượt sang các đoạn văn khác.
- **Move object with text vs Fix position on page:**
  - *Move object with text:* Shape di chuyển lên/xuống khi nội dung văn bản bên trên co giãn.
  - *Fix position on page:* Shape đứng yên tuyệt đối tại tọa độ cm đã chọn trên trang in, bất kể bạn có gõ thêm bao nhiêu chữ xung quanh nó.

### 18.2 Chi tiết 7 kiểu bọc chữ (Wrap Modes)

```
[ In Line ]       [ Square ]         [ Tight ]          [ Through ]
Text text         Text Text Text     Text     Text      Text  Text Text
Text [SHAPE]      Text [SHAPE] Text  Tex(SHAPE)ext      Tex(SHAPE)ext
Text text         Text Text Text     Text     Text      Text / lỗ / Text

[ Top & Bottom ]  [ Behind Text ]    [ In Front ]
Text Text Text    Văn bản in đè      [SHAPE che mất]
--------------    lên mặt của        chữ ở bên dưới
   [SHAPE]        [SHAPE chìm]       không nhìn thấy
--------------    vẫn đọc được       được nữa
Text Text Text    như bình thường
```

1. **In Line with Text (Nằm trong dòng chữ):** Shape hoạt động như một ký tự văn bản khổng lồ. Không thể kéo thả tự do, không có mỏ neo. Thích hợp chèn icon nhỏ hoặc công thức toán.
2. **Square (Bao quanh hình hộp):** Văn bản dạt ra tạo thành một khoảng trống hình chữ nhật bao quanh khung ngoài của shape, bất kể shape là hình tròn, hình sao hay tam giác.
3. **Tight (Bao sát viền hình):** Văn bản lượn sát theo đúng biên dạng thực của hình khối (ví dụ nếu là hình sao, chữ sẽ len lỏi vào các khe của cánh sao).
4. **Through (Xuyên qua hình):** Tương tự Tight, nhưng nếu shape có lỗ trống ở giữa (như hình xuyến Donut hoặc khung Frame), chữ sẽ tự động chảy xuyên qua phần ruột rỗng đó.
5. **Top and Bottom (Ngắt dòng trên/dưới):** Văn bản chỉ xuất hiện ở cạnh trên và cạnh dưới của shape, hai bên sườn trái/phải hoàn toàn để trống. Rất chuộng dùng cho biểu đồ, tiêu đề phân cách.
6. **Behind Text (Nằm chìm dưới chữ):** Shape nằm ở lớp nền sâu nhất, văn bản trôi đè lên mặt shape. Thường dùng làm tem đóng dấu, watermark, khung nền làm nổi bật đoạn văn.
7. **In Front of Text (Nổi đè lên trên chữ):** Shape nằm ở lớp trên cùng, che khuất toàn bộ văn bản phía dưới. Thích hợp làm nhãn dán, tem "CONFIDENTIAL", cờ ghi chú hoặc các mũi tên chỉ điểm.

> **Mẹo chỉnh viền bọc chữ:** Chọn Shape → Tab **Shape Format → Wrap Text → Edit Wrap Points**. Một đường viền màu đỏ với các nút vuông màu đen sẽ xuất hiện; bạn có thể kéo các nút này để quyết định văn bản được phép áp sát hình ở khoảng cách bao nhiêu milimet.

---

## 19. Hiệu ứng 3D & Chiều sâu không gian (3D Format & 3D Rotation)

Microsoft 365 sở hữu bộ engine dựng hình 3D cho shape rất mạnh mẽ, cho phép biến một hình phẳng 2D thành một khối nổi chân thực với góc nhìn phối cảnh, độ bóng và ánh sáng vật lý.

### 19.1 3D Format (Định dạng khối)
Nằm trong cửa sổ **Format Shape → Effects → 3-D Format**:
- **Top Bevel & Bottom Bevel (Gờ xiên đỉnh và đáy):** Tạo các đường vát cong hoặc góc cạnh ở mặt trên và mặt dưới của shape. Có sẵn các kiểu: *Circle (tròn mềm), Relaxed Inset (vát êm), Cross (chữ thập), Cool Slant (xiên sắc cạnh), Slope (dốc thoai thoải), Convex (lồi)*.
- **Depth (Độ sâu / Extrusion):** Độ dày đùn khối theo trục Z. Khi bạn nhập Depth = 30 pt, hình chữ nhật sẽ biến thành một khối hộp chữ nhật có chiều sâu 30 pt. Có thể chọn màu riêng cho phần thân khối này.
- **Contour (Đường viền khối):** Cho phép kẻ một đường viền kim loại hoặc màu tương phản quanh các mép giao của khối 3D.
- **Material (Vật liệu bề mặt):** Quyết định độ phản xạ ánh sáng của shape:
  - *Standard:* Matte (nhám mịn), Warm Matte, Plastic (nhựa bóng), Metal (kim loại ánh kim).
  - *Special Effect:* Dark Edge, Soft Edge, Flat, Wireframe (khung dây trong suốt).
  - *Translucent:* Clear (thủy tinh trong), Powder (phấn mờ), Purified.
- **Lighting (Nguồn sáng chiếu):** Mô phỏng hướng bóng đổ và ánh sáng studio: *Three Point (3 điểm chuẩn), Balanced, Warm (ấm), Cool (lạnh), Flat, Freezing, Sunrise, Sunset*. Bạn có thể chỉnh góc xoay nguồn sáng (**Angle**) từ 0° đến 359°.

### 19.2 3D Rotation (Xoay trong không gian 3 chiều)
Nằm trong mục **Effects → 3-D Rotation**:
- **Trục X, Y, Z:** Nhập số độ xoay chính xác quanh trục ngang (X), trục dọc (Y) hoặc trục vuông góc với mắt nhìn (Z).
- **Perspective (Góc nhìn phối cảnh):** Cho phép mô phỏng luật xa gần (vật ở gần mắt thì to, ở xa thì nhỏ). Bạn có thể chỉnh góc nhìn thị trường (Field of View) lên tới 120°.
- **Keep text flat:** Nếu tích chọn mục này, khối shape sẽ xoay 3D nhưng dòng chữ bên trong vẫn giữ nguyên tư thế nhìn phẳng trực diện, giúp người xem đọc chữ dễ dàng hơn.

---

## 20. Tương tác nâng cao trong PowerPoint: Triggers & Action Settings với Shape

Trong PowerPoint, Shape không chỉ để trang trí tĩnh mà còn có thể trở thành **nút bấm tương tác thông minh (Interactive Buttons)** để tạo bài giảng E-Learning, trò chơi trắc nghiệm (Quiz), hoặc Dashboard điều khiển dữ liệu.

### 20.1 Cơ chế Kích hoạt Hoạt ảnh (Animation Triggers)
Bình thường, hiệu ứng Animation sẽ chạy tuần tự theo cú click chuột bất kỳ trên màn hình. Nhưng với **Trigger**, hiệu ứng chỉ chạy khi bạn **bấm trúng đích danh một shape cụ thể**:
1. Vẽ 2 đối tượng:
   - Đối tượng A: Một nút bấm tròn mang tên "Nút Mở Quà" (đổi tên trong Selection Pane).
   - Đối tượng B: Một hộp quà.
2. Thêm hiệu ứng xuất hiện cho Hộp quà B (Vào **Animations → Add Animation → Zoom**).
3. Tại tab Animations, chọn nút **Trigger → On Click of → Chọn "Nút Mở Quà"**.
4. **Kết quả khi trình chiếu:** Bạn có thể bấm chuột thoải mái khắp slide thì hộp quà vẫn không hiện; chỉ khi rê chuột vào "Nút Mở Quà" (con trỏ chuột biến thành hình bàn tay 👆) và click, hộp quà mới nổ ra hiệu ứng.

### 20.2 Action Settings (Gán hành động tương tác)
Chọn Shape → Vào tab **Insert → nhóm Links → Action**:
- **Hộp thoại gồm 2 tab:** *Mouse Click* (hành động khi click) và *Mouse Over* (hành động khi rê chuột lướt qua).
- **Các hành động có thể gán:**
  - **Hyperlink to:** Chuyển nhanh đến Slide kế tiếp, Slide trước, Slide đầu, Slide cuối, một Slide số cụ thể, hoặc mở một trang Web URL / file PDF / file Excel ngoài máy tính.
  - **Run Program:** Mở một ứng dụng phần mềm cài trên máy tính (VD: `notepad.exe`, `calc.exe`).
  - **Run Macro:** Kích hoạt một đoạn mã lập trình VBA.
  - **Play Sound:** Phát một âm thanh hiệu ứng (Applause, Cash Register, Chime, Laser, Voltage...).

---

## 21. Thủ thuật thiết kế đỉnh cao với Shape (Design Best Practices)

Một slide thuyết trình hay tài liệu báo cáo trông chuyên nghiệp hay nghiệp dư phụ thuộc 80% vào cách xử lý hình khối. Dưới đây là những nguyên tắc thẩm mỹ từ các chuyên gia thiết kế hàng đầu:

### 21.1 Thiết kế giao diện Mockup thuần túy bằng Shape
Bạn hoàn toàn có thể vẽ một chiếc smartphone (iPhone), laptop hoặc cửa sổ trình duyệt Web chuyên nghiệp mà không cần tải ảnh ngoài:
- **Khung màn hình:** Vẽ hình *Rounded Rectangle*, kéo chấm vàng để đạt độ bo góc mềm mại 24px.
- **Thanh trình duyệt:** Vẽ 3 hình tròn nhỏ xíu đặt ngang nhau (đỏ, vàng, xanh) làm 3 nút đóng/thu nhỏ/phóng to. Kẻ một hình chữ nhật bo tròn màu xám nhạt làm thanh gõ địa chỉ URL.
- **Đổ bóng mềm (Soft Shadow):** Không dùng bóng mặc định thô cứng của Office; hãy thiết lập *Transparency = 75%, Size = 100%, Blur = 25 pt, Angle = 90°, Distance = 8 pt* để tạo bóng đổ tinh tế như thiết kế của Apple.

### 21.2 Phong cách Glassmorphism (Kính mờ xuyên thấu)
Xu hướng thiết kế giao diện kính mờ hiện đại được thực hiện trên Shape bằng cách:
1. Đặt shape đè lên một bức ảnh nền rực rỡ nhiều màu.
2. Đổ màu nền Shape Fill: Chọn màu trắng, đặt **Transparency = 70% đến 80%**.
3. Đường viền Shape Outline: Đặt viền trắng độ dày 1 pt, **Transparency = 60%**.
4. Thêm hiệu ứng bóng đổ nhẹ phía dưới. Shape sẽ trông như một tấm kính thủy tinh trong suốt phản chiếu ánh sáng.

### 21.3 Quy tắc sử dụng màu sắc và đường viền (Stroke)
- **Hạn chế dùng viền đen đậm:** Các tài liệu hiện đại gần như 90% **tắt viền shape** (*Shape Outline: No Outline*) để hình khối trông thanh thoát, rộng rãi.
- **Quy tắc phối màu:** Sử dụng 1 màu chủ đạo (Primary Color), 1 màu nhấn (Accent Color) cho các nút hành động quan trọng, và các sắc độ xám nhạt (Neutral Grays) cho các khối nội dung phụ.

---

## 22. Tự động hóa Shape qua Code: VBA trong Office & Lập trình C# trong GenOffice

### 22.1 Đoạn mã VBA điều khiển Shape trong Microsoft Office
Bạn có thể tự động hóa việc sinh hàng trăm shape báo cáo trong Word/Excel/PowerPoint bằng mã VBA:

```vba
Sub CreateModernCard()
    Dim sld As Slide
    Dim shp As Shape
    Set sld = ActivePresentation.Slides(1)
    
    ' Tạo một hình chữ nhật bo góc với kích thước chuẩn (Left, Top, Width, Height theo Point)
    Set shp = sld.Shapes.AddShape(msoShapeRoundedRectangle, 100, 100, 320, 180)
    
    With shp
        .Fill.Solid
        .Fill.ForeColor.RGB = RGB(245, 247, 250) ' Màu nền xám nhã nhặn
        .Line.Visible = msoFalse                 ' Tắt viền thô
        .Adjustments(1) = 0.1                    ' Chỉnh chấm vàng bo góc 10%
        
        ' Thiết lập hiệu ứng bóng đổ mềm
        With .Shadow
            .Visible = msoTrue
            .Blur = 15
            .OffsetX = 0
            .OffsetY = 6
            .Transparency = 0.8
        End With
        
        ' Ghi chữ vào thẻ
        With .TextFrame2
            .TextRange.Text = "Thẻ Phân Tích Dữ Liệu"
            .TextRange.Font.Name = "Segoe UI"
            .TextRange.Font.Size = 16
            .TextRange.Font.Bold = msoTrue
            .TextRange.Font.Fill.ForeColor.RGB = RGB(33, 37, 41)
            .VerticalAnchor = msoAnchorTop
        End With
    End With
End Sub
```

### 22.2 Kiến trúc xử lý Shape hiệu năng cao trong GenOffice (C# / WinForms)
GenOffice được thiết kế theo tư duy độc lập mã nguồn, không phụ thuộc vào bộ cài Microsoft Office cồng kềnh:
- **Mô hình dữ liệu `DocShapeObject`:** Đại diện cho một nút hình khối hoặc SmartArt. Lưu giữ loại shape (`DocShapeType`), tọa độ bounding box (`Bounds`), các tham số điều chỉnh chấm vàng (`AdjustValue1, 2, 3`), chế độ bọc chữ (`WrapMode`), và các nút con phân cấp (`SubNodeTexts`, `NodeColors`).
- **Engine kết xuất `ShapeRenderer.cs`:** Sử dụng GDI+ với kỹ thuật **Antialiasing (khử răng cưa mịn màng)**, dựng các đường cong Bézier chuẩn xác và vẽ con trỏ nhấp nháy `|` trực tiếp lên canvas điểm ảnh khi người dùng gõ chữ, đảm bảo tốc độ phản hồi 60 FPS mượt mà.
- **Tương thích OpenXML 100%:** Khi mở file `.docx`, GenOffice bóc tách trực tiếp các thẻ `<w:drawing>`, `<a:sp>`, `<a:custGeom>` và dựng lại đồ họa vector nguyên bản với độ sắc nét tuyệt đối.

---

*Tài liệu hướng dẫn toàn tập và bách khoa toàn thư về Shapes trong Microsoft 365 (Word, Excel, PowerPoint, Outlook) cùng kiến trúc đối chiếu ứng dụng GenOffice.*
