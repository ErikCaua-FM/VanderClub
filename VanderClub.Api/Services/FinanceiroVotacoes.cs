using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public async Task<object> Invoices(Trabalho w, Ator a)
    {
        var m = await Membership(w, a, holder: true);
        return (await w.Rows("SELECT f.*,c.quantidade_parcelas,EXISTS(SELECT 1 FROM pagamentos p WHERE p.fatura_id=f.id AND p.quitacao_aplicada=1 AND p.origem='SIMULADO') AS pagamento_simulado FROM faturas f JOIN contratos c ON c.id=f.contrato_id WHERE c.titular_pessoa_id=@person AND f.status<>'CANCELADA' ORDER BY f.vencimento,f.id", new { person = a.PessoaId })).Select(f => new { id = f.Id, usuarioId = a.Id, numero = f.I("numero_parcela"), totalParcelas = f.I("quantidade_parcelas"), vencimento = f.Date("vencimento"), valorCentavos = f.L("valor_centavos"), pagoUtc = f.DN("quitada_em"), pagamentoSimulado = f.B("pagamento_simulado"), parcela = $"{f.I("numero_parcela"):00}/{f.I("quantidade_parcelas"):00}" }).ToArray();
    }

    public async Task<object?> SimulatePayment(Trabalho w, Ator a, int id)
    {
        await Membership(w, a, holder: true, lockContract: true);
        var f = await w.Required("SELECT f.*,c.clube_origem_id FROM faturas f JOIN contratos c ON c.id=f.contrato_id WHERE f.id=@id AND c.titular_pessoa_id=@person FOR UPDATE", new { id, person = a.PessoaId });
        if (f.S("status") == "QUITADA")
            return null;
        Regras.Exigir(f.S("status") == "ABERTA", "A parcela está cancelada e não pode ser paga.", 409);
        var paid = DateTime.UtcNow;
        var p = await w.Insert("pagamentos", new { fatura_id = id, origem = "SIMULADO", meio = "OUTRO", valor_centavos = f.L("valor_centavos"), status = "CONFIRMADO", quitacao_aplicada = true, conciliacao = "REGULAR", referencia_manual = "SIM-" + Guid.NewGuid().ToString("N"), registrado_por_usuario_id = a.Id, pago_em = paid, confirmado_em = paid, observacao = "Simulação solicitada pelo titular no Mobile. Nenhuma cobrança ou transação bancária foi realizada." });
        await w.Exec("UPDATE faturas SET status='QUITADA',quitada_em=@paid WHERE id=@id", new { paid, id });
        await Execucao.Audit(w, a, "SIMULAR_PAGAMENTO", "pagamentos", p, f.I("clube_origem_id"));
        return null;
    }

    public async Task<object?> Receive(Trabalho w, Ator a, int id, PaymentInput d)
    {
        a.ExigirGeral();
        var f = await w.Required("SELECT * FROM faturas WHERE id=@id FOR UPDATE", new { id });
        Regras.Version(f, d.Version);
        Regras.Exigir(f.S("status") == "ABERTA", "A parcela já foi paga ou cancelada.", 409);
        var paid = Regras.Utc(d.ReceivedAt);
        Regras.Exigir(paid <= DateTime.UtcNow, "A data de recebimento não pode estar no futuro.");
        Regras.Exigir(d.Method is "PIX" or "BOLETO" or "CARTAO" or "OUTRO", "Meio de pagamento inválido.");
        var reference = Regras.Texto(d.Reference, "a referência do comprovante", 200);
        var note = Regras.Opcional(d.Note, 1000);
        var unit = await w.Scalar<int>("SELECT clube_origem_id FROM contratos WHERE id=@id", new { id = f.I("contrato_id") });
        var p = await w.Insert("pagamentos", new { fatura_id = id, origem = "MANUAL", meio = d.Method, valor_centavos = f.L("valor_centavos"), status = "CONFIRMADO", quitacao_aplicada = true, conciliacao = "REGULAR", referencia_manual = reference, registrado_por_usuario_id = a.Id, confirmado_por_usuario_id = a.Id, pago_em = paid, confirmado_em = DateTime.UtcNow, observacao = note });
        await w.Exec("UPDATE faturas SET status='QUITADA',quitada_em=@paid WHERE id=@id", new { paid, id });
        await Execucao.Audit(w, a, "CONFIRMAR_RECEBIMENTO", "pagamentos", p, unit);
        return null;
    }

    public static async Task<int> Overdue(Trabalho w, int contract, DateOnly day) => await w.Scalar<int>("SELECT COUNT(*) FROM faturas WHERE contrato_id=@contract AND status='ABERTA' AND vencimento<@day", new { contract, day });
    public async Task<object?> ContractState(Trabalho w, Ator a, int id, int version, string? reason, bool active)
    {
        var c = await w.Required("SELECT c.*,b.fuso_horario FROM contratos c JOIN clubes b ON b.id=c.clube_origem_id WHERE c.id=@id FOR UPDATE", new { id });
        a.ExigirUnidade(c.I("clube_origem_id"));
        Regras.Version(c, version);
        var day = Regras.Today(c.S("fuso_horario"));
        var debts = await Overdue(w, id, day);
        if (active)
        {
            Regras.Exigir(c.S("status") == "SUSPENSO" && c.Day("data_inicio") <= day && c.Day("data_fim") > day, "Contrato não está suspenso dentro da vigência.");
            Regras.Exigir(debts == 0, "Ainda há parcelas atrasadas.");
            await w.Exec("UPDATE contratos SET status='VIGENTE',suspenso_em=NULL,motivo_suspensao=NULL WHERE id=@id", new { id });
        }
        else
        {
            Regras.Exigir(c.S("status") == "VIGENTE" && debts > 0, "A suspensão financeira exige contrato vigente com parcela atrasada.");
            var why = Regras.Texto(reason, "o motivo", 500);
            await w.Exec("UPDATE contratos SET status='SUSPENSO',suspenso_em=UTC_TIMESTAMP(6),motivo_suspensao=@why WHERE id=@id", new { why, id });
        }

        await Execucao.Audit(w, a, active ? "REATIVAR_ASSOCIACAO" : "SUSPENDER_ASSOCIACAO", "contratos", id, c.I("clube_origem_id"));
        return null;
    }

    public async Task TickPolls(Trabalho w, int? unit = null)
    {
        var list = await w.Rows("SELECT id FROM votacoes WHERE status IN ('AGENDADA','ABERTA') AND inicio_em<=UTC_TIMESTAMP(6) AND (@unit IS NULL OR clube_id=@unit) ORDER BY id", new { unit });
        foreach (var row in list)
            await TickPoll(w, row.Id);
    }

    public async Task<Registro> TickPoll(Trabalho w, int id)
    {
        var v = await w.Required("SELECT v.*,c.fuso_horario FROM votacoes v JOIN clubes c ON c.id=v.clube_id WHERE v.id=@id FOR UPDATE", new { id });
        var now = DateTime.UtcNow;
        if (v.S("status")is "RASCUNHO" or "CANCELADA" or "ENCERRADA")
            return v;
        if (v.D("fim_em") <= now)
        {
            await w.Exec("UPDATE votacoes SET status='ENCERRADA',encerrada_em=UTC_TIMESTAMP(6) WHERE id=@id", new { id });
            return await w.Required("SELECT * FROM votacoes WHERE id=@id", new { id });
        }

        if (v.D("inicio_em") <= now && v.Get("eleitorado_gerado_em")is null)
        {
            await w.Exec("UPDATE votacoes SET status='ABERTA' WHERE id=@id", new { id });
            var day = Regras.Today(v.S("fuso_horario"));
            var electors = await w.Rows("""
                SELECT m.id,m.pessoa_id,c.id AS contrato_id FROM membros_contrato m
                JOIN contratos c ON c.id=m.contrato_id
                WHERE m.tipo='TITULAR' AND m.status='ATIVO' AND c.status='VIGENTE'
                  AND c.clube_origem_id=@unit AND c.data_inicio<=@day AND c.data_fim>@day
                ORDER BY c.id DESC FOR UPDATE
                """, new { unit = v.I("clube_id"), day });
            foreach (var e in electors.DistinctBy(e => e.I("pessoa_id")))
            {
                if (v.B("exige_adimplencia") && await Overdue(w, e.I("contrato_id"), day) > 0)
                    continue;
                await w.Insert("eleitores_votacao", new { votacao_id = id, pessoa_id = e.I("pessoa_id"), membro_contrato_id = e.Id });
            }

            await w.Exec("UPDATE votacoes SET eleitorado_gerado_em=UTC_TIMESTAMP(6) WHERE id=@id", new { id });
        }

        return await w.Required("SELECT * FROM votacoes WHERE id=@id", new { id });
    }

    public async Task<object> CreatePoll(Trabalho w, Ator a, AdminPollInput d)
    {
        var unit = Regras.Id(d.UnitId);
        a.ExigirUnidade(unit);
        await w.Required("SELECT id FROM clubes WHERE id=@unit AND status='ATIVO'", new { unit });
        var start = Regras.Utc(d.Start);
        var end = Regras.Utc(d.End);
        Regras.Exigir(end > start && (d.Closed || end > DateTime.UtcNow), "Confira o período da votação.");
        Regras.Exigir(d.Quorum is >= 0 and <= 100, "Quórum deve ser de 0 a 100.");
        var options = d.Options.Select(o => Regras.Texto(o, "a alternativa", 200)).ToArray();
        Regras.Exigir(options.Length is >= 2 and <= 8 && options.Distinct(StringComparer.OrdinalIgnoreCase).Count() == options.Length, "Informe de 2 a 8 alternativas diferentes.");
        var id = await w.Insert("votacoes", new { clube_id = unit, criada_por_usuario_id = a.Id, motivo = Regras.Texto(d.Title, "o motivo", 200), descricao = Regras.Texto(d.Description, "a descrição", 10000), regras = Regras.Texto(d.Rules, "as regras", 10000), inicio_em = start, fim_em = end, exige_adimplencia = d.RequirePaid, quorum_percentual = d.Quorum, status = "RASCUNHO" });
        for (int i = 0; i < options.Length; i++)
            await w.Insert("opcoes_votacao", new { votacao_id = id, texto = options[i], ordem = i + 1 });
        await w.Exec("UPDATE votacoes SET status=@state,publicada_em=UTC_TIMESTAMP(6),encerrada_em=@closed WHERE id=@id", new { state = d.Closed ? "ENCERRADA" : "AGENDADA", closed = d.Closed ? DateTime.UtcNow : (DateTime? )null, id });
        var row = await TickPoll(w, id);
        await Execucao.Audit(w, a, "CRIAR_VOTACAO", "votacoes", id, unit);
        return await AdminPoll(w, row);
    }

    public async Task<object?> ClosePoll(Trabalho w, Ator a, int id, VersionInput d)
    {
        var p = await AdminRecord(w, a, "votacoes", id, true);
        Regras.Version(p, d.Version);
        Regras.Exigir(p.S("status")is "ABERTA" or "AGENDADA", "Votação já encerrada.", 409);
        await w.Exec("UPDATE votacoes SET status='ENCERRADA',encerrada_em=UTC_TIMESTAMP(6) WHERE id=@id", new { id });
        await Execucao.Audit(w, a, "ENCERRAR_VOTACAO", "votacoes", id, p.I("clube_id"));
        return null;
    }

    public static object MobilePoll(Registro p) => new
    {
        id = p.Id,
        unidadeId = p.I("clube_id"),
        motivo = p.S("motivo"),
        descricao = p.S("descricao") + "\n\n" + p.S("regras"),
        inicioUtc = p.D("inicio_em"),
        fimUtc = p.D("fim_em"),
        encerrada = p.S("status")is "ENCERRADA" or "CANCELADA" || p.D("fim_em") <= DateTime.UtcNow
    };
    public async Task<object> AdminPoll(Trabalho w, Registro p)
    {
        var options = await w.Rows("SELECT * FROM opcoes_votacao WHERE votacao_id=@id ORDER BY ordem", new { id = p.Id });
        var electors = await w.Rows("SELECT membro_contrato_id FROM eleitores_votacao WHERE votacao_id=@id ORDER BY id", new { id = p.Id });
        return new
        {
            id = p.Ids(),
            unitId = p.Ids("clube_id"),
            title = p.S("motivo"),
            description = p.S("descricao"),
            rules = p.S("regras"),
            start = p.Iso("inicio_em"),
            end = p.Iso("fim_em"),
            closedAt = p.Iso("encerrada_em"),
            quorum = p.I("quorum_percentual"),
            requirePaid = p.B("exige_adimplencia"),
            options = options.Select(o => new { id = o.Ids(), text = o.S("texto") }).ToArray(),
            electorate = electors.Select(e => e.Ids("membro_contrato_id")).ToArray(),
            creatorId = p.Ids("criada_por_usuario_id"),
            createdAt = p.Iso("created_at"),
            version = p.I("versao")
        };
    }

    public async Task<object> Polls(Trabalho w, Ator a, int unit)
    {
        await Membership(w, a, unit);
        await TickPolls(w, unit);
        return (await w.Rows("SELECT * FROM votacoes WHERE clube_id=@unit AND status<>'RASCUNHO' ORDER BY inicio_em DESC", new { unit })).Select(MobilePoll).ToArray();
    }

    async Task<string> VoteBlock(Trabalho w, Ator a, Registro m, Registro poll)
    {
        if (m.S("tipo") != "TITULAR")
            return "Somente o titular/fundador tem direito a voto.";
        if (m.I("clube_origem_id") != poll.I("clube_id"))
            return "O direito a voto pertence à unidade de origem do contrato.";
        if (poll.S("status") != "ABERTA" || DateTime.UtcNow < poll.D("inicio_em") || DateTime.UtcNow >= poll.D("fim_em"))
            return "Votação fora do período de participação.";
        var day = Regras.Today(m.S("fuso_horario"));
        if (m.S("status") != "VIGENTE" || m.Day("data_inicio") > day || m.Day("data_fim") <= day)
            return "Associação suspensa ou fora da vigência.";
        if (poll.B("exige_adimplencia") && await Overdue(w, m.Id, day) > 0)
            return "Esta votação exige parcelas em dia.";
        var e = await w.One("SELECT id FROM eleitores_votacao WHERE votacao_id=@poll AND pessoa_id=@person", new { poll = poll.Id, person = a.PessoaId });
        if (e is null)
            return "Você não integra o eleitorado definido na abertura.";
        if (await w.Scalar<int>("SELECT COUNT(*) FROM votos WHERE eleitor_id=@id", new { id = e.Id }) > 0)
            return "Seu voto já foi registrado.";
        return "";
    }

    public async Task<object> PollDetail(Trabalho w, Ator a, int unit, int id)
    {
        var m = await Membership(w, a, unit);
        await w.Required("SELECT id FROM votacoes WHERE id=@id AND clube_id=@unit AND status<>'RASCUNHO'", new { id, unit });
        var poll = await TickPoll(w, id);
        var options = await w.Rows("SELECT * FROM vw_votacoes_resultados WHERE votacao_id=@id ORDER BY ordem", new { id });
        var vote = await w.One("SELECT v.* FROM votos v JOIN eleitores_votacao e ON e.id=v.eleitor_id WHERE v.votacao_id=@id AND e.pessoa_id=@person", new { id, person = a.PessoaId });
        var block = await VoteBlock(w, a, m, poll);
        return new
        {
            votacao = MobilePoll(poll),
            opcoes = options.Select(o => new { id = o.I("opcao_id"), texto = o.S("texto"), votos = o.I("quantidade_votos") }).ToArray(),
            meuVoto = vote is null ? null : new
            {
                id = vote.Id,
                votacaoId = id,
                usuarioId = a.Id,
                opcaoId = vote.I("opcao_id"),
                criadoUtc = vote.D("created_at")
            },
            podeVotar = block.Length == 0,
            impedimento = block,
            total = options.Sum(o => o.I("quantidade_votos"))
        };
    }

    public async Task<object?> Vote(Trabalho w, Ator a, int unit, int id, VotoRequest d)
    {
        await Membership(w, a, unit);
        await w.Required("SELECT id FROM votacoes WHERE id=@id AND clube_id=@unit AND status<>'RASCUNHO'", new { id, unit });
        var poll = await TickPoll(w, id);
        var m = await Membership(w, a, unit, lockContract: true);
        var block = await VoteBlock(w, a, m, poll);
        Regras.Exigir(block.Length == 0, block, 409);
        var e = await w.Required("SELECT id FROM eleitores_votacao WHERE votacao_id=@id AND pessoa_id=@person", new { id, person = a.PessoaId });
        await w.Required("SELECT id FROM opcoes_votacao WHERE id=@option AND votacao_id=@id", new { option = d.OpcaoId, id });
        await w.Insert("votos", new { votacao_id = id, eleitor_id = e.Id, opcao_id = d.OpcaoId, recibo = Guid.NewGuid().ToString() });
        // Nao gravar a alternativa na auditoria.
        await Execucao.Audit(w, a, "REGISTRAR_PARTICIPACAO", "votacoes", id, unit);
        return null;
    }
}
