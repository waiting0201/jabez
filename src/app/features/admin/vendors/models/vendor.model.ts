/** 輕量級廠商資料（供下拉選單用） */
export interface VendorLookup {
  id: number;
  name: string;
  taxId?: string;
}

export interface Vendor {
  id: number;
  name: string;
  taxId?: string;
  phone?: string;
  contactPerson?: string;
  address?: string;
  bankAccount?: string;
  bankBookImageUrl?: string;
  note?: string;
  isActive: boolean;
  usageCount: number;
  createdAt: Date;
}

/** GCIS 統編查詢回應（廠商名稱、地址、負責人） */
export interface VendorTaxIdLookup {
  taxId: string;
  name: string;
  address?: string;
  contactPerson?: string;
}
