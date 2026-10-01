import os, re, sys

with open('scratch/filtered_matches.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

out = []
for line in lines:
    line_clean = line.strip()
    if re.search(r'"[^"]*genoffice', line_clean, re.I):
        out.append(line_clean)

with open('scratch/string_matches.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(out))

print(f'Total string literal matches: {len(out)}')
