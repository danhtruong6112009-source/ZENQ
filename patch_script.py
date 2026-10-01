import re

file_path = r"c:\Users\Danh\Desktop\genofficepy\src\ui\SmartArtGalleryForm.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

missing_method = """
        private void DrawPreview_VerticalPictureStrips(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int x = r.X + 20 + i * 55;
                DrawPicturePlaceholder(g, new Rectangle(x, r.Y + 20, 45, 70), false);
            }
        }
"""

content = content.replace("private void DrawPreview_Spiral(Graphics g, Rectangle r)", missing_method + "\n        private void DrawPreview_Spiral(Graphics g, Rectangle r)")

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Done patching.")
