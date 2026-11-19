import re
import sys

import pymupdf

url_pdf = sys.argv[1]
doc = pymupdf.open(url_pdf)
needle = sys.argv[2] if len(sys.argv) > 2 else "GetDeviceStatus"
found = 0
for page_no, page in enumerate(doc):
    text = page.get_text()
    if needle in text:
        found += 1
        print(f"===== page {page_no + 1} =====")
        idx = text.find(needle)
        start = max(0, idx - 400)
        print(text[start:idx + 1600])
        if found >= 3:
            break
print("pages with needle:", found)
