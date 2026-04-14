import {
  Injectable,
  environment,
  setClassMetadata,
  ɵɵdefineInjectable
} from "./chunk-7FYQHGNM.js";

// src/app/shared/services/pdf-core.service.ts
function arrayBufferToBase64(buffer) {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  const chunk = 8192;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
  }
  return btoa(binary);
}
function resolveSignatureUrl(url) {
  if (!url.startsWith("http")) {
    return `${environment.apiUrl}/${url}`;
  }
  const match = url.match(/\/signatures\/(.+)$/);
  if (match) {
    return `${environment.apiUrl}/files/signatures/${match[1]}`;
  }
  return url;
}
function fmtDT(val) {
  const d = new Date(val);
  const tz = "Asia/Taipei";
  const date = d.toLocaleDateString("zh-TW", { year: "numeric", month: "2-digit", day: "2-digit", timeZone: tz });
  const time = d.toLocaleTimeString("zh-TW", { hour: "2-digit", minute: "2-digit", hour12: false, timeZone: tz });
  return `${date} ${time}`;
}
function fmtDate(val) {
  const d = new Date(val);
  return d.toLocaleDateString("zh-TW", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    timeZone: "Asia/Taipei"
  });
}
var fmt = (n) => n.toLocaleString("zh-TW");
var CIS = {
  forest: [105, 159, 52],
  forestMid: [74, 107, 58],
  accent: [140, 115, 85],
  textPrimary: [82, 83, 88],
  textMuted: [163, 150, 133],
  bgBase: [245, 242, 237],
  bgSurface: [253, 250, 245],
  border: [221, 214, 200],
  red: [160, 64, 64]
};
var FONT_FAMILY = "NotoSansTC";
async function optimizeSignatureImage(buf, mime) {
  const maxW = 300;
  const maxH = 150;
  const blob = new Blob([buf], { type: mime });
  const url = URL.createObjectURL(blob);
  try {
    const img = await new Promise((resolve, reject) => {
      const el = new Image();
      el.onload = () => resolve(el);
      el.onerror = reject;
      el.src = url;
    });
    const scale = Math.min(1, maxW / img.width, maxH / img.height);
    const w = Math.round(img.width * scale);
    const h = Math.round(img.height * scale);
    const canvas = document.createElement("canvas");
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(img, 0, 0, w, h);
    return canvas.toDataURL("image/png");
  } finally {
    URL.revokeObjectURL(url);
  }
}
var PdfCoreService = class _PdfCoreService {
  fontCache = null;
  /** 載入字體（singleton cache，全應用只載入一次） */
  loadFonts() {
    if (!this.fontCache) {
      this.fontCache = Promise.all([
        fetch("/assets/fonts/NotoSansTC-Regular.subset.ttf").then((r) => r.arrayBuffer()),
        fetch("/assets/fonts/NotoSansTC-Bold.subset.ttf").then((r) => r.arrayBuffer())
      ]).then(([regular, bold]) => ({
        regular: arrayBufferToBase64(regular),
        bold: arrayBufferToBase64(bold)
      }));
    }
    return this.fontCache;
  }
  /** 註冊字體到 jsPDF 文件 */
  registerFonts(doc, fonts) {
    doc.addFileToVFS("NotoSansTC-Regular.ttf", fonts.regular);
    doc.addFileToVFS("NotoSansTC-Bold.ttf", fonts.bold);
    doc.addFont("NotoSansTC-Regular.ttf", FONT_FAMILY, "normal");
    doc.addFont("NotoSansTC-Bold.ttf", FONT_FAMILY, "bold");
  }
  /** 預載所有簽名欄圖片（含壓縮），回傳 URL → base64 data URI 的 Map */
  async loadSignatureImages(blocks) {
    const urls = blocks.map((b) => b.signatureUrl).filter((u) => !!u);
    const unique = [...new Set(urls)];
    const map = /* @__PURE__ */ new Map();
    await Promise.all(unique.map(async (url) => {
      try {
        const fetchUrl = resolveSignatureUrl(url);
        const resp = await fetch(fetchUrl);
        const buf = await resp.arrayBuffer();
        const mime = resp.headers.get("content-type") || "image/png";
        const dataUri = await optimizeSignatureImage(buf, mime);
        map.set(url, dataUri);
      } catch {
      }
    }));
    return map;
  }
  /** 繪製簽名欄（含簽名圖片和日期），各 PDF service 可透過 opts 微調尺寸 */
  drawSignatureBlock(doc, mx, pw, cw, y, blocks, sigImageMap, opts) {
    const F = FONT_FAMILY;
    const gap = opts?.gap ?? 3;
    const labelSize = opts?.labelSize ?? 8.5;
    const maxH = opts?.maxH ?? 10;
    const padding = opts?.padding ?? 3;
    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.3);
    doc.line(mx, y, pw - mx, y);
    y += gap === 4 ? 6 : 5;
    const blockW = (cw - gap * (blocks.length - 1)) / blocks.length;
    for (let i = 0; i < blocks.length; i++) {
      const bx = mx + i * (blockW + gap);
      const block = blocks[i];
      doc.setFont(F, "bold");
      doc.setFontSize(labelSize);
      doc.setTextColor(...CIS.textPrimary);
      doc.text(block.label, bx + blockW / 2, y, { align: "center" });
      const lineY = y + (gap === 4 ? 16 : 14);
      doc.setDrawColor(...CIS.border);
      doc.setLineWidth(0.2);
      doc.line(bx + 2, lineY, bx + blockW - 2, lineY);
      if (block.signatureUrl && sigImageMap.has(block.signatureUrl)) {
        const sigData = sigImageMap.get(block.signatureUrl);
        const imgMaxW = blockW - padding * 2;
        try {
          const imgProps = doc.getImageProperties(sigData);
          const ratio = Math.min(imgMaxW / imgProps.width, maxH / imgProps.height);
          const imgW = imgProps.width * ratio;
          const imgH = imgProps.height * ratio;
          const imgX = bx + (blockW - imgW) / 2;
          const imgY = lineY - imgH - 1;
          doc.addImage(sigData, imgX, imgY, imgW, imgH);
        } catch {
        }
      }
      if (block.date) {
        doc.setFont(F, "normal");
        doc.setFontSize(6.5);
        doc.setTextColor(...CIS.textMuted);
        doc.text(block.date, bx + blockW / 2, lineY + 5, { align: "center" });
      }
    }
  }
  static \u0275fac = function PdfCoreService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PdfCoreService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _PdfCoreService, factory: _PdfCoreService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PdfCoreService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  fmtDT,
  fmtDate,
  fmt,
  CIS,
  FONT_FAMILY,
  PdfCoreService
};
//# sourceMappingURL=chunk-FDTOIMRI.js.map
