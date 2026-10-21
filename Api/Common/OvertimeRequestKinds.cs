namespace Jabez.Api.Common;

/// <summary>
/// 加班申請類別常數（2026-10）。取代原本依日期推導的 <c>OvertimeBackfill</c>（已退場）。
/// normal＝正常申請（事前）；post_hoc＝事後申請（已打下班卡，核准後自動寫入出缺勤加班欄）。
/// </summary>
public static class OvertimeRequestKinds
{
    public const string Normal  = "normal";
    public const string PostHoc = "post_hoc";

    /// <summary>未知值 / null 一律正規化為 normal（安全側：不會觸發自動寫入出缺勤）。</summary>
    public static string Normalize(string? value) =>
        string.Equals(value, PostHoc, StringComparison.OrdinalIgnoreCase) ? PostHoc : Normal;

    public static bool IsPostHoc(string? value) => Normalize(value) == PostHoc;
}
