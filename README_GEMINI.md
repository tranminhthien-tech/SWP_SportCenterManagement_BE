# Gemini cho chatbox SportCenter

Đã đọc báo cáo FE ngày 01/11/2026. AI trong repo dùng **Google Gemini REST** ở backend,
không phải FE gọi Google trực tiếp. Cần key thật và model có quyền sử dụng trên **đúng service Render**.
Không thể chữa thiếu secret trên Render bằng việc chỉ deploy source mới.

## Cấu hình

Dán các biến sau vào Environment của backend Render và thay placeholder trước khi lưu:

```dotenv
Ai__Enabled=true
Ai__ApiKey=THAY_BANG_GEMINI_API_KEY_THAT
Ai__Model=THAY_BANG_TEN_MODEL_GEMINI_CO_QUYEN_SU_DUNG
```

Tên model có dạng `gemini-...`, chỉ ID model, không phải URL và không thêm `models/`.
Chọn model đang có quyền sử dụng trong tài khoản của bạn; không giữ nguyên placeholder.
Lấy key qua Google AI Studio theo [tài liệu Google](https://ai.google.dev/api/generate-content).
Không gửi key trong chat, body API, Git, URL hoặc biến VITE_. Không sửa appsettings có
thay đổi riêng của người dùng trong đợt này. Restart/redeploy để env có hiệu lực.
Không có key thì giữ tắt; API trả 503, không trả câu mẫu giả rồi coi là Gemini hoạt động.

## Quyền và trạng thái cấu hình

- `GET /api/ai/options` yêu cầu đăng nhập, trả provider, enabled, configured,
  configurationIssues, chatAccess, exerciseAccess, providerVerified.
- Chat: mọi Account đã xác thực (Member/Coach/Receptionist/CenterManager), giữ quyền
  hiện có. Đây là quyết định contract gửi lại FE, không còn kỳ vọng MemberPolicy cũ.
- Exercise suggestions: chỉ Coach; nếu có memberId phải là học viên ghi danh hợp lệ
  trong lớp Coach phụ trách. Không mở thêm quyền staff xem riêng tư qua AI.

Ví dụ options chưa cấu hình:

```json
{"provider":"Gemini","enabled":false,"configured":false,
 "configurationIssues":["Ai__Enabled","Ai__ApiKey","Ai__Model"],
 "chatAccess":"Authenticated","exerciseAccess":"Coach","providerVerified":false}
```

`configured: true` chỉ là các biến cục bộ có hình thức hợp lệ, KHÔNG phải đã kiểm chứng
key/quota/model. `providerVerified` luôn false ở endpoint này: không tốn quota để kiểm tra.
FIX B48 02/11: key là chuỗi opaque, không áp regex đoán prefix/định dạng Google. Key có
dấu chấm và ký tự ASCII in được được giữ nguyên trong header. Vẫn chặn key rỗng,
whitespace/control/CR/LF, ký tự ngoài ASCII và key quá 4096 ký tự; không Trim hoặc sửa key
âm thầm. Nếu Google từ chối key/quyền/quota thì API vẫn trả 503 đã lọc, không giả thành công.
Key đã lộ trong ảnh/chat cần được thu hồi và thay trên Render, không dùng làm dữ liệu test.
Options không trả key hoặc giá trị model. Chỉ cần gọi lúc mở chat, không poll liên tục;
các request AI được giới hạn 10/phút/tài khoản, bao gồm options.

## FE gọi

```http
POST /api/ai/chat
Authorization: Bearer JWT_CUA_NGUOI_DUNG
Content-Type: application/json

{"message":"Trung tâm có những môn nào?","history":[]}
```

History tối đa 8 tin, mỗi tin tối đa 1500 ký tự, role user hoặc assistant;
message bắt buộc, không trắng, tối đa 2000 ký tự; tổng không quá 10000 ký tự.
Không gửi system role, API key hoặc memberId để chọn lịch người khác.

```http
POST /api/ai/exercise-suggestions
Authorization: Bearer JWT_COACH
Content-Type: application/json

{"goal":"Tăng sức bền","durationMinutes":20,
 "equipment":"bodyweight","level":"beginner","memberId":null}
```

Response 200 cùng schema AiReply, Swagger đã khai báo kiểu:

```json
{"reply":"Nội dung từ Gemini", "suggestions":"", "provider":"Gemini",
 "disclaimer":"Gợi ý tham khảo, không thay thế đánh giá của huấn luyện viên hoặc chuyên gia y tế. Dừng tập nếu đau, chóng mặt hoặc khó thở."}
```

Exercise đặt suggestions bằng reply; chat giữ suggestions rỗng. FE hiển thị reply và
disclaimer bằng text an toàn, không chèn trực tiếp HTML model trả. Provider trả nội dung
không có nghĩa nội dung luôn đúng: cần kiểm chứng, đặc biệt gợi ý bài tập.

Lỗi nghiệp vụ/provider/quota local dùng ProblemDetails JSON có status/title/detail,
message và traceId. Validation từ ApiController có thể có errors và không có message;
FE đọc message rồi detail/title làm fallback. 401/403 ở lớp auth có thể không có body.

| Status | Ý nghĩa |
| --- | --- |
| 400 | Request/history/duration không hợp lệ |
| 401/403 | Chưa đăng nhập hoặc sai quyền/phạm vi |
| 429 | Hết hạn mức 10 request/phút ở BE; có Retry-After khi limiter cung cấp |
| 503 | Thiếu env, model/key/quyền/quota Google chưa sẵn sàng, timeout/provider quá tải |
| 502 | Không kết nối được hoặc response Gemini không hợp lệ |

Không đưa body lỗi thô từ Google hoặc key vào API/log. Provider URL cố định phía server,
HTTP key nằm trong x-goog-api-key và được redact. Không thêm URL tùy ý từ FE.

## Dữ liệu trả lời

Server lấy danh mục môn, tối đa 10 gói đang bán, 20 lớp, 20 buổi sắp tới.
Lịch riêng tối đa 20 buổi: Member từ booking Booked/Attended của chính mình;
Coach từ lớp chính mình dạy. Manager/Receptionist chỉ có context công khai trong chat,
không cấp toàn bộ lịch riêng/hồ sơ học viên chỉ vì có role staff.
Ngày xét theo Việt Nam, context có mốc AsOfUtc và giới hạn; model được nhắc không coi
các danh sách giới hạn là toàn bộ DB và không lấy lịch công khai làm lịch đã đăng ký.
Không gửi tên, phone/email/DOB hoặc thanh toán cá nhân. Người dùng tránh tự nhập thông tin nhạy cảm.
History chỉ là ngữ cảnh hội thoại, không cấp quyền truy cập dữ liệu hoặc thực thi thao tác.
Ngoài phạm vi được nhắc từ chối trong system prompt; cần kiểm chứng phản hồi thật sau cấu hình,
không coi unit test/provider giả là bằng chứng model luôn tuân thủ.

## Gemini 3.8 và chẩn đoán timeout cập nhật 02/11

Giữ `Ai__Model=gemini-3.8-flash`; không đổi model tự động hoặc gọi provider khác khi lỗi.
Request Gemini 3 dùng `thinkingConfig.thinkingLevel=LOW`, không gửi temperature/topP/topK.
Không gửi thinkingLevel cho Gemini 2.x. Output mặc định 4096 token; không coi giới hạn
token nhỏ là biện pháp tắt thinking. MAX_TOKENS/output rỗng không giả trả lời thành công.
Theo [migration Google](https://ai.google.dev/gemini-api/docs/whats-new-gemini-3.8) và
[generateContent ThinkingConfig](https://ai.google.dev/api/generate-content#thinkingconfig).

Các env dưới đây là tùy chọn, mặc định đã có trong code; không cần thêm lại key vào Git:

```dotenv
Ai__ThinkingLevel=LOW
Ai__MaxOutputTokens=4096
Ai__TotalTimeoutSeconds=30
Ai__AttemptTimeoutSeconds=20
Ai__MaxRetries=0
```

FIX 03/11: TotalTimeout bao gồm đọc DB/context, chờ slot, đọc body và backoff.
Trần an toàn 30 giây được áp dụng cả khi env cũ đặt 45/90 giây; mỗi lần gọi mặc định 20 giây.
AttemptTimeout không được lớn hơn TotalTimeout. FE có deadline 35 giây hiện tại không cần tăng;
nếu user đóng/cancel request thì BE hủy và không ghi nhận nhầm thành lỗi 500.
FIX: mặc định chỉ 1 lần gửi (0 retry) để bảo vệ quota miễn phí. Nếu chủ service bật
MaxRetries=1 thì tối đa 2 lần gửi, tuần tự, backoff exponential có jitter; chỉ retry
408/500/502/503/504 hoặc 429 có RetryInfo/Retry-After ngắn và chưa nhận diện quota ngày.
Hint quá 2 giây không bị bỏ qua để retry sớm; trả unavailable để người dùng chờ.
Không retry lỗi key/quyền/model/billing, quota ngày, 429 thiếu hint, timeout mù hoặc lỗi
kết nối chưa rõ kết quả. Không chạy retry vô hạn/gọi chồng. Retry inference có thể tiêu
thêm quota/chi phí, vì vậy có thể đặt MaxRetries=0 để tắt; tối đa cho phép 2 retry.
Giới hạn 2 outbound đang chạy trong mỗi process; không thay hạn mức project hoặc nhiều replica.

Log có traceId, model, attempt, HTTP status gốc, provider status allowlist, category và
elapsedMs. Categories phân biệt RateLimit/DailyQuota/ConfigurationOrPermission/ProviderUnavailable;
có log timeout/caller-cancelled/transport-error/malformed-response. Tìm theo traceId báo cáo FE.
Không log key, prompt/history, body/message Google thô, project/quota ID hoặc thought.
Không thể lấy lại body gốc đã bị bản cũ bỏ qua từ các traceId lịch sử bằng bản sửa này.

Sau deploy cần thử cả chat và exercise bằng đúng role, đối chiếu log; options configured
vẫn không xác nhận quota/key thực tế. Nếu Google hết quota hoặc service vẫn quá tải,
code không thể tự chữa hạn mức. Không yêu cầu đổi key liên tục trong cùng project.

## Độ trễ SePay

Repo hiện xử lý và commit Paid ngay trong webhook, KHÔNG đợi worker 15 giây.
Worker chỉ hết hạn đơn; polling không làm tiền được xác nhận nhanh hơn trước khi webhook tới.
Không thêm SSE/SignalR hoặc job trả Paid giả để chữa một độ trễ chưa đo được.

Log `SePay commit` có EventId, Outcome, InvoiceId, ServiceStartedAt, ReceivedAt, CommittedAt và BackendMs.
ServiceStartedAt là lúc vào service sau binding/auth; ReceivedAt giữ mốc lịch sử của receipt khi replay,
không phải luôn là mốc request HTTP hiện tại.
`SePay retry` ghi thời gian xử lý retry; không log API key, nội dung/biên lai thô.
GET payment trả `Server-Timing: payment;dur=...` để đối chiếu thời gian xử lý phía BE.
Log commit không phải chính xác thời điểm browser nhận: vẫn còn response/network/FE render.
Giới hạn 30 giây của ACK theo [SePay](https://developer.sepay.vn/vi/sepay-webhooks/tich-hop-webhook)
không phải SLA xác nhận tiền trong 30 giây. Không cam kết 2 giây từ việc FE poll mỗi 2 giây.

Cần paymentId mới, mốc ngân hàng/SePay, delivery log, log commit và Network GET của cùng
giao dịch mới để xác định đoạn chậm. Chưa có các mốc đó, chưa kết luận Render hay provider.
Không yêu cầu chuyển thêm tiền; đơn B66 cũ 8000đ chưa được đánh dấu đã giải quyết.

## Kiểm thử và triển khai

Build thành công, 213 kiểm tra workflow qua với DB InMemory, HTTP Gemini giả. Có ca key
opaque chứa dấu chấm được gửi nguyên vẹn, config metadata không lộ key và CR/LF bị chặn;
Gemini 3.8 LOW, retry hữu hạn, daily quota, RetryInfo/Retry-After, MAX_TOKENS, output rỗng,
timeout, caller cancellation và log không lộ prompt/key/raw body.
Không gọi Google/SePay thật, không
login demo Render, không tạo đơn hay giả webhook production, không tiêu quota AI thật.
Chưa xác minh key/quota và nội dung Gemini thực tế; cần chủ service cấu hình rồi thử bằng
đúng role. Test một lần cả hai endpoint, không chỉ xem Swagger hoặc options configured.
Không thay schema, không cần migration mới. Chưa commit/push/deploy hoặc thay env Render.
