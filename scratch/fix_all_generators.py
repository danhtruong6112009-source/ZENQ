import glob, re

for f in sorted(glob.glob('scratch/cat*.py')):
    with open(f, 'r', encoding='utf-8') as fp:
        content = fp.read()
    
    # 1. Fix truncated names like Gtar, GegularPoly, Goly, Gllipse
    content = content.replace("Gtar", "GStar")
    content = content.replace("GegularPoly", "GRegularPoly")
    content = content.replace("Goly", "GPoly")
    content = content.replace("Gllipse", "GEllipse")
    
    content = content.replace("Wtar", "WStar")
    content = content.replace("WegularPoly", "WRegularPoly")
    content = content.replace("Woly", "WPoly")
    content = content.replace("Wllipse", "WEllipse")

    lines = content.splitlines(keepends=True)
    new_lines = []
    for line in lines:
        # Check AddItem pattern:
        # AddItem(cat, "id", "Name", <GDI_GEN>, <WPF_GEN>);
        # If WPF_GEN starts with (w, h) => G..., fix to W...
        # If GDI_GEN starts with (w, h) => W..., fix to G...
        
        # Let's check if line has AddItem
        if "AddItem(" in line:
            # Check if arg 5 (the last lambda before ');') has GPoly/GStar/etc.
            # e.g. (w, h) => GPoly( -> (w, h) => WPoly(
            # First, find all (w, h) => G... and (w, h) => W...
            parts = re.split(r'(\(w,\s*h\)\s*=>\s*[A-Za-z0-9_]+)', line)
            if len(parts) >= 5:
                # parts[1] is the first lambda: should be G*
                # parts[3] is the second lambda: should be W*
                lam1 = parts[1]
                lam2 = parts[3]
                
                # If lam1 starts with W, make it G
                m1 = re.search(r'(\(w,\s*h\)\s*=>\s*)W([A-Za-z0-9_]+)', lam1)
                if m1:
                    parts[1] = f"{m1.group(1)}G{m1.group(2)}"
                
                # If lam2 starts with G, make it W
                m2 = re.search(r'(\(w,\s*h\)\s*=>\s*)G([A-Za-z0-9_]+)', lam2)
                if m2:
                    parts[3] = f"{m2.group(1)}W{m2.group(2)}"
                
                line = "".join(parts)

        new_lines.append(line)
    
    with open(f, 'w', encoding='utf-8') as fp:
        fp.writelines(new_lines)

print("All cat*.py fixed!")
