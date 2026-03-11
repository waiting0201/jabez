namespace Jabez.Api.Models.Dtos;

public sealed record RoleDto(
    string   Id,
    string   Name,
    string?  Description,
    string[] PermissionCodes,
    DateTime CreatedAt);

public sealed record CreateRoleRequest(
    string   Id,
    string   Name,
    string?  Description,
    string[] PermissionCodes);

public sealed record UpdateRoleRequest(
    string?   Name,
    string?   Description,
    string[]? PermissionCodes);
