using System.Security.Cryptography;
using VanderClub.Api.Infrastructure;

namespace VanderClub.Api.Services;
public sealed class Inicializacao(Banco banco, Autenticacao auth, IConfiguration config)
{
    public async Task<object> Health(CancellationToken ct = default)
    {
        await using var w = await banco.Abrir(ct);
        var state = await w.One("SELECT estado FROM schema_migrations WHERE codigo='vanderclub-1.0.0'");
        Regras.Exigir(state?.S("estado") == "PRONTO", "Execute o script VanderClub_MySQL_Completo.sql.", 503);
        var count = await w.Scalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_type='BASE TABLE'");
        Regras.Exigir(count >= 45, "O banco não tem todas as tabelas necessárias.", 503);
        var update = await w.One("SELECT estado FROM schema_migrations WHERE codigo='vanderclub-1.0.1'");
        Regras.Exigir(update?.S("estado") == "PRONTO", "Execute Banco/Atualizacao_2026_11_04.sql antes de iniciar esta versão.", 503);
        return new
        {
            api = "OK",
            banco = "OK",
            schema = "vanderclub-1.0.1",
            contrato = "2.0"
        };
    }

    string Read(string key, string prompt, bool password = false)
    {
        var configured = config["Bootstrap:" + key];
        if (!string.IsNullOrEmpty(configured))
            return configured;
        Console.Write(prompt + ": ");
        if (!password || Console.IsInputRedirected)
            return Console.ReadLine() ?? "";
        var chars = new List<char>();
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string (chars.ToArray());
            }

            if (k.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0)
                    chars.RemoveAt(chars.Count - 1);
            }
            else if (!char.IsControl(k.KeyChar))
                chars.Add(k.KeyChar);
        }
    }

    public async Task Bootstrap()
    {
        await Health();
        await using var w = await banco.Abrir();
        var locked = await w.Scalar<int>("SELECT GET_LOCK('vanderclub_bootstrap_presidente',10)");
        Regras.Exigir(locked == 1, "Outro bootstrap está em execução.", 409);
        try
        {
            await w.Begin();
            if (await w.Scalar<int>("SELECT COUNT(*) FROM usuarios WHERE perfil='ADM_GERAL'") > 0)
            {
                Console.WriteLine("O Presidente já existe. Nenhuma conta foi alterada.");
                await w.Rollback();
                return;
            }

            Console.WriteLine("Cadastro inicial do Presidente. A senha não será mostrada nem salva em texto puro.");
            var name = Regras.Texto(Read("Nome", "Nome completo"), "o nome", 160, 3);
            var email = Regras.Email(Read("Email", "E-mail"));
            var cpf = Regras.Cpf(Read("Cpf", "CPF"));
            var birth = Regras.Birth(Regras.Day(Read("Nascimento", "Nascimento (yyyy-MM-dd)")), true);
            var phone = Regras.Phone(Read("Telefone", "Telefone com DDD"));
            var password = Read("Senha", "Senha (mínimo 8 caracteres)", true);
            var confirmation = Read("Confirmacao", "Confirme a senha", true);
            Regras.Exigir(password == confirmation, "As senhas não coincidem.");
            var pid = await w.Insert("pessoas", new { nome = name, cpf, nascimento = birth, telefone = phone, email_contato = email });
            var uid = await w.Insert("usuarios", new { pessoa_id = pid, email_login = email, email_normalizado = email, senha_hash = auth.HashPassword(password), perfil = "ADM_GERAL", status = "ATIVO", security_stamp = Guid.NewGuid().ToString() });
            await Execucao.Audit(w, new(uid, pid, name, "ADM_GERAL", 0, 0), "BOOTSTRAP_PRESIDENTE", "usuarios", uid);
            await w.Commit();
            Console.WriteLine("Presidente criado com sucesso. Inicie a API e entre na Web com o e-mail e a senha escolhidos.");
        }
        finally
        {
            await w.Exec("DO RELEASE_LOCK('vanderclub_bootstrap_presidente')");
        }
    }
}
