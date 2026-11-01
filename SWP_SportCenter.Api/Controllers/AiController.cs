using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SWP_SportCenter.Service.Ai;
using SWP_SportCenter.Extensions;
namespace SWP_SportCenter.Controllers;

[ApiController, Route("api/ai"), Authorize, EnableRateLimiting("ai")]
public sealed class AiController(AiService service) : ControllerBase
{
    // FIX B48: mọi tài khoản đăng nhập được chat; chỉ Coach được exercise. Không trả secret/model/key.
    [HttpGet("options")]
    [ProducesResponseType(typeof(AiConfigurationResponse), 200)]
    [ProducesResponseType(401)]
    public IActionResult Options([FromServices] Microsoft.Extensions.Options.IOptionsSnapshot<AiOptions> options)
    {
        var c = options.Value;
        var issues = c.ConfigurationIssues();
        return Ok(new AiConfigurationResponse("Gemini", c.Enabled, issues.Length == 0, issues));
    }
    // CODE CŨ: IActionResult/Swagger chỉ có 200 không có schema body.
    [HttpPost("chat"), RequestSizeLimit(32768)]
    [ProducesResponseType(typeof(AiReply), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 502)]
    [ProducesResponseType(typeof(ProblemDetails), 503)]
    [ProducesResponseType(401), ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> Chat(ChatRequest request, CancellationToken ct) => Ok(await service.ChatAsync(request, ct));
    [HttpPost("exercise-suggestions"), Authorize(Policy = JwtExtensions.CoachPolicy), RequestSizeLimit(8192)]
    [ProducesResponseType(typeof(AiReply), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    [ProducesResponseType(typeof(ProblemDetails), 502)]
    [ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Exercises(ExerciseRequest request, CancellationToken ct) => Ok(await service.ExercisesAsync(request, ct));
}
