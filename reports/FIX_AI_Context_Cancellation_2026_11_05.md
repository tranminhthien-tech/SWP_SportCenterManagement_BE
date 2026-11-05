# B48 — Context và cancellation, 05/11/2026

## Bằng chứng FE ngày 04/11

Chat trace ...00000003 và ...00000008 timeout sau khoảng 20–22 giây;
exercise ...00000006 trả provider 503 UNAVAILABLE. Không suy ra cùng nguyên nhân,
không kết luận quota đã hết/khôi phục từ báo cáo này.

## Đã sửa

1. Câu chào độc lập không history thuộc allowlist bỏ truy vấn catalog/lịch riêng.
   Vẫn gọi Gemini, không trả câu mẫu giả thành công. Câu hỏi giá/lịch hoặc có history
   giữ context và giới hạn quyền như cũ. Không dùng từ khóa chào để bỏ context tùy tiện.
2. CurrentActor.MemberIdAsync nhận CancellationToken tùy chọn và truyền vào EF;
   AiContextBuilder truyền token nghiệp vụ xuống truy vấn này. Caller cũ tương thích.
3. GeminiClient phân biệt HTTP RequestAborted với token nghiệp vụ. Token nghiệp vụ
   hủy khi HTTP chưa ngắt ghi operation-deadline; khi không có HTTP context chỉ ghi
   operation-token, không tự kết luận người dùng hủy.
4. Log attempt-finished chứa phase headers/body, attemptMs và elapsedMs cả khi
   thành công, lỗi hoặc hủy. Kết hợp AI context-ready/contextMs, queue-ready/queueMs,
   response/failure/retry và AI request để đối chiếu theo trace. Không ghi key/prompt/body.
5. Code cũ được giữ dạng comment CODE CŨ, thay đổi có FIX. Không đổi schema/migration.

## Giới hạn

Không tăng timeout, không đổi model/key, không gọi Google hay Render trong regression.
20 giây vẫn là timeout mỗi attempt mặc định; 30 giây toàn nghiệp vụ.
Việc tối ưu context chưa chứng minh Google phản hồi thành công. Nếu provider còn 503
thì vẫn trả AI_PROVIDER_UNAVAILABLE; hết thời gian trả AI_TIMEOUT, không fake 200.
Chủ service cần thử prompt tối giản từ môi trường deploy, đối chiếu model/quota và
log ba trace, xác nhận chat và exercise đều 200 có nội dung trước khi đóng B48.

## Kiểm tra

Build thành công (0 lỗi). 224 kiểm tra workflow đạt.

Regression kiểm câu chào gọi provider mà không cần Member profile/context, câu hỏi
giá/lịch không bị phân loại thành chào, history vẫn dùng context, cancellation log
không báo nhầm caller. Chạy cùng các kiểm tra roster/quyền exercise, lỗi provider,
retry giới hạn và quyền riêng tư hiện có. HTTP Gemini giả, DB InMemory.
