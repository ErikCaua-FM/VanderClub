# VanderClub API 1.0

API ASP.NET Core .NET 10 com Controllers, Infrastructure, Models e Requests, seguindo a organização da API VanderShop enviada. Usa Dapper e MySqlConnector para integrar o contrato dos clientes VanderClub Mobile/Web 2.0 ao MySQL 8.4.

A API foi implementada e validada contra o esquema existente. Os arquivos do Mobile, da Web e do banco existente **não foram alterados** nesta entrega. Antes de conectar as interfaces, leia [os ajustes encontrados](Docs/AJUSTES_NOS_CLIENTES.md).

## 1. Conferir os pré-requisitos no Windows

- SDK **.NET 10** instalado: no PowerShell, execute `dotnet --list-sdks`. Deve aparecer uma versão `10.0.xxx`.
- MySQL Server **8.4**, com o banco `vanderclub` criado pelo script da etapa anterior.
- Usuário MySQL `vanderclub_api`, sua senha e a porta correta dessa instalação. Workbench é o cliente de administração, não o servidor.
- Para abrir o projeto no Visual Studio, use uma instalação com suporte ao .NET 10 e ao desenvolvimento ASP.NET. Também é possível executar tudo pelo terminal.

A porta do MySQL pode ser **3307**, se você escolheu essa porta para manter o outro servidor na 3306. A API usará **7147** para HTTPS; essas portas têm funções diferentes.

O arquivo em `Banco/VanderClub_MySQL_Completo.sql` é uma cópia **sem alterações** do script já entregue. Se o banco já existe e está pronto, não precisa recriá-lo. A API não executa DDL nem altera o esquema ao iniciar.

## 2. Extrair e configurar a conexão

Extraia o ZIP em uma pasta, por exemplo `D:\Projetos\VanderClub.Api`. Abra o PowerShell nessa pasta, onde está `VanderClub.Api.csproj`.

```powershell
dotnet restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\Configurar-Local.ps1
```

O assistente pede a porta, o usuário e a senha **do MySQL**. Informe a senha da conta técnica `vanderclub_api` criada no script do banco. Ela é diferente da senha que você escolherá para o Presidente.

O assistente grava a conexão e uma chave aleatória nos **User Secrets** do seu usuário de desenvolvimento; não escreve senhas no projeto. `-ExecutionPolicy Bypass` vale apenas para essa execução do PowerShell. Confira o script antes de executá-lo. Não coloque a conexão MySQL no Mobile ou no JavaScript da Web.

Se perdeu a senha técnica, redefina-a na conexão correta do Workbench, com uma conta administradora do MySQL:

```sql
ALTER USER 'vanderclub_api'@'127.0.0.1' IDENTIFIED BY RANDOM PASSWORD;
```

Guarde a senha retornada e execute novamente o assistente. Esse comando troca somente a senha dessa conta técnica; não é para executar a cada inicialização. Nenhuma senha compartilhada acompanha o pacote.

## 3. Certificado, verificação e primeiro Presidente

```powershell
dotnet dev-certs https --trust
dotnet run -- --verify
dotnet run -- --bootstrap
```

- Confirme a confiança no certificado local quando o Windows solicitar.
- `--verify` deve retornar `api: OK`, `banco: OK`, esquema `vanderclub-1.0.0` e contrato `2.0`.
- `--bootstrap` solicita os dados reais do Presidente e senha com confirmação. Nascimento: `yyyy-MM-dd`; CPF válido; telefone com DDD. Senha: 8 a 128 caracteres.
- A senha é armazenada como hash PBKDF2, nunca em texto puro. O comando não publica uma rota de cadastro administrativo.
- Se já houver Presidente, executar o bootstrap novamente preserva a conta. Não é um comando de redefinição de senha.

## 4. Iniciar a API

```powershell
dotnet run --launch-profile https
```

Mantenha esse terminal aberto. Acesse:

- Saúde: `https://localhost:7147/api/v1/health`
- Contrato OpenAPI: `https://localhost:7147/openapi/v1.json`
- Base dos serviços: `https://localhost:7147/api/v1/`

No Visual Studio, abra `VanderClub.Api.csproj`, selecione o perfil **https** e execute. Não selecione “Windows Machine”: esse perfil pertence ao aplicativo MAUI, não à API.

A raiz `/` pode retornar 404 enquanto você não configurar a hospedagem opcional da Web. Isso não significa falha da API. O endpoint de saúde acima é o ponto de verificação.

## 5. Conectar as interfaces na próxima alteração

A configuração local pretendida, no mesmo PC, é:

- Mobile Windows: `ApiConfig.BaseUrl = "https://localhost:7147/api/v1/"`.
- Web: `apiBaseUrl: "https://localhost:7147/api/v1/"`.

Essas alterações **não foram aplicadas aos aplicativos** neste pacote, conforme solicitado. Os outros ajustes necessários estão em `Docs/AJUSTES_NOS_CLIENTES.md`.

Para hospedar a Web existente pela própria API, na mesma origem HTTPS:

```powershell
dotnet user-secrets set "Web:Diretorio" "D:\Projetos\VanderClub.Web\dist"
```

Troque o caminho pelo local real de `dist`, reinicie a API e abra `https://localhost:7147/`. Esse recurso apenas serve os arquivos existentes; não os reescreve. A configuração `apiBaseUrl` da Web ainda precisa estar preenchida.

