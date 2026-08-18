# 員工人事資料卡（HR Profile）

員工編輯頁 [user-form](../../Admin/src/app/features/admin/users/pages/user-form/user-form.html) 採 **3 Tab** 結構：

| Tab | 名稱 | 內容 | API |
|---|---|---|---|
| 1 | 員工基本資料 | Email / 角色 / 部門 / 職稱 / 底薪 / 伙食津貼 / 頭像 / 簽名 / **三個身份旗標 + 證明檔（原住民 / 低收入 / 殘障）** / **健保 / 勞保金額（可手動覆寫，留空 fallback 級距表）** | `POST /users` / `PATCH /users/{id}`（multipart） |
| 2 | 人事資料卡（**「薪資調整」子表需 `payroll:read`**） | 員工代號 / 英文名 / 身分證號 / 性別 / 婚姻 / 出生地 / 行動電話 / 戶籍 / 通訊 / 緊急聯絡 / 銀行帳號 / 投保起日 / 扶養人 / 專長興趣 / 離職原因 / **身分證正反面影本** / **最高學歷證明** / **存摺封面** / 學歷 / 經歷 / 家庭 / 訓練 / 語言 / 職務調整 / 獎懲 / 薪資調整 | `GET / PUT /users/{id}/profile`（PUT 為 multipart） |
| 3 | 健保眷屬 | 姓名 / 關係 / 身分證號 / 出生日期；上方提示「最多計 3 口」+ 即時試算 `健保費 = baseHealth × (1 + min(N, 3))` | 同 Tab 2（共用 PUT 端點） |

## 行為規則

- **Tab 2 / 3 lazy load**：第一次切到才呼叫 `GET /users/{id}/profile`；新增模式（尚未建立員工）兩個 Tab disabled
- **整批替換**：PUT 把 9 張子表（學歷 / 經歷 / 家庭 / 訓練 / 語言 / 職務調整 / 獎懲 / 薪資調整 / 健保眷屬）整批 `ExecuteDelete` 後 INSERT 新傳入清單
  - 例外：**薪資調整為「條件式」整批替換** —— payload 的 `salaryAdjustmentRecords` 為 nullable（`null` = 不變更、`[]` = 清空），且需持有 `payroll:read` 才會進入刪除 + 重建。無此權限者的前端不送該 key，既有薪資歷史不刪不改、也不觸發下方的薪資自動同步（見 [payroll-formula.md §誰看得到薪資欄位](payroll-formula.md)）
- **最高學歷證明**：與身分證影本同 multipart 機制，附在 `EmployeeProfile` 主表（`HighestEducationProofUrl`，非每筆學歷各掛附件），blob 命名 `{userId}_education{ext}`，前端使用 `ImageCompressionService.compress` 壓縮（圖片 maxSize=1600 quality=0.85；PDF passthrough），1MB 上限
- **存摺封面**：同上 multipart 機制，附在 `EmployeeProfile` 主表（`BankBookImageUrl`，選填），multipart part 為 `bankBookImage` / `removeBankBook`，blob 容器 `passbooks`、命名 `{userId}_passbook{ext}`，位於「緊急聯絡 / 財務 / 其他」卡片銀行欄位下方；管理端代理 `GET /files/passbooks/{fileName}`（需 `users:read`），員工自助走 `/me/files/passbooks/...`
- **薪資自動同步**：插入完所有 `SalaryAdjustmentRecord` 後，找 `EffectiveDate <= 今日(Asia/Taipei)` 中 `EffectiveDate` 最大的那一筆，把該筆 4 個金額欄位寫回 `User`：`BaseSalary`、`MealAllowance`、`OtherAllowance`（其他加給）、`AdjustmentDifference`（調整差額）；無符合則不變。同步後人事薪資計算自動納入。（2026-08 移除職務加給 / 主管加給 / 外派加給，DB 欄位保留作歷史封存）
- **日期欄位寬鬆解析（Safari 相容）**：學歷 / 經歷起訖用 `<input type="month">`，Safari 不支援會退化成純文字框，使用者可能手打 `2020/09`、`2020.9` 等格式。三層防護：(1) 前端送出前驗證學歷年月格式（不合法擋下並提示 YYYY-MM）；(2) 前端把可解析的年月正規化為 `yyyy-MM-dd`（另補獎懲次數預設 1、健保眷屬生日空字串→null、薪資 baseSalary 預設 0）；(3) 後端 payload 反序列化掛 [FlexibleDateTimeJsonConverter](../../Api/Common/FlexibleDateTimeJsonConverter.cs) 寬鬆解析 12 種常見年月/日期格式，失敗時回傳含欄位值的明確錯誤訊息（非籠統「請求內容格式不正確」）
- **2 寸彩照**：hr.doc 上「黏貼 2 吋彩照」欄位**不做**（也不重用 `User.Avatar`）
- **PDF 列印**：編輯頁 header 提供「列印人事資料卡」按鈕，呼叫 [hr-profile-pdf.service.ts](../../Admin/src/app/features/admin/users/services/hr-profile-pdf.service.ts) 輸出三頁 A4 PDF（純前端 jsPDF）。**第 3 頁為薪資調整歷史，需 `payroll:read`** —— 無此權限時整頁連同 `addPage()` 一起跳過，輸出 2 頁（只藏表格會留一張空白頁）

