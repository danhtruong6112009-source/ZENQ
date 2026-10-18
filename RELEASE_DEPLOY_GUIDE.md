# HƯỚNG DẪN QUY TRÌNH PHÁT HÀNH (RELEASE) VÀ BẢO VỆ MÃ NGUỒN ZENQ OFFICE

Tài liệu này quy định chi tiết quy trình chuẩn mỗi khi thực hiện phát hành phiên bản mới của **ZENQ OFFICE**, đảm bảo:
1. Mỗi lần phát hành tăng đúng **+0.1** (Ví dụ: `5.5` -> `5.6` -> `5.7`...).
2. **TUYỆT ĐỐI KHÔNG CHỨA MÃ NGUỒN (SOURCE CODE)** trên GitHub Release dưới bất kỳ hình thức nào.
3. Người dùng cập nhật tự động trong ứng dụng hoặc tải về từ web đều nhận đúng file bộ cài đặt duy nhất: `ZenQOffice-Setup.exe`.

---

## 1. QUY TẮC ĐÓNG DẤU PHIÊN BẢN (+0.1)

Mỗi lần có lệnh yêu cầu đóng dấu / đẩy bản phát hành, số phiên bản sẽ tăng thêm **+0.1**.

### 5 Tệp bắt buộc phải đồng bộ số phiên bản:

1. **`src/AssemblyInfo.cs`**:
   ```csharp
   [assembly: AssemblyVersion("5.6.0.0")]
   [assembly: AssemblyFileVersion("5.6.0.0")]
   [assembly: AssemblyInformationalVersion("v5.6")]
   ```
2. **`src/core/UpdateManager.cs`**:
   ```csharp
   public const string CURRENT_VERSION = "5.6.0";
   public const string CURRENT_VERSION_TAG = "v5.6";
   ```
3. **`src/setup/ShortcutHelper.cs`**:
   ```csharp
   // DisplayVersion đăng ký trong Windows Add/Remove Programs
   rk.SetValue("DisplayVersion", "5.6");
   ```
4. **`src/ui/ZenQOfficeSplashScreen.cs`**:
   ```csharp
   // Màn hình khởi động SplashScreen
   g.DrawString("Phiên bản 5.6", verFont, verBrush, ...);
   ```
5. **`src/ui/OfficeOptionsDialogForm.cs`**:
   ```csharp
   // Hộp thoại Tùy chọn (Options)
   lblVer.Text = "PHIÊN BẢN 5.6";
   ```

---

## 2. NGUYÊN TẮC BẢO MẬT: CHẶN LỘ MÃ NGUỒN TRÊN GITHUB

### ⚠️ Cơ chế tự động của GitHub:
* Khi tạo một Git Tag trên commit đang chứa mã nguồn (`master`), GitHub sẽ **tự động** nén toàn bộ commit đó thành 2 file:
  * `Source code (zip)`
  * `Source code (tar.gz)`
* Dù trong phần Assets chỉ tải lên file `.exe`, bất kỳ ai truy cập Release đều có thể tải file `Source code (zip)` và thấy toàn bộ mã nguồn `.cs` của dự án.

### 🛡️ Giải pháp chặn triệt để (Orphan Empty Commit):
* Dự án duy trì một mốc commit độc lập không chứa bất kỳ file code nào (chỉ có duy nhất 1 file README rỗng):
  * **Commit Hash sạch:** `334686645e6dd717519e020e388891dc4859c958`
* **QUY TẮC BẤT DI BẤT DỊCH:**
  * Tag phiên bản (ví dụ `5.6` hoặc `v5.6`) **CHỈ ĐƯỢC PHÉP TRỎ VÀO COMMIT RỖNG NÀY**.
  * **TUYỆT ĐỐI KHÔNG** trỏ tag phiên bản vào commit trên nhánh `master`.
  * Khi đó, file `Source code (zip)` của GitHub khi người ngoài tải về sẽ chỉ có duy nhất 1 file README rỗng, bảo vệ 100% bản quyền mã nguồn.

---

## 3. QUY TRÌNH THỰC HIỆN TỪNG BƯỚC

### Bước 1: Biên dịch bộ cài đặt sạch
Chạy lệnh đóng gói:
```bat
cmd.exe /c build_setup.bat
```
Lệnh này sẽ:
* Biên dịch `ZenQOffice.exe` ở cấu hình `Release` trên `.NET 10`.
* Đóng gói payload sạch (đã lọc bỏ toàn bộ file `.pdb`, `.xml` tài liệu nội bộ).
* Biên dịch bộ cài đặt tự động `ZenQOffice-Setup.exe` (chứa icon bản quyền, manifest quyền Administrator, tính năng Silent Update `/silent`).

