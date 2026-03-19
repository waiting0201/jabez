---
name: UseApplicantDesignated 指定審核流程端對端測試報告
description: 2026-03-19 針對多人指定審核者流程的完整 API 測試結果，含通過項目、失敗項目和異常行為
type: project
---

# UseApplicantDesignated 指定審核流程測試報告（2026-03-19）

## 測試環境
- 後端：http://localhost:7071/api
- ApprovalItem leave (id=4)：Step 1 修改為 UseApplicantDesignated=true，Step 2 新增固定 dept=3/jt=4
- 申請人：Carol（33333333，業務部，工程師 jt=1）
- 指定審核者 B：Alice（11111111，會計部，部門主管 jt=4）
- 指定審核者 C：Bob（22222222，財務部，部門主管 jt=4）
- Step 2 固定審核者：Tim（cb629b28，業務部，部門主管 jt=4）

## 測試結果摘要

### 情境一：2 位指定審核者正常流程（LR 4012）
| 步驟 | 期望 | 實際 | 判斷 |
|------|------|------|------|
| Carol 建立 draft（指定 Alice StepOrder=1, Bob StepOrder=2） | 201，DR 2 筆 pending | 201，DR 2 筆 pending | PASS |
| Carol 送出（draft → pending） | status=pending, step=1 | status=pending, step=None | ANOMALY |
| SA 查詢任務（期望看到 4012） | 看到，step=1 | 看到，step=1，approvalStatus=None | PARTIAL |
| Alice 查詢任務（無全域權限但被指定） | 看到 4012 | 看到 4012 | PASS |
| Bob（StepOrder=2）搶先審核 | 403 | 403 | PASS |
| Alice（StepOrder=1）核准 | 200，Alice DR→approved，Bob DR→pending，step 不變 | 200，正確 | PASS |
| Alice 再次嘗試審核 | 403 | 403 | PASS |
| Bob（StepOrder=2）核准 | 200，Bob DR→approved，step 推進到 2 | 200，Bob DR→approved，step=2 | PASS |
| Tim 查詢任務（Step 2 固定審核） | 看到 4012 step=2 | 看到 4012 step=2，approvalStatus=None | PARTIAL |
| Tim 審核 Step 2 | 200，status=approved | 200，成功 | PASS |
| 最終確認 GET /leave-requests/4012 | status=approved，step=2 | status=approved，step=None | BUG |

### 情境二：退回後重送（LR 4013）
| 步驟 | 期望 | 實際 | 判斷 |
|------|------|------|------|
| Alice 退回 | status=returned，DR1→returned | returned，DR1→returned，DR2 仍 pending | ANOMALY |
| Carol 重新送出 | DR 全部重置 pending，step=1 | DR 全部 pending，step=None | PASS（重置正常）|
| Alice 可再次審核 | 可看到且可審 | 可看到且可審 | PASS |

退回時 DR2（Bob，未輪到者）的狀態維持 pending（非 returned），這是合理行為。

### 情境三：單一指定審核者（LR 4014）
| 步驟 | 期望 | 實際 | 判斷 |
|------|------|------|------|
| Alice 審核唯一審核者 | step 推進到 Step 2 | step=2，Tim 可看到任務 | PASS |

### 情境四：無效操作
| Case | 期望 | 實際 | 判斷 |
|------|------|------|------|
| Case A：空 designatedReviewers 送出 | 400 | 400「此簽核流程包含申請人指定審核步驟，請提供指定審核者。」 | PASS |
| Case B：指定審核者為申請人自己（第 1 位）leave 類型 | 400 | 400「指定審核者不能是申請人本人。」 | PASS |

### 額外測試：自審漏洞（LR 4016, 4017）
| 測試 | 期望 | 實際 | 判斷 |
|------|------|------|------|
| 申請人指定自己為第 1 位（leave）→ submit | 400 | 400 | PASS |
| 申請人指定自己為第 2 位，第 1 位是他人 → submit | 200（允許） | 200 | PASS |
| 第 1 位（Alice）審核後，輪到申請人自己（第 2 位）自審 → review | 應報錯（leave 不允許自審） | 200 通過 | BUG |

## 確認的缺陷

### BUG-1：LeaveRequestDto 缺少關鍵欄位（嚴重性：中）
- `GET /leave-requests/{id}` 回傳的 `currentStepOrder` 永遠為 null
- `GET /leave-requests/{id}` 回傳的 `approvalItemId` 永遠為 null
- `GET /leave-requests/{id}` 回傳的 `reviewedById` 永遠為 null
- 根本原因：`LeaveRequestDto` record 無這些欄位；`LeaveRequestReadService.BaseSql` 未 SELECT 這些欄位
- 影響：前端若依賴 leave-requests/{id} 取得步驟狀態會得到錯誤資料

### BUG-2：approval-tasks 中 approvalStatus 永遠為 null（嚴重性：中）
- `GET /approval-tasks` 和 `GET /approval-tasks/{type}/{id}` 回傳的任務 `approvalStatus` 永遠為 null
- 後端資料庫欄位有正確值，但 Dapper mapping 未正確對應或 DTO 欄位名稱不匹配
- 影響：前端若依賴 approval-tasks 列表的 approvalStatus 欄位來判斷任務狀態會失敗

### BUG-3：自審漏洞（嚴重性：高）
- 申請人可以繞過自審保護：指定自己為第 2 位（非第 1 位）指定審核者
- submit 時只驗證 `designatedReviewers.OrderBy(r=>r.StepOrder).FirstOrDefault()` 是否等於申請人
- 等到輪到第 2 位時（第 1 位已審），申請人本人可以審核自己的申請（leave 類型）
- `AuthorizeStepAsync` 對 UseApplicantDesignated 步驟只查「status=pending 且 StepOrder 最小」
- 確認：Carol 審核自己的 LR 4017 第 2 輪 → 200 成功
- Location: `ApprovalFlowService.cs` ResolveStartingStepAsync（僅驗證 firstReviewer）

## 資料異常備註
- LR 4017 在 Carol 自審後 approvalStatus 應繼續推進至 Step 2 固定審核，
  但實際回傳 status=pending，step=2（未到 approved），
  顯示系統確實繼續流程（非 approved），但未能自動推進—需進一步確認是否等待 Tim 審核 Step 2

## 測試用帳號與密碼
- Carol: carol@example.com / Admin@123
- Alice: alice@example.com / Admin@123
- Bob: waiting0201@gmail.com / Admin@123（已重設）
- Tim: tim@weypro.com / Admin@123（已重設）
- SA: sa@system.local / Admin@123
