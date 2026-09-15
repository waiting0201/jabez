#!/usr/bin/env python3
"""
Generate tc-charset.txt for font subsetting.
Includes: ASCII printable, CJK punctuation, Big5 Level 1+2 (含姓名罕用字),
currency/math symbols, and all Chinese chars found in PDF source files.
"""
import os
import re
import struct

chars = set()

# 1. ASCII printable (U+0020 - U+007E)
for c in range(0x0020, 0x007F):
    chars.add(chr(c))

# 2. CJK punctuation and symbols
for c in range(0x3000, 0x3040):  # CJK Symbols and Punctuation
    chars.add(chr(c))
for c in range(0xFF00, 0xFF61):  # Fullwidth ASCII variants
    chars.add(chr(c))
for c in range(0xFE30, 0xFE50):  # CJK Compatibility Forms
    chars.add(chr(c))

# 3. Common symbols
extra = '\u00A5\u00B7\u2013\u2014\u2018\u2019\u201C\u201D\u2026\u2030\u20AC\u2103\u2109\u2190\u2191\u2192\u2193\u2460\u2461\u2462\u2463\u2464\u2465\u2466\u2467\u2468\u2469\u25A0\u25A1\u25B2\u25B3\u25C6\u25CB\u25CF\u2605\u2606\u2640\u2642\u00D7\u00F7\u2260\u2264\u2265\u221A\u00B1\u2248\u00AE\u00A9\u2122'
for c in extra:
    chars.add(c)

# 4. Big5 Level 1 + Level 2 characters (~13,000 Traditional Chinese chars)
# Level 1: A440-C67E（常用字）
# Level 2: C940-F9D5（次常用字）—— 姓名用字（如「闓」F16D）多落在此區，
#          只收 Level 1 會讓 PDF 的員工姓名**靜默缺字**（整個字不印，不是印方框），
#          從畫面上看不出來，故一律連 Level 2 一起收。
for lead in list(range(0xA4, 0xC7)) + list(range(0xC9, 0xFA)):
    for trail in list(range(0x40, 0x7F)) + list(range(0xA1, 0xFF)):
        try:
            b = struct.pack('BB', lead, trail)
            c = b.decode('big5')
            chars.add(c)
        except (UnicodeDecodeError, struct.error):
            pass

# 5. Scan all PDF service TS files for Chinese characters
base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src_dir = os.path.join(base_dir, 'src')
cjk_pattern = re.compile(r'[\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff]')

for root, dirs, files in os.walk(src_dir):
    for f in files:
        if f.endswith('.ts') or f.endswith('.html'):
            filepath = os.path.join(root, f)
            try:
                with open(filepath, 'r', encoding='utf-8') as fh:
                    content = fh.read()
                    for m in cjk_pattern.finditer(content):
                        chars.add(m.group())
            except (UnicodeDecodeError, FileNotFoundError):
                pass

# Sort and write
sorted_chars = sorted(chars)
output_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'tc-charset.txt')
with open(output_path, 'w', encoding='utf-8') as f:
    f.write(''.join(sorted_chars))

print(f"Generated {len(sorted_chars)} unique characters to tc-charset.txt")
