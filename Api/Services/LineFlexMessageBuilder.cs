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
