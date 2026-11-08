using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;
using VanderClub.Api.Services;

namespace VanderClub.Api.Controllers;
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(Autenticacao auth, Banco banco, ClubeService club, IConfiguration config) : ControllerBase
{
    [HttpPost("mobile/login"), EnableRateLimiting("login")]
    public async Task<IActionResult> MobileLogin(LoginRequest d)
    {
        var(token, u) = await auth.Login(d.Email, d.Senha, false, HttpContext.RequestAborted);
        await using var w = await banco.Abrir(HttpContext.RequestAborted);
        return Ok(new { perfil = await club.Profile(w, new Ator(u.Id, u.I("pessoa_id"), u.S("nome"), u.S("perfil"), 0, 0)), token });
    }

    [HttpPost("admin/login"), EnableRateLimiting("login")]
    public async Task<IActionResult> AdminLogin(AdminLogin d)
    {
        var(token, u) = await auth.Login(d.Email, d.Password, true, HttpContext.RequestAborted);
        Response.Cookies.Append(Autenticacao.Cookie, token, auth.CookieOptions(DateTime.UtcNow.AddHours(Math.Clamp(config.GetValue("Security:SessionHours", 8), 1, 24))));
        return Ok(new { actor = Autenticacao.AdminActor(u), csrfToken = Autenticacao.Csrf(token) });
    }

    [Authorize(Roles = "ADM_GERAL,ADM_LOCAL"), HttpGet("admin/session")]
    public async Task<IActionResult> Session()
    {
        var a = Ator.From(HttpContext);
        await using var w = await banco.Abrir(HttpContext.RequestAborted);
        var u = await w.Required(Autenticacao.UserSql + " WHERE u.id=@id", new { id = a.Id });
        return Ok(new { actor = Autenticacao.AdminActor(u), csrfToken = Autenticacao.Csrf((string)HttpContext.Items["session-token"]!) });
    }

    [Authorize(Roles = "ASSOCIADO"), HttpPost("mobile/logout")]
    public async Task<IActionResult> MobileLogout()
    {
        await auth.Logout(Ator.From(HttpContext), HttpContext.RequestAborted);
        return NoContent();
    }

    [Authorize(Roles = "ADM_GERAL,ADM_LOCAL"), HttpPost("admin/logout")]
    public async Task<IActionResult> AdminLogout()
    {
        await auth.Logout(Ator.From(HttpContext), HttpContext.RequestAborted);
        Response.Cookies.Delete(Autenticacao.Cookie, auth.CookieOptions());
        return NoContent();
    }
}
