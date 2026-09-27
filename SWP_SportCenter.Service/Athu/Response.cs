namespace SWP_SportCenter.Service.Athu;

public class Response
{
    public class IdentityResponse
    {
        public string Access_token { get; set; } = null!;
        public Guid UserId { get; set; }
        public bool IsVerify { get; set; }

    }
    
    // Kết quả đăng ký
    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsVerify { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Kết quả chung cho các thao tác Auth
    public class AuthMessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    // Thông tin tài khoản hiện tại
    public class MeResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsVerify { get; set; }
    }

}