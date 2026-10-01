import glob, re

for f in sorted(glob.glob('scratch/cat*.py')):
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    
    modified = False
    new_lines = []
    for line in lines:
        if "AddItem(" not in line:
            new_lines.append(line)
            continue
        
        orig_line = line
        # Replace broken names first
        line = line.replace("Gtar", "GStar")
        line = line.replace("GegularPoly", "GRegularPoly")
        line = line.replace("Goly", "GPoly")
        line = line.replace("Gllipse", "GEllipse")
        line = line.replace("Wtar", "WStar")
        line = line.replace("WegularPoly", "WRegularPoly")
        line = line.replace("Woly", "WPoly")
        line = line.replace("Wllipse", "WEllipse")

        # In the last argument before );
        # If it contains (w, h) => G...
        # We can find the last occurrence of (w, h) => G
        last_g = line.rfind("(w, h) => G")
        if last_g != -1:
            # Check if this G is indeed in the wpf argument position
            # Count commas or check if it's the second (w, h) =>
            first_lam = line.find("(w, h) => ")
            if last_g > first_lam:
                # The second lambda is G! Replace it with W
                # Replace '=> G' with '=> W' at last_g
                line = line[:last_g] + "(w, h) => W" + line[last_g + len("(w, h) => G"):]

        # Also check if the first lambda is (w, h) => W
        first_w = line.find("(w, h) => W")
        last_w = line.rfind("(w, h) => W")
        if first_w != -1 and first_w != last_w:
            # First lambda is W! Replace it with G
            line = line[:first_w] + "(w, h) => G" + line[first_w + len("(w, h) => W"):]

        if line != orig_line:
            modified = True
        new_lines.append(line)
    
    if modified:
        with open(f, 'w', encoding='utf-8') as fp:
            fp.writelines(new_lines)
        print(f"Fixed {f}")

print("Batch fix complete.")
