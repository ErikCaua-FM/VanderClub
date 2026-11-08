using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public async Task<object> Visitors(Trabalho w, Ator a, int unit, string date)
    {
        var m = await Membership(w, a, unit);
        var day = Regras.Day(date);
        return (await w.Rows("SELECT * FROM visitantes WHERE membro_responsavel_id=@member AND clube_id=@unit AND data_visita=@day ORDER BY nome", new { member = m.I("membro_id"), unit, day })).Select(v => new { id = v.Id, usuarioId = v.I("cadastrado_por_usuario_id"), unidadeId = v.I("clube_id"), nome = v.S("nome"), tipoDocumento = v.S("tipo_documento"), documento = v.S("documento"), nascimento = v.Date("nascimento"), telefone = v.S("telefone"), dia = v.Date("data_visita"), placa = v.S("placa"), observacao = v.S("observacao"), cancelado = v.S("status") == "CANCELADO" }).ToArray();
    }

    public async Task<object?> SaveVisitor(Trabalho w, Ator a, int unit, VisitanteRequest d)
    {
        var m = await Membership(w, a, unit, use: true);
        var u = await w.Required("SELECT fuso_horario FROM clubes WHERE id=@unit", new { unit });
        var day = Regras.Day(d.Dia);
        Regras.Exigir(day >= Regras.Today(u.S("fuso_horario")) && day < m.Day("data_fim"), "Visita fora da vigência ou em data passada.");
        var doc = Regras.Texto(d.Documento, "o documento", 40, 3);
        var type = Regras.Texto(d.TipoDocumento, "o tipo de documento", 30);
        if (type.Equals("CPF", StringComparison.OrdinalIgnoreCase))
            doc = Regras.Cpf(doc);
        var id = await w.Insert("visitantes", new { membro_responsavel_id = m.I("membro_id"), clube_id = unit, cadastrado_por_usuario_id = a.Id, nome = Regras.Texto(d.Nome, "o nome", 160, 3), tipo_documento = type, documento = doc, nascimento = Regras.Birth(d.Nascimento), telefone = Regras.Phone(d.Telefone, false), data_visita = day, placa = Regras.Opcional(d.Placa, 12).ToUpperInvariant(), observacao = Regras.Opcional(d.Observacao, 1000), status = "PREVISTO" });
        await Execucao.Audit(w, a, "CADASTRAR_VISITANTE", "visitantes", id, unit);
        return null;
    }

    public async Task<object?> CancelVisitor(Trabalho w, Ator a, int unit, int id)
    {
        var m = await Membership(w, a, unit);
        var v = await w.Required("SELECT * FROM visitantes WHERE id=@id AND clube_id=@unit AND membro_responsavel_id=@member FOR UPDATE", new { id, unit, member = m.I("membro_id") });
        if (v.S("status") == "CANCELADO")
            return null;
        Regras.Exigir(v.S("status") == "PREVISTO", "A visita já iniciou ou terminou.");
        await w.Exec("UPDATE visitantes SET status='CANCELADO',cancelado_em=UTC_TIMESTAMP(6),cancelado_por_usuario_id=@actor WHERE id=@id", new { actor = a.Id, id });
        await Execucao.Audit(w, a, "CANCELAR_VISITANTE", "visitantes", id, unit);
        return null;
    }

    static string TicketType(string type) => type switch
    {
        "Solicitação" or "SOLICITACAO" => "SOLICITACAO",
        "Reclamação" or "RECLAMACAO" => "RECLAMACAO",
        _ => throw new ApiException(422, "Tipo de atendimento inválido.")};
    static string TicketLabel(string type) => type == "SOLICITACAO" ? "Solicitação" : "Reclamação";
    public async Task<object> TicketProjection(Trabalho w, Registro t, bool admin = false)
    {
        var messages = await w.Rows("SELECT * FROM mensagens_chamado WHERE chamado_id=@id ORDER BY id", new { id = t.Id });
        var first = messages.FirstOrDefault();
        var responses = messages.Where(m => m.S("autor_perfil")is "ADM_GERAL" or "ADM_LOCAL").ToArray();
        var last = responses.LastOrDefault();
        if (admin)
        {
            var member = await w.One("SELECT m.id,m.tipo,p.nome FROM membros_contrato m JOIN usuarios u ON u.pessoa_id=m.pessoa_id JOIN pessoas p ON p.id=m.pessoa_id WHERE u.id=@user AND m.contrato_id=@cid", new { user = t.I("autor_usuario_id"), cid = t.I("contrato_id") });
            return new
            {
                id = t.Ids(),
                number = t.S("protocolo"),
                type = TicketLabel(t.S("tipo")),
                unitId = t.Ids("clube_id"),
                personId = member?.Ids() ?? "",
                requester = new
                {
                    id = member?.Ids() ?? "",
                    name = member?.S("nome") ?? first?.S("autor_nome") ?? "Associado",
                    type = member is null ? "Associado" : Regras.MemberType(member.S("tipo"))
                },
                title = t.S("motivo"),
                description = first?.S("texto") ?? "",
                createdAt = t.Iso("created_at"),
                responses = responses.Select(r => new { id = r.Ids(), authorId = r.Ids("autor_usuario_id"), authorName = r.S("autor_nome"), role = r.S("autor_perfil"), text = r.S("texto"), at = r.Iso("created_at") }).ToArray(),
                respondedAt = last?.Iso("created_at") ?? "",
                status = responses.Length > 0 ? "Atendida" : "Pendente",
                version = t.I("versao")
            };
        }

        return new
        {
            id = t.Id,
            usuarioId = t.I("autor_usuario_id"),
            unidadeId = t.I("clube_id"),
            tipo = TicketLabel(t.S("tipo")),
            motivo = t.S("motivo"),
            descricao = first?.S("texto") ?? "",
            status = responses.Length > 0 ? "Atendida" : "Pendente",
            respostaAdmin = string.Join("\n\n", responses.Select(r => $"{r.D("created_at"):dd/MM/yyyy HH:mm} UTC — {r.S("autor_nome")} ({Regras.Classe(r.S("autor_perfil"))})\n{r.S("texto")}")),
            criadoUtc = t.D("created_at"),
            respondidoUtc = last?.DN("created_at"),
            protocolo = t.S("protocolo")
        };
    }

    public async Task<object> Tickets(Trabalho w, Ator a, int unit, string type, int? id = null)
    {
        await Membership(w, a, unit);
        var list = await w.Rows("SELECT * FROM chamados WHERE clube_id=@unit AND autor_usuario_id=@author AND (@id IS NULL OR id=@id) AND (@id IS NOT NULL OR tipo=@type) ORDER BY id DESC", new { unit, author = a.Id, id, type = id.HasValue ? "" : TicketType(type) });
        var result = new List<object>();
        foreach (var t in list)
            result.Add(await TicketProjection(w, t));
        return id.HasValue ? result.SingleOrDefault() ?? throw ApiException.Missing() : result;
    }

    public async Task<object> CreateTicket(Trabalho w, Ator a, int unit, ChamadoRequest d)
    {
        var m = await Membership(w, a, unit);
        var id = await w.Insert("chamados", new { protocolo = Regras.Protocolo("C"), clube_id = unit, autor_usuario_id = a.Id, contrato_id = m.Id, tipo = TicketType(d.Tipo), motivo = Regras.Texto(d.Motivo, "o motivo", 200), status = "ABERTO" });
        await w.Insert("mensagens_chamado", new { chamado_id = id, autor_usuario_id = a.Id, autor_nome = a.Nome, autor_perfil = a.Perfil, texto = Regras.Texto(d.Descricao, "a descrição", 10000), chave_cliente = Guid.NewGuid().ToString() });
        await Execucao.Audit(w, a, "ABRIR_CHAMADO", "chamados", id, unit);
        return await TicketProjection(w, await w.Required("SELECT * FROM chamados WHERE id=@id", new { id }));
    }

    public async Task<object?> Reply(Trabalho w, Ator a, int id, TextInput d)
    {
        var t = await AdminRecord(w, a, "chamados", id, true);
        Regras.Version(t, d.Version);
        await w.Insert("mensagens_chamado", new { chamado_id = id, autor_usuario_id = a.Id, autor_nome = a.Nome, autor_perfil = a.Perfil, texto = Regras.Texto(d.Text, "a resposta", 10000), chave_cliente = Guid.NewGuid().ToString() });
        await w.Exec("UPDATE chamados SET status='RESOLVIDO',responsavel_usuario_id=@actor,resolvido_em=UTC_TIMESTAMP(6) WHERE id=@id", new { actor = a.Id, id });
        await Execucao.Audit(w, a, "RESPONDER_CHAMADO", "chamados", id, t.I("clube_id"));
        return null;
    }

    public async Task<object> Messages(Trabalho w, Ator a, int unit, int before, int limit)
    {
        await Membership(w, a, unit, use: true);
        Regras.Exigir(before >= 0 && limit is >= 1 and <= 100, "Paginação inválida.", 400);
        var list = await w.Rows("SELECT * FROM mensagens_chat WHERE clube_id=@unit AND status='VISIVEL' AND (@before=0 OR id<@before) ORDER BY id DESC LIMIT @limit", new { unit, before, limit });
        return list.OrderBy(m => m.Id).Select(m => new { id = m.Id, unidadeId = unit, autorUsuarioId = m.I("autor_usuario_id"), classeAutor = Regras.Classe(m.S("autor_perfil")), autorNome = m.S("autor_nome"), administrador = m.S("autor_perfil") != "ASSOCIADO", texto = m.S("texto"), criadoUtc = m.D("created_at"), chaveCliente = m.S("chave_cliente") }).ToArray();
    }

    public async Task<object?> SendMessage(Trabalho w, Ator a, int unit, string text, string key)
    {
        if (a.Admin)
            a.ExigirUnidade(unit);
        else
            await Membership(w, a, unit, use: true);
        Regras.Exigir(await w.Scalar<int>("SELECT COUNT(*) FROM clubes WHERE id=@unit AND status='ATIVO'", new { unit }) == 1, "Unidade indisponível.");
        var moderated = await w.Scalar<int>("SELECT COUNT(*) FROM moderacoes_chat WHERE clube_id=@unit AND usuario_id=@user AND revogada_em IS NULL AND inicio_em<=UTC_TIMESTAMP(6) AND (fim_em IS NULL OR fim_em>UTC_TIMESTAMP(6))", new { unit, user = a.Id });
        Regras.Exigir(moderated == 0, "Envio de mensagens temporariamente restrito pela moderação.", 403);
        await w.Insert("mensagens_chat", new { clube_id = unit, autor_usuario_id = a.Id, autor_nome = a.Nome, autor_perfil = a.Perfil, texto = Regras.Texto(text, "a mensagem", 2000), chave_cliente = key, status = "VISIVEL" });
        return null;
    }

    public async Task<object?> Pin(Trabalho w, Ator a, int id, PinInput d)
    {
        var m = await AdminRecord(w, a, "mensagens_chat", id, true);
        Regras.Version(m, d.Version);
        await w.Exec("UPDATE mensagens_chat SET fixada=@pin WHERE id=@id", new { pin = d.Pinned, id });
        await Execucao.Audit(w, a, "FIXAR_AVISO", "mensagens_chat", id, m.I("clube_id"));
        return null;
    }
}
