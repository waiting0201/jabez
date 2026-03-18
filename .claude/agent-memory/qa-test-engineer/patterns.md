---
name: Jabez 已確認缺陷模式
description: 在 UseApplicantDesignated 功能測試中發現的已確認 Bug 與品質風險
type: project
---

## 已確認嚴重缺陷

### BUG-01：SkipUnreviewableStepsAsync 中 UseDirectSupervisor 步驟判斷邏輯使用連續步驟號，無法處理非連續 StepOrder

**位置**: `Api/Services/ApprovalFlowService.cs` 第 169-196 行 `SkipUnreviewableStepsAsync()`

**根因**: 迴圈以 `current` 遞增 1 逐步走訪，但實際步驟的 `StepOrder` 可能非連續（例如 1, 10）。當步驟 1 完成後，`nextStep = 2`，但步驟 2 不存在（只有步驟 10），`steps.FirstOrDefault(s => s.StepOrder == current)` 回傳 null，導致 break 並觸發 `allSkipped=true`，整筆申請被自動核准。

**影響**: 任何使用非連續 StepOrder（如 1, 10 或 1, 5, 20）的流程，都可能在中間步驟完成後被錯誤地自動核准，後續步驟（包含 UseApplicantDesignated 步驟）被完全跳過。

**重現步驟**: 建立流程含 StepOrder=1（UseApplicantDepartment）和 StepOrder=10（UseApplicantDesignated），Carol 提交請假後 Tim 核准步驟 1，系統直接將狀態設為 approved，步驟 10 的 Alice 審核從未發生。

---

### BUG-02：LeaveRequestDto（及相關 DTO）不包含 DesignatedReviewerId 欄位

**位置**: `Api/Models/Dtos/LeaveRequestDtos.cs`、`Api/Services/Dapper/LeaveRequestReadService.cs`

**根因**: `LeaveRequestDto` record 沒有 `DesignatedReviewerId` 欄位；`LeaveRequestReadService` 的 SQL BaseSql 也未 SELECT 該欄位。因此 GET 回應、列表、以及 submit 後的回傳，都無法看到 `designatedReviewerId`。

**影響**: 前端在建立/更新請假申請後，無法從 API 回應確認 `designatedReviewerId` 是否被正確儲存。同樣問題可能存在於其他申請類型的 Dto（OvertimeRequestDto、TravelRequestDto 等）。

---

### BUG-03：非連續 StepOrder 也影響 ProcessReviewAsync 的下一步通知

**位置**: `Api/Handlers/ApprovalTaskHandler.cs` 第 360-393 行 `ProcessReviewAsync()`

**根因**: 當一步驟核准後，呼叫 `SkipUnreviewableStepsAsync(approvalItemId, applicantId.Value, nextStep, designatedReviewerId)` 傳入 `nextStep = currentStepOrder + 1`。若實際下一步的 StepOrder 不等於 `currentStepOrder + 1`，同樣的 null-break 問題導致 allSkipped=true，直接核准整筆申請。

---

### BUG-04：LeaveRequest 新增時的 FK 約束錯誤未被友善處理

**位置**: `Api/Handlers/LeaveRequestHandler.cs` CreateAsync()

**根因**: 傳入不存在的 `DesignatedReviewerId`（非真實 User.Id 的 GUID）時，EF Core 在 `db.SaveChangesAsync()` 時拋出 SQL FK 違反例外，被全域 ExceptionMiddleware 捕捉後回傳 500 `"An unexpected error occurred."`，而非 400 Bad Request 含有意義的錯誤訊息。

**How to apply**: 測試任何包含 FK 欄位的新增/更新操作時，需明確測試無效 ID 的錯誤回應品質。

---

## 已驗證正常運作

- UseApplicantDesignated 步驟 CRUD（新增、更新、刪除）：正常
- 三種模式互斥切換（UseApplicantDesignated / UseDirectSupervisor / 一般）：正常
- Submit 時缺少 DesignatedReviewerId 的 400 驗證：正常（訊息清楚）
- 指定審核者（Alice）能看到待審任務：正常
- 非指定審核者（Tim）嘗試審核被 403 阻擋：正常
- 指定審核者（Alice）能成功審核：正常

## Why（背景）
2026-03-18 對 UseApplicantDesignated 功能進行完整 CRUD 及審核流程測試後確認。
