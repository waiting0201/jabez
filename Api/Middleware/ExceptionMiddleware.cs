using Jabez.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jabez.Api.Middleware;

/// <summary>
/// 全域例外處理 Middleware。
/// 使用 IFunctionsWorkerMiddleware + context.GetHttpContext()
/// 因為採用 ConfigureFunctionsWebApplication（ASP.NET Core Integration）。
/// </summary>
public sealed class ExceptionMiddleware(ILogger<ExceptionMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (AppException appEx)
        {
            logger.LogWarning(appEx, "AppException [{StatusCode}]: {Message}", appEx.StatusCode, appEx.Message);
            await WriteErrorAsync(context, appEx.StatusCode, appEx.Message, appEx.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in [{Function}]", context.FunctionDefinition.Name);
            await WriteErrorAsync(context, 500, "An unexpected error occurred.", "Internal server error.");
        }
    }

    private static async Task WriteErrorAsync(
        FunctionContext context,
        int             statusCode,
        string          message,
        string          errorDetail)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null || httpContext.Response.HasStarted) return;

        var body = JsonSerializer.Serialize(ApiResponse.Fail(message, errorDetail), JsonOptions);
        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(body);
    }
}
