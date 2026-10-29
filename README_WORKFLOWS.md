# Nghiệp vụ học phí lớp nâng cấp gói và AI

Đợt 29/10/2026 thay quy tắc B45: thẻ dùng tự tập và giảm học phí, lớp thu phí riêng.
Không tích hợp MoMo. SePay vẫn nhận chuyển khoản và gọi `/api/sepay/webhook` bằng API Key.
Giữ code thay thế trong comment `CODE CŨ`; code mới đánh dấu `FIX`.

## Hợp đồng FE cần cập nhật

| API | Quyền | Dữ liệu và kết quả |
| --- | --- | --- |
| GET `/api/payments/options` | Công khai | `provider: SePay`, `methods: [2]`, `holdMinutes: 15`, `upgrade: true`; 503 nếu chưa bật |
| POST `/api/payments/checkout` | Member | Mua gói; một đơn gói hoặc nâng cấp đang chờ mỗi Member |
| POST `/api/payments/class-checkout` | Member | `{ classId, paymentMethod: 2 }`; server tính học phí, giảm giá, giữ chỗ 15 phút |
| POST `/api/payments/upgrade-checkout` | Member | `{ membershipId, membershipPackageId, paymentMethod: 2 }`; server tính credit và khoản phải trả |
| GET `/api/payments/{id}` | Chủ đơn hoặc staff được đọc | `kind`, `classId`, `bookingId`, `packageId`, giá/credit, QR, hạn, trạng thái, thông tin hoàn |
| POST `/api/payments/{id}/cancel` | Chủ đơn Member | Chỉ Pending, hủy và trả chỗ, giữ lịch sử |
| POST `/api/class-bookings` | Receptionist | `{ memberId, classId, paymentMethod: 1/2/3 }`; hóa đơn Paid tại quầy, collector từ token |
| DELETE `/api/class-bookings/{id}/member/{memberId}` | Chủ lượt hoặc Receptionist | Hủy mềm, trả `refunded`, `refundPending`, `refundAmount`, `requestedRefundAmount` |
| POST `/api/payments/{id}/refund-confirm` | Receptionist | `{ reference: "mã chuyển trả/biên nhận" }`; chỉ ghi nhận SAU khi đã thực trả |
| POST `/api/ai/chat` | Đã đăng nhập | `{ message, history: [{ role: user/assistant, content }] }` → `reply`, `provider`, `disclaimer` |
| POST `/api/ai/exercise-suggestions` | Coach | `{ goal, durationMinutes, level, equipment, memberId? }` → `reply`, `suggestions`, `disclaimer` |

**FE không được thấy GET options 200 rồi mặc định dùng MoMo 4/5.** Phải đọc `provider` và `methods`.
`paymentUrl`/`payUrl` hiện là URL ảnh QR SePay, không phải trang redirect thanh toán.
FE dùng `qrCodeUrl` hiển thị ảnh và poll GET payment; không tin URL return có `status=paid`.
`returnUrl` không được dùng để đổi trạng thái hay điều hướng sang URL tùy ý.

Response checkout giữ các trường cũ, thêm `kind` (Membership/Class/Upgrade), `bookingId`,
`originalAmount`, `discountPercent`, `creditAmount`, `fromPackageName`. `membershipId` có thể
null đối với hóa đơn lớp. Lớp giá 0 trả `status: 2`, `qrCodeUrl: null`, Booked ngay, không cần thẻ.
Khoản phải trả và credit KHÔNG lấy từ request. Đơn trùng trả 409 kèm `paymentId` của chính chủ.

## Trạng thái không dùng chung bảng mã

- Booking: Booked 1, Attended 2, Cancelled 3, PendingPayment 4.
- Payment DTO: Pending 1, Paid 2, Cancelled 4, Expired 5, Refunded 6.
- Invoice enum: Pending 1, Paid 2, Cancelled 3, Refunded 4.

PendingPayment còn hạn chiếm chỗ nhưng chưa được điểm danh, chưa vào roster/nhận bài tập.
Lớp phải Scheduled, có lịch và buổi sớm nhất CHƯA bắt đầu theo UTC+7.
Worker cập nhật Ongoing/Completed mỗi phút, nhưng guard không phụ thuộc worker chạy kịp.

## Hủy và hoàn tiền SePay

- Chưa trả hoặc lớp miễn phí: hủy và trả chỗ, không có tiền hoàn.
- Đã trả, chưa bắt đầu: tạo yêu cầu hoàn đủ số tiền; Invoice vẫn Paid cho tới khi thực trả.
- Đã bắt đầu: hủy được nhưng không hoàn, Invoice vẫn Paid; không thể đặt lại khóa đã mở.
- Tiền SePay vào đơn đã hủy/hết hạn: giữ receipt đối soát, không tự khôi phục hoặc giả đã hoàn.

Luồng thủ công đã được xác nhận: lễ tân chuyển trả bên ngoài hoặc trả tiền mặt, kiểm tra
đúng người nhận/số tiền rồi gọi `refund-confirm` với mã đối soát. API KHÔNG thực hiện chuyển tiền.
Sau đó mới có Invoice Refunded, ngày hoàn, account người xác nhận và RefundReference.
Không tự giảm doanh thu trước lúc xác nhận. Lệnh xác nhận lặp không ghi thêm lần hoàn.

## Nâng cấp

Thẻ chính chủ Active, còn hiệu lực hôm nay, có Invoice Paid; gói mới đang bán, dài hơn và đắt hơn;
không có đơn gói/nâng cấp chờ, không có kỳ gia hạn Active bắt đầu sau thẻ này.

