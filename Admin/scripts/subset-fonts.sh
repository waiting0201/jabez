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

echo ""
echo "Done! Font sizes:"
ls -lh "$FONT_DIR"/NotoSansTC-*.ttf
