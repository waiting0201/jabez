namespace Jabez.Api.Models.Dtos;

public sealed record PermissionDto(
    string  Id,
    string  Code,
    string  Name,
    string  Module,
    string? Description);

public sealed record CreatePermissionRequest(
    string  Code,
    string  Name,
    string  Module,
    string? Description = null);

public sealed record UpdatePermissionRequest(
    string?  Code,
    string?  Name,
    string?  Module,
    string?  Description);
