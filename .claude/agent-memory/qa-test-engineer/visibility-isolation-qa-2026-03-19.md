---
name: 簽核作業資料可見性隔離測試 (2026-03-19)
description: GET /approval-tasks 清單隔離、單筆查詢權限、審核動作授權的完整測試結果
type: project
---

## 測試日期：2026-03-19

### 測試結論

GET /approval-tasks 清單隔離：完全正確，6 位使用者無誤判

### CRITICAL：GET /approval-tasks/{type}/{id} 無存取控制

- 任何已登入使用者可透過 ID 枚舉讀取所有申請單完整詳情
- 確認：Carol（viewer）、David（主任工程師）、Eve、Bob 均成功讀取無關申請單
- Location: `ApprovalTaskHandler.cs` `GetByIdAsync` — 無 userId 過濾

### BUG：ForbidResult 在 Azure Functions Isolated Worker 回 HTTP 500 而非 403

- 觸發條件：使用者無 `approval-tasks:write` 權限（Carol viewer 角色）執行 PATCH review
- `ApprovalTaskHandler.ReviewAsync` line 124: `return new ForbidResult()` → HTTP 500 空 body
- 有 `approval-tasks:write` 但不符合 AuthorizeStepAsync 的使用者正確回 403
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/ApprovalTaskHandler.cs` line 124

### 設計問題：流程設定修改後，舊申請單仍依建立時快照運行

- task 4013 有 designatedReviewer Bob (stepOrder=2)，但目前 approval-items step=2 已改為業務部主管
- Bob 在 task 4013 上可見且審核成功，然後系統繼續要求 Tim（業務部主管）補審 step=2
- 這造成雙重審核問題（指定審核者 + 流程設定審核者各一次）

### approval-tasks/{type}/{id} 回傳 flow.steps 遺失 deptId/jtId

- approval-items/4 step=2 設定 deptId=3, jtId=4
- approval-tasks/leave/{id} 的 flow.steps[1] 顯示 deptId=None, jtId=None
- 資料不完整，前端流程圖顯示可能遺漏審核者資訊
- 此問題在 designated-reviewer-qa-2026-03-19.md 中也有記錄（LeaveRequestDto 缺欄位）

### 通過項目（2026-03-19）

- GET /approval-tasks 清單隔離：6 位使用者各自只看到應看到的任務（完全正確）
- UseApplicantDesignated 時序保護：stepOrder=2 等 stepOrder=1 審完才出現（正確）
- 固定部門/職稱步驟授權拒絕：David、Eve 搶審回 403（正確）
- Bob 嘗試跳過 Alice 搶審回 403（正確）
- Tim 嘗試審核 Alice 指定任務回 403（正確）
- 申請人 Carol 不出現在自己申請的待審清單中（正確）

### AppRouter 的 approval-tasks 路由設定

- GET /approval-tasks 和 GET /approval-tasks/{type}/{id}：回傳 null（登入即可，無需特定權限）
- PATCH /approval-tasks/{type}/{id}/review：回傳 null（由 Handler 內部判斷）
- 實際權限控制完全委由 ReviewAsync 內部邏輯

### ForbidResult 問題的根本原因

AppRouter 對 PATCH review 回傳 null（不要求特定權限），進入 ReviewAsync 後：
- isDesignatedReviewer=false AND 無 approval-tasks:write → `return new ForbidResult()`
- ForbidResult 在 Azure Functions Isolated Worker 無法序列化 → HTTP 500 空 body
- 解法方向：改回傳 ObjectResult + ApiResponse.Fail + HTTP 403
