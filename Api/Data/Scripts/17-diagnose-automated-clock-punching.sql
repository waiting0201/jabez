/*
================================================================================
 診斷：找出疑似「機器人 / 排程腳本」打卡與其他打卡異常（唯讀，不修改任何資料）
================================================================================

 背景
 ----
 2026-10 客戶反映有人用機器人打卡。打卡時間取自伺服器 Clock.Now（無法偽造），
 但 GPS 是前端自行回報、且打卡 API 只要持有效 JWT 即可呼叫 —— 寫一支排程腳本
 「登入 → 呼叫 /attendances/clock-in」就能在人不在場時完成打卡。

 系統沒有記錄 IP / User-Agent，故只能從 DB 既有欄位推斷，四個訊號：
   ① 取得登入憑證後 ≤1.5 秒就打卡（RefreshTokens.CreatedAt 與打卡時間皆為台北時間）。
      真人從開 App（自動 refresh）到按下打卡通常 2～10 秒；偶爾 ≤1.5 秒屬正常，
      **長期大量**才可疑。
   ② 每天打卡落在同一時刻 ±15 秒（腳本 cron 的特徵）。
      ⚠ 08:58～09:00 的集中是 LINE 打卡提醒（08:58 推播）點開後立即打卡，屬正常；
        已綁 LINE 者的此區段請忽略。
   ③ 完全沒有 GPS（腳本通常不送座標；但也可能是使用者拒絕定位權限，需搭配①②判斷）。
   ④ 已核准請假日仍在整點出現登入憑證（請假日打卡會被系統擋下而不留紀錄，
      只剩 RefreshTokens 可見）。

 另附「非機器人但值得人工確認」的異常：深夜 / 凌晨打上班卡後不打下班卡、
 由系統自動補 9 小時下班卡。

 判讀注意
 --------
 · 毫秒為 0 的打卡時間 = 管理者於出缺勤報表手動修改，已排除於時間規律統計之外。
 · IsClockInAuto / IsClockOutAuto = 系統自動補卡，已排除。
 · 本腳本只能指出「可疑」，最終仍須人工查證（約談、比對門禁 / 監視器）。

 用法
 ----
 直接執行；各段獨立。用 sqlcmd 跑請加 -u（否則中文會變亂碼）：
   sqlcmd -S <server> -d <db> -U <user> -P <pwd> -i 17-diagnose-automated-clock-punching.sql -u
 @Since 可調整檢查起日。
================================================================================
*/

SET NOCOUNT ON;

DECLARE @Since date = DATEADD(DAY, -90, CAST(GETDATE() AS date));

-- 所有本人打卡動作攤平（排除系統補卡）
IF OBJECT_ID('tempdb..#Punches') IS NOT NULL DROP TABLE #Punches;

SELECT * INTO #Punches FROM (
    SELECT UserId, RecordDate, N'上班' AS Kind, ClockInTime AS PunchAt, ClockInLatitude AS Lat
    FROM AttendanceRecords WHERE ClockInTime IS NOT NULL AND IsClockInAuto = 0
    UNION ALL
    SELECT UserId, RecordDate, N'下班', ClockOutTime, ClockOutLatitude
    FROM AttendanceRecords WHERE ClockOutTime IS NOT NULL AND IsClockOutAuto = 0
    UNION ALL
    SELECT UserId, RecordDate, N'加班開始', OvertimeStartTime, OvertimeStartLatitude
    FROM AttendanceRecords WHERE OvertimeStartTime IS NOT NULL
    UNION ALL
    SELECT UserId, RecordDate, N'加班結束', OvertimeEndTime, OvertimeEndLatitude
    FROM AttendanceRecords WHERE OvertimeEndTime IS NOT NULL
) p
WHERE RecordDate >= @Since
  AND DATEPART(MILLISECOND, PunchAt) <> 0;   -- 排除管理者手改

-- 每次打卡前 2 分鐘內最近一次取得登入憑證的間隔
IF OBJECT_ID('tempdb..#Gaps') IS NOT NULL DROP TABLE #Gaps;

SELECT p.UserId, p.RecordDate, p.Kind, p.PunchAt, p.Lat,
       MIN(DATEDIFF(MILLISECOND, r.CreatedAt, p.PunchAt)) AS AuthGapMs
INTO #Gaps
FROM #Punches p
LEFT JOIN RefreshTokens r
       ON r.UserId = p.UserId
      AND r.CreatedAt <= p.PunchAt
      AND r.CreatedAt >  DATEADD(MINUTE, -2, p.PunchAt)
GROUP BY p.UserId, p.RecordDate, p.Kind, p.PunchAt, p.Lat;

/* ---------------------------------------------------------------------------
   1. 綜合評分：每人一列，依「登入後 ≤1.5 秒打卡」次數排序
   --------------------------------------------------------------------------- */
