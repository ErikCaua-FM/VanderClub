# Decisões da Web e próxima integração

Entrega 1.0 · 03/11/2026. Base: solicitação atual, especificações VanderClub e pacote Mobile v1.0. Requisitos posteriores do usuário prevalecem sobre divergências dos documentos anteriores.

## Decisões aplicadas

1. **Mesmo login, dois perfis.** A conta validada define a classe; não existe seletor que permita escolher privilégios nem criação pública de administrador. Os dois atalhos na tela inicial apenas preenchem as contas fictícias.
2. **ADM Local por unidade.** Consulta os diretórios gerais de unidades e administradores; acessa dados pessoais de associados e operações apenas da unidade vinculada. Somente o Presidente cria/edita unidades, administradores e ofertas de planos. Não se concede edição/exclusão de planos ao ADM Local porque o catálogo afeta toda a rede.
3. **Acesso Rede não dá voto em todas as unidades.** O titular vota na unidade de origem do contrato. Dependentes e convidados não votam, independentemente da aprovação. Aprovação libera o beneficiário; o estado do contrato titular continua determinando o acesso.
4. **Reclamações pendentes.** A listagem mostra se há resposta. O objeto de reclamação não recebe status próprio; `respondedAt` e `responses` indicam o atendimento. Solicitações possuem Pendente/Atendida. Ambas podem receber complementos no histórico.
5. **Desligar significa suspender temporariamente.** Não remove cadastro, contrato ou parcelas. Exige atraso e motivo. A tolerância contratada, quando diferente de zero, também precisa ter terminado. Ao quitar visualmente todas as parcelas atrasadas, um contrato vigente suspenso por inadimplência é reativado. Contrato vencido não é renovado por pagamento.
6. **Planos são globais; acesso pode ser local ou de rede.** A versão guarda preços, condições, limites, regras e benefícios. O contrato guarda sua versão e uma cópia dos termos. Edição não recalcula contratos nem parcelas antigas; exclusão é arquivamento da oferta. Não existe renovação automática nesta entrega.
7. **Eventos são programação do clube.** Não são as reservas pessoais do Mobile. Se houver área vinculada, o período é confrontado com outros eventos e reservas fictícias daquela área. Intervalos adjacentes são permitidos. Há uma reserva inicial no Campo de Futebol de Foz Centro, amanhã das 18h às 19h, calculada na primeira inicialização. As datas de inscrição ficam cadastradas; inscrição de participantes não faz parte desta Web.
8. **Votações futuras ficam Agendadas.** São Aberta apenas dentro do período; passam a Encerrada no término ou por ação administrativa. Opções e votos não são editáveis após criação. O quórum é percentual sobre titulares do eleitorado. Apuração parcial é explicitamente identificada. Empates e ausência de votos não inventam vencedor. A demonstração armazena votos identificáveis; não implementa eleição secreta.
9. **Elegibilidade de votação.** Eleitorado é uma fotografia dos titulares ativos da origem quando a abertura é processada. Para abertura futura, é calculado na primeira observação da abertura (login, atualização por minuto ou primeiro voto). A eventual exigência de adimplência é revalidada na confirmação do voto. Sem servidor ativo continuamente, não há garantia de fotografia histórica exata de mudanças ocorridas enquanto todas as abas estavam fechadas. A API precisará executar a abertura e congelar o eleitorado no horário oficial.
10. **O simulador não altera o Mobile.** Gera dados no próprio navegador para demonstrar a reação da Web. Não há conexão, importação automática ou banco compartilhado com o arquivo SQLite do app.
11. **Datas e dinheiro.** Eventos/votações são interpretados no fuso IANA da unidade e gravados em UTC. Nascimento e vencimento são datas civis, sem conversão para UTC. Valores são inteiros em centavos, apresentados em pt-BR. Atraso começa no dia posterior ao vencimento. Vigência do contrato usa intervalo de início inclusivo e fim exclusivo; a UI mostra o último dia incluído.
12. **Dados mínimos e opcionais.** CPF é validado pelos dígitos, telefone por tamanho e e-mail por formato básico; não há consulta oficial de identidade, endereço ou titularidade. Estado civil, gênero e complemento são opcionais. Imagens são ilustrações locais, selecionáveis entre opções.

## Modelo da demonstração e relações

IDs são strings. A futura implementação poderá adotar UUIDs ou chaves numéricas, mantendo a mesma semântica. `localStorage` não é um banco relacional; as regras dos serviços validam relações usadas nas operações. As chaves estrangeiras definitivas devem existir no MySQL.

