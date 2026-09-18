#!/bin/bash
# Font subsetting script for NotoSansTC
# Requires: pip install fonttools brotli
#
# Usage: cd Admin && bash scripts/subset-fonts.sh
# Or:    npm run subset-fonts

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
FONT_DIR="$SCRIPT_DIR/../src/assets/fonts"
CHARSET="$SCRIPT_DIR/tc-charset.txt"
PDF_CORE="$SCRIPT_DIR/../src/app/shared/services/pdf-core.service.ts"

# Regenerate charset from source
echo "Generating character set from source files..."
python3 "$SCRIPT_DIR/generate-charset.py"

echo "Subsetting NotoSansTC-Regular.ttf..."
python3 -m fontTools.subset \
  "$FONT_DIR/NotoSansTC-Regular.ttf" \
  --text-file="$CHARSET" \
  --output-file="$FONT_DIR/NotoSansTC-Regular.subset.ttf" \
  --layout-features='*' \
  --no-hinting

echo "Subsetting NotoSansTC-Bold.ttf..."
python3 -m fontTools.subset \
  "$FONT_DIR/NotoSansTC-Bold.ttf" \
  --text-file="$CHARSET" \
  --output-file="$FONT_DIR/NotoSansTC-Bold.subset.ttf" \
  --layout-features='*' \
  --no-hinting

# 蓋版本戳：字型檔名不帶 content hash（Angular assets 原樣複製），
# 不換 URL 的話瀏覽器會沿用舊字型，而 jsPDF 缺字是「整個字消失、不報錯」。
# 這裡把兩個 subset 的內容短雜湊寫回 pdf-core.service.ts 的 FONT_SUBSET_VERSION，
# 由該常數以 ?v= 附在字型 URL 後，換字集必然換 URL。
echo ""
echo "Stamping FONT_SUBSET_VERSION..."
VERSION=$(cat "$FONT_DIR/NotoSansTC-Regular.subset.ttf" "$FONT_DIR/NotoSansTC-Bold.subset.ttf" \
  | { md5sum 2>/dev/null || md5; } | awk '{print $1}' | cut -c1-8)
perl -pi -e "s/^const FONT_SUBSET_VERSION = '[0-9a-f]*';/const FONT_SUBSET_VERSION = '$VERSION';/" "$PDF_CORE"
grep -q "const FONT_SUBSET_VERSION = '$VERSION';" "$PDF_CORE" \
  || { echo "ERROR: 無法寫入 FONT_SUBSET_VERSION，請檢查 $PDF_CORE"; exit 1; }
echo "  FONT_SUBSET_VERSION = $VERSION"

echo ""
echo "Done! Font sizes:"
ls -lh "$FONT_DIR"/NotoSansTC-*.ttf
echo ""
echo "記得一起進版控：*.subset.ttf、tc-charset.txt、pdf-core.service.ts"
