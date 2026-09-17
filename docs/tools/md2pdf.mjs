#!/usr/bin/env node
// 把 docs/ 底下的 markdown 轉成客戶可讀的 PDF（樣式比照〈四週彈性工時制度 — 功能規格書〉客戶確認版）。
//
//   node docs/tools/md2pdf.mjs <來源.md> <輸出.pdf> [PDF 標題]
//
// 例：
//   node docs/tools/md2pdf.mjs docs/business/flexible-work-hours-client.md \
//        output/四週彈性工時制度_功能規格書_v1.4_20260917.pdf "四週彈性工時制度 — 功能規格書"
//
// ⚠️ 產出一律放 repo 根目錄的 `output/`（已 gitignore，見 .gitignore「產出的 PDF 一律放 output/」），
//    不要丟 ~/Downloads —— 歷次版本都留在 output/，比對與回溯才找得到。
//
// 零依賴：markdown 轉換走 `npx --yes marked`（首次執行會下載，之後走 npx 快取），
// 排版走系統已安裝的 Chrome headless。CSS 內嵌於本檔，改樣式只改這一處。
// ⚠️ 客戶版與技術版是兩份文件，這支只該拿來產「客戶版」——
//    技術版含檔名 / 符號名 / 法條罰鍰，不對外。

import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const [src, out, title = 'Document'] = process.argv.slice(2);
if (!src || !out) {
  console.error('用法：node docs/tools/md2pdf.mjs <來源.md> <輸出.pdf> [PDF 標題]');
  process.exit(1);
}

const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';

const CSS = `@page { size: A4; margin: 18mm 14mm 16mm 14mm; }
* { box-sizing: border-box; }
body {
  font-family: "Noto Sans TC","PingFang TC","Heiti TC","Microsoft JhengHei",sans-serif;
  font-size: 10.5pt; line-height: 1.75; color: #2b2b2b; margin: 0;
  -webkit-font-smoothing: antialiased;
}
h1 { font-size: 17pt; color: #1a56a0; border-bottom: 3px solid #1a56a0;
     padding-bottom: 8px; margin: 26px 0 16px; letter-spacing: .5px; }
h1:first-child { margin-top: 0; }
h2 { font-size: 13pt; color: #1a56a0; margin: 26px 0 12px; padding-left: 10px;
     border-left: 5px solid #1a56a0; line-height: 1.4; }
h3 { font-size: 11.5pt; color: #2b2b2b; margin: 20px 0 8px; font-weight: 700; }
p { margin: 9px 0; }
strong { color: #111; font-weight: 700; }
ul, ol { margin: 9px 0; padding-left: 22px; }
li { margin: 4px 0; }
table { width: 100%; border-collapse: collapse; margin: 12px 0; font-size: 9.5pt;
        page-break-inside: auto; }
tr { page-break-inside: avoid; page-break-after: auto; }
thead { display: table-header-group; }
th { background: #e6eff9; color: #1a56a0; font-weight: 700; text-align: left;
     padding: 7px 10px; border-bottom: 1px solid #c3d6ec; }
td { padding: 7px 10px; border-bottom: 1px solid #e2e2e2; vertical-align: top; }
blockquote { background: #fdf8e8; border-left: 4px solid #e0b64a; margin: 12px 0;
             padding: 10px 14px; font-size: 9.5pt; color: #4a4437; }
blockquote p { margin: 5px 0; }
blockquote table { font-size: 9pt; background: #fff; }
code { font-family: "SF Mono",Menlo,monospace; font-size: 9pt;
       background: #f1f3f5; padding: 1px 4px; border-radius: 3px; color: #b5385a; }
hr { border: 0; border-top: 1px solid #dcdcdc; margin: 24px 0; }
em { color: #666; }
del { color: #999; }
th { white-space: nowrap; }`;

const body = execFileSync('npx', ['--yes', 'marked@15', '--gfm', '-i', src], {
  encoding: 'utf8', maxBuffer: 32 * 1024 * 1024,
});

const work = mkdtempSync(join(tmpdir(), 'md2pdf-'));
const htmlPath = join(work, 'doc.html');
writeFileSync(htmlPath, `<!doctype html><html lang="zh-Hant"><head><meta charset="utf-8">
<title>${title}</title><style>${CSS}</style></head><body>${body}</body></html>`);

execFileSync(CHROME, [
  '--headless', '--disable-gpu', '--no-sandbox',
  '--no-pdf-header-footer',          // 不要 Chrome 預設的日期 / file:// 網址 / 頁碼
  `--print-to-pdf=${out}`, `file://${htmlPath}`,
], { stdio: ['ignore', 'ignore', 'ignore'] });

console.log('→', out);
