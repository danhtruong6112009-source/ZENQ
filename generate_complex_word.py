import win32com.client
import os

def create_complex_document(filepath):
    print("Starting Microsoft Word...")
    word = win32com.client.Dispatch("Word.Application")
    word.Visible = True 
    
    doc = word.Documents.Add()
    selection = word.Selection
    
    print("Adding Header and Footer...")
    for section in doc.Sections:
        # Header
        header_range = section.Headers(1).Range # wdHeaderFooterPrimary
        header_range.Text = "AUTO GENERATED DOCUMENT - FULL FEATURES - " * 5
        header_range.Font.Name = "Arial"
        header_range.Font.Size = 14
        header_range.Font.Color = 255 # Red
        header_range.ParagraphFormat.Borders(-3).LineStyle = 1 # wdBorderBottom, wdLineStyleSingle
        
        # Footer
        footer_range = section.Footers(1).Range
        footer_range.Text = "Footer Content | Page: "
        footer_range.Collapse(0) # wdCollapseEnd
        # add page number field
        doc.Fields.Add(Range=footer_range, Type=33) # wdFieldPage

    # 1. Shapes
    print("Adding Shapes...")
    selection.TypeText("=== 1. SHAPES ===\n")
    for i in range(1, 25): # Multiple shapes
        try:
            shape = doc.Shapes.AddShape(Type=i, Left=50 + (i%5)*50, Top=50 + (i%5)*50, Width=100, Height=100, Anchor=selection.Range)
            shape.Fill.ForeColor.RGB = (255 * i) % 16777215
            shape.Line.Weight = 2
            shape.Line.ForeColor.RGB = 0
            if i % 3 == 0:
                shape.TextFrame.TextRange.Text = f"Shape {i}"
        except Exception as e:
            pass
    selection.EndKey(Unit=6) # wdStory
    selection.InsertBreak(Type=7) # wdPageBreak

    # 2. SmartArt
    print("Adding SmartArt...")
    selection.TypeText("=== 2. SMART ART ===\n")
    try:
        for i in range(1, 15): # Add some SmartArt layouts
            try:
                layout = word.Application.SmartArtLayouts(i)
                smartart = doc.InlineShapes.AddSmartArt(layout, selection.Range)
                smartart.Width = 300
                smartart.Height = 200
                selection.TypeParagraph()
            except Exception as e:
                pass
    except Exception as e:
        selection.TypeText("SmartArt not supported on this Word version.\n")

    selection.EndKey(Unit=6)
    selection.InsertBreak(Type=7)
    
    # 3. WordArt
    print("Adding WordArt...")
    selection.TypeText("=== 3. WORD ART ===\n")
    try:
        for i in range(0, 10):
            shape = doc.Shapes.AddTextEffect(PresetTextEffect=i, Text=f"WordArt Effect {i}", FontName="Arial", FontSize=36, FontBold=0, FontItalic=0, Left=100, Top=100, Anchor=selection.Range)
            selection.TypeParagraph()
            selection.TypeParagraph()
            selection.TypeParagraph()
    except Exception as e:
        pass
        
    selection.EndKey(Unit=6)
    selection.InsertBreak(Type=7)

    # 4. Tables
    print("Adding Tables...")
    selection.TypeText("=== 4. TABLES ===\n")
    for i in range(1, 5):
        table = doc.Tables.Add(Range=selection.Range, NumRows=5, NumColumns=5, DefaultTableBehavior=1, AutoFitBehavior=0)
        table.Borders.Enable = True
        for r in range(1, 6):
            for c in range(1, 6):
                table.Cell(r, c).Range.Text = f"Cell {r},{c}"
                table.Cell(r, c).Range.Font.Color = (r * c * 1000) % 16777215
                table.Cell(r, c).Shading.BackgroundPatternColor = (255255 - r*c*500) % 16777215
        selection.EndKey(Unit=6)
        selection.TypeParagraph()
        
    selection.InsertBreak(Type=7)
    
    # 5. Lists (Bullets and Numbers)
    print("Adding Lists...")
    selection.TypeText("=== 5. LISTS ===\n")
    selection.Range.ListFormat.ApplyBulletDefault()
    for i in range(5):
        selection.TypeText(f"Bullet item {i}\n")
    selection.Range.ListFormat.RemoveNumbers()
    selection.TypeParagraph()
    
    selection.Range.ListFormat.ApplyNumberDefault()
    for i in range(5):
        selection.TypeText(f"Numbered item {i}\n")
    selection.Range.ListFormat.RemoveNumbers()
    
    selection.InsertBreak(Type=7)

    # 6. Text formatting & Styles
    print("Adding Text Formatting...")
    selection.TypeText("=== 6. TEXT FORMATTING & STYLES ===\n")
    fonts = ["Arial", "Times New Roman", "Courier New", "Comic Sans MS", "Calibri", "Impact", "Georgia", "Verdana", "Trebuchet MS"]
    for i, font in enumerate(fonts):
        selection.Font.Name = font
        selection.Font.Size = 10 + i*2
        selection.Font.Color = (100000 * i) % 16777215
        selection.Font.Bold = i % 2
        selection.Font.Italic = (i+1) % 2
        selection.Font.Underline = i % 3
        selection.Font.StrikeThrough = i % 4 == 0
        selection.ParagraphFormat.Alignment = i % 4 # Left, Center, Right, Justify
        selection.TypeText(f"Text formatted with {font}, size {10+i*2}, alignment {i%4}\n")
        
    selection.Font.Reset()
    selection.ParagraphFormat.Reset()
    selection.InsertBreak(Type=7)
    
    # 7. Mass Generation (500+ pages)
    print("Generating pages up to 500+...")
    selection.TypeText("=== 7. MASS TEXT GENERATION ===\n")
    
    # Create a dense paragraph with many words, varying formats
    para_text = "XML generation text content for 500 pages document parsing testing. " * 30
    
    # Rather than loop 500 times, let's inject a lot of text, copy it, and paste it repeatedly
    # Insert 5 pages worth of text
    for i in range(100):
        selection.TypeText(f"Para {i}: " + para_text + "\n")
        
    word.ScreenUpdating = False # speed up
    
    for p in range(1, 505):
        selection.TypeText(f"\n--- PAGE CONTENT BLOCK {p} ---\n")
        selection.TypeText(para_text + "\n")
        selection.TypeText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. " * 20 + "\n")
        
        # Add a random shape every 10 pages so XML is rich across the doc
        if p % 10 == 0:
             try:
                 shape = doc.Shapes.AddShape(Type=1, Left=100, Top=100, Width=50, Height=50, Anchor=selection.Range)
                 shape.Fill.ForeColor.RGB = 255
                 selection.EndKey(Unit=6)
             except:
                 pass
                 
        selection.InsertBreak(Type=7) # Page break
        
        if p % 50 == 0:
            print(f"Generated {p} pages...")

    word.ScreenUpdating = True
    
    print(f"Saving file at: {filepath}")
    try:
        doc.SaveAs(filepath)
    except Exception as e:
        print("SaveAs failed, trying SaveAs2", e)
        doc.SaveAs2(filepath)
    print("Done!")
    # Leave Word open so user can inspect it, but document is saved.
    
if __name__ == '__main__':
    fp = os.path.abspath("Full_Features_Document.docx")
    # if file exists, delete it first to avoid prompt
    if os.path.exists(fp):
        os.remove(fp)
    create_complex_document(fp)
