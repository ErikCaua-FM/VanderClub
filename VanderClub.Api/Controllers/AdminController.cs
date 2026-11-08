using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Requests;
using VanderClub.Api.Services;

namespace VanderClub.Api.Controllers;
[ApiController, Authorize(Roles = "ADM_GERAL,ADM_LOCAL")]
[Route("api/v1/admin")]
public sealed class AdminController(Execucao exec, ClubeService s) : ControllerBase
{
    [HttpGet("bootstrap")]
    public Task<IActionResult> Bootstrap() => exec.Read(HttpContext, s.Bootstrap);
    [HttpPost("units"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> AddUnit(AdminUnitInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveUnit(w, a, 0, d), 200);
    [HttpPut("units/{id:int:min(1)}"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> EditUnit(int id, AdminUnitInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveUnit(w, a, id, d), 200);
    [HttpPost("administrators"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> AddAdmin(AdminAdministratorInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveAdmin(w, a, 0, d), 200);
    [HttpPut("administrators/{id:int:min(1)}"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> EditAdmin(int id, AdminAdministratorInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveAdmin(w, a, id, d), 200);
    [HttpPost("events")]
    public Task<IActionResult> AddEvent(AdminEventInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveEvent(w, a, 0, d), 200);
    [HttpPut("events/{id:int:min(1)}")]
    public Task<IActionResult> EditEvent(int id, AdminEventInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SaveEvent(w, a, id, d), 200);
    [HttpPost("events/{id:int:min(1)}/cancel")]
    public Task<IActionResult> CancelEvent(int id, ReasonInput d) => exec.Write(HttpContext, d, (w, a) => s.CancelEvent(w, a, id, d));
    [HttpPost("plans"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> AddPlan(AdminPlanInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SavePlan(w, a, 0, d), 200);
    [HttpPut("plans/{id:int:min(1)}"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> EditPlan(int id, AdminPlanInput d) => exec.Write(HttpContext, d, async (w, a) => await s.SavePlan(w, a, id, d), 200);
    [HttpPost("plans/{id:int:min(1)}/archive"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> ArchivePlan(int id, VersionInput d) => exec.Write(HttpContext, d, (w, a) => s.ArchivePlan(w, a, id, d));
    [HttpPost("polls")]
    public Task<IActionResult> AddPoll(AdminPollInput d) => exec.Write(HttpContext, d, async (w, a) => await s.CreatePoll(w, a, d), 200);
    [HttpPost("polls/{id:int:min(1)}/close")]
    public Task<IActionResult> ClosePoll(int id, VersionInput d) => exec.Write(HttpContext, d, (w, a) => s.ClosePoll(w, a, id, d));
    [HttpPost("beneficiaries/{id:int:min(1)}/review")]
    public Task<IActionResult> Review(int id, ReviewInput d) => exec.Write(HttpContext, d, (w, a) => s.ReviewBeneficiary(w, a, id, d));
    [HttpPost("contracts/{id:int:min(1)}/suspend")]
    public Task<IActionResult> Suspend(int id, ReasonInput d) => exec.Write(HttpContext, d, (w, a) => s.ContractState(w, a, id, d.Version, d.Reason, false));
    [HttpPost("contracts/{id:int:min(1)}/reactivate")]
    public Task<IActionResult> Reactivate(int id, VersionInput d) => exec.Write(HttpContext, d, (w, a) => s.ContractState(w, a, id, d.Version, null, true));
    [HttpPost("tickets/{id:int:min(1)}/responses")]
    public Task<IActionResult> Reply(int id, TextInput d) => exec.Write(HttpContext, d, (w, a) => s.Reply(w, a, id, d));
    [HttpPost("units/{unitId:int:min(1)}/messages")]
    public Task<IActionResult> Message(int unitId, TextInput d) => exec.Write(HttpContext, d, (w, a) => s.SendMessage(w, a, unitId, d.Text, Request.Headers["Idempotency-Key"].ToString()));
    [HttpPut("messages/{id:int:min(1)}/pin")]
    public Task<IActionResult> Pin(int id, PinInput d) => exec.Write(HttpContext, d, (w, a) => s.Pin(w, a, id, d));
    [HttpPost("invoices/{id:int:min(1)}/payments"), Authorize(Roles = "ADM_GERAL")]
    public Task<IActionResult> Payment(int id, PaymentInput d) => exec.Write(HttpContext, d, (w, a) => s.Receive(w, a, id, d));
}
