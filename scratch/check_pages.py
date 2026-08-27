import re

with open(r"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf", "rb") as f:
    data = f.read()

pages = re.findall(rb"/Type\s*/Page[^s]", data)
print("Pages count in PDF:", len(pages))
