# SePay nhận tiền nhưng InvoiceId null

## Nguyên nhân

Invoice lưu mã SWP đủ 32 ký tự hex, nhưng nội dung ngân hàng chèn khoảng trắng trước
hai ký tự cuối. Regex cũ chỉ nhận mã liền nên không tìm ra hóa đơn, lưu receipt
Outcome Unmatched, InvoiceId null. Webhook ACK 200 không đồng nghĩa hóa đơn Paid.
API GET payment vì thế vẫn Pending, FE tiếp tục hiển thị QR.

## Code sửa

- `PaymentReferenceParser.cs`: chỉ chuẩn hóa whitespace trong mã đủ 32 hex; không dùng
  tiền tố/LIKE, không đoán ký tự thiếu, không nối Code với Content. Hai mã khác nhau
  vẫn AmbiguousReference. Giữ kiểm tra mã canonical khi sinh QR, không thay giá trị DB.
- `SepayService.cs`: dùng parser mới, ghi InvoiceId khi tìm thấy đúng hóa đơn. Chỉ Paid
  khi đúng tiền, tài khoản/gateway, đơn còn hạn, đúng trạng thái và đủ quy tắc settlement.
- Retry receipt cũ: payload hash phải khớp, chỉ Unmatched + chưa có InvoiceId được thử
  lại. Cập nhật cùng dòng, không tạo receipt trùng hoặc kích hoạt lần hai. Paid và các
  outcome khác không tự chạy lại; cùng ID nhưng payload khác trả 409.
- Giữ ReceivedAt/hash lịch sử. Kiểm tra hạn và ghi thời điểm xác nhận theo thời gian
  xử lý hiện tại, không lùi về ReceivedAt để ép đơn đã hết hạn thành Paid.
- Code thay thế giữ comment CODE CŨ; phần mới đánh dấu FIX SEPAY.

## Khoản đã chuyển

Sau deploy, có thể dùng chức năng gửi lại **giao dịch thật đã lưu** tại dashboard SePay.
Không tạo webhook giả, sửa payload, xóa receipt, điền InvoiceId tay hoặc sửa status DB.

Nếu đơn vẫn hợp lệ, receipt được liên kết và settlement cập nhật Invoice/Booking hoặc
Membership trong transaction. GET payment trả status 2, paidAt và qrCodeUrl null.
FE dùng GET trạng thái này để đóng QR; không lấy success của ACK webhook làm kết quả tiền.

Nếu đơn đã hết hạn/hủy, retry chỉ có thể liên kết hóa đơn và ghi RejectedState; **không
tự kích hoạt lại**. Cần đối soát khoản tiền thực nhận để quyết định xử lý đơn/hoàn tiền
theo quy trình được duyệt. Đợt sửa code này không tự giải quyết khoản đã chuyển trên Render.

InvoiceId vẫn có thể null hợp lệ khi mã thiếu, không tồn tại trong DB hoặc có nhiều
mã khác nhau. Không gán hóa đơn theo số tiền hay lấy đơn mới nhất để làm hết null.

## Kiểm thử

Build thành công; 148 kiểm tra workflow qua. Bổ sung ca mã thực tế bị chèn khoảng trắng,
tab/newline, thiếu/dư ký tự, từ bao ngoài, nhiều mã, key sai, tiền sai, tài khoản sai,
retry Paid, payload bị sửa, retry Unmatched cũ, giữ ReceivedAt và không hồi sinh đơn hết hạn.
Đã xác minh qua HTTP local: receipt InvoiceId đúng, status Paid và QR null.
Database InMemory, không gọi ngân hàng hoặc Render; chưa kiểm thử concurrency PostgreSQL thật.

Không thay schema nên không cần migration mới. Chưa commit/push/deploy hoặc sửa DB thật.
