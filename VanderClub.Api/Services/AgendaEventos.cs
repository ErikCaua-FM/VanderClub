using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public async Task<object> AreaProjection(Trabalho w, Registro r, int? idOverride = null)
    {
        var hours = r.B("padrao") ? new List<Registro>() : await w.Rows("SELECT * FROM horarios_area WHERE area_id=@id ORDER BY dia_semana,inicio", new { id = r.Id });
        var h = hours.FirstOrDefault();
        return new
        {
            id = idOverride ?? (r.B("padrao") ? r.Id : -r.Id),
            unidadeId = r.I("clube_id"),
            nome = r.S("nome"),
            descricao = r.S("descricao"),
            regras = r.S("regras"),
            imagem = MobileImage(r.S("imagem")),
            capacidade = r.I("capacidade"),
            maxMinutos = r.I("max_minutos"),
            abreMinuto = r.B("padrao") ? r.I("abre_minuto") : h is null ? 0 : (int)((TimeSpan)h.Get("inicio")!).TotalMinutes,
            fechaMinuto = r.B("padrao") ? r.I("fecha_minuto") : h is null ? 0 : (int)((TimeSpan)h.Get("fim")!).TotalMinutes,
            limiteTexto = r.I("max_minutos") == 0 ? "Sem limite de duração; respeite a disponibilidade." : $"Até {r.I("max_minutos") / 60.0:0.#} horas por reserva."};
    }

    public static string MobileImage(string s) => string.IsNullOrEmpty(s) ? "social.png" : Path.ChangeExtension(s, ".png");
    public async Task<object> Areas(Trabalho w, Ator a, int unit, int? id = null)
    {
        await Membership(w, a, unit);
        var list = id.HasValue ? new[] { CatalogoAreas.Obter(unit, id.Value) } : CatalogoAreas.Todos.Select(x => CatalogoAreas.Obter(unit, x.Id)).ToArray();
        var result = new List<object>();
        foreach (var r in list)
            result.Add(await AreaProjection(w, r));
        return id.HasValue ? result.SingleOrDefault() ?? throw ApiException.Missing() : result;
    }

    static object ReservationProjection(Registro r) => new
    {
        id = r.Id,
        usuarioId = r.I("criado_por_usuario_id"),
        areaId = r.I("area_padrao_id") > 0 ? r.I("area_padrao_id") : -r.I("area_id"),
        inicioUtc = r.D("inicio_em"),
        fimUtc = r.D("fim_em"),
        status = r.S("status") switch
        {
            "CANCELADA" => "Cancelada",
            "CONCLUIDA" => "Concluída",
            _ => "Confirmada"
        },
        criadoUtc = r.D("created_at"),
        chaveCliente = "",
        protocolo = r.S("protocolo")
    };
    public async Task<object> Availability(Trabalho w, Ator a, int unit, int area, DateTimeOffset start, DateTimeOffset end)
    {
        await Membership(w, a, unit);
        _ = CatalogoAreas.Obter(unit, area);
        var from = Regras.Utc(start);
        var until = Regras.Utc(end);
        Regras.Exigir(until > from && (until - from).TotalDays <= 370, "Consulte um intervalo de até 370 dias.");
        var list = await w.Rows("SELECT id,inicio_em,TIMESTAMPADD(MINUTE,intervalo_apos_minutos,fim_em) AS fim_em FROM ocupacoes_area WHERE clube_id=@unit AND area_padrao_id=@area AND status='ATIVA' AND inicio_em<@until AND TIMESTAMPADD(MINUTE,intervalo_apos_minutos,fim_em)>@from ORDER BY inicio_em", new { unit, area, from, until });
        return list.Select(r => new { id = r.Id, usuarioId = 0, areaId = area, inicioUtc = r.D("inicio_em"), fimUtc = r.D("fim_em"), status = "Confirmada", criadoUtc = DateTime.UnixEpoch, chaveCliente = "", protocolo = "" }).ToArray();
    }

    async Task Free(Trabalho w, int unit, int area, DateTime start, DateTime end, int buffer, int exclude = 0)
    {
        // A linha da unidade é o mutex transacional; nenhuma linha de catálogo é necessária.
        var conflict = await w.One("SELECT id FROM ocupacoes_area WHERE clube_id=@unit AND (area_padrao_id=@area OR (@area<0 AND area_id=-@area)) AND status='ATIVA' AND id<>@exclude AND inicio_em<@endBuffer AND TIMESTAMPADD(MINUTE,intervalo_apos_minutos,fim_em)>@start LIMIT 1 FOR UPDATE", new { unit, area, exclude, start, endBuffer = end.AddMinutes(buffer) });
        if (conflict is not null)
            throw ApiException.Conflict("Este horário já está ocupado por uma reserva, evento ou bloqueio.");
    }

    async Task OperatingHours(Trabalho w, Registro area, string zone, DateTime start, DateTime end)
    {
        var tz = Regras.Zone(zone);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(start, tz);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(end, tz);
        var hours = area.B("padrao")
            ? Enumerable.Range(0, 7).Select(day => new Registro(new Dictionary<string, object?> { ["dia_semana"] = day, ["inicio"] = TimeSpan.FromMinutes(area.I("abre_minuto")), ["fim"] = TimeSpan.FromMinutes(area.I("fecha_minuto")) })).ToList()
            : await w.Rows("SELECT * FROM horarios_area WHERE area_id=@id ORDER BY dia_semana,inicio", new { id = area.Id });
        for (var day = localStart.Date; day < localEnd; day = day.AddDays(1))
        {
            var begin = day == localStart.Date ? localStart.TimeOfDay : TimeSpan.Zero;
            var finish = day == localEnd.Date ? localEnd.TimeOfDay : TimeSpan.FromDays(1);
            if (finish == TimeSpan.Zero)
                continue;
            var cursor = begin;
            foreach (var h in hours.Where(h => h.I("dia_semana") == (int)day.DayOfWeek))
            {
                var hs = (TimeSpan)h.Get("inicio")!;
                var he = (TimeSpan)h.Get("fim")!;
                if (hs <= cursor && he > cursor)
                    cursor = he;
            }

            Regras.Exigir(cursor >= finish, "O intervalo ultrapassa o horário de funcionamento da área.");
        }
    }

    public async Task<object> Reserve(Trabalho w, Ator a, int unit, ReservaRequest d)
    {
        var c = await Membership(w, a, unit, use: true, lockContract: true);
        var area = CatalogoAreas.Obter(unit, d.AreaId);
        var club = await w.Required("SELECT id,fuso_horario FROM clubes WHERE id=@unit AND status='ATIVO' FOR UPDATE", new { unit });
        var start = Regras.Utc(d.InicioUtc);
        var end = Regras.Utc(d.FimUtc);
        var now = DateTime.UtcNow;
        Regras.Exigir(end > start && start > now, "A reserva precisa começar no futuro e terminar após o início.");
        Regras.Exigir(start >= now.AddHours(area.I("antecedencia_minima_horas")) && start <= now.AddDays(area.I("antecedencia_maxima_dias")), "Confira a antecedência permitida para reservar.");
        Regras.Exigir(area.I("max_minutos") == 0 || (end - start).TotalSeconds <= area.I("max_minutos") * 60, "Esta área permite no máximo 3 horas, ou o limite menor indicado.");
        var tz = Regras.Zone(c.S("fuso_horario"));
        var ls = TimeZoneInfo.ConvertTimeFromUtc(start, tz);
        var le = TimeZoneInfo.ConvertTimeFromUtc(end, tz);
        Regras.Exigir(DateOnly.FromDateTime(ls) >= c.Day("data_inicio").AddDays(c.I("carencia_dias")) && le <= c.Day("data_fim").ToDateTime(TimeOnly.MinValue), "Reserva fora da vigência ou dentro da carência do plano.");
        var future = await w.Scalar<int>("SELECT COUNT(*) FROM reservas r JOIN membros_contrato m ON m.id=r.membro_contrato_id JOIN ocupacoes_area o ON o.reserva_id=r.id WHERE m.contrato_id=@id AND r.status='CONFIRMADA' AND o.status='ATIVA' AND o.fim_em>UTC_TIMESTAMP(6)", new { id = c.Id });
        Regras.Exigir(future < c.I("limite_reservas_futuras"), "Seu contrato atingiu o limite de reservas futuras.");
        await OperatingHours(w, area, club.S("fuso_horario"), start, end);
        await Free(w, unit, area.Id, start, end, area.I("intervalo_apos_minutos"));
        var id = await w.Insert("reservas", new { protocolo = Regras.Protocolo("R"), membro_contrato_id = c.I("membro_id"), criado_por_usuario_id = a.Id, finalidade = "Uso pelo associado", quantidade_pessoas = 1, regras_aceitas = area.S("regras"), cancelamento_minimo_horas = area.I("cancelamento_minimo_horas"), status = "CONFIRMADA" });
        await w.Insert("ocupacoes_area", new { area_padrao_id = area.Id, clube_id = unit, tipo = "RESERVA", reserva_id = id, inicio_em = start, fim_em = end, intervalo_apos_minutos = area.I("intervalo_apos_minutos"), status = "ATIVA", criado_por_usuario_id = a.Id });
        await Execucao.Audit(w, a, "RESERVAR_AREA", "reservas", id, unit);
        return ReservationProjection(await w.Required("SELECT * FROM vw_reservas_detalhadas WHERE id=@id", new { id }));
    }

    public async Task<object> Reservations(Trabalho w, Ator a, int unit)
    {
        var c = await Membership(w, a, unit);
        var list = await w.Rows("SELECT * FROM vw_reservas_detalhadas WHERE membro_contrato_id=@member AND clube_id=@unit ORDER BY inicio_em DESC", new { member = c.I("membro_id"), unit });
        var result = new List<object>();
        foreach (var r in list)
            result.Add(new { reserva = ReservationProjection(r), area = await AreaProjection(w, r.I("area_id") > 0 ? await w.Required("SELECT * FROM areas WHERE id=@id", new { id = r.I("area_id") }) : CatalogoAreas.Obter(unit, r.I("area_padrao_id")), r.I("area_padrao_id") > 0 ? r.I("area_padrao_id") : -r.I("area_id")) });
        return result;
    }

    public async Task<object?> CancelReservation(Trabalho w, Ator a, int unit, int id)
    {
        var c = await Membership(w, a, unit, lockContract: true);
        var r = await w.Required("SELECT * FROM vw_reservas_detalhadas WHERE id=@id AND membro_contrato_id=@member AND clube_id=@unit", new { id, member = c.I("membro_id"), unit });
        await w.Required("SELECT id FROM clubes WHERE id=@unit FOR UPDATE", new { unit });
        r = await w.Required("SELECT r.*,o.inicio_em FROM reservas r JOIN ocupacoes_area o ON o.reserva_id=r.id WHERE r.id=@id FOR UPDATE", new { id });
        if (r.S("status") == "CANCELADA")
            return null;
        Regras.Exigir(r.S("status") == "CONFIRMADA" && DateTime.UtcNow <= r.D("inicio_em").AddHours(-r.I("cancelamento_minimo_horas")), "O prazo de cancelamento encerrou. Contate a administração.");
        await w.Exec("UPDATE reservas SET status='CANCELADA',cancelado_por_usuario_id=@actor,cancelado_em=UTC_TIMESTAMP(6),motivo_cancelamento='Cancelada pelo associado' WHERE id=@id", new { actor = a.Id, id });
        await w.Exec("UPDATE ocupacoes_area SET status='CANCELADA' WHERE reserva_id=@id", new { id });
        await Execucao.Audit(w, a, "CANCELAR_RESERVA", "reservas", id, unit);
        return null;
    }

    public static string EventStatus(Registro e) => e.S("status") == "CANCELADO" ? "Cancelado" : e.D("fim_em") <= DateTime.UtcNow ? "Realizado" : e.S("status") == "CONFIRMADO" ? "Confirmado" : "Agendado";
    public static object MobileEvent(Registro e) => new
    {
        id = e.Id,
        unidadeId = e.I("clube_id"),
        nome = e.S("titulo"),
        descricao = e.S("descricao"),
        local = e.S("local_descricao"),
        inicioUtc = e.D("inicio_em"),
        fimUtc = e.D("fim_em"),
        status = EventStatus(e),
        publico = e.S("publico"),
        capacidade = e.I("capacidade"),
        imagem = MobileImage(e.S("imagem")),
        motivoCancelamento = e.S("motivo_cancelamento")
    };
    public static object AdminEvent(Registro e) => new
    {
        id = e.Ids(),
        unitId = e.Ids("clube_id"),
        title = e.S("titulo"),
        description = e.S("descricao"),
        audience = e.S("publico"),
        image = e.S("imagem"),
        areaId = e.Ids("area_id"),
        location = e.S("local_descricao"),
        capacity = e.I("capacidade"),
        start = e.Iso("inicio_em"),
        end = e.Iso("fim_em"),
        regStart = e.Iso("inscricoes_abrem_em"),
        regEnd = e.Iso("inscricoes_fecham_em"),
        cancelledAt = e.DN("cancelado_em"),
        creatorId = e.Ids("criado_por_usuario_id"),
        version = e.I("versao"),
        status = EventStatus(e)
    };
    public async Task<object> Events(Trabalho w, Ator a, int unit, int? id = null)
    {
        await Membership(w, a, unit);
        var events = await w.Rows("SELECT * FROM eventos WHERE clube_id=@unit AND (@id IS NULL OR id=@id) ORDER BY inicio_em DESC", new { unit, id });
        return id.HasValue ? MobileEvent(events.SingleOrDefault() ?? throw ApiException.Missing()) : events.Select(MobileEvent).ToArray();
    }

    const string AdminEventsSql = "SELECT e.*,(SELECT COALESCE(area_padrao_id,-area_id) FROM ocupacoes_area o WHERE o.evento_id=e.id AND (o.status='ATIVA' OR e.status='CANCELADO') ORDER BY o.id DESC LIMIT 1) AS area_id FROM eventos e";
    public async Task<object> SaveEvent(Trabalho w, Ator a, int id, AdminEventInput d)
    {
        var unit = Regras.Id(d.UnitId);
        a.ExigirUnidade(unit);
        await w.Required("SELECT id FROM clubes WHERE id=@unit AND status='ATIVO' FOR UPDATE", new { unit });
        Registro? old = null;
        Registro? occupation = null;
        if (id > 0)
        {
            old = await AdminRecord(w, a, "eventos", id, true);
            Regras.Version(old, d.Version);
            Regras.Exigir(old.I("clube_id") == unit, "O evento deve permanecer na unidade em que foi criado.");
            Regras.Exigir(old.S("status") != "CANCELADO" && old.D("fim_em") > DateTime.UtcNow, "Evento encerrado ou cancelado não pode ser editado.");
            occupation = await w.One("SELECT * FROM ocupacoes_area WHERE evento_id=@id", new { id });
        }

        var start = Regras.Utc(d.Start);
        var end = Regras.Utc(d.End);
        var rs = Regras.Utc(d.RegStart);
        var re = Regras.Utc(d.RegEnd);
        Regras.Exigir(end > start && start > DateTime.UtcNow && rs < re && re <= start, "Confira os períodos de evento e inscrições.");
        Regras.Exigir(d.Status is "Agendado" or "Confirmado", "Status inválido.");
        Regras.Exigir(d.Audience is "ASSOCIADOS" or "PUBLICO", "Público inválido.");
        Regras.Exigir(d.Capacity > 0, "Capacidade deve ser positiva.");
        int? areaId = string.IsNullOrWhiteSpace(d.AreaId) ? null : int.TryParse(d.AreaId, out var parsedArea) && parsedArea != 0 && parsedArea != int.MinValue ? parsedArea : throw new ApiException(422, "Área inválida.");
        Registro? area = null;
        if (areaId.HasValue)
        {
            if (areaId.Value < 0)
            {
                var legacy = await w.Required("SELECT ar.*,t.codigo FROM areas ar JOIN tipos_area t ON t.id=ar.tipo_area_id WHERE ar.id=@id AND ar.clube_id=@unit AND ar.ativo=1", new { id = -areaId.Value, unit });
                var mapped = CatalogoAreas.Todos.FirstOrDefault(x => x.Codigo == legacy.S("codigo"));
                if (mapped is not null) areaId = mapped.Id;
                area = mapped is not null ? CatalogoAreas.Obter(unit, mapped.Id) : legacy;
            }
            else area = CatalogoAreas.Obter(unit, areaId.Value);
            Regras.Exigir(d.Capacity <= area.I("capacidade"), "Capacidade excede a área selecionada.");
            await Free(w, unit, areaId.Value, start, end, area.I("intervalo_apos_minutos"), occupation?.Id ?? 0);
        }

        var values = new
        {
            clube_id = unit,
            criado_por_usuario_id = old?.I("criado_por_usuario_id") ?? a.Id,
            titulo = Regras.Texto(d.Title, "o título", 200),
            descricao = Regras.Texto(d.Description, "a descrição", 10000),
            inicio_em = start,
            fim_em = end,
            publico = d.Audience,
            capacidade = d.Capacity,
            local_descricao = Regras.Texto(d.Location, "o local", 200),
            imagem = Regras.Image(d.Image),
            inscricoes_abrem_em = rs,
            inscricoes_fecham_em = re,
            status = d.Status == "Confirmado" ? "CONFIRMADO" : "AGENDADO"
        };
        if (id == 0)
            id = await w.Insert("eventos", values);
        else
            await UpdateValues(w, "eventos", id, values);
        if (area is not null)
        {
            var ov = new
            {
                area_id = areaId < 0 ? -areaId : null,
                area_padrao_id = areaId > 0 ? areaId : null,
                clube_id = unit,
                tipo = "EVENTO",
                evento_id = id,
                inicio_em = start,
                fim_em = end,
                intervalo_apos_minutos = area.I("intervalo_apos_minutos"),
                status = "ATIVA",
                criado_por_usuario_id = occupation?.I("criado_por_usuario_id") ?? a.Id
            };
            if (occupation is null)
                await w.Insert("ocupacoes_area", ov);
            else
                await UpdateValues(w, "ocupacoes_area", occupation.Id, ov);
        }
        else if (occupation is not null)
            await w.Exec("UPDATE ocupacoes_area SET status='CANCELADA' WHERE id=@id", new { id = occupation.Id });
        await Execucao.Audit(w, a, "SALVAR_EVENTO", "eventos", id, unit);
        return AdminEvent(await w.Required(AdminEventsSql + " WHERE e.id=@id", new { id }));
    }

    public async Task<object?> CancelEvent(Trabalho w, Ator a, int id, ReasonInput d)
    {
        var ev = await AdminRecord(w, a, "eventos", id);
        await w.Required("SELECT id FROM clubes WHERE id=@unit FOR UPDATE", new { unit = ev.I("clube_id") });
        ev = await AdminRecord(w, a, "eventos", id, true);
        Regras.Version(ev, d.Version);
        var reason = Regras.Texto(d.Reason, "o motivo", 500);
        await w.Exec("UPDATE eventos SET status='CANCELADO',cancelado_em=UTC_TIMESTAMP(6),motivo_cancelamento=@reason WHERE id=@id", new { reason, id });
        await w.Exec("UPDATE ocupacoes_area SET status='CANCELADA' WHERE evento_id=@id", new { id });
        await Execucao.Audit(w, a, "CANCELAR_EVENTO", "eventos", id, ev.I("clube_id"));
        return null;
    }
}
