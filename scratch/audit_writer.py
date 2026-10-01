import sys, re
sys.stdout.reconfigure(encoding='utf-8')

with open('src/ui/RibbonBar.cs', 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

def inspect_range(start, end, title):
    print(f'=== {title} ===')
    for i in range(start, min(end, len(lines))):
        line = lines[i].strip()
        if line.startswith('//') and any(k in line.upper() for k in ['===', 'NHOM', 'GROUP', 'TAB']):
            print(f'  {line}')
        matches = re.findall(r'"([^"]{2,30})"', line)
        for s in matches:
            if 'DrawString' in line or 'DrawInteractive' in line or 'DrawRibbon' in line:
                if not any(ign in s for ign in ['Segoe UI', 'Arial', 'Calibri', 'Times New Roman']):
                    print(f'    [{i+1}] {s}')

inspect_range(3053, 3333, 'TAB TRANG DAU (HOME)')
inspect_range(3333, 3804, 'TAB CHEN (INSERT)')
inspect_range(5209, 5479, 'TAB THIET KE (DESIGN)')
inspect_range(5479, 6579, 'TAB BO TRI (LAYOUT)')
inspect_range(6579, 6903, 'TAB THAM CHIEU (REFERENCES)')
inspect_range(6903, 7305, 'TAB HIEN THI (VIEW)')

print('\n=== CLICK HANDLERS ===')
with open('src/ui/RibbonBar.cs', 'r', encoding='utf-8', errors='ignore') as f:
    for i, line in enumerate(f):
        if 'ActiveRibbonTab ==' in line and i > 12900:
            print(f'Line {i+1}: {line.strip()[:100]}')

