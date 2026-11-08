using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService(Autenticacao auth)
{
    public const string UnitSql = """
        SELECT c.*,ci.nome AS cidade,e.uf FROM clubes c JOIN cidades ci ON ci.id=c.cidade_id JOIN estados e ON e.id=ci.estado_id
        """;
    public static object MobileUnit(Registro u) => new
    {
        id = u.Id,
        nome = u.S("nome"),
        cidade = u.S("cidade"),
        endereco = u.S("logradouro") + ", " + u.S("numero"),
        fusoHorario = u.S("fuso_horario")
    };
    public static object AdminUnit(Registro u) => new
    {
        id = u.Ids(),
        name = u.S("nome"),
        acronym = u.S("sigla"),
        description = u.S("descricao"),
        street = u.S("logradouro"),
        number = u.S("numero"),
        complement = u.S("complemento"),
        district = u.S("bairro"),
        city = u.S("cidade"),
        uf = u.S("uf"),
        cep = u.S("cep"),
        gender = "",
        maritalStatus = "",
        phone = u.S("telefone"),
        email = u.S("email"),
        timezone = u.S("fuso_horario"),
        active = u.S("status") == "ATIVO",
        image = u.S("imagem"),
        version = u.I("versao")
    };
    public async Task<object> PublicUnits(Trabalho w) => (await w.Rows(UnitSql + " WHERE c.status='ATIVO' ORDER BY c.nome")).Select(MobileUnit).ToArray();
    public async Task<int> City(Trabalho w, string city, string uf)
    {
        city = Regras.Texto(city, "a cidade", 120);
        uf = Regras.Texto(uf, "a UF", 2, 2).ToUpperInvariant();
        var state = await w.One("SELECT id FROM estados WHERE uf=@uf", new { uf }) ?? throw new ApiException(422, "UF inválida.");
        await w.Exec("INSERT INTO cidades(estado_id,nome) VALUES (@state,@city) ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id)", new { state = state.Id, city });
        return await w.Scalar<int>("SELECT id FROM cidades WHERE estado_id=@state AND nome=@city", new { state = state.Id, city });
    }

    public async Task<Registro> Membership(Trabalho w, Ator a, int? unit = null, bool use = false, bool holder = false, bool lockContract = false)
    {
        var r = await w.One("""
            SELECT c.*,m.id AS membro_id,m.tipo,m.status AS membro_status,m.versao AS membro_versao,
                pv.plano_id,pv.numero_versao AS versao_plano,pv.nome AS plano_nome,pv.abrangencia,pv.limite_beneficiarios,
                pv.limite_dependentes,pv.limite_convidados,pv.carencia_dias,pv.limite_reservas_futuras,pv.beneficios,pv.regras,
                cp.nome AS condicao_nome,b.fuso_horario,b.nome AS unidade_nome
            FROM membros_contrato m JOIN contratos c ON c.id=m.contrato_id
            JOIN plano_versoes pv ON pv.id=c.plano_versao_id JOIN condicoes_pagamento cp ON cp.id=c.condicao_pagamento_id
            JOIN clubes b ON b.id=c.clube_origem_id WHERE m.pessoa_id=@pid
              AND m.status NOT IN ('REMOVIDO','REJEITADO') AND c.status<>'CANCELADO'
            ORDER BY c.data_inicio DESC,c.id DESC LIMIT 1
            """, new { pid = a.PessoaId }) ?? throw new ApiException(403, "Nenhuma associação disponível para este acesso.");
        if (lockContract)
        {
            await w.Required("SELECT id FROM contratos WHERE id=@id FOR UPDATE", new { id = r.Id });
            return await Membership(w, a, unit, use, holder, false);
        }

        Regras.Exigir(r.S("membro_status") == "ATIVO", "O cadastro do associado precisa estar aprovado.", 403);
        if (holder)
            Regras.Exigir(r.S("tipo") == "TITULAR", "Ação exclusiva do titular do contrato.", 403);
        if (unit is int id)
        {
            Regras.Exigir(id == r.I("clube_origem_id") || r.S("abrangencia") == "REDE", "Seu plano não autoriza esta unidade.", 403);
            Regras.Exigir(await w.Scalar<int>("SELECT COUNT(*) FROM clubes WHERE id=@id AND status='ATIVO'", new { id }) == 1, "Unidade indisponível.", 403);
        }

        if (use)
        {
            var today = Regras.Today(r.S("fuso_horario"));
            Regras.Exigir(r.S("status") == "VIGENTE" && r.Day("data_inicio") <= today && r.Day("data_fim") > today, "A associação está suspensa ou fora da vigência. Consulte Minha Conta.", 403);
        }

        return r;
    }

    public static object Association(Registro m, int user) => new
    {
        id = m.Id,
        usuarioId = user,
        planoId = m.I("plano_id"),
        unidadeOrigemId = m.I("clube_origem_id"),
        planoNome = m.S("plano_nome"),
        todaRede = m.S("abrangencia") == "REDE",
        limiteBeneficiarios = m.I("limite_beneficiarios"),
        condicao = m.S("tipo") == "TITULAR" ? m.S("condicao_nome") : "",
        valorTotalCentavos = m.S("tipo") == "TITULAR" ? m.L("valor_total_centavos") : 0L,
        parcelas = m.S("tipo") == "TITULAR" ? m.I("quantidade_parcelas") : 0,
        inicioLocal = m.Date("data_inicio"),
        fimLocal = m.Date("data_fim"),
        versaoPlano = m.I("versao_plano"),
        beneficios = m.S("beneficios"),
        regras = m.S("regras"),
        status = Regras.ContractStatus(m.S("status"))
    };
    public async Task<object> Profile(Trabalho w, Ator a)
    {
        var m = await Membership(w, a);
        var u = await w.Required(Autenticacao.UserSql + " WHERE u.id=@id", new { id = a.Id });
        return new
        {
            id = a.Id,
            nome = u.S("nome"),
            email = u.S("email_login"),
            tipoSocio = Regras.MemberType(m.S("tipo")),
            titular = m.S("tipo") == "TITULAR",
            iniciais = string.Concat(u.S("nome").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => s[..1].ToUpperInvariant()))
        };
    }

    public async Task<object> Context(Trabalho w, Ator a, int? unit)
    {
        var m = await Membership(w, a, unit);
        var u = await w.Required(UnitSql + " WHERE c.id=@id", new { id = unit ?? m.I("clube_origem_id") });
        return new
        {
            perfil = await Profile(w, a),
            associacao = Association(m, a.Id),
            unidade = MobileUnit(u)
        };
    }

    public async Task<object> Units(Trabalho w, Ator a)
    {
        var m = await Membership(w, a);
        return (await w.Rows(UnitSql + " WHERE c.status='ATIVO' AND (@rede=1 OR c.id=@id) ORDER BY c.nome", new { rede = m.S("abrangencia") == "REDE", id = m.I("clube_origem_id") })).Select(MobileUnit).ToArray();
    }

    public async Task<Registro> AdminRecord(Trabalho w, Ator a, string table, int id, bool lockRow = false, string unitColumn = "clube_id")
    {
        var r = await w.Required($"SELECT * FROM `{table}` WHERE id=@id" + (lockRow ? " FOR UPDATE" : ""), new { id });
        a.ExigirUnidade(r.I(unitColumn));
        return r;
    }

    public static string UnitFilter(Ator a, string alias, string column = "clube_id") => a.Geral ? "1=1" : $"{alias}.{column}={a.UnidadeId}";
}
