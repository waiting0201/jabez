namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 出缺勤報表列。列表為「打卡紀錄 ∪ 當日請假日」的合併結果（見 AttendanceLeaveMerger）：
/// <c>Id = null</c> 代表「當日只有已核准請假、沒有任何打卡紀錄」的請假虛擬列（前端據此不顯示編輯鈕）。
/// </summary>
public sealed record AttendanceRecordDto(
    /// <summary>AttendanceRecord.Id；null＝請假虛擬列（DB 無對應紀錄，不可編輯）</summary>
    int?      Id,
    /// <summary>員工 Id。虛擬列沒有 Id，前端的 track key 與分組一律靠此欄</summary>
    Guid      UserId,
    string    UserName,
    DateTime  RecordDate,
    DateTime? ClockInTime,
    double?   ClockInLatitude,
    double?   ClockInLongitude,
    DateTime? ClockOutTime,
    double?   ClockOutLatitude,
    double?   ClockOutLongitude,
    /// <summary>下班時間為登入時系統自動補卡（非本人打卡），出缺勤清單以 badge 標示</summary>
    bool      IsClockOutAuto,
    DateTime? OvertimeStartTime,
    double?   OvertimeStartLatitude,
    double?   OvertimeStartLongitude,
    DateTime? OvertimeEndTime,
    double?   OvertimeEndLatitude,
    double?   OvertimeEndLongitude,
    int?      OvertimeRequestId,
    /// <summary>AttendanceRecord.CreatedAt；虛擬列為 null</summary>
    DateTime? CreatedAt,
    /// <summary>當日第一張假單的假別（相容欄位，多張假單的完整清單見 <see cref="Leaves"/>）</summary>
    string?   LeaveType,
    /// <summary>當日第一張假單的起日（整張單的區間，非當日）</summary>
    DateTime? LeaveStartDate,
    /// <summary>當日第一張假單的訖日（整張單的區間，非當日）</summary>
    DateTime? LeaveEndDate,
    /// <summary>當日請假時數合計（同日多張假單加總）。無請假為 null</summary>
    decimal?  LeaveHours = null,
    /// <summary>當日所有已核准（且該日未被銷假）的請假，依 StartDate 排序。無請假為 null</summary>
    IReadOnlyList<AttendanceLeaveDto>? Leaves = null,
    /// <summary>該日打卡時勾選為出差，出缺勤清單以 badge 標示。虛擬列恆為 false</summary>
    bool      IsBusinessTrip = false,
    /// <summary>管理者填寫的備註（僅出缺勤編輯表單使用，清單不顯示）。虛擬列恆為 null</summary>
    string?   Remark = null,
    /// <summary>上班時間為登入時系統自動補卡（非本人打卡），出缺勤清單以 badge 標示</summary>
    bool      IsClockInAuto = false,
    /// <summary>
    /// 列的種類：clock＝有打卡紀錄／leave＝當日只有請假／absent＝工作日無打卡且無請假（缺勤）。
    /// 請假列與缺勤列同樣 Id = null，前端不可再用 Id 判斷是哪一種。
    /// </summary>
    string    RowKind = "clock",
    /// <summary>當日應出勤起（扣掉請假時段後）。null＝當日免出勤（全日請假）</summary>
    DateTime? ExpectedStart = null,
    /// <summary>當日應出勤訖（扣掉請假時段後）。null＝當日免出勤（全日請假）</summary>
    DateTime? ExpectedEnd = null,
    /// <summary>曾被管理者修改過（與 IsClockInAuto / IsClockOutAuto 各自獨立：原本是系統補卡、後來被改過可同時為 true）。虛擬列恆為 false</summary>
    bool      IsManuallyAdjusted = false,
    /// <summary>最後修改者姓名（無 FK，由 LastAdjustedById 左連 Users；查無為 null）</summary>
    string?   AdjustedByName = null,
    /// <summary>最後修改時間</summary>
    DateTime? AdjustedAt = null,
    /// <summary>遲到（四週彈性工時切換後才判定；出差當日恆 false）。虛擬列恆為 false</summary>
    bool      IsLate = false,
    /// <summary>早退（四週彈性工時切換後才判定；出差當日恆 false）。虛擬列恆為 false</summary>
    bool      IsEarlyLeave = false,
    /// <summary>員工上班打卡時填寫的遲到原因</summary>
    string?   LateReason = null,
    /// <summary>員工下班打卡時填寫的早退／逾時原因（依 IsEarlyLeave 區分標籤）</summary>
    string?   ClockOutReason = null,
    /// <summary>連結的加班單申請類別（normal / post_hoc）；無連結加班單為 null。post_hoc 者加班欄由核准自動寫入，下班卡可能落在加班時段內（預期）。</summary>
    string?   OvertimeRequestKind = null);

