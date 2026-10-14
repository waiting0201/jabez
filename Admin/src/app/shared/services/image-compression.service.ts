import { Injectable } from '@angular/core';
import { convertHeicToJpeg, isHeicFile } from '../utils/heic';

export interface ImageCompressionOptions {
  maxSize?: number;
  quality?: number;
}

/**
 * 共用圖檔壓縮服務：
 * - PDF → 直接回傳原檔（不壓縮）
 * - HEIC/HEIF → 先轉 JPEG（shared/utils/heic，轉不了會丟例外），再走 Canvas 等比縮放
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
    if (await isHeicFile(file)) {
      workingBlob = await convertHeicToJpeg(file, quality);
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

  /**
   * 將圖檔順時針旋轉 90°，輸出 JPEG File（供頭像「旋轉 90°」使用）。
   * 長邊上限同 compress 的預設行為不再縮放——旋轉前通常已壓縮過；原檔若是 HEIC 會先轉 JPEG。
   */
  async rotate90(file: Blob, fileName = 'rotated.jpg', quality = 0.9): Promise<File> {
    let workingBlob: Blob = file;
    if (file instanceof File && await isHeicFile(file)) {
      workingBlob = await convertHeicToJpeg(file, quality);
    }
    const url = URL.createObjectURL(workingBlob);
    try {
      const img = await new Promise<HTMLImageElement>((resolve, reject) => {
        const i = new Image();
        i.onload  = () => resolve(i);
        i.onerror = () => reject(new Error('Failed to load image'));
        i.src = url;
      });
      const canvas = document.createElement('canvas');
      canvas.width  = img.naturalHeight;
      canvas.height = img.naturalWidth;
      const ctx = canvas.getContext('2d');
      if (!ctx) throw new Error('Canvas context unavailable');
      // 順時針 90°：原點移到右上角再旋轉
      ctx.translate(canvas.width, 0);
      ctx.rotate(Math.PI / 2);
      ctx.drawImage(img, 0, 0);
      const blob: Blob = await new Promise((resolve, reject) =>
        canvas.toBlob(b => b ? resolve(b) : reject(new Error('toBlob returned null')), 'image/jpeg', quality));
      const baseName = fileName.replace(/\.[^.]+$/, '');
      return new File([blob], `${baseName}.jpg`, {type: 'image/jpeg'});
    } finally {
      URL.revokeObjectURL(url);
    }
  }
}
