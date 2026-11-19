import pefile, sys, os

path = sys.argv[1]
out = sys.argv[2]
p = pefile.PE(path)
found = False
for r in getattr(p, "DIRECTORY_ENTRY_RESOURCE", []).entries:
    name = r.name.string.decode() if r.name else str(r.id)
    print("type:", name)
    if name != "TYPELIB":
        continue
    for i in r.directory.entries:
        for l in i.directory.entries:
            data = p.get_data(l.data.struct.OffsetToData, l.data.struct.Size)
            print("  lang", i.id, "size", len(data))
            open(out, "wb").write(data)
            found = True
print("extracted" if found else "no typelib found")
