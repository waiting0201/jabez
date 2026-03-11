using Jabez.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

public sealed class HealthHandler
{
    public IActionResult Get() =>
        new OkObjectResult(ApiResponse.Ok(new
        {
            status    = "healthy",
            version   = "1.0.0",
            timestamp = DateTimeOffset.UtcNow,
            runtime   = ".NET 9 / Azure Functions v4 Isolated",
        }, "Service is healthy."));
}
