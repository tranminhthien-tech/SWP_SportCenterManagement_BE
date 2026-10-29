using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SWP_SportCenter.Service.Ai;
using SWP_SportCenter.Extensions;
namespace SWP_SportCenter.Controllers;

[ApiController, Route("api/ai"), Authorize, EnableRateLimiting("ai")]
public sealed class AiController(AiService service) : ControllerBase
{
    [HttpPost("chat"), RequestSizeLimit(32768)]
    public async Task<IActionResult> Chat(ChatRequest request, CancellationToken ct) => Ok(await service.ChatAsync(request, ct));
    [HttpPost("exercise-suggestions"), Authorize(Policy = JwtExtensions.CoachPolicy), RequestSizeLimit(8192)]
    public async Task<IActionResult> Exercises(ExerciseRequest request, CancellationToken ct) => Ok(await service.ExercisesAsync(request, ct));
}
