namespace Jabez.Api.Services;

/// <summary>
/// 升級審核結果
/// </summary>
/// <param name="ReviewerId">實際審核者 ID</param>
/// <param name="OnBehalfOfUserId">代理情境：代替哪位原審核者（null 表示非代理）</param>
/// <param name="IsEscalated">是否為升級審核</param>
public sealed record EscalationResult(
    Guid  ReviewerId,
    Guid? OnBehalfOfUserId,
    bool  IsEscalated);