## 涉及元件

| 元件 | 說明 |
|---|---|
| `EmployeeProfile` Entity | 1:1 對 User，PK=UserId，含 21 個 scalar 欄位 + 9 個導覽集合 |
| 9 張 HR 子表 Entity | 全部 PK=Guid，FK=UserId OnDelete Cascade |
| `EmployeeProfileHandler` | `GetByUserIdAsync` / `UpsertAsync`（multipart） |
| `EmployeeProfileReadService` | 一次 `QueryMultipleAsync` 讀回 EmployeeProfile + 9 張子表 |
| `image-compression.service.ts` | 共用圖檔壓縮（HEIC→JPEG / Canvas resize / JPEG quality；PDF passthrough）；avatar 用 maxSize 800、證件用 maxSize 1600 |
| `employee-profile.service.ts` | 前端 `getByUserId` / `upsert(payload, files)` multipart |
| `hr-profile-pdf.service.ts` | jsPDF + jspdf-autotable + Noto Sans TC，輸出三頁人事資料卡（`generate(profile, user, canSeeSalary)`；無薪資權限時輸出 2 頁） |
| 6 個新 Blob 容器 | `indigenous-proofs`、`low-income-proofs`、`disabled-proofs`、`id-cards`、`education-proofs`、`passbooks`（皆走 `/files/{container}/{fileName}` 授權代理，需 `users:read`） |

---

## 員工自助唯讀檢視（個人資訊）

員工從右上角 avatar 下拉選單的「個人資訊」進入唯讀頁 [my-profile](../../Admin/src/app/features/account/pages/my-profile/)，比照管理頁三分頁排版（員工基本資料含薪資 / 人事資料卡 / 健保眷屬），但**全唯讀、無任何輸入框 / 上傳 / 儲存**。

- **資料來源（自助端點，登入即可、免 `users:read`）**：`GET /me/user`（基本 + 薪資）、`GET /me/profile`（人事資料卡 + 9 子表 + 健保眷屬）、`GET /me/files/{container}/{fileName}`（自己的 PII 檔案）。
- **為何不重用 `/users/{id}` 系列**：那需 `users:read`，一般員工沒有 → 走 self/me 模式，見 [backend-design.md §13.4](../backend-design.md#134-自己讀自己模式self--me-endpoints)。
- **檔案顯示**：簽名 / 頭像走公開 `/files/`（`<img src>` 直接顯示）；身分證影本 / 學歷證明 / 存摺封面 / 三種證明走 `/me/files` blob 下載後開新分頁（`<img>` 無法帶 token）。
- **路由**：`/account/my-profile`，lazy load，僅 `authGuard`（不掛 permission guard）。
- **薪資欄位級權限不適用**：`/me/user` 與 `/me/profile` 刻意不套 `payroll:read`，員工看**自己**的薪資與薪資調整歷史照常顯示。

## 跨業務關聯

- **健保眷屬數影響薪資公式（最多計 3 口）** → [payroll-formula.md](payroll-formula.md)
- **薪資調整紀錄同步 4 個薪資欄位（底薪 / 伙食費 + 2 加給） → User → Payroll 完整連動規則 + 加 / 改 / 刪欄位 Checklist** → [payroll-formula.md §薪資欄位連動規則](payroll-formula.md#薪資欄位連動規則重要--避免遺忘)
- **健保 / 勞保覆寫值優先級 fallback 級距表** → [payroll-formula.md](payroll-formula.md)
- **HR Tab 9 張子表整批替換流程**（後端 transaction 寫入） → [backend-design.md §6.2 EF Core 寫入](../backend-design.md#62-ef-core-寫入)
- **multipart upload + Blob 條件式刪除** → [backend-design.md §12 檔案上傳](../backend-design.md#12-檔案上傳multipart--blob)
- **3 Tab UI / FormArray pattern** → [frontend-design.md §5 Tab UI](../frontend-design.md#5-tab-uipill-button-pattern) + [§7 明細列表](../frontend-design.md#7-明細列表formarray)
- **Entity 結構（EmployeeProfile + 9 子表 + HealthInsuranceDependent）** → [database-schema.md](../database-schema.md)
- **API 端點 GET / PUT /users/{id}/profile** → [api-routes.md §員工人事資料卡](../api-routes.md#員工人事資料卡hr-profile)
