using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Exceptions;
using SWP_SportCenter.Service.Payment;

namespace SWP_SportCenter.Service.Sepay;

// FIX SEPAY: thay Order/Promotion của Juratifact bằng Invoice/Membership SWP.
public sealed class SepayService(AppDbContext db, IOptionsSnapshot<SePayOptions> options,
    IHttpContextAccessor http, TimeProvider clock, ILogger<SepayService> logger) : ISepayService
{
    private static readonly Regex PaymentCode = new(@"(?<![A-Z0-9])SWP[0-9A-F]{32}(?![A-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    public bool IsEnabled => options.Value.Enabled;

    private SePayOptions Ready()
    {
        var c = options.Value;
        if (!c.Enabled) throw new SePayUnavailableException("SePay chưa được bật. Xem THAY_THONG_TIN_SEPAY.");
        if (string.IsNullOrWhiteSpace(c.BankBin) || c.BankBin.Length > 64 ||
            string.IsNullOrWhiteSpace(c.AccountNumber) || c.AccountNumber.Length > 64 ||
            string.IsNullOrWhiteSpace(c.WebhookApiKey) || c.WebhookApiKey.Length < 32 || c.WebhookApiKey.Length > 512 ||
            (c.Gateway?.Length ?? 0) > 100 || c.QrTemplate is not ("" or "compact" or "qronly" or "standee"))
            throw new SePayUnavailableException("SePay thiếu/sai BankBin, AccountNumber, WebhookApiKey hoặc QrTemplate. Xem THAY_THONG_TIN_SEPAY.");
        return c;
    }
    public bool VerifyWebhookAuthorization(string? authorization)
    {
        var c = Ready();
        const string scheme = "Apikey ";
        if (authorization == null || authorization.Length > 1024 || !authorization.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return false;
        // FIX SEPAY: khóa bí mật chỉ so ở server, không log hoặc trả về DTO.
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(c.WebhookApiKey)),
            SHA256.HashData(Encoding.UTF8.GetBytes(authorization[scheme.Length..])));
    }
    public Task<string> GenerateQrCode(decimal amount, string referenceCode)
    {
        var c = Ready();
        if (amount <= 0 || amount != decimal.Truncate(amount) || amount > 999999999999999m)
            throw new BusinessException("Số tiền SePay phải là số nguyên VND dương trong giới hạn hỗ trợ.");
        if (string.IsNullOrEmpty(referenceCode) || PaymentCode.Match(referenceCode).Value != referenceCode)
            throw new BusinessException("Mã thanh toán SWP không hợp lệ.");
        // CODE CŨ từ file nhóm: nội suy amount theo culture và không URL encode tham số.
        // FIX SEPAY: URL ảnh QR cố định HTTPS; không có API Key hay returnUrl trong URL.
        return Task.FromResult("https://qr.sepay.vn/img?bank=" + Uri.EscapeDataString(c.BankBin.Trim()) +
            "&acc=" + Uri.EscapeDataString(c.AccountNumber.Trim()) + "&template=" + Uri.EscapeDataString(c.QrTemplate) +
            "&amount=" + amount.ToString("0", CultureInfo.InvariantCulture) + "&des=" + Uri.EscapeDataString(referenceCode));
    }
    public async Task PrepareInvoiceAsync(Repository.Entity.Invoice invoice)
    {
        var c = Ready();
        if (invoice.PaymentMethod != PaymentMethod.BankTransfer)
            throw new BusinessException("SePay chỉ hỗ trợ chuyển khoản ngân hàng, chưa hỗ trợ thanh toán thẻ.");
        invoice.PaymentReference = "SWP" + invoice.Id.ToString("N").ToUpperInvariant();
        invoice.SePayQrCodeUrl = await GenerateQrCode(invoice.Amount, invoice.PaymentReference);
        // FIX SEPAY: đóng băng ngân hàng nhận của hóa đơn; đổi config không đổi QR hóa đơn đang chờ.
        invoice.SePayBankBin = c.BankBin.Trim();
        invoice.SePayAccountNumber = c.AccountNumber.Trim();
        invoice.SePayGateway = string.IsNullOrWhiteSpace(c.Gateway) ? null : c.Gateway.Trim();
    }
    public async Task<bool> ProcessSePayWebhook(Request.SepayWebhookDto data)
    {
        // FIX SEPAY: kiểm tra lại trong service, không cho caller bỏ qua auth ở Controller.
        if (!VerifyWebhookAuthorization(http.HttpContext?.Request.Headers["Authorization"].ToString()))
            throw new UnauthorizedAccessException("Webhook SePay không được xác thực.");
        Validator.ValidateObject(data, new ValidationContext(data), true);
        if (data.TransferAmount != decimal.Truncate(data.TransferAmount)) throw new ArgumentException("SePay chỉ hỗ trợ số nguyên VND.");
        if (string.Equals(data.TransferType, "out", StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(data.TransferType, "in", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("transferType phải là in hoặc out.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data))));
        // FIX SEPAY: cùng isolation với confirm thủ công/expiration, invoice + thẻ + receipt cùng commit.
        await using var tx = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var previous = await db.Set<SePayWebhookReceipt>().AsNoTracking().SingleOrDefaultAsync(r => r.Id == data.Id);
        if (previous != null)
        {
            if (previous.PayloadHash != hash) throw new ConflictException("Id webhook đã được ghi nhận với dữ liệu khác.");
            return true; // retry không thay ngày trả, không gia hạn/kích hoạt lần hai.
        }
        var receipt = new SePayWebhookReceipt { Id = data.Id, TransferAmount = data.TransferAmount,
            AccountNumber = data.AccountNumber, SubAccount = data.SubAccount, Gateway = data.Gateway,
            BankReferenceCode = data.ReferenceCode, BankTransactionDate = data.TransactionDate, Content = data.Content,
            PayloadHash = hash, ReceivedAt = clock.GetUtcNow(), Outcome = "Unmatched" };
        // CODE CŨ từ file nhóm: ILike("%" + reference + "%") rồi lấy giao dịch mới nhất.
        // FIX SEPAY: tìm đúng mã đủ 35 ký tự, không match một phần/LIKE wildcard hoặc bank ReferenceCode.
        var codes = PaymentCode.Matches((data.Code ?? "") + " " + (data.Content ?? ""))
            .Select(m => m.Value.ToUpperInvariant()).Distinct().ToArray();
        // CODE CŨ: chỉ Include Membership.Member.Account, hóa đơn lớp gây null.
        var invoice = codes.Length == 1 ? await PaymentQueries.Graph(db)
            .SingleOrDefaultAsync(i => i.PaymentReference == codes[0]) : null;
        if (codes.Length > 1) receipt.Outcome = "AmbiguousReference";
        if (invoice != null)
        {
            receipt.InvoiceId = invoice.Id;
            var receiverMatches = !string.IsNullOrEmpty(invoice.SePayAccountNumber) &&
                (data.AccountNumber.Trim() == invoice.SePayAccountNumber || data.SubAccount?.Trim() == invoice.SePayAccountNumber);
            if (!receiverMatches || (!string.IsNullOrEmpty(invoice.SePayGateway) &&
                    !string.Equals(data.Gateway?.Trim(), invoice.SePayGateway, StringComparison.OrdinalIgnoreCase)))
                receipt.Outcome = "WrongReceiver";
            // CODE CŨ từ file nhóm: tiền thiếu đổi Failed; tiền dư vẫn coi thành công.
            // FIX SEPAY: tiền không khớp giữ Pending, lưu receipt đối soát; không cộng dồn/hoàn tiền tự động.
            else if (data.TransferAmount != invoice.Amount) receipt.Outcome = "AmountMismatch";
            else if (invoice.Status == InvoiceStatus.Paid) receipt.Outcome = "AlreadyPaid";
            else if (invoice.PaymentMethod != PaymentMethod.BankTransfer || await PaymentSettlement.RejectionReasonAsync(db, invoice, receipt.ReceivedAt) != null)
                receipt.Outcome = "RejectedState";
            else
            {
                PaymentSettlement.Apply(invoice, receipt.ReceivedAt, receptionistId: null, db); // FIX B53 B55 B57
                receipt.Outcome = "Paid";
            }
        }
        db.Set<SePayWebhookReceipt>().Add(receipt);
        await db.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
        if (receipt.Outcome != "Paid") logger.LogWarning("SePay receipt {EventId}: {Outcome}; invoice {InvoiceId}. Cần đối soát nếu đã nhận tiền.", receipt.Id, receipt.Outcome, receipt.InvoiceId);
        return true; // ACK đã ghi nhận, KHÔNG đồng nghĩa invoice đã Paid; FE phải đọc payment status.
    }
}
