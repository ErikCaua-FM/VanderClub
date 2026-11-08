using System.Text.Json;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public async Task<object> Me(Trabalho w, Ator a)
    {
        var m = await Membership(w, a);
        var p = await w.Required("SELECT p.*,ci.nome AS cidade,e.uf,u.email_login FROM pessoas p JOIN usuarios u ON u.pessoa_id=p.id LEFT JOIN cidades ci ON ci.id=p.cidade_id LEFT JOIN estados e ON e.id=ci.estado_id WHERE p.id=@id", new { id = a.PessoaId });
        var interests = await w.Rows("SELECT i.nome FROM pessoas_interesses pi JOIN interesses i ON i.id=pi.interesse_id WHERE pi.pessoa_id=@id ORDER BY i.nome", new { id = a.PessoaId });
        return new
        {
            dados = new
            {
                versao = p.I("versao"),
                nome = p.S("nome"),
                email = p.S("email_login"),
                cpf = p.S("cpf"),
                nascimento = p.Date("nascimento"),
                telefone = p.S("telefone"),
                endereco = p.S("logradouro"),
                numero = p.S("numero"),
                complemento = p.S("complemento"),
                bairro = p.S("bairro"),
                cidade = p.S("cidade"),
                uf = p.S("uf"),
                cep = p.S("cep"),
                sexo = p.S("sexo"),
                estadoCivil = p.S("estado_civil"),
                interesses = string.Join(", ", interests.Select(i => i.S("nome")))
            },
            associacao = Association(m, a.Id),
            tipoSocio = Regras.MemberType(m.S("tipo")),
            status = Regras.ContractStatus(m.S("status")),
            unidadeOrigem = m.S("unidade_nome")
        };
    }

    public async Task<object?> SaveMe(Trabalho w, Ator a, DadosPessoais d)
    {
        var m = await Membership(w, a);
        var p = await w.Required("SELECT * FROM pessoas WHERE id=@id FOR UPDATE", new { id = a.PessoaId });
        Regras.Version(p, d.Versao);
        var email = Regras.Email(d.Email);
        var city = await City(w, d.Cidade, d.Uf);
        await UpdateValues(w, "pessoas", a.PessoaId, new { nome = Regras.Texto(d.Nome, "o nome", 160, 3), email_contato = email, cpf = Regras.Cpf(d.Cpf), nascimento = Regras.Birth(d.Nascimento, m.S("tipo") == "TITULAR"), telefone = Regras.Phone(d.Telefone), logradouro = Regras.Texto(d.Endereco, "o endereço", 200), numero = Regras.Texto(d.Numero, "o número", 20), complemento = Regras.Opcional(d.Complemento, 100), bairro = Regras.Texto(d.Bairro, "o bairro", 100), cidade_id = city, cep = Regras.Cep(d.Cep), sexo = Regras.Opcional(d.Sexo, 30), estado_civil = Regras.Opcional(d.EstadoCivil, 40) });
        await w.Exec("UPDATE usuarios SET email_verificado_em=IF(email_login=@email,email_verificado_em,NULL),email_login=@email,email_normalizado=@email WHERE id=@id", new { email, id = a.Id });
        var interests = (d.Interesses ?? "").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Regras.Exigir(interests.Length <= 20, "Informe no máximo 20 interesses.");
        await w.Exec("DELETE FROM pessoas_interesses WHERE pessoa_id=@id", new { id = a.PessoaId });
        foreach (var item in interests)
        {
            var name = Regras.Texto(item, "o interesse", 80);
            await w.Exec("INSERT INTO interesses(nome) VALUES (@name) ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id)", new { name });
            var iid = await w.Scalar<int>("SELECT id FROM interesses WHERE nome=@name", new { name });
            await w.Insert("pessoas_interesses", new { pessoa_id = a.PessoaId, interesse_id = iid });
        }

        await Execucao.Audit(w, a, "ALTERAR_PERFIL", "pessoas", a.PessoaId, m.I("clube_origem_id"));
        return null;
    }

    const string BenefSql = """
        SELECT m.*,p.nome,p.cpf,p.nascimento,p.telefone,u.id AS usuario_vinculado_id,
        (SELECT sv.dados_propostos FROM solicitacoes_vinculo sv WHERE sv.membro_contrato_id=m.id AND sv.status='PENDENTE' LIMIT 1) AS propostos,
        (SELECT sv.justificativa FROM solicitacoes_vinculo sv WHERE sv.membro_contrato_id=m.id ORDER BY sv.id DESC LIMIT 1) AS observacao_admin
        FROM membros_contrato m JOIN pessoas p ON p.id=m.pessoa_id LEFT JOIN usuarios u ON u.pessoa_id=p.id
        """;
    static BeneficiarioRequest Proposed(Registro r)
    {
        var s = r.S("propostos");
        if (!string.IsNullOrEmpty(s))
            return JsonSerializer.Deserialize<BeneficiarioRequest>(s, Execucao.Json)!;
        return new()
        {
            Nome = r.S("nome"),
            Cpf = r.S("cpf"),
            Nascimento = r.Day("nascimento"),
            Telefone = r.S("telefone"),
            Tipo = Regras.MemberType(r.S("tipo")),
            Parentesco = r.S("parentesco")
        };
    }

    public async Task<object> Beneficiaries(Trabalho w, Ator a)
    {
        var c = await Membership(w, a, holder: true);
        var rows = await w.Rows(BenefSql + " WHERE m.contrato_id=@id AND m.tipo<>'TITULAR' AND m.status<>'REMOVIDO' ORDER BY m.id DESC", new { id = c.Id });
        return rows.Select(r =>
        {
            var d = Proposed(r);
            return new
            {
                id = r.Id,
                versao = r.I("versao"),
                titularId = a.Id,
                usuarioVinculadoId = r.Get("usuario_vinculado_id"),
                nome = d.Nome,
                cpf = d.Cpf,
                nascimento = d.Nascimento,
                telefone = d.Telefone,
                tipo = d.Tipo,
                parentesco = d.Parentesco,
                status = Regras.Approval(r.S("status")),
                observacaoAdmin = r.S("observacao_admin"),
                criadoUtc = r.D("created_at")
            };
        }).ToArray();
    }

    public async Task<object?> SaveBeneficiary(Trabalho w, Ator a, int id, BeneficiarioRequest d)
    {
        var c = await Membership(w, a, holder: true, use: true, lockContract: true);
        Registro? old = null;
        d.Nome = Regras.Texto(d.Nome, "o nome", 160, 3);
        d.Cpf = Regras.Cpf(d.Cpf);
        d.Nascimento = Regras.Birth(d.Nascimento);
        d.Telefone = Regras.Phone(d.Telefone, false);
        d.Parentesco = Regras.Opcional(d.Parentesco, 60);
        var type = d.Tipo switch
        {
            "Dependente" => "DEPENDENTE",
            "Convidado" => "CONVIDADO",
            _ => throw new ApiException(422, "Selecione Dependente ou Convidado.")};
        if (id > 0)
        {
            old = await w.Required("SELECT * FROM membros_contrato WHERE id=@id AND contrato_id=@cid AND tipo<>'TITULAR' AND status<>'REMOVIDO' FOR UPDATE", new { id, cid = c.Id });
            Regras.Version(old, d.Versao);
            Regras.Exigir(old.S("tipo") == type, "A categoria do vínculo é fixa nesta versão. Alterar Convidado/Dependente exige a migração indicada na documentação.");
        }

        var active = await w.Rows("SELECT tipo,COUNT(*) AS total FROM membros_contrato WHERE contrato_id=@cid AND tipo<>'TITULAR' AND status IN ('PENDENTE','ATIVO','SUSPENSO') AND id<>@id GROUP BY tipo", new { cid = c.Id, id });
        Regras.Exigir(active.Sum(x => x.I("total")) < c.I("limite_beneficiarios"), "O plano já atingiu a cota de beneficiários.");
        var same = active.FirstOrDefault(x => x.S("tipo") == type)?.I("total") ?? 0;
        Regras.Exigir(same < c.I(type == "DEPENDENTE" ? "limite_dependentes" : "limite_convidados"), "Limite dessa categoria atingido.");
        var duplicate = await w.One("SELECT id FROM pessoas WHERE cpf=@cpf", new { cpf = d.Cpf });
        Regras.Exigir(duplicate is null || duplicate.Id == old?.I("pessoa_id"), "Este CPF já está cadastrado. Contate a administração.", 409);
        int person;
        if (old is null)
        {
            person = await w.Insert("pessoas", new { nome = d.Nome, cpf = d.Cpf, nascimento = d.Nascimento, telefone = d.Telefone });
            id = await w.Insert("membros_contrato", new { contrato_id = c.Id, pessoa_id = person, tipo = type, parentesco = d.Parentesco, responsavel_pessoa_id = a.PessoaId, status = "PENDENTE" });
        }
        else
        {
            person = old.I("pessoa_id");
            await w.Exec("UPDATE solicitacoes_vinculo SET status='CANCELADA',justificativa='Substituida por nova revisao do titular' WHERE membro_contrato_id=@id AND status='PENDENTE'", new { id });
            await w.Exec("UPDATE membros_contrato SET status='PENDENTE',motivo_status=NULL WHERE id=@id", new { id });
        }

        await w.Insert("solicitacoes_vinculo", new { protocolo = Regras.Protocolo("V"), membro_contrato_id = id, solicitante_usuario_id = a.Id, tipo = old is null ? "INCLUSAO" : "ALTERACAO", dados_propostos = JsonSerializer.Serialize(d, Execucao.Json), status = "PENDENTE" });
        await RevokePerson(w, person);
        await Execucao.Audit(w, a, "SOLICITAR_VINCULO", "membros_contrato", id, c.I("clube_origem_id"));
        return null;
    }

    static Task<int> RevokePerson(Trabalho w, int person) => w.Exec("UPDATE sessoes s JOIN usuarios u ON u.id=s.usuario_id SET s.revogada_em=UTC_TIMESTAMP(6) WHERE u.pessoa_id=@person AND s.revogada_em IS NULL", new { person });
    public async Task<object?> RemoveBeneficiary(Trabalho w, Ator a, int id, VersaoRequest d)
    {
        var c = await Membership(w, a, holder: true, lockContract: true);
        var m = await w.Required("SELECT * FROM membros_contrato WHERE id=@id AND contrato_id=@cid AND tipo<>'TITULAR' FOR UPDATE", new { id, cid = c.Id });
        Regras.Version(m, d.Versao);
        await w.Exec("UPDATE solicitacoes_vinculo SET status='CANCELADA',justificativa='Vinculo removido pelo titular' WHERE membro_contrato_id=@id AND status='PENDENTE'", new { id });
        await w.Exec("UPDATE membros_contrato SET status='REMOVIDO',removido_em=UTC_TIMESTAMP(6),motivo_status='Removido pelo titular' WHERE id=@id", new { id });
        await RevokePerson(w, m.I("pessoa_id"));
        await Execucao.Audit(w, a, "REMOVER_BENEFICIARIO", "membros_contrato", id, c.I("clube_origem_id"));
        return null;
    }

    public async Task<object?> ReviewBeneficiary(Trabalho w, Ator a, int id, ReviewInput d)
    {
        var link = await w.Required("SELECT contrato_id FROM membros_contrato WHERE id=@id", new { id });
        var c = await w.Required("SELECT * FROM contratos WHERE id=@id FOR UPDATE", new { id = link.I("contrato_id") });
        a.ExigirUnidade(c.I("clube_origem_id"));
        var m = await w.Required("SELECT * FROM membros_contrato WHERE id=@id AND tipo<>'TITULAR' FOR UPDATE", new { id });
        Regras.Version(m, d.Version);
        Regras.Exigir(m.S("status") == "PENDENTE", "Este vínculo não está pendente.", 409);
        var sv = await w.Required("SELECT * FROM solicitacoes_vinculo WHERE membro_contrato_id=@id AND status='PENDENTE' FOR UPDATE", new { id });
        var yes = d.Decision == "Aprovado";
        Regras.Exigir(yes || d.Decision == "Recusado", "Decisão inválida.");
        var reason = yes ? Regras.Opcional(d.Reason, 1000) : Regras.Texto(d.Reason, "o motivo da recusa", 1000);
        if (yes)
        {
            var p = JsonSerializer.Deserialize<BeneficiarioRequest>(sv.S("dados_propostos"), Execucao.Json)!;
            await UpdateValues(w, "pessoas", m.I("pessoa_id"), new { nome = p.Nome, cpf = Regras.Cpf(p.Cpf), nascimento = Regras.Birth(p.Nascimento), telefone = p.Telefone });
            await w.Exec("UPDATE membros_contrato SET status='ATIVO',parentesco=@relationship,aprovado_por_usuario_id=@actor,aprovado_em=UTC_TIMESTAMP(6),motivo_status=@reason WHERE id=@id", new { relationship = p.Parentesco, actor = a.Id, reason, id });
        }
        else
            await w.Exec("UPDATE membros_contrato SET status='REJEITADO',motivo_status=@reason WHERE id=@id", new { reason, id });
        await w.Exec("UPDATE solicitacoes_vinculo SET status=@status,analisado_por_usuario_id=@actor,analisado_em=UTC_TIMESTAMP(6),justificativa=@reason WHERE id=@id", new { status = yes ? "APROVADA" : "REJEITADA", actor = a.Id, reason, id = sv.Id });
        await RevokePerson(w, m.I("pessoa_id"));
        await Execucao.Audit(w, a, yes ? "APROVAR_VINCULO" : "RECUSAR_VINCULO", "membros_contrato", id, c.I("clube_origem_id"));
        return null;
    }
}
