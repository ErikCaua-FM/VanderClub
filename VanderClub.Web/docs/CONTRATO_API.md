# VanderClub — contrato de integração 2.0

Este documento e CONTRATO_API.openapi.json definem a próxima implementação da API. Não representam um backend já disponível. MySQL será acessado somente pelo servidor.

## Transporte e sessão

Base HTTPS /api/v1/, JSON UTF-8, propriedades camelCase. Listas Mobile retornam arrays; escritas sem representação retornam 204. As escritas Mobile cuja resposta a interface utiliza são login (LoginResult), chamado (Chamado) e reserva (Reserva). Registro de associação retorna 204 depois de concluir a transação.

Mobile: POST auth/mobile/login recebe {email, senha}, retorna {perfil, token}. O aplicativo envia Authorization: Bearer e salva o token somente no SecureStorage. GET mobile/context valida sessão e associação, inclusive na restauração. POST auth/mobile/logout revoga a sessão. Expiração exige novo login; não há renovação automática nesta versão.

Web: POST auth/admin/login recebe {email, password}, retorna {actor, csrfToken} e define cookie vanderclub_admin com HttpOnly/Secure, expiração e SameSite adequados à implantação. GET auth/admin/session restaura sessão e CSRF; POST auth/admin/logout revoga e expira o cookie. Escritas autenticadas enviam X-CSRF-Token. O servidor valida Origin, sessão e CSRF e verifica também a origem do login. Não há senha/token administrativo armazenado pelo JavaScript.

Para origens diferentes, CORS deve permitir somente origens Web configuradas, com credenciais e headers Content-Type, X-CSRF-Token e Idempotency-Key. Nunca usar wildcard com credenciais. Preferir frontend e API no mesmo site para evitar bloqueio de cookies de terceiros. Na Web hospedada, configurar um HTTPS acessível pelo navegador.

Não existe cadastro público de administrador. O primeiro Presidente será provisionado no servidor por processo controlado. Hash de senha, recuperação de acesso, limitação de tentativas e revogação são responsabilidades da API. Nunca retornar hashes, salts ou tokens administrativos no bootstrap.

## IDs, datas e valores

- Mobile: IDs inteiros positivos de 32 bits. Zero representa somente cadastro ainda não criado na interface.
- Web: strings decimais canônicas dos mesmos IDs, por exemplo "123". Todas as referências mantêm esse formato.
- Os DTOs Mobile usam português; as projeções administrativas usam inglês para preservar as interfaces. Ambos representam as mesmas entidades e transações. Exemplos: Evento.nome ↔ event.title, inicioUtc ↔ start, fimUtc ↔ end; DadosPessoais.nome ↔ person.name, endereco ↔ street, numero ↔ number, bairro ↔ district, nascimento ↔ birth; Mensalidade.valorCentavos ↔ invoice.amount, pagoUtc ↔ paidAt.
- Instantes: ISO 8601 UTC com Z. Nascimento, vencimento, visita e vigências: yyyy-MM-dd, sem fuso. O Mobile possui um conversor de datas civis.
- Associacao.fimLocal e contract.end são limites exclusivos. A interface mostra o último dia válido subtraindo um dia.
- A API verifica o fuso IANA da unidade e utiliza seu relógio para validar intervalos e estados.
- Dinheiro: inteiros em centavos BRL. Não usar FLOAT no MySQL. Distribuir arredondamentos das parcelas preservando exatamente o preço total.

## Consultas e contexto

public/plans e public/units mostram somente ofertas/unidades abertas para adesão. mobile/units lista unidades autorizadas. mobile/context?unitId= verifica a abrangência do contrato; trocar unidade não transfere contrato nem direito de voto.

A API restringe beneficiários, visitantes, chamados, reservas e finanças ao usuário/titular autorizado, além do escopo da unidade. A agenda da área mostra somente intervalos ocupados, sem identificar outros associados.

GET admin/bootstrap retorna actor e os arrays definidos em OpenAPI. O servidor filtra pessoas, contratos, parcelas, eventos, votações, atendimentos, chat e reservas antes de responder. Para ADM Local, o diretório de outros administradores apresenta somente os contatos autorizados. Auditoria segue o mesmo controle.

