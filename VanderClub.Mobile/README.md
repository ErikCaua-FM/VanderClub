# VanderClub Mobile — 2.0

Cliente .NET MAUI preparado para a próxima etapa de integração. Esta entrega não inclui a API nem o MySQL. O login e as operações reais dependerão desse serviço.

## Estrutura

O projeto segue a organização do ZIP VanderShop.Mobile fornecido: um único projeto na raiz, com Helpers, Models, Views, Platforms, Properties e Resources. As telas usam pares `Views/frm*.xaml` e `.xaml.cs`. App.xaml, AppShell.xaml, MainPage.xaml, MauiProgram.cs, VanderClub.csproj e VanderClub.slnx ficam na raiz.

Helpers reúne transporte HTTP, configuração, sessão, SQLite, serialização e componentes visuais. Models contém um DTO por arquivo. As telas preservadas usam contêiner XAML e cartões dinâmicos no code-behind; Minha Conta declara os campos no XAML. Resources mantém a identidade visual do VanderClub.

Não há projetos Core/Tests separados, pasta src, laboratório, banco preenchido, credenciais prontas, bin, obj ou arquivos pessoais da IDE no pacote.

## Mudanças

- **Eventos:** lista da unidade autorizada, busca, filtro Agendado/Confirmado/Cancelado/Realizado e detalhes. Somente consulta; o Mobile não cria nem edita eventos.
- **Minha Conta:** edição de nome, e-mail, CPF, nascimento, telefone, endereço completo e informações opcionais. Exibe tipo e situação da associação, unidade de origem, abrangência, vigência, cotas, benefícios e condições do contrato. Valores financeiros são apresentados ao titular. A gravação usa controle de versão.
- **Planos:** duração, preços, condições e versão vêm da API. Não há mais doze parcelas ou desconto de 10% fixos no cliente. A adesão envia a versão e a condição exatas escolhidas.
- **Dados remotos:** ApiService substitui o serviço local de demonstração. Os demais módulos preservados também passam a usar HTTP.
- **SQLite:** cache temporário de catálogos, áreas e eventos por usuário/unidade, com validade de 30 segundos. Não armazena senhas, tokens, perfis pessoais, mensagens, votos ou finanças. Limpeza ao iniciar, sair, expirar sessão e após alterações. Não há fila de gravação offline.
- **Sessão:** token no SecureStorage e restauração validada no servidor. Os dois antigos bancos exclusivamente demonstrativos são descartados; seus registros não são importados como produção.
- **Mensalidades:** consulta de parcelas e detalhes, sem cobrança bancária, QR fictício ou botão para marcar pagamento localmente. O Presidente poderá registrar recebimentos conferidos na Web através da API.
- **Confiabilidade:** timeout, tratamento de erros, idempotência, serialização JSON gerada em compilação, datas civis sem conversão de fuso e horários no fuso da unidade.

## Abrir e compilar

1. Extraia a pasta inteira. Abra VanderClub.slnx ou VanderClub.csproj em um ambiente com .NET SDK 10 e o workload .NET MAUI.
2. Configure `Helpers/ApiConfig.cs`, propriedade BaseUrl, com a URL HTTPS terminada em /api/v1/. Não coloque conexão MySQL ou segredo de servidor no aplicativo.
3. Restaure os pacotes e selecione Android. O alvo Windows é incluído no Windows; iOS e Mac Catalyst no macOS, exigindo os SDKs correspondentes.

```text
dotnet workload restore VanderClub.csproj
dotnet restore VanderClub.csproj
dotnet build VanderClub.csproj -f net10.0-android -c Debug
```

A URL vem vazia intencionalmente. Sem API, o aplicativo abre o login e informa a indisponibilidade ao tentar uma operação; não inventa contas nem libera autenticação local. Para emulador, use HTTPS acessível e certificado confiável. Não foi desativada a validação de certificados.

Leia CONTRATO_API.md e CONTRATO_API.openapi.json antes de implementar o backend. O SQLite é cache e não substitui o modelo relacional MySQL.

## Validação e limites desta entrega

A compilação Android Debug e os componentes de transporte/cache foram verificados. A interface não foi executada em aparelho físico ou emulador Android. A geração Release encontrou uma limitação do task host MSBuild/ILLink no ambiente de compilação e precisa ser repetida no ambiente de publicação. Windows, iOS e Mac Catalyst não foram compilados aqui. Assinatura de distribuição, lojas e integração ponta a ponta dependem das próximas etapas.
