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
        catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("Incorrect Content-Type", StringComparison.OrdinalIgnoreCase))
        {
            // ReadFormAsync 對非 multipart/form-data 或 x-www-form-urlencoded 請求拋此例外
            logger.LogWarning(ioEx, "Form parse failed in [{Function}]: {Message}", context.FunctionDefinition.Name, ioEx.Message);
            await WriteErrorAsync(context, 400, "請求需為 multipart/form-data 或 application/x-www-form-urlencoded。", ioEx.Message);
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