### Bước 2: Tạo và đẩy Git Tag an toàn
Gắn tag phiên bản mới vào commit sạch `334686645e6dd717519e020e388891dc4859c958`:
```bash
# Ví dụ phát hành bản 5.6:
git tag -a 5.6 334686645e6dd717519e020e388891dc4859c958 -m "ZenQ Office Release 5.6"
git tag -a v5.6 334686645e6dd717519e020e388891dc4859c958 -m "ZenQ Office Release v5.6"

# Đẩy tag lên GitHub:
git push origin 5.6 v5.6
```

### Bước 3: Đẩy Release và Tệp Cài Đặt Lên GitHub
Sử dụng script Python lấy Token tự động từ **Windows Credential Manager** (`git:https://danhtruong6112009-source@github.com`):

```python
import ctypes
from ctypes import wintypes
import json, os, urllib.request

# 1. Đọc Token bảo mật từ Windows Credential Manager
class Credential(ctypes.Structure):
    _fields_ = [
        ('Flags', wintypes.DWORD), ('Type', wintypes.DWORD),
        ('TargetName', wintypes.LPWSTR), ('Comment', wintypes.LPWSTR),
        ('LastWritten', wintypes.FILETIME), ('CredentialBlobSize', wintypes.DWORD),
        ('CredentialBlob', ctypes.c_void_p), ('Persist', wintypes.DWORD),
        ('AttributeCount', wintypes.DWORD), ('Attributes', ctypes.c_void_p),
        ('TargetAlias', wintypes.LPWSTR), ('UserName', wintypes.LPWSTR),
    ]

cp = ctypes.POINTER(Credential)()
ctypes.windll.advapi32.CredReadW('git:https://danhtruong6112009-source@github.com', 1, 0, ctypes.byref(cp))
token = ctypes.string_at(cp.contents.CredentialBlob, cp.contents.CredentialBlobSize).decode('utf-16-le').strip('\x00')

headers = {
    'Authorization': f'token {token}',
    'User-Agent': 'ZenQ-Release-Uploader',
    'Accept': 'application/vnd.github.v3+json'
}

TAG = "5.6"
REPO = "danhtruong6112009-source/ZENQ"

# 2. Tạo Release mới trên GitHub
create_rel_url = f"https://api.github.com/repos/{REPO}/releases"
rel_payload = json.dumps({
    "tag_name": TAG,
    "name": f"ZenQ Office {TAG} Official Release",
    "body": f"### Bản phát hành chính thức ZenQ Office {TAG}\n- Bộ cài đặt chuẩn tự động tích hợp đầy đủ tính năng.",
    "draft": False,
    "prerelease": False
}).encode('utf-8')

req = urllib.request.Request(create_rel_url, data=rel_payload, headers=headers, method='POST')
rel = json.loads(urllib.request.urlopen(req).read().decode('utf-8'))
release_id = rel['id']

# 3. Tải DUY NHẤT file ZenQOffice-Setup.exe lên Release
setup_path = r'c:\Users\Danh\Desktop\genofficepy\ZenQOffice-Setup.exe'
with open(setup_path, 'rb') as f:
    setup_bytes = f.read()

upload_url = f"https://uploads.github.com/repos/{REPO}/releases/{release_id}/assets?name=ZenQOffice-Setup.exe"
upload_headers = {
    'Authorization': f'token {token}',
    'User-Agent': 'ZenQ-Release-Uploader',
    'Content-Type': 'application/octet-stream',
    'Content-Length': str(len(setup_bytes))
}

up_req = urllib.request.Request(upload_url, data=setup_bytes, headers=upload_headers, method='POST')
res = json.loads(urllib.request.urlopen(up_req).read().decode('utf-8'))
print(f"Hoàn thành: {res['name']} ({res['size']:,} bytes)")
```

### Bước 4: Kiểm tra và xác thực
Kiểm tra lại Release qua API để đảm bảo:
1. `tag_name` khớp phiên bản.
2. Danh sách `assets` chỉ có **DUY NHẤT 1 FILE**: `ZenQOffice-Setup.exe`.
3. Tải thử file `Source code (zip)` về kiểm tra: chỉ chứa 1 file README rỗng, hoàn toàn không có source code.

---

## 4. TÓM TẮT NGUYÊN TẮC VÀNG
* **Tăng phiên bản:** Luôn tăng **+0.1** mỗi lần có lệnh đẩy phát hành.
* **Tag trỏ vào đâu:** Luôn trỏ vào `334686645e6dd717519e020e388891dc4859c958`.
* **File tải lên:** **CHỈ DUY NHẤT** `ZenQOffice-Setup.exe`, không đưa file exe portable hay file nén zip nào khác lên release.
