/** 輕量級廠商資料（供下拉選單用） */
export interface VendorLookup {
  id: number;
  name: string;
  taxId?: string;
  idNumber?: string;
}

export interface Vendor {
  id: number;
  name: string;
  taxId?: string;
  idNumber?: string;
  phone?: string;
  contactPerson?: string;
  address?: string;
  /** 匯款戶名（實際受款人，常與 name 不同） */
  bankAccountName?: string;
  /** 匯款銀行（含分行） */
  bankName?: string;
  /** 銀行代號（農漁會為 xxx-xxxx） */
  bankCode?: string;
  /** 銀行帳號 */
  bankAccount?: string;
  bankBookImageUrl?: string;
  idCardFrontUrl?: string;
  idCardBackUrl?: string;
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
