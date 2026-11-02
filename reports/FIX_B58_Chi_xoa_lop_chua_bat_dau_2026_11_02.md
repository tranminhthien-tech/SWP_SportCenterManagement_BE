# B58 Quy tắc xóa lớp đã chốt ngày 02/11/2026

Chọn cách 1: chỉ cho xóa mềm lớp chưa bắt đầu. Không thay đổi schema hoặc dữ liệu thật.

## Nguyên nhân cũ

Delete dùng IsRunning: sau khi buổi cuối kết thúc thì false, dù status vẫn Scheduled.
Vì thế fixture lớp có buổi đầu và cuối đều đã qua vẫn DELETE 200. Đây là khác biệt
quy tắc nghiệp vụ, không phải chỉ thiếu kiểm tra status Ongoing.

## Quy tắc mới

- Scheduled: nếu buổi đầu đã tới giờ hoặc đã qua thì trả 400; không phụ thuộc status cập nhật kịp.
- Ongoing và Completed: không cho xóa để giữ lịch sử đã bắt đầu hoặc kết thúc.
- Cancelled: chỉ xóa khi chưa tới giờ buổi đầu; nếu buổi đầu đã tới/qua thì vẫn chặn.
- Lớp Draft/Scheduled chưa có buổi nào: chưa có bằng chứng bắt đầu, vẫn xóa mềm được.
- Tính cả buổi đã xóa mềm khi xét lịch sử, không cho ẩn buổi quá khứ để vượt guard.
- Đúng giờ bắt đầu cũng chặn; giờ trung tâm UTC+7, không dùng timezone máy Render.

ClassRules.EnsureCanDelete được gọi trước mọi thay đổi và gọi lại ngay trước lưu sau
các await. Giữ transaction Serializable và luồng hủy hold/yêu cầu hoàn khi xóa lớp
chưa mở. Bị chặn thì không SaveChanges, lớp vẫn GET được và dữ liệu tài chính không đổi.

Thông báo JSON 400 có message:
Lớp đã bắt đầu hoặc đã kết thúc, không được xóa. Chỉ được xóa lớp chưa bắt đầu buổi học đầu tiên.

Code cũ giữ comment CODE CŨ, phần mới đánh dấu FIX B58. Các tài liệu ngày 29–30/10
mô tả cho xóa lớp đã kết thúc là quy tắc lịch sử, đã được thay thế bằng quyết định này.

## Kiểm tra

Build thành công; 194 kiểm tra workflow qua (12 kiểm tra bổ sung B58). Bao gồm session
đã kết thúc với Scheduled/Ongoing/Completed/Cancelled, GET vẫn thấy sau delete bị chặn,
đúng ranh giờ Việt Nam, session quá khứ đã xóa mềm, Scheduled tương lai và Cancelled
trước buổi đầu vẫn được xóa. HTTP local, InMemory; không gọi Render hoặc PostgreSQL thật.

Không cần migration mới. Chưa commit/push/deploy. Bản sửa không khôi phục fixture đã
bị xóa trước đó và không thực hiện thao tác dữ liệu thật.