| Coleção | Relação principal | Regra para o banco futuro |
|---|---|---|
| `units` | Unidade do clube | Sigla única; inativação preserva histórico |
| `admins` | `unitId → units` para ADM Local | Login/CPF únicos; unidade obrigatória para local e nula para geral |
| `plans` | Catálogo global e versão corrente | Código único; exclusão lógica |
| `plans[].versions` | Versão pertence ao plano | Normalizar em `plano_versoes`, com UNIQUE(plano_id, numero); imutável após oferta |
| `versions[].conditions` | Condições pertencem à versão | Normalizar em `plano_condicoes_pagamento`; preço e cronograma próprios |
| `contracts` | `holderId → people`, `unitId → units`, `planId → plans`, `planVersionId → versões` | Titular, origem, período e condição contratada; preservar snapshot/termos |
| `people` | `contractId → contracts`; convidado/dependente ligado ao titular pelo contrato | Na modelagem relacional, separar pessoa, vínculo e contrato; evitar dependência circular obrigatória na inserção |
| `invoices` | `contractId → contracts` | UNIQUE(contrato_id, numero_parcela); valores em DECIMAL ou centavos; pagamentos em tabela própria |
| `areas` | `unitId → units` | Área pertence a uma unidade |
| `events` | `unitId → units`, `areaId → areas` opcional, `creatorId → admins` | Datas válidas; vínculo de área na mesma unidade; histórico de cancelamento |
| `reservations` | Unidade, área, pessoa | Unificar controle de ocupação com eventos; conflito precisa ser transacional |
| `polls` | `unitId → units`, `creatorId → admins` | Opções e eleitorado em tabelas próprias; estado e janela de votação |
| `votes` | `pollId → polls`, `personId → people`, `optionId → opção da votação` | UNIQUE(votacao_id, pessoa_id), FK composta para opção pertencente à votação |
| `tickets` | `unitId → units`, `personId → people` | Protocolo único; respostas em tabela própria com autor e data |
| `messages` | Unidade e remetente | Remetentes de pessoas/admins devem referenciar uma conta comum ou ter FKs explícitas; não FK polimórfica sem integridade |
| `audit` | Administrador e ação | Registro de auditoria protegido no servidor; demo guarda os últimos 200 |

Não usar `ON DELETE CASCADE` para apagar contratos, pagamentos, votos ou históricos quando uma oferta/unidade for inativada. As operações de arquivamento da demonstração já preservam essas referências. O pedido atual não inclui implantação MySQL; este mapeamento prepara essa próxima etapa.

## Limites do ambiente de testes

- Base independente por origem e navegador; não é armazenamento compartilhado da rede.
- Sem backend, autenticação de servidor, autorização resistente a manipulação, recuperação de senha por e-mail, MFA ou logs imutáveis.
- Senhas locais têm derivação PBKDF2 e sal. As credenciais das contas fictícias são públicas. A sessão local não é um token seguro de servidor.
- HTTPS/localhost permite Web Locks. Em HTTP sem suporte, as gravações têm fila por instância, sem garantia de exclusão mútua entre abas. Para ensaio simultâneo, prefira localhost ou o endereço HTTPS publicado. Isso não substitui transações e concorrência da API.
- Sem integração financeira, cadastro de novos titulares nesta Web, controle de portaria, envio de documentos, notificações push ou inscrição efetiva em eventos.
- Sem sincronização offline, importador de dados Mobile, exportador de backups ou serviço que rode após fechar todas as abas.
- Testes de navegador ocorreram no Chrome disponível; não foram realizados em aparelhos físicos, Safari ou Firefox.
- O visual responsivo foi inspecionado num frame de 390 px, equivalente a 375 px úteis com a barra de rolagem. Não é um teste de hardware móvel.

## Preparação para MySQL e API

A camada `Service` concentra operações e validações. Na integração, substituir a persistência local por um cliente da API, mantendo a UI e seus contratos de tela. As regras deverão ser repetidas/centralizadas no servidor; o cliente não decide autorização.

| Operação futura | Responsabilidade do servidor |
|---|---|
| Autenticar e manter sessão | Hash de senha no servidor, cookie HttpOnly ou fluxo de tokens, expiração, revogação e limitação de tentativas |
| Consultar unidades/admins/sócios | Escopo por perfil e origem, paginação, busca e minimização dos dados devolvidos |
| Aprovar beneficiário | Conferir titular, vínculo e cota da versão contratada dentro de transação; registrar parecer |
| Alterar ou arquivar plano | Versão imutável e exclusão lógica; nenhuma atualização retroativa de contratos |
| Receber pagamento demonstrativo | Evento idempotente autorizado; atualizar parcela e elegibilidade no mesmo domínio; nunca aceitar status enviado sem validação |
| Criar evento/reserva | Bloqueio transacional da área, verificação de intervalo e política de cancelamento |
| Abrir/encerrar votação e votar | Relógio do servidor, fotografia de eleitorado, voto único, alternativa correta, quórum e apuração |
| Responder atendimento | Autor autenticado, histórico e publicação de atualização ao sócio |
| Chat | WebSocket/SSE ou consulta incremental, autorização por unidade, moderação, rate limit e notificações |
| Sincronizar SQLite Mobile | IDs estáveis, revisões, idempotência, tombstones e resolução de conflitos; API como fonte de verdade |

Rotas sugeridas: `/auth`, `/unidades`, `/administradores`, `/eventos`, `/votacoes`, `/socios`, `/beneficiarios`, `/contratos`, `/mensalidades`, `/atendimentos`, `/chat`, `/planos`. São sugestões para a próxima etapa; nenhum endpoint é chamado nesta entrega.

## Dependências e fontes técnicas

- Manrope: Google Fonts, licença OFL incluída em `dist/assets/MANROPE_LICENSE.txt`.
- `@noble/hashes` 1.8.0: módulos oficiais do pacote npm, licença MIT em `dist/js/vendor/noble/LICENSE.txt`. Somente o import de `crypto` em `utils.js` foi tornado relativo para uso direto no navegador; comentários de sourcemap foram removidos. Demais algoritmos preservados.
- Referência primária: https://github.com/paulmillr/noble-hashes/tree/1.8.0
- Web Locks: https://developer.mozilla.org/en-US/docs/Web/API/Web_Locks_API
- Web Crypto: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveBits
- A aplicação não carrega esses sites durante o uso.
