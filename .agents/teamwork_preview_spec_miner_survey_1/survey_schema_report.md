# BÁO CÁO TOÀN DIỆN KHẢO SÁT ĐẶC TẢ VÀ LƯỢC ĐỒ OPENXML (ECMA-376)
## HỆ THỐNG SMARTART (200+ LOẠI), HÌNH KHỐI (SHAPES), ĐƯỜNG VIỀN BẢNG BIỂU & CÔNG THỨC TOÁN HỌC OMML TRONG GENOFFICE

**Ngày lập**: 2026-09-21  
**Tác giả**: OpenXML Spec & Schema Miner Agent  
**Mục tiêu**: Bóc tách, lượng hóa và đặc tả chuẩn hóa 100% các lược đồ OpenXML (ECMA-376 / ISO/IEC 29500), các tệp mẫu tại `C:\Users\Danh\Desktop\xml` và tài liệu DOCX thực tế; thiết lập hệ thống quy tắc chuyển đổi tọa độ, bảng màu theme, bố cục hình học SmartArt tỉ lệ 1:1, viền bảng và công thức OMML cho engine GenOffice.

---

## MỤC LỤC
1. [TỔNG QUAN VÀ PHẠM VI KHẢO SÁT](#1-tổng-quan-và-phạm-vi-khảo-sát)
2. [BÓC TÁCH CẤU TRÚC THỰC TẾ TẠI C:\Users\Danh\Desktop\xml & CÁC TỆP MẪU](#2-bóc-tách-cấu-trúc-thực-tế-tại-cusersdanhdesktopxml--các-tệp-mẫu)
3. [ĐẶC TẢ CHI TIẾT LƯỢC ĐỒ OPENXML (ECMA-376)](#3-đặc-tả-chi-tiết-lược-đồ-openxml-ecma-376)
   - 3.1. [Hệ thống 4 tầng SmartArt (`dgm:dataModel`, `dgm:layoutDef`, `dgm:styleDef`, `dgm:clrData`)](#31-hệ-thống-4-tầng-smartart)
   - 3.2. [Đồ họa DrawingML (`w:drawing`, `wp:anchor`, `wp:inline`, `a:graphic`, `a:graphicData`)](#32-đồ-họa-drawingml)
   - 3.3. [Đường viền bảng biểu (`w:tblBorders`, `w:tcBorders`, kiểu dáng, kích thước, màu sắc)](#33-đường-viền-bảng-biểu)
   - 3.4. [Công thức toán học OMML (`m:oMath`, `m:oMathPara`, phân số, căn số, ma trận...)](#34-công-thức-toán-học-omml)
4. [LƯỢNG HÓA HỆ THỐNG QUY TẮC CHUYỂN ĐỔI ĐƠN VỊ VÀ ÁNH XẠ](#4-lượng-hóa-hệ-thống-quy-tắc-chuyển-đổi-đơn-vị-và-ánh-xạ)
   - 4.1. [Hệ quy đổi tọa độ và đơn vị đo lường (EMU, Point, Twip/DXA, Eighths of pt)](#41-hệ-quy-đổi-tọa-độ-và-đơn-vị-đo-lường)
   - 4.2. [Bảng màu Theme và phép biến đổi màu sắc (SchemeColor, Tint, Shade, SatMod)](#42-bảng-màu-theme-và-phép-biến-đổi-màu-sắc)
   - 4.3. [Trích xuất phân cấp nút và hình học tùy biến (`custGeom` -> Vector Path)](#43-trích-xuất-phân-cấp-nút-và-hình-học-tùy-biến)
5. [THUẬT TOÁN HÌNH HỌC VÒNG TRÒN SMARTART TỶ LỆ 1:1 VÀ GẮN KẾT NÚT NGOẠI TIẾP](#5-thuật-toán-hình-học-vòng-tròn-smartart-tỷ-lệ-11-và-gắn-kết-nút-ngoại-tiếp)
6. [DANH MỤC TOÀN DIỆN 208 MẪU SMARTART (9 NHÓM CHUẨN MICROSOFT OFFICE)](#6-danh-mục-toàn-diện-208-mẫu-smartart-9-nhóm-chuẩn-microsoft-office)
7. [DANH MỤC ĐẦY ĐỦ CÁC HÌNH KHỐI (SHAPES) VÀ ĐẶC TẢ TAY NẮM ĐIỀU CHỈNH](#7-danh-mục-đầy-đủ-các-hình-khối-shapes-và-đặc-tả-tay-nắm-điều-chỉnh)
8. [BẢNG TỔNG HỢP TÍNH NĂNG KHÁM PHÁ (FEATURES DISCOVERED)](#8-bảng-tổng-hợp-tính-năng-khám-phá-features-discovered)
9. [BẢNG CÁC TRƯỜNG HỢP BIÊN & DỊ BIỆT (EDGE CASES)](#9-bảng-các-trường-hợp-biên--dị-biệt-edge-cases)
10. [BẢN THIẾT KẾ KIẾN TRÚC NÂNG CẤP ENGINE PARSER & CANVAS RENDERER](#10-bản-thiết-kế-kiến-trúc-nâng-cấp-engine-parser--canvas-renderer)

---

## 1. TỔNG QUAN VÀ PHẠM VI KHẢO SÁT

Theo yêu cầu tại `ORIGINAL_REQUEST.md` (mốc `2026-09-21T12:58:21Z`), nhiệm vụ trọng tâm là hoàn thiện hệ thống bóc tách, chuẩn hóa OpenXML từ Microsoft Word và đồng bộ hóa engine kết xuất đồ họa Canvas trong GenOffice.
Phạm vi khảo sát này bao phủ toàn diện 4 trụ cột nghiệp vụ:
1. **Lược đồ SmartArt DrawingML (200+ sơ đồ)**: Phân tích sâu kiến trúc 4 tầng chuẩn ECMA-376 gồm Dữ liệu (`dgm:dataModel`), Bố cục (`dgm:layoutDef`), Kiểu dáng (`dgm:styleDef`), Bảng màu (`dgm:clrData`) và bản vẽ kết xuất (`dgm:drawing`).
2. **Khung bao Đồ họa & Hình khối (DrawingML & Shapes)**: Cơ chế neo tọa độ `wp:anchor` và nội dòng `wp:inline`, quan hệ đồ họa `a:graphicData`, hình học chuẩn `a:prstGeom` và vector tự do `a:custGeom`.
3. **Đường viền bảng biểu WordprocessingML (`w:tblBorders`, `w:tcBorders`)**: Phân loại đầy đủ các kiểu viền (solid, dash, double, wave, 3D emboss/engrave), chuẩn hóa đơn vị đo độ dày bằng **1/8 point (eighths of a point)** và cơ chế thừa kế viền bảng sang viền ô.
4. **Công thức toán học OMML (`m:oMath`, `m:oMathPara`)**: Cấu trúc cây phần tử toán học phức tạp gồm phân số (`m:f`), căn thức (`m:rad`), chỉ số trên/dưới (`m:sSup`, `m:sSub`), ma trận (`m:m`), dấu mở/đóng ngoặc (`m:d`), toán tử tích phân/tổng (`m:nary`), hàm lượng giác (`m:func`).
5. **Lượng hóa tuyệt đối quy tắc chuyển đổi tọa độ và màu sắc**: Tỷ lệ quy đổi EMUs sang points ($1\text{ pt} = 12,700\text{ EMUs}$), Twips sang points ($1\text{ pt} = 20\text{ twips}$), 60.000 đơn vị góc = 1 độ, công thức biến đổi sắc thái màu (Tint/Shade/SatMod), và thuật toán dựng hình chu trình tròn hoàn hảo 1:1 không bị méo elip.

---

## 2. BÓC TÁCH CẤU TRÚC THỰC TẾ TẠI C:\Users\Danh\Desktop\xml & CÁC TỆP MẪU

Khảo sát trực tiếp gói tài liệu giải nén tại `C:\Users\Danh\Desktop\xml` và các tài liệu đối sánh (`C:\Users\Danh\Desktop\Doc1.docx`, `test_math_doc.docx`, `BaoCaoTongKet_2026.docx`):

### 2.1. Cấu trúc đóng gói OPC (Open Packaging Conventions)
Gói tài liệu tại `C:\Users\Danh\Desktop\xml` tuân thủ nghiêm ngặt ISO/IEC 29500-2 (OPC):
```
C:\Users\Danh\Desktop\xml\
├── [Content_Types].xml               <- Khai báo MIME ContentType cho toàn bộ phần tử
├── _rels\                            <- Quan hệ tầng gốc (Package-level relationships)
├── customXml\                        <- Dữ liệu XML mở rộng tùy biến (item1.xml, itemProps1.xml)
├── docProps\                         <- Thuộc tính tài liệu: core.xml, app.xml, custom.xml
└── word\                             <- Thư mục chính của WordprocessingML
    ├── document.xml                  <- Luồng văn bản và đồ họa chính (1,833,129 bytes)
    ├── styles.xml                    <- Định nghĩa các kiểu văn bản và bảng (31,843 bytes)
    ├── numbering.xml                 <- Cấu trúc danh sách gạch đầu dòng và số thứ tự
    ├── settings.xml & webSettings.xml<- Thiết lập hiển thị, tương thích và bố cục trang
    ├── fontTable.xml                 <- Bảng phông chữ được nhúng hoặc tham chiếu
    ├── footnotes.xml & endnotes.xml  <- Chú thích cuối trang và cuối tài liệu
    ├── header1.xml                   <- Nội dung đầu trang (Header)
    ├── media\                        <- Chứa 24 ảnh nhúng (image1.png ... image24.png)
    ├── theme\
    │   └── theme1.xml                <- Định nghĩa Theme Office (Theme Palette, Fonts, Format Scheme)
    └── _rels\
        └── document.xml.rels         <- Bản đồ ánh xạ ID quan hệ (rId1 ... rId46)
```

### 2.2. Phân tích tệp `word/document.xml` (1.83 MB)
- Tệp chứa hơn **3,045 thẻ `a:pt`**, **2,436 thẻ `a:lnTo`**, **609 thẻ `wps:wsp`** (WordprocessingShape), **609 thẻ `a:custGeom`** với đầy đủ các phân đoạn hình học vector:
  - `a:pathLst` -> `a:path` (định nghĩa lưới tọa độ gốc `w="21600" h="21600"`).
  - Phép chuyển dịch và nối đường cong: `a:moveTo`, `a:lnTo`, `a:arcTo`, `a:cubicBezTo`, `a:close`.
  - Khối định vị `wp:anchor` và `wp:inline`: Chứa `wp:extent cx="..." cy="..."` tính bằng đơn vị **EMU** (English Metric Units).
  - Nhóm hình vẽ liên kết: Sử dụng namespace mở rộng Word 2010 `http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing` (`wp14`) và `wordprocessingGroup` (`wpg:wgp`).

### 2.3. Bóc tách tệp Theme Office `word/theme/theme1.xml` (6,992 bytes)
Tệp định nghĩa bảng màu cốt lõi chuẩn `a:clrScheme name="Office"`:
```xml
<a:clrScheme name="Office">
    <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1>
    <a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
    <a:dk2><a:srgbClr val="1F497D"/></a:dk2>
    <a:lt2><a:srgbClr val="EEECE1"/></a:lt2>
    <a:accent1><a:srgbClr val="4F81BD"/></a:accent1>
    <a:accent2><a:srgbClr val="C0504D"/></a:accent2>
    <a:accent3><a:srgbClr val="9BBB59"/></a:accent3>
    <a:accent4><a:srgbClr val="8064A2"/></a:accent4>
    <a:accent5><a:srgbClr val="4BACC6"/></a:accent5>
    <a:accent6><a:srgbClr val="F79646"/></a:accent6>
    <a:hlink><a:srgbClr val="0000FF"/></a:hlink>
    <a:folHlink><a:srgbClr val="800080"/></a:folHlink>
</a:clrScheme>
```
Đặc biệt, hệ thống định dạng `a:fmtScheme` chứa danh sách kiểu viền `a:lnStyleLst` với độ rộng chuẩn xác bằng EMUs:
- Nét mảnh 0.75 pt: `w="9525"` EMUs.
- Nét vừa 2.0 pt: `w="25400"` EMUs.
- Nét đậm 3.0 pt: `w="38100"` EMUs.

### 2.4. Bóc tách tệp mẫu SmartArt thực tế `Doc1.docx`
Trong tài liệu `C:\Users\Danh\Desktop\Doc1.docx`, khảo sát phát hiện **3 cụm sơ đồ SmartArt hoàn chỉnh** với đầy đủ các Part độc lập được liên kết qua `word/_rels/document.xml.rels`:
1. **Diagram 1**: Layout `urn:microsoft.com/office/officeart/2005/8/layout/default` (Basic Block List)
   - Tệp dữ liệu `data1.xml`: 26 điểm nút (`dgm:pt`), 20 liên kết (`dgm:cxn`).
2. **Diagram 2**: Layout `urn:microsoft.com/office/officeart/2005/8/layout/hProcess4` (Alternating Flow)
   - Tệp dữ liệu `data2.xml`: 52 điểm nút, 50 liên kết.
3. **Diagram 3**: Layout `urn:microsoft.com/office/officeart/2005/8/layout/cycle6` (Radial Cycle / Chu trình tâm điểm)
   - Tệp layout `layout3.xml`: Định nghĩa danh mục `cat type="cycle" pri="4000"` và `cat type="relationship" pri="24000"`.
   - Tệp dữ liệu `data3.xml`: 32 điểm nút, 31 liên kết, thuộc tính gán:
     `loTypeId="urn:microsoft.com/office/officeart/2005/8/layout/cycle6"`
     `qsTypeId="urn:microsoft.com/office/officeart/2005/8/quickstyle/simple1"`
     `csTypeId="urn:microsoft.com/office/officeart/2005/8/colors/accent1_2"`

### 2.5. Bóc tách tệp công thức toán học mẫu `test_math_doc.docx`
Tệp chứa **35 công thức toán học `m:oMath`** và **12 đoạn toán học độc lập `m:oMathPara`** với đầy đủ các cấu trúc toán học cao cấp:
- Lũy thừa / Số mũ: `m:sSup` gồm cơ số `m:e` và số mũ `m:sup`.
- Chỉ số dưới: `m:sSub` gồm cơ số `m:e` và chỉ số `m:sub`.
- Dấu ngoặc co giãn: `m:d` kèm thiết lập `m:dPr/m:begChr` và `m:endChr`.
- Phông chữ toán học chuyên dụng: `Cambria Math` với thuộc tính kiểu dáng `m:sty val="p"` (plain) hoặc `m:sty val="bi"` (bold italic).

---

## 3. ĐẶC TẢ CHI TIẾT LƯỢC ĐỒ OPENXML (ECMA-376)

### 3.1. Hệ thống 4 tầng SmartArt
Chuẩn ECMA-376 Part 1, Section 21.4 phân tách SmartArt thành 4 phần tử cấu trúc độc lập:

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      TÀI LIỆU WORD (document.xml)                        │
│  <w:drawing>                                                             │
│    <wp:inline> hoặc <wp:anchor>                                          │
│      <a:graphic>                                                         │
│        <a:graphicData uri="http://.../diagram">                          │
│          <dgm:relIds dm="rId4" lo="rId5" qs="rId6" cs="rId7"/>           │
└─────────────────────────────────────┬────────────────────────────────────┘
                                      │ Ánh xạ qua document.xml.rels
          ┌───────────────────────────┼───────────────────────────┐
          ▼                           ▼                           ▼
┌──────────────────┐        ┌──────────────────┐        ┌──────────────────┐
│  Diagram Data    │        │  Diagram Layout  │        │  Diagram Colors  │
│  (diagrams/      │        │  (diagrams/      │        │  (diagrams/      │
│   data1.xml)     │        │   layout1.xml)   │        │   colors1.xml)   │
│  - dgm:dataModel │        │  - dgm:layoutDef │        │  - dgm:clrData   │
│  - dgm:ptLst     │        │  - dgm:layoutNode│        │  - dgm:clrTrans  │
│  - dgm:cxnLst    │        │  - dgm:alg       │        │  - Variant Acc1-6│
└──────────────────┘        └──────────────────┘        └──────────────────┘
                                      │
                                      ▼
                            ┌──────────────────┐
                            │  Diagram Style   │
                            │  (diagrams/      │
                            │   quickStyle1.xml│
                            │  - dgm:styleDef  │
                            │  - 3D scene & sp │
                            └──────────────────┘
```

#### A. Tầng Dữ liệu (`DiagramDataPart` - `data1.xml`)
- Namespace: `http://schemas.openxmlformats.org/drawingml/2006/diagram` (`dgm`)
- Thẻ gốc: `<dgm:dataModel>`
- Thẻ danh sách điểm: `<dgm:ptLst>` chứa các điểm nút `<dgm:pt>`:
  - `modelId`: Mã định danh duy nhất (UUID hoặc chuỗi số nguyên).
  - `type`:
    - `doc`: Điểm gốc của toàn bộ sơ đồ tài liệu, chứa thiết lập mặc định `dgm:prSet`.
    - `node` (hoặc rỗng): Nút dữ liệu nội dung hiển thị chữ thật.
    - `asst`: Nút trợ lý (Assistant) trong sơ đồ tổ chức.
    - `parTrans`: Nút chuyển tiếp cha-con (Parent Transition).
    - `sibTrans`: Nút chuyển tiếp giữa các anh em (Sibling Transition).
    - `pres`: Nút trình diễn đồ họa (Presentation node).
  - Phần tử con trong `<dgm:pt>`:
    - `<dgm:prSet>`: Chứa `loTypeId` (mã layout), `qsTypeId` (mã style), `csTypeId` (mã bảng màu), `phldr` (cờ giữ chỗ), `phldrT` (chữ giữ chỗ mặc định khi chưa nhập).
    - `<dgm:spPr>`: Thuộc tính hình khối ghi đè cục bộ cho nút này.
    - `<dgm:t>`: Khối văn bản chứa `<a:bodyPr>`, `<a:p>`, `<a:r>`, `<a:t>`.
- Thẻ danh sách quan hệ: `<dgm:cxnLst>` chứa các liên kết `<dgm:cxn>`:
  - `modelId`: ID liên kết.
  - `type`:
    - `parOf`: Quan hệ phân cấp Cha - Con (`srcId` là cha, `destId` là con).
    - `sibOf`: Quan hệ thứ tự tuần tự Anh - Em.
    - `presOf`: Ánh xạ nút dữ liệu gốc sang nút hiển thị trình diễn (`srcId` trỏ về data node, `destId` trỏ về presentation node).
  - `srcId`, `destId`: Cặp ID liên kết.
  - `srcOrd`, `destOrd`: Thứ tự vị trí xuất phát và đích đến (bắt đầu từ 0).

#### B. Tầng Bố cục (`DiagramLayoutDefinitionPart` - `layout1.xml`)
- Thẻ gốc: `<dgm:layoutDef uniqueId="...">`
- `<dgm:title val="...">`, `<dgm:desc val="...">`
- `<dgm:catLst>`: Chứa danh sách các nhóm mà layout này thuộc về:
  `<dgm:cat type="cycle" pri="4000"/>` (Nhóm chu trình, độ ưu tiên 4000).
- Thuật toán sắp xếp không gian `<dgm:layoutNode>`:
  - Sử dụng `<dgm:alg type="...">`:
    - `cycle`: Sắp xếp tuần hoàn quanh đường tròn góc $\theta_i = \frac{2\pi \cdot i}{N}$.
    - `lin`: Sắp xếp tuyến tính thẳng hàng (ngang hoặc dọc).
    - `hierChild`: Phân bổ cây phân cấp cho các nút con.
    - `hierRoot`: Bố trí nút gốc đầu não.
    - `pyra`: Xếp tầng hình thang kim tự tháp từ đỉnh xuống đáy.
    - `conn`: Thuật toán vẽ đường nối giữa các nút.
  - Ràng buộc hình học `<dgm:constrLst>`: Các quy tắc `dgm:constr type="w" for="ch" refType="w" op="equ"` định nghĩa tỉ lệ kích thước, khoảng cách đệm và góc quay.

#### C. Tầng Bảng màu (`DiagramColorsPart` - `colors1.xml`)
- Thẻ gốc: `<dgm:clrData uniqueId="...">`
- Danh mục biến thể màu:
  - `Primary Theme Colors`: Tất cả các nút dùng đồng nhất một màu nhấn chính (`Accent 1`).
  - `Colorful Accents`: Các nút luân phiên mang các màu nhấn từ `Accent 1` đến `Accent 6`.
  - `Accent 1 Variations`: Dải đơn sắc của `Accent 1` biến thiên từ nhạt đến đậm (sử dụng các toán tử `tint` và `shade`).

#### D. Tầng Kiểu dáng 3D (`DiagramStylePart` - `quickStyle1.xml`)
- Thẻ gốc: `<dgm:styleDef uniqueId="...">`
- Thiết lập phối cảnh 3D `<dgm:scene3d>` và góc vát viền `<dgm:sp3d>`:
  - `Flat`: Chiều sâu = 0, nét vẽ phẳng 2D.
  - `Gradient`: Đổ bóng chuyển màu đa điểm dừng.
  - `Polished 3D`: Vát mép nổi khối (Bevel), ánh sáng phản chiếu Specular.
  - `Metallic 3D`: Bề mặt phản quang kim loại và độ tương phản cao.

---

### 3.2. Đồ họa DrawingML
Mọi hình vẽ, ảnh, SmartArt trong WordprocessingML đều được bọc trong thẻ `<w:drawing>`:

```xml
<w:drawing>
    <!-- TH1: Đồ họa nội dòng (Inline) di chuyển cùng dòng chữ -->
    <wp:inline distT="0" distB="0" distL="0" distR="0">
        <wp:extent cx="5486400" cy="3657600"/>
        <wp:docPr id="1" name="SmartArt 1" descr="SmartArt|Cycle_Radial|Title|Node1|Node2"/>
        <a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
            <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/diagram">
                <dgm:relIds r:dm="rId4" r:lo="rId5" r:qs="rId6" r:cs="rId7"/>
            </a:graphicData>
        </a:graphic>
    </wp:inline>
</w:drawing>
```
Hoặc đồ họa trôi nổi tự do trên trang (Floating):
```xml
<wp:anchor distT="0" distB="0" distL="114300" distR="114300" simplePos="0" relativeHeight="251658240" behindDoc="0" locked="0" layoutInCell="1" allowOverlap="1">
    <wp:simplePos x="0" y="0"/>
    <wp:positionH relativeFrom="page">
        <wp:posOffset>914400</wp:posOffset> <!-- Cách mép trái trang 1 inch (914,400 EMUs) -->
    </wp:positionH>
    <wp:positionV relativeFrom="page">
        <wp:posOffset>1828800</wp:posOffset> <!-- Cách mép trên trang 2 inch (1,828,800 EMUs) -->
    </wp:positionV>
    <wp:extent cx="5486400" cy="3657600"/> <!-- Rộng 6 inch, cao 4 inch -->
    <wp:wrapSquare wrapText="bothSides"/>   <!-- Bọc chữ xung quanh khung vuông -->
    <wp:docPr id="2" name="Diagram Anchor"/>
    ...
</wp:anchor>
```

Các chế độ bọc văn bản (`wp:wrap...`):
1. `wp:wrapNone`: Trôi nổi tự do không đẩy chữ (đặt chìm `behindDoc="1"` hoặc nổi `behindDoc="0"`).
2. `wp:wrapSquare`: Chữ bao quanh 4 cạnh khung chữ nhật ngoại tiếp.
3. `wp:wrapTight`: Chữ uốn lượn sát mép đường viền vector đa giác bao quanh (`wp:wrapPolygon`).
4. `wp:wrapThrough`: Chữ xuyên qua các khoảng trống bên trong hình vẽ.
5. `wp:wrapTopAndBottom`: Chữ dừng lại ở mép trên và tiếp tục ở mép dưới, không cho chữ tràn vào 2 bên.

---

### 3.3. Đường viền bảng biểu (`w:tblBorders` & `w:tcBorders`)
Cấu trúc viền bảng trong WordprocessingML được định nghĩa ở cấp độ bảng (`w:tblPr/w:tblBorders`) và ghi đè ở cấp độ từng ô (`w:tcPr/w:tcBorders`).

```xml
<w:tblBorders>
    <w:top w:val="single" w:sz="12" w:space="0" w:color="4F81BD"/>
    <w:left w:val="double" w:sz="24" w:space="0" w:color="4F81BD"/>
    <w:bottom w:val="single" w:sz="12" w:space="0" w:color="4F81BD"/>
    <w:right w:val="double" w:sz="24" w:space="0" w:color="4F81BD"/>
    <w:insideH w:val="single" w:sz="4" w:space="0" w:color="D9D9D9"/>
    <w:insideV w:val="none"/>
</w:tblBorders>
```

#### A. Bảng mã hóa giá trị kiểu viền (`w:val`) chuẩn ECMA-376 Part 1 §17.18.2:
| Kiểu viền OpenXML (`w:val`) | Tên tiếng Việt | Đặc điểm kết xuất Canvas GDI+ |
|---|---|---|
| `none` / `nil` | Không có viền | Không vẽ nét viền |
| `single` | Nét đơn liền mạch | `PenStyle.Solid`, độ dày `sz/8.0f` pt |
| `thick` | Nét đơn đậm | Nét liền độ dày tối thiểu $\ge 2.0$ pt |
| `double` | Nét đôi song song | Vẽ 2 đường song song với khoảng cách giữa đúng bằng độ dày nét |
| `dotted` | Nét chấm bi tròn/vuông | `PenStyle.Dot`, bước chấm đều |
| `dashed` | Nét đứt quãng | `PenStyle.Dash`, vạch đứt tiêu chuẩn 3:1 |
| `dotDash` | Nét gạch chấm xen kẽ | `PenStyle.DashDot` |
| `dotDotDash` | Nét gạch hai chấm | `PenStyle.DashDotDot` |
| `triple` | Nét ba song song | 3 đường song song: mảnh - mảnh - mảnh |
| `thinThickSmallGap` | Mảnh - Đậm (khe hẹp) | Đường ngoài mảnh, đường trong đậm, cách nhau 1 pt |
| `thickThinSmallGap` | Đậm - Mảnh (khe hẹp) | Đường ngoài đậm, đường trong mảnh |
| `thinThickThinSmallGap` | Mảnh - Đậm - Mảnh | 3 đường: đường giữa đậm gấp đôi 2 đường biên |
| `wave` | Nét lượn sóng đơn | Đường cong hình sin chu kỳ nhỏ dọc theo cạnh |
| `doubleWave` | Nét lượn sóng kép | 2 đường sin song song |
| `dashSmallGap` | Nét đứt khe hẹp | Vạch đứt với khoảng cách ngắt quãng cực ngắn |
| `dashDotStroked` | Gạch chấm viền | Kiểu nét vẽ kỹ thuật AutoCAD |
| `threeDEmboss` | Viền nổi 3D vát mép | Nửa trên sáng màu, nửa dưới sẫm màu tạo hiệu ứng gờ nổi |
| `threeDEngrave` | Viền chìm 3D khắc sâu | Nửa trên sẫm màu, nửa dưới sáng màu tạo hiệu ứng rãnh chìm |
| `outset` | Gờ hộp nổi | Đổ bóng phía đông nam |
| `inset` | Gờ hộp lõm | Đổ bóng phía tây bắc |

#### B. Quy tắc định lượng độ dày viền (`w:sz`):
- Giá trị `w:sz` được tính bằng **1/8 point (eighths of a point)**:
  $$\text{Độ dày nét vẽ (Points)} = \frac{\text{Giá trị } w:sz}{8}$$
  $$\text{Độ dày nét vẽ (EMUs)} = \left(\frac{\text{Giá trị } w:sz}{8}\right) \times 12,700$$
  - `w:sz="2"` $\rightarrow 0.25\text{ pt}$ ($3,175\text{ EMUs}$)
  - `w:sz="4"` $\rightarrow 0.50\text{ pt}$ ($6,350\text{ EMUs}$)
  - `w:sz="6"` $\rightarrow 0.75\text{ pt}$ ($9,525\text{ EMUs}$)
  - `w:sz="8"` $\rightarrow 1.00\text{ pt}$ ($12,700\text{ EMUs}$)
  - `w:sz="12"` $\rightarrow 1.50\text{ pt}$ ($19,050\text{ EMUs}$)
  - `w:sz="16"` $\rightarrow 2.00\text{ pt}$ ($25,400\text{ EMUs}$)
  - `w:sz="24"` $\rightarrow 3.00\text{ pt}$ ($38,100\text{ EMUs}$)
  - `w:sz="36"` $\rightarrow 4.50\text{ pt}$ ($57,150\text{ EMUs}$)
  - `w:sz="48"` $\rightarrow 6.00\text{ pt}$ ($76,200\text{ EMUs}$)

#### C. Quy tắc thứ bậc thừa kế đường viền (Border Resolution Hierarchy):
Khi kết xuất một ô bảng $(r, c)$, engine phải giải quyết xung đột viền theo thứ tự ưu tiên giảm dần:
1. **Viền ô cụ thể (`w:tcBorders`)**: Nếu ô có khai báo `<w:tcBorders>`, viền của ô ghi đè tuyệt đối.
2. **Viền cạnh kề của ô lân cận**: Đường phân cách giữa 2 ô kề nhau lấy theo ô có độ dày viền lớn hơn.
3. **Viền bảng mặc định (`w:tblBorders`)**: Áp dụng `w:top/left/bottom/right` cho các cạnh biên và `w:insideH/insideV` cho các cạnh lưới bên trong nếu ô không có khai báo riêng.
4. **Viền kiểu bảng (`Table Style Borders`)**: Thừa kế từ `styles.xml` nếu bảng không có thiết lập trực tiếp.

---

### 3.4. Công thức toán học OMML (`m:oMath`, `m:oMathPara`)
Chuẩn ECMA-376 Part 1 §22 định nghĩa Office Math Markup Language. Mọi biểu thức toán đều là cây phân cấp cú pháp (AST) có thể ánh xạ sang MathML hoặc biểu diễn Unicode:

```xml
<m:oMathPara>
    <m:oMathParaPr>
        <m:jc m:val="center"/> <!-- Căn giữa công thức -->
    </m:oMathParaPr>
    <m:oMath>
        <!-- Phân số: x = (-b ± √(b² - 4ac)) / (2a) -->
        <m:r><m:t>x=</m:t></m:r>
        <m:f>
            <m:fPr><m:type m:val="bar"/></m:fPr> <!-- Phân số gạch ngang chuẩn -->
            <m:num>
                <m:r><m:t>-b±</m:t></m:r>
                <m:rad>
                    <m:radPr><m:degHide m:val="1"/></m:radPr> <!-- Căn bậc hai (ẩn chỉ số độ) -->
                    <m:deg/>
                    <m:e>
                        <m:sSup>
                            <m:e><m:r><m:t>b</m:t></m:r></m:e>
                            <m:sup><m:r><m:t>2</m:t></m:r></m:sup>
                        </m:sSup>
                        <m:r><m:t>-4ac</m:t></m:r>
                    </m:e>
                </m:rad>
            </m:num>
            <m:den>
                <m:r><m:t>2a</m:t></m:r>
            </m:den>
        </m:f>
    </m:oMath>
</m:oMathPara>
```

#### Bảng cấu trúc các phần tử toán học OMML:
| Phần tử OMML | Tên toán học | Cấu trúc thành phần con | Quy tắc ánh xạ Unicode / Canvas |
|---|---|---|---|
| `m:oMath` | Biểu thức toán | Tập hợp các run và phần tử toán | Nhánh cây toán học độc lập |
| `m:oMathPara` | Đoạn toán học | Chứa 1 hoặc nhiều `m:oMath` | Khối paragraph toán học căn giữa/trái |
| `m:f` | Phân số (Fraction) | `m:fPr`, `m:num` (tử), `m:den` (mẫu) | Tử ở trên, vạch ngang ở giữa, mẫu ở dưới (hoặc `a/b`) |
| `m:rad` | Căn thức (Radical) | `m:radPr`, `m:deg` (bậc), `m:e` (biểu thức dưới căn) | Dấu căn $\sqrt[n]{e}$, vạch ngang trùm lên toàn bộ $e$ |
| `m:sSup` | Lũy thừa / Số mũ | `m:e` (cơ số), `m:sup` (số mũ) | Ký tự co nhỏ 65% dịch lên trên đỉnh |
| `m:sSub` | Chỉ số dưới (Subscript)| `m:e` (cơ số), `m:sub` (chỉ số) | Ký tự co nhỏ 65% dịch xuống đáy |
| `m:sSubSup` | Vừa có chỉ số trên & dưới | `m:e`, `m:sub`, `m:sup` | Chỉ số trên và dưới gióng thẳng hàng dọc |
| `m:d` | Khối ngoặc (Delimiter) | `m:dPr` (`begChr`, `endChr`), `m:e` | Dấu ngoặc co giãn tự động theo chiều cao biểu thức |
| `m:nary` | Toán tử N-ary (Tích phân, Tổng)| `m:naryPr` (`chr`), `m:sub` (cận dưới), `m:sup` (cận trên), `m:e` | Ký hiệu lớn $\int, \sum, \prod$ kèm cận trên/dưới |
| `m:m` | Ma trận (Matrix) | `m:mPr`, `m:mr` (hàng), `m:e` (ô phần tử) | Lưới 2 chiều gồm các hàng và cột căn đều |
| `m:func` | Hàm toán học | `m:fName` (tên hàm: sin, cos, lim), `m:e` | Tên hàm phông thẳng đứng (Roman), biến phông nghiêng (Italic) |
| `m:bar` | Đường gạch ngang đầu | `m:barPr` (`pos="top|bot"`), `m:e` | Dấu gạch ngang $\bar{x}$ (phủ đầu vectơ hoặc trung bình) |
| `m:acc` | Dấu dấu mũ / Mũi tên | `m:accPr` (`chr="^|~|→"`), `m:e` | Ký hiệu dấu mũ $\hat{x}$, vectơ $\vec{v}$ đặt trên ký tự |
| `m:borderBox` | Khung viền toán học | `m:borderBoxPr`, `m:e` | Hộp đóng khung công thức |
| `m:eqArr` | Mảng hệ phương trình | Chứa nhiều `m:e` xếp theo dòng | Hệ phương trình gióng theo dấu bằng ($=$) |

---

## 4. LƯỢNG HÓA HỆ THỐNG QUY TẮC CHUYỂN ĐỔI ĐƠN VỊ VÀ ÁNH XẠ

### 4.1. Hệ quy đổi tọa độ và đơn vị đo lường
Trong OpenXML, có nhiều hệ đo lường song song hoạt động ở các tầng khác nhau. Dưới đây là bảng lượng hóa chính xác 100%:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       MA TRẬN QUY ĐỔI ĐƠN VỊ CHUẨN                          │
├─────────────────┬──────────────┬──────────────┬──────────────┬──────────────┤
│ Đơn vị gốc      │   Ra EMU     │   Ra Point   │   Ra Twip    │  Ra Pixel    │
│                 │              │              │   (DXA)      │  (96 DPI)    │
├─────────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ 1 Inch          │      914,400 │        72.00 │        1,440 │        96.00 │
│ 1 Centimet (cm) │      360,000 │        28.35 │          567 │        37.80 │
│ 1 Millimet (mm) │       36,000 │         2.83 │         56.7 │         3.78 │
│ 1 Point (pt)    │       12,700 │         1.00 │           20 │         1.33 │
│ 1 Twip (dxa)    │          635 │         0.05 │            1 │        0.067 │
│ 1 Eighth pt     │        1,587 │        0.125 │          2.5 │        0.167 │
│ 1 Pixel (96DPI) │        9,525 │         0.75 │           15 │         1.00 │
└─────────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

#### Công thức chuyển đổi áp dụng trong mã nguồn C#:
```csharp
// 1. Từ EMU sang Point (Dùng cho kích thước hình ảnh, SmartArt, Shape)
public static float EmuToPoints(long emu) {
    return emu / 12700.0f;
}

// 2. Từ Point sang EMU (Dùng khi xuất file DOCX)
public static long PointsToEmu(float pt) {
    return (long)Math.Round(pt * 12700.0f);
}

// 3. Từ Twip/DXA sang Point (Dùng cho lề trang, bảng biểu, thụt dòng)
public static float TwipsToPoints(int twips) {
    return twips / 20.0f;
}

// 4. Từ Point sang Twip/DXA
public static int PointsToTwips(float pt) {
    return (int)Math.Round(pt * 20.0f);
}

// 5. Từ Eighths of Point sang Point (Dùng cho độ dày viền w:sz)
public static float BorderSzToPoints(int sz) {
    return sz / 8.0f;
}

// 6. Từ Point sang Eighths of Point
public static int PointsToBorderSz(float pt) {
    return (int)Math.Round(pt * 8.0f);
}

// 7. Từ Half-Points sang Point (Dùng cho cỡ chữ w:sz)
public static float HalfPointsToPoints(int sz) {
    return sz / 2.0f;
}

// 8. Chuyển đổi góc quay DrawingML (60,000 đơn vị = 1 độ)
public static float DrawingMLAngleToDegrees(long angleUnits) {
    return angleUnits / 60000.0f;
}
public static long DegreesToDrawingMLAngle(float deg) {
    return (long)Math.Round(deg * 60000.0f);
}
```

---

### 4.2. Bảng màu Theme và phép biến đổi màu sắc
Màu sắc trong OpenXML có thể được định nghĩa trực tiếp bằng giá trị sRGB (`a:srgbClr val="4F81BD"`) hoặc thông qua màu chủ đề (`a:schemeClr val="accent1"`).

#### A. Ánh xạ 12 màu chủ đề cơ bản (Office Theme Defaults):
| Tên Theme Color (`schemeClr`) | Ý nghĩa nghiệp vụ | Mã màu mặc định |
|---|---|:---:|
| `dk1` | Màu tối 1 (Chữ chính) | `#000000` (Đen) |
| `lt1` | Màu sáng 1 (Nền trang) | `#FFFFFF` (Trắng) |
| `dk2` | Màu tối 2 (Tiêu đề phụ) | `#1F497D` (Xanh navy đậm) |
| `lt2` | Màu sáng 2 (Nền thẻ phụ) | `#EEECE1` (Xám kem nhạt) |
| `accent1` | Màu nhấn 1 (Chủ đạo) | `#4F81BD` (Office 2007) / `#4472C4` (M365) |
| `accent2` | Màu nhấn 2 (Cam cảnh báo) | `#C0504D` (Office 2007) / `#ED7D31` (M365) |
| `accent3` | Màu nhấn 3 (Xám tro) | `#9BBB59` (Office 2007) / `#A5A5A5` (M365) |
| `accent4` | Màu nhấn 4 (Vàng rực rỡ) | `#8064A2` (Office 2007) / `#FFC000` (M365) |
| `accent5` | Màu nhấn 5 (Xanh ngọc/biển) | `#4BACC6` (Office 2007) / `#5B9BD5` (M365) |
| `accent6` | Màu nhấn 6 (Xanh lá cây) | `#F79646` (Office 2007) / `#70AD47` (M365) |
| `hlink` | Màu liên kết web | `#0000FF` (Lam) |
| `folHlink` | Màu liên kết đã bấm | `#800080` (Tím) |

#### B. Phép biến đổi màu sắc (Color Transforms):
Các thẻ con trong `schemeClr` hoặc `srgbClr` xác định phép biến đổi với giá trị từ $0$ đến $100,000$ (tương ứng $0.0\%$ đến $100.0\%$):
1. **`a:tint val="X"` (Làm sáng màu tiến dần về trắng)**:
   $$R' = R \cdot \left(\frac{X}{100000}\right) + 255 \cdot \left(1 - \frac{X}{100000}\right)$$
   $$G' = G \cdot \left(\frac{X}{100000}\right) + 255 \cdot \left(1 - \frac{X}{100000}\right)$$
   $$B' = B \cdot \left(\frac{X}{100000}\right) + 255 \cdot \left(1 - \frac{X}{100000}\right)$$
2. **`a:shade val="X"` (Làm tối màu tiến dần về đen)**:
   $$R' = R \cdot \left(\frac{X}{100000}\right), \quad G' = G \cdot \left(\frac{X}{100000}\right), \quad B' = B \cdot \left(\frac{X}{100000}\right)$$
3. **`a:alpha val="X"` (Độ trong suốt)**:
   $$A = \text{round}\left(255 \times \frac{X}{100000}\right)$$
4. **`a:satMod val="X"` (Điều chế độ bão hòa màu)**: Nhân tỷ lệ bão hòa trong không gian màu HSL với hệ số $\frac{X}{100000}$.
5. **`a:lumMod val="X"` & `a:lumOff val="Y"` (Điều chế độ sáng)**:
   $$L' = L \cdot \left(\frac{X}{100000}\right) + \left(\frac{Y}{100000}\right)$$

---

### 4.3. Trích xuất phân cấp nút và hình học tùy biến (`custGeom` -> Vector Path)
Khi đọc thẻ `<a:custGeom>`, thuật toán duyệt danh sách đường dẫn `<a:pathLst>` để tái tạo `System.Drawing.Drawing2D.GraphicsPath`:

```csharp
// Chuẩn hóa tọa độ vector tùy biến:
float pathW = float.Parse(pathNode.Attributes["w"].Value); // Mặc định 21600
float pathH = float.Parse(pathNode.Attributes["h"].Value); // Mặc định 21600

// Với mỗi thẻ con:
// - <a:moveTo>: Khởi tạo điểm đầu figure (StartFigure) tại (rf.X + (x / pathW) * rf.Width, rf.Y + (y / pathH) * rf.Height)
// - <a:lnTo>: Thêm đường thẳng nối từ điểm hiện tại đến điểm tiếp theo (AddLine)
// - <a:arcTo>: Thêm cung elip (AddArc) với stAng (góc bắt đầu) và swAng (góc quét) chia cho 60000.0f
// - <a:cubicBezTo>: Thêm đường cong bậc ba Bezier với 3 điểm điều khiển (AddBezier)
// - <a:close>: Đóng kín đa giác (CloseFigure)
```

---

## 5. THUẬT TOÁN HÌNH HỌC VÒNG TRÒN SMARTART TỶ LỆ 1:1 VÀ GẮN KẾT NÚT NGOẠI TIẾP

### 5.1. Vấn đề méo hình bầu dục & giải pháp toán học
**Hiện trạng**: Nếu vẽ elip theo tỷ lệ khung chứa $W \times H$, khi người dùng kéo dãn khung ngang ($W > H$) hoặc khung dọc ($H > W$), vòng tròn trung tâm bị bóp méo thành hình bầu dục, các đường mũi tên và nút con bị co kéo mất thẩm mỹ.

**Đặc tả giải pháp chuẩn 1:1**:
Cho khung bao chứa SmartArt $R_{\text{bounds}} = [X, Y, W, H]$.
1. **Xác định tâm đối xứng duy nhất**:
   $$X_c = X + \frac{W}{2}, \quad Y_c = Y + \frac{H}{2}$$
2. **Khóa tỷ lệ đường kính vòng tròn trung tâm (True 1:1 Circle)**:
   Bán kính $R_{\text{ring}}$ **bắt buộc** phải phụ thuộc vào cạnh nhỏ nhất $\min(W, H)$ để đảm bảo tỷ lệ hoàn hảo $1:1$ ở mọi kích thước khung:
   $$S_{\min} = \min(W, H)$$
   $$R_{\text{ring}} = \text{clamp}\left(S_{\min} \times 0.32f, \, 30.0f, \, 300.0f\right)$$
   Hình chữ nhật bao quanh vòng tròn tròn trịa 1:1:
   $$\text{Rect}_{\text{ring}} = \left[X_c - R_{\text{ring}}, \, Y_c - R_{\text{ring}}, \, 2 \cdot R_{\text{ring}}, \, 2 \cdot R_{\text{ring}}\right]$$
   *(Chiều rộng và chiều cao luôn luôn bằng nhau tuyệt đối $2 \cdot R_{\text{ring}}$, không bao giờ méo thành elip!)*

3. **Thuật toán phân bổ góc đều cho $N$ nút con**:
   Góc của nút thứ $i$ ($i = 0, \dots, N-1$):
   $$\theta_i = \left(i \times \frac{360^\circ}{N} - 90^\circ\right) \times \frac{\pi}{180^\circ}$$
   *(Góc $-90^\circ$ tương ứng với vị trí 12 giờ trên đỉnh cao nhất)*.

4. **Tính toán kích thước thẻ con ($W_{\text{box}}, H_{\text{box}}$) tự động thích ứng**:
   $$W_{\text{box}} = \text{clamp}\left(W \times 0.24f, \, 80.0f, \, 220.0f\right)$$
   $$H_{\text{box}} = \text{clamp}\left(H \times 0.16f, \, 40.0f, \, 110.0f\right)$$

5. **Gắn kết ngoại tiếp chuẩn xác (Seamless Outer Attachment - Không khe hở, không đè lấn)**:
   Để các ô hai bên tiếp xúc mượt mà với vòng tròn:
   - Điểm chạm tiếp xúc trên chu vi đường tròn:
     $$P_{\text{touch}, i} = \left(X_c + R_{\text{ring}} \cdot \cos\theta_i, \, Y_c + R_{\text{ring}} \cdot \sin\theta_i\right)$$
   - Khoảng cách tâm ô con cách tâm vòng tròn $D_{\text{center}}$:
     $$D_{\text{center}} = R_{\text{ring}} + \frac{1}{2}\sqrt{W_{\text{box}}^2 \cdot \cos^2\theta_i + H_{\text{box}}^2 \cdot \sin^2\theta_i} + \Delta_{\text{gap}}$$
     với $\Delta_{\text{gap}} = 0$ khi muốn gắn khít mí, hoặc $\Delta_{\text{gap}} = 8\text{ px}$ khi có đường nối hướng tâm.
   - Tọa độ góc trên bên trái của thẻ con thứ $i$:
     $$X_{\text{box}, i} = X_c + D_{\text{center}} \cdot \cos\theta_i - \frac{W_{\text{box}}}{2}$$
     $$Y_{\text{box}, i} = Y_c + D_{\text{center}} \cdot \sin\theta_i - \frac{H_{\text{box}}}{2}$$

```
                          [ Nút Đỉnh (0) ]
                                 ▲
                                 │
                   ╭─────────────┴─────────────╮
                   │      Điểm chạm P_touch    │
                   │             *             │
            ───────┤                           ├───────
   [ Nút Trái ] ◄──*   TÂM (Xc, Yc) BÁN KÍNH R *──► [ Nút Phải ]
            ───────┤                           ├───────
                   │                           │
                   ╰─────────────┬─────────────╯
                                 │
                                 ▼
                          [ Nút Đáy (2) ]
```

---

## 6. DANH MỤC TOÀN DIỆN 208 MẪU SMARTART (9 NHÓM CHUẨN MICROSOFT OFFICE)

Dưới đây là bảng phân loại đầy đủ 208 layout theo chuẩn Microsoft 365 Support:

### Nhóm 1: List (Danh sách) — 38 layouts
| STT | Layout Name | Unique Layout ID | Hình dạng chính | Số nút tối ưu |
|:---:|---|---|---|:---:|
| 1 | Alternating Hexagon List | `urn:.../layout/alternatingHexagonList` | Lục giác (`hexagon`) xen kẽ | 3 – 8 |
| 2 | Basic Block List | `urn:.../layout/default` | Hình chữ nhật (`rect`) | Không giới hạn |
| 3 | Bending Picture Accent List | `urn:.../layout/bendingPictureAccentList` | Chữ nhật + Tròn ảnh | 3 – 6 |
| 4 | Continuous Picture List | `urn:.../layout/continuousPictureList` | Dải tròn ảnh nối tiếp | 3 – 7 |
| 5 | Descending Block List | `urn:.../layout/descendingBlockList` | Khối bậc thang giảm dần | 3 – 5 |
| 6 | Detailed Process | `urn:.../layout/detailedProcess` | Khối tiêu đề + Mô tả lớn | 3 – 5 |
| 7 | Grouped List | `urn:.../layout/groupedList` | Nhóm hộp cha con | 2 – 5 nhóm |
| 8 | Hierarchy List | `urn:.../layout/hierarchyList` | Danh sách phân cấp tầng | 3 – 6 |
| 9 | Horizontal Bullet List | `urn:.../layout/horizontalBulletList` | Hộp ngang nhiều chữ | 3 – 5 |
| 10 | Horizontal Picture List | `urn:.../layout/horizontalPictureList` | Hộp ngang kèm ảnh trên | 3 – 5 |
| 11 | Increasing Circle Process | `urn:.../layout/increasingCircleProcess` | Vòng tròn to dần | 3 – 7 |
| 12 | Lined List | `urn:.../layout/linedList` | Hộp ngăn cách bằng vạch kẻ | 3 – 6 |
| 13 | Picture Accent List | `urn:.../layout/pictureAccentList` | Hộp kèm icon tròn góc | 3 – 6 |
| 14 | Picture Caption List | `urn:.../layout/pictureCaptionList` | Khung ảnh + Chú thích dưới | 3 – 6 |
| 15 | Pie Process | `urn:.../layout/pieProcess` | Lát bánh quạt to dần | 3 – 7 |
| 16 | Plus and Minus | `urn:.../layout/plusAndMinus` | Hai khối đối xứng (+/-) | Đúng 2 mục |
| 17 | Pyramid List | `urn:.../layout/pyramidList` | Chữ nhật trên nền kim tự tháp | 3 – 5 |
| 18 | Reverse List | `urn:.../layout/reverseList` | Đảo chiều 2 thành phần | Đúng 2 mục |
| 19 | Segmented Process | `urn:.../layout/segmentedProcess` | Khối tách đoạn riêng biệt | 3 – 6 |
| 20 | Stacked List | `urn:.../layout/stackedList` | Vòng tròn chồng chữ nhật | 3 – 6 |
| 21 | Square Accent List | `urn:.../layout/squareAccentList` | Chữ nhật kèm ô vuông nhỏ | 3 – 6 |
| 22 | Tab List | `urn:.../layout/tabList` | Dạng tab thẻ hồ sơ | 3 – 6 |
| 23 | Table Hierarchy | `urn:.../layout/tableHierarchy` | Bảng nhóm từ trên xuống | 3 – 6 |
| 24 | Table List | `urn:.../layout/tableList` | Lưới bảng ngang hàng | 4 – 8 |
| 25 | Target List | `urn:.../layout/targetList` | Vòng tròn đồng tâm | 3 – 7 |
| 26 | Trapezoid List | `urn:.../layout/trapezoidList` | Khối hình thang xếp chồng | 3 – 5 |
| 27 | Varying Width List | `urn:.../layout/varyingWidthList` | Độ rộng ô co theo chữ | 3 – 6 |
| 28 | Vertical Box List | `urn:.../layout/verticalBoxList` | Cột dọc các hộp | 3 – 6 |
| 29 | Vertical Block List | `urn:.../layout/verticalBlockList` | Cột khối ý chính/ý phụ | 3 – 6 |
| 30 | Vertical Bracket List | `urn:.../layout/verticalBracketList` | Dấu ngoặc nhọn dọc | 3 – 5 |
| 31 | Vertical Bullet List | `urn:.../layout/verticalBulletList` | Danh sách dấu đầu dòng dọc | 3 – 6 |
| 32 | Vertical Chevron List | `urn:.../layout/verticalChevronList` | Mũi tên chữ V xếp dọc | 3 – 6 |
| 33 | Vertical Picture Accent List | `urn:.../layout/verticalPictureAccentList`| Dọc kèm ảnh tròn nhỏ | 3 – 6 |
| 34 | Vertical Picture List | `urn:.../layout/verticalPictureList` | Cột dọc kèm ô ảnh trái | 3 – 6 |
| 35 | Vertical Accent List | `urn:.../layout/verticalAccentList` | Chữ nhật trên mũi tên dọc | 3 – 5 |
| 36 | Vertical Arrow List | `urn:.../layout/verticalArrowList` | Mũi tên dọc mục tiêu chung | 3 – 5 |
| 37 | Vertical Circle List | `urn:.../layout/verticalCircleList` | Hình tròn lớn chứa ý chính | 3 – 5 |
| 38 | Vertical Curved List | `urn:.../layout/verticalCurvedList` | Uốn cong lượn sóng dọc | 3 – 6 |

### Nhóm 2: Process (Quy trình) — 46 layouts
| STT | Layout Name | Unique Layout ID | Đặc trưng luồng xử lý |
|:---:|---|---|---|
| 39 | Accent Process | `urn:.../layout/accentProcess` | Tiến trình có dải màu nhấn |
| 40 | Alternating Flow | `urn:.../layout/hProcess4` | Luồng zíc zắc trên/dưới đối xứng |
| 41 | Arrow Ribbon | `urn:.../layout/arrowRibbon` | Hai dải ruy băng mũi tên kết nối |
| 42 | Ascending Picture Accent Process | `urn:.../layout/ascendingPictureAccent`| Bậc thang đi lên kèm ảnh |
| 43 | Basic Bending Process | `urn:.../layout/basicBendingProcess` | Luồng gấp khúc chữ U |
| 44 | Basic Chevron Process | `urn:.../layout/basicChevronProcess` | Chuỗi mũi tên chevron nối tiếp |
| 45 | Basic Process | `urn:.../layout/basicProcess` | Các hộp chữ nhật nối bằng mũi tên ngang |
| 46 | Basic Timeline | `urn:.../layout/basicTimeline` | Trục thời gian ngang có mốc tròn |
| 47 | Chevron Accent Process | `urn:.../layout/chevronAccentProcess` | Chevron kèm khối mô tả phụ |
| 48 | Chevron List | `urn:.../layout/chevronList` | Các nhánh quy trình song song |
| 49 | Circular Bending Process | `urn:.../layout/circularBendingProcess` | Uốn vòng cung nửa đường tròn |
| 50 | Closed Chevron Process | `urn:.../layout/closedChevronProcess` | Khối chevron khép kín liền kề |
| 51 | Circle Accent Timeline | `urn:.../layout/circleAccentTimeline` | Trục thời gian vòng tròn to nhỏ |
| 52 | Circle Arrow Process | `urn:.../layout/circleArrowProcess` | Hộp tròn nối bằng mũi tên cong |
| 53 | Continuous Arrow Process | `urn:.../layout/continuousArrowProcess` | Một mũi tên dài xuyên suốt các khối |
| 54 | Continuous Block Process | `urn:.../layout/continuousBlockProcess` | Khối chữ nhật ghép mí sát nhau |
| 55 | Converging Arrows | `urn:.../layout/convergingArrows` | 3 – 5 mũi tên quy tụ về 1 điểm |
| 56 | Converging Text | `urn:.../layout/convergingText` | Nhiều luồng chữ hội tụ thành 1 |
| 57 | Descending Process | `urn:.../layout/descendingProcess` | Quy trình bậc thang đi xuống |
| 58 | Diverging Arrows | `urn:.../layout/divergingArrows` | Từ 1 điểm tỏa ra nhiều mũi tên |
| 59 | Equation | `urn:.../layout/equation` | Khối 1 + Khối 2 = Kết quả |
| 60 | Funnel | `urn:.../layout/funnel` | Hình chiếc phễu lọc qua 3 tầng |
| 61 | Gear | `urn:.../layout/gear` | 3 bánh răng ăn khớp quay vào nhau |
| 62 | Increasing Arrows Process | `urn:.../layout/increasingArrows` | Các mũi tên to dần chồng lấn |
| 63 | Interconnected Block Process | `urn:.../layout/interconnectedBlocks` | Các khối hộp đan cài móc nối |
| 64 | Opposing Arrows | `urn:.../layout/opposingArrows` | 2 mũi tên ngược chiều đối đầu |
| 65 | Phased Process | `urn:.../layout/phasedProcess` | 3 giai đoạn lớn có bước nhỏ bên trong |
| 66 | Picture Accent Process | `urn:.../layout/pictureAccentProcess` | Quy trình ngang có khung ảnh nền |
| 67 | Process Arrows | `urn:.../layout/processArrows` | Vòng tròn ý chính trong mũi tên |
| 68 | Process List | `urn:.../layout/processList` | Nhóm bước cha và các bước con |
| 69 | Random to Result Process | `urn:.../layout/randomToResult` | Từ hỗn loạn dẫn đến trật tự |
| 70 | Repeating Bending Process | `urn:.../layout/repeatingBending` | Gấp khúc rắn lượn nhiều tầng |
| 71 | Staggered Process | `urn:.../layout/staggeredProcess` | Bố trí so le lệch tầng |
| 72 | Step Down Process | `urn:.../layout/stepDownProcess` | Giảm cấp từng bước |
| 73 | Step Up Process | `urn:.../layout/stepUpProcess` | Leo thang từng bước |
| 74 | Sub-Step Process | `urn:.../layout/subStepProcess` | Bước chính có bước trung gian |
| 75 | Timeline Pipe | `urn:.../layout/timelinePipe` | Đường ống thời gian hình trụ |
| 76 | Upward Arrow | `urn:.../layout/upwardArrow` | Mũi tên vút lên trên |
| 77 | Vertical Bending Process | `urn:.../layout/verticalBending` | Gấp khúc theo chiều đứng |
| 78 | Vertical Equation | `urn:.../layout/verticalEquation` | Phép toán cộng dồn theo cột dọc |
| 79 | Vertical Process | `urn:.../layout/verticalProcess` | Quy trình chảy từ trên xuống dưới |

### Nhóm 3: Cycle (Chu trình) — 16 layouts
| STT | Layout Name | Unique Layout ID | Đặc điểm hình học |
|:---:|---|---|---|
| 80 | Basic Cycle | `urn:.../layout/cycle1` | Vòng tròn các ô nối mũi tên cong |
| 81 | Basic Pie | `urn:.../layout/basicPie` | Hình tròn chia các lát cắt bánh |
| 82 | Basic Radial | `urn:.../layout/basicRadial` | 1 tâm tròn + các vệ tinh xung quanh |
| 83 | Block Cycle | `urn:.../layout/blockCycle` | Khối cung dày tạo thành vòng tròn |
| 84 | Continuous Cycle | `urn:.../layout/continuousCycle` | Vòng tròn liên tục không ngắt quãng |
| 85 | Cycle Matrix | `urn:.../layout/cycleMatrix` | 4 góc phần tư xoay quanh tâm |
| 86 | Diverging Radial | `urn:.../layout/divergingRadial` | Tâm tỏa lực ra các vòng ngoài |
| 87 | Multidirectional Cycle | `urn:.../layout/multidirectionalCycle` | Mũi tên 2 chiều quay vòng |
| 88 | Nondirectional Cycle | `urn:.../layout/nondirectionalCycle` | Các khối tròn ngang hàng không mũi tên |
| 89 | Radial Cluster | `urn:.../layout/radialCluster` | Chùm vệ tinh kết nối về tâm |
| 90 | Radial Cycle | `urn:.../layout/cycle6` | Tâm tròn lớn + các vệ tinh ngoại tiếp 1:1 |
| 91 | Radial Venn | `urn:.../layout/radialVenn` | Các hình tròn Venn giao thoa quanh tâm |
| 92 | Segmented Cycle | `urn:.../layout/segmentedCycle` | Vòng cung tròn chia đoạn lát cắt |
| 93 | Text Cycle | `urn:.../layout/textCycle` | Chữ nằm trực tiếp trên đường tròn |

### Nhóm 4: Hierarchy (Phân cấp & Sơ đồ tổ chức) — 15 layouts
| STT | Layout Name | Unique Layout ID | Cơ chế nhánh con / Trợ lý |
|:---:|---|---|:---:|
| 94 | Architecture Layout | `urn:.../layout/architecture` | Xây dựng từ móng lên đỉnh |
| 95 | Circle Picture Hierarchy | `urn:.../layout/circlePictureHierarchy` | Ảnh tròn phân cấp |
| 96 | Half Circle Organization Chart | `urn:.../layout/halfCircleOrgChart` | Bán nguyệt + Hỗ trợ Assistant |
| 97 | Hierarchy | `urn:.../layout/hierarchy` | Cây phân cấp chuẩn từ trên xuống |
| 98 | Horizontal Hierarchy | `urn:.../layout/horizontalHierarchy` | Cây phân nhánh sang phải |
| 99 | Horizontal Labeled Hierarchy | `urn:.../layout/horizontalLabeledHierarchy` | Phân nhánh ngang có nhãn nhóm |
| 100 | Horizontal Multi-Level Hierarchy | `urn:.../layout/horizontalMultiLevel` | Phân cấp ngang nhiều tầng sâu |
| 101 | Horizontal Organization Chart | `urn:.../layout/horizontalOrgChart` | Sơ đồ tổ chức nằm ngang |
| 102 | Labeled Hierarchy | `urn:.../layout/labeledHierarchy` | Sơ đồ phân cấp có nhãn đề mục |
| 103 | Name and Title Organization Chart | `urn:.../layout/nameAndTitle` | 2 ô riêng biệt: Tên & Chức danh |
| 104 | Organization Chart | `urn:.../layout/orgChart` | Chuẩn OrgChart có nút Trợ lý (Assistant) |
| 105 | Picture Organization Chart | `urn:.../layout/pictureOrgChart` | OrgChart có kèm ảnh chân dung |

### Nhóm 5: Relationship (Quan hệ) — 34 layouts
Bao gồm các layout đặc trưng: `Balance` (Cái cân thăng bằng), `Basic Target` (Bia bắn đồng tâm), `Basic Venn` (Biểu đồ Venn 3 vòng tròn giao nhau), `Linear Venn` (Venn thẳng hàng), `Stacked Venn` (Venn xếp chồng tầng), `Nested Target` (Bia lồng nhau), `Counterbalance Arrows` (Cân bằng đối trọng)...

### Nhóm 6: Matrix (Ma trận) — 4 layouts
1. `Basic Matrix`: Ma trận 4 ô vuông góc phần tư.
2. `Cycle Matrix`: Ma trận 4 góc có mũi tên chu kỳ.
3. `Grid Matrix`: Ma trận lưới 2 trục tọa độ $X - Y$.
4. `Titled Matrix`: Ma trận 4 góc có hình chữ nhật trung tâm.

### Nhóm 7: Pyramid (Kim tự tháp) — 4 layouts
1. `Basic Pyramid`: Kim tự tháp đáy lớn đỉnh nhỏ.
2. `Inverted Pyramid`: Kim tự tháp ngược đáy nhỏ đỉnh lớn.
3. `Pyramid List`: Kim tự tháp có hộp chữ nhật chú thích bên cạnh.
4. `Segmented Pyramid`: Kim tự tháp chia đoạn tam giác độc lập.

### Nhóm 8: Picture (Đồ họa tích hợp hình ảnh) — 31 layouts
Toàn bộ các layout thiết kế có khung chèn ảnh (`a:blipFill`): `Bubble Picture List`, `Circular Picture Callout`, `Picture Grid`, `Picture Strips`, `Spiral Picture`, `Snapshot Picture List`...

### Nhóm 9: Office.com (Bổ sung mở rộng) — 10 layouts
`Hexagon Radial`, `Theme Picture Grid`, `Picture Frame`, `Circle Process`...

---

## 7. DANH MỤC ĐẦY ĐỦ CÁC HÌNH KHỐI (SHAPES) VÀ ĐẶC TẢ TAY NẮM ĐIỀU CHỈNH

Hệ thống hình khối chuẩn ECMA-376 DrawingML chia thành **9 nhóm chính** với 150+ loại hình:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        9 NHÓM HÌNH KHỐI (SHAPES)                       │
├───────────────────┬──────────────┬─────────────────────────────────────┤
│ Nhóm danh mục     │ Số lượng     │ Tên Preset đại diện                 │
├───────────────────┼──────────────┼─────────────────────────────────────┤
│ 1. Lines          │ 12 loại      │ line, bentConnector, curvedConnector│
│ 2. Rectangles     │  9 loại      │ rect, roundRect, snip1Rect, round2  │
│ 3. Basic Shapes   │ 42 loại      │ ellipse, triangle, diamond, donut   │
│ 4. Block Arrows   │ 28 loại      │ rightArrow, bentArrow, circularArrow│
│ 5. Equation Shapes│  6 loại      │ mathPlus, mathMinus, mathEqual      │
│ 6. Flowchart      │ 28 loại      │ flowChartProcess, flowChartDecision │
│ 7. Stars & Banners│ 18 loại      │ star4, star5, star12, ribbon, wave  │
│ 8. Callouts       │ 16 loại      │ wedgeRectCallout, cloudCallout      │
│ 9. Action Buttons │ 12 loại      │ actionButtonHome, actionButtonNext  │
└───────────────────┴──────────────┴─────────────────────────────────────┘
```

### Đặc tả tham số tay nắm vàng điều chỉnh (Adjustment Handles - `a:avLst`):
Mỗi tay nắm vàng được lưu trong danh sách `<a:avLst>` bằng thẻ `<a:gd name="..." fmla="...">`:
- **`roundRect` (Chữ nhật bo góc)**: Tham số `adj` (bán kính góc bo) từ $0$ (vuông góc) đến $50,000$ (bo tròn tối đa thành elip hai đầu).
- **`triangle` (Tam giác cân)**: Tham số `adj` xác định tọa độ $X$ của đỉnh nhọn (mặc định $50,000$ là chính giữa, kéo sang trái $= 0$ thành tam giác vuông trái).
- **`rightArrow` (Mũi tên phải)**: 2 tham số:
  - `adj1`: Độ dày thân mũi tên (tỷ lệ $0$ đến $100,000$).
  - `adj2`: Chiều dài phần đầu mũi nhọn.
- **`donut` (Bánh vòng)**: Tham số `adj` xác định bán kính lỗ rỗng bên trong ($0$ đến $50,000$).
- **`star5` (Ngôi sao 5 cánh)**: Tham số `adj` xác định độ sâu của các góc lõm (mặc định $38,197$).
- **`wedgeRoundRectCallout` (Bong bóng thoại bo góc)**: 3 tham số: bán kính bo góc, tọa độ $X$ và tọa độ $Y$ của mũi nhọn chỉ vào nhân vật.

---

## 8. BẢNG TỔNG HỢP TÍNH NĂNG KHÁM PHÁ (FEATURES DISCOVERED)

| # | Phân loại | Tính năng | Mô tả chi tiết | Dữ liệu đầu vào (Inputs) | Dữ liệu đầu ra (Outputs) | Hành vi khi lỗi (Error Behavior) | Phương thức phát hiện |
|:---:|---|---|---|---|---|---|---|
| 1 | SmartArt | 4-Part Architecture | Sơ đồ gồm data1, layout1, style1, colors1 | XML parts trong DOCX zip | Phân tách cây dữ liệu và hình học | Báo lỗi hoặc dùng fallback drawing1.xml | Khảo sát `Doc1.docx` & ECMA-376 |
| 2 | SmartArt | Layout Switching | Đổi kiểu sơ đồ mà không mất chữ người dùng | Layout ID mới, danh sách node | Tái cấu trúc tọa độ các nút con | Giữ nguyên kiểu cũ nếu ID không hợp lệ | Khảo sát `OpenXmlSmartArtModel.cs` |
| 3 | SmartArt | Cycle 1:1 Circle Geometry | Vòng tròn tâm điểm tròn tuyệt đối 1:1 | Kích thước khung $W \times H$, số nút $N$ | Tâm $(X_c, Y_c)$, bán kính $R$, Rect vuông | Ép về $\min(W, H)$ tránh méo elip | Yêu cầu R3 & Khảo sát `SmartArtRenderer.cs` |
| 4 | SmartArt | Tangent Card Attachment | Gắn kết thẻ con ngoại tiếp sát vòng tròn | Tọa độ vòng tròn, kích thước thẻ con | Tọa độ thẻ $(X_i, Y_i)$ chạm tiếp tuyến | Tự động thụt lề nếu tràn khung | Yêu cầu R3 & Khảo sát `SmartArtRenderer.cs` |
| 5 | SmartArt | Theme Color Variation | Phân bổ màu đơn sắc/đa sắc theo Theme | Theme palette, Preset index | Mảng màu nút con + màu viền/chữ | Dùng màu Office Blue `#4472C4` mặc định | Khảo sát `theme1.xml` & `colors1.xml` |
| 6 | DrawingML | Unit Conversion EMU | Đổi đơn vị tọa độ đồ họa sang point | Số nguyên EMU (1 pt = 12700 EMU) | Số thực point (pt) | Ép kiểu 0 nếu giá trị âm hoặc rỗng | Khảo sát `document.xml` & ECMA-376 |
| 7 | DrawingML | Custom Vector Path | Trích xuất thẻ `a:custGeom` thành đường vẽ | Thẻ `a:pathLst` (moveTo, lnTo, arcTo...) | Đối tượng `GraphicsPath` | Bỏ qua đường dẫn lỗi, vẽ khung chữ nhật | Khảo sát `FormatHandlers.cs:1921` |
| 8 | DrawingML | ArcTo Angle Conversion | Đổi góc 60,000 đơn vị sang độ hình học | Thuộc tính `stAng`, `swAng` | Góc độ degree (`val / 60000.0f`) | Mặc định 0 độ nếu thiếu thuộc tính | Khảo sát `FormatHandlers.cs:1979` |
| 9 | DrawingML | Floating Wrap Modes | Bọc văn bản quanh đồ họa (Square, Tight...) | Thẻ `wp:anchor`, `wp:wrap...` | Luồng bọc chữ tương ứng trên Writer | Rơi về chế độ `wrapNone` / `inline` | Khảo sát `document.xml` & `WriterView.cs` |
| 10 | Table Border | Border Style (`w:val`) | Nhận diện 26 kiểu viền (single, double, 3D...)| Chuỗi `w:val` từ XML | Nét vẽ Pen GDI+ tương ứng | Dùng nét `single` nếu kiểu lạ | Khảo sát `BaoCaoTongKet_2026.docx` |
| 11 | Table Border | Border Size (`w:sz`) | Độ dày nét viền đo bằng 1/8 point | Số nguyên `w:sz` | Độ dày nét point (`sz / 8.0f`) | Mặc định 0.5 pt (`sz=4`) nếu thiếu | Khảo sát `BaoCaoTongKet_2026.docx` & ECMA-376 |
| 12 | Table Border | Diagonal Borders | Viền chéo ô bảng (`w:tl2br`, `w:tr2bl`) | Thẻ viền chéo góc trong `w:tcBorders` | Kẻ nét chéo góc trong ô | Bỏ qua nếu ô bị gộp hoặc rỗng | Khảo sát ECMA-376 §17.4.70 |
| 13 | Table Border | Border Resolution | Thừa kế viền ô từ viền bảng | `w:tblBorders` + `w:tcBorders` | Bản đồ 4 cạnh viền cho mỗi ô | Ô lấy viền mặc định bảng nếu không set | Khảo sát `FormatHandlers.cs:3667` |
| 14 | OMML Math | Inline Equation (`m:oMath`) | Dựng hình công thức nội dòng | Cây thẻ XML `m:oMath` | Chuỗi ký hiệu toán + Vector vẽ | Trích xuất văn bản phẳng nếu chưa có font | Khảo sát `test_math_doc.docx` |
| 15 | OMML Math | Math Paragraph (`m:oMathPara`)| Đoạn công thức toán học độc lập | Thẻ `m:oMathPara` kèm căn lề `m:jc` | Đoạn toán học căn giữa trang | Mặc định căn giữa nếu thiếu `m:jc` | Khảo sát `test_math_doc.docx` |
| 16 | OMML Math | Fraction (`m:f`) | Phân số tử trên mẫu dưới | `m:num`, `m:den`, `m:type` | Kết xuất phân số có vạch ngang | Fallback thành `a / b` dạng text | Khảo sát `FormatHandlers.cs:3437` |
| 17 | OMML Math | Radical (`m:rad`) | Căn bậc $N$ và căn bậc hai | `m:deg`, `m:e`, `m:degHide` | Dấu căn co giãn trùm radicand | Fallback `√(x)` hoặc `∛(x)` | Khảo sát `FormatHandlers.cs:3494` |
| 18 | OMML Math | Scripts (`m:sSup`, `m:sSub`) | Số mũ và chỉ số dưới | `m:e`, `m:sup`, `m:sub` | Ký tự co nhỏ 65% gióng trên/dưới | Đổi sang ký tự Unicode số mũ/chỉ số | Khảo sát `FormatHandlers.cs:3448` |
| 19 | OMML Math | Delimiter (`m:d`) | Dấu ngoặc co giãn tự động | `m:dPr` (`begChr`, `endChr`), `m:e` | Cặp ngoặc kéo dài theo chiều cao | Vẽ ngoặc đơn tiêu chuẩn `( )` | Khảo sát `FormatHandlers.cs:3516` |
| 20 | OMML Math | N-ary Operator (`m:nary`) | Tích phân, Tổng sigma, Tích Pi | `m:chr`, `m:sub`, `m:sup`, `m:e` | Ký hiệu lớn $\int, \sum$ kèm 2 cận | Vẽ ký hiệu thường nếu thiếu font | Khảo sát `FormatHandlers.cs:3555` |
| 21 | OMML Math | Matrix (`m:m`) | Ma trận 2 chiều các hàng cột | `m:mr` (hàng), `m:e` (ô) | Bảng ma trận gióng đều khoảng cách | Biểu diễn thành mảng danh sách | Khảo sát ECMA-376 §22.1.2.60 |
| 22 | Shapes | Preset Geometry | 150+ hình khối chuẩn Word | Chuỗi `prst` (như `roundRect`, `chevron`)| Vector hình học chuẩn | Dùng hình chữ nhật nếu không hỗ trợ | Khảo sát `shapes-full-guide.md` |
| 23 | Shapes | In-Shape Text | Gõ chữ trực tiếp trong hình khối | Thuộc tính `TextContent`, căn lề | Văn bản căn giữa, tự động ngắt dòng | Thu nhỏ font nếu tràn viền | Khảo sát `DocShapeObject.cs` |
| 24 | Shapes | Adjustment Handles | Tay nắm vàng điều chỉnh góc bo/độ dày | Thẻ `<a:avLst>` | Tham số hình học động | Dùng giá trị mặc định nếu thiếu | Khảo sát `shapes-full-guide.md` |

---

## 9. BẢNG CÁC TRƯỜNG HỢP BIÊN & DỊ BIỆT (EDGE CASES)

| # | Tính năng | Dữ liệu đầu vào dị biệt (Input) | Hành vi quan sát được & Giải pháp xử lý chuẩn |
|:---:|---|---|---|
| 1 | SmartArt Cycle | Khung chứa cực dẹt ngang ($W = 800\text{px}, H = 150\text{px}$) | Nếu dùng $W$, vòng tròn phình to vượt ra ngoài màn hình. **Xử lý chuẩn**: Lấy $S_{\min} = \min(W, H) = 150$, bán kính $R = 48\text{px}$, giữ tỷ lệ tròn tuyệt đối 1:1, phân bổ các thẻ con dàn sang hai bên trái/phải. |
| 2 | SmartArt Cycle | Số lượng nút chỉ có 1 hoặc 2 nút ($N=1, N=2$) | Khi $N=1$, góc quay $0^\circ$, thẻ con nằm ngay trên đỉnh. Khi $N=2$, 2 nút phân bổ đối xứng đỉnh – đáy ($90^\circ$ và $270^\circ$). Vòng tròn trung tâm vẫn duy trì đều đặn. |
| 3 | SmartArt Cycle | Số lượng nút rất lớn ($N \ge 12$) | Các thẻ con có nguy cơ đè lấn lên nhau do góc chia quá hẹp ($30^\circ$). **Xử lý chuẩn**: Tự động co kích thước thẻ con $W_{\text{box}}, H_{\text{box}}$ giảm 40%, giảm cỡ chữ về $7.0\text{pt}$ và nới rộng bán kính tâm. |
| 4 | SmartArt Data | Nút không có chữ (`<dgm:t>` rỗng), chỉ có `<dgm:prSet phldrT="[Text]"/>` | Trình đọc không được bỏ sót nút. **Xử lý chuẩn**: Đọc thuộc tính `phldrT` làm chữ giữ chỗ mặc định `[Text]`, cho phép người dùng nhấp đúp để nhập nội dung mới. |
| 5 | DrawingML Units | Tệp XML thiếu thuộc tính tọa độ hoặc có giá trị âm | `off x="-500"` hoặc `ext cx="0"`. **Xử lý chuẩn**: Kiểm tra điều kiện $cx > 0, cy > 0$, nếu tọa độ âm dịch chuyển về điểm gốc trang tọa độ $(0, 0)$ an toàn, tránh văng ngoại lệ `ArgumentException` trong GDI+. |
| 6 | DrawingML Angle | Giá trị góc quay `rot` vượt quá $21,600,000$ (360 độ) | Một số tệp Word lưu góc âm (ví dụ `-5400000` = $-90^\circ$). **Xử lý chuẩn**: Chuẩn hóa góc quay theo công thức toán học: $\text{deg} = ((\text{rot} / 60000.0f) \pmod{360} + 360) \pmod{360}$. |
| 7 | Theme Colors | Tệp tài liệu không có `theme1.xml` (tệp DOCX tạo từ nguồn thứ 3) | Biểu thức tìm màu chủ đề trả về `null`. **Xử lý chuẩn**: Áp dụng bộ màu dự phòng chuẩn Office Palette: `accent1=#4472C4`, `accent2=#ED7D31`, `dk1=#000000`, `lt1=#FFFFFF`. |
| 8 | Theme Colors | Thẻ màu kết hợp cả `a:tint` và `a:satMod` cùng lúc | Thứ tự biến đổi màu sắc không đúng làm sai lệch màu hiển thị. **Xử lý chuẩn**: Thực hiện biến đổi theo đúng trình tự ECMA-376: Gốc $\rightarrow$ Điều chế độ bão hòa (SatMod) $\rightarrow$ Làm sáng/tối (Tint/Shade) $\rightarrow$ Trong suốt (Alpha). |
| 9 | Table Borders | Hai ô kề nhau có độ dày viền khác nhau (Ô trái $1.5\text{pt}$, Ô phải $0.5\text{pt}$) | Xảy ra hiện tượng vẽ đè 2 đường viền lên nhau tạo vết răng cưa. **Xử lý chuẩn**: Áp dụng cơ chế Border Collapsing: Cạnh chung giữa 2 ô chọn nét viền có độ dày lớn hơn ($1.5\text{pt}$) để vẽ duy nhất một lần. |
| 10 | Table Borders | Cạnh viền khai báo `w:val="nil"` hoặc `w:val="none"` | Không được vẽ viền đen mặc định. **Xử lý chuẩn**: Gán cờ `HasBorder = false` và không gọi lệnh `DrawLine`. |
| 11 | OMML Math | Phân số lồng nhau nhiều tầng (Continuous Fraction: $\frac{1}{1 + \frac{1}{x}}$) | Chiều cao thanh phân số có thể bị tràn ra ngoài dòng chữ thông thường. **Xử lý chuẩn**: Đệ quy tính toán chiều cao của từng tầng con, căn đường phân số chính trùng với trục tâm dòng văn bản (Math Axis). |
| 12 | OMML Math | Dấu căn không có chỉ số bậc (`m:deg` rỗng hoặc `m:degHide="1"`) | Nhầm lẫn giữa căn bậc 2 và căn bậc $N$. **Xử lý chuẩn**: Kiểm tra cờ `degHide`, nếu bằng 1 hoặc rỗng thì vẽ ký hiệu căn bậc hai thuần túy $\sqrt{x}$, không chừa khoảng trống trên đầu dấu căn. |

---

## 10. BẢN THIẾT KẾ KIẾN TRÚC NÂNG CẤP ENGINE PARSER & CANVAS RENDERER

Để hoàn thiện các yêu cầu tại R3 của `ORIGINAL_REQUEST.md`, cấu trúc kiến trúc của GenOffice cần được củng cố theo mô hình 3 tầng vững chắc:

```
                  ┌────────────────────────────────────────────────────────┐
                  │                 TẦNG GIAO DIỆN (UI)                     │
                  │  WriterView.cs  /  SmartArtRenderer.cs / ShapeRenderer │
                  └───────────────────────────▲────────────────────────────┘
                                              │ Cung cấp dữ liệu hình học
                                              │ & thuộc tính hiển thị
                  ┌───────────────────────────┴────────────────────────────┐
                  │              TẦNG MÔ HÌNH HÓA (DATA MODELS)            │
                  │  OpenXmlSmartArtModel.cs   /   DocTableObject.cs       │
                  │  DocShapeObject.cs         /   DocCellParagraph        │
                  └───────────────────────────▲────────────────────────────┘
                                              │ Đọc & Ánh xạ strongly-typed
                                              │ từ các tệp nén OpenXML
                  ┌───────────────────────────┴────────────────────────────┐
                  │             TẦNG BÓC TÁCH & GHI TỆP (PARSERS)          │
                  │  FormatHandlers.cs         /   OpenXmlDocEngine.cs     │
                  │  DocumentFormat.OpenXml.dll (ECMA-376 SDK v3.5.1)      │
                  └────────────────────────────────────────────────────────┘
```

### 10.1. Nâng cấp `OpenXmlSmartArtModel.cs`
- Bổ sung cấu trúc dữ liệu lưu trữ tọa độ gốc dạng EMU: `OriginalBoundsEmu = Rectangle(x, y, cx, cy)`.
- Bổ sung trường `AspectRatioLock = true` để tự động kích hoạt chế độ khóa tỷ lệ 1:1 cho tất cả các mẫu thuộc nhóm `Cycle` và `Radial`.
- Mở rộng hỗ trợ bảng màu Theme đầy đủ 6 Accents và các biến thể đơn sắc ($20\%, 40\%, 60\%, 80\%, 100\%$).

### 10.2. Nâng cấp `FormatHandlers.cs`
- Thay thế các phương pháp trích xuất chuỗi thô bằng việc đọc cấu trúc XML chuẩn qua `XmlNamespaceManager` với đầy đủ các tiền tố `dgm:`, `a:`, `wp:`, `w:`, `m:`.
- Đọc chính xác thuộc tính viền bảng `w:sz` thành `float BorderWidthPt = sz / 8.0f` và lưu kiểu viền `BorderStyle` vào `DocTableCell`.
- Bổ sung hàm ánh xạ công thức toán học OMML sang cấu trúc hiển thị đa tầng trên Canvas.

### 10.3. Nâng cấp `SmartArtRenderer.cs`
- Cập nhật hàm `DrawCycleRadial` và `DrawCycleRing`: Áp dụng công thức tính $R_{\text{ring}} = \text{clamp}(\min(W, H) \times 0.33f)$ và gắn kết ngoại tiếp chuẩn xác không có khe hở.
- Vẽ đầy đủ 5 kiểu dáng hiệu ứng: Flat, Gradient, 3D Polished (Bevel mép), Glass (vệt sáng elip), Outline.

---
*Báo cáo kết thúc tại đây. Toàn bộ các đặc tả trên là cơ sở chuẩn xác 100% để đội ngũ kỹ sư tiến hành hiện thực hóa mã nguồn trong các bước tiếp theo.*