```text
tổng ngày = EndDate − StartDate + 1
ngày còn lại = max(0, EndDate − hôm nay)   không tính hôm nay
credit = floor(tiền thực trả × ngày còn lại / tổng ngày / 1000) × 1000
amount = max(0, giá gói mới − credit)
```

Chưa trả thì không đổi thẻ cũ. Trả thành công: thẻ cũ EndDate hôm nay, status Expired;
thẻ mới Active từ hôm nay, UpgradedFromId liên kết về thẻ cũ. Kiểm tra lại nguồn và kỳ gia hạn
trước settlement, không áp dụng quote đã qua ngày hoặc nguồn đã đổi. Khoản phải trả 0 áp dụng ngay.

## Quyền mới

Receptionist mới được tạo Member tại quầy, tạo/gia hạn/hủy/chỉnh thẻ, lập/sửa/xóa/in hóa đơn,
đặt và thu học phí tại quầy, xác nhận thu hoặc hoàn. Các action FormData cũng bị chặn đúng quyền.
Manager vẫn đọc và quản lý Account/gói/lớp/báo cáo, không thay cashier.
Đăng ký tự phục vụ vẫn mở; nhân viên đăng nhập đăng ký hộ chỉ cho Receptionist.

Manager không xóa được lớp đang diễn ra. Xóa mềm lớp chưa mở sẽ hủy hold và yêu cầu hoàn
các khoản đã thu, giữ lịch sử. Không sửa PUT Invoice online/Class/Upgrade để bỏ qua settlement.
Route export sai `/api/invoices/invoices/{id}/export` đã bỏ; dùng `/api/invoices/{id}/export`.

## AI và trang chat thử

Mở `https://BACKEND/ai-chat.html` trên backend, hoặc localhost khi phát triển.
Nhập JWT sau đăng nhập để thử. JWT chỉ ở ô nhập, không lưu localStorage, không đưa vào URL.
Đây là demo dùng cùng API, chưa tự thêm nút chat vào repository FE của nhóm.

```dotenv
# THAY_THONG_TIN_AI: chỉ cấu hình sau, chưa có thì để tắt
Ai__Enabled=true
Ai__ApiKey=KHOA_GEMINI_THAT_LUU_TAI_SERVER
Ai__Model=MODEL_GEMINI_DUOC_CAP_QUYEN
```

Không dùng nguyên placeholder, không commit khóa, không đưa vào FE. Không có key/model trả 503.
Code dùng Gemini REST, không tự mở một tài khoản hoặc đăng ký gói trả phí.
Người dùng phải tự cấp khóa/model có quyền sử dụng; test không gửi dữ liệu ra Google.
Tham khảo [tài liệu Google](https://ai.google.dev/api/generate-content).

10 request/phút/tài khoản, history tối đa 8 tin user/assistant và có giới hạn ký tự,
timeout 25 giây, response provider được lọc lỗi. Coach chỉ lấy mục tiêu của học viên mình;
không chuyển phone/email/tên/DOB/chi tiết thanh toán. Người dùng tránh nhập thông tin nhạy cảm.
AI không có tools sửa DB/thanh toán. Gợi ý có thể sai, không thay chẩn đoán hoặc chỉ dẫn chuyên môn.

## Migration và kiểm thử

Migration mới: `20261029133507_ClassFeesUpgradesRefundsAndNotificationSenders`.
Booking cũ giữ giá 0. Invoice cũ Kind Membership và OriginalAmount = amount. Ba gói đúng tên
Gói 1 tháng, Gói 6 tháng, Gói 1 năm backfill giảm 10%, chuyển bullet quyền lợi từ Description
sang Benefits. Không seed lại gói hoặc thu thêm tiền cho booking cũ.
Down tự động bị chặn vì không thể bỏ dữ liệu tài chính mới một cách an toàn.
Backup và review SQL trước khi chạy lên DB thật.

```powershell
dotnet ef database update --project SWP_SportCenter.Repository --startup-project SWP_SportCenter.Api --context AppDbContext
dotnet build tests/SportCenter.Regression/SportCenter.Regression.csproj --no-restore -p:BuildInParallel=false -m:1 -p:UseSharedCompilation=false -nodeReuse:false
dotnet tests/SportCenter.Regression/bin/Debug/net8.0/SportCenter.Regression.dll
```

Suite mặc định mới: 104 kiểm tra workflow qua HTTP/InMemory với provider Gemini giả, không gọi
ngân hàng, AI hoặc Render thật. Có kiểm tra snapshot và SQL/query PostgreSQL offline, KHÔNG coi là
đã chạy transaction/concurrency PostgreSQL thật. Cần smoke test staging trước production.
Suite 311 ca hợp đồng cũ vẫn giữ trong Program.cs sau `--legacy-contract` để đối chiếu,
không dùng các kỳ vọng B45/TTL 30 phút/quyền Manager cashier cũ làm tiêu chuẩn hiện tại.

## Việc cần thực hiện ngoài code

- B12/B42: chủ hệ thống xoay mật khẩu DB, JWT và Cloudinary ở nhà cung cấp; không chỉ sửa file.
- B44: chưa đọc hoặc reset mật khẩu demo_member, chưa kết nối kiểm tra DB cũ. Không thể đọc ngược hash.
  Cần người quản lý chọn cách reset và xác nhận quyền truy cập DB cũ/backup, không hardcode password demo.
- Chưa chạy migration Render, chưa chuyển tiền/hoàn tiền thực tế, chưa gọi Gemini thật, chưa push/deploy.
- File appsettings đang có thay đổi của người dùng được giữ nguyên; nếu có secret thật thì không commit.
