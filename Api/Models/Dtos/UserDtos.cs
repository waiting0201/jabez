namespace Jabez.Api.Models.Dtos;

public sealed record UserDto(
    Guid      Id,
    string    Name,
    string    Email,
    string?   Avatar,
    string[]  RoleIds,
    string    Status,
    int?      DepartmentId,
    string?   DepartmentName,
    int?      JobTitleId,
    string?   JobTitleName,
    DateTime? HireDate,
    DateTime? ResignDate,
    decimal?  BaseSalary,
    Guid?     AgentUserId,
    string?   AgentName,
    DateTime  CreatedAt);

public sealed record CreateUserRequest(
    string    Name,
    string    Email,
    string    Password,
    string?   Avatar,
    string[]  RoleIds,
    string    Status      = "active",
    int?      DepartmentId = null,
    int?      JobTitleId   = null,
    DateTime? HireDate     = null,
    DateTime? ResignDate   = null,
    decimal?  BaseSalary   = null,
    Guid?     AgentUserId  = null);

public sealed record UpdateUserRequest(
    string?    Name,
    string?    Email,
    string?    Password,
    string?    Avatar,
    string[]?  RoleIds,
    string?    Status,
    int?       DepartmentId,
    int?       JobTitleId,
    DateTime?  HireDate,
    DateTime?  ResignDate,
    decimal?   BaseSalary,
    Guid?      AgentUserId);
