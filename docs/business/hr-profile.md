# 員工人事資料卡（HR Profile）

員工編輯頁 [user-form](../../Admin/src/app/features/admin/users/pages/user-form/user-form.html) 採 **3 Tab** 結構：

| Tab | 名稱 | 內容 | API |
|---|---|---|---|
| 1 | 員工基本資料 | Email / 角色 / 部門 / 職稱 / 底薪 / 伙食津貼 / 頭像 / 簽名 / **三個身份旗標 + 證明檔（原住民 / 低收入 / 殘障）** / **健保 / 勞保金額（可手動覆寫，留空 fallback 級距表）** | `POST /users` / `PATCH /users/{id}`（multipart） |
| 2 | 人事資料卡 | 員工代號 / 英文名 / 身分證號 / 性別 / 婚姻 / 出生地 / 行動電話 / 戶籍 / 通訊 / 緊急聯絡 / 銀行帳號 / 投保起日 / 扶養人 / 專長興趣 / 離職原因 / **身分證正反面影本** / **最高學歷證明** / 學歷 / 經歷 / 家庭 / 訓練 / 語言 / 職務調整 / 獎懲 / 薪資調整 | `GET / PUT /users/{id}/profile`（PUT 為 multipart） |
| 3 | 健保眷屬 | 姓名 / 關係 / 身分證號 / 出生日期；上方提示「最多計 3 口」+ 即時試算 `健保費 = baseHealth × (1 + min(N, 3))` | 同 Tab 2（共用 PUT 端點） |

## 行為規則

- **Tab 2 / 3 lazy load**：第一次切到才呼叫 `GET /users/{id}/profile`；新增模式（尚未建立員工）兩個 Tab disabled
- **整批替換**：PUT 把 9 張子表（學歷 / 經歷 / 家庭 / 訓練 / 語言 / 職務調整 / 獎懲 / 薪資調整 / 健保眷屬）整批 `ExecuteDelete` 後 INSERT 新傳入清單
- **最高學歷證明**：與身分證影本同 multipart 機制，附在 `EmployeeProfile` 主表（`HighestEducationProofUrl`，非每筆學歷各掛附件），blob 命名 `{userId}_education{ext}`，前端使用 `ImageCompressionService.compress` 壓縮（圖片 maxSize=1600 quality=0.85；PDF passthrough），1MB 上限
- **薪資自動同步**：插入完所有 `SalaryAdjustmentRecord` 後，找 `EffectiveDate <= 今日(Asia/Taipei)` 中 `EffectiveDate` 最大的那一筆，把該筆 `BaseSalary` 寫回 `User.BaseSalary`；無符合則不變
- **2 寸彩照**：hr.doc 上「黏貼 2 吋彩照」欄位**不做**（也不重用 `User.Avatar`）
- **PDF 列印**：編輯頁 header 提供「列印人事資料卡」按鈕，呼叫 [hr-profile-pdf.service.ts](../../Admin/src/app/features/admin/users/services/hr-profile-pdf.service.ts) 輸出三頁 A4 PDF（純前端 jsPDF）

## 涉及元件

| 元件 | 說明 |
|---|---|
| `EmployeeProfile` Entity | 1:1 對 User，PK=UserId，含 21 個 scalar 欄位 + 9 個導覽集合 |
| 9 張 HR 子表 Entity | 全部 PK=Guid，FK=UserId OnDelete Cascade |
| `EmployeeProfileHandler` | `GetByUserIdAsync` / `UpsertAsync`（multipart） |
| `EmployeeProfileReadService` | 一次 `QueryMultipleAsync` 讀回 EmployeeProfile + 9 張子表 |
| `image-compression.service.ts` | 共用圖檔壓縮（HEIC→JPEG / Canvas resize / JPEG quality；PDF passthrough）；avatar 用 maxSize 800、證件用 maxSize 1600 |
| `employee-profile.service.ts` | 前端 `getByUserId` / `upsert(payload, files)` multipart |
| `hr-profile-pdf.service.ts` | jsPDF + jspdf-autotable + Noto Sans TC，輸出三頁人事資料卡 |
| 5 個新 Blob 容器 | `low-income-proofs`、`disabled-proofs`、`id-cards`、`education-proofs`（皆走 `/files/{container}/{fileName}` 授權代理，需 `users:read`） |

---

## 跨業務關聯

- **健保眷屬數影響薪資公式（最多計 3 口）** → [payroll-formula.md](payroll-formula.md)
- **薪資調整紀錄同步 BaseSalary** → [payroll-formula.md](payroll-formula.md)
- **健保 / 勞保覆寫值優先級 fallback 級距表** → [payroll-formula.md](payroll-formula.md)
- **HR Tab 9 張子表整批替換流程**（後端 transaction 寫入） → [backend-design.md §6.2 EF Core 寫入](../backend-design.md#62-ef-core-寫入)
- **multipart upload + Blob 條件式刪除** → [backend-design.md §12 檔案上傳](../backend-design.md#12-檔案上傳multipart--blob)
- **3 Tab UI / FormArray pattern** → [frontend-design.md §5 Tab UI](../frontend-design.md#5-tab-uipill-button-pattern) + [§7 明細列表](../frontend-design.md#7-明細列表formarray)
- **Entity 結構（EmployeeProfile + 9 子表 + HealthInsuranceDependent）** → [database-schema.md](../database-schema.md)
- **API 端點 GET / PUT /users/{id}/profile** → [api-routes.md §員工人事資料卡](../api-routes.md#員工人事資料卡hr-profile)
