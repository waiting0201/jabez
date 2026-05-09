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
  note?: string;
  isActive: boolean;
  usageCount: number;
  createdAt: Date;
}
