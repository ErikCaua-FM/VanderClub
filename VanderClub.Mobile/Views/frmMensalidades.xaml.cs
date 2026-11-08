using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmMensalidades : ClubPage
{
    private readonly AppState state;
    public frmMensalidades(AppState state) : base(state, "Mensalidades") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); await Run(async () =>
        {
            var bills = await State.Club.MensalidadesAsync(); var context = await State.Club.ContextoAsync();
            Body.Clear(); Heading("Tudo em dia, sem complicação", context.Associacao.PlanoNome + " • " + context.Associacao.Condicao);
            Body.Add(Ui.Banner("Consulte suas parcelas e simule pagamentos. A simulação atualiza a situação no sistema, sem cobrança real."));
            int paid = bills.Count(m => m.PagoUtc.HasValue); long remaining = bills.Where(m => !m.PagoUtc.HasValue).Sum(m => m.ValorCentavos);
            Body.Add(Ui.Row(Ui.Card(Ui.Stack(Ui.Text("Parcelas pagas", 12, Ui.Muted), Ui.Text($"{paid}/{bills.Count}", 26, Ui.Teal, true))),
                Ui.Card(Ui.Stack(Ui.Text("Saldo a vencer / aberto", 12, Ui.Muted), Ui.Text(Horario.Dinheiro(remaining), 20, Ui.Purple, true)))));
            foreach (var m in bills)
            {
                string status = Horario.Status(m, State.Hoje);
                var details = Ui.Stack(Ui.Row(Ui.Text("Parcela " + m.Parcela, 20, Ui.Ink, true), Ui.Badge(status, Ui.StatusColor(status))),
                    Ui.Text(Horario.Dinheiro(m.ValorCentavos), 26, Ui.Purple, true), Ui.Text($"Vencimento: {m.Vencimento:dd/MM/yyyy}", 14, Ui.Muted),
                    Action(m.PagoUtc.HasValue ? "Ver detalhes" : "Simular pagamento", () => Go(new frmPagamento(State, m)), true));
                Body.Add(Ui.Card(details));
            }
        });
    }
}
