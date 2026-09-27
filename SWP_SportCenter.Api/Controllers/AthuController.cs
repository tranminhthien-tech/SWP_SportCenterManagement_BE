using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Athu;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IService _authService;

    public AuthController(IService authService)
    {
        _authService = authService;
    }

    // POST: /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] Request.RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        return Ok(result);
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] Request.LoginRequest request)
    {
        var result = await _authService.Login(request);

        return Ok(result);
    }

    // POST: /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync();

        return Ok(result);
    }

    // POST: /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] Request.ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);

        return Ok(result);
    }

    // POST: /api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] Request.ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return Ok(result);
    }

    // GET: /api/auth/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _authService.GetMeAsync();

        return Ok(result);
    }
}