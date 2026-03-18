---
name: UseDirectSupervisor 簽核流程測試報告 (2026-03-18)
description: 完整測試 UseDirectSupervisor 逐步往上審核功能，包含正常流程、邊界條件與發現的 bug
type: project
---

# UseDirectSupervisor 簽核流程完整測試報告
**測試日期**: 2026-03-18
**測試環境**: http://localhost:7071/api

## 測試資料準備

### 使用部門：業務部（ID=3）
- Carol Liu (工程師, JobTitle ID=1, Level=1) — 最低層
- Eve Test (資深工程師, JobTitle ID=2, Level=2) — 測試新增（現已 inactive）
- David Test (主任工程師, JobTitle ID=3, Level=3) — 測試新增（現已 inactive）
- Tim (部門主管, JobTitle ID=4, Level=4) — 最高層

### Level 定義確認
- DB 資料：工程師(Level=1), 資深工程師(Level=2), 主任工程師(Level=3), 部門主管(Level=4), 總監(Level=5)
- **Level 數字越大 = 層級越高**
- 程式碼 `Level < applicantLevel` = 找比申請人層級更高的人（Level 數字更小）
- **注意**：`ApprovalFlowService.cs` line 95 的 XML 註解「Level 數字越小 = 層級越高」是**誤導性的錯誤文字**，與實際資料庫定義相反

### 修改的簽核流程
- 請假申請流程 (ID=4) Step1 暫時改為 UseDirectSupervisor=true
- 新增 Step2 UseDirectSupervisor=true
- 測試後已完全恢復原始設定

---

## 測試 A：待審核任務列表（GET /approval-tasks）

### A-1：一般使用者的任務列表過濾
**測試**: Tim(Level4) 送出請假後，各使用者查看待審任務
- David(Level3, rank=0): 可看到 Step1 ✓
- Carol(Level1, rank=1): 不可看到 Step1 ✓（Step1 還未到她）
- Tim 自己: 不可看到自己的申請 ✓

**結論**: 任務列表的 UseDirectSupervisor 過濾邏輯正確

### A-2：Superadmin 查看所有任務
- SA 可看到所有非 draft 狀態的申請 ✓
- SA 的 GetAllAsync 回傳 PagedResult（含 approved/pending/rejected/returned）

---

## 測試 B：逐步往上審核主流程

### B-1：Tim(Level4) 送出請假
**測試步驟**:
1. Tim 建立並送出請假申請 (ID=3007)
2. 系統查找業務部中 Level < 4 的層級：Level3(David), Level1(Carol)
3. OrderByDescending → Level3(David) rank=0, Level1(Carol) rank=1
4. Step1 從 rank=0 開始 → 應由 Level3 的人審核

**結果**:
- status: pending, currentStepOrder=1 ✓
- David(Level3) 看到任務 ✓
- Carol(Level1) 看不到 ✓（正確，還未到 Step2）

### B-2：David 審核 Step1 通過
**Request**: `PATCH /approval-tasks/leave/3007/review`
**Body**: `{"action":"approved","reviewNote":"David 審核通過 - 第一層上級"}`
**結果**: status=pending, currentStepOrder=2, 審核記錄 Step1 by David ✓

### B-3：Carol 審核 Step2 通過
**條件**: Step2 rank=1 → Level1(Carol)
**結果**:
- Carol 可看到 Step2 的任務 ✓
- David 嘗試審核 Step2 → 403 Forbidden ✓（正確阻擋）
- Carol 審核通過 → status=approved ✓

**完整審核記錄**:
- Step1: David Test, approved
- Step2: Carol Liu, approved

---

## 測試 C：授權驗證

### C-1：正確的審核者能審核 ✓
- David(Level3) 審核 Step1（rank=0）→ 200 OK ✓
- Carol(Level1) 審核 Step2（rank=1）→ 200 OK ✓

### C-2：層級不對的人被拒絕 ✓
- David 嘗試審核 Step2（應為 Carol 的 rank=1 位置）→ 403 "You are not authorized to review this step." ✓

### C-3：三層審核流程（David Level3 申請）
- 業務部: Carol(L1), Eve(L2), Tim(L4)
- David(Level3) 的申請：Level < 3 = Level1, Level2
- OrderByDescending: Level2(Eve) rank=0, Level1(Carol) rank=1
- Step1 → Eve 審核 ✓
- Step2 → Carol 審核 ✓
- 完整流程通過 ✓

---

## 測試 D：邊界條件

