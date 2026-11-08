# Implementação e operação

## Organização

- `Program.cs`: inicialização, HTTPS, CORS, autenticação, CSRF, limites de requisição, erros, saúde e hospedagem opcional da Web.
- `Controllers/`: rotas públicas, autenticação, Mobile, administração e cadastro de áreas.
- `Requests/`: contratos de entrada. IDs do Mobile são inteiros; IDs da projeção Web são strings, conforme os clientes existentes.
- `Models/`: ator autenticado e leitura interna de registros. Registros brutos de usuários não são serializados.
- `Infrastructure/`: conexões, transações, idempotência, validação, autenticação e erros.
- `Services/`: regras de associação, agenda, financeiro, comunicação, votações, bootstrap administrativo e rotinas por horário.
- `Scripts/`: configuração local e cadastro de áreas reais.
- `Banco/`: referência exata do esquema anterior, sem migração nesta entrega.

O projeto usa .NET 10, Dapper 2.1.66 e MySqlConnector 2.6.1. A organização segue a referência VanderShop; autenticação, transações e projeções foram implementadas especificamente para o VanderClub. O conector foi atualizado em relação à referência. As dependências são restauradas pelo NuGet.

## Endereços e autenticação

Todas as rotas funcionais começam em `/api/v1/`. O JSON é camelCase.

| Acesso | Rotas principais | Autenticação |
|---|---|---|
| Público | `GET public/units`, `GET public/plans`, `POST mobile/registrations` | Sem sessão; cadastro exige `Idempotency-Key`. |
| Associado | `auth/mobile/login`, `mobile/context`, `mobile/me` e demais rotas `mobile/*` | Bearer opaco devolvido pelo login; o Mobile o guarda no SecureStorage. |
| Administrativo | `auth/admin/login`, `auth/admin/session`, `admin/bootstrap` e gravações `admin/*` | Cookie `vanderclub_admin` HttpOnly/Secure. |
| Configuração das áreas | `GET admin/areas/types`, `GET/POST admin/areas`, `PUT admin/areas/{id}` | Presidente, sessão Web, CSRF em gravações. |
| Verificação | `GET health` | Sem sessão; informa apenas API/banco/esquema. |

Os Controllers aplicam perfis e os Services conferem unidade, propriedade e estado do registro. O usuário/classe enviados pelo cliente nunca definem a autoria do chat, respostas ou auditoria.

Para escritas administrativas, envie `Origin` autorizado e, depois do login, `X-CSRF-Token` recebido na sessão. O cookie é a credencial, não o CSRF. Origens exatas são exigidas inclusive no login. O token administrativo não é exposto no JSON.

Sessões duram oito horas por padrão, configuráveis de 1 a 24. Logout revoga no servidor. Alteração de administrador revoga outras sessões; remoção/edição de beneficiário revoga suas sessões. Cinco tentativas inválidas bloqueiam a conta por dez minutos. Há limitação adicional por IP/usuário. Não há refresh token nesta versão.

## Requisições repetidas e transações

Toda gravação de negócio exige `Idempotency-Key`, de 8 a 100 caracteres. Os clientes existentes já enviam a chave. Ao perder uma resposta, repita **a mesma chave e o mesmo corpo**. Alterar o corpo com a mesma chave retorna 409. A resposta concluída é preservada por sete dias; hashes do payload são HMAC, inclusive no cadastro que recebe senha.

A inserção da chave, as alterações de negócio, auditoria e resposta ficam na mesma transação. Se houver erro, há rollback. Campos `version`/`versao` protegem edições contra sobrescritas concorrentes; a versão é incrementada pelos gatilhos do banco. O cliente deve atualizar a tela após conflito.

Reservas e eventos bloqueiam a linha da área com `FOR UPDATE` antes de verificar a agenda. A verificação é feita no servidor e na mesma transação da gravação. Intervalos são `[início, fim)`: podem ser adjacentes quando o intervalo de limpeza é zero. Beneficiários e cota de reservas bloqueiam o contrato; pagamento bloqueia a parcela; voto bloqueia a votação e depende da unicidade do eleitor.

## Associação e planos

- A adesão cria pessoa, usuário, contrato, titular, parcelas e aceite do documento publicado. O titular precisa ter pelo menos 18 anos.
- O contrato começa vigente, sem exigir confirmação de um pagamento inicial. A API não informa que uma parcela foi paga no cadastro. A primeira vence cerca de sete dias depois, ajustada para dia até 28; as demais são mensais. Essa é a regra inicial adotada enquanto o contrato dos clientes não possui um seletor de vencimento.
- Valores monetários usam centavos inteiros. A soma das parcelas é exatamente o valor total, distribuindo o resto de centavos entre as primeiras parcelas.
- Condições e benefícios são preservados pela referência à versão imutável. Não se reescreve contrato antigo ao publicar nova oferta.
- A cota total de beneficiários é compartilhada entre dependentes e convidados, respeitando também o limite de cada categoria. Pendentes, aprovados e suspensos ocupam vaga; recusados/removidos não.
- Editar dados de beneficiário exige nova aprovação. O tipo do vínculo é imutável no esquema atual.
- CPF é único. Reaproveitar um CPF que já pertence a outra pessoa/vínculo exige um fluxo administrativo específico; não é feito por criação duplicada.
- O escopo de voto é a unidade de origem; a abrangência REDE permite usar outras unidades, mas não multiplica o direito ao voto.