O bootstrap desta versão é uma fotografia completa do escopo. Não truncar arrays silenciosamente nem trocar por envelopes paginados sem adaptar o cliente. Para redes maiores, evoluir para endpoints paginados e adaptar as telas na mesma versão de contrato. A paginação atual da Web é de exibição.

GET messages?beforeId=0&limit=50 no Mobile retorna as últimas mensagens em ordem crescente. beforeId positivo retorna a página imediatamente anterior, também crescente. Autor, nome, classe e instante são definidos no servidor. Mobile consulta o chat a cada 15 segundos; Web atualiza a cada 30 segundos quando visível e sem formulário em edição. O botão Atualizar permite consulta imediata. Não há implementação WebSocket nesta entrega.

## Permissões no servidor

| Operação | Presidente | ADM Local | Associado Mobile |
|---|---|---|---|
| Criar/editar unidade ou administrador | Toda a rede | Não | Não |
| Criar/versionar/arquivar oferta | Sim | Consulta | Consulta/adesão |
| Eventos e votações | Toda a rede | Unidade vinculada | Consulta; voto elegível |
| Aprovar convidados/responder chamados | Toda a rede | Unidade vinculada | Enviar/consultar próprios |
| Suspender/reativar associação | Toda a rede | Unidade vinculada | Não |
| Registrar recebimento conferido | Sim | Não | Consultar próprias parcelas |
| Dados pessoais | Fluxos autorizados | Escopo autorizado | Próprios dados |
| Chat | Grupos autorizados | Unidade vinculada | Unidades autorizadas |

A API ignora ou rejeita campos não permitidos. Usuário, papel, titular, protocolo e timestamps são determinados pelo servidor. Esconder botões não substitui autorização. Nunca usar cache para contornar 401/403.

## Eventos e Minha Conta

Eventos têm estados Agendado, Confirmado, Cancelado e Realizado. Criação/edição administrativa aceita Agendado ou Confirmado; cancelamento usa rota própria, motivo e versão. O servidor projeta Realizado após o término, preservando Cancelado. Inscrições encerram até o início do evento e após sua abertura.

Eventos vinculados a áreas bloqueiam o espaço com a mesma verificação transacional das reservas. Cancelar libera o intervalo sem apagar histórico. Mobile possui somente GET de eventos. Imagens: Web usa social.svg, festa.svg, futebol.svg ou quiosque.svg; Mobile usa os correspondentes nomes .png gerados dos recursos SVG pelo MAUI.

GET mobile/me retorna MinhaConta: dados, associação, tipoSocio, status e unidadeOrigem. A associação é a fotografia do plano contratado, incluindo versão, benefícios, cotas e condições. Para convidados/dependentes, não retornar informações financeiras privativas do titular; a interface também não as exibe.

PUT mobile/me recebe somente DadosPessoais e versao. Divergência de versão retorna 409. Validar nome, e-mail único, CPF, nascimento, CEP, telefone e endereço. O endpoint não altera papel, aprovação, plano ou parcelas. Alterar e-mail atualiza o identificador de login transacionalmente. Se for necessária verificação adicional de identidade, retornar 422 com orientação; não confirmar alteração ainda incompleta.

Adesão envia planoId, planoVersaoId, condicaoPagamentoId, unidadeId, dados e aceite de termos. Conferir a oferta publicada, a versão/condição e o aceite. Se a oferta mudou, retornar 409 para nova seleção. Criar usuário, associação, fotografia e parcelas na mesma transação, sem marcar parcelas como pagas.

## Regras de integridade

