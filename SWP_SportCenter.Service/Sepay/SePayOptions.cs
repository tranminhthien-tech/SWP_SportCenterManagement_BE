namespace SWP_SportCenter.Service.Sepay;

// FIX SEPAY: THAY_THONG_TIN_SEPAY tại env/user-secrets sau; KHÔNG điền secret vào code.
public sealed class SePayOptions
{
    public const string SectionName = "SePay";
    public bool Enabled { get; set; } // THAY_THONG_TIN_SEPAY: SePay__Enabled=true khi cấu hình xong.
    public string BankBin { get; set; } = ""; // THAY_THONG_TIN_SEPAY: BIN hoặc short_name ngân hàng.
    public string AccountNumber { get; set; } = ""; // THAY_THONG_TIN_SEPAY: tài khoản nhận tiền hoặc VA.
    public string Gateway { get; set; } = ""; // THAY_THONG_TIN_SEPAY: gateway trong webhook, VD tên ngân hàng; tùy chọn.
    public string WebhookApiKey { get; set; } = ""; // THAY_THONG_TIN_SEPAY: API Key chọn khi tạo webhook, >=32 ký tự.
    public string QrTemplate { get; set; } = "compact";
}

// FIX SEPAY: chưa cấu hình không làm sập API khác; route SePay trả 503, không bỏ qua xác thực.
public sealed class SePayUnavailableException(string message) : Exception(message);
