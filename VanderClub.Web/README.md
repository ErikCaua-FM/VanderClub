# VanderClub Web — 2.0

Portal administrativo em HTML, CSS e JavaScript preparado para consumir a API VanderClub. A identidade visual e as telas aprovadas foram preservadas. Esta entrega não contém backend nem MySQL.

## Mudanças

- Removidos laboratório, simulador, contas prontas, seed, autenticação local e persistência de registros no navegador.
- `dist/js/api.js` centraliza as chamadas HTTP. A sessão usa cookie seguro e CSRF fornecidos pela API, com restauração e expiração.
- As operações existentes esperam confirmação do servidor. Não há sucesso fictício nem cadastro público de administradores.
- Eventos incluem Confirmado, além de Agendado, Cancelado e Realizado, para alinhar com o Mobile.
- O Presidente pode registrar recebimento já conferido, com data, meio e referência. Isso chama a API e não realiza transação bancária. O botão de pagamento simulado foi removido.
- Alterações de planos criam novas versões; retirada da oferta preserva os contratos existentes como regra obrigatória do servidor.
- Atualização a cada 30 segundos, com a página visível e sem sobrescrever formulários, além do botão Atualizar. Rascunhos de chat são preservados.
- Versões, idempotência, timeout e tratamento de erros preparados para o backend.

## Configuração

1. Implemente o contrato em `docs/CONTRATO_API.openapi.json` e `docs/CONTRATO_API.md`.
2. Preencha apiBaseUrl em `dist/config.js`, por exemplo https://api.seudominio.com/api/v1/.
3. Configure cookie vanderclub_admin com HttpOnly/Secure, CSRF e CORS conforme o contrato.
4. Sirva a pasta dist por HTTP local ou HTTPS na implantação. Não utilize file:// para módulos JavaScript.

A URL vem vazia de propósito. O portal abre o login, mas não libera acesso real sem a API. Não existem usuários ou senhas de teste. Registros da antiga demonstração não são importados como dados reais.

Para abrir com Node.js instalado, execute `npm start` e acesse http://localhost:8080. No Windows, use também ABRIR_NO_WINDOWS.cmd. Esse servidor é estático, não é a API. A interface não exige npm install nem CDN.

## Organização

- dist/index.html e styles.css: página e estilos.
- dist/config.js: endereço público do serviço, sem segredos.
- dist/js/api.js: transporte, sessão e operações remotas.
- dist/js/core.js: formatação e validações de interface.
- dist/js/app.js: telas, formulários e navegação.
- dist/assets: imagens, marca e fontes.
- docs/CONTRATO_API.*: contrato compartilhado.
- scripts/empacotar.py: ZIP portátil sem metadados de hospedagem.

A API deve autorizar cada operação e filtrar dados por papel/unidade. Esconder botões não é uma barreira de segurança. MySQL, transações e sincronização Mobile/Web precisam de validação após a implementação do servidor.

Foram verificadas a sintaxe JavaScript, o transporte e as principais telas no navegador com respostas controladas externas ao produto. Os arquivos de verificação não acompanham o pacote. Não foi realizada integração ponta a ponta com API de produção.