1. Editar plano cria nova versão. Arquivar plano encerra somente novas adesões. Fotografias, valores e benefícios de contratos anteriores permanecem até o fim da vigência.
2. Cota de beneficiários soma pendentes e aprovados e respeita limites por categoria. Alterações relevantes retornam à aprovação. Recusas exigem motivo. Exclusão preserva auditoria e revoga o acesso vinculado. Convidados/dependentes não votam nem administram outros beneficiários.
3. Reservas exigem início futuro, fim maior, funcionamento, capacidade, vigência e limite de reservas futuras. Esportes/jogos/academia: até 180 minutos; salões de festa/social: sem teto, mantendo conflitos. Intervalos [início,fim) permitem encostar no fim da reserva anterior. Impedir sobreposição com reservas/eventos transacionalmente, inclusive entre usuários concorrentes; consulta prévia do cliente não garante exclusividade.
4. Chamados recebem protocolo e timestamps no servidor. Solicitações têm status. Reclamações mantêm histórico e respostas, sem exigir status equivalente no Mobile. Resposta acrescenta histórico, não o substitui. Na projeção Mobile, respostaAdmin deve conter o histórico legível de respostas, nomes/classes/datas, e respondidoUtc a última resposta. Uma evolução pode adotar lista nas duas pontas.
5. Voto somente do titular/fundador, uma vez por votação na unidade de origem. Conferir período, condição financeira quando exigida e fotografia do eleitorado na abertura. Índice único votação+titular. Opção deve pertencer à votação. PodeVotar e Impedimento vêm do servidor. Divulgar contagens conforme a regra da votação, sem expor associação eleitor/opção no bootstrap.
6. Parcela paga exige recebimento confirmado. Se vencida e não paga, está atrasada. Suspensão/reativação verificam débitos e vigência no servidor. O cliente não efetua baixa visual.
7. POST admin/invoices/{id}/payments registra recebimento já conferido pelo Presidente; não executa cobrança bancária. Validar versão, parcela aberta, data não futura, valor integral conhecido pelo servidor e duplicidade. Registrar referência, meio, instante e autor da sessão. Manter histórico auditável. Estorno e recebimento parcial não fazem parte desta entrega.
8. Chat aceita até 2.000 caracteres, sem HTML executável. Nome/classe são fornecidos pelo servidor. Fixar avisos exige autorização administrativa.

## Erros e concorrência

Erros: {code, message, requestId, errors?}. Mensagem adequada ao usuário, sem SQL, stacktrace ou segredo. 400 formato; 401 autenticação; 403 permissão; 404 recurso indisponível no escopo; 409 versão/conflito/duplicidade; 422 validação; 429 limite. Não retornar HTML em endpoints JSON.

Todas as escritas de negócio enviam Idempotency-Key. Persistir resultado por usuário+rota+método+chave e hash de payload. Mesma chave/payload repete a resposta original; conteúdo diferente retorna conflito. Os clientes não repetem automaticamente. Uma nova tentativa idêntica após falha de transporte mantém a chave em memória; não é fila offline durável.

Web: version obrigatória em edição/ação sobre registros existentes e omitida em criação. Mobile: versao obrigatória na alteração de Minha Conta e edição/exclusão de beneficiário. Cancelamentos de visitante/reserva são idempotentes, limitados ao dono e validados contra o estado atual.

SQLite guarda apenas catálogo/áreas/eventos por 30 segundos e por contexto; é limpo no início/logout/expiração. Perfil, mensagens, votos e finanças sempre exigem a API. A Web mantém apenas snapshot em memória. Na atualização, os registros da antiga demonstração não são importados como produção.

## Próxima implementação

1. Modelar MySQL com PK/FK e índices para usuários/perfis, unidades, planos/versões/condições, contratos/fotografias, beneficiários, visitantes, áreas/agenda, eventos, chamados/respostas, mensagens, votações/opções/votos, parcelas/recebimentos, sessões, idempotência e auditoria.
2. Implementar autenticação, catálogos, contexto, Minha Conta e bootstrap.
3. Implementar transações e autorizações de cada rota OpenAPI.
4. Configurar Helpers/ApiConfig.cs e dist/config.js para o mesmo serviço.
5. Validar evento Web→Mobile, perfil Mobile→Web, aprovação, reservas concorrentes, respostas, voto duplicado, recebimentos, plano antigo preservado, isolamento local e expiração/logout.

Usar TLS, validação de entrada, CORS restrito, auditoria, limites de requisição e segredos somente no servidor. Não colocar conexão MySQL nos clientes.

Referências técnicas: [SQLite no MAUI](https://learn.microsoft.com/dotnet/maui/data-cloud/database-sqlite), [SecureStorage](https://learn.microsoft.com/dotnet/maui/platform-integration/storage/secure-storage) e [serialização JSON gerada](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation).
