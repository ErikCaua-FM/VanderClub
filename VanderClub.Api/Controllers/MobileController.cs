using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Requests;
using VanderClub.Api.Services;

namespace VanderClub.Api.Controllers;
[ApiController, Authorize(Roles = "ASSOCIADO")]
[Route("api/v1/mobile")]
public sealed class MobileController(Execucao exec, ClubeService s) : ControllerBase
{
    [HttpGet("units")]
    public Task<IActionResult> Units() => exec.Read(HttpContext, s.Units);
    [HttpGet("context")]
    public Task<IActionResult> Context([FromQuery] int? unitId) => exec.Read(HttpContext, (w, a) => s.Context(w, a, unitId));
    [HttpGet("me")]
    public Task<IActionResult> Me() => exec.Read(HttpContext, s.Me);
    [HttpPut("me")]
    public Task<IActionResult> Me(DadosPessoais d) => exec.Write(HttpContext, d, (w, a) => s.SaveMe(w, a, d));
    [HttpGet("beneficiaries")]
    public Task<IActionResult> Beneficiaries() => exec.Read(HttpContext, s.Beneficiaries);
    [HttpPost("beneficiaries")]
    public Task<IActionResult> AddBeneficiary(BeneficiarioRequest d) => exec.Write(HttpContext, d, (w, a) => s.SaveBeneficiary(w, a, 0, d));
    [HttpPut("beneficiaries/{id:int:min(1)}")]
    public Task<IActionResult> EditBeneficiary(int id, BeneficiarioRequest d) => exec.Write(HttpContext, d, (w, a) => s.SaveBeneficiary(w, a, id, d));
    [HttpDelete("beneficiaries/{id:int:min(1)}")]
    public Task<IActionResult> RemoveBeneficiary(int id, VersaoRequest d) => exec.Write(HttpContext, d, (w, a) => s.RemoveBeneficiary(w, a, id, d));
    [HttpGet("invoices")]
    public Task<IActionResult> Invoices() => exec.Read(HttpContext, s.Invoices);
    [HttpPost("invoices/{id:int:min(1)}/simulate-payment")]
    public Task<IActionResult> SimulatePayment(int id) => exec.Write(HttpContext, null, (w, a) => s.SimulatePayment(w, a, id));
    [HttpGet("units/{unitId:int:min(1)}/visitors")]
    public Task<IActionResult> Visitors(int unitId, [FromQuery] string date) => exec.Read(HttpContext, (w, a) => s.Visitors(w, a, unitId, date));
    [HttpPost("units/{unitId:int:min(1)}/visitors")]
    public Task<IActionResult> AddVisitor(int unitId, VisitanteRequest d) => exec.Write(HttpContext, d, (w, a) => s.SaveVisitor(w, a, unitId, d));
    [HttpPost("units/{unitId:int:min(1)}/visitors/{id:int:min(1)}/cancel")]
    public Task<IActionResult> CancelVisitor(int unitId, int id) => exec.Write(HttpContext, null, (w, a) => s.CancelVisitor(w, a, unitId, id));
    [HttpGet("units/{unitId:int:min(1)}/tickets")]
    public Task<IActionResult> Tickets(int unitId, [FromQuery] string type) => exec.Read(HttpContext, (w, a) => s.Tickets(w, a, unitId, type));
    [HttpGet("units/{unitId:int:min(1)}/tickets/{id:int:min(1)}")]
    public Task<IActionResult> Ticket(int unitId, int id) => exec.Read(HttpContext, (w, a) => s.Tickets(w, a, unitId, "", id));
    [HttpPost("units/{unitId:int:min(1)}/tickets")]
    public Task<IActionResult> AddTicket(int unitId, ChamadoRequest d) => exec.Write(HttpContext, d, async (w, a) => await s.CreateTicket(w, a, unitId, d), 200);
    [HttpGet("units/{unitId:int:min(1)}/areas")]
    public Task<IActionResult> Areas(int unitId) => exec.Read(HttpContext, (w, a) => s.Areas(w, a, unitId));
    [HttpGet("units/{unitId:int:min(1)}/areas/{id:int:min(1)}")]
    public Task<IActionResult> Area(int unitId, int id) => exec.Read(HttpContext, (w, a) => s.Areas(w, a, unitId, id));
    [HttpGet("units/{unitId:int:min(1)}/areas/{id:int:min(1)}/availability")]
    public Task<IActionResult> Availability(int unitId, int id, [FromQuery] DateTimeOffset start, [FromQuery] DateTimeOffset end) => exec.Read(HttpContext, (w, a) => s.Availability(w, a, unitId, id, start, end));
    [HttpGet("units/{unitId:int:min(1)}/reservations")]
    public Task<IActionResult> Reservations(int unitId) => exec.Read(HttpContext, (w, a) => s.Reservations(w, a, unitId));
    [HttpPost("units/{unitId:int:min(1)}/reservations")]
    public Task<IActionResult> Reserve(int unitId, ReservaRequest d) => exec.Write(HttpContext, d, async (w, a) => await s.Reserve(w, a, unitId, d), 200);
    [HttpPost("units/{unitId:int:min(1)}/reservations/{id:int:min(1)}/cancel")]
    public Task<IActionResult> CancelReservation(int unitId, int id) => exec.Write(HttpContext, null, (w, a) => s.CancelReservation(w, a, unitId, id));
    [HttpGet("units/{unitId:int:min(1)}/messages")]
    public Task<IActionResult> Messages(int unitId, [FromQuery] int beforeId = 0, [FromQuery] int limit = 50) => exec.Read(HttpContext, (w, a) => s.Messages(w, a, unitId, beforeId, limit));
    [HttpPost("units/{unitId:int:min(1)}/messages")]
    public Task<IActionResult> Message(int unitId, MensagemRequest d) => exec.Write(HttpContext, d, (w, a) => s.SendMessage(w, a, unitId, d.Texto, Request.Headers["Idempotency-Key"].ToString()));
    [HttpGet("units/{unitId:int:min(1)}/polls")]
    public Task<IActionResult> Polls(int unitId) => exec.Read(HttpContext, (w, a) => s.Polls(w, a, unitId));
    [HttpGet("units/{unitId:int:min(1)}/polls/{id:int:min(1)}")]
    public Task<IActionResult> Poll(int unitId, int id) => exec.Read(HttpContext, (w, a) => s.PollDetail(w, a, unitId, id));
    [HttpPost("units/{unitId:int:min(1)}/polls/{id:int:min(1)}/vote")]
    public Task<IActionResult> Vote(int unitId, int id, VotoRequest d) => exec.Write(HttpContext, d, (w, a) => s.Vote(w, a, unitId, id, d));
    [HttpGet("units/{unitId:int:min(1)}/events")]
    public Task<IActionResult> Events(int unitId) => exec.Read(HttpContext, (w, a) => s.Events(w, a, unitId));
    [HttpGet("units/{unitId:int:min(1)}/events/{id:int:min(1)}")]
    public Task<IActionResult> Event(int unitId, int id) => exec.Read(HttpContext, (w, a) => s.Events(w, a, unitId, id));
}
