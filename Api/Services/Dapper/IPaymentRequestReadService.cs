using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IPaymentRequestReadService
{
    Task<IEnumerable<PaymentRequestDto>> GetAllAsync();
    Task<PagedResult<PaymentRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<PaymentRequestDto?>             GetByIdAsync(int id);
    Task<IEnumerable<ApprovalTaskDto>>   GetApprovalTasksAsync(int? reviewerJobTitleId = null, int? reviewerDepartmentId = null, string? status = null, Guid? reviewerUserId = null, string? paymentStatus = null, string? applicationType = null, Guid? submittedByUserId = null, int? directorStepDeptId = null, bool directorScope = false, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    Task<IEnumerable<ApprovalTaskApplicantDto>> GetApprovalTaskApplicantsAsync();
    Task<ApprovalTaskDto?>               GetApprovalTaskByIdAsync(int id);
    Task<ApprovalTaskDto?>               GetApprovalTaskByIdAsync(int id, string applicationType);
}
