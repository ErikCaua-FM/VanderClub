using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Requests;
using VanderClub.Api.Services;

namespace VanderClub.Api.Controllers;
[ApiController]
[Route("api/v1")]
public sealed class PublicController(Execucao exec, ClubeService service) : ControllerBase
{
    [HttpGet("public/plans")]
    public Task<IActionResult> Plans() => exec.Public(HttpContext, service.PublicPlans);
    [HttpGet("public/units")]
    public Task<IActionResult> Units() => exec.Public(HttpContext, service.PublicUnits);
    [HttpPost("mobile/registrations"), EnableRateLimiting("login")]
    public Task<IActionResult> Register(Cadastro d) => exec.Write(HttpContext, d, (w, _) => service.Register(w, d), registrationEmail: Regras.Email(d.Email));
}
