# SePay cho SWP SportCenter

## Luồng đã tích hợp

Code tham khảo của nhóm được chuyển từ `Juratifact.Order/Promotion/Transaction` sang
`SWP.Invoice/Membership`. Không thêm các bảng Order, Product hoặc Promotion không thuộc SWP.

1. Member gọi `POST /api/payments/checkout`, `paymentMethod = 2`.
2. Khi SePay bật và cấu hình hợp lệ, API tạo Invoice Pending + Membership Inactive,
   mã `SWP` + 32 ký tự hex, hạn thanh toán 15 phút và ảnh QR.
3. FE hiển thị `qrCodeUrl`, số tiền, `paymentReference`, `bankBin`, `bankAccountNumber`, `expiresAt`.
   `paymentUrl` cũng là **URL ảnh QR**, không phải trang redirect thanh toán.
4. Member chuyển khoản đúng nội dung và đúng số tiền. SePay gửi webhook có API Key.
5. Server xác minh và lưu Invoice Paid + Membership Active + receipt **cùng transaction**.
6. FE đọc `GET /api/payments/{paymentId}` (poll khoảng 3–5 giây khi chờ, dừng khi Paid/hết hạn).
   Không dùng việc mở ảnh QR, `returnUrl`, hay redirect FE làm bằng chứng thanh toán.

`paymentMethod = 3` bị từ chối khi SePay bật: tích hợp này là chuyển khoản VietQR,
không phải cổng thanh toán thẻ. Thanh toán nội bộ không tự đổi sang SePay.

## THAY_THONG_TIN_SEPAY

**Chưa sửa bất kỳ file appsettings nào. Không điền API Key vào code hoặc commit secret.**
Tìm chuỗi `THAY_THONG_TIN_SEPAY` trong:

- `SWP_SportCenter.Service/Sepay/SePayOptions.cs`: các trường cấu hình cần điền.
- `SWP_SportCenter.Api/Program.cs`: đăng ký options, service và nguồn cấu hình.
- `SWP_SportCenter.Api/Controllers/SePayController.cs`: URL webhook cần khai báo.

Khi sẵn sàng, thêm qua Render Environment hoặc user-secrets/env local:

```dotenv
# THAY_THONG_TIN_SEPAY: chỉ bật sau khi đã cấu hình đủ và chạy migration
SePay__Enabled=true
# THAY_THONG_TIN_SEPAY: BIN hoặc short_name/alias/code của ngân hàng nhận
SePay__BankBin=THAY_BIN_HOAC_TEN_NGAN_HANG
# THAY_THONG_TIN_SEPAY: số tài khoản nhận, giữ nguyên số 0 đầu
SePay__AccountNumber=THAY_SO_TAI_KHOAN
# THAY_THONG_TIN_SEPAY: API Key của WEBHOOK, không phải JWT hoặc API token truy vấn SePay
SePay__WebhookApiKey=THAY_KHOA_NGAU_NHIEN_IT_NHAT_32_KY_TU
# THAY_THONG_TIN_SEPAY: tùy chọn, đúng tên gateway nhận trong payload thực tế
SePay__Gateway=THAY_GATEWAY_NGAN_HANG
SePay__QrTemplate=compact
```

Không dùng nguyên chuỗi `THAY_...`. Gateway có thể bỏ nếu chưa xác định; nếu điền sai
thì tiền nhận sẽ bị đưa vào đối soát `WrongReceiver`, không tự kích hoạt thẻ.
Xem thêm [nghiệp vụ lớp và nâng cấp mới](README_WORKFLOWS.md) cho hợp đồng 29/10 và hoàn thủ công.
Để tắt, dùng `SePay__Enabled=false`. Mặc định chưa có cấu hình là **tắt**,
checkout cũ không có QR và webhook trả 503; không có chế độ webhook bỏ xác thực.

