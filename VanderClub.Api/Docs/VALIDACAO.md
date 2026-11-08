# Evidência de validação

Verificado em 03/11/2026, ambiente isolado, sem acesso ao MySQL do usuário.

- API compilada em Release com SDK .NET 10.0.403: **zero erros e zero avisos**.
- Banco **MySQL Community Server 8.4.7 real**, importando o SQL anterior completo, sem alteração do esquema.
- Chamadas HTTP reais por HTTPS, sessões, cookies, CSRF, transações, gatilhos e chaves estrangeiras ativos.
- **63 verificações aprovadas**, listadas abaixo.
- Transporte JavaScript original (`ApiService` da Web) executado contra a API: login, bootstrap, mensagem, atualização, CSRF e logout.
- Transporte C# original (`ApiService`, DTOs, conversores e contexto JSON do Mobile) compilado em um executor .NET separado e executado contra a API, usando **SQLite real** para o cache. Validadas consultas dos módulos, datas, perfil, atendimento, chat e logout.
- O teste de reserva concorrente enviou duas requisições de associados diferentes simultaneamente: uma gravou e a outra recebeu conflito 409.
- Dados fictícios, contas e certificados usados nas verificações existiram apenas no banco descartável de teste; não acompanham os dados iniciais da aplicação.

## Limites da validação

Não foi executada a interface MAUI em telefone/emulador nem o aplicativo no Windows do usuário. O teste de transporte usa o código original, mas não substitui a conferência visual e de navegação nos aparelhos. A instalação/execução dos assistentes PowerShell e confiança do certificado devem ser conferidas no PC Windows. A integração bancária não existe e não foi simulada como pagamento real.

Os problemas de interface listados em `AJUSTES_NOS_CLIENTES.md` permanecem pendentes por solicitação do usuário. As respostas HTTP e o esquema JSON foram validados; isso não significa que uma Web ainda sem URL configurada já esteja conectada.

## Verificações aprovadas

1. Bootstrap cria apenas um Presidente e preserva conta existente.
2. Senha nao fica em texto puro.
3. Saude HTTP confirma schema MySQL real.
4. Bootstrap exige autenticacao.
5. Login Web exige Origin autorizado.
6. Cookie Web Secure e HttpOnly.
7. Bootstrap inicial sem dados ficticios.
8. Mutacao exige CSRF.
9. Idempotencia retorna o mesmo ID.
10. Mesma chave com outro payload retorna 409.
11. Concorrencia otimista da unidade.
12. ADM Local nao cria unidades.
13. Cadastro transacional e login Mobile com repeticao idempotente.
14. Contexto informa unidade e titular.
15. Parcelas inteiras preservam o total.
16. Plano local nao acessa outra unidade.
17. Plano de rede acessa ambas.
18. Minha Conta persiste e rejeita versao antiga.
19. Pendentes tambem consomem cota.
20. Aprovacao Web aparece no Mobile.
21. Visitantes filtrados por data.
22. Visitante somente do responsavel pode ser cancelado.
23. Resposta Web persiste no atendimento Mobile.
24. Solicitacoes privadas por autor.
25. Reclamacao usa historico proprio.
26. Chat define autor e classe no servidor.
27. Aviso pode ser fixado por administrador.
28. Reservas concorrentes: uma vence, outra recebe 409.
29. Intervalos adjacentes permitidos.
30. Esporte rejeita mais de tres horas.
31. Salao aceita cinco horas.
32. Agenda nao expoe outro associado.
33. Evento respeita reserva existente.
34. Evento Web aparece no Mobile.
35. Cancelamento do evento libera a agenda.
36. Eleitorado abre para titulares elegiveis.
37. Voto unico com repeticao idempotente segura.
38. Apuracao Web anonima.
39. Apuracao Mobile reflete voto.
40. Votacao encerrada rejeita votos.
41. Somente Presidente recebe; data futura rejeitada.
42. Baixa manual aparece no Mobile e nao duplica.
43. Versionar plano preserva contrato original.
44. Arquivar retira oferta sem cancelar associado.
45. ADM Local nao recebe contratos/financas de outra unidade.
46. Chamado entre unidades traz autor resumido sem vazar contrato.
47. Resposta da unidade visitada chega ao associado de rede.
48. Suspensao permite consultar conta e mensalidades.
49. Suspenso nao usa reservas/chat nem reativa com atraso.
50. Regularizacao e reativacao restauram associacao.
51. Dependente nao acessa financas nem gerencia beneficiarios.
52. Dependente aprovado nao vota.
53. Edicao de beneficiario exige nova aprovacao e revoga sessao.
54. Recusa preserva motivo no Mobile.
55. Exclusao logica preserva historico no banco.
56. Cancelar reserva libera intervalo para nova reserva.
57. Catalogo administrativo de tipos inclui saloes sem teto.
58. ApiService JavaScript original integra login, bootstrap, CSRF, chat, refresh e logout.
59. ApiService C# e DTOs originais integram MySQL com cache SQLite real.
60. Bootstrap satisfaz esquema JSON da Web 2.0.
61. Esquema mantem 92 chaves estrangeiras.
62. Logout revoga token Mobile.
63. Logout revoga cookie Web.
