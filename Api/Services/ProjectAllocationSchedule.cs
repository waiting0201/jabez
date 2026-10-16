using Jabez.Api.Common;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Services;

/// <summary>
/// 「專案工時佔比」功能的上線日（app setting <c>ProjectAllocation:StartDate</c>，yyyy-MM-dd，各環境分開設定）。
/// 涵蓋下班打卡填佔比、當日修改、系統補卡日強制補填、工時總表。
///
/// - 未設定或格式錯誤 ＝ 功能不啟用（安全側：漏設不會突然要求全公司填佔比）。
/// - 上線日之前：下班打卡不收佔比、無修改入口、不追問補填、工時總表無資料。
/// - 上線日當天起自動生效，不需重新部署；補填與報表只算上線日（含）之後的紀錄。
/// </summary>
public interface IProjectAllocationSchedule
{
    /// <summary>上線日；null ＝ 未設定（不啟用）</summary>
    DateTime? StartDate { get; }

    /// <summary>指定日期是否已在上線日（含）之後</summary>
    bool IsActive(DateTime date);

    /// <summary>以今天判斷是否已上線</summary>
    bool IsActiveToday { get; }
}

public sealed class ProjectAllocationSchedule(IConfiguration config) : IProjectAllocationSchedule
{
    public DateTime? StartDate { get; } =
        DateTime.TryParse(config["ProjectAllocation:StartDate"], out var d) ? d.Date : null;

    public bool IsActive(DateTime date) => StartDate is { } s && date.Date >= s;

    public bool IsActiveToday => IsActive(Clock.Now);
}