QR và tài khoản nhận được chụp lại trên từng Invoice. Đổi ngân hàng trong cấu hình
không đổi chỉ dẫn của Invoice cũ. Hãy xử lý hết/hủy các giao dịch đang chờ trước khi
thay tài khoản hoặc tắt webhook; nếu đã nhận tiền sau khi hết hạn cần đối soát.

## Thiết lập trên dashboard SePay

- Kết nối tài khoản ngân hàng nhận của nhóm, khớp AccountNumber.
- Tạo webhook HTTP POST JSON, sự kiện **Có tiền vào**, lọc đúng tài khoản ngân hàng.
- **THAY_THONG_TIN_SEPAY** URL: `https://DOMAIN_API_CUA_BAN/api/sepay/webhook`.
- Chọn chứng thực **API Key**, nhập đúng khóa đã lưu tại `SePay__WebhookApiKey`.
- SePay phải gửi `Authorization: Apikey <KHÓA_WEBHOOK>`; không chọn No Authentication,
  không dùng Bearer JWT người dùng. HMAC là phương thức khác và chưa triển khai ở đợt này.
- Giữ nội dung chuyển khoản nguyên vẹn. Mã có dạng `SWP` + 32 ký tự hex.
  Có thể cấu hình tiền tố SWP; không áp dụng bộ lọc khiến mất webhook của mã này.
  `referenceCode` trong payload là mã ngân hàng, **không phải** mã SWP.
- Nếu dùng VA, server chấp nhận AccountNumber hoặc SubAccount khớp tài khoản đã chụp
  trên Invoice. Một số ngân hàng yêu cầu nội dung cố định; cần thử tài khoản cụ thể trước
  khi dùng thực tế, không thay nội dung mã SWP bằng một câu tùy ý.

Request mẫu chỉ để hiểu cấu trúc, **không chứa khóa thật**:

```json
{
  "id": 123456789,
  "gateway": "TEN_GATEWAY_THUC_TE",
  "transactionDate": "2026-10-29 14:30:00",
  "accountNumber": "SO_TAI_KHOAN_NHAN",
  "subAccount": "",
  "code": "SWP0123456789ABCDEF0123456789ABCDEF",
  "content": "SWP0123456789ABCDEF0123456789ABCDEF",
  "transferType": "in",
  "transferAmount": 100000,
  "referenceCode": "MA_GIAO_DICH_NGAN_HANG"
}
```

Phải dùng mã và số tiền do checkout trả về. Payload test dashboard có `id = 0` sẽ bị
400 theo chủ ý để không kích hoạt thẻ bằng event mẫu không có ID định danh.
Kiểm tra E2E bằng một giao dịch có ID dương duy nhất trong sandbox/tài khoản thử.

## Bảo vệ tài chính và đối soát

- Chỉ tiền vào `transferType = in` mới xét thanh toán; `out` chỉ ACK, không kích hoạt.
- Thiếu/sai API Key trả 401; JWT staff/member không thay thế API Key.
- Chưa cấu hình/bị tắt trả 503. Body không hợp lệ trả 400.
- Mã SWP phải đủ 35 ký tự, không match tiền tố/một phần, không dùng LIKE wildcard.
  Nhiều mã trong cùng nội dung bị ghi `AmbiguousReference`, không tự chọn hóa đơn.
- Số tiền phải **bằng chính xác** giá Invoice, là số nguyên VND; thiếu/dư lưu
  `AmountMismatch`, không đổi hóa đơn sang Paid và không cộng dồn nhiều lần chuyển.
  Đây là thay đổi an toàn so với code mẫu nhóm cho phép tiền dư tự thành công.
- Hóa đơn đã hủy, hết TTL, thẻ đã kích hoạt/hủy hoặc tài khoản không hoạt động:
  ghi `RejectedState`; không phục hồi đơn đã hủy. Tiền chuyển muộn cần xử lý thủ công.
- Event ID là khóa chính `sepay_webhook_receipt.Id`. Gửi lại cùng payload là idempotent;
  cùng ID nhưng payload khác trả 409. Event mới trả cho Invoice đã Paid ghi `AlreadyPaid`,
  không đổi PaymentDate và không gia hạn lại thẻ.
