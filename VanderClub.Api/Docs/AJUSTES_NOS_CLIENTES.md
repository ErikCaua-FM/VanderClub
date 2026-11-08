# Ajustes identificados — ainda não aplicados

Foram examinados o Mobile 2.0, a Web 2.0 e o SQL `vanderclub-1.0.0` existentes. Esta entrega cria apenas a API e seus arquivos de suporte. A integração dos serviços foi testada usando MySQL real e o transporte original dos clientes; a instalação no seu PC e o uso pelas interfaces dependem dos pontos abaixo.

## Para conectar os aplicativos

| Aplicação | Arquivo / ponto | Alteração necessária |
|---|---|---|
| Mobile | `Helpers/ApiConfig.cs`, `BaseUrl` | Informar a URL HTTPS real terminada em `/api/v1/`. No Windows do mesmo PC, pode ser `https://localhost:7147/api/v1/`. No telefone, precisa de um endereço acessível e certificado válido. |
| Web | `dist/config.js`, `apiBaseUrl` | Informar a URL da API. Não adicionar usuário/senha MySQL nem qualquer segredo ao JavaScript. |
| Implantação | CORS, cookie e certificado | Preferir servir a Web na mesma origem da API. Caso contrário, configurar as origens exatas e, se necessário, cookie entre sites. |

## Correções de coerência na Web

1. **Atendimento a associado de outra unidade.** Um plano de rede permite abrir chamado na unidade visitada. O administrador dessa unidade recebe o chamado, mas não deve receber o contrato, as mensalidades e todo o cadastro pessoal da unidade de origem. A função `ticketDetail` em `dist/js/app.js` acessa `D.people.find(...).name` sem tratar a ausência. A API acrescenta `ticket.requester = { id, name, type }`. A futura correção deverá usar esse resumo como alternativa na lista e no detalhe, e mostrar “Ver sócio” somente se a pessoa estiver em `D.people`. Não ampliar o bootstrap para contornar esse problema: isso exporia dados de outra unidade.
2. **Carência do plano.** No formulário de planos, `graceDays` está rotulado como “Tolerância de atraso (dias)”, mas corresponde ao campo `carencia_dias` do banco e à espera inicial para reservar. Alterar o rótulo para **“Carência para reservas (dias)”** e a explicação. Se for necessária tolerância financeira, ela precisa ser uma regra/campo separado; nesta API atraso começa após o vencimento civil da unidade.
3. **Limite de reservas futuras.** O formulário aceita `0` e o descreve como bloqueio. O esquema exige valor maior que zero. Alterar `min` para **1** e a explicação; a API aceita de 1 a 100. Se quiser permitir plano sem reservas, será necessária uma migração específica e ajuste coordenado da regra.
4. **Cadastro de áreas.** Acrescentar uma tela de administração de áreas se desejar realizar toda a configuração pela Web. A API já oferece GET/POST/PUT e o assistente `Cadastrar-Area.ps1`; portanto o funcionamento das reservas não depende de inventar registros no banco.

## Edição de beneficiários no Mobile

O formulário permite trocar “Convidado” por “Dependente”. O gatilho `bu_membros_contrato` do banco torna a categoria imutável, preservando o histórico. A API permite editar os dados, envia-os para nova aprovação e retorna uma mensagem clara caso tentem trocar a categoria.

A solução compatível com o banco atual é desabilitar o seletor de categoria durante uma edição. Se a regra desejada for permitir a conversão, devemos projetar uma migração com histórico, cotas e nova aprovação. Não removi o gatilho nem liberei a troca silenciosamente.

## Banco

**Nenhuma migração é necessária para executar a API entregue.** Ela usa o esquema anterior sem alterações. Há uma cópia idêntica do SQL em `Banco/` para referência. A conta técnica precisa ter os privilégios DML já previstos naquele script.

Uma migração será necessária apenas se escolhermos permitir mudança de categoria de beneficiário, planos com zero reservas, tolerância financeira separada ou outras regras novas. Não desative chaves estrangeiras/gatilhos para fazer esses casos passarem.

## Funcionalidades para uma etapa posterior

- Ativação de login individual de convidado/dependente: o vínculo e a aprovação existem, mas os clientes atuais não coletam e-mail/senha nem oferecem convite de ativação. Não criei senhas padrão. A API já aplica as restrições de dependente/convidado a contas existentes aprovadas.
- Recuperação automática de senha por e-mail, renovação de contrato, estorno financeiro e envio de anexos não têm telas/contratos ativos nesta versão. Nenhuma rota fictícia foi criada para simular esses recursos.
- O pagamento visual do Mobile não autoriza quitar parcelas. A baixa real nesta etapa é registrada pelo Presidente após conferência, e o Mobile consulta o mesmo resultado. Integração bancária permanece fora do escopo combinado.
- Histórico de chat e dados da Web usam as consultas/polling existentes. Para volumes maiores, paginar o bootstrap e o histórico requer ajuste conjunto do contrato dos clientes.

Esses pontos foram registrados para a próxima alteração coordenada. Os arquivos dos aplicativos existentes permanecem como estavam.
