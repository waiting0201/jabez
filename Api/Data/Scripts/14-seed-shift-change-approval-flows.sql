/*
================================================================================
 建立〈改班申請〉的逐部門簽核流程
================================================================================

 背景
 ----
 改班申請（shift_change）是四週彈性工時新增的第 12 種申請類型，**以自己的
 ApplicationType 解析流程**（不像銷假是借用請假的設定），故管理員必須替它建
 ApprovalItem，否則同仁按下送簽會因為找不到流程而卡住。

 客戶指定的五條路線（2026-09-17 回覆）：
   1. 營運管理及發展部〈含底下部門〉 部門協理 → 執行長 → 財務協理 → 總監
   2. 品牌事業部〈含底下部門〉       部門協理 → 財務協理 → 總監
   3. 數位研發部                     財務協理 → 總監
   4. 總監室專案部門                 總監
   5. 行政財務管理部〈含會計室〉     財務協理 → 總監
 另依既有慣例補第 6 條：總監室本身 → 總監
 （9 種申請類型中有 7 種都替總監室建了專屬流程，不補的話總監室直屬同仁沒有流程可用。）

 「含底下部門」不需要任何新功能 —— ApprovalItem 綁在母部門，子部門送單時會沿
 Departments.ParentId 往上繼承最近的祖先流程（見 ApprovalReadService.GetActiveByTypeAsync）。

 ⚠ 部門 / 職稱一律以**名稱**解析，不寫死 Id
 --------------------------------------------
 正式站與測試站的部門結構**已經漂移**（實測 2026-09-22）：
   · 部門 16：正式站「營運管理及發展部」／測試站「營運管理部」
   · 職稱 7：正式站「財務協理」／測試站「財務長」
   · 「總監室專案部門」測試站**不存在**
 故本腳本以名稱比對並接受多個候選寫法，查無者**跳過該條路線並在報表列出**，
 不會建出一條指向錯誤部門的流程。跑之前請先看空跑報表確認命中了哪幾條。

 ⚠ 「部門協理」關卡：**沒有任何現成機制能完全表達它**（2026-09-22 實測後的結論）
 ------------------------------------------------------------------------
 客戶要的是「申請人所屬部門的協理」。引擎有兩種寫法，兩種都不完全對：

   · UseApplicantDepartment + JobTitleId = 協理
     → **只看申請人自己的部門、不會往上層找**（ApprovalFlowService.ResolveReviewerPoolAsync）。
       該部門沒有協理就在送簽當下被擋：「第 1 關找不到可審核的人員」。
   · UseDirectSupervisor
     → 找的是「下一個**職級**」而非協理。店員送單會解析到**店長**，整個跳過協理，
       與客戶指定的路線不符（實測：發展一部店員 → 徐嘉秀 店長，而非簡子珮 協理）。

 本腳本採**前者**（忠於規格）。後者雖然永遠不會擋住，但會靜默把單子送給錯的人，
 比擋下來更糟 —— 擋下來至少使用者看得到訊息、管理員知道要處理。

 ⚠ **因此下列部門的同仁目前送不出改班申請**（正式站實測，空跑報表會列出）：
   發展三部、新太平洋1號店、成功海銀行、東發號 —— 這些部門沒有人掛「協理」。
   這是**需要回問客戶的業務問題**（要送給母部門協理？還是該部門的經理？），
   不要自行發明 fallback。上線前務必確認。

 ⚠ 關卡若查無可簽核人員，單子會卡住
 ----------------------------------
 例如測試站原本沒有任何人掛「執行長」，路線 1 的第 2 關就沒有人能簽。
 空跑報表會列出每一關的候選人數，**0 人的關卡要先補人或調整路線**。

 執行方式
 --------
 1. 先原樣執行（@Commit = 0）看空跑報表
 2. 再把 @Commit 改成 1 重跑
 冪等：同一部門已有 shift_change 流程者跳過，可安全重跑。
================================================================================
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @Commit bit = 0;   -- 改成 1 才會真的寫入
DECLARE @Now datetime2(7) = SYSDATETIME();

BEGIN TRAN;

-- ── 名稱解析（接受正式站 / 測試站兩種寫法） ────────────────────────────────
DECLARE @DirectorDept int = (SELECT TOP 1 Id FROM Departments WHERE Name = N'總監室');
DECLARE @FinanceDept  int = (SELECT TOP 1 Id FROM Departments WHERE Name IN (N'行政財務管理部', N'財務管理部'));
DECLARE @OpsDept      int = (SELECT TOP 1 Id FROM Departments WHERE Name IN (N'營運管理及發展部', N'營運管理部'));
DECLARE @BrandDept    int = (SELECT TOP 1 Id FROM Departments WHERE Name LIKE N'品牌事業部%');
DECLARE @DigitalDept  int = (SELECT TOP 1 Id FROM Departments WHERE Name IN (N'數位研發部', N'數位發展部'));
DECLARE @ProjectDept  int = (SELECT TOP 1 Id FROM Departments WHERE Name = N'總監室專案部門');

DECLARE @DirectorTitle int = (SELECT TOP 1 Id FROM JobTitles WHERE Name = N'總監');
DECLARE @CeoTitle      int = (SELECT TOP 1 Id FROM JobTitles WHERE Name = N'執行長');
DECLARE @CfoTitle      int = (SELECT TOP 1 Id FROM JobTitles WHERE Name IN (N'財務協理', N'財務長'));
DECLARE @DeptHeadTitle int = (SELECT TOP 1 Id FROM JobTitles WHERE Name = N'協理');

-- ── 路線定義：一列一關 ─────────────────────────────────────────────────────
DECLARE @Routes TABLE (
    RouteNo   int,
    DeptId    int,           -- 流程綁定的部門（null 不允許，六條都綁部門）
    RouteName nvarchar(100),
    StepOrder int,
    StepDept  int,           -- 關卡部門；null + UseApplicantDept=1 代表用申請人部門
    StepTitle int,
    UseApplicantDept bit,
    UseDirectSup     bit
);

-- 1 營運管理及發展部：部門協理 → 執行長 → 財務協理 → 總監
INSERT INTO @Routes VALUES
 (1, @OpsDept, N'改班申請（營運管理及發展部）', 1, NULL,           @DeptHeadTitle, 1, 0),
 (1, @OpsDept, N'改班申請（營運管理及發展部）', 2, @OpsDept,       @CeoTitle,      0, 0),
 (1, @OpsDept, N'改班申請（營運管理及發展部）', 3, @FinanceDept,   @CfoTitle,      0, 0),
 (1, @OpsDept, N'改班申請（營運管理及發展部）', 4, @DirectorDept,  @DirectorTitle, 0, 0),
-- 2 品牌事業部：部門協理 → 財務協理 → 總監
 (2, @BrandDept, N'改班申請（品牌事業部）', 1, NULL,          @DeptHeadTitle, 1, 0),
 (2, @BrandDept, N'改班申請（品牌事業部）', 2, @FinanceDept,  @CfoTitle,      0, 0),
 (2, @BrandDept, N'改班申請（品牌事業部）', 3, @DirectorDept, @DirectorTitle, 0, 0),
-- 3 數位研發部：財務協理 → 總監
 (3, @DigitalDept, N'改班申請（數位研發部）', 1, @FinanceDept,  @CfoTitle,      0, 0),
 (3, @DigitalDept, N'改班申請（數位研發部）', 2, @DirectorDept, @DirectorTitle, 0, 0),
-- 4 總監室專案部門：總監
 (4, @ProjectDept, N'改班申請（總監室專案部門）', 1, @DirectorDept, @DirectorTitle, 0, 0),
-- 5 行政財務管理部〈含會計室〉：財務協理 → 總監
 (5, @FinanceDept, N'改班申請（行政財務管理部）', 1, @FinanceDept,  @CfoTitle,      0, 0),
 (5, @FinanceDept, N'改班申請（行政財務管理部）', 2, @DirectorDept, @DirectorTitle, 0, 0),
-- 6 總監室（依既有慣例補）：總監
 (6, @DirectorDept, N'改班申請（總監室）', 1, @DirectorDept, @DirectorTitle, 0, 0);

-- ── 排除：部門或職稱查無、以及已經建過的 ───────────────────────────────────
SELECT DISTINCT RouteNo, DeptId, RouteName
INTO #Ok
FROM @Routes r
WHERE r.DeptId IS NOT NULL
  -- 「上級」型關卡（UseDirectSup = 1）刻意不指定部門與職稱，故不算缺漏；
  -- 只有固定關卡（部門＋職稱）少了任一邊才算解析失敗
  AND NOT EXISTS (SELECT 1 FROM @Routes x WHERE x.RouteNo = r.RouteNo
                    AND x.UseDirectSup = 0
                    AND (x.StepTitle IS NULL OR x.StepDept IS NULL))
  AND NOT EXISTS (SELECT 1 FROM ApprovalItems ai
                  WHERE ai.ApplicationType = 'shift_change' AND ai.DepartmentId = r.DeptId);

-- ── 空跑報表 ────────────────────────────────────────────────────────────────
SELECT N'【將建立】' AS Section, o.RouteNo, o.RouteName, d.Name AS 綁定部門
FROM #Ok o JOIN Departments d ON d.Id = o.DeptId ORDER BY o.RouteNo;

SELECT N'【跳過】' AS Section, r.RouteNo, r.RouteName,
       CASE WHEN r.DeptId IS NULL THEN N'查無此部門'
            WHEN EXISTS (SELECT 1 FROM ApprovalItems ai WHERE ai.ApplicationType='shift_change' AND ai.DepartmentId=r.DeptId) THEN N'已建立過'
            ELSE N'關卡的部門或職稱查無' END AS 原因
FROM (SELECT DISTINCT RouteNo, DeptId, RouteName FROM @Routes) r
WHERE r.RouteNo NOT IN (SELECT RouteNo FROM #Ok)
ORDER BY r.RouteNo;

-- 每一關實際有幾位在職候選人（0 人的關卡會讓單子卡住，送簽時直接被擋下）
--
-- ⚠ **「申請人部門」型關卡不可用全域計數** —— 那種關卡找的是「送單那個人所屬部門」的該職稱，
--   不是全公司。早期版本寫成 `UseApplicantDept = 1 OR u.DepartmentId = ...`，
--   把全公司的協理都算進去，於是「營運管理部有 4 位協理」—— 實際上該部門一位也沒有，
--   送簽當場被擋（實測 2026-09-22）。故此處改為**逐部門展開**：
--   列出「流程綁定部門 ∪ 其所有子孫部門」各自有幾位，這才是實際會送單的人所在的部門。
-- ⚠ STRING_AGG **不能包住含 COUNT 的子查詢**（Msg 130），故先把逐部門人數落成暫存表再彙總。
SELECT t.RouteNo, t.Id AS DeptId, t.Name AS DeptName
INTO #Tree
FROM (
    SELECT o.RouteNo, d.Id, d.Name, CAST(d.Id AS varchar(max)) AS Path
    FROM #Ok o JOIN Departments d ON d.Id = o.DeptId
    UNION ALL
    SELECT t.RouteNo, c.Id, c.Name, t.Path + '>' + CAST(c.Id AS varchar(max))
    FROM (SELECT o.RouteNo, d.Id, d.Name, CAST(d.Id AS varchar(max)) AS Path
          FROM #Ok o JOIN Departments d ON d.Id = o.DeptId) t
    JOIN Departments c ON c.ParentId = t.Id
) t;

SELECT r.RouteNo, r.StepOrder, tr.DeptName,
       (SELECT COUNT(*) FROM Users u
        WHERE u.Status='active' AND u.IsSuperAdmin=0
          AND u.JobTitleId = r.StepTitle AND u.DepartmentId = tr.DeptId) AS Cnt
INTO #PerDept
FROM @Routes r
JOIN #Tree tr ON tr.RouteNo = r.RouteNo
WHERE r.UseApplicantDept = 1 AND r.RouteNo IN (SELECT RouteNo FROM #Ok);

SELECT N'【關卡候選人】' AS Section, r.RouteNo, r.StepOrder,
       CASE WHEN r.UseDirectSup = 1 THEN N'（申請人的直屬上級）'
            WHEN r.UseApplicantDept = 1 THEN N'（申請人部門）' ELSE sd.Name END AS 關卡部門,
       jt.Name AS 職稱,
       CASE WHEN r.UseDirectSup = 1
            THEN N'（送簽時依申請人職級動態解析：同部門更高階者 → 找不到則沿部門 ParentId 往上）'
            WHEN r.UseApplicantDept = 1
            THEN ISNULL((SELECT STRING_AGG(CAST(CONCAT(p.DeptName, N' ', p.Cnt, N' 人') AS nvarchar(max)), N'、')
                         FROM #PerDept p WHERE p.RouteNo = r.RouteNo AND p.StepOrder = r.StepOrder), N'（無子部門）')
            ELSE CAST((SELECT COUNT(*) FROM Users u WHERE u.Status='active' AND u.IsSuperAdmin=0
                       AND u.JobTitleId = r.StepTitle AND u.DepartmentId = r.StepDept) AS nvarchar(20)) + N' 人'
       END AS 候選人
FROM @Routes r
LEFT JOIN Departments sd ON sd.Id = r.StepDept
LEFT JOIN JobTitles  jt ON jt.Id = r.StepTitle
WHERE r.RouteNo IN (SELECT RouteNo FROM #Ok)
ORDER BY r.RouteNo, r.StepOrder;

DROP TABLE #Tree; DROP TABLE #PerDept;

-- ── 寫入 ────────────────────────────────────────────────────────────────────
DECLARE @RouteNo int, @DeptId int, @RouteName nvarchar(100), @ItemId int;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT RouteNo, DeptId, RouteName FROM #Ok ORDER BY RouteNo;
OPEN c; FETCH NEXT FROM c INTO @RouteNo, @DeptId, @RouteName;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO ApprovalItems (Name, Code, Description, IsActive, ApplicationType, DepartmentId, CreatedAt)
    VALUES (@RouteName, CONCAT('shift_change_', @DeptId), N'四週彈性工時：班表定案後的異動申請', 1, 'shift_change', @DeptId, @Now);
    SET @ItemId = SCOPE_IDENTITY();

    INSERT INTO ApprovalSteps (ApprovalItemId, StepOrder, DepartmentId, JobTitleId,
                               UseApplicantDepartment, UseDirectSupervisor, UseApplicantDesignated,
                               DesignatedRequiresDepartment, MinDays, Note, CreatedAt)
    SELECT @ItemId, r.StepOrder, r.StepDept, r.StepTitle,
           r.UseApplicantDept, r.UseDirectSup, 0, 0, NULL, NULL, @Now
    FROM @Routes r WHERE r.RouteNo = @RouteNo ORDER BY r.StepOrder;

    FETCH NEXT FROM c INTO @RouteNo, @DeptId, @RouteName;
END
CLOSE c; DEALLOCATE c;

-- ── 寫入後核對 ──────────────────────────────────────────────────────────────
SELECT N'【結果】' AS Section, ai.Id, ai.Name, d.Name AS 綁定部門, s.StepOrder,
       ISNULL(sd.Name, N'（申請人部門）') AS 關卡部門, jt.Name AS 職稱
FROM ApprovalItems ai
JOIN Departments d ON d.Id = ai.DepartmentId
JOIN ApprovalSteps s ON s.ApprovalItemId = ai.Id
LEFT JOIN Departments sd ON sd.Id = s.DepartmentId
LEFT JOIN JobTitles  jt ON jt.Id = s.JobTitleId
WHERE ai.ApplicationType = 'shift_change'
ORDER BY ai.Id, s.StepOrder;

DROP TABLE #Ok;

IF @Commit = 1 BEGIN COMMIT TRAN; PRINT N'== 已認可（COMMIT）=='; END
ELSE BEGIN ROLLBACK TRAN; PRINT N'== 空跑（ROLLBACK），把 @Commit 改成 1 才會真的寫入 =='; END
