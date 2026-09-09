namespace Jabez.Api.Services;

/// <summary>產生 LINE Flex Message JSON 物件（6 種簽核通知）。</summary>
public static class LineFlexMessageBuilder
{
    private const string BrandGreen = "#699F34";
    private const string WarningBrown = "#B8892A";
    private const string SuccessGreen = "#4A6B3A";
    private const string DangerRed = "#A04040";
    private const string TextPrimary = "#525358";
    private const string TextSecondary = "#6E6F73";

    /// <summary>待審核通知 — 通知審核者。</summary>
    public static object BuildReviewerMessage(
        string applicantName, string label, int applicationId,
        string summary, int stepOrder, string linkUrl)
    {
        return BuildBubble(
            altText: $"[待審核] {label} #{applicationId} — {applicantName}",
            headerColor: BrandGreen,
            headerText: "待審核通知",
            rows: new[]
            {
                ("申請人", applicantName),
                ("申請類型", label),
                ("申請編號", $"#{applicationId}"),
                ("摘要", summary),
                ("目前步驟", $"第 {stepOrder} 步"),
            },
            buttonLabel: "前往審核",
            buttonUrl: linkUrl);
    }

    /// <summary>審核結果通知 — 通知申請人。</summary>
    public static object BuildApplicantResultMessage(
        string label, int applicationId, string action, string? reviewNote, string linkUrl)
    {
        var (headerText, headerColor) = action switch
        {
            "approved" => ("已核准", SuccessGreen),
            "returned" => ("已退回", WarningBrown),
            "rejected" => ("已拒絕", DangerRed),
            _          => ("審核結果", BrandGreen),
        };

        var rows = new List<(string, string)>
        {
            ("申請類型", label),
            ("申請編號", $"#{applicationId}"),
            ("審核結果", headerText),
        };
        if (!string.IsNullOrWhiteSpace(reviewNote))
            rows.Add(("審核意見", reviewNote));

        return BuildBubble(
            altText: $"[{headerText}] 您的{label} #{applicationId}",
            headerColor: headerColor,
            headerText: $"{label} {headerText}",
            rows: rows.ToArray(),
            buttonLabel: "查看詳情",
            buttonUrl: linkUrl);
    }

    /// <summary>特定審核者通知（指定/升級/代理）。</summary>
    public static object BuildSpecificReviewerMessage(
        string applicantName, string label, int applicationId,
        string summary, string suffix, string linkUrl)
    {
        return BuildBubble(
            altText: $"[待審核] {label} #{applicationId} — {applicantName}（{suffix}）",
            headerColor: BrandGreen,
            headerText: $"待審核通知（{suffix}）",
            rows: new[]
            {
                ("申請人", applicantName),
                ("申請類型", label),
                ("申請編號", $"#{applicationId}"),
                ("摘要", summary),
            },
            buttonLabel: "前往審核",
            buttonUrl: linkUrl);
    }

    /// <summary>財務部撥款通知。</summary>
    public static object BuildFinanceDeptMessage(
        string applicantName, string label, int applicationId, string summary, string linkUrl)
    {
        return BuildBubble(
            altText: $"[可撥款] {label} #{applicationId} 已核准 — {applicantName}",
            headerColor: BrandGreen,
            headerText: $"{label}核准 — 可撥款",
            rows: new[]
            {
                ("申請人", applicantName),
                ("申請編號", $"#{applicationId}"),
                ("摘要", summary),
            },
            buttonLabel: "前往設定撥款日期",
            buttonUrl: linkUrl);
    }

    /// <summary>預支沖銷超額 — 通知財務部需匯款差額。</summary>
    public static object BuildRefundMessage(
        string applicantName, string requestNo, decimal advanceTotal,
        decimal refundAmount, string linkUrl)
    {
        return BuildBubble(
            altText: $"[需匯款] 預支沖銷超額 — 差額 {refundAmount:N0} 元",
            headerColor: WarningBrown,
            headerText: "預支沖銷超額 — 需匯款",
            rows: new[]
            {
                ("申請人", applicantName),
                ("預支單號", requestNo),
                ("預支金額", $"{advanceTotal:N0} 元"),
                ("應退差額", $"{refundAmount:N0} 元"),
            },
            buttonLabel: "查看詳情",
            buttonUrl: linkUrl);
    }