/// <summary>出缺勤報表列中的單張請假資訊（當日份）</summary>
public sealed record AttendanceLeaveDto(
    int      LeaveRequestId,
    string   LeaveType,
    /// <summary>該假單「當日」的時數（由 LeaveDayExpander 逐日展開，非整張單的 Hours）</summary>
    decimal  Hours,
    /// <summary>整張假單的起（非當日）</summary>
    DateTime StartDate,
    /// <summary>整張假單的訖（非當日）</summary>
    DateTime EndDate,
    /// <summary>該假單「當日」的時段代碼：full / am / pm / partial（見 LeaveDaySegments）</summary>
    string   DaySegment,
    /// <summary>該假單「當日」的實際請假起（含日期）</summary>
    DateTime DayStart,
    /// <summary>該假單「當日」的實際請假訖（含日期）</summary>
    DateTime DayEnd);

public sealed record TodayAttendanceDto(
    int       Id,
    DateTime  RecordDate,
    DateTime? ClockInTime,
    double?   ClockInLatitude,
    double?   ClockInLongitude,
    DateTime? ClockOutTime,
    double?   ClockOutLatitude,
    double?   ClockOutLongitude,
    DateTime? OvertimeStartTime,
    double?   OvertimeStartLatitude,
    double?   OvertimeStartLongitude,
    DateTime? OvertimeEndTime,
    double?   OvertimeEndLatitude,
    double?   OvertimeEndLongitude,
    int?      OvertimeRequestId,
    IReadOnlyList<ActiveLeaveDto> TodayLeaves,
    /// <summary>
    /// 今日免下班卡即可打「加班開始」（休假日或全日請假）。
    /// 與 AttendanceHandler.OvertimeStartAsync 的放行判定同源，前端不自行重組規則。
    /// 有預設值 → Dapper 的 MapTodayRow 不需異動（此欄位不存在於 DB，由 Handler 以 with { } 補上）。
    /// </summary>
    bool CanOvertimeWithoutClockOut = false,
    /// <summary>
    /// 現在是否已可打「加班開始」卡（時間規則）：一般上班日須在應下班時間（避開請假）與實際下班卡之後；
    /// 休假日 / 全日請假（<see cref="CanOvertimeWithoutClockOut"/>）恆為 true。
    /// 與 AttendanceHandler.OvertimeStartAsync 共用 ComputeOvertimeNotBefore，false 時前端應停用按鈕、不要按下才 400。
    /// </summary>
    bool CanStartOvertimeNow = true,
    /// <summary>尚不可打加班卡時，最早可打的時間（供按鈕提示「HH:mm 起可打」）；可打時為 null</summary>
    DateTime? OvertimeStartNotBefore = null,
    /// <summary>該日已被標記為出差，供打卡頁的勾選框帶回既有狀態</summary>
    bool IsBusinessTrip = false,

    // ── 四週彈性工時（切換日之前一律為預設值，前端據此維持舊行為）──────────
    /// <summary>四週彈性工時是否已對今日生效。false 時前端不顯示確認對話框、不判早退逾時。</summary>
    bool FlexibleEnabled = false,
    /// <summary>今日日別：work / rest_day / statutory_off / public_holiday</summary>
    string? DayType = null,
    /// <summary>本人是否為今日活動日的預定人力（國定假日據此解鎖上下班打卡）</summary>
    bool IsActivityAssignee = false,
    /// <summary>今日可否打上下班卡（日別鎖定的結果）。前端不自行重組規則，只吃這個旗標。</summary>
    bool CanClockInOut = true,
    /// <summary>不可打卡時的說明，可直接顯示給使用者</summary>
    string? ClockLockReason = null,
    /// <summary>
    /// 應下班時間 ＝ 實際上班打卡 ＋ 9 小時（請了上午半天假者為 ＋4 小時）；
    /// 設有自訂上下班時段者（賣店等）為當日固定下班時刻。
    /// 供前端在確認對話框顯示「目前出勤 X 小時 Y 分」與判斷是否要顯示原因欄位。
    /// 尚未打上班卡時為 null。
    /// </summary>
    DateTime? ExpectedClockOutTime = null,
    /// <summary>正常下班帶終點（應下班 ＋ 容許帶：公司預設 30 分、自訂上下班時段 5 分），晚於此即逾時。</summary>
    DateTime? NormalClockOutUntil  = null,
    /// <summary>
    /// 今日遲到界線（超過即為遲到、上班打卡須填遲到原因）：公司 09:30、自訂時段 S+2 分、
    /// 請上午半天假者 13:05（見 ClockRules.LateThreshold）。未切換時為 null。
    /// </summary>
    DateTime? LateAfter = null,
    /// <summary>
    /// 今日下班打卡填寫的專案工時佔比（依 SortOrder；無則為空陣列）。
    /// 不存在於 AttendanceRecords 欄位，由 AttendanceHandler.BuildTodayDtoAsync 以 with { } 補上。
    /// </summary>
    IReadOnlyList<ProjectAllocationDto>? ProjectAllocations = null,
    /// <summary>
    /// 今天下班打卡是否須填專案佔比（＝ 已到專案工時上線日，見 IProjectAllocationSchedule）。
    /// false 時前端維持上線前的下班打卡流程、不顯示今日專案工時卡片。
    /// </summary>
    bool ProjectAllocationRequired = false,
    /// <summary>
    /// 上班卡最晚可打的時間（當日有已核准請假時才有值）＝ 避開請假後的應出勤時段訖點；
    /// 整天被請假蓋滿時為當日 00:00（＝今日不開放上班打卡）。無請假為 null（不設限）。
    /// 與 AttendanceHandler.ClockInAsync 共用 ComputeClockInNotAfter，前端據此停用按鈕。
    /// </summary>
    DateTime? ClockInNotAfter = null);

