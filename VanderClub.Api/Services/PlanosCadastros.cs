using System.Security.Cryptography;
using System.Text;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public async Task<object> PublicPlans(Trabalho w)
    {
        var list = await w.Rows("SELECT * FROM vw_planos_publicos ORDER BY nome");
        var result = new List<object>();
        foreach (var p in list)
        {
            var conditions = await w.Rows("SELECT * FROM condicoes_pagamento WHERE plano_versao_id=@id ORDER BY quantidade_parcelas", new { id = p.I("plano_versao_id") });
            result.Add(new { id = p.I("plano_id"), nome = p.S("nome"), descricao = p.S("descricao"), todaRede = p.S("abrangencia") == "REDE", limiteBeneficiarios = p.I("limite_beneficiarios"), versaoId = p.I("plano_versao_id"), duracaoMeses = p.I("duracao_meses"), beneficios = p.S("beneficios"), regras = p.S("regras"), condicoes = conditions.Select(c => new { id = c.Id, nome = c.S("nome"), meio = c.S("meio"), parcelas = c.I("quantidade_parcelas"), valorTotalCentavos = c.L("valor_total_centavos") }).ToArray() });
        }

        return result;
    }

    public async Task<object?> Register(Trabalho w, Cadastro d)
    {
        var name = Regras.Texto(d.Nome, "o nome", 160, 3);
        var email = Regras.Email(d.Email);
        var cpf = Regras.Cpf(d.Cpf);
        var birth = Regras.Birth(d.Nascimento, true);
        var phone = Regras.Phone(d.Telefone);
        var password = auth.HashPassword(d.Senha);
        Regras.Exigir(d.AceitouTermos, "Leia e aceite os termos do plano.");
        await w.Required("SELECT id FROM planos WHERE id=@id AND ativo=1 FOR UPDATE", new { id = d.PlanoId });
        var p = await w.Required("SELECT * FROM vw_planos_publicos WHERE plano_id=@id", new { id = d.PlanoId });
        if (p.I("plano_versao_id") != d.PlanoVersaoId)
            throw ApiException.Conflict("A oferta mudou. Selecione o plano novamente.");
        var condition = await w.Required("SELECT * FROM condicoes_pagamento WHERE id=@id AND plano_versao_id=@version", new { id = d.CondicaoPagamentoId, version = d.PlanoVersaoId });
        var unit = await w.Required("SELECT * FROM clubes WHERE id=@id AND status='ATIVO'", new { id = d.UnidadeId });
        var city = await City(w, d.Cidade, d.Uf);
        var person = await w.Insert("pessoas", new { nome = name, cpf, nascimento = birth, telefone = phone, email_contato = email, cidade_id = city, logradouro = Regras.Texto(d.Endereco, "o endereço", 200), numero = Regras.Texto(d.Numero, "o número", 20), complemento = Regras.Opcional(d.Complemento, 100), bairro = Regras.Texto(d.Bairro, "o bairro", 100), cep = Regras.Cep(d.Cep) });
        var user = await w.Insert("usuarios", new { pessoa_id = person, email_login = email, email_normalizado = email, senha_hash = password, perfil = "ASSOCIADO", status = "ATIVO", security_stamp = Guid.NewGuid().ToString() });
        var start = Regras.Today(unit.S("fuso_horario"));
        var end = start.AddMonths(p.I("duracao_meses"));
        var due = start.AddDays(7);
        if (due.Day > 28)
            due = due.AddDays(28 - due.Day);
        var c = await w.Insert("contratos", new { numero = Regras.Protocolo("VC"), titular_pessoa_id = person, clube_origem_id = d.UnidadeId, plano_versao_id = d.PlanoVersaoId, condicao_pagamento_id = d.CondicaoPagamentoId, data_inicio = start, data_fim = end, valor_total_centavos = condition.L("valor_total_centavos"), quantidade_parcelas = condition.I("quantidade_parcelas"), dia_vencimento = due.Day, status = "VIGENTE", ativado_em = DateTime.UtcNow });
        await w.Insert("membros_contrato", new { contrato_id = c, pessoa_id = person, tipo = "TITULAR", status = "ATIVO", aprovado_em = DateTime.UtcNow });
        var total = condition.L("valor_total_centavos");
        var count = condition.I("quantidade_parcelas");
        for (int i = 0; i < count; i++)
            await w.Insert("faturas", new { contrato_id = c, numero = Regras.Protocolo("F"), numero_parcela = i + 1, descricao = $"Parcela {i + 1:00}/{count:00} — {p.S("nome")}", vencimento = due.AddMonths(i * condition.I("intervalo_meses")), valor_centavos = total / count + (i < total % count ? 1 : 0), status = "ABERTA" });
        await w.Insert("aceites_documentos", new { documento_id = p.I("documento_termos_id"), pessoa_id = person, registrado_por_usuario_id = user, contrato_id = c, manifestacao = "ACEITE", canal = "MOBILE" });
        await Execucao.Audit(w, new(user, person, name, "ASSOCIADO", 0, 0), "ADESAO", "contratos", c, d.UnidadeId);
        return null;
    }

    public async Task<object> SaveUnit(Trabalho w, Ator a, int id, AdminUnitInput d)
    {
        a.ExigirGeral();
        if (id > 0)
            Regras.Version(await w.Required("SELECT * FROM clubes WHERE id=@id FOR UPDATE", new { id }), d.Version);
        var city = await City(w, d.City, d.Uf);
        Regras.Zone(d.Timezone);
        var values = new
        {
            nome = Regras.Texto(d.Name, "o nome", 160),
            sigla = Regras.Texto(d.Acronym, "a sigla", 20),
            descricao = Regras.Opcional(d.Description, 10000),
            cidade_id = city,
            logradouro = Regras.Texto(d.Street, "a rua", 200),
            numero = Regras.Texto(d.Number, "o número", 20),
            complemento = Regras.Opcional(d.Complement, 100),
            bairro = Regras.Texto(d.District, "o bairro", 100),
            cep = Regras.Cep(d.Cep),
            telefone = Regras.Phone(d.Phone),
            email = Regras.Email(d.Email),
            imagem = Regras.Image(d.Image),
            fuso_horario = d.Timezone,
            status = d.Active ? "ATIVO" : "INATIVO"
        };
        if (id == 0)
            id = await w.Insert("clubes", values);
        else
            await UpdateValues(w, "clubes", id, values);
        await Execucao.Audit(w, a, "SALVAR_UNIDADE", "clubes", id, id);
        return AdminUnit(await w.Required(UnitSql + " WHERE c.id=@id", new { id }));
    }

    public static Task<int> UpdateValues(Trabalho w, string table, int id, object values)
    {
        var names = values.GetType().GetProperties().Select(x => x.Name);
        var dp = new Dapper.DynamicParameters(values);
        dp.Add("recordId", id);
        return w.Exec($"UPDATE `{table}` SET {string.Join(',', names.Select(n => $"`{n}`=@{n}"))} WHERE id=@recordId", dp);
    }

    public async Task<object> SaveAdmin(Trabalho w, Ator a, int id, AdminAdministratorInput d)
    {
        a.ExigirGeral();
        Registro? old = null;
        if (id > 0)
        {
            old = await w.Required("SELECT * FROM usuarios WHERE id=@id AND perfil IN ('ADM_GERAL','ADM_LOCAL') FOR UPDATE", new { id });
            Regras.Version(old, d.Version);
        }

        var general = old?.S("perfil") == "ADM_GERAL";
        var unit = general ? 0 : Regras.Id(d.UnitId);
        if (unit > 0)
            await w.Required("SELECT id FROM clubes WHERE id=@unit AND status='ATIVO'", new { unit });
        if (general)
            Regras.Exigir(d.Active, "O Presidente não pode desativar o próprio acesso.");
        var email = Regras.Email(d.Email);
        var login = Regras.Email(d.Login);
        var city = await City(w, d.City, d.Uf);
        var pv = new
        {
            nome = Regras.Texto(d.Name, "o nome", 160, 3),
            cpf = Regras.Cpf(d.Cpf),
            nascimento = Regras.Birth(d.Birth, true),
            telefone = Regras.Phone(d.Phone),
            email_contato = email,
            cidade_id = city,
            logradouro = Regras.Texto(d.Street, "a rua", 200),
            numero = Regras.Texto(d.Number, "o número", 20),
            complemento = Regras.Opcional(d.Complement, 100),
            bairro = Regras.Texto(d.District, "o bairro", 100),
            cep = Regras.Cep(d.Cep),
            sexo = Regras.Opcional(d.Gender, 30),
            estado_civil = Regras.Opcional(d.MaritalStatus, 40)
        };
        var person = old?.I("pessoa_id") ?? await w.Insert("pessoas", pv);
        if (old is not null)
            await UpdateValues(w, "pessoas", person, pv);
        var hash = !string.IsNullOrEmpty(d.Password) ? auth.HashPassword(d.Password) : old?.S("senha_hash") ?? throw new ApiException(422, "Informe a senha do novo administrador.");
        var uv = new
        {
            pessoa_id = person,
            email_login = login,
            email_normalizado = login,
            senha_hash = hash,
            perfil = general ? "ADM_GERAL" : "ADM_LOCAL",
            status = d.Active ? "ATIVO" : "INATIVO",
            security_stamp = Guid.NewGuid().ToString()
        };
        if (id == 0)
            id = await w.Insert("usuarios", uv);
        else
        {
            await UpdateValues(w, "usuarios", id, uv);
            await w.Exec("UPDATE sessoes SET revogada_em=UTC_TIMESTAMP(6) WHERE usuario_id=@id AND id<>@current AND revogada_em IS NULL", new { id, current = a.SessaoId });
        }

        if (!general)
            await w.Exec("INSERT INTO administradores_clubes(usuario_id,clube_id,atribuido_por_usuario_id,ativo) VALUES (@id,@unit,@author,1) ON DUPLICATE KEY UPDATE clube_id=@unit,atribuido_por_usuario_id=@author,ativo=1", new { id, unit, author = a.Id });
        await Execucao.Audit(w, a, "SALVAR_ADMINISTRADOR", "usuarios", id, unit == 0 ? null : unit);
        return Autenticacao.AdminActor(await w.Required(Autenticacao.UserSql + " WHERE u.id=@id", new { id }));
    }

    public async Task<object> SavePlan(Trabalho w, Ator a, int id, AdminPlanInput d)
    {
        a.ExigirGeral();
        Registro? old = null;
        if (id > 0)
        {
            old = await w.Required("SELECT * FROM planos WHERE id=@id FOR UPDATE", new { id });
            Regras.Version(old, d.Version);
            Regras.Exigir(old.B("ativo"), "Plano arquivado. Cadastre uma nova oferta.");
        }

        Regras.Exigir(d.Scope is "LOCAL" or "REDE", "Abrangência inválida.");
        Regras.Exigir(d.Quota is >= 0 and <= 50 && d.DependentLimit >= 0 && d.GuestLimit >= 0 && d.DependentLimit <= d.Quota && d.GuestLimit <= d.Quota, "Confira as cotas total, de dependentes e de convidados.");
        Regras.Exigir(d.DurationMonths is >= 1 and <= 120 && d.GraceDays is >= 0 and <= 90 && d.FutureReservationsLimit is >= 1 and <= 100, "Confira duração, carência e limite de reservas.");
        Regras.Exigir(d.Conditions.Count == 2 && d.Conditions.All(c => c is not null), "Informe as duas condições de pagamento.");
        foreach (var c in d.Conditions)
        {
            Regras.Texto(c.Name, "a condição", 100);
            Regras.Exigir(c.Method is "PIX" or "BOLETO" or "CARTAO" or "TRANSFERENCIA", "Meio de pagamento inválido.");
            Regras.Exigir(c.Installments is >= 1 and <= 120 && c.TotalCents >= c.Installments && c.TotalCents <= Regras.MaxCentavos, "Valor ou parcelamento inválido.");
        }

        Regras.Exigir(d.Conditions.Select(c => c.Name.Trim().ToLowerInvariant()).Distinct().Count() == 2, "As condições devem ter nomes diferentes.");
        var name = Regras.Texto(d.Name, "o nome", 120);
        var terms = Regras.Texto(d.Terms, "os termos", 10000);
        var benefits = Regras.Texto(d.Benefits, "os benefícios", 10000);
        var plan = new
        {
            codigo = Regras.Texto(d.Code, "o código", 30),
            nome = name,
            descricao = Regras.Opcional(d.Description, 10000),
            ativo = true
        };
        if (id == 0)
            id = await w.Insert("planos", plan);
        else
            await UpdateValues(w, "planos", id, plan);
        var num = await w.Scalar<int>("SELECT COALESCE(MAX(numero_versao),0)+1 FROM plano_versoes WHERE plano_id=@id", new { id });
        var document = await w.Insert("documentos", new { tipo = "TERMOS_ASSOCIACAO", codigo_versao = $"P{id}-V{num}", titulo = $"Condições do plano {name}", conteudo = terms, sha256 = SHA256.HashData(Encoding.UTF8.GetBytes(terms)), publicado_em = DateTime.UtcNow });
        var version = await w.Insert("plano_versoes", new { plano_id = id, numero_versao = num, nome = name, descricao = plan.descricao, abrangencia = d.Scope, duracao_meses = d.DurationMonths, limite_beneficiarios = d.Quota, limite_dependentes = d.DependentLimit, limite_convidados = d.GuestLimit, carencia_dias = d.GraceDays, limite_reservas_futuras = d.FutureReservationsLimit, beneficios = benefits, regras = terms, documento_termos_id = document, status = "RASCUNHO" });
        foreach (var c in d.Conditions)
            await w.Insert("condicoes_pagamento", new { plano_versao_id = version, nome = c.Name.Trim(), meio = c.Method, valor_total_centavos = c.TotalCents, quantidade_parcelas = c.Installments, intervalo_meses = 1 });
        await w.Exec("UPDATE plano_versoes SET status='PUBLICADA',publicada_em=UTC_TIMESTAMP(6) WHERE id=@version", new { version });
        await Execucao.Audit(w, a, "VERSIONAR_PLANO", "planos", id);
        return await PlanProjection(w, id);
    }

    public async Task<object?> ArchivePlan(Trabalho w, Ator a, int id, VersionInput d)
    {
        a.ExigirGeral();
        var p = await w.Required("SELECT * FROM planos WHERE id=@id FOR UPDATE", new { id });
        Regras.Version(p, d.Version);
        await w.Exec("UPDATE planos SET ativo=0 WHERE id=@id", new { id });
        await Execucao.Audit(w, a, "ARQUIVAR_PLANO", "planos", id);
        return null;
    }

    public static object ConditionProjection(Registro c) => new
    {
        id = c.Ids(),
        name = c.S("nome"),
        method = c.S("meio"),
        totalCents = c.L("valor_total_centavos"),
        installments = c.I("quantidade_parcelas"),
        intervalMonths = c.I("intervalo_meses")
    };
    public async Task<object> VersionProjection(Trabalho w, Registro v)
    {
        return new
        {
            id = v.Ids(),
            number = v.I("numero_versao"),
            name = v.S("nome"),
            description = v.S("descricao"),
            scope = v.S("abrangencia"),
            quota = v.I("limite_beneficiarios"),
            dependentLimit = v.I("limite_dependentes"),
            guestLimit = v.I("limite_convidados"),
            durationMonths = v.I("duracao_meses"),
            graceDays = v.I("carencia_dias"),
            futureReservationsLimit = v.I("limite_reservas_futuras"),
            benefits = v.S("beneficios"),
            terms = v.S("regras"),
            conditions = (await w.Rows("SELECT * FROM condicoes_pagamento WHERE plano_versao_id=@id ORDER BY id", new { id = v.Id })).Select(ConditionProjection).ToArray(),
            publishedAt = v.Iso("publicada_em")
        };
    }

    public async Task<object> PlanProjection(Trabalho w, int id)
    {
        var p = await w.Required("SELECT * FROM planos WHERE id=@id", new { id });
        var vs = await w.Rows("SELECT * FROM plano_versoes WHERE plano_id=@id AND status<>'RASCUNHO' ORDER BY numero_versao", new { id });
        var result = new List<object>();
        foreach (var v in vs)
            result.Add(await VersionProjection(w, v));
        return new
        {
            id = p.Ids(),
            code = p.S("codigo"),
            active = p.B("ativo"),
            currentVersionId = vs.LastOrDefault(x => x.S("status") == "PUBLICADA")?.Ids() ?? "",
            versions = result,
            version = p.I("versao")
        };
    }
}
