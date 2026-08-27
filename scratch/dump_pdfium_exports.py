import pefile

pe = pefile.PE("pdfium.dll")
print("Exported symbols count:", len(pe.DIRECTORY_ENTRY_EXPORT.symbols))
for exp in pe.DIRECTORY_ENTRY_EXPORT.symbols:
    if exp.name:
        name = exp.name.decode('utf-8')
        if "Text" in name or "Font" in name:
            print(name)