    /// <summary>撥款日將屆提醒 — 推送給財務人員的彙整通知（最多列前 5 筆）。</summary>
    public static object BuildUpcomingPaymentsMessage(
        string financeUserName,
        int    itemCount,
        IReadOnlyList<(string AppLabel, int ApplicationId, string Applicant, DateTime ExpectedDate, decimal Amount)> items,
        string linkUrl)
    {
        var rows = new List<(string, string)> { ("收件人", financeUserName), ("待撥筆數", $"{itemCount} 筆") };
        // 限制至多 5 筆，第 6 筆起以「⋯」表示
        var preview = items.Take(5).ToList();
        foreach (var (label, id, applicant, date, amount) in preview)
            rows.Add(($"{date:MM/dd}", $"{label} #{id} {applicant} {amount:N0}元"));
        if (items.Count > preview.Count)
            rows.Add(("⋯", $"另有 {items.Count - preview.Count} 筆，前往列表查看完整清單"));

        return BuildBubble(
            altText:     $"[撥款提醒] 您有 {itemCount} 筆預計撥款日將屆",
            headerColor: WarningBrown,
            headerText:  "撥款日將屆提醒",
            rows:        rows.ToArray(),
            buttonLabel: "查看待撥清單",
            buttonUrl:   linkUrl);
    }

    /// <summary>
    /// 打卡提醒 — 推播給尚未打該類型卡的員工。
    /// <paramref name="minutesUntil"/> 為「推播當下」距目標時刻的實際分鐘數，**可為 0 或負數**（＝目標時刻已過），
    /// 由呼叫端算出；負數時文案改為「已過 N 分鐘」。
    /// </summary>
    public static object BuildAttendanceReminderMessage(
        string reminderType, string userName, int minutesUntil, string workTime, string linkUrl)
    {
        var isClockIn   = reminderType == "clockIn";
        var headerText  = isClockIn ? "上班打卡提醒" : "下班打卡提醒";
        var action      = isClockIn ? "上班" : "下班";

        // minutesUntil 是「推播當下」到目標時刻的實際分鐘數，由呼叫端算出而非常數：
        // 命中窗有 30 分鐘（見 AttendanceReminderService.WindowMinutes），tick 延遲時
        // 目標時刻可能已經過了，寫死「再 2 分鐘」會變成 09:25 推播卻說「再 2 分鐘上班」。
        var overdue     = minutesUntil <= 0;
        var altText     = overdue
            ? $"[打卡提醒] {action}時間（{workTime}）已過 — {userName}"
            : $"[打卡提醒] 再 {minutesUntil} 分鐘{action}（{workTime}）— {userName}";
        var timeLabel   = overdue ? "目前狀態" : "剩餘時間";
        var timeValue   = overdue ? $"已過 {-minutesUntil} 分鐘" : $"{minutesUntil} 分鐘";
        var tip         = (isClockIn, overdue) switch
        {
            (true,  false) => "記得上班後打卡，開始新的一天",
            (true,  true)  => "還沒打卡的話請儘快打卡",
            (false, false) => "記得下班前打卡，別忘了喔",
            (false, true)  => "還沒打卡的話請記得補打下班卡",
        };

        return BuildBubble(
            altText:      altText,
            headerColor:  BrandGreen,
            headerText:   headerText,
            rows: new[]
            {
                ("員工",     userName),
                ("目標時刻", workTime),
                (timeLabel,  timeValue),
                ("提醒",     tip),
            },
            buttonLabel: "前往打卡",
            buttonUrl:   linkUrl);
    }