## Agenda, eventos e atendimento

O cadastro adicional de área aceita horários iguais nos sete dias, para representar o modelo atual do Mobile. Para liberar uso que atravessa a meia-noite, o funcionamento deve cobrir o intervalo inteiro; por exemplo 0 a 1440 em todos os dias. “Sem teto” para salões não elimina conflito, vigência, antecedência e horário de funcionamento.

Os campos `maxMinutes=0` e `maxMinutos=0` representam ausência de teto de duração dos tipos permitidos. Tipos esportivos/jogos/academia limitam em até 180 minutos. O servidor também limita o intervalo após uso, antecedência para reservar e prazo de cancelamento.

O contrato de criação de reserva atual não envia quantidade de convidados/participantes: a API grava uma pessoa responsável. Caso seja necessário controle de lotação por participantes, será preciso ampliar a tela e o DTO; não há contagem fictícia de participantes.

Eventos podem ocupar uma área da unidade. Cancelá-los libera sua ocupação sem remover o histórico. Mobile possui somente leitura. Imagens são nomes de recursos já incluídos nos clientes, como `tenis.svg`/`tenis.png`, não uploads externos.

Solicitações/reclamações são privadas ao autor e à administração da unidade de atendimento. O chamado de um visitante da rede inclui `requester` com identificação mínima para a Web. A resposta fica no mesmo histórico consultado pelo Mobile. Protocolos são únicos, gerados no servidor.

O chat tem histórico por unidade, autoria por sessão e fixação de avisos na Web. Usa consultas periódicas, conforme os clientes. Não há entrega por WebSocket, notificação push ou confirmação de leitura.

## Financeiro e votações

O Presidente registra recebimento integral de parcela, meio informado, referência, observação e data real de recebimento. Não pode registrar data futura. A transação aplica a quitação uma única vez. Esse registro não cobra o associado e não produz boleto, cartão ou PIX bancário.

Atraso é calculado pela data civil da unidade de origem, depois do vencimento. O ADM da unidade ou Presidente pode suspender contrato vigente com dívida vencida; a reativação exige regularização e vigência. Suspensos continuam consultando perfil/mensalidades e abrindo atendimento, mas não reservam nem usam chat. Não há suspensão automática por cron ou reativação automática ao pagar.

Votações abrem e encerram pela hora UTC configurada, com rotina a cada cinco segundos e conferência durante consultas/votos. Na abertura efetiva, a API congela os titulares elegíveis da unidade. Se houver exigência de adimplência, ela é conferida na abertura e no voto. Se o serviço ficar desligado, não existem operações nesse período; ao retornar, uma votação cujo prazo já acabou é encerrada sem votos retroativos. Para assembleias formais, mantenha o serviço disponível no horário de abertura.

Um eleitor vota uma vez em uma alternativa da votação. Convidados/dependentes nunca votam. A administração recebe contagens agregadas: o array `votes` da Web contém apenas `pollId`/`optionId`, sem identidade nem horário. A base possui vínculo entre eleitor e voto para unicidade; portanto isso **não é um protocolo criptográfico de voto secreto** perante quem administra diretamente o banco.

## Tempo, dados e falhas

Instantes são UTC no MySQL e em ISO 8601 na API. Datas civis, como nascimento, vencimento e vigência, usam `yyyy-MM-dd`, sem conversão de fuso. A unidade define seu fuso IANA, como `America/Sao_Paulo`. A vigência termina no início da data final, que é exclusiva.

A API não grava senhas, tokens ou escolhas de voto na auditoria. Tokens aleatórios de sessão são armazenados somente por hash. O hash de senha usa ASP.NET Core PasswordHasher/PBKDF2-SHA512 com 210 mil iterações e salt aleatório. SQL usa parâmetros; o usuário técnico não precisa de DDL.

Erros possuem `code`, `message` e `requestId`. Os retornos usuais são 400 formato inválido, 401 sessão/login, 403 permissão, 404 registro inexistente, 409 conflito/versão, 422 regra de negócio, 429 limite de requisições e 503 indisponibilidade do banco. Detalhes SQL ficam no log do servidor, não na resposta ao cliente.

A rotina periódica remove requisições idempotentes vencidas e sessões antigas expiradas. Não remove histórico financeiro, contratos, membros, mensagens ou auditoria.

## Documentação técnica

`CONTRATO_API.md` preserva o documento de interface recebido dos clientes 2.0, inclusive observações da etapa em que a API ainda não existia. `CONTRATO_API.openapi.json` é a cópia atualizada para esta implementação: mantém as rotas e DTOs consumidos pelos clientes e acrescenta saúde, áreas e `requester`. Ele pode ser importado em um cliente OpenAPI. Não há dependência de Swagger UI em produção.

Exemplos mínimos de requisição estão em `../VanderClub.Api.http`. Preencha credenciais somente no seu ambiente privado; não publique um arquivo com senhas/cookies reais.
