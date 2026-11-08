using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public async Task<object> Bootstrap(Trabalho w, Ator a)
    {
        await TickPolls(w, a.Geral ? null : a.UnidadeId);
        var actor = await w.Required(Autenticacao.UserSql + " WHERE u.id=@id", new { id = a.Id });
        var units = (await w.Rows(UnitSql + " ORDER BY c.nome")).Select(AdminUnit).ToArray();
        var admins = (await w.Rows(Autenticacao.UserSql + " WHERE u.perfil IN ('ADM_GERAL','ADM_LOCAL') ORDER BY (u.perfil='ADM_GERAL') DESC,p.nome")).Select(u => Autenticacao.AdminActor(u, a.Geral || u.Id == a.Id)).ToArray();
        var plans = new List<object>();
        foreach (var p in await w.Rows("SELECT id FROM planos ORDER BY id"))
            plans.Add(await PlanProjection(w, p.Id));
        var cs = await w.Rows("SELECT c.*,(SELECT id FROM membros_contrato m WHERE m.contrato_id=c.id AND m.tipo='TITULAR') AS holder_member FROM contratos c WHERE " + UnitFilter(a, "c", "clube_origem_id") + " ORDER BY c.id");
        var contracts = new List<object>();
        foreach (var c in cs)
        {
            var v = await w.Required("SELECT * FROM plano_versoes WHERE id=@id", new { id = c.I("plano_versao_id") });
            var cp = await w.Required("SELECT * FROM condicoes_pagamento WHERE id=@id", new { id = c.I("condicao_pagamento_id") });
            contracts.Add(new { id = c.Ids(), number = c.S("numero"), holderId = c.Ids("holder_member"), unitId = c.Ids("clube_origem_id"), planId = v.Ids("plano_id"), planVersionId = v.Ids(), snapshot = await VersionProjection(w, v), condition = ConditionProjection(cp), start = c.Date("data_inicio"), end = c.Date("data_fim"), status = Regras.ContractStatus(c.S("status")), version = c.I("versao") });
        }

        var members = await w.Rows("""
            SELECT m.*,p.nome,p.cpf,p.email_contato,p.telefone,p.nascimento,p.logradouro,p.numero,p.complemento,p.bairro,p.cep,p.sexo,p.estado_civil,
            ci.nome AS cidade,e.uf,
            (SELECT dados_propostos FROM solicitacoes_vinculo sv WHERE sv.membro_contrato_id=m.id AND sv.status='PENDENTE' LIMIT 1) AS propostos,
            (SELECT justificativa FROM solicitacoes_vinculo sv WHERE sv.membro_contrato_id=m.id ORDER BY id DESC LIMIT 1) AS observacao_admin
            FROM membros_contrato m JOIN contratos c ON c.id=m.contrato_id JOIN pessoas p ON p.id=m.pessoa_id
            LEFT JOIN cidades ci ON ci.id=p.cidade_id LEFT JOIN estados e ON e.id=ci.estado_id
            WHERE
            """ + " " + UnitFilter(a, "c", "clube_origem_id") + " AND m.status<>'REMOVIDO' ORDER BY m.id");
        var people = members.Select(m =>
        {
            var proposed = Proposed(m);
            return new
            {
                id = m.Ids(),
                name = proposed.Nome,
                cpf = proposed.Cpf,
                email = m.S("email_contato"),
                phone = proposed.Telefone,
                birth = proposed.Nascimento.ToString("yyyy-MM-dd"),
                street = m.S("logradouro"),
                number = m.S("numero"),
                complement = m.S("complemento"),
                district = m.S("bairro"),
                city = m.S("cidade"),
                uf = m.S("uf"),
                cep = m.S("cep"),
                gender = m.S("sexo"),
                maritalStatus = m.S("estado_civil"),
                type = Regras.MemberType(m.S("tipo")),
                approval = Regras.Approval(m.S("status")),
                contractId = m.Ids("contrato_id"),
                createdAt = m.Iso("created_at"),
                version = m.I("versao"),
                relationship = proposed.Parentesco,
                reviewReason = m.S("observacao_admin")
            };
        }).ToArray();
        var invoices = (await w.Rows("""
            SELECT f.*,c.quantidade_parcelas,p.origem,p.referencia_manual FROM faturas f JOIN contratos c ON c.id=f.contrato_id
            LEFT JOIN pagamentos p ON p.fatura_id=f.id AND p.quitacao_aplicada=1
            WHERE f.status<>'CANCELADA' AND
            """ + " " + UnitFilter(a, "c", "clube_origem_id") + " ORDER BY f.id")).Select(f => new { id = f.Ids(), contractId = f.Ids("contrato_id"), number = f.I("numero_parcela"), total = f.I("quantidade_parcelas"), due = f.Date("vencimento"), amount = f.L("valor_centavos"), paidAt = f.Iso("quitada_em"), origin = f.S("origem"), receipt = f.S("referencia_manual"), version = f.I("versao") }).ToArray();
        var events = (await w.Rows(AdminEventsSql + " WHERE " + UnitFilter(a, "e") + " ORDER BY e.id")).Select(AdminEvent).ToArray();
        var polls = new List<object>();
        foreach (var p in await w.Rows("SELECT v.* FROM votacoes v WHERE v.status<>'RASCUNHO' AND " + UnitFilter(a, "v") + " ORDER BY v.id"))
            polls.Add(await AdminPoll(w, p));
        var counts = await w.Rows("SELECT r.* FROM vw_votacoes_resultados r JOIN votacoes v ON v.id=r.votacao_id WHERE " + UnitFilter(a, "v") + " ORDER BY r.votacao_id,r.ordem");
        // Projecao anonima exigida pela Web 2.0; nenhum id de voto/eleitor ou timestamp.
        var votes = counts.SelectMany(r => Enumerable.Range(0, r.I("quantidade_votos")).Select(_ => new { pollId = r.Ids("votacao_id"), optionId = r.Ids("opcao_id") })).ToArray();
        var tickets = new List<object>();
        foreach (var t in await w.Rows("SELECT t.* FROM chamados t WHERE " + UnitFilter(a, "t") + " ORDER BY t.id"))
            tickets.Add(await TicketProjection(w, t, true));
        var messages = (await w.Rows("SELECT m.* FROM mensagens_chat m WHERE m.status='VISIVEL' AND " + UnitFilter(a, "m") + " ORDER BY m.id")).Select(m => new { id = m.Ids(), unitId = m.Ids("clube_id"), authorId = m.Ids("autor_usuario_id"), authorName = m.S("autor_nome"), role = m.S("autor_perfil"), text = m.S("texto"), at = m.Iso("created_at"), pinned = m.B("fixada"), clientKey = m.S("chave_cliente"), version = m.I("versao") }).ToArray();
        var availableUnits = await w.Rows("SELECT c.id FROM clubes c WHERE c.status='ATIVO' AND " + UnitFilter(a, "c", "id") + " ORDER BY c.id");
        var areas = availableUnits.SelectMany(u => CatalogoAreas.Todos.Select(ar => new { id = ar.Id.ToString(), unitId = u.Ids(), name = ar.Nome })).ToList();
        // IDs negativos preservam a consulta/edição de locais legados, sem criar novas áreas no banco.
        foreach (var ar in await w.Rows("SELECT ar.* FROM areas ar WHERE ar.ativo=1 AND " + UnitFilter(a, "ar") + " AND NOT EXISTS (SELECT 1 FROM tipos_area t WHERE t.id=ar.tipo_area_id AND t.codigo IN ('ACADEMIA','FUTEBOL','POLIESPORTIVA','AREIA','TENIS','JOGOS','SALAO_SOCIAL','SALAO_FESTA','QUIOSQUE'))"))
            areas.Add(new { id = (-ar.Id).ToString(), unitId = ar.Ids("clube_id"), name = ar.S("nome") });
        var reservations = (await w.Rows("SELECT r.* FROM vw_reservas_detalhadas r WHERE " + UnitFilter(a, "r") + " ORDER BY r.id")).Select(r => new { id = r.Ids(), unitId = r.Ids("clube_id"), areaId = (r.I("area_padrao_id") > 0 ? r.I("area_padrao_id") : -r.I("area_id")).ToString(), personId = r.Ids("membro_contrato_id"), start = r.Iso("inicio_em"), end = r.Iso("fim_em"), status = r.S("status") == "CANCELADA" ? "Cancelada" : "Confirmada" }).ToArray();
        var audit = (await w.Rows("SELECT au.id,au.acao,au.entidade,au.referencia,au.created_at,au.ator_usuario_id FROM auditoria au WHERE " + UnitFilter(a, "au") + " ORDER BY au.id")).Select(r => new { id = r.Ids(), action = r.S("acao"), entity = r.S("entidade"), reference = r.S("referencia"), at = r.Iso("created_at"), actorId = r.Ids("ator_usuario_id") }).ToArray();
        return new
        {
            createdAt = DateTime.UtcNow,
            sequence = new
            {
                ticket = 0
            },
            actor = Autenticacao.AdminActor(actor),
            units,
            admins,
            plans,
            contracts,
            people,
            invoices,
            events,
            polls,
            votes,
            tickets,
            messages,
            areas,
            reservations,
            audit
        };
    }
}
