using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>           Users           { get; set; }
    public DbSet<Role>           Roles           { get; set; }
    public DbSet<Permission>     Permissions     { get; set; }
    public DbSet<UserRole>       UserRoles       { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken>   RefreshTokens   { get; set; }
    public DbSet<Department>     Departments     { get; set; }
    public DbSet<JobTitle>       JobTitles       { get; set; }
    public DbSet<ApprovalItem>   ApprovalItems   { get; set; }
    public DbSet<ApprovalStep>   ApprovalSteps   { get; set; }
    public DbSet<Project>                 Projects                 { get; set; }
    public DbSet<ProjectPaymentSchedule>  ProjectPaymentSchedules  { get; set; }
    public DbSet<PaymentRequest> PaymentRequests { get; set; }
    public DbSet<InvoiceItem>    InvoiceItems    { get; set; }
    public DbSet<LeaveRequest>   LeaveRequests   { get; set; }
    public DbSet<TravelRequest>     TravelRequests     { get; set; }
    public DbSet<TravelRequestItem> TravelRequestItems { get; set; }
    public DbSet<ApprovalRecord>   ApprovalRecords   { get; set; }
    public DbSet<OvertimeRequest>  OvertimeRequests  { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<SystemSetting>    SystemSettings    { get; set; }
    public DbSet<InsuranceBracket>    InsuranceBrackets    { get; set; }
    public DbSet<EscalationOverride>  EscalationOverrides  { get; set; }
    public DbSet<AdvanceRequest>       AdvanceRequests       { get; set; }
    public DbSet<AdvanceRequestItem>   AdvanceRequestItems   { get; set; }
    public DbSet<WriteOffRecord>       WriteOffRecords       { get; set; }
    public DbSet<WriteOffItem>         WriteOffItems         { get; set; }
    public DbSet<TravelWriteOffRecord> TravelWriteOffRecords { get; set; }
    public DbSet<TravelWriteOffItem>   TravelWriteOffItems   { get; set; }
    public DbSet<PayrollAdjustment>    PayrollAdjustments    { get; set; }
    public DbSet<RequestDesignatedReviewer> RequestDesignatedReviewers { get; set; }
    public DbSet<CalendarDay>                CalendarDays               { get; set; }
    public DbSet<TravelRequestParticipant>   TravelRequestParticipants  { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 自動套用 Configurations/ 下所有 IEntityTypeConfiguration<T>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
