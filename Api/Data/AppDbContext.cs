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
    public DbSet<Vendor>         Vendors         { get; set; }
    public DbSet<ApprovalItem>   ApprovalItems   { get; set; }
    public DbSet<ApprovalStep>   ApprovalSteps   { get; set; }
    public DbSet<ApprovalStepException> ApprovalStepExceptions { get; set; }
    public DbSet<ApprovalStepDesignatedJobTitle> ApprovalStepDesignatedJobTitles { get; set; }
    public DbSet<Project>                 Projects                 { get; set; }
    public DbSet<ProjectPaymentSchedule>  ProjectPaymentSchedules  { get; set; }
    public DbSet<PaymentRequest> PaymentRequests { get; set; }
    public DbSet<InvoiceItem>    InvoiceItems    { get; set; }
    public DbSet<PaymentRequestAttachment> PaymentRequestAttachments { get; set; }
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
    public DbSet<AdvanceRequestSupplement> AdvanceRequestSupplements { get; set; }
    public DbSet<WriteOffRecord>       WriteOffRecords       { get; set; }
    public DbSet<WriteOffItem>         WriteOffItems         { get; set; }
    public DbSet<WriteOffAttachment>   WriteOffAttachments   { get; set; }
    public DbSet<TravelWriteOffRecord> TravelWriteOffRecords { get; set; }
    public DbSet<TravelWriteOffItem>   TravelWriteOffItems   { get; set; }
    public DbSet<PayrollAdjustment>    PayrollAdjustments    { get; set; }
    public DbSet<RequestDesignatedReviewer> RequestDesignatedReviewers { get; set; }
    public DbSet<CalendarDay>                CalendarDays               { get; set; }
    public DbSet<TravelRequestParticipant>   TravelRequestParticipants  { get; set; }
    public DbSet<TravelRequestParticipantDate> TravelRequestParticipantDates { get; set; }
    public DbSet<TravelPaymentRequest>       TravelPaymentRequests      { get; set; }
    public DbSet<TravelPaymentRequestItem>   TravelPaymentRequestItems  { get; set; }
    public DbSet<AttendanceReminderLog>      AttendanceReminderLogs     { get; set; }
    public DbSet<PaymentReminderLog>         PaymentReminderLogs        { get; set; }

    // 分期撥款
    public DbSet<PaymentRequestInstallment>      PaymentRequestInstallments      { get; set; }
    public DbSet<AdvanceRequestInstallment>      AdvanceRequestInstallments      { get; set; }
    public DbSet<TravelRequestInstallment>       TravelRequestInstallments       { get; set; }
    public DbSet<TravelPaymentRequestInstallment> TravelPaymentRequestInstallments { get; set; }
    public DbSet<WriteOffInstallment>            WriteOffInstallments            { get; set; }

    // 預審申請
    public DbSet<PreReviewRequest>           PreReviewRequests           { get; set; }
    public DbSet<PreReviewItem>              PreReviewItems              { get; set; }
    public DbSet<PreReviewRequestAttachment> PreReviewRequestAttachments { get; set; }

    // HR 人事資料卡（1:1 EmployeeProfile + 9 子表）
    public DbSet<EmployeeProfile>           EmployeeProfiles            { get; set; }
    public DbSet<EducationRecord>           EducationRecords            { get; set; }
    public DbSet<EmploymentHistoryRecord>   EmploymentHistoryRecords    { get; set; }
    public DbSet<FamilyMember>             FamilyMembers               { get; set; }
    public DbSet<ProfessionalTraining>     ProfessionalTrainings       { get; set; }
    public DbSet<LanguageAbility>          LanguageAbilities           { get; set; }
    public DbSet<JobTransferRecord>        JobTransferRecords          { get; set; }
    public DbSet<RewardPunishmentRecord>   RewardPunishmentRecords     { get; set; }
    public DbSet<SalaryAdjustmentRecord>   SalaryAdjustmentRecords     { get; set; }
    public DbSet<HealthInsuranceDependent> HealthInsuranceDependents   { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 自動套用 Configurations/ 下所有 IEntityTypeConfiguration<T>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