- Transaction Serializable bảo vệ khi webhook đụng confirm nội bộ/worker hết hạn.
  Unique/serialization conflict có thể trả 409, SePay retry; không nuốt lỗi DB thành 200.
- Sau khi ghi receipt thành công, trả `200 {"success":true}` kể cả receipt cần đối soát.
  **success là ACK đã nhận, không phải đã thanh toán.** FE luôn đọc payment status.
  Nếu response 400/401/409/503 thì không phải ACK thành công.
- Sổ `sepay_webhook_receipt` lưu event ID, invoice ID nếu tìm được, số tiền, tài khoản,
  mã ngân hàng, ngày giao dịch gốc, nội dung, hash payload và Outcome. Không lưu API Key.
  Quản lý đối soát các outcome khác `Paid`; không xóa hóa đơn có receipt tham chiếu.
- `PaymentDate` là thời điểm server xác nhận; ngày giao dịch gốc được giữ riêng để đối chiếu.
  Không tự lùi ngày hiệu lực thẻ hoặc gia hạn theo dữ liệu webhook.
- API xác nhận nội bộ vẫn chỉ cho Manager/Receptionist như B39. Webhook không giả mạo
  user staff để gọi `ConfirmPaymentAsync`. Mock gateway GET cũ vẫn 410, không nhận tiền giả.

API Key chỉ đặt trên server/SePay dashboard. Tuyệt đối không đưa vào FE, URL QR,
JSON checkout hoặc log. Luôn dùng HTTPS; nên chuyển sang HMAC nếu nhóm cần mức bảo vệ cao hơn.

## Migration và kiểm tra

Có migration mới `20261029101049_AddSePayPayments`: cột snapshot trên invoice, unique payment reference,
bảng receipt với event ID unique và FK Restrict. Không sửa Paid/Inactive của dữ liệu cũ.
Chưa tự chạy lên DB Render, chưa commit/push/deploy.

Sau khi review và backup đúng DB:

```powershell
dotnet ef database update --project SWP_SportCenter.Repository --startup-project SWP_SportCenter.Api --context AppDbContext
```

Kiểm tra tự động không gọi ngân hàng hoặc cloud thật:

Kết quả lịch sử trước đổi nghiệp vụ: 311 kiểm tra hợp đồng cũ đã qua. Sau đợt 29/10,
bộ mặc định chuyển sang 104 kiểm tra workflow mới; code bộ cũ giữ sau `--legacy-contract`
để đối chiếu, không phải hợp đồng nghiệm thu hiện tại. Xem README_WORKFLOWS.md.
Chưa kiểm tra tiền thật hoặc thực thi migration trên PostgreSQL Render;
không coi việc sinh SQL là đã thử concurrency trên PostgreSQL.

```powershell
dotnet build tests/SportCenter.Regression/SportCenter.Regression.csproj --no-restore -p:BuildInParallel=false -m:1 -p:UseSharedCompilation=false -nodeReuse:false
dotnet tests/SportCenter.Regression/bin/Debug/net8.0/SportCenter.Regression.dll
```

Sau deploy cần kiểm tra thật: QR đọc đúng ngân hàng/số tiền/nội dung; tiền vào đúng tài khoản;
webhook có API Key được nhận; Invoice Paid và thẻ Active; FE cập nhật khi polling; retry
không tạo thêm thanh toán. Bộ test InMemory và sinh SQL không thay thế thử PostgreSQL/SePay thật.

## Tài liệu chính thức đã đối chiếu

- [Webhook payload và ACK](https://docs.sepay.vn/tich-hop-webhooks.html)
- [Chứng thực API Key và HMAC](https://developer.sepay.vn/en/sepay-webhooks/xac-thuc)
- [Tham số QR](https://developer.sepay.vn/en/tien-ich-khac/tao-qr-code)