;WITH Regularity AS (
    -- 每人每種動作：同一時刻 ±15 秒內最多落了幾天
    SELECT g1.UserId, g1.Kind, g1.PunchAt, COUNT(*) AS Hits,
           ROW_NUMBER() OVER (PARTITION BY g1.UserId, g1.Kind ORDER BY COUNT(*) DESC) AS rn
    FROM #Gaps g1
    JOIN #Gaps g2
      ON g2.UserId = g1.UserId AND g2.Kind = g1.Kind
     AND ABS(DATEDIFF(MILLISECOND, CAST(g2.PunchAt AS time), CAST(g1.PunchAt AS time))) <= 15000
    GROUP BY g1.UserId, g1.Kind, g1.PunchAt
),
Peak AS (
    -- 先收斂成每人一列，避免與 #Gaps 相乘造成重複計數
    SELECT UserId,
           MAX(CASE WHEN Kind = N'上班' THEN Hits END) AS InHits,
           MAX(CASE WHEN Kind = N'上班' THEN CONVERT(varchar(8), PunchAt, 108) END) AS InAround,
           MAX(CASE WHEN Kind = N'下班' THEN Hits END) AS OutHits,
           MAX(CASE WHEN Kind = N'下班' THEN CONVERT(varchar(8), PunchAt, 108) END) AS OutAround
    FROM Regularity WHERE rn = 1
    GROUP BY UserId
),
Stat AS (
    SELECT UserId,
           COUNT(*) AS Total,
           SUM(CASE WHEN AuthGapMs <= 1500 THEN 1 ELSE 0 END) AS FastAuth,
           SUM(CASE WHEN Lat IS NULL THEN 1 ELSE 0 END) AS NoGps
    FROM #Gaps
    GROUP BY UserId
)
SELECT u.Name AS 姓名,
       d.Name AS 部門,
       CASE WHEN u.LineUserId IS NULL THEN N'否' ELSE N'是' END AS 綁定LINE,
       s.Total AS 打卡次數,
       s.FastAuth AS 登入後1_5秒內打卡,
       s.NoGps AS 無GPS次數,
       pk.InHits AS 上班同刻最多天數,
       pk.InAround AS 上班集中時刻,
       pk.OutHits AS 下班同刻最多天數,
       pk.OutAround AS 下班集中時刻
FROM Stat s
JOIN Users u ON u.Id = s.UserId
LEFT JOIN Departments d ON d.Id = u.DepartmentId
LEFT JOIN Peak pk ON pk.UserId = s.UserId
WHERE s.FastAuth >= 3
   OR pk.InHits >= 7
   OR pk.OutHits >= 7
ORDER BY s.FastAuth DESC, pk.InHits DESC;

/* ---------------------------------------------------------------------------
   2. 明細：登入後 ≤1.5 秒就打卡的每一筆
   --------------------------------------------------------------------------- */
SELECT u.Name AS 姓名,
       CONVERT(char(10), g.RecordDate, 23) AS 日期,
       g.Kind AS 動作,
       CONVERT(varchar(12), g.PunchAt, 114) AS 打卡時間,
       g.AuthGapMs AS 距登入毫秒,
       CASE WHEN g.Lat IS NULL THEN N'無' ELSE N'有' END AS GPS
FROM #Gaps g
JOIN Users u ON u.Id = g.UserId
WHERE g.AuthGapMs <= 1500
ORDER BY u.Name, g.PunchAt;

/* ---------------------------------------------------------------------------
   3. 已核准請假日仍在整點附近（±10 秒）取得登入憑證
      請假時段內打卡會被系統擋下，故只會在 RefreshTokens 留下痕跡
   --------------------------------------------------------------------------- */
SELECT u.Name AS 姓名,
       CONVERT(varchar(23), r.CreatedAt, 121) AS 登入時間,
       l.LeaveType AS 假別,
       CONVERT(varchar(16), l.StartDate, 120) AS 假起,
       CONVERT(varchar(16), l.EndDate, 120) AS 假迄
FROM RefreshTokens r
JOIN Users u ON u.Id = r.UserId
JOIN LeaveRequests l
  ON l.EmployeeId = r.UserId
 AND l.ApprovalStatus = 'approved'
 AND r.CreatedAt BETWEEN l.StartDate AND l.EndDate
WHERE r.CreatedAt >= @Since
  AND DATEPART(MINUTE, r.CreatedAt) IN (0, 1)
  AND DATEPART(SECOND, r.CreatedAt) <= 10
ORDER BY u.Name, r.CreatedAt;

/* ---------------------------------------------------------------------------
   4. 非機器人但需人工確認：06:30 前或 17:00 後才打上班卡
      （常見手法：凌晨在家打上班卡、不打下班卡，由系統自動補 9 小時）
   --------------------------------------------------------------------------- */
SELECT u.Name AS 姓名,
       CONVERT(char(10), a.RecordDate, 23) AS 日期,
       CONVERT(varchar(8), a.ClockInTime, 108) AS 上班,
       CONVERT(varchar(8), a.ClockOutTime, 108) AS 下班,
       CASE WHEN a.IsClockOutAuto = 1 THEN N'系統補卡' ELSE N'' END AS 下班來源,
       CONCAT(ROUND(a.ClockInLatitude, 4), ',', ROUND(a.ClockInLongitude, 4)) AS 上班GPS
FROM AttendanceRecords a
JOIN Users u ON u.Id = a.UserId
WHERE a.RecordDate >= @Since
  AND a.IsClockInAuto = 0
  AND (CAST(a.ClockInTime AS time) < '06:30' OR CAST(a.ClockInTime AS time) >= '17:00')
ORDER BY u.Name, a.RecordDate;

DROP TABLE #Gaps;
DROP TABLE #Punches;