/// <summary>
/// 出缺勤報表合併用的原料列：區間內已核准的假單（尚未逐日展開）。
/// 僅供 AttendanceLeaveMerger 內部使用，不直接回傳給前端。
/// </summary>
public sealed record AttendanceLeaveSourceRow(
    int      Id,
    Guid     UserId,
    string   UserName,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    bool     IsShiftWorker);

/// <summary>
/// 出缺勤報表合併用的原料列：區間內「應出勤」的員工母體（供缺勤虛擬列）。
/// 條件＝非超管 + 在職 + 持有 attendances:write（不打卡的角色不該被算成缺勤）。
/// </summary>
public sealed record AttendanceEmployeeRow(
    Guid      UserId,
    string    UserName,
    bool      IsShiftWorker,
    DateTime? HireDate,
    DateTime? ResignDate);

/// <summary>
/// 出缺勤報表合併用的原料列：已核准銷假的逐日紀錄（批次查詢結果）。
/// </summary>
public sealed record LeaveRevokedDateRow(
    int      LeaveRequestId,
    DateTime Date);

/// <summary>當下落在已核准請假時段內的請假資訊（供打卡阻擋訊息與前端提示）</summary>
public sealed record ActiveLeaveDto(
    int      Id,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate);

public sealed record ClockActionRequest(
    double? Latitude,
    double? Longitude,
    int?    OvertimeRequestId = null,
    /// <summary>本次打卡為出差：四個打卡動作皆以此值覆寫當日的 AttendanceRecord.IsBusinessTrip</summary>
    bool    IsBusinessTrip = false,
    /// <summary>
    /// 下班打卡的早退／逾時原因（四週彈性工時）。
    /// 早退或逾時且**非出差**時為必填，否則後端回 400；出差當日可填可不填。
    /// </summary>
    string? Reason = null,
    /// <summary>瀏覽器回報的定位精度（公尺），僅供稽核</summary>
    double? Accuracy = null,
    /// <summary>POST /attendances/clock-challenge 取得的一次性挑戰碼（防機器人打卡，必填）</summary>
    string? ChallengeToken = null,
    /// <summary>Cloudflare Turnstile token（防機器人打卡；是否必填依設定 Turnstile:Mode）</summary>
    string? TurnstileToken = null,
    /// <summary>
    /// 下班打卡的專案工時佔比（clock-out 必填：至少 1 筆、專案不重複、合計 100%，見 ProjectAllocationValidator）。
    /// 其他打卡動作忽略此欄。
    /// </summary>
    IReadOnlyList<ProjectAllocationInput>? ProjectAllocations = null);

/// <summary>專案工時佔比輸入（一列一專案）</summary>
public sealed record ProjectAllocationInput(int ProjectId, decimal Percentage);

/// <summary>PUT /attendances/today/project-allocations：當日整批替換</summary>
public sealed record UpdateProjectAllocationsRequest(IReadOnlyList<ProjectAllocationInput>? ProjectAllocations);

/// <summary>
/// GET /attendances/missing-project-allocations：本人「系統補下班卡、尚未填佔比」的日子（上線日之後、今天以前）。
/// </summary>
public sealed record MissingAllocationDayDto(
    int       Id,
    string    RecordDate,     // yyyy-MM-dd
    DateTime? ClockInTime,
    DateTime? ClockOutTime);

/// <summary>PUT /attendances/missing-project-allocations：同一份佔比一次補進多天（只能補一次）</summary>
public sealed record FillMissingAllocationsRequest(
    IReadOnlyList<int>?                    AttendanceIds,
    IReadOnlyList<ProjectAllocationInput>? ProjectAllocations);

/// <summary>專案工時佔比（回應，依 SortOrder）</summary>
public sealed record ProjectAllocationDto(
    int     ProjectId,
    string  ProjectCode,
    string  ProjectName,
    decimal Percentage);

/// <summary>取得打卡挑戰碼</summary>
public sealed record ClockChallengeRequest(string? Action);

/// <summary>打卡挑戰碼：簽發後須等 MinWaitMs 才可使用、ExpiresInMs 內有效（見 AttendancePunchGuard）</summary>
public sealed record ClockChallengeDto(
    string Token,
    int    MinWaitMs,
    int    ExpiresInMs);

/// <summary>修改出缺勤紀錄（四個時間欄位 + 備註；出差旗標僅由本人打卡時勾選，此處不開放）</summary>
public sealed record UpdateAttendanceRequest(
    DateTime? ClockInTime,
    DateTime? ClockOutTime,
    DateTime? OvertimeStartTime,
    DateTime? OvertimeEndTime,
    string?   Remark = null);
