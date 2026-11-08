using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Requests;
using VanderClub.Api.Services;

namespace VanderClub.Api.Controllers;
[ApiController, Authorize(Roles = "ADM_GERAL"), Route("api/v1/admin/areas")]
public sealed class AreasController(Execucao exec, ClubeService s) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> List() => exec.Read(HttpContext, async (w, _) => (object)(await w.Rows("SELECT id FROM clubes WHERE status='ATIVO' ORDER BY id")).SelectMany(u => CatalogoAreas.Todos.Select(r => new { id = r.Id.ToString(), unitId = u.Ids(), name = r.Nome, version = 1, active = true })).ToArray());
    [HttpGet("types")]
    public Task<IActionResult> Types() => exec.Read(HttpContext, (w, _) => Task.FromResult<object>(CatalogoAreas.Todos.Select(r => new { code = r.Codigo, name = r.Nome, maxMinutes = r.MaxMinutos }).ToArray()));
    [HttpPost]
    public Task<IActionResult> Create(AreaInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveArea(w, a, 0, d), 200);
    [HttpPut("{id:int:min(1)}")]
    public Task<IActionResult> Edit(int id, AreaInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveArea(w, a, id, d), 200);
}
