using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class Vendor
{
    public int      Id            { get; set; }
    public string   Name          { get; set; } = string.Empty;
    public string?  TaxId         { get; set; }
    public string?  IdNumber      { get; set; }   // 身分證字號（個人工作室 / 外包顧問，與 TaxId 擇一）
    public string?  Phone         { get; set; }
    public string?  ContactPerson { get; set; }
    public string?  Address       { get; set; }
    public string?  BankAccount   { get; set; }
    public string?  BankBookImageUrl { get; set; }
    public string?  IdCardFrontUrl   { get; set; }   // 身分證正面（個人工作室）
    public string?  IdCardBackUrl    { get; set; }   // 身分證反面（個人工作室）
    public string?  Note          { get; set; }
    public bool     IsActive      { get; set; } = true;
    public DateTime CreatedAt     { get; set; } = Clock.Now;

    // Navigation
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