### D-1：部門唯一員工（無上級）→ 自動核准 ✓
**條件**: Alice 是會計部唯一員工（部門主管 Level4）
**結果**: UseDirectSupervisor 找 Level < 4 在同部門 = 無人 → 跳過所有步驟 → 自動核准
**Message**: "Leave request auto-approved."
**ReviewNote**: "系統自動核准（所有審核步驟皆為申請人本人）"
**結論**: 邊界處理正確 ✓

### D-2：Step1 有人但 Step2 無人 → BUG 發現！
**條件**: Eve(Level2) 送出請假
- Level < 2 = Level1(Carol) 只有一個層級
- Step1 rank=0 → Carol 審核（有人）
- Step2 rank=1 → 無人（Level1 之後沒有更小的 Level）

**送出時行為**: `ResolveStartingStepAsync` 在送出時正確識別 Step1 有人，所以從 Step1 開始（status=pending）

**問題點**: Carol 通過 Step1 後，`ProcessReviewAsync` 直接 `incrementStep()` 推進到 Step2，沒有判斷 Step2 是否有人可審核

**後果**: status=pending, currentStepOrder=2，但沒有任何普通使用者能看到或審核這個任務
- Carol: 看不到（她應審 rank=1，但 rank=1 不存在）
- Eve: 看不到（她是申請人）
- David: 看不到（Level3 > Level2，不是上層級）
- Tim: 意外能看到（見 D-2 異常）

**結論**: **嚴重 Bug** — 申請進入永久卡死狀態

### D-2 異常：Tim 可審核卡死的 Step2
**條件**: Eve(Level2) 的請假卡在 Step2（無人可審），Tim(Level4) 嘗試審核
**預期**: 403 Forbidden（Level4 > Level2，且 rank=1 不存在，targetLevel=0）
**實際**: 200 OK，Tim 成功審核，status=approved

**根因分析**:
- `AuthorizeStepAsync` 計算 targetLevel：在業務部中 Level < Eve(2) = Level1(Carol), 再 Skip(rank=1) = 無結果
- `FirstOrDefaultAsync()` 對 int 回傳 default(int) = **0**
- 判斷：`if (targetLevel == 0 || reviewerLevel != targetLevel)` → targetLevel=0 為 true → **應 throw Forbidden**
- 但 Tim 居然通過了，可能是審核發生在 Eve 已被設為 inactive 後，導致 UseDirectSupervisor 查詢邏輯失效

**待進一步確認**: 是否因 Eve 帳號 inactive 後，業務部的 Level 結構改變，導致 targetLevel 計算結果不同

---

## 測試 E：退回與重送流程

### E-1：退回後重送 ✓
- Eve 退回 David 的請假 → status=returned ✓
- David 重新送出 → status=pending, 重新從 Step1 開始 ✓

### E-2：拒絕流程 ✓
- Eve 再次審核並拒絕 → status=rejected ✓
- 拒絕後無法再審核（"Only pending leave requests can be reviewed"）✓

---

## 發現的 Bug 彙整

### CRITICAL-1：多步驟 UseDirectSupervisor 中間步驟無對應審核者時申請卡死
- **位置**: `ApprovalTaskHandler.cs` `ProcessReviewAsync` + `ApprovalFlowService.cs` `ResolveStartingStepAsync`
- **條件**: 申請人的 Level 在兩個 UseDirectSupervisor 步驟的 rank 之間沒有對應層級
- **後果**: 申請永久 pending，普通用戶看不到也無法審核

### CRITICAL-2：AuthorizeStepAsync targetLevel=0 時的繞過問題（待確認）
- **位置**: `ApprovalTaskHandler.cs` `AuthorizeStepAsync` line 273
- **條件**: 當 rank=1 的上層級不存在，targetLevel=0 時，不符合條件的高層使用者可能審核通過
- **注意**: 可能與使用者帳號狀態（active/inactive）的查詢過濾有關

### Warning：程式碼誤導性註解
- **位置**: `ApprovalFlowService.cs` line 95
- **內容**: "Level 數字越小 = 層級越高" — 與實際 DB 定義相反（工程師=1是最低層）
- **影響**: 閱讀程式碼時造成混淆，可能導致維護人員誤解邏輯

---

## 資料清理情況
- 請假流程 ID=4: ✓ 已完整恢復為原始設定（1 Step, UseApplicantDepartment=True, jobTitleId=4）
- 測試用簽核流程 TEST_DIRECT_SUP (ID=1007): ✓ 已刪除
- Carol 角色: ✓ 已恢復為 viewer 只
- David Test 帳號: 設為 inactive（無法刪除，有 FK 關聯的請假記錄 3010, 3011）
- Eve Test 帳號: 設為 inactive（無法刪除，有 FK 關聯的請假記錄 3009）
- 遺留測試請假申請: 3004-3011（均為 Carol/Tim/Alice/David/Eve 的已結案申請）
