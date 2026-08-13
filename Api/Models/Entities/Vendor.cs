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
    public string?  BankAccountName { get; set; }   // 匯款戶名（實際受款人，常與 Name 不同）
    public string?  BankName        { get; set; }   // 匯款銀行（含分行）
    public string?  BankCode        { get; set; }   // 銀行代號（保留原格式，農漁會為 xxx-xxxx）
    public string?  BankAccount   { get; set; }     // 銀行帳號
    public string?  BankBookImageUrl { get; set; }
    public string?  IdCardFrontUrl   { get; set; }   // 身分證正面（個人工作室）
    public string?  IdCardBackUrl    { get; set; }   // 身分證反面（個人工作室）
    public string?  Note          { get; set; }
    public bool     IsActive      { get; set; } = true;
    public DateTime CreatedAt     { get; set; } = Clock.Now;

    // Navigation
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