Se a Web estiver em outra origem, inclua a URL exata em `Cors:Origens`, sem caminho final nem `*`. Exemplo de uma segunda origem de desenvolvimento:

```powershell
dotnet user-secrets set "Cors:Origens:1" "https://localhost:5173"
```

Prefira Web e API na mesma origem. Um site hospedado em domínio diferente precisa também da configuração de cookie `SameSite=None; Secure`, e pode sofrer bloqueio de cookies de terceiros pelo navegador. Não basta liberar CORS. No caso de um domínio real, configure também `AllowedHosts` e o certificado correspondente.

**Android/iOS:** `localhost` no telefone/emulador não é o seu PC. É necessário um endereço HTTPS acessível pelo aparelho e um certificado confiável correspondente a esse endereço. O certificado local `localhost` não valida automaticamente `10.0.2.2` nem o IP da rede. Não desative a validação TLS para contornar isso. A escolha desse endereço será feita ao configurar o Mobile.

## 6. Primeiro uso com dados reais

1. Entre como Presidente na Web conectada.
2. Cadastre as unidades e os planos; os catálogos começam vazios.
3. Cadastre os administradores locais e vincule cada um à sua unidade.
4. Cadastre as áreas reais. A Web atual ainda não tem essa tela; o pacote oferece o assistente abaixo enquanto ela não for adicionada.
5. No Mobile conectado, selecione um plano, cadastre o titular e faça login.
6. As inclusões de convidados/dependentes ficam pendentes para o administrador da unidade de origem.
7. Respostas, eventos, votações e recebimentos passam a usar os mesmos registros do MySQL. Os clientes os consultam por atualização de tela/polling; não há SignalR ou push nesta versão.

Para cadastrar uma área:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\Cadastrar-Area.ps1
```

O assistente usa o login do Presidente, lista unidades/tipos e solicita descrição, regras, capacidade e horários. Mostra os dados antes de gravar. Não cria áreas de demonstração. As áreas terão o mesmo horário nos sete dias, compatível com o DTO atual do Mobile; a API verifica esse horário em cada reserva.

## Comportamentos implementados

- Sessão Mobile com Bearer opaco; sessão Web com cookie HttpOnly/Secure e proteção CSRF.
- Permissões conferidas no servidor: Presidente, ADM Local da unidade e associado com plano local/rede.
- Cadastro transacional com contrato, versão do plano, parcelas e aceite; convidados sujeitos às cotas e aprovação.
- Reservas e eventos na mesma agenda, bloqueio transacional contra sobreposição, intervalo de limpeza, antecedência e cancelamento. Quadras/jogos/academia: até 180 minutos; salões: duração sem teto, respeitando funcionamento e vigência.
- Solicitações/reclamações com protocolo e respostas; chat com autor/classe definidos pela sessão, avisos fixados na Web.
- Votações de escolha única, eleitorado de titulares da unidade de origem, voto único e abertura/encerramento por horário. Convidados e dependentes não votam.
- Parcelas em centavos inteiros, pagamento manual confirmado pelo Presidente, suspensão por atraso e reativação após regularização. Não há integração bancária, geração de boleto/PIX ou cobrança real.
- Alteração de plano publica nova versão. Arquivamento retira a oferta, preservando os contratos já firmados.
- Idempotência nas gravações, controle de versão, auditoria e encerramento/revogação de sessões.

Veja detalhes operacionais em [IMPLEMENTACAO.md](Docs/IMPLEMENTACAO.md) e a evidência dos testes em [VALIDACAO.md](Docs/VALIDACAO.md).

## Se houver erro ao iniciar

| Situação | Conferência |
|---|---|
| `dotnet` não encontrado / NETSDK1045 | Instale/use o SDK .NET 10; confira `dotnet --list-sdks`. |
| `--verify` falha / banco indisponível | Serviço MySQL iniciado, porta correta, conta `vanderclub_api`, senha e esquema `vanderclub` completos. |
| `Access denied` no MySQL | Use `127.0.0.1`, a porta dessa instalação e a senha da conta técnica, não a senha do Presidente. |
| Chave de idempotência não configurada | Execute `Configurar-Local.ps1`; use o perfil https/Development para carregar User Secrets. |
| Aviso de certificado no PC | Execute `dotnet dev-certs https --trust` no mesmo usuário do Windows. |
| Navegador retorna 403 | Confira origem da Web em CORS e sessão/CSRF; não abra `index.html` por `file://`. |
| Login 401 após tentativas incorretas | Após cinco falhas, aguarde dez minutos. Confira o tipo de acesso: administrativo ou associado. |
| 409 ao salvar | Atualize o registro; a versão mudou ou já existe conflito. Não repita cegamente uma alteração com dados diferentes. |
| Área não aparece | Cadastre-a, marque-a ativa e confira a unidade selecionada e a abrangência do plano. |

User Secrets é próprio para desenvolvimento, não um cofre de produção. Para publicar, configure segredos no ambiente do servidor, HTTPS, origens e hosts exatos, conta MySQL restrita, certificados do banco verificados (`VerifyFull`) e rotinas de backup. O assistente local usa `SslMode=Required`, adequado ao certificado automático da instalação local; isso cifra a conexão, mas não verifica a identidade de um servidor MySQL remoto.
