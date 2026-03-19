/** 輕量級職稱資料（供下拉選單用） */
export interface JobTitleLookup {
  id: number;
  name: string;
}

export interface JobTitle {
  id: number;
  name: string;
  level: number;
  description?: string;
  employeeCount: number;
  createdAt: Date;
}
