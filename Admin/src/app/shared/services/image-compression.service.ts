import { Injectable } from '@angular/core';
import heic2any from 'heic2any';

export interface ImageCompressionOptions {
  maxSize?: number;
  quality?: number;
}

/**
 * 共用圖檔壓縮服務：
 * - PDF → 直接回傳原檔（不壓縮）
 * - HEIC/HEIF → 先 heic2any 轉 JPEG，再走 Canvas 等比縮放
 * - 其餘圖檔（JPEG/PNG/WEBP）→ Canvas 等比縮放至 maxSize × maxSize 範圍內，輸出 JPEG
 */
@Injectable({ providedIn: 'root' })
export class ImageCompressionService {

  /**
   * @param file    待處理的原始檔案
   * @param opts    maxSize（預設 800）：長邊上限（px）；quality（預設 0.85）：JPEG 品質
   * @returns       壓縮後的 File（PDF 直接回傳原檔）
   */
  async compress(file: File, opts?: ImageCompressionOptions): Promise<File> {
    const maxSize = opts?.maxSize ?? 800;
    const quality = opts?.quality ?? 0.85;

    // PDF 不壓縮，直接回傳
    if (file.type === 'application/pdf' || /\.pdf$/i.test(file.name)) {
      return file;
    }

    let workingBlob: Blob = file;

    // HEIC/HEIF → JPEG（iOS 預設拍照格式）
    if (/\.(heic|heif)$/i.test(file.name) || file.type === 'image/heic' || file.type === 'image/heif') {
      workingBlob = await heic2any({ blob: file, toType: 'image/jpeg', quality }) as Blob;
    }

    // 讀取 DataURL
    const dataUrl = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload  = () => resolve(reader.result as string);
      reader.onerror = () => reject(reader.error);
      reader.readAsDataURL(workingBlob);
    });

    // 建立 HTMLImageElement
    const img = await new Promise<HTMLImageElement>((resolve, reject) => {
      const i = new Image();
      i.onload  = () => resolve(i);
      i.onerror = () => reject(new Error('Failed to load image'));
      i.src = dataUrl;
    });

    // 等比縮放至 maxSize × maxSize 範圍內（不放大）
    const ratio = Math.min(maxSize / img.width, maxSize / img.height, 1);
    const w = Math.round(img.width  * ratio);
    const h = Math.round(img.height * ratio);

    const canvas = document.createElement('canvas');
    canvas.width  = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d');
    if (!ctx) throw new Error('Canvas context unavailable');
    ctx.drawImage(img, 0, 0, w, h);

    const compressed: Blob = await new Promise((resolve, reject) =>
      canvas.toBlob(
        b => b ? resolve(b) : reject(new Error('toBlob returned null')),
        'image/jpeg',
        quality,
      )
    );

    const baseName = file.name.replace(/\.[^.]+$/, '');
    return new File([compressed], `${baseName}.jpg`, { type: 'image/jpeg' });
  }
}
