import io, sys, zipfile, xml.etree.ElementTree as ET
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

docx_path = r'C:\Users\Danh\Downloads\Bài 1.ND GHI.docx'

def to_roman(n):
    val = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1]
    syb = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"]
    roman = ""
    for i in range(len(val)):
        while n >= val[i]:
            roman += syb[i]
            n -= val[i]
    return roman

def to_letter(n):
    # 1 -> a, 2 -> b, ..., 26 -> z, 27 -> aa
    result = ""
    while n > 0:
        n -= 1
        result = chr(97 + (n % 26)) + result
        n //= 26
    return result

with zipfile.ZipFile(docx_path, 'r') as z:
    num_tree = ET.fromstring(z.read('word/numbering.xml'))
    doc_tree = ET.fromstring(z.read('word/document.xml'))

namespaces = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}

# 1. Map numId -> abstractNumId
num_to_abstract = {}
for num in num_tree.findall('w:num', namespaces):
    numId = num.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}numId')
    ab = num.find('w:abstractNumId', namespaces)
    if ab is not None:
        num_to_abstract[numId] = ab.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val')

# 2. Map abstractNumId -> dict of ilvl -> info
abstract_nums = {}
for ab in num_tree.findall('w:abstractNum', namespaces):
    abId = ab.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}abstractNumId')
    lvls = {}
    for lvl in ab.findall('w:lvl', namespaces):
        ilvl = lvl.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}ilvl')
        start = lvl.find('w:start', namespaces)
        numFmt = lvl.find('w:numFmt', namespaces)
        lvlText = lvl.find('w:lvlText', namespaces)
        pPr = lvl.find('w:pPr', namespaces)
        ind = pPr.find('w:ind', namespaces) if pPr is not None else None
        
        start_val = int(start.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val')) if start is not None else 1
        numFmt_val = numFmt.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if numFmt is not None else 'decimal'
        lvlText_val = lvlText.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if lvlText is not None else ''
        
        left_val = int(ind.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}left')) if ind is not None and ind.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}left') else 0
        hanging_val = int(ind.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}hanging')) if ind is not None and ind.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}hanging') else 0
        firstLine_val = int(ind.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}firstLine')) if ind is not None and ind.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}firstLine') else 0
        
        lvls[int(ilvl)] = {
            'start': start_val,
            'numFmt': numFmt_val,
            'lvlText': lvlText_val,
            'left': left_val,
            'hanging': hanging_val,
            'firstLine': firstLine_val
        }
    abstract_nums[abId] = lvls

# 3. Simulate tracking counters through document.xml
counters = {} # (numId, ilvl) -> current number

body = doc_tree.find('w:body', namespaces)
for i, p in enumerate(body.findall('w:p', namespaces)[:30]):
    text = ''.join(p.itertext())[:45].replace('\n', ' ')
    pPr = p.find('w:pPr', namespaces)
    if pPr is not None:
        numPr = pPr.find('w:numPr', namespaces)
        if numPr is not None:
            ilvl_elem = numPr.find('w:ilvl', namespaces)
            numId_elem = numPr.find('w:numId', namespaces)
            if ilvl_elem is not None and numId_elem is not None:
                ilvl = int(ilvl_elem.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val'))
                numId = numId_elem.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val')
                abId = num_to_abstract.get(numId)
                if abId in abstract_nums and ilvl in abstract_nums[abId]:
                    lvl_info = abstract_nums[abId][ilvl]
                    # Reset lower levels for this numId
                    for sub_lvl in range(ilvl + 1, 9):
                        if (numId, sub_lvl) in counters:
                            del counters[(numId, sub_lvl)]
                    
                    key = (numId, ilvl)
                    if key not in counters:
                        counters[key] = lvl_info['start']
                    
                    cur_val = counters[key]
                    counters[key] += 1
                    
                    fmt = lvl_info['numFmt']
                    template = lvl_info['lvlText']
                    
                    if fmt == 'upperRoman':
                        num_str = to_roman(cur_val)
                    elif fmt == 'lowerRoman':
                        num_str = to_roman(cur_val).lower()
                    elif fmt == 'lowerLetter':
                        num_str = to_letter(cur_val)
                    elif fmt == 'upperLetter':
                        num_str = to_letter(cur_val).upper()
                    elif fmt == 'bullet':
                        num_str = template
                    else: # decimal
                        num_str = str(cur_val)
                    
                    # Format with template like %1. or %2.
                    placeholder = f"%{ilvl+1}"
                    if placeholder in template:
                        final_marker = template.replace(placeholder, num_str)
                    elif fmt == 'bullet':
                        final_marker = template
                    else:
                        final_marker = num_str + "."
                    
                    print(f'P{i}: [{final_marker}] {text}')
                    continue
    if len(text.strip()) > 0:
        print(f'P{i}: (no num) {text}')
