import pefile, sys

path = sys.argv[1]
p = pefile.PE(path)
print("machine", hex(p.FILE_HEADER.Machine))
if hasattr(p, "DIRECTORY_ENTRY_EXPORT"):
    print("exports:")
    for e in p.DIRECTORY_ENTRY_EXPORT.symbols:
        print("  ", e.name.decode() if e.name else e.ordinal)
else:
    print("exports: none")
print("imports:")
for d in getattr(p, "DIRECTORY_ENTRY_IMPORT", []):
    print("  ", d.dll.decode())
print("resource types:")
for r in getattr(p, "DIRECTORY_ENTRY_RESOURCE", []).entries if hasattr(p, "DIRECTORY_ENTRY_RESOURCE") else []:
    print("  ", r.id, r.name)
