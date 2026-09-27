namespace SWP_SportCenter.Service.Athu;

public interface IService
{
    public Task<Response.IdentityResponse> Login(Request.LoginRequest request);
    // POST /api/auth/register
    Task<Response.RegisterResponse> RegisterAsync(
        Request.RegisterRequest request);

    // POST /api/auth/logout
    Task<Response.AuthMessageResponse> LogoutAsync();

    // POST /api/auth/forgot-password
    Task<Response.AuthMessageResponse> ForgotPasswordAsync(
        Request.ForgotPasswordRequest request);

    // POST /api/auth/reset-password
    Task<Response.AuthMessageResponse> ResetPasswordAsync(
        Request.ResetPasswordRequest request);

    // GET /api/auth/me
    Task<Response.MeResponse> GetMeAsync();
}