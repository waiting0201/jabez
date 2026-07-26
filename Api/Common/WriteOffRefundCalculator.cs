namespace Jabez.Api.Common;

/// <summary>
/// 預支沖銷「本次應撥差額（RefundDue）」共用計算。
/// 沖銷金額累計超過預支總額時，超出的部分由公司補撥給員工；
/// 以「增額」而非「總超支」計算，讓每張沖銷單各自算得出、彼此不重疊，
/// 加總即等於整張預支單的超支總額，不需等到結案。
/// </summary>
public static class WriteOffRefundCalculator
{
    /// <param name="advanceGrandTotal">關聯預支單的總額（含追加批次）</param>
    /// <param name="otherWrittenOffTotal">本單之前（Id 較小）已核准沖銷單的金額加總</param>
    /// <param name="currentGrandTotal">本張沖銷單的金額</param>
    public static decimal Calculate(decimal advanceGrandTotal, decimal otherWrittenOffTotal, decimal currentGrandTotal)
    {
        var before = Math.Max(0m, otherWrittenOffTotal - advanceGrandTotal);
        var after  = Math.Max(0m, otherWrittenOffTotal + currentGrandTotal - advanceGrandTotal);
        return after - before;
    }
}
