# AI gọi được endpoint nhưng không có kết quả — 03/11/2026

## Lý do

Options 200/configured=true chỉ xác nhận env hợp lệ, không gọi Gemini.
Chat timeout và exercise quota ngày là hai kết quả độc lập. Không có câu trả lời
thật thì BE không trả 200/reply giả. Hình AI Studio có cảnh báo giới hạn, model
hiển thị 5 RPM và 20 RPD; không dùng dấu -/20 để suy ra số lượt đã sử dụng.
Code không thể tăng quota Google hoặc bảo đảm model hết lỗi UNAVAILABLE.

## Sửa contract cho FE

Giữ status 503 và message/detail/traceId hiện tại, thêm:

| code | Ý nghĩa | retryable |
|---|---|---|
| AI_DAILY_QUOTA | Google 429 với quotaId PerDay | false |
| AI_RATE_LIMIT | Google 429 chưa xác định là quota ngày | false |
| AI_TIMEOUT | Hết thời gian chờ, không có kết quả hoàn chỉnh | true |
| AI_PROVIDER_UNAVAILABLE | Google trả lỗi tạm thời 408/5xx | true |
| AI_PROVIDER_CONFIGURATION | Google từ chối key/model/quyền/billing | false |

retryable=true chỉ cho phép người dùng thử lại sau, không yêu cầu FE tự retry
liên tục. HTTP lỗi Google có providerHttpStatus/providerStatus allowlist;
timeout không có HTTP response thì không bịa metadata này.
Không trả raw body Google/key/prompt. HTTP 200 thật vẫn giữ reply/suggestions.

FE phải tắt loading cả khi request thất bại (finally), hiển thị message và code,
không đợi reply khi HTTP 503, không tự retry quota ngày. Repo này chỉ chứa BE,
không khẳng định đã sửa widget FE hay đã kiểm inference trên Render.

## Tiết kiệm quota

Mặc định MaxRetries đổi từ 1 sang 0. Env Render ghi đè mặc định nên cần đặt
Ai__MaxRetries=0 nếu đang có biến =1. Không cần migration. Không thay key/model
tự động, không dùng câu trả lời mẫu để che lỗi Google. Code cũ được comment,
code mới đánh dấu FIX. Các kiểm tra retry bật MaxRetries=1 tường minh.

## Cần xác minh sau deploy

Quota phải khôi phục/cấp đủ trên đúng project/model. Thử chat và exercise
tuần tự bằng tài khoản đúng quyền; chỉ hoàn tất AI thật khi cả hai trả 200 có
nội dung. Không suy ra công suất đồng thời từ regression mock.