    /// <summary>撥款完成通知 — 通知申請人款項已撥付。分期撥款情境下，標題附「第 N/M 期」。</summary>
    public static object BuildApplicantPaidMessage(
        string label, int applicationId, decimal amount, DateTime paidAt, string linkUrl,
        int? installmentNo = null, int? totalInstallments = null)
    {
        var installmentLabel = installmentNo.HasValue && totalInstallments.HasValue
            ? $"第 {installmentNo}/{totalInstallments} 期"
            : "";
        var titleSuffix = string.IsNullOrEmpty(installmentLabel) ? "" : $"（{installmentLabel}）";
        var rows = new List<(string, string)>
        {
            ("申請類型", label),
            ("申請編號", $"#{applicationId}"),
        };
        if (!string.IsNullOrEmpty(installmentLabel))
            rows.Add(("撥款期數", installmentLabel));
        rows.Add(("撥款金額", $"{amount:N0} 元"));
        rows.Add(("撥款日期", paidAt.ToString("yyyy-MM-dd")));

        return BuildBubble(
            altText: $"[已撥款] 您的{label} #{applicationId} 已撥款 — {amount:N0} 元{titleSuffix}",
            headerColor: SuccessGreen,
            headerText: $"{label}已撥款{titleSuffix}",
            rows: rows.ToArray(),
            buttonLabel: "查看詳情",
            buttonUrl: linkUrl);
    }

    /// <summary>退款完成通知 — 通知申請人退款已匯款。</summary>
    public static object BuildApplicantRefundedMessage(
        string label, int applicationId, decimal refundAmount, DateTime refundedAt, string linkUrl)
    {
        return BuildBubble(
            altText: $"[已退款] 您的{label} #{applicationId} 退款已匯款 — {refundAmount:N0} 元",
            headerColor: SuccessGreen,
            headerText: $"{label}退款完成",
            rows: new[]
            {
                ("申請類型", label),
                ("申請編號", $"#{applicationId}"),
                ("退款金額", $"{refundAmount:N0} 元"),
                ("退款日期", refundedAt.ToString("yyyy-MM-dd")),
            },
            buttonLabel: "查看詳情",
            buttonUrl: linkUrl);
    }

    /// <summary>出差沖銷超額 — 通知財務部需匯款差額。</summary>
    public static object BuildTravelRefundMessage(
        string applicantName, string destination, decimal travelTotal,
        decimal refundAmount, string linkUrl)
    {
        return BuildBubble(
            altText: $"[需匯款] 出差沖銷超額 — 差額 {refundAmount:N0} 元",
            headerColor: WarningBrown,
            headerText: "出差沖銷超額 — 需匯款",
            rows: new[]
            {
                ("申請人", applicantName),
                ("出差地點", destination),
                ("出差金額", $"{travelTotal:N0} 元"),
                ("應退差額", $"{refundAmount:N0} 元"),
            },
            buttonLabel: "查看詳情",
            buttonUrl: linkUrl);
    }

    // ── Flex Message Bubble 共用模板 ────────────────────────────────────────

    private static object BuildBubble(
        string altText, string headerColor, string headerText,
        (string label, string value)[] rows, string buttonLabel, string buttonUrl)
    {
        var bodyContents = new List<object>();
        foreach (var (label, value) in rows)
        {
            bodyContents.Add(new
            {
                type = "box",
                layout = "horizontal",
                contents = new object[]
                {
                    new { type = "text", text = label, size = "sm", color = TextSecondary, flex = 0, wrap = true },
                    new { type = "text", text = value, size = "sm", color = TextPrimary, weight = "bold", flex = 2, wrap = true },
                },
                margin = "lg"
            });
        }

        return new
        {
            type = "flex",
            altText,
            contents = new
            {
                type = "bubble",
                header = new
                {
                    type = "box",
                    layout = "vertical",
                    backgroundColor = headerColor,
                    paddingAll = "16px",
                    contents = new object[]
                    {
                        new { type = "text", text = headerText, color = "#FFFFFF", weight = "bold", size = "lg" },
                    }
                },
                body = new
                {
                    type = "box",
                    layout = "vertical",
                    paddingAll = "20px",
                    contents = bodyContents.ToArray()
                },
                footer = new
                {
                    type = "box",
                    layout = "vertical",
                    paddingAll = "12px",
                    contents = new object[]
                    {
                        new
                        {
                            type = "button",
                            action = new { type = "uri", label = buttonLabel, uri = buttonUrl },
                            style = "primary",
                            color = headerColor,
                            height = "sm"
                        }
                    }
                }
            }
        };
    }
}
