import {ITEM_CATEGORIES} from '../../features/admin/advance-requests/models/advance-request.model';

export const UNCATEGORIZED_LABEL = '未分類';

/** 只需要分類與總價即可加總（預支 / 沖銷 / 表單值皆可傳入） */
export interface CategorizedAmount {
  category?: string | null;
  totalPrice?: number | string | null;
}

/**
 * 依「分類」加總 totalPrice。
 * 排序：依 ITEM_CATEGORIES 既有順序，未知分類排其後（出現順序），空白分類併為「未分類」置最末。
 */
export function sumByCategory(items: readonly CategorizedAmount[] | null | undefined): Map<string, number> {
  const raw = new Map<string, number>();
  for (const it of items ?? []) {
    const key = (it.category ?? '').toString().trim() || UNCATEGORIZED_LABEL;
    raw.set(key, (raw.get(key) ?? 0) + (+(it.totalPrice ?? 0) || 0));
  }
  const known = (ITEM_CATEGORIES as readonly string[]).filter(c => raw.has(c));
  const unknown = [...raw.keys()].filter(k => !(ITEM_CATEGORIES as readonly string[]).includes(k) && k !== UNCATEGORIZED_LABEL);
  const tail = raw.has(UNCATEGORIZED_LABEL) ? [UNCATEGORIZED_LABEL] : [];
  const out = new Map<string, number>();
  for (const k of [...known, ...unknown, ...tail]) out.set(k, raw.get(k)!);
  return out;
}
